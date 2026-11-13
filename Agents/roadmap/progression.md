# 路线图：经验与天赋树

属于 [路线图索引](../Roadmap.md)；这个区域已经能用的功能见 [status/progression.md](../status/progression.md)。
条目沿用路线图的优先级分组，同一组内大致按优先级排列。

## 3. 玩法

### 角色成长：天赋树剩下的部分

- **已知：** 经验、等级、S→C 0x33 与 C→S 0x34（分配）都可用（[15](../docs/protocol/15-progression.md)）。
- **已知：** 在 Bestel 处花金币洗点（C→S 0x36 / 0x37）与精通（0x35）都可用。
- **已知：** 用退点退天赋（0x36 标记 0；0x33 的第一个 u16）可用；悔恨之球（右键，0x58）会增加退点。
- **已知：** 普通天赋树的 21 个大珠宝孔接受普通、魔法与稀有珠宝（面板摆放、保存/读取、全局词缀、退还保护）；
  传奇珠宝走同一路径，但还没有物品来源。
  185 条目录与分阶段的传奇珠宝计划见 [unique-jewels.md](unique-jewels.md)。
- **下一步：** 纹身：全部 100 件、协议、目标/限制规则与交互记录在 [tattoos.md](../docs/game/tattoos.md)。
  客户端流程、32 条纯属性、九个 Makanga 闸门、三个 Journey 纹身与 Journey Makanui 已由客户端核对
  （Hinekora/Kitava Makanga 无效；Ancestral Bloodlines 是部分实现）：C->S `0x77`、`0x33` 的第五个列表、
  物品消耗、重登与重铸石移除。五个 `PassiveSkillOverrides.Limit` 分组在服务端强制执行；Makanui 保存其随机核心天赋。
  下一步：35 条条件/技能行与 Loyalty；七个 Forbidden 升华纹身使用另一套客户端流程。
  仍待做的还有：替换/重新分配与 50 个纹身上限。
- **精准精通：** 目标客户端「精准」组当前提供的五个效果都已为既有的普通攻击命中掷值建模
  （[状态](../status/skills-and-combat.md)、[11](../docs/protocol/11-skills.md) §4–5）：
  精准 = 智力、每点敏捷 +3 而非 +2、每级 +500 / -2、对传奇怪物 40% 更多精准、对近距离敌人 50% 更多精准
  （两者都在攻击的命中掷值处；近距离是固定的 20 单位半径）。
  「召唤物的精准等于你的」不在该目标组内，仍随召唤物精准一起不支持。
  宝石攻击还需要自己的命中掷值，这些目标条件才能作用于它们。
- **已知：** 钉死的普通/Phrecia 拓扑可作为[只读分析产物](../docs/dev/passive-graph-data.md)取得，不需要安装客户端图谱。
- **已知：** 既有 Phrecia 能力与节点来源已抽取进 `Modules/PhreciaAscendancies`。
  它的赛季组成与 Idols 无关；后者有客户端数据但没有玩法实现。
  Behemoth 现在提供其描述符与来源，并带与生成绑定的共享 Rampage/Vaal 引擎。
  Whisperer 在实际 Behemoth 合并之后提供其精确描述符与通用魔力/Recoup/致盲消费者。
  **下一步：** 评审并重放 Whisperer 已和解的 current-main 候选，然后适配其余职业的 PR。
  见[模块范围与交付](../docs/game/phrecia-modules.md)。
  把 Harbinger 作为依赖的 #339 层来评审，并在 Whisperer 验收后和解其剩余差异。
  在其已文档化的引擎边界内完成剩余的接受方/竞赛与无情客户端矩阵。
  见 [Harbinger 集成与验收](../docs/game/harbinger.md)。
- **已知：** 命令选择、保存的身份、独立的点数发放、付费退点与普通/升华图谱连通性现在都有自动化覆盖；
  见[范围与限制](../docs/game/ascendancies.md)。
  普通命令选择、退点、重登与 Gateway 重启已用一次原生决斗者/屠杀者冒烟核对。
- **已知：** Antiquarian 完整的受支持节点消费者与三套合法构建已有原生验收（#98）。
  Whaletooth 会缩放实际的 Enduring Cry 恢复；付费退点、瞬时效果与持久的职业进度都通过。
  见[状态](../status/progression.md)与[模型/证据审计](../docs/game/antiquarian.md)。
