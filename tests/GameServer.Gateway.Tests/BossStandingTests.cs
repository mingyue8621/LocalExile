namespace GameServer.Gateway.Tests;

/// <summary>Where a boss an area leaves out stands when a quest brings it (AreaPopulation.BossStanding): the rooms' own
/// "mapboss" point for that WorldAreas Bosses row, not the object that woke it. Kitava came out at the Sign of Purity's
/// socket, 100 cells short of where he rises (reported live 2026-11-10).</summary>
public class BossStandingTests
{
    static AreaPopulation.RoomPoint At(string name, int x, int y) => new(name, x, y, 0);

    /// <summary>The Cathedral Rooftop: bossentry.arm has force:mapboss and the area lists one boss (Kitava1, 5747).
    /// Its ArenaSocket (the cross the Sign goes into) stands at (218, 558) in the room, the boss point at (217, 658).</summary>
    [Fact]
    public void The_single_boss_takes_the_unnumbered_point()
    {
        var points = new[] { At("force:mapboss", 1735, 3142), At("default", 505, 218) };
        Assert.Equal((1735, 3142), AreaPopulation.BossStanding(points, [5747], 5747));
    }

    /// <summary>The Harbour Bridge: bridge_arena.arm has mapboss1 and mapboss2 for Lunaris (5642) and Solaris (5666),
    /// and the fight starter they both hang off stands between them.</summary>
    [Fact]
    public void Two_bosses_take_their_numbered_points()
    {
        var points = new[] { At("mapboss1", 262, 468), At("mapboss2", 367, 482) };
        Assert.Equal((262, 468), AreaPopulation.BossStanding(points, [5642, 5666], 5642));
        Assert.Equal((367, 482), AreaPopulation.BossStanding(points, [5642, 5666], 5666));
    }

    /// <summary>A monster the area does not list as its boss, a numbered point that names another boss, and two bosses
    /// with only an unnumbered point: none of them may take a point (the object's own cell stays).</summary>
    [Fact]
    public void Only_the_areas_own_boss_with_a_point_for_it_takes_one()
    {
        var kitava = new[] { At("force:mapboss", 1735, 3142) };
        Assert.Null(AreaPopulation.BossStanding(kitava, [5747], 9999));
        Assert.Null(AreaPopulation.BossStanding(kitava, [], 5747));
        var bridge = new[] { At("mapboss1", 262, 468), At("mapboss2", 367, 482) };
        Assert.Null(AreaPopulation.BossStanding(bridge, [5642, 5666], 5747));
        Assert.Null(AreaPopulation.BossStanding(new[] { At("force:mapboss", 4, 5) }, [5642, 5666], 5666));
    }

    /// <summary>Map points (mapboss1 without a variety list of its own, the map's own bosses) and a point that takes
    /// several ("mapboss1&2") still answer for the boss they name.</summary>
    [Fact]
    public void A_point_naming_several_bosses_answers_for_each()
    {
        var points = new[] { At("mapboss1&2", 100, 200) };
        Assert.Equal((100, 200), AreaPopulation.BossStanding(points, [11, 22], 11));
        Assert.Equal((100, 200), AreaPopulation.BossStanding(points, [11, 22], 22));
    }
}
