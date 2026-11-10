# 状态：NPC、商店与聊天

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/npcs-and-chat.md)。

## 已实现（已用真实客户端验证）

- 普通商人/Kirac 的库存会保留；藏身处里的 Lilly 会为新角色提供服务：[17](../docs/protocol/17-npcs.md)。

- 聊天命令 `/commands`（可写 `/com`；分页显示，`/com <名称>` 只看一条）、`/raw <十六进制>`、`/set_stat`、
  `/set_speed`、`/set_level <1 到 100>`（保存到账号，就地生效）、`/add_passive_points <n>`、`/add_passive <id>`
  （以聊天行回复）。
  代码：`Program.Commands.cs`（`Commands`）、`ChatCommands.cs`。
  文档：[10](../docs/protocol/10-chat-and-commands.md)。
- 聊天行与频道（`Chat` 模块）：本地行发给该副本里的玩家，`#` 全局、`$` 交易、`@名字` 密语则经由聊天服务器，
  所以不同 Gateway 上的玩家也能对话。
  每个 Gateway 自带一个本地聊天服务器；某个 mod（`UseChatServer`：LocalExile-Mods 里的 `globalchat`）会把它上面的玩家
  迁到另一台服务器，停用该 mod 则迁回。该服务器故障时会在聊天里告知玩家并留在本地，一分钟后再试（换区域时）。
  `src/GameServer.Chat` 就是要托管的那个服务器；它不存任何东西。
  在背包或仓库里 Ctrl+Alt+点击链接的物品会在聊天行里显示为链接（C→S 0x12，S→C 0x17）。
  全局与交易频道的加入请求（C→S 0x134，进入区域后以及 `/global N`、`/trade N` 时）用 S→C 0x135 应答，
  并回复「You have joined global chat channel N.」，所以它们不会超时，Global 与 Trade 页签可用。
  客户端每个会话只加入一次，因此每次进入区域都会为已加入的频道重发 0x135。
  队伍与公会的聊天行不下发。聊天服务器上已存在的同名玩家会在那里被编号（`Name_1`）；这不能证明任何身份。
  代码：`Modules/Core/Player/Chat`、`src/GameServer.Core/Chat`。
  文档：[10](../docs/protocol/10-chat-and-commands.md) §6。
- 每个剧情城镇的 NPC（第一到第十幕、奥瑞亚、卡鲁海岸）：按各城镇房间自己的 NPC 行摆放，摆放方式与客户端一致
  （用它对每个城镇的行走地图抓取结果核对过），带名字、小地图图标、模型与朝向。
  它们的碰撞足迹现在来自继承的 OT 元数据，包含后续幕里的 NPC Id 别名。
  共用的路线与重叠修正覆盖城镇和藏身处里生成的 NPC。
  原生行走验证过狮眼守望的三个 NPC 和两个仓库；公会仓库在本地可用时会带上原生的 HaveGuild 访问标记，
  因此客户端侧的碰撞与服务端一致。
  赛季 NPC 与后续任务阶段尚未摆放（[17](../docs/protocol/17-npcs.md) §2）。
  点击会走过去并打开对话窗口（S→C 0x84）；对话选项可播放（0x82 → 0x84 + 序号）；
  Continue 与 Goodbye（C→S 0x85）会有应答；Purchase Items 打开商店（S→C 0xc0 manager 1 + 0x8c），
  购买（C→S 0x88）按物品各自的价格；Sell Items：出售窗口（S→C 0xb8）接受掉落物、Ctrl + 点击（第一个空格）与
  Shift + 点击，Accept 支付报价里的回报，Cancel 归还物品，之后玩家可以继续走动；
  Respecialisation 打开角色的天赋树（0x7b kind 0x5f），其中 Apply（C→S 0x36）与 Refund All（0x37）按客户端给出的
  金币价格退还天赋；普通天赋树的 Apply（0x36 带标记 0）消耗已保存的退点数；
  Identify Items 免费鉴定背包，在聊天里打印「N Items identified」（S→C 0x17）并置灰该选项；`/npc`、`/npcs`、`/pos`。
  对话选项现在会强制校验其必需/排除的任务标记，并保存自己的开始/完成标记。
  代码：`InstanceNpcs.cs`、`NpcCatalog.cs`、`TownNpcs.cs`、Gateway（`SpawnTownNpcs`、`TalkToNpc`）。
  文档：[17](../docs/protocol/17-npcs.md)。
