# 状态：登录、角色与存档

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../Roadmap.md)。

## 已实现（已用真实客户端验证）

- 「记住密码」现在会回传并校验官方客户端保存的 32 字节登录凭据，走账号登录 0x03 / 结果 0x04。
  它能跨服务端重启存活，与设备和账号绑定，改密码即失效。
  有协议/会话测试覆盖，并已由用户在真实客户端上确认可用。
  见 [01](../docs/protocol/01-login.md)。
- 登录：自定义 TCP，Diffie-Hellman + DSA 签名的服务端密钥 + Salsa20，账号登录、角色列表、Play。
  代码：`GameServer.Auth`、`GameServer.Core/Protocol/Login`。
  文档：[01](../docs/protocol/01-login.md)、[re/008](../docs/re/008-login-handshake.md)、
  [re/009](../docs/re/009-login-messages.md)。
- 免控制台注册账号：`GameServer.Auth` 的 `RegisterListener`（tcp/20497，环境变量 `Game_AUTH_REGISTER_PORT`，
  绑定地址跟随 `Game_BIND`）逐行应答 `{"op":"register","name":…,"password":…}` 与 `{"op":"count"}`，
  走的是登录界面校验所用的同一个 `AccountService.Register`，所以 流放之路登录器.exe 既能在主机本机建号，
  也能在主机对外放开该地址后由玩家远程建号。2026-11-08 在本地用临时数据库验证过（注册、NameTaken、计数）。
- 角色保存：物品、装备、孔位与技能栏按角色存进 `data/saves.db`（新技能栏：普通攻击在左键，宝石技能只进空槽，
  右键优先）；登录后与换区域后用 S→C 0x53 恢复（键位绑定是 C→S 0x4e）。
  代码：`CharacterSaves.cs`、`PlayerInventories.cs`、Gateway。
  文档：[11](../docs/protocol/11-skills.md) §11、[13](../docs/protocol/13-items.md) §7。
- 可进游戏的登录会从角色上次所在城镇的重生点开始（登出、退回角色选择、崩溃、重启，或死亡状态下掉线之后都是如此）；
  Auth 在城镇票据解析成功时记录该城镇（`data/saves.db` 里的 `Character.LastTown`）。
  代码：Auth（`InstanceDirectory`）、`WorldAreaTowns.cs`。
  文档：[09](../docs/protocol/09-area-change.md) §4。
- 赛季与角色创建：标准与赛季模块（Allflame、无情）及其变体在创建界面都能选（S→C 0x1b），24 个角色位，
  任意赛季可选任意职业（C→S 0x12 → 0x15、0x13），直接进游戏；游戏内区域名下方显示所属赛季（0x1d）；
  硬核死亡界面来自角色所属赛季；环境变量 `Game_LEAGUES`。
  代码：`LeagueCatalog.cs`、`LoginServerSession.cs`、Auth、Gateway。
  文档：[01](../docs/protocol/01-login.md) §7。
- 在角色界面删除角色（C→S 0x0c → 新列表 0x15 + 结果 0x0d）；存档在 `data/saves.db` 里被移到一边
  （`deleted_characters`）。
  代码：`LoginServerSession`、Auth。
  文档：[01](../docs/protocol/01-login.md) §5。
- 游戏内全部职业：Characters.dat 那一行的对象类型与模型、职业字节、属性、徒手攻击与初始装备
  （两个相连白孔加宝石的武器）；已用圣堂武僧验证，地上的武器与保存的宝石也用游侠和暗影验证过。
  代码：Gateway（`SkillCatalog.cs`、`PlayerInventories.cs`）。
  文档：[01](../docs/protocol/01-login.md) §7、[04](../docs/protocol/04-player-entity.md)。
- 退回角色选择：Esc →「退回角色选择」以及硬核死亡界面的退出（C→S 0x153）；Gateway 关闭副本连接，
  客户端在同一条登录连接上回到角色列表，可玩角色能再次进入。
  代码：Gateway。
  文档：[09](../docs/protocol/09-area-change.md) §6。
