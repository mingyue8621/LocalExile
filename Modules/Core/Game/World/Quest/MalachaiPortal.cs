using System.Runtime.CompilerServices;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using GameServer.Core.World;

static partial class Game
{
    // ---- Malachai, The Nightmare (Act 4, the Black Core inside The Harvest) ----
    //
    // The official fight is two stages with a submerge between them (the client's .ot files and
    // Agents/docs/game/campaign/act4.md §428-429):
    //   * first form Metadata/Monsters/Malachai/MalachaiBoss: set_cannot_die and cannot_drop_below_%_life 70 — it
    //     cannot be killed, and at 70% it submerges and the fight moves on;
    //   * second form Metadata/Monsters/Malachai/MalachaiBoss2: initial_life_percentage 92, with three hearts
    //     BeastHeart1/2/3 beside it (MV 4671-4673, NoAi, states idle -> vulnerable -> dead). The hearts heal him and
    //     he cannot die until every one of them is down;
    //   * when the second form falls, QuestObjects/Act4/MalachaiDeathPortal appears.
    //
    // The depths' room (boss_2.arm, the mapboss2 point at 311,322) is not fitted into this server's map — the room
    // set in data/room-points.json has no Belly entry at all — so "below" is a clearing on the same map, far enough
    // from the platform to read as a different place. The rift itself is this module's object; it does not go
    // through the area's transition table.
    //
    // The second form used to be spawned on top of the corpse the instant the first died (0.8.27), which is not the
    // official order and read as "no second phase": the first form never dies here.

    internal const string MalachaiArea = "1_4_6_3";
    internal const string MalachaiFirst = "Metadata/Monsters/Malachai/MalachaiBoss";
    internal const string MalachaiSecond = "Metadata/Monsters/Malachai/MalachaiBoss2";
    internal static readonly string[] MalachaiHearts =
    [
        "Metadata/Monsters/Malachai/BeastHeart1",
        "Metadata/Monsters/Malachai/BeastHeart2",
        "Metadata/Monsters/Malachai/BeastHeart3",
    ];
    // The portal's own rig (the .ot's Animated animated_object), drawn on the area transition blob.
    const string MalachaiPortalRig = "Metadata/Effects/Spells/monsters_effects/act4/Malachai/death_portal/rig.ao";
    /// <summary>cannot_drop_below_%_life on the first form: it submerges there instead of dying.</summary>
    const int MalachaiSubmergePercent = 70;
    /// <summary>initial_life_percentage on the second form.</summary>
    const int MalachaiSecondFormPercent = 92;
    /// <summary>What one standing heart gives him per second, in percent of his maximum life.</summary>
    const double MalachaiHeartHealPercent = 1.0;
    /// <summary>How far the hearts stand from him.</summary>
    const int MalachaiHeartDistance = 16;

