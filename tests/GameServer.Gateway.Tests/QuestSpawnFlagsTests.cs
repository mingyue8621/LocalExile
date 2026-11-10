namespace GameServer.Gateway.Tests;

/// <summary>A quest object's boss is owed on entering an area again while the object stands used and no flag says the fight
/// is over (Quests.UsedFlags / Quests.FightOverFlags). Live 2026-11-11: the Sign went into Kitava's socket, the player
/// relogged, and nothing put him back, so a5q6 could never be finished.</summary>
public class QuestSpawnFlagsTests
{
    const string Area = "1_5_8", Socket = "Metadata/Terrain/Act5/Area8/Objects/ArenaSocket";
    const string Kitava = "Metadata/Monsters/KitavaBoss/Kitava1";

    static Quests.RuleSet Set(params Quests.Rule[] rules)
    {
        var quest = new Quests.Quest("test");
        foreach (var rule in rules) quest.Add(rule);
        return new Quests.RuleSet([quest], new Dictionary<Quests.Rule, string>());
    }

    /// <summary>Kitava never dies: the threshold at half life is what ends the fight, and nothing looser (seen, entered)
    /// may stand in for it, or a relog after using the Sign would skip the boss.</summary>
    [Fact]
    public void A_used_socket_owes_kitava_until_its_half_life_threshold()
    {
        var rules = Set(new Quests.UsedRule(Area, Socket, ["a5q6-DeliveredItem"], ["a5q6-HaveItem"]),
            new Quests.SeenRule(Area, Kitava, ["a5q6-SeenBoss"]),
            new Quests.ThresholdRule(Area, Kitava, 50, ["a5q6-CompletedBoss"]));
        Assert.Equal(["a5q6-DeliveredItem"], rules.UsedFlags(Area, Socket));
        Assert.Equal(["a5q6-CompletedBoss"], rules.FightOverFlags(Area, Kitava));
    }

    /// <summary>A killed boss ends its fight with every flag its kill rule sets, and a use that sets two flags counts as
    /// used when either is there (the Vaal Oversoul's altar).</summary>
    [Fact]
    public void A_killed_boss_ends_its_fight()
    {
        const string oversoul = "Metadata/Monsters/IncaShadowBoss/BossIncaShadow";
        var rules = Set(new Quests.UsedRule("1_2_14_3", "altar", ["a2q8-PlacedApex", "a2q8-AltarUsed"]),
            new Quests.KillRule("1_2_14_3", oversoul, ["a2q8-KilledShadowMonster", "FinishedVaalOversoulQuest"]));
        Assert.Contains("a2q8-AltarUsed", rules.UsedFlags("1_2_14_3", "altar"));
        Assert.Equal(["a2q8-KilledShadowMonster", "FinishedVaalOversoulQuest"], rules.FightOverFlags("1_2_14_3", oversoul));
    }

    /// <summary>Act 8's two are one fight (KilledAllRule): killing only one leaves neither flag set, so both come back.</summary>
    [Fact]
    public void A_multi_kill_rule_holds_both_of_its_monsters()
    {
        const string lunaris = "Metadata/Monsters/LunarisSolaris/Lunaris", solaris = "Metadata/Monsters/LunarisSolaris/Solaris";
        var rules = Set(new Quests.KilledAllRule("2_8_13", [lunaris, solaris], ["a8q23-KilledBosses"]));
        Assert.Equal(["a8q23-KilledBosses"], rules.FightOverFlags("2_8_13", lunaris));
        Assert.Equal(["a8q23-KilledBosses"], rules.FightOverFlags("2_8_13", solaris));
        // A monster no rule mentions is owed until something says otherwise: no flags at all.
        Assert.Empty(rules.FightOverFlags("2_8_13", "Metadata/Monsters/Someone/Else"));
    }
}
