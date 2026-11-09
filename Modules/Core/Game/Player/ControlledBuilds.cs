using GameServer.Core.Accounts;
using GameServer.Core.Protocol.Instance;

static partial class Game
{
    // Conservative starting archetypes: ordinary connected allocations, wearable bases and compatible links.
    // No requirement exemptions or hidden attribute bonuses are used for a controlled character.
    internal static CharacterSaves.Save BuildControlledPlayer(CharacterClass characterClass, int level)
    {
        level = Math.Clamp(level, 1, 100);
        var cls = Skills.ClassFor(Skills.RowOfClass((int)characterClass));
        bool spell = characterClass is CharacterClass.Witch or CharacterClass.Templar;
        bool bow = characterClass == CharacterClass.Ranger;
        string skill = characterClass switch
        {
            CharacterClass.Witch or CharacterClass.Templar => "Fireball",
            CharacterClass.Ranger => "Split Arrow", CharacterClass.Marauder => "Ground Slam",
            CharacterClass.Shadow => "Frost Blades", _ => "Cleave",
        };
        string weapon = spell ? "Wand" : bow ? "Bow" : characterClass == CharacterClass.Marauder ? "Two Hand Mace"
            : characterClass == CharacterClass.Shadow ? "Claw" : "One Hand Sword";
        var nodes = ControlledNodes(characterClass, level);
        var passiveStats = Passives.Stats(nodes);
        int Attr(string name, int basis) => basis + (Stats.Key(name) is uint key ? passiveStats.GetValueOrDefault(key) : 0)
            + (Stats.Key("base_all_attributes") is uint all ? passiveStats.GetValueOrDefault(all) : 0);
        int str = Attr("base_strength", cls.Strength), dex = Attr("base_dexterity", cls.Dexterity), intel = Attr("base_intelligence", cls.Intelligence);
        bool Fits(ItemCatalog.BaseType t)
        {
            var req = Items.Requirements(t);
            return t.DropLevel <= level && req.Str <= str && req.Dex <= dex && req.Int <= intel
                && ItemModStats.LevelRequirement(ItemCatalog.Create(t), t.DropLevel, Items.Mods) <= level;
        }
        var inventory = new Dictionary<byte, List<CharacterSaves.SavedPlacement>>();
        uint itemId = 0;
        Item? Wear(string itemClass, byte row)
        {
            var type = Items.All.Where(t => t.Class == itemClass && t.Supported && Fits(t)
                && !t.Id.Contains("Royale", StringComparison.Ordinal) && !t.Id.Contains("Demigod", StringComparison.Ordinal)
                && !t.Components.Contains("Transformed"))
                .OrderByDescending(t => t.DropLevel).ThenBy(t => t.Id, StringComparer.Ordinal).FirstOrDefault();
            if (type is null) return null;
            var item = ItemCatalog.Create(type) with { ItemLevel = (byte)level, Quality = (byte)(level >= 20 ? 20 : 0), Sockets = [], Links = [] };
            inventory[row] = [new(++itemId, 0, 0, CharacterSaves.ToSaved(item, Items.Mods))];
            return item;
        }
        var main = Wear(weapon, 2) ?? (weapon == "Two Hand Mace" ? Wear("One Hand Mace", 2)
            : weapon == "Claw" ? Wear("Dagger", 2) : null)
            ?? throw new InvalidDataException($"No wearable {weapon} for controlled {characterClass}");
        if (Items.ByHash(main.TypeHash)?.Class is "Wand" or "One Hand Sword" or "Claw" or "Dagger" or "One Hand Mace") Wear("Shield", 3);
        Wear("Body Armour", 1); Wear("Helmet", 4); Wear("Gloves", 8); Wear("Boots", 9); Wear("Belt", 10);
        var gems = ControlledGems(characterClass, level, Items.ByHash(main.TypeHash)!);
        if (gems.Count == 0) throw new InvalidDataException($"No usable {skill} for controlled {characterClass}");
        main = main with { Sockets = SocketedGems(gems), Links = [(byte)gems.Count] };
        inventory[2] = [inventory[2][0] with { Item = CharacterSaves.ToSaved(main, Items.Mods) }];
        return new(inventory, Experience: CharacterStats.ExperienceFor(level), Passives: nodes,
            SkillBar: new() { [0] = "Action/Move", [1] = Skills.EffectId(Items.GemEffects(gems[0]).First())! },
            ModuleData: new() { ["PlayerBots.BuildVersion"] = "1" });
    }

