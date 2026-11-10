# 路线图：世界、移动与网络

属于 [路线图索引](../Roadmap.md)；这个区域已经能用的功能见 [status/world.md](../status/world.md)。
条目沿用路线图的优先级分组，同一组内大致按优先级排列。

## 1. 体验优化

### 服务端 tick 循环

- 玩家的持续恢复有自己独立的 16 ms 已过去游戏时间循环（回复、药剂与自身的正义之火）；
  原生护盾回复、来源移除与转移/重登的对照都通过。
  更广泛的恢复与自身正义之火的视觉核对是另一件事。
  见状态文档与 protocol/20-damage.md。
- 同一角色的并发/重登连接仍需要一个明确的属性发布归属方与顺序约定。
  已关闭/已退役连接的守卫及其限制见
  [角色属性生命周期](../docs/dev/character-stat-lifecycle.md)。
- **已知：** `Program.TickRate`（30/s）只约束攻击时间；命中用计时器，AI 用一个 100 ms 循环。
  队列/锁/发送的时序与共享的每轮阻挡索引已实现；但还没有统一的副本循环。
- **完成标准：** 每个副本一个 30 Hz 循环，驱动移动估算、AI 与战斗。

### 其余客户端报文布局

- **已知：** 固定长度的 0x020a 与 0x0059 报文，用它们各自的主要客户端写入点保留了每一个部分前缀。
  既有的 0x0099 与聊天频道布局保持不变。
- **入手：** 在加固定长度之前，先从其客户端写入点确定每一个未知布局。
  不要根据一次 TCP 读取推断完整报文长度，也不要在没有抓包的情况下归咎于旧的断线。
- **完成标准：** 每个受支持的客户端请求在拆分与合并读取下都能正确分帧。

## 2. 世界

### 每个区域在 seed 1 的布局

- **已知：** 生成的区域只存在于客户端里；`live.ps1 -Walkmaps` 会在客户端生成区域时抓取其行走地图、区域切换与传送点
  （[08](../docs/protocol/08-area-transitions.md) §7）。
  已抓取约 30 个区域；传送到未抓取的区域（沉没通道）会把玩家放到地图之外
  （[16](../docs/protocol/16-waypoints.md) 的 "Open"）。
- **入手：** 一个开发命令，用 `/goto` 走一遍 WorldAreas 列表并等待每次抓取。
- **完成标准：** 每一幕区域都有其 seed 1 抓取，每个有传送点的区域都在其中带上传送点。

### 服务端地图生成

- 本移植对每一份有记录的轨迹与日志里的加载都复现了客户端的生成器
  （[re/011](../docs/re/011-level-generation.md)、[status/world.md](../status/world.md)）。
  配合 `live.ps1 -Walkmaps` 用 `/tour <file>` 记录新的（区域, 种子）加载；`levelgen tourlist` 写出清单。
- **入手：** 受支持的区域会取随机种子，对其做一次 5 种子巡检发现本移植有 4 次加载算错
  （1_2_15 在 `0x14211bc2d`、1_2_14_2 在 `0x1420386d8`，见 [re/011](../docs/re/011-level-generation.md) §8 的随机种子）
  以及 202 个种子它根本生成不了（对这类种子，Gateway 会用它能生成的替代种子开新副本，见
  `ServerLevels.PlayableSeed`；修好生成器错误就能拿回这些种子）。
