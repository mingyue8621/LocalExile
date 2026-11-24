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
    /// <summary>This module's game data, built in a fixed order when it loads: before the Gateway listens, as the
    /// Gateway's own (WorldLoader). Tests that use the module call it once.</summary>
    public static void LoadData()
    {
        global::PortalsRules.LoadRulesSettingsData();
    }

    /// <inheritdoc/>
    public void Load(IModuleContext module)
    {
        LoadData();
        // The portals a player can take are their own view (each keeps its own portal state): theirs alone to see.
        GameEvents.PlayerEntered.Add(module.Manifest.Id, async player =>
        {
            Program.ViewOf.Value = player;
            try
            {
                ushort area = player.Layout.Hash;
                ulong instance = player.InstanceNumber;
                // 每一步各自兜住：前面任何一步失败（例如角色名还没绑上就去查地图装置），都不能把后面的步骤带掉。
                async Task Step(string name, Func<Task> run)
                {
                    try { await run(); }
                    catch (Exception e) { Console.WriteLine($"[gateway] {player.Connection.Remote} 区域生成：{name} 失败 {e.GetType().Name}: {e.Message}"); }
                }
                // 这几步都要创建世界对象，而 0x7d 的收尾阶段 WorldOutput 还没有 Begin（StartWorldFrames 是在本回调
                // 返回之后才调用的）：这时 lockstep 的批次发送会在 SendRecords 里被静默拒掉 —— 返回 false，
                // 一条日志都不写 —— 门在服务端"建好了"却从来没有发到客户端，城镇里一直没有返回的门就是这个原因。
                // 所以这些步骤排到世界帧启动之后的独立任务里跑：本回调立即返回，不阻塞 StartWorldFrames（否则死锁）。
                _ = Task.Run(async () =>
                {
                    var previousView = Program.ViewOf.Value;
                    Program.ViewOf.Value = player;
                    try
                    {
                        for (int i = 0; i < 300 && !player.Closed && !player.Connection.WorldOutput.Started; i++) await Task.Delay(10);
                        // 还要等客户端把 Network Mode 报上来：LockstepClient 默认 false，客户端连上后约 1 s 才随 tick 到达。
                        // 在那之前建的对象会按 predictive 写出（0x2bb 的 ordering 是 0），lockstep 客户端解析不了，
                        // 于是门"发出去了"却从不出现（19:07 抓包对比：野外 ordering=ffffffff，城镇 00000000）。
                        for (int i = 0; i < 250 && !player.Closed && !player.Connection.LockstepClient; i++) await Task.Delay(10);
                        Console.WriteLine($"[gateway] {player.Connection.Remote} 区域生成：帧已启动={player.Connection.WorldOutput.Started} lockstep={player.Connection.LockstepClient}，开始放置传送门/摆设");
                        if (player.Closed || player.Layout.Hash != area || player.InstanceNumber != instance) return;
                        await Step("回城门", () => SpawnTownPortals(player));
                        await Step("藏身处摆设", () => SpawnHideoutDoodads(player));
                        await Step("地图返回门", () => SpawnMapReturnPortal(player));
                        // 藏身处实例在最后一个人离开六分钟后消失，装置也跟着消失：那一局的传送门原本立在那里，
                        // 现在跟随角色来到脚下这个实例，所以要在摆门之前把它们挪过来。
                        await Step("跟随装置实例", () => PortalsRules.FollowDeviceToThisInstance(player));
                        // Everyone in this exact device instance sees and spends the same run's portals (upstream's shared
                        // map-device portals, by area and instance rather than by the character's own open map).
                        if (MapDevice.AtEntrance(player.Layout.Hash, player.InstanceNumber) is { PortalsLeft: > 0 } open)
                            await Step("地图装置的门", () => SpawnMapPortals(player, open, firstAfterMs: MapPortalEntryMs, standing: true));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[gateway] {player.Connection.Remote} 区域生成：帧后放置失败 {e.GetType().Name}: {e.Message}");
                    }
                    finally { Program.ViewOf.Value = previousView; }
                });
            }
            finally { Program.ViewOf.Value = null; }
        });
        GameEvents.ObjectUse.Add(module.Manifest.Id, async (player, use, requestedAt) =>
        {
            if (player.BackPortal is { } home && use.TargetId == home.Id && OpenPortals.TryGetValue(CharacterName(player), out var origin))
            {
                if (!TryBeginObjectWalk(player, requestedAt, home.X, home.Y, AreaTransitionObjectSize, out var homeWalk)) return true;
                var (homeAction, homeNow, _, _, homeArrive) = homeWalk;
                if (homeArrive > homeNow) await AreaDelay(player, homeArrive - homeNow);
                if (!PlayerActionCurrent(player, homeAction)) return true;
                if (await ExpireTownPortal(player, origin))
                {
                    if (!player.Closed && !player.LeavingArea && player.BackPortal?.Id == home.Id)
                    {
                        player.BackPortal = null;
                        await global::Game.SendWorldRemoval(player, home.Id, "expired town portal");
                    }
                    return true;
                }
                if (PlayerActionCurrent(player, homeAction) && OpenPortals.TryGetValue(CharacterName(player), out var current) && current == origin)
                    await ChangeArea(player, new PendingTransfer(origin.Area, origin.Instance, false, ArriveAt: (origin.X, origin.Y)),
                        "back through the town portal");
                return true;
            }
            if (player.ReturnPortal is { } back && use.TargetId == back.Id)
            {
                await UseMapReturnPortal(player, use, (back.X, back.Y), requestedAt);
                return true;
            }
            (string Owner, int X, int Y, ushort ReturnToTown)? peerPortal = null;
            lock (player.PeerPortals)
                if (player.PeerPortals.TryGetValue(use.TargetId, out var pp)) peerPortal = pp;
            if (peerPortal is { } clickedPeer)
            {
                await UseMapReturnPortal(player, use, (clickedPeer.X, clickedPeer.Y), requestedAt, clickedPeer.ReturnToTown);
                return true;
            }
            (int X, int Y, int Index)? portal;
            lock (player.MapPortals) portal = player.MapPortals.TryGetValue(use.TargetId, out var at) ? at : null;
            if (portal is { } p)
            {
                await UseMapPortal(player, use, p, requestedAt);
                return true;
            }
            HideoutCatalog.Doodad? doodad;
            lock (player.HideoutDoodads) doodad = player.HideoutDoodads.GetValueOrDefault(use.TargetId);
            if (doodad is null) return false;
            await UseMapDevice(player, use, doodad, requestedAt);
            return true;
        });
        // The Portal skill: a town portal, as the Portal Scroll's, once its cast (2.5 s) is over.
        GameEvents.SkillCast.Add(module.Manifest.Id, (player, effect, skillId, from, current) =>
        {
            if (effect != "Portal") return null;
            if (!PortalAllowed(player)) { Console.WriteLine($"[gateway] {player.Connection.Remote} skill 0x{skillId:x4} Portal: not here"); return false; }
            var gem = GemSkillEffects(player).FirstOrDefault(g => g.Id == skillId);
            if (CastMs(player, gem.Effect) is not int castMs) return false;
            _ = AreaFrames.Start(async () =>
            {
                await AreaDelay(player, castMs / 1000.0);
                if (current()) await OpenTownPortal(player, from.X, from.Y, "Portal skill");
            });
            return true;
        });
        GameEvents.ItemUse.Add(module.Manifest.Id, async (player, use, item, type) =>
        {
            if (type.Id != PortalScroll) return false;
            await UsePortalScroll(player, use, item, type);
            return true;
        });
        GameEvents.Packet(InstanceMapDevice.ActivateId).Add(module.Manifest.Id, async (player, bytes) =>
        {
            await ActivateMapDevice(player, InstanceMapDevice.ReadActivate(bytes));
            return true;
        });
        GameEvents.Packet(InstanceMapDevice.ActivateMemoryLineId).Add(module.Manifest.Id, async (player, bytes) =>
        {
            await ActivateMemoryLine(player, InstanceMapDevice.ReadActivateMemoryLine(bytes));
            return true;
        });
        GameEvents.Packet(InstanceMapDevice.ActivateMavenInvitationId).Add(module.Manifest.Id, async (player, bytes) =>
        {
            await ActivateMavenInvitation(player, InstanceMapDevice.ReadActivateMavenInvitation(bytes));
            return true;
        });
        GameEvents.Packet(InstanceMapDevice.SelectMapId).Add(module.Manifest.Id, async (player, bytes) =>
        {
            await SelectAtlasMap(player, InstanceMapDevice.ReadSelectMap(bytes));
            return true;
        });
        GameEvents.Packet(InstanceMapDevice.MoveId).Add(module.Manifest.Id, async (player, bytes) =>
        {
            await MoveToMapDevice(player, InstanceMapDevice.ReadMove(bytes));
            return true;
        });
        // Ctrl + click with the map device open, for the items that are not Atlas Idols: a map moves with one of these, and
        // without them the click reached no one and the map had to be dragged in.
        foreach (ushort clicked in new[] { InstanceMapDevice.MoveIntoDeviceId, InstanceMapDevice.MoveOutOfDeviceId })
            GameEvents.Packet(clicked).Add(module.Manifest.Id, async (player, bytes) =>
            {
                var click = InstanceMapDevice.ReadItemClick(bytes);
                // Which way it goes is where the item is, so the message's own direction flag is not needed here
                // (ItemMoves.MoveToMapDevice says so): a click in the inventory takes it in, one in the slots takes it out.
                Console.WriteLine($"[gateway] {player.Connection.Remote} <= 0x{clicked:x4} Ctrl + click item {click.ItemId} on the map device " +
                    $"({(click.IntoDevice ? "into" : "out")}, device 0x{click.Device:x})");
                await MoveToMapDevice(player,
                    new InstanceMapDevice.Move(click.ItemId, click.Flags, click.IntoDevice, 0, 0));
                return true;
            });
    }
}
