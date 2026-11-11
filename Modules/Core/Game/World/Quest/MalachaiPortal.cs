using System.Runtime.CompilerServices;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using GameServer.Core.World;

static partial class Game
{
    // ---- Malachai, The Nightmare (Act 4, the Black Core inside The Harvest) ----
    //
    // The official fight is two stages on two floors of the same area (the client's .ot files and
    // Agents/docs/game/campaign/act4.md §428-429):
    //   * first form Metadata/Monsters/Malachai/MalachaiBoss on the platform (room boss_1.arm, point mapboss1):
    //     set_cannot_die and cannot_drop_below_%_life 70 — it cannot be killed at all, and at 70% it submerges;
    //   * second form Metadata/Monsters/Malachai/MalachaiBoss2 in the room below (boss_2.arm, point mapboss2, 787
    //     cells from the platform at seed 1): initial_life_percentage 92, with three hearts BeastHeart1/2/3 at the
    //     room's own heart points (MV 4671-4673, NoAi, states idle -> vulnerable -> dead). Each standing heart heals
    //     him, and he cannot die until all three are down;
    //   * when the second form falls, QuestObjects/Act4/MalachaiDeathPortal appears.
    //
    // The way down is the area's own transition (malachai_fight_v01_01_area_transition, an internal pair in the
    // client's data whose other end is MalachaiDepthsReturn in boss_2.arm) — not a portal of this module's. It stays
    // off the client until the first form submerges (GameEvents.TransitionHidden); before 0.8.29 the server both showed
    // it from the moment the player entered and spawned a second gate of its own at 70%, which led nowhere.
    //
    // Both rooms are fitted from data/room-points.json (Metadata/Terrain/Mountain/Belly/Rooms/Unique/boss_1.arm and
    // boss_2.arm), so every point here is the client's own placement.

    internal const string MalachaiArea = "1_4_6_3";
    internal const string MalachaiFirst = "Metadata/Monsters/Malachai/MalachaiBoss";
    internal const string MalachaiSecond = "Metadata/Monsters/Malachai/MalachaiBoss2";
    internal static readonly string[] MalachaiHearts =
    [
        "Metadata/Monsters/Malachai/BeastHeart1",
        "Metadata/Monsters/Malachai/BeastHeart2",
        "Metadata/Monsters/Malachai/BeastHeart3",
    ];
    // The death portal's own rig (the .ot's Animated animated_object), drawn on the area transition blob.
    const string MalachaiPortalRig = "Metadata/Effects/Spells/monsters_effects/act4/Malachai/death_portal/rig.ao";
    /// <summary>cannot_drop_below_%_life on the first form: it submerges there instead of dying.</summary>
    const int MalachaiSubmergePercent = 70;
    /// <summary>initial_life_percentage on the second form.</summary>
    const int MalachaiSecondFormPercent = 92;
    /// <summary>What one standing heart gives him per second, in percent of his maximum life.</summary>
    const double MalachaiHeartHealPercent = 1.0;
    /// <summary>How close a player must be to the depths point for the second stage to rise there (the room's own gate
    /// lands 90 cells from mapboss2).</summary>
    const int MalachaiArrivalRange = 150;
    /// <summary>The depths' room as a radius around mapboss2: the area's gate whose far end lies inside it is the way
    /// down (malachai_fight_v01_01_area_transition1 at boss_1's "telepad", 90 cells away).</summary>
    const int MalachaiDepthsRadius = 150;

