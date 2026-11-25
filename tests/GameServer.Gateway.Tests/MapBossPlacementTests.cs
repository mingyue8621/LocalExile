using GameServer.Core.Protocol.Instance;
using GameServer.Data.Terrain.Generation;
using MonsterRarity = GameServer.Core.Protocol.Instance.MonsterRarity;

namespace GameServer.Gateway.Tests;

/// <summary>A map's unique boss stands on the point the client's rooms mark for it, never at the map's entrance
/// (live 2026-11-25: Jungle Valley's spider queen spawned beside the arrival). The rooms express the point in four
/// ways: "mapboss", a league leader point (Warbands' mapbosswarband&lt;element&gt;, Rogue Exiles' mapbossexilepack),
/// the 3.29 "ritual_boss_spawn_override", and a boss spawner object (MapBossSpawnerSimplified, the uber arenas'
/// BossSpawner). A league point holds one leader even though WorldAreas lists the camp's whole roster.</summary>
public sealed class MapBossPlacementTests
{
    [GameFilesFact]
    public void Map_bosses_stand_on_their_room_point_not_at_the_arrival()
    {
        // A map, the seed the client generated it with, and the point its boss must take: number of unique spawns, and
        // either the pack name (the room point) or null for "the furthest cell, the arena bosses with no point at all".
        (string Area, uint Seed, string? Point)[] cases =
        [
            ("MapWorldsJungleValley", 2072690396, "mapboss"),
            ("MapWorldsAcidLakes", 1, "mapbosswarbandchaos2"),
            ("MapWorldsPrecinct", 1, "mapbossexilepack"),
            ("MapWorldsGraveTrough", 1, "ritual_boss_spawn_override"),
            ("MapWorldsShipyard", 1, "mapbosswarbandlightning"),
            ("MapWorldsCourtyardUnique", 1, null),
        ];
        var catalog = new PackCatalog("data");
        foreach (var (area, seed, point) in cases)
        {
            var packs = Assert.IsType<PackCatalog.AreaPacks>(catalog.For(area));
            var level = Assert.IsType<ServerLevels.Level>(ServerLevels.For(area, seed));
            int w = level.Width, h = level.Height;
            bool Walkable(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && level.Walk[y * w + x] >= 2;
            var notes = new List<string>();
            var points = new List<AreaPopulation.RoomPoint>(AreaPopulation.FitRooms(area, seed, notes, "data").Points);
            var (extra, _) = AreaPopulation.LayoutRoomPoints(level.Records);
            foreach (var ep in extra)
                if (!points.Any(p => p.Name == ep.Name && Math.Abs(p.X - ep.X) <= 3 && Math.Abs(p.Y - ep.Y) <= 3))
                    points.Add(ep);
            var arrival = (X: level.Start?.X ?? w / 2, Y: level.Start?.Y ?? h / 2);
            var spawns = AreaPopulation.Plan(packs, w, h, Walkable, arrival,
                [.. level.Exits.Select(e => (e.Object.X, e.Object.Y))], points,
                AreaPopulation.PopulationSeed((ushort)0, seed), _ => true, notes,
                unassignedAtArrival: ServerLevels.IsMapArea(area));
            var bosses = spawns.Where(s => s.Rarity == MonsterRarity.Unique).ToList();
            Assert.Single(bosses);
            var boss = bosses[0];
            Assert.True(Math.Abs(boss.X - arrival.X) + Math.Abs(boss.Y - arrival.Y) > 300,
                $"{area} seed {seed}: the boss stands at ({boss.X},{boss.Y}), {Math.Abs(boss.X - arrival.X) + Math.Abs(boss.Y - arrival.Y)} cells from the arrival ({arrival.X},{arrival.Y})");
            if (point is not null) Assert.Equal(point, boss.Pack);
            Assert.True(Walkable(boss.X, boss.Y), $"{area} seed {seed}: the boss at ({boss.X},{boss.Y}) is off the walkable grid");
        }
    }

    /// <summary>A league point takes the roster's leader, not every candidate: Acid Lakes lists nine WbChaos leaders as
    /// the map's bosses and one of them stands on the map.</summary>
    [GameFilesFact]
    public void A_league_leader_point_holds_one_leader_of_the_roster()
    {
        var packs = Assert.IsType<PackCatalog.AreaPacks>(new PackCatalog("data").For("MapWorldsAcidLakes"));
        Assert.True(packs.Bosses.Count > 3);
        var level = Assert.IsType<ServerLevels.Level>(ServerLevels.For("MapWorldsAcidLakes", 1));
        int w = level.Width, h = level.Height;
        bool Walkable(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && level.Walk[y * w + x] >= 2;
        var (extra, _) = AreaPopulation.LayoutRoomPoints(level.Records);
        var leaguePoint = Assert.Single(extra, p => p.Name.StartsWith("mapbosswarband", StringComparison.Ordinal));
        var spawns = AreaPopulation.Plan(packs, w, h, Walkable, (level.Start?.X ?? w / 2, level.Start?.Y ?? h / 2),
            [], [leaguePoint], AreaPopulation.PopulationSeed((ushort)0, 1), _ => true, [],
            unassignedAtArrival: true);
        Assert.Single(spawns, s => s.Rarity == MonsterRarity.Unique);
    }
}
