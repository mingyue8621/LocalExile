using GameServer.Core.Accounts;
using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

public sealed class VolcanicFissureTests
{
    static Item Gem(string name) => ItemCatalog.Create(Program.Items.Find("Metadata/Items/Gems/" + name)!);

    [Fact]
    public void Probe()
    {
        var conn = SpellTotemExecutionTests.Connection("VolcanicProbe");
        var items = Game.ItemsOf(conn);
        foreach (var old in items.EquipmentSnapshot().Where(e => e.Row == PlayerInventories.Weapon1))
            items.Remove(PlayerInventories.Weapon1, old.Item.Id);
        var sword = ItemCatalog.Create(Program.Items.Find("Metadata/Items/Weapons/OneHandWeapons/OneHandSwords/OneHandSword1")!);
        items.Add(PlayerInventories.Weapon1, 0, 0, sword with { Sockets = [new(SocketColour.White, Gem("SkillGemVolcanicFissure"))], Links = [1] });
        var gem = Game.GemSkillEffects(conn).Single();
        // Volcanic Fissure is flagged is_projectile in the client but has no executable projectile recipe.
        Assert.False(Game.UsesProjectileRuntime(gem.Effect));
        Assert.True(Game.ProjectilePathAvailable("test", conn, gem.Id, gem.Effect));
        Assert.True(Game.SkillsFor(conn).IsAttack(gem.Effect));
        var damage = Game.SupportedHitFor(conn, gem.Id, suppress: false);
        Assert.True(damage.MinTotal > 0);
        Assert.Contains(GameServer.Core.World.DamageType.Fire, damage.Components.Select(c => c.Type));
    }
}
