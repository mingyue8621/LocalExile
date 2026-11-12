# 状态：宝石

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/gems.md)。

## 已实现（已用真实客户端验证）

暂无。

## 部分实现

- 无情模式的宝石使用原生的主/伴生效果替代项、需求与技能行。
  确定性的辅助宝石奖励被关闭；可用的主动宝石走 Siosa 的未雕琢宝石兑换。
  宝石经验会乘上配置的无情倍率。
  无情模式的可用性排除了原本 13 个位移宝石、Venom Gyre 与 Item Rarity Support。
  掉落、商人库存与运行时使用都执行这些排除；Quickstep 仍然允许。
  Frostblink 的另一个 hardmode 行不会让这个不可用的宝石变得可用。
  模式目录、获取与运行时的自动化检查通过。
  无情模式的 Nessa、Siosa 与不可用宝石情形都有自动化覆盖。
  见 [赛季模式](../docs/game/league-modes.md)。
- **宝石任务奖励（2026-10-26 实机验证）。** 凡服务端能构建其物品的 `QuestRewardOffers` 行，都由 NPC 的
  QuestReward 对话行提供，带 `Characters` 职业过滤与该报价的已领取标记；无法构建的行（8 级魔法护甲、魔法腰带）
  直接跳过，而不是用白名单把整个报价排除。
  这覆盖了各幕的宝石报价：a1q4 的六个职业宝石（报价 3）、a1q5 的辅助宝石（报价 2），以及每一幕后续的宝石选择。
  领取会在一个存档里把物品放进主背包并置位标记；第二次领取会被拒绝
  （`QuestRewardCatalog`；[12](../docs/protocol/12-quests.md) §1、[19](../docs/protocol/19-gems.md) §5）。
  实机：`/quest_flag a1q4-PlacedGlyphs` 之后 Tarkleigh 的「Glyph Reward」——六个女巫宝石，可领取，重复被拒。

- **宝石掉落（2026-10-26 实机验证）。** 怪物物品掉落中有 4% 是一颗可获得、且 `DropLevel` 满足该怪物的宝石，
  不带等级、不做稀有度掷骰（`ItemDrops`、`GemSources.DropPool`、
  [15](../docs/protocol/15-progression.md) §3）。
  掷骰池是任务奖励宝石、初始宝石与仅掉落的卓越辅助宝石；数据里的 Graft、物品授予/遗留（Envy、Death Aura、
  Quickstep）以及占位（"..."）行永不掉落。
  `GemDropsTests` 覆盖掷骰池、等级门槛与无等级宝石。在游戏的掉落池未知之前，这个比率是我们自己定的。
  实机：稀有怪物掉出了普通技能与辅助宝石。最初一版用的是数据里的 ShopTag 宝石，其中包含物品授予/遗留行，
  所以掷骰池收窄成了任务/初始/卓越三组。
- **Gem drops (verified live 2026-10-26).** 4% of a monster's item drops are an obtainable gem whose `DropLevel` the
  monster meets, unlevelled and without a rarity roll (`ItemDrops`, `GemSources.DropPool`,
  [15](../docs/protocol/15-progression.md) §3).
  The pool is the quest reward gems, the starting gems and the drop-only exceptional supports; the data's Graft,
  item-granted/legacy (Envy, Death Aura, Quickstep) and placeholder ("...") rows never drop.
  The rate is our own until the game's drop pools are known.
  Live: rare monsters dropped normal skill and support gems. The first attempt used the data's ShopTag gems, which
  include the item-granted/legacy rows, so the pool was narrowed to the quest/starting/exceptional sets.

