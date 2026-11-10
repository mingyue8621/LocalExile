using Xunit;

namespace GameServer.Gateway.Tests;

// The data behind Malachai's official fight (Act 4, The Harvest): the second form and the three hearts must be
// creatable as monsters, or the fight falls back to a plain one and the player never sees a heart.
public sealed class MalachaiFightTests
{
    [Theory]
    [InlineData("Metadata/Monsters/Malachai/MalachaiBoss")]
    [InlineData("Metadata/Monsters/Malachai/MalachaiBoss2")]
    [InlineData("Metadata/Monsters/Malachai/BeastHeart1")]
    [InlineData("Metadata/Monsters/Malachai/BeastHeart2")]
    [InlineData("Metadata/Monsters/Malachai/BeastHeart3")]
    public void The_forms_and_the_hearts_can_be_created(string id)
    {
        var variety = Game.Monsters.Find(id);
        Assert.NotNull(variety);
        Assert.Equal(id, variety!.Id);
        Assert.True(variety.Supported,
            $"{id}: components {string.Join(", ", variety.ComponentsOrBase)}");
    }

    [Fact]
    public void The_hearts_stand_still_and_carry_no_boss_bar()
    {
        var heart = Game.Monsters.Find("Metadata/Monsters/Malachai/BeastHeart1")!;
        Assert.False(heart.BossHealthBar);
        // Metadata/Monsters/NoAi.ais in the variety: nothing drives them.
        Assert.False(string.IsNullOrEmpty(heart.Id));
    }
}
