# 路线图：技能、光环、属性与战斗

属于 [路线图索引](../Roadmap.md)；这个区域已经能用的功能见 [status/skills-and-combat.md](../status/skills-and-combat.md)。
条目沿用路线图的优先级分组，同一组内大致按优先级排列。

## 1. 体验优化

光环意图的持久化、城镇内抑制、自身光环限制（#304）与死亡重置已实现。
自身光环限制有原生成对增益、加成、保留、重登与死亡对照。
请验证原生灰图标预测与更广的城镇/战斗/换角色往返。
挂起/恢复生命周期是明确的 LocalExile 策略；见 protocol/11-skills.md 第 10c 与 13 节。
- **Aura round trips:** validate the own-aura restriction's native grey-icon prediction and broader
  town/combat/character-switch round trips.
  The suspend/restore lifecycle is LocalExile policy ([11](../docs/protocol/11-skills.md) §10c and §13).

## 3. 玩法

- **强化（Fortification）扩展：** 命中生成的那套系统已实现，并与 Behemoth 分开做过原生验证。
  其他条件来源、永久授予与专用的怪物异常状态阈值仍是独立工作。
  见[强化](../docs/game/fortification.md)。

- **普通光环激活：** 九个此前被拒绝的自身光环现在共用保留、呈现与受支持的属性/伤害路径。
  Discipline 的普通护盾池与回充已实现，带共享的角色缩放。
  先驱触发、保留型召唤物、静止的寒冰护甲、风暴之盾与时空裂隙现在都有执行器；附近友方会获得普通光环。
  剩余工作记录在下面的光环章节里。见 protocol/11-skills.md 第 13 节。
- **光环施法者美术：** 在激活与区域恢复时会选用不同的 EPKFiles2 视觉；
  实机只出现在来源侧的回归已用左上角图标的单独接收增益处理，待确认。
  若日后加上按半径缩放的光环环，仍需要其真实半径。
- **电球起始格：** 服务端按客户端的漂移飞行每颗电球（见状态），但它从服务端的行走位置出发，常与客户端起点差几格，
  所以画出来的电球仍可能命中却不造成伤害。
  入手：把客户端的起始格与每一段每帧的起始时间（`scripts/re/frida/trace-spark-legs.js`）与服务端的 `spark seq=` 行对齐记录，
  然后让服务端的分段从同一格、同一帧开始。
  完成标准：每颗画出来的电球命中都恰好造成一次伤害，且没有任何伤害是没有画出电球却生效的。
- **幽灵投掷的命中时机：** 技能会造成伤害（见状态），但命中可能落在画出的武器抵达怪物之前或之后，
  多数是在返回段、朝着走向投掷者的怪物时。
  服务端的返回段以恒定平均速度飞行，而客户端从静止开始加速。
  其近战连击的释放时机只对使用剑、锤或斧配盾的女巫与圣堂武士已知，客户端何时在 01a 重开连击也未知。
  入手：在同一时钟上把客户端的武器（`scripts/re/frida/trace-projectile-path.js`）与服务端的命中行对齐记录，
  投掷之间穿插暂停与普通攻击；然后从 ACT 与 .ao 里读出每个职业、每个姿态的 Melee 视图（`scripts/re/anim_contacts.py`），
  而不是用 `SpectralThrowRelease` 的表。
  完成标准：每个职业与武器下，命中都在画出的武器抵达怪物时落下（去程与回程都是）。
- **带 Spell Echo 的 Ice Nova：** 服务端不给 Ice Nova 发 `spell_repeat_count`，所以 Spell Echo 只是加快它。
  它的动作有两次重复：一圈之后其更新（`0x141c02c10`）等 0.3 秒再响一圈，条件是 +0x320 小于计数且字节 +0x2b8 与 +0x2b9 相等；
  其 0x460 槽（`0x1412e1ad0`）跳到法术基底的 replay（`0x1419e6370`），把动画再播一遍。哪一个会跑、何时跑，静态读不出来。
  入手：连上 Spell Echo 施放 Ice Nova，开着 `scripts/re/frida/trace-ice-nova-repeat.js`（它记录那些字段、每圈的开始/受击者/结束
  与每次 replay），并给 Gateway 设 `Game_ICE_NOVA_REPEATS=1` 让它发计数，然后逐圈打上去。
  完成标准：带 Spell Echo 画出的每一圈 Ice Nova 都恰好造成一次伤害。
- **已完成区域修复：** Cyclone 的 `/set_aoe` 最终半径现在会下发给客户端并与命中判定共用；见状态与 protocol/11-skills.md（实机核对待做）。
- **Fortification extensions:** other conditional sources, permanent grants and monster ailment thresholds.
  See [Fortification](../docs/game/fortification.md).
- **Aura caster art:** confirm the received buff's top-left icon live; radius-scaled aura rings need their radius.
- **Spark start cell:** the server flies each spark on the client's wander but from the server's walk position, so a
  drawn spark can still hit with no damage.
  Start: trace the client's start cell and each leg's start time per frame (`scripts/re/frida/trace-spark-legs.js`)
  next to the server's `spark seq=` lines, then start the server's legs from the same cell and frame.
  Done when: every drawn spark hit deals damage once, and no damage lands without one.
- **Spectral Throw hit timing:** hits can land before or after the drawn weapon reaches a monster, most on the return
  toward monsters walking at the thrower ([11](../docs/protocol/11-skills.md) §10).
  Start: trace the client's weapon (`scripts/re/frida/trace-projectile-path.js`) next to the server's hit lines on one
  clock, with pauses and default attacks between throws; then read every class and stance's Melee views from the ACT
  and .ao in `scripts/re/anim_contacts.py` instead of `SpectralThrowRelease`'s table.
  Done when: each hit lands as the drawn weapon reaches the monster, out and back, for every class and weapon.
