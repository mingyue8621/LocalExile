# 状态：藏身处、制作台与地图

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/hideouts-and-maps.md)。

## 已实现（已用真实客户端验证）

- 夺宝柜（Heist Locker）在每个藏身处摆放，并打开其原生窗口（0x7b kind 0x37）。
  原生窗口的处理函数与可见状态已用真实客户端核对。
  它的摆放支持藏身处编辑器；这次改动没有加入存储转移。
  见 [21](../docs/protocol/21-hideouts.md) 的 Heist Locker。
- 园艺制作台（2026-10-27）：摆放在每个已选藏身处普通制作台旁边；交互会打开原生配方浏览器（0x7b kind 0x39）。
  C→S 0x274 携带所选 HarvestCraftOptions 的 HASH16（实机抓取：ReforgeFire 0xa3e5）；S→C 0x276 携带制作出的物品 id
  并清除面板的待处理状态，所以「Craft」可以反复点击而不必重开。
  定向/亲和重铸、增幅与词缀转换都在 HarvestCraftingItem 上对物品执行；精确的 Wild/Vivid/Primal/Sacred 生命之力消耗
  会从主背包与个人仓库合并扣除，并与物品在一个原子操作里提交。
  被拒绝时不消耗任何东西；尚不支持的势力、兑换、命运卡、宝石、腐化地图与品质附魔配方会在聊天里说明，并等待其前置系统。
  自动化测试覆盖原始行/消耗、每一种支持的操作族、付款、余额不足与存档回滚；制作台、窗口与请求已实机确认。
  见 [21](../docs/protocol/21-hideouts.md) 的 Horticrafting Station。
- 通用 3.29 地图，第一片（2026-10-27）：`MapKeyTierN` 物品会序列化其 MapKey 组件，`/item Map (Tier N)` 能生成一个；
  地图装置接受它们，并用 `MapKeys.dat` 取档位。原生的藏身处图鉴窗口（S?C 0x7b kind 0x2b）显示四个初始布局；
  点击节点（C?S 0x00e7）选中一个，Activate 打开它。每个角色都有尾声任务标记 `EndGameAvailable`，没有它客户端会拒绝
  图鉴激活（在 Kitava 给出它之前，只在藏身处与地图里下发，所以尾声的大门保持关闭；已离线核对）。
  2026-10-29 实机：选中 Estuary 打开了 Estuary；Cells 是默认项。
  基础怪物地图掉落使用本地近似。
  图鉴天赋树基础（2026-10-30）：三套已保存预设连同其节点列表与点数预算一起下发（S→C 0x3d）；
  C→S 0x3e/0x3f 分配/退还节点并保存；`/atlas_tree` 打开天赋树，`/atlas_points <n>` 设置开发用预算。
  实机：给了 30 点、分配三个「Map Drop Chance」节点后客户端更新了天赋树；预设被保存。
  预算现在来自图鉴已完成的地图：每张首次完成的地图一点，最多 100（#382），登录/进入区域时由已保存的完成集合推导，
  并在真实完成时发放。**2026-11-09 修正**：此前登录时会把全部地图标成已完成（上游"揭示整张图鉴"的便利），
  于是每个角色第一次上线就白拿 100 点、图鉴全是完成态；现在登录不再改动完成集合，新角色从 0 点开始，
  面板里各条 AtlasNode 仍由 0x1a3 的第一张掩码画出来（`AtlasProgress.FullMessage` 的 `OnAtlasRows`）。
  已被填满的老角色用 `/atlas_reset` 回到初始（现在会连点数与已分配的树一起清）。图鉴图谱本身还是客户端的，连通性交给它。
  选中的布局保存在角色存档里（一个 AtlasNode Id，`CharacterSaves.SelectedAtlasLayout`，用 `/atlas <布局>` 选择），
  Gateway 重启后会读回来而不是重置到起始节点（#382）。通用地图用其档位作为区域等级（档位 + 67）。
  图鉴进度（2026-11-01）：在地图里击杀其 WorldAreas 的 Boss（或任意传奇）会完成对应的 AtlasNode，按角色保存；
  S→C 0x1a4 与一行聊天提示只在第一次标注。进入区域时会在 0x21 之前把一份填好的 0x1a3 与 0x3d 预设一起下发，
  所以登录时不会把每张已保存的地图都播报一遍。`/atlas_complete <地图 | all>` 与 `/atlas_reset`（开发用）。
  实机：为 Chateau 发 0x1a4 后显示已完成并打开了 Springs 与 Arachnid Nest；击杀 Boss 的触发仍需实机核对。
  额外目标、虚空石、地图仓库摆放与官方掉落池仍未做。
  代码：`AtlasCatalog.cs`、`AtlasPassives.cs`、`AtlasProgress.cs`、`MapDevice.cs`、`ItemDrops.cs`、`InstanceItems.cs`。
  文档：[21](../docs/protocol/21-hideouts.md)、[re/013](../docs/re/013-atlas-progress.md)、[地图](../docs/game/maps.md)、[05](../docs/protocol/05-inventories-and-ui.md)。
