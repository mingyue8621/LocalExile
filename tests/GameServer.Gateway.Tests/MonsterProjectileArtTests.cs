namespace GameServer.Gateway.Tests;

// A Spine Serpent (Metadata/Monsters/Snake/SnakeScorpion) fights with SnakeSpineProjectile, whose
// monster_projectile_variation names MonsterProjectileAttack row 2 — Projectiles row 74, Metadata/Projectiles/SnakeSpine.
// Its ActiveSkills row is the generic monster_projectile_weapon shared by every armed monster, so the server used to
// fall through to SharedProjectile and hand it the Atlas basilisk snake's AtlasBasiliskSnakeProjectile instead; the
// client's release of that art crashed the game at offset 0x17a945f (The Marketplace, 1_3_5).
public class MonsterProjectileArtTests
{
    const string SnakeSpine = "Metadata/Projectiles/SnakeSpine";
    const string AtlasSnake = "Metadata/Projectiles/AtlasBasiliskSnakeProjectile";
    static readonly int SnakeSpineProjectileEffect = 1636;

    [GameFilesFact]
    public void A_weapon_attack_reads_its_own_projectile_row_and_lets_the_server_draw_it()
    {
        var projectile = new SkillCatalog().MonsterProjectile(SnakeSpineProjectileEffect, includeWeaponAttacks: true);
        Assert.NotNull(projectile);
        Assert.Equal(SnakeSpine, projectile!.Metadata);
        // Only a monster_mortar_attack release is drawn by the client; anything else must be the server's object.
        Assert.Equal(0, projectile.Variation);
    }

    [GameFilesFact]
    public void A_weapon_attacks_shot_is_its_own_art_not_another_monsters_on_the_shared_action_row()
    {
        var shot = Assert.IsType<SkillCatalog.MonsterProjectileInfo>(BossKits.ArtShot(new SkillCatalog(), SnakeSpineProjectileEffect, 1));
        Assert.Equal(SnakeSpine, shot.Metadata);
        Assert.NotEqual(AtlasSnake, shot.Metadata);
    }
}