- **宝石商人（2026-10-26 在 Nessa 上实机验证；Lilly/Siosa 有单元测试）。**
  在这个本地 checkout 里，Nessa 还额外拥有全部可生成宝石，与任务/职业无关，并带替代变体。
  觉醒宝石与卓越宝石各自有独立页签；颜色/类别页签只放普通宝石，但仍包含转化、Trarthan 与可生成的瓦尔变体，
  而 White Skills 合并两种白色类别。单独的 Skills 页签保留受任务/职业限制的普通库存，作为未来的玩法商店，
  只在有库存时显示。Skills 页签先列主动宝石再列辅助，按红/绿/蓝/白分组，辅助从新的一行开始。
  无论进度如何，颜色页签始终是完整的开发工具；`NpcShops.cs` 记录了这个区别，并且打开商店时不会把开发用提示塞进
  玩家聊天。
  Nessa 的每颗宝石在每个页签上都是无限库存：付费购买的副本会拿到新 ID，原报价仍然可用。
  网络购买测试覆盖重复购买、变体/品质持久化、余额不足与存档回滚。
  其他商人保持按进度供货。
  自动化全量库存、变体持久化与分页检查覆盖了这处覆盖；原生显示更新尚未核对。
  见 [19](../docs/protocol/19-gems.md) §5 的「Local Nessa shop」。
  同时卖宝石的通货/药剂/首饰商人提供七个页签的一级宝石，按其一级需求档定价（1 到 7 卷轴碎片、
  8 到 15 蜕变石、16 到 27 改造石、28 到 37 机会石、38+ 点金石）。库存是该角色已
  完成的任务报价（奖励显示或已领取）里的宝石，按角色职业与客户端的商店列表过滤；
  Siosa（「A Fixture of Fate」之后）与 Lilly Roth（`UnlockedLillyGemShop` 之后）忽略职业，Lilly 连任务完成也忽略；
  Faster Attacks（Nessa，a1q2 之后）与 Faster Casting（Yeena，a2q4 之后）是 3.29 新增的跨职业项。
  实机：Nessa 列出了女巫的 a1q1 + a1q4 宝石，置位标记后又列出了 a1q2 的宝石与 Faster Attacks Support；
  价格与需求档一致（a1q1/a1q4 的宝石 1 卷轴碎片，a1q2 的宝石与 Faster Attacks 1 蜕变石）。
  未实机核对：Lilly（第六幕城镇尚未抓取，会把玩家生成在水里）与 Siosa（图书馆不可达）；宝石掉落在上面。
  （`GemVendorStock`、`NpcShops.Tabs`；[17](../docs/protocol/17-npcs.md) §5、[19](../docs/protocol/19-gems.md) §5）。

