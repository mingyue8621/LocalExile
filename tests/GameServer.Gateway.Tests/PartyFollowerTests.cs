using System.Net;
using System.Net.Sockets;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using GameServer.Core.World;
using GameServer.Modding;
using MonsterRarity = GameServer.Core.Protocol.Instance.MonsterRarity;

namespace GameServer.Gateway.Tests;

public sealed class PartyFollowerTests
{
    [Fact]
    public async Task Shared_deaths_reward_nearby_players_and_hide_each_observers_bar_once()
    {
        await PartyExperienceChecks.Verify();
        using var runtime = new Runtime();
        using var firstWire = new Wire();
        using var sleepingWire = new Wire();
        var instance = new InstanceState();
        var first = runtime.Connection(instance, firstWire);
        var sleeping = runtime.Connection(instance, sleepingWire);
        instance.World.Runner = first;
        const uint id = 0x08000000;
        var variety = Game.Monsters.All.First();
        var monster = new SpawnedMonster("shared victim", new PlayerWalk(110, 100, 30), 60, 8, 1000)
            { Life = 0, Dead = true };
        instance.World.Monsters[id] = monster;
        instance.World.MonsterOrigins[id] = new(InstanceEntities.ObjectTypeHash(variety.Id),
            new MonsterObject(110, 100, 1000, variety.ComponentsOrBase, variety.AnimatedObject),
            (ushort)variety.Row, 1, MonsterRarity.Normal);
        // Also repair views whose life was already mirrored to zero before this module loaded.
        first.MonsterViews[id] = new((110, 100), (110, 100), 0);
        sleeping.MonsterViews[id] = new((110, 100), (110, 100), 1000);
        sleeping.AsleepMonsters.Add(id);
        string hide = Convert.ToHexString(InstanceEntities.StatsUpdate(id, [(StatKeys.HideMiniLifeBar, 1)]));
        string zero = Convert.ToHexString(InstanceProtocol.LifeUpdate(id, 0));
        int Count(Wire wire, string hex) => Program.HistoryFor(wire.Ciphers).Lines().Count(line => line.Contains(hex, StringComparison.Ordinal));
        await Game.SyncInstanceMonsters("observer", firstWire.Stream, firstWire.Ciphers, first);
        Assert.Equal(1, Count(firstWire, hide));
        Assert.Equal(1, Count(firstWire, zero));
        await Game.SyncInstanceMonsters("observer", firstWire.Stream, firstWire.Ciphers, first);
        Assert.Equal(1, Count(firstWire, hide));
        await Game.SyncInstanceMonsters("sleeping", sleepingWire.Stream, sleepingWire.Ciphers, sleeping);
        Assert.Equal(0, Count(sleepingWire, hide));
        sleeping.AsleepMonsters.Remove(id);
        await Game.SyncInstanceMonsters("awake", sleepingWire.Stream, sleepingWire.Ciphers, sleeping);
        Assert.Equal(1, Count(sleepingWire, hide));
        Assert.Equal(0u, sleeping.MonsterViews[id].Life);
        monster.Dead = false; monster.Life = 500;
        await Game.SyncInstanceMonsters("revived", firstWire.Stream, firstWire.Ciphers, first);
        Assert.Equal(1, Count(firstWire, Convert.ToHexString(InstanceEntities.StatsUpdate(id, [(StatKeys.HideMiniLifeBar, 0)]))));
    }

