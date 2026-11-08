# 路线图：传奇珠宝

属于 [路线图索引](../Roadmap.md)；天赋部分在 [roadmap/progression.md](progression.md)，
完整的传奇珠宝目录在 [docs/game/unique-jewels.md](../docs/game/unique-jewels.md)。

本页讲的是**镶嵌在天赋树里的传奇珠宝**。普通、魔法与稀有珠宝已经通过 `PassiveJewels1` 背包可用
（见 [status/progression.md](../status/progression.md)）；集群珠宝是另一项独立改动。

## 已经做完的部分

- 天赋树上 21 个普通大珠宝孔已映射到 `PassiveJewels1` 面板（Inventories.dat 第 23 行，背包 id 24，60x1）。
  面板里的格子就是 `PassiveJewelSlots.dat` 的行；该行的 `Slot` 列是插孔节点在 `PassiveSkills.dat` 里的行。
- 普通、魔法或稀有珠宝能放进已分配的天赋孔（C->S 0x2a 或 0x4a）、被拾起（0x28）、随角色的背包一起保存，
  其全局词缀通过 `ItemModStats` 生效，与已穿戴物品一致。
- 分配在服务端校验：插孔节点必须已分配、物品必须是非集群的 `Jewel`、且满足其等级需求。退还仍插着珠宝的天赋孔会被拒绝，
  直到珠宝被取出；等级下调若不得不让出该孔，会把珠宝退回主背包。
- 传奇珠宝走同一套插孔与属性路径，所以一件带纯粹无条件全局词缀的既有传奇物品已经能生效。
  **但目前还没有传奇物品目录、生成器、掉落与商人条目**，所以正常游玩里拿不到传奇珠宝。
- **试点完成：The Adorned。** `/adorned [0-1000]` 生成腐化的传奇 Crimson Jewel；它的
  `0-100% increased Effect of Jewel Socket Passive Skills containing Corrupted Magic Jewels` 会缩放普通天赋孔里的
  腐化魔法珠宝。实机核对：一颗腐化魔法珠宝给了 +15% 火焰抗性，100% 的 The Adorned 把它变成 +30%
  （225% -> 240% -> 255%）。集群插孔里的珠宝与 3.25 之前的 50-150% 变体未覆盖。

## 尚未完成的部分

- 没有任何传奇物品定义（名称、基底类型、外观标识、词缀列表、变体），也没有 `/item`/掉落/商人路径。
- 没有条件属性引擎：目前所有非局部词缀都无条件生效，所以「if you have」「recently」「on kill」、按充能与阈值类词缀都会算错。
- 没有天赋树的半径模型，所以「in Radius」类珠宝、阈值珠宝与改节点的珠宝都未建模。
- 没有永恒珠宝（军团）的种子、替换天赋树或核心天赋替换。
- 深渊珠宝属于物品上的深渊插孔，不在本页范围内。
- 集群珠宝（扩展插孔、代理天赋与显著天赋）是另一个 PR。

## 计划

### 1. 传奇物品目录与来源

- 用客户端的 `UniqueStashLayout`、`Words`、`BaseItemTypes`、`ItemVisualIdentity` 与 `Mods` 构建
  `data/unique-jewels.json`，并与 Path of Building 的 `src/Data/Uniques/jewel.lua`（目录所用的同一份数据）交叉核对。
  每条需要：稳定的传奇名、基底物品类型、传奇外观标识、内在/外在词缀的 HASH16 与数值、变体、等级需求，以及
  `UniqueJewelLimits` 中的对应项。
- 加上 `/item <传奇珠宝名>`（以及后续的传奇掉落/商人来源）以便测试。物品必须以 `ItemRarity.Unique`、传奇外观标识与
  正确的词缀列表下发；客户端会用自己的表推导出名字。
- 测试：目录的每一行都能生成可序列化的物品，基底、外观与词缀数量符合预期；未知名字被拒绝；重载后物品仍在。
- 完成标准：`/item Might of the Meek` 能生成该物品，后续阶段可以用它。

### 2. 全局型传奇珠宝（目录里 42 个）

- 它们只有无条件的角色属性。现有的 `ItemModStats.Global` 路径已经能应用；本阶段要做的是让这些物品能获得，
  并证明属性映射正确。
- 核对属性/等级需求、数量限制与变体（例如 3.28 之前的 Bloodnotch 变体）。
- 每类属性各加一个测试：属性、生命/魔力、抗性、伤害、速度、回复、召唤物属性、腐化标记与「cannot」类属性。
- 完成标准：每一行 Global 都能被创建、镶嵌进已分配的天赋孔，并产生与物品提示一致的最终属性键；
  不支持的属性键要列出来，而不是悄悄忽略。

### 3. 条件型传奇珠宝（还剩 40 个；The Adorned 已完成）

- The Adorned 作为试点已实现（只覆盖普通天赋孔；见上）。
- 在成本低的地方把条件建模：按物品计数（Primordial 系列）、按充能、低/满生命、近期击杀、已保留魔力，以及按已分配节点计数。
- 在某个条件建模之前，服务端**绝不能**应用它的条件部分：要么在天赋孔里拒绝该珠宝并给出明确提示，
  要么把受影响的属性标为未生效。现在的无条件路径不可接受。
