/// <summary>
/// The Vaal auras (Agents/docs/protocol/11-skills.md §10): a Vaal skill's use spends its souls and puts its buff on the
/// player for the skill's own <c>base_skill_effect_duration</c>, then the buff goes. They reserve nothing and have no
/// toggle: the next cast needs another Vaal soul charge. The buff rows are the client's, read from BuffDefinitions:
/// <list type="bullet">
/// <item>Vaal Clarity 158 vaal_aura_no_mana_cost (no_mana_cost), Vaal Grace 159 vaal_aura_dodge (chance_to_evade_attacks_%),</item>
/// <item>Vaal Discipline 160 vaal_aura_energy_shield (base_maximum_energy_shield), Vaal Haste 161 vaal_aura_speed,</item>
/// <item>the three Impurities 971/972/973 (fire_/cold_/lightning_impurity_buff: the maximum resistance and its ailment
/// immunity), Vaal Arctic Armour 2175 vaal_arctic_armour, Vaal Righteous Fire 1160 vaal_righteous_fire_duration.</item>
/// </list>
/// The auras' own stats come from the skill's EffectStats, as every other aura's do (AuraCatalog.RawStats), so no
/// per-skill value is written here.
/// </summary>
public static class VaalAuraCatalog
{
    public sealed record Definition(ushort BuffRow);

    public static readonly IReadOnlyDictionary<string, Definition> All = new Dictionary<string, Definition>
    {
        ["VaalClarity"] = new(158),             // vaal_aura_no_mana_cost
        ["VaalGrace"] = new(159),               // vaal_aura_dodge
        ["VaalDiscipline"] = new(160),          // vaal_aura_energy_shield
        ["VaalHaste"] = new(161),               // vaal_aura_speed
        ["VaalImpurityOfFire"] = new(971),      // fire_impurity_buff
        ["VaalImpurityOfIce"] = new(972),       // cold_impurity_buff
        ["VaalImpurityOfLightning"] = new(973), // lightning_impurity_buff
        ["VaalArcticArmour"] = new(2175),       // vaal_arctic_armour
        ["VaalRighteousFire"] = new(1160),      // vaal_righteous_fire_duration
    };

    public static Definition? Supported(string? effectId) => effectId is null ? null : All.GetValueOrDefault(effectId);

    /// <summary>Whether a running aura is a Vaal aura: it ends on its own instead of waiting to be switched off.</summary>
    public static bool IsVaal(string effectId) => All.ContainsKey(effectId);
}
