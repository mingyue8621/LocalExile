// The buff rows whose native behaviour reads two words from the buff's counted list without checking its length:
// Tempest Shield (0x1417a9410: art variation, hide-user-buff flag) and Temporal Rift (0x141f83180/0x141f832b0: sample
// ring size, interval in ms). An empty list is an access violation, so every path that sends one of these rows carries
// the words — the caster's own aura, the aura a peer shares, and a monster's aura or self buff, which has no skin.
public sealed class BuffBehaviourDataTests
{
    static readonly uint[] Zero = [0u, 0u];

    [Fact]
    public void Tempest_shield_is_row_112_and_its_base_form_sends_two_zeros()
    {
        Assert.Equal(112, AuraCatalog.TempestShieldRow);
        Assert.Equal(AuraCatalog.TempestShieldRow, AuraCatalog.BuffRow("TempestShield"));
        Assert.Equal(Zero, AuraCatalog.BehaviourData("TempestShield")!);
        // A monster holds only the row: the row lookup gives the same base pair.
        Assert.Equal(Zero, AuraCatalog.BehaviourDataOfRow(AuraCatalog.TempestShieldRow)!);
    }

    [Fact]
    public void Tempest_shield_carries_the_casters_skin_and_hide_flag()
    {
        Assert.Equal(new uint[] { 3u, 1u }, AuraCatalog.BehaviourData("TempestShield", 3, hideVisual: true)!);
        // A skin without the hide flag keeps the art variation and sends a zero flag.
        Assert.Equal(new uint[] { 3u, 0u }, AuraCatalog.BehaviourData("TempestShield", 3)!);
        // A negative variation (an art family the skin table does not know) falls back to the base form.
        Assert.Equal(Zero, AuraCatalog.BehaviourData("TempestShield", -1)!);
    }

    [Fact]
    public void Temporal_rift_keeps_the_servers_sampling_pair()
    {
        Assert.Equal(1915, AuraCatalog.TemporalRiftRow);
        Assert.Equal(AuraCatalog.TemporalRiftRow, AuraCatalog.BuffRow("TemporalRift"));
        Assert.Equal(new uint[] { 42u, 100u }, AuraCatalog.BehaviourData("TemporalRift")!);
        Assert.Equal(new uint[] { 42u, 100u }, AuraCatalog.BehaviourDataOfRow(AuraCatalog.TemporalRiftRow)!);
    }

    [Fact]
    public void Every_other_row_reads_nothing()
    {
        Assert.Null(AuraCatalog.BehaviourData("Haste"));
        Assert.Null(AuraCatalog.BehaviourDataOfRow(AuraCatalog.BuffRow("Haste")));
        Assert.Null(AuraCatalog.BehaviourDataOfRow(0));
    }

    [Fact]
    public void Both_buffs_of_a_tempest_shield_aura_carry_the_pair()
    {
        var aura = new AuraCatalog.Aura(7, Program.Skills.EffectRow("TempestShield"), "TempestShield", "Tempest Shield",
            100, AuraCatalog.TempestShieldRow, 0, 25, Level: 20, SourceBuff: 101,
            ArtVariation: 2, HideVisual: true);
        var buffs = AuraCatalog.Buffs(aura, 42, null).ToList();
        // The source art and the received HUD buff: both instantiate the behaviour, so both carry the words.
        Assert.Equal(2, buffs.Count);
        Assert.All(buffs, buff => Assert.Equal(new uint[] { 2u, 1u }, buff.List!.ToArray()));
    }

    // The client has one Tempest Shield skill (ActiveSkills 217) and one buff row for it; a monster that carries a
    // variant effect must still resolve to that row, or it draws no shield at all.
    [Theory]
    [InlineData("MonsterTempestShield")]
    [InlineData("WbTempestShield")]
    [InlineData("PerandusTempestShield")]
    [InlineData("TempestShieldMercenary")]
    [InlineData("TempestShieldMercenaryEncounter")]
    public void Every_tempest_shield_variant_resolves_to_the_one_buff_row(string effectId)
    {
        Assert.Equal("TempestShield", MonsterSkills.Family(effectId));
        Assert.Equal(AuraCatalog.TempestShieldRow, AuraCatalog.BuffRow(MonsterSkills.Family(effectId)));
    }
}