    [Fact]
    public void Followers_clear_the_entrance_remain_calm_and_balance_attacking_with_catching_up()
    {
        ControlledPlayerBuildChecks.VerifyCampaignBuilds();
        using var runtime = new Runtime();
        using var wire = new Wire();
        var instance = new InstanceState();
        var leader = runtime.Connection(instance, wire);
        var bot = runtime.Connection(instance, wire);
        leader.Walk.Teleport(100, 100, 0);
        leader.Walk.Order(180, 100, 0);
        var ahead = Game.ControlledFollowerPoint(bot, leader, arrival: true);
        // Behind the leader, off the line it is walking down: a follower used to take a cell ahead of it, and five of them
        // made a wall the player's own pathfinder had to walk round (live 2026-11-10).
        Assert.True(ahead.X < 95);
        Assert.InRange(Math.Sqrt(Math.Pow(ahead.X - 100, 2) + Math.Pow(ahead.Y - 100, 2)), 6, 20);
        var runner = runtime.Connection(instance, wire);
        runner.Walk.Teleport(201, 100, 0);
        runner.ControlledNextAction = 1000;
        Assert.True(Game.KeepControlledFollowerNear(runner, leader, 0));
        // A gap over the leash is closed on foot first: the order is a walk back toward the trail, not a jump beside the player.
        Assert.Equal((201, 100), runner.Walk.Position(0));
        Assert.True(Game.Dist(runner.Walk.Destination, (100, 100)) < 32);
        Assert.Equal(1000, runner.ControlledNextAction);
        // Too far to walk back at all, it comes back at once, and behind the leader.
        var stranded = runtime.Connection(instance, wire);
        stranded.Walk.Teleport(300, 300, 0);
        Assert.True(Game.KeepControlledFollowerNear(stranded, leader, 0));
        Assert.True(Game.Dist(stranded.Walk.Position(0), (100, 100)) < 32);
        Assert.Equal(.35, stranded.ControlledNextAction);
        var trailer = runtime.Connection(instance, wire);
        trailer.Walk.Teleport(180, 100, 0);
        int action = trailer.ActionCount;
        Assert.True(Game.KeepControlledFollowerNear(trailer, leader, 0));
        Assert.Equal((180, 100), trailer.Walk.Position(0)); // Normal catch-up walks, rather than teleports.
        Assert.True(trailer.ActionCount > action);
        Assert.True(trailer.Walk.Destination.X < 100);   // Back onto the trail behind the leader, not further ahead of it.
        Assert.True(Game.Dist(trailer.Walk.Destination, (100, 100)) < 32);
        // A nearby swing/cast gets its contact before following resumes, rather than being cancelled every AI tick.
        trailer.Walk.Teleport(70, 100, 0);
        trailer.Walk.Order(70, 100, 0);
        Game.HoldControlledFollowerAttack(trailer, 0, .75, 123);
        action = trailer.ActionCount;
        Assert.True(Game.KeepControlledFollowerNear(trailer, leader, .2));
        Assert.Equal(action, trailer.ActionCount);
        Assert.Equal((70, 100), trailer.Walk.Destination);
        Assert.True(Game.KeepControlledFollowerNear(trailer, leader, 1));
        Assert.True(trailer.ActionCount > action);
        Assert.True(Game.ControlledFollowAttackFits(20, 37, .75, ahead: -20));
        Assert.True(Game.ControlledFollowAttackFits(30, 40, .75, ahead: 30));
        Assert.True(Game.ControlledFollowAttackFits(60, 37, .75));   // fights from further out now that it does at 80
        Assert.False(Game.ControlledFollowAttackFits(70, 0, .1));    // but not from so far that the player is gone
        Assert.False(Game.ControlledFollowAttackFits(50, 70, 1));
        Assert.False(Game.ControlledFollowAttackFits(0, 37, 3));
        var inFront = runtime.Connection(instance, wire);
        inFront.Walk.Teleport(137, 100, 0);
        Assert.False(Game.KeepControlledFollowerNear(inFront, leader, 0));
        // Ahead of the leader on its own route it steps aside and waits there, rather than standing in the corridor.
        Assert.True(inFront.ActionCount > 0);
        Assert.True(Math.Abs(inFront.Walk.Destination.Y - 100) >= 5);

        leader.Walk.Teleport(100, 100, 0);
        var idle = runtime.Connection(instance, wire);
        idle.Walk.Teleport(160, 100, 0);
        action = idle.ActionCount;
        int adjustments = 0;
        for (int tick = 0; tick <= 1200; tick++)
        {
            double now = tick / 10.0; // Two minutes of simulated AI, without a real-time wait.
            Assert.False(Game.KeepControlledFollowerNear(idle, leader, now),
                $"idle at {now}: follower {idle.Walk.Position(now)}, leader {leader.Walk.Position(now)}, goal {leader.Walk.Destination}");
            var before = idle.Walk.Position(now);
            Game.IdleControlledFollower(idle, leader, now);
            if (idle.ActionCount != action)
            {
                adjustments++;
                Assert.InRange(Game.Dist(before, idle.Walk.Destination), 2, 8);
                Assert.True(Game.Dist(idle.Walk.Destination, (100, 100)) <= 60);
                action = idle.ActionCount;
            }
            if (now < 14) Assert.Equal(0, adjustments);
        }
        Assert.InRange(adjustments, 1, 3);
        foreach (int gap in new[] { 0, 3, 55, 60 })
        {
            var standing = runtime.Connection(instance, wire);
            standing.Walk.Teleport(100 + gap, 100, 0);
            for (int tick = 0; tick < 100; tick++)
            {
                double now = tick / 10.0;
                Assert.False(Game.KeepControlledFollowerNear(standing, leader, now));
                Game.IdleControlledFollower(standing, leader, now);
                Assert.Equal(0, standing.ActionCount);
            }
        }
        var delayed = runtime.Connection(instance, wire);
        delayed.Walk.Teleport(180, 100, 0);
        delayed.Walk.SetSpeed(1, 0);
        Assert.True(Game.KeepControlledFollowerNear(delayed, leader, 0));
        Assert.Equal((180, 100), delayed.Walk.Position(0));
        // Slow but still closing: it keeps walking instead of blinking.
        Assert.True(Game.KeepControlledFollowerNear(delayed, leader, 1.3));
        Assert.True(Game.Dist(delayed.Walk.Position(1.3), (100, 100)) < 80);
        // Formation search cannot cross a disconnected wall into an apparently clear pocket.
        var isolated = new ClientState(new(1, "pocket", 300, 300, 100, 100,
            (x, y) => x <= 105 || x >= 115, []), 1, 1);
        Assert.True(Game.ControlledFollowerPoint(isolated, leader, arrival: true).X <= 105);
    }

