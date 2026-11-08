using System.Net.Sockets;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;

static partial class Game
{
    /// <summary>The town portals standing for this connection (a Portal Scroll's, the Portal skill's, and the portal back
    /// to one) as 0x008f entries, each with the cosmetic's variation. The portals module fills this in: the game module
    /// cannot see its OpenPortals, and the entry's variation is the only thing a portal cosmetic changes.</summary>
    internal static Func<ClientState, List<InstanceMapDevice.Portal>>? TownPortals;

    // Lists the portals already standing again, so a portal cosmetic takes effect without reopening the map device: 0x008f
    // carries the variation of every entry and the client re-applies the visual. False when nothing stands.
    internal static async Task<bool> ResendMapPortals(string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn)
    {
        uint deviceId;
        lock (conn.HideoutDoodads) deviceId = conn.HideoutDoodads.Keys.FirstOrDefault();
        bool listed = false, mapPortals;
        lock (conn.MapPortals) mapPortals = conn.MapPortals.Count > 0;
        if (mapPortals)
        {
            await SendMapPortals(remote, stream, ciphers, conn, deviceId,
                MapDevice.OpenOf(CharacterName(conn))?.Area.Hash16 ?? conn.Layout.Hash);
            listed = true;
        }
        // A town portal belongs to no device: its list is the second message, complete for the portals it names.
        if (TownPortals?.Invoke(conn) is { Count: > 0 } town)
        {
            await Send(remote, stream, ciphers, InstanceMapDevice.Portals(0, town),
                $"0x008f {town.Count} standing town portal(s), variation {conn.CosmeticPortalVariation}");
            listed = true;
        }
        return listed;
    }

    // S→C 0x8f: the device's portals and where they lead (21-hideouts.md §5). Without it the client never draws them; a
    // portal listed with area 0 (one just taken) closes.
    internal static async Task SendMapPortals(string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn, uint deviceId, ushort area, uint closed = 0)
    {
        byte style = conn.MapPortalStyle;
        List<uint> ids;
        lock (conn.MapPortals) ids = [.. conn.MapPortals.Keys.Order()];
        string owner = CharacterName(conn);
        var list = (closed == 0 ? [] : new List<InstanceMapDevice.Portal> { new(closed, 0) })
            .Concat(ids.Select(id => new InstanceMapDevice.Portal(id, area, owner, style, conn.CosmeticPortalVariation))).ToList();
        await Send(remote, stream, ciphers, InstanceMapDevice.Portals(deviceId, list),
            $"0x008f map device 0x{deviceId:x} portals: {ids.Count} open to 0x{area:x4}{(closed == 0 ? "" : $", 0x{closed:x} closed")}");
    }
}
