# 路线图：宝石

属于 [路线图索引](../Roadmap.md)；这个区域已经能用的功能见 [status/gems.md](../status/gems.md)。
条目沿用路线图的优先级分组，同一组内大致按优先级排列。

## 3. 玩法

### 宝石：运作方式

Nessa 的本地全色供货、觉醒与卓越的拆分、替代变体的分页，以及独立的、按进度过滤的 Skills 页签都已实现；
见 [status/gems.md](../status/gems.md) 里的宝石商人条目。
White Skills 合并两种白色类别。Gateway 重启后请在真实客户端里核对更新后的多页商店。
未来的玩法限制应放在 Skills 页签里；颜色页签必须始终是与剧情进度无关的完整开发工具。
Nessa 所有页签上的宝石都是无限库存；在同一次实机核对里，顺便验证重复购买后的原生商店刷新。
库存/序列化的自动化覆盖在 NessaShopTabsTests。

宝石系统按 2026-10-20 议定的方案，分下面几块实现（#62、#63、#64）。
关于客户端与数据已知的内容见 [19](../docs/protocol/19-gems.md)。

- **一颗宝石物品**由一个基底与一个变体构成：普通宝石；
  转化（「of X」）或 Trarthan（「of Trarthus」）变体，它作为自己的技能存在，有自己的效果、标签与机制；
  或者瓦尔宝石，它授予常规的瓦尔技能与其基础技能（若其变体是转化或 Trarthan，则是那种）。
  宝石等级来自其经验，上限为其经验类型的上限：多数宝石 20，卓越辅助 3，觉醒 Empower、Enhance、Enlighten 为 4，
  其他觉醒辅助为 5，单级宝石为 1。腐化与额外等级可以更高，但绝不超过 40。
- **规则：** 镶嵌等级或属性需求不满足的宝石会被拒绝。
  已镶嵌但不再满足需求的宝石会被禁用：技能仍留在技能栏但无法使用，其辅助不生效，升级图标置灰，宝石仍留在孔里。
  每颗已镶嵌的技能宝石都是独立技能，镶嵌期间保持自己的 id；宝石离开时光环随之结束。
  辅助宝石永远不会被当作技能授予。
  Empower、Enhance、Enlighten 之外的觉醒辅助（自 3.28 起禁用）、「[DNT]」占位变体，以及名字里带
  「[UNUSED] …」或「WIP …」的宝石行永远不会被创建。

### 宝石 1：宝石等级、稳定的技能 id 与需求（#62）

- **已知：** 每颗已镶嵌的宝石在镶嵌期间保持其技能 id，光环或正义之火随其宝石一起结束（(a) 部分）。
  (b) 部分：宝石的技能使用其等级（来自经验）与品质，转化、Trarthan 与瓦尔宝石授予各自的技能，变体是
  SkillGem 数据里的 u32，`/gem` 能生成任意宝石（[19](../docs/protocol/19-gems.md) §1、§3）；尚未用真实客户端核对。
  (c) 部分即需求，已完成：等级取 `PlayerLevelReq`（Haste 宝石 1 级 24……20 级 70，与客户端提示一致），
  属性用客户端自己的公式，由 `0x142476710` 拟合、并用 570 个 poedb 提示格核对无一不符
  （`trunc((3·level_req + 11)·(percent/100)^0.9·k + 0.5)`，辅助 `k` 为 0.5、技能为 0.7，均不低于 14）。
  宝石经验会在角色未达到的第一个等级处停住；
  等级或属性任一不足时镶嵌被拒，而角色等级低于宝石需求时（用 `/set_level` 调低之后）会让其物品失效：
  客户端标红、词缀与宝石不再计入、技能从列表消失因而无法施放，失效的武器则回落到徒手攻击
  （见状态文档的「Gem requirements」）。
  2026-10-23 实机核对了等级与属性（把 `/set_stat intelligence` 调到 20 级 Fireball 所需 155 的上下两侧：
  130 时客户端把宝石标红、法杖词缀不再计入）。
  仍待解决：客户端仍会把失效物品画成穿着状态（手里拿着变红的法杖）——需要跟官方客户端对比那里的表现，
  以及是否改为把物品移出那一行（[13](../docs/protocol/13-items.md) §2d）。
- **入手：** 实机核对 (b) 部分（20 级 Fireball 的伤害与消耗、带品质的光环、Ice Nova of Frostbolts、
  瓦尔宝石的两个技能）。
- **完成标准：** 任意等级与品质的 `/gem` 宝石以该等级的数值施放，转化与瓦尔宝石授予正确的技能，
  其他宝石进出时技能 id 不变，且需求与提示一致并被强制执行。

### 宝石 3：辅助宝石与连接（#64）

