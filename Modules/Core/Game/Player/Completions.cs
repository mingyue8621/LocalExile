using GameServer.Core;

public static partial class Game
{
    /// <summary>
    /// What a chat command's argument of a kind can be (<see cref="ChatCommands.Add"/>'s arguments), from the catalogs the
    /// commands look it up in, for the launcher's console (`GameServer.Gateway values &lt;kind&gt;`). Each value is what
    /// the command finds: a name where only one row has it, else the row's Id. Rows the server cannot create are left
    /// out. Null for a kind there is none of.
    /// </summary>
    public static IEnumerable<(string Value, string Detail)>? CompletionValues(string kind) => kind switch
    {
        "area" => AreaTransitionCatalog.Areas.Where(a => a.Id != "NULL").OrderBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
            .Select(a => (a.Id, $"{a.Name} · {AreaSupport(a.Id)}")),
        "monster" => Named(Monsters.All.Where(v => v.Supported), v => v.Name, v => v.Id),
        "item" => Uniques.All.Select(u => (u.Name, "unique"))
            .Concat(Named(Items.All.Where(t => t.Supported && !t.Name.StartsWith('[') && Items.Gem(t) is null), t => t.Name, t => t.Id)),
        "gem" => Items.GemNames().Order(StringComparer.OrdinalIgnoreCase).Select(n => (n, "")),
        "stat" => Stats.Names.Select((n, i) => (n, $"key {i + 1}")),
        "npc" => Named(Npcs.Value.Rows.Where(n => n.Supported), n => n.Name, n => n.Id),
        "quest_flag" => Quests.FlagRows(ClientTables.TableOrNull("QuestFlags")).Keys.Select(id => (id, "")),
        "bosses" => BossAudit(),
        _ => null,
    };

    /// <summary>One line per campaign unique boss, for the boss audit (`GameServer.Gateway values bosses`): the area's
    /// WorldAreas boss place, the client rooms' own boss points for it (data/room-points.json, no placement), and what the
    /// spawn path makes of it — <see cref="MavenNucleus.CombatEffect"/>'s pick, whether that reads as a ranged attack, its
    /// projectile art, the cast/swing times and whether the client can be given the action at all. The Act 5 boss stood
    /// through his whole fight because a shared "Melee" row read as ranged with no projectile (live 2026-11-10), and this
    /// is the same question asked of every boss at once.</summary>
    internal static IEnumerable<(string Value, string Detail)> BossAudit()
    {
        var skills = Skills;
        foreach (var area in AreaTransitionCatalog.Areas.OrderBy(a => a.Id, StringComparer.Ordinal))
        {
            if (area.Id.Length == 0 || !char.IsDigit(area.Id[0])) continue;
            if (Packs.For(area.Id) is not { Bosses.Count: > 0 } packs) continue;
            var declared = AreaPopulation.DeclaredBossPoints(area.Id);
            // The fit of the area's rooms at seed 1: the cells the Gateway itself would find, so a point listed here is one
            // the layout actually placed (AreaPopulation.FitRooms; empty when only the client can build the area).
            var notes = new List<string>();
            var fitted = AreaPopulation.LoadPoints(area.Id, 1, notes);
            var bossPoints = fitted.Where(p => AreaPopulation.BossPoint(p.Name) is not null).ToList();
            for (int i = 0; i < packs.Bosses.Count; i++)
            {
                int row = packs.Bosses[i];
                if (Monsters.Find(row.ToString()) is not { } v)
                {
                    yield return ($"{area.Id}/{row}", $"area={area.Name} place={i + 1} {row}: not in MonsterVarieties");
                    continue;
                }
                int effect = MavenNucleus.CombatEffect(skills, v);
                bool ranged = MonsterAttackIsRanged(skills, v, effect);
                var shot = ranged ? MavenNucleus.Shot(skills, effect, v.RangedWeapon) : null;
                if (ranged && shot is null && skills.IsMelee(effect)) ranged = false;
                bool spell = effect >= 0 && skills.EffectStats(effect, packs.AreaLevel).Any(s => s.Item1 == StatKeys.CastingSpell && s.Item2 != 0);
                bool castTime = spell || effect >= 0 && skills.IsSpell(effect);
                double swing = effect >= 0 && skills.IsSpell(effect) && skills.CastTimeMs(effect) > 0
                    ? skills.CastTimeMs(effect) / 1000.0 : v.AttackMs / 1000.0;
                ushort skillId = effect >= 0 && skills.ActionHash(effect) is ushort hash ? (ushort)(hash & 0x3fff) : SkillCatalog.FirstSkillId;
                bool built = effect >= 0 && skills.FindByRow(skillId, effect, packs.AreaLevel) is not null;
                var standing = AreaPopulation.BossStanding(fitted, packs.Bosses, row);
                string point = standing is { } cell ? $"{cell.X},{cell.Y}" : "none";
                // The area does not place a boss a quest brings (Monsters.Spawnable), and whether it can be created at all.
                bool questSpawned = Quests.QuestSpawned(area.Id, v.Id);
                yield return ($"{area.Id}/{v.Id.Split('/')[^1]}",
                    $"area={area.Name} level={packs.AreaLevel} place={i + 1}/{packs.Bosses.Count} row={row} " +
                    $"supported={v.Supported} spawn={(questSpawned ? "quest" : "area")} point={point} " +
                    $"fitted=[{string.Join(" ", bossPoints.Select(p => $"{p.Name}@{p.X},{p.Y}"))}] " +
                    $"declared=[{string.Join(" ", declared.Select(d => $"{d.Point.Name}@{d.Point.X},{d.Point.Y}"))}] " +
                    $"level1={(ServerLevels.For(area.Id, 1) is null ? "no" : "yes")} " +
                    $"notes={string.Join(" | ", notes)} " +
                    $"effect={effect}:{skills.EffectId(effect) ?? "none"} ranged={ranged} shot={shot?.Metadata ?? "none"} " +
                    $"cast={castTime} swing={swing:0.###} skill=0x{skillId:x4} built={built} bossbar={v.BossHealthBar} " +
                    $"reach={(ranged ? Math.Max(v.AttackRange, v.AggroRange) / 2 : v.AttackRange):0.#}");
            }
        }
    }

    /// <summary>Rows by name where the name is theirs alone, else by Id (the lookups take either); the other as detail.</summary>
    public static IEnumerable<(string Value, string Detail)> Named<T>(IEnumerable<T> rows, Func<T, string> name, Func<T, string> id)
    {
        var list = rows.Where(r => name(r).Length > 0).ToList();
        var counts = list.GroupBy(name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        return list.Select(r => counts[name(r)] == 1 ? (name(r), id(r)) : (id(r), name(r)))
            .OrderBy(v => v.Item1, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>How the Gateway lays an area out: it generates it, a walk-map capture has it, or only the client can.</summary>
    public static string AreaSupport(string id) =>
        ServerLevels.IsSupported(id) ? "server-built"
        : Directory.Exists(Path.Combine("data", "walkability"))
            && Directory.EnumerateFiles(Path.Combine("data", "walkability"), $"{id}_seed*.bin").Any(f => !f.EndsWith(".projectile.bin", StringComparison.Ordinal))
            ? "captured" : "client-only";
}