- **Ice Nova with Spell Echo:** the server sends Ice Nova no `spell_repeat_count`, so Spell Echo only speeds it up
  (the action's two repeat paths are in [11](../docs/protocol/11-skills.md) §9).
  Start: cast Ice Nova with Spell Echo linked under `scripts/re/frida/trace-ice-nova-repeat.js`, with
  `Game_ICE_NOVA_REPEATS=1` set for the Gateway so it sends the count, then hit with each ring.
  Done when: every Ice Nova ring drawn with Spell Echo deals damage once.
- **Cyclone's area:** check the `/set_aoe` radius live.

### 玩家的真实属性

在 [357 项属性台账](../docs/dev/issue75-stat-ledger.md) 里跟踪 #75 的确切目标。部分审计记录保留其未解决范围；
待处理记录需要逐项追踪。历史原生运行或来源名字吻合都不能推出当前分支的完成证明。清单会拒绝成员漂移。

普通近战/弓现在共用防御方闪避熵与独立暴击确认，含目标闪避词缀/致盲、目标精通、合格的原始不可闪避标记与闪避箭矢的延续。
选定的固定/提高精准辅助与原始角色覆盖现在在最终向下取整之前组合。
范围攻击在被准入的接触内消费该档案，带未命中/暴击确认与回调对照。
宝石投射物的前向/穿透/返回闪避与暴击确认已实现，带未命中接触延续、预算与齐射排除对照。
宝石使用现在共用一个暴击掷值，带已准入使用的幸运与逐次命中的几率/禁止/精准确认。普通攻击的幸运现在跟随挥击/输入准入；
请复核其他弓快照与特殊重复。补齐剩余条件、纠正副手来源，并发布所有匹配的几率/必中行。
普通/选定的主手平均值/DPS 现在包含普通参考命中与暴击确认概率；双手/多段命中、整型掷值期望与其他来源仍待处理。
在共享资源空闲时原生验证这些路径。
见[伤害 §2h](../docs/protocol/20-damage.md#2h-ordinary-attack-evasion-and-critical-confirmation)。

穿刺现在对普通与受支持的命中有一套共享的实际接触实现，带作用域的几率/效果输入、捕获的持续时间/命中数与单独减免的反弹。
三个选定的几率别名包含真实辅助与本地武器输入。剩余工作包括条件性武器/骄傲几率、额外施加与消耗覆盖、
最强/穿刺者机制、原生计数/视觉发布、尸体消费者、仅暴击的法术几率呈现与官方持续时间 tick 取整。
等其他智能体释放共享测试资源后，在当前原生客户端验证来源、辅助、武器、暂停、切换与重登行为。
见[伤害 §3a](../docs/protocol/20-damage.md) 与[评审报告](../docs/dev/issue75-review.md)；自动化覆盖不能关闭这些行。

下面 #75 的校验计数与标准/无情原生矩阵描述的是来源侧 LocalExile 的修订版。
本分支移植其已完成的普通模式计算；新的构建/测试结果、范围排除与剩余原生核对记录在[移植记录](../docs/dev/issue75-port.md)。

- **已知：** 客户端属性键 = Stats.dat 行 + 1（[11](../docs/protocol/11-skills.md) §8）；`/set_stat` 接受名字。
  服务端必须下发最终值（客户端不推导任何值）：攻击的在 `SkillCatalog.Unarmed.Stats` 里计算。
  「每完整一组最终力量给魔力」现在有原生原始来源/装备对照与自动化池覆盖。
  原始来源的保留刷新与钳制有加密报文覆盖；请原生复核生效中的光环。
  见[状态](../status/skills-and-combat.md)与 [11](../docs/protocol/11-skills.md) 第 4 节。
  Mental Conditioning 现在在 current-main 测试里把「每点力量的魔力」与「魔力转护甲」组合起来。
  历史上的联合原生转换对照在 `c9cd1864` 通过；Phrecia 选择另属工作。
  普通一次性技能的冷却恢复现在共用原生基底/速率与一个服务端基底时间预算，并在动作副作用之前做原子预检。
  初始 Dash 恢复、过早的原始拒绝、暂停与原生冷却中途的速率变化实测通过，另有布局/发送闸门检查；
  剩余的基底进度保留而不额外消耗一次使用。
  剩余冷却工作包括多次充能恢复、共享组、绕过、带类型/条件/更多恢复、官方 tick 边界。
  见 [11](../docs/protocol/11-skills.md) §9。
- **法术施法时序（#192）：** 原生基础毫秒数被保留，施法速度只调整服务端等待与显示速率一次。
  自动化检查与实机客户端的 -80%/+100%/零片段时长都通过。投射物释放/接触对齐没有单独测量。
  瞬发施法与攻击时序另属工作；条件性动作速度与特殊原生机制仍待处理。
  攻击宝石现在共用按等级的内禀速度，与服务端节奏/接触和 APS 属性一致；客户端把自己的应用到片段上。
  原生输入验证了普通 0.714 秒弓时长与 Burning Arrow 的 1.02 秒。
  待处理：攻击长于其片段时会把片段的慢速段拉长，而 `AttackContactDelay` 没建模这一点
  （[11](../docs/protocol/11-skills.md)），并且箭矢仍在枪口闪光后约 25 ms 抵达，那个固定的 60 ms 提前量是在本机调出来的，
  而不是跟随连接的延迟。
  攻击宝石的伤害/DPS 与条件速度仍是独立工作。
- **法术命中面板（#188）：** 选定法术的范围现在共用战斗转换后的报文，无命中的法术会清除继承的武器命中伤害。
  自动化检查通过；请在客户端核对选择、装备更新与重登。
  DPS 现在包含当前暴击率/倍率与带符号幸运，带精确动作时长与有界的原生整数。攻击平均值/DPS 现在包含参考精准与独立暴击确认。
  整型掷值期望、冷却与 DoT/射线显示仍是独立工作。品质转换与既有修饰量优先级不变。
  选定的攻击宝石主手范围与按暴击加权的平均值共用战斗报文。
  原生选择、额外伤害刷新/清除、服务端命中与重登检查连同自动化测试都通过。装备界面刷新仍需单独核对。
  副手交替、条件伤害、辅助与修饰量范围纠正仍是独立工作。
- **战吼限制（#297）：** 共享的角色准入有回归与七个实机客户端对照（见状态）。
  被阻挡的输入、来源清除、付费重试与已接受战吼的释放都已验证。
  见 [11](../docs/protocol/11-skills.md) 第 10b 节；更广的战吼引擎与 Phrecia 启用仍属独立工作。
  战吼速度、一次性战吼冷却与共享释放效果（第 10i 节）对 Enduring Cry 生效；其他战吼需要执行器，
  而爆炸需要其全局附加伤害、固定暴击率、命中效果、原生爆炸美术（目前用通用血腥爆裂代替）与波次延迟。
- **角色面板（#75）：** 能量护盾回复行现在共用已实现的恢复计算，带词缀、清除与回充分离的原生核对。
  普通生命/魔力回复现在包含独立的回复/恢复提高与对应的禁止。
  原生实际恢复、清除与重登检查通过；强化到期的整合使用同一批因子。
  Zealot's Oath 现在把生命回复移到能量护盾；其他回复转换、具名的条件/更多修饰量与其他恢复系统仍待处理。
  确切的原生 Brand/特殊充能默认值现在进入角色来源一次，带一个原生选定的 Brand 闸门。
  真实的物品/天赋/辅助组合给出角色 Brand 6、选定 8；显式零与来源移除在两种模式下都有自动化用例覆盖，新的原生核对待做。
  三个有来源的镜像标记会由已完成的标准最大值发布特殊上限，含最终零覆盖；真实的不等 4/5/6 来源夹具保留各自映射。
  钉死的 PoB 3.29.x 仍是带标注的回落。
  Brutal / Affliction / Absorption 的计数、效果与增益现在跟随三个转换标记（状态、充能）；
  Brand 附着/上限强制、Affliction 的非伤害效果与 Absorption 的 recoup 消费者、Spirit/深渊珠宝计数仍待处理；
  这些改动并不等于给整条充能行背书。
  现在每个面板行都有服务端推导（状态里 2026-10-29 的批次覆盖防御估算、副手、有效暴击、次要与伤害型异常状态行；
  维护者 2026-10-29 的实机运行让它们通过了，还有转换后的充能与血充能）；#75 剩下的只有 Reap 的持续伤害减益
  与 Vaal Reap 的临时血充能上限，以及执行那些仅显示行的次要命中（Bear Trap、Herald of Ice）。
  暂缓（2026-10-29，维护者的决定）：Spirit 与 Siphoning 充能。它们在 3.29 里唯一的玩家来源是传奇词缀
  （`maximum_spirit_charges_per_abyss_jewel_equipped`、
  `maximum_siphoning_charges_per_elder_or_shaper_item_equipped`），所以在物品区域还需要：Mageblood 之外的传奇定义（#60）、
  可镶进深渊孔的深渊珠宝（Stygian Vise 创建时没有它的孔，`PlayerInventories.Socket` 只接受宝石，
  `ItemModStats.Global` 不读任何已镶嵌物品的词缀），以及 Shaper/Elder 的 `Item.Influences` 消费者（13-items.md §2f）。
  Blitz 与 Challenger 充能在 3.29 里完全没有玩家来源。
  选定的范围/物品稀有度/召唤物提高别名现在共用一个「原始角色 + 自身」快照，带显式的最终零/替换优先级、来源清除与
  Cyclone 半径保持。`SendPlayer` 的初始/过渡创建与后续技能刷新共用该快照路径。真实辅助对照覆盖普通可用性。
  历史上 LocalExile 对 Item Rarity Support 的无情排除不在本移植范围内。
  傀儡的战斗/掉落执行已接上；其他召唤物执行器与原生别名核对待做。
  选定的法术穿透现在包含真实的火/冰/闪电穿透辅助，来源/连接/需求生命周期与命中消费者一致性由九个新 Gateway 回归覆盖。
  攻击手的别名组合现在包含仅攻击来源、连携辅助与每只生效手的本地词缀，带自动化生命周期检查。
  显示禁止与武器本地/非基底战斗穿透现在有自动化来源/生命周期/接触覆盖。请原生核对更新后的行与接触；
  副手攻击选择与尚未读取的条件穿透形式（Zealotry 的仅暴击、Enmity's Embrace 的超上限火焰、近期击杀或格挡、药剂、
  酊剂、冰缓期间、单技能）仍需实现与验证。
  选定的异常状态持续时间/效果系数现在包含真实的 Unbound Ailments 贡献，带 24 个来源/连接/覆盖回归。
  请原生验证这十二个别名，然后完成异常状态几率/资格、伤害范围、持续时间/施加与叠加；单个正确的系数不能证明那些消费者。
  附加基底/默认镜像/选定别名的覆盖通过了 52/19/22 个新用例与 95 个既有对照：188 个聚焦用例无跳过。
  全新完整 Gateway 3803/Core 648/Data 37 无跳过通过（4488 个新重跑用例）。
  当前钩子记录了六个实际未被观察到的已释放 NetworkStream 错误，全部是 `MonsterKilled` 经 `Program.Send`，
  没有捕获到新的运算符路径或诊断错误/截断/上限命中。
  历史上的 G14/完整 3710 与更早的原生对照既不能确立这些运算符，也不完成任何一条 #75 行。
  护甲/闪避现在包含普通的物品本地固定/提高词缀与品质，带原生提示/面板、制作刷新、卸下与生效盾牌核对。
  保存的变量基底现在在本地词缀之前喂给三种防御；旧存档保留最小基底。
  神圣石重新掷值与受支持的特殊护盾规则已实现；范围与验证见物品与制作。
  仅盾牌的提高现在与全局提高组合，最终的小数防御总量按半进位取整。
  盾牌排除也会把普通盾牌基底从玩家池中移除。
  Determination/Grace 的整型光环缩放与确切的 aura/药剂更多因子组合现在会喂给这些池子。
  按身体部位的护盾提高、翻倍护甲与无护甲运算符现在保留其槽位范围，含 IR。
  每十点最终力量给物品护盾、每十点最终智力给闪避现在使用普通全局 INC，含 IR。
  完整的原生向量、来源移除、资格、生效集与显式零都有自动化覆盖；实际命令报文覆盖护盾钳制且不补给。
  十四个新的供给来源原生对照现在验证了修正后的护盾/闪避发布、普通装备互换、容量不补给/钳制与两种模式下的持久重载。
  Shaper's Touch 的其他效果与合法传奇获取仍属独立工作；两行防御都还不完整。
  普通的内禀护盾项现在跟随原生帮助文本与钉死的 PoB：每十点最终智力 1%。
  聚焦 216 与完整 Gateway 3939 无跳过通过，含十一个新基線用例。
  请原生核对依赖的回充/满护盾条件与排队发送调度；把先前的抓取保留为历史证据。
  五个原生禁止输入现在只压制内禀属性授予，保留属性、需求与物品组项。八十个新自动化用例与全新 Gateway 4019/Core 648/Data 37
  无跳过通过。请原生核对那些标记输入与重载；更早的十四个对照不能证明这些新运算符。
  确切的官方小数/整组运算、剩余禁止/转换与全来源的即时生命发布仍是独立工作；普通装备生命仍跟随恢复 tick。
  满护盾压制几率现在跟随有效资源状态，并共用面板/战斗消费者。
  普通的带类型/全属性提高现在为池子、装备与宝石共用一个整源计算。
  自动化真实来源/移除、零、派生池与生效集检查在两种模式下通过。三十四个原生对照验证了这些状态、重登后武器组保留与
  普通 Fireball 的需求拒绝/恢复（记录于移植仍保持一个合格宿主生效期间；宿主现在随其宝石一起失效，
  按 main 恢复的规则）。供给来源对照不能证明传奇获取。
  钉死 PoB 的小数取整仍是带标注的投影。带符号的装备循环、属性转换、条件/更多运算符与佣兵闸门仍待处理。
  六个缺失的普通生命/魔力系数现在在提高之前加入固定池：带符号的每级生命/魔力、每十点最终敏捷/智力的生命、
  每四点敏捷与每两点智力。实际等级与完成的属性组只应用一次；原生 -12/-6/-3 的每级生命削减保留其符号。
  整系数的开发用零/替换、池重建与资源发布保留既有的基于力量的魔力与保留路径。全部 71 个新用例
  （45 个纯/原生来源与 26 个 Gateway）加上 82 个既有回归通过：153 个聚焦无跳过。
  完整 Gateway 3874/3874 无跳过通过；本会话更早未改动的 Core 648/Data 37 仍适用（合计 4559 适用；
  这些改动之后只重跑了 Gateway）。那次运行的钩子记录了七个实际未被观察到的已释放 NetworkStream 错误：
  六个 `MonsterKilled` → `Send`，一个经投射物命中路径的 `NotifyMonsterHit`；另有 390 条首次机会记录。
  没有捕获到诊断错误/截断/上限命中或新的池来源路径；新的原生核对待做。
  保留的最终池取整与钉死 PoB 的最终取整不同，所以官方小数一致性仍待处理。
  孤立的来源夹具不能证明获取/事件天赋准入或完整转换节点，这个子集也不能完成任一生命/魔力面板行或其条件/更多/转换运算符。
  狂怒的攻击/施法速度系数包含合并的按充能修饰量，带两种模式下的带符号/零、来源移除与持有计数一致性覆盖
  （17 个选定测试，15 个新增）。八个原生供给来源对照验证了数值、正数格式、清除与重登；
  原生传奇装备获取与零行格式证据不在那些对照之内。
  四条元素自身持续时间削减行现在只减一次原始的全异常状态自身持续时间来源；Harsh Lessons 的真实 -20 在两种模式下给出 +20 削减。
  其 14 个新用例在合并的 53 个聚焦用例内通过，随后是完整 Gateway 3710/3710；未改动的 Core 648/Data 37 仍适用（合计 4395）。
  五个已释放流后台错误仍被记录且无新运算符引用；原生验证待做。左右环运算符、其他异常状态家族、条件护盾延迟与
  实际异常状态执行仍属独立工作；不要把两个槽位本地的环系数全局相加。
  合并后的格挡/眩晕恢复与固定的每级击杀/药剂持续时间别名现在共用一个原始快照与实际角色等级。
  十个 10/20/100 级的正来源对照在标准/无情下匹配全部六个固定字段。与六个功能药剂和四个生命对照合计，
  40 个完成的原生对照里 22 个通过；18 个完整零对照仍为 UNPROVED。六字段结论合计 138 VERIFIED / 42 UNPROVED，
  跨来源、保留与清除/恢复/重登证据；隐藏零的渲染未确立。
  完整的击杀恢复/百分比/禁止规则与格挡动画时序仍是独立工作。
  更早的面板/药剂批次通过了 55 个新增加 80 个既有聚焦检查与 Gateway 3657/3657；此前未改动的 Core 648/Data 37 结果仍适用。
  这些历史原生/测试结果不能验证新的自身持续时间/药剂效果运算符。随后一批八个正对照的药剂效果批次在两种模式的
  实际 10/100 级验证了恢复速率/总量与最终功能药剂移动/护甲强度、充能成本、原生计时器时长/移除、规范恢复与代表性重登。
  功能药剂验证器检查的是显式最终属性，而不是未使用的恢复值通道；抓取后的修订仍会披露。
  自身持续时间的原生证据、来源移除/零下次使用与生效来源变化、本地/魔法组合、传奇获取与提示一致性仍属独立工作。
  旧的 18 个全零对照仍为 UNPROVED。细节在 protocol13 §8。
  在加公式之前，继续按当前实现审计剩余的行。
  吸血上限现在有原生显示、词缀、禁止、池转换、保留与重登对照。
  普通生效吸血现在有已接受命中与资源写入实现；官方的小数端点取整未测量。
  当前分支现在会从原始角色、辅助与合格手输入重算全部 24 个作用域吸血系数与三个速度别名。
  这套组合/恢复的原生核对待做。普通引擎使用独立的持续时间、总恢复与最大速率规则（伤害 §5b）。
  Ghost Reaver 现在有独立的转换/原生护盾实例、上限/回充效果与来源/退点对照。请继续其他转换、条件与武器家族来源、
  技能专属恢复速度与速度/防御/异常状态/DoT 吸血状态消费者。实际的吸血/满护盾标记与普通/选定命中伤害条件现在有辅助、
  接触与空闲刷新对照；不要添加重叠的显示作用域或用回复替代吸血。
  仅攻击的护盾溢出吸血现在保留实例来源与共享上限；普通生命保留现在尊重零。
  请继续其他生命保留虚拟来源与原生验证；不要扩大仅攻击的物品范围。
  爪精通/武器大师的即时来源与生命即时转换现在有真实来源对照。
  请继续剩余条件即时来源，并在认证前验证原生混合转换/原生护盾组合。
  Highwayman 的总恢复翻倍与显式生命最大速率翻倍标记现在有各自的消费者与对照。
  请验证原生叠加/恢复与当前来源获取；不要从遗留的原始标记名推断 Vaal Pact 行为，也不要按每件物品副本乘一次布尔翻倍。
  原生转换/禁止交互仍未验证；保守的「来源加目标」策略与备选方案记录在伤害 §5b 与评审报告里。
  Ward 与不受支持的条件机制仍需要各自的系统与证据。
  武器本地的带类型伤害与乘算物理品质现在会喂给普通与受支持攻击，带正确的近战/投射物上下文与生效手来源。
  池/充能/时序/格挡条件使用生效装备。二十四个原生发布对照在标准/无情下通过，并有一个独立的本地词缀/品质提示。
  替代品质、受支持的接触原生组合与副手交替仍待处理。
  本地固定增强先于提高的暴击率已实现，带自动化来源/顺序用例；原生验证仍待做，且不能由更早的武器本地检查提供。
  伤害转换现在会为目的地/中间提高与继承的带类型更多因子保留访问过的类型修饰历史。
  Core 接缝有 21 个新路径用例、12 个既有报文对照，全部 648 个 Core 用例通过。
  共享玩家解析器还补上了此前遗漏的、在远程/攻击宝石命中上的充能更多。
  全部 43 个新 Gateway 用例、340 个聚焦检查与完整 Gateway3602/Core648/Data37 套件通过。三十二个原生数值对照在两种模式下通过：
  24 个原始的显式字段与 8 个单独作用域的稀疏零对照，含一个回顾性诊断基准。四个恢复/重登检查与全部 714 个冻结哈希通过。
  这些只证明本地面板发布；权威命中接触与条件运算符、仅端点修饰量、异常状态/DoT 与官方小数取整一致性仍是独立工作。
  药剂充能获取来源解析与独立的混合生命/魔力速率/增益生命周期有自动化覆盖，以及两种模式下 24 个原生对照。
  条件药剂来源与排队后的计时器/预测呈现仍待处理；见物品与制作状态文档。
  最终受支持法术的范围与暴击前平均值/DPS 现在会跟随连携辅助变化并清除被移除的类型。
  原生标准检查把十一个正格式抓取绑定到全新的自有技能属性，验证移除与重登，并为 Brutality/正义之火保留部分零行证据。
  原生就地连接变更、官方暴击加权、DoT 与特殊技能机制仍待处理。
- **完成标准：** 生命、魔力、属性、抗性及其派生值来自服务端自己的计算（Characters.dat 基值、等级、物品、天赋）
  并放在 Stats 组件里下发。
  超上限抗性的派生闪避与暴击提高现在遵循该规则，并已原生验证。
  通用属性提高在 `60044ad` 有历史性的原生原始来源、派生池与需求对照。
  当前 main 的每点力量魔力、需求与非生效武器的组合有自动化覆盖。
  十一个变基后的原生对照验证了最终力量魔力组、魔力提高只算一次与普通装备需求。
  在 `c9cd1864` 的有序联合原生对照验证了最终力量之后的转换与奇数智力。
  剩余的原生组合包括自身物品百分比保留、级联装备、天赋退点与互换。
  小数算法仍是有文档的 PoB 服务端模型；官方服务端一致性未确立。
  见[状态](../status/skills-and-combat.md)与[计算模型](../docs/protocol/11-skills.md) 第 4 节。
  Phrecia 游玩兼容仍是独立的前置条件。
  Power of Purpose 有历史性的原始 head 的原始来源、护甲/魔力、实际攻击消费者与重登对照。
  原生空闲保留、Iron Reflexes 与 MORE 组合仍待做；其模型与自动化测试在[计算说明](../docs/game/power-of-purpose.md)。
  合并的属性提高、每点力量魔力与转换在基线 `939b44fe` 上有回归。
  在 `c9cd1864` 的联合原生对照仍属历史；变基后的候选需要单独的原生验收。
- **Known:** client stat key = Stats.dat row + 1 ([11](../docs/protocol/11-skills.md) §8); `/set_stat` takes names.
  The server must send final values (the client derives none): the attack's are computed in
  `SkillCatalog.Unarmed.Stats`.
  Mana per complete group of final Strength now has native raw-source/gear controls and automated pool coverage.
  Raw-source reservation refresh and clamping have decrypted wire coverage; recheck active auras natively.
  See [Status](../status/skills-and-combat.md) and [11](../docs/protocol/11-skills.md) section 4.
  Mental Conditioning now combines mana per Strength with mana-to-armour conversion in current-main tests.
  Historical joint native conversion controls passed at `c9cd1864`; Phrecia selection stays separate.
  Ordinary one-use cooldown recovery now shares the native base/rate and a server base-time budget, with atomic
  preflight before action side effects.
  Initial Dash recovery, early raw rejection, pause and native mid-cooldown rate changes pass live alongside
  layout/send-gate checks; remaining base progress is retained without consuming another use.
  Remaining cooldown work includes multi-use recharge, shared groups, typed/conditional/more recovery,
  official tick boundaries.
  See [11](../docs/protocol/11-skills.md) §9.
- **Spell cast timing (#192):** native base milliseconds are preserved and cast speed adjusts server waits and
  displayed rate once. Automated checks and real-client -80%/+100%/zero clip durations pass.
  Projectile release/contact alignment was not separately measured.
  Instant casts and attack timing stay separate; conditional action-speed and special native mechanics remain open.
  Attack gems now share level-aware inherent speed with server pacing/contact and the APS stat; the client applies its
  own to the clip. Native input verified the ordinary 0.714 s bow duration and Burning Arrow 1.02 s.
  Open: an attack longer than its clip stretches the clip's slow span, which `AttackContactDelay` does not model
  ([11](../docs/protocol/11-skills.md)), and the arrow still arrives about 25 ms after the muzzle flash, with a fixed
  60 ms lead tuned on localhost rather than one that follows the connection's latency.
  Attack-gem damage/DPS and conditional speed remain separate work.
- **Spell hit panel (#188):** selected-spell ranges now share combat's converted packet and hitless spells clear
  inherited weapon hit damage. Automated checks pass; verify selection, equipment updates and relog in the client.
  DPS now includes the current critical chance/multiplier and signed luck, with exact action duration and
  bounded native integers. Attack averages/DPS now include reference accuracy and independent critical confirmation.
  Integer-roll expectation,
  cooldowns and DoT/beam displays remain separate work. Quality conversion and existing modifier precedence
  are unchanged. Selected attack-gem main-hand ranges and critical-weighted averages share the combat packet.
  Native selection, added-damage refresh/clearing, server hit and relog checks pass alongside automated tests.
  Equipment UI refresh remains to be checked separately.
  Off-hand alternation, conditional damage, supports and modifier-scope corrections remain separate work.
- **Warcry restriction (#297):** shared actor admission has regression and seven real-client controls in Status.
  Blocked input, source clearing, paid retries and an accepted cry's release are verified.
  See [11](../docs/protocol/11-skills.md) section 10b; broader warcry engines and Phrecia enablement remain separate.
  Warcry speed, one-use warcry cooldowns and the shared release effects (section 10i) run for Enduring Cry; the other
  warcries need executors, and explosions need their global added damage, flat crit chance, on-hit effects, the
  native explosion art (a generic gore burst stands in) and the wave delay.
- **Character panel (#75):** the Energy Shield regeneration row now shares the implemented recovery calculation,
  with native checks for modifiers, clearing and separation from recharge.
  Ordinary Life/Mana regeneration now includes separate regeneration/recovery increases and matching prohibitions.
  Native actual-recovery, clearing and relog checks pass; Fortification expiry integration uses the same factors.
  Zealot's Oath now moves life regeneration to Energy Shield; other regeneration conversions, named
  conditional/more modifiers and other recovery systems remain open.
  Exact native Brand/special-charge defaults now enter actor sources once, with a native selected Brand gate.
  Real item/passive/support composition gives actor Brand 6 and selected 8; explicit zero and source removal are covered
  by automated cases in both modes, with new native checks pending.
  Three sourced mirror flags publish special maxima from completed standard maxima, including final zero overrides;
  real unequal 4/5/6 source fixtures preserve each mapping. Pinned PoB 3.29.x remains a labelled fallback.
  Brutal / Affliction / Absorption counts, effects and buffs now follow the three conversion flags (Status, charges);
  Brand attachment/cap enforcement, the Affliction non-damaging effect and Absorption recoup consumers and
  Spirit/Abyss-jewel counting remain open; these changes do not certify entire charge rows.
  Every panel row now has a server derivation (the 2026-10-29 batch in Status covers the Defence estimates, off-hand,
  effective critical, secondary and damaging-ailment rows; the owner's live run of 2026-10-29 passed them, the converted
  charges and the Blood Charges); what remains for #75 is Reap's
  damage over time debuff and Vaal Reap's temporary Blood Charge maximum, and executing the secondary hits (Bear Trap,
  Herald of Ice) whose rows are display-only. Deferred (2026-10-29, owner's decision): Spirit and Siphoning Charges. Their only 3.29
  player sources are unique mods (`maximum_spirit_charges_per_abyss_jewel_equipped`,
  `maximum_siphoning_charges_per_elder_or_shaper_item_equipped`), so they need, in the items area, unique definitions
  beyond Mageblood (#60), abyss sockets on the other uniques that have them (only the Stygian Vise rolls its socket)
  and Shaper/Elder a consumer for `Item.Influences` (13-items.md §2f). Blitz and Challenger
  Charges have no 3.29 player source at all.
  Selected area/item-rarity/minion-increased aliases now use one raw actor-plus-own snapshot, with explicit final
  zero/replacement precedence, source clearing and Cyclone radius preservation. `SendPlayer` initial/transition
  creation and later skill refreshes share the snapshot path. Real support controls cover ordinary availability.
  The historical LocalExile Ruthless exclusion of Item Rarity Support is outside this port.
  Golem combat/drop execution is connected; other minion executors and native alias checks remain open.
  Selected spell penetration now includes real Fire/Cold/Lightning Penetration supports, with source/link/requirement
  lifecycle and hit-consumer agreement covered by nine new Gateway regressions.
  Attack-hand alias composition now includes attack-only sources, linked supports and each active hand's local
  modifiers, with automated lifecycle checks.
  Display prohibitions and weapon-local/non-base combat penetration now have automated source/lifecycle/contact
  coverage. Verify the updated rows and contacts natively; off-hand attack selection and the conditional penetration
  forms not read yet (Zealotry's critical-only, Enmity's Embrace's overcapped fire, recent kill or block, flask,
  tincture, while chilled, single-skill) still need implementation and validation.
  Selected ailment duration/effect coefficients now include real Unbound Ailments contributions, with 24
  source/link/override regressions. Validate the twelve aliases natively, then complete ailment chance/eligibility,
  damaging ranges, duration/application and stacking; a correct coefficient alone does not prove those consumers.
  Added-base/default-mirror/selected-alias coverage passes 52/19/22 new cases and 95 existing controls: 188 focused
  cases without skips. Fresh full Gateway 3803/Core 648/Data 37 pass without skips (4488 freshly rerun cases).
  The current hook records six actual unobserved disposed-NetworkStream faults, all `MonsterKilled` through
  `Program.Send`, with no captured new operator path or diagnostic errors/truncation/cap hit.
  Historical G14/full 3710 and earlier native controls do not establish these operators or complete an issue 75 row.
  Armour/evasion now include ordinary item-local flat/increased modifiers and quality, with native tooltip/panel,
  crafting refresh, unequip and active-shield checks.
  Saved variable bases now feed all three defences before local modifiers; legacy saves retain minimum bases.
  Sacred Orb rerolls and supported special ES rules are implemented; see Items and crafting for scope and validation.
  Shield-specific increases now compose with global increases, and final fractional defence totals round halfway up.
  Shield exclusions also remove ordinary shield bases from the player pools.
  Determination/Grace integer aura scaling and exact aura/flask more-factor composition now feed those pools.
  Body-specific ES increases, doubled Armour and no-Armour operators now retain their slot scope, including IR.
  Item ES per ten final Strength and Evasion per ten final Intelligence now use ordinary global INC, including IR.
  Complete native vectors, source removal, qualification, active sets and explicit zero have automated coverage;
  actual command packets cover ES clamping without refill. Fourteen fresh supplied-source native controls now verify
  corrected ES/Evasion publication, ordinary gear swaps, capacity no-refill/clamping and persisted reload in both modes.
  Other Shaper's Touch effects and legal unique acquisition remain separate; neither defence row is complete.
  The ordinary inherent ES term now follows native help text and pinned PoB: 1% per ten final Intelligence.
  Focused 216 and full Gateway 3939 pass without skips, including eleven new baseline cases.
  Verify dependent recharge/full-ES conditions and queued-send scheduling natively; preserve prior captures as historical.
  Five native prohibition inputs now suppress only the inherent attribute grants, retaining attributes, requirements
  and item group terms. Eighty new automated cases and fresh Gateway 4019/Core 648/Data 37 pass without skips.
  Verify those flag inputs and reload natively; the earlier fourteen controls do not certify the new operators.
  Exact official fractional/group arithmetic, remaining prohibitions/conversions and all-source immediate Life
  publication remain separate work; ordinary equipment Life still follows the recovery tick.
  Full-ES suppression chance now follows the effective resource state and shares the panel/combat consumer.
  Ordinary typed/all-attribute increases now share one whole-source calculation for pools, equipment and gems.
  Automated real-source/removal, zero, derived-pool and active-set checks pass in both modes. Thirty-four native
  controls verify those states, weapon-set persistence after relog and ordinary Fireball requirement refusal/recovery
  (recorded while the port kept a qualified host active; the host now goes inactive with its gem, main's restored
  rule). Supplied-source controls do not certify unique acquisition.
  Pinned-PoB fractional rounding remains a labelled projection. Signed equipment cycles, attribute conversions,
  conditional/more operators and mercenary gates remain open.
  Six missing ordinary Life/Mana coefficients now join flat pools before increases: signed per-level Life/Mana,
  Life per ten final Dexterity/Intelligence, per four Dexterity and per two Intelligence.
  Actual level and completed attribute groups apply once; native -12/-6/-3 Life-per-level reductions retain their sign.
  Whole-coefficient developer zero/replacements, pool rebuilding and resource publication preserve the existing
  Strength-based Mana and reservation paths. All 71 new cases (45 pure/native-source and 26 Gateway) plus 82 existing
  regressions pass: 153 focused with no skips. Full Gateway passes 3874/3874 without skips; earlier unchanged
  Core 648/Data 37 from this session remain applicable (4559 applicable; only Gateway rerun after these changes).
  That run's hook records seven actual unobserved disposed-NetworkStream faults: six `MonsterKilled` → `Send`
  and one `NotifyMonsterHit` through the projectile-hit path; 390 first-chance records are separate.
  No diagnostic errors/truncation/cap hit or new pool-source path is captured; new native checks remain pending.
  The retained final pool floor differs from pinned PoB's final round, so official fractional parity remains open.
  Isolated source fixtures do not prove acquisition/event-passive admission or full conversion nodes, and this subset
  does not complete either Life/Mana panel row or its conditional/more/conversion operators.
  Frenzy attack/cast speed coefficients include the combined per-charge modifier, with signed/zero,
  source-removal and held-count agreement covered in both modes (17 selected tests, 15 new).
  Eight native supplied-source controls verify values, positive formatting, clearing and relog; native unique-gear
  acquisition and zero-row formatter evidence remain outside those controls.
  The four elemental self-duration reduction rows now subtract the raw all-ailment self-duration source once;
  Harsh Lessons' real -20 gives +20 reduction in both modes. Its 14 new cases pass within the combined 53 focused
  cases, followed by full Gateway 3710/3710; unchanged prior Core 648/Data 37 remain applicable (4395 total).
  Five disposed-stream background faults remain recorded without new-operator references; native verification is
  outstanding. Left/right ring operators, other ailment families, conditional ES delay and actual
  ailment execution remain separate; do not sum both slot-local ring coefficients globally.
  Combined Block/Stun Recovery and flat per-player-level on-kill/flask-duration aliases now use one raw snapshot
  and actual character level. Twelve positive source controls at levels 10/20/100 match all six fixed fields in
  Standard/Ruthless. Combined with six utility and four Life controls, 22 of 40 completed native controls pass;
  the 18 complete zero controls remain UNPROVED. Six-field verdicts total 138 VERIFIED/42 UNPROVED across source,
  retained and clearing/restoration/relog evidence; hidden-zero rendering is not established.
  Full on-kill recovery/percentage/prohibition rules and block-animation timing remain separate work.
  The preceding panel/flask batch passed 55 new plus 80 existing focused checks and Gateway 3657/3657;
  prior unchanged Core 648/Data 37 results still apply. These historical native/test results do not validate the new
  self-duration/flask-effect operators. A subsequent eight-control positive flask-effect batch at actual levels10/100
  in both modes verifies recovery rate/amount and final utility movement/armour strength, charge costs, native timer
  duration/removal, canonical restoration and representative relog. The utility verifier checks explicit final stats
  rather than the unused recovery-value channel; after-capture revisions remain disclosed. Self-duration native
  evidence, source-removal/zero next-use and active-source changes, local/magic composition, unique acquisition and
  tooltip parity remain separate. The old18 whole-zero controls remain UNPROVED. Details are in protocol13 §8.
  Continue auditing the remaining rows against the current implementation before adding formulas.
  Leech limits now have native display, modifier, prohibition, pool-conversion, reservation and relog controls.
  Ordinary active leech now has an accepted-hit and resource-writer implementation; official fractional endpoint rounding is unmeasured.
  The current branch now recomputes all 24 scoped leech coefficients and three speed aliases from raw actor,
  support and qualified hand inputs. Native checks for this composition/recovery remain pending. The ordinary engine
  uses separate duration, total recovery and maximum-rate rules (damage §5b). Ghost Reaver now has independent
  converted/native ES instances, cap/recharge effects and source/refund controls. Continue other conversions,
  conditional and weapon-family sources, skill-specific recovery speeds and speed/defence/
  ailment/DoT leech-state consumers. Actual leech/full-ES flags and ordinary/selected hit-damage conditions now have
  support, contact and idle-refresh controls; do not add overlapping displayed scopes or substitute regeneration for leech.
  Attack-only ES overleech now preserves instance origin and the shared cap; ordinary Life retention now honors zero.
  Continue other Life-retention virtual sources and native validation; do not broaden the attack-only item scope.
  Claw mastery/Weapon Master instant sources and Life-instant conversion now have real-source controls. Continue
  remaining conditional instant sources and verify native mixed converted/native ES composition before certification.
  Highwayman's total-recovery doubling and the explicit Life maximum-rate doubling flag now have separate
  consumers and controls. Verify native stacking/recovery and current source acquisition; do not infer Vaal Pact
  behavior from legacy raw flag names or multiply one boolean doubling per item copy.
  Native conversion/prohibition interaction remains unverified; the conservative source-plus-destination policy
  and the alternative are recorded in damage §5b and the review report.
  Ward and unsupported conditional mechanics still need their own systems and evidence.
  Weapon-local typed damage and multiplicative physical quality now feed ordinary and supported attacks,
  with correct melee/projectile context and active-hand sources. Pool/charge/timing/block conditions use active gear.
  Twenty-four native publication controls pass in Standard/Ruthless, with an independent local-mod/quality tooltip.
  Alternative quality, supported-contact native composition and offhand alternation remain open.
  Local flat-before-increased critical chance is implemented with automated source/order cases; native validation
  remains outstanding and is not supplied by the earlier weapon-local checks.
  Damage conversion now retains visited-type modifier history for destination/intermediate increases and inherited
  typed more factors. The Core seam has 21 new path cases, 12 existing packet controls and all 648 Core cases passing.
  The shared player resolver also supplies previously omitted charge more on ranged/attack-gem hits.
  All 43 new Gateway cases, 340 focused checks and full Gateway3602/Core648/Data37 suites pass. Thirty-two native
  numeric controls pass across both modes: 24 original explicit-field and eight separately scoped sparse-zero
  controls, including one retrospective diagnostic baseline. Four restoration/relog checks and all 714 frozen hashes
  pass. These prove local panel publication; authoritative hit contacts and conditional operators,
  endpoint-only modifiers, ailments/DoT and official fractional-rounding parity remain separate.
  Flask charge-gain source resolution and separate hybrid Life/Mana rate/buff lifetimes have automated coverage
  and 24 native controls across both modes. Conditional flask sources and queued timer/predictor presentation
  remain open; see the items-and-crafting status.
  Final supported-spell ranges and pre-critical average/DPS now follow linked support changes and clear removed
  types. Native Standard checks bind eleven positive formatter captures to fresh owned skill stats, verify removal
  and relog, and retain partial zero-row evidence for Brutality/Righteous Fire. Native in-place link mutation,
  official critical weighting, DoT and special skill mechanics remain open.
The issue #75 target is the 357 virtual stats behind the character panel ([11](../docs/protocol/11-skills.md) §14).
Each needs its sources, consumers and lifecycle traced; no completion follows from a historical native run or a
matching source name.
The issue #75 native matrices were run on the source LocalExile revisions, not repeated on this port.

- **Accuracy and criticals:** review the remaining ordinary-bow snapshots and special repeats, complete the remaining
  accuracy conditions, correct off-hand sources and publish all chance/always-hit rows; dual-hand, multi-hit and
  integer-roll expectations are open ([20](../docs/protocol/20-damage.md) §2h).
- **Impale:** conditional weapon/Pride chance, extra applications and consumption overrides, Impaler mechanics,
  native count/visual publication, corpse consumers and official duration rounding ([20](../docs/protocol/20-damage.md)
  §3a).
- **Cooldowns:** remaining stored-use sources, conditional recovery modifiers and official tick boundaries
  ([11](../docs/protocol/11-skills.md) §9).
- **Attack timing:** an attack longer than its clip stretches the clip's slow span, which `AttackContactDelay` does not
  model; the arrow arrives about 25 ms after the muzzle flash, with a fixed 60 ms lead tuned on localhost rather than
  one that follows the connection's latency ([11](../docs/protocol/11-skills.md) §4).
- **Spell and attack panels:** integer-roll expectation, cooldowns, DoT/beam displays, in-place link mutation,
  off-hand alternation, conditional damage and support/modifier scope; check equipment UI refresh live.
- **Warcries:** executors for the other warcries; explosions need global added damage, flat crit chance, on-hit
  effects, their native art and the wave delay ([11](../docs/protocol/11-skills.md) §10i).
- **Character panel (#75)** ([11](../docs/protocol/11-skills.md) §14):
  - Regeneration: other conversions than Zealot's Oath, named conditional/more modifiers and other recovery systems.
  - Charges: Brand attachment/cap enforcement, the Affliction non-damaging effect and Absorption recoup consumers.
    Reap's damage over time debuff, Vaal Reap's temporary Blood Charge maximum, and executing the secondary hits
    (Bear Trap, Herald of Ice) whose rows are display-only.
  - Deferred (owner's decision, 2026-10-29): Spirit and Siphoning Charges. Their only 3.29 player sources are unique
    mods (`maximum_spirit_charges_per_abyss_jewel_equipped`,
    `maximum_siphoning_charges_per_elder_or_shaper_item_equipped`), so they need unique definitions beyond
    Mageblood (#60), abyss sockets on the other uniques that have them and a consumer for `Item.Influences`
    ([13](../docs/protocol/13-items.md) §2f). Blitz and Challenger have no 3.29 source.
  - Penetration: off-hand attack selection and the conditional forms not read yet (Zealotry's critical-only, Enmity's
    Embrace's overcapped fire, recent kill or block, flask, tincture, while chilled, single-skill).
  - Ailments: validate the twelve duration/effect aliases, then ailment chance/eligibility, damaging ranges,
    duration/application and stacking. Left/right ring operators are slot-local: do not sum them globally.
  - Defences: official fractional/group arithmetic, the remaining prohibitions and conversions, all-source immediate
    Life publication, other Shaper's Touch effects, Ward and the conditional/more pool operators.
  - Attributes: signed equipment cycles, attribute conversions, conditional/more operators and mercenary gates.
  - On kill and flasks: full on-kill recovery/percentage/prohibition rules, block-animation timing, conditional flask
    sources and queued timer/predictor presentation.
  - Leech ([20](../docs/protocol/20-damage.md) §5b): other conversions, conditional and weapon-family sources,
    skill-specific recovery speeds and the speed/defence/ailment/DoT leech-state consumers; other Life-retention
    sources. Do not add overlapping displayed scopes, substitute regeneration for leech, broaden the attack-only item
    scope, infer Vaal Pact from legacy raw flag names, or multiply one boolean doubling per item copy.
  - Damage: alternative quality, off-hand alternation, endpoint-only modifiers and official rounding.
  - Continue auditing the remaining rows against the current implementation before adding formulas.
- **Done when:** life, mana, attributes, resistances and their derived values come from the server's own calculation
  (Characters.dat base values, level, items, passives) and are sent in the Stats component.
  Open beyond that: own-item percentage retention, cascading equipment, passive refunds and swaps natively; Power of
  Purpose's idle reservation, Iron Reflexes and MORE natively ([Power of Purpose](../docs/game/power-of-purpose.md));
  official-server fractional parity; Phrecia play compatibility.

### 光环：保留与效果

- **已知：** `GrantedEffectsPerLevel` 携带保留；生命池的已保留字段与 S→C 0x1b1 更新它；
  Purity of Elements/Fire/Ice/Lightning、Clarity、Vitality、Precision 与 Haste 已实现，带其增益视觉与 `/auras`，
  待实机验证（[11](../docs/protocol/11-skills.md) 13、[状态](../status/skills-and-combat.md)）。
  自身光环的准入与挂起有来源/派发/发送闸门/生命周期覆盖，含狂怒刷新。
  十五个原生对照验证了已安装的成对光环、实际加成/魔力、拒绝、恢复、重登与死亡。
  原生灰图标预测与持久限制装备仍待单独验证。
  重复实例的辅助上下文与排队普通确认有各自的回归对照。
  普通玩家与受控战士的光环共享、生命保留与 Blasphemy 诅咒执行已实现；剩余的敌人/旗帜/瓦尔执行器另属工作。
  被动敌人光环至今只下发并应用命中转换（[11](../docs/protocol/11-skills.md) 第 10e 节）；其他（Overwhelm、秘术师的）
  需要先有它们属性的消费者。
- **入手：** 光环的 3D 效果（`IsSkillBuff` 行的 BuffVisuals EPK 不像正义之火那样渲染）；旗帜与不受支持的条件效果。
  验证完整的先驱战斗矩阵、零售版的 Virulence 衰减，以及共享 Skitterbot 触发/重置生命周期之外的技能专属陷阱/地雷载荷。
  技能栏指示器的增益关联已实现（0x1c1 第 3 位；实机核对待做，见状态）。
  辅助宝石随宝石系统一起来（下面的「宝石」）；生命保留有自己的预算与恢复生命周期。
- **完成标准：** 标准光环正确保留，效果与 PoB 一致，且角色面板与 HUD 显示的已保留与未保留值相同。
- **Known:** `GrantedEffectsPerLevel` carries the reservation; the Life pool's reserved field and S→C 0x1b1 update it;
  Purity of Elements/Fire/Ice/Lightning, Clarity, Vitality, Precision and Haste are implemented with their buff
  visuals and `/auras`, awaiting live verification ([11](../docs/protocol/11-skills.md) 13,
  [Status](../status/skills-and-combat.md)).
  Own-aura admission and suspension have source/dispatch/send-gate/lifecycle coverage, including Rage refreshes.
  Fifteen native controls verify installed pairs, actual bonuses/Mana, refusal, restoration, relog and death.
  Native grey-icon prediction and persistent restricting equipment remain separate verification.
  Duplicate-instance support context and queued ordinary acknowledgements have separate regression controls.
  Ordinary player and controlled-fighter aura sharing, Life reservation and Blasphemy hex execution are implemented;
  remaining enemy/banner/Vaal executors are separate.
  Passive enemy auras send and apply hit conversions and Overwhelm's crit multiplier ([11](../docs/protocol/11-skills.md)
  section 10e); the others (the Occultist's) need consumers for their stats first.
- **Start:** the auras' 3D effects (BuffVisuals EPKs of the `IsSkillBuff` rows do not render, unlike Righteous
  Fire's); banners and unsupported conditional effects. Verify the complete herald combat matrix,
  retail Virulence decay and the skill-specific trap/mine payloads beyond the shared Skitterbot trigger/rearm lifecycle.
  The skill-bar indicator's buff association is implemented (0x1c1 bit3; live check pending, see Status).
  Support gems come with the gem system ("Gems" below); Life reservation has its own budget and recovery lifecycle.
- **Known:** [11](../docs/protocol/11-skills.md) §13 has reservation, the implemented auras and the own-aura lifecycle.
  Passive enemy auras other than the implemented ones (the Occultist's) need consumers for their stats first (§10e).
- **Start:** the auras' 3D effects (BuffVisuals EPKs of the `IsSkillBuff` rows do not render, unlike Righteous Fire's);
  banners, enemy/Vaal aura executors and unsupported conditional effects.
  Verify the herald combat matrix, retail Virulence decay, the skill-specific trap/mine payloads beyond Skitterbots'
  trigger/rearm, and the skill-bar indicator's buff association (0x1c1 bit3) live.
- **Done when:** the standard auras reserve correctly, their effects match PoB, and the character panel and HUD show
  the same reserved and unreserved values.

### 区域进入保护核对

- **已知：** 宽限期保护与原生计时增益在城镇与藏身处之外已实现。
- **入手：** 在真实客户端验证 60 秒倒计时、第一步即移除与伤害阻止。图标可见；
  其修正后的浮点计时器需要在上游整合之后实机复核。
- **完成标准：** 计时器倒计时，保护在移动、动作或到期时结束。
- **Start:** verify the Grace Period's 60-second countdown, first-step removal and damage prevention live.
- **Done when:** the timer counts down and protection ends on movement, actions or expiry.

### 伤害模型（基础已完成，接下来是更深的防御）

- **客户端 Buff 描述符消费者后续（#370，已知问题 K1-K3）：** 从 `ChaosDegenerationAuraPlayersUniqueBodyStr3`
  跟踪一个准备好的描述符到真实读取者，并解释其输出。
  [归属/传输报告](../docs/re/client-buff-descriptor-consumers.md) 是有界的 #466 交付物。
  Mods 的 `+0x38` 与实体 ObjectMagicProperties 的 `+0xc8` 分别内嵌该准备好的结构。
  只有在出现新的同对象传输或带类型读取者证据时才继续；已停止的清单保持停止。
  绑定一个有用途的读取者及其输出；复制/清理与独立缓存并不能完成原来的 N1/N2。
  复用 PR #371 的固定证据，不要导入其整个分支；目前尚未确立任何玩法消费者。
  正向的配置/呈现分类与数值 DoT 发布是两件事。
  把任何重启的调查范围定为 #370 下的单独批次；#466 让 K1-K3 保持未结。

- **已完成：** 命中是按类型的 `DamagePacket`；转换按物理、闪电、冰霜、火焰、混沌，含链式转换、100% 上限与
  「获得为额外」。
  DoT/异常状态公式与小数 tick 在同一个模型里，按类型的抗性减免在报文上生效。
  普通攻击、Fireball 与怪物近战使用报文并读取玩家/技能的转换属性
  （[20](../docs/protocol/20-damage.md)）。
  玩家对怪物的命中按类型取目标的基础抗性（`MonsterTypes.Resistances` → 按等级难度的 `MonsterResistances`，
  `MonsterResistanceCatalog`/`HitMitigation`），命中的无条件穿透从钳制后的值里减去且不再二次钳制（`Penetration`）：
  每次命中都算通用按元素与元素类属性，攻击再加仅攻击变体，不能穿透标记会压制元素类的；DoT 与物理不受影响。
  怪物护甲仍未建模。
  玩家的真实生命、抗性、护甲与闪避由等级、天赋与基础装备算出，护甲/闪避会减免怪物命中（状态：护甲与闪避）。
  普通物理怪物近战现在可以触发原生的玩家受击恢复，并带匹配的服务端取消。
  复制存档的实机核对验证了行走、待处理施法取消、已释放飞行与一个眩晕免疫对照。
  敌方 TakeHit 恢复使用原生 Actor 类型 5，停止移动并使旧的挥击代次失效。
  历史上的 LocalExile 对照覆盖 50 ms 规划器、有界重新寻路、节奏与菜单暂停。这个独立移植的新原生验证仍待做。
  自动化回归保留完整攻击时长，并守护待处理挥击与 Boss 阶段完成。
  选定的 Antiquarian 现在有原生 Ward/Adrenaline、龟壳、必中/Far Shot 与盾牌对照。
  Whaletooth 会延长实际的 Enduring Cry 恢复；见[其模型与证据](../docs/game/antiquarian.md)。
- **已知：** 双方仍有固定的伤害范围（[11](../docs/protocol/11-skills.md) 5、6）；
  普通玩家的护盾吸收、回复与被打断的回充已实现并实机核对。
  当前护盾/预测发布现在在发送闸门处采样，带加密报文回归；请在原生客户端验证排队容量变化与预测器清除。
  附加伤害会读取其第一批派生属性：攻击的附加闪电获得最大魔力池的
  `attack_skills_have_added_lightning_damage_equal_to_%_of_maximum_mana` 百分比（Whisperer 的「we know all」），
  以及每点最终属性的全局附加攻击伤害（[20](../docs/protocol/20-damage.md) §2b）。
  武器本地的「此武器每 10 点 <属性>」与法术每点智力的附加伤害仍未被读取。
  Arcane Might 的六个现代契约共用攻击/面板消费者、最大独立授予与技能品质。
  普通 Crown 与精通生命周期有自动化覆盖；其原生验收仍待做。
  更早的 Whisperer 原始属性原生检查不能确立这套扩展组合的原生行为。
  Whisperer 的模块化移植加入通用带类型命中 Recoup、实际消耗与共享最强致盲消费者。
  确定性的生产状态对照覆盖合并的资源/转移/退役边界与重载安全的实用型 End 自动化与原子付费恢复；
  精确 head 的原生/重载验收仍在验证边界内。
  **下一步：** 完成精确 head 的完整套件与原生资源/致盲验收；见 [Whisperer](../docs/game/whisperer.md)。
  Iron Will/力量来源与缺失的上游条件法术生产者需要单独工作。
  不受支持的技能/变体执行与无关的 Crown 吸血另属工作；见
  [Arcane Might 边界](../docs/game/arcane-might.md) 与 [20](../docs/protocol/20-damage.md) §2c。
  Steel Willed 由完整组最终护甲换算的攻击提高有共享回归与原生覆盖。
  布尔型混沌命中的护甲有原生原始来源生命损失对照与共享的受击回归。
  混合怪物报文与护盾/格挡/抑制/强化有自动化覆盖；请原生复核。见状态与 protocol/20-damage.md 第 2e 节。
  Rampager 的原始法术限制有命中/面板与 RF 回归；十一个原生对照属历史（#293）。
  已验证的消费者范围见[状态](../status/skills-and-combat.md)。
  新的有序联合命中对照在 `c9cd1864` 通过；原始的 RF、Ice Nova 与重登检查仍属历史。
  Rampager 的其他机制与职业准入仍是独立工作。
  近期命中的护甲防御现在在实际与排队接触回归里与物理/混沌减免组合。
  完整的属性写入者（含 Rage、自身光环与 Mageblood）有到期与编码成对确认的回归。
  脏光环/强化组合也检查解码后的报文顺序、独立到期与空闲纠正。
  其十八个原始 head 对照仍属历史；新的有序联合对照在 `c9cd1864` 通过。
- **Done:** hits are per-type `DamagePacket`s; conversion follows physical, lightning, cold, fire, chaos, including
  chained conversion, the 100% cap and gain-as-extra.
  DoT/ailment formulas and fractional ticks are in the same model, and per-type resistance mitigation works on
  packets.
  The default attack, Fireball and monster melee use packets and read the player's/skill's conversion stats
  ([20](../docs/protocol/20-damage.md)).
  Player hits on monsters take the target's base resistance per type (`MonsterTypes.Resistances` →
  `MonsterResistances` by the level's difficulty, `MonsterResistanceCatalog`/`HitMitigation`), with the hit's
  unconditional penetration subtracted from the capped value and no second clamp (`Penetration`): general
  per-element and elemental stats on every hit, attack-only variants on attacks, the cannot-penetrate flag
  suppressing the elemental ones; DoT and physical take none. Monster armour is still not modelled.
  The player's real life, resistances, armour and evasion are computed from level, passives and base equipment, and
  armour/evasion mitigate monster hits (Status: Armour and Evasion).
  Ordinary physical monster melee can now trigger native player hit recovery with matching server cancellation.
  Copied-save live checks verified walking, pending cast cancellation, released flight and a stun-immune control.
  Enemy TakeHit recovery uses native Actor type 5, stops movement and invalidates old swing generations.
  Historical LocalExile controls cover the 50 ms planner, bounded repaths, pacing and menu pause.
  Fresh native validation of this standalone port remains pending.
  Automated regressions retain the full attack duration and guard pending swings and boss-stage completion.
  Selected Antiquarian now has native Ward/Adrenaline, Turtle Shell, always-hit/Far Shot and shield controls.
  Whaletooth extends actual Enduring Cry recovery; see [its model and evidence](../docs/game/antiquarian.md).
- **Known:** both sides still have flat damage ranges ([11](../docs/protocol/11-skills.md) 5, 6);
  ordinary player ES absorption, regeneration and interrupted recharge are implemented and live-checked.
  Current ES/prediction publication now samples at the send gate with encrypted-wire regressions;
  verify queued capacity changes and predictor clearing in the native client.
  Added damage reads its first derived stats: the attack's added lightning gets
  `attack_skills_have_added_lightning_damage_equal_to_%_of_maximum_mana` percent of the maximum mana pool
  (Whisperer "we know all"), and global added attack damage per final attribute is read
  ([20](../docs/protocol/20-damage.md) §2b).
  Weapon-local "with this Weapon per 10 <attribute>" and spell per-Intelligence added damage remain unread.
  Arcane Might's six modern contracts share attack/panel consumers, maximum independent grants and skill quality.
  Normal Crown and mastery lifecycle have automated coverage; their native acceptance remains pending.
  Earlier Whisperer raw-stat native checks do not establish this expanded composition's native behavior.
  Whisperer's modular port adds generic typed-hit Recoup, actual spending and shared strongest Blind consumers.
  Deterministic production-state controls cover the combined resource/transfer/retirement boundaries and reload-safe
  utility End automation and atomic paid recovery; exact-head native/reload acceptance remains in the validation boundary.
  **Next:** complete exact-head full suites and native resource/Blind acceptance; see [Whisperer](../docs/game/whisperer.md).
  Iron Will/Strength provenance and missing upstream conditional spell producers require separate work.
  Unsupported skill/variant execution and unrelated Crown leech remain separate; see
  [Arcane Might boundaries](../docs/game/arcane-might.md) and [20](../docs/protocol/20-damage.md) §2c.
  Steel Willed's attack increase from complete groups of final armour has shared regression and native coverage.
  Boolean chaos-hit armour has native raw-source Life-loss controls and shared incoming-hit regressions.
  Mixed monster packets and ES/block/suppression/Fortification have automated coverage; recheck them natively.
  See Status and protocol/20-damage.md section 2e.
  Rampager's raw spell restriction has hit/panel and RF regressions; eleven native controls are historical (#293).
  See [status](../status/skills-and-combat.md) for the verified consumer scope.
  Fresh ordered joint hit controls pass at `c9cd1864`; original RF, Ice Nova and relog checks remain historical.
  Rampager's other mechanics and class admission remain separate work.
  Recent-hit armour defence now composes with physical/chaos mitigation in actual and queued contact regressions.
  Full stat writers, including Rage, own auras and Mageblood, have expiry and encoded-pair acknowledgment regressions.
  Dirty aura/Fortification composition also checks decoded wire order, independent expiry and idle correction.
  Its eighteen original-head controls remain historical; fresh ordered joint controls pass at `c9cd1864`.
  Native cap/order uses one raw40 contact; four-contact timing and forced full-writer races remain automated coverage.
  See [the model](../docs/game/steel-willed-armour.md) for event policies and evidence boundaries.
  Partial armour percentages, other Behemoth nodes, class admission and suppression transfer remain separate.
  Fresh composed default-attack mana, penetration and critical-ignore checks pass at source `1366f48`.
  Verify the remaining supported-gem, modifier and bow combinations natively; retain their automated regressions.
  Unconditional penetration is integrated with base/mod resistance, fractional totals and target damage taken.
  Critical elemental ignore and non-critical penetration now compose with it on the same rolled packet.
  Rerun critical ignore against a rebuilt final DLL: the previous combined run used the preceding #225 artifact.
  Recheck the resolved bow-contact and target-modifier composition natively before claiming new combined proof.
  Special ES mechanics, the remaining conditional penetration, skill Exposure, damaging and alternative ailments on monsters
  (beyond the player's ignites and bleeds) and further added-damage/support effects remain open.
  An attack's packet goes through `MaximumAttackDamage.Apply` after conversion first (Devout of Ryslatha's more maximum
  damage scales attack ailments too), with a regression test.
  Ordinary Frostblink's bounded ground-instant acceptance passed actual Q, retry and invalid-request checks.
  Its hovered stationary-monster 0x191 path now uses the native size-aware object aim and object-bound confirmation.
  Automated acceptance/queued rejection checks and manual native endpoint experiments pass.
  Actual hovered Q reached native/server `(317,220)` with one object confirmation and one 12-mana cost.
  A framed early ground retry was refused without extra cost or confirmation.
  Production framed off-axis/far controls matched native/server endpoints.
  Framed moving-target, partial-terrain, missing-target and noninstant controls were refused without spending.
  Moving targets, coincident random aim and partial object-candidate terrain fallback remain outside this path.
  Production use during an active walk still needs measurement; native manual walk-stop experiments passed.
  Its departure/arrival damage, chilled ground, enemy-dependent recovery and shared Blink groups remain open.
  Extend terrain and movement restrictions from native evidence before accepting other instant movement skills.
- **Start:** special ES mechanics, skill Exposure, the remaining conditional penetration and critical strike stats
  from the stats both sides already carry; monster skills beyond the first granted effect (ranged, spells).
  Extend player stun rules to ES avoidance, substituted thresholds, conditional defences and monster-specific modifiers.
  Spell base critical chance now comes from each skill's level and shares its panel/combat calculation.
  Native skill switching, combined bonuses, no-crit, relog and hitless-spell controls are verified.
  The overcapped-resistance modifiers (Sea Legs' flags: cold overcap as increased evasion, lightning overcap as
  increased critical chance) are implemented through the pools with automated and original-head native checks.
  Recheck the combined armed 830 +45% =1204 result and extreme signed cancellation on the resolved stack natively.
  Recheck Purity plus typed/all-element dev replacements and zero maximum overrides after the aura-source correction.
  The pure base overrides are in as a generic stat consumer (`override_weapon_base_critical_strike_chance` and the
  unarmed base in `AttackBaseCriticalStrike`): Slayer Overwhelm, Blind Prophet's Sightless and Behemoth Final Blow
  have native-verified base calculations, active-set isolation and cannot-crit gates (Status and
  protocol/11-skills.md §critical strikes).
  Recheck the resolved stack's shared half-up midpoint natively; the original head's native checks remain historical.
  Recheck development generic/spell increases and rounding live after the input-consistency review fixes.
  Deliver their nodes through the separate ascendancy lifecycle.
  Generic conditional rolls and the unarmed melee multiplier are implemented and native-verified separately.
  Continue native spell/projectile checks and the resolved critical-hit/Fortification composition on `effb797`.
  Verify successful paid Final Blow refunds once Phrecia play has an agreed client-compatible graph.
  Focus/curse lifecycles and statistical RNG validation remain separate from the deterministic policy tests.
  Ordinary generic/typed added BASE now follows intrinsic-base replacement and precedes increased chance, including
  native weapon-local flat-before-increase ordering and linked Increased Critical Strikes once.
  Mode-specific passive/item, source-removal, zero, prohibition and hitless controls have automated cases;
  combined automated results are in the panel entry.
  Sixteen positive Fireball/ICS selected-field controls on `0aabeb6` pass at actual level 100 in both modes, including
  actor/support removal, relinking and relog, plus separate default-sentinel and current-Actor absence controls.
  The after-capture Standard sentinel correction and its current-identity/historical-numeric boundary are in protocol 11 §5.
  Continue native attack/local-weapon/Assassin, signed/zero/prohibition/hitless and actual critical-hit checks.
  These controls do not establish rendered zero or certify a complete issue75 row.
  Isolated modifiers do not prove acquisition; the local-flat fixture does not certify its no-extra-critical-damage
  consumer, and exact official rounding is unproved.
  Four unconditional MORE/LESS critical sources are now implemented with final cap/rounding and actual support/
  item lifecycle/contact tests. Recheck Controlled Destruction and the unique -40% source in the native client.
  Other conditional more chance, guaranteed-critical effects, off-hand chance/multiplier publication and execution,
  and the attack critical confirmation remain separate work. The off-hand range/timing publication currently
  has no corresponding critical fields; do not infer completion from its other weapon fields.
  Add support effects through `DamagePacket.Add`, `Scale`, `Convert` and `AddGain`.
  When owned-actor execution is implemented, supply actual-caster stats to `SkillDamageContext` explicitly.
  Do not inherit an owner's spell-damage restriction onto independent minions or daemons without a skill rule.
- **Client Buff descriptor consumer follow-up (#370, Known issues K1-K3):** follow one prepared descriptor
  from `ChaosDegenerationAuraPlayersUniqueBodyStr3` to an actual reader and explain its output.
  The [ownership/transfer report](../docs/re/client-buff-descriptor-consumers.md) is the bounded #466 delivery.
  Resume only with new same-object transfer or typed-reader evidence, as a separate batch under #370.
- **Known:** [20](../docs/protocol/20-damage.md) has the damage model; both sides still have flat damage ranges
  ([11](../docs/protocol/11-skills.md) §5, §6). Monster armour is not modelled.
- **Start:**
  - Special ES mechanics, skill Exposure, the remaining conditional penetration, damaging and alternative ailments on
    monsters beyond ignites and bleeds, and further added-damage/support effects.
  - Weapon-local "with this Weapon per 10 <attribute>" and spell per-Intelligence added damage.
  - Monster skills beyond the first granted effect (ranged, spells).
  - Player stun: ES avoidance, substituted thresholds, conditional defences and monster-specific modifiers.
  - Criticals: other conditional more chance, guaranteed-critical effects, off-hand chance/multiplier publication and
    execution, Focus/curse lifecycles. The off-hand range/timing publication has no critical fields: do not infer
    completion from its other weapon fields.
  - Whisperer, Arcane Might, Steel Willed, Rampager and Behemoth's remaining scope is in their docs
    ([Whisperer](../docs/game/whisperer.md), [Arcane Might](../docs/game/arcane-might.md),
    [Steel Willed armour](../docs/game/steel-willed-armour.md), [Behemoth](../docs/game/behemoth.md)).
  - Frostblink: moving targets, use during an active walk, departure/arrival damage, chilled ground, enemy-dependent
    recovery and shared Blink groups. Extend terrain and movement restrictions from native evidence before accepting
    other instant movement skills.
  - Add support effects through `DamagePacket.Add`, `Scale`, `Convert` and `AddGain`. When owned-actor execution is
    implemented, supply the actual caster's stats to `SkillDamageContext`; do not inherit an owner's spell-damage
    restriction onto independent minions without a skill rule.
  - Recheck natively: supported-gem, modifier and bow combinations; critical ignore against a rebuilt DLL; the
    resolved bow-contact and target-modifier composition; overcapped-resistance and critical rounding on the resolved
    stack; Controlled Destruction and the unique -40% critical source; queued ES capacity changes.
- **Done when:** damage numbers match the character panel and the monsters' levels.

  原生上限/顺序使用一次 raw40 接触；四接触时序与强制的全写入者竞态仍只有自动化覆盖。
  事件策略与证据边界见[模型](../docs/game/steel-willed-armour.md)。
  部分护甲百分比、其他 Behemoth 节点、职业准入与抑制转移仍属独立工作。
  新的组合默认攻击魔力、穿透与暴击忽略检查在来源 `1366f48` 通过。
  请原生核对剩余的受支持宝石、修饰量与弓组合；保留其自动化回归。
  无条件穿透已与基底/词缀抗性、小数总量与目标承伤整合。
  暴击元素忽略与非暴击穿透现在在同一个已掷报文上与它组合。
  请对重建后的最终 DLL 重跑暴击忽略：之前那次组合运行用的是上一个 #225 产物。
  在声称新的组合证明之前，请原生复核解析后的弓接触与目标修饰量组合。
  特殊护盾机制、剩余条件穿透、技能暴露、怪物身上的伤害型与替代异常状态（超出玩家施加的点燃与流血），
  以及进一步的附加伤害/辅助效果仍待处理。
  攻击的报文在转换之后先经过 `MaximumAttackDamage.Apply`（Ryslatha 信徒的更多最大伤害也会缩放攻击异常状态），并有回归测试。
  普通 Frostblink 的有界地面瞬发准入通过了实际的 Q、重试与非法请求核对。
  它对悬停静止怪物的 0x191 路径现在使用原生带尺寸的对象瞄准与对象绑定确认。
  自动化准入/排队拒绝检查与人工原生端点实验都通过。
  实际悬停 Q 抵达原生/服务端 `(317,220)`，一次对象确认与一次 12 点魔力消耗。
  被帧封装的过早地面重试被拒绝，没有额外消耗或确认。
  生产环境的偏轴/远距对照与原生/服务端端点一致。
  被帧封装的移动目标、部分地形、缺失目标与非瞬发对照被拒绝且不消耗。
  移动目标、重合随机瞄准与部分对象候选地形回落不在该路径内。
  生效行走期间的生产使用仍需测量；原生人工行走停止实验通过。
  其离开/抵达伤害、冰缓地面、依赖敌人的恢复与共享闪现组仍待处理。
  在接纳其他瞬发位移技能之前，先用原生证据扩展地形与移动限制。
- **入手：** 特殊护盾机制、技能暴露、剩余的条件穿透与双方已携带属性里的暴击属性；超出首个授予效果的怪物技能（远程、法术）。
  把玩家眩晕规则扩展到护盾规避、替代阈值、条件防御与怪物专属修饰量。
  法术基础暴击率现在来自每个技能的等级，并共用其面板/战斗计算。
  原生技能切换、组合加成、无暴击、重登与无命中法术对照已核对。
  超上限抗性修饰量（Sea Legs 的标记：冰霜超上限作为提高闪避、闪电超上限作为提高暴击率）已在池里实现，
  带自动化与原始 head 原生核对。
  请原生复核组合后的已装备 830 +45% =1204 结果与解析栈上的极端带符号抵消。
  请复核光环来源修正之后的 Purity 加带类型/全元素开发替换与零上限覆盖。
  纯基底覆盖作为通用属性消费者已加入（`override_weapon_base_critical_strike_chance` 与 `AttackBaseCriticalStrike` 里的
  徒手基底）：Slayer Overwhelm、Blind Prophet 的 Sightless 与 Behemoth Final Blow 有原生验证的基底计算、
  生效集隔离与不能暴击闸门（状态与 protocol/11-skills.md 的暴击段）。
  请原生复核解析栈共享的半进位中点；原始 head 的原生核对仍属历史。
  在输入一致性评审修正之后，实机复核开发用的通用/法术提高与取整。
  通过独立的升华生命周期交付它们的节点；Slayer 的每个附近敌人暴击倍率另属工作。
  通用条件掷值与徒手近战倍率已实现并单独原生验证。
  请在 `effb797` 上继续原生法术/投射物检查与解析后的暴击/强化组合。
  等 Phrecia 游玩有一套商定的客户端兼容图谱后，验证付费 Final Blow 的成功退点。
  Focus/诅咒生命周期与统计性 RNG 验证与确定性策略测试分开。
  普通通用/带类型附加 BASE 现在跟随内禀基底替换并先于提高的几率，含原生武器本地「固定先于提高」的顺序与
  连携的 Increased Critical Strikes 只算一次。
  按模式的被动/物品、来源移除、零、禁止与无命中对照有自动化用例；合并的自动化结果在面板条目里。
  在 `0aabeb6` 的十六个 Fireball/ICS 正选定字段对照在两种模式下按实际 100 级通过，含角色/辅助移除、重新连接与重登，
  外加独立的默认哨兵与当前 Actor 缺失对照。
  抓取之后的 Standard 哨兵修正及其当前身份/历史数值边界在 protocol 11 §5。
  请继续原生攻击/本地武器/刺客、带符号/零/禁止/无命中与实际暴击命中检查。
  这些对照不能确立渲染出来的零，也不能给某条 issue75 行背书。
  孤立修饰量不能证明获取；本地固定夹具不能为其「无额外暴击伤害」消费者背书，确切的官方取整也未被证明。
  四个无条件 MORE/LESS 暴击来源现在已实现，带最终上限/取整与真实的辅助/物品生命周期/接触测试。
  请在原生客户端复核 Controlled Destruction 与传奇的 -40% 来源。
  其他条件更多几率、必定暴击效果、副手几率/倍率的发布与执行，以及攻击暴击确认仍是独立工作。
  副手的范围/时序发布目前没有对应暴击字段；不要从其其他武器字段推断完成。
  通过 `DamagePacket.Add`、`Scale`、`Convert` 与 `AddGain` 添加辅助效果。
  实现归属角色的执行时，请显式把实际施法者属性提供给 `SkillDamageContext`。
  没有技能规则时，不要把主人的法术伤害限制继承到独立的召唤物或守护者上。
- **完成标准：** 伤害数字与角色面板和怪物等级一致。

### 元素异常状态扩展
- Check the sent `virtual_action_speed_+%` and the frozen pose with the real client.
  Open: the shatter visual (the packet that removes the corpse is not known), unique/boss thresholds, the monster
  variety's own immunities and a native check of the proliferation radius's area scaling
  ([20](../docs/protocol/20-damage.md#outgoing-chill-freeze-and-shock-on-monsters)).
- Dedicated native buff icons and source/threshold/conditional-immunity parity before a general ailment engine;
  hostile spell adapters and alternative-ailment sources ([Gambler](../docs/game/gambler.md)).

- 玩家对怪物的冰缓、冰冻与感电已实现，带 Gateway 覆盖。
  请用真实客户端核对下发的 `virtual_action_speed_+%` 与被冰冻的姿态。
  碎裂的视觉（服务端会结束被击碎怪物的异常状态且不从它扩散任何东西；移除尸体所用的报文未知）、传奇/Boss 阈值与
  怪物变体自身的免疫仍待处理，点燃扩散与其半径范围缩放的实机核对同样待做。
  见 [20](../docs/protocol/20-damage.md#outgoing-chill-freeze-and-shock-on-monsters)。
- 普通受支持的怪物基础接触现在会走点燃/冰缓/冰冻/感电与最终规避。
  在声称有通用异常状态引擎之前，请完成专用的原生增益图标与来源/阈值/条件免疫的一致性。
  敌对法术适配器与替代异常状态来源仍是独立工作；怪物迫击炮射击已在其落点跑接触管线（状态：技能与战斗的
  "Monster projectiles"）。
  见 [Gambler](../docs/game/gambler.md)。

### 狂怒扩展
- Native composition with supported projectiles, area sweeps, equipment sources, notification delays and slow
  destination loading; queued refreshes across expiry and decay-speed modifiers.
  Measure official capped-attempt refresh and area-loading timer parity before replacing policy v1.
  See [26-rage.md](../docs/protocol/26-rage.md).

- 通用狂怒生成、上限、已安装增益/计数、实际伤害、衰减、暂停与生命周期已原生验证。
  剩余的原生组合包括受支持的投射物、范围扫击、连携/生效装备来源、超出共享区间的通知延迟与慢速目的地加载。
  请复核跨到期排队的充能、装备与命令刷新，以及单独的衰减速度修饰量。
  140 伤害/11 强化的组合有自动化已接受接触覆盖；请原生验证。
  在替换策略 v1 之前，请测量官方的受限尝试刷新行为与区域加载计时器一致性。
  见 [26-rage.md](../docs/protocol/26-rage.md)；瓦尔消耗与整职业选择仍是独立机制。

### 瓦尔执行与剩余灵魂核对
- Native acceptance of duplicates, removal/readd, cooldown refresh, held death notifications, Vaal Haste/Grace aura
  costs, death/resurrection and slow destination loading ([27](../docs/protocol/27-vaal-souls.md)).
  Validate the [modular Behemoth candidate](../docs/game/behemoth.md) natively; other Vaal executors and official
  distribution/capacity parity remain open.

- 收集、有界的共享预算与已证明的 Actor 载荷（#308）通过自动化与有界原生核对。
  基线 `939b44fe` 上的来源检查覆盖属性/魔力组合、辅助、付款与当前发布。
  已证明缺失的技能记录第 1 位现在从原始瓦尔元数据实现，带解析与已镶嵌回归。
  四十二个钉死的 3.29.3b 检查覆盖真实击杀充能、上限、消耗变化、零消耗与阻止。
  它们也覆盖非致命/友好对照与不受支持的输入，且不会有动作回显或资源损失。
  剩余原生验收覆盖重复、移除/重新添加、冷却刷新与瞬时生命周期。
  请验证持有的死亡通知、Vaal Haste/Grace 的光环消耗、死亡/复活与慢速目的地加载。
  在背包通知之前包含属性装备移除；当前自动化覆盖不是原生证明。
  明确的 LocalExile 策略见 [27-vaal-souls.md](../docs/protocol/27-vaal-souls.md) 与状态文档。
  Vaal Ground Slam 的付款、共享攻击准入与阻止现在有有界实现。
  请用当前完整套件与原生客户端验证[模块化 Behemoth 候选](../docs/game/behemoth.md)。
  其他执行器与官方分布/容量一致性仍属独立工作；不受支持的施放仍被拒绝。

### 死亡界面变体
- **Known:** S→C 0x55 flags bit0/2/3 are mapped ([11](../docs/protocol/11-skills.md) §7);
  bit4 and the trailing u32 / u8 / u16 are not.
- **Done when:** Sanctum and other modes show their own options.

- **已知：** S→C 0x55 的 flag bit0/2/3 已映射（[11](../docs/protocol/11-skills.md) §7）；bit4 与尾部的 u32 / u8 / u16 尚未。
- **完成标准：** 圣所与其他模式显示各自的选项。
  硬核 Exit（C→S 0x153）已实现；死亡迁移与永久无情死亡跟踪在[赛季模式](../docs/game/league-modes.md)。

### 技能投射物

- **客户端契约研究：** 在扩展模拟器或改变阶段预测归属之前，先看冻结的[交付矩阵](../docs/re/projectiles/delivery-matrix.md)。
  已恢复的计数/布局、随机上下文、运动/接触/历史与选定的延续/技能规则都可用。
  请把有限的 [K1/K2/K3 后续](../docs/re/projectiles/verification-and-limitations.md) 分开：玩法 TLS 状态/顺序、
  官方 Fireball 结果路由与六见证的有效输入/读取更新时序。
  不要把完整渲染、每个属性生产者或完整引擎拆除当作使用已定规则的前置条件。
  研究本身不改变任何服务端行为；下面的生产任务仍在其既有范围内待做。
- **已知：** [框架](../docs/game/projectile-framework.md)拥有生产投射物路由与唯一的 Core 模拟器。
  目录已知的宝石与普通弓、法杖有基本的直线飞行与解析后的修饰量；[已注册技能](../docs/game/projectile-skills.md)
  提供终端的 Fireball 爆炸与独立的 KB 集群。集群把数量转成爆炸并禁止分裂；内部的 AltY 行是兼容别名。
  直接准入、独立爆炸重叠与受支持的范围缩放有自动化运行时/输入覆盖。
  按时间顺序的接触、命中配额、暂停/转移、释放快照与输出独立性有自动化生产输入覆盖，含原始物品修饰量、
  作用域范围伤害与延续/生命周期边界。
  普通法杖的地面/对象攻击共用弓的释放路径、原生主手接触与弹丸的骨架命中动画；
  法杖主手变化跟随每个职业的 ACT 池与原生输入播种的选择器（提取清单版本 2 提供全部七个职业的有序姿态池与精确骨架接触）。
  完整的副手时序/伤害与非零内禀攻击重复仍待处理。
  基础目录飞行不实现所有特殊技能行为；未知元数据在花费之前被拒绝。
  一次移动实例的上限为 150 格（15 米），通过 `projectile_maximum_range_cells`；分叉、连锁与分裂会得到每段的新预算。
  待处理：上限之前的按技能持续时间，以及延续边界的实机核对。
  动作速度不是投射物输入；原生动作速度/疾风契约仍是 #246。
  #75 的精准、共享暴击掷值、穿刺与吸血已整合，带自动化组合覆盖。
  更早的原生报文/碰撞对照需要在新路径上重测；角色位置仍使用整型行走估算与共享投射物半径；
  Gateway 的角色半径为零，使范围覆盖即使对大型怪物也以中心为准。请用可得的移动 `ObjectSize` 数据校准战斗身体几何。
  迫击炮之外的怪物投射物：武器投射物攻击（`monster_projectile_weapon`、`monster_snipe`、`monster_puncture`，
  以及持弓或持杖怪物的多态攻击）会飞一个服务端对象（`SkillCatalog.WeaponProjectile`，见状态），
  其美术来自技能在 ProjectilesArtVariations 的 `default_ranged` 家族里的 `arrow_projectile_variation`。
  待处理：技能既没点名变体也没有箭矢标记的变体（`monster_projectile_variation` 是按技能家族的索引，
  服务端还解析不了：同一个值 12 既是 MassSkeleton 的骨箭也是精华守护者的混沌射击），所以 Knitted Horror 仍射 DefaultArrow；
  MonsterVarieties 的 `Weapon1_ItemVisualIdentityKeys` → ItemVisualIdentity 的 `AOFile2` 是另一条候选链
  （`SkeletonArrow1` 的那个是箭袋模型而不是投射物，所以未采用）；
  480 个既没点名箭也没点名弹丸的远程攻击效果（扔出的石头、幽灵武器）仍瞬发命中；
  而 `MonsterProjectileSpell`（361 个效果，360 个带 `monster_projectile_variation`）是另一个适配器，
  所以施法者的火球仍然看不见。迫击炮的溅射半径是开发默认值，`projectile_spread_radius`（3）未建模。
  迫击炮在其片段的接触关键帧与点位释放（`data/monster-contacts.json`）；近战挥击仍在攻击时间的一半落地，
  尽管同一批池里带 `Melee` 关键帧。待处理：客户端是否用 0xffff 序列播种怪物动作的片段选择
  （喷怪的 attack2 会早 75 ms、向后多 1.3 格吐出）、关键帧横向偏移的网格手性（未应用）、
  MonsterVarieties 的 `MinimumAttackDistance`（喷怪为 6）与最小射程死区，都要实机核对。
  怪物距离是世界单位，一格为 250/23：仇恨与远程技能射程现在已换算（弓手的 105–135 是 9.6–12.4 格，其 58 是 5.3），
  但近战攻击的 6 仍读作 6 格，因为追击的走近会停在玩家前方 `Range - 1` 格，换算后的 0.55 会让怪物挤进玩家身上：
  换算它需要重做那段走近与 `LivingCollision.PlayerSize`，以及随之而来的近战接触射程检查。
- **实机参考**（[移植记录](../docs/dev/ranged-monster-actions-port.md) 的 "Live reference"，2026-10-29/30）：
  GGG 的服务端对玩家的弓箭射击与沙喷怪的迫击炮都不发对象，而客户端把两者都画出来。
  lockstep 客户端会用其记录上的计数画出自己的弓箭射击与法杖弹（[状态](../status/skills-and-combat.md)）；
  在那边丢掉服务端箭矢对象之前，先探测预测式客户端是否也用它画（一颗预测式 Burning Arrow 配计数没有画出任何东西）。
  对怪物，客户端会画 `monster_mortar_attack` 的迫击炮（[状态](../status/skills-and-combat.md)）；
  测量它落在哪里与服务端命中的关系（客户端用自己的 `projectile_spread_radius` 播种散布），
  并追踪法术形式的动作（`monster_mortar`，275 行 GrantedEffects，以及 `monster_mortar_around_caster`），
  在丢掉它们的服务端对象之前先探测一个。
  解释命中时序拟合（我们在表的 299.9 格/秒释放，接近实机的 308，但命中按每格 2.72 ms 抵达而实机是 3.25），
  并在命中上加上 300 ms 眩晕位。
  找出为什么 1 级沙喷怪在这里打 5 到 7，而实机是固定 4。
  解码 S→C `0x1b4`（仅玩家，攻击的技能 id 与一个点列表）。
- **入手：** 按技能指南由用户跑的 Fireball/KB 核对，含返回、地形与重叠爆炸。
  用实机客户端核对瞄准高度模型（低于崖边的射击停在崖面，瞄在顶上的能越过去），
  然后找出客户端自己对预测宝石投射物的投射物高度/地形规则，替换那 12 步净空；
  服务端 `DefaultArrow` 停在地形上的撞墙音效还没下发。
  用客户端证据校准集群径向分布、逆时针序列时序与原生美术。
  为平台与装饰件建服务端地表高度/世界模型（#182）、更多区域的高度网格（或放进行走地图录制器），
  以及与自带碰撞体的装饰件和物件做碰撞。
- **完成标准：** Fireball 与箭矢在每个区域都像真实的那样：在地形与平台上位于施法者躯干处，遇墙与物件停下。
- **Client-contract research:** use the frozen [delivery matrix](../docs/re/projectiles/delivery-matrix.md)
  before extending the simulator or changing phase prediction ownership.
  Keep the [K1/K2/K3 followups](../docs/re/projectiles/verification-and-limitations.md) separate.
  Do not make full rendering, every stat producer or complete engine teardown prerequisites for using closed rules.
- **Known:** the [framework](../docs/game/projectile-framework.md) owns production routing and the only simulator;
  [registered skills](../docs/game/projectile-skills.md) add special behaviour.
- **Open:**
  - Off-hand timing/damage and nonzero intrinsic attack repeats; per-skill duration before the 150-cell cap and a
    native check of the continuation boundary; the action-speed/Tailwind contract (#246).
  - Combat body geometry: actor positions use integer walk estimates and Gateway actor radii are zero, so area
    coverage is centre-based; calibrate against `ObjectSize`.
  - Monster weapon projectiles: `monster_projectile_weapon`, `monster_snipe` and `monster_puncture` (109 varieties'
    first effect) take their projectile from the monster's weapon art (MonsterVarieties
    `Weapon1_ItemVisualIdentityKeys`, e.g. `SkeletonArrow1`), which the server does not read, so they hit instantly
    with no shot. Monster spell projectiles (`MonsterProjectileSpell`) need their own adapter.
  - Mortars: the splash radius is a development default and `projectile_spread_radius` (3) is not modelled; monster
    melee still lands at half the attack time although the pools carry `Melee` keyframes. Check live whether the
    client seeds a monster's clip choice from the 0xffff sequence (the Spitter's attack2 would spit 75 ms earlier and
    1.3 cells farther back), the handedness of a keyframe's lateral offset, `MinimumAttackDistance` and the dead zone.
- **Live reference** ([11](../docs/protocol/11-skills.md#17-live-reference-ranged-attacks-on-gggs-servers)):
  probe whether a predictive client draws its own shot from the record's count before dropping its server arrows;
  measure where the client's mortar lands against the server's hit, and trace and probe the spell mortars
  (`monster_mortar`, `monster_mortar_around_caster`) before dropping their server objects.
  Explain the hit-timing fit (the arrow releases at the table's 299.9 cells/s, close to live's 308, yet hits arrive
  2.72 ms per cell against live's 3.25), add the 300 ms stun bit on hits, find why a level 1 Sand Spitter hits for 5–7
  here and a constant 4 on live, and decode S→C `0x1b4` (player only, the attack's skill id and a point list).
- **Start:** user-run Fireball/KB checks from the skill guide, including return, terrain and overlapping explosions.
  Check the aimed-height model live, then find the client's own projectile height/terrain rule for its predicted gem
  projectiles and replace the 12-step clearance; send the wall-hit sound of a server `DefaultArrow` at terrain.
  Calibrate cluster radial distribution, sequence timing and native art against client evidence.
  Build a surface-height/world model for platforms and doodads (#182), height grids for more areas, and collision with
  doodads and objects that have their own colliders.
- **Done when:** Fireball and bow arrows look like the real ones in every area: at the caster's torso on terrain and
  platforms, stopping at walls and objects.
