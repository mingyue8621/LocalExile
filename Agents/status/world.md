# 状态：世界、移动与网络

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/world.md)。

## 已实现（已用真实客户端验证）

- 副本连接：打开、加密切换（0x11）、分帧、分发器。
  代码：`GameServer.Gateway`、`GameServer.Core/Protocol/Instance`。
  文档：[02](../docs/protocol/02-instance-connection.md)。
- 进入狮眼守望：区域（0x1d）、本地玩家 id、世界激活、HUD。
  代码：Gateway。
  文档：[03](../docs/protocol/03-area-and-world.md)。
- 玩家对象（0x2bb）及其组件 blob；角色的名字与职业模型。
  代码：`InstanceEntities.cs`。
  文档：[04](../docs/protocol/04-player-entity.md)。
- 界面所需的背包与天赋槽位（空）。
  代码：Gateway。
  文档：[05](../docs/protocol/05-inventories-and-ui.md)。
- 延迟应答（0x1b/0x1c）与 HNC 旁路，在读取循环里优先于其他处理函数应答；每条连接建立时清空客户端过期的 tick 队列
  （服务端延迟 0xFFFF，所以 F1 的 NETWORK 读数为 0，只在 tick 逾期时才出现尖峰；超过 50 ms 的 tick 或处理函数会被记录）、
  可见性（唤醒区）、点击移动。一次完整的技能列表重建会持有发送闸门，10 颗宝石的角色耗时 5 到 10 ms
  （2026-10-29 之前是 180 到 270 ms）。
  代码：Gateway、`Hnc.cs`、`ClientInbox.cs`。
  文档：[07](../docs/protocol/07-latency-visibility-movement.md)。
- 服务端上玩家的行走就是客户端自己的寻路与迈步，1:1 移植（A* 加玩家的椭圆限制、路径点剪枝、
  按客户端步时的 Bresenham 迈步，NPC 与仓库作为阻挡）：在一次录制会话里，客户端的 424 条路径全部吻合，
  上报的格子精确或差一步之内（客户端帧时序）。
  走 NPC、仓库、传送点与区域切换时的接近使用客户端的距离（对象尺寸 + 3）；点击按 socket 读取时刻计时；
  怪物路径也是 1:1（1235 条全中，100 次扩展 + 粗筛）。
  代码：`GameServer.Core/World/ClientPathfinder.cs`、`PlayerWalk.cs`。
  文档：[07](../docs/protocol/07-latency-visibility-movement.md) §5。
- 服务端帧（S→C 0x2b8 帧长度、每 33 ms 的 0x2b7 帧结束）：lockstep 客户端的世界就跑在它上面；预测式客户端会忽略它。
  代码：`Game.RunLockstepFrames`、Gateway `WindowsTimerResolution`。
  文档：[07](../docs/protocol/07-latency-visibility-movement.md) §6。
- 每个区域的区域切换，带目的地、模型（悬停时高亮拱门）、旋转与高度。
  在 Gateway 自己生成的布局里，每个出口都通往其房间图节点所说的位置："entranceN" 到 Connections[N − 1]，
  "teleportN" 到 "telepadN" 槽位作为区域内的一道门（上层监狱通往监狱长区、Boss 竞技场）。
  布局禁用的出口（"entranceNdisabled"）在任务把它开启时会创建（`Quests.FlagToEnter`），角色拿到标记后就显示，
  与其他受任务开启的出口一样（第三幕：上神之权杖通往水渠、图书馆通往档案馆；第七到第十幕：灰烬之地向北的路、
  港桥通往第九幕、从喂养槽回家的 Sin 传送门；已离线核对，尚未在模块上用客户端核对）。
  其他区域使用行走地图录制器的抓取与 atlas。
  离线审计 `scripts/re/exits` 会在多个种子上检查每个生成的区域（`--quests`：连同各幕的表，所以也包含被任务开启的禁用出口）；
  已核对城镇 ↔ 海滩 ↔ 潮汐岛与海滩 ↔ 泥滩；经由它们换区域；Ctrl+点击打开副本管理器（列表、加入、创建）；
  `/goto <区域>`。
  旧区域的属性与恢复写入器会在其 socket 关闭前退役，无法替换当前的装备资格。
  原生的需求移除/恢复、四次城镇/海滩传送、法术付款、护盾回复与重登对照都通过。
  代码：Gateway（`AreaTransitionCatalog.cs`）、`AreaExits.cs`、`scripts/walkmaps`、`scripts/re/atlas`、
  `scripts/re/exits`、Auth 的副本注册表。
  文档：[08](../docs/protocol/08-area-transitions.md) §8、[09](../docs/protocol/09-area-change.md)。
- 到达点：通过出口（或 `/goto`）进入时，玩家落在回程出口旁边；
  暮光海滩 → 狮眼守望落在城镇的 `entrance1`；在生成的布局里，单向入口落在其 "entranceNdisabled" 点
  （泛滥深渊 → 沉没通道的深渊返回口）；
  Resurrect at Checkpoint 会在那里复活，角色激活传送点之后则在传送点旁。
  `/tp`（开发用）把玩家传送到某个格子，或某个具名怪物、NPC、切换点、传送点、已抓取物件旁边。
  代码：`AreaLayout.Arrival`、`TeleportTargets.cs`、Gateway。
  文档：[09](../docs/protocol/09-area-change.md) §4。
- 副本：每个城镇一个（WorldAreas `IsTown` 的全部 12 个剧情城镇）；其他区域得到私有副本，空置 6 分钟后销毁。
  代码：`InstanceRegistry.cs`、`WorldAreaTowns.cs`。
  文档：[09](../docs/protocol/09-area-change.md)。
