namespace GameServer.Gateway.Tests;

// Holy Sweep's hammers exist only from SkillArtVariations sweep's Holy variation (2): the skill sends 2 with no effect
// worn, and a worn effect's value counts from it (Originator's 1 → Zana 3); other skills send their own GrantedEffects
// value (Arc 10 of its 14 variants), and one that fills its variant list (8 of 8) is dropped.
public sealed class SkillArtBaseTests
{
    [Fact]
    public void Holy_sweep_counts_its_art_variation_from_holy()
    {
        Assert.Equal(2, SkillArtBase.Variation("Sweep", 0));
        Assert.Equal(3, SkillArtBase.Variation("Sweep", 1));
        Assert.Equal(5, SkillArtBase.Variation("SweepAltX", 3));
        Assert.Equal(0, SkillArtBase.Variation("VaalSweep", 0));
        Assert.Equal(7, SkillArtBase.Variation("RighteousFire", 7));
        Assert.Equal(0, SkillArtBase.Variation(null, 0));
    }

    [Fact]
    public void A_skill_sends_its_own_art_variation_while_its_row_has_that_variant()
    {
        var skills = new SkillCatalog();
        int arc = skills.EffectRow("Arc");
        Assert.Equal(10, skills.ArtVariation(arc));
        Assert.Equal(14, skills.ArtVariants(arc));
        Assert.Equal(10, SkillArtBase.Variation("Arc", 0, skills.ArtVariation(arc), skills.ArtVariants(arc)));
        // A worn effect counts from it, and one that reaches past the list is dropped rather than read.
        Assert.Equal(11, SkillArtBase.Variation("Arc", 1, skills.ArtVariation(arc), skills.ArtVariants(arc)));
        Assert.Equal(0, SkillArtBase.Variation("Arc", 4, skills.ArtVariation(arc), skills.ArtVariants(arc)));
        int molten = skills.EffectRow("GraftSkillXophMoltenShell");
        Assert.Equal((8, 8), (skills.ArtVariation(molten), skills.ArtVariants(molten)));
        Assert.Equal(0, SkillArtBase.Variation("GraftSkillXophMoltenShell", 0, skills.ArtVariation(molten), skills.ArtVariants(molten)));
        // A skill with no art row has no variant to send; the client draws its fixed art.
        int nova = skills.EffectRow("IceNova");
        Assert.Equal(-1, skills.ArtVariants(nova));
        Assert.Equal(0, SkillArtBase.Variation("IceNova", 0, skills.ArtVariation(nova), skills.ArtVariants(nova)));
    }
}