    sealed class MalachaiFight(uint bossId, SpawnedMonster boss, byte level)
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public uint BossId = bossId;
        public SpawnedMonster? Boss = boss;
        public byte Level = level;
        /// <summary>1 while the first form stands, 2 once the rift has been used.</summary>
        public int Stage = 1;
        public readonly List<uint> Hearts = [];
        public bool Submerged, Done;
        public int StandingSeen = -1;
        public uint PortalId;
        public (int X, int Y) PortalAt, DepthsAt;
        public double HealAt;
    }

    static readonly ConditionalWeakTable<InstanceState, MalachaiFight> MalachaiFights = new();

    static bool IsMalachaiForm(MonsterCatalog.Variety? variety) =>
        variety is { } v && v.Id is MalachaiFirst or MalachaiSecond;

    /// <summary>The encounter's loop: the first form's submerge at 70%, then the hearts healing the second.</summary>
    internal static async Task MalachaiTick(ClientState conn)
    {
        if (conn.Layout.Id != MalachaiArea || !conn.PlayerSent || conn.Closed || conn.LeavingArea) return;
        if (!MalachaiFights.TryGetValue(conn.Instance, out var fight))
        {
            KeyValuePair<uint, SpawnedMonster> found;
            lock (conn.Monsters) found = conn.Monsters.FirstOrDefault(p => IsMalachaiForm(p.Value.Variety));
            if (found.Value is null) return;
            fight = MalachaiFights.GetValue(conn.Instance, _ => new(found.Key, found.Value, found.Value.Level));
        }
        await fight.Gate.WaitAsync();
        try { if (IsMonsterRunner(conn)) await Step(conn, fight); }
        finally { fight.Gate.Release(); }
    }

    static async Task Step(ClientState conn, MalachaiFight fight)
    {
        if (fight.Done) return;
        double now = conn.Clock.Elapsed.TotalSeconds;
        if (fight.Stage == 1)
        {
            if (fight.Submerged) return;                        // down below: the second stage is the player's to raise
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

    /// <summary>At 70% the first form burrows away (ActBoss submerge, not a death) and leaves the rift the player
    /// follows him down through.</summary>
    static async Task Submerge(ClientState conn, MalachaiFight fight, double now)
    {
        if (fight.Submerged) return;
        fight.Submerged = true;
        var boss = fight.Boss!;
        var at = boss.Walk.Position(now);
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
        fight.PortalAt = at;
        fight.DepthsAt = DepthsPoint(conn, at);
        fight.PortalId = await OpenRift(conn, at, fight.DepthsAt, "Malachai's rift");
        await Send(remote, stream, ciphers,
            InstanceChat.ChatLine("玛拉凯钻入地下。地面裂开一道口子——下去追他。"),
            "0x0017 Malachai submerges, the rift opens");
    }

    static async Task<uint> OpenRift(ClientState conn, (int X, int Y) at, (int X, int Y) to, string note)
    {
        uint id = conn.NextObjectId++;
        // An AreaTransition object with the portal's rig: the layout's own gate blob, which the client draws and a
        // click addresses; walking through is this module's (MalachaiUse), not the area's transition table.
        var portal = new AreaTransitionObject(at.X, at.Y, conn.Layout.Hash, Model: MalachaiPortalRig) { TeleportTo = to };
        await Send(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers,
            InstanceEntities.CreateObject(id, InstanceEntities.ObjectTypeHash(portal.Type), portal.Blob()),
            $"0x02bb {note} 0x{id:x} at ({at.X}, {at.Y}) -> ({to.X}, {to.Y})");
        await Send(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers, InstanceProtocol.EnterWakeZone(id),
            $"0x01a9 {note} enters the wake zone");
        return id;
    }

    /// <summary>Where the depths stand in for boss_2.arm: the area's own mapboss2 point when this map has one on
    /// walkable ground and it is a real distance from the platform, else a clearing 18-40 cells out.</summary>
    static (int X, int Y) DepthsPoint(ClientState conn, (int X, int Y) from)
    {
        if (conn.LaterFormPoints.GetValueOrDefault(MalachaiSecond) is var point && point != default && point != from
            && Dist(point, from) > 12 && conn.Layout.WalkableFor(point.X, point.Y, 2) && !conn.Barrier.Contains(point))
            return point;
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

    /// <summary>C→S 0x24 on the rift: down to the depths, and the second form rises with his hearts. False when this is
    /// not the rift (another object's use). The rift stays open so the rest of the party can follow.</summary>
    internal static async Task<bool> MalachaiUse(ClientState conn, InstanceTransfer.UseSkillOnObject use, double requestedAt)
    {
        if (conn.Layout.Id != MalachaiArea || conn.Instance is not { } instance) return false;
        if (!MalachaiFights.TryGetValue(instance, out var fight)) return false;
        if (fight.PortalId == 0 || use.TargetId != fight.PortalId) return false;
        if (!TryBeginObjectWalk(conn, requestedAt, fight.PortalAt.X, fight.PortalAt.Y, 3, out var walk)) return true;
        var (action, now, here, stop, arrive) = walk;
        var (remote, stream, ciphers) = (conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers);
        if (!use.Started)
            await Send(remote, stream, ciphers,
                InstanceProtocol.ActionStarted(PlayerId, here.X, here.Y, fight.PortalAt.X, fight.PortalAt.Y, use.SkillId,
                    use.Sequence, use.Flags, targetObjectId: use.TargetId),
                $"0x01ab player steps into Malachai's rift 0x{use.TargetId:x}",
                still: () => PlayerActionCurrent(conn, action));
        if (arrive > now) await Task.Delay(TimeSpan.FromSeconds(arrive - now));
        if (!PlayerActionCurrent(conn, action)) return true;
        if (!PlayerWalk.InReach(stop, fight.PortalAt, 3, conn.Layout.Walkable)) return true;
        await fight.Gate.WaitAsync();
        try
        {
            double at = conn.Clock.Elapsed.TotalSeconds;
            using (ProjectileMutation(conn))
            {
                Interlocked.Increment(ref conn.ActionCount);
                conn.Walk.Teleport(fight.DepthsAt.X, fight.DepthsAt.Y, at);
            }
            await Send(remote, stream, ciphers, InstanceProtocol.Teleport(PlayerId, fight.DepthsAt.X, fight.DepthsAt.Y),
                $"0x01b7 player through Malachai's rift to ({fight.DepthsAt.X}, {fight.DepthsAt.Y})");
            if (fight.Stage == 1) await RaiseSecondStage(conn, fight);
        }
        finally { fight.Gate.Release(); }
        return true;
    }

    /// <summary>The second form at its 92% starting life and his three hearts around it, on the connection that runs
    /// the instance's monsters (SharedMonsters): spawned on the killer's connection, an instance boss never acts.</summary>
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
                Console.WriteLine($"[gateway] {remote} Malachai's second stage rises at " +
                    $"({fight.DepthsAt.X}, {fight.DepthsAt.Y}) at {MalachaiSecondFormPercent}% life");
            }
        }
        // The three hearts: NoAi in the data (Metadata/Monsters/NoAi.ais), so they never chase or swing; they are his
        // life. They stand in three directions around him.
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
        Console.WriteLine($"[gateway] {remote} Malachai's {fight.Hearts.Count} heart(s) stand at " +
            $"({fight.DepthsAt.X}, {fight.DepthsAt.Y}): he cannot die while any of them beats");
    }

    static (int X, int Y) HeartPoint(ClientState conn, (int X, int Y) center, int index)
    {
        double angle = Math.PI * 2 * index / MalachaiHearts.Length;
        for (int r = MalachaiHeartDistance; r >= 6; r--)
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
        var to = (conn.Layout.SpawnX, conn.Layout.SpawnY);
        if (Dist(to, at) < 8) to = (at.X + 6, at.Y);
        var id = await OpenRift(conn, at, to, "Malachai's death portal");
        if (MalachaiFights.TryGetValue(conn.Instance, out var fight)) { fight.PortalId = id; fight.PortalAt = at; fight.DepthsAt = to; fight.Done = true; }
    }

    internal static void LeavingMalachaiPortal(ClientState conn)
    {
        if (conn.Instance is { } instance) MalachaiFights.Remove(instance);
    }
}
