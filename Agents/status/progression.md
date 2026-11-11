# 状态：经验与天赋树

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/progression.md)。

## 已实现（已用真实客户端验证）

- 经验与等级：150 格内的击杀给 DefaultMonsterStats 经验 × 该变体的倍率 × 稀有度，并带等级惩罚
  （S→C 0x2bc 的 Player experience；等级由客户端推导）；升级会下发其属性、满生命与魔力、天赋点与该账号等级；
  经验按角色保存；`/add_xp`。
  代码：`Progression.cs`、Gateway（`GainExperience`、`ApplyLevel`）。
  文档：[15](../docs/protocol/15-progression.md) §1。
- 天赋树：S→C 0x33（等级、职业、已分配节点、天赋点与退点；客户端会把点数算成等级 − 1），C→S 0x34 分配一个节点，
  经校验、应用到属性（属性值、生命、魔力、抗性等）并保存；C→S 0x36 带标记 0 用一个已保存的退点退还一个节点
  （经校验，精通效果一并移除）；使用悔恨之球（右键，C→S 0x58）消耗一件物品并加一点；
  技能之书（右键，C→S 0x58；所有同名任务书）会被消耗并再给一点天赋点，两者一起保存；悔恨之书以同样方式给两点退点；
  `/passives`、`/add_refund_points`、`/reset_passives`。
  代码：`PassiveTree.cs`、`CharacterSaves.cs`、Gateway（`ChangePassives`、`UseItem`）。
  文档：[15](../docs/protocol/15-progression.md) §2。
- 天赋精通：S→C 0x33 携带已选效果（`node | effect << 16`），C→S 0x35 选择效果的 HASH16；
  精通节点可以正常分配，其效果可被选择/更换而不额外花点。
  格挡精通的四个效果已实现：最大攻击格挡 +2、最大法术格挡 +2、格挡时 20 生命 + 20 魔力、
  每 5% 攻击格挡 +1% 法术格挡；效果与角色一起保存。
  火焰精通「每 1% 未封顶火焰抗性每秒回复 1 点生命」计入生命回复（只算正抗性；2026-11-03 实机：161% 加了每秒 161）。
  法杖精通的法杖专属点数已建模：额外攻击投射物与等同于智力的命中，各只在持法杖时生效；该精通的 0.5% 攻击生命与魔力偷取
  是通用效果，任何武器都生效。法杖暴击得能量球与法杖攻击的加性法术伤害此前已生效。Spellslinger 的效率保留在
  Spellslinger 本身未实现前没有消费者。
  代码：`PassiveTree.cs`、`CharacterSaves.cs`、Gateway（`ChangePassives`、`PassivesMessage`）。
  文档：[15](../docs/protocol/15-progression.md) §2。
- 天赋树的珠宝孔（服务端与实机属性核对 2026-10-27；树形美术核对待做）：21 个普通大孔就是 `PassiveJewels1` 面板
  （Inventories.dat 第 23 行，背包 id 24，60x1）。面板格子 = `PassiveJewelSlots.dat` 的行，其 `Slot` = `PassiveSkills.dat` 的行。
  普通/魔法/稀有珠宝可以放入、换位与拾取（0x2a/0x4a/0x28），随背包保存，其全局词缀像已穿戴物品一样生效；
  插孔必须已分配、物品必须是不可为集群的珠宝、且满足等级需求。退还仍插着珠宝的孔会被拒绝直到取出；
  等级下调会把珠宝退回背包。
  传奇珠宝走同一路径，但还没有传奇物品目录/来源。
  The Adorned 是传奇珠宝的试点：`/adorned [0-1000]` 创建腐化的传奇 Crimson Jewel，其效果会缩放普通天赋孔里的腐化魔法珠宝
  （实机：一颗 +15% 珠宝配 100% 的 The Adorned，火焰抗性 225% -> 240% -> 255%）。集群孔与 3.25 之前的 50-150% 变体未覆盖。
  百分比属性词缀（`strength_+%`、`dexterity_+%`、`intelligence_+%`、`all_attributes_+%`）现在会缩放最终职业与物品属性，
  所以 Brawn 与帝王的珠宝能进入池子与角色面板。
  按属性统计的就绪度：43 颗 Global 套装珠宝（42 Global + The Adorned）的 97 项属性中 46 项已在游戏内，另有 11 项只在面板，
  所以这套按属性算是 47% 完成（把面板行也算上是 59%）；整个 185 颗的目录是 10%。逐颗的表格见
  [unique-jewels](../docs/game/unique-jewels.md)。
  代码：`PassiveTree.JewelNodeForSlot`、Gateway（`PlaceItem`、`HeldOntoItem`、`PickUpItem`、`CharacterStatSources`）。
  文档：[15](../docs/protocol/15-progression.md) §2a、[unique-jewels](../docs/game/unique-jewels.md)。
- 集群珠宝（服务端与实机客户端核对 2026-10-27）：`/cluster_jewel <small|medium|large> [天赋数] [孔数] [主题] [显著天赋…]`
  与 `/voices [1|3|5|7]` 会生成真实物品，嵌套遵循尺寸规则（天赋树里的孔接受任意尺寸，大珠宝的孔接受中/小，
  中珠宝的孔接受小）。它们的天赋是客户端自己的扩展 id（按面板格固定基底，例如第 20 格为 195..206）；
  服务端把它们放在 0x33 的第三个列表里，每点计一点，随角色保存，并在登录时按已插珠宝的节点预算恢复
  （每颗珠宝的 `Adds N Passive Skills` 计数，其中包含孔；Voices 的空天赋等于它的孔数）。`ClusterJewelLayout`
  复现客户端实测的布局：树孔的块基底是 `39 * ClusterIndex`，其孔的块基底是 `9 * 子珠宝的 ClusterIndex + 12`，
  更深一层是 `6`；位置是 `(StartIndices[size] + templateIndex) % TotalIndices`；
  树孔里的大珠宝保留整环（小天赋拿主题、显著天赋占自己一行、孔什么都没有），
  扩展格里的珠宝被压到半环（实测一颗 4 天赋的中珠宝：第 51 格的小天赋 208、209、211 与孔 212，
  第 53 格的 225、228、229 与 226）。42 个主题属性中有 20 个已翻译；没有实测布局的扩展格形状（显著天赋）仍用按计数估算。
  小集群珠宝现在也能放进中孔与大孔（客户端会补齐格子）；一颗蓝/黄小珠宝（带显著天赋）会得到实测的图谱
  （第 54 格基底 213：显著天赋 213、小天赋 214/215；在 17 号大号树孔里，小天赋的 157/161 是主题小天赋、159 是它的第一个显著天赋，
  其他显著天赋词缀无效），并按每个小天赋给主题加第一个显著天赋的那一行，而纯主题的小珠宝在我们的测试里客户端没有画出图谱。
  整树实机核对：树孔 11 里一颗大珠宝（基底 0）、它第 21 格里一颗中珠宝（基底 12）、第 24 格里一颗蓝色小珠宝（基底 18）；
  小珠宝的 18/19 加了主题，20 是 Fettle。聊天里逐节点的调试行已去掉；Gateway 仍记录 id 与 `passive jewel stats` 行。
  珠宝稀有度跟随其显著天赋（0 个为普通、1 或 2 个为魔法、3 个为稀有）：一颗没有词缀的魔法集群珠宝会让客户端把整个集群
  画成空的「Nothingness」节点。
  仍缺：核心天赋、半径规则与商人配方。集群珠宝只能放进同尺寸的孔：客户端从格子构建布局，所以更小的珠宝会被补上
  什么都不给的空「Nothingness」节点。
  端到端客户端核对（`scripts/dev/jewel_selftest.py`）覆盖插入/移除且不占节点、大集群的小与显著天赋属性、
  大珠宝孔里的中集群，以及集群孔里一颗 +80 生命的普通珠宝。
  代码：`Program.Items.cs`（`ClusterJewelSocketed`、`ClusterNodeBudget`、`CreateClusterJewel`、`CreateVoices`）、
  `ClusterJewelStats.cs`、Gateway（`ChangeClusterPassive`）。文档：[cluster-jewels](../docs/game/cluster-jewels.md)。
