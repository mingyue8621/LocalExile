# The launcher's console channel

Chat commands run on the running Gateway itself, on a character that is in game, so a tool needs neither the game client
nor the [client control bridge](client-control.md). The launcher's **Console** section uses it
(Waypoint, `docs/launcher.md`); a release install, which has no bridge and no Frida, can run commands with it.

## The port

`127.0.0.1:Console.Port` (`Config/localexile.conf`, default 20499; `Game_CONSOLE_PORT` wins). It is bound to loopback
whatever `Game_BIND` says: the command surface is a development tool and a player on another machine never reaches it. A
second stack on the same machine keeps the first one's port; the second Gateway logs that it could not listen and serves
the game as usual.

On start the Gateway writes `data/console.json` (`{"port":20499,"pid":…}`) and removes it when it stops, so a tool can tell
a Gateway that has this channel from one built before it, and from one that is not running at all. A stale marker after a
crash is harmless: the tool also checks that something listens.

## A request, an answer

One JSON object per line, on one connection per request (as Auth's control port):

```json
{"op":"who"}
{"ok":true,"players":[{"character":"明月小哥","area":"0x2f11"}]}

{"op":"run","line":"/item Iron Ring","character":"明月小哥"}
{"ok":true,"character":"明月小哥","lines":["Item: Iron Ring"]}
```

- **who**: the connections commands can act on — in an area, command set built, not leaving.
- **run**: the line must start with `/`. `character` picks one of `who`'s names; without it the first is used.
- Errors: `no_player` (nobody is in game — no command can act on a character that is not), `not_a_command`,
  `bad_request`, `unknown_op`.

The command's replies are both the answer and chat lines in that character's chat panel, exactly as a line typed in the
chat box produces. One request runs at a time per connection: the replies are collected through `ChatCommands.Echo`, which
two runs on one connection would share.

## What it is not

It runs the same chat commands the client runs, so it needs a character in game; it does not run commands at the login
screen or for a character that is offline, and it is not a remote admin interface (loopback only, no accounts, no rate
limits beyond one request at a time).

## Tests

`tests/GameServer.Gateway.Tests/ConsoleChannelTests.cs`: the refusals, and `who` with nobody in game. A command that needs
a character is a live check: start the servers, log in, then send a line to the port (`nc 127.0.0.1 20499`) or use the
launcher's Console section.
