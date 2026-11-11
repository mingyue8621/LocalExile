using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;

public static partial class Game
{
    public static async Task InitializeControlledPlayer(ClientState conn, ClientState leader)
    {
        RegisterVitalsAdmission(conn);
        PrepareCharacterForArea(conn.Wire.Remote, conn, conn.Pools.Level);
        // The gear the PlayerBots module picked for this character, put on before the peer template is built from it, and
        // the skins that gear takes, so a bot is dressed like a player of its class rather than bare.
        EquipControlledGear(conn, conn.Pools.Level);
        WearBotCosmetics(conn);
        InitializeCharacterVitals(conn);
        conn.ControlledPrepared = true;
        var at = ControlledFollowerPoint(conn, leader, arrival: true);
        conn.Walk.Teleport(at.X, at.Y, conn.Clock.Elapsed.TotalSeconds);
        conn.PeerType = InstanceEntities.ObjectTypeHash(SkillsFor(conn).ClassFor(conn.ClassRow).ObjectType);
        conn.PeerTemplate = PlayerForCreation(conn) with { GridX = at.X, GridY = at.Y };
        conn.ClientSkillIds = [.. conn.PeerTemplate.Skills!.Select(s => s.Id)];
        conn.PlayerSent = conn.PlayerObjectCreated = true;
        Program.InGame[conn] = 0;
        global::HotReload.Register(conn);
        await PopulateArea(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers, conn);
        if (conn.Closed) return;
        PublishPlayerPose(conn);
        EnsurePlayerUpdates(conn);
    }

