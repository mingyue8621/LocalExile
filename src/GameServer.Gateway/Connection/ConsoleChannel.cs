using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// The launcher's console: the Gateway's own chat commands, run on the character in game, without the game client and
/// without the client control bridge (Agents/docs/dev/console-channel.md). A release install has neither, so its launcher
/// could only copy a command line for the game's chat box; this runs it and hands back what it answered.
///
/// 127.0.0.1:Console.Port (20499, <c>Game_CONSOLE_PORT</c>) whatever <c>Game_BIND</c> says: the command surface is a dev
/// tool, and a player on another machine never reaches it. One JSON object per line, one request per line:
/// <c>{"op":"who"}</c> → <c>{"ok":true,"players":[{"character":"…","level":N,"level_area":N}]}</c>;
/// <c>{"op":"run","line":"/item Iron Ring","character":"…"}</c> → <c>{"ok":true,"character":"…","lines":["…"]}</c>.
///
/// The port is written to <c>data/console.json</c> once it listens and removed when the Gateway stops, so a tool finds a
/// Gateway that has this channel and never starts one that does not.
/// </summary>
static class ConsoleChannel
{
    public const string MarkerPath = "data/console.json";

    /// <summary>One request at a time per character: a command's replies are told apart by <see cref="ChatCommands.Echo"/>,
    /// which two requests on one connection would share.</summary>
    static readonly ConcurrentDictionary<ClientState, SemaphoreSlim> Gates = new();

    /// <summary>Starts listening; anything that keeps it from listening is a line in the log, never a stopped Gateway, since
    /// the game itself does not need the console.</summary>
    public static void Start()
    {
        int port;
        try { port = GameServer.Core.ServerConfig.ConsolePort; }
        catch (InvalidOperationException e) { Console.Error.WriteLine($"[gateway] the console channel is off: {e.Message}"); return; }
        var listener = new TcpListener(IPAddress.Loopback, port);
        try { listener.Start(); }
        catch (SocketException e)
        {
            // A second stack on this machine keeps the first one's console port; /module and the chat box still work.
            Console.Error.WriteLine($"[gateway] the console channel can't listen on 127.0.0.1:{port} ({e.Message}); another Gateway may have it");
            return;
        }
        WriteMarker(port);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DeleteMarker();
        Console.WriteLine($"[gateway] console on 127.0.0.1:{port} (chat commands for the launcher)");
        _ = Task.Run(async () =>
        {
            while (true)
            {
                TcpClient tcp;
                try { tcp = await listener.AcceptTcpClientAsync(); }
                catch (Exception e) when (e is SocketException or ObjectDisposedException) { return; }
                _ = Task.Run(() => HandleAsync(tcp));
            }
        });
    }

    static void WriteMarker(int port)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(MarkerPath))!);
            File.WriteAllText(MarkerPath, new JsonObject { ["port"] = port, ["pid"] = Environment.ProcessId }.ToJsonString());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"[gateway] {MarkerPath} can't be written ({e.Message}): the launcher will not find the console");
        }
    }

    static void DeleteMarker()
    {
        try { File.Delete(MarkerPath); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    static async Task HandleAsync(TcpClient tcp)
    {
        using var _ = tcp;
        try
        {
            var stream = tcp.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            while (await reader.ReadLineAsync() is { } line)
                if (line.Length > 0) await writer.WriteLineAsync((await AnswerAsync(line)).ToJsonString());
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException) { }
    }

    internal static async Task<JsonObject> AnswerAsync(string line)
    {
        JsonObject request;
        try { request = JsonNode.Parse(line)?.AsObject() ?? throw new JsonException(); }
        catch (JsonException) { return Err("bad_request"); }
        try
        {
            return (request["op"]?.GetValue<string>() ?? "") switch
            {
                "who" => Who(),
                "run" => await RunAsync(request["line"]?.GetValue<string>() ?? "", request["character"]?.GetValue<string>()),
                _ => Err("unknown_op"),
            };
        }
        catch (Exception e) when (e is InvalidOperationException or FormatException or ArgumentException or JsonException)
        {
            return Err(e.Message);
        }
    }

    static JsonObject Who()
    {
        var players = new JsonArray();
        foreach (var conn in Players())
            players.Add(new JsonObject { ["character"] = conn.Character ?? "", ["area"] = $"0x{conn.Layout.Hash:x4}" });
        return new JsonObject { ["ok"] = true, ["players"] = players };
    }

    /// <summary>Runs one command line on a character in game: the named one, else the first. Its replies are the answer,
    /// and they also reach the player's chat panel as a command typed there would.</summary>
    static async Task<JsonObject> RunAsync(string line, string? character)
    {
        line = line.Trim();
        if (!line.StartsWith('/')) return Err("not_a_command");
        var target = Players().FirstOrDefault(c => string.IsNullOrEmpty(character)
            || string.Equals(c.Character, character, StringComparison.OrdinalIgnoreCase));
        if (target?.Commands is not { } commands) return Err("no_player");

        var lines = new List<string>();
        var gate = Gates.GetOrAdd(target, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            commands.Echo = lines.Add;
            await commands.RunAsync(line);
        }
        finally
        {
            commands.Echo = null;
            gate.Release();
        }
        return new JsonObject { ["ok"] = true, ["character"] = target.Character ?? "", ["lines"] = new JsonArray([.. lines.Select(l => (JsonNode?)l)]) };
    }

    /// <summary>The connections commands can act on: in an area, with their command set built, not leaving.</summary>
    static IEnumerable<ClientState> Players()
    {
        foreach (var conn in Program.InGame.Keys)
            if (conn is { Commands: not null, Closed: false, LeavingArea: false }) yield return conn;
    }

    static JsonObject Err(string error) => new() { ["ok"] = false, ["error"] = error };
}
