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
        // Volcanic Fissure uses the client's lava mortar: six lobbed bursts with the skill's own physical/fire
        // damage, reduced by its -50% projectile hit/ailment stat.
        Assert.True(Game.UsesProjectileRuntime(gem.Effect));
        Assert.True(Game.ProjectilePathAvailable("test", conn, gem.Id, gem.Effect));
        Assert.True(Game.SkillsFor(conn).IsAttack(gem.Effect));
        var definition = Game.ResolveProjectileDefinition(gem.Effect);
        Assert.NotNull(definition);
        Assert.Equal("Metadata/Projectiles/VolcanicSlamMortar", definition!.Metadata);
        var stats = Game.SupportedSkill(conn, gem.Id).Stats;
        var merged = Game.MergeSkillStats(Game.CharacterPlayerStats(conn), stats);
        int Get(string name) => Program.Stats.Key(name) is uint k ? merged.GetValueOrDefault(k) : 0;
        var profile = definition.Parameters.Resolve(Get, 600, false);
        Assert.Equal(6, profile.Count);
        Assert.True(profile.Lobbed);
        Assert.False(profile.DirectDamage);
        Assert.Equal(-50, Get("active_skill_hit_ailment_damage_with_projectile_+%_final"));
        var damage = Game.SupportedHitFor(conn, gem.Id, suppress: false);
        Assert.True(damage.MinTotal > 0);
        Assert.Contains(GameServer.Core.World.DamageType.Fire, damage.Components.Select(c => c.Type));
    }
}
