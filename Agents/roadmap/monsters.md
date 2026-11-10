# 路线图：怪物

属于 [路线图索引](../Roadmap.md)；这个区域已经能用的功能见 [status/monsters.md](../status/monsters.md)。
条目沿用路线图的优先级分组，同一组内大致按优先级排列。

## 2. 世界

### 怪物：`/spawn_mob` 之外的部分

- **已知：** 生成、等级、稀有度、名字、阵营、生命、StateMachine / Transitionable 类型、行走、追击与近战攻击
  都可用（[10](../docs/protocol/10-chat-and-commands.md) §5、[11](../docs/protocol/11-skills.md) §6）；
  区域会按其 MonsterPacks 与专属 Boss 填充（[14](../docs/protocol/14-monster-packs.md)）；
  暮光海滩有一个暂定的 45 只怪物校准上限；完整的官方数量与摆放规则仍未定论，因为参考抓包缺了它的开场。
  破土而出的怪物会在玩家附近升起（[10](../docs/protocol/10-chat-and-commands.md) §5）。
  命中能让怪物逃跑（[11](../docs/protocol/11-skills.md) §6）；实机核对应当确定逃跑距离，以及真实服务端是用
  Monster.ot 的 `Flee` 动作还是 Move 来驱动逃跑中的怪物。
  继承自 Life 的尸体/下沉标记与来源资格判定已为普通创建实现
  （[状态](../status/monsters.md#部分实现)）；整体性的原生证据范围见
  [04](../docs/protocol/04-player-entity.md#44-life-0x141e72890-55-bytes)。
  原生 TakeHit 恢复会在同一个恢复区间内停止移动并使普通待处理攻击失效。
  AI 决策现在使用 50 ms 游戏时间步长、100 ms 上限的重新寻路与完整时长的攻击占用期。
  历史上的 LocalExile 控制验证过追击响应、转向、节奏与原生暂停；这次移植待重跑。
  自动化节奏与生命周期检查保留了打断与阶段完成的保护。
- **入手：** 74 个尚不支持的怪物 mod 所需的系统（对友方的光环、反击与死亡时技能、玩家身上的异常状态与诅咒；
  [14](../docs/protocol/14-monster-packs.md) §5）、其他 Boss 的阶段（`BossFights.cs` 里有 Hillock 的，
  `KingInMists.cs` 里是双形态的仪式巅峰）、每个 Boss 在其首个授予效果之后的技能顺序
  （瓦尔超灵只会施放 IncaBossRocks：砸石、喷吐、位移、爆炸、召唤、激光与它的两个触发法术，以及
  `ChangeToStance2/3` 的三个形态都未建模）、怪群的 `Grounds`、
  带 ControlZone / BlightTower / Mercenary …… 等组件的变体（每个都需要自己的反序列化器，见 schema.md；
  NPC、InteractionAction 与 Timer 已写好）、盗贼领主的帮助或击杀对话、Boss 的后续形态
  （Abberath2、Shakari2、Malachai 的第三形态、The Depraved Trinity）由各自的战斗创建、按格子刷新的 Boss
  （Marceus）。
  破土怪物：官方的破土距离、以及一个怪群是否同时升起都未知（服务端的 `.ais` 文件）；Transitionable 列出更多状态的类型
  （SpiderBoss：3 个）仍是以已升起状态创建。
  尸体消费者仍需要权威的最终死亡登记与身份；一次性消耗只以 Deathrattle 的 `SpawnedMonster.CorpseConsumed` 标记存在，
  消费者之间没有共享的预留。
  请保留零生命、类型、幽魂与已驯服等限制；死亡快照还需要 Actor 的条件死亡字节。
  每个新组件都用 `scripts/re/frida/check-components.js` 核对。
- **完成标准：** 区域里的怪物与官方游戏一致（词缀、稀有度加成、破土、Boss 血条）。
- **Known:** spawning, levels, rarity, names, team, life, StateMachine / Transitionable types, walking, chasing and
  melee attacks all work ([10](../docs/protocol/10-chat-and-commands.md) §5, [11](../docs/protocol/11-skills.md) §6);
  areas are populated with their MonsterPacks and unique boss ([14](../docs/protocol/14-monster-packs.md));
  Twilight Strand has a provisional 45-monster calibration cap; complete official population and placement rules
  remain unresolved because the reference capture misses its opening.
  emerge monsters rise near the player ([10](../docs/protocol/10-chat-and-commands.md) §5).
  Act 2's Oversoul has its campaign attack set and altar/exit lifecycle; official decision weights/cooldowns still need
  a reference capture ([status](../status/monsters.md#partly-working)).
  Hits can make monsters flee ([11](../docs/protocol/11-skills.md) §6); a live check should settle the flee distance
  and whether the real server walks a fleeing monster with Monster.ot's `Flee` action instead of Move.
  Inherited Life corpse/sinking flags and the source eligibility predicate are implemented for ordinary creation
  ([status](../status/monsters.md#partly-working)); integrated native evidence is scoped in
  [04](../docs/protocol/04-player-entity.md#44-life-0x141e72890-55-bytes).
  Native TakeHit recovery stops locomotion and invalidates ordinary pending attacks for the same recovery interval.
  AI decisions now use 50 ms game-time steps with 100 ms bounded repaths and full-duration attack busy periods.
  Historical LocalExile controls verify chase response, turning, pacing and native pause; this port awaits a rerun.
  Automated cadence and lifecycle checks retain interruption and stage-completion guards.
- **Start:** the systems the 74 unsupported monster mods need (auras on allies, retaliation and on-death skills, ailments and
  curses on the player; [14](../docs/protocol/14-monster-packs.md) §5), stages of other bosses (`BossFights.cs` has Hillock's
  and `KingInMists.cs` the two-form Ritual pinnacle), pack
  `Grounds`, the varieties with ControlZone / BlightTower / Mercenary … components (each needs its deserializer,
  schema.md; NPC, InteractionAction and Timer are written), the bandit lords' help-or-kill dialogue, bosses' later
  forms (Abberath2, Shakari2, Malachai's second form) created by their fight, tile spawner bosses
  (Marceus).
  Emerge monsters: the official emerge distance and whether a pack rises together are unknown (the server's `.ais`
  files); types whose Transitionable lists more states (SpiderBoss: 3) are still created risen.
  Corpse consumers still need authoritative final-death registration and identity; one-use consumption exists only as
  Deathrattle's `SpawnedMonster.CorpseConsumed` flag, with no reservation shared between consumers.
  Preserve zero-Life, type, spectre and owned restrictions; dead snapshots also need Actor's conditional death bytes.
  Check every new component with `scripts/re/frida/check-components.js`.
- **Done when:** monsters in areas match the official game's (mods, rarity bonuses, emerge, boss bar).

## 4. 服务端设计

- **客户端的怪物控制器：** 怪物走客户端自己的路径（`PlayerWalk`、`ClientPathfinder`）。
  决策循环现在使用 50 ms 游戏时间，目标变化超过三格后重新寻路不快于 100 ms。
  原生响应、转向、攻击节奏与暂停控制通过；官方的怪物控制器规则仍是另一件事。
  估算按客户端骰出的速度行走（见状态文档里的怪物行走速度条目）。
  仍缺的是客户端的怪物控制器（它自己的重新寻路、远处怪物被冻结）以及客户端应用指令所需的 5 到 50 ms
  （[07](../docs/protocol/07-latency-visibility-movement.md) §5 的 "Open"）。
