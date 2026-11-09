using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

/// <summary>A Vaal Orb corrupts what it is used on, uniques too, and can add a corruption implicit: live 2026-11-09 the
/// orb did nothing at all on a unique (the old rule refused them) and never added a Vaal implicit.</summary>
public class VaalCorruptionTests
{
    static Item Base(string id) => ItemCatalog.Create(Program.Items.Find(id)!);

    static bool IsCorruptionImplicit(Item item, ItemMod mod) =>
        mod.Section == ModSection.Implicit && Program.Items.Mods.ByHash(mod.Hash) is { GenerationType: 5 };

    [Fact]
    public void A_unique_is_corrupted_and_gains_the_vaal_implicit_its_base_has()
    {
        var rolls = new ItemRolls(Program.Items);
        var type = Program.Items.Find("Metadata/Items/Armours/BodyArmours/BodyStr3")!;   // a plain armour base
        Assert.True(Pool(type, 60) > 0, "the data has no corruption implicit for a strength body armour");
        var unique = Base(type.Id) with { Rarity = ItemRarity.Unique, ItemLevel = 60 };
        bool added = false;
        for (int seed = 0; seed < 20; seed++)
        {
            var result = rolls.Vaal(unique, type, (0, 0, 0), new Random(seed));
            Assert.NotNull(result);
            Assert.True(result!.Corrupted);
            Assert.Equal(ItemRarity.Unique, result.Rarity);
            added |= result.Mods.Any(m => IsCorruptionImplicit(result, m));
        }
        Assert.True(added, "no use in twenty gave the unique a Vaal implicit");
    }

    [Fact]
    public void A_rare_can_gain_a_corruption_implicit_and_is_always_corrupted()
    {
        var rolls = new ItemRolls(Program.Items);
        var type = Program.Items.Find("Metadata/Items/Weapons/OneHandWeapons/OneHandSwords/OneHandSword6")!;
        var rare = rolls.Roll(Base(type.Id) with { ItemLevel = 60 }, ItemRarity.Rare, new Random(4));
        Assert.Equal(ItemRarity.Rare, rare.Rarity);
        bool added = false;
        for (int seed = 0; seed < 30; seed++)
        {
            var result = rolls.Vaal(rare, type, (0, 0, 0), new Random(seed));
            Assert.NotNull(result);
            Assert.True(result!.Corrupted);
            added |= result.Mods.Any(m => IsCorruptionImplicit(result, m));
        }
        Assert.True(added, "no use in thirty added a corruption implicit");
    }

    [Fact]
    public void A_jewel_can_take_a_vaal_implicit_too()
    {
        var rolls = new ItemRolls(Program.Items);
        var type = Program.Items.Find("Metadata/Items/Jewels/JewelInt")!;
        Assert.True(Pool(type, 60) > 0, "the data has no corruption implicit for a jewel");
        var jewel = rolls.Roll(Base(type.Id) with { ItemLevel = 60 }, ItemRarity.Rare, new Random(7));
        bool added = false;
        for (int seed = 0; seed < 30; seed++)
        {
            var result = rolls.Vaal(jewel, type, (0, 0, 0), new Random(seed));
            Assert.NotNull(result);
            Assert.True(result!.Corrupted);
            added |= result.Mods.Any(m => IsCorruptionImplicit(result, m));
        }
        Assert.True(added, "no use in thirty gave the jewel a corruption implicit");
    }

    [Fact]
    public void A_corrupted_item_is_still_refused()
    {
        var rolls = new ItemRolls(Program.Items);
        var type = Program.Items.Find("Metadata/Items/Armours/BodyArmours/BodyStr3")!;
        var corrupted = Base(type.Id) with { Rarity = ItemRarity.Rare, Corrupted = true };
        Assert.Null(rolls.Vaal(corrupted, type, (0, 0, 0), new Random(1)));
    }

    static int Pool(ItemCatalog.BaseType type, int itemLevel) =>
        Program.Items.Mods.All.Count(m => m.GenerationType == 5 && m.Domain == type.ModDomain
            && m.Level <= itemLevel && m.WeightFor(type.Tags) > 0);
}
