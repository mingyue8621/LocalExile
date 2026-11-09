# 状态：技能、光环、属性与战斗

属于 [状态索引](../Status.md)；这个区域接下来要做什么见
[路线图/技能与战斗](../roadmap/skills-and-combat.md)。

## 已实现（已用真实客户端验证）

- 强化（Fortification）：合格的近战命中通过一套显式的 LocalExile 生成模型积累临时护佑。
  原生核对确认了 buff 组件、到期、20/30 层上限、攻击速度与实际减伤。
  已流逝的生命回复、到期即停、跨区域携带、死亡与重登也都验证过。
  证据用的是未升华角色上的裸输入；它不能确立巨兽（Behemoth）的选择。
  公式、来源覆盖与尚未做的扩展见 [强化](../docs/game/fortification.md)。
- 单手武器配盾的「仅盾」技能（2026-11-01 实测：权杖 + 盾的盾牌冲锋）。
  当 `is_dual_wielding` 被设置而主手不符合时，客户端会把技能置灰，而它从不下发这个属性：不发时，
  每个角色都被读成 1。
  现在网关会带上双手的原生武器类型（`WeaponTypeStats`）下发 `is_dual_wielding`，技能才可用。
  盾牌冲锋的冲撞（2026-11-01 实测）：结束时播放收尾特效（用宝石 `AttackTime` 的盾牌副手攻击时间），
  以自身速度前进（冲撞中 `movement_velocity_+%` 22 → 112 → 22），沿路径命中（约每 100 ms、8 格内，
  每只怪一次）并在终点命中（16 格内）。
  它的命中与「攻击」页签用盾牌伤害（宝石的自加伤害，加上每 15 点盾牌护甲与闪避的伤害，按宝石攻击时间），
  随已冲撞距离最多再 +100%，冲撞上限 75 格（2026-11-01 实测）。眩晕缩放与
  `shield_charge_extra_distance` 未实现。
  代码：`WeaponTypeStats.cs`、`Skills.cs`（`FinalPlayerStatsFrom`、`GemSkills`）、`ShieldCharge.cs`、`SkillCatalog.cs`。
  文档：[11](../docs/protocol/11-skills.md) §10a 与 §10k、[13](../docs/protocol/13-items.md)「客户端武器类型属性」。
- 古物学者（Antiquarian）的真实已选节点现在驱动守护（Ward）的吸收/回复，以及符文石板可暂停的肾上腺素。
  巨人的零智力也能达到需求与池；鲸齿延长实际的坚毅战吼回复。
  龟壳在近战眩晕被接受后，把一秒内 8% 的生命并入，到期时按原生生命做修正。
  狮眼之发绕过受支持攻击的命中判定，弓按实际飞出的投射物缩放，盾牌替换可用盾牌的基础格挡、
  并在护甲之后降低物理远程命中。
  三套原生版本覆盖全部节点、战斗控制、付费退还以及效果/存档生命周期。
  证据、精确的图谱准入与仍存的引擎边界见 [古物学者](../docs/game/antiquarian.md)。
- 条件暴击策略（#242）：普通、濒死、专注以及尖锐（Sharp）与脆弱（Brittle）各类来源共用同一个带符号的
  钳制。相反的幸运互相抵消，多个幸运来源合计最多只多抽一次暴击。
  徒手倍率只作用于当前的空手近战攻击，面板数值一致。
  十五项原生核对验证了真实的 200/150 暴击、激活集切换与加成移除。
  一个 66 生命的野蛮人验证了高于、正好等于和低于含端点的 50% 边界。
  原生虚函数确认了抵消、钳制、不幸状态、不可暴击、治疗、死亡与重登。
  原生证据针对 `ea24316`；已完成的 `effb797` 组合有自动化覆盖，尚待原生复跑。
  这些核对用的是未升华角色的裸输入；法术/弓的接触规则与随机数顺序有自动化覆盖。
  一次解析出的 200 点暴击打 400 生命，用同一个已解析倍率生成 14 层强化。
  去掉徒手加成得到 150 伤害、12 层；不可暴击则 100 伤害、10 层。
  #75 的复核现在让一次宝石使用的所有区域目标、多发、穿透/回返命中与既有命中回调共用同一次暴击抽取。
  宝石幸运在准入使用时捕获；每次命中各自按当前概率/禁止状态取样，并独立确认攻击命中。
  新增 22 个用例与 4 个改造过的解析使用时机幸运控制覆盖了改动后的生命周期。这项修正属自动化，
  不含在更早的原生抓取里。
  普通近战/物体弓现在在贴近之后的挥击准入时捕获幸运，地面弓在输入准入时捕获。新增的 20 个控制覆盖
  确认之前生命/专注/裸值的变化，以及走位进入的时机；八个既有的带符号来源/已解除禁止用例现在断言准入幸运。
  其余普通弓快照、特殊重复与原生复跑仍未完成。
  付费升华退还、专注/诅咒生命周期仍属独立的集成工作。
  见 [11-skills.md](../docs/protocol/11-skills.md) 的条件暴击策略。
- Holy Sweep's art: the skill sends its base `skill_art_variation` 2 and a worn effect's value adds to it, so hits no
  longer crash the client drawing the hammers (live 2026-11-08 in a map: none, Originator's, Awakener's, Abyssal).
  Code: `SkillArtBase.cs`. Docs: [23](../docs/protocol/23-microtransactions.md) §3.
- Holy Sweep's area: its swings (ActionTypes Sweep, SweepAltX) hit around the player, where the client draws them,
  instead of around the aimed monster (live 2026-11-08); other melee area attacks still land on the target.
  Code: `MeleeAreaCentre.cs`, `AreaSkillHit`. Docs: [11](../docs/protocol/11-skills.md) §9a.
- Fortification: qualifying melee hits build temporary protection through an explicit LocalExile generation model.
  Native checks confirmed the buff component, expiry, 20/30-stack limits, attack speed and actual hit protection.
  Elapsed Life regeneration, stopping at expiry, area carry, death and relog were also verified.
  The evidence uses a new unascended character with raw inputs; it does not establish Behemoth selection.
  See [Fortification](../docs/game/fortification.md) for the formula, source coverage and remaining extensions.
- Shield-only skills beside a one-handed weapon (verified live 2026-11-01: Shield Charge with a sceptre and a shield).
  The client greys out a skill when `is_dual_wielding` is set and the main hand does not fit it, and it never derives
  that stat from network stats: unsent, it read 1 for every character.
  The Gateway now sends `is_dual_wielding` with both hands' native weapon types (`WeaponTypeStats`), and the skill
  became usable.
  Shield Charge's charge (verified live 2026-11-01): it ends and plays its end effect (the shield's off-hand attack time
  from the gem's `AttackTime`), runs at its own speed (`movement_velocity_+%` 22 → 112 → 22 while charging), and hits
  along its path (within 8 cells about every 100 ms, each monster once) and where it ends (within 16 cells).
  Its hits and Offence tab use the shield's damage (the gem's added damage plus its damage per 15 shield armour and
  evasion, at the gem's attack time), up to 100% more by distance charged, the charge capped at 75 cells (verified live
  2026-11-01). The stun scaling and `shield_charge_extra_distance` are not applied.
  Code: `WeaponTypeStats.cs`, `Skills.cs` (`FinalPlayerStatsFrom`, `GemSkills`), `ShieldCharge.cs`, `SkillCatalog.cs`.
  Docs: [11](../docs/protocol/11-skills.md) §10a and §10k, [13](../docs/protocol/13-items.md) "Client weapon-type stats".
- Antiquarian's real selected nodes now drive Ward absorption/restoration and Runic Tablet's pausable Adrenaline.
  Goliath's zero Intelligence reaches requirements and pools; Whaletooth extends actual Enduring Cry recovery.
  Turtle Shell integrates 8% Life over one second after accepted melee stun, with native Life correction at expiry.
  Lioneye's Hair bypasses supported attack accuracy, Bow scales actual travelled projectiles, and Shield replaces
  usable-shield base block and reduces physical ranged hits after armour.
  Three native builds cover all nodes, combat controls, paid refunds and effect/save lifecycles.
  See [Antiquarian](../docs/game/antiquarian.md) for evidence, exact graph admission and remaining engine boundaries.
