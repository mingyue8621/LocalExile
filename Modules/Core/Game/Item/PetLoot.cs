using System.Net.Sockets;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;

static partial class Game
{
    // 捡物宠物（装扮里 Pet 槽那两只，见 CosmeticsModule.SpawnPets）：每 0.4 秒把玩家周围 40 格内的堆叠物收进主背包。
    //   只捡能堆叠的（MaxStack > 1）：通货、碎片、精华、命运卡、圣甲虫这些；装备、宝石、任务物品一律不碰。
    //   金币另走 PickUpGold（进仓库的金库，和玩家自己捡一样）。
    //   一次最多 3 件，由近到远；背包放不下就留在原地，下一轮再试，不会凭空消失。
    // 落库与封包和玩家自己点拾取（PickUpGroundItem 的堆叠分支）完全同一条路径：Ground.Take → AddStacked → 保存，
    // 再发 0x2bd 移除物件、0x00bf 背包变更。不走路、不播玩家动作，所以看起来就是宠物在身边把东西收了。
    const double PetLootRange = 40;
    const int PetLootIntervalMs = 400;
    const int PetLootPerTick = 3;

    /// <summary>Whether the pet would take this: gold (it goes to the stash's gold) or a stack of something.</summary>
    internal static bool PetLootable(WorldItemObject ground) => IsGold(ground) || (ShapeOf(ground.Item)?.MaxStack ?? 1) > 1;

    /// <summary>The pet's pickup loop, started with the pets' follow loop and ended with the connection.</summary>
    internal static async Task RunPetLoot(string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn)
    {
        try
        {
            while (!conn.Closed)
            {
                await Task.Delay(PetLootIntervalMs);
                if (conn.Closed) break;
                lock (conn.Pets) if (conn.Pets.Count == 0) continue;
                if (HardcoreProgressBlocked(conn)) continue;
                await PetLootOnce(remote, stream, ciphers, conn);
            }
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException or SocketException or InvalidOperationException) { }
    }

    static async Task PetLootOnce(string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn)
    {
        double now = conn.Clock.Elapsed.TotalSeconds;
        var (x, y) = conn.Walk.Position(now);
        var near = conn.Instance.Ground.Within(x, y, PetLootRange, PetLootable)
            .OrderBy(g => (double)(g.Item.GridX - x) * (g.Item.GridX - x) + (double)(g.Item.GridY - y) * (g.Item.GridY - y))
            .Take(PetLootPerTick)
            .ToList();
        foreach (var (id, ground) in near)
        {
            if (conn.Closed) return;
            await PetPickUp(remote, stream, ciphers, conn, id, ground);
        }
    }

    // Takes one ground item into the main inventory, the way the player's own stacking pickup does, without the walk.
    static async Task PetPickUp(string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn, uint id, WorldItemObject ground)
    {
        if (IsGold(ground))
        {
            await PickUpGold(remote, stream, ciphers, conn, id, ground, "by the pet");
            return;
        }
        if (ShapeOf(ground.Item) is not { MaxStack: > 1 } shape) return;
        var items = ItemsOf(conn);
        PlayerInventories.Stacking stacked;
        WorldItemObject? rest = null;
        uint restId = 0;
        lock (conn.Monsters)
        {
            if (HardcoreProgressBlocked(conn)) return;
            // No room: it stays on the ground and is tried again on the next pass.
            if (!items.Fits(PlayerInventories.MainInventory, ground.Item, ShapeOf)) return;
            if (conn.Instance.Ground.Take(id) is null) return;   // already picked up
            stacked = KeepOnGroundIfNotSaved(conn, ground, () => items.AddStacked(PlayerInventories.MainInventory, ground.Item, ShapeOf));
            if (stacked.Left > 0)
            {
                // Fits said yes, so this is not expected; the rest goes back on the ground rather than vanish.
                rest = new WorldItemObject(ground.GridX, ground.GridY, ground.Item with { StackSize = (ushort)stacked.Left });
                restId = conn.Instance.Ground.Add(rest, conn, shared: true, justDropped: false);
            }
        }
        if (rest is not null)
            await Send(remote, stream, ciphers,
                InstanceEntities.CreateObject(restId, InstanceEntities.ObjectTypeHash(WorldItemObject.MetadataPath), rest.Blob()),
                $"0x02bb create world item 0x{restId:x} (the rest of 0x{id:x})");
        await Send(remote, stream, ciphers, InstanceProtocol.RemoveObject(id), $"0x02bd remove world item 0x{id:x} (taken by the pet)");
        await Send(remote, stream, ciphers,
            InstanceItems.InventoryChange(PlayerInventories.InventoryId(PlayerInventories.MainInventory),
                items.NextRevision(PlayerInventories.MainInventory), stacked.RemovedIds, stacked.AddedItems),
            $"0x00bf inventory 1: pet took {ground.Item.StackSize} ({stacked.Changed.Count} stack(s) grown, {stacked.Added.Count} new)");
    }
}