- 首次到访时从客户端抓取行走地图与区域切换（可选，`live.ps1 -Walkmaps`：不加就什么都不录）；
  Gateway 对自己不生成的区域使用它们。
  代码：`scripts/walkmaps`、`live.ps1 -Walkmaps`。
  文档：[GettingStarted](../GettingStarted.md) 第 9 步、[08](../docs/protocol/08-area-transitions.md) §7。
- 传送点：区域布局被捕获处生成传送点物件（狮眼守望内置），进入城镇时激活、进入 25 格内或点击时激活（客户端播放激活动画），
  按角色保存；点击会以旅行模式打开世界面板（S→C 0x7b），其图钉与连线按角色到访过的区域与已激活传送点显示
  （S→C 0x4c，任务标记经 0x2bc 放在 Player 组件里），选中一个图钉（C→S 0x4d，Ctrl → 副本选择器）
  会把玩家送到目的地传送点旁；`/waypoints [all | reset]`。
  代码：`InstanceWaypoints.cs`、`WaypointCatalog.cs`、`AreaVisitFlags.cs`、Gateway。
  文档：[16](../docs/protocol/16-waypoints.md)。
- 暂停：在城镇与藏身处之外按 Esc 菜单会暂停区域（C→S 0x1d4 → S→C 0x1d3）：Gateway 的时钟停止，怪物不移动也不挥击，
  回复与药剂等待，客户端也暂停其世界；关闭菜单即恢复。
  代码：`InstancePause.cs`、Gateway（`SetPaused`）。
  文档：[22](../docs/protocol/22-pause.md)。

