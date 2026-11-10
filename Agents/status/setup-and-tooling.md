# 状态：环境搭建、工具与测试

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/setup-and-tooling.md)。

## 已实现（已用真实客户端验证）

- 3.29.3c 客户端：登录、角色、城镇、背包与仓库、时装、击杀、物品、药剂与技能、传送点与区域切换、NPC 与商店、
  藏身处、聊天与重登（2026-11-08 实机跑通）。
  移植工具：`scripts/re/port`、`PortMap.java`；文档：[工具](../docs/re/tooling.md) §11。
- 一步从 GGPK 解包游戏数据：`scripts/dev/extract-data.ps1`（由 `live.ps1` 每次启动时运行，`-NoExtract` 可跳过）
  会重新生成每个缺失、或清单版本已变化的数据文件（版本戳记在 `data/extract-stamps.json`），
  Gateway 启动时会对任何缺失文件警告一次。
  代码：`scripts/dev/extract-data.ps1`、`GameDataFiles.cs`。
  文档：[GettingStarted](../GettingStarted.md) 第 7 步。
- 服务端不需要 `Client/` 也能跑：等级生成与 Phrecia 图谱校验读 `data/game-files.zip`（它们要读的每种类型的全部文件），
  HNC mask 与 GGPK root hash 来自 `data/client-constants.json`，运行时不需要 Node。
  `live.ps1` 在每次启动客户端前刷新 root hash，因为 GGPK 里的网关列表会改变它。
  代码：`scripts/re/game-pack`、`GamePack.cs`、`ClientConstants.cs`。
- 玩家发布包，手动启动：一个 workflow 构建带自包含 zip 的草稿，`LocalExile.exe` 让玩家在没装 SDK、Python 或 Node 的机器上
  从客户端走到游戏里（给图形界面用 `--json`）。
  已构建并在某台 PC 的客户端上核对过解包；第一次完整客户端运行是维护者对首版草稿的核对。
  代码：`src/LocalExile`、`scripts/release`、`.github/workflows/release.yml`。
  文档：[发布](../docs/dev/releases.md)。
- 本地补丁服务端：客户端永远不会联系官方补丁服务器。
  代码：`GameServer.Patch`。
  文档：[01](../docs/protocol/01-login.md) §1、[GettingStarted](../GettingStarted.md) 第 6 步。
- 开发用账号服务：注册、登录、创建角色、列出全部账号（TCP 上的 JSON 行）。
  代码：`GameServer.Host`、`GameServer.DevClient`。
  文档：[dev-server.md](../docs/dev-server.md)。
- 给外部工具（例如桌面启动器）的钩子：`live.ps1 -Stop`（也可配 `-Only` 或 `-NoClient`）、
  `live.ps1 -ClientOnly -Walkmaps`（对着已运行的服务端起客户端与其行走地图录制器）、
  Gateway 的 `commands` 与 `values <kind>` 模式（它的聊天命令、参数类型与取值，以 JSON 行输出）、
  它的 `crafter` 模式与 `/item_give`（物品制作器的基底、词缀池与英文属性描述，以及它合成的物品：
  [item-crafter.md](../docs/dev/item-crafter.md)）、它的 `labyrinth [yyyy-mm-dd | seed]` 模式
  （某一天的四座迷宫：房间及其地图位置、连接与秘密，`LabyrinthDay.Listing`），以及开发宿主的 `accounts`。
  已在真实环境核对：`-Stop` 结束一个由 `dotnet watch` 托管、正在被观察的 Gateway、每种参数类型的取值，
  以及从真实存档数据库读出的账号列表。
  代码：`scripts/dev/live.ps1`、`Program.Completions.cs`、`ChatCommands.cs`、`GameSession.cs`。
  文档：[GettingStarted](../GettingStarted.md) 第 10 步、[10](../docs/protocol/10-chat-and-commands.md) §4、
  [dev-server.md](../docs/dev-server.md)。
