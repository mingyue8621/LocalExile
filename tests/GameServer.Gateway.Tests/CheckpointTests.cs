using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

/// <summary>"Resurrect at Checkpoint" (ClientState.Checkpoint, Player.Resurrect): the last gate of the area the player came
/// through is the checkpoint, so a death in a boss arena revives at its door rather than at the area's entrance (live
/// 2026-11-11: Shavronne's Tower, every act's boss sent the player back to the first floor).</summary>
public sealed class CheckpointTests
{
    // Shavronne's Tower, one of its seeds: the entrance at (617, 568) and the arena's ladder landing at (2472, 444).
    static AreaLayout Tower() => new(0xb62a, "2_6_7_2", 3933, 1173, 617, 568, (_, _) => true, [])
    {
        Waypoint = new WaypointObject(600, 590, Active: 1),
    };

    [Fact]
    public void A_gate_of_this_area_is_the_checkpoint()
    {
        var layout = Tower();
        Assert.Equal((2472, 444), layout.Checkpoint(waypointActivated: false, (0xb62a, 2472, 444)));
        // Even when the area's waypoint is activated: the gate is the checkpoint the player touched last.
        Assert.Equal((2472, 444), layout.Checkpoint(waypointActivated: true, (0xb62a, 2472, 444)));
    }

    [Fact]
    public void A_gate_of_another_area_is_ignored()
    {
        var layout = Tower();
        Assert.Equal((617, 568), layout.Checkpoint(waypointActivated: false, (0x1234, 2472, 444)));
        Assert.Equal(layout.Near((600, 590)), layout.Checkpoint(waypointActivated: true, (0x1234, 2472, 444)));
    }

    [Fact]
    public void Without_a_gate_the_activated_waypoint_then_the_arrival_is_the_checkpoint()
    {
        var layout = Tower();
        Assert.Equal((617, 568), layout.Checkpoint(waypointActivated: false));
        Assert.Equal(layout.Near((600, 590)), layout.Checkpoint(waypointActivated: true));
    }
}