- 副本分帧会把被拆开的 0x020a 与 0x0059 报文按其主要写入点长度保留为 3 与 16 字节。
  每个拆分与合并的 tick/聊天回归都通过；集成的原生传送之后命令仍被保留。
  未知报文布局与既有的 0x0099 处理保持不变。
  文档：[副本分帧](../docs/protocol/02-instance-connection.md#fixed-message-boundaries-across-tcp-reads)。

## 部分实现

- 只要客户端选择 lockstep 就提供服务（没有服务端开关；日志会注明更推荐预测式）。它处理拾取/攻击恢复、尸体碰撞与
  地面近战/宝箱接近；人群与预测式方面的差距仍在（[07](../docs/protocol/07-latency-visibility-movement.md) §6）。
- 赛季隔离按完整赛季对城镇与副本选择强制执行。
  SSF 野外副本与 Gateway 的地面物品状态还额外属于稳定的角色 ID。
  另一个角色（包括复用已删除名字的角色）无法继承或加入那份状态。
  赛季/SSF 副本的自动化检查通过。
  共享的同伴可见性保留 SSF 城镇可见性，而可变的世界状态保持私有。
  其他玩家用客户端有动画的 Move 动作行走。
  多客户端加入世界与拾取边界仍由自动化检查覆盖。
  见 [赛季模式](../docs/game/league-modes.md)。
- **预测式的角色碰撞。** 玩家路径把同一副本里活着的怪物与已发布的玩家计入。
  怪物路径使用其 `MonsterVarieties.ObjectSize` 净空，并避开其他活体（含友好的）。
  一个 50 ms 的 pass 按时间顺序推进玩家与怪物的估算，与客户端一致（不发报文）：被地形或身体挡住的一步会结束行走，
  被截断的路径会从其末端重新寻路。在 lockstep 客户端上，这个 pass 改为整帧重放，每个行走者按对象顺序（玩家优先）对着
  到目前为止已移动的格子前进，正如客户端的世界 tick（[07](../docs/protocol/07-latency-visibility-movement.md) §5）。
  任何东西都不会被挤出身体。死亡释放占用的空间，尸体留在区域里。
  近战怪物以其指向玩家的攻击追击，约每 200 ms 一次，挥击在 S→C 0x1ac 时开始。
  远程怪物与召唤物在玩家周围取环位（`MonsterSurround`）；AI 每 50 ms 决策一次。
  自动化回归覆盖新槽位与复用槽位的玩家身体净空。
  进一步的自动化检查覆盖短攻击射程、静止被挡的槽位与排队的引导/狂怒刷新。
  一个 50 ms 的 pass 像客户端一样推进估算（不发报文）：被地形或身体挡住的一步结束行走，被截断的路径从其末端重新寻路，
  攻击不会停止行走；死亡释放占用的空间，尸体留在区域里。
  追击者在玩家周围按环散开（`MonsterSurround`）；AI 每 100 ms 决策一次。
  自动化回归覆盖负足迹的直接追击与新槽位/复用槽位的玩家身体净空。
  进一步的自动化检查覆盖短攻击射程、静止被挡的槽位、有界的人群脱离与排队的引导/狂怒刷新。
  组合回归覆盖静默的引导更新、被移除的宝石，以及人群移动之前的投射物接触。
  AI 与修正的 pass 共享带索引的身体快照；包围位的预留使用空间查询与缓存环。
  队列年龄、发送/线路/socket 时序与热战斗锁时序都独立于 tick 延迟测量。
  眩晕与冰冻会按住行走与最新点击直到结束；速度变化会让进行中的段重新开始；攻击会跟随目标到挥击；图腾与教学宝箱阻挡路径。
  近战命中在挥击片段的接触关键帧落下。
  重同步（S→C 0x128）：每次眩晕、位移技能之后、客户端自己请求时（C→S 0x16 命令 8）以及 `/oos` 时下发；
  它修正 20 格及以上的误差，更小的不动。
  原生客户端对照覆盖普通僵尸、传奇 Hillock 与一个注入的远程玩家对象。
  远程玩家的创建、移动、生命、离场与技能动作都已复制。
  代码：`LivingCollision.cs`、`ClientState.LivingCollision.cs`、`Program.LivingCollision.cs`、`InstancePlayers.cs`。
  文档：[07](../docs/protocol/07-latency-visibility-movement.md)。
- **共享副本**（两个客户端，2026-11-05）：一个副本的怪物属于它自己（`InstanceWorld`），跑在它的时钟与 id 上；
  每个客户端都会看到它们，任何玩家的命中伤害的是同一批怪物，每只怪物瞄准最近的玩家。
  地面物品共享，任务物品与击杀任务进度给附近的每个玩家，击杀经验给附近所有人。
  异常状态 tick 与怪物碰撞跑在一条连接的循环上；迷雾之王的竞技场物件与赛季遭遇仍属于单个玩家。
  代码：`InstanceWorld.cs`、`SharedMonsters.cs`、`SharedKills.cs`、`SharedGround.cs`。
- **出生点。** 登录、Resurrect in Town，以及除暮光海滩 → 狮眼守望之外的单向入口，都使用区域的出生点：
  城镇的固定点，或生成区域里离任何边缘最远的可走格子。
- **区域种子。** Gateway 自己生成的区域，新副本会取随机种子（`Game_RANDOM_SEEDS=0` 固定为 1）；
  城镇、藏身处以及仍从抓取读取的区域使用 seed 1。
  客户端在 seed 1 也生成不了的区域（第二与第七幕的十字路口、第六幕的暮光海滩与南部森林、卡鲁要塞、
  部分地图与赛季竞技场）会用客户端在该处构建出的某个种子代替 seed 1 游玩
  （`ServerLevelAreas.SeedsInPlaceOfOne`、`AreaSeeds`）；一旦该种子的轨迹在每一层都吻合，Gateway 就自己生成它
  （re/011 §7），迷宫的 Estate Crossing 区段还不行。
  本移植生成不了的种子（海滩 21 个中 2 个、1_2_12 的 21 个中 5 个）会用由它派生的 8 个替代种子中本移植能生成的第一个
  来游玩（`ServerLevels.PlayableSeed`，在 0x1d 里下发）：在此之前，这种地图没有行走网格、区域切换与传送点。
  已打开的地图按 0x1d 下发的区域等级生成（其档位：16 档为 83），也就是客户端生成所用的等级，而不是它的 WorldAreas 等级
  （2026-11-01）。此前一张 16 档的 Chateau（WorldAreas 71）拿到的是与客户端不同的拓扑，于是玩家被生成到客户端布局之外
  （黑屏），而且用了客户端构建不了的种子。
- **等级生成器移植。** `GameServer.Data/Terrain/Generation` 复现客户端的等级生成器：格子网格、可走与投射物网格、
  装饰件、每个格子的地面高度（`CellHeights.cs`）以及区域切换与传送点（`AreaObjects.cs`：格子的物件，
  以及没有格子摆放传送点时房间的 `Waypoint` 行，如 The Ledge 的 `waypoint.arm`）。
  1671 条 seed 1 轨迹中有 1669 条在格子哈希、两个网格、本移植的每一次随机抽取与每个装饰件上都与客户端一致；
  客户端 3.29.3 记录的 2886 次加载中有 2884 次按格子哈希一致（`scripts/re/levelgen/gate.ps1`）；
  客户端自己生成失败的 118 次加载里，本移植以同样方式失败 112 次（gate 重跑后预计 115 次：圣所地窖、中殿与金库
  现在会像客户端一样失败）。客户端在 seed 1 生成不了的 34 个区域（且不需要服务端数据）用本移植能生成的随机种子做了巡检：
  73 次加载全部吻合。
  对每个带拓扑的 WorldAreas 行做的一次无人值守巡检（2026-10-27，`scripts/re/levelgen/tourloop.py`，它会自己重启并登录客户端）
  使 1932 行中有 234 行没有客户端侧等级可比对：17 个没有拓扑、202 个需要服务端的数据（侵攻神庙、夺宝矿坑格子与生物群系、
  夺宝契约、融合记忆、圣所各层、深水生物群系网格；它们随各赛季的服务端一起来，见
  [路线图](../roadmap/world.md)），以及 15 个客户端在任何试过的种子上都生成不了（清单写在 data/areas-unavailable.txt）。
  本移植与其余每一行的一次加载都吻合。
  对每个吻合区域取 3 个随机种子巡检：519 次全部吻合。那次巡检里其他种子的每一次有记录加载（1215 次，`lg.ps1 loads`）
  在抽取、网格、装饰、高度与切换上也全部吻合，最后一个（某个神龛的地面，2_6_2）自 2026-10-29 起吻合。
  对每个受支持区域做 5 种子巡检，在 475 个停靠点（101 个区域）后停止：469 个在每一层以及房间的怪物点上都吻合；
  4 个不吻合（1_2_15 在 3 个种子上、1_2_14_2 在 1 个上），本移植生成不了 7700 个种子中的 202 个（re/011 §8）。
  每一次有高度抓取的加载都逐格吻合，186 次有切换抓取的也全部精确吻合（gate 重跑后预计 186 次：有一次抓取被录制器截断，现已修复）。
  第五幕奴隶围栏的弹坑效果（`GlowyFX`、`SlavePensIntroGround`：`server_only` 类型，只有服务端创建时客户端才绘制）
  以其终态下发（Transitionable 3）；客户端会绘制它们（2026-10-29，seed 1）。
  Gateway 自己生成受支持的区域（`ServerLevels.cs`）：即那些 seed 1 轨迹通过每一项 gate 检查的区域（`gate-baseline.json`）；
  它们的行走网格、投射物网格、格子高度、切换与传送点，以及怪物点与 a1 任务所拟合的装饰件、房间与任务格子物件，都不需要抓取。
  进入其他任何区域会告知玩家：该区域由客户端生成、服务端尚不支持。
  客户端在装饰件之后的阶段（网格变化、地面杂物、小生物）是纯客户端的，没有移植。
  代码：`GameServer.Data/Terrain/Generation`、`scripts/re/levelgen`。
  文档：[re/011](../docs/re/011-level-generation.md)。
- **迷宫当天的布局。** Gateway 像客户端一样构建当天布局（区段、房间、连接与秘密：168 个客户端布局吻合），
  并按官方服务端的做法给每个房间分配其 LabyrinthAreas 区域；进入这些区域之一时，房间的节点会放在 0x1d 的额外数据里下发，
  客户端与服务端的生成随后同样应用该房间的覆盖与秘密房间（实机：`1_Labyrinth_OH_quad_#` 及其 Gauntlet 房间，2026-11-02）。
  当天是官方的那一天：种子（UTC 天数，`Game_LAB_SEED`）与房间区域都与 poelab.com 的布局一致（re/011）。
  房间的格子种子是我们自己定的。`/lab_layout [阶] [yyyy-mm-dd]` 列出某天的布局。
  秘密房间的物件（隐藏门、Gauntlet 的终点）尚未创建。
  代码：`LabyrinthLayout.cs`、`LabyrinthPass.cs`、`LabyrinthDay.cs`、`ServerLevels.cs`。
  文档：[03](../docs/protocol/03-area-and-world.md)、[re/011](../docs/re/011-level-generation.md)（"Labyrinth pass"）。
- **一次迷宫跑（普通）。** 萨恩营地（第三幕、第八幕）的铭牌在六个普通试炼完成前显示试炼，之后永久打开雕像与其后通往广场的路
  （`/trials off` 可关闭）。在广场上，装置打开选择窗口（0x7b kind 0x10）；选择普通（C→S 0x212 = 1）会带动画打开大门，
  并显示通往当天第一个房间的路；传送门回城。每个房间是它自己的副本（跑动会选定其种子，所以服务端知道它的节点），
  其 "entranceN" 出口通往邻居，雕文雕像打开迷宫地图（0x7b kind 0x0f）并显示本次跑动的房间（S→C 0x1cb），
  出口旁的雕像还会显示它后面的房间、通往那里的路用绿色标出（2026-11-07 实机核对）；
  跑到别处即结束这次跑动，广场的门重新关闭。2026-11-03 实机核对。
  在最后一个房间的升华室里，洗礼池给出该阶的两点升华点并打开职业选择器（0x7b kind 0x0a；选择 C→S 0x39 会被保存并重载区域），
  其传送门回城。2026-11-03 实机核对：在那里选的职业重登后仍保留。
  Izaro 的三个升华者试炼：走廊的门使用后打开（他的战前台词），玩家进入竞技场时他从电梯上升、手持当天的武器（机制台词），
  竞技场的门关闭；他的血条在试炼之间按三分之一递进：前两次他在下一个三分之一处离开（他的台词、走到电梯中间、乘其下降），
  门与继续的路打开；最后一次他死亡，通往升华室的路打开，并置位 `LabyrinthKilledIzaro` 与该阶的 `LabyrinthComplete` 标记。
  他的台词是 S→C 0x1c7。2026-11-07 实机核对（第一个试炼）。
  尚未做：竞技场的机制、正义女神、他的宝箱、附魔台、无情与更高难度的阶、秘密房间的物件；房间的出口仍用普通 AreaTransition 类型
  （不是 type 5）。
  代码：`LabyrinthDay.cs`、`IzaroFights.cs`、`AreaTransitionCatalog.cs`、QuestObjects 的 `Rules.LabyrinthTrials.cs`、
  `Rules.Izaro.cs`、`Act3.cs`、`Act8.cs`。
- **升华试炼。** 在每个 LabyrinthTrials 区域里，Gateway 会摆放试炼房间的门、拉杆、上锁的门、铭牌与回程传送门，
  以及门口的青铜铭文，点击后立刻可读。
  门会打开，拉杆打开其房间连接所指的东西（`.arm` 文件的连接段），铭牌置位试炼的任务标记与其赛季任务标记，
  按账号所属赛季保存，所以该赛季的每个角色在那里都算完成（客户端会把它计为完成），并打开回门的传送门、
  打开带已完成试炼的试炼铭牌窗口（0x7b kind 9）；已完成试炼的传送门也只用其铭牌时才打开。
  进入试炼区域会给出「领主迷宫」任务（`LabyrinthQuestReceived`）：客户端的追踪器随之显示
  「Solve the Trial of Ascendancy in this Area」直到完成，六个普通试炼完成之后显示「Enter the Labyrinth」
  （[labyrinth-trials](../docs/game/labyrinth-trials.md) 的 "The Lord's Labyrinth quest"）；已实机核对（2026-11-01）。
  一个副本打开过的东西保持打开；已打开试炼的上锁门（栅栏）被移除，房间的门保持打开。
  已在真实客户端的下层监狱核对（2026-10-31）。
  它的尖刺陷阱从拉杆出发沿环向门依次触发（铭牌房间的按棋盘式），在拉长的出土动画（0x1d1）中升起，
  对站在其格子上的玩家造成 LbRetractableTrap 那份生命与能量护盾、流血一秒，并在其显示期间封住该格子；
  已与原始游戏肉眼比对。
  帝王花园的 14 座飞镖炮台每 1.5 秒齐射一次，每座四支毒镖、间隔四分之一圈，经由投射物世界飞行，遇墙与炮台 Hull 停下；
  一支镖造成 LbArrowTrap 那份伤害，并按命中的生命份额叠加一层 2 秒毒，扣减护甲，并使移动速度降低 60%、持续 2 秒。
  正在实机核对。
  地下墓穴的八台扫地机沿轨道运行（两个整圈、两条往返链，按它们的朝向布置）并切割其中的玩家，
  每秒按生命与能量护盾的份额递减（2026-11-01 按原版实机拟合），并带上扫地机自身的效果；试炼的门为行走而打开（已在地下墓穴核对）。
  罪恶之室的九片锯刃沿轨道往返（显示，Rail_Visible），两两同步，切割方式同扫地机；它的两个拉杆（`Switch`）双向：
  拉下，一个拉杆关闭其房间周围的笼子（`Door_Open`）并停下其连接所指的锯刃，拉回则打开并让它们继续运转。
  已对照原版实机核对（2026-11-01）。
  地穴的十二个旋转器（旋转的晨星）逐段沿轨道片段运行，在端点与挡块处折返，绕过转角，在岔口按轨道开关的状态选择方向，
  切割方式同扫地机；它的 `Switch_Multi` 抬起挡路的两处挡块，使那些旋转器跑到各自通道尽头，它的 `Switch` 把上层房间的
  TriCorner 摆直，让那两个旋转器离开小环进入方形轨道，两者都双向；其上层旋转器房间与铭牌房间之间的普通门在被点击前保持关闭，
  把铭牌房间的怪物关在里面。已对照原版实机核对（2026-11-01）。
  火葬场的炽热地板（包括其墙边格栅下的那些）按各自行的固定高度位于地面格栅之下，并按计时器一起加热（峰值 5 秒、冷却 6 秒），
  在岩浆存在时灼烧玩家（每秒按生命与能量护盾的份额，扣减火焰抗性，带岩浆增益的燃烧观感）；它的三个拉杆各打开其对应的上锁门
  并让齿轮转动；它的 Immolators 会带来 Ember Claimants。时序、伤害与 Immolators 的骷髅已对照原版实机核对（2026-11-01）；
  格栅下地板的实际高度正在核对。
  区域的怪群会避开试炼的陷阱；其房间得到房间文件摆放的东西：花园的四只 Devourer、下层监狱铭牌平台的怪群与其刷新点的稀有
  Brittle Poacher、地穴刷新点的稀有 Frost Priest、火葬场刷新点的稀有 Cinder Elemental 与其四个 Immolators，
  以及原版在房间文件没有怪群的地方有的怪群（罪恶之室上层笼子前的两个、地穴试炼门后与其铭牌旁各一个、
  火葬场试炼第一个房间里一个、其第二道上锁门后一个）。
  未做：Izaro 房间的陷阱，以及炽热地板在无情与残酷难度下的房间。
  迷宫自己的房间（`1_Labyrinth_*`）从 Gateway 生成的房间得到相同的陷阱、拉杆与门，按其房间的连接接线
  （尖刺链与触发器、轨道、拉杆、地板与飞镖）：尖刺按各自的 `Trigger_*` 计时器与踩上去触发的压力尖刺、深渊边缘刀刃、
  飞镖炮台与墙镖、扫地机、锯刃、带双拉杆房间互换件的旋转器、ElegantHouse 房间的炽热地板（`*_EH`），
  以及它们的谜题：`Door_Counter` 门、`Counter_Handler(_Any)`、会回弹的曲柄、会打开上锁门并翻转共用笼子的 `Switch_Multi` 拉杆。
  它们的格子还加了 ElegantHouse 的门洞滚轮（下压时切割的滚轮、带片段打击的上升尖刺）、深渊边缘刀刃、
  一击可破的碎墙、一对隐藏开关打开的 DryGarden 秘密墙，以及玩家靠近时其怪物走下底座的雕像。
  它们的区域按房间组得到迷宫自己的怪物怪群（我们自己定的：没有数据点名它们）。已在
  `1_Labyrinth_RT_straight`、`_C_straight`、`_OH_end`、`_P_straight`、`_EH_end`、`_EH_end_bottleneck`、`_DG_end`、
  `_DG_quad_bottleneck`、`_P_end_bottleneck` 与 `_P_straight_bottleneck` 的 seed 1 上实机核对（2026-11-01、2026-11-02）。
  怪物除攻击之外的技能（Discharge、焦油、死亡新星）与房间出口（一次跑动）尚未实现。
  代码：`LabyrinthTrials.cs`、QuestObjects 的 `Rules.LabyrinthTrials.cs`。
  文档：[labyrinth-trials](../docs/game/labyrinth-trials.md)。
- Lockstep is served whenever the client picks it (no server setting; the log notes Predictive is preferred). It
  handles pickup/attack recovery, corpse collision and ground melee/chest approach;
  crowd and predictive gaps remain ([07](../docs/protocol/07-latency-visibility-movement.md) §6).
- League isolation is enforced by full league for towns and instance selection.
  SSF wilderness instances and Gateway ground-item state additionally belong to the stable character ID.
  Another character, including one reusing a deleted name, cannot inherit or join that state.
  Automated league/SSF instance checks pass.
  Shared peer presence preserves SSF town visibility while mutable world state remains private.
  Other players walk with the Move action the client animates.
  Multi-client world joining and pickup boundaries remain covered by automated checks.
  See [league modes](../docs/game/league-modes.md).
- **Predictive actor collision.** Player paths include living monsters and published players in the same instance.
  Monster paths use their `MonsterVarieties.ObjectSize` clearance and avoid other living bodies, including friendlies.
  A 50 ms pass steps the player's and the monsters' estimates in time order as the client does (no packets): a step
  blocked by terrain or a body ends the walk, and a path cut short paths again from its end. On a lockstep client the
  pass replays whole frames instead, each walker in object order (the player first) against the cells as moved so far,
  as the client's world ticks ([07](../docs/protocol/07-latency-visibility-movement.md) §5). Nothing is pushed out of a
  body. Death releases the occupied space and the corpse stays in the area.
  Melee monsters chase with their attack aimed at the player, about every 200 ms, and the swing starts on S→C 0x1ac.
  Ranged monsters and minions take rings around the player (`MonsterSurround`); the AI decides every 50 ms.
  Automated regressions cover player-body clearance for new and reused slots.
  Further automated checks cover short attack reach, stationary-blocked slots and queued channel/Rage refreshes.
  A 50 ms pass steps the estimates as the client does (no packets): a step blocked by terrain or a body ends the walk, a
  path cut short paths again from its end, and an attack does not stop the walk; death releases the occupied space and
  the corpse stays in the area.
  Chasers spread around the player on rings (`MonsterSurround`); the AI decides every 100 ms.
  Automated regressions cover direct chases for negative footprints and player-body clearance for new and reused slots.
  Further automated checks cover short attack reach, stationary-blocked slots, bounded crowd escape and queued
  channel/Rage refreshes.
  Combined regressions cover silent channel updates, removed gems and projectile contacts before crowd movement.
  AI and correction passes share indexed body snapshots; surround reservations use spatial queries and cached rings.
  Queue age, send/wire/socket timings and hot combat-lock timings are measured independently of tick latency.
  Stuns and freezes hold the walk and the newest click until they end; a speed change restarts the segment in progress;
  an attack follows its target to the swing; totems and the tutorial chest block paths.
  Melee hits land at the swing clip's contact keyframe.
  Resync (S→C 0x128): sent on every stun, after a movement skill, on the client's own request (C→S 0x16 command 8) and by `/oos`;
  it corrects errors of 20 cells or more and leaves smaller ones alone.
  Native-client checks cover a normal zombie, unique Hillock and an injected remote player object.
  Remote player creation, movement, life, departure and skill actions are replicated.
  Code: `LivingCollision.cs`, `ClientState.LivingCollision.cs`, `Program.LivingCollision.cs`, `InstancePlayers.cs`.
  Docs: [07](../docs/protocol/07-latency-visibility-movement.md).
- **Shared instance** (two clients, 2026-11-05): an instance's monsters are its own (`InstanceWorld`), on its clock and
  ids; every client is shown them, any player's hits hurt the same ones and each monster targets the nearest player.
  Ground items are shared, quest items and kill quest progress go to each player near, kill experience to all near.
  Ailment ticks and monster collisions run on one connection's loop; the King in the Mists' arena objects and the
  league encounters stay one player's.
  Code: `InstanceWorld.cs`, `SharedMonsters.cs`, `SharedKills.cs`, `SharedGround.cs`.
- **Spawn points.** Logins, Resurrect in Town and one-way entries other than The Twilight Strand → Lioneye's Watch use
  the area's spawn: the town's fixed point, or the walkable cell farthest from any edge in a generated area.
- **Area seeds.** A new instance of an area the Gateway generates itself gets a random seed (`Game_RANDOM_SEEDS=0`
  pins it to 1); towns, hideouts and the areas still read from captures use seed 1.
  An area the client cannot generate at seed 1 either (The Crossroads of acts 2 and 7, Act 6's Twilight Strand and
  Southern Forest, The Karui Fortress, some maps and league arenas) is played at a seed the client built in place of
  seed 1 (`ServerLevelAreas.SeedsInPlaceOfOne`, `AreaSeeds`); the Gateway generates it once a trace at that seed
  matches in every layer (re/011 §7), not yet the Labyrinth's Estate Crossing sections.
  A seed the port cannot generate (2 of 21 for The Coast, 5 of 21 for 1_2_12) is played at the first of 8 replacements
  derived from it that the port generates (`ServerLevels.PlayableSeed`, sent in 0x1d): before, such a map had no walk
  grid, area transitions or waypoint.
  An opened map is generated at the area level 0x1d sends (its tier: 83 for tier 16), the level the client generates
  at, not its WorldAreas level (2026-11-01). Before, a tier-16 Chateau (WorldAreas 71) took another topology than the
  client's, so the player spawned outside the client's layout (a black screen), and seeds the client cannot build
  were played.
- **Level generator port.** `GameServer.Data/Terrain/Generation` reproduces the client's level generator: the tile
  grid, the walkability and projectile grids, the doodads, every cell's ground height (`CellHeights.cs`) and the area
  transitions and waypoints (`AreaObjects.cs`: the tiles' objects, and a room's `Waypoint` line where no tile places
  one, as The Ledge's `waypoint.arm`).
  1669 of the 1671 seed-1 traces match the client in tile hash, both grids, every random draw the port makes and every doodad,
  and 2884 of the 2886 loads the 3.29.3 client logged match by tile hash (`scripts/re/levelgen/gate.ps1`); of the 118
  loads the client itself failed to generate, the port fails 112 the same way (115 expected once the gate reruns:
  the Sanctum Cellar, Nave and Vaults now fail as the client does). The 34 areas the client cannot
  generate at seed 1 (and that need no server data) were toured at random seeds the port generates: all 73 loads match.
  An unattended tour of every WorldAreas row with a topology (2026-10-27, `scripts/re/levelgen/tourloop.py`, which
  restarts and logs in the client by itself) left 234 of the 1932 rows with no client-side level to match: 17 without a
  topology, 202 that need data from the server (Incursion temples, Delve cells and biomes, Heist contracts, Synthesis
  memories, the Sanctum floors, the Deepwater biome grid; they come with each league's server side, see the
  [roadmap](../roadmap/world.md)), and 15 the client cannot generate at any seed tried (the
  list is written to data/areas-unavailable.txt). The port matches a load of every other row.
  A tour of 3 random seeds per matching area: all 519 match. Every traced load at another seed of that tour (1215,
  `lg.ps1 loads`) matches in draws, grids, doodads, heights and transitions too, the last one (a shrine's ground,
  2_6_2) since 2026-10-29. A 5-seed tour of every supported area, stopped after 475 stops (101 areas): 469 match in
  every layer and in the rooms' monster points; 4 do not (1_2_15 at 3 seeds, 1_2_14_2 at 1), and the port cannot
  generate 202 of the 7700 seeds (re/011 §8).
  Every load with a height capture matches cell for cell, and every one of the 186 with a transitions capture
  matches exactly (186 expected once the gate reruns: one capture was cut short by the recorder, now repaired).
  Act 5's crater effects in The Slave Pens (`GlowyFX`, `SlavePensIntroGround`: `server_only` types the client draws
  only when the server creates them) are sent at their end state (Transitionable 3); the client draws them
  (2026-10-29, seed 1).
  The Gateway generates the supported areas itself (`ServerLevels.cs`): those whose seed-1 trace passes every gate
  check (`gate-baseline.json`); their walk grid, projectile grid, cell heights, transitions and waypoint, and the
  doodads, rooms and quest tile objects the monster points and a1 quests are fitted to, need no capture.
  Entering any other area tells the player it is generated on the client and not supported by the server yet.
  The client's stages after the doodads (mesh variation, ground clutter, critters) are client-only and not ported.
  Code: `GameServer.Data/Terrain/Generation`, `scripts/re/levelgen`.
  Docs: [re/011](../docs/re/011-level-generation.md).
- **The Labyrinth's day layout.** The Gateway builds the day's layout as the client does (sections, rooms,
  connections and secrets: 168 client layouts match) and gives each room its LabyrinthAreas area as the official
  server does; entering one of those areas sends the room's node in 0x1d's extra data, and the client and the server's
  generation then apply the room's overrides and secret rooms alike (live: `1_Labyrinth_OH_quad_#` with its Gauntlet
  room, 2026-11-02). The day is the official one: the seed (the UTC day number, `Game_LAB_SEED`) and the room areas
  match poelab.com's layouts (re/011). A room's tile seed is ours. `/lab_layout [tier] [yyyy-mm-dd]` lists a day.
  The secret rooms' objects (hidden doors, the Gauntlet's end) are not created yet.
  Code: `LabyrinthLayout.cs`, `LabyrinthPass.cs`, `LabyrinthDay.cs`, `ServerLevels.cs`.
  Docs: [03](../docs/protocol/03-area-and-world.md), [re/011](../docs/re/011-level-generation.md) ("Labyrinth pass").
