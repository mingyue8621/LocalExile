using System.Net.Sockets;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using GameServer.Core.World;
using GameServer.Modding;
using static Program;
using static PortalsFeature;

namespace GameServer.Modules.M_portals;

/// <summary>Town portals, Portal Scrolls, the map device and map portals.</summary>
public sealed class PortalsModule : IModule
{
    /// <inheritdoc/>
    public void Load(IModuleContext module)
    {
        GameEvents.AreaGenerated.Add(module.Manifest.Id, async conn =>
        {
            var (remote, stream, ciphers) = conn.Wire;
            ushort area = conn.Layout.Hash;
            ulong instance = conn.InstanceNumber;
            // 每一步各自兜住：前面任何一步失败（例如角色名还没绑上就去查地图装置），都不能把后面的步骤带掉。
            async Task Step(string name, Func<Task> run)
            {
                try { await run(); }
                catch (Exception e) { Console.WriteLine($"[gateway] {remote} 区域生成：{name} 失败 {e.GetType().Name}: {e.Message}"); }
            }
            // 这几步都要创建世界对象，而 0x7d 的收尾阶段 WorldOutput 还没有 Begin（StartWorldFrames 是在本回调
            // 返回之后才调用的）：这时 lockstep 的批次发送会在 SendRecords 里被静默拒掉 —— 返回 false，
            // 一条日志都不写 —— 门在服务端"建好了"却从来没有发到客户端，城镇里一直没有返回的门就是这个原因。
            // 所以这些步骤排到世界帧启动之后的独立任务里跑：本回调立即返回，不阻塞 StartWorldFrames（否则死锁）。
            _ = Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < 300 && !conn.Closed && !conn.WorldOutput.Started; i++) await Task.Delay(10);
                    // 还要等客户端把 Network Mode 报上来：LockstepClient 默认 false，客户端连上后约 1 s 才随 tick 到达。
                    // 在那之前建的对象会按 predictive 写出（0x2bb 的 ordering 是 0），lockstep 客户端解析不了，
                    // 于是门"发出去了"却从不出现（19:07 抓包对比：野外 ordering=ffffffff，城镇 00000000）。
                    for (int i = 0; i < 250 && !conn.Closed && !conn.LockstepClient; i++) await Task.Delay(10);
                    Console.WriteLine($"[gateway] {remote} 区域生成：帧已启动={conn.WorldOutput.Started} lockstep={conn.LockstepClient}，开始放置传送门/摆设");
                    if (conn.Closed || conn.Layout.Hash != area || conn.InstanceNumber != instance) return;
                    await Step("回城门", () => SpawnTownPortals(remote, stream, ciphers, conn));
                    await Step("藏身处摆设", () => SpawnHideoutDoodads(remote, stream, ciphers, conn));
                    await Step("地图返回门", () => SpawnMapReturnPortal(remote, stream, ciphers, conn));
                    // The portals of the character's open map stand at the device again.
                    string? who = CharacterName(conn);
                    if (who is { Length: > 0 } && MapDeviceHere(conn) is { } device && MapDevice.OpenOf(who) is { PortalsLeft: > 0 } open
                        && (open.ReturnTo is null ? !device.Campaign : open.ReturnTo.Area == conn.Layout.Hash && open.ReturnTo.Instance == conn.InstanceNumber))
                        await Step("地图装置的门", () => SpawnMapPortals(remote, stream, ciphers, conn, open, firstAfterMs: MapPortalEntryMs));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[gateway] {remote} 区域生成：帧后放置失败 {e.GetType().Name}: {e.Message}");
                }
            });
        });
        GameEvents.ObjectUse.Add(module.Manifest.Id, async (conn, use, requestedAt) =>
        {
            var (remote, stream, ciphers) = conn.Wire;
            if (conn.BackPortal is { } home && use.TargetId == home.Id && OpenPortals.TryGetValue(CharacterName(conn), out var origin))
            {
                if (!TryBeginObjectWalk(conn, requestedAt, home.X, home.Y, AreaTransitionObjectSize, out var homeWalk)) return true;
                var (homeAction, homeNow, _, _, homeArrive) = homeWalk;
                if (homeArrive > homeNow) await Task.Delay(TimeSpan.FromSeconds(homeArrive - homeNow));
                if (!PlayerActionCurrent(conn, homeAction)) return true;
                if (await ExpireTownPortal(conn, origin))
                {
                    if (!conn.Closed && !conn.LeavingArea && conn.BackPortal?.Id == home.Id)
                    {
                        conn.BackPortal = null;
                        await global::Game.SendWorldRemoval(remote, stream, ciphers, conn, home.Id, "expired town portal");
                    }
                    return true;
                }
                if (PlayerActionCurrent(conn, homeAction) && OpenPortals.TryGetValue(CharacterName(conn), out var current) && current == origin)
                    await ChangeArea(remote, stream, ciphers, conn, new PendingTransfer(origin.Area, origin.Instance, false, ArriveAt: (origin.X, origin.Y)),
                        "back through the town portal");
                return true;
            }
            if (conn.ReturnPortal is { } back && use.TargetId == back.Id)
            {
                await UseMapReturnPortal(remote, stream, ciphers, conn, use, (back.X, back.Y), requestedAt);
                return true;
            }
            (int X, int Y, int Index)? portal;
            lock (conn.MapPortals) portal = conn.MapPortals.TryGetValue(use.TargetId, out var at) ? at : null;
            if (portal is { } p)
            {
                await UseMapPortal(remote, stream, ciphers, conn, use, p, requestedAt);
                return true;
            }
            HideoutCatalog.Doodad? doodad;
            lock (conn.HideoutDoodads) doodad = conn.HideoutDoodads.GetValueOrDefault(use.TargetId);
            if (doodad is null) return false;
            await UseMapDevice(remote, stream, ciphers, conn, use, doodad, requestedAt);
            return true;
        });
        // The Portal skill: a town portal, as the Portal Scroll's, once its cast (2.5 s) is over.
        GameEvents.SkillCast.Add(module.Manifest.Id, (conn, effect, skillId, from, current) =>
        {
            if (effect != "Portal") return null;
            var (remote, stream, ciphers) = conn.Wire;
            if (!PortalAllowed(conn)) { Console.WriteLine($"[gateway] {remote} skill 0x{skillId:x4} Portal: not here"); return false; }
            var gem = GemSkillEffects(conn).FirstOrDefault(g => g.Id == skillId);
            if (CastMs(conn, gem.Effect) is not int castMs) return false;
            _ = Task.Delay(castMs).ContinueWith(async _ =>
            {
                if (current()) await OpenTownPortal(remote, stream, ciphers, conn, from.X, from.Y, "Portal skill");
            });
            return true;
        });
        GameEvents.ItemUse.Add(module.Manifest.Id, async (conn, use, item, type) =>
        {
            if (type.Id != PortalScroll) return false;
            var (remote, stream, ciphers) = conn.Wire;
            await UsePortalScroll(remote, stream, ciphers, conn, use, item, type);
            return true;
        });
        GameEvents.Packet(InstanceMapDevice.ActivateId).Add(module.Manifest.Id, async (conn, bytes) =>
        {
            var (remote, stream, ciphers) = conn.Wire;
            await ActivateMapDevice(remote, stream, ciphers, conn, InstanceMapDevice.ReadActivate(bytes));
            return true;
        });
        GameEvents.Packet(InstanceMapDevice.SelectMapId).Add(module.Manifest.Id, async (conn, bytes) =>
        {
            var (remote, stream, ciphers) = conn.Wire;
            await SelectAtlasMap(remote, stream, ciphers, conn, InstanceMapDevice.ReadSelectMap(bytes));
            return true;
        });
        GameEvents.Packet(InstanceMapDevice.MoveId).Add(module.Manifest.Id, async (conn, bytes) =>
        {
            var (remote, stream, ciphers) = conn.Wire;
            await MoveToMapDevice(remote, stream, ciphers, conn, InstanceMapDevice.ReadMove(bytes));
            return true;
        });
    }
}