- 回城卷轴与传送门技能（2.5 秒施法）：在城镇与藏身处之外会在玩家身边开一个传送门，替换掉上一个；从地图里开的通往藏身处，
  其他地方开的通往最后所在的城镇。
  该城镇或藏身处会得到一个回程传送门，把玩家送回同一副本里那个传送门的格子；在那里传送门会重新立起
  （`OpenTownPortal`、`SpawnTownPortals`、`OpenPortals`；服务端运行期间，2026-10-23 在测）。
  代码：Gateway（`UsePortalScroll`、`UseMapReturnPortal`）。
  文档：[21](../docs/protocol/21-hideouts.md) §5。
- 藏身处：每个角色都解锁藏身处（任务标记 HideoutUnlocked、CanEditHideout ……；赛季任务标记 UnlockedHideouts、
  HaveHideout、UnlockedMapDevice 在 Player 组件里，以位 id − 1 下发）并拥有 Lush Hideout；
  `/hideout`（C→S 0x16 命令 45）与世界面板的 Hideout 按钮（C→S 0x17a）把玩家送到那里（它自己的副本，seed 1）、
  位于其传送点旁；它的传送点（带 HideoutDoodad 数据）、仓库、Nessa 与地图装置都来自它的装饰数据
  （`scripts/re/atlas hideouts` → `data/hideouts.json`）；HUD 显示 Edit 按钮；
  地图装置可点击并打开其窗口（S→C 0x7b kind 5）；
  地图放进它的槽位后，Activate（C→S 0x00d8）会校验（只收地图与地图碎片，一张地图）、消耗它们，
  并开出六个传送门（MultiplexPortal + S→C 0x008f）通往该地图区域的一个副本，每个只能用一次（用掉的那个就消失）；
  `An Audience With The King` 是一片能单独打开虚无之核的碎片；在地图里有回藏身处的传送门、地图自己的怪物群（Maps.dat）
  与其 Boss 竞技场大门（传送）；Nessa 卖海滩地图；`/league_flag`、`/hideout_keys`（开发用，在聊天里应答）。
  代码：`HideoutCatalog.cs`、`InstanceHideouts.cs`、`WorldAreaHideouts.cs`、Gateway（`WarpToHideout`、
  `SpawnHideoutDoodads`、`UseMapDevice`、`ActivateMapDevice`、`UseMapPortal`）、`MapDevice.cs`。
  文档：[21](../docs/protocol/21-hideouts.md)。
- Heist Locker is placed in each hideout and opens its native window (0x7b kind 0x37).
  The native window handler and visible state were verified through the real client.
  Its placement supports the hideout editor; storage transfers were not added by this change.
  See [21](../docs/protocol/21-hideouts.md), Heist Locker.
- Horticrafting Station (2026-10-27): placed beside the ordinary bench in every selected hideout; interaction opens
  the native recipe browser (0x7b kind 0x39).
  C→S 0x274 carries the selected HarvestCraftOptions HASH16 (captured live: ReforgeFire 0xa3e5); S→C 0x276 carries the
  crafted item id and clears the panel's pending state, so Craft can be clicked repeatedly without reopening.
  Targeted/affinity reforges, augments and modifier conversions execute on the item in HarvestCraftingItem; the exact
  Wild/Vivid/Primal/Sacred lifeforce cost is split across the main inventory and personal stash and commits with the
  item atomically.
  Refusals spend nothing; unsupported influence, exchange, divination-card, gem, corrupted-map and quality-enchant
  recipes are reported in chat and wait for their prerequisite systems.
  Automated tests cover raw rows/costs, every supported operation family, payment, insufficient funds and save
  rollback; station, window and the request were confirmed live.
  See [21](../docs/protocol/21-hideouts.md), Horticrafting Station.