- Gateway 热重载，默认开启（`live.ps1 -NoHotReload` 关闭）：Gateway 跑在 `dotnet watch` 下，会把保存的代码改动用在实际运行的
  服务端上；`HotReload.cs` 会记录每次应用的改动，并在聊天里告知玩家：某次保存开始了重载、何时应用、编译失败时
  （前三个错误，来自 `dotnet watch` 的输出）、改动没有带来任何变化时，以及该改动需要重启时。
  实机（客户端在游戏内且未掉线）：Gateway 改动 9 到 18 秒内应用（其中就有 Burning Arrow 的修复），Core 改动 0.08 秒，
  两个文件一起保存算一次重载，保存出错的代码会保留旧代码并列出错误（5 个显示为 3 个 +「还有 2 个」），随后修复被应用。
  删除成员会让 Gateway 按预期重启；在 `-NoHotReload` 之间来回切换会停掉还在加载中的 Gateway。
  错误行的解析有单元测试（`HotReloadTests`）。
  代码：`scripts/dev/live.ps1`、`HotReload.cs`。
  文档：[GettingStarted](../GettingStarted.md) 第 10 步（热重载）、[实机测试](../docs/dev/live-testing.md)。
- 客户端控制能区分显式的密码登录与记住密码的原生表单提交。
  替换密码会清除将要发出的已保存凭据标记，并保留「记住密码」偏好。
  记住密码表单的回归测试通过正常 Auth 走到角色选择；已保存登录的保留则是用 mock 验证的。
  代码：`scripts/dev/client-control.js`、`scripts/dev/test-client-login-writer.cjs`。
  文档：[客户端控制](../docs/dev/client-control.md)。
- The 3.29.3c client: login, characters, town, inventory and stash, cosmetics, kills, items, flasks and skills,
  waypoints and transitions, NPCs and shops, the hideout, chat and relog (live run, 2026-11-08). Both 3.29.3c builds
  work with the same code: the release (build 327984) and the 3.29.3.5 hotfix (build 328126; login, zones, combat,
  items and waypoints live 2026-11-09).
  Port tools: `scripts/re/port`, `PortMap.java`; docs: [tooling](../docs/re/tooling.md) §11.
- Game data extracted from the GGPK in one step: `scripts/dev/extract-data.ps1` (run by `live.ps1` on every start,
  `-NoExtract` to skip) regenerates each data file that is missing or whose manifest version changed (stamps in
  `data/extract-stamps.json`), and the Gateway warns once at startup about any missing file.
  Code: `scripts/dev/extract-data.ps1`, `GameDataFiles.cs`.
  Docs: [GettingStarted](../GettingStarted.md) step 7.
- The servers run without `Client/`: level generation and the Phrecia graph check read `data/game-files.zip` (every file
  of the types they read), the HNC mask and GGPK root hash come from `data/client-constants.json`, and Node is not needed
  at run time. `live.ps1` refreshes the root hash before each client start, because the gateway list in the GGPK changes it.
  Code: `scripts/re/game-pack`, `GamePack.cs`, `ClientConstants.cs`.
- Player releases, started by hand: a workflow builds a draft with a self-contained zip, and `LocalExile.exe` takes a player
  from the client to the game with no SDK, Python or Node installed (`--json` for a GUI).
  Built and its extraction checked on a PC's client; the first full client run is the maintainer's check of the first draft.
  Code: `src/LocalExile`, `scripts/release`, `.github/workflows/release.yml`.
  Docs: [releases](../docs/dev/releases.md).
- Local patching server: the client never contacts the official patch servers. A GGPK whose root hash is zero (the
  client then always patches and times out at "Checking resources") gets one on each start (`root-hash --repair`).
  Code: `GameServer.Patch`, `GgpkMaintenance.RepairRootHash`.
  Docs: [01](../docs/protocol/01-login.md) §1, [GettingStarted](../GettingStarted.md) step 6.
- Dev account service: register, log in, create characters, list every account (JSON-lines over TCP).
  Code: `GameServer.Host`, `GameServer.DevClient`.
  Docs: [dev-server.md](../docs/dev-server.md).