- 加一个条件接口，让 `CharacterStatSources` 与命中、面板的消费者能在使用的当下查询。
- 完成标准：每一行要么被完整建模，要么被明确拒绝，并在目录状态里写明原因。

### 4. 半径型传奇珠宝（78 行、76 个不同名字；属性 + 伤害/防御试点已完成）

完整分类、机制、半径/环模型与可复用说明见
[radius-jewels](../docs/game/radius-jewels.md)；生成的逐颗表格见
[radius-jewel-list](../docs/game/radius-jewel-list.md)。

- **半径模型：** `PassiveJewelRadii.dat` 给出 Small 800、Medium 1200、Large 1500、VeryLarge 2000、Massive 2400，
  以及对应的环内外值。`PassiveTree.NodesInRadius` 已处理永恒的圆盘；为 Thread of Hope 增加 `NodesInRing`。
  复用永恒的排除规则，并跳过已被征服的节点。
- **机制分组：** 6 个属性转换、6 个伤害/防御/武器转换、3 个显著天赋替换、10 个天赋追加、33 个阈值/技能珠宝、
  11 个按节点缩放的珠宝、3 个分配/天赋树规则、1 个天赋效果/显著天赋压制珠宝，以及 5 个特殊/仅全局的珠宝。
- **可复用永恒的实现：** `EquipmentStatSources` 里的 `nodeStats` 覆盖模式、与 TinyMT32 无关的逐节点属性替换、
  `PassiveTree` 的几何（`NodesInRadius`/`NodesInRing`）、天赋珠宝的插孔/拒绝路径，以及物品美术表。
- **缺少的层：** 按技能限定作用域的阈值消费者；Lioneye's Fall 的服务端标记模型；剩下两个物品来源
  （Inspired Learning、Warrior's Tale）；特殊机制（偷取稀有词缀、纹身、销毁尸体）。
- **维基差异核对（2026-10-29）：** poewiki 的清单（189 个不同名字）与本目录（181）对比，剩下 Bound By Destiny、
  Forbidden Flame、Forbidden Flesh、Megalomaniac、Sublime Vision、The Balance of Terror 与 Watcher's Eye
  没有物品来源；它们记录在 `unique-jewels.md` 里，此处不实现。
- **已生成的特殊项：** Impossible Escape（`/impossible_escape`，48 个核心天赋变体）已实现，带核心天赋半径内的
  分配绕过；Rational Doctrine 的 `/unique_jewel` 来源及其静止的圣化/暴击亵渎地面机制已实现
  （见 [状态](../status/progression.md)）。
- **试点完成（42 行有服务端效果；33 个阈值与 Lioneye's Fall 的物品来源已生成）：**
  `RadiusJewelCatalogue`、`ThresholdJewelCatalogue`、`/radius_jewel`、1500/1200/800 圆盘与 Thread of Hope 环查询、
  六个属性转换、五个伤害/防御转换（Cold Steel、Fireborn、Energy From Within、Healthy Mind、Energised Armour）、
  三个显著天赋替换（Fevered Mind、Fortress Covenant、Quickening Covenant）、十个天赋追加（Coated Shrapnel、
  Martial Artistry、Dreams/Nightmares、The Light of Meaning、The Perandus Pact）、十一个按节点缩放的珠宝
  （Anatomical/Eldritch Knowledge、Pugilist、Spire of Stone、Static Electricity 以及 Tempered/Transcendent 三件套，
  含已分配/未分配计数与 PoB 的 `getPerStat` 下限）、Might of the Meek（对非核心天赋 50% 效果，显著天赋被压制）、
  Intuitive Leap/Thread of Hope（分配绕过、环形变体、移除被拒）与 Unnatural Instinct（已分配的小天赋被压制、
  未分配的小天赋被授予）。集群插孔的拒绝、物品限制与实机核对都通过（力量 -10 / 智力 +34；
  装 Energy From Within 时生命 5857 -> 5311、护盾 243 -> 273；装 The Light of Meaning（生命）时生命 5311 -> 5355；
  在完整布局里 Anatomical 在第 11 号孔值 +165（装上 5520、不装 5355），其中第 11 号孔自身的 20 点智力贡献
  `floor(20 / 3) = 6` 点固定生命，叠加在其全局生命%之上；Might of the Meek 放在第 3 格把生命 5520 -> 5529、
  魔力 2345 -> 2375，未封顶的攻击/法术格挡 168%/27% -> 162%/20%，因为 Dark Arts 显著天赋被压制；
  Intuitive Leap 接受了不连通的节点 55247（生命 5520 -> 5575），且分配在重登后仍保留，而在第 1 格它拒绝了外部相邻的
  53802（无桥接）；Thread of Hope 使用 PoB 的 3_16 环 960-1320 / 1320-1680 / 1680-2040 / 2040-2400 / 2400-2880，
  不会启用珠宝孔，其 -10% 全抗词缀把 `/resistances` 268/221/221 变成 258/211/211；它的环值是内在的、基础半径为 0，
  所以客户端会画出环带。Massive 接受了 23507（2628）与 48477（523 已分配），退还它们之后 Large 接受了 38129（1808）；
  在这些绕过节点仍存在时换下物品会被拒绝。Might in All Forms 在第 1 格把 `passive jewel stats` 的近战值从 129 改成 137
  （+8 = 半径内 40 点敏捷+智力 / 5），当时 Brute Force 在背包里；把 Brute Force 恢复后回到 127。
  Pitch Darkness 与 Sudden Ignition 作为全局物品来源创建；它们的火焰伤害属性会生效，而 Sudden Ignition 的销毁尸体属性
  目前未被读取。33 个阈值物品由 `scripts/re/threshold_jewels.py` 从 PoB 生成；`ThresholdJewels` 汇总半径内每个普通天赋，
  并在属性总和达到时加上映射的属性。实机：`Combat Focus Cobalt` 在 36634 号孔记录了
  `elemental_hit_cannot_roll_fire_damage 1`（60 智力）；`Collateral Damage` 在 60735 号孔记录了
  `galvanic_arrow_area_damage_+% 50` 与 `shrapnel_shot_radius_+% 25`（60 敏捷）；`Spirited Response` 在备用孔里
  靠其 10% 最大魔力的全局词缀把魔力 2355 -> 2476。按技能限定作用域的消费者仍是部分实现。
  Unnatural Instinct 把生命 5520 -> 5685、魔力 2355 -> 2326、护盾 277 -> 285）。Lioneye's Fall 的物品来源与客户端转换
  已存在（`/radius_jewel Lioneye's Fall`）；其服务端标记模型仍缺失。
