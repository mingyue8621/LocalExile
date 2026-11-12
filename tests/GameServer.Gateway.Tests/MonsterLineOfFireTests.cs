namespace GameServer.Gateway.Tests;

/// <summary>A monster's shot, mortar or area spell must not land through a wall: the client's own projectile grid
/// (0 blocks, 1..5 pass) decides, and both ends of the line are excluded so a door frame neither end stands on does
/// not refuse the attack.</summary>
public sealed class MonsterLineOfFireTests
{
    static ClientState Conn(Func<int, int, int>? sight) =>
        new(new AreaLayout(0, "test", 128, 128, 32, 32, (_, _) => true, []) { Sight = sight }, 1, 1);

    [Fact]
    public void A_wall_between_the_two_cells_blocks_the_line()
    {
        var conn = Conn((x, _) => x == 50 ? 0 : 2);
        Assert.True(Game.LineOfFireBlocked(conn, (40, 32), (60, 32)));
    }

    [Fact]
    public void Nothing_between_them_lets_the_attack_through()
    {
        var conn = Conn((_, _) => 2);
        Assert.False(Game.LineOfFireBlocked(conn, (40, 32), (60, 32)));
        // A wall behind the player, or behind the monster, is not in the way.
        var wall = Conn((x, _) => x is 70 or 30 ? 0 : 2);
        Assert.False(Game.LineOfFireBlocked(wall, (40, 32), (60, 32)));
    }

    [Fact]
    public void The_cells_they_stand_on_are_not_a_wall()
    {
        // Both ends standing on a blocked cell (a door frame, a bridge rail) does not refuse the attack.
        var conn = Conn((x, _) => x is 40 or 60 or 50 ? 0 : 2);
        Assert.False(Game.LineOfFireBlocked(conn, (40, 32), (41, 32)));
        Assert.True(Game.LineOfFireBlocked(conn, (40, 32), (60, 32)));
    }

    [Fact]
    public void Adjacent_cells_and_one_cell_apart_never_meet_a_wall()
    {
        var conn = Conn((_, _) => 0);
        Assert.False(Game.LineOfFireBlocked(conn, (40, 32), (40, 32)));
        Assert.False(Game.LineOfFireBlocked(conn, (40, 32), (41, 32)));
    }

    [Fact]
    public void An_area_without_a_projectile_grid_lets_the_attack_through()
    {
        // No capture and no server-generated level: the layout has no grid, and refusing every attack would make the
        // monsters of that area harmless.
        Assert.False(Game.LineOfFireBlocked(Conn(null), (40, 32), (60, 32)));
    }
}