- **要与其赛季一起实现（目前不在范围内）：** 那 202 个区域，其等级由客户端根据服务端下发的数据构建
  （`data/areas-unavailable.txt`）。它们随各赛季的服务端一起到来，而不是随生成器：
  - 夺宝（Delve，82 个）：矿坑格子与生物群系（`Delve_*`）；客户端需要服务端的格子数据。
    现在一个初始 Delve_Main 地形块、枢纽入口与矿坑返回门已能用原生生成与抓取到的移动工作。
    初始爬行者带挂载轮子、引擎待机与火把；枢纽的原生装备控制台已摆放。
    装备购买/升级有账号/赛季持久化。第一次任务行程有一个有界的预置轨道实现，含渐进的精确总额扣费、护卫节奏与教学怪群揭示。
    原生检查用协助护卫确认了移动/光照/战斗与精确扣费。
    剩余：对照可持续的矿坑图谱校验/定价通用路线、原版生物群系填充与遭遇波次脚本、黑暗层数/抗性与照明弹/炸药使用。
    初始胜利奖励与最高完成深度现已持久化；通用的按节点奖励与矿坑图谱仍需实现。完成矿坑内的图表连续性、
    重进时恢复已铺的灯/线缆轨迹，并在不用调试传送的情况下验证一次连续的徒步护卫。
    还要完成共享硫磺迁移、装备线缆的视觉层级与其他生物群系区段。
    这不会把夺宝加入服务端地图生成器的受支持区域。
  - 夺宝（Heist，80 个）：契约与蓝图区域；布局来自夺宝计划。
  - 融合（Synthesis，24 个）：记忆区域（`Synthesis_Main*`、`Synthesis_Single*`）。
  - 侵攻（Incursion，10 个）：神庙（`Incursion_Temple*`）；客户端需要已开启与已升级房间的房间哈希
    （"Unknown Incursion Temple room hash"）。
  - 圣所（Sanctum，4 个）：各层（`SanctumCellar`、`SanctumCrypt`、`SanctumNave`、`SanctumVaults`）；
    客户端根据服务端下发的房间重建层图。
  - 深水（Deepwater，2 个）：3.29 赛季的 DeepwaterEncounter 与 DeepwaterEncounterVoyage，由服务端下发的生物群系网格构建；
    这是本移植唯一对不上的 seed 1 轨迹。