    /// <summary>The gems a controlled character carries in its weapon at that level: its one active skill, at the highest
    /// level its attributes allow, and then the supports that resolve for that weapon's class. Shared by the starting build
    /// and by the gear pass, which replaces the weapon and must not take the bot's only skill with it
    /// (<see cref="EquipControlledGear"/>). Empty when the class has no usable gem at that level.</summary>
    internal static List<Item> ControlledGems(CharacterClass characterClass, int level, ItemCatalog.BaseType weapon)
    {
        level = Math.Clamp(level, 1, 100);
        bool spell = characterClass is CharacterClass.Witch or CharacterClass.Templar;
        bool bow = characterClass == CharacterClass.Ranger;
        string skill = characterClass switch
        {
            CharacterClass.Witch or CharacterClass.Templar => "Fireball",
            CharacterClass.Ranger => "Split Arrow", CharacterClass.Marauder => "Ground Slam",
            CharacterClass.Shadow => "Frost Blades", _ => "Cleave",
        };
        var (str, dex, intel) = ControlledAttributes(characterClass, level);
        Item? Gem(string name)
        {
            var type = Items.All.FirstOrDefault(t => t.Name == name && Items.Gem(t) is { IsVaalOrAwakened: false });
            if (type is null || Items.Gem(type) is not { } data) return null;
            int effect = data.Variant(0).Effect;
            int gemLevel = Math.Min(data.MaxLevel, Skills.MaxLevelAt(effect, level));
            while (gemLevel > 0)
            {
                int required = Skills.PlayerLevelReq(effect, gemLevel);
                if (required <= level && SkillCatalog.GemAttributeRequirement(data.StrengthPercent, required, data.IsSupport) <= str
                    && SkillCatalog.GemAttributeRequirement(data.DexterityPercent, required, data.IsSupport) <= dex
                    && SkillCatalog.GemAttributeRequirement(data.IntelligencePercent, required, data.IsSupport) <= intel)
                    return ItemCatalog.Create(type) with { GemExperience = data.ExperienceFor(gemLevel) };
                gemLevel--;
            }
            return null;
        }
        if (Gem(skill) is not { } active) return [];
        string[] candidates = spell ? ["Added Lightning Damage Support", "Faster Casting Support", "Controlled Destruction Support"]
            : bow ? ["Added Cold Damage Support", "Faster Attacks Support", "Pierce Support"]
            : ["Melee Physical Damage Support", "Faster Attacks Support", "Added Fire Damage Support"];
        var gems = new List<Item> { active };
        foreach (string supportName in candidates)
        {
            if (gems.Count >= 3) break;
            if (Gem(supportName) is not { } support) continue;
            int effect = Items.Gem(Items.ByHash(support.TypeHash)!)!.Variant(support.GemVariant).Effect;
            if (Supports.Resolve(Items.GemEffects(active).First(), [new(effect, Items.GemLevel(support), 0, gems.Count)], Skills,
                weapon.ClassRow).Count > 0) gems.Add(support);
        }
        return gems;
    }

    /// <summary>Those gems as sockets in the order they are linked.</summary>
    internal static ItemSocket[] SocketedGems(IReadOnlyList<Item> gems) =>
        [.. gems.Select(g => new ItemSocket((SocketColour)(Items.Gem(Items.ByHash(g.TypeHash)!)!.Colour), g))];

