# 状态：怪物

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/monsters.md)。

## 已实现（已用真实客户端验证）

- 怪物属于区域副本：重新进入时，活着的怪物留在原地并带着它们的伤害，死了的保持死亡；新副本则是全新的怪群。
  代码：`InstanceStore.cs`、`PopulateArea`、Gateway。
  文档：[14](../docs/protocol/14-monster-packs.md) §2。
- 怪物：`/spawn_mob <行号、Id 或名字>[@等级] [friendly] [rarity] [名字]`、`/mobs`、`/despawn`、`/mob_walk`、
  `/ai`：10 691 个 MonsterVarieties 中的任意 10 345 个，可指定等级与稀有度、生命按等级、敌对（红色）或友好，
  靠近时会追击玩家（攻击指向玩家，0x1ab，客户端寻路）。
  代码：`MonsterObject`、`MonsterCatalog.cs`、Gateway。
  文档：[10](../docs/protocol/10-chat-and-commands.md) §5。
- 区域里的怪物群：进入时按区域等级用其 MonsterPacks 填充（海滩：螃蟹与溺亡者怪群，有些是魔法怪，至少两个带稀有；
  篝火旁的食人族怪群），并在地图的 `mapboss` 点生成专属 Boss 与其 Boss 怪群（篝火旁的 Fire Fury，2026-10-18 实机核对）；
  带种子的怪群方案会跨服务端重启保持一致。
  房间点通过把房间拟合到 `-Walkmaps` 抓取的装饰件上来求得。
  潮汐岛的船屋房间太稀疏无法拟合，改用其抓取到的摆放：Hailrake 站在残骸上。
  有 Boss 点的房间采用其实际摆放（抓取或生成）；未摆放的同级房间则不额外生成。
  `mapboss<n>[WG]` 是区域内第 n 个 Boss，含子图房间（Brutus、Merveil）。
  `mapboss`、`force:mapboss`（Kitava）与 `mapbosspack` 会取走剩下所有 Boss，每个一次（兽穴的两个、两个封印）。
  具名怪群会带来它们的 Boss（Pikerivet、The Burning Man）；码头的 Tolman 站在他的刷新点。
  灰烬之地的 Boss 是角色在第二幕放过的那个盗贼领主，或 Greust（`MapBossOverride`）。
  若 Boss 的点在行走地图之外，它会站在最近的可走格子上；Boss 怪群每个区域只生成一次。
  `CampaignBossPlacementTests`（`Game_BOSS_CHECK=1`）会检查 Gateway 生成的第一到第十幕区域里的每个 Boss。
  代码：`MonsterPacks.cs`、Gateway（`PopulateArea`）、`scripts/walkmaps`、`scripts/re/atlas room-points`。
  文档：[14](../docs/protocol/14-monster-packs.md)。
- 传奇地图 Boss 会应用地图表的生命与伤害加成：`MonsterMapBossDifficulty` 加 `MonsterMapDifficulty`，以百分比相加
  （83 级：生命 ×20.73、伤害 ×1.15）。所以迷雾之王的类人形态在 83 级有 45,614,731 生命，与 poedb 显示一致。
  代码：`MonsterCatalog.MapBossScaling`、`SpawnMonster`。
  文档：[king-in-the-mists](../docs/game/king-in-the-mists.md)。
- 破土怪物（Gravel Eaters、Dripping Dead、暮光海滩的溺亡者……）以埋地状态创建，埋着时无法被击中，玩家进入 24 格内时升起：
  客户端播放其出土片段（Emerge 动作 0x0101，然后是 Transitionable 2），随后它们像其他怪物一样战斗、死亡并给经验。
  客户端在等待期就显示为可命中的那些（Tide Striders、血元素……）可以被击中，并在被击中时升起；传奇 Boss 保持自己的入场状态。
  它们的怪群会被重新摆放（海滩的 `Act1-SandspitterEmerge`、泥滩的 `Act1-ZombieMiredEmerge`）；
  副本里埋着的保持埋着、升起的保持升起。
  2026-10-25 实机核对（`/spawn_mob` Gravel Eater、海滩与泥滩与暮光海滩上的怪群，以及潮汐岛上潜伏的 Tide Striders）。
  代码：`MonsterEmerge.cs`、`Program.Emerge.cs`、Gateway。
  文档：[10](../docs/protocol/10-chat-and-commands.md) §5。
