using GameServer.Core.Protocol.Instance;

static partial class Game
{
    // ---- what a party bot is wearing ----
    // A controlled player used to carry no items at all: it fought unarmed (WeaponFor falls back to the class's unarmed
    // skill, so its hits were fists) and every other client drew it bare. The PlayerBots module picks gear for the bot's
    // class and level (BotGear); this puts those base items on the character as real equipment, so the model shows them
    // and the weapon's damage counts.

    /// <summary>Wears the gear <see cref="ControlledPlayers.Gear"/> names for this character, at the given level. Replaces
    /// whatever is in those slots, so a bot that came back at another level is dressed for that level.</summary>
    internal static void EquipControlledGear(ClientState conn, int level)
    {
        if (ControlledPlayers.Gear?.Invoke(CharacterName(conn)) is not { Count: > 0 } ids) return;
        var items = ItemsOf(conn);
        var catalog = ItemsFor(conn);
        var used = new HashSet<byte>();
        int worn = 0, carried = 0;
        foreach (string id in ids)
        {
            if (catalog.All.FirstOrDefault(t => t.Id == id) is not { } type || RowFor(type, used) is not { } row) continue;
            used.Add(row);
            // The gems socketed in what this replaces go on its replacement: a bot's only skill is the gem in its weapon, and
            // replacing the weapon without it left the follower with no skill at all, so it stood in every fight doing
            // nothing (live 2026-11-09: "the party bots do not attack"). A bot whose weapon lost its gems that way gets its
            // class's set back here, instead of staying mute for good.
            var gems = items.Items(row).SelectMany(old => old.Item.Sockets).Where(s => s.Gem is not null).ToList();
            if (gems.Count == 0 && row == PlayerInventories.Weapon1)
                gems = [.. SocketedGems(ControlledGems(
                    (GameServer.Core.Accounts.CharacterClass)SkillsFor(conn).ClassFor(conn.ClassRow).IntegerId, level, type))];
            foreach (var old in items.Items(row).ToList()) items.Remove(row, old.Id);
            var fresh = ItemCatalog.Create(type) with { ItemLevel = (byte)Math.Clamp(level, 1, 100) };
            if (gems.Count > 0) fresh = fresh with { Sockets = [.. gems], Links = [(byte)Math.Min(gems.Count, 6)] };
            items.Add(row, 0, 0, fresh);
            worn++;
            carried += gems.Count;
        }
        if (worn > 0) Console.WriteLine($"[bots] {CharacterName(conn)} wears {worn} piece(s) at level {level}, {carried} gem(s) in the weapon");
        EquipControlledFlasks(level, items, catalog);
    }

    /// <summary>The flasks a bot of this level carries: the best life and mana flask its level allows. A bot carried the
    /// level-1 starter flasks (or none), so a fight longer than its mana lasted left it unable to swing a 12-mana Cleave:
    /// five characters stood in front of a boss doing nothing (live 2026-11-10).</summary>
    static void EquipControlledFlasks(int level, PlayerInventories items, ItemCatalog catalog)
    {
        var best = catalog.All
            .Where(t => t.Class is "LifeFlask" or "ManaFlask" && t.DropLevel > 0 && t.DropLevel <= level)
            .GroupBy(t => t.Class)
            .Select(g => g.OrderByDescending(t => t.DropLevel).First())
            .OrderBy(t => t.Class == "LifeFlask" ? 0 : 1)
            .ToList();
        if (best.Count == 0) return;
        foreach (var old in items.Items(PlayerInventories.Flask).ToList()) items.Remove(PlayerInventories.Flask, old.Id);
        byte slot = 0;
        foreach (var type in best) items.Add(PlayerInventories.Flask, slot++, 0, ItemCatalog.Create(type));
        Console.WriteLine($"[bots] flasks for level {level}: {string.Join(", ", best.Select(t => t.Name))}");
    }

    // The slot a base item is worn in: the hand that takes that weapon class, the shield or quiver hand, or the armour row
    // its class names (PlayerInventories.Accepts knows the class lists). A row already given a piece in this pass is skipped,
    // so a dual wielder's second one-hander goes to the off hand instead of replacing the first.
    static byte? RowFor(ItemCatalog.BaseType type, HashSet<byte> used) =>
        new[]
        {
            PlayerInventories.Weapon1, PlayerInventories.Offhand1, PlayerInventories.BodyArmour, PlayerInventories.Helmet,
            PlayerInventories.Gloves, PlayerInventories.Boots, PlayerInventories.Belt,
        }.Cast<byte?>().FirstOrDefault(row => !used.Contains(row!.Value) && PlayerInventories.Accepts(row.Value, type.Class));
}