    internal sealed class Runtime : IDisposable
    {
        readonly IGameLogic? previousLogic = Program.GameLogicOrNull;
        readonly IModuleLifetime? previousLifetime = Game.CombatLifetime;
        readonly ModuleLifetime lifetime = new("Game");
        readonly List<ClientState> players = [];
        public Runtime()
        {
            Game.CombatLifetime = lifetime;
            Program.SetGameLogic(new GameServer.Modules.Game.GameLogic());
        }
        public ClientState Connection(InstanceState instance, Wire wire, string? character = null)
        {
            var conn = new ClientState(new(1, "follower", 300, 300, 100, 100, null, []), 1, 1)
            { Instance = instance, Character = character, PlayerLife = 1000, PlayerSent = true, Controlled = true,
                Wire = ("follower-test", wire.Stream, wire.Ciphers) };
            players.Add(conn); Program.InGame.TryAdd(conn, 0);
            return conn;
        }
        public void Dispose()
        {
            foreach (var player in players) { player.Closed = true; Program.InGame.TryRemove(player, out _); }
            lifetime.Stop(ex => throw ex);
            Game.CombatLifetime = previousLifetime;
            Program.SetGameLogic(previousLogic!);
        }
    }

    internal sealed class Wire : IDisposable
    {
        readonly TcpListener listener = new(IPAddress.Loopback, 0);
        readonly TcpClient writer = new();
        readonly TcpClient reader;
        public readonly ConnectionCiphers Ciphers = new(new byte[64], false);
        public NetworkStream Stream => writer.GetStream();
        public Wire()
        {
            listener.Start();
            var accepting = listener.AcceptTcpClientAsync();
            writer.Connect(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
            reader = accepting.GetAwaiter().GetResult();
        }
        public void Dispose() { reader.Dispose(); writer.Dispose(); listener.Stop(); }
    }
}
