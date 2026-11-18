/// <summary>
/// The skill_art_variation a skill draws at with no skill effect worn; a worn effect's own value adds to it.
/// Holy Sweep's art (SkillArtVariations <c>sweep</c>: Default, NoEffect, Holy, Zana, Sirius, Abyssal) keeps the old
/// Sweep's variations at 0 and 1, and its hammers (<c>sweep_hammer_fall</c>) have no row there: the base hammer
/// (HolySweepHammerFall) is Holy (2), and the effects' values (Originator's 1, Awakener's 2, Abyssal 3) are the Zana (3),
/// Sirius (4) and Abyssal (5) hammers above it. Each hit's 0x1af makes the client draw the hammer, and at 0 it crashed
/// ("failed trying to access data from index 3 of skill art data (sweep:0, )": art slot 3 at variation 0).
/// </summary>
public static class SkillArtBase
{
    // GrantedEffects Ids of the player gems: Holy Sweep, Holy Sweep of Hammerfalls and the unused SweepAltY.
    static readonly Dictionary<string, int> Bases = new(StringComparer.Ordinal)
    {
        ["Sweep"] = 2, ["SweepAltX"] = 2, ["SweepAltY"] = 2,
    };

    /// <summary>The skill_art_variation the skill sends: its base plus the worn effects' (0 when none is worn).
    /// <paramref name="own"/> is the skill's own GrantedEffects value (<c>SkillCatalog.ArtVariation</c>), which every
    /// skill with a variant carries (Arc 10, Firestorm 30, Leap Slam 19); the table above only covers the Sweep gems
    /// that have no value of their own. <paramref name="variants"/> is the fewest variants the skill's SkillArtVariations
    /// row defines: the value indexes that list, so one at or past it (molten_shell 8 of 8, tornado 2 of 2) is dropped
    /// rather than read as a slot the row does not have. A negative count means the skill has no art row at all.</summary>
    public static int Variation(string? effectId, int worn, int own = 0, int variants = 0)
    {
        int baseValue = own > 0 ? own : effectId is not null && Bases.TryGetValue(effectId, out int b) ? b : 0;
        int total = baseValue + worn;
        if (total <= 0 || variants < 0) return 0;
        return variants == 0 || total < variants ? total : 0;
    }
}