- Character selection reads the saved main ascendancy for its class byte and preserves portrait cosmetics;
  the native client shows the correct portraits and names for all 21 normal ascendancies across seven base classes.

- Remember Password now returns and verifies the official client's 32-byte saved-login credential in account login
  0x03 / result 0x04.
  It survives server restarts, is device/account-bound and is invalidated by changing the password.
  Covered by protocol/session tests and confirmed working by the user in the real client.
  See [01](../docs/protocol/01-login.md).
- Login: custom TCP, Diffie-Hellman + DSA-signed server key + Salsa20, account login, character list, Play.
  Code: `GameServer.Auth`, `GameServer.Core/Protocol/Login`.
  Docs: [01](../docs/protocol/01-login.md), [re/008](../docs/re/008-login-handshake.md),
  [re/009](../docs/re/009-login-messages.md).
- Saving characters: items, equipment, sockets and the skill bar per character in `data/characters.db` (a new bar: the
  default attack on the left mouse button, gem skills into empty slots only, right mouse button first);
  the bar is restored with S→C 0x53 after login and area changes (binds are C→S 0x4e).
  Code: `CharacterSaves.cs`, `PlayerInventories.cs`, Gateway.
  Docs: [11](../docs/protocol/11-skills.md) §11, [13](../docs/protocol/13-items.md) §7.
- Playable logins start at the character's last town's spawn point (after logging out, Exit to Character Selection,
  a crash, a restart or a disconnect while dead);
  Auth records the town when a town ticket resolves (`Character.LastTown` in `data/characters.db`).
  Code: Auth (`InstanceDirectory`), `WorldAreaTowns.cs`.
  Docs: [09](../docs/protocol/09-area-change.md) §4.
- Leagues and character creation: Standard and the league modules' (Allflame, Ruthless) with their variants on the create
  screen (S→C 0x1b), 24 character slots, any class in any league (C→S 0x12 → 0x15, 0x13), straight into the game;
  the league under the area name in game (0x1d); the hardcore death screen from the character's league;
  `Game_LEAGUES`.
  Code: `LeagueCatalog.cs`, `LoginServerSession.cs`, Auth, Gateway.
  Docs: [01](../docs/protocol/01-login.md) §7.
- Character deletion on the character screen (C→S 0x0c → new list 0x15 + result 0x0d);
  the save is set aside in `data/characters.db` (`deleted_characters`).
  Code: `LoginServerSession`, Auth.
  Docs: [01](../docs/protocol/01-login.md) §5.
- Every class in game: the Characters.dat row's object type and model, class byte, attributes, unarmed attack and
  start kit (weapon with two linked white sockets, gems); verified with a Templar, the weapon on the ground and the
  saved gems also with a Ranger and a Shadow.
  Code: Gateway (`SkillCatalog.cs`, `PlayerInventories.cs`).
  Docs: [01](../docs/protocol/01-login.md) §7, [04](../docs/protocol/04-player-entity.md).
- Exit to character selection: Esc → "Exit to Character Selection" and the hardcore death screen's Exit (C→S 0x153);
  the Gateway closes the instance connection and the client returns to the character list on the same login
  connection; playable characters can enter again.
  Code: Gateway.
  Docs: [09](../docs/protocol/09-area-change.md) §6.

## 部分实现

- 选定的赛季模式已有服务端规则，并具备自动化覆盖以及原生的进入、退出、迁移验证。
  普通硬核死亡会迁移到对应的软核基础赛季，同时保留 SSF；硬核无情死亡则保持死亡。
  携带的进度与死亡记录在同一事务里提交，来源仓库/金币留在原处，过期写入无法把它撤销。
  自动化覆盖包括普通与无情模式下的致命点燃、过期死亡回调与一次性死亡效果。
  Auth、Gateway 与开发服入口都会拒绝已死亡角色和过期票据；瞬时失败导致的死亡会在进入前重试。
  原生角色列表曾把一个已死亡的硬核无情角色带着终止标记返回到了虚空（Void）；
  一次普通硬核 SSF 死亡则回到了 SSF 标准，并且能再次进入。
  完整的赛季模式对齐仍取决于服务端尚未完成的玩法系统。
  SSF 隔离与无情规则见 [赛季模式](../docs/game/league-modes.md)。