- Chat commands `/commands` (`/com`; paged, `/com <name>` for one), `/raw <hex>`, `/set_stat`, `/set_speed`,
  `/set_level <1 to 100>` (saved to the account, sent in place), `/add_passive_points <n>`, `/add_passive <id>`
  (replies as chat lines).
  Code: `Program.Commands.cs` (`Commands`), `ChatCommands.cs`.
  Docs: [10](../docs/protocol/10-chat-and-commands.md).
- Chat lines and channels (the `Chat` module): local lines go to the players of the instance, `#` global, `$` trade and
  `@Name` whispers go through a chat server, so players of separate Gateways can talk.
  Each Gateway runs a local chat server; a mod (`UseChatServer`: `globalchat` in LocalExile-Mods) moves its players to another server, and
  disabling the mod moves them back. When that server fails a player is told in chat and stays local, trying again at an area change a minute later.
  `src/GameServer.Chat` is the server to host; it stores nothing.
  An item linked with Ctrl+Alt+click from an inventory or the stash shows as a link in the line (C→S 0x12, S→C 0x17).
  The global and trade channel joins (C→S 0x134, after entering an area and for `/global N` and `/trade N`) are
  answered with S→C 0x135 and "You have joined global chat channel N.", so they do not time out and the Global and
  Trade tabs work.
  The client joins once per session, so every area entry sends 0x135 for the channels already joined.
  Party and guild lines are not sent. A name already on a chat server gets a number there (`Name_1`); nothing proves a name.
  Code: `Modules/Core/Player/Chat`, `src/GameServer.Core/Chat`.
  Docs: [10](../docs/protocol/10-chat-and-commands.md) §6.
- NPCs in every campaign town (acts 1-10, Oriath, the Karui Shores): each town room's own NPC lines, placed as the
  client places the room (checked against its walk-map capture of every town), with name, minimap icon, model and
  facing.
  Their collision footprints now come from inherited OT metadata, including later-act NPC Id aliases.
  The shared routing and overlap correction covers spawned NPCs in towns and hideouts.
  Native walking respected the three NPCs and both stashes in Lioneye's Watch; the guild stash receives
  its native HaveGuild access flag when locally available, so its client collision matches the server.
  League NPCs and later quest stages are not placed ([17](../docs/protocol/17-npcs.md) §2).
  Niko also stands in Act 4 Highgate at the player's confirmed position and facing.
  Clicking walks to them and opens the dialogue window (S→C 0x84); talk options play (0x82 → 0x84 + index);
  Continue and Goodbye (C→S 0x85) are answered; Purchase Items opens the shop (S→C 0xc0 manager 1 + 0x8c) and buying
  (C→S 0x88) uses item-specific prices; Sell Items: the sell window (S→C 0xb8) takes drops, Ctrl + click (first free
  cell) and Shift + click, Accept pays the quoted reward and Cancel returns them, and the player can walk
  afterwards; Respecialisation opens the character's passive tree (0x7b kind 0x5f), where Apply refunds (C→S 0x36) and
  Refund All (0x37) refund passives for the client's gold price;
  the normal tree's Apply refunds (0x36 with flag 0) spend saved refund points;
  Identify Items identifies the inventory for free, prints "N Items identified" in chat (S→C 0x17) and grays the
  option; `/npc`, `/npcs`, `/pos`.
  Dialogue options now enforce their required/excluded quest flags and save their start/finish flags.
  Code: `InstanceNpcs.cs`, `NpcCatalog.cs`, `TownNpcs.cs`, Gateway (`SpawnTownNpcs`, `TalkToNpc`).
  Docs: [17](../docs/protocol/17-npcs.md).

- **玩家之间的交易**（同一副本内，两个客户端，2026-10-28）：请求会显示发起方的等待框和对面的通知，Accept 让双方
  都打开窗口，报价的物品在两边都出现，Accept 会同时把接受方的报价在双方界面上置灰，双方都接受后在一个事务里
  交换物品、同时写两份存档。取消、拒绝、掉线或离开区域都会归还所有已报价的物品。
  见 [28](../docs/protocol/28-player-trade.md)。
