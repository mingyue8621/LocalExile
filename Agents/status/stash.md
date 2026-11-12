# 状态：仓库

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/stash.md)。

## 已实现（已用真实客户端验证）

- **每个剧情城镇的仓库都来自该城镇自己的房间数据（2026-11-08）。** 十个剧情幕的城镇现在按城镇房间里
  `chest_v01_01.ao` / `guildstash_v01_01.ao` 这两行来摆放仓库与公会仓库，用的是 `TownNpcs` 摆 NPC 的同一套摆放逻辑
  （房间的旋转码与原点格），不再靠肉眼量格子：
  狮眼守望（第一幕）313 279 / 324 278、森林营地 191 296 / 206 302、萨恩营地 293 422 / 303 428、
  高地（第四幕）201 520 / 217 484、监工之塔 338 337 / 336 347、狮眼守望（第六幕）313 417 / 324 416、
  桥头营地 601 632 / 610 622、萨恩营地（第八幕）294 422 / 303 428、高地（第九幕）199 521 / 217 484、
  奥瑞亚码头 532 296 / 542 276（仓库 / 公会仓库）。
  在此之前，第四、五、七、八、九、十幕根本没有仓库，也都没有公会仓库；第一、三、六幕的位置则比房间数据给出的格子
  偏了 14 到 16 格，因为那三处是在读房间数据之前用 `/stash_place` 现场摆的。公会仓库也不再依赖一份手工维护的区域
  名单——`Stash.cs` 改为查 `StashCatalog.GuildIn`，这样以后往那里加城镇就不会悄悄丢掉自己的公会仓库。
  旋转取的是房间自身在该摆放方式下的角度，而不是原始行里的角度：R270 会让它转四分之一圈
  （森林营地在此之前一直用的是原始角度）。尚未用真实客户端核对。
- 夺宝（Delve）仓库修正：按原生下发布局 41 × 4 的背包边界；所有化石/共振器槽位保留 5000 上限。
  空的旧式共振器会迁移为可堆叠类型，带 1 到 4 个独立的黄/橙色夺宝孔。
  有自动化的布局/合并/重载覆盖；已由用户在真实客户端确认可用。
  见 protocol/18-stash.md。
- **通货取出（#249）。** Ctrl + 左键从点击的槽位里最多取一组普通背包大小的堆叠；
  Ctrl + 右键走单独的 `0xe3` 批量布尔，取该堆叠能装下的全部。
  实机客户端/Gateway 日志确认：批量 0 从 218 中取 40，剩 178；批量 1 取走 178，即四组 40 加一组 18。
  自动化测试覆盖数量上限、向已有堆叠加料、背包部分/完全放满、回滚与存档重载。
- 仓库：狮眼守望的仓库（319, 272）打开仓库面板（S→C 0x7b kind 1），带该角色所属赛季的 11 个账号页签
  （普通、高级、四方、通货、精华、碎片、地图、药剂、宝石），页签列表来自 S→C 0x21；每个页签的内容在客户端请求时
  才下发（C→S 0xc7 → 0xc0 row 26 + 0xc8）；物品进出页签靠点击（0x28 / 0x2a）与 Ctrl + 点击（C→S 0xe0 存入、
  0xe3 取出，堆叠进堆叠）；如果点击的页签刚好被一次页签切换卸载，会先重新加载该页签的背包（0xc0 + 0xc8）再更新，
  这修掉了客户端因
  `Got an inventory set change for an inventory that isn't in the set` 而中止的问题（#605）；
  特殊页签只放自己的东西（地图页签放在该地图类型的隐藏分页上），通货、精华、碎片页签每个物品进自己的格子，
  依据游戏数据里的仓库表（18 §7）；通货页签上限来自 `CurrencyItems.CurrencyTab_StackSize`（一般是 10 000，
  生命之力 50 000）。此前的实机验证与 Gateway 日志显示黄色生命之力会合并成第 99 格的一个 50 000 堆叠。
  自动化测试覆盖手动与快速合并、丰收类型、溢出与存档重载；
  对通货按 Ctrl + 点击会进它亲和页签的槽位（C→S 0xe1，2026-10-20 实测），对通货槽位普通点击则拾起该堆叠；
  页签设置里的亲和关系（0xd3）会被保存；
  重登后仓库会打开在该账号上次的页签（每条连接第一次打开时 0x7b 带亲和），连接期间保持面板自己的页签；
  页签与物品都是账号级的（`data/saves.db` 里该账号那一行）。
  已用普通页签、高级页签与通货页签的生命之力槽实机验证；其他特殊页签的槽位还需进一步核对。
  公会仓库：每个藏身处里的第二个物件打开公会仓库面板（0x7b kind 2），带自己的页签（manager 2），
  在还没有公会系统之前按账号各一份（已实机验证）。
  代码：`AccountStash.cs`、`StashCatalog.cs`、`InstanceStash.cs`、Gateway（`OpenStash`、`SendStashTab`）。
  文档：[18](../docs/protocol/18-stash.md)。
