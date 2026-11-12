using System.Net.Sockets;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;

static partial class Game
{
    // 捡物宠物（装扮里 Pet 槽那两只，见 CosmeticsModule.SpawnPets）：每 0.4 秒把玩家周围 60 格内的通货与金币收进主背包。
    //   只用 2 号槽（slot 19）那只；1 号槽（slot 18）那只只跟着走，不捡东西。
    //   通货 = 能堆叠的（MaxStack > 1：通货、碎片、精华、命运卡、圣甲虫）加上同样算通货但不堆叠的（共振器、诱饵、
    //   部分门票碎片，见 PetLootClasses）。装备、宝石、药剂、地图一律不碰。
    //   金币另走 PickUpGold（进仓库的金库，和玩家自己捡一样）。
    //   一轮最多收 3 件，由近到远；收不下的直接跳过，不会堵住后面能收的（以前是取最近 3 件，3 件都塞不下就原地卡死）。
    // 落库与封包和玩家自己点拾取（PickUpGroundItem 的堆叠分支）完全同一条路径：Ground.Take → AddStacked → 保存，
    // 再发 0x2bd 移除物件、0x00bf 背包变更。不走路、不播玩家动作，所以看起来就是宠物在身边把东西收了。
    // 60 是默认值，可在 world.db 的 system_settings 里用 pet_loot_radius 改（不用改代码、不用重新发版）。
    // 历史：硬编码 80 → 120（脚步跟不上掉落）→ 60（用户 2026-11-12 定：范围不用那么大）。
    static double PetLootRange => SystemSettings.Default.PetLootRadius;
    const int PetLootIntervalMs = 400;
    const int PetLootBusyIntervalMs = 1000;   // 地上有货但一件都收不进来（背包满）时放慢，别空转
    const int PetLootPerTick = 3;
    const int PetLootAttempts = 12;           // 一轮最多试几件：收不下的让位给后面的
    const byte PetLootSlot = 19;              // 只有 2 号槽的宠物捡东西

    // client.db 里 ItemClasses.Id 的通货类。能堆叠的那些本来就走 MaxStack > 1 那条路，这里补上同样算通货、
    // 但不堆叠的（Alchemical Resonator 8 个、Farric Lure、Breachstone 这类 MaxStack 为 1 的）。
    static readonly HashSet<string> PetLootClasses = new(StringComparer.Ordinal) {
        "StackableCurrency", "Currency", "DelveSocketableCurrency", "DelveStackableSocketableCurrency",
        "DivinationCard", "MapFragment", "UniqueFragment", "Incubator", "IncubatorStackable",
        "Breachstone", "MiscMapItem",
    };

    /// <summary>Whether the pet would take this: gold (it goes to the stash's gold) or currency.</summary>
    internal static bool PetLootable(WorldItemObject ground)
    {
        if (IsGold(ground)) return true;
        if (ShapeOf(ground.Item) is not { } shape) return false;
        return shape.MaxStack > 1 || PetLootClasses.Contains(shape.Class);
    }

    /// <summary>Whether the 2nd Pet slot's pet is out: the only one of the two that picks things up.</summary>
    static bool PetLooterOut(ClientState conn)
    {
        lock (conn.Pets)
            foreach (var pet in conn.Pets.Values)
                if (pet.Slot == PetLootSlot) return true;
        return false;
    }