- 每日迷宫的布局是 [迷宫](#迷宫) 的第 1 步。
- **完成标准：** 任意种子都能拿到其布局，不需要抓取。

## 3. 玩法

### 游戏开局与复活

- **已知：** 物品、装备、技能栏、经验与天赋按角色存进 `data/saves.db`
  （[存档](../docs/saves.md)）（[13](../docs/protocol/13-items.md) §7、[11](../docs/protocol/11-skills.md) §11、
  [15](../docs/protocol/15-progression.md)），并且每次登录都从最后所在城镇的重生点开始
  （[09](../docs/protocol/09-area-change.md) §4）。
  这是有意跟随真实游戏：角色所在的区域与位置不保存，因为真实客户端从不会把任何人登录进去。
  新角色从暮光海滩开始，并在那里登录，直到进入城镇；
  地上的武器、垂死流放者/僵尸的技能宝石遭遇与辅助宝石宝箱都已实现并实机核对。
  教学弹窗确认与 1/2 号力墙仍未做，摆放也还需要本地抓取。
  Hillock 的死亡（任务 a1q1）会打开通往狮眼守望的大门（[12](../docs/protocol/12-quests.md) §6）。
  死亡界面的「Resurrect in Town」会去最后所在的城镇并带加载界面，在角色还没有城镇之前不提供；
  「Resurrect at Checkpoint」会去该区域已激活传送点旁边，没有的话去该区域的到达点
  （[09](../docs/protocol/09-area-change.md) §7）。
- **完成标准**（低优先级：开局与大门已能用，这些只是视觉）：Hillock 死亡时海滩的大门播放其开场、力墙在他活着时挡路、
  门房打开大门（[12](../docs/protocol/12-quests.md) §6 的 "The gate's animation"）；
  「Resurrect at Checkpoint」去该区域最后激活的检查点，且不带加载界面。
  传送点就是检查点，已激活传送点这部分已实现；
  独立的 `Metadata/MiscellaneousObjects/Checkpoint` 物件尚未摆放，所以跨多个检查点的「最后激活」仍未做。
  在 Boss 竞技场里死亡会在竞技场门外复活，即使已激活检查点；
  在地图里死亡会在藏身处复活（藏身处有了，见 [21](../docs/protocol/21-hideouts.md)；地图还没有）。
- **单向入口：** 落点在回程出口旁，就需要一个回程出口。
  从暮光海滩进狮眼守望用的是城镇里名为 `entrance1` 的点（`AreaLayout.OneWayArrivals`，
  [08](../docs/protocol/08-area-transitions.md) §3）：真实游戏里玩家会出现在大门靠城镇那一侧。
  其他单向链接在各自的命名入口点补上之前，会回落到该区域的出生点。
- **Known:** items, equipment, the skill bar, experience and passives are saved per character in `data/characters.db`
  ([saves](../docs/saves.md)) ([13](../docs/protocol/13-items.md) §7, [11](../docs/protocol/11-skills.md) §11,
  [15](../docs/protocol/15-progression.md)), and every login starts at the last town's spawn point
  ([09](../docs/protocol/09-area-change.md) §4).
  This follows the real game on purpose: the area and position a character was in are not saved, because the real
  client never logs anyone into them.
  New characters start on The Twilight Strand and log in there until they enter a town;
  the ground weapon, dying-exile/zombie skill-gem encounter and support chest are implemented and live-checked.
  Tutorial popup acknowledgements and force walls 1/2 remain open, and placements still need local captures.
  Hillock's death (quest a1q1) opens the gate into Lioneye's Watch ([12](../docs/protocol/12-quests.md) §6).
  The death screen's "Resurrect in Town" goes to the last town, with a loading screen, and is not offered before the
  character has one; "Resurrect at Checkpoint" goes next to the area's activated waypoint, else to the area's arrival
  point ([09](../docs/protocol/09-area-change.md) §7).
- **Done when** (low priority: the start and the gate work, these are visual): the Strand's gate plays its opening
  when Hillock dies, the force wall blocks the way while he lives and the Doorman opens the door
  ([12](../docs/protocol/12-quests.md) §6, "The gate's animation");
  "Resurrect at Checkpoint" goes to the last activated checkpoint of the area, with no loading screen.
  Waypoints are checkpoints, and the activated waypoint is done;
  the separate `Metadata/MiscellaneousObjects/Checkpoint` objects are not placed yet, so "last activated" across
  several checkpoints is open.
  Dying in a boss arena revives just outside the arena's door, even with a checkpoint activated;
  dying in a map revives in the hideout (hideouts exist, [21](../docs/protocol/21-hideouts.md); maps do not).
- **One-way entries:** the arrival next to the exit back needs an exit back.
  Lioneye's Watch from The Twilight Strand uses the town's named point `entrance1` (`AreaLayout.OneWayArrivals`,
  [08](../docs/protocol/08-area-transitions.md) §3): in the real game the player comes out on the town side of the
  gate.
  Other one-way links fall back to the area's spawn until their named entrance points are added.

### 迷宫

- **已知：** 服务端移植能生成迷宫的各个房间（257 个里 249 个吻合），gate 基线列出了受支持的那些
  （[re/011](../docs/re/011-level-generation.md) 的 "Labyrinth pass"）。
  当天的布局连同其中每个房间一起下发，普通难度一次跑通（[状态](../status/world.md) 的 "A Labyrinth run"）。
  升华者广场（`Labyrinth_Airlock`、`WaypointCatalog.LabyrinthPlaza`）是一个传送点，世界面板的迷宫图钉始终显示它。
  升华室的洗礼池用于选择升华并给出该阶的两点，Izaro 的三个试炼可以打
  （[状态](../status/world.md) 的 "A Labyrinth run"；
  [labyrinth-trials](../docs/game/labyrinth-trials.md) 的 "Izaro's arenas"）。
  相关表已解包（`data/tables/Labyrinth*.json`）：4 个阶及其必需试炼、任务标记与区域等级，18 个试炼、47 个区段、
  223 行区段布局、58 个秘密与 Izaro 的宝箱。
  AreaTransition 的 type 5 是迷宫（[08](../docs/protocol/08-area-transitions.md) §7），而
  `AreaTransitionCatalog` 还不认识迷宫区域。
- **已知（试炼）：** Gateway 会摆放每个试炼的门、`Switch_Once` 拉杆、上锁的门、铭牌、传送门、尖刺陷阱、
  帝王花园的飞镖陷阱、地下墓穴的扫地机、罪恶之室的锯刃及其切换用的 `Switch` 拉杆与 `Door_Open` 笼子，
  以及地穴的旋转器及其 `Switch_Multi` 与轨道开关；
  下层监狱的试炼已用真实客户端完成（[状态](../status/world.md) 的 "Trials of Ascendancy"）。
  对客户端而言，一个试炼由其任务标记或其赛季任务标记表示完成；铭牌会同时置位两者，赛季标记按账号所属赛季保存，
  所以该赛季的每个角色都有这个试炼（[labyrinth-trials](../docs/game/labyrinth-trials.md)）。
- **入手**，每步一个 PR，按此顺序：
  1. Izaro 竞技场的机制（当天在他房间上的 LabyrinthSecrets：其物件位于 BossObjectiveMarkers、
     LabyrinthSecretEffects，并带到后续试炼），一次做一个，以及正义女神。
  2. 最后一个竞技场里 Izaro 的宝箱（LabyrinthIzaroChests 位于 IzaroChestMarkers）。
  3. 无情与残酷迷宫（其选择窗口的选择）及其试炼，然后是秘密房间的物件（隐藏门、Gauntlet）与黑暗神龛、
     神圣洗礼池（LabyrinthCraftOptions）、附魔，以及带地图试炼的永恒迷宫（在地图缺失时可用开发命令打开）。
     后两者有 Sentinel 陷阱（wiki，2.3.0）：怪物（`Metadata/Monsters/Totems/LabyrinthPopUpTotem*`，类型
     `LabyrinthMonsterTotem`：Endless Pain、Hazard、Drought、Sting、Unending Lethargy、Frost、Fire、Storm、Vaal Totem，
     有些带 Proximity Shield）会从地面升起并通过光环施加减益，受伤时下沉、随后再次升起（`ReviveAfterDuration`）。
     地图试炼的终点房间把它们作为点摆放（`TrialsEndgame/*/end*.arm`），MonsterGroupEntries 的第 35 组列出它们；
     普通、无情与残酷的房间都没有。无情与永恒迷宫还有 Argus（wiki），它是当天布局中某个房间里的 Boss，掉一把宝藏钥匙。
- **完成标准：** 一个角色完成六个普通试炼，从广场进入当天的普通迷宫，走完其房间，击败 Izaro 三次并在祭坛选择升华，
  重登后仍保有该升华与它的两点。
- **Known:** the server port generates the Labyrinth's rooms (249 of 257 match), and the gate baseline lists the
  supported ones ([re/011](../docs/re/011-level-generation.md), "Labyrinth pass").
  The day's layout is sent with each room in it, and a Normal run works ([status](../status/world.md), "A Labyrinth run").
  The Aspirants' Plaza (`Labyrinth_Airlock`, `WaypointCatalog.LabyrinthPlaza`) is a waypoint the World panel's
  Labyrinth pin always shows.
  The Ascendancy Chamber's font chooses the ascendancy and gives the tier's two points, and Izaro's three trials are
  fought ([status](../status/world.md), "A Labyrinth run";
  [labyrinth-trials](../docs/game/labyrinth-trials.md), "Izaro's arenas").
  The tables are extracted (`data/tables/Labyrinth*.json`): 4 tiers with their required trials, quest flag and area
  level, 18 trials, 47 sections, 223 section layout rows, 58 secrets and the Izaro chests.
  AreaTransition type 5 is Labyrinth ([08](../docs/protocol/08-area-transitions.md) §7), and
  `AreaTransitionCatalog` does not know the Labyrinth areas yet.
- **Known (trials):** the Gateway places each trial's door, `Switch_Once` levers, Locked Doors, plaque, portal,
  spike traps, the Imperial Gardens' dart traps, the Catacombs' roombas, the Chamber of Sins' sawblades with
  their toggling `Switch` levers and `Door_Open` cages, and the Crypt's spinners with its `Switch_Multi` and rail
  switches;
  The Lower Prison's trial completes with the real client ([status](../status/world.md), "Trials of Ascendancy").
  A trial is done for the client by its quest flag or its league quest flag; the plaque sets both, and the league
  flag is saved for the account's league, so every character of it has the trial
  ([labyrinth-trials](../docs/game/labyrinth-trials.md)).
- **Start**, one pull request per step, in this order:
  1. Izaro's arenas' other mechanics (Toggle Statues and Gargoyles are done: `Rules.IzaroArena.cs` places them and
     what they leave), one at a time, and the Goddess of Justice.
  2. Izaro's chests in the last arena (LabyrinthIzaroChests at the IzaroChestMarkers).
  3. The Cruel and Merciless Labyrinths (their select-window choice) and trials, then the secret rooms' objects (hidden doors, the Gauntlet) and darkshrines, the Divine Font (LabyrinthCraftOptions),
     enchantments and the Eternal Labyrinth with its map trials (a dev command opens them while maps are missing).
     Those two have Sentinel traps (wiki, 2.3.0): monsters (`Metadata/Monsters/Totems/LabyrinthPopUpTotem*`, type
     `LabyrinthMonsterTotem`: Endless Pain, Hazard, Drought, Sting, Unending Lethargy, Frost, Fire, Storm, Vaal Totem,
     some with Proximity Shield) that rise from the floor and debuff by an aura, sink when damaged and rise again
     (`ReviveAfterDuration`). The map trials' end rooms place them as points (`TrialsEndgame/*/end*.arm`), and
     MonsterGroupEntries' group 35 lists them; no Normal, Cruel or Merciless room has one. The Merciless and Eternal
     Labyrinths also have Argus (wiki), a boss in one room of the day's layout who drops a Treasure Key.
- **Done when:** a character completes the six Normal trials, enters the day's Normal Labyrinth from the plaza,
  walks its rooms, defeats Izaro three times and chooses an ascendancy at the Altar, keeping it and its two points
  after a relog.

## 4. 服务端设计

- **预测式失同步：** Shield Charge 与地图装置的行走尚未追踪，两只怪物争抢同一格时可能在客户端与服务端之间互换位置
  （[07](../docs/protocol/07-latency-visibility-movement.md) §5 的 "Open"）。
- **Lockstep 网络模式：** 服务端决定每个动作，客户端按服务端帧回放，每次动作开始都把客户端放到服务端的格子上，
  这消除了人群里分叉的副本。
  它在本地可用、默认关闭；在真实网络下未验证（[状态](../status/world.md)）。
  - **已知：** M5 的休眠角色归属与保存格恢复已被接受，但留有未结问题。
    拾取完成、单手锤恢复、尸体碰撞、地面近战走近与教学宝箱靠近都跟随客户端。
    持续行走时的人群精度仍落后；预测漂移与静止起步的差异仍未解决
    （[07](../docs/protocol/07-latency-visibility-movement.md) §5、§6）。
  - **入手：** 补上持续行走的剩余差距，然后完成帧记录顺序、动作归属与远程追击行为，再处理其余技能、世界交互与网络验证。
  - **完成标准：** 在固定基准上，每个同步指标都在 GGG 的容差内。基准是每种武器的怪群战斗、自然怪群与一个密集区域。
    指标是各指令类型下站立的怪物数、一个怪群里的怪物数、各速度下行走的怪物数、玩家站立与行走，以及瞬移片段。
    服务端不发送任何客户端会忽略的东西，且指令的组帧与 GGG 一致。
    技能、物件、区域切换、Boss、两名玩家与真实网络连接的表现与 GGG 一致，或在 GGG 没有记录的地方与客户端的播放一致。
    默认开启与否仍由维护者决定。
- **来自客户端的 HNC 报文**（type 0xDD）：服务端应当如何处理它们
  （[07](../docs/protocol/07-latency-visibility-movement.md)）。
- **服务端世界模拟**（怪物、战斗）：还没有对应项目；需要时再开。
- **同一副本里的多名玩家：** 玩家存在、姿态、生命、离场与身体阻挡都已下发。
  需要一起验证两条已认证连接，然后给私有副本加上组队准入与共享战斗。
  玩家之间的交易与组队邀请/接受/离开都可用。下一步：好友请求（C→S 0x80）与好友列表，
  以及组队的拒绝、踢人与掉线（[28](../docs/protocol/28-player-trade.md) 的 "Open"）。
  原生的远程玩家身体与普通/传奇怪物的净空都只有单客户端核对。
  见 [status/world.md](../status/world.md) 与 [07](../docs/protocol/07-latency-visibility-movement.md)。