- Hooks for outside tools such as the desktop launcher: `live.ps1 -Stop` (also with `-Only` or `-NoClient`),
  `live.ps1 -ClientOnly -Walkmaps` (the client and its walk-map recorder against running servers), the
  Gateway's `commands` and `values <kind>` modes (its chat commands, their argument kinds and values, as JSON lines),
  its `crafter` mode with `/item_give` (an item crafter's bases, mod pools and English stat descriptions, and the item
  it composed: [item-crafter.md](../docs/dev/item-crafter.md)), its `labyrinth [yyyy-mm-dd | seed]` mode (a day's four
  Labyrinths: rooms with their map positions, connections and secrets, `LabyrinthDay.Listing`), and the dev host's `accounts`.
  Checked on a real setup: `-Stop` ending a watched Gateway with its `dotnet watch`, every argument kind's values, and
  the account list from the real save database.
  Code: `scripts/dev/live.ps1`, `Program.Completions.cs`, `ChatCommands.cs`, `GameSession.cs`.
  Docs: [GettingStarted](../GettingStarted.md) step 10, [10](../docs/protocol/10-chat-and-commands.md) §4,
  [dev-server.md](../docs/dev-server.md).
- Gateway hot reload, on by default (`live.ps1 -NoHotReload` turns it off): the Gateway runs under `dotnet watch`,
  which applies saved code edits to the running server; `HotReload.cs` logs each applied edit and tells players in
  chat when a save starts a reload, when it is applied, when it fails to compile (first three errors, from
  `dotnet watch`'s output), when it changes nothing, and when the edit needs a restart.
  Live, with the client in game and no disconnect: Gateway edits applied in 9-18 s (the Burning Arrow fix among
  them), a Core edit in 0.08 s, two files saved together as one reload, a broken save kept the old code and listed its
  errors (5 shown as 3 + "2 more"), and the fix applied.
  A removed member restarted the Gateway as expected; switching to and from `-NoHotReload` stops a Gateway that is
  still loading. Parsing of the error lines is unit-tested (`HotReloadTests`).
  Code: `scripts/dev/live.ps1`, `HotReload.cs`.
  Docs: [GettingStarted](../GettingStarted.md) step 10 (hot reload), [live testing](../docs/dev/live-testing.md).
- Client control distinguishes explicit password login from a remembered native form submission.
  Password replacement clears the outgoing saved-credential flag and retains the Remember Password preference.
  A remembered-form regression reaches character selection through normal Auth; saved-login preservation is mocked.
  Code: `scripts/dev/client-control.js`, `scripts/dev/test-client-login-writer.cjs`.
  Docs: [client control](../docs/dev/client-control.md).

## 部分实现

- Linux：`live.ps1` 与 `extract-data.ps1` 能在 PowerShell 7 下运行，客户端在 Proton 下运行
  （[GettingStarted](../GettingStarted.md#linux)）。已核对：解包、三套测试、带热重载的服务端，以及真实客户端登录并在海岸游玩。
  `build-release.ps1` 在 pwsh 下能构建 linux-x64 包；其启动器以原生方式配置并运行服务端。发布 workflow 会在 Windows 版旁边
  一起构建它。尚未做：`-Walkmaps`、命名客户端，以及从 Linux 发布包里起客户端。
  代码：`scripts/dev/platform-linux.ps1`。
- mod：Gateway 会把每个 `Mods/Binaries/<id>/`（一个 `mod.json` 加一个程序集，在其依赖的 mod 之后）载入它自己的
  可回收加载上下文，并在其文件夹变化时连同依赖它的 mod 一起重载；聊天里有 `/mod`、`/mod_reload`、
  `/mod_enable` 与 `/mod_disable`（后两个能跨重启保留，记在 `mod-state.txt`；运行中的 Gateway 也会在启动器改写该文件后应用它）。
  mod 文件夹里的松散 `.cs` 在加载时用 Roslyn 编译，所以保存一个就会重载它；错误会进 `/mod`、日志与聊天。
  实机：把一个脚本保存进正在运行的 Gateway 的 `Mods/Binaries/` 会被载入，而写坏的改动会报告其行号。
  设置：mod 或模块的 `settings.json` 声明哪些可以调（十种类型、默认值、限制）；用户的值存放在 `data/mods/` 与
  `data/modules/`，会被校验，改动后会重载它（[modding](../docs/dev/modding.md) 的 "Settings"）。
  用户按赛季的模块在 `data/league-modules.json` 里增删，覆盖赛季自己的清单。
  赛季（[模块](../docs/dev/modules.md)）是 `Leagues/` 下的文件夹，启动时读一次：创建界面上的卡片与勾选框与真实一致，
  有一个可迁移到的父赛季、Void，以及在其 `modules.json` 里按赛季列出的模块。已删除或已结束赛季的角色无法进入。
  尚未在客户端试过：被阻挡的角色。
  丰收、夺宝（Heist）与夺宝（Delve）是带各自程序集的真实模块，可通过 `IPlayerWorld` 热重载。
  交易与组队、图鉴天赋、制作台、时装、商人、传送门与地图装置、纹身、剧情任务（QuestObjects）、藏身处编辑、
  聊天命令与传奇腰带也都是 Core 模块。游戏本身的规则是 `Game` 模块；Gateway 保留框架、游戏数据、存档与连接状态。
  NPC 选项做什么是 `npcdialogue` 模块。模块只为列出它的赛季的玩家运行，所以没有丰收的藏身处不会摆出园艺台。
  搬迁之后尚未在客户端试过。
  七个示例 mod（默认全部关闭）已搬到 [LocalExile-Mods](https://github.com/LocalExile/LocalExile-Mods)
  （2026-11-07）；Waypoint 会安装它们。发布包不带任何 mod。
  mod 只能看到 `GameServer.Modding`：聊天命令、玩家 tick、客户端消息（mod 可以接管一条）、区域生成、
  怪物被击杀，以及掉落、经验与属性修饰量；抛异常的 mod 会被记录并跳过。
  `live.ps1` 会把 `Mods\Source\` 里的工程构建到 `Mods\Binaries\`。
  实机：示例 mod 首次构建后，运行中的 Gateway 自己就把它载入了。
  已单元测试（`ModHostTests`）：每个钩子、两个 mod 的修饰量叠加、重载会重开状态、依赖稍后到达并重载其依赖者、
  加载失败不留钩子、观察器，以及卸载后的上下文被释放。
  程序集从私有的临时磁盘副本加载，而不是可回收内存流；原始构建产物保持不被占用，回收后的上下文会释放其副本文件。
  加载器与清理的回归覆盖了这条路径。
  同样已单元测试：脚本（加载、重载、编译错误、无服务端引用、依赖、卸载）、赛季模块（回落、必需特性、可调项、
  作用域钩子）与内置模块。
  缺口：mod 替换目录里的行，以及尚未用客户端试过的钩子（掉落、经验、属性、报文）。
  代码：`src/GameServer.Modding/`、`ModHost.cs`、`ModLoadContext.cs`、`ModScripts.cs`、`ServerLeagues.cs`、`Program.Mods.cs`、
  `Program.ModuleWorld.cs`、`Modules/`、`src/GameServer.Modding/Mod.props`。
  文档：[modding](../docs/dev/modding.md)。
- Databases from SQL in git ([#734](https://github.com/LocalExile/LocalExile/issues/734),
  [databases](../docs/dev/databases.md)): `auth.db`, `characters.db`, `client.db` (the client's 943 tables, 97 MB of
  SQL, and the 14 generated files the servers read, 70 MB) and `world.db` (game content) are built and updated from
  `sql/` by each server at startup, one transaction per
  file, a changed merged file refused. `Config/localexile.conf` (written with the defaults on the first start) holds the addresses, ports and client folder. CI builds every
  database from scratch and checks the SQL; the merge names pending updates. Verified: the Gateway starting on fresh
  databases with the former seed's counts, every client table and generated file rebuilt identically, a real `saves.db`
  migrated and played on. Not yet: the pending rename on `main` (set up with a deploy key, not yet run), and the generated files
  served from `client.db` in a real-client session.
  Code: `GameServer.Storage/SqlUpdater.cs`, `GameServer.Core/Tables/ClientTables.cs`, `GameServer.Core/Config/ServerConfig.cs`.
- Linux: `live.ps1` and `extract-data.ps1` run under PowerShell 7 and the client under Proton
  ([GettingStarted](../GettingStarted.md#linux)). Verified: the extraction, the three test suites, the servers with hot
  reload, and the real client logging in and playing on the Coast. `build-release.ps1` under pwsh builds a linux-x64
  package; its launcher sets up and runs the servers natively. The release workflow builds it beside the Windows one. Not yet:
  `-Walkmaps`, named clients, and the client from a Linux release.
  Code: `scripts/dev/platform-linux.ps1`.
- Mods: the Gateway loads each `Mods/Binaries/<id>/` (a `mod.json` and an assembly, after the mods it depends on) into a
  collectible load context of its own and reloads it, with its dependents, when its folder changes; `/mod`, `/mod_reload`,
  `/mod_enable` and `/mod_disable` in chat (the last two last across restarts, in `mod-state.txt`; a running Gateway also
  applies that file when a launcher rewrites it).
  Loose `.cs` files in a mod's folder compile with Roslyn when it loads, so saving one reloads it; errors go to `/mod`, the log and chat.
  Live: a script saved into a running Gateway's `Mods/Binaries/` loaded, and a broken edit reported its line.
  Settings: a mod's or module's `settings.json` declares what can be tuned (ten types, defaults, limits); the user's values
  are in `data/mods/` and `data/modules/`, checked, and a change reloads it ([modding](../docs/dev/modding.md), "Settings").
  A user's modules per league are added and removed in `data/league-modules.json`, over the leagues' own lists.
  Leagues ([modules](../docs/dev/modules.md)) are folders in `Leagues/`, read once at start: cards and check boxes as in the real create screen, a parent to migrate to, Void, and
  modules listed per league in its `modules.json`. Characters of a deleted or ended league cannot be entered.
  Not tried in the client: a blocked character.
  Harvest, Heist and Delve are real modules with their own assemblies, which hot-reload, through `IPlayerWorld`.
  Trades and parties, Atlas passives, the crafting bench, cosmetics, vendors, portals and the map device, tattoos, the campaign's quests (QuestObjects), hideout editing, chat commands and unique belts are Core modules too. The game\'s rules are the `Game` module; the Gateway keeps the framework, game data, saves and connection state. What an NPC's options do is the `npcdialogue` module. A module runs only for the players whose league lists it, so a hideout
  without Harvest places no station. Not tried in the client after the move.
  The seven example mods, all off by default, moved to [LocalExile-Mods](https://github.com/LocalExile/LocalExile-Mods)
  (2026-11-07); Waypoint installs them. A release ships no mods.
  Mods see only `GameServer.Modding`: chat commands, the player tick, client messages (a mod may take one), area
  generated, monster killed, and drop, experience and stat modifiers; a mod that throws is logged and skipped.
  `live.ps1` builds the projects in `Mods\Source\` into `Mods\Binaries\`.
  Live: the running Gateway loaded the example mod by itself when it was first built.
  Unit-tested (`ModHostTests`): every hook, two mods' modifiers together, a reload starting the state over, a
  dependency arriving later and reloading its dependent, a failed load leaving no hook, the watcher, and the unloaded
  context being freed.
  Assemblies load from a private temporary disk copy rather than collectible memory streams; original build outputs
  remain free, and collected contexts release their copied files. Loader and cleanup regressions cover this path.
  Unit-tested as well: scripts (load, reload, compile errors, no server references, dependencies, unload), league modules
  (fallback, required features, tunables, scoped hooks) and built-ins.
  Missing: mods replacing catalog rows, and the hooks not yet tried with the client (drops, experience, stats, packets).
  Code: `src/GameServer.Modding/`, `ModHost.cs`, `ModLoadContext.cs`, `ModScripts.cs`, `ServerLeagues.cs`, `Program.Mods.cs`,
  `Program.ModuleWorld.cs`, `Modules/`, `src/GameServer.Modding/Mod.props`.
  Docs: [modding](../docs/dev/modding.md).

- 只读的天赋图谱工具会把钉死的 3.29.3b 拓扑与合并后的 PSG 导出为 `data/` 下的分析产物。
  解析与合并有往返、畸形输入与普通天赋树保持的自动化覆盖。
  导出器拒绝被改动过的源图谱，且从不把产物安装进客户端归档。
  这只是数据工具；Gateway 侧的消费者与原生集成仍属后续工作。
  这一独立层没有新的原生启动证据；改动客户端归档需要另行同意。
  代码：`PassiveSkillGraph.cs`、`AdditionalAscendancyGraph.cs`、`GameFileSystem.cs`、`scripts/re/passive-graphs`。
  文档：[天赋图谱数据指南](../docs/dev/passive-graph-data.md)。

- 一台 PC 上跑多套栈与多个客户端：`live.ps1 -PortBase N` 让一个 checkout 的服务端跑在它自己的五个端口上
  （记在 `data\stack.json`；Auth 现在会把客户端送到那个 Gateway），`-Session NAME` 再起该栈的更多客户端，
  每个在自己的文件夹里、有自己的设置文件、已保存登录、`Client.txt` 与 PID 记录，`-Stop` 只停该 checkout 自己的服务端与客户端。
  `live.ps1` 停的永远是这个 checkout 自己的；非默认端口的栈从不写共享的 GGPK 网关列表。
  桥接（`--session`）会挂到自己会话的客户端，并按该客户端的连接过滤 Gateway 的聊天回复。
  已核对：21000-21004 上的一个栈与另一个 checkout 在默认端口上运行着的实机栈和客户端并存，启动、重启、停止之后，
  后者仍在正常运行；会话客户端在该栈上以自己的文件夹启动（自己的 `Client.txt`、链接的 GGPK、共享的音效库与
  经 junction 的 Streamline）。测试：`scripts/dev/test_live_client_only.ps1`（端口、会话、PID 记录、进程归属、
  GGPK 被占用时真实读取网关列表）、`test_client_control.py`（通过真实 junction 启动的真实进程）、`GgpkMaintenanceTests`。
  尚未实机见到：会话设置文件被实际使用，以及在某会话上用桥接登录（内存守护在加载时把客户端停掉了）。
  代码：`scripts/dev/live.ps1`、`scripts/dev/client_control.py`、`GatewayEndpoint.cs`。
  文档：[实机测试](../docs/dev/live-testing.md#several-stacks-and-clients)。

- 剧情自动化：`scripts/dev/campaign.ps1` 会在需要时启动该 checkout 的服务端（`-PortBase`），打开它自己的会话客户端
  （`-Session`，默认 `campaign`；`-Nullifier`），登录、创建一个全新角色（或继续某个角色），并通过控制桥用
  `/god immune`、传送与 `/kill` 走完任务顺序；任务进度来自任务引擎对真实击杀、拾取、物件使用与 NPC 对话的反应。
  可续跑（已完成的任务与步骤会跳过），带诊断与卡住步骤的恢复；如果其端口上的服务端不属于该 checkout、
  `Game_SAVES_DB` 缺失（除非 `-AllowDefaultSaves`）、Gateway 用在另一个存档数据库上，或同一会话上已有第二次运行，
  都会中止运行。步骤文件：第一幕（a1q1-a1q9）；后续幕的任务在其所在幕进入任务引擎之前都是「未实现」。
  开发命令 `/state`、`/kill`、`/quest_preset`、`/god immune`；`/tp` 也能找到 `/state` 报告的 id 与任务规则关注的物件。
  同一个驱动在游戏搬进模块之前实机跑过第一到第十幕与尾声；在任务引擎的第一幕上它只做离线核对
  （对着模拟服务端的跑测器测试、`test_client_control.py`、`CampaignCommandTests`）。
  代码：`scripts/dev/campaign/`、`Modules/Core/Player/Commands/Rules.Campaign.cs`、`CampaignState.cs`。
  文档：[剧情自动化](../docs/dev/campaign-automation.md)、[10](../docs/protocol/10-chat-and-commands.md)。
- Campaign automation: `scripts/dev/campaign.ps1` starts the checkout's servers if needed (`-PortBase`), opens its
  own session client (`-Session`, default `campaign`; `-Nullifier`), logs in, creates a fresh character (or continues
  one) and plays the quest order through the control bridge with `/god immune`, teleports and `/kill`; quest progress
  comes from the quest engine reacting to the real kills, pickups, object use and NPC talk. Resumable (done quests and
  steps are skipped), with diagnostics and recoveries for stuck steps; servers on its ports that are not the
  checkout's, a missing `Game_CHARACTERS_DB` (unless `-AllowDefaultSaves`), a Gateway on another save database and a second
  run on the same session stop the run. Step files: Act 1 (a1q1-a1q9); the later acts' quests are "not implemented" until their act
  is on the quest engine. Dev commands `/state`, `/kill`, `/quest_preset`, `/god immune`; `/tp` also finds the ids
  `/state` reports and the objects quest rules watch.
  The same driver played Acts 1-10 and the Epilogue live before the game moved into modules; on the quest engine's Act 1
  it is checked offline only (runner tests against a simulated server, `test_client_control.py`,
  `CampaignCommandTests`).
  Code: `scripts/dev/campaign/`, `Modules/Core/Player/Commands/Rules.Campaign.cs`, `CampaignState.cs`.
  Docs: [campaign automation](../docs/dev/campaign-automation.md), [10](../docs/protocol/10-chat-and-commands.md).

- 调试辅助：带时间戳的服务端日志、带堆栈的完整异常（出错的报文处理函数会点名其消息）、每条 `DISCONNECT` 行的原因、
  最近 10 次完整运行的归档日志、每条连接最后的 256 个报文（出错或无法解释的断线后打印，可导出到 `logs\packets`、
  `/lastpackets`）、`Game_TRACE` 分类、`scripts/dev/bugreport.ps1` 与 `scripts/re/frida/trace-crash.js`。
  例行外发报文的摘要使用有界后台队列；显式报文追踪保持同步。
  已单元测试（`ServerLogTests`、`PacketHistoryTests`），归档清理与 bug 报告（日志被占用、各种形式的文件名）
  用脚本核对过；用 dump 与 `trace-crash.js` 捕获真实客户端崩溃的实机运行仍待做，`trace-crash.js` 是否正确读取客户端
  接收泵也待核对。
  代码：`ServerLog.cs`、`PacketHistory.cs`、`Program.Connection.cs`、`scripts/dev/live.ps1`。
  文档：[调试](../docs/dev/debugging.md)。

- 轻量会话客户端：`live.ps1 -Session S` 会通过 `scripts/dev/nullifier.py` 启动每个会话客户端，它把客户端生到自己的会话文件夹里、
  放进随关闭即杀的作业对象，并注入 Frida 代理：让它的两副本互斥量变成会话本地名字（这样它永远不会占用用户客户端需要的
  `Global\PoERunMutexA/B` 两个槽位）、不让其窗口抢焦点、绝不允许它限制、移动、捕获或隐藏用户的鼠标、不装全局输入钩子、
  不注册后台原始输入，并设为低于正常的优先级与 EcoQoS；`-Nullifier` 再加上引擎自己的 Null 渲染器（不用 GPU 设备；
  代理修好了 Null 索引缓冲缺失格式的问题，那曾导致进区域崩溃）、工作集裁剪、分配器立即归还释放的内存
  （mimalloc `purge_delay=0`），以及 Null 缓冲区未用的 CPU 副本在被触碰前不提交。`live.ps1 -Agent` 会启动服务端与一个这样的客户端
  （没有记录端口段时自动挑一段）。新的会话默认使用轻量 DirectX 11 预设。
  2026-10-31 实机：两种渲染器下的登录、游玩、斜杠命令、聊天、`/tp`、传送点旅行与 `/goto`；DirectX 11 预设提交 4.3 GB、
  显存 1.7 GB；Null（精简默认值）提交 1.58 GB（之前 1.93 GB）、无 GPU 显存、城镇里约占 0.2 个核心，旅行后站立 5% 到 9%
  （普通客户端：7.5 GB、3.4 GB 显存、37%）。覆盖巡检（`nullifier-tour.py`：每一幕的城镇与一个战斗区域、第一幕 Boss 区域、
  一张地图、藏身处，以及每个战斗区域里用普通攻击打一场）在 Null 与精简默认值下通过了 27/27；开场影片会跳过（`--nologo`），
  加载画面影片会自己关闭。游戏数据库无法在多个客户端间共享：已加载的表持有进程专有的指针（无头客户端 §6）。
  离线测试：`test_nullifier.py`、`test-nullifier-mutex.cjs`、`test_client_control.py`、`test_live_client_only.ps1`。
  代码：`scripts/dev/nullifier.js`、`nullifier.py`、`nullifier-measure.ps1`、`nullifier-bench.py`、`nullifier-tour.py`。
  文档：[无头客户端](../docs/dev/headless-client.md)。

## 尚未实现

暂无。

## 测试

游戏搬进模块时 Gateway 的测试被清空了；`tests/GameServer.Gateway.Tests` 在新测试落地前只有共享的初始化。新测试的规则见 AGENTS.md。

传送名称类对照使用一个未映射的合成目标，所以真实的 WorldAreas 数据不会改变它们的标签。
弓击恢复的对照会显式在检查输入准入之前启动其连接时钟。
这些夹具修正不会改动生产目录与共享的模块初始化器。
Arcane Might 的区域夹具从武器接触原点测量到其目标的距离。
怪物 AI 节奏测试跑在非并行集合里：它们真实的 300 ms socket 延迟上限没变，
但无关的目录加载与 GC 停顿不再在旁边同时进行。
每目标范围精度的回归会等待一次完成的扫射，而不是与持续的 Cyclone 引导抢时序。
Gateway 的测试集合现在默认串行运行，以便让真实的 socket 时限与可回收的 mod 上下文远离无关的目录加载/GC 竞争。
个别并发测试仍会跑自己的并发任务。

## 工具

PR 检查：扫描新增行里的隐藏文本，以及只有维护者能批准的改动文件（[docs](../docs/dev/pr-checks.md)）。
Pull request checks: a scan of added lines for hidden text, the SQL rules and a from-scratch build of every database, and
files only the maintainer approves ([docs](../docs/dev/pr-checks.md)).

用来弄明白协议的一切都在仓库里：`scripts/re/` 下的 Ghidra、Frida 与 Python 脚本，`Tools/` 下我们自己的工具，
以及第三方工具的安装方法在 [GettingStarted](../GettingStarted.md)（"Third-party tools"）。
完整的逆向环境还没有从全新 clone 重建过；缺什么就改指南。

客户端的每条网络消息都按两个方向列出，由二进制生成。
S→C：[schema.md](../docs/protocol/schema.md)，四个工厂（副本 272、世界 63、登录 27、补丁 6 个类），
各自与其工厂 switch 的显式分支数量相等。
C→S：[c2s.md](../docs/protocol/c2s.md)，副本 354、登录 28 与补丁 4 个 id，来自每一处调用流刷新的地方。
其精确长度与 Gateway `KnownClientLength` 里全部 87 个可比较的 id 一致。
代码：`scripts/re/ghidra/ReadSchema.java`、`SendSites.java`、`scripts/re/schema_md.py`、`c2s_md.py`。

可选的[原生客户端控制](../docs/dev/client-control.md)提供 CLI/JSON RPC，便于开发时不必反复手动操作界面。
它只针对复制出来的独立 3.29.3c 客户端与本地服务。
登录在成功 0 先于已认证状态 3 时，会在其既有超时内继续等待。
用 mock 的执行/RPC 回归覆盖了这段等待、拒绝 6 与超时；这个修复没有新的原生验证。
它还能创建与删除角色、按传送点旅行（也可按 WorldAreas Id）、等待区域并获知当前区域；它能使用物品、存进仓库并
拾取地面物品（这些是剧情驱动脚本的请求）。

## 文档注意事项

`docs/re/003`–`004` 是带有已知错误的早期笔记（例如「登录是 HTTPS」；实际是自定义 TCP，见 `re/007`）。
把它们当作历史，而不是参考资料。
