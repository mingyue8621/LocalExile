# 路线图：物品、通货与制作

属于 [路线图索引](../Roadmap.md)；这个区域已经能用的功能见 [status/items-and-crafting.md](../status/items-and-crafting.md)。
条目沿用路线图的优先级分组，同一组内大致按优先级排列。

## 1. 体验优化

精华物品制作已实现（105 种通货、按类别保证的词缀；见状态文档与 protocol/13-items.md）。
被困精华遭遇、怪物身上的腐化残留与仓库页签的精华升级仍是独立功能。

## 3. 玩法

### 物品：词缀、稀有度、使用药剂与通货

- **工匠石：** 最初的本地 0x71/Shift 0x72 实现会重新掷出孔数、颜色与连接，受类别与物品等级上限约束；
  实机发现的「在物品等级上限处被拒绝」的问题已修复（只有基底容量会阻止使用），并已实机核对。
  要求的本地规则与剩余的确切游戏行为见 13-items.md 的 "Jeweller's Orb"。
- **连接石：** 普通/Shift 的处理与只重掷连接已实现并实机核对。
  确切的连接概率、品质缩放与特殊物品覆盖仍待处理（13-items.md 的 "Orb of Fusing"）。
- **已知：** 物品能序列化、移动、镶嵌与掉落（[13](../docs/protocol/13-items.md)）；
  稀有度、固有词缀与每一份词缀列表都会下发并保存（13 §2a）；
  魔法与稀有物品会掷出、以未鉴定状态掉落，智慧卷轴可鉴定它们（13 §2b）；
  已装备物品的词缀会计入角色（13 §2c）。
  生命/魔力药剂充能、恢复、原生饮用音效与满池拒绝已实现（13 §8）。
  物品状态已实现并实机核对（13 §2f）；待做：创建它们的通货与读取它们的机制（#60 的后续），以及区域与怪物 mod（#61）。
  泥滩的雕文是任务物品（`Metadata/Items/QuestItems/Glyphs/Glyph1..3`，
  [12](../docs/protocol/12-quests.md)）。