- **顺序：** 按技能限定作用域的阈值消费者 -> Lioneye's Fall 的标记层 -> 其余特殊机制。
- 完成标准：每组都在一棵小的合成天赋树上有测试，开发命令生成的珠宝只改变预期的节点，全局词缀仍然生效，
  移除珠宝后回到基线。

### 5. 永恒珠宝（目录里 6 个；节点转换已完成）

参考、测量与实机证据见 [timeless-jewels](../docs/game/timeless-jewels.md)。

- 已完成：物品数据、`/timeless`、替换天赋树生成器、固定半径、替换/追加属性、只允许一颗 Historic 的限制、存档/读取、
  实机客户端核对、Militant Faith 在物品上的两条随机虔诚词缀，以及 `DevotionStats` 按 `floor(Devotion / 10)` 缩放这些词缀。
- 剩余：征服者核心天赋的特殊玩法机制（属性行会生效，但 Divine Flesh、Chainbreaker 等还没有消费者）、
  仍缺消费者的按虔诚目标属性（诅咒持续时间、召唤物/图腾/烙印/引导、魔力消耗与盾牌防御）、旧版征服者，
  以及与半径型传奇珠宝共用的、未来任何逐节点属性覆盖。
- 下一个完成点：至少一个征服者核心天赋的机制被建模并在战斗中核对，同时不改动已经验证过的小天赋/显著天赋替换。

### 6. 深渊珠宝与集群珠宝

目录里另有 1 行 Special、8 行集群与 9 行深渊，合计 185 条。

- **深渊珠宝（9）：** 属于物品上深渊插孔的独立功能，不是天赋树的孔。目录里的这些行标记为不在范围内。
- **集群珠宝（8）：** 是独立的集群珠宝改动（进行中，见
  [status/progression.md](../status/progression.md)）：物品、按大小嵌套、客户端自身节点 id 的分配与主题/显著天赋属性
  已完成；id 到节点的映射（部分已分配的集群）、核心天赋、半径规则与商人配方尚未完成。
  参考与测量见 [cluster-jewels](../docs/game/cluster-jewels.md)。

## 建议顺序与验收

1. 目录 + `/item`（解锁后面全部阶段）。
2. 全局行（价值最高、风险最低）。
3. 条件行（在使用时求值条件）。
4. 半径行，拆成半径内属性、阈值、节点修改与授予节点。
5. 永恒行。
6. 深渊物品插孔与集群珠宝放在各自的改动里。

每个阶段都必须保持普通/魔法/稀有珠宝的路径通过：插孔摆放、换位、拾取、存档/读取、退还保护、等级需求，
以及 `PassiveJewels1` 面板更新。用真实客户端通过 `scripts/dev/live.ps1` 验证并查看 `logs/live-gateway.log`；
为每条新的物品/词缀路径加针对性的测试。

## 待定问题

- 传奇珠宝的名字应当按名称、还是按传奇外观标识从 `UniqueStashLayout` 解析（有些物品共享名字家族，或有替代美术行）。
- 半径珠宝的逐节点覆盖如何表示，才能让角色面板与客户端一致，尤其当节点自身的属性被替换时。
- 条件/半径部分尚不支持的珠宝，是在插孔处拒绝，还是允许它只带全局部分生效。上面的计划选择在建模之前拒绝，
  以免角色被悄悄算错。
