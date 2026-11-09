using System.Net.Sockets;
using System.Runtime.CompilerServices;
using GameServer.Core.Accounts;
using GameServer.Core.Protocol.Login;

// Server-controlled players use the same rules and ordered output as socket players. Only the
// final transport is absent; action replication still sees their unencrypted outgoing packets.
interface IControlledPlayerRules
{
    CharacterSaves.Save BuildControlled(CharacterClass characterClass, int level);
    Task InitializeControlled(ClientState player, ClientState leader);
    Task StepControlled(ClientState player, ClientState leader);
    Task ControlledCompletion(ClientState player);
}

// Optional command boundary supplied by a game module. The transport does not implement gameplay rules.
sealed record ControlledPlayerIntent(string Id, string Action, double Until, ushort Skill = 0,
    uint Target = 0, int X = 0, int Y = 0, int Flask = 0, long WallDeadline = 0);

interface IControlledPlayerCommands
{
    double MaximumLeaderDistance { get; }
    void QueueControlledIntent(ClientState player, ControlledPlayerIntent intent);
    void ClearControlledIntent(ClientState player);
    object ObserveControlledPlayer(ClientState player, ClientState leader);
}

static class ControlledPlayers
{
    /// <summary>The gear a party bot wears when its controlled player is made, by character name: base item ids of the game
    /// data for its class and level. The PlayerBots module fills this in (it owns the choice); the game module equips them,
    /// because only it can put items in the character's rows. Null or empty keeps whatever the character already has.</summary>
    internal static Func<string, IReadOnlyList<string>>? Gear;

    /// <summary>The cosmetic skin a base item takes, by (base item Id, seed): its MtxTypeGameSpecific hash and the
    /// ItemVisualIdentity art to draw. The game module answers from the cosmetics data, only for a cosmetic whose own
    /// ItemClasses name that item; null for none. The PlayerBots module uses it for the body it draws in a town, which has
    /// no connection for the worn table to arrive on.</summary>
    internal static Func<string, string, (uint Hash, ushort Visual)?>? BotSkin;

    /// <summary>Whether a base item's attribute requirements are within what that class reaches at that level (the game
    /// module answers with the same allocation its controlled characters use). The PlayerBots module filters its gear
    /// choice with it, so a bot is never dressed in armour it could not wear.</summary>
    internal static Func<GameServer.Core.Accounts.CharacterClass, int, ItemCatalog.BaseType, bool>? GearFitsClass;

    static readonly ConditionalWeakTable<ConnectionCiphers, object> Transports = new();
    internal static bool IsTransport(ConnectionCiphers ciphers) => Transports.TryGetValue(ciphers, out _);

    internal static ClientState Open(Character character, string account, ClientState leader)
    {
        var ciphers = new ConnectionCiphers(new byte[64], isClient: false);
        Transports.Add(ciphers, new());
        var player = new ClientState(leader.Layout, 0, leader.Seed)
        {
            Character = character.Name, CharacterId = character.Id, Account = account, League = character.League,
            ClassRow = Program.Skills.RowOfClass((int)character.Class),
            Pools = CharacterStats.For(Program.Skills.ClassFor(Program.Skills.RowOfClass((int)character.Class)), character.Level),
            Instance = leader.Instance, InstanceNumber = leader.InstanceNumber, LastTown = leader.LastTown,
            Controlled = true,
        };
        // The stream is borrowed solely as an opaque routing argument. Write rejects its virtual
        // cipher before touching or encrypting anything; closing a bot never disposes the leader's stream.
        player.Wire = ($"bot:{character.Name}", leader.Wire.Stream, ciphers);
        player.Outbox = (bytes, log) => Program.Send(player.Wire.Remote, player.Wire.Stream, ciphers, bytes, log);
        return player;
    }

    internal static void Close(ClientState player)
    {
        if (player.Closed) return;
        var updates = (Program.GameLogicOrNull as IControlledPlayerRules)?.ControlledCompletion(player) ?? Task.CompletedTask;
        player.Closed = true;
        Program.GameLogicOrNull?.Closing(player);
        Program.InGame.TryRemove(player, out _);
        player.Instance.RemovePlayer(player.PeerId);
        try { updates.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
        Program.GameLogicOrNull?.Save(player, prepared: player.ControlledPrepared);
        Program.GameLogicOrNull?.Left(player);
    }
}