- 怪物受击反应：一次命中后怪物仍活着、且通过 wiki 的眩晕掷骰时，会请求原生的 Actor TakeHit 恢复
  （S→C 0x1d1，动画类型 5）。
  眩晕免疫、Boss 的 4 秒免疫、缺失 TakeHit 配置或已存在的恢复都会阻止它。
  致命一击不会有受击后仰。
  [几率与持续时间](../docs/game/juggernaut.md#stun)遵循 wiki 与 `Character.ot`，而不是片段长度。
  角色自身的原生 ACT 会选取并适配呈现的片段；服务端不强制骨架索引。
  恢复会停止服务端与客户端的移动、暂停 AI，并使旧的普通挥击回显与接触失效。
  历史上的 LocalExile 移动溺亡者样本在恢复期间保持在一格内，然后重新开始一次新的 AI 行走。
  这个独立移植有新的自动化覆盖；原生集成仍待处理，见
  [移植记录](../docs/dev/ranged-monster-actions-port.md)。
  仅骨架的 0x1b2 不会停止移动；它已不再用作敌人的恢复报文。
  武器命中音效与目标的血液来自 S→C 0x1af 的「action hit」，在近战与攻击宝石命中时下发，也在怪物命中玩家时下发
  （2026-10-25；加入时序上限之后尚未实机核对）。
  GGG 的服务端也会发它，但带的是怪物动作自己的序列与技能索引 2，而我们发的是 0xffff 与 0
  （沙喷怪，2026-10-30 实机抓取；[移植记录](../docs/dev/ranged-monster-actions-port.md) 的 "Live reference"）。
  这里 1 级沙喷怪打 5 到 7，实机是固定 4（暴击 5）；原因不明。
  代码：`HitReaction.cs`、`MonsterCatalog.TakeHit`、Gateway（`SendMonsterFlinch`）、`scripts/re/monster_takehit.py` →
  `data/monster-takehit.json`。
  文档：[11](../docs/protocol/11-skills.md)。
- 魔法与稀有怪物从 Mods.dat 掷词缀（怪物域、按怪物标签的生成权重、等级、每个家族一条）：魔法怪一条词缀，
  在同一副本里同名怪物间共享；稀有怪两到四条（poewiki 的「Rarity」），稀有名字取自 Words.dat 的怪物词表。
  魔法怪发蓝光、稀有怪发金光：服务端给它们挂上隐藏的冠军增益（`MonsterRarityBuffs`，BuffDefinitions 66 / 67；
  [14](../docs/protocol/14-monster-packs.md) §2）。
  客户端在悬停时显示（2026-10-25 实机核对：「Gorewalker」/「Extra Life」、「Quick Hungry Corpse」/「Hasted」）。
  它们的生命、命中、攻击时间、行走（以 movement_velocity_+% 下发，客户端走同样速度）、抗性、物理减免、格挡、闪避、
  暴击与回复都在服务端生效（逐项实机核对）；副本会保留它们。
  只有服务端能兑现的 37 个词缀会被掷出；其余 74 个列在 [14](../docs/protocol/14-monster-packs.md) §5。
  代码：`MonsterMods.cs`、`Program.MonsterMods.cs`、`MagicProperties.cs`。
  文档：[14](../docs/protocol/14-monster-packs.md) §5。
- 怪物决策使用 50 ms 的已过去游戏时间，暂停或慢步之后不会补跑。
  近战追击每 200 ms 重复一次（[07](../docs/protocol/07-latency-visibility-movement.md) §5），沿其走近路径分成 12 格的段，
  当玩家超出其射程一段以上时（攻击从该射程内指向玩家），而移动到包围位的 Move 也同样截到前 12 格：
  这样每道指令的搜索都能装进客户端一帧的路径预算。离开唤醒区（171 格）的怪物在服务端就像在客户端一样站着，
  直到回来才有指令（[07](../docs/protocol/07-latency-visibility-movement.md) §5）。Move 追击有独立的 100 ms 预算，
  且要求曼哈顿目标变化至少三格，除非一段行走仍在进行中，此时每 200 ms 会对同一格再下一次指令
  （被尸体挡住的 lockstep 客户端怪物会重新动起来）；在 lockstep 客户端上，行走中怪物的重新指令会等它估算迈步之后的那一帧开始，
  最多晚两步（[07](../docs/protocol/07-latency-visibility-movement.md) §6）。
  远程追击者站在其射程内一格处，射程取 MonsterVarieties 的 `MaximumAttackDistance` 与 `MinAgroRange`/`MaxAgroRange`
  按格读之后较大者的一半——与发现范围同一条规则：瓦尔弓手 max(58, 135) / 2 ≈ 67 格，沙喷怪 15 格——只有那个环满了才往里挪，
  而不是占用近战接触格（`MonsterSurround.Asker.Ranged`；2026-10-29 实机：喷怪曾贴上来）。若按一格的 250/23 世界单位换算，
  那个弓手只有 12.4 格，会站着看六格外的玩家；用户的决定（2026-11-08 实机）是用减半的行值，所以远程怪物看到多远就射多远。
  近战攻击仍把行里的数值当作格数（6）：追击的走近会停在玩家前方 `Range - 1` 格，而换算后的 0.55 会让怪物挤进玩家身上（路线图）。
  放弃追击发生在超出该变体自身仇恨行（按格读）——`SpawnedMonster.ChaseRange`，按游戏数据原值读（瓦尔弓手 105–135），
  而不是它发现玩家所用的 60–100 格——此时会在其 Move 之前用 S→C 0x1ac 结束客户端的追击动作
  （GGG 的服务端也这样：3.29 抓包里 1166 次实机放弃中有 925 次带了它；preemptive 客户端需要它，lockstep 客户端会忽略
  ——[07](../docs/protocol/07-latency-visibility-movement.md) §11）。当初用仇恨范围的两倍，在仇恨换算成格之后就变成 24 格就放弃，
  于是怪物不再跟随一个只是走开的玩家（2026-11-08 实机）。发现范围本身是用户的决定：行值的四分之三，钳制在 60–100 格
  （`Math.Clamp(variety.AggroRange * 0.75, 60, 100)`）。
  攻击占用期覆盖完整攻击时长；待处理的挥击与阶段代次可避免重复工作。
  三次原生的 AI 启用对照分别在 43、38 与 50 ms 后各发出一道追击指令。
  四次故意转向在 249 ms 间隔内产生了四道追击指令，转向之间没有重复指令。
  溺亡者的攻击起始间隔保持 2432 与 2428 ms，对应 2.4 秒的攻击。
  真正的 Esc 菜单暂停在一格上保持了 30 个采样且无动作；恢复后沿原路径继续。
  自动化节奏、暂停、打断与阶段完成检查通过；引用的原生对照是历史上的 LocalExile 证据，不是这个独立移植的新一次运行。
  代码：`MonsterAiCadence.cs`、`Program.Monsters.cs`。
  证据：`artifacts/wand-ai-validation/ai-native-response-*.json`、`ai-native-turns.json`、
  `ai-native-attack-pacing.json` 与 `ai-native-menu-{paused,resumed}.json`。
  文档：[11](../docs/protocol/11-skills.md) §6。
- 怪物会还手：以其首个授予效果作为非寻路技能，使用 MonsterVarieties 的攻击时间、射程与等级伤害；进入射程时会挥击（有动画），
  玩家的生命在闪避、护甲与格挡之后下降；只有数据或词缀给出暴击率的怪物才会暴击（目前都不是来自其数据；致命性除外）；
  走开可以躲过；死亡会播放并阻止移动；死亡界面（S→C 0x55）显示「Resurrect in Town」/「Resurrect at Checkpoint」
  （硬核：「Exit」），两个按钮都可用（C→S 0x54；在城镇里：最后一个城镇）；`/heal`。
  代码：`MonsterAttack`、`Resurrect`、Gateway。
  文档：[11](../docs/protocol/11-skills.md) §6–7。
- Dominus（上神之权杖）：两个形态都能打到结束且客户端不崩溃（2026-11-05 实机）。
  他挥出 `DominusLightningSlam`（羁绊、标记与灵魂效果被跳过）；远处的 Boss 以睡眠状态创建，玩家靠近时唤醒。
  代码：`MavenNucleus.CombatEffect`、`SpawnMonster`。
  文档：[11](../docs/protocol/11-skills.md) §6。

## 部分实现

- **每击伤害按「同等级共享 DPS」折算（2026-11-09，按官方修正）。** 官方 Wiki「Monster damage」：同等级怪物共享
  一个 DPS，慢攻怪每击更高、快攻怪每击更低。`MonsterCatalog.BaseDamage` 因此接受动作自身的耗时：
  法术传 `GrantedEffects.CastTime`，物理攻击传 `MonsterVarieties.AttackSpeed`。原来一律用品种攻击间隔，
  让施法 1.0 秒的 Shrieker（品种 1.5 秒）打出 1.5 倍 DPS、投火灯笼手（1.0 对 1.395）1.4 倍；
  现在 34 级两者分别为 103 / 100 每击，与同级物理怪一致。Boss 脚本（BrineKing 的砸地、VaalOversoul 的砸地）
  同样按各自动作时间传入。
- **单次命中的上限是同级 Damage 的 4 倍（2026-11-08）。** `MonsterCatalog.BaseDamage` 把一次命中封顶在
  `HitCapFactor`（4）× 该等级的 `DefaultMonsterStats.Damage`，因为表里的 `DamageMultiplier` 与攻击时间属于游戏的技能行，
  那些行带有一个本服务端未建模的伤害效率。有两个变体需要它：瓦尔超灵（4.005 秒时 1595%，23 级 1913，
  而早期角色约 650 生命）与 Kitava（`Kitava1` 与 `KitavaFinal`，6 秒时 200%：45 级 1345、67 级 4262，
  而该等级自身的 Damage 已经是 361）。上限在传奇稀有度自身的 ×1.139 之前应用，所以被封顶的 Kitava 一击落在 1440
  （带稀有度 1640），对 67 级角色约 3200 的生命——刚过半，整个 6 秒挥击都是预警。
  其他剧情 Boss 都在 4 倍以下并保留其伤害：Dominus 2.9×、Malachai 4.0×、Avarius 4.4×、盐水之王 2.4×、Shakari 3.0×、
  Garukhan 1.7×、Vilenta 2.0×；Arakaali 的 4.8× 与 Piety 的野兽 5.8× 会被压到上限。法术命中（`SkillCatalog.HitDamage`）
  读技能自身的 `spell_minimum/maximum_base_{type}_damage`，不做封顶，因为那是客户端的真实数值。
  尚未用真实客户端核对。
- Doedre: cauldron and effluent modes; AI tuning provisional ([Act 8](../docs/game/campaign/act8.md)).

- Vaal Oversoul: campaign encounter; AI timing uncalibrated ([Act 2](../docs/game/campaign/act2.md)).
- The King in the Mists' Crux of Nothingness encounter: `An Audience With The King` alone opens the area in the map
  device; the humanoid form fights with the default attack, spell projectile, Vine Slam and Vine Cascade, spawns the three
  Mystic Fetishes as visible shrines, teleports next to them and channels its `0x400e` ritual into one (the fetish is
  immune through the whole ritual animation and becomes attackable at `call_end_01`; the King is immune throughout;
  live 2026-11-07: three rituals, no crash), then returns to the centre (Portent Vapors stacks), summons Plagued Rats
  and an effigy. The fetish/shrine StateMachine `active` states are not sent yet; the central `RitualPillar2` shrine
  is created but its `active_pillars` animation is not advanced.
  Fetish rituals are life-gated at 92/73/55% (one ritual per hit, immunity for the channel); the humanoid is
  phase-locked at 50%, slams its staff and teleports the player into the same-area hedge maze (wisps, exit portal and
  Branching Affliction). On the return the husk is set to the official 25% life and becomes the treant
  (`VoodooKingBoss3`, full 25.2M life) with a 7 s emergence grace, Treant Slam, Bug Bomb, Bugrend, effigy summons and a
  walk that follows a kiting player. The kill
  rolls the guaranteed unique (with That Which Was Taken paired with The Light of Meaning), Hextoad/Hexpass and The
  Endless Darkness.
  The 2026-10-31 official 3.29.3b capture (docs, "Official 3.29.3b capture") showed the real fight differs. The
  fetish ritual is the client's `GSVoodooKingRitual` `0x400e` channel with server-side hazards (its crash was the
  record's missing `skill_behaviour_variation`, [king-in-the-mists](../docs/game/king-in-the-mists.md)). The fetishes are hostile and destructible (no experience or loot); the
  central shrine object exists but its `active_pillars` animation is not advanced yet; the maze uses a room-graph route
  with real `GuidingLightDaemonMaze_` actors plus a werewolf and spider; the treant uses Meditation/Dance/Punishment;
  BugRend is the official `VoodooKingBugRend`/`BugRendSingle` + `BugSoulrend` with six projectiles; the emergence grace
  is 7 s and the morph waits for the full clip. Teleport and return actions move the object at departure and end before
  the next action, so the model is not left in a teleport/channel pose. The official bark pools are wired to their skills
  (Vine Cascade/Thorngrip, Vine Slam/Sprout-Invasive growth-Grow, effigy/Agrik armach-Dark effigy, the two hexes, the
  ritual line at `call_start_01`, the maze lines); the dash/strafe settle now uses a 0.1 s `0x1ba` with the
  follow-terrain marker instead of 5000 s, which the client had silently ignored (the cause of the 2026-11-01 position
  drift). The dash clips move 24 cells (not 19) and the strafe direction now follows the clip's own convention
  (`move_left` = `(dy, -dx)`, `move_right` = `(-dy, dx)` relative to the player-to-King vector), with a `0x1b9`
  facing the player before each clip so a leftover cast facing cannot skew it, and both the facing and the landing
  cell are computed at the send gate from the player cell the action carries (a fast player dash during the 120 ms
  action-end settle otherwise made the clip and settle disagree). The spell projectile's cast step (~12 cells toward
  the target) is adopted on the server too, so the next dash starts from the model's cell instead of 12+ cells behind.
  The dash/strafe action now holds 0.65 s before its settle (the client starts the 0.5 s clip late), and the previous
  action ends 220 ms earlier, so the clip is not cut at a fraction of its move. An approach blink jump is capped at 24
  cells instead of teleporting 34+ cells from a 50-cell gap.
  The `0x2909` step
  glides both sides with `0x1ba` instead of teleporting only the server. The blink-away now fires after two dashes
  instead of waiting behind a ready strafe, and the spell projectile is checked before movement, so the King keeps
  casting between the escapes. A re-entered kept instance puts the phase-1 humanoid back on the mapboss point before
  the intro (it used to start on its saved cell, e.g. 45 cells south of the centre). The generic monster collision
  estimate no longer moves either King form: it changed only the server cell without a packet, so a strafe settle near
  a melee player was reverted and the model snapped back to the ramp start. Some backward dashes still settle with
  20-41-cell corrections when the client cuts its move clip; the measured table is in PR #425.
  Not yet: blood/caustic ground objects, the Ritual imprisonment/maim movement rule, the phase-trigger confirmation,
  ground hazards, monster energy shield, Shadow Surge/Mystic Miasma damage over time, corpse items, the Unlight
  Altar/Bloodline and the remaining native animation captures.
  Code: `Modules/Core/Game/Monster/KingInMists.cs`, `ItemDrops.KingInMists`, `MapDevice`.
  Docs: [king-in-the-mists](../docs/game/king-in-the-mists.md).