- **连携辅助宝石。** 原生兼容性、连接组、同类重复与技能栏的辅助记录都已实现；
  带范围增大的 Cyclone 已被真实客户端成功读取。
  普通命中、范围、速度、暴击、消耗与保留计算都使用连携辅助的属性。
  Overcharge、Bloodthirst 及另外四个辅助按其更多伤害缩放命中（[20](../docs/protocol/20-damage.md)）。
  物品授予的辅助进入同一套兼容性、去重、计算与原生角标记录，作用于其来源物品内的宝石，与连接无关；
  自动化测试覆盖它们的添加与移除。
  无情辅助每第三次使用是一次无情打击（[25](../docs/protocol/25-gem-supports.md)）。
  本地已镶嵌的等级/品质加成作用于物理宝石，包括辅助宝石，且不会抬高需求。
  夺宝已镶嵌的攻击/法术更多伤害效果进入普通命中计算。
  Hypothermia、Behead 与 Damage on Full Life 会缩放其作用的命中（[25](../docs/protocol/25-gem-supports.md)）。
  Vile Toxins 按敌人身上的中毒数缩放一次命中；Lethal Dose 给未中毒的敌人加两层毒；Hypothermia 的额外冻结几率会把
  先前命中造成的冰缓算进去；Sadism 的减少持续时间会缩短其异常状态；Vicious Projectiles 的减少攻击速度与
  Greater Chain 的更多连锁都会生效。Greater Fork 能在分叉处多出一发投射物；Rupture 的层数提高承受的流血伤害；
  Close Combat 的战斗冲刺会加快位移技能；Block Chance Reduction 会降低怪物的格挡掷骰并施加 Overpowered；
  Machinations 会为其混沌增益统计已分配的关键天赋（[25](../docs/protocol/25-gem-supports.md)）。
  觉醒辅助中有消费者的额外属性现在会生效：按类型的受辅助宝石等级、Brutality 的 Crush、
  Spell Echo 在最后一次重复时的双倍伤害，以及 Generosity 的光环效果与它的「不影响你」。
  Multistrike 在攻击时间内重复其命中，Melee Splash 会击中每个被击中的敌人周围的敌人
  （[25](../docs/protocol/25-gem-supports.md)）。
  Maim、Bloodlust、Pulverise、Rage、Power Charge on Critical、Volatility、Combustion、Item Quantity、Iron Grip 与
  Iron Will 也都会应用自己的属性（[25](../docs/protocol/25-gem-supports.md)）。
  Immolate、Bloodthirst、Bonechill、Coursing Current、Fist of War、Devour、Sacrifice、Spellblade、Hallow 与
  Excommunicate 同样生效（[25](../docs/protocol/25-gem-supports.md)）。
  Arcane Surge 会跟踪每个法术消耗的魔力并给出增益；Inspiration 按消耗的魔力获得充能；Controlled Blaze 按近期被点燃的
  敌人数量缩放点燃伤害与命中伤害（[25](../docs/protocol/25-gem-supports.md)）。
  一个真实客户端的 Cyclone 对照接受了物品辅助记录，并显示了蓝色的 s/c、红色的 W 与白色的 d。
  受辅助的暴击数值出现在其面板与提示里；特殊执行仍有下面的缺口。
  Spell Echo 会以一次施法时间的间隔重复被辅助法术的释放与范围命中，并带 40% 更多施法速度
  （`SpellRepeats`；[11](../docs/protocol/11-skills.md) 的「Spell repeats」）；已用真实客户端核对（2026-11-04）。
  Spell Cascade 会把额外范围沿一条线、以一个有效半径为间距摆开，并在 Greater 与觉醒版 `support_spell_cascade_sideways`
  下沿瞄准方向横向铺开，所以瞄准的中心仍然打中主命中；从 3.29.3b 客户端抓取（2026-11-04）：
  Reap 的五个觉醒范围在 28.5 有效半径时相距 28 到 29 格。
  Arrow Nova 先朝瞄准点射出一发载荷箭，然后从那里把该技能的整个齐射打成环形（同一次抓取里 Split Arrow 是 18 发、
  每发间隔 20 度）；官方的下雨骨架站在施法者格子上并在那里播放抬手，再在目标处播放落地，技能的箭矢美术挂在
  `aux_projectile_attach` 上，记录在 [25](../docs/protocol/25-gem-supports.md)。
  特殊执行机制仍不完整；见 [25](../docs/protocol/25-gem-supports.md)。
