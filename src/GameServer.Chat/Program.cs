using System.Net;
using GameServer.Core.Chat;

// The console's encoding: the launcher reads logs\live-chat.log as UTF-8, so a chat line with a player's name must go
// out as UTF-8 rather than the console's own (936 here). Auth sets this for the same reason.
Console.OutputEncoding = new System.Text.UTF8Encoding(false);

// A public chat server for LocalExile's Gateways (Agents/docs/protocol/10-chat-and-commands.md §6): it transmits lines
// between characters on channels and stores nothing. localexile.conf Chat.BindAddress (default 0.0.0.0) and Chat.Port
// (default 6113), or Game_CHAT_BIND / Game_CHAT_PORT.
var bind = GameServer.Core.ServerConfig.ChatBind;
int port = GameServer.Core.ServerConfig.ChatPort;

using var server = new ChatServer(bind, port, maxPerAddress: 32);
server.Start();
Console.WriteLine($"[chat] listening on {bind}:{server.Port}");

var stopped = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopped.TrySetResult(); };
AppDomain.CurrentDomain.ProcessExit += (_, _) => stopped.TrySetResult();
await stopped.Task;
