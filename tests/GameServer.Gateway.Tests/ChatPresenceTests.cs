using GameServer.Core.Accounts;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using GameServer.Modding;
using GameServer.Modules.M_commands;

namespace GameServer.Gateway.Tests;

/// <summary>A lockstep client answers S→C 0x194 with a C→S 0x16 /afk carrying its text every couple of seconds. That
/// answer is not a second /afk: only the first one may reach the chat window (live 2026-11-23: every answer was
/// answered again and the chat window filled with "AFK mode is now ON.").</summary>
public sealed class ChatPresenceTests
{
    const string DefaultText = "This player is AFK.";

    static Player PlayerOf(string name) => new(new(1, name, 1000, 1000, 100, 100, null, []), 1, 1)
    {
        Character = { Name = name },
        Connection = { PlayerSent = true }
    };

    static byte[] Afk(string text) =>
        new LoginWriter().U16(InstanceItems.CommandId).U8(InstanceItems.AfkCommand).WStr(text).ToArray();

    [Fact]
    public async Task The_clients_repeated_afk_answer_is_answered_once()
    {
        var player = PlayerOf("AfkAnswer");
        var replies = new List<string>();
        player.Commands = new() { Send = (_, _) => Task.CompletedTask, Reply = text => { replies.Add(text); return Task.CompletedTask; } };
        using var context = new Context();
        try
        {
            new CommandsModule().Load(context);
            for (int i = 0; i < 5; i++)
                Assert.True(await GameEvents.AnyPacket(player, InstanceItems.CommandId, Afk(DefaultText)));
            Assert.Equal($"AFK mode is now ON. Autoreply \"{DefaultText}\"", Assert.Single(replies));
            Assert.True(await GameEvents.AnyPacket(player, InstanceItems.CommandId, Afk("back in five")));
            Assert.Equal(2, replies.Count);
            Assert.True(await GameEvents.AnyPacket(player, InstanceItems.CommandId, Afk("")));
            Assert.Equal(3, replies.Count);
            Assert.Equal("AFK mode is now OFF.", replies[^1]);
        }
        finally { GameEventOwners.Release("Commands", GameEventOwners.ModuleRank); }
    }

    sealed class Context : IModuleContext, IDisposable
    {
        readonly ModuleLifetime lifetime = new("Commands");
        public ModManifest Manifest => new("Commands", "1.0.0", ModKind.Feature, ["Game"], "commands.dll");
        public IModuleLifetime Lifetime => lifetime;
        public string Folder => "";
        public void Log(string message) { }
        public T Settings<T>() => throw new NotSupportedException();
        public void SaveSettings<T>(T settings) => throw new NotSupportedException();
        public void ProvideLeagueRules(Func<LeagueRules> rules) { }
        public void Dispose() => lifetime.Stop(e => throw e);
    }
}
