using System.Runtime.CompilerServices;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using GameServer.Core.World;

static partial class Game
{
    // ---- Malachai's death portal (Act 4) ----
    // The Black Core's fight is two stages on either side of a portal: Malachai falls, the portal his body leaves
    // (Metadata/QuestObjects/Act4/MalachaiDeathPortal, an AreaTransition in the official area) opens, and the second stage
    // waits in the depths behind it (the area's mapboss2 point, room boss_2.arm in the client's data — not fitted in this
    // server, so the portal leads to that point itself). The engine used to spawn the second form on top of the corpse the
    // instant the first died, which read as "no second phase" (live 2026-11-10).
    // The depths' three hearts (BeastHeart1/2/3) and their invulnerable stages are still not modelled: the second form
    // fights normally once the player steps through.

    const string MalachaiDeathPortal = "Metadata/QuestObjects/Act4/MalachaiDeathPortal";
    // The portal's own rig (the .ot's Animated animated_object): an AreaTransition object drawn as Malachai's death portal.
    const string PortalRig = "Metadata/Effects/Spells/monsters_effects/act4/Malachai/death_portal/rig.ao";
    const string MalachaiBoss = "Metadata/Monsters/Malachai/MalachaiBoss";
    const string MalachaiBoss2 = "Metadata/Monsters/Malachai/MalachaiBoss2";

    internal sealed record MalachaiPortalFight(uint Id, string Next, (int X, int Y) At, (int X, int Y) To, byte Level);

    static readonly ConditionalWeakTable<ClientState, Dictionary<uint, MalachaiPortalFight>> MalachaiPortals = new();

    /// <summary>The variety whose fall opens the portal, and nothing for the others.</summary>
    internal static bool OpensPortal(string varietyId) => varietyId == MalachaiBoss;

    /// <summary>The portal at the first form's body: its object in the area, and where it leads. Returns false when the
    /// object type has no serializable components, so the caller can fall back to spawning the form in place.</summary>
    internal static async Task<bool> OpenMalachaiPortal(string remote, System.Net.Sockets.NetworkStream stream,
        ConnectionCiphers ciphers, ClientState conn, (int X, int Y) at, string next, byte level)
    {
        var to = conn.LaterFormPoints.GetValueOrDefault(next, at);
        // The depths' room is not fitted in this server: where its boss point is not walkable, the portal leads to the
        // near side of where the first form fell rather than into a wall.
        if (to == at || !conn.Layout.WalkableFor(to.X, to.Y, 2)) to = (at.X + 4, at.Y);
        uint id = conn.NextObjectId++;
        // An AreaTransition object with the portal's rig: the layout's own gate blob, which the client draws and a click
        // addresses; the walk-through is this module's (UseMalachaiPortal), not the area's transition table.
        var portal = new AreaTransitionObject(at.X, at.Y, conn.Layout.Hash, Model: PortalRig) { TeleportTo = to };
        var pending = MalachaiPortals.GetOrCreateValue(conn);
        lock (pending) pending[id] = new(id, next, at, to, level);
        await Send(remote, stream, ciphers, InstanceEntities.CreateObject(id, InstanceEntities.ObjectTypeHash(portal.Type), portal.Blob()),
            $"0x02bb Malachai's death portal 0x{id:x} at ({at.X}, {at.Y}) -> ({to.X}, {to.Y})");
        await Send(remote, stream, ciphers, InstanceProtocol.EnterWakeZone(id), "0x01a9 death portal enters the wake zone");
        return true;
    }

    /// <summary>C→S 0x24 on the portal: step through and the second form is waiting on the other side. False when this is
    /// not the portal (another object's use).</summary>
    internal static async Task<bool> UseMalachaiPortal(ClientState conn, InstanceTransfer.UseSkillOnObject use, double requestedAt)
    {
        if (!MalachaiPortals.TryGetValue(conn, out var pending)) return false;
        MalachaiPortalFight fight;
        lock (pending) if (!pending.TryGetValue(use.TargetId, out fight!)) return false;
        if (!TryBeginObjectWalk(conn, requestedAt, fight.At.X, fight.At.Y, 3, out var walk)) return true;
        var (action, now, here, stop, arrive) = walk;
        var (remote, stream, ciphers) = (conn.Wire.Remote, conn.Wire.Stream, conn.Wire.Ciphers);
        if (!use.Started)
            await Send(remote, stream, ciphers,
                InstanceProtocol.ActionStarted(PlayerId, here.X, here.Y, fight.At.X, fight.At.Y, use.SkillId, use.Sequence, use.Flags,
                    targetObjectId: use.TargetId),
                $"0x01ab player steps into Malachai's portal 0x{use.TargetId:x}",
                still: () => PlayerActionCurrent(conn, action));
        if (arrive > now) await Task.Delay(TimeSpan.FromSeconds(arrive - now));
        if (!PlayerActionCurrent(conn, action)) return true;
        if (!PlayerWalk.InReach(stop, fight.At, 3, conn.Layout.Walkable)) return true;
        lock (pending) pending.Remove(use.TargetId);
        // Through: the player stands at the depths' own boss point and the second form is raised there, on the connection
        // that runs the instance's monsters (SharedMonsters), so it acts instead of standing where it appeared.
        double at = conn.Clock.Elapsed.TotalSeconds;
        using (ProjectileMutation(conn))
        {
            Interlocked.Increment(ref conn.ActionCount);
            conn.Walk.Teleport(fight.To.X, fight.To.Y, at);
        }
        await Send(remote, stream, ciphers, InstanceProtocol.Teleport(PlayerId, fight.To.X, fight.To.Y),
            $"0x01b7 player through Malachai's portal to ({fight.To.X}, {fight.To.Y})");
        await Send(remote, stream, ciphers, InstanceProtocol.RemoveObject(use.TargetId), "0x02bd death portal closes behind the player");
        await RaiseSecondForm(conn, fight);
        return true;
    }

    /// <summary>The second form at the depths' point, aggressive at once so it takes the fight to the player.</summary>
    static async Task RaiseSecondForm(ClientState conn, MalachaiPortalFight fight)
    {
        if (Monsters.Find(fight.Next) is not { Supported: true } form || form.Id != fight.Next) return;
        var owner = conn.Instance?.World.Runner is { Closed: false } runner && !ReferenceEquals(runner, conn) ? runner : conn;
        if (owner.Wire.Remote is null) return;
        uint id = await SpawnMonster(owner.Wire.Remote, owner.Wire.Stream, owner.Wire.Ciphers, owner, form, fight.Level,
            MonsterRarity.Unique, fight.To.X, fight.To.Y, log: true);
        lock (owner.Monsters)
            if (owner.Monsters.TryGetValue(id, out var boss))
            {
                boss.Aggro = true;
                boss.LastTarget = null;
                Console.WriteLine($"[gateway] {owner.Wire.Remote} {boss.Label} rises through Malachai's portal at " +
                    $"({fight.To.X}, {fight.To.Y}): life {boss.Life}/{boss.MaxLife}, attack {(boss.Attack is null ? "none" : "ready")}");
            }
    }

    internal static void LeavingMalachaiPortal(ClientState conn)
    {
        MalachaiPortals.Remove(conn);
    }
}