    /// <summary>The pet's pickup loop, started with the pets' follow loop and ended with the connection.</summary>
    internal static async Task RunPetLoot(string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn)
    {
        int busy = 0;
        double nextReport = 0, nextError = 0, nextMissing = 0, nextDiagnosis = 0;
        while (!conn.Closed)
        {
            try
            {
                await Task.Delay(busy == 0 ? PetLootIntervalMs : PetLootBusyIntervalMs);
                if (conn.Closed) break;
                if (!PetLooterOut(conn))
                {
                    busy = 0;
                    // 没有 2 号槽的宠物就没什么可捡的；说一次，免得"宠物不捡"分不清是没宠物还是没逻辑。
                    double missing = conn.Clock.Elapsed.TotalSeconds;
                    if (missing >= nextMissing)
                    {
                        nextMissing = missing + 60;
                        int spawned = 0;
                        lock (conn.Pets) spawned = conn.Pets.Count;
                        Console.WriteLine($"[gateway] {remote} pet: no pet in slot {PetLootSlot} (spawned pets: {spawned}), nothing to pick up");
                    }
                    continue;
                }
                if (HardcoreProgressBlocked(conn)) continue;
                var (tried, taken, gold, notes) = await PetLootOnce(remote, stream, ciphers, conn);
                // 有货一件没进（多半是背包满了）就放慢；地上没货则保持正常节奏，新掉落照旧 0.4 秒内收走。
                if (taken > 0) busy = 0;
                else if (tried > 0) busy = Math.Min(busy + 1, 3);
                else busy = 0;
                double now = conn.Clock.Elapsed.TotalSeconds;
                // 收成功也记一条：以前成功完全不写日志，于是"宠物到底捡了什么"根本无从判断。
                if (taken > 0 && now >= nextReport)
                {
                    nextReport = now + 10;
                    Console.WriteLine($"[gateway] {remote} pet: took {taken} of {tried} nearby item(s) ({gold} gold)");
                    foreach (string note in notes) Console.WriteLine($"[gateway] {remote} pet:   {note}");
                }
                else if (taken == 0 && tried > 0 && now >= nextReport)
                {
                    nextReport = now + 15;
                    Console.WriteLine($"[gateway] {remote} pet: took none of {tried} nearby item(s), they stay on the ground");
                    foreach (string note in notes) Console.WriteLine($"[gateway] {remote} pet:   {note}");
                }
                // 地上有东西、候选却一件都没有（tried 一直是 0）：把身边最近的几件连同判据写出来，别只能靠猜。
                else if (tried == 0 && now >= nextDiagnosis && conn.Instance.Ground.All() is { Count: > 0 } all)
                {
                    nextDiagnosis = now + 15;
                    var (px, py) = conn.Walk.Position(now);
                    var ranked = all
                        .Select(e => (e.Id, e.Item, Dist: Math.Sqrt((double)(e.Item.GridX - px) * (e.Item.GridX - px)
                            + (double)(e.Item.GridY - py) * (e.Item.GridY - py))))
                        .OrderBy(e => e.Dist).ToList();
                    int inRange = ranked.Count(e => e.Dist <= PetLootRange);
                    int lootable = ranked.Count(e => PetLootable(e.Item));
                    int both = ranked.Count(e => e.Dist <= PetLootRange && PetLootable(e.Item));
                    Console.WriteLine($"[gateway] {remote} pet: nothing to take at ({px}, {py}); the ground holds {ranked.Count} item(s) — "
                        + $"{inRange} within {PetLootRange:0} cells, {lootable} lootable anywhere, {both} lootable and in range");
                    foreach (var e in ranked.Take(8))
                    {
                        var shape = ShapeOf(e.Item.Item);
                        Console.WriteLine($"[gateway] {remote} pet:   0x{e.Id:x} type 0x{e.Item.Item.TypeHash:x8} "
                            + $"at ({e.Item.GridX}, {e.Item.GridY}) dist {e.Dist:0.#} gold {IsGold(e.Item)} lootable {PetLootable(e.Item)} "
                            + $"maxStack {(shape is { } ms ? ms.MaxStack.ToString() : "-")} "
                            + $"class {(shape is { } cl ? cl.Class : "(no catalog row)")}");
                    }
                }
            }
            catch (Exception e)
            {
                // 任何一次异常都不能把拾取永久停掉。以前这里只接 4 种异常，别的（ArgumentException / NullReferenceException
                // 从 Blob()、AddStacked() 里冒出来）直接跳出 while，而 PetLootRunning 仍是 true，SpawnPets 也不会再启动
                // 它，于是本次连线里宠物就再也不捡了——这就是"捡了一会儿就不捡了"。连接真的断了才退出。
                if (conn.Closed) break;
                double now = conn.Clock.Elapsed.TotalSeconds;
                if (now >= nextError)
                {
                    nextError = now + 30;
                    Console.WriteLine($"[gateway] {remote} pet: pick-up loop recovered from {e.GetType().Name}: {e.Message}");
                }
                busy = 3;
            }
        }
        // 循环停了就允许下一次进图（SpawnPets）重新启动它，而不是留一个永远为 true 的标志位。
        conn.PetLootRunning = false;
    }

    /// <summary>Why one item stayed on the ground.</summary>
    enum PetPick { Taken, NotLootable, NoRoom, Gone }

    /// <summary>One pass: how many it tried, how many went in, and a line per item for the log.</summary>
    static async Task<(int Tried, int Taken, int Gold, List<string> Notes)> PetLootOnce(
        string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn)
    {
        double now = conn.Clock.Elapsed.TotalSeconds;
        var (x, y) = conn.Walk.Position(now);
        var near = conn.Instance.Ground.Within(x, y, PetLootRange, PetLootable)
            .OrderBy(g => (double)(g.Item.GridX - x) * (g.Item.GridX - x) + (double)(g.Item.GridY - y) * (g.Item.GridY - y))
            .Take(PetLootAttempts)
            .ToList();
        int taken = 0, tried = 0, gold = 0;
        var notes = new List<string>();
        foreach (var (id, ground) in near)
        {
            if (conn.Closed || taken >= PetLootPerTick) break;
            tried++;
            var picked = await PetPickUp(remote, stream, ciphers, conn, id, ground);
            if (picked == PetPick.Taken)
            {
                taken++;
                if (IsGold(ground)) gold++;
            }
            if (notes.Count < 8) notes.Add($"0x{id:x} {picked} {ground.Item.StackSize}x {Label(ground)}");
        }
        return (tried, taken, gold, notes);
    }

    /// <summary>What one line of the pet's log calls this item: gold, or its client.db item class.</summary>
    static string Label(WorldItemObject ground) =>
        IsGold(ground) ? "Gold" : ShapeOf(ground.Item)?.Class ?? "(no catalog row)";

    // Takes one ground item into the main inventory, the way the player's own stacking pickup does, without the walk.
    static async Task<PetPick> PetPickUp(string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn, uint id, WorldItemObject ground)
    {
        if (IsGold(ground))
        {
            await PickUpGold(remote, stream, ciphers, conn, id, ground, "by the pet");
            // PickUpGold takes it itself; still on the ground means it refused (dead, paused, or someone took it first).
            return conn.Instance.Ground.Get(id) is null ? PetPick.Taken : PetPick.Gone;
        }
        if (!PetLootable(ground)) return PetPick.NotLootable;
        var items = ItemsOf(conn);
        PlayerInventories.Stacking stacked;
        WorldItemObject? rest = null;
        uint restId = 0;
        lock (conn.Monsters)
        {
            if (HardcoreProgressBlocked(conn)) return PetPick.Gone;
            // No room: it stays on the ground and is tried again on the next pass, and this pass goes on to the next item
            // (which is where the old nearest-three-with-no-room case used to hold up everything behind it).
            if (!items.Fits(PlayerInventories.MainInventory, ground.Item, ShapeOf)) return PetPick.NoRoom;
            if (conn.Instance.Ground.Take(id) is null) return PetPick.Gone;   // already picked up
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
        return PetPick.Taken;
    }
}