- **组队**（两个客户端，2026-11-05）：「Invite to Party」，好友面板里也能发起，能触达任意区域的玩家；Accept 后两人
  入队，双方客户端都显示，并且跨区域保持。
  队员跟随另一人进入区域时会加入对方的副本。
  组队加成遵循客户端的全部规则：每个额外队员给怪物 +50% 普通 / +60% 魔法 / +80% 稀有 / +100% 传奇生命，
  外加 23% 通货掉落，以及它的图标（S→C 0x231）。**传奇（含 BOSS）同样吃这条生命加成**（2026-11-10 恢复；
  0.8.26 曾为「玛拉凯打不死」把传奇排除，那是绕过症状而不是修机制——玛拉凯现在按官方的 70% 钻地 + 三颗心脏打）。
  机器人是受控的玩家连接，因此也计进队伍人数：4 个机器人 + 玩家 = 5 人，BOSS 生命 ×5。
  「Leave Party」移除该玩家，只剩一人的队伍会在双方客户端解散。
  拒绝、踢人与掉线尚未处理。
  见 [28](../docs/protocol/28-player-trade.md) §5。
  模式整合会在入队与结算时拒绝 SSF、不同赛季以及终止/离场状态的队长。
  组合自动化检查覆盖资格、物品归属，以及第二次背包保存失败时的回滚。
- **好友：** 玩家右键菜单与好友面板的名字输入框都能把账号加进列表。
  对方账号会收到带 Accept/Decline 的邀请；Unfriend 会从双方列表里删除该关系。
  账号 GUID 与邀请都以事务方式保存，与角色和赛季无关。
  原生双客户端核对确认了添加、邀请显示、Accept、Unfriend、离线显示与登录时恢复。
  条目完成时（藏身处也算）以及登出时双向更新在线状态；换区域时不发送。
  持久化/重开、回滚以及原生报文组帧都有自动化覆盖。
  备注、屏蔽列表与状态编辑尚未做。
  见 [28](../docs/protocol/28-player-trade.md) §6。

## 部分实现

- 无情（Ruthless）会选用其原生的宝石/奖励限制与购买成本；装备走本地生成策略。
  普通商人不卖宝石与首饰；解锁后的 Siosa 用未雕琢宝石兑换可用的主动宝石。
  辅助宝石被排除在确定性奖励和开局的辅助宝石箱之外。
  受支持的药剂商人、Helena 与 Niko 会补充无情模式的池子与药剂。
  自动化检查覆盖原生无情商店行、受限库存以及商人资源/药剂补充。
  其他商人、任务奖励与定价情形由自动化检查覆盖。
  见 [赛季模式](../docs/game/league-modes.md)。