    sealed class MalachaiFight(uint bossId, SpawnedMonster boss, byte level)
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public uint BossId = bossId;
        public SpawnedMonster? Boss = boss;
        public byte Level = level;
        /// <summary>1 while the first form stands, 2 once a player reaches the room below.</summary>
        public int Stage = 1;
        public readonly List<uint> Hearts = [];
        public bool Submerged, Done;
        /// <summary>The connections already told which gates stand (the way down is per-player).</summary>
        public readonly HashSet<uint> GateSynced = [];
        public int StandingSeen = -1;
        /// <summary>boss_2.arm's mapboss2 point: where the second form and his hearts belong.</summary>
        public (int X, int Y) DepthsAt;
        public double HealAt;
    }

    static readonly ConditionalWeakTable<InstanceState, MalachaiFight> MalachaiFights = new();

    static bool IsMalachaiForm(MonsterCatalog.Variety? variety) =>
        variety is { } v && v.Id is MalachaiFirst or MalachaiSecond;

    /// <summary>The encounter's loop: the first form's submerge at 70%, the way down, then the hearts healing the second.</summary>
    internal static async Task MalachaiTick(ClientState conn)
    {
        if (conn.Layout.Id != MalachaiArea || !conn.PlayerSent || conn.Closed || conn.LeavingArea) return;
        if (!MalachaiFights.TryGetValue(conn.Instance, out var fight))
        {
            KeyValuePair<uint, SpawnedMonster> found;
            lock (conn.Monsters) found = conn.Monsters.FirstOrDefault(p => IsMalachaiForm(p.Value.Variety));
            if (found.Value is null) return;
            fight = MalachaiFights.GetValue(conn.Instance, _ => new(found.Key, found.Value, found.Value.Level));
            double started = conn.Clock.Elapsed.TotalSeconds;
            fight.DepthsAt = DepthsPoint(conn, found.Value.Walk.Position(started));
        }
        await fight.Gate.WaitAsync();
        try
        {
            if (IsMonsterRunner(conn)) await Step(conn, fight);
            // The platform's way down is the area's own transition: kept off every client while the first form stands
            // (TransitionHidden), created for each of them the moment he submerges.
            if (fight.GateSynced.Add(conn.PeerId))
            {
                var wire = conn.Wire;
                await UpdateShownTransitions(wire.Remote, wire.Stream, wire.Ciphers, conn, "Malachai's state");
            }
        }
        finally { fight.Gate.Release(); }
    }

    static async Task Step(ClientState conn, MalachaiFight fight)
    {
        if (fight.Done) return;
        double now = conn.Clock.Elapsed.TotalSeconds;
        if (fight.Stage == 1)
        {
            if (fight.Submerged)
            {
                // He is below: the second stage rises once a player follows him down through the platform's gate.
                if (AtDepths(conn, fight.DepthsAt, now)) await RaiseSecondStage(conn, fight);
                return;
            }
            if (fight.Boss is not { } boss || boss.Dead || boss.Life == 0) { fight.Done = true; return; }
            bool begin;
            lock (conn.Monsters)
            {
                // cannot_drop_below_%_life: the gate holds his life at 70% and refuses the next hit.
                boss.LifePhases ??= new([MalachaiSubmergePercent]);
                begin = boss.LifePhases.Begin(boss.Life, boss.MaxLife);
            }
            if (begin) await Submerge(conn, fight, now);
            return;
        }
        SpawnedMonster? second;
        lock (conn.Monsters) conn.Monsters.TryGetValue(fight.BossId, out second);
        if (second is null || second.Dead || second.Life == 0) { fight.Done = true; return; }
        int standing = 0;
        lock (conn.Monsters)
            foreach (uint id in fight.Hearts)
                if (conn.Monsters.TryGetValue(id, out var heart) && !heart.Dead && heart.Life > 0) standing++;
        if (fight.Hearts.Count == 0)
        {
            // No heart was created (an unsupported variety): nothing keeps him alive, so the fight stays a plain one.
            lock (conn.Monsters) second.CannotDie = false;
            return;
        }
        if (standing != fight.StandingSeen)
        {
            fight.StandingSeen = standing;
            await Send(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers,
                InstanceChat.ChatLine(standing == 0 ? "玛拉凯的心脏全部停止跳动。" : $"玛拉凯的心脏还剩 {standing} 颗。"),
                $"0x0017 Malachai's hearts: {standing} standing");
        }
        if (standing == 0)
        {
            // Every heart is down: he can finally be killed (.ot set_cannot_die ends with them).
            lock (conn.Monsters)
            {
                if (!second.CannotDie) return;
                second.CannotDie = false;
            }
            return;
        }
        if (now < fight.HealAt) return;
        fight.HealAt = now + 1;
        uint healed;
        lock (conn.Monsters)
        {
            if (second.Dead || second.Life == 0 || second.Life >= second.MaxLife) return;
            long add = Math.Max(1, (long)(second.MaxLife * (MalachaiHeartHealPercent / 100.0) * standing));
            healed = (uint)Math.Min(second.MaxLife, (long)second.Life + add);
            second.Life = healed;
        }
        await Send(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers,
            InstanceProtocol.LifeUpdate(fight.BossId, healed), null);
    }

    /// <summary>Whether any player of the instance stands in the room below (the fight's second floor).</summary>
    static bool AtDepths(ClientState conn, (int X, int Y) at, double now)
    {
        if (Dist(conn.Walk.Position(now), at) < MalachaiArrivalRange) return true;
        foreach (var peer in conn.Instance?.PlayersExcept(conn.PeerId) ?? [])
            if (peer.Current && peer.Life > 0 && Dist((peer.X, peer.Y), at) < MalachaiArrivalRange) return true;
        return false;
    }

    /// <summary>At 70% the first form burrows away (ActBoss submerge, not a death) and the platform's own gate down to
    /// boss_2.arm opens.</summary>
    static async Task Submerge(ClientState conn, MalachaiFight fight, double now)
    {
        if (fight.Submerged) return;
        fight.Submerged = true;
        var boss = fight.Boss!;
        // Off the field without being a corpse: no death, no drops, no kill flags — the mechanics table has him at
        // cannot_drop_below 70 and the second form is what ends the quest.
        lock (conn.Monsters)
        {
            boss.Dead = true;
            boss.Life = 0;
            conn.Monsters.Remove(fight.BossId);   // the instance's own dictionary: every connection in it loses him
        }
        if (conn.Instance is { } instance) instance.World.MonsterOrigins.TryRemove(fight.BossId, out _);
        var (remote, stream, ciphers) = (conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers);
        await Send(remote, stream, ciphers, InstanceProtocol.RemoveObject(fight.BossId),
            $"0x02bd {boss.Label} submerges at {boss.LifePhases?.Completed} stage ({MalachaiSubmergePercent}% life)");
        // The area's own gate to the room below, off every client until now.
        foreach (var player in MalachaiParty(conn))
        {
            var wire = player.Wire;
            await UpdateShownTransitions(wire.Remote, wire.Stream, wire.Ciphers, player, "Malachai submerges");
            await Send(wire.Remote, wire.Stream, wire.Ciphers,
                InstanceChat.ChatLine("玛拉凯钻入地下。平台中间的裂口开了——下去追他。"),
                "0x0017 Malachai submerges, the way down opens");
        }
    }

    /// <summary>This connection and the rest of the instance's players (their own wire, their own gates).</summary>
    static IEnumerable<ClientState> MalachaiParty(ClientState conn)
    {
        yield return conn;
        foreach (var peer in conn.Instance?.PlayersExcept(conn.PeerId) ?? [])
            if (peer.Owner is { Closed: false, LeavingArea: false } owner && !ReferenceEquals(owner, conn)) yield return owner;
    }

    /// <summary>An encounter's gate the client must not show yet: the platform's way down while the first form stands.</summary>
    internal static bool MalachaiGateHidden(ClientState conn, AreaTransitionObject transition)
    {
        if (conn.Layout.Id != MalachaiArea || conn.Instance is not { } instance) return false;
        if (transition.TeleportTo is not { } to) return false;
        if (!MalachaiFights.TryGetValue(instance, out var fight) || fight.Submerged) return false;
        // boss_1's "telepad" pair: its far end stands in boss_2.arm, beside MalachaiDepthsReturn.
        return fight.DepthsAt != default && Dist(to, fight.DepthsAt) < MalachaiDepthsRadius;
    }

    /// <summary>A named point of the area's fitted rooms (boss_2.arm and its heart points came from the client's own
    /// placement). Null when this map has none by that name.</summary>
    static (int X, int Y)? NamedRoomPoint(ClientState conn, string name)
    {
        foreach (var point in AreaPopulation.FitRooms(MalachaiArea, conn.Seed, [], "data").Points)
            if (point.Name == name) return (point.X, point.Y);
        return null;
    }

    /// <summary>Where the depths stand: boss_2.arm's own mapboss2 point, which the fit places in the room below the
    /// platform (787 cells away at seed 1). Never the platform itself.</summary>
    static (int X, int Y) DepthsPoint(ClientState conn, (int X, int Y) from)
    {
        if (NamedRoomPoint(conn, "mapboss2") is { } point && Dist(point, from) > 12)
            return conn.Layout.WalkableFor(point.X, point.Y, 2) ? point : conn.Layout.Near(point);
        // No fitted room: a clearing far enough from the platform.
        for (int r = 40; r >= 18; r--)
            for (int i = 0; i < 24; i++)
            {
                double angle = Math.PI * 2 * i / 24;
                int x = from.X + (int)Math.Round(Math.Cos(angle) * r), y = from.Y + (int)Math.Round(Math.Sin(angle) * r);
                if (x < 2 || y < 2 || x >= conn.Layout.GridW - 2 || y >= conn.Layout.GridH - 2) continue;
                if (!conn.Layout.WalkableFor(x, y, 2) || conn.Barrier.Contains((x, y))) continue;
                return (x, y);
            }
        return (from.X + 4, from.Y);
    }

    /// <summary>The second form at its 92% starting life and his three hearts at the room's own points, on the
    /// connection that runs the instance's monsters (SharedMonsters): spawned on a party member's connection, an
    /// instance boss never acts.</summary>
    static async Task RaiseSecondStage(ClientState conn, MalachaiFight fight)
    {
        if (fight.Stage != 1) return;
        fight.Stage = 2;
        var (remote, stream, ciphers) = (conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers);
        if (Monsters.Find(MalachaiSecond) is { Supported: true } form && form.Id == MalachaiSecond)
        {
            uint id = await SpawnMonster(remote, stream, ciphers, conn, form, fight.Level, MonsterRarity.Unique,
                fight.DepthsAt.X, fight.DepthsAt.Y, lifePercent: MalachaiSecondFormPercent, log: true,
                owned: (_, spawned) => { spawned.Aggro = true; spawned.LastTarget = null; spawned.CannotDie = true; });
            if (id != 0)
            {
                fight.BossId = id;
                lock (conn.Monsters) conn.Monsters.TryGetValue(id, out fight.Boss);
                Console.WriteLine($"[gateway] {remote} Malachai's second stage rises in the depths at " +
                    $"({fight.DepthsAt.X}, {fight.DepthsAt.Y}) at {MalachaiSecondFormPercent}% life");
            }
        }
        // The three hearts: NoAi in the data (Metadata/Monsters/NoAi.ais), so they never chase or swing; they are his
        // life, standing where boss_2.arm places them.
        for (int i = 0; i < MalachaiHearts.Length; i++)
        {
            if (Monsters.Find(MalachaiHearts[i]) is not { Supported: true } heart || heart.Id != MalachaiHearts[i])
            {
                Console.WriteLine($"[gateway] {remote} {MalachaiHearts[i]} is not supported: the heart is left out");
                continue;
            }
            var at = HeartPoint(conn, fight.DepthsAt, i);
            uint id = await SpawnMonster(remote, stream, ciphers, conn, heart, fight.Level, MonsterRarity.Normal,
                at.X, at.Y, scripted: true, log: false,
                owned: (_, spawned) => { spawned.Aggro = false; spawned.LastTarget = null; });
            if (id != 0) fight.Hearts.Add(id);
        }
        Console.WriteLine($"[gateway] {remote} Malachai's {fight.Hearts.Count} heart(s) stand: he cannot die while any beats");
    }

    static (int X, int Y) HeartPoint(ClientState conn, (int X, int Y) center, int index)
    {
        // The room's own heart points (boss_2.arm): each heart stands where the client places it, 48-70 cells out.
        if (NamedRoomPoint(conn, MalachaiHearts[index]) is { } point && Dist(point, center) > 6)
            return conn.Layout.WalkableFor(point.X, point.Y, 2) ? point : conn.Layout.Near(point);
        double angle = Math.PI * 2 * index / MalachaiHearts.Length;
        for (int r = 16; r >= 6; r--)
        {
            int x = center.X + (int)Math.Round(Math.Cos(angle) * r), y = center.Y + (int)Math.Round(Math.Sin(angle) * r);
            if (x < 2 || y < 2 || x >= conn.Layout.GridW - 2 || y >= conn.Layout.GridH - 2) continue;
            if (!conn.Layout.WalkableFor(x, y, 2) || conn.Barrier.Contains((x, y))) continue;
            return (x, y);
        }
        return (center.X + 2 + index * 2, center.Y);
    }

    /// <summary>The second form's fall opens the way back up (QuestObjects/Act4/MalachaiDeathPortal). The campaign
    /// text leaves its destination unverified; this map sends the player to the area's entrance.</summary>
    internal static async Task OpenMalachaiDeathPortal(ClientState conn, (int X, int Y) at)
    {
        var to = (X: conn.Layout.SpawnX, Y: conn.Layout.SpawnY);
        if (Dist(to, at) < 8) to = (at.X + 6, at.Y);
        uint id = conn.TakeObjectId();
        // An AreaTransition object with the death portal's rig: the layout's own gate blob, which the client draws and
        // the area's transition handling moves the player through.
        var portal = new AreaTransitionObject(at.X, at.Y, conn.Layout.Hash, Model: MalachaiPortalRig) { TeleportTo = to };
        await Send(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers,
            InstanceEntities.CreateObject(id, InstanceEntities.ObjectTypeHash(portal.Type), portal.Blob()),
            $"0x02bb Malachai's death portal 0x{id:x} at ({at.X}, {at.Y}) -> ({to.X}, {to.Y})");
        await Send(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers, InstanceProtocol.EnterWakeZone(id),
            "0x01a9 death portal enters the wake zone");
        if (MalachaiFights.TryGetValue(conn.Instance, out var fight)) fight.Done = true;
    }

    internal static void LeavingMalachaiPortal(ClientState conn)
    {
        if (conn.Instance is { } instance) MalachaiFights.Remove(instance);
    }
}
