using GameServer.Data.Terrain.Generation;

namespace GameServer.Gateway.Tests;

/// <summary>A gate into a boss arena is marked as such from the client's own room data (AreaExits.Resolve →
/// AreaTransitionObject.ToBossRoom): the arena is the room whose .arm names the boss ("mapboss1"), so a death inside it
/// makes the gate the checkpoint from the outside (live 2026-11-11: reviving inside the arena instead of at its door,
/// Shavronne's Tower 2_6_7_2, bossentry1.arm's ladder into boss1.arm).</summary>
public sealed class BossRoomGateTests
{
    static AreaExits.LayoutRoom Room(string path, int x, int y, int w, int h,
        AreaExits.RoomPoint[] points, AreaExits.Description[] descriptions) => new()
    {
        Room = new PlacedRoom(new RoomDef(path, "", new TileKey(), new RoomSlot[0, 0]), x, y, w, h, [], 0),
        Points = points,
        Descriptions = descriptions,
    };

    // bossentry1.arm: the ladder out of the arena, teleport7 to the arena's "telepad7"; boss1.arm: the arena itself, which
    // stands Shavronne and Brutus at "mapboss1"/"mapboss2".
    static (AreaExits.LayoutRoom Outside, AreaExits.LayoutRoom Arena) Tower() => (
        Room("Metadata/Terrain/Act6/Area7Level2/Rooms/Unique/bossentry1.arm", 100, 100, 10, 10,
            [new AreaExits.RoomPoint("teleport", 100 * 23 + 40, 100 * 23 + 40)],
            [new AreaExits.Description(ExitKind.Teleport, 7, true, null)]),
        Room("Metadata/Terrain/Act6/Area7Level2/Rooms/Unique/boss1.arm", 120, 100, 10, 10,
            [new AreaExits.RoomPoint("telepad7", 120 * 23 + 40, 100 * 23 + 40), new AreaExits.RoomPoint("mapboss1", 120 * 23 + 60, 100 * 23 + 60)],
            [new AreaExits.Description(ExitKind.Unused, 7, true, null, IsTelepad: true)]));

    [Fact]
    public void The_arena_is_the_room_whose_arm_names_the_boss()
    {
        var (outside, arena) = Tower();
        Assert.False(AreaExits.IsBossRoom(outside));
        Assert.True(AreaExits.IsBossRoom(arena));
    }

    [Fact]
    public void A_gate_into_the_arena_is_marked_and_the_one_back_out_is_not()
    {
        var (outside, arena) = Tower();
        var ladder = new PlacedTileObject("Metadata/MiscellaneousObjects/AreaTransition", "", 100 * 23 + 40, 100 * 23 + 40, 0, 0);
        var gate = Assert.Single(AreaExits.Resolve([outside, arena], [ladder]));
        Assert.Equal(ExitKind.Teleport, gate.Kind);
        // No slots are placed here, so the gate's target is the arena room's centre: inside the room that names the boss.
        Assert.Equal((120 * 23 + 115, 100 * 23 + 115), gate.Target!.Value);
        Assert.True(gate.ToBossRoom);
    }

    [Fact]
    public void A_gate_between_two_ordinary_rooms_is_not_a_boss_gate()
    {
        var (outside, arena) = Tower();
        var stairs = Room("Metadata/Terrain/Act6/Area7Level2/Rooms/Unique/corridor.arm", 140, 100, 10, 10,
            [new AreaExits.RoomPoint("telepad7", 140 * 23 + 40, 100 * 23 + 40)],
            [new AreaExits.Description(ExitKind.Unused, 7, true, null, IsTelepad: true)]);
        var ladder = new PlacedTileObject("Metadata/MiscellaneousObjects/AreaTransition", "", 100 * 23 + 40, 100 * 23 + 40, 0, 0);
        var gate = Assert.Single(AreaExits.Resolve([outside, stairs, arena], [ladder]));
        // The telepad sits in the corridor, whose points name no boss: the checkpoint stays the landing cell.
        Assert.False(gate.ToBossRoom);
    }
}