- **Linked support gems.** Native compatibility, link groups, duplicate families and skill-bar support records
  are implemented; Cyclone with Increased Area of Effect was read successfully by the real client.
  Ordinary hit, area, speed, critical, cost and reservation calculations use linked support stats.
  Overcharge, Bloodthirst and four more supports scale hits by their more damage ([20](../docs/protocol/20-damage.md)).
  Item-granted supports join the same compatibility, duplicate resolution, calculations and native badge records
  for gems in their source item, independently of links; automated tests cover their addition and removal.
  Every third Ruthless-supported use is a Ruthless Blow ([25](../docs/protocol/25-gem-supports.md)).
  Local socketed level/quality bonuses apply to physical gems, including support gems, without raising requirements.
  Second Wind adds a stored use only to non-instant skills; ordinary cooldown charges recover sequentially on game time.
  The Delve socketed attack/spell more-damage effects reach the ordinary hit calculation.
  Hypothermia, Behead and Damage on Full Life scale the hits they apply to ([25](../docs/protocol/25-gem-supports.md)).
  Vile Toxins scales a hit per poison on the enemy; Lethal Dose adds two poisons to an unpoisoned one; Hypothermia's
  extra freeze chance counts an enemy chilled by an earlier hit; Sadism's less duration shortens its ailments; Vicious
  Projectiles' less attack speed and Greater Chain's more chains apply. Greater Fork can add a third projectile at the
  fork; Rupture's stacks raise bleeding damage taken; Close Combat's Combat Rush speeds up eligible travel skills
  and is consumed by a paid travel use; Block Chance Reduction lowers the monster's block roll and applies Overpowered;
  Machinations counts allocated keystones for its chaos gain and skill-local cost increase ([25](../docs/protocol/25-gem-supports.md)).
  Awakened supports' extra stats that have a consumer now apply: per-type supported gem levels, Brutality's Crush,
  Spell Echo's double damage on the final repeat, and Generosity's aura effect with its "does not affect you".
  Multistrike repeats its hits within the attack time, and Melee Splash hits the enemies around each struck one
  ([25](../docs/protocol/25-gem-supports.md)).
  Maim, Bloodlust, Pulverise, Rage, Power Charge on Critical, Volatility, Combustion, Item Quantity, Item Rarity,
  Iron Grip and Iron Will apply their stats as well ([25](../docs/protocol/25-gem-supports.md)).
  Immolate, Bloodthirst, Bonechill, Coursing Current, Fist of War, Devour, Sacrifice, Spellblade, Hallow and
  Excommunicate apply too. Sacrifice adds chaos damage at a valid spell release, including projectile hits and poison
  ([25](../docs/protocol/25-gem-supports.md)).
  Mirage Archer uses linked bow skills after arrow hits; owner-only damage restriction changes await a client check.
  Arcane Surge tracks each spell's mana spent and grants its buff; Inspiration gains charges on mana spent; Controlled
  Blaze scales ignite damage and hit damage by enemies ignited recently
  ([25](../docs/protocol/25-gem-supports.md)).
  A real-client Cyclone control accepted the item-support records and displayed blue s/c, red W and white d.
  The supported critical values appeared in its panel and tooltip; special execution still has the gaps below.
  Spell Echo repeats supported releases and area hits, including ordinary Ice Nova rings, a cast time apart
  (`SpellRepeats`; [11](../docs/protocol/11-skills.md) "Spell repeats"); checked with the real client (2026-11-04).
  Greater Spell Echo scales repeated area and projectile hits. Focused Channelling scales channelled area hits by elapsed whole seconds.
  Meat Shield reduces damage taken by supported minions, and Elemental Army scales their elemental damage paths.
  Spell Cascade places its extra areas one effective radius apart in a line, and across the aim with Greater and
  Awakened `support_spell_cascade_sideways`, so the aimed centre still lands the main hit; captured from the 3.29.3b
  client (2026-11-04): Reap's five Awakened areas were 28-29 cells apart at a 28.5 effective radius.
  Arrow Nova fires one payload arrow to the aim and then the skill's whole volley in a circle from there (18 arrows
  20 degrees apart for Split Arrow in the same capture); the official rain rig stands at the caster's cell and plays
  its up-cast there, then its landing at the target, with the skill's arrow art riding `aux_projectile_attach`,
  documented in [25](../docs/protocol/25-gem-supports.md).
  CWDT projectile spells execute; other trigger payloads refuse manual use ([25](../docs/protocol/25-gem-supports.md)).
  Special execution mechanics remain incomplete; see [25](../docs/protocol/25-gem-supports.md).

- **斩杀（Culling Strike）。** 近战、范围与投射物命中会在生命 10% 时斩杀；Cull the Weak 按其自身份额斩杀普通与魔法敌人，
  Pacifism 的命中会留下 1 点生命（[25](../docs/protocol/25-gem-supports.md)）。

- **稳定的宝石技能 id（已测试，尚未用真实客户端核对）。** 每颗宝石在保持镶嵌期间保有自己的技能 id
  （经光标移动的宝石保留自己的 id；换进来的宝石绝不会继承旧 id），所以其他宝石进出时技能栏的键位不再移动；
  消失的技能会结束它的光环或正义之火；同类光环同时只跑一个（`GemSkillIds.cs`；[19](../docs/protocol/19-gems.md) §3）。
