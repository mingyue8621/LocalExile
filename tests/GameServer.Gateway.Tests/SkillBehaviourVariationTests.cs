namespace GameServer.Gateway.Tests;

// A skill's skill_behaviour_variation is its GrantedEffects SkillID: the client takes its GeometryAttack row (and that
// row's GeometryChannel) from it, and the network record has to carry it because the client runs no such calculator.
public sealed class SkillBehaviourVariationTests
{
    [Fact]
    public void Player_skills_send_the_behaviour_row_they_name()
    {
        var skills = new SkillCatalog();
        // The variants that carry one name it; the default rows name 0 and send none.
        Assert.Equal(1, skills.BehaviourVariation(skills.EffectRow("IceNova")));
        Assert.Equal(7, skills.BehaviourVariation(skills.EffectRow("ArcAltY")));
        Assert.Equal(2, skills.BehaviourVariation(skills.EffectRow("LeapSlamAltX")));
        Assert.Equal(0, skills.BehaviourVariation(skills.EffectRow("Arc")));
        Assert.Equal(0, skills.BehaviourVariation(skills.EffectRow("HeavyStrike")));
    }
}