- 任务奖励会为每个 `QuestRewardOffers` 行打开原生奖励界面（只限服务端能构建的物品），并发放按职业过滤后的选项
  （一级宝石、普通药剂；无法构建的魔法护甲行会被跳过）。
  符合条件的普通任务对话/奖励会随引导自动进行；Nessa、Bestel 与 Tarkleigh 已实机核对。
  物品与领取标记一起保存，并有重复、前置条件、背包满与保存失败的保护。
  Tarkleigh 的 Freezing Pulse 与 Nessa 的 Combustion Support 领取已在一个一次性角色上实机核对，
  Tarkleigh 的 a1q4 Glyph 奖励于 2026-10-26 核对。
  见 [任务奖励](../docs/protocol/17-npcs.md#quest-dialogue-and-first-rewards-2026-10-25)。
- NPC 出售可见性：挂牌的物品会从主背包隐藏，只在「Your Offer」里显示。
  它们原来的格子/物品在持久化存档里仍然被占住；
  取回/取消会恢复显示，掉线/崩溃会保住物品，接受则在一个存档里交换物品与货款。
  配方报价覆盖稀有套装、宝石转化、孔位/品质回收、升级与常见装备配方。
  接受会以原子方式支付缓存的产物，拒绝背包已满，并在保存失败时保留材料。
  精确词缀价格与特殊的赛季/传奇配方仍是部分实现；真实客户端的配方验证尚未做。
  支持的情形与明确的限制见 [商人配方](../docs/protocol/24-vendor-recipes.md)。
  实机可见性验证尚未做；见 protocol/17-npcs.md。
- 已抓取的宝石分组会被恢复；未抓取的职业/商人使用文档化的顺序，Kirac 刷新的界面仍未做。
- Nessa 的开发目录（觉醒/卓越宝石、通用/创始者/传奇地图）是可选模块 `NessaDevelopmentTabs`：
  接在她的进度页签之后或替换它们，已实机核对；没有赛季列出它。
  标准赛季使用进度库存；完整的传奇词缀、传奇区域路由与更新后的开发展示仍是部分实现。
  见 [17](../docs/protocol/17-npcs.md#vendor-evidence-and-local-policy)。
- **玩家机器人**（PlayerBots 模块，默认关闭）：机器人随时间创建，在任务之间走访城镇。
  玩家能看到它们以自己的职业模型与装备行走。
  每个剧情城镇维持 2 到 5 名居民，等级显示为原生数值（第一幕 1 到 3；第十幕 59 到 75；中间的幕按两端插值并 ±5）。
  受邀的机器人跨区域保留其在城镇选定的等级，并通过获得的经验成长，不会随怪物等级缩放。
  队员形态使用普通玩家战斗：兼容的连携宝石、已连线的天赋构建、敌对目标选择、伤害、复活以及持久化的击杀/宝石经验，
  近战、投射物与自己的击杀共享。
  观察者会获得实际获得的经验、等级属性与默认的升级闪光；队伍在活动状态下重载会保留已得经验。
  原生队伍菜单里的踢人会移除选中的机器人。
  跟随者在待机队长 60 格内安静站立，偶尔做小调整，并从入口前方的空格进入。
  行走时它们排在队长身后 9/13/17 格（各占一档，纵队而不是横排），并避开队长正在走的走廊（前方 26 格、左右 5 格）；
  已经跑到队长前面的会向侧面让开 7 格站到路边，而不是堵在路上——其他玩家的身体是玩家寻路要绕开的障碍。
  每个跟随者有自己的性格（按名字哈希固定）：冲劲决定它离队长多远去追怪（55~87 格），谨慎决定它多早喝药
  （生命 20%~40%）与被贴脸时是否后退，忠诚决定它多积极去打正在打你的怪，话痨决定它说不说话。
  它只打自己 80 格内（法术/箭矢的出手距离也是 80）且在队长范围内的敌人，优先稀有、其次打在队长身上的；
  普通怪与其他机器人分开打，稀有与首领共用目标。远程机器人被贴到 6~14 格会后退一段，近战机器人站着打。
  有目标期间它的跟随半径放宽到 90 格并原地守住，不会为了站队形而走出自己的射程。
  移动中的跟随者会就近对可及的目标出手，有目标时原地守住，不会为此打断自己的挥击/施法。
  超过 60 格（战斗中 90 格）的间隙会获得短暂追赶宽限再传送；大幅跳变则立即把跟随者带回。
  共享死亡会在每个观察者那里隐藏尸体血条，包括被受控队员击杀的情况。
  可选的 [MCP 控制](../../Modules/PlayerBots/MCP.md) 提供按机器人的租约、观测与原生战术指令，同时保留普通跟随/回落行为。
  队长在线时队伍状态可跨模块重载存活。屏幕外的任务仍为模拟；交易、升华构建与高级战术 AI 尚未做
  （聊天只做了一半：机器人会在区域本地聊天里偶尔说一两句，但不会读也不会回）。
  见 [玩家机器人](../docs/dev/player-bots.md)。
- Faustus's Currency Exchange at official prices works through `/exchange` (orders, claims, cancelling; live 2026-11-08).
  Spreads by volume, a market of 6 steps, fills limited per hour, orders filling while away and with time are written and not yet played.
  Faustus stands in every hideout; his dialogue option there is not yet played.
  See [29](../docs/protocol/29-currency-exchange.md).
- Ruthless selects its native gem/reward restrictions and purchase costs; equipment uses local generation policy.
  Ordinary vendors offer no gems or jewellery; unlocked Siosa exchanges Uncarved Gemstones for eligible active gems.
  Support gems are excluded from deterministic rewards and the opening support chest.
  Supported flask vendors, Helena and Niko refill Ruthless pools and flasks.
  Automated checks cover native Ruthless shop rows, restricted stock and vendor resource/flask refills.
  Other vendor, quest-reward and pricing cases are covered by automated checks.
  See [league modes](../docs/game/league-modes.md).
- Quest rewards open the native reward inventory for every `QuestRewardOffers` row whose items the server can build,
  and grant the class-filtered choices (level-one gems, normal flasks; unbuildable magic armour rows are skipped).
  Eligible ordinary quest talks/rewards follow introductions automatically; Nessa, Bestel and Tarkleigh checked live.
  The item and claimed flag are saved together, with duplicate, prerequisite, full-inventory and save-failure guards.
  Tarkleigh's Freezing Pulse and Nessa's Combustion Support claims were checked live on a disposable character, and
  Tarkleigh's a1q4 Glyph Reward on 2026-10-26.
  See [quest rewards](../docs/protocol/17-npcs.md#quest-dialogue-and-first-rewards-2026-10-25).
- NPC sale visibility: offered items are hidden from the main inventory and shown only in Your Offer.
  Their original cells/items remain reserved in the durable save;
  take-back/cancel restores the view, disconnect/crash preserves items, and acceptance exchanges items/payment in one
  save.
  Recipe quotes cover rare sets, gem transformations, socket/quality salvage, upgrades and common equipment recipes.
  Acceptance pays the cached outputs atomically, refuses a full inventory and preserves ingredients on save failure.
  Exact affix prices and special league/unique recipes remain partial; real-client recipe verification is pending.
  See [vendor recipes](../docs/protocol/24-vendor-recipes.md) for supported cases and explicit limitations.
  Live visibility verification pending; see protocol/17-npcs.md.
- Captured gem grouping is restored; uncaptured classes/vendors use documented ordering, and Kirac refresh UI remains open.
- Nessa's development catalogue (awakened/exceptional gems, generic/originator/unique maps) is the optional
  `NessaDevelopmentTabs` module: after her progression tabs or replacing them, live-checked; no league lists it.
  Standard uses progression stock; full unique modifiers, unique-area routing and the updated development display remain partial.
  See [17](../docs/protocol/17-npcs.md#vendor-evidence-and-local-policy).
- **Player bots** (PlayerBots module, off by default): bots are created over time and visit the towns between quests.
  Players see them walk with their class model and gear.
  Each campaign town maintains 2–5 residents with native level display (Act 1: 1–3; Act 10: 59–75; intermediate acts interpolate the center with ±5).
  Invited bots keep their town-selected level across zones and grow through earned experience, without scaling to monster levels.
  Party followers use ordinary player combat: compatible linked gems, connected passive builds, hostile targeting,
  damage, resurrection and persisted kill/gem experience, shared for melee, projectile and owned kills.
  Observers receive earned experience, level stats and the default level-up flash; active-party reload retains earned XP.
  The native party-menu kick removes any selected bot; `/botinvite <name>` and `/botleave` do the invite and the leave
  from chat, and an invite also matches a bot standing in the player's town that has not been published to them yet
  (a module reload clears that flag until the bot's next update).
  Followers stand calmly within 60 cells of an idle leader, with rare short adjustments, and enter on clear cells ahead of the entrance.
  On the move they take the trail 9, 13 or 17 cells behind the leader — one rank each, a column rather than a rank — and keep
  out of the corridor the leader is walking down (26 cells ahead, 5 to either side); one that ends up ahead of a moving
  leader steps 7 cells off the road and waits there, since a player's body is a blocker the leader's pathfinder walks round.
  Moving followers take reachable attacks and hold their ground while they have a target, without cancelling their own swing/cast.
  Gaps over 60 cells get a short catch-up grace before relocation; large jumps return followers immediately.
  Shared deaths hide corpse health bars on each observer, including kills made by controlled party members.
  Optional [MCP control](../../Modules/PlayerBots/MCP.md) exposes per-bot leases, observations and native tactical orders with ordinary following/fallback.
  Party state survives module reload while the leader remains online. Offscreen quests remain simulated;
  trade, chat, ascendancy builds and advanced tactical AI remain open.
  See [player bots](../docs/dev/player-bots.md).

## 尚未实现

- 尚未实现的 NPC 对话选项、特殊赛季商店、赛季 NPC 与任务阶段变体
  （[17](../docs/protocol/17-npcs.md) 的 "Open" 一节）；
  还有 346 个带 NPC、ControlZone、BlightTower…… 等组件的怪物变体。
- `data/room-points.json` 给版本 35 和 36 的房间（所有城镇房间都在其中）的服务端对象（NPC、仓库……）一律填角度 0：
  `RoomFile`（`src/GameServer.Data/Terrain`）读的是每行的第三个数，而这两个版本把它填成 0，`MonsterPacks.LoadObjects`
  又把它传下去。
  城镇 NPC 不受影响（`TownNpcs` 会复制角度）；暮光海滩的力场墙属于更老的房间，保留自己的角度
  （[17](../docs/protocol/17-npcs.md) §2）。