- **A Labyrinth run (Normal).** The Sarn Encampment's (Acts 3, 8) plaque shows the trials until the six Normal ones are
  done, then opens the statue for good and the way to the plaza behind it (`/trials off` shuts it). At the plaza the
  device opens the select window (0x7b kind 0x10); choosing Normal (C→S 0x212 = 1) opens the door with its animation
  and shows the way into the day's first room; the portal goes back to town. Each room is its own instance (the run
  picks its seed, so the server knows its node), its "entranceN" exits lead to its neighbours, the glyph statues open
  the Labyrinth map (0x7b kind 0x0f) with the run's rooms (S→C 0x1cb),
  and a statue by an exit also shows the room behind it, the way there in green (checked live 2026-11-07);
  being anywhere else ends the run, and the plaza's door shuts again. Checked live 2026-11-03.
  In the last room's Ascendancy Chamber the font gives the tier's two ascendancy points and opens the class selector
  (0x7b kind 0x0a; the choice, C→S 0x39, is saved and the area reloads), and its portal goes back to town.
  Checked live 2026-11-03: the class chosen there stays after a relog.
  Izaro's three Aspirant's Trials: the corridor's gate opens when used (his prefight line), he rises on the elevator
  with the day's weapon as the player comes into the arena (the mechanic's line) and the arena's doors shut; his life
  bar goes in thirds from trial to trial: in the first two he leaves at the next third (his line, a walk to the
  elevator's middle, down on it) and the doors and the way on open; in the last he dies, the way to the Ascendancy
  Chamber opens and `LabyrinthKilledIzaro` and the tier's `LabyrinthComplete` flag are set. His lines are S→C 0x1c7.
  Checked live 2026-11-07 (trial 1).
  Two arena mechanics, Toggle Statues and Gargoyles, with what they leave on him shown and active in the later
  trials (alcove and ledge) and his two end lines; no life bar while he rises, dimmed at his trial's floor; the account's
  stash in the room before each arena and the Ascendancy Chamber. Checked live 2026-11-08: a whole run on 3.29.3c, the
  gargoyles' beam on 3.29.3b.
  Not yet: the other arena mechanics, the Goddess of Justice, his chests, the room's craft recipe, the enchanting bench,
  Cruel and harder tiers, the secret rooms' objects; a room's exits keep the plain AreaTransition type (not type 5).
  Code: `LabyrinthDay.cs`, `IzaroFights.cs`, `AreaTransitionCatalog.cs`, QuestObjects `Rules.LabyrinthTrials.cs`,
  `Rules.Izaro.cs` (`Rules.IzaroArena.cs`, `Rules.IzaroStatues.cs`, `Rules.IzaroGargoyles.cs`), `Act3.cs`, `Act8.cs`.
- **Trials of Ascendancy.** In each LabyrinthTrials area the Gateway places the trial rooms' door, levers, Locked
  Doors, plaque and portal back, and the Bronze Monograph at the door, which reads at once when clicked.
  The door opens, a lever opens what its room's links name (the `.arm` files' link section), the plaque sets the trial's quest flag and its league quest
  flag, saved for the account's league so the trial is done for each of its characters there (the client counts it
  done), opens the portal back to the door and opens the Trial Plaque window (0x7b kind 9) with the trials done; a
  done trial's portal too opens only when its plaque is used.
  Entering a trial's area gives the Lord's Labyrinth quest (`LabyrinthQuestReceived`): the client's tracker then says
  "Solve the Trial of Ascendancy in this Area" until it is done, and "Enter the Labyrinth" once the six Normal trials
  are ([labyrinth-trials](../docs/game/labyrinth-trials.md), "The Lord's Labyrinth quest"); checked live (2026-11-01).
  What an instance opened stays open; an opened trial Locked Door (bars) is removed, a room's gate stays open.
  Verified with the real client in The Lower Prison (2026-10-31).
  Its spike traps cascade round their ring from the lever towards the door (the plaque room's as a checkerboard), rise
  over a stretched Emerge animation (0x1d1), hit a player on their tile for LbRetractableTrap's share of life and energy
  shield, bleed for a second and wall their tile while they show; matched by eye against the original game.
  The Imperial Gardens' 14 dart turrets fire together every 1.5 s, four poison darts each a quarter turn apart, through
  the projectile world, stopping at walls and at the turrets' Hull; a dart hits for LbArrowTrap's share and adds a 2 s
  poison stack reckoned from the hit's life share, less armour, and slows movement by 60% for 2 s. Being checked live.
  The Catacombs' eight roombas run their rails (two rings round, two chains to and fro, set out as they face) and cut
  a player inside them, who degenerates by a share of life and energy shield a second (fitted live to the original,
  2026-11-01) with the roomba's own effect on them; the trial doors open for walking (checked in The Catacombs).
  The Chamber of Sins' nine sawblades run their tracks (shown, Rail_Visible) to and fro, each pair together, and cut
  as the roombas do; its two levers (`Switch`) go both ways: pulled, a lever shuts the cage round its room
  (`Door_Open`) and stops the sawblades its links pause, pulled back it opens it and they run on. Checked live
  against the original (2026-11-01).
  The Crypt's twelve spinners (spinning morning stars) run their track pieces piece by piece, turning back at ends
  and at stoppers, round corners, and at a junction as its rail switch stands, and cut as the roombas do; its
  `Switch_Multi` lifts the two stoppers in the way up so those spinners run to their lanes' ends, its `Switch` sets
  the upper room's TriCorner straight so its two spinners leave their small rings for the square, both going both
  ways; the plain door between its upper spinner room and the plaque room stays shut until clicked, keeping the
  plaque room's monsters in. Checked live against the original (2026-11-01).
  The Crematorium's hot plates, those under the grates by its walls too, lie at their lines' fixed height under the
  floor's grates and heat together on a timer (5 s at the peak, 6 s cold) and
  burn a player while their lava lies (a share of life and energy shield a second, less fire resistance, with the lava
  buff's burning look); each of its three levers opens its Locked Door and sets its gears turning; its Immolators
  bring Ember Claimants. Timings, damage and the Immolators' skeletons checked live against the original (2026-11-01);
  the plates' height under the grates is being checked.
  The area's packs keep away from a trial's traps; its rooms get what their room file places: the Gardens' four
  Devourers, the Lower Prison plaque platform's pack and its spawner's rare Brittle Poacher, the Crypt spawner's rare
  Frost Priest, the Crematorium spawner's rare Cinder Elemental and its four Immolators, and the packs the original has where the room file
  has none (two before the Chamber of Sins' upper cage, one past the Crypt trial's door and one by its plaque, one in
  the Crematorium trial's first room and one past its second Locked Door).
  Not built: the traps of Izaro's rooms and the hot plates' Cruel and Merciless rooms.
  The Labyrinth's own rooms (`1_Labyrinth_*`) get the same traps, levers and doors from the Gateway's generated rooms,
  wired by their rooms' links (spike chains and triggers, rails, levers, plates and their darts): spikes on their
  `Trigger_*` timers and pressure spikes a step fires, abyss edge blades, dart turrets and wall darts, roombas,
  sawblades, spinners with the two-lever rooms' swap piece, the ElegantHouse rooms' hot plates (`*_EH`), and their
  puzzles: `Door_Counter` gates, `Counter_Handler(_Any)`, cranks that come back up, `Switch_Multi` levers that open
  Locked Doors and flip shared cages. Their tiles add the ElegantHouse doorway rollers (a dropping roller that cuts while
  down, rising spikes striking with their clips), abyss edge blades, smashable walls a hit breaks, the DryGarden secret
  wall a pair of hidden switches opens, and statues whose monster steps off its pedestal when the player comes near.
  Their areas get packs of the Labyrinth's own monsters by room set (ours: no data names them). Checked live on seed 1
  of `1_Labyrinth_RT_straight`, `_C_straight`, `_OH_end`, `_P_straight`, `_EH_end`, `_EH_end_bottleneck`, `_DG_end`,
  `_DG_quad_bottleneck`, `_P_end_bottleneck` and `_P_straight_bottleneck` (2026-11-01, 2026-11-02). The monsters'
  skills beyond attacks (Discharge, tar, death novas) and the room exits (a run) are not built.
  Code: `LabyrinthTrials.cs`, QuestObjects `Rules.LabyrinthTrials.cs`.
  Docs: [labyrinth-trials](../docs/game/labyrinth-trials.md).

## 尚未实现

- Gateway 自己不生成的区域，其区域切换需要该种子的 `-Walkmaps` 抓取。
  游戏后来才打开的出口没有建模："teleportNdisabled" 门（竞技场的入口、回去的捷径）以打开状态创建。
  没有自身物件的 "entranceN"（Malachai 之后通往 Highgate 的传送门、Weylam 的船）是一个在该点没有模型的出口，从一开始就在。
  战斗结束时游戏做的移动没有建模：腐烂之核在 Depraved Trinity 之后通往 Weylam 船的路（telepad16）
  （[08](../docs/protocol/08-area-transitions.md) §8）。
  带额外组件的任务物件会以普通 AreaTransition 下发，所以暮光海滩的大门出现时没有动画；门房与开场教学的力墙没有被创建
  （[12](../docs/protocol/12-quests.md) §6）。
  只有任务 a1q1 与 a1q4 有脚本；客户端的任务追踪器由标记构建。
- 传送到一个没有该种子 `-Walkmaps` 抓取的生成区域，会把玩家放到任意格子（沉没通道，seed 1）；
  任务标记由到访过的区域与传送点推导，而不是来自任务（[16](../docs/protocol/16-waypoints.md) 的 "Open"）。