- Experience and levels: kills within 150 cells give DefaultMonsterStats experience × the variety's multiplier ×
  rarity (the hidden rarity mods in Mods.json) × the level penalty (safe zone, level 95+ and the effective monster
  level above 70) × the party share (every living player within 150 cells, (level + 10)^2.71), with the gems' 10% taken
  before the penalty (S→C 0x2bc Player experience; the client derives the level);
  a new level sends its stats, full life and mana, the passive points and the account's level;
  experience is saved per character; `/add_xp`.
  Code: `Progression.cs`, Gateway (`GainExperience`, `ApplyLevel`).
- Experience and levels from kills, with the level penalty and party share.
  Docs: [15](../docs/protocol/15-progression.md) §1.
- Passive Skill Tree: allocation, refunds, Orbs of Regret, Books of Skill and Regrets.
  Docs: [15](../docs/protocol/15-progression.md) §2.
- Passive masteries: S→C 0x33 carries selected effects (`node | effect << 16`) and C→S 0x35 selects the effect HASH16;
  a mastery node can be allocated normally and its effect selected/changed without spending another point.
  Block Mastery's four effects are implemented: max attack block +2, max spell block +2, 20 life + 20 mana on block,
  and +1% spell block per 5% attack block; the effects are saved with the character.
  Fire Mastery's 1 Life regenerated per second per 1% uncapped fire resistance counts in the life regeneration
  (positive resistance only; live 2026-11-03: 161% added 161 per second).
  Wand Mastery's wand-only points are modelled: the extra attack projectile and accuracy equal to intelligence, each
  only while a wand is held; the mastery's 0.5% attack life and mana leech is a generic effect that applies with any
  weapon. Power charge on wand crit and additive spell damage to wand attacks were already applied. Spellslinger's
  reservation efficiency has no consumer while Spellslinger itself is unimplemented.
  Code: `PassiveTree.cs`, `CharacterSaves.cs`, Gateway (`ChangePassives`, `PassivesMessage`).
  Docs: [15](../docs/protocol/15-progression.md) §2.
- Passive tree jewel sockets (server and live stat check 2026-10-27; tree art check pending): the 21 ordinary large
  sockets are the `PassiveJewels1` pane (Inventories.dat row 23, inventory id 24, 60x1). Pane cell = the
  `PassiveJewelSlots.dat` row and its `Slot` = the `PassiveSkills.dat` row. Ordinary/magic/rare jewels can be placed,
  swapped and picked up (0x2a/0x4a/0x28), are saved with the inventories, and their global mods apply like worn items;
  the socket must be allocated, the item a non-cluster jewel and its level requirement met. Refunding a socket that
  holds a jewel is refused until it is taken out; a level trim returns the jewel to the inventory.
  Unique jewels use the same path but have no unique item catalogue/source yet.
  The Adorned is the pilot unique jewel: `/adorned [0-1000]` creates the corrupted unique Crimson Jewel, and its effect
  scales corrupted magic jewels in the ordinary passive sockets (live: 225% -> 240% -> 255% fire resistance with a
  +15% jewel and The Adorned at 100%). Cluster sockets and the pre-3.25 50-150% variant are not covered.
  Percentage attribute mods (`strength_+%`, `dexterity_+%`, `intelligence_+%`, `all_attributes_+%`) now scale the final
  class and item attributes, so Brawn and the Emperor's jewels reach the pools and the character panel.
  Readiness, counted per stat: 46 of the 97 stats of the 43 Global-set jewels (42 Global + The Adorned) are in game
  and 11 more are panel only, so the set is 47% done by stats (59% counting the panel rows); the whole 185-jewel
  catalogue is 10%. The per-jewel table is in [unique-jewels](../docs/game/unique-jewels.md).
  Code: `PassiveTree.JewelNodeForSlot`, Gateway (`PlaceItem`, `HeldOntoItem`, `PickUpItem`, `CharacterStatSources`).
  Docs: [15](../docs/protocol/15-progression.md) §2a, [unique-jewels](../docs/game/unique-jewels.md).
- The seven single-keystone small cluster jewels grant their native keystone stat on allocation. Their individual
  combat mechanics remain partial. For ordinary clusters, `/cluster_jewel <small|medium|large> [passives] [sockets]
  [theme] [notables…]` and `/voices [1|3|5|7]` make real items, and nesting follows the size rule (a tree socket takes any
  size, a large jewel's sockets medium/small, a medium one's socket small). Their passives are the client's own
  expansion ids (block base fixed per pane cell, e.g. 195..206 for cell 20); the server keeps them in the third list of
  0x33, counts a point each, saves them with the character and restores them on login up to the socketed jewels' node
  budget (the `Adds N Passive Skills` count per jewel, which counts the sockets; Voices' empty passives are plus its
  sockets). `ClusterJewelLayout` reproduces the client's measured layout: the block base is `39 * ClusterIndex` for a tree socket,
  `9 * the child's ClusterIndex + 12` for a socket of one and `6` deeper, the position is `(StartIndices[size] +
  templateIndex) % TotalIndices`, a tree-socket large jewel keeps the full ring (a small passive gets the theme, a notable
  its own row, a socket nothing) and a jewel in an expansion cell is compressed onto half the ring (a measured 4-passive
  medium: smalls 208, 209, 211 and socket 212 in cell 51, 225, 228, 229 and 226 in cell 53). 20 of the 42 theme stats are
  translated; expansion-cell shapes without a measured layout (notables) still use the count-based estimate. A small
  cluster jewel also fits medium and large sockets now (the client pads the cell); a blue/yellow small (with a notable)
  gets its measured graph (base 213 in cell 54: notable 213, smalls 214/215; in the large tree socket 17 the small's
  157/161 are theme smalls and 159 its first notable, the other notable mods inert) and gives the theme per small plus
  the first notable's row, while a Normal theme-only small drew no client graph in our test. Live full-tree check: a
  large in tree socket 11 (base 0), a medium in its cell 21 (base 12) and a blue small in cell 24 (base 18); the small's
  18/19 added the theme and 20 was Fettle. The chat's per-node debug line is gone; the Gateway still logs the ids and the
  `passive jewel stats` line. The jewel's rarity follows its notables (none Normal, one or
  two Magic, three Rare): a Magic cluster jewel with no affix makes the client draw the whole cluster as empty
  "Nothingness" nodes.
  Still missing: keystones, radius rules and the vendor recipe. A cluster jewel fits only a socket of its own size: the client builds the layout from the cell, so a
  smaller jewel is padded with empty "Nothingness" nodes that grant nothing.
  An end-to-end client check (`scripts/dev/jewel_selftest.py`) covers insert/remove with no node taken, the small and
  notable stats of a large cluster, a medium cluster in a large one's socket, and a normal jewel with +80 life in a
  cluster socket.
  Code: `Program.Items.cs` (`ClusterJewelSocketed`, `ClusterNodeBudget`, `CreateClusterJewel`, `CreateVoices`),
  `ClusterJewelStats.cs`, Gateway (`ChangeClusterPassive`). Docs: [cluster-jewels](../docs/game/cluster-jewels.md).

- 永恒珠宝（军团；服务端与实机客户端核对 2026-10-29）：`/timeless <名称> [显示种子] [征服者]`
  用客户端的 `UniqueJewelAlternateTreeInRadius*` 词缀创建六颗传奇珠宝。客户端自己从这些属性画出被征服的半径；
  服务端用 TinyMT32 生成器、`AlternateTreeVersions` 标记、`AlternatePassiveSkills` 与 `AlternatePassiveAdditions`
  复现同一棵替换树，并在固定的 0..1500 半径内对已分配的普通节点应用替换或追加属性。随机与生成值都与
  Path of Building 的查找表一致（Vaal 小天赋 62429/种子 2000：光环效果 3；显著天赋 37326：Cult of Fire、
  30 火焰抗性与 1 最大抗性），实机客户端日志确认了替换树版本、种子与征服者。`Elegant Hubris` 保存它显示种子除以 20；
  只提供当前征服者；只允许镶嵌一颗 Historic 永恒珠宝；集群孔被拒绝。Militant Faith 还带两条不同的、当前版本的
  每 10 点虔诚词缀（3.29.0 之前的降低魔力消耗行属遗留，不会掷出）；`DevotionStats` 按 `floor(Devotion / 10)`
  把它们缩放成普通属性，所以抗性/伤害/光环/回复/异常状态目标会生效，而诅咒持续时间、召唤物/图腾/烙印/引导、
  魔力消耗与盾牌防御仍未被读取。生成的清单覆盖 96 条永恒追加，包含 Path of Building 参考数据里缺的四行。
  核心天赋的属性行会生效，但征服者核心天赋的特殊玩法机制（Divine Flesh、Chainbreaker 等）仍缺。
  代码：`TimelessJewelCatalog.cs`、`TimelessJewelEngine.cs`、`PassiveTree` 的几何/半径、`Program.Progression.cs`
  （`EquipmentStatSources`）、`Program.Items.cs`（`PassiveJewelRefusal`）。文档：
  [timeless-jewels](../docs/game/timeless-jewels.md)。