    /// <summary>The passives a controlled character of that class and level allocates: a conservative, ordinary allocation
    /// that prefers attributes, life and its own damage type. Shared by its starting gear and by the gear it is given later
    /// (<see cref="ControlledAttributes"/>).</summary>
    internal static List<ushort> ControlledNodes(CharacterClass characterClass, int level)
    {
        level = Math.Clamp(level, 1, 100);
        var cls = Skills.ClassFor(Skills.RowOfClass((int)characterClass));
        bool spell = characterClass is CharacterClass.Witch or CharacterClass.Templar;
        bool bow = characterClass == CharacterClass.Ranger;
        var nodes = new List<ushort>();
        double Score(ushort id)
        {
            var n = Passives.Find(id);
            if (n is null || n.IsKeystone || n.IsJewelSocket) return -1000;
            double score = .1;
            foreach (var (key, value) in n.Stats)
            {
                string name = Stats.Name(key) ?? "";
                if (name is "base_strength" or "base_dexterity" or "base_intelligence" or "base_all_attributes") score += value * .65;
                if (name is "maximum_life_+%" or "base_maximum_life") score += value * 2;
                if (name is "base_resist_all_elements_%") score += value;
                if (name == "damage_+%" || spell && name is "spell_damage_+%" or "fire_damage_+%"
                    || !spell && name is "attack_damage_+%" or "physical_damage_+%"
                    || bow && name == "projectile_damage_+%") score += value;
                if (name == (spell ? "base_cast_speed_+%" : "attack_speed_+%")) score += value * 2;
            }
            return score;
        }
        for (int point = 0; point < level - 1; point++)
        {
            var frontier = OrdinaryPassives.Frontier((byte)cls.IntegerId, AscendancyFamily.Normal, nodes)
                .Where(id => Score(id) >= 0).ToArray();
            if (frontier.Length == 0) break;
            var pick = frontier.OrderByDescending(id => Score(id) + .5 * OrdinaryPassives.Adjacent(id, AscendancyFamily.Normal)
                .Where(next => !nodes.Contains(next)).Select(Score).DefaultIfEmpty(0).Max()).ThenBy(id => id).First();
            nodes.Add(pick);
        }
        OrdinaryPassives.Validate((byte)cls.IntegerId, AscendancyFamily.Normal, nodes);
        return nodes;
    }

    static readonly System.Collections.Concurrent.ConcurrentDictionary<(CharacterClass Class, int Level), (int Str, int Dex, int Int)> AttributeCache = new();

    /// <summary>The strength, dexterity and intelligence that build reaches at that level (base attributes plus the
    /// allocation's attribute nodes): the highest requirements a base item it wears may ask for. Cached, since every bot's
    /// gear is picked through it.</summary>
    internal static (int Str, int Dex, int Int) ControlledAttributes(CharacterClass characterClass, int level)
    {
        level = Math.Clamp(level, 1, 100);
        return AttributeCache.GetOrAdd((characterClass, level), key =>
        {
            var cls = Skills.ClassFor(Skills.RowOfClass((int)key.Class));
            var passiveStats = Passives.Stats(ControlledNodes(key.Class, key.Level));
            int Attr(string name, int basis) => basis + (Stats.Key(name) is uint stat ? passiveStats.GetValueOrDefault(stat) : 0)
                + (Stats.Key("base_all_attributes") is uint all ? passiveStats.GetValueOrDefault(all) : 0);
            return (Attr("base_strength", cls.Strength), Attr("base_dexterity", cls.Dexterity), Attr("base_intelligence", cls.Intelligence));
        });
    }

    /// <summary>Whether a base item's own attribute requirements are within what that class reaches at that level: the check
    /// behind the gear bots are given, so none is dressed in something it could not wear.</summary>
    internal static bool GearFitsClass(CharacterClass characterClass, int level, ItemCatalog.BaseType type)
    {
        var (str, dex, intel) = ControlledAttributes(characterClass, level);
        var req = Items.Requirements(type);
        return req.Str <= str && req.Dex <= dex && req.Int <= intel;
    }
}
