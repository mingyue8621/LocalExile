using System.Net.Sockets;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;

// One ground per instance (InstanceGround): an item one player drops, or a monster drops for them, lies there for every
// client in the instance, and goes for all of them when someone picks it up. The client that put it down was sent it
// where that happened; the others get it here, within a sync (100 ms). Quest items and the start weapon stay the
// player's own (InstanceGround.Add shared: false).
static partial class Game
{
    internal static async Task SyncSharedGround(string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn)
    {
        if (!GameEvents.AllowsLoot(conn)) return;
        var ground = conn.Instance.Ground;
        await Send(remote, stream, ciphers, [], null, atGate: () =>
        {
            if (!CurrentGameWork || conn.Closed || conn.LeavingArea || !conn.PlayerSent) return null;
            var all = ground.All();
            var now = new HashSet<uint>(all.Select(e => e.Id));
            var packets = new List<byte[]>();
            // SeenGround is a plain set: the area's first send, this sync and a bot's loot request all touch it, so every
            // access takes its lock. Guarding only this one left the first send adding an id while this ran, and a lost id
            // is an item the client was shown and is never told to remove.
            lock (conn.SeenGround)
            {
                // Picked up by someone else (this client's own pickups have removed theirs already; a second remove of an
                // object it no longer has is ignored).
                foreach (uint gone in conn.SeenGround.Where(id => !now.Contains(id)).ToList())
                {
                    conn.SeenGround.Remove(gone);
                    packets.Add(InstanceProtocol.RemoveObject(gone));
                }
                foreach (var (id, item) in all)
                {
                    if (conn.SeenGround.Contains(id)) continue;
                    var (by, shared) = ground.Origin(id);
                    if (!shared && !ReferenceEquals(by, conn)) continue; // another player's own
                    conn.SeenGround.Add(id);
                    if (ReferenceEquals(by, conn)) continue; // shown where it was put down
                    packets.Add(InstanceEntities.CreateObject(id, InstanceEntities.ObjectTypeHash(WorldItemObject.MetadataPath),
                        item.Blob(justDropped: ground.IsNewFor(conn, id))));
                    packets.Add(InstanceProtocol.EnterWakeZone(id));
                }
            }
            return packets.Count == 0 ? null : packets.SelectMany(p => p).ToArray();
        });
    }

    /// <summary>Whether this client has been shown that ground item. SeenGround is a plain set touched by the area's first
    /// send, the sync above and a bot's loot request, all on different threads, so a read takes its lock too.</summary>
    internal static bool SeenOnGround(ClientState conn, uint id)
    {
        lock (conn.SeenGround) return conn.SeenGround.Contains(id);
    }
}