- **宝石等级（已测试，1 级 → 2 级用真实客户端核对过）。** 宝石等级来自其经验，直至其经验类型的上限；它的技能使用该等级的
  消耗、伤害、保留与冷却，以及品质属性；转化与 Trarthan 宝石授予各自的技能；瓦尔宝石同时授予其瓦尔技能与基础技能。
  **2026-11-08 修正「自动升级」**：经验上限原先只取「角色撑得到的最高等级」（`MaxLevelAt`），经验越过中间每一级门槛时
  客户端就按经验把等级抬上去，看起来像自动升级；现在上限改成「当前等级 + 1 的门槛 − 1」，每升一级都要玩家点宝石上的
  「+」（C→S 0x31，`Items.LevelUpGem`），与官方一致。角色等级/属性撑不住的下一级仍是第二道闸：经验停在其门槛前一点，
  点「+」会被回一条缺什么。
  瓦尔灵魂的收集/原生额度载荷已实现，待原生验收。
  即使有灵魂，不支持的瓦尔施放仍会被拒绝；见 [27](../docs/protocol/27-vaal-souls.md)。
  变体在 SkillGem 数据里下发并保存。
  物品序列化也会为独立与已镶嵌宝石推导原生变体标记，保留等级锁与已保存的标记。报文回归覆盖该标记；
  转化后的物品名仍需原生复测；`/gem <名称> [等级] [品质]`（`ItemCatalog.cs`、`SkillCatalog.cs`；
  [19](../docs/protocol/19-gems.md) §1、§3）。尚未做：腐化。
- **宝石需求（已测试，用真实客户端核对过）。** 宝石的等级需求就是其等级对应的角色等级
  （GrantedEffectStatSetsPerLevel 的 `PlayerLevelReq`；Haste 宝石 1 级为 24、20 级为 70，与客户端提示一致），
  其属性需求遵循客户端自己的公式（`0x142476710`：`trunc((3 * level_req + 11) *
  (percent/100)^0.9 * k + 0.5)`，技能 `k` 为 0.7、辅助为 0.5，均不低于 14；
  用 570 个 poedb 提示格拟合，无一不符：Haste 20 级 155 敏捷、Clarity 20 级 119 智力、Fireball 20 级 155）。
  镶嵌角色等级不够或属性不足的宝石会被拒绝（0x30，`GemRequirementsUnmet`），而宝石需求高于角色时会让其所在物品失效
  （物品需求取自身与其宝石中最高的，`EquipmentStatSources`；在 PR #175 中丢失，2026-10-29 恢复），
  与角色等级不够穿戴的物品一样：客户端会把它标红（「Requires level 70」），物品词缀停止计入、其技能离开 Actor 列表，
  所以没有东西可施放；即便如此仍到达的使用请求会被拒绝且不产生动作（无 0x1ab，并重发魔力池以便客户端的预判消耗能回落；
  `GemDisabled`）。
  失效的主手武器会被忽略：普通攻击回落到该职业的徒手命中，就像没穿一样。
  2026-10-23 用角色 10 级、20 级 Fireball 法杖实机核对，其属性用 `/set_stat intelligence 200` 再改 130
  （需要 155：客户端把宝石标红、法杖词缀停止计入、使用被拒）。
  实际游玩中宝石到不了那一步：它的经验会在角色未达到的第一个等级处停住
  （`GainGemExperience` 里的 `SkillCatalog.MaxLevelAt`，即游戏本身的规则；客户端只凭经验推导等级，自己不会停），
  而等级变化会重发技能。
  尚未做：核对每个失效宝石的光环切换（[19](../docs/protocol/19-gems.md) 1、3）。