- **仓库开得更快。** 客户端只保留它正在显示的那个仓库页签：切换到某页签会卸载其他页签（S→C 0xc1），并重发页签列表
  把它们标为未加载。进入区域时只预加载带商店通货的页签。
  在此之前，每多加载一个页签，打开就慢一点（通货、精华、碎片、宝石、占卜全满时约 1.5 秒）。
  用户在真实客户端确认（2026-10-29）打开明显变快，且没有崩溃。
  Gateway 测试覆盖卸载、保留的商店价目页签、首次打开的两个请求以及 0xc1 的字节。
  见 protocol/18-stash.md §3。
- **开发用页签填充（`/item_tab <页签名> <数量>`）。** 在通货、精华、碎片、夺宝、枯萎、谵妄或占卜页签上，
  给每种有独立格子的物品最多再加 `数量`（单条命令最多 1000），加到各自格子装满为止。
  占卜页签会填成完整套牌，这正是该页签展示的形式。
  在宝石页签上，为每种尚不存在、且可生成的宝石变体各加一个，直到 500 格装满。
  药剂页签会补一个缺失的标准药剂或酊剂基底，以及每种可生成的传奇药剂/酊剂身份。
  普通药剂包含 12 种生命、12 种魔力、6 种混合与 18 种功能基底；Royale 的重复项排除在外。
  已有的普通或魔法物品按基底计；传奇按基底、名称与外观标识各自独立计数。
  重复填充不会产生重复，并保留已有物品，包括同一基底上的不同传奇。
  其他没有对应填充模式的页签会被拒绝并给出原因。
  枯萎、谵妄、占卜、最后通牒与裂隙页签现在会下发自己的 StashType 尺寸；此前 12 × 12 的默认值让油画到了枯萎页签外面。
  已实机验证（2026-10-28）：枯萎油、占卜页签两行的全部卡片，以及宝石页签的颜色子页签。
  自动化测试覆盖上限、就地增长、重载、被拒绝的页签、页签查找、格子填充，以及每种真实布局与其下发尺寸的比对。
  地图页签（#477）给每个 `MapKeys` 基底六个分页：客户端的页键是 `(page << 37) | (base HASH32 << 5)`，
  组号在低位（2026-11-06 从客户端读出），所以档位按钮能显示自己的分页条、角标统计该页地图数，
  而丢到页签上的地图（从背包或光标）会被送到同类型中有空位的第一页
  （`AccountStash.MapPageKey`/`MapDestination`）。传奇地图有自己单独一页（组 1，按外观标识作键），
  征服者或长老守卫的地图会进对应守卫的页（组 2 和组 3），先查 `MapConquerorContainsBossN`/
  `MapElderContainsBossN` 词缀，再回落到势力标记；一个分页只收属于它的地图。
  传奇收藏使用 22 个隐藏分类背包与原生收藏格；`/item_tab Уники 1` 会给每个可用收藏格各填一次，并保留已有物品。
  已实机验证：1309 件物品，含传奇地图与未启用的酊剂。重复填充不会在已占用的格子上产生重复。
  七个旧目录名在客户端没有对应的收藏格，会报告为不可用；传奇夺宝契约不生成。
  历史遗留物品可能让原生收藏计数超过其当前赛季的分母。