- **入手：** 特殊词缀与传奇自身的机制（#60，13 §2e：`UniqueCatalog` 已有每件传奇的基底、词缀与掷值，
  这些词缀背后的机制才是待做的部分）、点化（chancing），以及 Mageblood 的后续（增益图标、
  「效果无法被移除」的兜底、Foulborn 最右变体）。
  Enkindling/Instilling 的交易、充能锁与命中稀有怪物的自动化已有原生验收；请原生核对其他自动触发，
  并在那些机制存在后接上受到的流血/中毒来源。
  生成的传奇，2026-10-31 与 poedb 对比之后剩下的差异（13 §2e）：不生成孔位（`Sockets:` 行被丢弃，
  Tabula Rasa 既没有孔也没有词缀）；五件合成传奇与 The Eternal Struggle 缺其随机固有词缀；
  有多种变体可选的传奇只取一个固定选择，生成器列出的 34 件传奇的变量词缀也未纳入，`{crafted}` 行同样
  （Cinderswallow Urn 那种类附魔掷值）。Doryani's Delusion 生成的变体是它的腐化变体。
  poedb 与 PoB 当前变体在 Twyzel、Replica Twyzel、Kitava's Feast 与 Dissolution of the Flesh 的基底上不同，
  在 Timetwist 的掷值与 Night's Hold 的腐化上也不同；这里保留 PoB，且没有一件在客户端见过。
  神圣石与祝福石按名字与基底找传奇，所以没有匹配定义的物品会在 Mods.dat 里重掷。
  evryBuild 的 Send Item（`tools/LocalExile.Export/ItemSend.cs`）只读传奇的名字、基底与词缀：
  它还需要 `Implicits`、`Ranges`、`Fractured`、`Mirrored` 与 `Influences`，以及 PoB 的 "Split" 与 "Shaper Item" 行。
  蜕变石的 CoE 对比与准入检查已实现；请原生复核 Shift/仓库与手动界面使用。见[证据限制](../docs/re/transmutation-coe.md)。
  增幅石使用同一个校验过的入口与经 CoE 检查的加权池；请原生复核其准入与 Shift/仓库路径。
  见[证据](../docs/re/augmentation-coe.md)。
  富豪石使用同一入口并在升级后打开稀有池；药剂被拒绝。请原生复核。见[证据](../docs/re/regal-coe.md)。
  改造石通过同一入口重掷 1 到 2 条词缀并拒绝空池；与 CoE 的 300 次应用计数与药剂检查一致。请原生复核。
  见[证据](../docs/re/alteration-coe.md)。
  点金石使用同一入口与 CoE 的 4/5/6 与珠宝 3/4 稀有词缀计数；药剂被拒绝。请原生复核。
  见[证据](../docs/re/alchemy-coe.md)。
  崇高石在基底上限内加一条稀有词缀（珠宝每侧 2 条），保留名字并拒绝药剂。请原生复核。
  见[证据](../docs/re/exalted-coe.md)。
  混沌石重掷同一个稀有池、保留元词缀锁并拒绝药剂/空池。请原生复核。见[证据](../docs/re/chaos-coe.md)。
  瓦尔的本地结果规则有校验过的准入（腐化/镜像/传奇/不受支持/空池被拒）；官方 CoE 瓦尔结果另属工作。
  见[边界](../docs/re/vaal-coe.md)。
  机会石的本地 30/70 规则有校验过的准入；CoE beta 没有 Chance 处理函数，所以官方传奇/稀有度结果另属工作。
  见[边界](../docs/re/chance-coe.md)。
  剥离石的移除规则有校验过的准入与 CoE 元词缀/稀有度检查；请原生复核。见[证据](../docs/re/annulment-coe.md)。
  四种腐化灵液与余烬会加上分阶的吞噬者/圣战者固有词缀，并有 CoE 基底/阶检查。
  冲突宝珠与独立的支配/势力状态另属工作。见[灵液证据](../docs/re/eldritch-ichor-coe.md)
  与[余烬证据](../docs/re/eldritch-ember-coe.md)。
  保存的变量防御基底、本地词缀与品质现在会喂给池子。
  神圣石重新掷值、本地零护盾规则、装备护盾转生命与腐化物品全局护盾提高都已实现，带自动化覆盖与历史原生对照
  （13 §2c、§4；状态文档）。
  剩余：合并分支的新实机客户端核对、Ward 计算与不受支持的传奇/条件防御机制。
  仅盾牌的提高与排除现在会喂给普通防御池，并带原生最终半进位取整。
  剩余的特殊机制包括 Ward 转换、戒指槽与珠宝半径运算符。
  神圣石重新掷值走普通/Shift 通货路径与原子化的背包/仓库保存。
  原生核对与本地掷值分布策略见状态文档与 13-items.md 的 "Sacred Orb"。
  破碎与镜子有校验过的类别/稀有度准入（破碎用 `ItemClasses.CanBeFractured`，镜子用装备标记、非传奇目标、
  镜像物品不接受通货）；2026-10-30 的原生运行覆盖了成功与拒绝路径。被遮蔽的词缀与反光油/圣化另属工作，
  从仓库页签使用镜子仍不受支持。见[游戏规则](../docs/re/fracturing-mirror-mechanics.md)。
  Hinekora's Lock 的服务端规则已实现（Mods 标记第 8 位 + 种子、C→S 0x29d / S→C 0x2a0 带 status 0 的预览、
  精确的提交结果、改动即清除）；客户端的预览分支是物品弹窗的预见元素。见[证据](../docs/re/hinekora-lock.md)。
  Enkindling 与 Instilling 已按官方五/十五个池实现，含共存与丰收/制作台替换；
  Enkindling 的「生效期间不获得充能」代价由共用的充能获取路径强制执行。
  传奇药剂定义、权重为零的 Instilling 行、原生的遗留丰收检查与制作台 Instilling 配方仍待做。
  见[Enkindling 证据](../docs/re/enkindling-coe.md)与[Instilling 证据](../docs/re/instilling-coe.md)。
- **孔位颜色（3.29）：** 3.29 规则已实现（默认白色并有按物品等级的颜色几率，幻色石与制作台的非白孔工艺，
  工匠石/制作台加孔带来的白孔，瓦尔给一个白孔上色；[13](../docs/protocol/13-items.md) 的 "Socket colours (3.29)"、状态文档）。
  剩余：污染幻色石/工匠石、束缚宝珠、腐化之地、里程碑制作台奖励（25 次重铸保留最多非白孔）、特殊物品覆盖，
  以及用实机抓取拟合确切的颜色概率（当前曲线是近似值）。