- **击杀获得宝石经验（第一套武器里的单颗宝石用真实客户端核对过）。** 每颗镶嵌在已装备物品里的宝石（两套武器都算）
  获得一次击杀怪物经验的 10%（含辅助宝石；失效物品、已达最高等级的宝石以及超过上限的腐化宝石不给；
  任务给的经验与 `/add_xp` 不给宝石经验），随击杀以 0x00bf 物品变更下发，每件物品只发一次，所以经验条会跟上，
  并与角色一起保存（`GainGemExperience`、`PlayerInventories.ChangeSocketedGems`；
  [19](../docs/protocol/19-gems.md) §1）。
  一次击杀的份额会停在下个阈值前 1 点，那里客户端会显示「+」：它不会自己把宝石升级（2026-10-23 实机核对）。
  尚未实机核对：一件物品里多颗宝石、第二套武器。
- **宝石升级与锁定（用真实客户端核对过）。** 点击「+」发送 C→S 0x31（背包、物品、镶嵌位；14 字节）；
  服务端把下一个阈值写进宝石、替换物品（0xbf）并重发技能（0x02bc Actor），于是客户端立刻推导出新等级与其数值，
  并会拒绝已达最高等级的宝石、已锁定的宝石，或下一级需求角色达不到的宝石（`LevelUpGem`；
  客户端自己的「+」检查不会测这些，所以服务端会回答缺失的等级或属性）。
  右键发送 C→S 0xd7（同样的三个 u32 加一个 u8 锁定标记；15 字节），保存在 SkillGem 标记的第 2 位
  （`Item.GemFlags`）并写入存档，所以锁能跨登录存活；置位期间客户端会把「+」置灰（`LockGem`）。
  2026-10-23 实机核对：1 级 → 2 级、右键、重登、解锁、再升一级；
  在角色 31 级（10 级 Fireball 需要 32）以及智力 70（需要 75）时升级被拒；`/gem_xp <数量|near>` 设置经验。
  未核对：背包里（未镶嵌）的宝石，抓包里它什么都没发。