- Inherited Life corpse eligibility and sinking flags flow from object-types manifest v4 through `MonsterCatalog`
  into ordinary monster creation.
  The combined object-types format retains the stationary collision footprints and models alongside Life data.
  Explicit false, native defaults, missing inheritance and the Player veto have focused extraction and Core tests.
  The ordinary corpse path requires zero Life; a life-1 boss stage remains alive, with sinking handled independently.
  Six integrated native cases at source `dbfe717`, recorded by `53edc77`, cover ordinary alive/dead, Raised Zombie
  false, Hillock life 1, sinking and an owned Spectral Wolf forced false by that source's separate summon runtime.
  This branch preserves the ordinary flag math and fields; it has no independent native pass or owned-actor runtime.
  The Core helper retains an optional owned guard; corpse identity and final-death registration remain open.
  Deathrattle's warcry explosion is the first consumer: a dead zero-Life eligible monster explodes once and is marked
  `SpawnedMonster.CorpseConsumed` and removed ([11](../docs/protocol/11-skills.md) section 10i).
  Code: `MonsterLifeData.cs`, `MonsterObject`, `MonsterCatalog.cs`, `Program.Monsters.cs`, `ot_components.py`.
  Docs: [04](../docs/protocol/04-player-entity.md#44-life-0x141e72890-55-bytes).
- Player hits on monsters: each one records the time on the monster (`PlayerHitAt`), and the player's death clears it.
  `enemy_life_regeneration_rate_+%_for_4_seconds_on_hit` (Devout of Ryslatha) scales a monster's regeneration for 4 s
  after a hit.
  Gateway coverage only; not checked with the real client.
  Code: `Program.MonsterMods.cs`, `Program.Combat.cs`.
  Docs: [14](../docs/protocol/14-monster-packs.md) §5, [Polytheist](../docs/game/polytheist.md).
- Monster hits on the player: an attack that is neither evaded nor blocked records the time on the monster
  (`HitPlayerAt`), and the player's death clears it.
  For 4 s after it, the player's `enemies_that_hit_you_with_attack_recently_attack_speed_+%` (Devout of Gruthkul)
  lengthens the monster's swings, and its attack stats are sent again on its Stats component.
  Gateway coverage only; whether the client's swing animation follows those stats is not checked with the real client.
  Code: `Program.Monsters.cs` (`MonsterSwingSeconds`, `SendAttackerSlows`), `MonsterMods.cs`.
  Docs: [11](../docs/protocol/11-skills.md) §6, [Polytheist](../docs/game/polytheist.md).
- Monster walk speed on the server includes the client's per-monster speed roll: Actor setup draws a uniform percent
  in the MonsterVarieties row's range (the Goatmen ±5 %, the 7.60–8.40 band measured live for a base of 8; most rows
  none) from a TinyMT32 state keyed by the object id and its type, and the walk estimate, swing range and contact
  checks use that speed; equal bounds retain their fixed percent rather than defaulting to zero.
  The range, the generator and the key order come from the decompile and pass the tests; whether the key's type word
  is the 0x2bb type hash, and so whether the exact value matches the client's, is not checked live yet
  (`scripts/re/frida/trace-movement.js` prints each monster's seed words, range and multiplier).
  Code: `MonsterSpeedRoll.cs`, `AnimationVariantSelector.cs`, `MonsterCatalog.cs`, Gateway (`SpawnMonster`).
  Docs: [07](../docs/protocol/07-latency-visibility-movement.md) §5 ("Monsters on the client").
- Monster mods' accuracy (of the Marksman, of Precision): the monster's accuracy is raised in its swing, but only the
  tests check it; live, the player was never evaded.
  Code: `MonsterMods.cs`.
  Docs: [14](../docs/protocol/14-monster-packs.md) §5.
- Monster summoning skills: a monster whose skill brings monsters (the stat set's `alternate_minion`, a
  SummonedSpecificMonsters row; the Labyrinth's Immolator) uses it within its aggro range, at the skill's cast time and
  cooldown, while fewer than five of its monsters live (our cap); at the action's contact its
  `number_of_monsters_to_summon` appear `summon_specific_monsters_in_front_offset` ahead of it, towards the player,
  hostile, giving no experience or items when the skill says so, and not kept in the instance; the summoner plays its
  actor's `SpellAreaOfEffect` clip (0x1d1), as the skill's own action plays nothing on the client.
  Built for the Crematorium trial: its Immolators' skeletons checked live (2026-11-01), their opening being checked.
  Code: `SkillCatalog.MonsterSummon`, `Program.Monsters.cs` (`MonsterSummon`), Gateway.
  Docs: [labyrinth-trials](../docs/game/labyrinth-trials.md) ("Monsters").
- Monster flee: a player hit with a chance to cause monsters to flee (Surfcaster's small nodes, Chance to Flee Support)
  makes a living monster walk to a floor cell 5 m away from the player and not attack until that walk ends.
  Magic monsters avoid half of the flees and rare and unique ones all, through their rarity's own mods.
  Only the Gateway tests check it; the distance and the action the real server walks it with are not verified.
  Code: `MonsterFlee.cs`, `MonsterAilments.cs`, `Program.Monsters.cs`.
  Docs: [11](../docs/protocol/11-skills.md) §6, [Surfcaster](../docs/game/surfcaster.md).

- **Spell 类型的怪物技能发施法统计而不是攻击统计（2026-11-08）。** `MonsterAttack.CastTime` 取该效果 stat set 的
  `casting_spell`，没有的话看其 ActiveSkillType 是否含 Spell。瓦尔超灵的 IncaBossRocks 是它首个授予效果、也是它一直在用的那一个，
  并不带 casting_spell，于是技能只发了 `main_hand_attack_duration_ms`；客户端的施法时长路径把缺失的虚拟施法时间读成 0，
  Boss 便站着承受每一个 0x1ab 而没有任何动画。`Spell`（伤害管线里不可闪避的命中）仍按 stat set 判定。
  尚未实机核对。这只 Boss 其余的技能顺序也未建模：它从不用另外七个授予效果（喷吐、位移、爆炸、召唤、激光、两个触发法术），
  形态（骨、肉、爪：ACT 的 `ChangeToStance2/3`）也从不变化，这些记在路线图上。
- 迷雾之王的虚无之核遭遇：仅靠 `An Audience With The King` 就能在地图装置里打开该区域；类人形态用普通攻击、法术投射物、
  Vine Slam 与 Vine Cascade 作战，把三个 Mystic Fetishes 生成为可见的神龛，传送到它们旁边并把其 `0x400e` 仪式引导进其中一个
  （该雕像在整个仪式动画期间免疫，在 `call_end_01` 起可被攻击；国王全程免疫；2026-11-07 实机：三次仪式，无崩溃），
  然后回到中心（Portent Vapors 层数），召唤 Plagued Rats 与一个雕像。雕像/神龛 StateMachine 的 `active` 状态尚未下发；
  中央的 `RitualPillar2` 神龛已创建，但其 `active_pillars` 动画没有推进。
  雕像仪式按生命闸在 92/73/55% 触发（一次命中一个仪式，引导期间免疫）；类人形态在 50% 锁血，砸下法杖并把玩家传送到同区域的
  树篱迷宫里（鬼火、出口传送门与 Branching Affliction）。回来时外壳被设为官方的 25% 生命并变成树人
  （`VoodooKingBoss3`，满生命 25.2M），有 7 秒出土宽限、Treant Slam、Bug Bomb、Bugrend、雕像召唤，
  以及会跟随风筝玩家的行走。击杀会掷出必掉的传奇（其中 That Which Was Taken 与 The Light of Meaning 配对）、
  Hextoad/Hexpass 与 The Endless Darkness。
  2026-10-31 的官方 3.29.3b 抓包（文档里的 "Official 3.29.3b capture"）显示真实战斗与之不同。
  雕像仪式是客户端的 `GSVoodooKingRitual` `0x400e` 引导，危险由服务端负责（它曾经的崩溃是记录里缺
  `skill_behaviour_variation`，见 [king-in-the-mists](../docs/game/king-in-the-mists.md)）。雕像敌对且可被摧毁（不给经验与掉落）；
  中央神龛物件存在但其 `active_pillars` 动画尚未推进；迷宫使用房间图路线，含真实的 `GuidingLightDaemonMaze_` 角色
  外加一只狼人与蜘蛛；树人使用 Meditation/Dance/Punishment；BugRend 是官方的 `VoodooKingBugRend`/`BugRendSingle`
  加 `BugSoulrend`，六发投射物；出土宽限 7 秒，变身前等完整片段。传送与返回动作在离开时移动对象并在下一个动作前结束，
  所以模型不会僵在传送/引导姿势。官方的语音池已接到各自技能（Vine Cascade/Thorngrip、Vine Slam/Sprout-Invasive growth-Grow、
  effigy/Agrik armach-Dark effigy、两条诅咒、`call_start_01` 处的仪式台词、迷宫台词）；冲刺/横移的收尾现在改用 0.1 秒的 `0x1ba`
  带跟随地形标记，而不是 5000 秒——那曾被客户端静默忽略（2026-11-01 位置漂移的原因）。冲刺片段移动 24 格（不是 19），
  横移方向现在遵循片段自身的约定（相对玩家到国王的向量：`move_left` = `(dy, -dx)`、`move_right` = `(-dy, dx)`），
  并在每个片段前用一个 `0x1b9` 朝向玩家，避免残留的施法朝向把它带偏；朝向与落点都在发送闸门处由动作携带的玩家格计算
  （否则 120 ms 动作收尾期间玩家的快速冲刺会让片段与收尾不一致）。法术投射物的施法步（朝目标约 12 格）也在服务端采纳，
  于是下一次冲刺从模型所在格开始，而不是落后 12 格以上。
  冲刺/横移动作现在会在收尾前保持 0.65 秒（客户端把 0.5 秒片段起得晚），且前一个动作提前 220 ms 结束，
  这样片段不会在位移的一小段上被切断。接近用的闪烁跳跃封顶 24 格，而不是从 50 格间隙传 34 格以上。
  `0x2909` 那一步现在用 `0x1ba` 让两侧一起滑行，而不是只把服务端传送过去。闪离现在在两次冲刺后就触发，
  而不是排在就绪的横移之后，且法术投射物在移动之前检查，所以国王在两次逃脱之间仍会持续施法。
  重新进入保留下来的副本时，第一阶段类人形态会先回到 mapboss 点再演出开场（过去会从它保存的格子开始，例如中心以南 45 格）。
  通用怪物碰撞估算不再移动国王的任一形态：它只改服务端格子而不发报文，所以近战玩家旁边的横移收尾会被回退、
  模型弹回坡道起点。客户端截断移动片段时，一些向后冲刺收尾仍有 20 到 41 格的修正；测量表在 PR #425。
  尚未做：血液/腐蚀地面物件、仪式的囚禁/致残移动规则、阶段触发的确认、地面危险、怪物能量护盾、
  Shadow Surge/Mystic Miasma 的持续伤害、尸体物品、Unlight Altar/Bloodline，以及其余原生动画抓取。
  代码：`Modules/Core/Game/Monster/KingInMists.cs`、`ItemDrops.KingInMists`、`MapDevice`。
  文档：[king-in-the-mists](../docs/game/king-in-the-mists.md)。
- 继承自 Life 的尸体资格与下沉标记，从 object-types 清单 v4 经 `MonsterCatalog` 流入普通怪物创建。
  合并后的 object-types 格式在保留 Life 数据的同时保留静止碰撞足迹与模型。
  显式 false、原生默认值、缺失继承与 Player 否决都有针对性的提取与 Core 测试。
  普通尸体路径要求生命为零；生命为 1 的 Boss 阶段仍算活着，下沉则独立处理。
  源码 `dbfe717` 上的六个集成原生用例（由 `53edc77` 记录）覆盖普通活着/死亡、Raised Zombie 为 false、Hillock 生命 1、
  下沉，以及被该源码自身召唤运行时强制为 false 的归属 Spectral Wolf。
  本分支保留普通的标记运算与字段；它没有独立原生通过，也没有归属角色的运行时。
  Core 辅助函数保留一个可选的归属保护；尸体身份与最终死亡登记仍未做。
  Deathrattle 的战吼爆炸是第一个消费者：一个已死、生命为零且符合条件的怪物爆炸一次，被标记
  `SpawnedMonster.CorpseConsumed` 并移除（[11](../docs/protocol/11-skills.md) 第 10i 节）。
  代码：`MonsterLifeData.cs`、`MonsterObject`、`MonsterCatalog.cs`、`Program.Monsters.cs`、`ot_components.py`。
  文档：[04](../docs/protocol/04-player-entity.md#44-life-0x141e72890-55-bytes)。
- 玩家对怪物的命中：每次都会在怪物上记录时间（`PlayerHitAt`），玩家死亡会清除它。
  `enemy_life_regeneration_rate_+%_for_4_seconds_on_hit`（Ryslatha 的信徒）会让怪物在受击后 4 秒内的回复被缩放。
  只有 Gateway 覆盖；未用真实客户端核对。
  代码：`Program.MonsterMods.cs`、`Program.Combat.cs`。
  文档：[14](../docs/protocol/14-monster-packs.md) §5、[Polytheist](../docs/game/polytheist.md)。
- 怪物对玩家的命中：一次既未被闪避也未被格挡的攻击会在怪物上记录时间（`HitPlayerAt`），玩家死亡会清除它。
  在其后 4 秒内，玩家的 `enemies_that_hit_you_with_attack_recently_attack_speed_+%`（Gruthkul 的信徒）会拉长怪物的挥击，
  并在其 Stats 组件上重发攻击属性。
  只有 Gateway 覆盖；客户端的挥击动画是否跟随这些属性，未用真实客户端核对。
  代码：`Program.Monsters.cs`（`MonsterSwingSeconds`、`SendAttackerSlows`）、`MonsterMods.cs`。
  文档：[11](../docs/protocol/11-skills.md) §6、[Polytheist](../docs/game/polytheist.md)。
- 服务端上的怪物行走速度包含客户端那种逐怪物速度掷骰：Actor 建立时从以对象 id 及其类型为键的 TinyMT32 状态中，
  在 MonsterVarieties 该行的范围内取一个均匀百分比（羊头人 ±5%，实测基础值 8 时落在 7.60–8.40 区间；多数行没有范围），
  而行走估算、挥击射程与接触检查都用这个速度；上下界相等时保留固定百分比而不是回落为零。
  范围、生成器与键顺序来自反编译并通过测试；键里的类型字是否是 0x2bb 的类型哈希、从而数值是否与客户端完全一致，
  尚未实机核对（`scripts/re/frida/trace-movement.js` 会打印每只怪物的种子字、范围与倍率）。
  代码：`MonsterSpeedRoll.cs`、`AnimationVariantSelector.cs`、`MonsterCatalog.cs`、Gateway（`SpawnMonster`）。
  文档：[07](../docs/protocol/07-latency-visibility-movement.md) §5（"Monsters on the client"）。
- 怪物词缀的命中（神射手的、精准的）：怪物的命中在其挥击里被提高，但只有测试检查过；实机里玩家从未被闪避过。
  代码：`MonsterMods.cs`。
  文档：[14](../docs/protocol/14-monster-packs.md) §5。
- 怪物召唤技能：技能会带来怪物的怪物（stat set 的 `alternate_minion`，一行 SummonedSpecificMonsters；迷宫的 Immolator）
  会在其仇恨范围内、按技能的施法时间与冷却使用，条件是它自己的怪物活着少于五只（我们的上限）；
  在动作的接触点，其 `number_of_monsters_to_summon` 会出现在它前方 `summon_specific_monsters_in_front_offset` 处、
  朝向玩家、敌对，技能说明不给时不提供经验与物品，且不被副本保留；召唤者播放其角色自身的 `SpellAreaOfEffect` 片段（0x1d1），
  因为该技能自己的动作在客户端上什么都不播。
  为火葬场试炼而建：其 Immolators 的骷髅已实机核对（2026-11-01），其开场动画正在核对。
  代码：`SkillCatalog.MonsterSummon`、`Program.Monsters.cs`（`MonsterSummon`）、Gateway。
  文档：[labyrinth-trials](../docs/game/labyrinth-trials.md)（"Monsters"）。
- 怪物逃跑：一次带「使怪物逃跑」几率的玩家命中（Surfcaster 的小天赋、Chance to Flee Support）会让活着的怪物走到距玩家 5 米外的地板格子，
  并在那段行走结束前不攻击。
  魔法怪免疫一半的逃跑，稀有的全部免疫，走各自稀有度的词缀。
  只有 Gateway 测试检查它；距离与真实服务端用什么动作驱动它都未验证。
  代码：`MonsterFlee.cs`、`MonsterAilments.cs`、`Program.Monsters.cs`。
  文档：[11](../docs/protocol/11-skills.md) §6、[Surfcaster](../docs/game/surfcaster.md)。

## 尚未实现

- 区域怪物：怪群数量、魔法/稀有几率与稀有权重数都是我们自己定的数值；
  第一幕暮光海滩把新生成的怪物上限暂定为 45 只（含 Hillock 与教学僵尸）。
  参考抓包缺了它的开场，所以这是校准而不是已核对的官方总数；
  74 个怪物词缀需要服务端缺少的系统（对友方的光环、怪物技能、玩家身上的异常状态……），因此不会掷出；
  溺亡者忽略其地面类型；破土怪物在玩家靠近多近时才升起是我们的选择；
  Boss 血条与 Boss 阶段只为 Hillock 实现（`BossFights.cs`；其他带 `BossHealthBar` 的 Boss 也会得到血条）；怪物每次进入都重生；
  新的（区域, 种子）需要一次 `-Walkmaps` 走访才有其专属 Boss（[14](../docs/protocol/14-monster-packs.md) §4）。
  第二幕的盗贼领主（Kraityn、Alira、Oak）以敌对状态创建，没有帮助或击杀的对话
  （[17](../docs/protocol/17-npcs.md)）。
  未摆放：Marceus the Defaced（某个格子的雕像刷新器）、太阳神庙二层的三个旗帜（布局从不摆放的房间）与 Thistlesage
  （没有房间点名他的怪群）（[14](../docs/protocol/14-monster-packs.md) §3）。
  Boss 的后续形态（Abberath2、Shakari2、Malachai 的第三形态、The Depraved Trinity）交给尚未创建它们的战斗或任务。
