using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

/// <summary>Dropped items roll their sockets: a count capped by the class and the item level, with links (ItemCatalog's
/// DefaultSockets). They used to come out as two linked sockets whatever the base (reported live 2026-11-10).</summary>
public class SocketDropTests
{
    /// <summary>The game's table: two sockets from the first item level, three from 2, four from 25, five from 35 and six
    /// from 50 (wiki Item socket, "Minimum item level").</summary>
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(24, 3)]
    [InlineData(25, 4)]
    [InlineData(34, 4)]
    [InlineData(35, 5)]
    [InlineData(49, 5)]
    [InlineData(50, 6)]
    [InlineData(80, 6)]
    public void The_item_level_ceiling_follows_the_game(int itemLevel, int sockets) =>
        Assert.Equal(sockets, CurrencyEffects.LevelSocketLimit(itemLevel));

    [Fact]
    public void A_drop_rolls_its_socket_count_instead_of_always_two()
    {
        var body = Program.Items.Find("Simple Robe")!;
        var random = new Random(20261110);
        var counts = new HashSet<int>();
        for (int i = 0; i < 200; i++)
        {
            var (sockets, links) = Program.Items.DefaultSockets(body, itemLevel: 30, random: random);
            Assert.InRange(sockets.Count, 1, 6);
            Assert.NotNull(links);
            Assert.Equal(sockets.Count, links!.Sum(group => (int)group));
            counts.Add(sockets.Count);
        }
        Assert.True(counts.Count > 1, "same base, same item level: the socket count must vary, not always be 2");
    }

    [Fact]
    public void A_drop_never_exceeds_its_item_level()
    {
        var body = Program.Items.Find("Simple Robe")!;
        var random = new Random(7);
        for (int level = 1; level <= 40; level++)
            for (int i = 0; i < 20; i++)
                Assert.True(Program.Items.DefaultSockets(body, itemLevel: level, random: random).Sockets.Count
                    <= CurrencyEffects.LevelSocketLimit(level), $"item level {level}");
    }

    [Fact]
    public void A_ring_has_no_sockets()
    {
        Assert.Empty(Program.Items.DefaultSockets(Program.Items.Find("Iron Ring")!, itemLevel: 80, random: new Random(1)).Sockets);
        Assert.Empty(Program.Items.DefaultSockets(Program.Items.Find("Chain Belt")!, itemLevel: 80, random: new Random(1)).Sockets);
    }
}