- **已知：** Gambler 完整的受支持图谱消费者与两个格挡幸运选项已有原生验收。
  普通的来自怪物的元素异常状态让 Risk Aversion 有了实际的规避消费者。
  当前基线的测试与精确的相关 head 组合都通过；在组合 Whisperer 时请保留人工解出的致命点燃 Recoup 处理，
  记录在 [Gambler](../docs/game/gambler.md)。
- **入手：** 任务天赋/退点奖励与原生迷宫/祭坛流程（#151，[迷宫](world.md#迷宫)）。
  Phrecia 游玩默认仍被闸住；Gambler 的显式选择加入会以只读方式核验单独安装的图谱。
  其他 Phrecia 职业保留其默认闸门；Antiquarian 需要其精确认证过的已安装图谱选择加入。
  不要加自动写入归档，也不要为普通角色要求替换图谱。
- **已知：** Forest Tracking 由完整组的最终闪避推导攻击伤害（#243）。
  它与 Steel Willed 组合；分配/退还/重载、Iron Reflexes 与受支持的伤害有自动化覆盖。
  其他 Daughter of Oshabi 机制与 Phrecia 客户端准入仍是独立工作。
- **已知：** Herald、Vaal、Link 与 Raise Zombie 的家族宝石等级会进入通用的有效技能等级。
  连通的分配/退还/重载有自动化覆盖（#244）。
  家族专属的执行引擎与 Phrecia 客户端准入仍是独立工作。
- **已知：** Aristocrat 的节点都有服务端消费者与 Gateway 覆盖；`Game_VERIFIED_ARISTOCRAT=1` 选择加入。
  **下一步：** 在装有经过评审的合并图谱的客户端上做原生验收，与 Gambler 一样。
  见 [Aristocrat](../docs/game/aristocrat.md)。
- **已知：** Ancestral Commander 的防御、战吼、打击、敌人光环、Spirit 与图腾节点都有服务端消费者
  （Deathrattle、Ancestral Reach 与 Echo、Spirits 与图腾节点是部分实现：投射物打击宝石没有近战命中、
  酋长只用第一个技能、Kahuturoa 等怪物暴击、只有回春图腾会摆出图腾、怪物不攻击图腾）；
  `Game_VERIFIED_ANCESTRAL_COMMANDER=1` 选择加入。
  **下一步：** 实机核对打击（Ancestral Echo 的客户端侧）、Born under Fire、Spirit 增益、参战酋长与图腾；
  等 Marks 存在后再做 Spirit of Rakiata，等怪物会用多于一个技能后做酋长的其他技能，
  等怪物有自己的暴击率后做 Kahuturoa，等投射物打击宝石有近战命中后做打击节点，以及其他图腾技能；然后原生验收。
  见 [Ancestral Commander](../docs/game/ancestral-commander.md)。
- **已知：** Paladin 的节点都有服务端消费者与 Gateway 覆盖；`Game_VERIFIED_PALADIN=1` 选择加入。
  **下一步：** 在装有经过评审的合并图谱的客户端上做原生验收，与 Gambler 一样。Field Medicine 仍缺相位召唤物的行走
  （客户端的相位过滤未追踪）以及遗物爆炸上的药剂伤害规则。普通守卫的 Unwavering Crusade（19641）使用同一套遗物代码，
  需要自己的实机核对。
  见 [Paladin](../docs/game/paladin.md)。
- **已知：** Polytheist 的每个节点都有消费者或已文档化的引擎缺口。`Game_VERIFIED_POLYTHEIST=1` 选择加入。
  **下一步：** 在装有经过评审的合并图谱的客户端上做原生验收（含 Gruthkul 变慢的挥击动画、客户端为 Lunaris 的新星发送的点、
  Solaris 对怪物的致盲、Abberath 的爆炸美术、半径与显示增益）。
  Yugul、腐化之血免疫与 Garukhan 的地面免疫需要先有诅咒、反弹伤害、腐化之血与危险地面。
  见 [Polytheist](../docs/game/polytheist.md)。
- **已知：** Surfcaster 的钓竿显著天赋、Sea Legs、Stormy Seas、Ghost of the Deep、Changing Seasons、Brine Bound、
  Glacial Wave、Within the Tempest 以及小天节点的施法速度与怪物逃跑都有服务端消费者；
  `Game_VERIFIED_SURFCASTER=1` 选择加入。
  **下一步：** 在装有经过评审的合并图谱的客户端上做原生验收。
  见 [Surfcaster](../docs/game/surfcaster.md)。
