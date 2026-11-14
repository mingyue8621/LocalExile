# 路线图：藏身处、制作台与地图

属于 [路线图索引](../Roadmap.md)；这个区域已经能用的功能见 [status/hideouts-and-maps.md](../status/hideouts-and-maps.md)。
条目沿用路线图的优先级分组，同一组内大致按优先级排列。

## 1. 体验优化

园艺台的摆放、原生配方浏览与物品配方的执行路径（定向/亲和重铸、增幅、词缀转换、原子化的生命之力付款）都已实现；
见状态文档与 protocol/21-hideouts.md。
剩下的丰收配方（势力、堆叠兑换、命运卡、宝石透镜、腐化地图与品质附魔）在等各自的前置系统。

### 切换藏身处模板

- **已知：** Helena 现在站在 Niko 的画面右侧，并打开 HideoutSelectionWindow（0x7b kind 0x22）。
  已拥有的列表里包含默认的 Lush Hideout。
  [21](../docs/protocol/21-hideouts.md)。
- **入手：** 逆向出选择请求，持久化所选模板，并让 /hideout、玩家按键与传送门都用它。

普通制作台的有效配方、解锁以及背包/个人仓库付款都已实现（见状态文档与 protocol/21-hideouts.md）。
被停用的遗留配方、地图装置配方与其他专用制作台与普通制作台是分开的。

## 2. 世界

### 地图与图鉴（3.29）

- **已知：** 3.29 的规则与 20 个通用地图基底（`MapKeyTier1` …… `MapKeyTier16` 以及四个特殊的）在
  [game/maps.md](../docs/game/maps.md) 里。
  通用 `MapKeyTierN` 物品、`MapKeys.dat` 档位、MapKey 组件、默认图鉴布局与基础地图掉落都已实现；见状态文档。
  原生图鉴面板/进度与地图仓库尚未实现。
- **入手：** 图鉴的保存进度（已完成节点、相邻解锁、点数发放）与图鉴图谱校验；虚空石；地图仓库页签的布局（18 A5）；
  官方掉落池。
- **完成标准：** 地图能按 3.29 的方式掉落、存储与运行，图鉴能显示并保存进度。

### 藏身处：装饰、编辑与地图装置

- **已知：** Lush Hideout 可用：传送、传送点、仓库、Nessa、Edit 按钮与地图装置：槽位、
  Activate 以及六个通往地图的传送门（[21](../docs/protocol/21-hideouts.md) §5）。
  它的装饰数据（`data/hideouts.json`）列出每个装饰件的类型与模型。
  夺宝柜已摆放并打开其原生存储窗口；契约/蓝图转移需要另行处理。
  移动/旋转已安装的家具与 NPC 现在通过 C→S 0x5b 按角色/模板保存。
  地图装置、Nessa 与仓库已实机核对，包括重连后位置被恢复。
  其他已安装物件使用同一套处理逻辑；它们在编辑器里的具体交互需要人工核对。
  装置编辑通过 0x161 重建原生可行走性，并在保存位置替换服务端足迹。
  小型装置脚底逃逸与走过它原位，已在真实客户端核对。
  NPC 与功能性家具现在通过同一份注册表使用继承的 OT 足迹与静态模型外壳。
  传送点与掉落保持可通行。大型时装模型与贴墙布局要单独核对。
  所有受支持的 NPC 类型的保存/重进持久化与碰撞逃逸都有自动化覆盖。
  开发用 `/statue` 命令会在玩家处创建一个任意 MonsterVarieties 行的石制怪物雕像，并按角色/模板保存；
  C→S 0x0209 以同样方式替换被指向的藏身处怪物。雕像会播放待机动作，进入时重建。
  见 [21](../docs/protocol/21-hideouts.md) 的 Monster Statue。
- **入手：** 把其余装饰做出来（`HideoutObject`，各类型的组件来自 data/object-types.json）；编辑模式的摆放/移除与外观报文；
  地图：出生点、回程传送门、碎片与装置配方以及地图词缀；
  `An Audience With The King` 已经能打开虚无之核（其他碎片仍然无效）。
  个人模板选择与登录到所选家宅已实现（见状态文档）。
- **完成标准：** 藏身处看起来与真实版本一致，能编辑并保存，地图能从它的装置里开。
- **Known:** the Lush Hideout works: travel, waypoint, stash, Nessa, the Edit button and the map device: slots,
  Activate and six portals into the map ([21](../docs/protocol/21-hideouts.md) §5).
  Its decoration (`data/hideouts.json`) lists every doodad with its type and model.
  The Heist Locker is placed and opens its native storage window; contract/blueprint transfers need separate work.
  Moving/rotating installed furniture and NPCs now saves per character/template through C→S 0x5b.
  The map device, Nessa and stash were checked live, including restored positions after reconnect.
  Other installed objects use the same handler; their individual editor interactions need manual verification.
  Device edits rebuild native walkability through 0x161 and replace the server footprint at the saved position.
  Small-device underfoot escape and walking across its former position are verified in the real client.
  NPCs and functional furniture now use inherited OT footprints and static-model hulls through one registry.
  Waypoints and loot remain passable. Check large MTX models and wall-adjacent layouts separately.
  Save/reentry persistence and collision escape for all supported NPC types have automated coverage.
  The dev `/statue` command creates a stone Monster Statue of any MonsterVarieties row at the player and
  saves it per character/template; C→S 0x0209 replaces a targeted hideout monster the same way. The
  statues play their idle and are recreated on entry. See [21](../docs/protocol/21-hideouts.md), Monster Statue.
- **Start:** create the rest of the decorations (`HideoutObject`, each type's components from
  data/object-types.json); the Edit mode's placement/removal and appearance messages;
  maps: the arrival point, a return portal, fragments and device recipes and map mods;
  `An Audience With The King` opens the Crux; Simulacrum has arena selection and a wave controller.
  Simulacrum's native reward/fog presentation, specialised boss behaviour and full clear need verification.
  Personal template selection and login to the selected home are implemented (see Status).
- **Done when:** the hideout looks like the real one, can be edited and saved, and maps can be run from its device.