- 本地启动会在拉起客户端之前，显式把缓存的登录网关重置为回环地址，避免过期的远程地址导致 DSA 握手失败
  （General Rhoa Failure #3）。见 protocol/01-login.md。
- **存档放在 SQLite（已测试，尚未用真实客户端核对）。** 账号、角色、角色存档与仓库都是 `data/saves.db` 里的行
  （WAL，synchronous=FULL）；一个仓库是一行表头加每个页签一行。
  每次物品、仓库、金币、天赋、任务标记与传送点的变化都会在一个事务里立即保存。
  仓库移动、购买、出售、在物品上使用通货、金币洗点、地图装置与删除角色，都是要么一次事务保存全部，要么全不保存；
  保存失败会回滚、重新载入被改动的部分并关闭连接。
  经验与击杀回充的药剂每 5 秒、升级时以及断线时保存。
  挂售中的物品保留其背包格子，与 3.29 的灰色印记一致。
  等级来自保存的经验值。
  未知的存档字段会被保留；若存档里有游戏数据不认识的物品，登录会被阻止。
  JSON 存档与 accounts.json 只会被导入一次（`Saves.cs`、`ItemMoves.cs`、`GameServer.Storage/SaveDatabase.cs`、
  `SqliteAccountStore.cs`；[存档](../docs/saves.md)）。

- Selected league modes have server rules with automated coverage and native entry, exit and migration checks.
  Ordinary Hardcore death migrates to the matching softcore base while preserving SSF; Hardcore Ruthless stays dead.
  Carried progress and the death record commit together, source stash/gold stay behind, and stale writes cannot undo it.
  Automated coverage includes lethal ignite, stale death callbacks and one-time death effects
  in ordinary and Ruthless modes.
  Auth, Gateway and dev-server entry reject dead characters and stale tickets; transient failed deaths retry before entry.
  The native character list returned a dead Hardcore Ruthless character in Void with its terminal marker;
  an ordinary Hardcore SSF death returned in SSF Standard and could enter again.
  Full retail mode parity still depends on the server's incomplete gameplay systems.
  SSF isolation and Ruthless rules are described in [league modes](../docs/game/league-modes.md).
- Local startup explicitly resets the cached login gateway to loopback before launching the client, preventing a stale
  remote endpoint from causing DSA handshake failure (General Rhoa Failure #3).
  See protocol/01-login.md.
- **Saves in SQLite (tested, not yet checked with the real client).** Accounts are rows in `data/auth.db`; characters,
  character saves and stashes in `data/characters.db` (WAL, synchronous=FULL), both built from `sql/`
  ([databases](../docs/dev/databases.md); the first server start splits an old `data/saves.db`); a stash is a header row and
  one row per tab.
  Every item, stash, gold, passive, quest flag and waypoint change is saved at once in a transaction.
  Stash moves, purchases, sales, currency used on items, gold respec, the map device and character deletion save all
  their parts in one transaction or none; a failed save rolls back, reloads what it changed and closes the connection.
  Experience and kill flask refills are saved every 5 s, at a level-up and at disconnect.
  Offered sale items keep their inventory cells, as 3.29's greyed imprints.
  The level comes from the saved experience.
  Unknown save fields are kept; a save holding items the game data doesn't know stops the login.
  JSON saves and accounts.json are imported once (`Saves.cs`, `ItemMoves.cs`, `GameServer.Storage/SaveDatabase.cs`,
  `SqliteAccountStore.cs`; [saves](../docs/saves.md)).

## 尚未实现

暂无。
