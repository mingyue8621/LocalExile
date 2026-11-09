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