    public static async Task StepControlledPlayer(ClientState conn, ClientState leader)
    {
        if (conn.Closed || leader.Closed || conn.LeavingArea || conn.Paused || !leader.PlayerSent) return;
        EnsurePlayerUpdates(conn);
        double now = conn.Clock.Elapsed.TotalSeconds;
        if (conn.PlayerLife == 0)
        {
            if (double.IsNaN(conn.ControlledDeadSince)) conn.ControlledDeadSince = now;
            if (now - conn.ControlledDeadSince >= 3)
            {
                await Resurrect(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers, conn, inTown: false);
                conn.ControlledDeadSince = double.NaN;
            }
            return;
        }
        bool following = KeepControlledFollowerNear(conn, leader, now, ControlledExternalMovement(conn, leader, now));
        if (now < conn.ControlledNextAction) return;
        conn.ControlledNextAction = now + .35;
        if (conn.Passives.Count < conn.Pools.Level - 1)
        {
            var cls = (GameServer.Core.Accounts.CharacterClass)SkillsFor(conn).ClassFor(conn.ClassRow).IntegerId;
            var nodes = BuildControlledPlayer(cls, conn.Pools.Level).Passives!;
            lock (conn.Monsters)
            {
                CharacterSaves.SetMainAscendancy(CharacterName(conn), conn.MainAscendancy, nodes, conn.MasteryEffects,
                    expected: conn.SavedPassiveProgress);
                conn.Passives = [.. nodes];
                conn.SavedPassiveProgress = CharacterSaves.PassiveProgressSnapshot.From(CharacterSaves.Load(CharacterName(conn))!);
                conn.Pools = PoolsFor(conn, conn.Pools.Level);
                conn.ApplyMovementSpeed();
            }
            await SendCharacterStats(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers, conn, "bot allocates earned passive points");
        }
        if (await ExecuteControlledIntent(conn, leader, now, following)) return;
        var here = conn.Walk.Position(now);
        bool inTown = InTown(conn);
        var plan = FollowerPlans.GetOrCreateValue(conn);
        // First the thing a player reaches for: a follower that stands in the fire until it dies is what reads as an idiot.
        if (!inTown && await DrinkControlledFlask(conn, now)) return;
        // Further behind than a fight may take it: it walks back first. Without this every enemy met on the way keeps it
        // fighting where it stands, the engage leash is pushed forward each time it picks one, and it never closes the gap
        // at all (live 2026-11-11: four followers stuck 110 cells behind the player, swinging at whatever came by).
        if (!inTown && Dist(here, leader.Walk.Position(now)) > ControlledEngageLeash)
        {
            plan.EngageUntil = 0;
            ControlledSmallTalkAt(conn, now);
            return;
        }
        // The enemy it picks for itself: the rarest, then what is on the player's back (ControlledCombat.ControlledTarget).
        var enemy = inTown ? null : ControlledTarget(conn, leader, here, now);
        if (enemy is not { } picked)
        {
            plan.EngageUntil = 0;                       // nothing to fight: the party tightens again
            if (!following) IdleControlledFollower(conn, leader, now);
            ControlledSmallTalkAt(conn, now);
            return;
        }
        // It has picked a fight, so the leash widens for as long as it keeps one (ControlledFollowing).
        plan.EngageUntil = now + 1.2;
        var at = picked.Monster.Walk.Position(now);
        double distance = Dist(here, at);
        if (picked.Monster.Rarity >= MonsterRarity.Rare) ControlledWarn(conn, now, picked.Id, $"当心，{picked.Monster.Label}！");
        if (leader.PlayerLife > 0 && leader.PlayerMaxLife > 0 && leader.PlayerLife / (double)leader.PlayerMaxLife < .3
            && MindOf(conn).Talk > .7)
            ControlledSay(conn, now, "你血不多了，退一下。", gap: 120);
        var skills = ControlledSkills(conn);
        if (skills.Count == 0) return;
        // A crowd inside its reach is given ground, as a player kites (ControlledMind.ControlledBackOff).
        if (ControlledBackOff(conn, leader, at, distance, skills.Max(s => s.Reach), now)) return;
        // A gem that reaches it, area first when a pack stands there, and one that is both off cooldown and affordable: a
        // cooling gem hands the turn to the next instead of standing still on it.
        var reaching = ControlledSkillsFor(skills, distance, ControlledPacked(conn, picked.Monster, now));
        var skill = reaching.FirstOrDefault(s => s.Mana <= conn.PlayerMana && ControlledSkillReady(conn, s, now));
        bool onTheMove = following || leader.Walk.Moving(now);
        double gap = Dist(here, leader.Walk.Position(now));
        double ahead = ControlledAhead(here, leader.Walk.Position(now), leader.Walk.Destination);
        if (skill is null)
        {
            if (reaching.Count == 0)
            {
                ApproachControlledTarget(conn, leader, at, skills.Max(s => s.Reach), now);
                return;
            }
            // Every gem that reaches is beyond its mana: the weapon's own attack costs none and still hits, as a player's
            // fallback does. A party of bots used to stand in front of the boss with 3 to 10 mana, swinging at nothing
            // (live 2026-11-10: Malachai taking ~1,300 damage a second from five characters).
            if (reaching.Any(s => s.Mana <= conn.PlayerMana)) return;   // the rest is cooling: wait a tick
            if (onTheMove && !ControlledFollowAttackFits(gap, leader.Walk.Moving(now) ? leader.Walk.Speed : 0, .6, ahead))
                return;
            HoldControlledFollowerAttack(conn, now, .6, picked.Id);
            var bare = new LoginWriter().U16(InstanceTransfer.UseSkillOnObjectId).U32(picked.Id)
                .U16(SkillCatalog.FirstSkillId).U16(0xffff).U16(onTheMove ? (ushort)1 : (ushort)0).ToArray();
            await OnClientMessages(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers,
                [(InstanceTransfer.UseSkillOnObjectId, bare)], [], conn);
            return;
        }
        if (onTheMove && !ControlledFollowAttackFits(gap, leader.Walk.Moving(now) ? leader.Walk.Speed : 0, skill.Seconds, ahead))
            return;
        HoldControlledFollowerAttack(conn, now, skill.Seconds, picked.Id);
        var bytes = new LoginWriter().U16(InstanceTransfer.UseSkillOnObjectId).U32(picked.Id)
            .U16(skill.Id).U16(0xffff).U16(onTheMove ? (ushort)1 : (ushort)0).ToArray();
        await OnClientMessages(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers,
            [(InstanceTransfer.UseSkillOnObjectId, bytes)], [], conn);
    }

    static double ControlledInPlaceReach(ClientState conn, ushort skill, int effect)
    {
        if (SkillsFor(conn).ActionHash(effect) == GroundSlamWave.Action)
            return GroundSlamWave.Length(SkillRadius(conn, SupportedSkill(conn, skill).Stats, spell: false, effect));
        if (SkillsFor(conn).IsMelee(effect)) return WeaponFor(conn).Range;
        if (SkillsFor(conn).IsSelfTargeted(effect))
            return SkillRadius(conn, SupportedSkill(conn, skill).Stats, SkillsFor(conn).IsSpell(effect), effect);
        // A spell or an arrow reaches as far as the bot fights (ControlledSight), rather than the 65 an older check used.
        return ControlledSight;
    }
}