- **Stable gem skill ids (tested, not yet checked with the real client).** Each gem keeps its skill id while it stays
  socketed (a gem moved through the cursor keeps its own; a gem swapped in never takes the old gem's id), so skill bar
  keys no longer move when other gems come or go; a skill that goes away ends its aura or Righteous Fire;
  one aura of a kind runs at a time (`GemSkillIds.cs`; [19](../docs/protocol/19-gems.md) §3).
- **Gem levels (tested, level 1 → 2 checked with the real client).** A gem's level comes from its experience up to its
  experience type's cap; its skill uses that level's cost, damage, reservation and cooldown, and its quality stats;
  transfigured and Trarthan gems grant their own skill; Vaal gems grant their Vaal skill and base skill.
  Vaal soul collection/native budget payloads are implemented, pending native acceptance.
  Unsupported Vaal casts remain refused even with souls; see [27](../docs/protocol/27-vaal-souls.md).
  The variant is sent in the SkillGem data and saved.
  Item serialization also derives the native variant flag for standalone and socketed gems, preserving level-lock
  and saved flags. Wire regressions cover the flag; transfigured item names still need a native retest;
  `/gem <name> [level] [quality]` (`ItemCatalog.cs`, `SkillCatalog.cs`; [19](../docs/protocol/19-gems.md) §1, §3).
  Not yet: corruption.
- **Gem requirements (tested, checked with the real client).** A gem's level requirement is the character level of its
  level (GrantedEffectStatSetsPerLevel `PlayerLevelReq`; Haste 24 at gem level 1 . 70 at 20, the client's tooltips),
  and its attribute requirements follow the client's own formula (`0x142476710`: `trunc((3 * level_req + 11) *
  (percent/100)^0.9 * k + 0.5)`, `k` 0.7 for a skill and 0.5 for a support, none below 14;
  fitted against 570 poedb tooltip cells with no mismatch: Haste 20 is 155 Dex, Clarity 20 119 Int, Fireball 20 155).
  Socketing a gem the character is too low or too weak for is refused (0x30, `GemRequirementsUnmet`), and a gem its
  requirements above the character's makes its item inactive (the item's requirements are the highest of its own and its
  gems', `EquipmentStatSources`; lost in PR #175, restored 2026-10-29), as a worn item the character is too low for is: the
  client reds it ("Requires level 70"), the item's mods stop counting and its skills leave the Actor list, so there is
  nothing to cast; a use that arrives anyway is refused without the action (no 0x1ab, and the mana pool again so the
  client's predicted cost comes back; `GemDisabled`).
  An inactive main-hand weapon is ignored: the default attack falls back to the class's unarmed hit, as if it were not
  worn.
  Checked live 2026-10-23 with a level 20 Fireball wand at character level 10, and its attributes with `/set_stat
  intelligence 200` and then 130 (155 needed: the client reds the gem, the wand's mods stop counting, a use is
  refused).
  In play the gem never gets there: its experience stalls at the first level the character does not meet
  (`SkillCatalog.MaxLevelAt` in `GainGemExperience`, the game's rule;
  the client derives levels from experience alone and would not stall by itself), and the level change re-sends the
  skills.
  Not yet: verify every disabled-gem aura transition
  ([19](../docs/protocol/19-gems.md) 1, 3).
- **Gem experience from kills (one gem in the first weapon set checked with the real client).** Every gem socketed in
  an equipped item, in either weapon set, gains 10% of a kill's experience **before the character's level penalty**
  (support gems included; inactive items, gems at their last level and corrupted gems over their cap nothing;
  quest levels and `/add_xp` give the gems nothing), sent with the kill as 0x00bf item changes, each item once, so the
  XP bars follow, and saved with the character (`GainGemExperience`, `PlayerInventories.ChangeSocketedGems`;
  [19](../docs/protocol/19-gems.md) §1).
  A kill's share stops one point below the next threshold, where the client shows the "+": it does not level a gem by
  itself (checked live 2026-10-23; rechecked 2026-11-09: a level-4 Fireball stopped at 8598 and levelled only when the
  click sent it to 8599).
  Several gems in one item and both weapon sets: checked live 2026-11-09.
- **Gem level-up and the lock (checked with the real client).** Clicking the "+" sends C→S 0x31 (inventory, item,
  socket; 14 bytes); the server writes the next threshold into the gem, replaces the item (0xbf) and re-sends the
  skills (0x02bc Actor), so the client derives the new level and its numbers at once, and refuses a gem at its last
  level, a locked one, or one whose next level's requirements the character does not meet (`LevelUpGem`;
  the client's "+" check does not test them, so the server answers with the missing level or attribute).
  The gem level-up panel's "Click to level up ALL" is C→S 0x32 (the id alone, 2 bytes): one explicit click levels every
  socketed gem that is ready (its experience at the stall point the client draws the "+" for) by one level, all in the
  same step, in either weapon set; not-ready, locked, maxed and requirement-blocked gems stay, and a second click finds
  nothing ready. Experience gains, logins, area changes and opening the inventory never level a gem. Sent as one item
  change per row and one skills update, so the client's prompts and XP bars follow (`LevelUpAllGems`, `LevelUpReadyGems`,
  `GemLevelUpReady`, `NextGemLevel`). Checked live 2026-11-09: one click levelled every ready gem across several
  equipment rows and both weapon sets, and a second click levelled only the newly ready gems.
  A right-click sends C→S 0xd7 (the same three u32s and a u8 lock flag;
  15 bytes), kept in the SkillGem flags bit 2 (`Item.GemFlags`) and saved, so the lock survives a login;
  the client greys the "+" while it is set (`LockGem`).
  Checked live 2026-10-23: level 1 → 2, right-click, relogin, unlock, level again;
  a level-up refused at character level 31 (Fireball level 10 needs 32) and with 70 Int (needs 75);
  `/gem_xp <amount|near>` sets the experience.
  Not checked: a gem in the inventory (not socketed), which sent nothing in the captures.

## 尚未实现

暂无。