- **完成标准：** 物品的伤害与防御会计入，且药剂与通货能使用。
- **药剂词缀（#75）：** 角色充能获取来源解析与独立的混合恢复组件在普通模式下有处理函数/生命周期覆盖。
  本条里的计数与标准/无情观察描述的是历史 LocalExile 验证，不是本分支的新原生运行；
  见[移植记录](../docs/dev/issue75-port.md)。24 个更早的原生对照验证了实际增益、恢复与充能发放，
  含无情的功能药剂禁用与普通商人补给。
  固定与按玩家等级的持续时间现在为面板与实际使用共用一个原始解析器；等级与输入在发送充能之前捕获。
  功能药剂的本地/全局持续时间因子相乘而不是相加，品质与本地更多保持分开。
  运行时全局效果现在为恢复与功能药剂共用同一个「原始固定值 + 实际等级」解析器，本地/适用的魔法效果相加，
  原始发布不变。它新增的 39 个纯/处理函数用例在合并的 53 个聚焦用例与完整 Gateway 3710/3710 里通过；
  此前未改动的 Core 648/Data 37 仍适用（合计 4395，并非全部重跑）。五个实际已释放流的任务错误仍被记录，与新计算无关；
  原生验证待做。每次使用的快照、立即/低生命/速率顺序、标记与独立 Onslaught 另属工作。宽投影守卫并不覆盖每个下游聚合。
  更早的面板/持续时间批次通过了 55 个新增加 80 个既有聚焦用例、完整 Gateway 3657/3657 与适用的 Core 648/Data 37 检查。
  40 个完成的原生对照里 22 个完整对照通过（12 个正来源、6 个功能药剂与 4 个生命）；18 个完整零对照仍为 UNPROVED，
  面板字段结论为 138 VERIFIED / 42 UNPROVED。功能药剂持续时间与 标准13.8/3/3.9 与 无情4.6/2/2.6 秒吻合
  （分别为 10 级白色、100 级白色与 100 级本地使用）。生命在 10/100 级观察到 标准160/34 与 无情161/34，
  对照固定值161/35，落在原有一个整数的边界内。干净的 recovery3 清理、769 个保留哈希与保留的存档/规范背包，
  并不能把未证明的零对照变成通过。这些历史原生结果不能验证新的效果解析器。条件获取/效果运算符、
  整件传奇的原生获取/提示行为、非正持续时间策略、官方小数取整与排队后的原生计时器/预测显示仍待处理。
  见 protocol 13 §8 与物品状态文档。
- **Jeweller's Orb:** initial local 0x71/Shift 0x72 implementation rerolls counts, colours and links with class and
  item-level caps; the live-found refusal at the item-level cap is fixed (only base capacity blocks use), verified
  live.
  See 13-items.md, "Jeweller's Orb" for the requested local rules and remaining exact-game behaviour.
- **Orb of Fusing:** normal/Shift handling and link-only rerolls are implemented and verified live.
  Exact link probabilities, quality scaling and special item overrides remain open (13-items.md, "Orb of Fusing").