- Conditional critical policy (#242): ordinary, low-life, Focus and Sharp and Brittle sources share a signed clamp.
  Opposing luck cancels, and multiple lucky sources still add at most one critical draw.
  The unarmed multiplier applies only to the active empty-hand melee attack, with matching panel values.
  Fifteen native checks verified real 200/150 critical hits, active-set switching and bonus removal.
  A 66-Life Marauder verified above, exactly at and below the inclusive 50% boundary.
  Native virtuals confirmed cancellation, clamping, unlucky state, cannot-crit, healing, death and relog.
  Native evidence targets `ea24316`; the resolved `effb797` composition has automated coverage and awaits native reruns.
  The checks use raw inputs on an unascended character; spell/bow contact rules and RNG order have automated coverage.
  A parsed 200-damage critical hit against 400 Life generates 14 Fortification from the same resolved multiplier.
  Removing the unarmed bonus gives 150 damage and 12 stacks; cannot-crit gives 100 damage and 10 stacks.
  The issue #75 review now shares one critical roll across a gem use's area targets, volley, pierce/return hits
  and existing impact callbacks. Gem luck is captured at admitted use; each hit samples current chance/prohibition
  and confirms attack accuracy independently. Twenty-two new cases and four updated parsed use-time-luck controls
  cover the changed lifecycle. This correction is automated, not part of the older native captures.
  Ordinary melee/object-bow now capture luck at swing admission after approach, and ground bows at their input
  admission. Twenty added controls cover Life/Focus/raw changes before confirmation, plus walk-in timing;
  eight existing signed-source/released-prohibition cases now assert admission luck. The other ordinary-bow
  snapshots, special repeats and native reruns remain open.
  Successful paid ascendancy refunds and Focus/curse lifecycles remain separate integration work.
  See [11-skills.md](../docs/protocol/11-skills.md), conditional critical policy.

- 通用狂怒（#281）：受来源限定的攻击/近战/暴击与受击获得的层数共用半秒间隔。
  上限、效果、独立的延迟回复/衰减速度、阻止与最终攻击倍率都会被消费。
  十七项原生核对验证了裸来源生成、已装 buff/层数、实际伤害与未变的攻速。
  固定 100 伤害在 15 层狂怒时显示并打出 115；阻止、衰减、暂停、传送、死亡与重登均通过。
  核对用的是未升华的女巫；巨兽选择与瓦尔消费仍分开。
  受支持接触、区域扫描去重、排队刷新与 140 伤害/11 层强化的组合有自动化覆盖；
  更多原生组合核对仍在 [26-rage.md](../docs/protocol/26-rage.md)。
  被上限截断的尝试增益与冻结的目标加载时间使用显式的 LocalExile 策略 v1。

- 每点最终力量提供的魔力（#287）：`X_mana_per_4_strength` 在增幅之前并入普通固定魔力桶。
  十三项原生控制验证了完整的四点力量分组、带符号输入、来源移除与重登。
  新建女巫的 56 魔力在 14/15 力量时变为 80，16 力量时 88，再加 25% 魔力增幅为 110。
  基于最大魔力的攻击闪电跟随最终池；仅供显示的力量覆盖不能反过来喂它。
  闲置光环保留与当前魔力钳制有解密指令线覆盖，含发送闸。
  历史原生控制使用裸输入与合成装备；Phrecia 选择仍受限。
  完整的「精神调理」节点现在在自动化测试中与「意志之力」组合；原生复跑仍待做。
  见 [11](../docs/protocol/11-skills.md) 第 4 节。

- 玩家 Actor 缩放（#299）：被动、激活物品、光环、药剂与时装的尺寸输入共用 final1973。
  元数据 limit100 在普通上限之前相加，然后是显式的开发替换值，包括零。
  十八项原生核对在一个新建未升华女巫上量了真实的 Positioned 与插值 Render 缩放。
  物品 20 加时装 20 加时装 3 得到 1.43 缩放；移除来源恢复 1.0。
  进入区域、重登、死亡/复活、默认上限与显式零覆盖都验证过。
  只有 raw720 时原生模型不变；服务端推导的 final1973 驱动它的缩放观察者。
  原生物体尺寸、类型缩放与拉伸未变；几何消费方有自动化覆盖。
  当前/后期对等体变更、重置、创建与复活有回环线覆盖，不是第二客户端的视觉核对。
  Phrecia 准入、狂怒（Rampage）与独立的原生近战距离标志 9807 仍是分开的工作。
  公式与证据边界见 [04](../docs/protocol/04-player-entity.md#41a-player-actor-scale-299)。

- 对混沌命中的护甲（#288）：布尔属性 `base_armour_applies_to_chaos_damage` 在混沌抗性之后启用既有护甲
  模型，套用普通的 90% 上限。
  十一项原生控制验证了真实的玩家生命损失、来源开关、抗性顺序与重登。
  额外的物理/元素减伤仍归各自类型；原生最终护甲值未变。
  混合怪物报文、格挡、抑制、ES、强化与持续伤害分离有自动化覆盖。
  原生控制使用未升华野蛮人上的裸来源；不灭（Incorruptible）选择与部分护甲百分比仍分开。
  近期受击翻倍在自动化测试中组合，另有 `c9cd1864` 上的全新有序联合原生控制。
  见 [20](../docs/protocol/20-damage.md) 第 2e 节。

- 属性增幅（#285）：按类型与全属性的百分比，用文档化的 PoB 取整模型，对完整的职业/被动/激活装备固定桶
  缩放一次。
  `60044ad` 上的历史证据有十三项原生控制，覆盖未升华野蛮人上的 3% 力量、加性增幅、派生池与一致的
  需求值。
  板甲背心在其需求以下停止贡献，最终属性恢复时再恢复。
  算出的零属性保持为零；来源清空与重登恢复预期基础值。
  自身物品百分比保留、级联激活、被动退还与武器切换有自动化覆盖。
  当前主线与「每点最终力量的魔力」、未激活武器与裸零/关闭覆盖的组合有回归。
  十一项当前主线原生控制验证最终力量分组、加性增幅、魔力与装备需求。
  最大魔力随属性上升从 47 到 111，再到 127/145；100% 魔力增幅一次性把它抬到 290。
  来源移除与普通背心的需求禁用/恢复在重基后的来源 `00b246f2` 通过。
  `c9cd1864` 上的有序联合原生验证也确认最终属性能喂「魔力转护甲」转换。
  奇数最终智力保留其半份魔力，直到转换后的池取整；16 力量 / 7 智力给出 15 魔力。
  未激活武器、级联装备、被动退还与激活光环的组合仍只有自动化覆盖。
  证据不确立与官服的分数级一致，也不启用 Phrecia 职业选择。
  见 [11](../docs/protocol/11-skills.md) 第 4 节与 [13](../docs/protocol/13-items.md) 第 2d 节。

- 意志之力（#291）：裸 `keystone_mental_conditioning` 把 80% 的固定魔力转成两倍的护甲。
  按文档化的 PoB 计算模型，魔力与护甲增幅对该转换贡献做加性叠加。
  原始 head `7678d60` 上的十三项历史原生控制覆盖开关、增幅、取整与重登。
  那些控制在未升华女巫上实际跑了钢志（Steel Willed）攻击伤害与剩余魔力闪电。
  当前主线测试组合「每点最终力量的魔力」、激活装备、增幅、混沌护甲/CI 与法师之血。
  重基到 `939b44fe` 后新增属性增幅与完整指令快照回归。
  最终缩放后的力量/智力、奇数半魔力、独立的零/关闭覆盖与主线边界在那里组合。
  保留、钳制、零/关闭/移除、铁之反射、更多（MORE）与自身光环发送闸的新鲜度有自动化覆盖。
  `c9cd1864` 上的历史联合验证通过 28 项原生控制，含八个最终属性转换用例。
  B=127 给出 25 魔力 / 222 护甲；100% 魔力给出 51 / 425；奇数最终智力保留其半份魔力。
  剩余原生用例包括保留、铁之反射、更多与完整节点的独立消费方。
  `939b44fe` 上的候选没有新的原生验收；更早的控制仍是历史证据。
  Phrecia 选择仍受限。
  见 [模型与范围](../docs/game/power-of-purpose.md)。

- 技能与战斗：玩家在 Actor 技能表里拥有默认攻击（PlayerMelee，Characters.dat 的 StartingMeleeSkill）；
  它能上技能栏，走进距离，按徒手速度挥击（双持 10% 时 757 ms），或用 `/set_stat attack_speed_+%` 更快
  （服务端计算的最终属性，0x2bc 技能更新，面板的每秒攻击次数），服务端在接触点把怪物的生命降 2–5；
  移动会取消；死亡怪物的血条隐藏。
  需求不满足的武器会离手：模型把该槽位发成空，于是客户端把它限制所需的技能置灰、默认攻击变成挥拳；
  服务端也拒绝这类技能，并在资格变化而没有换装时重发模型。仅自动化；待原生核对。
  代码：`GrantedSkill`、`SkillCatalog.cs`、Gateway。
  文档：[11](../docs/protocol/11-skills.md) §10a。
- 本地战斗包：站定的默认近战不再吃 150 ms 的走位偏移。
  本条里的原生证据属于 LocalExile；独立的落点移植有自动化验证，仍需要原生复跑，记录见
  [移植记录](../docs/dev/ranged-monster-actions-port.md)。
  一次已进入距离的徒手攻击没有贴近过程，它的权威命中在动作回显之后 312 ms 到达
  （757 ms 攻击，attack1 接触基线）；正的走位进入仍保留补偿。
  普通地面/物体法杖攻击会创建原生的 WandProjectile 弹，在第一次实际碰撞时结算伤害，
  并在移除前播放原生 500 ms 命中剪辑。
  职业 ACT 池与原生按动作播种的选择器选出正确的主手剪辑与释放接触点。
  十六个地面右键输入匹配到原生女巫剪辑 31/36/39/42；物体序列 4113 匹配剪辑 39。
  15 个地面样本里的释放在所选接触点之后 6.1 到 31.4 ms；第一个冷样本晚了 55.6 ms。
  敌人受击退缩现在会取消移动与普通的待发挥击，按原生要求的 TakeHit 恢复暂停 AI，并永久作废旧的排队接触。
  移动中的溺亡者原生样本在恢复期间保持在一格；之后 AI 移动恢复。原先只有骨架的受伤动画不会停止位移；
  投射物命中（`CommitProjectileHit`）改为发送同样的原生恢复而不是那个剪辑。
  更快的怪物规划器在原生控制里 38 到 50 ms 后就发出第一次追击，没有重复指令。
  转身跟踪、完整的 2.4 s Drowned 攻击节奏与真实的 Esc 菜单暂停/恢复也验证过。
  完整的副手计时/伤害与非零的固有攻击重复未实现。
  代码：`Program.Combat.cs`、`Program.RangedAttacks.cs`、`AnimationContacts.cs`、`AnimationVariantSelector.cs`、
  `Program.Monsters.cs`、`HitReaction.cs`。
  证据：`artifacts/combat-validation/`、`artifacts/wand-ai-validation/wand-native-summary.json`
  与 `wand-after-object.json`；文档：[11](../docs/protocol/11-skills.md)。
- 穿刺（Impale）：普通近战/弓与受支持的投射物/区域命中会存下实际减伤前的物理掷值，
  含来源暴击倍率与穿刺效果。每个实例保留五次后续命中/八秒，带着捕获的修正；任何被接受的命中都能触发它
  独立的反射伤害，不再有第二次暴击、仅法术或回返倍率。不再有人为的五层上限或死亡清除。目标规避、
  本地/攻击/法术范围、真实的穿刺辅助与全部三个已选概率别名都有自动化覆盖。
  2026-10-20 的历史原生观察用的是旧的「取最大掷值」实现；当前原生核对、条件来源与进阶的应用/消耗规则
  仍未完成。
  代码：`Impale.cs`、`ImpaleStats.cs`、`Program.Impale.cs`、`Program.Combat.cs`、`OrdinaryRangedShot.cs`。
  文档：[20](../docs/protocol/20-damage.md) §3a 与 [11](../docs/protocol/11-skills.md) §14。
- 攻击（Offence）面板伤害行：服务端挥出的那次命中成为 `main_hand_minimum/maximum_total_damage` 以及
  按类型的最小/最大行，并带上 `skill_uses_main_hand` 让客户端显示它们；
  取整数，仅主手，空报文则一条都不发。
  这些行与服务端的命中共用 `AttackHit`，所以按最大魔力加的闪电（低语者的「我们知晓一切」）
  也以同样方式出现在面板里。
  十四项真实客户端属性核对验证了 56 魔力 → 3–3 闪电、1000 → 60–60、50% 闪电增幅 → 90–90、
  997 → 按面板下限的 59–59，以及移除与重登。
  通用攻击增幅生效；近战增幅跟随实际攻击类型与激活武器集。
  原生的法杖/短棒切换验证了该增幅的远程排除与近战纳入。
  这些原生核对属于原始 head `378ffde`。
  已完成的受支持技能路径有自动化的上下文、效果与法术隔离核对。
  需原生复核那个组合。
  代码：`PanelStats.HandDamage`、Gateway（`CharacterPlayerStats`）。
  文档：[11](../docs/protocol/11-skills.md) §14。
- 低语者「你的心智被摧毁」：已完成的法术增/减幅也以 150% 的数值缩放攻击。
  武器、自加伤害与受支持攻击的构造函数共用这份分数加性贡献。
  原生面板与目标生命核对验证了 100 到 250、加性 230、半百分点端点 203 以及减幅。
  移除与重登会清掉效果；普通弓保留释放快照，受支持法术仍隔离。
  该原生核对使用开发属性输入；Phrecia 节点选择与低语者其余部分仍分开。
  这些原生结果是历史的裸属性核对；扩展后的「奥术之力」覆盖列在「部分实现」下。
  代码：`ArcaneMight`、`DamageStats`、`SkillCatalog.Unarmed.HitDamage`、`SupportedAttackDamage`。
  文档：[20](../docs/protocol/20-damage.md) §2c。
- 巨兽的钢志：每完整 450 点最终护甲增加 2% 攻击伤害增幅。
  该贡献一次性并入裸攻击增幅，并喂武器物理、自加伤害与受支持攻击。
  原生固定命中核对验证阈值、加性 204/214、修正移除与最大魔力闪电。
  武器物理面板核对显示 2–5 变成 6–15；重登清掉开发输入。
  原生核对用未升华角色上的裸属性；选择仍分开。
  近期受击的护甲防御实现在下文；当前主线的组合待原生验证。
  代码：`ArmourAttackDamage`、Gateway（`CharacterPlayerStats`）、`SkillCatalog.Unarmed.IncreasedDamage`。
  文档：[20](../docs/protocol/20-damage.md) §2d。
- 钢志的受击护甲：此前被接受的敌人接触提供一个四秒的权威计数器。
  每次命中的贡献上限 30%，然后真实的通用双倍护甲概率在 0..100 钳制之前相加。
  一次判定同时供给物理与已启用的混沌减伤；被格挡与零接触计入但不掷护甲随机。
  永久护甲与出招攻击加成保持不变。
  解析过的开发路由、混合怪物报文、闲置到期、暂停与过期挥击通过测试。
  连接生命周期、AI 收集世代与排队的完整/部分属性发送也有自动化覆盖。
  回到上一对、排队到期与被拒发送的回归覆盖了这些剩余的完整写入方。
  脏光环前缀与强化尾段按线上顺序跨到期构建，并有解密对/闲置核对。
  原始 head `777b292` 上的十八项历史原生核对验证了 50/33 生命损失与一次真实的僵尸命中。
  它们覆盖了未变的 500 护甲/出招 102、30% 上限、加性 50%、暂停/到期与完整刷新计数器。
  死亡、复活、换区域与重登在显式的 LocalExile 模型 v1 下清空历史。
  当前主线的混合物理/混沌、充能/抗性/强化/ES 与解析过的抑制回归通过。
  排队的命中在接触时按变化后的来源与最终防御取样，包括惰性的自身光环挂起。
  `c9cd1864` 上的有序联合验证通过 28 项原生控制，最终属性与魔力转换均激活。
  永久护甲 251 保持不变，一次物理 100 命中在正常情况下扣 67 生命，确定翻倍时扣 50。
  一个 raw40 的单接触来源验证上限 30 加通用 20；完整刷新与闲置到期保留通用 20。
  四接触计时以及被强制的强化/光环/狂怒/法师之血截止竞争有自动化覆盖。
  职业准入与「抑制转护甲几率」的集成仍是分开的工作。
  见 [钢志护甲](../docs/game/steel-willed-armour.md)。

- 命中（Accuracy）：玩家的最终值包含固定/增幅被动、武器专属属性与无条件的命中精通效果；
  攻击按 PoB 的命中几率对怪物的 `DefaultMonsterStats.Evasion` 掷骰，未命中不发生命更新，法术永远命中；
  攻击面板拿到 `main_hand_accuracy_rating` / `off_hand_accuracy_rating` 与按角色等级的命中几率行；
  `/accuracy [等级]`。
  精通的两个目标条件效果是施加在掷骰自身目标上的最终倍率（`TargetAccuracy`）：
  对传奇敌人的命中 +40% 更多（`SpawnedMonster.Rarity`），对恒定 20 单位近距（2 米；每米 10 单位）内的敌人
  +50% 更多，两者同时成立时相乘，且从不并入面板数值或 `/accuracy` 的无条件值。
  普通、区域与宝石投射物攻击都套用这些目标条件。
  普通近战/弓现在共用按防御者的闪避熵与一次独立的暴击确认抽取。
  弓的接触包含目标闪避修正/致盲与实时的目标精通条件，保留释放时刻的拥有者输入；被闪避的箭继续飞向后续目标。
  裸的全局/合格本地「不可闪避」标志绕过两道判定。暴击确认失败保留普通命中与普通穿刺存栈。
  被拒绝的近战接触不消耗熵；普通弓的格挡在闪避之后判定。确定性的解析接触、穿透、来源冻结/移除与目标条件
  测试通过。当前原生验证、副手来源/执行与完整的命中/必中发布仍未完成。
  无条件已选的固定/增幅命中现在在最终取整之前并入原来的未取整池基数，更新主手数值与普通/闪避参考几率。
  真实的「额外命中」在 0/20 品质下覆盖连接/移除、属性变化与无关技能。裸命中命令以完整来源替换重建池，
  含精准与显式零；`off` 不重登也能恢复来源。
  宽位带符号求和与精确取整避免中间溢出与分数漂移。区域攻击现在在被接受的接触下消费所选档案，
  含格挡与独立的暴击确认；未命中/被取消的动作没有暴击/穿刺/命中效果。新增 14 个用例覆盖旋风斩、
  重击、真实辅助输入、独立目标、绕过、法术与取消；267 项定向核对通过。
  宝石投射物的前进/穿透/回返接触现在使用同一档案与独立确认。
  未命中会穿过去，不花穿透预算、不触发命中效果，也不为那一轮占用目标。一个共用的前进排除防止同时重复直击。
  新增 24 个用例覆盖真实宝石/辅助、回返缩放、预算、多发共用与排队准入；29 项定向核对通过。
  宝石使用现在在整个接触过程共用暴击抽取。其他条件/更多命中、剩余投射物几何/技能例外与原生核对仍未完成。
  见 [伤害 §2h](../docs/protocol/20-damage.md#2h-ordinary-attack-evasion-and-critical-confirmation)。
  十项原生核对使用连上的普通路径与真实精通选择请求。
  面板保持 711，而目值掷骰对传奇用 995、近距用 1066、两者同时成立时用 1493。
  远处的传奇只保留 995；目标移动、重登与退还恢复了预期条件与基线。
  代码：`CharacterStats.cs`、`TargetAccuracy.cs`、`DamageModel.cs`、`MonsterCatalog.cs`、Gateway。
  文档：[11](../docs/protocol/11-skills.md) §4–5。
- 护甲与闪避：来自天赋树的固定护甲/闪避（`base_physical_damage_reduction_rating`、
  `base_evasion_rating`）以及身体护甲/头盔/手套/鞋子/激活盾牌（掷出的 `ArmourTypes.dat` 基数），加上
  `physical_damage_reduction_rating_+%`、`evasion_rating_+%`、`evasion_and_physical_damage_reduction_rating_+%`
  （布与锁）、`global_defences_+%` 以及那个合并增幅；
  角色面板拿到最终 `physical_damage_reduction_rating` / `evasion_rating`；
  物理 `/hit` 与怪物近战套用 PoB 的护甲减伤（90% 上限），怪物攻击在结算前使用 PoE 的共用闪避熵计数器，
  `/defences` 显示护甲、物理减伤与对一个参考怪物的闪避几率。
  铁之反射把所有闪避转成护甲并去掉敏捷的闪避加成。
  逐件的本地固定/增幅防御词缀、可用的复合词缀与乘法品质都计入。
  额外的本地品质与替代品质的防御禁止被遵守。
  每件物品的结果在全局增幅之前按半进位取整一次；未激活物品不贡献。
  原生核对把造出的背心 56/56 的提示与防御面板上的 56 护甲 / 73 闪避对上。
  品质通货刷新了穿戴中物品的池，本地额外品质只显示一次 30%，卸下则清空属性。
  激活/未激活盾牌核对、原生武器切换与物理命中减伤也通过。
  存下的共用基数掷值在本地修正与品质之前选出各防御的取整原生基数。
  新的护甲物品得到普通掷值；旧存档保留它们先前的最低基数。
  原生基数掷值地标、raw-32 标志处理、本地/品质组合与重登保留都通过。
  混沌免疫（CI）在生命修正之后把最大生命设为 1，并在伤害模型与面板的「免疫」行里授予混沌免疫；
  能量护盾仍作为池。
  用于眩晕/异常状态阈值的 CI 前生命与濒死条件未建模。
  圣化宝珠重掷、本地零 ES 规则、装备 ES 转生命的转换与腐化物品的全局 ES 增幅已实现，
  有自动化覆盖与「物品与制作」里描述的历史原生控制。
  合并分支的全新真实客户端核对仍未完成。
  它们的作用范围见「物品与制作」；其他传奇/条件机制与守护计算仍未完成。
  圣化宝珠通过普通与 Shift 通货使用重掷存下的基数，刷新穿戴物品的池与面板。
  原生的护甲、闪避与 ES 重掷与角色更新一致，同时保留词缀与品质。
  共用的物品防御计算现在包含塔巴与显式的无 ES 物品覆盖。
  ES 转生命转换在全局生命缩放之前使用每件激活物品的最终本地 ES。
  独立的全局 ES 仍然是 ES。
  百分比变体逐件截断，移除全部物品 ES，并让位于完整转换标志。
  回复守卫读的是有效 ES 而不是被抑制或已转换的基数。
  原生完整/50% 转换、生命缩放、独立 ES、盾牌切换与网关重启重载都通过。
  那个本地 69 ES 控制在 100% 时转成 69 生命，50% 时 33 生命，同时保留独立 ES。
  腐化物品的条件 ES 只在那个来源物品被腐化时缩放全局 ES，保留本地提示的 ES。
  盾牌专属增幅只在激活盾牌的本地贡献上并入全局增幅。
  原生 `shield_armour_+%` 属性提升盾牌全部三项防御；按类型增幅仍保持各自范围。
  铁之反射保留两个盾牌类型增幅，并把通用盾牌增幅只计一次。
  最终 AR/EV/ES 池在汇总分数来源（含转换来的闪避）之后按半进位取整。
  盾牌防御排除把盾牌基数从玩家池里去掉，同时保留其他装备与固定来源。
  原生通用/全局增幅、盾牌 ES 被动、IR、排除、转换与盾牌切换都对上。
  一件本地 99/99/99 的盾牌把分数保留到最终合计 168 护甲 / 186 闪避 / 176 ES。
  合成背包与预期池都撑过了网关重启。
  仅限身体的 ES 增幅现在只加进身体的全局增幅桶。身体护甲翻倍也覆盖 IR 转换后的身体闪避；
  无身体护甲标志抑制这些来源，保留普通 EV/ES。
  十二项原生核对覆盖加性缩放、排除、IR、转换、独立 ES 与身体移除/放回。
  翻倍有真实被动回归与原生计算证据。
  那些核对用的登录路径过滤了升华被动，所以那个断开的不破（Unbreakable）客户端夹具无法在游戏内实测它。
  完整的召唤物重定向与依赖虔诚的盾牌缩放仍是分开的机制。
  守护转换、戒指槽与珠宝半径算子仍是分开的工作。
  代码：`CharacterStats.cs`、`DamageModel.cs`、`ItemCatalog.cs`、`ItemDefences.cs`、Gateway。
  文档：[11](../docs/protocol/11-skills.md) §4、[13](../docs/protocol/13-items.md) §2。
- 宝石技能与弓攻击：镶入的宝石施放火球，用它 1 级的数值（9–14 火，750 ms 施法通过
  `virtual_base_spell_cast_time_ms`，5 魔力），打地面或打怪；弓的普通攻击与攻击宝石在攻击动画的
  `attack1_bow` 接触点（攻击时间的 0.374）创建 `Metadata/Projectiles/DefaultArrow`（或宝石自己的投射物）。
  攻击宝石用武器的伤害 × `DamageEffectiveness` 结算，带该攻击的暴击；弓的箭没有火球爆炸。
  火球的命中会爆炸：对其半径（`active_skill_base_area_of_effect_radius`）内的敌人或墙面做一次区域命中，
  排除投射物自己的目标；爆炸美术是一个独立效果对象，投射物在命中处被移除。
  客户端自己绘制的法术投射物（火球、电球）按客户端的接触规则命中：在该行
  max(CollisionRadius, CollisionCandidateRadius) 加上怪物 `ObjectSize − 1` 的切比雪夫格内
  （[11](../docs/protocol/11-skills.md)「客户端的物体接触」）；仅自动化，未实测。
  普通弓的地面/物体攻击现在共用接触时刻的释放，并在实际飞行碰撞处结算伤害
  （#203，自动化与原生碰撞/释放/暂停核对通过）。
  物体使用不再额外结算一次近战接触命中。
  客户端自己生成它自己法术宝石使用的主投射物，一轮里的每一发，都从它站的位置发出；
  服务端模拟它们但不为它们发对象（一个火球被当成了两个，2026-10-29）。它们后续的子体仍由服务端发对象。
  服务端用客户端自己的多发来瞄准：它的扇形、固定扩散与环射
  （[11-skills](../docs/protocol/11-skills.md)「客户端的多发」；静态逆向，扇形按实测电球多发拟合）
  以及它播种的随机方向（静态逆向，2026-10-31 实测：只在画出投射物的方向造成伤害）。
  平行多发仍用服务端自己的扇形。
  普通燃烧箭使用客户端的主箭美术；真实的鼠标输入跟踪暴露了旧报文驱动控制漏掉的重复服务端箭。
  它的服务端碰撞/伤害模拟仍然生效。
  其他攻击宝石在被逐个验证之前保留既有呈现方式。
  电球的动作走同一个客户端方法，所以它的电球也是客户端的；它们活技能的 `skill_effect_duration`，
  宝石技能现在会带这个值（没有它则不绘制电球；静态逆向 2026-10-30，尚未实测）。
  服务端让每个电球按客户端自己的播种游走飞行（[11-skills](../docs/protocol/11-skills.md)「电球的游走」；
  从同一起点出发，三次实测电球里服务端的每一段都逐格与客户端一致，扇形也在几度之内吻合）。
  每次施放的一组电球每 0.66 秒忘记它打过的敌人（wiki Spark），所以一次施放可以再次打到同一个目标。
  电球的蜕变版与瓦尔版发出 Spark 自己的 Projectiles.dat 行，遵循相同规则；在此之前它们在服务端没有投射物。
  仍分开的：起点格用的是服务端的行走位置，常与客户端差几格，所以路径平行但有偏移，
  实测里偶尔仍会看到画出的电球命中却没伤害。
  GGG 的服务端对普通弓的箭也不发对象，所以实际客户端自己画（2026-10-29 实测抓包，
  [移植记录](../docs/dev/ranged-monster-actions-port.md)「实测参考」）。
  龙卷射击发出 `Metadata/Projectiles/TornadoShotArrow`（`SkillCatalog.ProjectileFallbacks`；
  它的行不是按效果命名的，所以之前每次使用都被拒、客户端停下而服务端继续走，2026-10-29）；
  它只实现了基础直线飞行：从目标点发出的次级箭尚未建模。
  灵体投掷发出 `Metadata/Projectiles/ThrownWeapon`，穿透每个敌人并在回返时再次命中，每次命中带血；
  客户端绘制武器，服务端飞同样的减速、停顿与回返路径。部分实现：
  命中时机仍需调整。回返以恒定平均速度飞，而客户端是加速的，它的命中在朝投掷者走来的怪物上偏差最大。
  释放只对持单手剑、锤或斧加盾的女巫与圣堂武士跟随近战连段；服务端假设每次投掷都交替连段，
  客户端何时重新开始则未知。墙会挡住武器且不回返。蜕变版与瓦尔版未映射。
  滚烫岩浆发出 `Metadata/Projectiles/MagmaOrbShort`（同一个兜底；之前每次施放都被拒），落在瞄准点，
  爆炸并按每段连锁笔直弹跳一次，每次命中都是一次区域命中（2026-11-01 实测：每次命中都掉血，
  1 级 3 次命中）。基础半径 14 与两次命中之间 16 格不是从客户端读的；
  它更慢的弹跳（每次 375 ms）意味着一次弹跳的命中会早约 0.1 s 落地。
  分裂箭发出 `Metadata/Projectiles/SplitArrowDefault`（`SkillCatalog.ProjectileFallbacks`；
  它的行带 Default 后缀，所以之前每次使用都被拒）。
  闪电箭的命中会对 18 格半径内最多 3 个附近敌人触发次级打击
  （`ContactTargets`、`lightning_arrow_maximum_number_of_extra_targets`），随区域效果修正缩放。
  投射物生成在捕获的地形高度 + 释放剪辑的关键帧高度（弓 16 步，法杖 12，沙喷怪的喷吐 9；
  无骨架数据时 `Game_PROJECTILE_HEIGHT` 为 5 步）上，只在自然地面上；没有捕获则用裸偏移。
  攻击宝石打地面时，若目标点在武器距离内则挥击（并付魔力），超出则只走位（攻击移动，不付魔力）；
  两次使用客户端自己都会扣魔力，所以网关对一次攻击移动回以权威魔力值。
  代码：`SkillCatalog.cs`、Gateway（`CastOn`、`LaunchProjectile`、`AreaSkillHit`）、`ProjectileObject`。
  文档：[11](../docs/protocol/11-skills.md) §9–10。
- 抗性：被动 `base_resist_all_elements_%` / 各元素 `base_*_damage_resistance_%` 从 0% 基数相加，
  两项奇塔瓦任务惩罚（各 -30%）按任务标记镜像，最终值使用 Path of Building 的下限 -200 / 默认上限 75 /
  绝对上限 90（`DamageModel`）；
  正义之火的自我燃烧与 `/hit` 使用它；`/resistances` 显示修正、惩罚与最终值。
  代码：`CharacterStats.cs`、`DamageModel.cs`、Gateway。
  文档：[11](../docs/protocol/11-skills.md) §12。
- 法术抑制（Spell Suppression）：被动 `base_spell_suppression_chance_%` 上限 100%，成功抑制时法术伤害
  减少 40% 再加 `base_spell_damage_%_suppressed`（PoB `SuppressionChanceCap`/`SuppressionEffect`）；
  `spell_suppression_chance_is_lucky` 掷两次取较好的一次（有效 `1-(1-p)^2`），开发模式下有不幸的 `p^2`；
  条件概率被动在其条件成立时计入：
  `spell_suppression_chance_%_while_on_full_life`、`_while_moving`（走位估计）、`_if_enemy_hit_recently`
  （最近 4 s 内有攻击或法术命中），以及按有效 ES 池的 `_while_on_full_energy_shield`。
  满 ES 要求玩家活着、容量为正且当前 ES 整数上等于最大值；零 ES 不算。
  裸基数与已激活条件在一次 0..100 钳制之前相加，面板与法术命中掷骰共用。
  怪物法术在护甲与固定减伤之后套用抑制。
  十六项原生控制检查了伤害/回充、真实充能、上限、转换、独立 ES、无 ES/零 ES 来源与独立的满生命/满 ES 条件。
  那次保证命中的首个法术对 118 ES 造成 60 伤害，剩 58 ES，并把下一次命中的抑制从 100% 变为 90%。
  检查点复活会显式发布恢复后的满池条件，即使玩家 tick 在死亡前已缓存了同一个概率。
  加密线回归在修复前失败、修复后通过；原生清晰/死亡/检查点控制确认 100% -> 90% -> 100%，
  无需另一次装备或光环刷新。
  `/suppression`、`/suppression_luck`、`/spellhit` 暴露共用结果。
  未实现：条件抑制效果属性与怪物法术命中。
  条件变化后 250 ms 内角色面板再次拿到该概率（玩家 tick）。
  代码：`CharacterStats.cs`、`DamageModel.cs`、Gateway。
  文档：[11](../docs/protocol/11-skills.md) §12。
- 移动速度：被动 `base_movement_velocity_+%` 汇总进 `Pools`，作为客户端的 `movement_velocity_+%` 下发，
  并用于服务端的行走副本（37 × (1 + pct / 100) 格/秒）；
  登录、升级、被动与换区域时应用；`/set_speed` 在其上加百分比。
  裸来源与开发覆盖的 `action_speed_-%` 与冰缓/冻结做加性组合，然后一次性乘进移动、攻击、施法与眩晕恢复节奏。
  新动作限每帧一个（30/s，`TickSeconds`）；引导重复绕过此上限。条件/更多倍率、装备/buff 移动、
  尾风/先驱者/时空锁链来源、陷阱/地雷/战吼/图腾计时、怪物的下限与 128 上限仍未做（#246）。
  代码：`Player/CharacterStats.cs`、`Combat/PlayerAilments.cs`（`PlayerActionFactor`）、Gateway（`ApplyMovementSpeed`）。
  文档：[07](../docs/protocol/07-latency-visibility-movement.md) §3。
- 格挡 / 法术格挡：被动的 `additional_block_%` 与 `base_spell_block_%` 汇总并上限 75% / 90%（PoB
  `BlockChanceCap`），最大抗性被动 `maximum_block_%` / `base_maximum_spell_block_%` 提升上限；
  成功格挡免掉该次命中，怪物近战与 `/blockhit` 都使用它；`/block` 显示两个概率；
  装备盾牌会加上它的 `ShieldTypes.Block` 基数并启用盾牌专属被动，长杖被动在持长杖时生效，双持加上其固有的
  20% 攻击格挡与双持格挡/法术格挡被动；
  多面手（Versatile Combatant）把两个上限各降 10%，并把溢出的攻击格挡按 1% 转 2% 法术格挡；
  幸运格挡对攻击/法术用 1-(1-p)^2（开发 `/block_luck`）；
  角色面板拿到 `uncapped_attack_block_%` 与 `display_spell_block_chance`，从而在括号里显示未封顶值，
  并显示法术格挡行。
  代码：`CharacterStats.cs`、`DamageModel.cs`、Gateway。
  文档：[11](../docs/protocol/11-skills.md) §12。
- 耐力、狂怒与暴击球：来自击杀（含被冻结的敌人）、命中、暴击、非暴击、受击与格挡概率属性，
  按时触发的那些（每秒、静止或移动时、最近被击中时）以及技能（狂怒、能量虹吸、闪打、坚毅战吼）；
  持续 10 s 加持续时间修正，一次获得刷新整个类型，全部一起到期，最低充能始终保有，死亡时失去，
  换区域时带着剩余时间带走。
  Character.ot 的每球加成与服务器计算时用到的每个「每球」修正都作用于池、命中、承受伤害、行走速度与下发的属性；
  充能页签拿到 `current_*_charges` 与每球行。
  HUD 每种一个图标显示层数（0x1c1 flags bit2）与剩余时间（bit1），以及球体。
  2026-10-25 实测：受击、非暴击命中与击杀时获得，刷新、到期、死亡、换区域、最低充能，
  3 个暴击球把攻击暴击从 8.3% 抬到 20.75%，一个狂怒球缩短攻击，3 个耐力球把物理与火命中减 12%；
  城镇里不画充能图标。
  复制品脆弱、复制品和平主义与复制品无力（「每秒失去…充能」，属性存为 `-1`）每秒掉一层到最低：
  以前负层数被跳过，所以什么也没发生。
  在此之前，最大值行（含最大充能修正）已在原生充能页签上按 `0 / 3` 验证过；
  现在受控的原生数量 0 与 3 与保有状态一致。坚毅战吼与按时获得未实测。
  三个原生镜像修正现在从已完成的耐力/狂怒/暴击最大值发布残暴/折磨/吸收上限，
  含最终标准/特殊零覆盖与独立来源清空。真实的 4/5/6 原生修正夹具覆盖两种模式；
  组合后的自动化结果与下面的面板改动一起记录。
  这里用的是标注过的固定 PoB 3.29.x 公式与原生来源标识，并未认证官方每一个 getter。
  转换充能（2026-10-29）：在三个「获得…而不是」传奇标志下，持有的耐力/狂怒/暴击球就是残暴/折磨/吸收球：
  特殊的 `current_*` 与 `minimum_*` 行与 BuffDefinitions 1757–1759 的 buff，标准效果关闭，
  残暴的每球三倍伤害几率与眩晕阈值生效，折磨的更多异常伤害出现在异常面板行
  （`ConvertedChargesGatewayTests`、`ChargeEffectsTests`、`ChargeSessionTests`、`ChargeBuffsTests`；
  2026-10-29 实测，用 `/set_stat` 设标志：残暴行、被清零的耐力行与换掉的 buff）。
  收割的血充能（2026-10-29）：一次命中留下敌人存活给一层，击杀拿一层，上限为 Character.ot 的 5 加附魔；
  持有的层数在命中与攻击行上给每层 15% 更多伤害、消耗 20% 更多，含充能页签的行、
  `show_blood_charge_info` 闸与 BuffDefinitions 1822；无持续时间（表里没有），不跨区域携带
  （`BloodChargesTests`、`BloodChargesGatewayTests`；2026-10-29 实测：行、获得、失去与 buff，镶有收割）。
  精魂、虹吸、闪击与挑战者在这里没有可达的玩家来源（深渊珠宝 / 长老-塑界计数，3.29 里没有），
  折磨的非伤害效果与吸收的补偿没有消费方。
  代码：`Charges.cs`、`ChargeGains.cs`、`ChargeEffects.cs`、`ChargeSession.cs`、`ChargeBuffs.cs`、
  `Program.Charges.cs`、`ChargePanelStats.cs`、`BloodCharges.cs`、`Program.BloodCharges.cs`。
  文档：[11](../docs/protocol/11-skills.md) §15。
- 角色面板汇总：约 150 项「按基础形式相加」的面板统计由被动、物品与光环及其无条件合并值算出：
  眩晕、异常规避、持续与效果（点燃、冰缓、冻结、感电、灼烧、脆弱、枯竭）、异常概率、穿透、汲取、
  击杀回生命/魔力/ES、稀有度与数量、冷却、投射物速度、区域、诅咒效果与药剂持续、反射、充能持续、
  不可/免疫标志（含四种伤害免疫与物理免疫，来自它们的基础形式）；
  主手/副手行加上该武器的本地属性；
  投射物格挡与对持续伤害的混沌抗性把池与对应属性合并；
  防御页签的每秒生命/魔力/能量护盾回复行是 tick 自己的每分钟速率
  （`total_*_recovery_per_minute_from_regeneration`）。
  引入的 #75 条目的证据范围：更早的测试计数与标准/无情原生矩阵描述的是旧的 LocalExile 修订，
  不是本分支的全新原生跑测。私有游戏模式机制被排除；当前自动化结果与剩余核对见
  [移植记录](../docs/dev/issue75-port.md)。
  六个原生 Character.ot 默认值现在各进入原始来源一次：烙印上限 3、残暴三倍伤害/眩晕系数 3/10、
  折磨伤害/非伤害系数 8/8、吸收补偿系数 12。
  真实的烙印物品 +1 与烙印公平 +2 给 actor 6；兼容的已连接污抓给出 selected 8。
  烙印闸只在已选烙印上跟随原生技能类型 64；显式替换/零与移除在两种模式下都保留其来源优先级。
  特殊值仍是系数；烙印附着/上限强制执行、特殊充能玩法与补偿执行未加入；新的原生核对仍待做。
  已选的区域、物品稀有度与召唤物增幅别名现在一次性合并裸 actor 与合格的自身来源，
  保留带符号抵消/零与最终开发替换优先级。初始 `SendPlayer` 与换区域的对象创建与后续 `PlayerSkills`
  刷新共用那份快照；旋风的半径输入在已选覆盖层之前算好，保持既有结果。
  真实辅助覆盖区域 20+30=50、召唤物 15+品质 10=25 与普通物品稀有度 10+40=50。
  已选的火/冰/闪电穿透现在也包含已连接辅助的裸贡献。
  每个真实的一级辅助给 actor 10 加 20，显示 30 而不是先前错误的 10。
  九项新的网关回归覆盖连接/来源移除、抵消、覆盖、需求与技能隔离；
  通用元素穿透在面板与命中消费方都组合为 35。
  这些在本分支的 29 项定向核对内通过；全新原生验证仍待做。
  两个攻击手穿透行都包含按类型/元素仅攻击来源与合格的已连接辅助。
  裸本地手快照在重算已选技能时保留每把激活武器的贡献。
  另有十二个自动化用例覆盖三种元素辅助、混沌/仅攻击求和、独立本地修正、未激活武器、来源移除与连接变化；
  原生显示核对仍待做。
  穿透消费方现在也包含非基数通用元素/混沌来源与正在攻击的武器的本地修正；法术排除本地/仅攻击输入。
  解析的接触用例覆盖普通近战/弓、旋风、燃烧箭与火球，含/不含元素禁止。已选与 actor 投影在禁止下对全部
  九个元素别名发零，移除则恢复合计；混沌仍分开。
  真实元素穿透辅助在一级贡献 14，与命中消费方一致。
  条件穿透：「持武器伤害穿透」对持武器的攻击计入（命中与持武器那只手的手行），对冰缓敌人的冰穿透在命中时
  针对被冰缓的怪物，以及守望者之眼的愤怒、憎恨与狂怒形态在该光环运行时（`InflictedAilmentTests`）。
  当前分支的原生验证、副手攻击选择与其他条件形式（[20](../docs/protocol/20-damage.md) §2）仍未完成。
  已选的异常持续/效果系数现在也包含合格辅助：真实一级「无束异常」为全部七个持续与五个非伤害效果别名
  给角色 10 加 35。它 10% 更多异常伤害仍分开。
  二十四个回归覆盖被复现的遗漏、连接/来源移除、带符号抵消、显式替换与无关/默认技能隔离。
  原生呈现与实际异常施加仍未做；这不代表异常系统完成。
  历史上的 LocalExile 无情辅助排除不在本分支。连接/来源移除、不兼容或不合格辅助、
  无关/默认技能与重复构建都有覆盖；召唤物战斗与物品掉落执行仍分开。
  新增基数暴击/默认镜像/已选别名用例通过 52/19/22 项新测试，另有 95 项既有控制：
  合并后的定向批次 188/188 通过且无跳过。全新的网关 3803/Core 648/Data 37 全部不跳过通过，
  即 4488 个刚复跑的用例。当前钩子记录到六次真实未观察到的已释放 NetworkStream 任务故障，
  全部是 `MonsterKilled` 经 `Program.Send`；385 条首次机会记录与那六次故障分开。
  捕获到的故障里没有出现诊断错误/截断/命中上限，也没有新的算子路径。
  测试通过不等于后台执行无故障。
  原生默认/镜像与已选别名核对仍待做；下面的暴击条目记录它有限的那些控制。
  更早的 G14/完整 3710 与下方原生观察不验证这些新算子，也不认证全部 387 行。
  能量护盾那一行排除充能，并在来源被移除或回复被禁止时清零。
  原生核对验证了该行在每秒 2 点、修正合计、零清零与重登。
  充能期间它的合计停在每分钟 60，而原生回复预测从 60 变到 340。
  狂怒攻击/施法速度系数包含合并的每球修正。原生 Mods 第 7236 行的 -4 对两者都抵消固有的 4；
  持有的充能仍把裸来源展开一次。十七项定向测试（15 项新增）覆盖两种模式下的来源移除与显式零。
  八项原生供给来源控制一致，含 +4%/+6%/仅攻击 +5% 的格式、零发布、充能清空与重登。
  零值行在视觉上被抑制；缺少格式化回调不能证明零渲染或装备获取。
  点燃/冰缓/冻结/感电的持续时间减少也会一次性减去裸 `base_all_ailment_duration_on_self_+%`。
  真实的「严酷教训」在两种模式下给 -20，产生 +20 的减少；按类型与元素来源仍加性，
  带符号增幅仍是负的减少，最终开发零/覆盖保持优先级。
  全部 14 个新的纯/Gateway 用例在合并的 53/53 定向批次中通过；完整网关 3710/3710 无跳过通过。
  未变的先前 Core 648/Data 37 结果仍适用（合计 4395，并非全部重新跑过）。
  仍记录着五次真实未观察到的已释放流故障（四次 MonsterKilled、一次投射物命中路径），
  没有捕获到对新算子的引用。不主张新的原生证据。戒指槽选择、
  其他异常族、条件 ES 延迟与实际异常执行仍分开。
  格挡恢复现在从同一份快照一次性加上基数与裸的直接眩晕恢复，含已展开的每球恢复。
  击杀回生命/魔力/ES 的数值与药剂持续系数包含它们真实的每角色等级输入，使用实际角色等级与宽位运算。
  三十个新的面板/来源用例覆盖两种模式。
  前面的面板/药剂批次通过 55 项新增加 80 项既有定向用例与网关 3657/3657；
  未变的先前 Core 648/Data 37 结果仍适用（合计 4342）。合并后的原生评估有
  40 个已完成控制：22 个完整 PASS（12 个正来源用例、六个功能使用与四个生命使用），
  18 个完整零值控制 UNPROVED。实际 10/20/100 级上的正来源用例在两种模式下六项字段全对。
  六个面板字段有 138 个 VERIFIED/42 个 UNPROVED 判定，含保留与过渡证据；
  防御的零值字段在任何零值控制里都未被证明，部分杂项零值也仍是 UNPROVED。
  隐藏的行不被算作渲染出的零。完整的击杀回复/百分比/禁止算子与格挡动画时机仍未做；
  精确的运行时数值与原来那个单整数生命边界见协议 13 §8。
  那些历史原生核对早于新的自持续与药剂效果算子。随后一批八控制实测验证了两种模式下实际 10/100 级的
  正每级药剂效果：生命速率 700/1960 持续三秒，以及功能药剂移动 +20% / 护甲 +2100，配原生标准/无情持续。
  功能药剂的力量在显式最终角色属性上验证，与它的计时 buff 分开。全部库存/充能与原始数据都保留，
  两种模式的代表性重登通过，常规服务已恢复。保留的验证器修订与单整数生命边界记录在协议 13 §8。
  这些控制不提升旧的 18 个整零用例，也不确立来源移除/零的下次使用行为、整件传奇的获取/提示一致性、
  新的自持续原生覆盖，或 #75 某行的完成。
  2026-10-29 的行批次发布了没有推导的那些行：防御估计（物理减伤普通 / 对投射物 / 对有差异标志的命中，
  闪避几率，守护延迟，魔力充能，全知需求）按角色等级对 DefaultMonsterStats；副手自己的命中、命中几率、
  攻击速度、暴击几率/倍率与伤害范围，并带 `skill_uses_off_hand` 用于双持的默认与宝石攻击，
  它们的每次使用 / DPS / 每秒攻击行取双手平均（`DualWieldPanel`）；法术的有效/双倍暴击几率
  （先幸运，再刺客分裂）；带标志法术与熊陷阱 / 冰之传令的次级伤害行，来自它们各自的次级基础伤害
  （仅通用桶，`Program.SecondaryPanel.cs`）；`skill_show_average_damage_instead_of_dps`；
  每只手、法术与次级上下文的 32 个流血/点燃/中毒每秒伤害行（`AilmentPanelStats`，
  来自命中的裸报文与转换，配异常桶与 DoT 倍率）；按类型眩晕标志、烙印每目标上限、精魂上限与虹吸的
  承受伤害系数。60 个新自动化用例通过，用真实剑/匕首、旋风与火球夹具；
  拥有者 2026-10-29 的实测（基线 `7fd944be`）通过了该批次的测试计划（副手与清空、异常行、
  有效暴击、次级块、`/set_stat` 下的防御估计）。实际异常施加与长老/塑界及深渊珠宝物品计数
  （精魂、虹吸；推迟到 #60 之后）仍未做
  （[复核](../docs/dev/issue75-review.md#offence-defence-misc-and-charges-row-batch)）。
  代码：`PanelStats.cs`、`PanelStatDefaults.cs`、`AilmentPanelStats.cs`、`Program.OffHandPanel.cs`、`DualWieldPanel.cs`、
  Gateway（`CharacterPlayerStats`、`HandLocalStats`、`GemSkills`）。
  文档：[11](../docs/protocol/11-skills.md) §14。
- 角色面板汲取上限：完整最大池提供各自独立的每实例数值与总回复上限。
  当前的 #75 复核还修正了 24 个汲取系数与三个回复速度别名：通用、
  按类型/元素、攻击/法术与合格的双手来源，缺失的物理/混沌 ES，以及真实的已选辅助。
  actor/辅助的分数输入在单位换算之前组合。新组合有自动化控制；
  原生 getter/格式一致性仍未做（伤害 §5b、技能 §14）。
  六个绝对值行及其百分比伴随行包含受支持的增幅/减幅与禁止标志。
  原生控制验证显示换算、濒死/治疗、真实物品池转换、保留与重登。
  周期性属性刷新使用同一份一致的开发快照，另有四个已观察任务的回归。
  分数端点截断是文档化的本地投影；官服取整未测量。
  普通被接受的近战/弓/投射物/区域命中现在从裸合格来源与减伤后直接伤害创建独立的生命/魔力/ES 实例。
  它们的持续时间、总回复、上限与通用回复倍率各不相同。瞬发部分在命中事务内回复；既有的资源写入方
  整合按时部分与精确的到期边界。满池移除、无条件超汲取、禁止、暂停/死亡/转移清理与真实辅助接触都有
  自动化控制。幽灵掠夺者（Ghost Reaver）现在创建独立的转换/原生 ES 实例，把总 ES 上限翻倍并禁用充能；
  实际节点来源与解析退还都有控制。普通转换不会加上那些基石副作用。其他转换/条件、技能专属的
  回复速度归属与当前原生回复/预测一致性仍未做。汲取中/满 ES 标志现在反映实际实例/池，
  并在普通 tick 上刷新玩家/已选属性。通用/资源/攻击/物理汲取的命中伤害与能量汲取的满 ES 更多/品质增幅
  有各自的桶与实际辅助/接触控制；速度/防御/异常/DoT 消费方仍未做。
  仅攻击的 ES 超汲取在满 ES 时保留攻击实例（含幽灵掠夺者转换）而移除法术实例，保留原到期与一个共用上限。
  实际词缀/接触与来源移除控制通过。普通超汲取的生命投影现在尊重显式零替换；其他来源仍未做。
  爪精通的瞬发汲取统计可用爪的数量，武器大师只应用一次它的爪条件，
  血舞者（Gore Dancer）的生命瞬发来源在幽灵掠夺者转换后仍存活。真实来源接触与分裂/覆盖控制通过；
  精确的原生混合生命/ES 瞬发组合仍未验证（伤害 §5b 记录该策略）。
  击杀回复生命、魔力与 ES，异常状态击杀也算；未实测（[20](../docs/protocol/20-damage.md) §5b）。
  拦路强盗（Highwayman）的真实修正现在一次性把总生命汲取回复翻倍，独立于显式的生命上限速率翻倍标志。
  来源/移除、重复标志、上限、持续、瞬发与转换控制通过；
  当前瓦尔契约的获取与原生叠层/回复一致性不由裸标志支持推论。
  命中获得攻击回复生命、魔力与 ES；未实测（[20](../docs/protocol/20-damage.md) §5b）。
  代码：`LeechLimits.cs`、`LeechSources.cs`、`LeechRecoveryState.cs`、`LeechSession.cs`、Gateway（`Program.Leech`、资源写入方）。
  文档：[20](../docs/protocol/20-damage.md) §5b。
- 普通生命/魔力回复：固定与按最大池百分比的来源接受回复与恢复增幅。
  对应的禁止会停止回复，面板/原生预测输入共用实际的资源写入方。
  十项原生核对验证固定来源、独立增幅、禁止、清空与重登。
  控制把每分钟 1080 生命对上每秒 18，把每分钟 3339 魔力对上约每秒 55.65 的回复。
  强化的精确到期整合使用同样的因子，并有自动化的修正/禁止覆盖。
  狂信者之誓（`keystone_zealots_oath`，以及 Geofri's Sanctuary 的
  `regenerate_energy_shield_instead_of_life`）把生命回复，含强化、龟壳与既有的坚毅战吼回复路径，
  移到能量护盾；面板与资源写入方共用这次拆分，ES 回复禁止会去掉被转换的部分。在 Qblolz 上实测，
  用 `/set_stat` 切换 Geofri 的属性：关时每分钟 9974 生命与 6697 ES；开时 0 生命与每分钟 16672 ES。
  条件药剂/物品来源与其他回复转换仍是分开的工作。
  见 [20](../docs/protocol/20-damage.md)「连续资源更新」。
- 坚定与优雅在角色级防御计算之前截断它们缩放后的固定/更多光环输入。
  标准与无情的原生数据与品质隔离有回归覆盖。
  玄武岩/辉锑矿在最终值之前组合更多因子而不对合并百分比取整。
  原生普通光环/药剂、全局增幅、IR 与清空核对与共用池一致。
  文档：[11](../docs/protocol/11-skills.md) §13。
- 纪律把按等级的固定 ES 供给共用池、面板、吸收与充能路径。
  智力与全局 ES 增幅只应用一次；仅盾牌增幅与护甲 ES 转生命不作用于该光环来源。
  品质只影响区域效果；光环效果缩放用固定 PoB 截断整数 ES/充能属性。
  原生激活、清空、不瞬时回满的容量、吸收与延迟充能都对上。
  原生 20 级、品质隔离、全局缩放、转换与盾牌来源控制也对上。
  该光环的城镇抑制/恢复、宝石宿主移除/恢复与死亡清空都核对过。
  1 级与 20 级的两个夹具在网关重启后都恢复了预期 ES 与完全相同的存档背包。
  代码：`AuraCatalog.cs`、Gateway（`PoolsFor`、`BaseStats`）。
  文档：[11](../docs/protocol/11-skills.md) §13、[20](../docs/protocol/20-damage.md)「玩家能量护盾」。
- Buff（S→C 0x1c1 添加、0x1c2 移除）与正义之火：对自己施放，它的 buff 与大燃烧圈（一个效果对象，
  `Metadata/Effects/PermanentServerEffect` 配 `rifire.ao`，附着在玩家身上），玩家每秒 90% 生命的非致死燃烧，
  半径内（18 格，按玩家区域效果属性增长，`SkillArea`）怪物每秒 70% 生命（会烧死），
  圈按该半径缩放（MiscAnimated 的参考半径，统一 Positioned 缩放；1 级与真实游戏一致），
  任何延迟下都由客户端承载且不随玩家转动；服务端侧魔力（消耗、回复）；`/god`（另加 200% 区域效果增幅）、`/set_aoe`。
  代码：Gateway（`ActivateRighteousFire`、`RunPlayerTick`）、`SkillArea.cs`。
  文档：[11](../docs/protocol/11-skills.md) §12。
- 战吼准入限制（#297）：任何带符号非零的 `your_warcry_skills_are_disabled` 都会拦下新的按类型战吼。
  实际施法者的被动/装备/裸开发来源在技能覆盖、动作回显、消耗与冷却之前被读取。
  显式零与来源移除恢复准入，无需重建池或移除 Actor 技能。
  解析的地面/物体、付费坚毅战吼、来源、瞬发安全与排队发送回归通过。
  被接受的不可取消战吼释放保留既有行为；坚毅战吼的充能执行器仍是部分实现。
  七项原生核对验证拒绝时不掉魔力、不释放，来源清空与付费 13 魔力的重试。
  原生耐力充能确认被接受的释放；火球仍可用，重登清掉开发标志。
  Phrecia 玩法仍受限；来源优先级、别名与排队边界有额外的自动化覆盖。
  见 [11](../docs/protocol/11-skills.md) 第 10b 节。
- 自身光环限制（#304）消费带符号的 21148 或 11881，并有显式最终 11075 的开发优先级。
  按类型的「光环」与被接受的亵渎元数据驱动准入与原生技能标志。
  准入与排队确认使用请求的技能 ID；重复宝石保持各自的辅助上下文。
  记住的候选会检查它们自己的需求，所以一个不可用的副本无法压掉另一个可用的宝石。
  本地策略在保留意图的同时挂起自身拥有的成对光环、加成与保留，且不回满魔力。
  解除限制时一次性恢复可用的已记住普通自身光环，配新的 buff ID 与一次魔力钳制。
  RF、药剂、强化与受得效果在此拥有的生命周期之外；不主张任何队伍光环执行器。
  来源、真实派发、回环、排队快照/添加、初始创建与进入重放回归通过。
  狂怒刷新在快照之前调和成对移除与保留，即使狂怒未变。
  十五项原生控制验证清晰/迅捷成对、真实加成/魔力、三条被拒路径与普通火球。
  重登在瞬时来源清除后恢复记住的迅捷；挂起期间死亡会清掉存档意图。
  原生灰图标预测与持续性限制装备仍未验证。
  挂起/恢复生命周期是显式的 LocalExile 策略，不是已证实的 GGG 行为；Phrecia 玩法仍受限。
  见 [11](../docs/protocol/11-skills.md) 第 10c 节。
- **法术技能不造成伤害（#293）：** 裸施法者键 5341 抑制受支持法术的命中报文与 RF 的敌人燃烧。
  物理、元素与混沌法术伤害、受支持的投射物与区域共用最终报文限制。
  已选的原生记录范围、平均伤害与 DPS 使用那份报文，含继承的主手零行。
  施法、消耗、时机、视觉、零命中回调、攻击与光环授予的攻击加成保留各自范围。
  RF 的自燃独立继续；来源变化按既有命中/tick 解析，不重建池。
  自动化的受支持构造函数、派发/生命、飞行中变化、光环控制与 RF tick 核对通过。
  当前主线控制覆盖狂怒/充能接触、品质、魔力来源、法师之血、混沌护甲
  与时装标识。
  未付费释放守卫、功能保留/冷却与 RF 分数携带保持既有行为。
  原始 head `e80a81c` 上的十一项历史原生核对覆盖火球/冰霜新星、付费施法与攻击。
  原生火球动作行清空范围与 DPS；RF 保留自燃而敌人伤害停止/恢复。
  那些控制覆盖历史 head 上的来源变化与重登。
  `c9cd1864` 上的全新有序联合控制验证火球 200 / 零、攻击 100 与恢复后的火球 200。
  最终属性与转换后的池在同一次未升华野蛮人会话中仍生效。
  狂暴者 49937 提供了这个来源，但职业准入、拥有者执行与该节点的其他机制是分开的。
  代码：`SkillDamageContext`、`SupportedHit`、`SpellHit`、`BurnAround`、`GemSkills`。
  文档：[20](../docs/protocol/20-damage.md) 第 2g 节。
- 法术在施法动画的接触关键帧释放（女巫 1066 ms 里的 599 ms；
  每个职业的来自它的骨架 .amd），服务端持续时间按施法速度调整一次。
  代码：`AnimationContacts.cs`、`scripts/re/anim_contacts.py`。
  文档：[11](../docs/protocol/11-skills.md) §9。
- 对敌人的骄纵：它的 buff（第 1331 行）覆盖光环内每只怪物，且不打断客户端的流。
  代码：`EnemyDebuffs.Rules.cs`。文档：[11](../docs/protocol/11-skills.md)（玩家施加在怪物身上的减益）。
- Accuracy: the player's final rating includes flat/increased passives, weapon-specific stats and the unconditional
  Accuracy Mastery effects; attacks roll PoB's hit chance against the monster's `DefaultMonsterStats.Evasion`, misses
  send no life update, spells always hit; the Offence panel gets `main_hand_accuracy_rating` /
  `off_hand_accuracy_rating` and the Chance to Hit rows at the character's level; `/accuracy [level]`.
  The Mastery's two target-conditional effects are final multipliers applied to the roll's own target
  (`TargetAccuracy`): 40% more accuracy against Unique enemies (`SpawnedMonster.Rarity`) and 50% more against enemies
  in the constant 20-unit close range (2 metres; 10 units per metre), multiplying when both hold and never folded into
  the panel's rating or `/accuracy`'s unconditional rating.
  Ordinary, area and gem projectile attacks apply these target conditions.
  Ordinary melee/bow now share per-defender evasion entropy and a separate critical-confirmation draw.
  Bow contacts include target evasion modifiers/Blind and live target mastery conditions, retaining release-time
  owner inputs; an evaded arrow continues to later targets. Raw global/qualified local cannot-evade flags bypass
  both checks. Failed critical confirmation keeps a normal hit and normal impale storage. Rejected melee contacts
  do not consume entropy; ordinary bow block is checked after evasion. Deterministic parsed-contact, pass-through,
  source-freeze/removal and target-condition tests pass. Current native validation,
  off-hand sources/execution and complete accuracy/always-hit publication remain open.
  Unconditional selected flat/increased accuracy now joins the original unrounded pool base before the final
  floor, updating main-hand rating and normal/evasive reference chances. Real Additional Accuracy at 0/20 quality
  covers link/removal, attribute changes and unrelated skills. Raw accuracy commands rebuild pools with complete
  source replacement, including Precision and explicit zero; `off` restores sources without relog.
  Wide signed sums and exact flooring prevent intermediate overflow and fractional drift. Area attacks now
  consume the selected profile under admitted contact, including block and independent critical confirmation;
  misses/cancelled actions have no critical/impale/on-hit side effects. Fourteen added cases cover Cyclone,
  Heavy Strike, real support inputs, independent targets, bypass, spells and cancellation; 267 focused checks pass.
  Gem projectile forward/piercing/return contacts now use the same profile and independent confirmation.
  Misses pass through without spending pierce budget, triggering impacts/on-hit effects or reserving the
  target for the volley. One shared forward exclusion prevents repeated simultaneous direct hits.
  Twenty-four added cases cover actual gems/supports, return scaling, budgets, volley sharing and queued admission;
  29 focused checks pass. Gem uses now share critical rolls across their contacts. Other conditional/more accuracy, remaining projectile
  geometry/skill exceptions and native checks remain open.
  See [damage §2h](../docs/protocol/20-damage.md#2h-ordinary-attack-evasion-and-critical-confirmation).
  Ten native checks used connected ordinary paths and actual mastery selection requests.
  The panel stayed at 711 while target rolls used 995 against Unique, 1066 in close range and 1493 when both held.
  A far Unique kept only 995; target movement, relog and refund restored the expected conditions and baseline.
  Code: `CharacterStats.cs`, `TargetAccuracy.cs`, `DamageModel.cs`, `MonsterCatalog.cs`, Gateway.
  Docs: [11](../docs/protocol/11-skills.md) §4–5.
- Armour and Evasion: flat armour/evasion from the passive tree (`base_physical_damage_reduction_rating`,
  `base_evasion_rating`) and body armour/helmet/gloves/boots/active shield (the rolled `ArmourTypes.dat` bases), with
  `physical_damage_reduction_rating_+%`, `evasion_rating_+%`, `evasion_and_physical_damage_reduction_rating_+%` (Cloth
  and Chain), `global_defences_+%` and the combined increase;
  the character panel gets the final `physical_damage_reduction_rating` / `evasion_rating`;
  physical `/hit` and monster melee apply PoB's armour reduction (90% cap), monster attacks use PoE's shared evasion
  entropy counter before damage, and `/defences` shows armour, physical reduction and the chance to evade a reference
  monster.
  Iron Reflexes converts all evasion to armour and drops dexterity's evasion bonus.
  Item-local flat and increased defence mods, applicable hybrid mods and multiplicative quality are included per item.
  Additional local quality and the alternate-quality defence prohibition are respected.
  Each item's result rounds halfway up once before global increases; inactive items contribute nothing.
  Native checks matched the crafted vest's 56/56 tooltip to 56 armour / 73 evasion on the Defence panel.
  Quality currency refreshed worn-item pools, local additional quality showed 30% once, and unequipping cleared stats.
  Active/inactive shield checks, native weapon swap and physical-hit mitigation also passed.
  The saved shared base roll selects each defence's rounded native base before local modifiers and quality.
  New armour items get ordinary rolls; legacy saves retain their previous minimum bases.
  Native base-roll landmarks, raw-32 flag handling, local/quality composition and relog preservation passed.
  Chaos Inoculation sets maximum life to 1 after life modifiers and grants chaos immunity in the damage model
  and the panel's Immune row; energy shield remains the pool.
  The pre-CI life used for stun/ailment thresholds and the low-life condition are not modelled.
  Sacred Orb rerolls, local zero-ES rules, equipment ES-to-Life conversion and corrupted-item global ES increases
  are implemented with automated coverage and historical native controls described in Items and crafting.
  Fresh real-client checks of the merged branch remain open.
  See Items and crafting for their scope; other unique/conditional mechanics and Ward calculations remain open.
  Sacred Orbs reroll the saved base through ordinary and Shift currency use, refreshing worn-item pools and panel.
  Native armour, evasion and ES rerolls matched the character updates while preserving mods and quality.
  Shared item defence calculations now include the Tabula and explicit no-ES item overrides.
  ES-to-Life conversion uses each active item's final local ES before global Life scaling.
  Independent global ES remains ES.
  The percentage variant truncates per item, removes all item ES, and yields to the full-conversion flag.
  Recovery guards read effective ES rather than suppressed or converted bases.
  Native full/50% conversion, Life scaling, independent ES, shield swaps and Gateway-restart reloads passed.
  The 69 local ES control converted to 69 Life at 100% or 33 Life at 50%, preserving independent ES.
  Corrupted-item conditional ES scales global ES only while that source item is corrupted, preserving local tooltip ES.
  Shield-specific increases add to global increases only on the active shield's local contribution.
  The native `shield_armour_+%` stat increases all three shield defences; typed increases retain their own scope.
  Iron Reflexes keeps both shield typed increases and counts the generic shield increase once.
  Final AR/EV/ES pools round halfway up after summing fractional sources, including converted evasion.
  Shield defence exclusions remove shield bases from player pools while retaining other equipment and flat sources.
  Native generic/global increases, the shield ES passive, IR, exclusions, conversion and shield swaps matched.
  A 99/99/99 local shield retained fractions through final totals of 168 armour / 186 evasion / 176 ES.
  Both synthetic inventories and expected pools survived a Gateway restart.
  Body-only ES increases now add only to the body's global increase bucket. Body Armour doubling also covers
  IR-converted body Evasion; the no-body-Armour flag suppresses those sources, preserving ordinary EV/ES.
  Twelve native checks covered additive scaling, exclusions, IR, conversion, independent ES and body removal/return.
  Doubling has real-passive regression and native calculation evidence.
  The login path used for those checks filtered ascendancy passives, so the disconnected Unbreakable client fixture
  could not exercise it in-game.
  Full minion redirection and Devotion-dependent shield scaling remain separate mechanics.
  Ward conversions, ring-slot and jewel-radius operators remain separate work.
  Code: `CharacterStats.cs`, `DamageModel.cs`, `ItemCatalog.cs`, `ItemDefences.cs`, Gateway.
  Docs: [11](../docs/protocol/11-skills.md) §4, [13](../docs/protocol/13-items.md) §2.
- Gem skills and bow attacks: Fireball from the socketed gem with its level-1 stats (9–14 fire, 750 ms cast via
  `virtual_base_spell_cast_time_ms`, 5 mana), cast at the ground or at a monster; a bow's ordinary attack and attack
  gems create `Metadata/Projectiles/DefaultArrow` (or the gem's own projectile) at the attack animation's `attack1_bow`
  contact (0.374 of the attack time).
  Attack gems deal the weapon's damage × `DamageEffectiveness` with the attack's crit; a bow arrow has no Fireball
  impact.
  Fireball's impact explodes: an area hit of its radius (`active_skill_base_area_of_effect_radius`) on an enemy or a
  wall, excluding the projectile's own target; the explosion art is a standalone effect object and the projectile is
  removed at the impact.
  Spell projectiles the client draws itself (Fireball, Spark) hit by the client's contact rule: Chebyshev cells within
  the row's max(CollisionRadius, CollisionCandidateRadius) plus the monster's `ObjectSize − 1`
  ([11](../docs/protocol/11-skills.md), "The client's object contact"); automated only, not live-checked.
  Ordinary bow ground/object attacks now share contact-time release and damage at the actual flight collision
  (#203, automated and native collision/release/pause checks pass).
  Object uses no longer also deal a melee contact hit.
  The client spawns the primary projectiles of its own spell gem use itself, every one of a volley, from where it
  stands; the server simulates them but sends no object for them (one fireball was two, 2026-10-29). Their
  continuation children still get server objects.
  The server aims them with the client's own volley: its fan, fixed spread and nova
  ([11-skills](../docs/protocol/11-skills.md) "The client's volley"; static RE, the fan fitted to live Spark volleys)
  and its seeded random directions (static RE, checked live 2026-10-31: damage only where a projectile was drawn).
  Parallel volleys still use the server's own fan.
  Ordinary Burning Arrow uses the client's primary arrow visual; real mouse-input tracing exposed the duplicate
  server arrow missed by the old packet-driven control. Its server collision/damage simulation remains active.
  Other attack gems retain their existing presentation until verified individually.
  Spark's action fires through the same client method, so its sparks are the client's too; they live the skill's
  `skill_effect_duration`, which gem skills now carry (without it no spark was drawn; static RE 2026-10-30, not yet
  checked live).
  The server flies each spark on the client's own seeded wander ([11-skills](../docs/protocol/11-skills.md) "Spark's
  wander"; from the same start the server's legs are the client's cell for cell for three traced live sparks, and the
  live fan agrees within a few degrees).
  Each cast's set of sparks forgets the enemies it hit every 0.66 seconds (wiki Spark), so one cast can hit a target
  again.
  Spark's transfigured and Vaal versions fire Spark's own Projectiles.dat row and follow the same rules; before, they
  had no projectile on the server.
  Still apart: the start cell is the server's walk position, often a few cells from the client's, so the paths run
  parallel but offset, and a live check still saw a drawn spark hit with no damage now and then.
  GGG's server sends no object for ordinary bow arrows either, so the live client draws them itself (live capture
  2026-10-29, [port record](../docs/dev/ranged-monster-actions-port.md) "Live reference").
  Tornado Shot fires `Metadata/Projectiles/TornadoShotArrow` (`SkillCatalog.ProjectileFallbacks`; its row is not named
  after the effect, so every use was refused and the server walked on while the client stopped, 2026-10-29); it is the
  basic straight-flight arrow only: the secondary arrows from the target point are not modelled yet.
  Spectral Throw fires `Metadata/Projectiles/ThrownWeapon`, pierces every enemy and hits again on its return, with blood
  on each hit; the client draws the weapon, and the server flies the same slowing, resting and returning path. Partial:
  hit timing still needs tuning. The return flies at a constant average speed where the client's accelerates, and
  its hits vary most on monsters walking toward the thrower. The release follows the Melee combo only for the Witch
  and Scion with a one-handed sword, mace or axe and a shield; the server assumes the combo alternates on every throw,
  and when the client starts it over is not known. A wall stops the weapon without a return. The transfigured and Vaal
  versions are not mapped.
  after the effect, so every use was refused and the server walked on while the client stopped, 2026-10-29); the
  terminal secondary group is in Partly working.
  Rolling Magma fires `Metadata/Projectiles/MagmaOrbShort` (same fallback; every cast was refused before) and lands on
  the aim, explodes and bounces straight on once per chain, each impact an area hit (verified live 2026-11-01: damage at
  every impact, 3 impacts at level 1). The base radius 14 and 16 cells between impacts are not read from the client;
  its slower bounces (375 ms each) mean a bounce's hits land about 0.1 s early.
  Split Arrow fires `Metadata/Projectiles/SplitArrowDefault` (`SkillCatalog.ProjectileFallbacks`; its row carries a
  Default suffix, so every use was refused before).
  Lightning Arrow's hits call secondary strikes on up to 3 nearby enemies in an 18-cell radius
  (`ContactTargets`, `lightning_arrow_maximum_number_of_extra_targets`), scaling with Area of Effect modifiers.
  Projectiles spawn at the captured terrain height + the releasing clip's keyframe height (the bow 16 steps, a wand
  12, the Sand Spitter's spit 9; `Game_PROJECTILE_HEIGHT`, 5 steps, without rig data) on natural ground; without a
  capture they use the bare offset.
  An attack gem at the ground swings (and pays its mana) when the point is within the weapon's reach, and only walks
  (attack-move, no mana) beyond it; the client spends the mana itself on either use, so the Gateway answers an
  attack-move with the authoritative mana.
  Code: `SkillCatalog.cs`, Gateway (`CastOn`, `LaunchProjectile`, `AreaSkillHit`), `ProjectileObject`.
  Docs: [11](../docs/protocol/11-skills.md) §9–10.
- Resistances: the passive `base_resist_all_elements_%` / per-element `base_*_damage_resistance_%` are summed from the
  0% base, the item tables' non-base spellings (`fire_damage_resistance_+%`, `elemental_damage_resistance_+%`) count as
  the same stats, and the pair-resistance stats (`cold_and_lightning_`, `fire_and_cold_`, `fire_and_lightning_`) each
  raise both named elements; the two Kitava quest penalties (-30% each) are mirrored from quest flags, and the final value
  uses Path of Building's floor -200 / default max 75 / absolute cap 90 (`DamageModel`);
  used by Righteous Fire's self-burn and `/hit`; `/resistances` shows modifier, penalty and final.
  Code: `CharacterStats.cs`, `DamageModel.cs`, Gateway.
  Docs: [11](../docs/protocol/11-skills.md) §12.
- Spell Suppression: the passive `base_spell_suppression_chance_%` is capped at 100% and a successful suppress takes
  40% less spell damage plus `base_spell_damage_%_suppressed` (PoB `SuppressionChanceCap`/`SuppressionEffect`);
  `spell_suppression_chance_is_lucky` rolls twice and keeps the better roll (effective `1-(1-p)^2`), with an unlucky
  `p^2` dev mode; the conditional chance passives count while their condition holds:
  `spell_suppression_chance_%_while_on_full_life`, `_while_moving` (the walk estimate), `_if_enemy_hit_recently`
  (an attack or spell hit in the last 4 s), and `_while_on_full_energy_shield` from the effective ES pool.
  Full ES requires a living player with positive capacity and whole current ES at maximum; zero ES does not qualify.
  The raw base and active conditions add before one 0..100 clamp, shared by the panel and spell-hit roll.
  Monster spells apply suppression after armour and flat damage taken.
  Sixteen native controls checked damage/refill, actual recharge, cap, conversion, independent ES, no-ES/zero-ES
  sources and independent full-Life/full-ES conditions. The guaranteed first spell dealt 60 damage to 118 ES,
  leaving 58 ES and changing suppression from 100% to 90% for the next hit.
  Checkpoint resurrection publishes restored full-pool conditions explicitly, even if the player tick cached the
  same chance before death. The encrypted-wire regression fails before the fix and passes afterward; native
  Clarity/death/checkpoint controls confirm 100% -> 90% -> 100% without another equipment or aura refresh.
  `/suppression`, `/suppression_luck`, `/spellhit` expose the shared result.
  Not implemented: conditional suppression effect stats and monster spell hits.
  The character panel gets the chance again within 250 ms of a condition changing (the player tick).
  Code: `CharacterStats.cs`, `DamageModel.cs`, Gateway.
  Docs: [11](../docs/protocol/11-skills.md) §12.
- Movement speed: the passive `base_movement_velocity_+%` and the item/conditional `movement_velocity_+%` are summed
  into `Pools`, sent as the client's `movement_velocity_+%` and used by the server's walk copy (37 × (1 + pct / 100)
  cells/s);
  applied on login, level, passive and area changes; `/set_speed` adds its percentage on top.
  `action_speed_-%` from raw sources and dev overrides is composed additively with chill/freeze and multiplied once
  into movement, attack, cast and stun-recovery pacing. New actions are limited to one per frame (30/s, `TickSeconds`);
  channel repeats bypass that cap. Conditional/more multipliers, gear/buff movement, Tailwind/Harbinger/Temporal Chains
  sources, trap/mine/warcry/totem timings, monster floors and the 128 cap remain open (#246).
  Code: `Player/CharacterStats.cs`, `Combat/PlayerAilments.cs` (`PlayerActionFactor`), Gateway (`ApplyMovementSpeed`).
  Docs: [07](../docs/protocol/07-latency-visibility-movement.md) §3.
- Block / Spell Block: passives' `additional_block_%` and `base_spell_block_%` are summed and capped at 75% / 90% (PoB
  `BlockChanceCap`), max-res passives `maximum_block_%` / `base_maximum_spell_block_%` raise the caps;
  a successful block prevents the hit, monster melee and `/blockhit` use it; `/block` shows both chances;
  an equipped shield adds its `ShieldTypes.Block` base plus the shield's local `local_additional_block_chance_%`
  (unique and rare shields alike, and Kongming's `local_no_block_chance` removes it) and enables the shield-specific
  passives, staff passives apply
  with a staff, and dual wielding adds its inherent 20% attack block and dual-wield block/spell-block passives;
  Versatile Combatant reduces both maxima by 10% and converts overcapped attack block to spell block at 2% per 1%;
  lucky block uses 1-(1-p)^2 for attack/spell (dev `/block_luck`);
  the character panel gets `uncapped_attack_block_%` and `display_spell_block_chance` so it shows the uncapped value
  in parentheses and the spell block row.
  Code: `CharacterStats.cs`, `DamageModel.cs`, Gateway.
  Docs: [11](../docs/protocol/11-skills.md) §12.
- Endurance, Frenzy and Power charges: gained from the on-kill (a frozen enemy's too), on-hit, on-crit, on-non-crit,
  when-hit and on-block chance stats, the timed ones (each second, while stationary or moving, if hit recently) and
  skills (Frenzy, Power Siphon, Flicker Strike, Enduring Cry); 10 s duration with the duration mods, a gain refreshes
  the whole type, all expire together, minimum charges always held, lost on death, carried over area changes with the
  time left.
  Character.ot's per-charge bonuses and every "per charge" mod the server calculates with apply to the pools, hits,
  damage taken, walk speed and the sent stats; the Charges tab gets `current_*_charges` and the per-charge rows.
  The HUD shows one icon per type with the count (0x1c1 flags bit2) and time left (bit1), and the orbs.
  Live 2026-10-25: gains when hit, on non-critical hit and on kill, refresh, expiry, death, area change, minimum
  charges, 3 Power Charges taking attack crit 8.3% → 20.75%, a Frenzy Charge shortening the attack, 3 Endurance
  Charges taking 12% off physical and fire hits; towns draw no charge icons.
  Replica Fragility, Replica Pacifism and Replica Powerlessness ("Lose ... Charge each second", the stat stored as
  `-1`) take one charge each second down to the minimum: the negative count used to be skipped, so nothing happened.
  The maximum rows (with the maximum-charge mods) were verified on the native Charges tab at `0 / 3` before this;
  controlled native counts of 0 and 3 now agree with the held state. Enduring Cry and timed gains are not live-checked.
  Three native mirror modifiers now publish Brutal/Affliction/Absorption maxima from completed Endurance/Frenzy/Power
  maxima, with final standard/special zero overrides and independent source clearing. Real 4/5/6 native-modifier
  fixtures cover both modes; combined automated results are recorded with the panel changes below.
  This uses labelled pinned-PoB 3.29.x formulas and native source identities, without certifying every official getter.
  Converted charges (2026-10-29): under the three "gain … instead of" unique flags the held Endurance / Frenzy / Power
  charges are Brutal / Affliction / Absorption Charges: the special `current_*` and `minimum_*` rows and BuffDefinitions
  1757–1759 buffs, the standard effects off, Brutal's triple-damage chance and stun threshold per charge applied, and
  Affliction's more ailment damage in the ailment panel rows (`ConvertedChargesGatewayTests`, `ChargeEffectsTests`,
  `ChargeSessionTests`, `ChargeBuffsTests`; live 2026-10-29 with the flags set by `/set_stat`: the Brutal row, the
  zeroed Endurance row and the swapped buff). Reap's Blood Charges (2026-10-29): a use whose hit
  leaves an enemy alive gives one, a kill takes one, up to Character.ot's 5 plus the enchantment; the charges held are
  15% more damage per charge on the hits and the Offence rows and 20% more cost, with the Charges tab rows, the
  `show_blood_charge_info` gate and BuffDefinitions 1822; no duration (none in the tables), not carried across
  areas (`BloodChargesTests`, `BloodChargesGatewayTests`; live 2026-10-29: rows, gain, loss and buff with a socketed
  Reap). Spirit, Siphoning, Blitz and Challenger
  have no reachable player source here (abyss-jewel / Elder-Shaper counting, none in 3.29), the Affliction
  non-damaging effect and Absorption recoup have no consumer.
  Code: `Charges.cs`, `ChargeGains.cs`, `ChargeEffects.cs`, `ChargeSession.cs`, `ChargeBuffs.cs`,
  `Program.Charges.cs`, `ChargePanelStats.cs`, `BloodCharges.cs`, `Program.BloodCharges.cs`.
  Docs: [11](../docs/protocol/11-skills.md) §15.
- Character panel sums: about 150 panel stats that sum base forms are computed from passives, items and auras with
  their unconditional combined stats: stun, ailment avoidance, durations and effects (ignite, chill, freeze, shock,
  scorch, brittle, sap), ailment chances, penetration, leech, life/mana/ES on kill, rarity and quantity, cooldown,
  projectile speed, area, curse effect and flask duration, reflect, charge durations, cannot-be/immune flags (including the
  four damage immunities and physical immunity, from their base forms);
  the main hand / off hand rows add that weapon's local stats;
  projectile block and chaos resistance against damage over time combine the pools with their stat;
  the Defence tab's Life / Mana / Energy Shield Regeneration per Second rows are the tick's own per-minute rates
  (`total_*_recovery_per_minute_from_regeneration`).
  Evidence scope for the imported issue #75 entries: earlier test counts and Standard/Ruthless native matrices
  describe the source LocalExile revisions, not a fresh native run of this branch. Private game-mode mechanics
  are excluded; current automated results and remaining checks are in the [port record](../docs/dev/issue75-port.md).
  Six native Character.ot defaults now enter the original sources once: Brand limit 3, Brutal triple-damage/stun
  coefficients 3/10, Affliction damage/non-damaging coefficients 8/8 and Absorption recoup coefficient 12.
  Real Brand item +1 and Brand Equity +2 give actor 6; compatible linked Foulgrasp gives selected 8.
  The Brand gate follows native skill type 64 only on selected Brands; explicit replacements/zero and removal retain
  their source precedence in both modes. The special values remain coefficients; Brand attachment/cap enforcement,
  special-charge gameplay and recoup execution are not added; new native checks remain pending.
  Selected area, item-rarity and minion-increased aliases now combine raw actor and qualified own sources once,
  retaining signed cancellation/zero and final developer replacement precedence. Initial `SendPlayer` and
  area-transition object creation share that snapshot with later `PlayerSkills` refreshes; Cyclone's radius inputs
  are calculated before the selected overlay and keep their existing result.
  Real supports cover area 20+30=50, minion 15+quality 10=25 and ordinary Item Rarity 10+40=50.
  Selected fire/cold/lightning penetration now also includes the linked support's raw contribution.
  Each real level-one support adds 20 to actor 10, displaying 30 instead of the previous incorrect 10.
  Nine new Gateway regressions cover link/source removal, cancellation, overrides, requirements and skill isolation;
  general elemental penetration composes to 35 in both the panel and hit consumer.
  These pass within 29 focused checks on this branch; fresh native verification remains pending.
  Both attack-hand penetration rows include typed/elemental attack-only sources and qualified linked supports.
  Raw local hand snapshots preserve each active weapon's contribution during selected-skill recomputation.
  Twelve additional automated cases cover all three elemental supports, chaos/attack-only sums, independent
  local modifiers, inactive weapons, source removal and link changes; native display checks remain pending.
  The penetration consumer now includes non-base general elemental/chaos sources and the qualified attacking
  weapon's local modifiers; spells exclude local/attack-only inputs. Parsed contact cases cover ordinary melee/bow,
  Cyclone, Burning Arrow and Fireball with/without elemental prohibition. Selected and actor projections emit zero
  for all nine elemental aliases under prohibition and restore totals when removed; chaos remains separate.
  Real Elemental Penetration support contributes 14 at level one and agrees with the hit consumer.
  Conditional penetration: "Damage with Weapons Penetrates" counts for attacks with a weapon (hits and the hand rows
  of a hand holding one), cold penetration against chilled enemies against a monster chilled at the hit, and Watcher's
  Eye's Anger, Hatred and Wrath forms while that aura runs (`InflictedAilmentTests`).
  Current-branch native validation, off-hand attack selection and the other conditional forms
  ([20](../docs/protocol/20-damage.md) §2) are still open.
  Selected ailment duration/effect coefficients now also include qualifying supports: real level-one Unbound
  Ailments adds 35 to character 10 for all seven duration and five non-damaging effect aliases. Its 10% more
  ailment damage stays separate. Twenty-four regressions cover the reproduced omission, link/source removal,
  signed cancellation, explicit replacements and unrelated/default-skill isolation. Native presentation and
  actual ailment application remain open; this does not complete the ailment system.
  Historical LocalExile Ruthless support exclusions are outside this branch. Link/source removal, incompatible or unqualified supports,
  unrelated/default skills and repeated builds are covered; minion combat and item-drop execution remain separate.
  Added-base critical/default-mirror/selected-alias cases pass 52/19/22 new tests plus 95 existing controls:
  the combined focused batch passes 188/188 without skips. Fresh Gateway 3803/Core 648/Data 37 all pass without skips,
  for 4488 freshly rerun cases. The current hook records six actual unobserved disposed-NetworkStream task faults,
  all `MonsterKilled` through `Program.Send`; 385 first-chance records are separate from those six faults.
  No diagnostic errors/truncation/cap hit or new operator path appears in the captured faults.
  Passing tests do not mean fault-free background execution.
  Native default/mirror and selected-alias checks remain pending; the critical entry below records its bounded controls.
  Earlier G14/full 3710 and native observations below do not validate these new operators or certify all 387 rows.
  Energy Shield's row excludes recharge and clears when its sources are removed or regeneration is prohibited.
  Native checks verified the row at 2 per second, modifier totals, zero clearing and relog.
  During recharge its total stayed at 60 per minute while native recovery prediction changed from 60 to 340.
  Frenzy attack/cast speed coefficients include the combined per-charge modifier. Native Mods row 7236's -4
  cancels the inherent 4 for both; held charges still expand the raw source once. Seventeen selected tests
  (15 new) cover source removal and explicit zero in both modes. Eight native supplied-source controls agree,
  including +4%/+6%/attack-only +5% formatting, zero publication, charge clearing and relog.
  Zero rows are visually suppressed; missing formatter callbacks do not prove zero rendering or gear acquisition.
  Ignite/Chill/Freeze/Shock duration reductions also subtract raw `base_all_ailment_duration_on_self_+%` once.
  Real Harsh Lessons gives -20 in both modes, producing +20 reduction; typed and elemental sources remain additive,
  signed increases remain negative reductions, and final developer zero/overrides keep precedence.
  All 14 new pure/Gateway cases pass in the combined 53/53 focused batch; full Gateway passes 3710/3710 with no
  skips. Unchanged prior Core 648/Data 37 results remain applicable (4395 total, not all newly rerun).
  Five actual unobserved disposed-stream faults remain recorded (four MonsterKilled, one projectile-hit path),
  with no captured reference to the new operators. No new native evidence is claimed. Ring-slot selection,
  other ailment families, conditional ES delay and actual ailment execution remain separate.
  Block Recovery now adds base and raw direct Stun Recovery once from the same snapshot, including already expanded
  per-charge recovery. Life/Mana/ES on-kill amounts and the flask-duration coefficient include their real per-player-level
  inputs, using the actual character level and wide arithmetic. Thirty new panel/source cases cover both modes.
  The preceding panel/flask batch passed 55 new plus 80 existing focused cases and Gateway 3657/3657;
  unchanged prior Core 648/Data 37 results remain applicable (4342 total). The combined native assessment has
  40 completed controls: 22 complete PASS (12 positive source cases, six utility uses and four Life uses), with
  18 complete zero controls UNPROVED. Positive source cases at actual levels 10/20/100 match all six fields in both
  modes. The six panel fields have 138 VERIFIED/42 UNPROVED verdicts including retained and transition evidence;
  neither Defence zero field is proved in any zero control, and some Misc zeros also remain UNPROVED.
  No hidden row is counted as a rendered zero. Full on-kill recovery/percentage/prohibition operators and block
  animation timing remain open; exact runtime values and the original one-integer Life boundary are in protocol 13 §8.
  Those historical native checks predate the new self-duration and flask-effect operators. A subsequent eight-control
  live batch verifies positive per-level flask effect at actual levels 10/100 in both modes: Life rates 700/1960 for
  three seconds, and utility movement +20% / armour +2100 with native Standard/Ruthless durations. Utility strength
  is verified on explicit final character stats, separately from its timer buff. All stock/charges and original data
  were preserved, both-mode representative relogs passed, and normal services were restored. The retained verifier
  revisions and one-integer Life boundary are documented in protocol 13 §8. These controls do not promote the old
  18 whole-zero cases or establish source-removal/zero next-use behavior, whole-unique acquisition/tooltip parity,
  new self-duration native coverage, or completion of an issue 75 row.
  The 2026-10-29 row batch publishes the rows that had no derivation: the Defence estimates (physical reduction plain /
  against projectiles / against hits with their differs flags, chance to evade, ward delay, mana recharge, Omniscience
  requirements) against DefaultMonsterStats at the character's level; the off hand's own accuracy, chance to hit,
  attack speed, critical chance/multiplier and damage ranges with `skill_uses_off_hand` for dual-wielded default and
  gem attacks, whose per-use / DPS / attacks-per-second rows average both hands (`DualWieldPanel`); the spell
  effective / twice-critical chance (luck, then Assassin bifurcation); secondary-damage rows for
  flagged spells and for Bear Trap / Herald of Ice from their own secondary base damage (generic buckets only,
  `Program.SecondaryPanel.cs`); `skill_show_average_damage_instead_of_dps`; the 32 Bleeding / Ignite / Poison damage per second rows
  per hand, spell and secondary context (`AilmentPanelStats`, from the hit's raw packet and conversion with the
  ailment buckets and DoT multipliers); stun-by-kind flags, brand per-target limit, Spirit maximum and the Siphoning
  damage-taken coefficient. 60 new automated cases pass with real sword/dagger, Cyclone and Fireball fixtures; the
  owner's live run of 2026-10-29 (base `7fd944be`) passed the batch's test plan (off hand and clearing, ailment rows,
  effective critical, secondary blocks, Defence estimates under `/set_stat`). Actual ailment application and
  Elder/Shaper and abyss-jewel item counting (Spirit, Siphoning; deferred behind #60) remain open
  ([review](../docs/dev/issue75-review.md#offence-defence-misc-and-charges-row-batch)).
  Code: `PanelStats.cs`, `PanelStatDefaults.cs`, `AilmentPanelStats.cs`, `Program.OffHandPanel.cs`, `DualWieldPanel.cs`,
  Gateway (`CharacterPlayerStats`, `HandLocalStats`, `GemSkills`).
  Docs: [11](../docs/protocol/11-skills.md) §14.
- Character-panel leech limits: full maximum pools supply separate per-instance amounts and total recovery caps.
  The current issue #75 review also corrects 24 leech coefficient and three recovery-speed aliases: general,
  typed/elemental, attack/spell and qualified hand sources, missing physical/chaos ES, and real selected supports.
  Actor/support fractional inputs combine before unit conversion. The new composition has automated controls;
  native getter/formatting parity remains open (damage §5b, skills §14).
  Six absolute rows and their percent companions include supported increases/reductions and prohibition flags.
  Native controls verify display conversion, Low Life/healing, real item pool conversion, reservation and relog.
  The periodic stat refresh uses one consistent developer snapshot, with four observed-task regressions.
  Fractional endpoint truncation is a documented local projection; official-server rounding is unmeasured.
  Ordinary accepted melee/bow/projectile/area hits now create separate Life/Mana/ES instances from raw qualified
  sources and post-mitigation direct damage. Their duration, total recovery, cap and generic recovery multipliers
  are distinct. Instant portions recover inside the hit transaction; the existing resource writer integrates timed
  portions and exact expiry boundaries. Full-pool removal, unconditional overleech, prohibition, pause/death/transfer
  cleanup and real support contacts have automated controls. Ghost Reaver now creates separate converted/native
  ES instances, doubles the total ES cap and disables recharge; actual node sources and parsed refund have controls.
  Plain conversion does not add those keystone side effects. Other conversions/conditions, skill-specific
  recovery-speed ownership and current native recovery/prediction parity remain open. Leeching/full-ES flags now
  reflect actual instances/pools and refresh player/selected stats on the normal tick. General/resource/attack/
  physical leeching hit damage and Energy Leech's full-ES MORE/quality increased bonuses have separate buckets
  and actual support/contact controls; speed/defence/ailment/DoT consumers remain open.
  Attack-only ES overleech retains attack instances (including Ghost Reaver conversion) while removing spell
  instances at full ES, preserving original expiry and one shared cap. Actual mod/contact and source-removal
  controls pass. Ordinary Life-overleech projection now respects explicit zero replacements; other sources remain open.
  Claw mastery instant leech counts active usable claws, Weapon Master applies its claw condition once, and
  Gore Dancer's Life instant source survives Ghost Reaver conversion. Real-source contacts and split/override
  controls pass; exact native mixed Life/ES instant composition remains unverified (damage §5b records the policy).
  Kills recover Life, Mana and ES on kill, also by ailment; not checked live ([20](../docs/protocol/20-damage.md) §5b).
  Highwayman's real modifier now doubles total Life-leech recovery once, independently of the explicit Life
  maximum-rate doubling flag. Source/removal, duplicate flags, caps, duration, instant and conversion controls pass;
  current Vaal Pact acquisition and native stacking/recovery parity are not implied by raw-flag support.
  Attack gain on hit recovers Life, Mana and ES; not checked live ([20](../docs/protocol/20-damage.md) §5b).
  Code: `LeechLimits.cs`, `LeechSources.cs`, `LeechRecoveryState.cs`, `LeechSession.cs`, Gateway (`Program.Leech`, resource writer).
  Docs: [20](../docs/protocol/20-damage.md) §5b.
- Ordinary Life/Mana regeneration: flat and maximum-pool percentage sources receive regeneration and recovery increases.
  The matching prohibitions stop recovery, and the panel/native prediction inputs share the actual resource writer.
  Ten native checks verify flat sources, independent increases, prohibition, clearing and relog.
  Controls matched 1080 Life/minute to 18/second and 3339 Mana/minute to recovery near 55.65/second.
  Fortification's exact expiry integration uses the same factors, with automated modifier/prohibition coverage.
  Zealot's Oath (`keystone_zealots_oath`, and Geofri's Sanctuary's `regenerate_energy_shield_instead_of_life`) moves
  life regeneration, including Fortification, Turtle Shell and the existing Enduring Cry recovery path, to Energy
  Shield; the panel and resource writer share the split, and ES recovery prohibitions drop the converted part. Live on
  Qblolz, toggling Geofri's stat with `/set_stat`: off gave 9974 Life/minute and 6697 ES/minute; on gave 0 Life and
  16672 ES/minute.
  Conditional flask/item sources and other regeneration conversions remain separate work.
  See [20](../docs/protocol/20-damage.md), "Continuous resource updates".
- Determination and Grace truncate their scaled flat/more aura inputs before character-wide defence calculation.
  Normal and Ruthless native data and quality isolation have regression coverage.
  Basalt/Stibnite compose more factors without rounding the combined percentage before the final rating.
  Native ordinary aura/flask, global-increase, IR and clearing checks matched the shared pools.
  Code: `AuraCatalog.cs`, `UtilityFlasks.cs`, `CharacterStats.cs`.
  Docs: [11](../docs/protocol/11-skills.md) §13.
- Discipline supplies its level-based flat ES to the shared pool, panel, absorption and recharge paths.
  Intelligence and global ES increases apply once; shield-only increases and armour ES-to-Life leave the aura source.
  Quality affects area of effect only; aura-effect scaling truncates integer ES/recharge stats using pinned PoB.
  Native activation, clearing, capacity without instant refill, absorption and delayed recharge matched.
  Native level 20, quality isolation, global scaling, conversion and shield-source controls also matched.
  Town suppression/return, gem-host removal/return and death clearing were checked with this aura.
  Both level-1 and level-20 fixtures restored expected ES and identical saved inventories after a Gateway restart.
  Code: `AuraCatalog.cs`, Gateway (`PoolsFor`, `BaseStats`).
  Docs: [11](../docs/protocol/11-skills.md) §13, [20](../docs/protocol/20-damage.md), "Player energy shield".
- Buffs (S→C 0x1c1 add, 0x1c2 remove) and Righteous Fire: cast on oneself, its buffs and the big burning circle (an
  effect object, `Metadata/Effects/PermanentServerEffect` with `rifire.ao`, attached to the player), 90% of life per
  second nonlethal burn on the player, 70% of life per second to monsters within its radius (18 cells, grown by the
  player's area of effect stats, `SkillArea`; they burn to death), the circle scaled to that radius (MiscAnimated's
  reference radius, the uniform Positioned scale; matches the real game at level 1), carried by the client at any
  latency and not turning with the player; server-side mana (cost, regeneration); `/god` (also 200% increased area of
  effect), `/set_aoe`.
  Code: Gateway (`ActivateRighteousFire`, `RunPlayerTick`), `SkillArea.cs`.
  Docs: [11](../docs/protocol/11-skills.md) §12.
- Warcry admission restriction (#297): any signed nonzero `your_warcry_skills_are_disabled` blocks new typed warcries.
  Actual caster passive/equipment/raw dev sources are read before skill overlays, action echoes, costs and cooldowns.
  Explicit zero and source removal restore admission without rebuilding pools or removing Actor skills.
  Parsed ground/object, paid Enduring Cry, source, instant safety and queued-send regressions pass.
  Accepted non-cancellable cry releases retain their existing behavior; Enduring Cry's charge executor stays partial.
  Seven native checks verify refusal without Mana loss or release, source clearing and paid 13-Mana retries.
  Native Endurance Charges confirm accepted releases; Fireball remains usable and relog clears the developer flag.
  Phrecia play stays gated; source precedence, aliases and queued boundaries have additional automated coverage.
  See [11](../docs/protocol/11-skills.md) section 10b.
- Own-aura restriction (#304) consumes signed 21148 OR 11881, with explicit final 11075 developer precedence.
  Typed Aura and accepted Blasphemy metadata drive admission and native skill flags.
  Admission and queued acknowledgements use the requested skill ID; duplicate gems keep separate support context.
  Remembered candidates check their own requirements, so an unusable copy cannot suppress another available gem.
  Local policy suspends self-owned pairs, bonuses and reservation while retaining intent, without a mana refill.
  Lifting the restriction restores available remembered ordinary self auras once with fresh buff IDs and a mana clamp.
  RF, flasks, Fortification and received effects remain outside this owned lifecycle; no party aura executor is claimed.
  Source, real-dispatch, loopback, queued snapshot/add, initial creation and entry replay regressions pass.
  Rage refreshes reconcile paired removals and reservation before their snapshots, even when Rage is unchanged.
  Fifteen native controls verify Clarity/Haste pairs, real bonuses/Mana, three refused routes and ordinary Fireball.
  Relog restores remembered Haste after the transient source clears; death while suspended clears saved intent.
  Native grey-icon prediction and persistent restricting equipment remain unverified.
  The suspend/restore lifecycle is explicit LocalExile policy, not proven GGG behavior; Phrecia play stays gated.
  See [11](../docs/protocol/11-skills.md) section 10c.
- **Spell skills deal no damage (#293):** raw caster key 5341 suppresses supported Spell hit packets and RF enemy burn.
  Physical, elemental and chaos spell damage, supported projectiles and areas share the final packet restriction.
  Selected native record ranges, average damage and DPS use that packet, including inherited main-hand zero rows.
  Casting, costs, timing, visuals, zero-hit callbacks, attacks and aura-granted attack bonuses retain their scope.
  RF self-burn continues independently; source changes follow existing impact/tick resolution without pool rebuilds.
  Automated supported constructors, dispatch/Life, in-flight changes, aura controls and RF tick checks pass.
  Current-main controls cover Rage/charge contacts, quality, Mana sources, Mageblood, chaos armour
  and cosmetic identity.
  Unpaid release guards, utility reservation/cooldowns and RF fractional carry keep their existing behavior.
  Eleven historical native checks at original head `e80a81c` cover Fireball/Ice Nova, paid casts and attacks.
  Native Fireball action rows clear ranges and DPS; RF keeps its self-burn while its enemy damage stops/resumes.
  Those controls cover source changes and relog at the historical head.
  Fresh ordered joint controls at `c9cd1864` verify Fireball 200 / zero, attack 100 and restored Fireball 200.
  Final attributes and converted pools remain active in that same unascended Marauder session.
  Rampager 49937 supplies the source, but class admission, owner execution and the node's other mechanics are separate.
  Code: `SkillDamageContext`, `SupportedHit`, `SpellHit`, `BurnAround`, `GemSkills`.
  Docs: [20](../docs/protocol/20-damage.md) section 2g.
- Spells are released at the cast animation's contact keyframe (Witch 599 of 1066 ms;
  every class's from its rig .amd), with the server duration adjusted once for cast speed.
  Code: `AnimationContacts.cs`, `scripts/re/anim_contacts.py`.
  Docs: [11](../docs/protocol/11-skills.md) §9.
- Pride on enemies: its buff (row 1331) reaches every monster in the aura without breaking the client's stream.
  Code: `EnemyDebuffs.Rules.cs`. Docs: [11](../docs/protocol/11-skills.md) (The player's debuffs on monsters).

## 部分实现

- 战斗负载：报文/帧与新建的投射物释放快照；按输入校验的技能/装备缓存忽略同级宝石经验与延长的近期激活窗口；
  不可变的物品本地属性、条件解析器与投射物配方被复用。F1 NETWORK 保留真实的输入队列延迟；
  大的负载尖峰仍在调查。代码：`PlayerStatsCache.cs`、`StatConditions.cs`。
- Combat load: message/frame and fresh projectile-release snapshots; input-checked skill/equipment caches ignore
  within-level gem XP and extended active recent windows; immutable item-local stats, condition parsers and projectile
  recipes are reused. Aura reconciliation compares live sources, restrictions, equipment and reservation inputs;
  support resolution shares raw gem sources, quest rewards follow flag changes, and projectile motion is reused only
  until the next contact. F1 NETWORK retains real input-queue latency; pathfinding and load spikes remain under investigation.
  Code: `PlayerStatsCache.cs`, `OwnAuras.cs`, `GemLevels.cs`, `ProjectileRuntime.cs`, `StatConditions.cs`.

- 幸存者赛季与三个可选模块，含七个阶段闸门 Boss；目录只保留飞行/脉冲呈现能播放的技能
  （每个投射物都有可见兜底），地面区域法术落在目标上，辅助池是所有施法管线会读数值的普通辅助宝石，
  敌人 5 倍耐打、4 倍伤害（远程弹 30%），对生命有 DPS 威胁，附近有经验球，第一波之前有暂停窗口，
  25% 生命的死亡区与尸体清理；传奇在两次跑之间保留，Boss 是它们阶数的 5 倍：
  [客户端呈现与限制](../docs/game/survivors-mode.md)。

- 攻速准入：宝石攻击现在用受支持的攻速（宝石与辅助属性加上固有的 GrantedEffectsPerLevel 倍率）
  同时决定准入闸与挥击间隔，不再与 `SkillAttackSeconds` 混用。龙卷射击的 -20% 与玩家/弓攻速生效。
  投射物速度把玩家的 `projectile_speed_+%` 加到技能的 `base_projectile_speed_+%` 上。自动化的节奏与
  投射物速度核对通过；原生燃烧箭数值控制确认持续时间之前的整数最终速度取整，
  而更广的原生准入核对仍待做。代码：`GemSupports.Rules.cs`、`Combat.cs`、`ProjectileSpeed.cs`。
- 生命分流辅助（Lifetap）：受支持技能把全部魔力消耗当作生命支付，含 300% 消耗倍率；
  已付出的生命按 `support_lifetap_spent_life_threshold` 累积。达到阈值授予 4 秒（加品质）的生命分流 buff，
  其更多伤害作用于受支持命中，其 20% 生命回复增幅作用于药剂。
  瓦尔使用支付同样的生命消耗。该 buff 通过自身光环的发送闸呈现；隐藏的已付出生命 buff
  与召唤物 buff 不发送，持续伤害不吃那个更多倍率。
  自动化测试覆盖带一级生命分流辅助的龙卷射击：9 魔力变 27 生命，首次使用授予 buff，
  命中与药剂加成生效。代码：`Combat/Lifetap.cs`、`Player.cs`、`ConditionalSupportDamage.cs`；
  文档：[25](../docs/protocol/25-gem-supports.md)。
- 弓的投射物数量：实际弓攻击现在在通用投射物添加之上读 `number_of_additional_arrows` 与
  `number_of_additional_arrows_while_main_hand_accuracy_is_3000_or_more`，所以弓/箭袋修正与「大师制箭师」
  这类被动会加主箭。这些箭输入被限制在真实弓上，不会落到法杖、法术或近战投射物；已选技能面板也发布箭总数。
  终端次级组是另一个改动（在 #549 实现）。
  自动化测试覆盖弓/法杖分离与面板行；尚无原生核对。
  代码：`GatewayProjectileModifiers.cs`、`GatewayProjectileDefaults.cs`、`ProjectilePanel.cs`；
  [逆向计数输入](../docs/re/projectiles/count-stat-inputs.md) 第 3 节。
- 龙卷射击的终端次级：每支主箭穿透它的接触，并在终点发出三发 `TornadoShotSecondaryArrow`，
  围绕客户端原生的终端随机射线（`[sequence, mother-derived byte, owner, repeats]`）分布。
  客户端自己预测主箭与次级箭（Frida：一次使用有七个本地与七个服务端 `TornadoShotArrow` 对象），
  所以服务端把两者都隐藏、只模拟它们的伤害；每套次级里有一发能命中敌人，子体保留辅助的穿透/分叉/连锁。
  它们的射程用客户端 0.3 s 的次级动作寿命（Frida 跟踪把最终回调限制在 300 ms），所以投射物速度会缩放它，
  而近距离/远射的距离包含主箭那一段。测试覆盖档案、客户端随机向量、300 ms 生命与延续的近距离/远射距离；
  2026-11-02 实测确认了单一主箭起点、扇形与伤害。
  代码：`GatewayProjectileSkillProfiles.cs`、`ProjectileStages.cs`、`ProjectileTypes.cs`、`Projectiles.cs`；
  wiki [Tornado Shot](https://www.poewiki.net/wiki/Tornado_Shot)、
  [逆向终端几何](../docs/re/projectiles/tornado-geometry.md)。
- 石化之血：35% 魔力的保留 buff、其在半生命处仅对命中的生命损失分流、四秒的延迟损失、
  濒死时非药剂回复上限与其上的生命消耗都已实现。生命汲取实例与客户端的汲取显示保留完整未保留最大生命的上限；
  濒死时它们不被移除，但实际回复像回复与补偿一样被封顶。补偿基于石化之血之前的命中，它的四秒实例在濒死时
  也继续跑；21 级/20 品质宝石的 30% 延迟份额被 30% 的补偿抵掉，同为四秒的生命。
  隐形的生命/魔力/能量护盾汲取 buff 以完整每分钟速率呈现，让客户端能画汲取叠层；只有实际池更新被封顶。
  延迟速率用隐形 BuffDefinitions 1796（`slow_life_loss_from_hit_damage`）呈现，为仍欠的损失画暗色生命球叠层。
  濒死使用共用的 50% 默认值（先 `low_life_threshold_%_override` 再 `low_life_threshold_%`），
  不是最大生命的固定一半；3.29.3b 的生命精通 `Life4` 把覆盖设为 55%。客户端自己的
  `life_recovery_per_minute` 在上限的 250 ms 回复边距内发布为 0、更低下则恢复，
  这样它的池预测不会越过边界。
  激活、关闭、跨区域的存档意图、死亡清理与自身光环/受得 buff 呈现共用既有的光环发送闸；
  该 buff 不是光环，光环限制不会碰它。
  未实现：该 buff 专属的保留效率行、客户端提示里的生命消耗，以及任何非命中的生命损失
  （持续伤害与已经延迟的损失都不受保护）。
  自动化测试覆盖分流、1 级/20 级延迟、阈值、回复上限（含一个在濒死之上保留但回复被封顶的汲取实例）
  与生命消耗；尚无原生核对。代码：`Combat/PetrifiedBlood.cs`、`Combat/LowLife.cs`、
  `Modules/Core/Game/Combat/PetrifiedBlood.cs`；文档：[20](../docs/protocol/20-damage.md) 第 2i 节。
- 共用魔力/补偿与致盲：真实的低语者节点通过可选模块喂通用 Game 引擎。
  按类型命中保留守护/ES 顺序；被接受的技能/瓦尔消耗、持续药剂生命复制与补偿共用既有资源写入方，
  在魔力回复之前同步清理致命点燃。
  按时/烟雾/被动光环/狂怒的致盲在普通与自身拥有接触及呈现之间使用同一个最强值。
  两个确定性的网关事实覆盖资源、付费退还、发送闸、转移与退役组合，
  含不同 Game 的功能到期/结束支付与被阻挡输出之前的回复药剂准入。
  历史 `57053948` 在 `cc0f9c34` 上通过 36 项原生控制；合并后的更新主线需要全新的精确 head 验收。
  它当前的召唤物/连接/脚步角色保留主线的节奏，并与资源写入方共用生成所有权。
  被接受的召唤物到期与玩家/自身击杀玩法先于 await；中性输出能在替换后存活。
  排队的任务/天赋/装备投影保留更新的进度；被接受的充能事件保住实时周期时间。
  所有充能 buff 消费方在发送闸投影并确认，避免过期的缓冲 ticker 输出。
  延迟的任务击杀保留同步的阶段/奖励输出；冻结与持握移动的准入共用密码闸。
  分组确定性控制覆盖它们在跨不同 Game 替换时的待发与被接受输出。
  见 [低语者](../docs/game/whisperer.md)。
- Attack-speed admission: gem attacks now use the supported attack speed (gem and support stats plus the inherent
  GrantedEffectsPerLevel multiplier) for both the admission gate and the swing interval, instead of mixing it with
  `SkillAttackSeconds`. Tornado Shot's -20% and player/bow attack speed apply. Projectile speed adds the player's
  `projectile_speed_+%` to the skill's `base_projectile_speed_+%`. Automated pace and projectile-speed checks pass;
  native Burning Arrow numeric controls confirm integer final-speed rounding before duration, while broader native
  admission checks remain pending. Code: `GemSupports.Rules.cs`, `Combat.cs`, `ProjectileSpeed.cs`.
- Lifetap Support: supported skills pay their whole Mana cost as Life, including the 300% cost multiplier; the paid
  Life is accumulated against `support_lifetap_spent_life_threshold`. Reaching it grants the 4 s (plus quality) Lifetap
  buff, whose more damage applies to supported hits and whose 20% increased Life recovery applies to flasks.
  Vaal uses pay the same Life cost. The buff is presented through the own-aura send gate; the hidden spent-Life buff
  and minion buffs are not sent, and damage over time does not receive the more multiplier.
  Automated test covers Tornado Shot with level-1 Lifetap: 9 Mana becomes 27 Life, the first use grants the buff,
  and the hit and flask bonuses apply. Code: `Combat/Lifetap.cs`, `Player.cs`, `ConditionalSupportDamage.cs`;
  docs: [25](../docs/protocol/25-gem-supports.md).
- Blood Magic: `keystone_blood_magic` (the passive node and item-granted keystones such as Malachai's Awakening and
  Tukohama's Fortress) converts every skill's whole Mana cost to Life in `PayMana`, without Lifetap's multiplier,
  counter or buff; a skill whose Life cost is not below current Life is refused, and reservations stay on Mana.
  Automated test: Tornado Shot's 9 Mana becomes 9 Life with Mana unchanged. Code: `Player.cs`;
  docs: [25](../docs/protocol/25-gem-supports.md).
- Bow projectile counts: actual bow attacks now read `number_of_additional_arrows` and
  `number_of_additional_arrows_while_main_hand_accuracy_is_3000_or_more` on top of the generic projectile additions, so
  bow/quiver modifiers and passives such as Master Fletcher add primary arrows. The arrow inputs are gated to a real
  bow and do not reach wand, spell or melee projectiles; the selected-skill panel also publishes the arrow total.
  The terminal secondary group is a separate change (implemented in #549).
  Automated tests cover the bow/wand separation and the panel rows; no native check yet.
  Code: `GatewayProjectileModifiers.cs`, `GatewayProjectileDefaults.cs`, `ProjectilePanel.cs`;
  [RE count inputs](../docs/re/projectiles/count-stat-inputs.md) section 3.
- Tornado Shot terminal secondaries: each primary arrow pierces its contacts and emits three
  `TornadoShotSecondaryArrow` flights at its endpoint, spaced around the client's native terminal RNG ray
  (`[sequence, mother-derived byte, owner, repeats]`). The client predicts the primary and secondary arrows itself
  (Frida: seven local and seven server `TornadoShotArrow` objects for one use), so the server hides both and only
  simulates their damage; one secondary per set can hit an enemy, and the children keep
  support Pierce/Fork/Chain. Their range uses the client's 0.3 s secondary action lifetime (a Frida trace caps the
  final callback at 300 ms), so projectile speed scales it, and Point Blank/Far Shot distance includes the primary
  leg. Tests cover the profile, the client RNG vector, the 300 ms lifetime and the continued PB/Far Shot distance; the
  2026-11-02 live check confirmed the single primary origin, the fan and the damage.
  Code: `GatewayProjectileSkillProfiles.cs`, `ProjectileStages.cs`, `ProjectileTypes.cs`, `Projectiles.cs`;
  wiki [Tornado Shot](https://www.poewiki.net/wiki/Tornado_Shot),
  [RE terminal geometry](../docs/re/projectiles/tornado-geometry.md).
- Petrified Blood: the 35% Mana reservation buff, its hit-only Life-loss split at half Life, the four-second deferred
  loss, the non-flask recovery cap at Low Life and the Life cost above it are implemented. Life leech instances and
  the client's leech display keep the full maximum-unreserved-Life cap; they are not removed at Low Life, but their
  actual recovery is capped like regeneration and Recoup. Recoup is based on the hit before Petrified Blood and its
  four-second instance also keeps running at Low Life; a level-21/20-quality gem's 30% deferred share is offset by 30%
  Recoup as Life over the same four seconds. The invisible life/mana/energy-shield leech buffs are presented with the
  full per-minute rate, so the client can draw the leech overlay; only the actual pool update is capped. The deferred
  rate is presented with invisible BuffDefinitions 1796 (`slow_life_loss_from_hit_damage`), drawing the dark life-orb
  overlay for the loss still owed.
  Low Life uses the shared 50% default (`low_life_threshold_%_override` then `low_life_threshold_%`), not a fixed
  half of maximum Life; the 3.29.3b Life mastery `Life4` sets the override to 55%. The client's own
  `life_recovery_per_minute` is published as 0 inside a 250 ms regeneration margin of the cap and restored farther
  below it, so its pool prediction cannot cross the boundary.
  Activation, deactivation, saved intent across areas, death cleanup and the own-aura/received-buff presentation share
  the existing aura send gate; the buff is not an aura and the aura restriction leaves it alone.
  Not implemented: the buff-specific reservation-efficiency rows, the client tooltip's Life cost, and any non-hit
  Life loss (damage over time and the already-deferred loss are not protected).
  Automated tests cover the split, level-1/level-20 deferral, the threshold, the recovery cap (including a leech
  instance retained above Low Life while its recovery is capped), and the Life cost; no native check yet. Code:
  `Combat/PetrifiedBlood.cs`, `Combat/LowLife.cs`,
  `Modules/Core/Game/Combat/PetrifiedBlood.cs`; docs: [20](../docs/protocol/20-damage.md) section 2i.
- Shared Mana/Recoup and Blind: actual Whisperer nodes feed generic Game engines through the optional module.
  Typed hits preserve Ward/ES ordering; accepted skill/Vaal spending, continuous flask Life copying and Recoup
  share the existing resource writer, with synchronous lethal-Ignite cleanup before Mana recovery.
  Timed/smoke/passive-aura/Rampage Blind use one strongest value across ordinary and owned contacts and presentation.
  Two deterministic Gateway Facts cover resource, paid-refund, send-gate, transfer and retirement composition,
  including distinct-Game utility expiry/End payment and recovery-flask admission before blocked output.
  Historical `57053948` passed native 36 controls on `cc0f9c34`; the combined newer main needs fresh exact-head acceptance.
  Its current minion/link/footstep roles retain main's cadences and share generation ownership with the resource writer.
  Accepted minion expiry and player/owned-kill gameplay precede awaits; neutral output survives replacement.
  Queued quest/tree/equipment projections retain newer progress; accepted charge events leave live periodic time intact.
  All charge-buff consumers project and acknowledge at the send gate, avoiding stale buffered ticker output.
  Deferred quest kills retain synchronous phase/reward output; freeze and held-move admission share the cipher gate.
  Grouped deterministic controls cover their pending and accepted output across distinct-Game replacement.
  See [Whisperer](../docs/game/whisperer.md).

- 已选受支持法术面板：最终的已连接辅助伤害替换法术与继承的手部范围，
  清掉已移除的类型，并按受支持施法时间更新暴击前平均/DPS。
  八个管线控制覆盖伤害、投射物、自加伤害、宝石等级、类型禁止、连接生命周期
  与一个带已装备武器的无命中的法术。十四次原生抓取覆盖十二个标准夹具阶段：
  十一个正向格式器/自身技能核对与一个技能移除核对通过严格标识核对。
  残忍与正义之火有显式零命中字段与视觉上被抑制的行；它们的零值行格式器证据仍是部分。
  解除连接/重连用的是宿主移除与重新装备，不是原地修改。
  重登恢复基线与精确物品内容；原始复制的存档与仓库未被改动。
  官方暴击加权 DPS、持续伤害与特殊技能机制仍分开。
  代码：Gateway（`PlayerSkills`）、`SpellPanelStats.cs`；测试：`SupportedSpellPanelTests.cs`。
  文档：[11](../docs/protocol/11-skills.md) §9。

- 普通百分比属性：按类型与全属性的增幅在完整的职业/被动/激活物品固定桶上作用一次。
  池、装备资格与宝石需求共用由此得到的最终属性。
  算出的零属性发布零；未激活物品不贡献，镶入宝石需求不满足会让它的宿主再次变为未激活
  （2026-10-29 恢复主线规则，[19](../docs/protocol/19-gems.md)；移植版曾让宿主保持激活）。
  四十七个新自动化用例与 63 项定向核对覆盖两种模式、真实的 Utmost 来源、
  带符号/零修正、依赖池、来源移除/恢复、最终开发覆盖与武器切换。
  三十四项原生控制（每模式 17 项）验证最终值、显式零、派生池、物品激活、
  来源移除/恢复与重登后激活武器集的持久性。普通火球输入在 INT 68 时成功，
  在 62 对其 67 需求时被拒，恢复后成功（那些控制早于恢复的宿主停用：现在宿主会随它的宝石一起变为未激活）。
  来源/运行时哈希与原始存档保持；隔离的物品内容/格子回到它们的规范状态。
  供给来源控制不认证传奇获取。半进位分数取整遵循固定 PoB；官服取整、
  带符号装备循环、属性转换、条件/更多算子与佣兵闸仍未做。
  宽位最终求值不主张任意裸 int 来源聚合都无溢出。
  六个普通池系数在固定生命/魔力桶里使用那些已完成的属性与实际等级，在最终池增幅之前：
  带符号 `life_per_level`/`mana_per_level`、每十点敏捷/智力生命、
  每四点敏捷与每两点智力生命。
  原生 -12/-6/-3 的每级生命惩罚从固有成长中扣除；属性来源只计完整组一次。
  既有的按力量魔力、普通半数与装备 ES 转生命固定值保留它们的顺序。
  一份开发快照整体替换系数，含零；命令重建/钳制池，并在发送闸发布受影响的资源，同时保留魔力保留。
  全部 71 个新用例（45 个纯/原生来源与 26 个网关）加 82 个既有回归通过：153 项定向，无跳过。
  完整网关 3874/3874 无跳过通过；本次会话中未变的更早 Core 648/Data 37 仍适用
  （适用用例 4559；这些改动后只重跑了网关）。那次运行的钩子记录七次真实未观察到的
  已释放 NetworkStream 故障：六次 `MonsterKilled` → `Send`，一次 `NotifyMonsterHit` 经投射物命中
  路径到 `Send`。它的 390 条首次机会记录是分开的；没有出现诊断错误/截断/命中上限或新的池来源
  路径。测试通过不意味着后台执行无故障；新的原生核对仍待做。
  既有的最终池下限与固定 PoB 的最终取整不同；官方分数一致未验证。
  真实修正/隔离被动夹具不能证明获取、事件被动准入或完整转换节点，
  这六个输入也不完成生命/魔力行或条件/更多池算子。
  每十点最终力量给物品 ES 与每十点最终智力给闪避现在一次性并入它们各自的普通 INC 桶。
  原生系数 1/2、完整属性分组、槽位/盾牌排除、IR 与带符号宽位抵消有
  33 个纯用例与 14 个双模式网关用例；实际命令覆盖整来源零、ES 钳制与不回满。
  以及重载：ES 103/104/124/135/124/113/135，EV 15/16/22/26/22/18/26。
  六个容量增幅在 487–749 ms 内保留已受损的当前 ES；四次移除把满 ES 钳到 124/113。
  两次全新进入把当前/最大 ES 恢复到 135。已完成的 World 更新把每个目标绑定到它的实际来源动作。
  获取、完整的塑界之触与防御行格式器绑定仍未做。
  固有 ES 现在使用完整的十点最终智力分组，修正了原先被翻倍的 `/5` 项。
  静态原生智力帮助文本与固定 PoB 一致；精确的官方分数运算仍未验证。
  这些新控制覆盖修正后的合计、不回满、钳制与重载；更早的原生合计仍是历史。
  修正后的充能/满 ES 条件与原生排队发送调度仍需分开验证。
  十一个新增基线与已选组合在 216 项定向用例中通过。
  完整网关 3939 覆盖所有更新过的测试，含该过滤器略过的百分比属性与 tick 用例。
  未变的更早 Core 648/Data 37 仍适用；没有测试被跳过。
  保留的诊断故障与原生/持久化限制在下面的防御参考里做了限定说明。
  五个原生禁止现在只移除固有的力量生命/近战、敏捷命中/EV 与智力魔力/ES；
  那两个窄生命/魔力标志与它们的宽对应项重叠一次。最终属性、需求与物品组
  项仍然生效。一份完整裸开发快照支持零替换与受影响资源的发布。
  全部 16 个完整原生向量在双模式自动化控制中保留了它们的伴随属性与存档负载。
  八十个新用例通过（64 个纯/原生来源，16 个网关）；全新的完整网关 4019/Core 648/Data 37 无跳过通过。
  它的钩子记录六次真实的已释放流 `MonsterKilled`→`Send` 任务故障与 386 条分开的首次机会记录；
  没有捕获到新的标志路径或诊断错误/截断/命中上限。这不是「无故障运行」的主张。
  原生标志发布/重载与全来源立即生命发布仍未验证；普通装备生命
  仍在既有的回复 tick 上钳制。Key18620 没有核对过的直接来源，也不是 IR 别名。
  代码：`CharacterStats.cs`、`ItemModStats.cs`、Gateway（`CharacterStatSources`、`PassiveBonus`、`ClassAttributes`）。
  文档：[11](../docs/protocol/11-skills.md) §4、[13](../docs/protocol/13-items.md) §2d、
  [按属性缩放的防御](../docs/protocol/20-damage.md#player-energy-shield-2026-10-25)。

- 武器本地伤害与时机：激活武器供给一份带类型的报文，含乘法物理品质、
  本地固定/增幅伤害、无物理/品质禁止标志与只应用一次的本地攻速。
  普通近战/远程与受支持攻击共用实际上下文；最终的按类型行清掉已移除类型。
  自动化控制覆盖武器切换、未激活手本地排除与共用的转换/狂怒组合。
  池/充能/时机条件与盾牌基础格挡也排除未激活或已禁用的装备。
  二十四项原生发布控制在标准/无情下通过；独立的本地词缀/品质提示
  对上 36-70 物理伤害与每秒 1.80 次攻击。来源/DLL 哈希与背包保持。
  权威副手交替与替代品质仍分开。
  本地暴击「先固定后增幅」的顺序在下面暴击实现里有自动化覆盖；
  前面的原生武器核对不验证该改动。
  受支持接触的组合保留自动化覆盖，需要它自己的原生核对。
  一个 Core 裸转换历史接缝已可用，21 个新路径用例与 12 项既有报文控制通过。
  完整 Core 套件通过 648 项测试。`PlayerHitDamage` 现在跨普通/受支持命中共用同一个访问类型增幅桶、
  继承的辅助/光环更多、最终目的地禁止与通用充能更多。
  它保留显式上下文、来源优先级、无转换浮动分组与远程释放快照。
  四十三个新网关用例与 340 项定向核对通过，随后是全部 3602 项网关测试与 37 项数据测试。
  三十二项原生数值控制在标准/无情下通过：24 项原始显式字段控制与八项
  普通弓稀疏零控制。第一个稀疏标准基线是回溯的；接下来七个是前瞻的。
  真实的自然 getter 零与显式线上零或隐藏行渲染不同。
  四项恢复/重登控制、184 项完整物品/插槽确认、原始存档保留与 714 项未变
  来源/运行时输入通过。客户端已关闭，正常 `data/saves.db` 已恢复。原生发布不
  认证权威命中接触、暴击加权 DPS、条件/端点/异常/DoT 或官方取整。
  既有的已释放流清理故障与分开的原生连接关闭仍记录在证据里。
  代码：`ItemWeaponStats.cs`、`DamageStats.cs`、`SkillCatalog.cs`、`SupportedAttackDamage.cs`。
  文档：[20](../docs/protocol/20-damage.md) §2a。


通用的[投射物框架](../docs/game/projectile-framework.md)拥有生产环境的宝石/弓路由与
唯一的 30 Hz 运行时。目录兜底提供基础飞行；注册的技能定义提供特殊规则，
并可要求显式配方。

[客户端投射物研究](../docs/re/012-projectile-execution.md)现在把共用执行、
客户端/服务端输入契约与六个普通见证与当前服务端实现分开。
最终的绑定、有限算术、本地接触/历史、延续与已选技能阶段衔接都已记录。
公开标量/向量与合成观察核对与保留的原始指令控制分开。
游戏 TLS 状态/顺序、官方火球直接/区域结果路由与实际有效输入时机仍未做。
这是一份研究基线，不是新的实测技能验收，也不是完整的原生/服务端一致。

- 奥术之力（#364）在当前攻击与伤害面板之间共用一个「最高合格比例」消费方。
  普通「心灵之冠」、低语者既有消费方与法杖精通使用那六份现代属性契约。
  独立授予不叠层；技能基数加品质仍在最大值比较之前相加。
  武器本地伤害现在在普通与受支持攻击中缩放；受限类型从技能面板清空。
  自动化用例覆盖实际直接/区域目标生命损失、快照、精通退还与心灵之冠的持久/移除。
  全部 24 个已清点数值技能常量与三条品质记录有计算/数据核对。
  这些核对不启用不受支持的技能、变体、佣兵或 Phrecia 分配。
  铁意志/力量的来源、缺失的上游法术输入与无关汲取在此消费方之外。
  原生用户核对确认普通心灵之冠比例、动能爆破不叠层与火球在伤害面板中的隔离。
  动能爆破的绝对伤害暴露了一个效果偏移解码错误；裸 4000 现在解析为 140%，不是 40%。
  修正后的绝对伤害与精通生命周期仍需原生验证。
  见 [范围与覆盖](../docs/game/arcane-might.md) 与 [20](../docs/protocol/20-damage.md) §2c。
- 无情按连接解析原生技能/宝石、被动与词缀的替代版本，含零值覆盖。
  同一网关里的普通角色保留普通目录。
  硬核死亡对旧连接是终结性的；排队的升级不能复活它。
  硬核迁移与永久无情死亡有自动化生命周期覆盖。
  更早的原生核对是针对原始 fork 的；当前集成上的原生核对另外记录。
  原生技能/被动/词缀替代与迟到动作守卫有自动化覆盖。
  主线的投射物击杀路径保留无情奖励、宝石经验与激活功能药剂限制。
  同步任务/宝石提交保留硬核终结进度守卫；组合回归通过。
  见 [赛季模式](../docs/game/league-modes.md)。
- 恩典期在城镇与藏身处之外授予 60 秒进入保护。
  第一次移动、一个技能、一瓶药剂或到期都会结束它；受得伤害、敌对选目标与 RF 伤害都被抑制。
  原生 buff 用浮点秒做倒计时；图标已实测，计时器复测待做。
  见 [11](../docs/protocol/11-skills.md)「区域进入恩典期」。
- 瓦尔之魂（#308）：真正的自身击杀汇聚到有上限的共用 ActiveSkills 行预算。
  创建与技能更新发布已证实的原生 Actor 列表；自动化的线/派发核对通过。
  重复、来源/消耗变化、移除/重加、范围、阻止、排队发送与瞬时生命周期都有覆盖。
  资格与致命变更在死亡通知之前提交，含共用命中与正义之火。
  瓦尔光环消耗使用按实例的 Aura 类型与被接受的辅助，并有显式开发替换优先级。
  玩家魂在生命为零时提交损失；延迟的死亡通知保留实际的复活所得。
  瓦尔宝石/辅助需求在装备通知被持握时使用当前装备快照。
  基线 `939b44fe` 上的来源核对保留宽位裸属性、魔力、池标志与受支持技能支付。
  技能记录 bit1（0x02）现在在两条查找路径里都用目录裸已存使用数保留原生瓦尔魂输入。
  解析的创建/更新与真实镶入测试覆盖零消耗/余额、普通搭档与未激活宿主。
  一个固定的 3.29.3b 会话通过 42 项收集/计数核对：真实击杀、上限、消耗、零容量与阻止。
  原生所需魂与管理者消费方一致，含自然宝石升级与不受支持输入的拒绝。
  构建与三个套件都通过；与 PR #294 的精确私有组合也通过。
  分配与容量是文档化的 LocalExile 策略，不是官方算法一致的主张。
  原生重复/移除、死亡/重登/转移、光环消耗与持握通知组合仍未验证。
  不受支持的瓦尔施放在动作回显或冷却占用之前被拒，含有效零消耗。
  瓦尔大地震击现在有共用执行器、原子的魂/狂怒支付与可暂停的阻止计时器。
  攻击就绪性在支付/动作占用之前提交，含以物体为目标的 flags bit0。
  其他瓦尔执行器仍是显式拒绝；那次有限的模块化原生会话没有重复完整的瓦尔矩阵。
  执行模型与来源所有权见 [巨兽](../docs/game/behemoth.md)。
  见 [27-vaal-souls.md](../docs/protocol/27-vaal-souls.md)。
- 大地震击现在按它数据动作命名的前向波命中，而不是一个圆：那些 ActionTypes 动作是
  「DistanceScaledGroundSlam」的技能（GrantedEffects 的 GroundSlam、GroundSlamAltX、GroundSlamAltY、
  GroundSlamRoyale 与 PlaytestSlam）从玩家站位发出一个扇形，其前沿推进到技能距离，每只怪在前沿到达时被命中，
  其余的扫描在死亡、换区域或连接关闭时丢弃。瓦尔大地震击自己的技能是另一个动作，所以它 40 格的放射爆炸不变，
  冰霜新星的环、旋风、滚烫岩浆与其他所有圆也一样。玩家的宝石解析到 GrantedEffects「GroundSlam」；
  那个 +40% 最终半径与 4 次近战重复属于旧的「ExileGroundSlam」行，没有任何 GemEffects 行授予它。
  表里没有波长度、基础角度或速度（四个 `ground_slam_*` 形状属性不被任何玩家属性集与 3.29 表的任何 Mod
  持有），所以 `GroundSlamWave` 存了三个估计值——在通用 12 格区域半径下 60 格的长度，按技能自身半径
  与它的差距移动；半角 30 度（`ground_slam_cone_angle_override` 命名的 60 度锥的一半），
  按宝石的 `ground_slam_angle_+%` 增长；以及每秒 60 格——留待实测校准。
  波的近距离伤害（`groundslam_damage_to_close_targets_+%_final`，在宝石自己的属性里确实是真值）仍只由
  瓦尔执行器施加。某次实测会话的 0x0024 物体使用走同一次扫描，波的起点已经是走位后的挥击位置，
  所以它的物体使用与扫描之间的差值是服务端丢掉的挥击命中（80 次挥击回显里的 22 次），不是缺了区域的一部分。
  客户端事实（2026-11-07 实测）：点一只怪并立刻走开，客户端会播完整挥击并画出波，
  所以客户端把 Move 排队而不是取消攻击；服务端当时只凭动作计数器就丢掉了付费命中。
  一次宝石近战挥击（`holdsItsSwing`：是近战、非引导的攻击）现在在动作计数器已改变时仍保留它的命中，
  只有死亡、换区域、暂停、冻结或眩晕的命中恢复才结束它。这影响每一次宝石近战攻击，不只是大地震击
  （劈砍、横扫、大地震击的替代品质形态与瓦尔大地震击自己的爆炸都走同一路径），
  且这类挥击的接触现在来自它自己的骨架剪辑（`GemAttackContactDelay`：groundslam_maul 是攻击的 0.633，
  而该职业的 attack1_maul、也就是默认攻击的挥击是 0.692）——两者都与扫描一起记录。
  尚未用真实客户端核对。
  剩余：客户端把它的 Move 排到剪辑的切入（Lockstep.cs），而服务端在 Move 到达时就走了，所以挥击期间
  服务端位置会领先最多约 0.3 s；把 Move 持到接触时刻（`HeldMove`）能让它们对齐，需要 `Messages.cs`、
  `PlayerAilments.cs` 与一个 `ClientState` 字段。
  代码：`SkillArea.cs`（`GroundSlamWave`）、`Combat.cs`（`AreaSkillHit`、`GemAttackContactDelay`）、
  `AnimationContacts.cs`（`SkillAttackContact`）。
- 战吼释放（坚毅战吼，唯一战吼执行器）：战吼时间像客户端一样跟随战吼速度与动作速度，
  其释放在其半径内跑共用战吼效果：消耗尸体的尸爆与任务箱子开启（Deathrattle）。
  爆炸吃玩家的全局伤害增幅与转换。
  每一个单次战吼，对每个职业（坚毅、鼓舞、恐吓、震地、炼狱、先祖、将军与战法师战吼），
  现在都用通用的单次冷却，并把 `warcry_cooldown_speed_+%` 像客户端那样加进它的恢复；
  「瞬发技能与冷却」下的实测冷却核对早于战吼走上这条路径。
  网关覆盖；尚未用真实客户端核对。
  每具爆炸的尸体显示通用血花；原生美术未追踪。
  全局固定自加伤害与固定暴击几率（wiki 的 Explode 页两者都计）、命中效果与客户端 0.22 s 波延迟未建模。
  代码：`Warcries.cs`、`UseChargeSkillCore`、`SingleUseCooldown.cs`。
  文档：[11](../docs/protocol/11-skills.md) 第 10i 节。
- 打击选目标：近战打击的灵魂打击（`melee_attack_number_of_spirit_strikes`）命中客户端挑的怪物，
  按它的扫描与播种洗牌镜像，作用于宝石打击与默认攻击。
  有 `melee_strike_skill_strike_previous_location` 时，打击也命中上一次使用落地的位置，
  服务端在第一次使用时发 `skill_location_memory` buff，客户端据此记录后续使用并自己打上一位置。
  网关覆盖；尚未用真实客户端核对。
  未镜像：客户端的视线判定与它在超过 20 个候选里的选择。
  对类型为投射物的打击宝石不适用（冰刃、闪电打击、熔岩打击、野性打击及其蜕变宝石，
  除了没有 Projectile 类型的「深渊冰刃」两者都吃）：
  服务端只为它们发投射物、没有近战命中，所以客户端为它们画的灵魂打击不造成伤害。
  代码：`StrikeTargets.cs`、`Strikes.cs`。
  文档：[11](../docs/protocol/11-skills.md) 第 10d 节。
- 被动敌人光环：被动 BuffTemplates 的敌人光环把它 buff 放到半径内的敌对怪物身上，
  它的命中转换转换它们的命中（火中而生）。
  服务端暂不施加的属性所在的光环既不发送也不施加。
  网关覆盖；尚未用真实客户端核对。
  代码：`PassiveBuffAuras.cs`、`MonsterSwing`。
  文档：[11](../docs/protocol/11-skills.md) 第 10e 节。
- 被动触发技能：先祖指挥官的精魂酋长选项在传奇怪物暴击、野蛮打击或被格挡的命中上触发，
  花掉共用的 20 s 冷却，给酋长 12 s 的领导 buff（含技能效果与召唤物持续增幅），
  其间该冷却不恢复；死亡结束 buff。
  Kahuturoa 的抗元素与混沌命中护甲份额与 Kiloava 的格挡时生命、魔力与 ES 生效。
  服务端怪物没有基础暴击几率，所以 Kahuturoa 的暴击触发只在被强制永远暴击（`/mob_mods`）的传奇上触发，
  正常玩法里永不触发。
  酋长作为他的召唤物在玩家身边升起，打附近的怪物直到 buff 结束。
  网关覆盖；尚未用真实客户端核对。
  未建模：酋长第一个之后的技能、技能的记录（客户端只显示 buff）、Utula（乌图拉）的汲取与 Rakiata 的印记触发
  （见「尚未实现」），以及把 buff、召唤物或冷却带过换区域。
  代码：`PassiveGrantedSkills.cs`、`SpiritChieftains.cs`、`MonsterSwing`。
  文档：[11](../docs/protocol/11-skills.md) 第 10f 节、[20](../docs/protocol/20-damage.md) 第 2e 节。
- 会战斗的玩家召唤物：属于玩家队伍的召唤物，对客户端标记为他所有，跟他走，去打两者附近最近的敌对怪物，
  用他的召唤物修正挥第一个技能（它来自 DefaultMonsterStats `MinionDamage`、友方表的基础命中），
  它的击杀给他经验与掉落；区域存档、他的区域命中与击杀充能把它排除在外。
  这个第一技能控制器服务精魂酋长；魔像用下面它们自己的控制器。
  网关覆盖；尚未用真实客户端核对。
  代码：`PlayerMinions.cs`、`MinionAttacks.cs`、`RunMonsterAi`。
  文档：[11](../docs/protocol/11-skills.md) 第 10h 节。
- 生命补偿在接下来 4 s 内返还每次受击伤害的一份（[20](../docs/protocol/20-damage.md) §5）。
- 先祖指挥官的防御：`damage_reduction_rating_from_body_armour_doubled` 在增幅之前把身体护甲的护甲翻倍
  （有铁之反射时还包括它转换来的闪避）。
  `missing_unreserved_life_%_gained_as_life_before_hit` 在每次未被闪避的怪物挥击与开发命中之前获得那份
  缺失生命，含被格挡的（生命保留未建模）。
  `movement_speed_cannot_be_reduced_below_base` 把移动速度合计下界钳到 0，和客户端按同一个标志做的一样。
  网关覆盖；尚未用真实客户端核对（实测之后移到上面的护甲与移动速度条目）。
  文档：[ancestral-commander.md](../docs/game/ancestral-commander.md)。
- 光环恢复引导修正：镶入宝石的需求在池初始化期间不再让整个宿主物品失效。
  装备在逐个宝石校验之前贡献属性；
  存档的光环在城镇/藏身处往返时仍是候选。
  回归覆盖新连接的精准/清晰装备与全部 17 个受支持自身光环。
  待实测复测。
- 光环生命周期：城镇抑制所有光环效果/保留，同时保留按角色的启用意图。
  离开/重登/换角色恢复可用的已记住光环，不重新检查施放支付能力；
  手动激活仍校验魔力。
  死亡清空存档/运行时光环；技能刷新恢复暂时不可用的效果。
  共用的自身光环限制在上面有独立的原生控制。
  纪律的城镇、死亡与宝石宿主生命周期在 LocalExile 里也通过了历史原生核对。
  合并分支的全新往返与其他光环族的往返仍是部分。
  见 protocol/11-skills.md。
- 光环呈现（2026-10-25）：自身光环选择它们各自的 EPKFiles2 美术。
  实测发现只有来源的 buff 会遮住左上角图标；
  来源与受得 buff 实例现在共存，一起恢复、一起结束。
  图标恢复待客户端确认。
  见 [11](../docs/protocol/11-skills.md) 第 13 节。
- 普通光环扩展（2026-10-25，待客户端确认）：坚定、优雅、愤怒、憎恨、
  雷霆、狂信、恶意与嫉妒现在带保留与成对的视觉/HUD buff 激活。
  护甲/闪避、自加命中伤害、获得为冰、法术伤害/暴击与 RF 的更多 DoT 已接通。
  纪律的普通 ES 池与充能在上面单独验证；条件效果仍是部分。
  保留溢出会被拒绝。
  见 [11](../docs/protocol/11-skills.md) 第 13 节。
- 血肉与沙血共用一个已持久化的姿态：重放切换两者而不释放保留；
  `/auras off <name|effectId|all>` 显式禁用它们。沙血修改近战区域伤害与区域。
  血肉与沙血使用当前宝石随距离变化的更多物理命中伤害 / 更少受得命中伤害；
  过时的致盲、全局减伤与物理 DoT 加成已移除。原生姿态切换与关闭通过。
  代码：`AuraCatalog`（`FleshAndStone*`、`RawStats`）、`Game.ToggleAura` / `EnemyDebuffsTick`、`MonsterMods`。
- 被动授予的技能：被动的 `GrantedEffectsPerLevel` 技能按该行的固定等级加入宝石技能，
  带稳定 id、无辅助、品质 0、无需求；它像宝石的一样施放、保留、存档与结束。
  在其他授予技能（召唤物、其他种类）有执行器之前，只列出自身光环。
  被授予的触发技能从不列出，而是从它的触发运行。
  「召唤元素遗物」（圣骑士的元素勇气、普通守护者的不屈十字军）在击杀与对稀有/传奇的命中上掷骰，
  在其冷却之后，无情模式里也一样。
  用于圣骑士的光环选项（坚定、优雅、纪律）；仅自动化覆盖，未用真实客户端核对。
  见 [11](../docs/protocol/11-skills.md) 第 13 节与 [圣骑士](../docs/game/paladin.md)。
- 自身拥有的召唤物：遗物与酋长保持限时；全部 17 个可玩魔像宝石都能召唤、战斗并承受通用敌对
  命中/投射物。魔像跟随、按宝石等级/品质与辅助缩放、授予它们各自的拥有者 buff 并跨区域保存
  激活状态。本地规则：每个已激活魔像在死亡后两个游戏秒返回，前提是它的来源可用。
  全部变体都在真实客户端里召唤过；普通战斗、buff 与重复返回实测过。
  附近的普通光环现在影响自身拥有的战斗单位。部分：精确的官方 AI 优先级、部分辅助交互、
  与其他玩家共享魔像拥有者 buff 与定制的 Boss 伤害。
  见 [11](../docs/protocol/11-skills.md) 第 10h 节；遗物仍在第 13 节与[圣骑士](../docs/game/paladin.md)。
- 连接技能：灵魂、烈焰、吸血、守护、直觉与破坏连接在玩家有 `link_skills_can_target_minions`
  （深度连接）时连接他的一个召唤物；没有队伍玩法就没有别的东西可连。
  连接持续它的时长，每秒耗魔力，出距离 4 s 后或消耗未支付时断开，
  其目标死亡会杀死玩家（退还遗物的被动会无死亡地结束连接）。
  `number_of_active_links` 发给客户端；赦免与战地医术使用
  这些连接。未建模：光束、视线、宝石对目标的效果与队伍盟友。
  仅自动化覆盖，未用真实客户端核对。
  见 [11](../docs/protocol/11-skills.md) 第 13 节与[圣骑士](../docs/game/paladin.md#link-skills)。
- 已保存的光环开关（2026-10-25，待客户端确认）：已激活效果按角色持久，重登、进入区域与服务端重启时恢复，
  受可用宝石与可用保留限制。
  海伦娜的选择器现在收到全部 103 个导出的个人藏身处，含 MTX 模板；
  切换模板现在使用原生选择请求并持久所选的家。
  见 [11](../docs/protocol/11-skills.md) 与 [21](../docs/protocol/21-hideouts.md)。
- 法术被禁用（`your_spells_are_disabled`，格鲁斯库尔的虔信者）：每次法术使用在战吼与
  自身光环准入路径上都被拒，技能记录带 `skill_disabled_by_stat`。
  正在运行的法术光环被挂起并保留意图，来源消失后恢复；燃烧中的正义之火继续烧玩家，
  对敌人不造成伤害。
  对已在运行效果的本地策略；仅网关覆盖，未用真实客户端核对。
  代码：Gateway（`SpellsDisabled`、`ActorSkillDisabledCore`、`SuspendRestrictedOwnAuras`）。
  文档：[11](../docs/protocol/11-skills.md) §10j。
- 被动致盲光环（索拉里斯的虔信者、生于暗影：BuffTemplates `SaboteurBlind`）：35 格内的活着的敌对怪物
  每个玩家 tick 获得 `blinded` buff，死亡、退还或离开半径会摘掉它。
  来自它或烟雾的致盲把它们的命中与闪避降 20%，
  让 `critical_strike_chance_+%_vs_blinded_enemies` 提升玩家对它们的暴击几率（默认攻击、区域
  扫描、脚步技能、投射物），并让 `damage_taken_+%_from_blinded_enemies` 降低它们的命中与点燃。
  命中按它们的命中与暴击几率致盲 4 s（[11](../docs/protocol/11-skills.md) §12）。
  其他被动光环暂时什么也不做。
  仅网关覆盖（一个多神教徒与一个普通破坏者），未用真实客户端核对。
  代码：Gateway（`PassiveBuffAuras`、`TickPassiveAuras`、`Game.Blinded`、`CriticalChanceOn`）。
  文档：[11](../docs/protocol/11-skills.md) §12。
- 被动授予的脚步技能（阿贝拉斯的虔信者：20 级阿贝拉斯之怒）：玩家行走时，圣堂武士跑步动画的每个脚步
  （每 800 ms 两个，随移动速度更快、冰缓时更慢）在玩家脚下爆炸，用技能的美术，
  作为无宝石的区域法术命中 7 格（按法术区域增长）内每个活着的敌对怪物：300-450 火，
  带玩家的增幅、暴击、格挡与通常的命中效果。
  不在城镇、恩典期、被冻结、眩晕或死亡时，也不在法术被禁用时；位移技能的闪现不算走，
  盾牌冲锋的也不算，而 wiki 说它应该触发（一个已知缺口）。
  落地的命中按它 25% 的几率或一次暴击点燃怪物（玩家的点燃见下面怪物条目）。
  半径来自爆炸美术（没有表给出），其他被动授予技能暂时什么也不做。
  仅网关覆盖，未用真实客户端核对。
  代码：Gateway（`PassiveGrantedSkills`、`RunFootstepSkills`、`TriggerFootstepSkill`）。
  文档：[11](../docs/protocol/11-skills.md) §12。
- **投射物修正。** 通用档案在唯一的阶段化运行时里消费已解析的主数量、速度、穿透/分叉/连锁/分裂/回返
  授予与独立禁止。普通弓与目录已知的投射物宝石无需技能提供者即可工作。基础配方是直线飞行，
  不是完整的特殊技能行为。
  回返开启另一个命中准入阶段，并读共用的回返伤害倍率。客户端绘制的法术按客户端种子逐投射物掷它的部分回返
  几率（静态逆向，2026-10-31 实测：回来的正是造成回返伤害的那些），
  并在回程飞出客户端 150 格射程、越过施法者（`projectile_maximum_range_override` 替换它）。回返几率、
  回返速度、接触预算、过冲与地形行为可通过技能定义配置。
  布尔分叉来源授予一代，裸弓永远穿透来源绑定同一个通用档案。
  一个移动实例上限 150 格（15 m），通过 `projectile_maximum_range_cells`；分叉、连锁与分裂
  以新预算开新实例，所以总飞行距离可以超过 15 m。回返保留剩余延续
  次数与原生命；回返穿透全部需要它自己的属性。
  动作速度不改变投射物射程：投射物速度只缩短飞行，`action_speed_-%` 与冰缓/冻结
  加性组合用于攻击、施法与移动节奏；尾风/先驱者来源仍是 #246。
  直接与区域快照把区域增幅/辅助限定在它们的伤害阶段。
  近距离（Point Blank）按飞行距离缩放攻击投射物命中（[投射物](../docs/game/projectile-framework.md)）。
  当已选美术导出原生半径时，区域效果缩放到那个半径；未知美术不缩放。
  被地形阻挡的区域中心被略过，不重采样也不占时间；存活的区域保持连续区间。
  技能爆炸、脉冲、子体、形态、原生美术与专门资源属于技能定义。
  缺失元数据或不受支持的伤害方案在消耗之前被拒，不做通用区域命中兜底。
  [具名技能](../docs/game/projectile-skills.md)实现终端火球爆炸与动能爆破簇。
  燃烧箭用目录飞行，带它 100% 火转换、25% 基础点燃几率（暴击总是点燃）与来自最大生命的活力之火；
  投射物点燃按怪物火抗烧它们，配原生 `ignited` buff（[20](../docs/protocol/20-damage.md) §3）。
  仅自动化；原生点燃视觉与伤害仍需用户自己跑一次核对。
  直接多发在去程与回程分别共用准入；被拒的同批穿过去。
  火球等待耗尽的延续，并给终端受害者一次归类为区域的命中。
  普通动能爆破创建四个偏移爆炸，区域伤害少 40%；聚簇创建三个加数量修正，
  保留一个主爆且不能分裂。
  内部的 AltY 行保留普通动能爆破兼容性，但不确定第三个可玩宝石。
  分离的爆炸/落点半径一次性使用受支持区域属性；独立爆炸可以叠到覆盖到的受害者身上。
  每个动能爆破接触组排除它自己的触发敌人，含延迟爆炸；其他组可以打它。
  自动化核对覆盖大体型触发者、被穿透的邻居与墙/射程分组。网关 actor 半径
  仍为零，所以实测区域覆盖用怪物中心而不是校准过的身体相交。
  动能爆破中心使用固定的缩放圆与随机扇区角度，配独立的首延迟/槽间隔参数：
  立即首个爆炸，然后每槽 1/30 s。这些是用户调过的体感值，不是官方测量。
  次级爆炸用它们自己的小爆美术与已装备 MTX；原生美术缩放到实际爆炸半径。
  技能栏爆炸总数使用已解析的簇配方，含品质与聚簇的投射物转换。
  火球撞墙伤害与默认/冥河报文有回归覆盖；视觉可见性仍未验证。
  飞行保持固定的地形加偏移高度（无网格时裸偏移）；爆炸效果跟随客户端地形。
  玩家的主射以释放高度与瞄准地面中较高者飞行：地形高出它 12 步以上就会挡住它，
  所以瞄准崖下的射击打在崖面上，瞄准崖顶的则越过
  （[框架](../docs/game/projectile-framework.md)「瞄准高度」）。LocalExile 模型 v1，待原生核对。
  官方几何/时机、表面高度与美术等价仍需要客户端验证。
  代码：Core `Projectiles/`、Gateway `Projectiles/` 与 `Projectiles/Skills/`；见
  [框架指南](../docs/game/projectile-framework.md)。
- **瞬发技能与冷却（2026-10-21 实测）。** 迅捷、各纯净与正义之火带
  `skill_is_instant`，所以客户端把它们当作 C→S 0x191 发送并继续走；
  服务端施加它们并回 S→C 0x1ce，后者启动技能栏冷却。
  特拉图斯的剑雨不再卡住客户端（它的多发间隔在记录里），并按其开关运行：
  每间隔一次多发命中玩家周围区域、地面刀刃、每秒消耗魔力直到耗尽
  （2026-11-07 实测：多发与命中核对过；无魔力时的结束还没看到）。
  技能记录保留 GEPL Cooldown 作为原生基数，并携带冷却计数。
  普通单次法术/攻击现在一次性解析玩家与技能的通用恢复，服务端在属性变化与暂停期间保留剩余
  基础时间预算。
  预检在动作/移动/回显副作用之前占位；排队效果保留原来的动作/区域票据。
  真实客户端的冲刺初始 +100%/-50% 恢复、过早裸拒绝且无移动/回显/魔力，以及暂停核对都通过。
  原生正速率缓存在技能属性变化之后需要一次显式冷却状态刷新；
  这在发送闸保留剩余/总基础时间，不再消耗一次使用。
  原生冲刺冷却中恢复变化现在实测通过：一次进入改变速率而基数/总数/进度保留，
  过早输入没有回显/消耗，原生到期后重用成功。
  线、激活/过期/零速率与排队加密发送核对也通过。
  多次使用、同效果重复宝石、共用组、额外使用、绕过、按类型/条件/更多恢复与官方
  tick 等价仍在此切片之外。
  技能记录携带 GEPL Cooldown 与用于冷却计数的属性，服务端在冷却结束之前拒绝技能。
  地面目标 C→S 0x190 现在框定它的 12 字节，并把普通霜闪路由过魔力/冷却准入。
  地面 0x1ce 使用裸瞄准；服务端镜像原生 32/42 格距离限制并结束旧的行走。
  落点要求已捕获的网格与畅通地形、障碍与端点净空。
  动作令牌与待发释放保持不变；其他位置型瞬发位移被扣留。
  实际 Q 输入与一次冷却重试匹配原生/服务端位置，两次各 12 魔力消耗与两次确认。
  过早重试、非瞬发火球与无符号非法坐标请求不改变位置或魔力。
  手工原生实验另外验证了近/远钳制与闪现后旧预测行走结束。
  悬停怪物的霜闪发 0x191 并共用移动准入，带一个绑定物体的 0x1ce。
  它按尺寸的物体候选追踪在距离钳制之前；
  它不会把怪物中心重新当作地面瞄准。
  预检与发送闸都要求一个活着的、已知尺寸、无活动行走的怪物，并重新检查落点。
  非法预检不改变魔力/冷却；后续失效在既有消耗策略下扣留确认。
  自动化实发测试覆盖当前目标/玩家取样、目标死亡/走位、暂停、转移、障碍与卸装。
  四次手工原生物体确认验证了近/远/对角/偏轴端点都畅通。
  反射地形的兜底被刻意拒绝。
  对静止怪物的实际悬停 Q 发出 0x191 与一次绑定物体的 0x1ce，flags 0x20。
  从 `(280,220)` 对 `(300,220)`，原生/服务端位置变成 `(317,220)`，一次 12 魔力消耗。
  0.5 秒时框定的地面重试不改变魔力或位置，也不发额外确认。
  合并的 #197 同步保留基数 3000 ms、总计 3 秒与速率 1。
  生产环境的框定偏轴/远距物体请求匹配原生/服务端端点，各一次 12 魔力消耗。
  框定的移动目标、反射地形、缺失目标与非瞬发请求不改变魔力或位置。
  它们不发确认，非法组合控制让原生冷却项保持为空。
  活动行走期间的生产移动尚未测量。
  霜闪伤害、冰缓地面、依赖敌人的恢复与共用闪现冷却组仍不受支持。
  技能被重发或宝石重新镶入时技能栏保留它的槽位（保存的栏以 0x53 发出）
  （[11](../docs/protocol/11-skills.md) §9、§11）。
  槽位的「始终攻击但不移动」（C→S 0x4f）被保存并在 0x53 里发回，所以它保持勾选。
- **光环（简单集已实测）。** 保留从 GrantedEffectsPerLevel 读取（`ManaReservationFlat`
  / `ManaReservationPercent`），被被动/精通保留效率降低，作为生命池的已保留魔力下发（并在每次 0x1b1 魔力更新里）。
  光环技能在施放时开关；元素/火/冰/闪电纯净、清晰、活力、精准、迅捷、
  坚定、优雅、纪律、愤怒、憎恨、雷霆、狂信、恶意与嫉妒施加它们宝石的等级与
  品质效果及 buff 图标；它们的 3D 效果（`affected_player.epk`、`faster_attack.epk` …）还不渲染。
  骄纵施加它的敌人减益：半径内每只敌对怪物带 BuffDefinitions 1331 并承受它的
  `physical_damage_aura_nearby_enemies_physical_damage_taken_+%`（20 级 19%），在 4 s 后线性升到 `_max`（39%），
  两者都按光环效果缩放；离开光环时斜坡重置，并喂命中与物理持续
  伤害。2026-11-01 实测：`Auras: Pride (0 flat, 50%)`、一只恐鸟身上的 0x01c1 第 1331 行、关闭时的 0x01c2，以及
  死亡把两者都清掉。
  技能栏的激活框与旋转圈现在收到 buff 的技能 id/动作哈希关联（0x1c1 bit3），
  含进入区域恢复；自动化核对通过，图标实测验证待做。
  永久法术 buff、五个传令、蹦跳机器人（Skitterbots）与时空裂隙共用来源/保留调和。
  纯净授予元素异常免疫；极地护甲给出静止减伤、冻结免疫与冰缓
  尾迹；风暴之盾授予盾牌法术格挡、感电免疫与格挡反击。传令的击杀/中毒触发用
  普通伤害/召唤物管线。时空裂隙从四秒前召回位置/资源，有原生证明。
  附近的玩家光环用施法者效果到达盟友与受控战斗单位；重复的族按每个属性的
  最强值选取。生命保留使用独立预算、傲慢与池/回复上限；诅咒光环用
  诅咒生命周期。蹦跳机器人施加缩放后的冰缓/感电并触发已放置的陷阱/地雷，让一个地雷重新武装一次；
  完整的陷阱/地雷技能专属负载、旗帜/瓦尔执行器与苦难的零售病毒衰减仍是部分。
  （[11](../docs/protocol/11-skills.md) 13）。
- 召唤/地雷/陷阱的输入与清理可用；特殊负载仍是部分（[11](../docs/protocol/11-skills.md) 10h）。
- 诅咒/印记有上限与战斗效果；反射与特殊负载仍是部分（`MonsterCurses`）。
- **图腾（数据给出图腾变体的每个效果，2026-11-08）。** 图腾技能的使用会在召唤时间之后，在瞄准格召唤它的图腾：
  一个 team-0 的怪物对象，生命为该变体按宝石等级需求的召唤物生命，
  带它的持续与上限（1 加 `number_of_additional_totems_allowed`；超出则最老的死亡）。
  客户端给出 `SkillTotemVariations` 行的每个效果都会立起它的图腾——共 122 个：攻城、破片与
  炮击弩炮、动能雨、冲击波、圣火、灼热与冰寒之链、诱饵、吞噬、先祖
  守护者与战酋、瓦尔先祖战酋、回春与瓦尔回春。在此之前，`TotemVariety` 还
  要求效果在 `TotemAuras` 里，而那里只列了回春图腾，所以弩炮的使用施放了技能的
  普通投射物、什么也没立。这 122 个都是图腾技能，所以没有普通技能改变它的施放。
  数据里没有 `base_totem_duration` 的技能（弩炮与近战图腾；只有法术图腾有）
  把图腾保留 `PermanentTotemSeconds`，即一天的游戏秒，而不是一个零持续、在客户端能画出来之前就到期。
  图腾与怪物分开存在，所以区域命中、怪物 AI、掉落与区域存档从不看到它们。
  图腾在生命 0、持续结束、被替换或玩家死亡时死亡，一秒后离开。
  回春图腾的光环在半径内回复玩家生命，配 buff 172。
  `damage_removed_from_your_nearest_totem_before_life_or_es_%` 把怪物或 `/hit` 命中在
  减伤之后的那个份额移给最近图腾的生命（先祖森林、卡奥姆之灵）；由网关测试覆盖。
  尚未做：图腾不开火（没有弩炮射击、没有圣火喷射——图腾自己的技能不运行）、怪物
  AI 不选图腾为目标、落点射程与服务端图腾碰撞未建模；未用
  真实客户端核对（[11](../docs/protocol/11-skills.md) §10g）。
- **瓦尔光环（2026-11-08）。** 瓦尔光环的使用花掉它的魂，然后落到投射物路径，所以
  瓦尔清晰、优雅、纪律、迅捷、三个不纯、瓦尔极地护甲与瓦尔正义之火为一个
  什么也不做的技能付了费。`VaalAuraCatalog` 给每个它的客户端 buff 行——158 `vaal_aura_no_mana_cost`、159
  `vaal_aura_dodge`、160 `vaal_aura_energy_shield`、161 `vaal_aura_speed`、971/972/973
  `fire_`/`cold_`/`lightning_impurity_buff`（最大抗性与其异常免疫）、2175
  `vaal_arctic_armour`、1160 `vaal_righteous_fire_duration`——`CastVaalAura` 按技能
  自己的 `base_skill_effect_duration`（没有则 4 s）把它放到玩家身上，第二次使用时刷新结束时间。
  瓦尔光环不保留任何东西，也不是开关，所以 `AuraCatalog.Aura` 带一个 `Until`：`ReconcileOwnAuras` 在那里
  结束它，而且那个检查每个 tick 都跑，而不是放进重新定价闸——后者只在属性脏时打开，而运行中的光环不是脏的。
  重新定价循环也跳过它，因为它没有可读的技能消耗。它的属性像其他所有光环一样来自技能自己的 `EffectStats`，
  所以没有手写的数值。
  尚未做：瓦尔正义之火随时间的生命消耗，它需要与普通正义之火同一个 tick，且
  没有任何瓦尔光环用真实客户端核对过。
- **已放置的旗帜（2026-11-08）。** 战争、蔑视与恐惧旗帜之前什么也没立。它们现在坐在 `AuraCatalog` 的 buff
  行里——战争 1221 `bloodstained_banner_buff_aura`（`war_banner_physical_damage_+%_final_with_melee_skills` 与
  `accuracy_rating_+%`）、蔑视 1864 `armour_evasion_banner_buff_aura`、恐惧 1222
  `puresteel_banner_buff_aura`——所以普通光环开关保留它们的魔力、施加 buff，并在旗帜被捡起时归还。
  `AuraCatalog.BannerAo` 加上地面效果：客户端自己的 MiscObjects
  `BannerGround` 行（`Metadata/Effects/Spells/banner/WarBannerGround`、`DreadBannerGround`、
  `ArmourEvasionBannerGround`），在旗帜立起时作为 `EffectObject` 发到玩家站的位置，被捡起时移除。
  尚未做：旗帜的勇气层数（每次击杀提升其效果，最多 50）、它施加给附近敌人的减速
  （`banner_debuff_aura` 2484），以及与真实客户端的任何核对。
- 被动自身 buff（`PassiveBuffAuras`）：被动 BuffTemplates 施加在玩家身上的 buff 把它的属性加进
  被动总和，并在分配后与进入区域时用 0x1c1 发送，退还时用 0x1c2 摘掉
  （冲浪者的深渊之魂）。
  只有网关测试覆盖它；图标与客户端在死亡后是否保留该 buff 未实测。
  敌人与盟友被动光环不施加。
  代码：`PassiveBuffAuras.cs`。
  文档：[11](../docs/protocol/11-skills.md) §12。
- 击杀获得猛攻，授予猛攻 buff 与它的速度（[13](../docs/protocol/13-items.md) §8a）。
- 来自物品属性的击杀驱动 buff：`player_gain_rampage_stacks` 开始一段狂怒连击，它的击杀喂
  「每 X 次狂怒击杀增加 1% 物品稀有度」属性（完整 X 组）；近战命中来源像以前一样继续
  在命中时授予层数。「狂怒阈值」修正现在每跨过一档触发一次：生命
  回复、解除异常、物理伤害免疫与相位，持续所列时长。按层修正
  的区域效果（每 25 次击杀）与链钩 / 盾牌冲锋攻速（每 10 次击杀）计入，连击期间
  一次击杀额外授予瓦尔之魂。`gain_soul_eater_during_flask_effect` 在药剂效果运行时授予噬魂者：
  每魂 5% 攻击与施法速度增幅、1% 角色尺寸，60 格内每次击杀一魂，
  基础上限 45 加物品层数修正，4 s 无击杀后每 0.5 s 失去一魂。
  玩家拿到它的图标与计数
  （`soul_eater_player`，279）与隐形的 `soul_eater_aura`（277），客户端的 `using_flask` 标志会下发，每魂
  从尸体到玩家画官方 `ServerBeamEffect` 线（见
  [11-skills.md](../docs/protocol/11-skills.md) §12）。
  灵魂之渴（Soulthirst）在药剂使用时失去已吃灵魂未实现。
  当前未建模：阈值烟雾（没有限时烟雾地面）、邪恶之力（没有邪恶之力系统）、
  每 50 次击杀的狂怒球（充能会话没有狂怒计数规则）、在满耐力充能时开始连击并在结束时失去它们，
  以及地图/联盟石的狂怒修正。
  代码：`Rampage.cs`、`RampageSession.cs`、`SoulEater.cs`、`SoulEaterState.cs`、`GemSupports.Rules.cs`、`VaalSouls.cs`。
- 猛攻（击杀获得与银药剂的）授予 Path of Building 的速度修正：攻速属性与挥击时机里 20% 攻击速度增幅，
  施法速度属性里 20% 施法速度增幅，行走里 20% 移动速度增幅（[13](../docs/protocol/13-items.md) §8a）。
  代码：`UtilityFlasks.cs`、`OnslaughtOnKill.cs`。
- **属性。** 客户端的属性键是 Stats.dat 行号 + 1，配随附的 schema
  （[11](../docs/protocol/11-skills.md) §8）；`/set_stat` 接受属性名或键。
  [357 项属性台账](../docs/dev/issue75-stat-ledger.md)跟踪当前逐项复核状态与剩余
  范围。清单校验精确成员关系；这些状态不算已完成实现。
  玩家的生命、魔力、命中、护甲与闪避来自它的职业与等级，加上属性的加成
  （能量护盾、闪避与近战物理伤害增幅）；
  命中还吃它的固定/增幅被动、武器专属属性与命中精通（面板用数值，
  掷骰用两个目标条件效果），最终数值作为 `main_hand_accuracy_rating` / `off_hand_accuracy_rating`
  发给攻击面板；
  护甲与闪避也吃它们的固定/增幅被动与已装备的护甲基底（身体、头盔、
  手套、鞋子）；生命/魔力还吃带符号的每级与完整最终属性组来源，然后走它们
  既有的固定/增幅投影。它的最终下限仍是文档化的本地选择；当前自动化结果
  在上面的属性条目里，新的原生核对待做。
  见 [11](../docs/protocol/11-skills.md) §4。
- **攻速被动。** 通用、近战、单/双手、武器族、盾牌与双持的被动攻速
  汇总进客户端的 `main_hand_attack_speed_+%`；
  双持加上 PoB 的 10% 更多，服务端的徒手攻击使用同一个最终值。
  攻击宝石只在服务端应用它们提取的固有速度倍率；客户端应用它自己的，所以下发的
  总基础持续仍是武器的。节奏、接触/释放时机、引导间隔与已选 APS
  共用服务端的持续。Frida 用一把 714 ms 的弓验证了剪辑：普通 0.714 s、燃烧箭 1.02 s。
  攻击由服务端创建的投射物在接触前 60 ms 释放（`AttackReleaseDelay`），
  留给服务端读动作、它的 tick 与输出队列的时间。燃烧箭的箭在客户端枪口闪光之后约 25 ms 到达
  （以前约 75 ms）；默认箭在它接触后 0-52 ms，分两簇（约 265 与 319 ms）。
  辅助、条件/每球速度、特殊时间覆盖与攻击宝石伤害/DPS 仍是分开的工作。
  已选攻击宝石的主手范围现在共用转换后的战斗报文，含技能效果。
  全部十二个范围清掉不存在的类型；暴击前平均值保留分数，DPS 使用下发的 APS 值。
  创建与刷新把裸角色属性与实际武器同继承的显示值分开传入。
  原生燃烧箭显示 9-23 火/总、物理零、每次使用 16.52 与 APS 0.98 时 DPS 16.19。
  自加冰伤害显示 9-9 冰与 18-32 总；清掉它则移除冰行。
  重登保留了基础结果。
  一次非暴击服务端命中造成 24 伤害，而普通攻击保留它原本的范围与平均。
  报文、目录、转换、来源隔离与原生行格式测试也通过。
  普通攻击、法术与副手显示路径未变。
  条件/辅助效果、暴击加权与更广的武器修正范围仍是分开的工作。
  默认攻击还发送面板的 `hundred_times_damage_per_second` 与
  `display_hundred_times_damage_per_skill_use`。
  实测过：双持角色的 APS 跟随被动（加一个节点后 2.93 → 2.98）。
  召唤物、图腾与按时/条件属性未建模；
  角色面板的 APS 在分配被动后保持缓存（重开面板或切它的页签都不会
  刷新；只有重登才会）。
  （[11](../docs/protocol/11-skills.md) §4）
- **法术施法时机（#192）。** 目录的基础/虚拟施法毫秒现在对原生法术动作保持不变，
  它们自己应用施法速度。
  服务端等待与显示的每秒施放共用 `SpellCastTiming`；攻击保留它们既有的路径。
  旧的 -80% 火球恢复抓包持续了 18.75 秒而不是 3.75，因为两次除以 0.2。
  自动化的 0/+100/-80 时机、瞬发/引导分类、非法速率与既有光环/召唤核对通过。
  真实客户端 Q 火球的剪辑在 -80% 时测得 3.750000157 s，+100% 时 0.375 s，零时 0.75 s。
  原生虚拟基数保持 750 ms；下发的施法速率分别是 0.27、2.67 与 1.33 次/秒。
  投射物释放/接触对齐未单独测量。
  「更多」施法速度（回响的 40%、带鱼竿的尖啸卷线器的 60%、狂信、图腾辅助与
  原生合并最终的其余来源）在服务端等待与法术记录的 `cast_speed_+%` 里使用原生计算器公式。
  那个记录值现在也包含辅助自己的施法速度（快速施法），服务端等待本来就这么做了。
  两处改动都未用真实客户端核对；只有网关测试覆盖它们。
  无投射物的区域法术（冰霜新星）在它们的 `spell_aoe` 剪辑接触点释放（女巫是施法的 266/766，
  `AnimationContacts.SpellAreaRelease`；剪辑选择从 IceNova 动作的代码读出），不再在施法
  结束时释放（对 700 ms 的冰霜新星约晚 450 ms）。
  冰霜新星的受害者在环到达它们时被命中，历时 0.2 s（`IceNovaRing`），已释放的环在
  玩家移动或再次施法时仍会落地。
  尚未用真实客户端逐帧核对。
  瞬发施法保持瞬发；引导伤害节奏与条件动作速度机制未变。
  服务端数值持续低于 1 ms 的被向下取整，非法因子/不可表示的等待不产生法术效果。
  代码：`SpellCastTiming.cs`、`AnimationContacts.cs`、Gateway（`GemSkills`、`CastMs`、`CastGemSkill`、`LaunchProjectile`、
  `AreaSkillHit`）。
  文档：[11](../docs/protocol/11-skills.md) 第 9 节。
- **已选法术命中伤害面板（#188）。** 法术总计/按类型范围与历史暴击前平均伤害/DPS
  现在使用与投射物和区域战斗相同的转换报文。
  所有范围字段都显式，含不存在类型的零；无命中的法术清掉继承的武器 DPS/每次使用。
  最终已选法术记录对法术与继承的主手范围使用受支持战斗报文。
  #75 的复核修正现在用当前暴击几率/倍率与带符号幸运给普通/受支持平均伤害与 DPS 加权，
  保留精确动作持续并钳制原生整数边界。
  攻击估计还包含普通参考落点概率与独立暴击确认，使用
  普通/已选命中档案。裸全局/本地不可闪避更新估计与两个普通
  主手几率别名；最终诊断几率不反向喂估计。新增十九个用例
  覆盖六处被复现的遗漏、法术控制、真实辅助连接/移除、绕过、幸运与边界；
  扩展后的定向运行通过 55 项。原生重新验证、整数掷值期望、多命中、
  双手、冷却与 DoT 计算仍未做。见 [11](../docs/protocol/11-skills.md)。
  这包含法术伤害被抑制之后的显式零范围，不改变攻击显示。
  自动化的混合转换/获得、自加伤害效果、无命中、攻击与原生行格式核对通过。
  原生选择/getter 核对显示带 +12% 法术伤害法杖时火球 10-15 火、冰霜脉冲 10-15 冰。
  一个转换/获得控制把火球分成火与混沌行，并造成一次非暴击 14 伤害的服务端命中。
  灼烧射线清掉继承的命中范围与武器 DPS，含在自加法术伤害刷新它的记录之后。
  范围使用服务端取整；修正后的平均保留报文分数并排除目标减伤、
  按类型整数掷值期望与冷却/空转。
  光束/DoT 显示、条件伤害规则与更广的转换语义仍在此切片之外。
  代码：`SpellHit.cs`、`SpellPanelStats.cs`、Gateway（`GemSkills`、`LaunchProjectile`、`AreaSkillHit`）。
  文档：[11](../docs/protocol/11-skills.md) 第 9 与 14 节。
- **暴击。** 服务端掷无条件的暴击被动：已装备主手的 `WeaponTypes.dat`
  暴击几率（万分数），对攻击由通用/攻击/近战/武器形状的暴击几率增幅提升，
  每个法术提取的基础几率，以及 150% 加 `base_critical_strike_multiplier_+`；
  暴击伤害被乘算并记录。
  徒手基础暴击为 0%。
  两个纯基数值在增幅几率之前替换那个基数，方式同 PoB 的 `WeaponBaseCritChance`
  在该手有武器时替换一次攻击的 `source.CritChance`：
  `override_weapon_base_critical_strike_chance`（Stats 第 9892 行，万分数）作用于已武装的主手——屠杀者
  压倒（Overwhelm，graph id 17315）授予 800 = 8.00%，盲先知的无目（graph id 24698）授予 900 =
  9.00%——以及 `base_melee_critical_strike_chance_while_unarmed_%`（Stats 第 3906 行，万分数；巨兽的最后一击，
  graph id 10135，授予 1000 = 10.00%）作用于徒手近战攻击。
  每个都是基数，不是固定的最终几率：通用的暴击增幅仍然在其上生效；
  武器覆盖不进徒手与法术掷骰，徒手基数也不进持武器的手。
  只有激活武器集供给攻击基数与其武器专属增幅。
  普通自加 BASE 现在在固有基数或其覆盖之后、增幅几率之前，组合通用与攻击/法术专属的裸 actor 与
  已选技能/辅助来源，用带符号宽位求和与最终半进位上限。
  原生武器本地固定暴击几率在本地增幅几率与其万分取整之前。
  已连接的「暴击几率增幅」一次性供给它自己的 BASE 与增幅；已知零固有几率可以接受
  自加 BASE，而缺失/负数据、无命中技能与不可暴击闸仍按失败关闭。
  原生修正标识、按模式的升华刺客来源、激活集隔离、移除、零与
  辅助组合有自动化覆盖；当前组合结果在上面的面板条目里。
  `0aabeb6` 上十六项正向火球/暴击几率增幅原生控制在两种模式下按实际 100 级通过。
  已选万分数值 500/550/1200/1120 渲染为 5/5.5/12/11.2%，含 actor/辅助移除、重连与重登。
  两个当前 Actor 的火球移除控制与两个默认哨兵也通过，没有做数值零的推断。
  原始标准 D0 哨兵仍是 UNPROVED；它那次单独复核的抓取后修正使用当前
  火球/默认标识与历史 C2 数值证据，没有同时主张火球接收方。
  它的版本冻结早于无情哨兵抓取。
  规范库存/充能与受保护数据保持；宝石 ID 重映射与保留的参与者状态在
  协议 11 §5 里做了限定，常规服务已恢复。
  原生暴击命中、渲染出的零与剩余攻击/本地/刺客/取整上下文在此仍未验证。
  隔离载体不确立合法的传奇获取，本地固定几率夹具保留它真实的
  无额外暴击伤害标志，但不认证倍率行为。固定 PoB 3.29.x 是标注过的兜底。
  `/set_stat` 替换输入并重建池，所以面板与掷骰一致。
  通用/法术开发增幅替换法术掷骰所用的同一份完整裸输入，含光环来源。
  攻击/法术增幅百分比与合并的法术参考现在跟随那些有效输入。
  自动化回归覆盖这些替换、带符号求和边界与共用整数取整。
  攻击与法术几率现在共用半进位万分取整：650 加 +1% 得到 657。
  下面的原生证据针对原始 head `8c4c62d`；需原生复核改动后的中点与已完成的叠层。
  五项后续原生核对验证通用/法术输入一致与最大/最小整数饱和。
  十二项原生核对验证 830 → 800 → 900、增幅几率 → 1800、不可暴击 → 零、
  空集/徒手行为、武器恢复与重登。
  升华分配仍在分开的生命周期改动里。
  全局、虚拟全局与仅攻击的不可暴击标志抑制攻击几率。
  `SpellCriticalStrike` 两个基数覆盖都不读。
  最终主手与法术暴击几率/倍率属性发给客户端用于攻击面板；
  已选法术几率与服务端投射物/区域掷骰现在一次性共用裸玩家与技能输入。
  精准与狂信保留既有加成而不重复计入。
  实际 Spell 技能类型设置面板的施法标志；攻击与非法术战吼保持清除。
  无命中法术与不受支持的动作类型不能落到武器伤害或攻击暴击掷骰。
  原生面板/getter 核对显示火球 5%、冰霜脉冲与冰霜新星 7.5%、电弧 6%，含重登之后。
  合并的通用/法术加成一次性相加；无暴击标志清掉原生几率并隐藏它的行。
  一次无命中的灼烧射线施放不造成武器伤害，而同一连接的火球打出暴击。
  目录、几率、面板闸、光环来源与命中路由也有自动化核对。
  四个无条件 MORE/LESS 几率来源现在在最终上限/取整之前运行。真实的「受控毁灭」
  把已选 10% 变成 2%，攻击与法术都是；它的连接/需求/移除生命周期与实际解析的
  暴击掷值一致。那个真实的 -40% 传奇来源把普通武器的 10% 变成 6%，显式零
  与物品移除恢复 10%。来源抵消、极端边界、独立因子组合与
  半进位取整有回归；当前原生验证待做。
  冲浪者的「诱饵之息」在持鱼竿时让每次命中暴击，含 0% 基数；不可暴击优先。
  专注技能与诅咒生命周期、保证暴击效果、副手发布/执行与其他
  敌人状态/更多暴击机制仍是分开的工作。
  `/crit` 显示攻击值与一个合并的 5% 基数法术参考（[11](../docs/protocol/11-skills.md) §5）。
- **超上限抗性修正。** 标志属性被设置时，超过其最大值的未封顶抗性是一个
  通用增幅修正，正是 PoB 的 `ResistOverCap`（最大值按 90 封顶）：每 1% 冰超上限 +1% 闪避增幅，
  每 1% 闪电超上限 +1% 暴击几率增幅，与其他增幅加性，攻击与法术一样。
  这些标志是普通属性，所以任何来源都能设置：冲浪者的「海腿」显著点（graph 61696，配它的
  +40% 冰与闪电成对抗性，聚合器现在把它加进两个元素）、一个物品词缀，或 `/set_stat`。
  一个显式 `/set_stat` 值替换它自己的输入（按类型、成对与全元素抗性，以及按类型或
  全局最大抗性）而不是加进聚合，并重建池；奇塔瓦变化会再次重建它们。
  `PoolsFor` 每次建池推导一次增幅并存进池，所以怪物攻击的闪避
  掷骰、面板行与法术暴击消费方读同样的数；未封顶抗性值保持
  不动，铁之反射转换后的闪避保留超上限增幅一次。
  自动化核对覆盖低于/等于/高于上限、90 最大值、两项惩罚、标志移除与覆盖、加性
  行为、法术/攻击隔离与铁之反射转换。
  合并的自动化核对保留 830 +45% =1204 的已武装几率与完整的带符号暴击抵消。
  感知纯净的裸替换现在对那个精确键移除光环贡献，含显式零最大值。
  真实光环/目录/池回归通过；修正后的光环覆盖组合需要原生复核。
  已完成的叠层需要原生复核；下面十三项核对属于原始 head `39cbda2`。
  真实客户端属性核对在 120% 抗性下验证闪避 15 → 22 与法术暴击 500 → 725，
  提升最大值、全局输入替换、两项奇塔瓦惩罚、标志移除与重登。
  升华分配仍是分开的生命周期改动的一部分。
  见 [11](../docs/protocol/11-skills.md) §4、§5 与 §12。
- **投射物。** 一个按时间排序的运行时驱动生产环境的宝石/弓飞行、接触与延续。
  它以 30 Hz 边界运行，带子 tick 接触，并在网络输出之前提交伤害/奖励。
  原生宝石技能记录从共用参数规则推导投射物数量、穿透、连锁、分叉、分裂与回返。
  这些仅显示的投影遵守禁止，并在辅助移除后刷新而不改变释放输入。
  缺失投射物元数据允许通用面板参数，但可执行释放仍不可用。
  释放捕获位置与攻击；目标移动、闪避、防御与穿刺在接触时解析。
  #75 的命中熵/暴击确认、每次使用一次暴击抽取、裸穿透、实际掷值穿刺与
  直接命中汲取在这个同一引擎里跑。资源回复在消费汲取之前追上投射物；尸体
  保留普通生命。合并的自动化回归覆盖守护/槽位防御、魔力转换与接触
  恢复。细节与测试凭据：[合并复核](../docs/dev/issue75-review.md#upstream-merge-resolution)。
  后来的主线合并把赌徒、瓦尔记账、受得异常与赛季模式与这些属性消费方组合。
  合并回归覆盖仅直接汲取与持握输出下的单次魂记账，以及按模式的防御。
  它的[验证报告](../docs/dev/issue75-review.md#main-merge-league-modes-gambler-and-vaal-souls)记录这些决定。
  那个合并构建的原生重新验证仍待做；更广的 357 属性目标保持暂停。
  暂停冻结游戏时钟，转移/退役作废世代，已释放的射击在后续动作中存活。
  普通弓与法杖在那个运行时上共用一条释放路径：射击在被接受输入的武器
  接触点排程（`attack1_bow`，或原生 ACT 为该输入序列选出的法杖剪辑），动作回显随后；
  被取消的动作、死亡、冻结或命中恢复不释放任何东西。法杖弹在它停下的地方播放骨架的命中剪辑，
  并在同一游戏时钟上过了那个剪辑之后被移除，所以暂停会按住它、转移则丢掉这次移除。
  箭在命中处停一秒（`ArrowStuckSeconds`）再移除。两者在接触时都发 `0x1af`（kind 3、该射击的
  动作序列、flags 0x4201 加暴击位、按伤害类型的 code）用于目标的血迹与
  粒子；主手位跟随实测抓包，它也填的技能索引与头部字保持为零。投射物对象在它们的释放点创建，
  它们的首次移动从那里开始：以前那个惰性
  快照会在模拟已经把它们放好的位置创建它们，实测日志里在弓前方一到十格
  （2026-10-29）。在锁步客户端上，默认攻击的记录像 GGG 的一样带该射击的计数，
  客户端自己画弹或箭，没有服务端对象；服务端按客户端的接触与
  射程模拟它，并在客户端自己的释放帧释放它（[07](../docs/protocol/07-latency-visibility-movement.md)
  §6），所以它停下之前的命中恢复会同时停住两者（2026-11-05 实测对一只怪的指令：一次客户端
  每次射击一个 `WandProjectile`，41 次里一个 `DefaultArrow`，没有投射物 `0x2bb`，客户端释放比服务端晚 0–7 ms
  （法杖）与 1–3 ms（弓），p10–p90；第一版是晚 16–40 ms，更早带服务端对象时是晚 66–99
  ms）（[11](../docs/protocol/11-skills.md) §4、§10）。打地面的射击（原地攻击）
  同样释放但未实测。预测式客户端保留服务端的对象：它的记录
  没有计数，没有计数释放就什么都不发（2026-10-29 的一次原生跟踪在 40 次里没看到客户端箭）。
  实测命中时机拟合把我们的箭放在 367 格/秒，而实测是 308，但网关按
  表的 3260 单位/秒 = 299.9 格/秒释放 DefaultArrow（每次释放都记日志），所以那条拟合的斜率量的是别的东西
  （路线图）。它们从
  释放剪辑的接触关键帧出发（`data/animation-contacts.json` 的位置，`anim_contacts.py` v3）：游侠的
  弓沿瞄准方向前 5.5 格、高 16 步，法杖尖端前 7.5 格、高 12（5 步的默认值是在一个
  火球上调的，会让箭的起点拖到腰上，2026-10-29 实测）；关键帧的横向部分不应用。
  怪物投射物：迫击炮攻击的射击（沙喷怪、食人族的 ThrowFire：`monster_mortar_attack` 经
  GrantedEffects SkillID → MonsterMortar → Projectiles）由客户端绘制，它的记录像 GGG 的一样带 MonsterMortar
  行与技能的计数，没有服务端对象（2026-11-05 在锁步与
  预测客户端上实测：每个指令一个客户端 `SandspitterMortarProjectile`，没有服务端对象；
  [11](../docs/protocol/11-skills.md) §6）；法术迫击炮（`monster_mortar`）保留它的服务端对象。尚未测量：
  客户端的迫击炮落在哪里对服务端的命中。
  服务端在同一运行时模拟它，按客户端为该
  技能播放的剪辑释放（MonsterMortar.Animation → ACT 的 `SpellProjectile` 池，原生选择器按动作的 0xffff
  序列：attack1，接触在攻击的 37.5 %），从该剪辑的关键帧出发（背对的躯体后方 4.2 格，
  也就是朝向玩家，高 9 步：`projectile_uses_contact_position`；`data/monster-contacts.json`、
  `scripts/re/monster_contacts.py`），抛向玩家的格子但不近于技能
  `projectile_minimum_range`（4.6 格），team 2，落点溅射（`Game_MONSTER_MORTAR_RADIUS`，默认 10
  格）跑怪物接触管线（`HostileProjectileOffense`、`Program.ProjectileHostile.cs`）。尚未实测：客户端是否
  按序列为怪物播种剪辑选择，以及死区。另一个怪物的
  投射物对象从生成起播放它骨架的飞行剪辑，在落点播放命中剪辑
  （`data/projectile-clips.json`：每个 Projectiles 行的剪辑索引与长度，
  以及它的 .ao 默认动画），没有它的话，那些 .ao 未声明默认动画的对象（迫击炮的；法杖弹的声明了）
  就是不可见的。数据加载器在一个不可读文件上带一条警告关掉
  对应特性，而不是让泵出错，怪物 ACT 池只取自根块
  （一个嵌套姿态的条目曾替换掉 191 个变体的；复核 2026-10-30）。武器投射物怪物
  （`monster_projectile_weapon`、`monster_snipe`、`monster_puncture`，以及持弓或杖怪物的多态
  攻击）现在飞它们自己的服务端对象（`SkillCatalog.WeaponProjectile`）：美术是技能的
  `arrow_projectile_variation` 在 ProjectilesArtVariations 的 `default_ranged` 族里的行（瓦尔弓手 32/33/34 是
  LightningArrowRearOrigin、BurningArrowRearOrigin 与 IceArrowRearOrigin，也正是它们武器美术命名的行），
  没有则 DefaultArrow，法杖技能则 WandProjectile；该行的速度按 `base_projectile_speed_+%` 缩放
  （编织恐魔的 200 %，卡奥姆弓手的 100 %）。技能既不命名箭也不命名弹的怪物（扔出的
  石头、幽灵武器：480 个远程攻击效果）仍即时命中，而法术的投射物是单独的
  MonsterProjectileSpell 适配器；两者都在路线图上。尚未实测：箭的飞行、它们在
  Melee 池接触点释放（瓦尔守卫没有骨架数据，所以按一半释放）以及元素
  箭 .ao 在没有剪辑数据时是否动画（`data/projectile-clips.json` 缺箭行）。
  法杖主手变化跟随每个职业的 ACT 池与原生按输入播种的选择器；十六次实际地面
  输入覆盖全部四个女巫剪辑，选择一致，一个物体输入也匹配（早期飞行循环上的历史 LocalExile
  证据；运行时路径仍需原生复核）。完整的副手计时/伤害
  与非零固有攻击重复仍未做。
  真实输入持握输出、移动目标、墙、闪避、穿透、回返、暂停、转移、穿刺与 actor 缩放核对
  都在跑这个同一运行时。原生弓/技能控制与动画对齐仍需要客户端验证。
  直线飞行使用捕获地面加释放关键帧的高度，否则 `Game_PROJECTILE_HEIGHT`（默认 5）；
  缺失捕获则用裸偏移。
  创建、滑行与停止保持同一高度；独立效果仍跟随客户端地形。
  平台/装饰物仍需要表面模型（#182）。整数走位估计与共用半径仍是近似。
  代码：Core `Projectiles/`、Gateway `Projectiles/`（`Program.ProjectileRanged.cs`）、`Program.RangedAttacks.cs`、
  `OrdinaryRangedAttack.cs`、`OrdinaryRangedShot.cs`、`Program.Combat.cs` 与 `ClientState.cs`。
  文档：[框架](../docs/game/projectile-framework.md)、[11](../docs/protocol/11-skills.md) 第 10 节。
- **战斗。** 玩家的默认攻击用服务端伤害杀死怪物（还没有怪物护甲；一次命中的每种伤害
  类型取怪物自己的基础抗性，命中穿透从封顶值里减掉，
  `HitMitigation`/`Penetration`；玩家的命中落在攻击动画的接触关键帧，怪物的仍
  在它攻击时间的一半）。
  怪物用它们第一个授予效果并回击；它们的攻击掷玩家闪避，然后走护甲
  与格挡。
  普通物理近战命中可以通过 0x1d1 触发原生玩家 TakeHit 恢复。
  原生证明停住了预测式走位与施法、拦下新动作，并在之后清掉恢复标志。
  服务端恢复取消待发动作与引导命中，并按同一区间停止走位估计。
  恢复或冻结也会挂起运行中的引导：在玩家恢复之前它既不付费也不扫描。
  那个引导挂起只有自动化覆盖，还没有原生核对。
  实际物理怪物命中在复制存档实测里停住走位并在释放前取消一次慢施法。
  已释放的火球飞行继续，一个免疫眩晕的控制吃伤害但不进入恢复。
  装备弓时，一次命中取消了待发的燃烧箭、在恢复期间拦住普通箭，
  之后允许新的箭；已经释放的燃烧箭继续飞。
  几率、规避、免疫、恢复与延迟的物体走位有自动化核对。
  ES 构建、被替换的阈值与不受支持的条件眩晕防御被保守跳过。
  范围与协议证据见 [11](../docs/protocol/11-skills.md) 的玩家命中恢复。
  玩家的生命来自职业/等级/被动池。
  服务端没有共用的模拟循环：命中用计时器，攻击时间被 30/s 的 tick（`TickRate`）约束。
  怪物决策用 50 ms 游戏时间步、独立的 100 ms 重寻路预算与三格目标阈值。
  完整攻击持续、待发挥击世代与阶段完成保护更快的规划器不重复干活。
  原生控制验证初始响应、刻意转身、完整攻击节奏与实际菜单暂停/恢复。
  Core、Data 与 Gateway 套件通过，含节奏、待发挥击与阶段完成回归。
  物品词缀通过全局物品属性计入；
  带 NPC / ControlZone / … 组件的 346 个变体被拒绝。
- **暴击元素抗性规则（必然审判）。** 命中已经掷出的暴击布尔决定抗性：报文掷一次（原来的按类型掷），
  既有的单次暴击抽取在它们之后保持原位，减伤在已经掷出的报文上解析，不再掷第二次——一次带
  `critical_strikes_ignore_elemental_resistances` 的暴击元素命中用一个有效抗性
  为零（含负值）做减伤，覆盖任何穿透；仅攻击变体只覆盖攻击；一次
  非暴击元素命中保留它封顶的抗性与 `non_critical_strikes_penetrate_elemental_resistances_%`
  （10），在封顶之后相减，与命中的通用穿透叠层；
  `cannot_penetrate_or_ignore_elemental_resistances` 恢复普通的封顶抗性。
  物理、混沌与持续伤害在这些规则之外；必然审判（图节点 48214）通过普通被动属性求和
  授予通用忽略与那 10 点穿透。
  共用的辅助函数（`HitResistances`、网关的 `MonsterDamage`）覆盖默认攻击、弓与宝石命中。
  默认与区域伤害日志命名该判定；宝石投射物发单独的减伤跟踪。
  Core 与网关核对覆盖正/负抗性、最大值独立与节点的精确属性键。
  八项原生核对读怪物生命：固定 100 火正常情况下造成 60，有非暴击穿透则 70。
  保证暴击的命中在通用或仅攻击忽略下造成 150，移除或禁止后为 90。
  那些抓取属于原始 head `13c41e5`；合并的穿透/修正/弓路径需要原生复核。
  自动化核对保留同键来源求和、分数合计、目标承受伤害与原随机顺序。
  顺序式 C24 处理器测试在同一连接与目标上先前的命中之后读取真实 `/set_stat` 变化。
  技能 `0x102`、序列 0、flags `0x408` 给出忽略 300、普通暴击 330、禁止 240。
  友方与敌对控制在没有生产敌对性改动的情况下通过。
  标注为 `462c883` 的那次合并运行用了经哈希证明是 #225 `4a71968` 的 DLL；它的第九个用例不是 #226 的证明。
  升华分配仍在它分开的生命周期改动里。
  代码：`HitResistances.cs`、`HitMitigation.cs`、Gateway（`MonsterDamage`）。
  文档：[20](../docs/protocol/20-damage.md) §4。
- **客户端 Buff 描述符所有权/转移研究（#370、#466，仅离线）。**
  原始字节把准备好的结构绑定到物品
  Mods `+0x38` 与实体 ObjectMagicProperties `+0xc8`，带一条结构化的物品赋值路径。
  一个单独的 `+0x1d8` 缓存按指针来源与记录步长被排除在描述符路径之外。
  这不成立一个有意义的描述符消费方、实际目标 DoT 发布或实测行为。
  修订后的交付记录这些结构发现；实际读取方与输出仍是已知问题 K1-K3。
  原始 N1 是部分，N2 仍未解决，R2/R3/R4 保持部分。
  见[报告](../docs/re/client-buff-descriptor-consumers.md)。
- **伤害模型（基础，含护甲/闪避）。** 一次命中是按类型的 `DamagePacket`；
  `DamageConversion` 处理游戏的物理、闪电、冰、火、混沌顺序、链式转换、技能/全局
  优先级、100% 上限与获得为额外。
  DoT 是单一类型带分数 tick 携带，`Ailments` 有点燃/流血/中毒的基础公式。
  默认攻击、火球与怪物近战构造报文并读玩家/技能的转换属性；
  报文减伤套用每种类型自己的抗性，物理部分随后走护甲。
  正义之火的燃烧使用 DoT tick 模型。
  伤害类型免疫（`*_immunity`、`elemental_damage_immunity`）把该类型在玩家的命中中清零，来源
  与面板免疫行所用的相同（`DamageImmunities.cs`）。
  按完整最终力量、敏捷或智力组的全局自加攻击伤害并入裸攻击
  范围，用于面板行与命中（Hyrri's Demise、（复制）铁之指挥官、复制阿尔贝隆的战径每 80
  力量——尽管它的 id 是 `_per_50_strength`；[20](../docs/protocol/20-damage.md) §2b）。
  3.29 的整类型物品禁止（「不能造成非混沌伤害」「不造成物理伤害」…）共用一个列表：
  命中、流血、点燃与中毒行（各自按其类型设闸）、中毒命中施加与施法者的
  出招 DoT 都去掉那些类型。
  仅自动化测试；原生核对待做。
  承受的命中在读生命之前先读承受伤害与魔力属性（[20](../docs/protocol/20-damage.md#damage-taken-from-hits)）。
  普通玩家能量护盾来自装备、智力与被动。
  装备 ES 在本地修正与品质之前之后使用存下的可变基数掷值与原生取整。
  它在生命之前吸收非混沌命中，并通过回复与延迟、被伤害打断的充能恢复。
  既有的 16 ms 已流逝时间循环供给客户端原生 ES 恢复预测。
  当前 ES 与其预测在网络发送闸一起取样，含重复抑制。
  加密线持握闸测试覆盖排队钳制、清空预测、死亡与未变状态的抑制；
  这个排队发送用例的原生核对仍待做。
  实测覆盖吸收、回复、被打断的充能与混沌绕过。
  狂信者之誓已实现；其他特殊 ES 基石、补偿与条件替换仍未
  实现；普通 ES 汲取使用共用的回复写入方（§5b）。
  见 [ES 范围](../docs/protocol/20-damage.md#player-energy-shield-2026-10-25)。
  按最大魔力加的闪电有原始 head 原生核对与已完成的自动化核对。
  玩家的命中冰缓、冻结与感电怪物：阈值最大生命、最强效果带自己的持续、
  感电作为承受伤害增幅（命中与正义之火）、冰缓与冻结在怪物 AI 与行走中、怪物
  词缀的异常属性（冰之、风暴之、不可阻挡、镜像）以及冰缓/冻结/感电 buff。
  元素专注与永不异常属性停掉那些异常（[20](../docs/protocol/20-damage.md) §3）。
  冰缓可以提升承受伤害并减慢怪物挥击（冰川波），冻结可以有最小持续，
  击中玩家的怪物可以被冻结（盐缚）或冰缓（潘达蒙尼乌斯）。
  玩家格挡的一次怪物命中可以被冰缓或感电 4 s（「格挡时冰缓/感电攻击者」）。
  「视作造成更多伤害」提升它施加的冰缓、感电或冻结（[20](../docs/protocol/20-damage.md)）。
  感电与冰缓的下限（毁灭信标）与上限可以提升（[20](../docs/protocol/20-damage.md)）。
  「所有/混沌/…伤害可以冰缓/冻结/感电」计那些类型，「命中总是感电」不需要几率。
  尸体保留致死命中的异常，一次击杀也能感电它，除非那次命中的击杀是冻结致死并把它击碎。
  用 Berek's Respite 击杀一个被感电的怪物，会给 2 m 内的怪物同样的感电。
  每个冰缓、冻结与感电都传播到它所附着的怪物或尸体各自 `virtual_*_proliferation_radius` 内的活着的敌人，
  像一个光环。
  一个区域技能在它最后一个目标之后施加镜像怪物的反射异常，一次中途停止的扫描
  仍然给它的击杀发奖励。
  这些 buff、下发的动作速度与冻结姿态未用真实客户端核对。
  见 [20](../docs/protocol/20-damage.md#outgoing-chill-freeze-and-shock-on-monsters)。
  命中按它们的几率施加元素暴露，默认 -10% 持续 4 s（[20](../docs/protocol/20-damage.md) §2）。
  元素均衡移除一次命中各元素的暴露，并对其他元素施加 -25% 暴露。
  尚未做：来自技能与光环的暴露、依赖光环的穿透、其他自加伤害、
  增幅/更多辅助修正与出招异常的施加/持续（[20](../docs/protocol/20-damage.md)），
  除了玩家对怪物的冰缓、冻结、感电、点燃、流血与中毒。
  投射物命中按释放时捕获的面板点燃行点燃，默认攻击与近战或区域
  技能按它们命中时的同样行；阿贝拉斯之怒的脚步命中自己算
  （每秒该命中固定火的 90% 持续 4 s，点燃增幅、持续伤害倍率、暴击时 +50%，
  以及持续属性）。
  两者都加进怪物那一套点燃：只有最强的在烧，经怪物火抗，每个
  玩家 tick，配一个限时 `ignited` buff 与击杀。
  玩家死亡期间它们继续烧（未验证），硬核角色死后的击杀不推进。
  近战与区域点燃与脚步点燃未用真实客户端核对（`MonsterIgnites.cs`、
  [20](../docs/protocol/20-damage.md) §3）。
  攻击命中（默认攻击、近战与区域攻击、攻击投射物与弓射击）按面板流血行与攻击流血几率
  使怪物流血，持续 5 s 加流血持续属性：只有最强的在流
  （血舞：最强的 8 个），怪物移动或加重时三倍，配一个限时 `bleeding` buff
  与击杀（`MonsterBleeds.cs`；未用真实客户端核对）。
  一次命中的中毒按面板中毒行结算（`MonsterPoisons.cs`），未用真实客户端核对。
  受得的普通怪物基础接触异常现在有运行时与网关覆盖：
  暴击火点燃、冰缓/冻结与暴击闪电感电，含规避、到期、免疫、致命点燃/ES 与冻结动作取消。
  `action_speed_cannot_be_reduced_below_base` 在冰缓与冻结下把动作因子保持在 1（网关覆盖，
  未用真实客户端核对）。
  反向冰缓（`chill_effect_is_reversed`）是动作速度增幅而冻结仍停住玩家，
  `all_damage_taken_can_chill` 按任何命中的总伤害冰缓（冲浪者的「变换季节」）。
  下发的属性现在包含原生走位与动作剪辑所读的最终 `virtual_action_speed_+%`；那个
  原生效果未实测。
  原生核对验证真实怪物点燃/规避、致命死亡面板/检查点行为与非致命 RF。
  替代异常与敌对法术适配器仍未实现；怪物迫击炮射击经同一条
  接触管线落地（见下面「怪物投射物」）。
  精确的有限范围见[赌徒](../docs/game/gambler.md)。
  双倍伤害几率把命中翻倍，三倍伤害覆盖它们（[20](../docs/protocol/20-damage.md) §2）。
  `unaffected_by_shock` 保留感电与 `is_shocked` 而不带承受伤害增幅；`unaffected_by_ignite`
  保留点燃与 `is_ignited` 而不带它的伤害。
  「移动时」闪避与回复跟随服务端行走，更多最大攻击伤害在狂怒旁边缩放每次攻击
  命中的最大值；仅网关覆盖（[多神教徒](../docs/game/polytheist.md)）。
  冲击、近距战斗与盾牌碾压按邻近度缩放近战命中（[20](../docs/protocol/20-damage.md) §2j）。
  无条件穿透作用于玩家的出招命中（按元素与元素属性的通用项、仅攻击变体、不可穿透标志；
  DoT 与物理不吃）。
  玩家与技能对同一裸穿透属性的贡献相加，不覆盖任一方来源。
  九项原生核对验证固定 100 火命中随穿透叠层造成 60 → 80 → 90 → 100 伤害，
  禁止时回到 60，冰穿透对零抗性给出 120，混沌 75 → 105。
  每项核对在权威更新之后读实际的原生怪物生命。
  那些抓取属于原始 head `0d800d1`；已完成的弓与目标修正路径需要原生复核。
  目标修正与基础抗性在穿透之前组合；承受伤害跟随减伤并取整一次。
  十个全新的组合原生组验证暴击取整、激活超上限与默认攻击魔力闪电。
  实际混合命中的生命损失在暴击之前是 160/220，然后忽略时 300、不忽略 330、禁止 240。
  构建版本与来源哈希匹配 `1366f48`；受支持宝石、修正与弓的组合仍只有自动化。
- 连续生命/魔力恢复（2026-10-24）：回复、药剂恢复与自身 RF 现在共用 16 ms
  已流逝游戏时间循环，替代回复的 250 ms 跳变；池分数与上限保留。
  以回归测试实现；网关重启后的真实客户端视觉验证待做。
  见 [20](../docs/protocol/20-damage.md)「连续资源更新」。
- 静止时的圣化地面（圣骑士的受膏勇士）：静止半径的地面停在
  玩家站的位置，他移动、死亡或在城镇时消失；他地面的效果（硫磺药剂的也是）
  在最后一次有地面覆盖他之后仍残留，`consecrated_ground_effect_+%` 提升它（每秒生命的 5% 到 7.5%）。
  仅自动化覆盖；地面美术与残留未用真实客户端核对。
  见 [13](../docs/protocol/13-items.md) §8a 与[圣骑士](../docs/game/paladin.md)。
- 玩家施加在怪物身上的减益：每个战吼在施放结束时嘲讽其半径内的敌对怪物，
  玄武岩药剂在它的 30 格内（3 s，`taunt_duration_+%`，`taunted` buff 带它的两个值）；被动敌人光环
  （神性之光的苦修预兆）落在这名活着的玩家附近的敌对怪物身上。
  它们的承受伤害增幅（嘲讽来自「带来战斗」20%，元素 25%）在每次
  玩家命中与正义之火的燃烧里加到怪物自己的之上；`warcries_have_infinite_power` 给坚毅战吼最大充能。
  命中施加威吓与丧失：4 s 内 10% 承受命中伤害增幅（[11](../docs/protocol/11-skills.md) §12）。
  命中施加凋零：每层 6% 承受混沌伤害增幅，最多 15 层（[11](../docs/protocol/11-skills.md) §12）。
  其他战吼自己的 buff、激励与召唤、嘲讽免疫与其他被动敌人光环未做。
  仅自动化覆盖；未用真实客户端核对。
  见 [11](../docs/protocol/11-skills.md) §12 与[圣骑士](../docs/game/paladin.md)。
- **旋风区域效果（2026-10-25）。** `/set_aoe` 现在在技能记录里重发旋风算出的最终半径，
  服务端命中使用同一套半径计算。
  旋风/CycloneAltX 在它们导出的属性集没有半径时用 16 格基数。
  玩家的区域增幅对每个区域技能只计一次（`AreaPlayerStats`）；以前计了两次。
  仅法术区域不影响攻击。
  测过的计算；实测视觉核对待做。
  第一次实测发现仅半径不足以缩放引导视觉；
  它的条件路径现在也收到聚合区域（0xd7）与半径百分比（0x1591）。
  旋风的额外近战距离与阶段修正仍是部分。
  见 protocol/11-skills.md。
- **自目标法术（2026-10-29）。** 自目标法术（ActiveSkillTargetTypes 4 或 10：冰霜新星、解放、
  闪电新星）无论瞄准哪里都在施法者周围命中，和客户端画的一样；以前对怪物施放的冰霜新星
  会在怪物周围爆开。
  图腾、地雷与陷阱技能以及没有自目标的新星（冲击波图腾、风暴爆裂地雷、黑暗契约）保留
  瞄准点。
  有 `nova_spells_cast_at_target_location`（露娜里斯的虔信者）时自目标新星在瞄准点周围命中，
  新星技能记录带 `skill_is_nova_skill`，并且在该属性存在时带推导出的
  `virtual_self_targeted_skills_target_location_instead`，所以客户端在光标处施放它们（2026-10-30 实测）。
  区域技能记录带它们的最终 `active_skill_area_of_effect_radius`，客户端按它绘制；没有它
  冰霜新星什么也不画（2026-10-30 实测）。
  代码：`SkillCatalog.IsSelfTargeted`、`SkillCatalog.IsNova`、Gateway（`SpellCentre`、`AreaSkillHit`）。
  文档：[11](../docs/protocol/11-skills.md) §9a。
- Arcane Might (#364) shares one highest-eligible-ratio consumer across current attacks and damage panels.
  Normal Crown of Eyes, Whisperer's existing consumer and wand mastery use the six modern stat contracts.
  Independent grants do not stack; skill base plus quality still adds before the maximum comparison.
  Weapon-local damage now scales in ordinary and supported attacks; restricted types are cleared from skill panels.
  Automated cases cover actual direct/area target Life loss, snapshots, mastery refund and Crown persistence/removal.
  All 24 inventoried numeric skill constants and three quality records have calculation/data checks.
  Those checks do not enable unsupported skills, variants, mercenaries or Phrecia allocation.
  Iron Will/Strength provenance, missing upstream spell inputs and unrelated leech remain outside this consumer.
  Native user checks confirm ordinary Crown ratios, KB non-stacking and Fireball isolation in the damage panels.
  KB's absolute damage exposed an effectiveness-offset decoding error; raw 4000 now resolves to 140%, not 40%.
  Corrected absolute damage and mastery lifecycle still need native verification.
  See [scope and coverage](../docs/game/arcane-might.md) and [20](../docs/protocol/20-damage.md) §2c.
- Ruthless resolves native skill/gem, passive and mod alternatives per connection, including zero-valued overrides.
  Ordinary characters in the same Gateway keep ordinary catalogs.
  Hardcore death is terminal for the old connection; a queued level-up cannot revive it.
  Hardcore migration and permanent Ruthless death have automated lifecycle coverage.
  Earlier native checks were against the original fork; native checks on the current integration are recorded separately.
  Native skill/passive/mod alternatives and late-action guards have automated coverage.
  Main's projectile kill path retains Ruthless rewards, gem XP and active utility-flask restrictions.
  Synchronous quest/gem commits retain the Hardcore terminal-progress guard; composed regressions pass.
  See [league modes](../docs/game/league-modes.md).
- Grace Period grants 60 seconds of entry protection outside towns and hideouts.
  First movement, a skill, a flask or expiry ends it; incoming damage, hostile targeting and RF damage are suppressed.
  The native buff uses floating-point seconds for its countdown; the icon was checked live, timer retest is pending.
  See [11](../docs/protocol/11-skills.md), "Area-entry Grace Period".
- Vaal souls (#308): genuine owner kills collect into bounded shared ActiveSkills-row budgets.
  Creation and skill updates publish the proved native Actor lists; automated wire/dispatcher checks pass.
  Duplicates, source/cost changes, removal/readd, range, prevention, queued sends and transient lifecycle are covered.
  Eligibility commits with lethal mutations before death notifications, including shared hits and Righteous Fire.
  Vaal aura costs use per-instance Aura types and accepted supports, with explicit developer replacement precedence.
  Player soul loss commits at zero Life; delayed death notices preserve actual resurrection earnings.
  Vaal gem/support requirements use current equipment snapshots while gear notifications are held.
  Source checks on base `939b44fe` preserve wide raw attributes, mana, pool flags and supported skill payment.
  Skill-record bit1 (0x02) now retains native Vaal soul inputs using catalog raw stored uses in both lookup paths.
  Parsed creation/update and real socketed tests cover zero cost/balance, ordinary partners and inactive hosts.
  A pinned 3.29.3b session passes 42 collection/counter checks: real kills, cap, cost, zero capacity and prevention.
  Native required-soul and manager consumers agree, including natural gem level-ups and unsupported-input refusal.
  The build and all three suites pass; exact private composition with PR #294 also passes.
  Distribution and capacity are documented LocalExile policies, not a claim of official algorithm parity.
  Native duplicate/removal, death/relog/transfer, aura costs and held-notification composition remain unverified.
  Unsupported Vaal casts are refused before action echoes or cooldown reservation, including effective zero cost.
  Vaal Ground Slam now has a shared executor, atomic soul/Rage payment and pausable prevention timer.
  Attack readiness commits before payment/action reservation, including object-targeted flags bit0.
  Other Vaal executors remain explicit refusals; the bounded modular native session did not repeat a full Vaal matrix.
  See [Behemoth](../docs/game/behemoth.md) for execution models and source ownership.
  See [27-vaal-souls.md](../docs/protocol/27-vaal-souls.md).
- Ground Slam now hits as the forward wave its data action names, not as one circle: the skills whose ActionTypes action
  is "DistanceScaledGroundSlam" (GrantedEffects GroundSlam, GroundSlamAltX, GroundSlamAltY, GroundSlamRoyale and
  PlaytestSlam) send a sector from where the player stands whose front advances to the skill's reach, each monster hit
  as the front arrives it and the rest of the sweep dropped on death, area change or a closed connection. Vaal Ground
  Slam's own skill is a different action, so its 40-cell radial blast is unchanged, as are Ice Nova's ring, Cyclone,
  Rolling Magma and every other circle. The player's gem resolves to GrantedEffects "GroundSlam"; the +40% final radius
  and 4 melee repeats belong to the old "ExileGroundSlam" row, which no GemEffects row grants. The tables carry no wave
  length, base angle or speed (the four `ground_slam_*` shape stats are held by no player stat set and no Mod of the
  3.29 table), so `GroundSlamWave` holds three estimates — a 60-cell length at the generic 12-cell area radius, moved by
  however far the skill's own radius sits from it; half angle 30 deg (half the 60-degree cone `ground_slam_cone_angle_override`
  names), grown by the gem's `ground_slam_angle_+%`; and 60 cells a second — to calibrate against a live measurement.
  The wave's close-range damage (`groundslam_damage_to_close_targets_+%_final`, real in the gem's own stats) is still
  applied by the Vaal executor only. A live session's 0x0024 object uses reach the same sweep, with the wave's origin
  already the post-walk swing position, so the gap between its object uses and its sweeps was swings whose hit the server
  dropped (22 of 80 swing echoes), not a missing area part. Client fact (measured live 2026-11-07): clicking a monster
  and immediately walking away plays the whole swing and draws the wave, so the client queues the Move instead of
  cancelling the attack; the server was dropping the paid hit on the action counter alone. A gem melee swing
  (`holdsItsSwing`: an attack that is Melee, not channelled) now keeps its hit over a moved action counter, and only a
  death, an area change, a pause, a freeze or a stun's hit recovery ends it. This was every gem melee attack, not Ground
  Slam alone (Cleave, Sweep, Ground Slam's alternate-quality forms and Vaal Ground Slam's own blast all take the same
  path), and each such swing's contact now comes from its own rig clip (`GemAttackContactDelay`: groundslam_maul's 0.633
  of the attack, where the class's attack1_maul, the default attack's swing, is 0.692) — both are logged with the sweep.
  Not yet checked with the real client.
  Remaining: the client queues its Move until the clip's cut-in (Lockstep.cs) while the server walks on the Move as it
  arrives, so the server's position leads by up to about 0.3 s during a swing; holding the Move until the contact
  (`HeldMove`) would align them, and needs `Messages.cs`, `PlayerAilments.cs` and a `ClientState` field.
  Code: `SkillArea.cs` (`GroundSlamWave`), `Combat.cs` (`AreaSkillHit`, `GemAttackContactDelay`),
  `AnimationContacts.cs` (`SkillAttackContact`).
- Warcry release (Enduring Cry, the only warcry executor): the cry's time follows warcry speed and action speed as the
  client's does, and its release runs the shared warcry effects within its radius: corpse explosions that consume the
  corpse and quest chests opening (Deathrattle).
  The explosion takes the player's global damage increases and conversion.
  Every one-use warcry, for every class (Enduring, Rallying, Intimidating, Seismic, Infernal, Ancestral, General's and
  Battlemage's Cry), now uses the generic one-use cooldown, with `warcry_cooldown_speed_+%` added to
  its recovery as the client adds it; the live cooldown checks under "Instant skills and cooldowns" ran before
  warcries took that path.
  Gateway coverage; not yet checked with the real client.
  Each exploding corpse shows a generic gore burst; the native art is not traced.
  Global flat added damage and flat crit chance (the wiki's Explode page applies both), on-hit effects and the
  client's 0.22 s wave delay are not modelled.
  Code: `Warcries.cs`, `UseChargeSkillCore`, `SingleUseCooldown.cs`.
  Docs: [11](../docs/protocol/11-skills.md) section 10i.
- Strike targeting: a melee Strike's spirit strikes (`melee_attack_number_of_spirit_strikes`) hit the monsters the
  client picks, mirrored from its scan and its seeded shuffle, for gem Strikes and the default attack.
  With `melee_strike_skill_strike_previous_location` a Strike also hits where its previous use landed, and the
  server sends the `skill_location_memory` buff with that first use, from which the client records the later uses and
  strikes the previous location itself.
  Gateway coverage; not yet checked with the real client.
  Not mirrored: the client's line-of-sight test and its choice among more than 20 candidates.
  Not for the Strike gems typed Projectile (Frost Blades, Lightning Strike, Molten Strike, Wild Strike and their
  transfigured gems except Frost Blades of Katabasis, which has no Projectile type and gets both):
  the server launches only their projectiles and has no melee hit for them, so the spirit strikes the client draws
  for them deal no damage.
  Code: `StrikeTargets.cs`, `Strikes.cs`.
  Docs: [11](../docs/protocol/11-skills.md) section 10d.
- Passive enemy auras: a passive's BuffTemplates enemy aura puts its buff on hostile monsters in its radius, and its
  hit conversion converts their hits (Born under Fire).
  Auras with stats the server does not apply yet are neither sent nor applied.
  Gateway coverage; not yet checked with the real client.
  Code: `PassiveBuffAuras.cs`, `MonsterSwing`.
  Docs: [11](../docs/protocol/11-skills.md) section 10e.
- Passive-triggered skills: Ancestral Commander's Spirit chieftain options trigger on a Unique monster's crit, Savage
  Hit or blocked hit, spend their shared 20 s cooldown and give the chieftain's Leadership buff for 12 s (with increased
  skill effect and minion duration), during which
  that cooldown does not recover; death ends the buff.
  Kahuturoa's share of armour against elemental and chaos hits and Kiloava's Life, Mana and ES on block act.
  Server monsters have no base critical strike chance, so Kahuturoa's crit trigger fires only on a Unique with a
  forced always-crit mod (`/mob_mods`), never in normal play.
  The chieftain rises beside the player as his minion and fights the monsters near him until the buff ends.
  Gateway coverage; not yet checked with the real client.
  Not modelled: the chieftain's skills past its first, the skills' records (the client shows only the buff), Utula's
  leech and Rakiata's Mark trigger (see "Not working yet"), and carrying the buff, minion or cooldown through an area
  change.
  Code: `PassiveGrantedSkills.cs`, `SpiritChieftains.cs`, `MonsterSwing`.
  Docs: [11](../docs/protocol/11-skills.md) section 10f, [20](../docs/protocol/20-damage.md) section 2e.
- Player minions that fight: a minion on the player's team, marked as his for the client, walks with him, goes for
  the nearest hostile monster near both, swings its first skill with his minion modifiers (its base hit from
  DefaultMonsterStats `MinionDamage`, the ally table), and its kills give him
  experience and loot; the instance save, his area hits and his on-kill charges leave it out.
  This first-skill controller serves Spirit chieftains; golems use their own controller below.
  Gateway coverage; not yet checked with the real client.
  Code: `PlayerMinions.cs`, `MinionAttacks.cs`, `RunMonsterAi`.
  Docs: [11](../docs/protocol/11-skills.md) section 10h.
- Life Recoup returns a share of each hit taken over the next 4 s ([20](../docs/protocol/20-damage.md) §5).
- Ancestral Commander defences: `damage_reduction_rating_from_body_armour_doubled` doubles the body armour's armour
  (and, with Iron Reflexes, its converted evasion) before the increases.
  `missing_unreserved_life_%_gained_as_life_before_hit` gains that share of missing Life before every non-evaded
  monster swing and dev hit, blocked ones included (Life reservation is not modelled).
  `movement_speed_cannot_be_reduced_below_base` bounds the movement speed total below by 0, as the client does from
  the same flag.
  Gateway coverage; not yet checked with the real client (move into the armour and movement speed entries under
  Works after a live check).
  Docs: [ancestral-commander.md](../docs/game/ancestral-commander.md).
- Aura restoration bootstrap fix: socketed gem requirements no longer deactivate their entire host item during pool
  initialization.
  Equipment contributes attributes before individual gem validation;
  saved auras remain candidates on town/hideout round trips.
  Regression covers fresh-connection Precision/Clarity equipment and all 17 supported self auras.
  Live retest pending.
- Aura lifecycle: towns suppress all aura effects/reservation while retaining per-character enabled intent.
  Exit/relogin/character return restores available remembered auras without rechecking cast affordability;
  manual activation still validates mana.
  Death clears saved/runtime auras; skill refresh resumes temporarily unavailable effects.
  The shared own-aura restriction has separate native controls above.
  Discipline's town, death and gem-host lifecycle also passed historical native checks in LocalExile.
  Fresh merged-branch and other aura-family round trips remain partial.
  See protocol/11-skills.md.
- Aura presentation (2026-10-25): self auras select their distinct EPKFiles2 art.
  A live check found that source-only buffs hid the top-left icons;
  source and received buff instances now coexist, restore and end together.
  Icon restoration awaits client confirmation.
  See [11](../docs/protocol/11-skills.md) section 13.
- Ordinary aura expansion (2026-10-25, awaiting client confirmation): Determination, Grace, Anger, Hatred,
  Wrath, Zealotry, Malevolence and Envy now activate with reservation and paired visual/HUD buffs.
  Armour/evasion, added hit damage, gain-as-cold, spell damage/crit and RF's more DoT are connected.
  Discipline's ordinary ES pool and recharge are verified separately above; conditional effects remain partial.
  Reservation overflow is refused.
  See [11](../docs/protocol/11-skills.md) section 13.
- Flesh and Stone and Blood and Sand share a persisted stance: recasting switches both without releasing reservation;
  `/auras off <name|effectId|all>` explicitly disables them. Blood and Sand modifies melee area damage and area.
  Flesh and Stone uses the current gem's distance-dependent more physical hit damage / less incoming hit damage;
  the obsolete blind, global reduced-damage and physical-DoT bonuses are removed. Native stance switching and off passed.
  Code: `AuraCatalog` (`FleshAndStone*`, `RawStats`), `Game.ToggleAura` / `EnemyDebuffsTick`, `MonsterMods`.
- Passive-granted skills: a passive's `GrantedEffectsPerLevel` skill joins the gem skills at the row's fixed level,
  with a stable id, no supports, quality 0 and no requirements; it casts, reserves, saves and ends like a gem's.
  Only self auras are listed until other granted skills (minions, other kinds) have executors.
  A granted triggered skill is never listed and runs from its trigger.
  Summon Elemental Relic (Paladin's Elemental Valiance, the ordinary Guardian's Unwavering Crusade) rolls on kills and
  on hits against rares and uniques, behind its cooldown, in Ruthless too.
  Used by the Paladin Aura options (Determination, Grace, Discipline); automated coverage only, not checked with the
  real client. See [11](../docs/protocol/11-skills.md) section 13 and [Paladin](../docs/game/paladin.md).
- Owned minions: relics and chieftains remain timed; all 17 playable golem gems summon, fight and take generic hostile
  hits/projectiles. Golems follow, scale with gem level/quality and supports, grant their distinct owner buffs and save
  activation across areas. Local rule: each activated golem returns two game seconds after death while its source is
  usable. All variants summoned in the real client; ordinary combat, buffs and repeated returns checked live.
  Nearby ordinary auras now affect owned fighters. Partial: exact official AI priorities, some support interactions,
  sharing golem owner buffs with other players and bespoke boss damage.
  See [11](../docs/protocol/11-skills.md) section 10h; relics remain in section 13 and [Paladin](../docs/game/paladin.md).
- Link skills: Soul, Flame, Vampiric, Protective, Intuitive and Destructive Link link one of the player's minions when
  he has `link_skills_can_target_minions` (Deep Connections); with no party play there is nothing else to link.
  A link lasts its duration, costs mana every second, breaks after 4 s out of range or when its cost is not paid, and
  its target's death kills the player (a refund of the relics' passive ends the link without one).
  `number_of_active_links` goes to the client; Absolution and Field Medicine use
  the links. Not modelled: the beam, line of sight, the gems' effects on the target, and party allies.
  Automated coverage only, not checked with the real client.
  See [11](../docs/protocol/11-skills.md) section 13 and [Paladin](../docs/game/paladin.md#link-skills).
- Saved aura toggles (2026-10-25, awaiting client confirmation): active effects persist per character and restore on
  relog, area entry and server restart, subject to usable gems and available reservation.
  Helena's picker now receives all 103 exported personal hideouts, including MTX templates;
  switching templates now uses the native selection request and persists the chosen home.
  See [11](../docs/protocol/11-skills.md) and [21](../docs/protocol/21-hideouts.md).
- Spells disabled (`your_spells_are_disabled`, Devout of Gruthkul): every spell use is refused on the warcry and
  own-aura admission routes, and the skill record carries `skill_disabled_by_stat`.
  Running spell auras are suspended with their intent kept and restored after the source goes; a burning Righteous
  Fire keeps burning the player and deals enemies nothing.
  Local policy for already-running effects; Gateway coverage only, not checked with the real client.
  Code: Gateway (`SpellsDisabled`, `ActorSkillDisabledCore`, `SuspendRestrictedOwnAuras`).
  Docs: [11](../docs/protocol/11-skills.md) §10j.
- Passive blind aura (Devout of Solaris, Born in the Shadows: BuffTemplates `SaboteurBlind`): living hostile monsters
  within 35 cells get the `blinded` buff each player tick, which a death, a refund or leaving the radius takes off.
  Blind from it or the smoke cloud lowers their accuracy and evasion by 20%, lets
  `critical_strike_chance_+%_vs_blinded_enemies` raise the player's critical chance against them (default attack, area
  sweeps, footstep skills, projectiles) and `damage_taken_+%_from_blinded_enemies` lower their hits and ignites.
  Hits Blind by their on-hit and critical-strike chances for 4 s ([11](../docs/protocol/11-skills.md) §12).
  Other passive auras do nothing yet.
  Gateway coverage only (a Polytheist and an ordinary Saboteur), not checked with the real client.
  Code: Gateway (`PassiveBuffAuras`, `TickPassiveAuras`, `Game.Blinded`, `CriticalChanceOn`).
  Docs: [11](../docs/protocol/11-skills.md) §12.
- Passive-granted footstep skill (Devout of Abberath: level 20 Abberath's Fury): while the player walks, each footstep
  of the Templar's run animation (two per 800 ms, faster with movement speed and slower when chilled) explodes at the
  player's feet with the skill's art and hits every living hostile monster within 7 cells (grown by spell area) as an
  area spell without a gem: 300-450 fire with the player's increases, critical strikes, block and the usual on-hit
  effects.
  Not in town, the grace period, while frozen, stunned or dead, nor with spells disabled; a movement skill's blink is
  no walk, and so is Shield Charge's, which the wiki says triggers it (a known gap).
  A landed hit ignites the monster with its 25% chance or a critical strike (the player's ignites on monsters below).
  The radius comes from the explosion art (no table gives one), and other passive-granted skills do nothing yet.
  Gateway coverage only, not checked with the real client.
  Code: Gateway (`PassiveGrantedSkills`, `RunFootstepSkills`, `TriggerFootstepSkill`).
  Docs: [11](../docs/protocol/11-skills.md) §12.
- **Projectile modifiers.** Generic profiles consume resolved primary counts, speed, pierce/fork/chain/split/return
  grants and independent prohibitions in the sole staged runtime. Ordinary bows and catalog-known projectile gems
  work without a skill provider. A basic recipe is straight flight, not complete special-skill behavior.
  Return opens another hit-admission phase and reads the shared returning-damage multiplier. A client-drawn spell
  rolls its partial return chance per projectile with the client's seed (static RE, checked live 2026-10-31: the
  projectiles that came back were the ones that dealt return damage), and flies the client's 150-cell range out and
  past the caster on the way back (`projectile_maximum_range_override` replaces it). Return chance,
  return speed, contact budgets, overshoot and terrain behavior are configurable through skill definitions.
  Boolean fork sources grant one generation, and raw bow always-pierce sources bind to the same common profile.
  One movement instance is capped at 150 cells (15 m) through `projectile_maximum_range_cells`; fork, chain and split
  start a new instance with a fresh budget, so total travel can exceed 15 m. Return retains remaining continuation
  counts and the original lifetime; return pierce-all requires its own stat.
  Action speed does not change projectile range: projectile speed only shortens the flight, and `action_speed_-%` is
  composed additively with chill/freeze for attack, cast and movement pacing; Tailwind/Harbinger sources remain #246.
  Direct and area snapshots keep area increases/supports scoped to their damage stages.
  Point Blank scales attack projectile hits by travel ([projectiles](../docs/game/projectile-framework.md)).
  Area effects scale to the committed radius when the selected art exports a native radius; unknown art stays unscaled.
  Terrain-blocked area centers are omitted without resampling or reserving time; surviving areas keep consecutive intervals.
  Skill explosions, pulses, children, forms, native art and specialized resources belong to skill definitions.
  Missing metadata or unsupported damage plans are refused before costs, without generic area-hit fallback.
  [Named skills](../docs/game/projectile-skills.md) implement terminal Fireball blasts and Kinetic Blast clusters.
  Burning Arrow uses catalog flight with its 100% fire conversion, a 25% base ignite chance (critical hits always
  ignite) and Vigour's fire added from maximum Life; projectile ignite burns monsters through their fire resistance
  with the native `ignited` buff ([20](../docs/protocol/20-damage.md) §3). Automated only; native ignite visuals and
  damage still need a user-run check.
  Direct volleys share admission separately outbound and returning; denied siblings pass through.
  Fireball waits for exhausted continuation and gives the terminal victim one area-classified hit.
  Normal KB creates four offset explosions with 40% less area damage; Clustering creates three plus quantity modifiers,
  keeps one primary and cannot Split.
  The internal AltY row retains normal-KB compatibility, without establishing a third playable gem.
  Separate blast/placement radii use supported area stats once; independent explosions can overlap covered victims.
  Each KB contact group excludes its own triggering enemy, including delayed explosions; other groups may hit it.
  Automated checks cover large triggering victims, pierced neighbors and wall/range groups. Gateway actor radii
  remain zero, so live area coverage uses monster centers rather than calibrated body intersections.
  KB centers use a fixed scaled circle and random sector angles, with separate first-delay/inter-slot parameters:
  immediate first blast, then 1/30 s between slots. These are user-tuned feel values, not official measurements.
  Secondary explosions use their own small-burst art and equipped MTX; native art scales to the actual blast radius.
  Skill-bar explosion totals use the resolved cluster recipe, including quality and Clustering's projectile conversion.
  Fireball wall-contact damage and default/Stygian packets have regression coverage; visual visibility remains unverified.
  Flights retain fixed terrain-plus-offset height (bare offset without a grid); explosion effects follow client terrain.
  A player's primary shot flies at the higher of its release and aimed ground: terrain rising more than 12 steps above
  that stops it, so a shot aimed below a cliff hits the face and one aimed on top clears it
  ([framework](../docs/game/projectile-framework.md), "Aimed height"). LocalExile model v1, awaiting a native check.
  Official geometry/timing, surface height and art equivalence still need client validation.
  Code: Core `Projectiles/`, Gateway `Projectiles/` and `Projectiles/Skills/`; see the
  [framework guide](../docs/game/projectile-framework.md).
- **Instant skills and cooldowns (verified live 2026-10-21).** Haste, the Purities and Righteous Fire carry
  `skill_is_instant`, so the client sends them as C→S 0x191 and keeps walking;
  the server applies them and answers S→C 0x1ce, which starts the skill bar's cooldown.
  Bladefall of Trarthus no longer hangs the client (its volley interval is in the record) and runs as its toggle: a
  volley hitting the area around the player every interval, ground blades, the mana spent per second until it runs out
  (live 2026-11-07: volleys and hits checked; the end at no mana not seen yet).
  Skill records retain GEPL Cooldown as the native base and carry the cooldown count.
  Ordinary one-use spells/attacks now resolve player and skill generic recovery once, and the server keeps remaining
  base-time budget through stat changes and pause.
  Preflight reserves before action/movement/echo side effects; queued effects retain the original action/area ticket.
  Real-client Dash initial +100%/-50% recovery, early raw rejection without movement/echo/mana, and pause checks pass.
  The native positive-rate cache required an explicit cooldown-state refresh after skill-stat changes;
  this preserves remaining/total base time at the send gate without consuming another use.
  Native Dash mid-cooldown recovery changes now pass live: one entry changes rate with base/total/progress preserved,
  early input has no echo/cost, and reuse succeeds after native expiry.
  Wire, active/expired/zero-rate and queued encrypted-send checks also pass.
  Multi-use, duplicate same-effect gems, shared groups, extra uses, bypass, typed/conditional/more recovery and official
  tick equivalence remain outside this slice.
  Skill records carry the GEPL Cooldown and the stats the cooldown count, and the server refuses a skill until its
  cooldown is over.
  Ground-target C→S 0x190 now frames its 12 bytes and routes ordinary Frostblink through mana/cooldown acceptance.
  The ground 0x1ce uses the raw aim; the server mirrors native 32/42-cell distance limits and ends the old walk.
  Placement requires a captured grid and clear terrain, barriers and endpoint clearance.
  Action tokens and pending releases remain unchanged; other location instant movement is withheld.
  Actual Q input and a cooldown retry matched native/server positions, with two 12-mana costs and two confirmations.
  Early retry, noninstant Fireball and unsigned-invalid coordinate requests changed no position or mana.
  Manual native experiments separately verified near/far clamps and the old predicted walk ending after a blink.
  Hovered-monster Frostblink sends 0x191 and now shares movement acceptance, with an object-bound 0x1ce.
  Its size-aware object candidate trace precedes the distance clamp;
  it does not reuse the monster centre as a ground aim.
  Both preflight and the send gate require a living known-size monster with no active walk, and recheck placement.
  Invalid preflight changes no mana/cooldown; later invalidation withholds confirmation under the existing cost policy.
  Automated actual-send tests cover current target/player sampling, target death/walk, pause, transfer, barriers and unequip.
  Four manual native object confirmations verified clear near/far/diagonal/off-axis endpoints.
  A reflected terrain fallback is deliberately rejected.
  Actual hovered Q on a stationary monster emitted 0x191 and one object-bound 0x1ce with flags 0x20.
  From `(280,220)` against `(300,220)`, native/server position became `(317,220)` for one 12-mana cost.
  A framed ground retry at 0.5 seconds changed no mana or position and sent no extra confirmation.
  Combined #197 synchronization retained base 3000 ms, total 3 seconds and rate 1.
  Production framed off-axis/far object requests matched native/server endpoints with one 12-mana cost each.
  Framed moving-target, reflected-terrain, missing-target and noninstant requests changed no mana or position.
  They sent no confirmation, and the invalid-combination control kept native cooldown entries empty.
  Production movement during an active walk has not been measured.
  Frostblink damage, chilled ground, enemy-dependent recovery and shared Blink cooldown groups remain unsupported.
  The skill bar keeps its slots when skills are re-sent or a gem is socketed again (the saved bar goes out as 0x53)
  ([11](../docs/protocol/11-skills.md) §9, §11).
  A slot's "Always Attack Without Moving" (C→S 0x4f) is saved and goes back in 0x53, so it stays ticked.
- **Auras (the simple set is verified live).** Reservation is read from GrantedEffectsPerLevel (`ManaReservationFlat`
  / `ManaReservationPercent`), reduced by the passive/mastery reservation efficiency and sent as the Life pool's
  reserved mana (and in every 0x1b1 mana update).
  Aura skills toggle on cast; Purity of Elements/Fire/Ice/Lightning, Clarity, Vitality, Precision, Haste,
  Determination, Grace, Discipline, Anger, Hatred, Wrath, Zealotry, Malevolence and Envy apply their gem's level and
  quality effects and buff icons; their 3D effects (`affected_player.epk`, `faster_attack.epk` …) do not render yet.
  Pride applies its enemy debuff: every hostile monster in its radius carries BuffDefinitions 1331 and takes its
  `physical_damage_aura_nearby_enemies_physical_damage_taken_+%` (19% at level 20) rising linearly to `_max` (39%)
  after 4 s, both scaled by aura effect; the ramp resets on leaving the aura and feeds hits and physical damage over
  time. Live 2026-11-01: `Auras: Pride (0 flat, 50%)`, `0x01c1` with row 1331 on a Rhoa, `0x01c2` on toggle-off, and
  death cleared both.
  The skill-bar active frame and rotating circle now receive the buff's skill id/action hash association (0x1c1 bit3),
  including area-entry restoration; automated checks pass, live icon verification pending.
  Permanent spell buffs, five heralds, Skitterbots and Temporal Rift share source/reservation reconciliation.
  Purity grants elemental-ailment immunity; Arctic Armour gives stationary hit mitigation, freeze immunity and chilled
  trail; Tempest Shield grants shield spell block, shock immunity and block retaliation. Herald kill/poison procs use
  the ordinary damage/minion pipeline. Temporal Rift recalls position/resources from four seconds ago, with native proof.
  Nearby player auras reach allies and controlled fighters using caster effect; duplicate families select each stat's
  strongest value. Life reservation uses an independent budget, Arrogance and pool/recovery caps; curse auras use
  the hex lifecycle. Skitterbots apply scaled chill/shock and trigger placed traps/mines, rearming a mine once;
  full trap/mine skill-specific payloads, banner/Vaal executors and Agony's retail Virulence decay remain partial.
  ([11](../docs/protocol/11-skills.md) 13).
- Summon/mine/trap input and cleanup work; special payloads remain partial ([11](../docs/protocol/11-skills.md) 10h).
- Hexes/marks have limits and combat effects; reflection and special payloads remain partial (`MonsterCurses`).
- **Totems (Rejuvenation Totem only).** The gem's use summons its totem after the 0.6 s summon, at the aimed cell:
  a team-0 monster object with the variety's minion Life at the gem's level requirement, its duration and the limit
  (1 plus `number_of_additional_totems_allowed`; past it the oldest dies).
  Totems live apart from the monsters, so area hits, monster AI, loot and the instance save never see them.
  A totem dies at Life 0, at the end of its duration, when replaced or when the player dies, and leaves a second later.
  Its aura regenerates the player's Life within its radius, with buff 172.
  `damage_removed_from_your_nearest_totem_before_life_or_es_%` moves that share of a monster or `/hit` hit, after
  mitigation, to the nearest totem's Life (Forest of Ancestors, Spirit of Kaom); covered by Gateway tests.
  Not yet: other totem skills (their use places nothing), monsters attacking totems, placement range and server-side
  totem collision; not yet checked with the real client ([11](../docs/protocol/11-skills.md) §10g).
- Passive self buffs (`PassiveBuffAuras`): a passive's BuffTemplates buff on the player adds its stats to the
  passives' sum and is sent with 0x1c1 after an allocation and on area entry, and taken off with 0x1c2 on a refund
  (Surfcaster's Ghost of the Deep).
  Only the Gateway tests cover it; the icon and whether the client keeps the buff after a death are not checked live.
  Enemy and ally passive auras are not applied.
  Code: `PassiveBuffAuras.cs`.
  Docs: [11](../docs/protocol/11-skills.md) §12.
- Onslaught on kill grants the Onslaught buff and its speed ([13](../docs/protocol/13-items.md) §8a).
- Kill-driven buffs from item stats: `player_gain_rampage_stacks` starts a Rampage streak and its kills feed the
  "1% increased Rarity of Items found per X Rampage Kills" stat (whole groups of X); the melee-hit source keeps
  granting stacks on hits as before. The "on Rampage threshold" modifiers now trigger once per tier crossed: life
  recovery, dispelling ailments, physical damage immunity and phasing for their listed durations. The per-stack
  modifiers for area of effect (per 25 kills) and Chain Hook / Shield Charge attack speed (per 10 kills) count, and a
  kill grants the extra Vaal Soul while the streak is active. `gain_soul_eater_during_flask_effect` grants Soul Eater
  while a flask effect runs: 5% increased attack and cast speed and 1% character size per soul, one soul per kill
  within 60 cells, a base maximum of 45 plus item stack modifiers, and one lost every 0.5 s after 4 s without a kill.
  The player gets its icon and count
  (`soul_eater_player`, 279) and the invisible `soul_eater_aura` (277), the client's `using_flask` flag is sent, and each
  soul draws the official `ServerBeamEffect` line from the corpse to the player (see
  [11-skills.md](../docs/protocol/11-skills.md) §12).
  Soulthirst's loss of eaten souls on flask use is not implemented.
  Not modelled at present: the threshold smoke cloud (no timed smoke ground), Unholy Might (no Unholy Might system),
  the frenzy charge every 50 kills (the charge session has no Rampage-count rule), starting a streak at maximum
  endurance charges and losing them when it ends, and the map/leaguestone Rampage modifiers.
  Code: `Rampage.cs`, `RampageSession.cs`, `SoulEater.cs`, `SoulEaterState.cs`, `GemSupports.Rules.cs`, `VaalSouls.cs`.
- Onslaught (on kill and a Silver Flask's) grants Path of Building's Speed modifier: 20% increased attack speed in the
  attack-speed stat and the swing timing, 20% increased cast speed in the cast-speed stat, and 20% increased movement
  speed in the walk ([13](../docs/protocol/13-items.md) §8a).
  Code: `UtilityFlasks.cs`, `OnslaughtOnKill.cs`.
- **Stats.** The client's stat key is the Stats.dat row + 1 with the vendored schema
  ([11](../docs/protocol/11-skills.md) §8); `/set_stat` takes stat names or keys.
  The [357-stat ledger](../docs/dev/issue75-stat-ledger.md) tracks current individual review state and remaining
  scope. The inventory validates exact membership; these states do not count completed implementations.
  The player's life, mana, accuracy, armour and evasion come from its class and level, with the attributes' bonuses
  (energy shield, evasion and melee physical damage increases);
  accuracy also takes its flat/increased passives, weapon-specific stats and Accuracy Mastery (rating for the panel,
  the two target-conditional effects at the roll), and the final rating value is
  sent as `main_hand_accuracy_rating` / `off_hand_accuracy_rating` for the Offence panel;
  armour and evasion also take their flat/increased passives and the base equipped armour pieces (body, helmet,
  gloves, boots); Life/Mana also take signed per-level and complete final-attribute-group sources before their
  existing flat/increased projection. Its final floor remains a documented local choice; current automated results
  are in the attribute entry above, with new native checks pending.
  See [11](../docs/protocol/11-skills.md) §4.
- **Attack speed passives.** Generic, melee, one-/two-handed, weapon-family, shield and dual-wield passive attack
  speed is summed into the client's `main_hand_attack_speed_+%`;
  dual wielding adds PoB's 10% more and the server's unarmed attack uses the same final value.
  Attack gems apply their extracted inherent speed multiplier on the server only; the client applies its own, so the
  total base duration it is sent stays the weapon's. Pacing, contact/release timing, channel intervals and selected APS
  share the server's duration. Frida verified the clips with a 714 ms bow: ordinary 0.714 s, Burning Arrow 1.02 s.
  An attack's server-created projectile is released 60 ms before its contact (`AttackReleaseDelay`), for the time before
  the server reads the action, its tick and the output queue. Burning Arrow's arrow arrives about 25 ms after the client's
  muzzle flash (was about 75 ms); the default arrow 0-52 ms after its contact, in two clusters (about 265 and 319 ms).
  Supports, conditional/per-charge speed, special time overrides and attack-gem damage/DPS remain separate work.
  Selected attack-gem main-hand ranges now share the converted combat packet, including skill effectiveness.
  All twelve ranges clear absent types; pre-crit averages retain fractions and DPS uses the emitted APS value.
  Creation and refresh pass raw character stats and the actual weapon separately from inherited display values.
  Native Burning Arrow showed 9-23 fire/total, physical zero, per-use 16.52 and DPS 16.19 at APS 0.98.
  Added cold damage showed 9-9 cold and 18-32 total; clearing it removed the cold rows.
  Relog retained the base result.
  A noncritical server hit dealt 24 damage, while ordinary attack retained its original ranges and averages.
  Packet, catalog, conversion, source-isolation and native row-format tests also pass.
  Ordinary attacks, spells and off-hand display paths are unchanged.
  Conditional/support effects, crit weighting and broader weapon modifier scope remain separate work.
  The default attack also sends the panel's `hundred_times_damage_per_second` and
  `display_hundred_times_damage_per_skill_use`.
  Live verified: the dual-wield character's APS follows the passives (2.93 → 2.98 after a node).
  Minion, totem and timed/conditional stats are not modelled;
  the character panel's APS stays cached after a passive allocation (reopening the panel or switching its tab does not
  refresh it; only a relog does).
  ([11](../docs/protocol/11-skills.md) §4)
- **Spell cast timing (#192).** Catalog base/virtual cast milliseconds now stay unchanged for native spell actions,
  which apply cast speed themselves.
  Server waits and displayed casts per second share `SpellCastTiming`; attacks keep their existing path.
  The old -80% Fireball recovery capture lasted 18.75 seconds instead of 3.75 due to two divisions by 0.2.
  Automated 0/+100/-80 timing, instant/channel classification, invalid-rate and existing aura/sum checks pass.
  Real-client Q Fireball clips measured 3.750000157 s at -80%, 0.375 s at +100%, and 0.75 s at zero.
  Native virtual base stayed 750 ms; emitted cast rates were 0.27, 2.67 and 1.33 casts/s respectively.
  Projectile release/contact alignment was not separately measured.
  "More" cast speed (Spell Echo's 40%, Screaming Reels' 60% with a Fishing Rod, Fanaticism's, the totem supports' and
  the other sources of the native combined final) uses the native calculator's formula in the server wait and in the
  spell record's `cast_speed_+%`.
  That record value now also includes a support's own cast speed (Faster Casting), as the server wait already did.
  Neither change is checked with the real client; only the Gateway tests cover them.
  Area spells without a projectile (Ice Nova) release at their `spell_aoe` clip's contact (Witch 266/766 of the cast,
  `AnimationContacts.SpellAreaRelease`; the clip choice read from the IceNova action's code), no longer at the end of the
  cast (about 450 ms late for a 700 ms Ice Nova).
  Ice Nova's victims are hit as its ring reaches them, over 0.2 s (`IceNovaRing`), and a released ring still lands when
  the player moves or casts again.
  Not checked frame by frame with the real client yet.
  Instant casts remain instant; channel damage cadence and conditional action-speed mechanics are unchanged.
  Numeric server durations below 1 ms are floored, and invalid factors/unrepresentable waits produce no spell effect.
  Code: `SpellCastTiming.cs`, `AnimationContacts.cs`, Gateway (`GemSkills`, `CastMs`, `CastGemSkill`, `LaunchProjectile`,
  `AreaSkillHit`).
  Docs: [11](../docs/protocol/11-skills.md) section 9.
- **Selected spell hit damage panel (#188).** Spell total/per-type ranges and historical pre-crit average damage/DPS
  now use the same converted packet as projectile and area combat.
  All range fields are explicit, including zeros for absent types; hitless spells clear inherited weapon DPS/per-use.
  Final selected spell records use the supported combat packet for spell and inherited main-hand ranges.
  The issue #75 review correction now weights ordinary/supported average damage and DPS with current critical
  chance/multiplier and signed luck, retaining exact action duration and clamping the native integer bounds.
  Attack estimates also include normal-reference landing probability and separate critical confirmation, using
  the ordinary/selected accuracy profile. Raw global/local cannot-evade updates estimates and both ordinary
  main-hand chance aliases; final diagnostic chances do not feed back into estimates. Nineteen added cases
  cover six reproduced omissions, spell controls, actual support link/removal, bypass, luck and bounds;
  the expanded focused run passes 55. Native revalidation, integer-roll expectation, multi-hit,
  dual-hand, cooldown and DoT calculations remain open. See [11](../docs/protocol/11-skills.md).
  This includes explicit zero ranges after spell damage suppression, without changing attack displays.
  Automated mixed conversion/gain, added-damage effectiveness, hitless, attack and native row-format checks pass.
  Native selection/getter checks show Fireball 10-15 fire and Freezing Pulse 10-15 cold with a +12% spell-damage wand.
  A conversion/gain control split Fireball into fire and chaos rows and dealt a noncritical 14-damage server hit.
  Scorching Ray cleared inherited hit ranges and weapon DPS, including after added spell damage refreshed its record.
  Ranges use server floors; corrected averages retain packet fractions and exclude target mitigation,
  per-type integer-roll expectation and cooldown/downtime.
  Beam/DoT displays, conditional damage rules and broader conversion semantics remain outside this slice.
  Code: `SpellHit.cs`, `SpellPanelStats.cs`, Gateway (`GemSkills`, `LaunchProjectile`, `AreaSkillHit`).
  Docs: [11](../docs/protocol/11-skills.md) sections 9 and 14.
- **Critical strikes.** The server rolls unconditional crit passives: the equipped main hand's `WeaponTypes.dat`
  critical chance (permyriad) raised by generic/attack/melee/weapon-shape increased crit chance for attacks,
  each spell's extracted base chance, and 150% plus `base_critical_strike_multiplier_+`;
  critical damage is multiplied and logged.
  Unarmed has 0% base crit.
  Two pure base stats replace that base before the increased chance, the way PoB's `WeaponBaseCritChance` replaces
  an attack pass's `source.CritChance` when that hand has a weapon:
  `override_weapon_base_critical_strike_chance` (Stats row 9892, permyriad) on the armed main hand — Slayer
  Overwhelm (graph id 17315) grants 800 = 8.00% and Blind Prophet's Sightless (graph id 24698) grants 900 =
  9.00% — and `base_melee_critical_strike_chance_while_unarmed_%` (Stats row 3906, permyriad; Behemoth Final Blow,
  graph id 10135, grants 1000 = 10.00%) on the unarmed melee attack.
  Each is a base, not a fixed final chance: generic increased crit still applies on top;
  the weapon override stays out of unarmed and spell rolls, and the unarmed base stays out of a weapon hand.
  Only the active weapon set supplies the attack base and its weapon-specific increases.
  Ordinary added BASE now combines generic and attack/spell-specific raw actor and selected skill/support sources
  after the intrinsic base or its override and before increased chance, with signed wide sums and final half-up cap.
  Native weapon-local flat critical chance precedes local increased chance and its permyriad rounding.
  Linked Increased Critical Strikes supplies its own BASE and increase once; known zero intrinsic chance may receive
  added BASE, while missing/negative data, hitless skills and cannot-crit gates still fail closed.
  Native modifier identities, mode-specific Ascendant Assassin sources, active-set isolation, removal, zero and
  support composition have automated coverage; current combined results are in the panel entry above.
  Sixteen positive Fireball/ICS native controls on `0aabeb6` pass at actual level 100 in both modes.
  Selected permyriad values 500/550/1200/1120 render as 5/5.5/12/11.2%, including actor/support removal, relinking and relog.
  Two current-Actor Fireball-removal controls and two default sentinels also pass without numeric-zero inference.
  The original Standard D0 sentinel remains UNPROVED; its separately reviewed after-capture correction uses current
  Fireball/Default identities and historical C2 numeric evidence, with no simultaneous Fireball receiver claim.
  Its version freeze preceded the Ruthless sentinel capture.
  Canonical stock/charges and protected data held; gem-ID remaps and retained participant state are qualified in
  protocol 11 §5, and normal services were restored.
  Native critical hits, rendered zero and the remaining attack/local/Assassin/rounding contexts remain unverified here.
  Isolated carriers do not establish legal unique acquisition, and the local-flat chance fixture retains its real
  no-extra-critical-damage flag without certifying multiplier behavior. Pinned PoB 3.29.x is a labelled fallback.
  `/set_stat` replaces the input and rebuilds the pools, so the panel and the rolls agree.
  Generic/spell development increases replace the same complete raw input used by spell rolls, including aura sources.
  Attack/spell increased percentages and the pooled spell reference now follow those effective inputs.
  Automated regressions cover these replacements, signed sum bounds and shared integer rounding.
  Attack and spell chances now share half-up permyriad rounding: 650 with +1% gives 657.
  Native evidence below targets original head `8c4c62d`; recheck the changed midpoint and resolved stack natively.
  Five follow-up native checks verify generic/spell input agreement and maximum/minimum integer saturation.
  Twelve native checks verified 830 → 800 → 900, increased chance → 1800, cannot-crit → zero,
  empty-set/unarmed behavior, weapon restoration and relog.
  Ascendancy allocation remains in the separate lifecycle change.
  Global, virtual-global and attack-specific cannot-crit flags suppress the attack chance.
  `SpellCriticalStrike` reads neither base override.
  The final main-hand and spell crit chance/multiplier stats go to the client for the Offence panel;
  selected spell chance and server projectile/area rolls now share raw player and skill inputs once.
  Precision and Zealotry retain their existing bonuses without counting them twice.
  Actual Spell skill types set the panel's casting flag; attacks and non-spell warcries keep it clear.
  Hitless spells and unsupported action types cannot fall through to weapon damage or attack critical rolls.
  Native panel/getter checks show Fireball 5%, Freezing Pulse and Ice Nova 7.5%, and Arc 6%, including after relog.
  Combined generic/spell bonuses add once; the no-crit flag clears the native chance and hides its row.
  A hitless Scorching Ray cast dealt no weapon damage, while the same connection's Fireball dealt a critical hit.
  Catalog, chance, panel gates, aura sources and hit routing also have automated checks.
  Four unconditional MORE/LESS chance sources now run before final cap/rounding. Real Controlled Destruction
  changes selected 10% to 2% for both attacks and spells; its link/requirement/removal lifecycle and actual parsed
  critical draws agree. The real unique -40% source changes an ordinary weapon's 10% to 6%, with explicit zero
  and item removal restoring 10%. Source cancellation, extreme bounds, independent factor composition and
  half-up rounding have regressions; current native verification is pending.
  Surfcaster's Baited Breath makes every hit critical while a Fishing Rod is held, a 0% base included; cannot-crit wins.
  Focus skill and curse lifecycles, guaranteed-critical effects, off-hand publication/execution and other
  enemy-state/more crit mechanics remain separate work.
  `/crit` shows the attack values and a pooled 5%-base spell reference ([11](../docs/protocol/11-skills.md) §5).
- **Overcapped-resistance modifiers.** While the flag stat is set, the uncapped resistance above its maximum is a
  generic increased modifier, exactly PoB's `ResistOverCap` (the maximum capped at 90): +1% increased evasion per 1%
  of cold overcap, +1% increased critical chance per 1% of lightning overcap, additive with the other increases for
  attacks and spells alike.
  The flags are ordinary stats, so any source sets them: the Surfcaster's Sea Legs notable (graph 61696, with its
  +40% cold-and-lightning pair resistance the aggregator now sums into both elements), an item mod, or `/set_stat`.
  An explicit `/set_stat` value replaces its own input (per-type, pair and all-element resistance, and per-type or
  global maximum resistance) instead of adding to the aggregate, and rebuilds the pools; a Kitava change rebuilds them
  again.
  `PoolsFor` derives the increases once per pool build and stores them in the pools, so the monster-attack evade
  rolls, the panel rows and the spell crit consumers read the same numbers; the uncapped resistance values stay
  untouched, and Iron Reflexes' converted evasion keeps the overcapped increase once.
  Automated checks cover below/equal/above cap, the 90 maximum, both penalties, flag removal and override, additive
  behaviour, spell/attack isolation and the Iron Reflexes conversion.
  Combined automated checks retain the 830 +45% =1204 armed chance and complete signed critical cancellation.
  Purity-aware raw replacements now remove the aura contribution for that exact key, including explicit zero maxima.
  Real aura/catalog/pool regressions pass; the corrected aura-override composition needs a native recheck.
  The resolved stack needs native rechecking; the following thirteen checks belong to original head `39cbda2`.
  Real-client stat checks verified evasion 15 → 22 and spell crit 500 → 725 at 120% resistances,
  raised maxima, replacement of global inputs, both Kitava penalties, flag removal and relog.
  Ascendancy allocation remains part of the separate lifecycle change.
  See [11](../docs/protocol/11-skills.md) §4, §5 and §12.
- **Projectiles.** One chronological runtime drives production gem/bow flight, contacts and continuation.
  It runs at 30 Hz boundaries with subtick contacts and commits damage/rewards before network output.
  Native gem skill records derive projectile count, pierce, chain, fork, split and return from shared parameter rules.
  These display-only projections respect prohibitions and refresh after support removal without changing release inputs.
  Missing projectile metadata allows generic panel parameters while executable release remains unavailable.
  Release captures position and offense; target movement, evasion, defenses and impales resolve on contact.
  Issue #75 accuracy entropy/critical confirmation, one critical roll per use, raw penetration, actual-roll impales and
  direct-hit leech run in this same engine. Resource recovery catches up projectiles before consuming leech; corpses
  retain the ordinary lifetime. Combined automated regressions cover Ward/slot defences, mana conversion and contact
  recovery. Details and test receipts: [merge review](../docs/dev/issue75-review.md#upstream-merge-resolution).
  The later main merge composes Gambler, Vaal credit, incoming ailments and league modes with these stat consumers.
  Combined regressions cover direct-only leech and once-only soul credit under held output, plus mode-aware defences.
  Its [validation report](../docs/dev/issue75-review.md#main-merge-league-modes-gambler-and-vaal-souls) records the decisions.
  Native revalidation of that combined build remains pending; the wider 357-stat goal stays paused.
  Pause freezes the game horizon, transfer/retirement invalidates epochs, and released shots survive later actions.
  Ordinary bows and wands share one release path on that runtime: the shot is scheduled at the accepted input's weapon
  contact (`attack1_bow`, or the wand clip the native ACT selects for the input's sequence) and the action echo follows
  it; a cancelled action, death, freeze or hit recovery releases nothing. The wand bolt plays its rig's impact clip where
  it stops and is removed after that clip on the same game clock, so pause holds it and a transfer drops the removal.
  The arrow stays a second where it hit (`ArrowStuckSeconds`) before removal. Both send `0x1af` (kind 3, the shot's
  action sequence, flags 0x4201 plus the critical bit, code by damage type) on contact for the target's blood and
  particles; the main-hand bit follows the live capture, the skill index and header words it also fills stay zero. Projectile objects are created at their release point and their first move runs from there: the lazy
  snapshot used to create them where the simulation already had them, one to ten cells past the bow in a live log
  (2026-10-29). On a lockstep client the default attack's record carries the shot's count, as GGG's do, and the
  client draws the bolt or arrow itself with no server object; the server simulates it on the client's contact and
  range and releases it on the client's own release frame ([07](../docs/protocol/07-latency-visibility-movement.md)
  §6), so a hit recovery before it stops both (checked live 2026-11-05 on orders at a monster: one client
  `WandProjectile` per shot of 41 and one `DefaultArrow` of 41, no projectile `0x2bb`, the client's release 0–7 ms
  after the server's for the wand and 1–3 for the bow, p10–p90; it was 16–40 ms after on the first version and 66–99
  ms before with server objects) ([11](../docs/protocol/11-skills.md) §4, §10). A shot at the ground (attack in place)
  is released the same way but not checked live. A predictive client keeps the server's objects: its record
  has no count, without which the release fires nothing (a native trace on 2026-10-29 saw no client arrow in 40).
  The live hit-timing fit put our arrows at 367 cells/s against live's 308, but the Gateway releases DefaultArrow at
  the table's 3260 units/s = 299.9 cells/s (logged on every release), so the fit's slope measures something else
  (the roadmap). They leave from the
  releasing clip's contact keyframe (`data/animation-contacts.json` positions, `anim_contacts.py` v3): the Ranger's
  bow 5.5 cells ahead along the aim at 16 steps up, a wand's tip 7.5 cells ahead at 12 (the 5-step default, tuned on a
  fireball, had the arrow trail start at the hip, live 2026-10-29); the keyframe's lateral part is not applied.
  Monster projectiles: a mortar attack's shot (Sand Spitter, a Cannibal's ThrowFire: `monster_mortar_attack` via
  GrantedEffects SkillID → MonsterMortar → Projectiles) is drawn by the client, whose record carries the MonsterMortar
  row and the skill's count as GGG's does, with no server object (checked live 2026-11-05 on a lockstep and a
  predictive client: one client `SandspitterMortarProjectile` per order, no server object;
  [11](../docs/protocol/11-skills.md) §6); a spell mortar (`monster_mortar`) keeps its server object. Not measured yet:
  where the client's mortar lands against the server's hit.
  The server simulates it on the same runtime, released at the clip the client plays for the
  skill (MonsterMortar.Animation → the ACT's `SpellProjectile` pool, the native selector on the action's 0xffff
  sequence: attack1, contact at 37.5 % of the attack) from that clip's keyframe (4.2 cells behind the body that faces
  away, so toward the player, 9 steps up: `projectile_uses_contact_position`; `data/monster-contacts.json`,
  `scripts/re/monster_contacts.py`), lobbed at the player's cell but no closer than the skill's
  `projectile_minimum_range` (4.6 cells), team 2, and its landing splash (`Game_MONSTER_MORTAR_RADIUS`, default 10
  cells) runs the monster contact pipeline (`HostileProjectileOffense`, `Program.ProjectileHostile.cs`). Not checked
  live yet: that the client seeds a monster's clip choice from the sequence, and the dead zone. Another monster's
  projectile object plays its rig's flight clip from spawn and its impact clip where it lands
  (`data/projectile-clips.json`: every Projectiles row's clip indexes and lengths,
  and its .ao default animation), without which an object whose .ao declares no default animation (the mortar's; the
  wand bolt's declares one) is invisible. The data loaders turn their feature off
  with one warning on an unreadable file instead of faulting the pump, and monster ACT pools come from the root block
  only (a nested stance's entries had replaced them for 191 varieties; review 2026-10-30). Weapon-projectile monsters
  (`monster_projectile_weapon`, SkillID 0) still hit instantly with no shot; see the roadmap.
  Wand main-hand variation follows each class ACT pool and the native input-seeded selector; sixteen actual ground
  inputs covered all four Witch clips with matching selection and an object input also matched (historical LocalExile
  evidence on the earlier flight loop; the runtime path still needs a native recheck). Complete off-hand timing/damage
  and nonzero intrinsic attack repeats remain open.
  Real-input held-output, moving-target, wall, evasion, pierce, return, pause, transfer, impale and actor-scale checks
  exercise this same runtime. Native bow/skill controls and animation alignment still require client validation.
  Straight flight uses captured ground plus the release keyframe's height, else `Game_PROJECTILE_HEIGHT` (default 5);
  missing captures use the bare offset.
  Creation, glide and stop retain the same height; independent effects still follow client terrain.
  Platforms/doodads still need a surface model (#182). Integer walk estimates and shared radius remain approximations.
  Code: Core `Projectiles/`, Gateway `Projectiles/` (`Program.ProjectileRanged.cs`), `Program.RangedAttacks.cs`,
  `OrdinaryRangedAttack.cs`, `OrdinaryRangedShot.cs`, `Program.Combat.cs` and `ClientState.cs`.
  Docs: [framework](../docs/game/projectile-framework.md), [11](../docs/protocol/11-skills.md) section 10.
- **Combat.** The player's default attack kills monsters with server-side damage (no monster armour yet; each damage
  type of a hit takes the monster's own base resistance with the hit's penetration subtracted from the capped value,
  `HitMitigation`/`Penetration`; the player's hit lands at the attack animation's contact keyframe, a monster's still
  at half its attack time).
  Monsters use their first granted effect and swing back; their attacks roll player evasion and then go through armour
  and block.
  Ordinary physical melee hits can trigger native player TakeHit recovery through 0x1d1.
  Native proof stopped predictive walking and casting, blocked new actions, and cleared the recovery flag afterward.
  Server recovery cancels pending actions and channel hits and stops the walk estimate for the same interval.
  Recovery or a freeze also suspends a running channel: it pays and sweeps nothing until the player recovers.
  That channel suspension has automated coverage only, with no native check yet.
  Actual physical monster hits stopped walking and cancelled a slow cast before release in copied-save live checks.
  Released Fireball flight continued, and a stun-immune control took damage without recovery.
  With a bow equipped, a hit cancelled pending Burning Arrow, blocked an ordinary arrow during recovery,
  and allowed a fresh arrow afterward; an already released Burning Arrow kept flying.
  Chance, avoidance, immunity, recovery and delayed object walk-ups have automated checks.
  ES builds, substituted thresholds and unsupported conditional stun defences are skipped conservatively.
  See [11](../docs/protocol/11-skills.md), player hit recovery, for scope and protocol evidence.
  The player's life comes from the class/level/passive pools.
  The server has no shared simulation loop: hits use timers and attack time is bounded by a 30/s tick (`TickRate`).
  Monster decisions use 50 ms game-time steps, a separate 100 ms repath budget and a three-cell target threshold.
  Full attack duration, pending-swing generations and stage completion guard the faster planner against duplicate work.
  Native controls verified initial response, deliberate turns, full attack pacing and actual menu pause/resume.
  Core, Data and Gateway suites pass, including cadence, pending-swing and stage-completion regressions.
  Item mods count through the global item stats;
  346 varieties with NPC / ControlZone / ... components are refused.
- **Critical-strike elemental resistance rules (Inevitable Judgement).** The hit's
  already-rolled critical boolean decides the resistance: the packet rolls once (the original per-type draws), the
  existing single critical draw keeps its original place after them, and the mitigation resolves on the
  already-rolled packet without a second roll — a critical elemental hit with
  `critical_strikes_ignore_elemental_resistances` mitigates with an effective resistance
  of zero, negative included, overriding any penetration; the attack-only variant covers attacks only; a
  non-critical elemental hit keeps its capped resistance and `non_critical_strikes_penetrate_elemental_resistances_%`
  (10) subtracts after the cap, stacked with the hit's general penetration;
  `cannot_penetrate_or_ignore_elemental_resistances` restores the plain capped resistance.
  Physical, chaos and damage over time are outside the rules; Inevitable Judgement (graph node 48214) grants the
  generic ignore and the 10 penetration through the ordinary passive stat sum.
  The shared helper (`HitResistances`, the Gateway's `MonsterDamage`) covers default attacks, bows and gem hits.
  Default and area damage logs name the decision; gem projectiles emit a separate resistance trace.
  Core and Gateway checks cover positive/negative resistance, maximum independence and the node's exact stat keys.
  Eight native checks read monster life: fixed 100 fire dealt 60 normally and 70 with non-critical penetration.
  Guaranteed critical hits dealt 150 with generic or attack-only ignore, and 90 after removal or prohibition.
  Those captures belong to original head `13c41e5`; combined penetration/modifier/bow paths need native rechecking.
  Automated checks preserve same-key source sums, fractional totals, target damage taken and the original RNG order.
  Sequential C24 handler tests read real `/set_stat` changes after a prior hit on the same connection and target.
  Skill `0x102`, sequence 0 and flags `0x408` give ignore 300, ordinary critical 330 and prohibition 240.
  Friendly and hostile controls pass without a production hostility change.
  The combined run labelled `462c883` used a DLL proven by hash to be #225 `4a71968`; its ninth case is not #226 proof.
  Ascendancy allocation remains in its separate lifecycle change.
  Code: `HitResistances.cs`, `HitMitigation.cs`, Gateway (`MonsterDamage`).
  Docs: [20](../docs/protocol/20-damage.md) §4.
- **Client Buff descriptor ownership/transfer research (#370, #466, offline only).**
  Original bytes bind the prepared structure to item
  Mods `+0x38` and entity ObjectMagicProperties `+0xc8`, with a structural item-assignment path.
  A separate `+0x1d8` cache is excluded from the descriptor path by pointer provenance and record stride.
  This does not establish a meaningful descriptor consumer, actual target DoT publication or live behavior.
  The revised delivery records these structural findings; actual readers and outputs remain Known issues K1-K3.
  Original N1 is partial, N2 remains open and R2/R3/R4 stay Partial.
  See the [report](../docs/re/client-buff-descriptor-consumers.md).
- **Damage model (foundation, with armour/evasion).** A hit is a per-type `DamagePacket`;
  `DamageConversion` handles the game's physical, lightning, cold, fire, chaos order, chained conversion, skill/global
  precedence, the 100% cap and gain-as-extra.
  DoT is a single type with fractional tick carry, and `Ailments` has the ignite/bleed/poison base formulas.
  The default attack, Fireball and monster melee build packets and read the player's/skill's conversion stats;
  packet mitigation applies each type's own resistance, and the physical part then goes through armour.
  Righteous Fire's burn uses the DoT tick model.
  A damage-type immunity (`*_immunity`, `elemental_damage_immunity`) zeroes that type on the player's hits, from the
  same sources the panel's Immune rows use (`DamageImmunities.cs`).
  Global added attack damage per complete group of final Strength, Dexterity or Intelligence joins the raw attack
  range for the panel rows and the hit (Hyrri's Demise, (Replica) Iron Commander, Replica Alberon's Warpath per 80
  Strength despite its `_per_50_strength` id; [20](../docs/protocol/20-damage.md) §2b).
  The 3.29 whole-type item prohibitions ("Cannot deal non-Chaos Damage", "Deal no Physical Damage", ...) share one list:
  hits, the bleed, ignite and poison rows (each gated on its own type), the poison hits inflict and the caster's
  outgoing DoT drop those types.
  Automated tests only; the native check is pending.
  Hits taken read damage taken and Mana before Life stats ([20](../docs/protocol/20-damage.md#damage-taken-from-hits)).
  Ordinary player energy shield derives from equipment, intelligence and passives.
  Equipment ES uses the saved variable base roll and native rounding before and after local modifiers and quality.
  It absorbs non-chaos hits before life and recovers through regeneration and delayed, damage-interrupted recharge.
  The existing 16 ms elapsed-time loop supplies the client's native ES recovery prediction.
  Current ES and its prediction are sampled together at the network send gate, including duplicate suppression.
  Encrypted-wire held-gate tests cover a queued clamp, cleared prediction, death and unchanged-state suppression;
  native checks of this queued-send case remain pending.
  Live checks covered absorption, regeneration, interrupted recharge and chaos bypass.
  Zealot's Oath is implemented; other special ES keystones, recoup and conditional substitutions remain
  unimplemented; ordinary ES leech uses the shared recovery writer (§5b).
  See [ES scope](../docs/protocol/20-damage.md#player-energy-shield-2026-10-25).
  Maximum-mana added lightning has original-head native checks and resolved automated checks.
  The player's hits chill, freeze and shock monsters: threshold maximum life, strongest effect with its own duration,
  shock as increased damage taken (hits and Righteous Fire), chill and freeze in the monster AI and walk, the monster
  mods' ailment stats (of the Ice, of the Tempest, Unstoppable, Mirrored) and the chilled/frozen/shocked buffs.
  Elemental Focus and the never-ailment stats stop those ailments ([20](../docs/protocol/20-damage.md) §3).
  Chill can raise damage taken and lessen the monster's swings (Glacial Wave), a freeze can have a minimum duration
  and a monster that hits the player can be frozen (Brine Bound) or chilled (The Pandemonius).
  A monster hit the player blocks can be chilled or shocked for 4 s ("Chill/Shock Attackers on Block").
  "As though dealing more damage" raises the chill, shock or freeze it inflicts ([20](../docs/protocol/20-damage.md)).
  Shock and chill minimums (Beacon of Ruin) and caps can be raised ([20](../docs/protocol/20-damage.md)).
  "All/Chaos/... Damage can Chill/Freeze/Shock" count those types, and "Hits always Shock" needs no chance.
  A corpse keeps its killing hit's ailments and a kill can shock it, unless the hit killed it frozen and it shatters.
  Killing a shocked monster with Berek's Respite gives the monsters within 2 m the same shock.
  Each chill, freeze and shock spreads to living enemies within its own `virtual_*_proliferation_radius` of the
  monster or corpse it is on, like an aura.
  An area skill applies a Mirrored monster's reflected ailments after its last target, and a sweep stopped partway
  still rewards its kills.
  The buffs, the sent action speed and the frozen pose are not checked with the real client.
  See [20](../docs/protocol/20-damage.md#outgoing-chill-freeze-and-shock-on-monsters).
  Hits inflict elemental Exposure by their chances, -10% for 4 s by default ([20](../docs/protocol/20-damage.md) §2).
  Elemental Equilibrium removes the Exposure of a hit's elements and inflicts -25% Exposure of the others.
  Not yet: Exposure from skills and auras, aura-dependent penetration, other added damage,
  increased/more support modifiers and outgoing ailment application/duration ([20](../docs/protocol/20-damage.md)),
  except the player's chill, freeze, shock, ignite, bleed and poison on monsters.
  Projectile hits ignite from the panel's ignite rows captured at release, and the default attack and melee or area
  skills from the same rows at their hit; Abberath's Fury's footstep hits work out
  their own (90% of the hit's flat fire per second for 4 s, the ignite increases, damage over time multipliers, +50% on
  a critical strike, and duration stats).
  Both add to the monster's one set of ignites: only the strongest burns, through the monster's fire resistance, every
  player tick, with one timed `ignited` buff and kills.
  They burn on while the player is dead (unverified), with no progress from a dead Hardcore character's kills.
  The melee and area ignite and the footstep ignite are not checked with the real client (`MonsterIgnites.cs`,
  [20](../docs/protocol/20-damage.md) §3).
  Attack hits (the default attack, melee and area attacks, attack projectiles and bow shots) bleed monsters from the
  panel's bleeding rows with the attack bleed chances, for 5 s and the bleed duration stats: only the strongest bleeds
  (Crimson Dance: the 8 strongest), three times while the monster moves or aggravated, with one timed `bleeding` buff
  and kills (`MonsterBleeds.cs`; not checked with the real client).
  A hit's poisons deal the panel's poison rows (`MonsterPoisons.cs`), not checked with the real client.
  Incoming ordinary monster basic-contact ailments now have runtime and Gateway coverage:
  critical fire ignite, cold chill/freeze and critical lightning shock, with avoidance, expiry, immunity, lethal ignite/ES and freeze action cancellation.
  `action_speed_cannot_be_reduced_below_base` keeps the action factor at 1 under chill and freeze (Gateway coverage,
  not yet checked with the real client).
  Reversed chill (`chill_effect_is_reversed`) is increased action speed while freeze still stops the player, and
  `all_damage_taken_can_chill` chills from any hit's total damage (Surfcaster's Changing Seasons).
  The sent stats now include the final `virtual_action_speed_+%` that the native walk and action clips read; that
  native effect is not checked live.
  Native checks verify real monster ignite/avoidance, lethal death-panel/checkpoint behavior and nonlethal RF.
  Alternative ailments and hostile spell adapters remain unimplemented; monster mortar shots land through the same
  contact pipeline ("Monster projectiles" below).
  See [Gambler](../docs/game/gambler.md) for the exact bounded scope.
  Double damage chances double hits, and triple damage overrides them ([20](../docs/protocol/20-damage.md) §2).
  `unaffected_by_shock` keeps the shock and `is_shocked` without the damage-taken increase; `unaffected_by_ignite`
  keeps the ignite and `is_ignited` without its damage.
  "While moving" evasion and regeneration follow the server's walk, and more maximum attack damage scales every attack
  hit's maxima next to Rage; Gateway coverage only ([Polytheist](../docs/game/polytheist.md)).
  Impact, Close Combat and Shield Crush scale melee hits by proximity ([20](../docs/protocol/20-damage.md) §2j).
  Unconditional penetration works on the player's outgoing hits (general per-element and elemental stats, the
  attack-only variants, the cannot-penetrate flag; DoT and physical take none).
  Player and skill contributions to the same raw penetration stat add without overwriting either source.
  Nine native checks verified fixed 100-fire hits for 60 → 80 → 90 → 100 damage as penetration stacked,
  the prohibition returning 60, cold penetration giving 120 against zero resistance, and chaos 75 → 105.
  Each check read the actual native monster life after the authoritative update.
  Those captures belong to original head `0d800d1`; resolved bow and target-modifier paths need native rechecking.
  Target mods compose with base resistance before penetration; damage taken follows mitigation and rounds once.
  Ten fresh composed native groups verify critical rounding, active overcap and default-attack mana lightning.
  Actual mixed-hit Life losses are 160/220 before criticals, then 300 with ignore, 330 without and 240 prohibited.
  Build versions and source hashes match `1366f48`; supported-gem, modifier and bow combinations remain automated.
- Continuous life/mana recovery (2026-10-24): regeneration, flask recovery and self RF now share a 16 ms
  elapsed-game-time loop, replacing regeneration's 250 ms jumps; pool fractions and caps are preserved.
  Implemented with regression tests; real-client visual verification after Gateway restart is pending.
  See [20](../docs/protocol/20-damage.md), "Continuous resource updates".
- Consecrated ground while stationary (Paladin's Anointed Champion): a ground of the stationary radius stays where the
  player stands and goes when he moves, dies or is in a town; the effect of his grounds (the Sulphur Flask's too)
  lingers after the last moment one covered him, and `consecrated_ground_effect_+%` raises it (5% to 7.5% of life per
  second). Automated coverage only; the ground art and the linger are not checked with the real client.
  See [13](../docs/protocol/13-items.md) §8a and [Paladin](../docs/game/paladin.md).
- The player's debuffs on monsters: every warcry taunts the hostile monsters in its radius when its cast ends, the
  Basalt Flask in its 30 cells (3 s, `taunt_duration_+%`, the `taunted` buff with its two values); passive enemy auras
  (Light of Divinity's Augury of Penitence) sit on the hostile monsters near the living player.
  Their increased damage taken (taunted 20% from Bring the Battle, elemental 25%) adds to the monster's own in every
  player hit and in Righteous Fire's burn; `warcries_have_infinite_power` gives Enduring Cry the maximum charges.
  Hits Intimidate and Unnerve: 10% increased hit damage taken for 4 s ([11](../docs/protocol/11-skills.md) §12).
  Hits Wither: 6% increased chaos damage taken per stack, up to 15 stacks ([11](../docs/protocol/11-skills.md) §12).
  The other warcries' own buffs, exerts and summons, taunt immunity and other passive enemy auras are not done.
  Automated coverage only; not checked with the real client.
  See [11](../docs/protocol/11-skills.md) §12 and [Paladin](../docs/game/paladin.md).
- **Cyclone area of effect (2026-10-25).** `/set_aoe` now resends Cyclone's calculated final radius in the skill
  record, using the same radius calculation for server hits.
  Cyclone/CycloneAltX use a 16-cell base when their exported stat set has none.
  The player's increased area counts once for every area skill (`AreaPlayerStats`); it counted twice before.
  Spell-only area does not affect the attack.
  Tested calculations; live visual check pending.
  The first live check found that direct radius alone did not scale the channelled visual;
  its conditional path now also receives aggregate area (0xd7) and radius percentage (0x1591).
  Cyclone's additional melee-range and stage modifiers remain partial.
  Its sweeps follow the player walking toward the aimed cell (C→S 0x197) at the gem's movement speed, not the cursor.
  Checked with the real client (2026-11-08): the sweeps advance with the player.
  See protocol/11-skills.md.
- **Self-targeted spells (2026-10-29).** Self-targeted spells (ActiveSkillTargetTypes 4 or 10: Ice Nova, Discharge,
  Shock Nova) hit around the caster wherever they are aimed, as the client draws them; before, an Ice Nova cast on a
  monster went off around the monster.
  Totem, mine and trap skills and Novas without a self target (Shockwave Totem, Stormblast Mine, Dark Pact) keep the
  aimed point.
  With `nova_spells_cast_at_target_location` (Devout of Lunaris) self-targeted Novas hit around the aimed point, and
  Nova skill records carry `skill_is_nova_skill` and, with the stat, the derived
  `virtual_self_targeted_skills_target_location_instead`, so the client casts them at the cursor (live 2026-10-30).
  Area skill records carry their final `active_skill_area_of_effect_radius`, which the client draws them at; without it
  Ice Nova drew nothing (live 2026-10-30).
  Code: `SkillCatalog.IsSelfTargeted`, `SkillCatalog.IsNova`, Gateway (`SpellCentre`, `AreaSkillHit`).
  Docs: [11](../docs/protocol/11-skills.md) §9a.

## 尚未实现

- 平台与装饰物上的投射物生成高度：协议不带玩家 z，服务端也没有
  表面高度模型，所以藏身处传送点平台上的投射物会生成在它下面的地形高度。
  跟踪于 #182。
- 乌图拉之灵的领导与 Rakiata 印记专属触发仍需要它们自己的酋长执行器。
- 应附着到骨骼上的效果（正义之火的球、MiscAnimated RighteousFireOrb）不发送；
  效果对象只靠 0x1ba 滑行跟随玩家。
  旗帜的勇气层数与它对附近敌人的减速未建模，图腾仍不开火
  （见「部分实现」下的图腾与旗帜条目）。
- Projectile spawn height on platforms and doodads: the protocol carries no player z and the server has no
  surface-height model, so a projectile on the hideout waypoint platform spawns at the terrain height below it.
  Tracked in issue #182.
- Spirit of Utula's Leadership and Rakiata's Mark-specific trigger still need their own chieftain executors.
- Holy Sweep's hammers deal no damage: the client draws them on the hits, but the server has no hammerfall hits
  (`holy_sweep_number_of_holy_bolts_to_create`, the 12-cell secondary area, `holy_sweep_hammerfall_damage_+%_final`).
- Effects that should be attached to bones (Righteous Fire's orbs, MiscAnimated RighteousFireOrb) are not sent;
  an effect object follows the player only by 0x1ba glides.
  Combat auras, Heralds and banners are not implemented; only the simple auras above use buffs.
