using GameServer.Core.Protocol.Instance;
using GameServer.Core.World;

static partial class Game
{
    // ---- a party bot's own decisions in a fight ----
    // Everything here runs only for a controlled player (ControlledPlayers), never for a human's connection: which enemy
    // to hit, with which gem, whether to close the distance, and when to drink. A follower used to take the first gem that
    // could hit at all and only ever hit what was already in its reach, so it stood still while a rare killed the leader
    // three cells away (live 2026-11-09: "not smart, like an idiot").

    /// <summary>A gem the follower can bring to bear, with what aiming it takes: the reach the server itself uses for a
    /// hit in place (<see cref="ControlledInPlaceReach"/>), whether it covers an area, and how long its use occupies the
    /// character (the follower may not move mid-swing).</summary>
    internal sealed record ControlledSkill(ushort Id, int Effect, int Level, int Quality, double Reach, bool Area, double Seconds);

    const double ControlledTargetRange = 60, ControlledTargetSlack = 5;
    /// <summary>Under this share of its life a follower drinks, as a player would.</summary>
    const double ControlledFlaskBelow = .35;
    /// <summary>A monster within this many cells of the target means there is a pack, so an area gem is worth it.</summary>
    const double ControlledPackRange = 15;
    const int ControlledPackSize = 3;

    internal static List<ControlledSkill> ControlledSkills(ClientState conn)
    {
        var list = new List<ControlledSkill>();
        foreach (var gem in GemSkillEffects(conn))
        {
            if (GemSkillInstanceDisabled(conn, gem.Effect, gem.Id) is not null) continue;
            if (!SkillHitPlan.ForSkill(SkillsFor(conn), gem.Effect, gem.Level, gem.Quality).CanHit) continue;
            double seconds = SkillsFor(conn).IsAttack(gem.Effect)
                ? SupportedAttackSeconds(conn, gem.Id) : (CastMs(conn, gem.Effect, gem.Id) ?? 750) / 1000.0;
            var supported = SupportedSkill(conn, gem.Id);
            bool area = SkillRadius(conn, supported.Stats, SkillsFor(conn).IsSpell(gem.Effect), gem.Effect) >= 12;
            list.Add(new(gem.Id, gem.Effect, gem.Level, gem.Quality,
                ControlledInPlaceReach(conn, gem.Id, gem.Effect), area, seconds));
        }
        return list;
    }

    /// <summary>The enemy to hit: the rarest first, then the nearest. Null when nothing hostile stands within the range the
    /// old code used (60 of the leader, 65 of the follower).</summary>
    internal static (uint Id, SpawnedMonster Monster)? ControlledTarget(ClientState conn, ClientState leader, (int X, int Y) here, double now)
    {
        var leaderAt = leader.Walk.Position(now);
        lock (conn.Monsters)
            return conn.Monsters
                .Where(p => p.Value is { Hostile: true, Dead: false, Life: > 0, Invulnerable: false, Asleep: false }
                    && !conn.AsleepMonsters.Contains(p.Key)
                    && Dist(p.Value.Walk.Position(now), leaderAt) < ControlledTargetRange
                    && Dist(p.Value.Walk.Position(now), here) < ControlledTargetRange + ControlledTargetSlack)
                .OrderByDescending(p => (int)p.Value.Rarity)
                .ThenBy(p => Dist(p.Value.Walk.Position(now), here))
                .Select(p => ((uint Id, SpawnedMonster Monster)?)(p.Key, p.Value))
                .FirstOrDefault();
    }

    /// <summary>Whether several monsters stand around this one: an area gem hits them all.</summary>
    internal static bool ControlledPacked(ClientState conn, SpawnedMonster target, double now)
    {
        var at = target.Walk.Position(now);
        lock (conn.Monsters)
            return conn.Monsters.Count(p => p.Value is { Hostile: true, Dead: false, Life: > 0, Asleep: false }
                && Dist(p.Value.Walk.Position(now), at) <= ControlledPackRange) >= ControlledPackSize;
    }

    /// <summary>The gems that reach that distance, in the order they are socketed, with an area gem first when a pack stands
    /// there. Empty when nothing reaches: the caller closes the distance.</summary>
    internal static List<ControlledSkill> ControlledSkillsFor(List<ControlledSkill> skills, double distance, bool packed)
    {
        var reaching = skills.Where(s => distance <= s.Reach).ToList();
        if (packed && reaching.FirstOrDefault(s => s.Area) is { } area)
        {
            reaching.Remove(area);
            reaching.Insert(0, area);
        }
        return reaching;
    }

    /// <summary>Whether this gem can be used now: the next gem in the list is used meanwhile, where the follower used to
    /// stand on a cooling skill doing nothing.</summary>
    internal static bool ControlledSkillReady(ClientState conn, ControlledSkill skill, double now)
    {
        lock (conn.Monsters)
        {
            if (CooldownFor(conn, skill.Effect, skill.Level, skill.Quality) is not { } plan) return true;
            return CooldownStateLocked(conn, skill.Effect, plan, now).Ready;
        }
    }

    /// <summary>Walk to just inside the reach of that enemy. <see cref="ControlledMoveFits"/> keeps the destination inside
    /// the leader's leash and on walkable ground, so chasing never leaves the party or walks through a wall.</summary>
    internal static bool ApproachControlledTarget(ClientState conn, ClientState leader, (int X, int Y) monster, double reach, double now)
    {
        var plan = FollowerPlans.GetOrCreateValue(conn);
        if (now < plan.ApproachAt) return plan.Approaching;
        plan.ApproachAt = now + .5;
        var here = conn.Walk.Position(now);
        double dx = here.X - monster.X, dy = here.Y - monster.Y, length = Math.Sqrt(dx * dx + dy * dy);
        if (length < .001) return plan.Approaching = false;
        // A cell short of the reach, on the side it already stands: no walking into the monster.
        int step = Math.Max(2, (int)Math.Round(Math.Max(2, reach) - 3));
        var at = (X: monster.X + (int)Math.Round(dx / length * step), Y: monster.Y + (int)Math.Round(dy / length * step));
        plan.Approaching = ControlledMoveFits(conn, leader, at.X, at.Y, now);
        if (!plan.Approaching) return false;
        MoveControlledFollower(conn, at, now);
        return true;
    }

    /// <summary>Drink the first life flask of the bar that still has charges, under <see cref="ControlledFlaskBelow"/> of
    /// the character's life. False when there is nothing to drink, the bar is empty or it drank a moment ago.</summary>
    internal static async Task<bool> DrinkControlledFlask(ClientState conn, double now)
    {
        if (conn.PlayerLife == 0 || conn.PlayerMaxLife == 0) return false;
        if (conn.PlayerLife / (double)conn.PlayerMaxLife > ControlledFlaskBelow) return false;
        var plan = FollowerPlans.GetOrCreateValue(conn);
        if (now < plan.FlaskAt) return false;
        plan.FlaskAt = now + 1.2;
        foreach (var held in ItemsOf(conn).Items(PlayerInventories.Flask))
        {
            if (ItemsFor(conn).ByHash(held.Item.TypeHash) is not { } type) continue;
            if (ItemsFor(conn).FlaskOf(type) is not { LifePerUse: > 0 } use) continue;
            if (held.Item.Charges < use.ChargesPerUse) continue;
            await UseFlask(conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers, conn, held.X);
            Console.WriteLine($"[bots] {CharacterName(conn)} drinks a life flask: {conn.PlayerLife}/{conn.PlayerMaxLife}");
            return true;
        }
        return false;
    }
}
