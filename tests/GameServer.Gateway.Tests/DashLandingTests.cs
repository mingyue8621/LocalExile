namespace GameServer.Gateway.Tests;

public sealed class DashLandingTests
{
    static AreaLayout Terrain(Func<int, int, bool> floor, Func<int, int, int>? sight) =>
        new AreaLayout(0, "test", 128, 128, 32, 32, floor, []) { Sight = sight };

    [Fact]
    public void Dash_crosses_a_visible_gap_in_both_directions_and_scales_its_range()
    {
        var layout = Terrain((x, y) => x < 40 || x >= 55, (_, _) => 2);
        var barriers = new HashSet<(int, int)>();
        Assert.Equal((62, 32), Game.DashLanding(layout, barriers, (32, 32), (58, 32), 0, false));
        Assert.Equal((32, 32), Game.DashLanding(layout, barriers, (62, 32), (36, 32), 0, false));
        Assert.Equal((32, 32), Game.DashLanding(layout, barriers, (62, 32), (70, 32), 0, true));
        Assert.Equal((77, 32), Game.DashLanding(layout, barriers, (32, 32), (58, 32), 50, false));
    }

    [Fact]
    public void Dash_respects_sight_barriers_and_the_last_standable_fallback()
    {
        var barriers = new HashSet<(int, int)>();
        var layout = Terrain((x, _) => x != 62 && x != 52, (x, _) => x == 54 ? 1 : 2);
        Assert.Equal((53, 32), Game.DashLanding(layout, barriers, (32, 32), (62, 32), 0, false));
        layout = Terrain((_, _) => true, (x, _) => x == 54 ? 1 : 2);
        Assert.Equal((62, 32), Game.DashLanding(layout, barriers, (32, 32), (62, 32), 0, false));
        layout = layout with { Sight = (x, _) => x == 55 ? 0 : 2 };
        Assert.Equal((54, 32), Game.DashLanding(layout, barriers, (32, 32), (62, 32), 0, false));
        barriers.Add((48, 32));
        Assert.Equal((47, 32), Game.DashLanding(layout, barriers, (32, 32), (62, 32), 0, false));
        layout = Terrain((x, _) => x != 40, null);
        Assert.Equal((39, 32), Game.DashLanding(layout, barriers, (32, 32), (62, 32), 0, false));
    }

    [Fact]
    public void Dash_from_a_cell_the_projectile_grid_blocks_still_moves()
    {
        // A player standing on a door frame or a bridge rail: the client's grid marks that cell as blocking for
        // projectiles. Judging the starting cell returned it as the landing, so the dash went nowhere on the server and
        // the client's own move was undone by the next resync (live 2026-11-12).
        var barriers = new HashSet<(int, int)>();
        var layout = Terrain((_, _) => true, (x, _) => x == 32 ? 0 : 2);
        var landing = Game.DashLanding(layout, barriers, (32, 32), (60, 32), 0, false);
        Assert.NotEqual((32, 32), landing);
        Assert.True(landing.X > 32);
    }

    [Fact]
    public void Dash_from_a_cell_another_body_stands_on_still_moves()
    {
        // The same with the blocking coming from a barrier (a monster in the way) rather than the terrain grid.
        var barriers = new HashSet<(int, int)> { (32, 32) };
        var layout = Terrain((_, _) => true, (_, _) => 2);
        var landing = Game.DashLanding(layout, barriers, (32, 32), (60, 32), 0, false);
        Assert.NotEqual((32, 32), landing);
        Assert.True(landing.X > 32);
    }
}
