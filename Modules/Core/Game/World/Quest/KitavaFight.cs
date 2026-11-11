using System.Runtime.CompilerServices;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using GameServer.Core.World;

static partial class Game
{
    // ---- Kitava, the Insatiable (Act 5, The Cathedral Rooftop, a5q6) ----
    //
    // The official fight is one arena, two forms and the heart between them (the client's own files):
    //   * Metadata/Monsters/KitavaBoss/Kitava1 (MV 5747, the room's force:mapboss in bossentry.arm): Kitava1.ot carries
    //     set_monster_has_second_form and immobile, its transitions play phase1_transition_in / phase1_transition_out,
    //     and its skills are the five slams, the two swipes and the two debris throws;
    //   * Metadata/Monsters/KitavaBoss/Kitava2: BaseMonsterTypeIndex Kitava1 with the same 7 200 % life; it adds
    //     kitava_devouring_darkness and kitava_x_blast (GrantedEffects 6619/6622/6586) and its .act has a second stance
    //     ("stance stance2 { Idle = phase2_heartexposed; ChangeToStance1 = phase2_recover; }", entered through
    //     ChangeToStance2 = phase2_beamhit);
    //   * Metadata/Monsters/KitavaBoss/KitavaHeart: NoAi, immobile, 3 150 % life, no boss bar; its .act's Emerge is that
    //     same phase2_beamhit clip and its Emerge2 is phase2_recover, so the heart rises exactly while Stanza 2 stands, and
    //     its .ot makes it the fight's loot surrogate (globals.kitava_loot_surrogate).
    // So the fight alternates: damage him, then damage the heart while his chest is open, until he enrages and his X blast
    // destroys the Sign of Purity — it cannot be won, which is why a5q6 sets a5q6-CompletedBoss rather than a kill.
    //
    // Before 0.8.46 the quest rule's 50 % threshold simply removed him: no second form, no heart (live 2026-11-11).
    // His life is gated instead (LifePhases, installed in Monsters.SpawnMonster): 50 % ends the first form, each of the
    // next three gates opens his chest once, and the hit that would kill him after the last heart ends the fight.

    internal const string KitavaArea = "1_5_8";
    internal const string KitavaFirst = "Metadata/Monsters/KitavaBoss/Kitava1";
    internal const string KitavaSecond = "Metadata/Monsters/KitavaBoss/Kitava2";
    internal const string KitavaHeartMonster = "Metadata/Monsters/KitavaBoss/KitavaHeart";
    /// <summary>The rooftop's own portal (bossentry.arm (218, 584), a steam portal): it is the way back once the fight is
    /// over, and the client creates its type untransitioned (closed, PortalToggleable.ot's create_untransitioned).</summary>
    internal const string KitavaArenaPortal = "Metadata/MiscellaneousObjects/PortalToggleable";

    /// <summary>His life gates: 50 % ends the first form, then one heart for each of the other three.</summary>
    internal static readonly int[] KitavaPhases = [50, 35, 20, 5];

    /// <summary>How far from his chest the exposed heart stands (cells; the room gives it no point of its own, and his
    /// model is 20 cells across, so it has to be well clear of him to be seen and hit).</summary>
    const int KitavaHeartDistance = 26;

