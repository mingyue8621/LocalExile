namespace GameServer.Gateway.Tests;

public class MonsterAttackPayloadTests
{
    // The Alpine Devil's first usable granted effect is GAGoatmanLeapSlamDamage, the hit its leap lands with: its action
    // is GeometryAttack and Goatman2.act has no such name (Melee, LeapSlam, DodgeForward, Taunt only), so the client
    // logged "PlayAnimation failed! [type: GroundSlam]" and the goat stood still (live 2026-11-10). Its
    // MeleeAtAnimationSpeed row is the swing every actor has and takes the payload's place.
    [Fact]
    public void Alpine_devil_swings_the_melee_row_not_the_ga_payload()
    {
        var skills = new SkillCatalog();
        var goat = new MonsterCatalog().Find("Metadata/Monsters/Goatman/MountainGoatmanLeapSlam_");
        Assert.NotNull(goat);

        int effect = Game.MavenNucleus.CombatEffect(skills, goat!);

        Assert.StartsWith("MeleeAtAnimationSpeed", skills.EffectId(effect));
        Assert.True(skills.IsMelee(effect));
    }

    // A phase that offers no melee swing at all keeps its payload rather than dropping the attack: this goat's only
    // usable row is the damage one.
    [Fact]
    public void A_variety_with_only_a_payload_keeps_it()
    {
        var skills = new SkillCatalog();
        var goat = new MonsterCatalog().Find("Metadata/Monsters/Goatman/GoatmanLightningLeapSlamExtra");
        Assert.NotNull(goat);

        int effect = Game.MavenNucleus.CombatEffect(skills, goat!);

        Assert.Equal("GAGoatmanLeapSlamDamage", skills.EffectId(effect));
    }
}
