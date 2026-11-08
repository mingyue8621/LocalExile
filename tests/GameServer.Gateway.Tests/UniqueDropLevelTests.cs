using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

/// <summary>A base's uniques each carry their own level requirement, which the base's drop level does not bound: Biting
/// Braid is a Chain Belt (drop level 2) that requires 52, so a low area must not drop it (live 2026-11-09: a level-10
/// character identified level-30 to level-60 uniques off low bases).</summary>
public class UniqueDropLevelTests
{
    [Fact]
    public void A_unique_never_needs_a_level_the_area_does_not_have()
    {
        var catalog = Program.Items;
        var drops = new ItemDrops(catalog, uniques: new UniqueCatalog(catalog));
        // Chain Belt and Simple Robe: bases of drop level 2 and 3 that host uniques well above them.
        foreach (string id in new[] { "Metadata/Items/Belts/Belt2", "Metadata/Items/Armours/BodyArmours/BodyInt1" })
        {
            Assert.NotNull(catalog.Find(id));
            var type = catalog.Find(id)!;
            for (int level = 1; level <= 20; level++)
                for (int roll = 0; roll < 40; roll++)
                    if (drops.Unique(type, level, level + ItemDrops.ItemLevelBonus(MonsterRarity.Unique), new Random(level * 1000 + roll)) is { } item)
                        Assert.True(Required(catalog, item) <= level, $"{id} at level {level} gave a unique needing {Required(catalog, item)}");
        }
    }

    [Fact]
    public void A_unique_above_the_area_comes_back_only_once_the_area_reaches_it()
    {
        var catalog = Program.Items;
        var drops = new ItemDrops(catalog, uniques: new UniqueCatalog(catalog));
        var belt = catalog.Find("Metadata/Items/Belts/Belt2")!;   // Chain Belt, drop level 2
        // Every Chain Belt unique the catalogue rolls at level 60 is legal there, and at least one needs more than a level
        // 2 area: the gate is what keeps that one out of the early acts.
        var highs = Enumerable.Range(0, 300)
            .Select(seed => drops.Unique(belt, 60, 63, new Random(seed)))
            .OfType<Item>()
            .Select(item => Required(catalog, item))
            .ToList();
        Assert.NotEmpty(highs);
        Assert.All(highs, need => Assert.True(need <= 60));
        Assert.Contains(highs, need => need > 2);
    }

    static int Required(ItemCatalog catalog, Item item) =>
        ItemModStats.LevelRequirement(item, catalog.ByHash(item.TypeHash)?.DropLevel ?? 0, catalog.Mods);
}