    sealed class KitavaFight(uint bossId, SpawnedMonster boss, byte level, (int X, int Y) at)
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public uint BossId = bossId;
        public SpawnedMonster? Boss = boss;
        public byte Level = level;
        /// <summary>1 while the first form stands, 2 once his chest has opened.</summary>
        public int Stage = 1;
        public uint HeartId;
        public bool HeartUp, Done;
        /// <summary>force:mapboss, where both forms stand (immobile).</summary>
        public (int X, int Y) At = at;
    }

    static readonly ConditionalWeakTable<InstanceState, KitavaFight> KitavaFights = new();

    static bool IsKitavaForm(MonsterCatalog.Variety? variety) =>
        variety is { } v && v.Id is KitavaFirst or KitavaSecond;

    /// <summary>The fight's loop: the first form's 50 %, the second form, and one heart per gate while his chest is open.</summary>
    internal static async Task KitavaTick(ClientState conn)
    {
        if (conn.Layout.Id != KitavaArea || !conn.PlayerSent || conn.Closed || conn.LeavingArea) return;
        if (!KitavaFights.TryGetValue(conn.Instance, out var fight))
        {
            KeyValuePair<uint, SpawnedMonster> found;
            lock (conn.Monsters) found = conn.Monsters.FirstOrDefault(p => IsKitavaForm(p.Value.Variety));
            if (found.Value is null) return;
            var at = found.Value.Walk.Position(conn.Clock.Elapsed.TotalSeconds);
            fight = KitavaFights.GetValue(conn.Instance, _ => new(found.Key, found.Value, found.Value.Level, at));
        }
        await fight.Gate.WaitAsync();
        try { if (IsMonsterRunner(conn)) await Step(conn, fight); }
        finally { fight.Gate.Release(); }
    }

    static async Task Step(ClientState conn, KitavaFight fight)
    {
        if (fight.Done) return;
        SpawnedMonster? boss;
        lock (conn.Monsters) conn.Monsters.TryGetValue(fight.BossId, out boss);
        if (boss is null || boss.Dead || boss.Life == 0) { fight.Done = true; return; }
        if (boss.LifePhases is not { } phases) return;
        // His chest is open: the heart is the only thing that can be hit, and the gate holds until it falls.
        if (fight.HeartUp)
        {
            bool standing;
            lock (conn.Monsters) standing = conn.Monsters.TryGetValue(fight.HeartId, out var heart) && !heart.Dead && heart.Life > 0;
            if (standing) return;
            await HeartFalls(conn, fight, phases);
            return;
        }
        bool begin;
        lock (conn.Monsters) begin = phases.Begin(boss.Life, boss.MaxLife);
        if (!begin) return;
        if (fight.Stage == 1) await RiseSecondForm(conn, fight, phases);
        else await OpenChest(conn, fight, boss);
    }

    /// <summary>At 50 % the first form withdraws (phase1_transition_out) and the second rises in his place, the bar going
    /// on from where it stood.</summary>
    static async Task RiseSecondForm(ClientState conn, KitavaFight fight, LifePhaseGate phases)
    {
        var (remote, stream, ciphers) = (conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers);
        var form = Monsters.Find(KitavaSecond);
        if (form is not { Supported: true } || form.Id != KitavaSecond)
        {
            // Nothing to rise: he keeps the first form and the gate opens, so the fight can still be finished.
            Console.WriteLine($"[gateway] {remote} {KitavaSecond} is not supported: Kitava keeps his first form");
            lock (conn.Monsters) phases.Finish();
            return;
        }
        fight.Stage = 2;
        var first = fight.Boss;
        uint maxLife = first?.MaxLife ?? 0;
        if (first is not null)
        {
            // Its own basic action: Kitava1.act's phase1_transition_out.
            var at = first.Walk.Position(conn.Clock.Elapsed.TotalSeconds);
            if (Emerges.ActionSkill("Emerge2") is ushort out2)
                await Send(remote, stream, ciphers, InstanceProtocol.ActionStartedNow(fight.BossId, at.X, at.Y, at.X, at.Y,
                    out2, first.NextSequence(), 0x0800, fight.BossId),
                    $"0x01ab Kitava 0x{fight.BossId:x} withdraws (phase1_transition_out) at {first.Life}/{first.MaxLife}");
            await AreaWait(PlayerUpdateRole.MonsterAction, 700);
            lock (conn.Monsters)
            {
                first.Dead = true;
                first.Life = 0;
                conn.Monsters.Remove(fight.BossId);
            }
            if (conn.Instance is { } gone) gone.World.MonsterOrigins.TryRemove(fight.BossId, out _);
            await Send(remote, stream, ciphers, InstanceProtocol.RemoveObject(fight.BossId),
                $"0x02bd Kitava 0x{fight.BossId:x} leaves the platform (his second form takes his place)");
            ToOthers(conn, fight.BossId, InstanceProtocol.RemoveObject(fight.BossId));
        }
        uint id = await SpawnMonster(remote, stream, ciphers, conn, form, fight.Level, MonsterRarity.Unique,
            fight.At.X, fight.At.Y, lifePercent: 50, log: true,
            ownedMaximumLife: maxLife == 0 ? null : maxLife,
            owned: (_, spawned) =>
            {
                spawned.Aggro = true;
                spawned.LastTarget = null;
                // His life is the fight's: the gates above it, and no hit may end him before the last heart has fallen.
                spawned.CannotDie = true;
                spawned.LifePhases = phases;
            });
        if (id == 0)
        {
            Console.WriteLine($"[gateway] {remote} Kitava's second form could not be created: the fight ends here");
            fight.Done = true;
            return;
        }
        fight.BossId = id;
        lock (conn.Monsters) conn.Monsters.TryGetValue(id, out fight.Boss);
        // His own entrance (Emerge = phase2_transition_in): the chest cavity opens with the construction effect pack.
        if (Emerges.SkillId is ushort emerge && fight.Boss is { } second)
        {
            var at = second.Walk.Position(conn.Clock.Elapsed.TotalSeconds);
            await Send(remote, stream, ciphers, InstanceProtocol.ActionStartedNow(id, at.X, at.Y, at.X, at.Y, emerge,
                second.NextSequence(), 0x0800, id), $"0x01ab Kitava's second form rises at ({at.X}, {at.Y})");
        }
        Console.WriteLine($"[gateway] {remote} Kitava's second form 0x{id:x} rises at " +
            $"{fight.Boss?.Life}/{fight.Boss?.MaxLife} life; the heart is his next gate");
        await Send(remote, stream, ciphers, InstanceChat.ChatLine("奇塔弗发出怒吼，伤口裂开了。"),
            "0x0017 Kitava's second form rises");
    }

    /// <summary>A gate past the first: his chest opens (ChangeToStance2 = phase2_beamhit) and the heart rises with it.</summary>
    static async Task OpenChest(ClientState conn, KitavaFight fight, SpawnedMonster boss)
    {
        var (remote, stream, ciphers) = (conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers);
        double now = conn.Clock.Elapsed.TotalSeconds;
        var at = boss.Walk.Position(now);
        if (Emerges.ActionSkill("ChangeToStance2") is ushort open)
            await Send(remote, stream, ciphers, InstanceProtocol.ActionStartedNow(fight.BossId, at.X, at.Y, at.X, at.Y,
                open, boss.NextSequence(), 0x0800, fight.BossId),
                $"0x01ab Kitava 0x{fight.BossId:x} opens his chest (ChangeToStance2) at {boss.Life}/{boss.MaxLife}");
        if (Monsters.Find(KitavaHeartMonster) is not { Supported: true } heart || heart.Id != KitavaHeartMonster)
        {
            // Nothing to hit: the gate would hold him forever, so let the phase pass as a plain one.
            Console.WriteLine($"[gateway] {remote} {KitavaHeartMonster} is not supported: his chest opens with nothing in it");
            lock (conn.Monsters) boss.LifePhases?.Finish();
            return;
        }
        var spot = HeartPoint(conn, at, now);
        // The last heart is the fight's loot surrogate (KitavaHeart.ot: globals.kitava_loot_surrogate = this): the ones
        // before it are only the way through, so they give neither items nor experience.
        bool last;
        lock (conn.Monsters) last = boss.LifePhases is { } gate && gate.Completed >= KitavaPhases.Length - 1;
        uint id = await SpawnMonster(remote, stream, ciphers, conn, heart, fight.Level, MonsterRarity.Normal,
            spot.X, spot.Y, scripted: true, log: true,
            owned: (_, spawned) => { spawned.Aggro = false; spawned.LastTarget = null; spawned.NoRewards = !last; });
        if (id == 0)
        {
            Console.WriteLine($"[gateway] {remote} Kitava's heart could not be created: the phase passes");
            lock (conn.Monsters) boss.LifePhases?.Finish();
            return;
        }
        fight.HeartId = id;
        fight.HeartUp = true;
        Console.WriteLine($"[gateway] {remote} Kitava's heart 0x{id:x} stands at ({spot.X}, {spot.Y}): he cannot be hit " +
            "while it beats");
        await Send(remote, stream, ciphers, InstanceChat.ChatLine("心脏露出来了——去砍它！"),
            "0x0017 Kitava's heart is exposed");
    }

    /// <summary>The heart has fallen: the chest closes again (ChangeToStance1 = phase2_recover) and the next interval
    /// opens — or, after the last of them, he enrages and the fight ends.</summary>
    static async Task HeartFalls(ClientState conn, KitavaFight fight, LifePhaseGate phases)
    {
        fight.HeartUp = false;
        fight.HeartId = 0;
        bool last;
        lock (conn.Monsters)
        {
            phases.Finish();
            last = phases.NextPercent == 0;
        }
        SpawnedMonster? boss;
        lock (conn.Monsters) conn.Monsters.TryGetValue(fight.BossId, out boss);
        if (boss is not null && Emerges.ActionSkill("ChangeToStance1") is ushort close)
        {
            var at = boss.Walk.Position(conn.Clock.Elapsed.TotalSeconds);
            await Send(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers,
                InstanceProtocol.ActionStartedNow(fight.BossId, at.X, at.Y, at.X, at.Y, close, boss.NextSequence(), 0x0800,
                    fight.BossId),
                $"0x01ab Kitava 0x{fight.BossId:x} closes his chest (ChangeToStance1) at {boss.Life}/{boss.MaxLife}");
        }
        Console.WriteLine($"[gateway] {conn.Wire.Remote} Kitava's heart fell: {(last ? "the fight ends" : "his chest closes")}");
        if (last) { await EndFight(conn, fight); return; }
        await Send(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers,
            InstanceChat.ChatLine("心脏停了。他的胸膛合上了——趁现在砍他。"), "0x0017 Kitava's chest closes");
    }

    /// <summary>The fight cannot be won: he enrages and his X blast destroys the Sign of Purity, a5q6-CompletedBoss is set
    /// (−30 % to all resistances, and Sin is on the rooftop), and he leaves the field without dying.</summary>
    static async Task EndFight(ClientState conn, KitavaFight fight)
    {
        fight.Done = true;
        var (remote, stream, ciphers) = (conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers);
        SpawnedMonster? boss;
        lock (conn.Monsters) conn.Monsters.TryGetValue(fight.BossId, out boss);
        if (boss is not null)
        {
            lock (conn.Monsters)
            {
                boss.Dead = true;
                boss.Life = 0;
                conn.Monsters.Remove(fight.BossId);
            }
            if (conn.Instance is { } instance) instance.World.MonsterOrigins.TryRemove(fight.BossId, out _);
            await Send(remote, stream, ciphers, InstanceProtocol.RemoveObject(fight.BossId),
                $"0x02bd Kitava 0x{fight.BossId:x} leaves the rooftop (the fight cannot be won)");
            ToOthers(conn, fight.BossId, InstanceProtocol.RemoveObject(fight.BossId));
        }
        // QuestStaticRewards 38: the permanent −30 % to elemental and chaos resistances (Player.ResistancePenaltyAct5).
        await SetQuestFlags(remote, stream, ciphers, conn, ["a5q6-CompletedBoss"]);
        await Send(remote, stream, ciphers,
            InstanceChat.ChatLine("奇塔弗的巨口撕碎了纯净之印，整个屋顶开始崩塌。辛在最后一刻抓住了你。"),
            "0x0017 Kitava destroys the Sign of Purity, Sin saves the player");
    }

    /// <summary>Where the exposed heart stands: between his chest and the player fighting him (the room gives the heart no
    /// point of its own, and the arena pads of bossentry.arm only mark the middle).</summary>
    static (int X, int Y) HeartPoint(ClientState conn, (int X, int Y) boss, double now)
    {
        var player = conn.Walk.Position(now);
        double dx = player.X - boss.X, dy = player.Y - boss.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1) { dx = 0; dy = -1; length = 1; }
        for (int r = KitavaHeartDistance; r >= 8; r--)
        {
            int x = boss.X + (int)Math.Round(dx / length * r), y = boss.Y + (int)Math.Round(dy / length * r);
            if (x < 2 || y < 2 || x >= conn.Layout.GridW - 2 || y >= conn.Layout.GridH - 2) continue;
            if (!conn.Layout.WalkableFor(x, y, 2) || conn.Barrier.Contains((x, y))) continue;
            return (x, y);
        }
        return (boss.X + 6, boss.Y);
    }

    internal static void LeavingKitava(ClientState conn)
    {
        if (conn.Instance is { } instance) KitavaFights.Remove(instance);
    }

    /// <summary>
    /// The rooftop's own portal (bossentry.arm (218, 584)) kept off the client until the fight is over. Its type is a
    /// PortalToggleable: PortalToggleable.ot starts untransitioned — create_untransitioned plays clip 1 and stops — and
    /// only its on_transition opens it. The engine creates types that add a Transitionable as a plain AreaTransition
    /// (AreaTransitionCatalog.Resolve), which the client draws in its open state and labels as an arena gate ("阶段2":
    /// live 2026-11-11, it stood in the arena from the moment the player entered, before the Sign was even placed). It is
    /// the way back after Kitava destroys the Sign, so it stands from a5q6-CompletedBoss on.
    /// </summary>
    internal static bool KitavaPortalHidden(ClientState conn, AreaTransitionObject transition)
    {
        if (conn.Layout.Id != KitavaArea || transition.SourceType != KitavaArenaPortal) return false;
        return !ClientHasQuestFlag(conn, "a5q6-CompletedBoss");
    }
}