- **Known:** items serialize, move, socket and drop ([13](../docs/protocol/13-items.md));
  rarity, implicits and every mod list are sent and saved (13 §2a);
  magic and rare items roll, drop unidentified and a Scroll of Wisdom identifies them (13 §2b);
  equipped items' mods count on the character (13 §2c).
  Life/mana flask charges, recovery, native drinking sound and full-pool refusal are implemented (13 §8).
  Item states are in and verified live (13 §2f); open: currency that creates them and the mechanics that read them
  (#60's follow-ups), and area and monster mods (#61).
  The Mud Flats glyphs are quest items (`Metadata/Items/QuestItems/Glyphs/Glyph1..3`,
  [12](../docs/protocol/12-quests.md)).
- **Start:** special mods and the uniques' own mechanics (#60, 13 §2e: `UniqueCatalog`
  has every unique's base, mods and rolls, the mechanics behind those mods are the open part), chancing, and
  Mageblood's follow-ups (buff icons, the "effects cannot be removed" backstop, the Foulborn rightmost variant).
  Enkindling/Instilling transactions, charge lock and rare-hit automation have native acceptance; verify the other
  automatic triggers natively and connect incoming Bleed/Poison sources when those mechanics exist.
  Generated uniques, what is left after the poedb comparison of 2026-10-31 (13 §2e): Abyss socket layouts still lack
  generation rules (`Sockets:` lines are dropped); ordinary default sockets, Tabula's six linked whites, Skin of the
  Loyal's six linked random colours, the 3.29 no-white property, fixed "Has X Sockets" counts and Malachai's all-white
  socket are implemented, awaiting a fresh native check. Skin of the Lords is absent from the generated seed, so it
  cannot be created until its definition is added. The five synthesis
  uniques and The Eternal Struggle lack their random implicits; uniques with a choice of variants are one fixed choice, and the variable mods of the 34 uniques the
  generator lists stay out, `{crafted}` lines too (Cinderswallow Urn's enchant-like rolls).
  Doryani's Delusion's generated variants are its corrupted ones.
  poedb differs from PoB's current variant on the base of Twyzel, Replica Twyzel, Kitava's Feast and Dissolution of the
  Flesh, and on Timetwist's roll and Night's Hold's corruption; PoB is kept, and none was seen in the client.
  Divine and Blessed Orbs find a unique by its name and base, so an item with no matching definition rerolls in Mods.dat.
  evryBuild's Send Item (`tools/LocalExile.Export/ItemSend.cs`) reads a unique's name, base and mods only: it needs
  `Implicits`, `Ranges`, `Fractured`, `Mirrored` and `Influences`, and PoB's "Split" and "Shaper Item" lines.
  Transmutation's CoE comparison and admission checks are implemented; recheck Shift/stash and manual UI use natively.
  See [the evidence limits](../docs/re/transmutation-coe.md).
  Augmentation uses the same validated entry point and CoE-checked weighted pools; recheck its admission and
  Shift/stash paths natively. See [the evidence](../docs/re/augmentation-coe.md).
  Regal uses the same entry point and opens the rare pool after the upgrade; flasks are refused. Recheck it natively.
  See [the evidence](../docs/re/regal-coe.md).
  Alteration rerolls 1-2 affixes through the same entry point and refuses empty pools; CoE's 300-apply count and
  flask checks agree. Recheck it natively. See [the evidence](../docs/re/alteration-coe.md).
  Alchemy uses the same entry point and CoE's 4/5/6 and jewel 3/4 rare counts; flasks are refused. Recheck it
  natively. See [the evidence](../docs/re/alchemy-coe.md).
  Exalted adds one rare affix within the base's cap (jewels 2 per side), keeps the name and refuses flasks. Recheck
  it natively. See [the evidence](../docs/re/exalted-coe.md).
  Chaos rerolls the same rare pool, keeps metamod locks and refuses flasks/empty pools. Recheck it natively. See
  [the evidence](../docs/re/chaos-coe.md).
  Vaal's local outcome rule has validated admission (corrupted/mirrored/unique/unsupported/empty refused); official
  CoE Vaal outcomes remain separate. See [the boundary](../docs/re/vaal-coe.md).
  Chance's local 30/70 rule has validated admission; CoE beta has no Chance handler, so official unique/rarity
  outcomes remain separate. See [the boundary](../docs/re/chance-coe.md).
  Annulment's removal rule has validated admission and CoE metamod/rarity checks; recheck it natively. See
  [the evidence](../docs/re/annulment-coe.md).
  The four Eldritch Ichors and Embers add tiered Eater/Exarch implicits and have CoE base/tier checks. The Orb of
  Conflict and a separate dominance/influence state remain separate. See [the Ichor evidence](../docs/re/eldritch-ichor-coe.md)
  and [the Ember evidence](../docs/re/eldritch-ember-coe.md).
  Saved variable defence bases, local mods and quality now feed the pools.
  Sacred Orb rerolls, local zero-ES rules, equipment ES-to-Life conversion and corrupted-item global ES increases
  are implemented with automated coverage and historical native controls (13 §2c, §4; Status).
  Remaining: fresh real-client checks of the merged branch, Ward calculations and unsupported unique/conditional
  defence mechanics.
  Shield-specific increases and exclusions now feed ordinary defence pools with native final half-up rounding.
  Remaining special mechanics include Ward conversions, ring-slot and jewel-radius operators.
  Sacred Orb rerolls use the ordinary/Shift currency path and atomic inventory/stash saves.
  See Status and 13-items.md, "Sacred Orb" for the native checks and local roll distribution policy.
  Fracturing and Mirror have validated class/rarity admission (Fracturing uses `ItemClasses.CanBeFractured`, Mirror
  the equipment flag, non-unique targets and no currency on mirrored items); the 2026-10-30 native run covered the
  success and refusal paths. Veiled modifiers and Reflective Oil/Sanctified remain separate,
  and Mirror from a stash tab is still unsupported. See
  [the game rules](../docs/re/fracturing-mirror-mechanics.md).
  Hinekora's Lock's server rules are in (Mods flag bit 8 + seed, C→S 0x29d / S→C 0x2a0 preview with status 0,
  exact committed result, clear-on-modification); the client's preview branch is the item popup's foresight element.
  See [the evidence](../docs/re/hinekora-lock.md).
  Enkindling and Instilling are implemented with the official five/fifteen pools, coexistence and Harvest/bench
  replacement; the Enkindling "Gains no Charges during Effect" drawback is enforced by the shared charge-gain path.
  Unique flask definitions, the zero-weight Instilling row, a native legacy-Harvest check and the bench Instilling
  recipes remain. See [the Enkindling evidence](../docs/re/enkindling-coe.md) and
  [the Instilling evidence](../docs/re/instilling-coe.md).
- **Socket colours (3.29):** the 3.29 rules are in (white by default with an item-level colour chance, the
  Chromatic and non-white bench crafts, white sockets from Jeweller's/bench socket adds, Vaal colouring one white
  socket; [13](../docs/protocol/13-items.md), "Socket colours (3.29)", Status).
  Remaining: Tainted Chromatic/Jeweller's Orbs, the Orb of Binding, the Locus of Corruption, the milestone bench
  reward (25 reforges keeping the most non-white sockets), special item overrides, and fitting the exact colour
  probabilities from live captures (the current curve is an approximation).
- **Done when:** items' damage and defences count, and flasks and currency can be used.
- **Flask modifiers (#75):** character charge-gain source resolution and independent hybrid recovery components
  have ordinary-mode handler/lifecycle coverage here. The counts and Standard/Ruthless observations in this entry
  describe historical LocalExile validation, not a new native run on this branch; see the
  [port record](../docs/dev/issue75-port.md). Twenty-four earlier native controls verify actual buffs,
  recovery and charge awards, including Ruthless utility prevention and ordinary vendor refill.
  Flat and per-player-level duration now share one raw resolver for the panel and actual uses; level and inputs are
  captured before the charge send. Utility local/global duration factors multiply rather than add, with quality and
  local more kept separate. Runtime global effect now shares one raw flat-plus-actual-level resolver for recovery
  and utility, with local/applicable magic effect additive and raw publication unchanged. Its 39 new pure/handler
  cases pass in the combined 53 focused cases and full Gateway 3710/3710; prior unchanged Core 648/Data 37 remain
  applicable (4395 total, not all newly rerun). Five actual disposed-stream task faults remain recorded without
  new-calculation references; native verification is outstanding. Per-use snapshots, instant/low-life/rate order,
  flags and independent Onslaught remain separate. Wide projection guards do not cover every downstream aggregate.
  The preceding panel/duration batch passed 55 new plus 80 existing focused cases,
  full Gateway 3657/3657 and applicable prior Core 648/Data 37 checks. Of 40 completed native controls, 22 complete
  controls pass (12 positive source, six utility and four Life); 18 complete zero controls remain UNPROVED, with
  138 VERIFIED/42 UNPROVED panel-field verdicts. Utility durations match Standard13.8/3/3.9 and Ruthless4.6/2/2.6
  seconds for level-10 white, level-100 white and level-100 local uses. Life observed Standard160/34 and Ruthless161/34
  against fixed161/35 at levels10/100 under the original one-integer boundary. Clean recovery3 cleanup,769 held
  hashes and preserved saves/canonical inventories do not turn the unproved zeros into passes.
  These historical native results do not validate the new effect resolver. Conditional gain/effect operators,
  whole-unique native acquisition/tooltip behavior, nonpositive-duration policy, official fractional rounding and
  queued native timer/predictor presentation remain open. See protocol 13 §8 and the items status.

## 4. 服务端设计

- **可编辑的游戏数据与限制：** SQLite 覆盖层、快照/重载基础、词缀掷值范围与物品限制已实现并实机核对。
  传奇物品、半径/阈值/传奇/永恒珠宝、纹身效果与珠宝美术目录现在都在 `catalog_entries` 里
  （含完整的 1,299 行传奇目录），由当前代码数组播种，并在 `/game_data reload` 时替换。
  `stats`/`mods`/`mod_stats` 参考表与 `effective_mod_stats` 视图让编辑器不必打开 JSON 就能拿到默认键与范围。
  `system_settings` 现在存放按物品类别的孔位上限、每级天赋点、掉落/怪群/经验/最高等级/金币/默认限制/宽限等数值。
  下一步是带自定义名称的物品模板，然后是管理界面，再之后是 NPC 商店/摆放与其余掉落/平衡设置。
  客户端兼容边界、schema 与缓存计划见
  [dev/editable-game-data.md](../docs/dev/editable-game-data.md)。
  解包出的 JSON 仍是参考；数据库只覆盖它。

## 5. 项目

- 已完成为受支持的显式词缀通货做元工艺保护；
  专用的不受支持通货与官方 Chance/Vaal 结果分布仍是独立工作。
