using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GameServer.Core.Accounts;

/// <summary>
/// Account registration for the launcher (流放之路登录器.exe): a JSON line in, a JSON line back, on
/// <c>Game_AUTH_REGISTER_PORT</c> (default 20497) with the same <c>Game_BIND</c> as the login server — so the host's own
/// launcher reaches it at 127.0.0.1 and a player's reaches it at the host's address once that is opened.
///
/// ops: <c>{"op":"register","name":…,"password":…}</c> → <c>{"ok":true,"account":…}</c> or
/// <c>{"ok":false,"error":"NameTaken"}</c>; <c>{"op":"count"}</c> → <c>{"ok":true,"count":N}</c>.
///
/// A listener of its own rather than more commands on the login port: that port speaks the official client's protocol and
/// nothing else should be mixed into it.
/// </summary>
static class RegisterListener
{
    public const int DefaultPort = 20497;
    public static int Port => int.TryParse(Environment.GetEnvironmentVariable("Game_AUTH_REGISTER_PORT"), out var p) ? p : DefaultPort;

    public static async Task ServeAsync(AccountService accounts, IPAddress bind)
    {
        var listener = new TcpListener(bind, Port);
        listener.Start();
        Console.WriteLine($"[auth] account registration on tcp/{Port}");
        while (true)
        {
            var tcp = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleAsync(tcp, accounts));
        }
    }

    static async Task HandleAsync(TcpClient tcp, AccountService accounts)
    {
        using var _ = tcp;
        try
        {
            var stream = tcp.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            while (await reader.ReadLineAsync() is { } line)
                if (line.Length > 0) await writer.WriteLineAsync(Answer(line, accounts));
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException) { }
    }

    static string Answer(string line, AccountService accounts)
    {
        JsonObject request;
        try { request = JsonNode.Parse(line)?.AsObject() ?? throw new JsonException(); }
        catch (JsonException) { return Err("bad_request"); }
        string op = request["op"]?.GetValue<string>() ?? "";
        try
        {
            return op switch
            {
                "register" => Register(accounts, request),
                "count" => new JsonObject { ["ok"] = true, ["count"] = accounts.AccountsWithCharacters().Count }.ToJsonString(),
                _ => Err("unknown_op"),
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or ArgumentException) { return Err("bad_request"); }
    }

    static string Register(AccountService accounts, JsonObject request)
    {
        var result = accounts.Register(request["name"]?.GetValue<string>() ?? "", request["password"]?.GetValue<string>() ?? "");
        return result.Ok
            ? new JsonObject { ["ok"] = true, ["account"] = result.Value!.Name }.ToJsonString()
            : new JsonObject { ["ok"] = false, ["error"] = result.Error.ToString() }.ToJsonString();
    }

    static string Err(string code) => new JsonObject { ["ok"] = false, ["error"] = code }.ToJsonString();
}
