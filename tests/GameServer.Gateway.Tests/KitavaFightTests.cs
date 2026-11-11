using Xunit;

namespace GameServer.Gateway.Tests;

// The data behind Kitava's official fight (Act 5, The Cathedral Rooftop): his second form and the heart his chest opens
// for must be creatable as monsters, or the fight falls back to the first form alone and the player never sees a heart.
public sealed class KitavaFightTests
{
    const string First = "Metadata/Monsters/KitavaBoss/Kitava1";
    const string Second = "Metadata/Monsters/KitavaBoss/Kitava2";
    const string Heart = "Metadata/Monsters/KitavaBoss/KitavaHeart";

    [Theory]
    [InlineData(First)]
    [InlineData(Second)]
    [InlineData(Heart)]
    public void The_two_forms_and_the_heart_can_be_created(string id)
    {
        var variety = Game.Monsters.Find(id);
        Assert.NotNull(variety);
        Assert.Equal(id, variety!.Id);
        Assert.True(variety.Supported, $"{id}: components {string.Join(", ", variety.ComponentsOrBase)}");
    }

    /// <summary>Kitava2's BaseMonsterTypeIndex is Kitava1 and it carries the same 7 200 % life: the bar goes on from where
    /// the first form stood, which is why the fight hands it the first form's maximum life.</summary>
    [Fact]
    public void The_second_form_is_the_first_forms_own_form()
    {
        var first = Game.Monsters.Find(First)!;
        var second = Game.Monsters.Find(Second)!;
        Assert.Equal(First, second.BaseType);
        Assert.Equal(first.LifeMultiplier, second.LifeMultiplier);
        Assert.True(second.BossHealthBar);
    }

    /// <summary>KitavaHeart is NoAi and immobile with no boss bar of its own (3 150 % life, its own MonsterTypes row): it
    /// stands still for the player to break while Kitava's chest is open.</summary>
    [Fact]
    public void The_heart_stands_still_and_carries_no_boss_bar()
    {
        var heart = Game.Monsters.Find(Heart)!;
        Assert.False(heart.BossHealthBar);
        Assert.NotEqual(First, heart.BaseType);
    }

    /// <summary>The phase changes are basic actions of the client's ActionTypes table; without them the server cannot play
    /// phase1_transition_out, phase2_transition_in or the chest opening (Kitava2.act's stance2 and phase2_beamhit).</summary>
    [Theory]
    [InlineData("Emerge")]
    [InlineData("Emerge2")]
    [InlineData("ChangeToStance1")]
    [InlineData("ChangeToStance2")]
    public void The_phase_change_actions_are_in_the_table(string action) =>
        Assert.NotNull(Game.Emerges.ActionSkill(action));
}