- Campaign towns in Acts 2–9 have personal and guild stashes at player-confirmed positions.
- Delve stash correction: publishes the native 41 × 4 inventory bounds;
  all fossil/resonator slots retain the 5000 limit.
  Empty legacy resonators migrate to stackable types with 1–4 independent yellow/orange Delve sockets.
  Automated layout/merge/reload coverage; confirmed working by the user in the real client.
  See protocol/18-stash.md.
- **Currency withdrawal (#249).** Ctrl + left click takes at most one normal inventory stack from the clicked slot;
  Ctrl + right click uses the separate `0xe3` bulk boolean to take as much of that stack as fits.
  Real-client/Gateway logs confirm bulk 0 takes 40 from 218, leaving 178;
  bulk 1 takes 178 as four stacks of 40 and one of 18.
  Automated tests cover quantity limits, existing-stack top-ups, partial/full inventory, rollback and save reloads.
- Stash: the Stash in Lioneye's Watch at (319, 272) opens the stash panel (S→C 0x7b kind 1) with the account's 11 tabs
  of the character's league (normal, premium, quad, currency, essence, fragment, map, flask, gem) from the tab list in
  S→C 0x21; each tab's contents come on the client's request (C→S 0xc7 → 0xc0 row 26 + 0xc8);
  items go into and out of tabs by click (0x28 / 0x2a) and Ctrl + click (C→S 0xe0 in, 0xe3 out, stacks onto stacks);
  a click on a tab that a tab switch just unloaded re-loads that tab's inventory (0xc0 + 0xc8) before the update, which
  stopped the client aborting on `Got an inventory set change for an inventory that isn't in the set` (#605);
  special tabs hold only their own items (the map tab on the hidden page of the map's kind), the currency, essence and fragment tabs each item
  in its own slot, from the game data's stash tables (18 §7);
  currency-tab limits come from `CurrencyItems.CurrencyTab_StackSize` (generally 10 000; lifeforce 50 000).
  Earlier real-client verification and Gateway logs show yellow lifeforce merging into one 50 000 stack in slot 99.
  Automated tests cover manual and quick merges, Harvest types, overflow and save reloads;
  Ctrl + click on currency goes to its affinity tab's slot (C→S 0xe1, live 2026-10-20) and a plain click on a currency
  slot picks up the stack; the tab settings' affinities (0xd3) are saved;
  the stash reopens on the account's last tab after a relog (0x7b affinity on the first open per connection) and keeps
  the panel's own tab while the connection lasts; tabs and items are account-wide (the account's row in
  `data/characters.db`).
  Verified live with normal, premium and the currency lifeforce slot; other special-tab slots need further checks.
  Guild stash: a second object in every hideout opens the guild stash panel (0x7b kind 2) with its own tabs (manager
  2), one per account until there are guilds (verified live).
  Code: `AccountStash.cs`, `StashCatalog.cs`, `InstanceStash.cs`, Gateway (`OpenStash`, `SendStashTab`).
  Docs: [18](../docs/protocol/18-stash.md).
- **Fast stash opens.** The client keeps only the stash tab it shows loaded: picking a tab unloads the others (S→C 0xc1)
  and resends the tab list with them marked not loaded. Area entry preloads only the tabs with shop currency.
  Before, each open got slower with every loaded tab (about 1.5 s with full currency, essence, fragment, gem and
  divination tabs). The user confirmed in the real client (2026-10-29) that opens became much faster, with no crash.
  Gateway tests cover the unload, the price tab that stays, the first open's two requests and the 0xc1 bytes.
  See protocol/18-stash.md §3.
- **Dev tab fill (`/item_tab <tab name> <count>`).** On a currency, essence, fragment, delve, blight, delirium or divination tab it
  adds up to `count` more, at most 1000 per command, of every item type that has its own slot, as far as each slot holds.
  Divination cards are filled to complete sets, which is what that tab shows.
  On a gem tab it adds one of every creatable gem variant not already there, as far as its 500 cells go.
  The flask tab adds one missing standard flask or tincture base and each creatable unique flask/tincture identity.
  Ordinary flasks include 12 life, 12 mana, 6 hybrid and 18 utility bases; Royale duplicates are excluded.
  Existing normal or magic items count by base; uniques count by base, name and visual identity, independently.
  Repeat filling adds no duplicates and preserves existing items, including different uniques on the same base.
  Other tabs without a supported fill mode are refused with the reason.
  Blight, Delirium, Divination, Ultimatum and Breach tabs now publish their StashType sizes; the 12 × 12 default made the
  oils draw over the blight tab.
  Verified in the real client (2026-10-28): blight oils, all divination cards in both rows of the tab, and the gem tab's
  colour sub-tabs. Automated tests cover caps, in-place growth, reloads, refused tabs, tab lookup, cell fill and every
  real layout against its published size. The map tab (#477) gives every `MapKeys` base its six pages: the client's page
  keys are `(page << 37) | (base HASH32 << 5)` with the group in the low bits (read from the client, 2026-11-06), so a
  tier button shows its page strip, its badge counts the maps, and a map dropped on the tab (from the inventory or the
  cursor) is routed to the first page of its own kind with room (`AccountStash.MapPageKey`/`MapDestination`). A legendary
  map has one page of its own (group 1, keyed by its visual identity) and a conqueror's or elder guardian's map goes to
  its guardian's page (groups 2 and 3), found from the `MapConquerorContainsBossN`/`MapElderContainsBossN` modifiers first
  and the influence flags as the fallback; a page only takes maps that belong to it.
  New and existing Unique collections accept ordinary deposits through 22 hidden category inventories and the native collection cells; `/item_tab Уники 1` fills
  each available collection cell once, preserving existing items. Verified in the real client: 1309 items, including
  unique maps and inactive tinctures. Repeat filling does not duplicate occupied cells. Seven old catalogue names
  have no matching client collection cell and are reported as unavailable; unique Heist contracts are not generated.
  Legacy items can make the native collected count exceed its current-league denominator.
- `AccountStash.AddTabs` appends general tabs (normal, premium, quad) for a mod; they show in the next tab list
  (0x21, on entering an area) or after a relog. Automated test: `StashAddTabsTests`.

## 部分实现

- 个人仓库与金币只在同一账号、同一完整赛季内共享，包含各种模式组合。
  公会仓库存储同样按赛季隔离；标准赛季保留旧的银行，其他赛季各用独立的银行。
  SSF 会拒绝打开公会仓库、拒绝页签请求与背包移动。
  自动化检查覆盖 SSF 的公会仓库排除与普通公会仓库访问。
  仓库共享、伪造请求与掉落物隔离都有自动化检查覆盖。
  城镇里 SSF 共享玩家的可见性与私有掉落物仍然分开。
  见 [赛季模式](../docs/game/league-modes.md)。
- **制作格。** 夺宝制作格接受装备、支持宝石交换，重载后保留内容。
  狮眼守望现在会在个人仓库旁边生成公会仓库。
  持久化检查通过；真实客户端打开过城镇公会仓库并加载了它的公会页签。

## 尚未实现

- 仓库：切换到已经加载过的页签时不发送任何内容，所以如果玩家没有重新打开仓库、也没有移动物品就登出，这次切换会丢失；
  特殊页签的槽位规则、页签文件夹（页签顺序、名称与颜色会被保留，2026-10-22）、公会仓库真正对接公会，
  以及其他城镇的仓库（[18](../docs/protocol/18-stash.md) 的 "Open" 一节）。
