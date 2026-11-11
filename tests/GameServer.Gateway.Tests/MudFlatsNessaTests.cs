using System.Text.Json;

namespace GameServer.Gateway.Tests;

public class MudFlatsNessaTests
{
    // The Mud Flats' Nessa treads water in the client's own data: create_untransitioned plays water_idle (the only type in
    // object-types.json that does), so on our dry ground she draws with her body under the floor. The stand-in must be the
    // same model with the same components, or the client would draw a different person.
    [Fact]
    public void The_dry_ground_stand_in_is_the_same_model_with_the_same_components()
    {
        const string MudFlats = "Metadata/NPC/Act6/NessaMudFlats";
        string twin = Game.WireTypeOf(MudFlats);
        Assert.NotEqual(MudFlats, twin);

        using var doc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine("data", "object-types.json")));
        JsonElement? Of(string type)
        {
            foreach (var r in doc.RootElement.GetProperty("types").EnumerateArray())
                if (r.GetProperty("type").GetString() == type) return r;
            return null;
        }

        var water = Of(MudFlats);
        var rock = Of(twin);
        Assert.NotNull(water);
        Assert.NotNull(rock);
        Assert.Contains("water_idle", water!.Value.GetProperty("client_transitionable").ToString());
        Assert.DoesNotContain("water_idle", rock!.Value.GetProperty("client_transitionable").ToString());
        Assert.Equal(water.Value.GetProperty("animated_object").GetString(), rock.Value.GetProperty("animated_object").GetString());
        Assert.Equal(water.Value.GetProperty("components").ToString(), rock.Value.GetProperty("components").ToString());
    }
}
