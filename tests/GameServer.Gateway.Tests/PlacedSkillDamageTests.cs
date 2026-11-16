using GameServer.Core.Accounts;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.World;

namespace GameServer.Gateway.Tests;

public sealed class PlacedSkillDamageTests
{
    static Item Gem(string name, int level = 1)
    {
        var item = ItemCatalog.Create(Program.Items.Find("Metadata/Items/Gems/" + name)!);
        return item with { GemExperience = Program.Items.Gem(Program.Items.ByHash(item.TypeHash)!)!.ExperienceFor(level) };
    }

    [Fact]
    public void Probe()
    {
        var expected = new Dictionary<string, (double Min, double Max)> {
            ["SkillGemBearTrap"] = (35, 49),       // client GrantedEffectStatSets level 4, secondary base physical
            ["SkillGemFireTrap"] = (27, 41),       // client level 4, spell base fire
            ["SkillGemStormblastMine"] = (5, 15),  // client level 4, spell base lightning
        };
        foreach (var (active, support) in new[] {
            ("SkillGemBearTrap", "SupportGemTrapAndMineDamage"),
            ("SkillGemFireTrap", "SupportGemTrapAndMineDamage"),
            ("SkillGemStormblastMine", "SupportGemTrapAndMineDamage") })
        {
            var conn = SpellTotemExecutionTests.Connection("PlacedDamage");
            var items = Game.ItemsOf(conn);
            foreach (var old in items.EquipmentSnapshot().Where(e => e.Row == PlayerInventories.Weapon1))
                items.Remove(PlayerInventories.Weapon1, old.Item.Id);
            var sword = ItemCatalog.Create(Program.Items.Find("Metadata/Items/Weapons/OneHandWeapons/OneHandSwords/OneHandSword1")!);
            items.Add(PlayerInventories.Weapon1, 0, 0, sword with
            {
                Sockets = [new(SocketColour.White, Gem(active, 4)), new(SocketColour.White, Gem(support, 4))], Links = [2],
            });
            foreach (var g in Game.GemSkillEffects(conn))
            {
                if (g.Name != active.Replace("SkillGem", "")) continue;
                var d = Game.SupportedHitFor(conn, g.Id, suppress: false);
                var (min, max) = expected[active];
                Assert.Equal(min, d.MinTotal);
                Assert.Equal(max, d.MaxTotal);
            }
            PlayerInventories.Forget("PlacedDamage");
        }
    }
}