- 半径型传奇珠宝（试点 2026-10-29）：所有带 `Radius:` 行的 **78 行 / 76 个不同名字** 都在
  [radius-jewels](../docs/game/radius-jewels.md) 与生成的
  [radius-jewel-list](../docs/game/radius-jewel-list.md) 里分类：6 个属性转换、6 个伤害/防御/武器转换、
  3 个显著天赋替换、10 个天赋追加、33 个阈值/技能珠宝、11 个按节点缩放、3 个分配/天赋树规则、
  1 个天赋效果/显著天赋压制、5 个特殊/仅全局。**已实现的 42 颗（39 行试点加 Might in All Forms、Pitch Darkness
  与 Sudden Ignition）：** `/radius_jewel <名称> [变体]` 创建六个属性转换、五个伤害/防御转换、三个显著天赋替换、
  十个天赋追加、十一个按节点缩放的珠宝（Anatomical Knowledge、Eldritch Knowledge、Pugilist、Spire of Stone、
  Static Electricity、Tempered/Transcendent 三件套）、Might of the Meek，以及三个分配/天赋树规则的珠宝
  （Intuitive Leap、Thread of Hope 的五个环形变体、Unnatural Instinct）。
  `EquipmentStatSources` 移动/变换对应属性、替换已分配的显著天赋、追加天赋追加（含按抗性缩放的裂隙珠宝）、
  选择 Light/Perandus 变体、由已分配与未分配属性算出按节点缩放、应用 Might of the Meek 对非核心天赋的 50% 效果
  并压制显著天赋、交换 Unnatural Instinct 的已分配与未分配小天赋。`OrdinaryPassiveRules` 把 Intuitive Leap/Thread of Hope
  的绕过节点当作额外根；`PassiveTree.NodesInRing` 选出 Thread of Hope 的环，只要还有绕过分配的节点在位，取下这两颗珠宝就会被拒绝。
  实机：兽性之力在 1 号孔把力量 559 -> 549、智力 647 -> 681；Energy From Within 在 16 号孔把生命 5857 -> 5311、
  护盾 243 -> 273；The Light of Meaning（生命）在 12 号孔把生命 5311 -> 5355；在完整演示布局里（兽性之力 1 号孔、
  Anatomical 11 号孔、Fevered Mind 13 号孔、Light 12 号孔、Energy From Within 16 号孔）Anatomical 值 +165
  （装上生命 5520、不装 5355），同一布局读到魔力 2345、护盾 276（后来重登后是魔力 2355/护盾 277）。11 号孔有 20 智力，
  所以 `floor(20 / 3) = 6` 点固定生命是那个总数的一部分；13 号孔没有智力，在更早的单独核对里只应用了全局生命%
  （5355 -> 5502）。Might of the Meek 放在第 3 格，靠对智力/力量小天赋的 50% 效果把生命 5520 -> 5529、魔力 2345 -> 2375；
  未封顶的攻击/法术格挡从 168%/27% -> 162%/20%，因为 Dark Arts 显著天赋被压制（部分被缩放后的双持格挡小天赋抵消）。
  Intuitive Leap 在第 3 格接受了不连通的节点 55247，生命 5520 -> 5575，且分配在重登后保留；在第 1 格它接受了 42981，
  但拒绝了圆盘之外的邻居 53802（无桥接）。Thread of Hope 使用 PoB 的 3_16 可变环 960-1320、1320-1680、1680-2040、
  2040-2400 与 2400-2880，不会启用环内的珠宝孔，其 -10% 全抗词缀把 `/resistances` 268/221/221 -> 258/211/211；
  它的环值是内在的，且分配词缀的基础半径为 0，所以客户端画出环带而不是实心圆盘。Massive 在第 3 格接受了 23507（2628）
  与 48477（523 已分配）；退还它们之后 Large 接受了 38129（1808）；在这些绕过节点仍在时换下珠宝会被拒绝。
  Might in All Forms 在第 1 格把 `passive jewel stats` 的近战值从 129 改成 137（+8 = 半径内 40 点敏捷+智力 / 5），
  当时兽性之力在背包里；把兽性之力恢复后回到 127（转换会改变基线）。集群天赋从不在半径珠宝的半径内：
  Path of Building 在其集群图谱构建之前就预先算好 `nodesInRadius`，所以服务端只用普通 PSG 算半径是有意为之。
  33 个阈值珠宝有生成的物品来源（`scripts/re/threshold_jewels.py`、`ThresholdJewelCatalogue.cs`）；
  客户端应用它们的局部阈值词缀，`ThresholdJewels.cs` 解析目标/属性掩码、累加半径内每个普通天赋（无论是否分配，
  即 PoB 的 `Threshold` pass），并在总和达标时加上映射的属性。实机：`Combat Focus Cobalt` 在 36634 号孔记录
  `elemental_hit_cannot_roll_fire_damage 1`（60 智力）；`Collateral Damage` 在 60735 号孔记录
  `galvanic_arrow_area_damage_+% 50` 与 `shrapnel_shot_radius_+% 25`（60 敏捷）；`Spirited Response` 在 6 号孔
  （第 8 格）靠其 10% 最大魔力全局把魔力 2355 -> 2476。按技能限定作用域的消费者仍是部分实现，没有客户端属性行的目标会被跳过。
  Unnatural Instinct 在第 3 格靠三个已分配智力小天赋与三个未分配生命/护盾小天赋把生命 5520 -> 5685、
  魔力 2355 -> 2326、护盾 277 -> 285。Lioneye's Fall 的物品来源与客户端转换已存在（`/radius_jewel Lioneye's Fall`），
  但其服务端标记模型缺失。Inspired Learning 与 Warrior's Tale 仍不生效；缺失的层是近战/武器标记模型、
  偷取稀有词缀、纹身与按技能限定作用域的阈值消费者。插孔/保存路径、`PassiveTree.NodesInRadius`/`NodesInRing`、
  永恒的逐节点模式与美术表都被复用。半径套件有 98 个测试。

- Impossible Escape（生成的特殊项）：`/impossible_escape <核心天赋>|list` 创建 48 个变体的 Viridian Jewel
  （Small，限 1，腐化）。物品词缀保存该核心天赋的 `PassiveSkillGraphId` 与显示基础半径 800；
  效果半径是 Path of Building 3_16 的 Small **960**。带 Maven 表格行的旧物品在加载时迁移。
  `AllocationBypassNodes` 以已分配的核心天赋为中心、珠宝孔为提供者。实机：Versatile Combatant（第 2444 行，
  图谱 50679）画出了客户端环；节点 29547（生命偷取，相距 293 单位）无路径被接受，在其退还前拾取被拒绝，随后两者都被移除。
  Supreme Ego（第 2312 行，图谱 49639）在核心天赋分配前不显示环；分配后环出现，与其相邻的 Dire Torment（9015）被接受。
  Replenishing Remedies（50690，距 Supreme Ego 953.7 单位）在客户端环内，现在也被接受。命令现在会在所选核心天赋未分配时警告；
  五个旧的按行取值物品在加载时已迁移，其提示现在显示 Supreme Ego/Versatile Combatant。Rational Doctrine（`/unique_jewel`）
  在静止且力量最高时制造圣化地面、在暴击且智力最高时制造亵渎地面；两种效果都留存 4 秒，
  对亵渎地面上敌人的命中有 100% 提高暴击率。亵渎地面的减益与其暴击率消费者有自动化覆盖。
  2026-11-01 实机（物品属性用 `/set_stat` 设置）：一次暴击制造出亵渎地面美术、施加增益 1569，并在留存结束后移除它。

- 维基目录差异核对：poewiki 清单（189 个不同名字）与本目录（182）相比，剩下 Bound By Destiny、
  Forbidden Flame/Flesh、Megalomaniac、Sublime Vision、The Balance of Terror 与 Watcher's Eye 未实现；它们记录在
  [unique-jewels](../docs/game/unique-jewels.md) 里且没有物品来源。Impossible Escape 与 Rational Doctrine
  是这里补上的两处缺口。
- Timeless jewels roll a faction seed and current conqueror in ordinary unique generation.
  `/timeless <name> [displayed seed] [conqueror]` creates the six unique jewels with the client's `UniqueJewelAlternateTreeInRadius*` mod. The client itself draws the
  conquered radius from those stats; the server reproduces the same alternate tree with the TinyMT32 generator, the
  `AlternateTreeVersions` flags, `AlternatePassiveSkills` and `AlternatePassiveAdditions`, and applies replacement or
  added stats to allocated ordinary nodes in the fixed 0..1500 radius. The RNG and generated values match Path of
  Building's LUTs (Vaal small 62429/seed 2000: Aura Effect 3; notable 37326: Cult of Fire, 30 fire resistance and 1
  maximum) and the live client log confirms the alternate tree version, seed and conqueror. `Elegant Hubris` stores its
  displayed seed divided by 20; only current conquerors are offered; only one Historic timeless jewel may be socketed;
  cluster sockets are refused. Militant Faith also carries two distinct current per-10-Devotion mods (the pre-3.29.0
  reduced Mana Cost row is legacy and not rolled); `DevotionStats` scales them by `floor(Devotion / 10)` into the normal
  stats, so resistance/damage/aura/regen/ailment targets are applied while curse duration, minion/totem/brand/channelled,
  mana cost and shield defences remain unread. The generated list covers the 96 timeless additions, including the four
  rows missing from Path of Building's reference data.
  Keystone stat rows are applied, but the conqueror keystones' special gameplay mechanics (Divine Flesh, Chainbreaker and
  the rest) are still missing.
  Code: `TimelessJewelCatalog.cs`, `TimelessJewelEngine.cs`, `PassiveTree` geometry/radius, `Program.Progression.cs`
  (`EquipmentStatSources`), `Program.Items.cs` (`PassiveJewelRefusal`). Docs:
  [timeless-jewels](../docs/game/timeless-jewels.md).

- Radius unique jewels (pilot 2026-10-29): all **78 rows / 76 distinct names** with a `Radius:` line are classified in
  [radius-jewels](../docs/game/radius-jewels.md) and the generated
  [radius-jewel-list](../docs/game/radius-jewel-list.md): 6 attribute conversions, 6 damage/defence/weapon
  conversions, 3 notable replacements, 10 passive additions, 33 threshold/skill jewels, 11 per-node scaling, 3
  allocation/tree rules, 1 passive-effect/notable-suppression, 5 special/global-only. **The 42 implemented jewels
  (39 pilot rows plus Might in All Forms, Pitch Darkness and Sudden Ignition):** `/radius_jewel <name> [variant]`
  creates the six attribute conversions, five damage/defence conversions, three notable replacements, ten passive
  additions, eleven per-node scaling jewels
  (Anatomical Knowledge, Eldritch Knowledge, Pugilist, Spire of Stone, Static Electricity, the Tempered/Transcendent
  trios), Might of the Meek and the three allocation/tree-rule jewels (Intuitive Leap, Thread of Hope's five ring
  variants, Unnatural Instinct).
  `EquipmentStatSources` moves/transforms the matching stats, replaces allocated notables, appends the passive
  additions (including the resistance-scaled Breach jewels), selects the Light/Perandus variant, computes the per-node
  scaling from allocated and unallocated attributes, applies Might of the Meek's 50% non-keystone effect with notables
  suppressed, and swaps Unnatural Instinct's allocated and unallocated small passives. `OrdinaryPassiveRules` treats
  Intuitive Leap/Thread of Hope bypass nodes as extra roots; `PassiveTree.NodesInRing` selects the Thread of Hope ring,
  and taking either jewel out is refused while a bypass-allocated node remains. Live: Brute Force in socket 1 changed
  Strength 559 -> 549 and Intelligence 647 -> 681; Energy From Within in socket 16 changed life 5857 -> 5311 and ES
  243 -> 273; The Light of Meaning (Life) in socket 12 changed life 5311 -> 5355; in the full demo layout (Brute Force
  socket 1, Anatomical socket 11, Fevered Mind socket 13, Light socket 12, Energy From Within socket 16) Anatomical is
  worth +165 (life 5520 with it, 5355 without), and the same layout reads mana 2345 and ES 276 (mana 2355/ES 277 after
  the later relog). Socket 11 has 20 Int, so `floor(20 / 3) = 6` flat life is part of that total; socket 13 has no
  Intelligence and in the earlier isolated check only the global life% applied (5355 -> 5502). Might of the Meek in cell
  3 changed life 5520 -> 5529 and mana 2345 -> 2375 from the 50% effect on the Intelligence/Strength smalls; uncapped
  attack/spell block moved 168%/27% -> 162%/20% as the Dark Arts notable was suppressed (partly offset by the scaled
  dual-wield block smalls). Intuitive Leap in cell 3 accepted disconnected node 55247, life went 5520 -> 5575 and the
  allocation survived a relog; in cell 1 it accepted 42981 but refused its neighbour 53802 outside the disc (no
  bridging). Thread of Hope uses the PoB 3_16 Variable rings 960-1320, 1320-1680, 1680-2040, 2040-2400 and 2400-2880,
  does not enable jewel sockets in the ring, and its -10% all-res roll changed `/resistances` 268/221/221 ->
  258/211/211; its ring value is implicit and the allocate mod's base radius is 0 so the client draws the annulus
  instead of a filled disc. Massive in cell 3 accepted 23507 (2628) and 48477 (523 allocated); after refunding them the
  Large item accepted 38129 (1808); swapping the jewel while those bypass nodes remained was refused. Might in All
  Forms in cell 1 changed the `passive jewel stats` melee value 129 -> 137 (+8 = 40 Dex+Int in radius / 5) while Brute
  Force was in the inventory; restoring Brute Force returned it to 127 (the conversion changes the baseline). Cluster
  passives are never in a radius jewel's radius: Path of Building precomputes `nodesInRadius` before its cluster graphs
  are built, so the server's ordinary-PSG-only radius is intentional. The 33 threshold jewels have generated item
  sources (`scripts/re/threshold_jewels.py`, `ThresholdJewelCatalogue.cs`); the client applies their local threshold
  mods. Current Dead Reckoning gives skeleton hits flat chaos from the equipped shield's local energy shield without
  an attribute threshold. `ThresholdJewels.cs` parses the target/attribute mask, sums every ordinary passive in radius (allocated or
  not, PoB's `Threshold` pass) and adds the mapped stat when the total is met. Live: `Combat Focus Cobalt` in socket
  36634 logged `elemental_hit_cannot_roll_fire_damage 1` (60 Int); `Collateral Damage` in socket 60735 logged
  `galvanic_arrow_area_damage_+% 50` and `shrapnel_shot_radius_+% 25` (60 Dex); `Spirited Response` in socket 6
  (cell 8) changed mana 2355 -> 2476 from its 10% maximum mana global. The skill-specific consumers remain partial and
  targets without a client stat row are skipped. Unnatural Instinct in cell 3 changed life 5520 -> 5685, mana
  2355 -> 2326 and ES 277 -> 285 from three allocated Int smalls and three unallocated Life/ES smalls. Lioneye's Fall's
  radius now converts melee and melee-weapon stats with native bow equivalents, including physical damage, attack
  speed and critical modifiers. Conditional modifiers without a mapped bow stat still need individual consumers.
  Warrior's Tale scales tattoo stats within its socket radius. Grand Spectrum rolls one of three current variants
  per base and scales by the total equipped across bases (limit three). Inspired Learning's rare-mod steal and the
  skill-specific threshold consumers remain partial. The socket/save path,
  `PassiveTree.NodesInRadius`/`NodesInRing`, the timeless per-node pattern and the art table are reused. The radius
  suite has 98 tests.

- Impossible Escape (generated special): `/impossible_escape <keystone>|list` creates the 48-variant Viridian Jewel
  (Small, limit 1, corrupted). The item mod stores the keystone's `PassiveSkillGraphId` and display base radius 800;
  the effect radius is Path of Building 3_16's Small **960**. Legacy items with the Maven table row are migrated on
  load. The allocated socket grants disconnected passives around the named keystone, excluding the keystone itself.
  A live Witch allocated Dire Torment (9015) without taking Supreme Ego (49639). Earlier client checks also placed
  Replenishing Remedies (50690) within the ring and refused pickup while a disconnected node depended on the jewel. Rational Doctrine (`/unique_jewel`) creates
  consecrated ground while stationary with Strength highest and profane ground on a critical strike with Intelligence
  highest; both effects linger for 4 seconds, and hits against enemies on profane ground have 100% increased critical
  chance. The profane-ground debuff and its critical-chance consumer have automated coverage. Live 2026-11-01 (the
  item's stats set through `/set_stat`): a critical strike created the profane ground art, applied buff 1569, and
  removed it after the linger.

- Watcher's Eye, Bound By Destiny, Sublime Vision, The Balance of Terror and Megalomaniac have named item generation.
  Their encounter drop rates and individual conditional-stat consumers are partial. Megalomaniac adds three distinct
  notables and an empty small passive. Forbidden Flame and Forbidden Flesh still lack a mainline source.

- 基于命令的普通升华选择、独立的点数发放、连通的分配、付费退点与保存。
  一次原生的决斗者/屠杀者冒烟核对过普通节点、升华分配/退点与两个点数预算。
  选择与进度在重登与 Gateway 进程重启后都保留；一次 Phrecia 请求没有改动已保存的 JSON。
  只读的客户端追踪观察到职业字节、Default 图谱、已分配节点与独立的点数计数器。
  自动化领域、报文与生产处理函数测试覆盖归属、独立预算、图谱连通性、多选效果、原子退点、过期连接与旧/未知存档。
  区域准入在改动背包之前校验已保存的进度，然后在金币迁移之前校验物品。
  等级变化在应用前会重查并发的天赋变更；排队的树报文会发布当前状态。
  旧的 XP 事件无法撤销后来的收益或显式等级重置；Auth 跟随当前 XP。
  旧区域的收益无法压制活跃连接待处理的等级发布。
  等级换算在更新运行时状态之前，以原子方式采纳已保存完成的降级/退点。
  额外点数的发放会在运行时发布之前提交，包含并发 XP 更新与写入失败检查。
  代码：`AscendancyCatalog`、`AscendancyProgress`、`OrdinaryPassiveRules`、`CharacterSaves`、Gateway 的进度相关部分。
  文档：[范围、证据与入门步骤](../docs/game/ascendancies.md)。

- Antiquarian：全部九个显著天赋与九个小天节点通过三套合法的连通八点构建执行。
  原生核对覆盖 Ward/Adrenaline、最终属性/需求、眩晕恢复、Enduring Cry 时长、必中攻击、Far Shot 箭矢与
  持盾格挡/远程减伤。已核对付费全额退点、重新分配、暂停、死亡、跨区域携带、重登与 Gateway 重启。
  选择需要 `Game_VERIFIED_ANTIQUARIAN=1`，并以只读方式核验所安装的兼容图谱完全一致。
  全额退点后选择普通 Juggernaut 会恢复 Default 图谱。
  文档：[整职业证据、配置与明确建模边界](../docs/game/antiquarian.md)。
- Juggernaut：全部 14 个普通节点都会生效并通过原生验收，[逐节点](../docs/game/juggernaut.md)。
- Elementalist Shaper of Flames: all three stats work. `always_ignite` adds 100% ignite chance;
  `all_damage_can_ignite` lets any hit ignite and puts every damage type into the ignite's base (90% per second,
  fire scaling plus the types the portion visited, as poison's physical part); the panel's ignite rows follow.
  `enemies_ignited_by_you_have_physical_damage_%_converted_to_fire` (40%) converts a burning monster's hits on the
  player through the Born under Fire hook (`EnemyAuraConversionLocked`). Verified live on 3.29.3c with Arc: 40 of 40
  surviving hits ignited, 56 converted monster hits, no client crash. Not covered: the golem and Abberath ignites
  still need fire damage; the conversion applies to hits only.

- Antiquarian: all nine notables and nine small nodes execute through three legal connected eight-point builds.
  Native checks cover Ward/Adrenaline, final attributes/requirements, stun recovery, Enduring Cry duration,
  always-hit attacks, Far Shot arrows and active-shield block/ranged mitigation.
  Paid full refunds, reallocation, pause, death, area carry, relog and Gateway restart were checked.
  Selection requires `Game_VERIFIED_ANTIQUARIAN=1` and read-only verification of the exact installed compatible graph.
  Ordinary Juggernaut selection after a full refund restores the Default graph.
  Docs: [whole-class evidence, setup and explicit model boundaries](../docs/game/antiquarian.md).
- Juggernaut: all 14 ordinary nodes apply and passed native acceptance, [node by node](../docs/game/juggernaut.md).
- Slayer: every ordinary node's stats apply except Impact's melee range, checked in the client except overkill leech
  and the while-leeching defences, [node by node](../docs/game/slayer.md).

- Gambler：其已安装图谱里的每个节点都有玩法消费者，包括两个格挡幸运选项及其代价。
  合法八点构建、互斥选择、付费退点、保存失败回滚与重载有 Gateway 覆盖。
  来自普通怪物的异常状态让 Risk Aversion 有了实际的规避消费者，同时还有有界眩晕恢复。
  原生核对覆盖合法构建、两个选项、出手/受击的抗性结果、伤害、真实点燃规避、致死时的死亡面板/检查点行为、退点、重登与重启。
  当前基线的构建/测试与精确的相关 head 组合保留了已文档化的共享合并解决方式。
  Default Phrecia 准入被闸住；显式选择 Gambler 会以只读方式核验已安装图谱字节。
  文档：[Gambler 清单、范围与验证](../docs/game/gambler.md)。
- Passive masteries, with Block, Fire and Wand Mastery effects. Docs: [15](../docs/protocol/15-progression.md) §2.
- Passive tree jewel sockets, percentage attribute mods and The Adorned (tree art check pending).
  Docs: [15](../docs/protocol/15-progression.md) §2a, [unique jewels](../docs/game/unique-jewels.md).
- Cluster jewels: the single-keystone smalls' keystone stats, nesting, the client's layout and theme stats; keystones,
  radius rules and the vendor recipe are missing. Docs: [cluster jewels](../docs/game/cluster-jewels.md).
- Timeless jewels: the alternate tree in radius and Militant Faith's Devotion mods; conqueror keystone mechanics are
  missing. Docs: [timeless jewels](../docs/game/timeless-jewels.md).
- Radius unique jewels: 42 implemented and the 33 threshold jewels; skill-specific consumers are partial.
  Docs: [radius jewels](../docs/game/radius-jewels.md), [list](../docs/game/radius-jewel-list.md).
- Impossible Escape and Rational Doctrine. Docs: [radius jewels](../docs/game/radius-jewels.md),
  [13](../docs/protocol/13-items.md), "What some flasks and Rational Doctrine make".
- Watcher's Eye, Bound By Destiny, Sublime Vision, The Balance of Terror and Megalomaniac items; their drops and
  conditional consumers are partial. Docs: [unique jewels](../docs/game/unique-jewels.md).
- Ordinary ascendancy selection, point awards, allocations, paid refunds and saves.
  Docs: [ascendancies](../docs/game/ascendancies.md).
- Elementalist Shaper of Flames. Docs: [20](../docs/protocol/20-damage.md) §3.
- Antiquarian. Docs: [Antiquarian](../docs/game/antiquarian.md).
- Juggernaut. Docs: [Juggernaut](../docs/game/juggernaut.md).
- Slayer, except Impact's melee range. Docs: [Slayer](../docs/game/slayer.md).
- Gambler. Docs: [Gambler](../docs/game/gambler.md).

## 部分实现

- Behemoth：可选 Phrecia 模块提供其能力、已核验图谱的选择加入与按模式的节点来源。
  共享的 Rampage 层与 Vaal Ground Slam 的付款在 Game 中执行，带生成/来源退役保护。
  两个确定性的 Gateway 回归覆盖工作线程归属与原子化的攻击/付款准入。
  有界的模块化原生核对覆盖选择/来源、真实命中/击杀层数、重载/重进、15 层派发/过期、付费退点与普通重载对照；
  历史上的整职业证据保留其原始来源范围。最终精确 head 的全套测试、评审与原生回执修订/结果记录在 PR 里。
  文档：[Behemoth 的消费者、合法构建与验证限制](../docs/game/behemoth.md)。
- Whisperer：可选模块提供实际的 Deadeye/Ranger 描述符与已分配节点来源。
  通用的魔力、消耗、Recoup 与共享致盲消费者在 Game 中运行，各有独立的生命周期状态。
  两个确定性的 Gateway Facts 覆盖实际准入、合法构建、付费退点与资源/生命周期组合，
  包括当前 Game 的实用型 End 自动化与替换期间保留的付费恢复。
  当前 main 的本地构建、套件、独立评审与有界原生核对覆盖资源与重载路径。
  空间分叉、完整的 Vaal/Rampage 与完整的归属召唤物矩阵不在该验收范围内。
  文档：[Whisperer 的消费者、合法构建与验证限制](../docs/game/whisperer.md)。
- Aristocrat：其已安装图谱里的每个节点都有服务端消费者（宝石等级与品质、属性需求、条件性异常免疫、
  最低属性伤害、赠送天赋点），并有用例覆盖合法构建、付费退点与重载。尚未用真实客户端核对：那需要安装经过评审的合并图谱。
  文档：[Aristocrat 清单](../docs/game/aristocrat.md)。
- Ancestral Commander：防御节点有服务端消费者（护甲与生命、Ancestral Endurance、Toughened Skin 的动作/移动下限
  与翻倍身体护甲、Ancestral Defiance），并有用例覆盖合法构建、付费的 Ancestral Spirit 选择退点与重载。
  Call of the Ancestors 加快 Enduring Cry 的释放并放缓其一次性冷却，记录与客户端以同样方式推导；
  Deathrattle 会爆炸并消耗服务端的尸体，并在释放时打开任务宝箱。
  爆炸取玩家的全局伤害与转换；Deathrattle 是部分实现：没有额外伤害、命中效果或美术，只对 Enduring Cry 生效，
  且除任务宝箱外不开其他箱子。
  Ancestral Reach 的灵魂打击命中客户端所选怪物；Ancestral Echo 在服务端打击前一个位置，
  并把客户端需要在那里也打击的第一个位置发过去。
  两者对类型为投射物的打击宝石都不生效（Frost Blades、Lightning Strike、Molten Strike、Wild Strike 及其转化宝石，
  除 Frost Blades of Katabasis 之外）：服务端对它们没有近战命中，尽管客户端会画出灵魂打击。
  Born under Fire 会转换附近怪物的物理命中并给它们施加减益。
  Spirits of Kahuturoa、Utula 与 Kiloava 在传奇怪物的暴击、Savage Hit 或被格挡命中时触发，共享冷却并给出其 Leadership 增益
  （Kahuturoa 的护甲份额与 Kiloava 的格挡恢复会生效；Utula 的偷取没有可偷取的对象）；
  每位酋长在增益持续期间在玩家身边作战，只会用第一个技能。
  服务端怪物没有基础暴击率，所以 Kahuturoa 只在通过 `/mob_mods` 给了必暴词缀的传奇怪物上触发，正常游玩中永远不会触发。
  Forest of Ancestors 的 +1 图腾与「最近图腾承担 20% 命中」对回春图腾生效，那是服务端唯一会摆放的图腾技能；
  在传奇怪物附近被击杀的图腾会触发 Spirit of Kaom，其 Leadership 加 25%。
  尚未做：Spirit of Rakiata（没有 Marks）、怪物攻击图腾以及其他图腾技能。
  `Game_VERIFIED_ANCESTRAL_COMMANDER=1` 选择加入；尚未用真实客户端核对。
  文档：[Ancestral Commander 清单](../docs/game/ancestral-commander.md)。
- Paladin：九个小天节点（生命回复、光环效果）、Anointed Champion（静止时圣化地面、其留存与 50% 提高效果）、
  Paladin Aura 选项（赠送 30 级 Determination 或 Grace、40 级 Discipline）、Light of Divinity（附近怪物承受 25% 提高的元素伤害）、
  Bring the Battle（战吼以无限威力嘲讽；被嘲讽怪物承受 20% 提高伤害）、Elemental Valiance（触发式元素遗物，给出其光环并爆炸，
  无情模式里也有）、Absolution（连接期间 40% 光环效果）、Deep Connections（连接玩家的遗物、+5 连接宝石等级）与
  Field Medicine（40% 魔法功能药剂效果；对已连接目标使用药剂，遗物按目标的移动速度行走）都有服务端消费者，
  并有用例覆盖三套合法构建、Paladin Aura 选项互换、付费退点与重载。
  `Game_VERIFIED_PALADIN=1` 在经过评审的合并图谱上选择加入。
  没有消费者：诅咒、队友、死亡经验惩罚、已连接召唤物的药剂防御，以及 Bring the Battle 对其他目标的较少伤害
  （怪物只打玩家）。
  未应用：已连接召唤物的药剂相位，以及遗物爆炸上的药剂伤害（未验证）。
  尚未用真实客户端核对。文档：[Paladin 清单](../docs/game/paladin.md)。
- Polytheist：每个节点都有服务端消费者或已文档化的引擎缺口，并有用例覆盖三套合法构建、付费退点与重载。
  十二个小天节点（防御；光照半径由客户端应用）、Devout of the Brine King 与 Ralakesh 的最低充能都有消费者。
  Devout of Garukhan（移动中的闪避与回复、不受感电影响）与 Devout of Ryslatha（更多最大攻击伤害、
  近期被命中的敌人没有回复）有消费者，并与普通构建做过 Gateway 对照。
  Devout of Gruthkul（禁用法术且不造成伤害、生命、回复、命中你的敌人攻击更慢；其挥击动画是否变慢未验证）、
  Devout of Lunaris（目标处的新星、多两个法术投射物、更多法术范围）与 Devout of Solaris（对附近敌人的致盲光环、
  对被致盲敌人的更高暴击率与更少伤害；其降低暴击率没有可降低的对象，因为怪物只通过 `always_crit` 这个覆盖项暴击）
  同样有消费者。
  Devout of Abberath 在行走时每一步触发 Abberath's Fury（其半径来自爆炸美术，未验证），其命中会点燃怪物，
  客户端画出燃烧的蹄印，玩家身上的点燃不造成任何伤害。
  Devout of Yugul、Ralakesh 的腐化之血免疫与 Garukhan 的地面免疫在等诅咒、反弹伤害、腐化之血与危险地面。
  `Game_VERIFIED_POLYTHEIST=1` 选择加入。
  尚未用真实客户端核对：那需要安装经过评审的合并图谱。
  文档：[Polytheist 清单](../docs/game/polytheist.md)。
- Surfcaster：Baited Breath（持钓竿时每次命中都暴击）、Screaming Reels（持竿 60% 更多施法速度，在服务端等待与原生片段里都有）、
  Sea Legs、Stormy Seas 的转换、Brine Bound 的无法被冰冻与小天节点的施法速度都有消费者，
  并有用例覆盖合法构建、付费退点与重载。
  Ghost of the Deep 的 Torment Touched 增益给出四项 23% 加成并显示在玩家身上。
  Changing Seasons 让任何命中都能冰缓玩家，并使冰缓变成提高动作速度。
  Stormy Seas 的冰霜伤害会使怪物感电（通用的怪物冰缓/冰冻/感电引擎）。
  在该引擎上，Glacial Wave 的冰缓提高怪物承受的伤害并减弱其挥击，Brine Bound 会冻结命中你的怪物并把每次冰冻延长 1 秒，
  Within the Tempest 会给击杀造成感电并把感电扩散 1.5 米。
  小天节点的「使怪物逃跑」几率会让被命中的怪物走开且不攻击。
  尚未用真实客户端核对：那需要安装经过评审的合并图谱。
  钓鱼（鱼的稀有度、腐化的鱼）在 3.29.3b 里没有玩法。
  `Game_VERIFIED_SURFCASTER=1` 选择加入。
  文档：[Surfcaster 清单](../docs/game/surfcaster.md)。
- Phrecia 的身份、图谱归属与点数/存档生命周期有自动化覆盖。
  既有职业能力与天赋来源被抽取进可选模块 `PhreciaAscendancies`。
  Phrecia 赛季显式列出它；Idols 仍是独立未实现的功能。
  模块缺失会拒绝已保存家族的准入而不改动进度；重载要求受影响的玩家重新进入。
  有界的模块化原生核对覆盖三个职业来源、付费退点、普通对照与重载重进。
  下面的历史整职业核对保留其原始范围。
  文档：[模块边界、范围与重载策略](../docs/game/phrecia-modules.md)。
  Harbinger 有选择加入的同伴、诅咒、接受方时钟与带类型的资源消费者。
  有界客户端核对覆盖召唤、Focus、诅咒、计时器、旅行与重载；完整矩阵仍是部分实现。
  见 [Harbinger](../docs/game/harbinger.md)。
  Phrecia 的游戏连接/选择默认被闸住；Antiquarian 与 Gambler 已有上文所述的验收通过的选择加入。
  普通启动不会安装或恢复图谱，普通/未升华的游玩没有替换图谱的预检。
  原生迷宫/祭坛流程是另一件事（#151）；其他 Phrecia 职业玩法也属独立工作。
  原生普通命令核对不能证明扩展的选择器或特殊职业玩法。
  Forest Tracking 由最终闪避换算的攻击伤害加成与既有的 Steel Willed 计算共用。
  自动化检查覆盖实际节点分配/退还/重载、最终防御、带符号的组合与攻击消费者。
  十七项原生原始输入对照覆盖真实伤害、护甲/闪避组合、额外伤害与重登。
  完整的 Daughter of Oshabi 玩法仍属独立工作。
  Herald、Architect of Chaos、Paladin 与 Puppeteer 家族宝石等级会进入实际的技能记录与计算。
  自动化检查覆盖真实连通分配、付费退点、重载、受支持的等级与固有需求。
  二十八项原生核对覆盖有效家族等级、无关来源、移除、存储不变与重登。
  家族专属执行与 Phrecia 原生准入仍是独立工作。
  文档：[范围与兼容闸门](../docs/game/ascendancies.md)。
- 无情模式默认从 68 级起应用一个可配置的复利 XP 倍率。
  具体数值是 LocalExile 的调整值，因为零售服务端公式未公开。
  原生无情模式的天赋属性/点数替代项会为每条无情连接选中。
  自动化配置测试覆盖有效的实机改动，以及在非法输入后保留最后一个有效快照。
  原生天赋替代项与复利等级公式有自动化覆盖。
  见 [赛季模式](../docs/game/league-modes.md)。
- Hierophant: every ordinary node's stats apply except the Brand ones (no Brand executes), checked in the client
  except the halved shock while Arcane Surge runs,
  [node by node](../docs/game/hierophant.md).
- Behemoth: the optional Phrecia module supplies its capability, verified-graph opt-in and mode-aware node sources.
  Shared Rampage tiers and Vaal Ground Slam payment execute in Game with generation/source retirement guards.
  Two deterministic Gateway regressions cover worker ownership and atomic attack/payment admission.
  Bounded modular native checks cover choice/source, real hit/kill stacks, reload/re-entry, tier-15 dispatch/expiry,
  paid refund and ordinary reload controls; historical whole-class evidence retains its original source scope.
  Final exact-head full-suite, review and native receipt revisions/results are recorded in the PR.
  Docs: [Behemoth consumers, legal builds and validation limits](../docs/game/behemoth.md).
- Whisperer: the optional module supplies the actual Deadeye/Ranger descriptor and allocated node sources.
  Generic Mana, spending, Recoup and shared Blind consumers run in Game with independent lifetime-owned state.
  Two deterministic Gateway Facts cover actual admission, legal builds, paid refunds and resource/lifecycle composition,
  including current-Game utility End automation and retained paid recovery across replacement.
  Current-main local build, suites, independent review and bounded native checks cover the resource and reload paths.
  Spatial forks, full Vaal/Rampage and the complete owned-minion matrix remain outside that acceptance.
  Docs: [Whisperer consumers, legal builds and validation limits](../docs/game/whisperer.md).
- Aristocrat: every node in its installed graph has a server consumer (gem levels and quality, attribute requirements,
  conditional ailment immunity, lowest-attribute damage, granted passive points), with Gateway coverage of legal builds,
  paid refunds and reload. Not yet checked with the real client: that needs the reviewed combined graph installed.
  Docs: [Aristocrat checklist](../docs/game/aristocrat.md).
- Ancestral Commander: the defensive nodes have server consumers (Armour and Life, Ancestral Endurance, Toughened
  Skin's action/movement floors and doubled body armour, Ancestral Defiance), with Gateway coverage of legal builds,
  the paid Ancestral Spirit choice refund and reload.
  Call of the Ancestors speeds Enduring Cry's release and slows its one-use cooldown, which the record and the client
  derive the same way; Deathrattle explodes and consumes the server's corpses and opens the quest chests at the release.
  The explosion takes the player's global damage and conversion; Deathrattle is partial: no added damage, on-hit
  effects or art, only Enduring Cry, and no chests beyond the quest ones.
  Ancestral Reach's spirit strikes hit the monsters the client picks; Ancestral Echo strikes the previous location on
  the server and sends the client the first location it needs to strike there too.
  Neither acts for the Strike gems typed Projectile (Frost Blades, Lightning Strike, Molten Strike, Wild Strike and
  their transfigured gems except Frost Blades of Katabasis): the server has no melee hit for them, though the client draws their spirit strikes.
  Born under Fire converts nearby monsters' physical hits and puts its debuff on them.
  Spirits of Kahuturoa, Utula and Kiloava trigger on a Unique's crit, Savage Hit or blocked hit, with the shared
  cooldown and their Leadership buff (Kahuturoa's armour share and Kiloava's block recovery act; Utula's leech has no
  leech to act on); each chieftain fights beside the player for the buff's duration, with its first skill only.
  Server monsters have no base critical strike chance, so Kahuturoa triggers only on a Unique given an always-crit mod
  through `/mob_mods`, never in normal play.
  Forest of Ancestors' +1 totem and its 20% of hits from the nearest totem act on Rejuvenation Totems, the one totem
  skill the server places; a totem killed while a Unique is near triggers Spirit of Kaom, whose Leadership adds 25%.
  Not yet: Spirit of Rakiata (no Marks), monsters attacking totems and other totem skills.
  `Game_VERIFIED_ANCESTRAL_COMMANDER=1` opts in; not yet checked with the real client.
  Docs: [Ancestral Commander checklist](../docs/game/ancestral-commander.md).
- Paladin: the nine small nodes (life regeneration, aura effect), Anointed Champion (consecrated ground while
  stationary, its linger and 50% increased effect), the Paladin Aura options (granted level 30 Determination or Grace,
  level 40 Discipline), Light of Divinity (nearby monsters take 25% increased elemental damage), Bring the Battle
  (warcries taunt with infinite power; taunted monsters take 20% increased damage), Elemental Valiance (triggered
  elemental relics that give their aura and explode, in Ruthless too), Absolution (40% aura effect while linked), Deep
  Connections (links to the player's relics, +5 link gem levels) and Field Medicine (40% magic utility flask effect;
  flasks on linked targets, whose movement speed a relic walks at) have server consumers, with Gateway coverage of
  three legal builds, the Paladin Aura option swap, paid refunds and reload.
  `Game_VERIFIED_PALADIN=1` opts in on the reviewed combined graph.
  No consumer: curses, party allies, a death experience penalty, a linked minion's flask defences, and Bring the
  Battle's less damage against other targets (monsters only hit the player).
  Not applied: a linked minion's flask phasing, and flask damage on a relic's explosion (unverified).
  Not yet checked with the real client. Docs: [Paladin checklist](../docs/game/paladin.md).
- Polytheist: every node has a server consumer or a documented engine gap, with Gateway coverage of three legal
  builds, paid refunds and reload.
  The twelve small nodes (defences; light radius, which the client applies), Devout of the Brine King and Ralakesh's
  minimum charges have consumers.
  Devout of Garukhan (evasion and regeneration while moving, Unaffected by Shock) and Devout of Ryslatha (more maximum
  attack damage, no regeneration on enemies hit recently) have consumers and Gateway coverage against a plain build.
  So do Devout of Gruthkul (spells disabled and dealing no damage, life, regeneration, enemies that hit you attack
  slower; whether their swing animation slows is unverified), Devout of Lunaris (Novas at the target, two more spell
  projectiles, more spell area) and Devout of Solaris (a blind aura on nearby enemies, more critical chance against and
  less damage from blinded enemies; their reduced critical chance has nothing to reduce, because monsters crit only
  through `always_crit`, an override).
  Devout of Abberath triggers Abberath's Fury on each footstep while walking (its radius comes from the explosion art
  and is unverified), its hits ignite monsters, the client draws the burning hoofprints, and ignites on the player
  deal nothing.
  Devout of Yugul, Ralakesh's Corrupted Blood immunity and Garukhan's ground immunity wait for curses, reflected
  damage, Corrupted Blood and hazardous ground.
  `Game_VERIFIED_POLYTHEIST=1` opts in.
  Not checked with the real client: that needs the reviewed combined graph installed.
  Docs: [Polytheist checklist](../docs/game/polytheist.md).
- Surfcaster: Baited Breath (every hit crits while holding a Fishing Rod), Screaming Reels (60% more cast speed with a
  rod, in the server wait and the native clip), Sea Legs, Stormy Seas' conversion, Brine Bound's Cannot be Frozen and
  the small nodes' cast speed have consumers, with Gateway coverage of legal builds, paid refunds and reload.
  Ghost of the Deep's Torment Touched buff gives its four 23% bonuses and shows on the player.
  Changing Seasons lets any hit chill the player and makes the chill increased action speed.
  Stormy Seas' cold damage shocks monsters (the general monster chill/freeze/shock engine).
  On that engine, Glacial Wave's chill raises the monster's damage taken and lessens its swings, Brine Bound freezes
  monsters that hit you and holds every freeze for 1 s, and Within the Tempest shocks kills and spreads shocks 1.5 m.
  The small nodes' chance to cause monsters to flee makes a hit monster walk away without attacking.
  Not yet checked with the real client: that needs the reviewed combined graph installed.
  Fishing (fish rarity, corrupted fish) has no gameplay in 3.29.3b.
  `Game_VERIFIED_SURFCASTER=1` opts in.
  Docs: [Surfcaster checklist](../docs/game/surfcaster.md).
- Phrecia identities, graph ownership and the point/save lifecycle have automated coverage.
  Existing class capabilities and passive sources are extracted into the optional `PhreciaAscendancies` module.
  The Phrecia league lists it explicitly; Idols remains a separate unimplemented feature.
  Module absence refuses saved-family admission without changing progress; reload requires affected players to re-enter.
  Bounded modular native checks cover the three class sources, paid refunds, normal controls and reload re-entry.
  Historical whole-class checks below retain their original scope.
  Docs: [module boundary, scope and reload policy](../docs/game/phrecia-modules.md).
  Harbinger has opt-in companions, curses, recipient clocks and typed resource consumers.
  Bounded client checks cover summons, Focus, curses, timers, travel and reload; the full matrix remains partial.
  See [Harbinger](../docs/game/harbinger.md).
  Phrecia game connections/selection are gated by default; Antiquarian and Gambler have accepted opt-ins above.
  Normal startup does not install or recover a graph and ordinary/unascended play has no alternate-graph preflight.
  The native Labyrinth/Altar flow is separate (#151); other Phrecia class gameplay remains separate work.
  Native ordinary command checks do not establish an expanded chooser or special class gameplay.
  Forest Tracking's attack-damage bonus from final evasion shares the existing Steel Willed calculation.
  Automated checks cover actual node allocation/refund/reload, final defences, signed composition and attack consumers.
  Seventeen native raw-input controls cover real damage, armour/evasion composition, added damage and relog.
  Full Daughter of Oshabi gameplay remains separate.
  Herald, Architect of Chaos, Paladin and Puppeteer family gem levels reach actual skill records and calculations.
  Automated checks cover real connected allocation, paid refunds, reload, supported levels and intrinsic requirements.
  Twenty-eight native checks cover effective family levels, unrelated sources, removal, unchanged storage and relog.
  Family-specific execution and Phrecia native admission remain separate.
  Docs: [scope and compatibility gate](../docs/game/ascendancies.md).
- Ruthless applies a configurable compounding XP multiplier from level 68 by default.
  The numeric rate is LocalExile tuning because the retail server formula is not public.
  Native Ruthless passive stat/point alternatives are selected for each Ruthless connection.
  Automated configuration tests cover valid live edits and retaining the last valid snapshot after invalid input.
  Native passive alternatives and the compounding level formula have automated coverage.
  See [league modes](../docs/game/league-modes.md).
- Hierophant, except the Brand nodes. Docs: [Hierophant](../docs/game/hierophant.md).
- Behemoth through the optional Phrecia module. Docs: [Behemoth](../docs/game/behemoth.md).
- Whisperer through the optional module. Docs: [Whisperer](../docs/game/whisperer.md).
- Aristocrat (not checked live). Docs: [Aristocrat](../docs/game/aristocrat.md).
- Ancestral Commander (not checked live). Docs: [Ancestral Commander](../docs/game/ancestral-commander.md).
- Paladin (not checked live). Docs: [Paladin](../docs/game/paladin.md).
- Polytheist (not checked live). Docs: [Polytheist](../docs/game/polytheist.md).
- Surfcaster (not checked live). Docs: [Surfcaster](../docs/game/surfcaster.md).
- Phrecia ascendancies: the optional module, Harbinger, Forest Tracking and family gem levels.
  Docs: [Phrecia modules](../docs/game/phrecia-modules.md), [Harbinger](../docs/game/harbinger.md),
  [ascendancies](../docs/game/ascendancies.md).
- Ruthless experience and passive alternatives. Docs: [league modes](../docs/game/league-modes.md).

## 尚未实现

- 保存任务进度。
  任务天赋/退点奖励与原生迷宫/祭坛获取（#151）仍缺。
  其余特殊升华玩法与更广的 Phrecia 客户端兼容是独立工作。
- 纹身：3.29.3b 的 100 条目录、协议与实现分组记录在
  [tattoos](../docs/game/tattoos.md)。客户端流程已实现：C->S `0x77` 点名背包、物品与天赋；`0x33` 的第五个列表携带
  `node | PassiveSkillOverrides.HASH16 << 16`；物品被消耗，映射独立于分配保存。**45 行已由客户端核对**：
  十个试点、22 个纯属性、九个 Makanga 闸门、三个 Journey 纹身与 Journey Makanui；Ancestral Bloodlines 是部分实现
  （生命/魔力回复未读），Hinekora/Kitava Makanga 无效。Firewalker 还跨重登保留，并且用重铸石移除成功。
  Makanga 闸门、Journey 计数与五个 `PassiveSkillOverrides.Limit` 分组在服务端强制执行；Makanui 把其随机核心天赋
  存进 `TattooKeystones`。测试会话里该角色目前有 530 个天赋与 12 个纹身。52 行仍未实现；
  下一步是条件/技能行、Loyalty 与七个 Forbidden 升华纹身。
- Saving quest progress.
  Quest passive/refund rewards and native Labyrinth/Altar acquisition (#151) remain missing.
  Remaining special ascendancy gameplay and broader Phrecia client compatibility are separate work.
- Tattoos: the 100-item 3.29.3b catalogue, protocol and implementation groups are documented in
  [tattoos](../docs/game/tattoos.md). The client flow is implemented: C->S `0x77` names the inventory, item and
  passive; `0x33`'s fifth list carries `node | PassiveSkillOverrides.HASH16 << 16`; the item is consumed and the
  map is saved independently of allocation. **66 rows are client-verified**: the ten pilot, 22 plain stats, the
  nine skill/minion/flask plain-stat rows, the knockback row, the eleven Honoured rows (attribute notables
  included), nine Makanga gates, three Journey tattoos and Journey Makanui; Ancestral Bloodlines is partial
  (life/mana recovery unread) and Hinekora/Kitava Makanga are inert. Firewalker also survived relog and was removed
  with an Orb of Scouring. The Makanga gate, Journey counting and the five `PassiveSkillOverrides.Limit` groups are
  enforced server-side; Makanui stores its random keystone in `TattooKeystones`. 31 rows remain not implemented:
  the nine ailment and avoidance rows, five rows that need new consumers (corpse consumption, Fortify-on-stun,
  guard duration, hinder and herald effect), the ten Loyalty summons and the seven Forbidden ascendancy tattoos.
- Saving quest progress, quest passive/refund rewards and native Labyrinth/Altar acquisition (#151).
- Tattoos: 75 rows work in the client; 22 rows (new consumers, Loyalty summons, Forbidden tattoos) are missing.
  Docs: [tattoos](../docs/game/tattoos.md).