- **完成标准：** 任务奖励能持久化，且原生祭坛能选择普通升华，并有实机客户端核对。
  实现全部 19 个 Phrecia 职业的特殊玩法，与这条选择生命周期是分开的。
- **Known:** experience, levels, S→C 0x33 and C→S 0x34 (allocate) work ([15](../docs/protocol/15-progression.md)).
- **Known:** gold respec at Bestel (C→S 0x36 / 0x37) and masteries (0x35) work.
- **Known:** refunds with refund points (0x36 flag 0; 0x33's first u16) work;
  Orbs of Regret (right-click, 0x58) add them.
- **Known:** the ordinary tree's 21 large jewel sockets accept ordinary, magic and rare jewels (pane placement,
  save/load, global mods, refund guard); unique jewels use the same path but have no item source yet.
  See [unique-jewels.md](unique-jewels.md) for the 185-entry catalogue and the phased unique-jewel plan.
- **Next:** tattoos: all 100 items, protocol, target/limit rules and interactions are documented in
  [tattoos.md](../docs/game/tattoos.md). The client flow, the plain stats, the nine ailment and avoidance rows,
  the nine skill/minion/flask plain-stat rows, the knockback row, the eleven Honoured rows, the Herald, guard and
  Fortify rows, the corpse-consumption and Hinder rows, all ten Loyalty summons, nine Makanga gates,
  three Journey tattoos, Journey Makanui and Ancestral Bloodlines are client-verified (Hinekora/Kitava Makanga are
  inert): C->S `0x77`, `0x33`'s fifth list, item consumption, relog and Orb of Scouring removal. The
  five `PassiveSkillOverrides.Limit` groups are enforced server-side; Makanui saves its random keystone. The vendor
  recipe works (three same-tribe tattoos for one weighted random tattoo of that tribe; not live-checked). The ten
  Loyalty Leaderships all have consumers (Rage regeneration, travel-skill cooldown speed, the action-speed floor and
  the earlier block/leech/totem stats). The seven event-only Forbidden ascendancy tattoos are applied by the server
  (random class notable saved in `TattooKeystones`, ascendancy-policy stats, `0x33` HASH16) and are partial: the
  client's separate use flow is untraced. Also still open: replace/reallocation and the 50-tattoo cap.
- **Accuracy Mastery:** all five effects currently offered by the target client's Accuracy group are modelled
  for the existing default-attack accuracy roll ([Status](../status/skills-and-combat.md),
  [11](../docs/protocol/11-skills.md) §4–5): Accuracy = Intelligence, +3 per Dexterity instead of +2, +500 / -2 per
  level, 40% more accuracy against Unique Enemies and 50% more against Enemies in Close Range (both at the attack's
  accuracy roll; close range is the constant 20-unit radius).
  Minions' Accuracy equals yours is not offered by that target group and remains unsupported with minion accuracy.
  Gem attacks still need their own accuracy rolls before these target conditions can affect them.
- **Known:** pinned ordinary/Phrecia topology is available as
  [read-only analysis artifacts](../docs/dev/passive-graph-data.md), without installing a client graph.
- **Known:** existing Phrecia capabilities and node sources are extracted into `Modules/Legacy/PhreciaAscendancies`.
  Its league composition is independent of Idols; the latter has client data but no gameplay implementation.
  Behemoth now supplies its descriptor and sources with generation-bound shared Rampage/Vaal engines.
  Whisperer supplies its exact descriptor and generic Mana/Recoup/Blind consumers after the actual Behemoth merge.
  **Next:** review and replay Whisperer's reconciled current-main candidate, then adapt the remaining class PRs.
  See [module scope and delivery](../docs/game/phrecia-modules.md).
  Review Harbinger as the dependent #339 layer and reconcile its remaining diff after Whisperer acceptance.
  Complete the remaining recipient/race and Ruthless client matrix within its documented engine boundaries.
  See [Harbinger integration and acceptance](../docs/game/harbinger.md).
- **Known:** command selection, saved identities, separate point awards, paid refunds and ordinary/ascendancy
  graph connectivity now have automated coverage; see [scope and limitations](../docs/game/ascendancies.md).
  Ordinary command selection, refunds, relog and a Gateway restart are verified with a native Duelist/Slayer smoke.
- **Known:** Antiquarian's complete supported node consumers and three legal builds have native acceptance (#98).
  Whaletooth scales actual Enduring Cry recovery; paid refunds, transient effects and durable class progress pass.
  See [Status](../status/progression.md) and the [model/evidence audit](../docs/game/antiquarian.md).