- **已知：** 技能记录里有一份辅助列表（[11](../docs/protocol/11-skills.md) §3）；
  把辅助当作技能授予会让客户端崩溃（[13](../docs/protocol/13-items.md) §6）。
  GrantedEffects 保存每个辅助兼容的技能类型与它在技能栏上的字母；GrantedEffectsPerLevel 保存其消耗倍率。
- **已实现：** 连携的原生兼容性、完整的辅助引用、重复消解，以及普通的伤害、范围、速度、暴击、消耗与保留计算
  （[25](../docs/protocol/25-gem-supports.md)）。带范围增大的 Cyclone 已用真实客户端核对。
  物品授予的辅助引用与本地已镶嵌宝石的等级/品质加成现在共用同一套消解与计算。
  物品辅助记录、重复消解、骰出的等级与夺宝更多伤害都有自动化覆盖。
- **下一步：** 单独实现特殊执行机制：触发、攻击重复（Multistrike）、Ice Nova 的重复环形与 Spell Echo 最后一段重复的
  数值（Greater 与觉醒版 Spell Echo）、投射物分叉、会使用自己技能的图腾（目前只有回春图腾的光环有执行器，
  而且怪物不会攻击图腾；[11](../docs/protocol/11-skills.md) §10g）、地雷、召唤物、吸血与条件效果；
  每一项都要对照原生数据与实机行为核对。
  Cyclone 的物品授予角标字母/颜色与受辅助的面板数值已用真实客户端核对。
  核对改 mod 之后的原生刷新，以及受控的前后本地等级/品质变化。
- **完成标准：** 连了 Arcane Surge 的 Fireball 显示该辅助的字母并支付乘算后的消耗，像 Faster Casting 这样已建模的辅助
  会改变技能，且辅助导致的崩溃消失。
- **Known:** the skill record has a support list ([11](../docs/protocol/11-skills.md) §3);
  granting a support as a skill crashes the client ([13](../docs/protocol/13-items.md) §6).
  GrantedEffects holds each support's compatible skill types and its skill-bar letter;
  GrantedEffectsPerLevel its cost multiplier.
- **Implemented:** linked native compatibility, complete support references, duplicate resolution and ordinary
  damage, area, speed, critical, cost and reservation calculations ([25](../docs/protocol/25-gem-supports.md)).
  Cyclone with Increased Area of Effect was checked with the real client.
  Item-granted support references and local socketed-gem level/quality bonuses now share the resolver and calculations.
  Item-support records, duplicate resolution, rolled levels and Delve more damage have automated coverage.
- **Next:** implement remaining trigger payloads beyond CWDT projectile spells, attack repeats (Multistrike), Ice Nova's
  repeated rings and Spell Echo's final-repeat stats (Greater and Awakened Spell Echo), projectile branching,
  remaining totem payloads (Artillery/Focused Ballista, beams, channels and pulses; monsters do not attack totems;
  [11](../docs/protocol/11-skills.md) §10g), mines, minions, leech and conditional effects; verify each against
  native data and live behaviour.
  Barrage still needs sequential release, cancellation and per-projectile hit admission beyond its attack-time penalty.
  Cyclone's item-granted badge letters/colours and supported panel values were checked with the real client.
  Verify native refresh after mod changes and controlled before/after local level/quality changes.
- **Done when:** Fireball linked with Arcane Surge shows the support's letter and pays the multiplied cost, a modelled
  support such as Faster Casting changes the skill, and the support crash is gone.

### 宝石后续：品质、获取、腐化、额外等级

- **品质：** 实机核对 Gemcutter's Prism，以及品质属性确实改变技能（`SkillCatalog.EffectStats`）。
- **获取宝石：** 任务奖励、城镇宝石商人与怪物掉落已完成（服务端能构建的每个 `QuestRewardOffers` 行，按职业；
  商人的已完成任务/职业进度、Siosa 与 Lilly，以及 3.29 的跨职业辅助；物品掉落的 4% 宝石份额，按 DropLevel；
  [19](../docs/protocol/19-gems.md) §5）。
  剩余：等第六幕与图书馆可达后（这两个城镇还没有行走地图抓取）实机核对 Lilly 与 Siosa。
- **腐化与瓦尔技能：** 宝石上的瓦尔宝珠结果（超过上限一级、品质最高 23%、瓦尔变体）、宝石透镜的双腐化，
  以及受支持的瓦尔技能执行器。
  灵魂收集/额度载荷已实现；原生验收留在 skills-and-combat 里。
  与物品腐化（#60）一起做。
- **额外等级：** 本地全宝石、主动/辅助与 GemTags 家族的等级/品质加成现在会进入实际宝石计算，包括 Empower；
  已存经验与固有需求不变。
  用真实客户端核对物品加成这条路径，并覆盖其余特殊的跨物品与条件词缀。
- **觉醒辅助：** 它们的效果替代普通版本而不是叠加；其等级需求从 72 起。
