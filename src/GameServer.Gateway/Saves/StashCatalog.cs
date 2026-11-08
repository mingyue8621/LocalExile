/// <summary>
/// Where a town's Stash stands (Agents/docs/protocol/18-stash.md §1). The client files do not place it (Stash.ot has only
/// its model; the town tiles' .tmd records hold the waypoint and doodads), so the cells are ours, as for the NPCs. A
/// hideout's comes from its decoration (HideoutCatalog).
/// </summary>
static class StashCatalog
{
    public sealed record Placement(int X, int Y, float Rotation);

    static readonly Dictionary<ushort, Placement> Towns = new()
    {
        // Lioneye's Watch (1_1_town): its room's own chest_v01_01.ao at room 134 313, angle 1.853, through the
        // room's placement (beach_town_unique_normal.arm, R270, origin 0 0).
        [AreaLayouts.LioneyesWatch] = new(313, 279, 0.281924f),
        // The Azurite Mine Encampment (Delve_Hub): placed live with /stash_place 355 295 180 (2026-10-30). It also gets
        // the guild stash (Program.SpawnTownStash), at TownGuildStashes.
        [DelveHub] = new(355, 295, MathF.PI),
        // The Sarn Encampment (1_3_town): chest_v01_01.ao at room 224 353, angle 0.406 (city_town.arm, I,
        // origin 69 69).
        [SarnEncampment] = new(293, 422, 0.405527f),
        // The Forest Encampment (1_2_town): the room's own Stash doodad (chest_v01_01.ao, room cell 186 191, angle 1.130),
        // turned into the area grid with the room's placement (R270, origin 0 92).
        [ForestEncampment] = new(191, 296, 5.842389f),
        // Highgate (1_4_town): its own chest_v01_01.ao at room 238 201, angle 0.225, through the room's placement
        // (R270, origin 0 0). Highgate's room is the same file in Act 9 with other doodad cells.
        [Highgate] = new(201, 520, 4.937389f),
        // The Overseer's Tower (1_5_town): chest_v01_01.ao at room 200 199, angle -1.102 (I, origin 138 138).
        [OverseersTower] = new(338, 337, 5.181185f),
        // The Bridge Encampment (2_7_town): chest_v01_01.ao at room 164 425, angle -0.446 (I, origin 437 207).
        [BridgeEncampment] = new(601, 632, 5.837185f),
        // Act 8's Sarn Encampment (2_8_town): chest_v01_01.ao at room 225 353, angle 0.398 (I, origin 69 69).
        [SarnEncampmentAct8] = new(294, 422, 0.398f),
        // Act 9's Highgate (2_9_town): chest_v01_01.ao at room 237 199, angle 0.360 (R270, origin 0 0).
        [HighgateAct9] = new(199, 521, 5.072389f),
        // The Oriath Docks (2_10_town): chest_v01_01.ao at room 532 296, angle -0.800 (I, origin 0 0).
        [OriathDocks] = new(532, 296, 5.483185f),
        // Act 6 Lioneye's Watch (2_6_town): the same lines as Act 1's room, in part2_beach_town.arm (552 wide,
        // R270, origin 0 0).
        [LioneyesWatchAct6] = new(313, 417, 0.281924f),
        // The Menagerie (Menagerie_Hub): placed live with /stash_place 455 480 0 (2026-10-30).
        [MenagerieHub] = new(455, 480, 0),
        // The Forbidden Sanctum (SanctumFoyer_Fellshrine): by the waypoint (126, 149), to be placed live with /stash_place.
        [SanctumHub] = new(116, 160, 0),
        // Kingsmarch: next to the arrival (an offset, FromArrival), to be placed live with /stash_place.
        [Kingsmarch] = new(6, 6, 0),
        // The Rogue Harbour: next to the arrival (an offset, FromArrival), to be placed live with /stash_place.
        [HeistHub] = new(6, 8, 0),
        // The Monastery of the Keepers: next to the arrival (an offset, FromArrival), to be placed live with /stash_place.
        [ChayulaHub] = new(6, 8, 0),
    };