- **Known:** Gambler's complete supported graph consumers and both Block Luck choices have native acceptance.
  Ordinary incoming elemental ailments give Risk Aversion an actual avoidance consumer.
  Current-base tests and exact related-head composition pass; preserve the manual lethal-ignite Recoup resolution
  when composing Whisperer, as recorded in [Gambler](../docs/game/gambler.md).
- **Start:** quest passive/refund rewards and the native Labyrinth/Altar flow
  (#151, [The Labyrinth](world.md#the-labyrinth)).
  Phrecia play stays gated by default; Gambler's explicit opt-in verifies the separately installed graph read-only.
  Other Phrecia classes retain their default gate; Antiquarian requires its exact verified installed graph opt-in.
  Do not add automatic archive writes or require the alternate graph for ordinary characters.
- **Known:** Forest Tracking derives attack damage from complete groups of final evasion (#243).
  It composes with Steel Willed; allocation/refund/reload, Iron Reflexes and supported damage have automated coverage.
  Other Daughter of Oshabi mechanics and Phrecia client admission remain separate work.
- **Known:** family gem levels for Herald, Vaal, Link and Raise Zombie reach the common effective skill level.
  Connected allocation/refund/reload has automated coverage (#244).
  Family-specific execution engines and Phrecia client admission remain separate work.
- **Known:** Aristocrat's nodes all have server consumers and Gateway coverage; `Game_VERIFIED_ARISTOCRAT=1` opts in.
  **Next:** native acceptance on a client with the reviewed combined graph, as for Gambler.
  See [Aristocrat](../docs/game/aristocrat.md).
- **Known:** Ancestral Commander's defensive, warcry, strike, enemy-aura, Spirit and totem nodes have server
  consumers (Deathrattle, Ancestral Reach and Echo, the Spirits and the totem nodes partly: the Projectile Strike gems
  have no melee hit, the chieftains use only their first skill, Kahuturoa waits for monster crits, only Rejuvenation
  Totem places a totem, and monsters do not attack totems);
  `Game_VERIFIED_ANCESTRAL_COMMANDER=1` opts in.
  **Next:** a live check of the strikes (Ancestral Echo's client side), Born under Fire, the Spirit buffs, the fighting
  chieftains and the totems; Spirit of Rakiata once Marks exist, the chieftains' other skills once monsters use more
  than one, Kahuturoa once monsters have their own crit chance, the strike nodes once Projectile Strikes have their
  melee hit, and other totem skills; then native acceptance.
  See [Ancestral Commander](../docs/game/ancestral-commander.md).
- **Known:** Paladin's nodes all have server consumers and Gateway coverage; `Game_VERIFIED_PALADIN=1` opts in.
  **Next:** native acceptance on a client with the reviewed combined graph, as for Gambler. Field Medicine still
  lacks a phasing minion's walk (the client's phasing filter is not traced) and the rule for flask damage on a
  relic's explosion. The ordinary Guardian's Unwavering Crusade (19641) uses the same relic code and needs its own
  live check.
  See [Paladin](../docs/game/paladin.md).
- **Known:** every Polytheist node has a consumer or a documented engine gap.
  `Game_VERIFIED_POLYTHEIST=1` opts in.
  **Next:** native acceptance on a client with the reviewed combined graph (including Gruthkul's slowed swing
  animation, the point the client sends for a Nova with Lunaris, Solaris's blind on monsters, and Abberath's explosion
  art, radius and display buff).
  Yugul, Corrupted Blood immunity and Garukhan's ground immunity need curses, reflected damage, Corrupted Blood and
  hazardous ground first.
  See [Polytheist](../docs/game/polytheist.md).
- **Known:** Surfcaster's fishing-rod notables, Sea Legs, Stormy Seas, Ghost of the Deep, Changing Seasons, Brine
  Bound, Glacial Wave, Within the Tempest and the small nodes' cast speed and monster flee have server consumers;
  `Game_VERIFIED_SURFCASTER=1` opts in.
  **Next:** native acceptance on a client with the reviewed combined graph.
  See [Surfcaster](../docs/game/surfcaster.md).
- **Done when:** quest rewards persist and the native Altar can choose an ordinary ascendancy, with real-client checks.
  Implementing all 19 Phrecia classes' special gameplay is separate from this selection lifecycle.
