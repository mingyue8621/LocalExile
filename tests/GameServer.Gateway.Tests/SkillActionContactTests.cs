namespace GameServer.Gateway.Tests;

// The client's own skill actions (ACT): HeavyStrike plays bash1_mace even though the skill's GrantedEffects Id spells
// "heavystrike" and the rig has no such clip, and GlacialHammer plays three segments (attack_maul_01a with a 0.6 s
// budget, 01b with 0.8, then bash1_mace, whose ice effect it carries). The hit is the first segment that strikes
// (01a at 0.499 s), not the end of the chain; the sequence is compressed into a shorter attack time.
public sealed class SkillActionContactTests
{
    const string Json = """
            { "Metadata/Characters/Str/Str.ao": {
                "animations": {
                  "attack1_maul": [866, 599], "bash1_mace": [1166, 799], "whirl_sword": [800, 400],
                  "attack_maul_01a": [566, 499], "attack_maul_01b": [434, 358], "groundslam_maul": [1000, 633] },
                "defaultAttack": {
                  "TwoHandMace": [ { "animation": "attack1_maul", "source": "attack1_maul", "index": 0, "begin": 0,
                                     "length": 866, "contact": 599, "cancel": null, "slow": null, "budget": null,
                                     "weight": 1, "item": false, "pair": false, "position": null } ] },
                "skillActions": {
                  "@root": { "parent": null, "actions": {} },
                  "TwoHandMace": { "parent": "@root", "actions": {
                    "HeavyStrike": { "segments": [ { "animation": "bash1_mace", "source": "bash1_mace", "index": 1,
                        "begin": 0, "length": 1166, "contact": 799, "cancel": null, "slow": [100, 611],
                        "budget": null, "item": false, "pair": false, "position": null } ], "effects": [] },
                    "GlacialHammer": { "segments": [
                      { "animation": "attack_maul_01a", "source": "attack_maul_01a", "index": 2, "begin": 0,
                        "length": 566, "contact": 499, "cancel": null, "slow": [235, 358], "budget": 0.6,
                        "item": false, "pair": false, "position": null },
                      { "animation": "attack_maul_01b", "source": "attack_maul_01b", "index": 3, "begin": 0,
                        "length": 434, "contact": 358, "cancel": null, "slow": null, "budget": 0.8,
                        "item": false, "pair": false, "position": null },
                      { "animation": "bash1_mace", "source": "bash1_mace", "index": 1, "begin": 0,
                        "length": 1166, "contact": 799, "cancel": null, "slow": [100, 611], "budget": null,
                        "item": false, "pair": false, "position": null } ],
                      "effects": ["Metadata/Effects/Spells/icehammer_geo/rig.ao"] } } },
                  "OneHandSword": { "parent": "@root", "actions": {
                    "HeavyStrike": { "segments": [ { "animation": "bash1_mace", "source": "bash1_mace", "index": 1,
                        "begin": 0, "length": 1166, "contact": 799, "cancel": null, "slow": [100, 646],
                        "budget": null, "item": false, "pair": false, "position": null } ], "effects": [] },
                    "Cyclone": { "segments": [ { "animation": "whirl_sword", "source": "whirl_sword", "index": 4,
                        "begin": 0, "length": 800, "contact": 400, "cancel": null, "slow": null,
                        "budget": null, "item": false, "pair": false, "position": null } ], "effects": [] } } },
                  "OneHandSwordShield": { "parent": "OneHandSword", "actions": {} } } } }
        """;

    const string Str = "Metadata/Characters/Str/Str.ao";
    static WeaponProfile Maul => new(Shield: false, DualWield: false, Staff: false, Bow: false, DualClaws: false,
        TwoHand: true, Melee: true, TwoHandMelee: true, Mace: true);
    static WeaponProfile SwordAndShield => new(Shield: true, DualWield: false, Staff: false, Bow: false, DualClaws: false,
        OneHand: true, Melee: true, OneHandMelee: true, Sword: true);

    [Fact]
    public void A_skill_action_hits_on_its_first_striking_segment_scaled_into_the_attack()
    {
        var contacts = AnimationContactsTables.From(Json);
        // bash1_mace is 1166 ms with its contact at 799; a 0.6 s attack compresses the clip, so the hit is at 0.799 of it.
        Assert.Equal(0.799 * 0.6 / 1.166, contacts.SkillActionContactSeconds(Str, Maul, "HeavyStrike", 0.6)!.Value, 6);
        // An attack longer than the clip plays it through: 799 ms of a 1166 ms clip, then nothing to stretch.
        Assert.Equal(0.799, contacts.SkillActionContactSeconds(Str, Maul, "HeavyStrike", 2)!.Value, 6);
        // GlacialHammer winds up (attack_maul_01a, contact 499 of 566 ms, 0.6 s budget), then swings the ice hammer;
        // the first segment that strikes is the swing the player sees land. An attack as long as its budget plays it whole.
        Assert.Equal(0.499, contacts.SkillActionContactSeconds(Str, Maul, "GlacialHammer", 0.6)!.Value, 6);
        Assert.Equal(0.499, contacts.SkillActionContactSeconds(Str, Maul, "GlacialHammer", 0.75)!.Value, 6);
        // A faster attack compresses that segment into it.
        Assert.Equal(0.2495, contacts.SkillActionContactSeconds(Str, Maul, "GlacialHammer", 0.3)!.Value, 6);
    }

    [Fact]
    public void A_stance_inherits_its_parents_actions_and_an_unknown_action_answers_null()
    {
        var contacts = AnimationContactsTables.From(Json);
        // OneHandSwordShield declares nothing of its own: the client plays OneHandSword's bash1_mace.
        double shielded = contacts.SkillActionContactSeconds(Str, SwordAndShield, "HeavyStrike", 0.6)!.Value;
        Assert.Equal(0.799 * 0.6 / 1.166, shielded, 6);
        Assert.Contains("HeavyStrike", contacts.SkillActionNames(Str, SwordAndShield));
        // A variant Id plays the base action the ACT declares: CycloneChannelled is Cyclone's whirl_sword.
        Assert.Equal(0.4 * 0.6 / 0.8, contacts.SkillActionContactSeconds(Str, SwordAndShield, "CycloneChannelled", 0.6)!.Value, 6);
        Assert.Null(contacts.SkillActionContactSeconds(Str, SwordAndShield, "CycloneBogus", 0.6));
        // A skill the ACT sends to the default attack, and one the rig has no data for: null keeps the caller's fallback.
        Assert.Null(contacts.SkillActionContactSeconds(Str, Maul, "GroundSlam", 0.6));
        Assert.Null(contacts.SkillActionContactSeconds("Metadata/None.ao", Maul, "HeavyStrike", 0.6));
        Assert.Null(contacts.SkillActionContactSeconds(Str, Maul, null, 0.6));
    }
}
