using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

/// <summary>A gem goes into a socket of its own colour only (3.x): a white socket takes any colour and a white gem any
/// socket. Until 2026-11-15 the socket check asked only whether the held item was a gem at all, so any gem fitted any
/// coloured socket.</summary>
public sealed class SocketColourTests
{
    [Fact]
    public void A_gem_only_fits_a_socket_of_its_own_colour()
    {
        var items = Program.Items;
        var host = ItemCatalog.Create(items.Find("Driftwood Sceptre")!) with
        {
            Sockets = [new(SocketColour.Red), new(SocketColour.Blue), new(SocketColour.White)],
            Links = [3],
        };
        var gem = ItemCatalog.Create(items.Find("Fireball")!);
        var path = Path.Combine(Path.GetTempPath(), $"socket-colour-{Guid.NewGuid():N}.db");
        const string name = "SocketColour";
        Saves.Open(path);
        try
        {
            SocketColour? Held(Item _) => SocketColour.Blue;
            var inv = PlayerInventories.For(name, items);
            inv.Put(PlayerInventories.MainInventory, new(81, 0, 0, host));
            uint next = 82;
            // Holds a fresh copy of the gem: a socketed one leaves the cursor empty.
            void Hold() => inv.Put(PlayerInventories.Cursor, new(next++, 0, 0, gem));

            // A blue gem: the red socket refuses it, the blue one takes it.
            Hold();
            Assert.Null(inv.Socket(PlayerInventories.MainInventory, 81, 0, Game.ShapeOf, inv.NewItemId, out var redWhy, Held));
            Assert.Contains("Red socket", redWhy);
            Assert.NotNull(inv.Socket(PlayerInventories.MainInventory, 81, 1, Game.ShapeOf, inv.NewItemId, out _, Held));

            // A white socket takes the same blue gem.
            Hold();
            Assert.NotNull(inv.Socket(PlayerInventories.MainInventory, 81, 2, Game.ShapeOf, inv.NewItemId, out _, Held));

            // A white gem takes a coloured socket.
            Hold();
            Assert.NotNull(inv.Socket(PlayerInventories.MainInventory, 81, 0, Game.ShapeOf, inv.NewItemId, out _,
                _ => SocketColour.White));

            // A gem the tables give no colour for keeps the old behaviour: no check.
            Hold();
            Assert.NotNull(inv.Socket(PlayerInventories.MainInventory, 81, 1, Game.ShapeOf, inv.NewItemId, out _, _ => null));
        }
        finally { PlayerInventories.Forget(name); TestEnvironment.ReleaseSaves(path); }
    }
}
