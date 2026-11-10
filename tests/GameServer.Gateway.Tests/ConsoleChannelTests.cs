namespace GameServer.Gateway.Tests;

/// <summary>The launcher's console channel (ConsoleChannel.cs): the requests it refuses, and the listing it gives with
/// nobody in game. A command that needs a character is a live test (Agents/docs/dev/console-channel.md).</summary>
public class ConsoleChannelTests
{
    [Fact]
    public async Task Who_lists_nothing_when_nobody_is_in_game()
    {
        var answer = await ConsoleChannel.AnswerAsync("""{"op":"who"}""");
        Assert.True(answer["ok"]!.GetValue<bool>());
        Assert.Empty(answer["players"]!.AsArray());
    }

    [Fact]
    public async Task Run_without_a_character_says_so()
    {
        var answer = await ConsoleChannel.AnswerAsync("""{"op":"run","line":"/item Iron Ring"}""");
        Assert.False(answer["ok"]!.GetValue<bool>());
        Assert.Equal("no_player", answer["error"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("""{"op":"run","line":"hello"}""", "not_a_command")]
    [InlineData("""{"op":"chat","line":"/item Iron Ring"}""", "unknown_op")]
    [InlineData("nonsense", "bad_request")]
    public async Task A_request_it_cannot_run_is_an_error(string request, string error)
    {
        var answer = await ConsoleChannel.AnswerAsync(request);
        Assert.False(answer["ok"]!.GetValue<bool>());
        Assert.Equal(error, answer["error"]!.GetValue<string>());
    }
}