- Generic 3.29 maps, first slice (2026-10-27): `MapKeyTierN` items serialize their MapKey component and `/item Map
  (Tier N)` creates one; the map device accepts them and uses `MapKeys.dat` for the tier. The native hideout Atlas
  window (S?C 0x7b kind 0x2b) shows the four starting layouts; a node click (C?S 0x00e7) selects one and Activate opens
  it. Every character has the epilogue quest flag `EndGameAvailable`, without which the client refuses Atlas activation
  (until Kitava earns it, only in a hideout and the maps, so the Epilogue's gates stay shut; checked offline).
  Live 2026-10-29: selecting Estuary opened Estuary; Cells is the default.
  Basic monster map drops use a local approximation.
  Atlas passive tree foundation (2026-10-30): the three saved presets are sent with their node lists and point budget
  (S→C 0x3d); C→S 0x3e/0x3f allocate/refund nodes and save them; `/atlas_tree` opens the tree and
  `/atlas_points <n>` sets the dev budget. Live: with 30 points, three "Map Drop Chance" nodes allocated and the
  client updated the tree; the presets are saved. The budget now comes from the Atlas's completed maps: one point per
  map first completed, up to 100 (#382), derived at login/area entry from the saved completed set and awarded on a real
  completion. A fresh save earns its first map point on its first completion. The Atlas
  graph itself is still the client's, so connectivity is left to it.
  The selected layout is kept in the character save (an AtlasNode Id, `CharacterSaves.SelectedAtlasLayout`, chosen with
  `/atlas <layout>`), and a restarted Gateway reads it back instead of resetting to the starting node (#382). Generic
  maps use their tier as the area level (tier + 67).
  Atlas progress (2026-11-01): killing a map's WorldAreas boss (or any unique) in it completes its AtlasNode, saved per
  character; S→C 0x1a4 and one chat line mark it the first time only. Area entry sends one 0x1a3 (both masks still include all on-Atlas rows) with the 0x3d
  presets, before 0x21, so login does not announce every saved map. `/atlas_complete <map | all>` and `/atlas_reset` (dev).
  Live: 0x1a4 for Chateau showed it completed and opened Springs and Arachnid Nest; the boss-kill trigger still needs a
  real-client check. Bonus objectives, Voidstones, map stash placement and official drop pools remain open.
  Code: `AtlasCatalog.cs`, `AtlasPassives.cs`, `AtlasProgress.cs`, `MapDevice.cs`, `ItemDrops.cs`, `InstanceItems.cs`.
  Docs: [21](../docs/protocol/21-hideouts.md), [re/013](../docs/re/013-atlas-progress.md), [maps](../docs/game/maps.md), [05](../docs/protocol/05-inventories-and-ui.md).
- Portal Scroll and the Portal skill (its 2.5 s cast): outside towns and hideouts a portal opens beside the player,
  replacing the previous one; from a map it leads to the hideout, anywhere else to the last town.
  That town or hideout gets a portal back, which brings the player to the portal's cell of the same instance;
  there the portal stands again (`OpenTownPortal`, `SpawnTownPortals`, `OpenPortals`;
  while the server runs, being tested 2026-10-23).
  Code: Gateway (`UsePortalScroll`, `UseMapReturnPortal`).
  Docs: [21](../docs/protocol/21-hideouts.md) §5.
- Hideouts: every character has the hideout unlocked (quest flags HideoutUnlocked, CanEditHideout …;
  league quest flags UnlockedHideouts, HaveHideout, UnlockedMapDevice in the Player component, sent as bit id − 1) and
  owns the Lush Hideout; `/hideout` (C→S 0x16 command 45) and the World panel's Hideout button (C→S 0x17a) take the
  player there (its own instance, seed 1) next to its waypoint;
  its Waypoint (with HideoutDoodad data), Stash, Nessa and map device come from its decoration (`scripts/re/atlas
  hideouts` → `data/hideouts.json`); the HUD shows the Edit button;
  the map device can be clicked and opens its window (S→C 0x7b kind 5);
  maps go into its slots, Activate (C→S 0x00d8) checks them (maps and map fragments only, one map), consumes them and
  opens six portals (MultiplexPortal + S→C 0x008f) into one instance of the map's area, each used once (the one taken
  stays gone); `An Audience With The King` is a fragment that opens the Crux of Nothingness by itself; in the map, a
  portal back to the hideout, the map's own monster packs (Maps.dat) and its boss arena
  gates (teleports); Nessa sells Beach Maps; `/league_flag`, `/hideout_keys` (dev, answered in chat).
  Code: `HideoutCatalog.cs`, `InstanceHideouts.cs`, `WorldAreaHideouts.cs`, Gateway (`WarpToHideout`,
  `SpawnHideoutDoodads`, `UseMapDevice`, `ActivateMapDevice`, `UseMapPortal`), `MapDevice.cs`.
  Docs: [21](../docs/protocol/21-hideouts.md).

## 部分实现

- **传送点到达。** 进入藏身处时，出生点与移动原点都使用保存的精确传送点格子，登录与从其他区域返回也一样。
  实机中玩家与传送点记录用的是同一个保存格。

- 藏身处编辑能通过原生 C→S 0x5b 移动并旋转已安装的家具、传送点与 NPC。
  摆放按角色/模板持久化；运行时的交互与寻路阻挡跟随移动后的物件。
  所选藏身处的每个模板都可编辑：没有抓取行走地图的模板把格子校验交给客户端。
  家具移动会发送 0x161 以重建原生藏身处的模型碰撞，而不改变物件 id。
  服务端阻挡会在进入时按保存的位置重新登记。
  NPC 与所有功能性家具共用继承的 OT 碰撞足迹与交互净空。
  到达修正会在整个场景登记完成后运行；传送点与地面掉落仍可通行。
  九个功能性藏身处物件的实机重叠脱离与释放地面检查都通过了。
  玩家与地形或静止物件重叠时，会在编辑之后以及每个 tick 内被移到附近的空格。
  地图装置脚底逃逸、以及从它释放出来的旧位置走过，已在客户端核对。
  提取出的 AO 外壳半径覆盖小型与大型地图装置；持久化与净空有自动化覆盖。
  地图装置、Nessa 与仓库的移动/旋转以及重连持久化已在真实客户端核对。
  增删任意装饰、改变其外观仍未实现；开发用 `/statue` 命令会摆放一座按所选 MonsterVarieties 行保存的石制怪物雕像
  （2026-11-01，实机核对：无血条无名字、待机动画、离开再进入后仍在）。见
  [21](../docs/protocol/21-hideouts.md) 的 Monster Statue。
  见 [21](../docs/protocol/21-hideouts.md) 的 Moving installed furniture and NPCs。
- 普通制作台（2026-10-25，有自动化覆盖；实机核对待做）：全部 765 个有效配方及其 130 个配方标记都可用。
  精确词缀、类别掩码、孔位/连接配方、药剂附魔、替换/移除与稀有权重骰都已提供。
  付款会合并主背包与个人仓库（含多个页签/多种通货），并与物品在一个原子操作里提交。
  替换已有工艺词缀（Craft 按钮改名为「Replace」，一个普通请求）只收该配方的费用；装了多工艺时只替换冲突的那一条。
  Ctrl + 点击（C→S 0xe9）把背包物品移到空的制作台槽位，并把制作台上的物品移回。
  测试覆盖每个配方/类别、带与不带多工艺的替换，以及付款失败/保存的情形。
  见 [21](../docs/protocol/21-hideouts.md) 的 Ordinary bench recipes and payment。
- 制作台可负担性修正：原生制作台计数器不包含普通仓库背包。
  现在另有一个独立的、不可花费的通货计数视图，在打开时公布当前个人仓库总量并在制作后刷新，
  而主背包里的实际通货只计一次。
  付款仍然只使用真实物品；大额总量保留完整数值。
  自动化检查通过；修正后的实机界面核对待做。
  见 [21](../docs/protocol/21-hideouts.md) 的 Bench affordability。
- Helena 的摆放与藏身处选择器入口（2026-10-25，待客户端确认）：Helena 站在 Niko 右侧，
  Select Hideout 会打开原生窗口。
  所有导出的个人模板都已拥有；选择、转移与持久化已实现。
  见 [21](../docs/protocol/21-hideouts.md)。
- 藏身处选择器崩溃修复（2026-10-25，待客户端确认）：进入区域时用 S→C 0x1f6 初始化剧情解锁位集，早于 Helena 打开选择器。
  缺失数据曾导致已确认的客户端访问冲突（0x142174267）。
  见 [21](../docs/protocol/21-hideouts.md)。
- **藏身处里的 Niko 与 Kirac（2026-10-21 实机核对）：** Niko 在 Nessa 右侧，Kirac 在她左侧。
  Kirac 的藏身处行没有自己的对话，用的是主 Kirac 的（`NpcCatalog.TalkSource`）；Purchase Items 卖地图。
  Niko 的夺宝选项需要任务标记 ThisIsMyHideout（在藏身处与矿坑营地会下发）与赛季任务标记 68 DelveEncounterComplete
  （每个角色都有）：NPCTalk 的第 15-18 列是赛季任务标记 id。
  见 [21](../docs/protocol/21-hideouts.md)。
- **夺宝（Delve），最初的几步（2026-10-21 实机核对）：** View Subterranean Chart 打开图表（S→C 0x7b kind 0x1c，
  取自客户端的界面表）；其数据请求 C→S 0x23f 用 S→C 0x240 应答，其中一条记录绘制出第一个矿坑区段
  （其余字段正用 `/delve_chart` 逐步弄明白）。
  Visit Mine Encampment 前往 Delve_Hub，那里有它格子的传送点（仍显示为未激活：没有 0x4c 位）、Niko、仓库与公会仓库。
  世界面板的特殊图钉全部显示（为每个角色下发赛季任务标记，迷宫的传送点始终激活）。隐藏藏身处、兽栏、矿坑营地、
  禁咒圣所、Kingsmarch、盗贼港与守护者修道院的图钉会通往各自的枢纽，枢纽里有可点击的回程传送点、仓库、公会仓库
  与各自的 NPC（Einhar、Divinia、Kingsmarch 村民、夺宝船员、Ailith；Einhar 也出现在每个藏身处）。
  枢纽传送点保持不亮（客户端的区域类型规则）；The Sovereign（Deepwater）还没有布局。
  枢纽上方的入口现在能到达 Delve_Main 的一个初始矿坑区段，其返回门通往枢纽。
  原生进入、普通行走与返回都已核对；见下面的初始地形块。
  通用的矿坑推进仍未实现；第一次任务行程见下。
  见 [16](../docs/protocol/16-waypoints.md) §4。
  见 [21](../docs/protocol/21-hideouts.md)。
- **夺宝的硫磺与爬行者（2026-10-29 实机核对）。**
  - 角色的 Voltaxic Sulphite 会被保存，在进入矿坑营地或夺宝区域时下发（S→C 0x228），并由地下图表的量表显示。
    `/sulphite <n>` 设置它。
    完整资源快照在进入与打开图表后跟随 0x226 量表更新。
    两次数值不变的更新会让暂停的图表稳定下来并使其标签位置失效；数字会跟随填充边缘。
    第三次更新会抑制原生的失效。填充与标签都在没有改动客户端的情况下核对过。
  - HUD 量表只在 WorldAreas 类型 27（矿坑区域）显示；营地在客户端数据里是类型 15，所以那里的量表属于图表。
  - `/delve_crawler [x y 角度]` 在营地或初始矿坑区段创建静止的爬行者，或移动并转向它。
    它使用原生的 DelveMapViewer 交互类型与爬行者模型，悬停会高亮，真实鼠标点击会打开图表；
    漫游版本的 DelveRobot 序列化器仍是另一套。
    重复创建是幂等的；客户端/服务端阻挡跟随移动，而图表的使用会检查走近范围。
    挂载的轮子网格与点亮的火把使用原体的骨骼；显式循环待机会让引擎运转。
    用户重启客户端后确认了引擎动画。
  - Voltaxic 发生器控制台及其线缆占据其原生枢纽房间位置。
    走近点击会打开原生的夺宝升级页签，含资源、升级价格与购买按钮。
    它共用的窗口需要 AreaInstance 里的永久成就集 2；空列表会隐藏它的所有页签。
    蓝晶、升级等级与照明弹/炸药数量按账号/赛季持久化，具备原子扣费、上限与回滚。
    服务端事务有自动化覆盖；实机购买仍未验证，因为当前余额为零。
    装备属性在夺宝区域会下发；黑暗伤害与消耗品使用仍未实现。
  - 第一个矿坑区域接收一个 1×2 的夺宝地形块，带一个入口与一个蓝晶任务房间。
    Delve_Main 里复制出来的枢纽被排除在到达选择之外；真正矿坑侧的过渡是回到营地。
    真实客户端生成了该区段、接受在其中行走，并经由那道门返回。
    几何来自客户端行走地图抓取。
    图表的原生路线请求已解析；不支持的路线会报告不可用且不消耗硫磺。
    第一次任务行程现在会解析预置轨道、等待护卫、保持恒定行进速度、移动其挂载光源并在任务房间停下。
    硫磺按累计距离扣除，终点总额精确为 24。
    运动循环与怪物群创建分离，带 50 ms 缓冲更新、锁定的前向朝向与停止/恢复的滞回。
    点亮的灯每 30 个路径格出现，原生线缆把它们连起来；爬行者身后的照明已实机核对。
    轨迹目前属于本次行程，重进时不会恢复。
    房间生成点会揭示受支持的 Weta/蜘蛛任务怪群；通用生物群系池与遭遇脚本仍是部分实现。
    原生检查确认了轨道行进、等待护卫、移动照明与精确的 300 → 276 扣费。
    一次普通 Leap Slam 击杀了 18 只沿途怪物并掉落。调试用的服务端传送协助了护卫；完整的徒步行走仍未验证。
    路线要从枢纽图表启动；矿坑图表目前只显示地形预览。
    爬行者现在用原生 Positioned 角度（atan2(dx,-dy)）朝向当前轨道切线，停止时保留朝向。
    原版的夺宝传送点美术标记终点。爬行者到达且其生成的遭遇全部死亡后，一次奖励会在它周围散落普通可拾取的世界物品。
    基础数量 15 到 20；第一个实体深度（2）加 10% 数量与 5% 稀有度提升。每深一层加同样的增量，
    上限可配置为 250 件。已完成的最高深度会持久化，且不会削减其他角色的进度；重复同深度不会增加其加成。
    完整的图谱持久化与黑暗仍未完成。
    原生验收确认了终点垫、停止且朝前的爬行者、精确 24 点硫磺扣费、一次 22 件的胜利奖励，以及正常拾取进已保存的背包
    （协助护卫）。
  - 爬行者每次进入都站在矿坑入口旁的轨道上 (408, 310)、朝向 90°；仓库与公会仓库在 (355, 295) 与 (344, 295)，朝向 180°。
    狮眼守望的这两处在 (327, 270) 与 (317, 270)。
    萨恩营地的在 (295, 423) 与 (285, 418)，朝向 28°（2026-11-04）。
    这些位置都是用户实机摆好的（2026-10-30）。
  - `/stash_place` 与 `/guild_stash_place <x> <y> <角度>` 会移动任意区域的仓库与公会仓库，直到玩家离开该区域，用于找位置。
  - 见 [21](../docs/protocol/21-hideouts.md) 的 "Delve"。
- **藏身处制作台的摆放（2026-10-25）。** 个人藏身处选择现在会处理原生的 0x254 请求，按角色保存所选 WorldAreas Id
  并立即转移。Auth 在登录时使用保存的选择；世界面板的传送、`/hideout` 与地图回程传送门都使用同一个家。
  功能性家具与全部四个 NPC 都摆放在所选模板的地面上。
  选择解码、持久化与摆放有自动化覆盖；新的转移与重连路径待用户在客户端核对。
- 标准制作台生成在藏身处公会仓库的左侧，使用现有装饰的模型与可行走的摆放（摆放已在真实客户端确认）。
  现在点击它会走进交互范围并发送 0x7b kind 0x33、type 0，以打开普通制作窗口。
  自动化检查覆盖报文格式；实机开窗核对待做。
  配方解锁与应用工艺现在已实现；见 protocol/21-hideouts.md。
- Hideout editing moves and rotates installed furniture, the waypoint and NPCs through native C→S 0x5b.
  Placements persist per character/template; runtime interactions and path blockers follow the moved objects.
  Every template of the selected hideout is editable: one without a captured walk map leaves cell checks to the client.
  Furniture moves send 0x161 to rebuild native hideout model collision without changing object ids.
  Server blockers are registered again at saved positions on entry.
  NPCs and all functional furniture share inherited OT collision footprints and interaction clearance.
  Arrival correction runs after the full scene is registered; waypoints and ground loot remain passable.
  Live overlap escape and released-floor checks passed for all nine functional hideout objects.
  A player overlapping terrain or stationary objects is moved to a nearby clear cell after edits and during ticks.
  The map-device underfoot escape and walking through its released old position were verified in the client.
  Extracted AO hull radii cover small and large map devices; persistence and clearance have automated coverage.
  Map-device, Nessa and stash movement/rotation and reconnect persistence were verified in the real client.
  Adding/removing arbitrary decorations and changing their appearance remain unimplemented; the dev `/statue`
  command places a saved stone Monster Statue of a chosen MonsterVarieties row (2026-11-01, live client verified:
  no life bar or name, idle animation, survives leaving and re-entering). See
  [21](../docs/protocol/21-hideouts.md), Monster Statue.
  See [21](../docs/protocol/21-hideouts.md), Moving installed furniture and NPCs.
- Ordinary crafting bench (2026-10-25, automated coverage; live client verification pending): all 765 active recipes
  and their 130 recipe flags are available.
  Exact modifiers, class masks, socket/link recipes, flask enchantments, replacement/removal and rare-affix rerolls
  are served.
  Payment combines main inventory and personal stash, including multiple tabs/currencies, and commits with the item
  atomically.
  Replacing an existing crafted mod (the Craft button relabelled "Replace", an ordinary request) costs only the
  recipe; with the multimod only the conflicting craft is replaced.
  Ctrl + click (C→S 0xe9) moves an inventory item onto the empty bench slot and the bench's item back.
  Tests cover each recipe/class, replacement with and without the multimod, and failed-payment/save cases.
  See [21](../docs/protocol/21-hideouts.md), Ordinary bench recipes and payment.
- Bench affordability correction: native bench counters exclude ordinary stash inventories.
  A separate, nonspendable currency-count view now publishes current personal stash totals on opening and refreshes
  after crafting, while actual main-inventory currency is counted once.
  Payment still uses only real items; large totals retain their full value.
  Automated checks pass; corrected live UI verification is pending.
  See [21](../docs/protocol/21-hideouts.md), Bench affordability.
- Helena placement and hideout picker entry point (2026-10-25, awaiting client confirmation): Helena stands
  screen-right of Niko and Select Hideout opens the native window.
  All exported personal templates are owned; selection, transfer and persistence are implemented.
  See [21](../docs/protocol/21-hideouts.md).
- Hideout picker crash fix (2026-10-25, awaiting client confirmation): initialize the campaign unlock bitset with S→C
  0x1f6 on area entry, before Helena can open the picker.
  Missing data caused the confirmed client access violation at 0x142174267.
  See [21](../docs/protocol/21-hideouts.md).
- **Niko and Kirac in the hideout (verified live, 2026-10-21):** Niko to Nessa's right, Kirac to her left.
  Faustus uses his native hideout NPC alongside them in every template, with the existing dialogue and saved editor placement.
  Kirac's hideout row has no dialogue of its own and uses the main Kirac's (`NpcCatalog.TalkSource`);
  Purchase Items sells maps.
  Niko's Delve options need quest flag ThisIsMyHideout (sent in hideouts and the Mine Encampment) and league quest
  flag 68 DelveEncounterComplete (every character): NPCTalk columns 15-18 are league quest flag ids.
  See [21](../docs/protocol/21-hideouts.md).
- **Delve, first steps (verified live, 2026-10-21):** View Subterranean Chart opens the chart (S→C 0x7b kind 0x1c,
  from the client's screen table); its data request C→S 0x23f is answered with S→C 0x240, and one entry draws the
  first mine section (the other fields are being worked out with `/delve_chart`).
  Visit Mine Encampment goes to Delve_Hub, which has its tile's waypoint (still drawn inactive: no 0x4c bit), Niko,
  the stash and the guild stash.
  The World panel's special pins are all shown (league quest flags for every character, the Labyrinth's waypoint
  always activated). The Hideout, Menagerie, Mine Encampment, Forbidden Sanctum, Kingsmarch, Rogue Harbour and
  Monastery of the Keepers pins lead to their hubs, which have a clickable waypoint back, a stash, a guild stash and
  their NPCs (Einhar, Divinia, the Kingsmarch villagers, the Heist crew, Ailith; Einhar also in every hideout).
  The hub waypoints stay unlit (client area-type rule); The Sovereign (Deepwater) has no layout yet.
  The upper hub entrance now reaches an initial Delve_Main mine section and its return gate leads back to the hub.
  Native entry, ordinary walking and return were verified; see the initial terrain block below.
  Account checkpoint progression is implemented; physical terrain and encounter limitations are described below.
  See [16](../docs/protocol/16-waypoints.md) §4.
  See [21](../docs/protocol/21-hideouts.md).
- **Delve sulphite and the Crawler (verified live, 2026-10-29).**
  - The character's Voltaxic Sulphite is saved, sent on entering the Mine Encampment or a Delve area (S→C 0x228), and
    shown by the Subterranean Chart's gauge. `/sulphite <n>` sets it.
    Sulphite scarabs guarantee three shared, single-use map deposits with Niko; native area-level rewards scale with
    map quantity and the owner's legacy scarab bonus, cap at equipment capacity, and survive reentry/module reload.
    Fumes adds mine guards and a native enrage visual with +50% item quantity on affected monsters; its radius,
    guard placement and exact official combat tuning remain server approximations.
    Entry sends a full snapshot without a zero-gain event; chart opening refreshes its gauge with 0x226.
    Two unchanged-value updates settle the paused chart and invalidate its label position; the number follows the fill edge.
    A third update suppresses the native invalidation. Both the fill and label were verified without client changes.
  - The native HUD gauge is permanent in mine areas (type 27). Niko maps now keep its transient display visible
    with resource refreshes at the real saved balance, without gain popups. Native checks confirmed visibility
    across waiting and a deposit credit up to capacity. Wild Niko follows terrain height instead of a fixed Z.
    The encampment (type 15) still shows sulphite in its chart.
  - `/delve_crawler [x y degrees]` creates the stationary Crawler in the encampment or initial mine section,
    or moves and turns it.
    While parked it uses the native DelveMapViewer interaction type with the Crawler model and robot map icon,
    highlights on hover and opens the chart on a real mouse click. Moving carts use DelveRobot with native turning
    wheels; separate stationary chart pads carry the circular start/destination markers and original floor model.
    Repeated creation is idempotent; client/server blockers follow relocation and chart use checks walk-up range.
    Hub wheel meshes and the powered torch use the original body bones; explicit looping idle runs the engine.
    The user confirmed the engine animation after restarting the client.
  - The Voltaxic Generator console and its cables occupy their native hub-room positions.
    Walking up and clicking opens the native Delve upgrades tab with resources, upgrade prices and purchase buttons.
    Its shared window requires permanent achievement set 2 in AreaInstance; an empty list hides all its tabs.
    Azurite, upgrade levels and flare/dynamite counts persist per account/league with atomic costs, caps and rollback.
    Server transactions have automated coverage; live purchases remain unverified because the current balance is zero.
    Equipment stats are sent in Delve areas; darkness damage and consumable use remain unimplemented.
  - Delve chart routes now persist completed points/edges and the last successful checkpoint per account/league.
    The entrance layout is used only for the first trip; later edges connect two mine checkpoint rooms in the selected
    direction. Later trips generate fresh native Mines windows with new seeds, varied rooms and authored rail shapes;
    account history rejects repeated geometry/routes and prevents minimap reuse. Other biomes remain unfinished. Successful destinations expand
    chart blocks down and sideways. A complete neighbour halo preserves the native five-cell fog fade across block corners.
    Each confirmation reaches its selected node, then the Crawler parks.
    Intermediate corridor cells are part of that journey and never become separate checkpoint stops.
    Arrival starts three defence waves over five unpaused seconds, using the route's monster pool. Only after every
    wave has spawned and all destination defenders are dead does the point save and refresh the chart.
    Tunnel stragglers do not block completion; the next direction requires a new selection.
    Death, leaving or module reload discards the unfinished trip, retains spent sulphite and restores the saved point.
    The Crawler follows authored rails, waits at 70 cells (resumes below 60), and speeds up along the rails when led.
    Departure uses the visible native circle boundary, with no travel debit while waiting.
    Completion keeps a permanent endpoint light and plays the native checkpoint activation animation with its success sound.
    The Crawler's own forward axis leads each rail segment; rounded cells do not change its heading, and only clear
    sleeve connections are smoothed; each generated route stays on player-walkable cells. Lamps cover the exact start and finish.
    Wheels, engine, torch, checkpoint pads, 30-cell lamps and cables remain in place.
    Supported Weta/Spider packs
    scale with depth (up to 100 per authored pack); levels rise from 34 at depth 1 to 86 at depth 150, then stay 86.
    Kill rewards and 15–20 base completion items gain depth IIQ/IIR; the existing configurable item cap remains.
    Newly opened destinations receive one completion reward; revisiting opened points cannot farm free rewards.
    Native walking/Kinetic Blast completed a route with exact 24 sulphite debit and one reward. Native checks also
    confirmed cross-character checkpoint sharing and unfinished-trip death reset.
    DelveOwner enables chart continuity; Niko and Crawler center it on the last saved account checkpoint.
    Mine windows follow chart-cell origins so the underground position arrow follows the occupied checkpoint.
    General biome/encounter parity, darkness damage and consumable use remain partial.
  - The Crawler stands on the rails by the mine entrance at (408, 310), facing 90°, on every entry; the stash and the
    guild stash are at (355, 295) and (344, 295), facing 180°. Lioneye's Watch has them at (327, 270) and (317, 270).
    The Sarn Encampment has them at (295, 423) and (285, 418), facing 28° (2026-11-04).
    The user placed all of these live (2026-10-30).
  - `/stash_place` and `/guild_stash_place <x> <y> <degrees>` move the stash and the guild stash of any area until the
    player leaves it, to find their places.
  - See [21](../docs/protocol/21-hideouts.md) "Delve".
- **Hideout crafting bench placement (2026-10-25).** Personal hideout selection now handles the native 0x254 request,
  saves the selected WorldAreas Id per character and transfers immediately.
  Auth uses the saved choice on login; world-panel travel, `/hideout` and map return portals use the same home.
  Functional furniture and all four NPCs are placed on the selected template's floor.
  Selection decoding, persistence and placement have automated coverage;
  the new transfer and reconnect path awaits user verification in the client.
- The standard bench spawns to the left of the guild stash in hideouts, using the existing decoration's model and a
  walkable placement (placement confirmed in the real client).
  Clicking it now walks into interaction range and sends 0x7b kind 0x33, type 0 to open the ordinary crafting window.
  Automated checks cover the wire format; a live window-opening check is pending.
  Recipe unlocks and applying crafts are now implemented; see protocol/21-hideouts.md.

## 尚未实现

- 藏身处：增删装饰树木/帐篷、改变装饰外观尚未实现。
  个人模板选择、保存家宅的登录，以及功能性家具的摆放已实现（[21](../docs/protocol/21-hideouts.md)）。
- 地图装置：没有行走地图抓取的地图会把玩家丢在任意格子（海滩有抓取）；竞技场大门落在其配对物旁边而不是传送垫上；
  传送门看起来像玩家传送门；已打开的地图在 Gateway 重启后丢失
  （[21](../docs/protocol/21-hideouts.md) §5 的 "Open"）。