    /// <summary>A town's guild stash where it has a place of its own (else next to the stash). The campaign towns take
    /// their room's own guildstash_v01_01.ao line, as the stash does; the Mine Encampment 344 295 180 and the
    /// Menagerie 440 480 0 have no such line and were placed live with /guild_stash_place (2026-10-30).</summary>
    public static Placement? GuildIn(ushort areaHash) => areaHash switch
    {
        DelveHub => new(344, 295, MathF.PI),
        MenagerieHub => new(440, 480, 0),
        SanctumHub => new(106, 166, 0),
        Kingsmarch => new(-6, 6, 0),
        HeistHub => new(-6, 8, 0),
        ChayulaHub => new(-6, 8, 0),
        AreaLayouts.LioneyesWatch => new(324, 278, 6.022399f),
        SarnEncampment => new(303, 428, 0.749202f),
        // Its own guild stash doodad in the room (guildstash_v01_01.ao, room cell 180 206, angle 2.296).
        ForestEncampment => new(206, 302, 0.725204f),
        // The other campaign towns: each room's own guildstash_v01_01.ao line, through the same placement.
        Highgate => new(217, 484, 3.940389f),
        OverseersTower => new(336, 347, 4.760185f),
        BridgeEncampment => new(610, 622, 5.300185f),
        SarnEncampmentAct8 => new(303, 428, 0.749f),
        HighgateAct9 => new(217, 484, 3.940389f),
        OriathDocks => new(542, 276, 3.141185f),
        LioneyesWatchAct6 => new(324, 416, 6.022399f),
        _ => null,
    };

    /// <summary>HASH16 of Act 6 Lioneye's Watch (WorldAreas 2_6_town).</summary>
    public const ushort LioneyesWatchAct6 = 0xb0fb;

    /// <summary>HASH16 of the Sarn Encampment (WorldAreas 1_3_town).</summary>
    public const ushort SarnEncampment = 0x0431;

    /// <summary>HASH16 of the Forest Encampment, Act 2's town (WorldAreas 1_2_town).</summary>
    public const ushort ForestEncampment = 0x2233;

    /// <summary>HASH16 of Highgate, Act 4's town (WorldAreas 1_4_town).</summary>
    public const ushort Highgate = 0xb63e;

    /// <summary>HASH16 of the Overseer's Tower, Act 5's town (WorldAreas 1_5_town).</summary>
    public const ushort OverseersTower = 0xbeab;

    /// <summary>HASH16 of the Bridge Encampment, Act 7's town (WorldAreas 2_7_town).</summary>
    public const ushort BridgeEncampment = 0x2a6d;

    /// <summary>HASH16 of Act 8's Sarn Encampment (WorldAreas 2_8_town).</summary>
    public const ushort SarnEncampmentAct8 = 0x0334;

    /// <summary>HASH16 of Act 9's Highgate (WorldAreas 2_9_town).</summary>
    public const ushort HighgateAct9 = 0x23f3;

    /// <summary>HASH16 of the Oriath Docks, Act 10's town (WorldAreas 2_10_town).</summary>
    public const ushort OriathDocks = 0x95b0;

    /// <summary>HASH16 of the Azurite Mine Encampment (WorldAreas Delve_Hub).</summary>
    public const ushort DelveHub = 0x8b9b;

    /// <summary>HASH16 of the Menagerie (WorldAreas Menagerie_Hub).</summary>
    public const ushort MenagerieHub = 0xf3f4;

    /// <summary>HASH16 of the Forbidden Sanctum (WorldAreas SanctumFoyer_Fellshrine).</summary>
    public const ushort SanctumHub = 0xe217;

    /// <summary>HASH16 of Kingsmarch (WorldAreas KalguuranSettlersLeague).</summary>
    public const ushort Kingsmarch = 0x8f53;

    /// <summary>Areas whose stash, guild stash and NPC places are offsets from the arrival cell: not placed live yet.</summary>
    public static bool FromArrival(ushort areaHash) => areaHash is Kingsmarch or HeistHub or ChayulaHub;

    /// <summary>HASH16 of the Monastery of the Keepers (WorldAreas ChayulaLeague).</summary>
    public const ushort ChayulaHub = 0xcb44;

    /// <summary>HASH16 of the Rogue Harbour (WorldAreas HeistHub).</summary>
    public const ushort HeistHub = 0xb5be;

    public static Placement? In(ushort areaHash) =>
        Towns.GetValueOrDefault(areaHash)
        ?? (HideoutCatalog.ByArea(areaHash)?.First(GameServer.Core.Protocol.Instance.StashObject.MetadataPath) is { } d ? new(d.X, d.Y, d.Rotation) : null);
}
