# 状态：任务

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/quests.md)。

## 已实现（已用真实客户端验证）

- 任务 a1q1：新角色从暮光海滩的海滩上开始，并在那里登录，直到进入城镇；
  走到距 Hillock 60 格以内会置位 `a1q1-SeenHillock`（「Kill Hillock」）；
  Hillock 被激怒后屏幕顶部会有 Boss 血条（StateMachine `boss_life_bar`，0x2bc）、传奇生命值（1 级 311）并有第二阶段：
  半血时拔出长剑（ZombieBossExtract，Transitionable → 2），并在其提取出的接触点回血（约 750 ms），
  有独立的 3 秒恢复；通往大门的力墙会一直立着直到他死亡，服务端在自己的行走地图上也会封路（`QuestBarriers`），
  所以它的位置估算与客户端停在同一点；他死后任务日志会提示进入大门（每个角色都有 `CompletedAllTutorials`）；
  他的死亡会让角色升到 2 级、移除力墙并创建通往狮眼守望的大门（Gateway 也会检查该标记）；
  回城卷轴在海滩上无效（该区域的区域 mod `MapNoPortals`）；进城镇之前城里没有「Resurrect in Town」。
  代码：`Act1.cs`、`BossFights.cs`、`QuestBarriers`（QuestObjects 模块）、`AreaModCatalog.cs`。
  文档：[12](../docs/protocol/12-quests.md) §5–6。
- a1q4「Breaking Some Eggs」，位于泥滩（实机跑通，seed 1）：三个恐鸟巢立在抓取的格子记录处，角色仍需要时每个副本各掉一次雕文。
  拾取雕文会置位 `a1q4-KnowsAboutGlyphs` 与 `a1q4-HaveGlyphN`；靠近雕文墙会置位 `a1q4-SeenGlyphWall`。
  带齐三个雕文使用墙面会收走它们、置位 `a1q4-PlacedGlyphs` 并播放墙的切换（0x2bc 到 Transitionable）。
  随后客户端的隐形墙打开，通往沉没通道的出口出现；在此之前服务端的行走估算与客户端停在同一道隐形墙处。
  进度写在存档里：服务端重启后墙仍保持完成、通道仍开着。
  Tarkleigh 的「Glyph Reward」提供六颗技能宝石并置位 `a1q4-QuestRewardTaken`。
  需要该区域的 `-Walkmaps` 抓取（`1_1_3_seed<seed>.objects.json`）。
  代码：`GlyphQuest.cs`、QuestObjects 模块、`Act1.cs`。
  文档：[12](../docs/protocol/12-quests.md) §1–4。
- a1q5「Mercy Mission」（实机跑通，潮汐岛 seed 1）：Nessa 通过各自的 NPCTalk 行给出任务，Bestel 给出线索，
  任务追踪跟随标记。
  Hailrake 站在沉船处的 `mapboss`（medicine_boat 房间抓取到的摆放点），并掉落药箱——除非 Nessa 已经有一份、角色身上带着一份，
  或副本地面上已经躺着一份。
  `a1q5-HaveMedicineChest` 跟随携带的药箱；Nessa 的「The Medicine Chest」会在一个存档里收下它并置位
  `a1q5-NessaCongratulated`，而在此之后捡到的药箱会被收走。
  需要该区域的 `-Walkmaps` 抓取（seed 1 有）：没有的话 Hailrake 不会被摆放。
  她的两个奖励各给一次普通药剂（物品等级 4）与该职业的一颗辅助宝石。
  进度能跨重登与服务端重启存活。
  Hailrake 的掉落是引擎表里的规则，药箱的标记与交付则来自它 `.it` 里的 Quest 组件。
  代码：`MercyMission.cs`、`Act1.cs`、`QuestItems.cs`、`QuestRewardCatalog.cs`。
  文档：[12](../docs/protocol/12-quests.md) §7。
- Quests: a1q1: new characters
  start on The Twilight Strand's beach and log in there until they reach a town;
  coming within 60 cells of Hillock sets `a1q1-SeenHillock` ("Kill Hillock");
  Hillock has the boss life bar at the top of the screen once he aggroes (StateMachine `boss_life_bar`, 0x2bc), unique
  life (311 at level 1) and a second stage: at half life he pulls out his sword (ZombieBossExtract, Transitionable → 2)
  and heals at its extracted contact (~750 ms), with separate 3 s recovery; the force wall stands across the
  way to the gate until he dies, and the server closes the way on its own walk map too (`QuestBarriers`), so its
  position estimate stops where the client stops; after his death the quest log says to enter the gate (every
  character has `CompletedAllTutorials`); his death levels the character to 2, removes the wall and creates the gate
  into Lioneye's Watch (the Gateway checks the flag too); Portal Scrolls do nothing on the Strand (its area mod
  MapNoPortals); no Resurrect in Town before a town.
  Code: `Act1.cs`, `BossFights.cs`, `QuestBarriers` (QuestObjects module), `Rules.AreaModCatalog.cs` (Portals module).
  Docs: [12](../docs/protocol/12-quests.md) §5–6.
- a1q4 "Breaking Some Eggs" in The Mud Flats (played through live, seed 1): the three Rhoa Nests stand at the captured
  tile records and each drops its glyph once per instance while the character still needs it.
  Picking a glyph up sets `a1q4-KnowsAboutGlyphs` and `a1q4-HaveGlyphN`; coming near the Glyph Wall sets
  `a1q4-SeenGlyphWall`.
  Using the wall with all three glyphs takes them, sets `a1q4-PlacedGlyphs` and plays the wall's transition (0x2bc to
  Transitionable).
  The client's invisible wall then opens, and the exit into The Submerged Passage appears; until then the server's
  walk estimate stops at the same invisible wall as the client.
  Progress is in the save: after a server restart the wall stays done and the passage open.
  Tarkleigh's "Glyph Reward" offers the six skill gems and sets `a1q4-QuestRewardTaken`.
  Needs the area's `-Walkmaps` capture (`1_1_3_seed<seed>.objects.json`).
  Code: `GlyphQuest.cs`, the QuestObjects module, `Act1.cs`.
  Docs: [12](../docs/protocol/12-quests.md) §1–4.
- a1q5 "Mercy Mission" (played through live, The Tidal Island seed 1): Nessa gives the quest and Bestel his clue
  through their own NPCTalk rows, and the quest tracker follows the flags.
  Hailrake stands at the shipwreck's `mapboss` (the medicine_boat room's captured placement) and drops the Medicine
  Chest, unless Nessa has it, the character carries one or one lies on the instance's ground.
  `a1q5-HaveMedicineChest` follows the carried chest; Nessa's "The Medicine Chest" takes it with
  `a1q5-NessaCongratulated` in one save, and a chest found after that is taken away.
  Needs the area's `-Walkmaps` captures (seed 1 has them): without them Hailrake is not placed.
  Her two rewards give a normal flask (item level 4) and a support gem of the class, once each.
  Progress survives relogs and server restarts.
  Hailrake's drop is a rule of the engine's table, the chest's flags and hand-in its `.it` Quest component's.
  Code: `MercyMission.cs`, `Act1.cs`, `QuestItems.cs`, `QuestRewardCatalog.cs`.
  Docs: [12](../docs/protocol/12-quests.md) §7.
## Partly working

## 部分实现
- The quest engine and all of Act 1 on it, checked by the Act 1 oracle and not yet played with the client on the
  modules (a1q1, a1q4 and a1q5 under "Works" were played before they moved onto it; the other Act 1 quests below were
  played on the engine before the game moved into modules): flags per character in the save, one declarative table per
  quest, one file per act (`Modules/Core/World/QuestObjects/Acts/Act1.cs` …, registered by the QuestObjects module and
  run only for the leagues that list it; [campaign quests](../docs/game/campaign-quests.md) "Adding a quest"),
  `/quest_flag`.
  Kills, sightings, room objects come near, exits, cleared areas, quest chests, room NPCs, NPCs that turn into
  monsters, second forms and mirrored blockers are rules; quest items (their `.it` Quest flags: held, picked up,
  handed in, removed, used), dialogue flags and scripts, session flags and the rewards come from the client's data.
  Quest chest and Rhoa Nest markers disappear for the character once their item is collected or handed in.
  Quest passive points (QuestStaticRewards) are counted from the flags into the tree's `extra_skill_points` and stay
  after a relog; refund points are credited once (the Book of Regrets: 54 → 56); QuestStaticRewards' stats are stat
  sources. `QuestSimulator` plays an act's events through the engine; no test runs it since the Gateway tests were wiped.
  What existing saves see: a character with `a10q3-SeenBoss` but without `EndGameAvailable` earned no longer has the
  Act 10 −30% resistance penalty (it follows `EndGameAvailable`, QuestStaticRewards 39); a second copy of a quest book
  (from `/item`) is used up without its points once the first was used; a quest item lying on the ground after its
  quest is done with it (a Medicine Chest after Nessa has one) goes when clicked instead of being picked up.
  Code: Gateway `Quest/` (`Quests.cs`, `QuestItems.cs`, `QuestStaticRewards.cs`, `QuestStates.cs`, `QuestSimulator.cs`),
  Game `World/Quest/`, the QuestObjects module.
  Docs: [12](../docs/protocol/12-quests.md) §5, [campaign quests](../docs/game/campaign-quests.md).
- a1q8 "A Dirty Job" and a1q7 "The Dweller of the Deep":
  Kadavrus and the Dweller spawn at `mapboss`; seeing and killing them set their flags; the last of The Fetid Pool's 96
  monsters sets `a1q8-Cleared`. Tarkleigh's rows, then his book rewards (offers 5, 6, which opened nothing before) are
  claimed; using the Book of Regrets gives two refund points, the Book of Skill a passive point (`extra_skill_points` 1).
  Docs: [12](../docs/protocol/12-quests.md) §8.
- a1q2 "The Caged Brute": Brutus spawns in the Warden's
  section (`mapboss2`); Piety's room sets `SeenPietyPrison`; his death creates the exit to Prisoner's Gate, entered;
  Tarkleigh's reward is claimed.
  Docs: [12](../docs/protocol/12-quests.md) §9.
- a1q6 "The Marooned Mariner": Fairgraves stands on his
  ship; "Marooned" gives the quest; the slave girl's chest in The Ship Graveyard Cave drops the Allflame; "Thank You"
  hands it in and turns him into the boss, who fights; his death, Bestel's book and its passive point follow.
  Docs: [12](../docs/protocol/12-quests.md) §10.
- a1q3 "The Siren's Cadence" and Act 2: Merveil, the Siren
  spawns in her arena, her death brings Merveil, the Twisted, whose death sets `a1q3-KilledSeaWitch` and creates the
  exit; the character entered The Southern Forest. Nessa's help and reward are claimed.
  Docs: [12](../docs/protocol/12-quests.md) §9.
- a1q9 "The Way Forward", Act 1: Piety's blockage at Prisoner's
  Gate sets `a1q9-SeenBlockage` and `a1q9-RoadBlocked`; the road to The Western Forest stays closed (Act 2 opens it).
  After all of Act 1 the save had every `Act1Complete` flag.
  Docs: [12](../docs/protocol/12-quests.md) §9.
- Acts 2 and 3 (live 2026-10-30, seed 1, on the engine before the modules: the campaign runner on a character started
  with `/quest_preset Act1Complete`; on the modules checked by the oracle and the offline exit audit, not yet with the
  client): a2q11 (Arteri, his emblem, the Thaumetic Seal, Bestel's book), a2q10, a2q6 (Helena climbing up after
  Piety in the Chamber of Sins, Fidelitas, the Strange Device), a2q1–a2q3 (the bandit lords killed, their amulets), a2q4, a2q5 (the crypt's altar,
  Yeena's book), a2q7 (Eramir takes the amulets: a passive point; The Apex), a2q9 (the tree poisoned with the spike and
  the gem), a2q8 (the Ancient Seal, the Dark Altar, the Vaal Oversoul, the pyramid's exit), a3q1–a3q5, a3q8, a3q9,
  a3q11–a3q13 and a3q10 (the Epic Door, Dominus' two forms, Lady Dialla on the tower, the exit to The Aqueduct) ran
  unattended in one pass and ended in Highgate with every flag of `Act2CompleteEramir` and `Act3Complete` and every
  Act 2/3 quest log at "Quest Complete". In the Crematorium, Piety retreats at half life. Once the room guards
  are gone each character can take Tolman's Bracelet, including before rescuing Clarissa.
  Multi-item hand-ins take every item in one click (Eramir's amulets, Hargan's busts, Siosa's pages, Fairgraves' plum
  and decanter).
  The Library's candle opens the hidden door for the instance only.
  The two exits the layout disables and a quest gates (The Upper Sceptre of God → The Aqueduct, The Library → The
  Archives) are created (`scripts/re/exits --quests`: `gated` at seed 1 and three other seeds).
  Every quest object, chest and NPC the rules name is placed at seed 1.
  Driver step files: `scripts/dev/campaign/quests/a2q*.py`, `a3q*.py`.
  Code: `Acts/Act2.cs`, `Acts/Act3.cs` (QuestObjects module).
  Docs: [12](../docs/protocol/12-quests.md) §11–12; [act 2](../docs/game/campaign/act2.md),
  [act 3](../docs/game/campaign/act3.md).
- Acts 4, 5 and 6 (live 2026-10-30, seed 1, on the engine before the modules: a fresh Witch from `/quest_preset
  Act3Complete`, the campaign runner unattended; on the modules checked by the oracle, not yet with the client): a4q2
  (Voll, Deshret's Banner, the seal opens the Mines), a4q6 (Deshret's spirit, the book), a4q3 and a4q4 (Dialla at the
  Rapture, Kaom, Daresso, both Eyes handed in), a4q5 (the Rapture Device, Piety in the Belly and her talk, the three
  guardians' organs in one click to open the Black Core), a4q1 (Malachai's second form at the second arena's boss point, the withered Dialla opening the Ascent,
  her reward), a5q1a (the Resonator), a5q1b, a5q3, a5q2, a5q4 (the Templar Courts door, Avarius → Innocence, Sin's talk,
  the arena shortcut after Innocence falls in that instance, burnt Oriath's exits to their own versions of the areas), a5q5 (Bannon, Utula), a5q7 (the relic cases, one click) and
  a5q6 (the Ossuary tomb, the Sign in its socket wakes Kitava again on an unfinished return to a fresh rooftop, `/kill` reaches his threshold: `a5q6-CompletedBoss`,
  −30 %; Lilly sails to Lioneye's Watch), then a6q4 (The Twilight Strand cleared), a6q5, a6q3 (Tukohama and the two bridges to his keep), a6q2
  (Shavronne), a6q7 (Abberath, both phases), a6q6 (Ryslatha) and a6q1 (the three Nessas, the Beacon lit, the Black
  Flag, the Brine King; Weylam sails to The Bridge Encampment). It ended in 2_7_town with every flag of `Act4Complete`
  and the act 5/6 completion flags; every area change through an exit, a waypoint or an NPC-script transport. The god
  Sins appear where the gods die. `a4q1-KillGuardians` is not set. The Beacon now has both proximity-escorted fuel
  carts, route encounters, staged refuelling and separate ignition/flag use (live 2026-11-06); the escort uses the payload's displayed ring and current rail leg. Encounter counts still need comparison with the original server. The Brine King now has a separate staged controller
   (75/50/25% life gates, flood, tide, storm, defensive stance, minions and boss life bar): all three floods,
   tide buff entry/exit, combat and the post-kill Act 7 transition were verified live 2026-11-06.
   A party member can finish while the instance host stays at the entrance or dies; late joins retain valid packets.
   Original-server timing and damage comparison remain pending. Driver: `scripts/dev/campaign/quests/a[456]q*.py`.
  Code: `Acts/Act4.cs`, `Acts/Act5.cs`, `Acts/Act6.cs` (QuestObjects module), Highgate's two Diallas in `NPC/TownNpcs.cs`.
  Docs: [12](../docs/protocol/12-quests.md) §13–15; [act 4](../docs/game/campaign/act4.md),
  [act 5](../docs/game/campaign/act5.md), [act 6](../docs/game/campaign/act6.md).
- The Pantheon (live 2026-10-30 before the game moved into modules: the campaign runner chose The Brine King and
  Tukohama; on the modules checked by the oracle and the data test, not yet with the client): a god unlocks with its
  boss's kill flag (Act 6's four, the later acts' as their tables come); C→S 0x1ee chooses the major or minor god of a
  PantheonPanelLayout row in a town, a hideout, the Rogue Harbour or after Sin's god row (`CanChangePantheon`), saved
  per character and shown by the Player component (creation blob and update 0x0080); the god's power (and the souls'
  upgrades once `EndGameAvailable` is earned: every character is given it at login, so it counts only after a quest
  set it) joins the stat sources. `/pantheon` and /state's `pantheon` for the driver.
  The panel's highlight of the choice is not checked; the souls (Divine Vessels) are not built.
  Code: the Pantheon module (`Modules/Core/Player/Pantheon`), Gateway `Player/PantheonCatalog.cs`, Core
  `InstancePantheon.cs`. Docs: [pantheon](../docs/game/campaign/pantheon.md).
- Acts 7–10 and the Epilogue (live 2026-10-31, seed 1, on the engine before the modules: one Witch from
  `/quest_preset E3StartOfAct7`, the runner unattended per act, no `/goto`; on the modules checked by the oracle and
  the offline exit audit; firefly pickup and Arakaali entry checked in the client): a7q5, a7q2 (Maligaro's Map, the Reverie Device's portal into the
  Sanctum, Black Death's venom), a7q3 (Silk's Obsidian Key, the Sins barrier, the passage into The Den), a7q1
  (Greust), a7q8, a7q9, a7q7 (seven scattered Fireflies, unopened chests remain targetable;
  Yeena burns the webs), a7q6, a7q4 (Arakaali's entrance lands inside the arena, the bridge into
  the offline exit audit; Reverie device also checked in the client): a7q5, a7q2 (Maligaro's Map, the Reverie Device's
  native inventory and six consumable portals into one Sanctum instance with return to the same device;
  Black Death's venom), a7q3 (Silk's Obsidian Key, the Sins barrier, the passage into The Den), a7q1
  (Greust), a7q8, a7q9, a7q7 (seven scattered Fireflies, unopened chests remain targetable; native pickup checked;
  Yeena burns the webs), a7q6, a7q4 (Arakaali's entrance lands inside the arena, the bridge into
  Act 8); a8q1, a8q5, a8q4, a8q6, a8q7 (the legion's multi-kill), a8q3 and a8q2 (both orbs at the fight starter,
  Solaris and Lunaris, the bridge exit into Act 9); a9q3 (the mummy event), a9q5 (the Bottled Storm carried to the
  barrier, both Shakaris), a9q4, a9q2 (the feather to Irasha), a9q1 (both ingredients in one click, the membrane, the
  three spirits, the Trinity, Lilly's voyage); a10q1 (Plaguewing comes near Bannon), a10q6, a10q4, a10q5, a10q2
  (Bannon becomes Innocence: Oriath Docks' two stages), a10q3 (the horns, Kitava's scripted end: `EndGameAvailable`
  earned, −60% with Act 5's, before him −30%; Sin's portal home) and A11q0 (Lilly sails to Karui Shores). Every act
  7–10 quest done with every act 7–10 flag of `EpilogueAtlasDemo`, 24 quest passive points and 20 refund points.
  The exits the layouts disable and a quest gates (The Ashen Fields' way north, The Harbour Bridge's into Act 9, Sin's
  portal home) are created (`scripts/re/exits --quests`: `gated` at seed 1 and three other seeds); burnt Oriath's
  exits keep Act 10's destinations where Act 5's rules name Act 5's (`AreaTransitionCatalog.SameAct`). Before Kitava
  the Epilogue stays shut (`EndGameAvailable` is sent only in a hideout and the maps until earned:
  `Quests.EndgameOnly`). Every quest object, chest and NPC the rules name is placed at seed 1 but The Crossroads'
  BanditBlock (its sight is optional).
  Driver step files `scripts/dev/campaign/quests/a7q*.py` … `a10q*.py`, `A11q0.py`, `a10q3a11.py`.
  Code: `Acts/Act7.cs` … `Acts/Act10.cs` (QuestObjects module), Oriath Docks' InnocenceTown line in `NPC/TownNpcs.cs`.
  Docs: [12](../docs/protocol/12-quests.md) §16–19; [act 7](../docs/game/campaign/act7.md) …
  [act 10](../docs/game/campaign/act10.md).

- 任务引擎以及建立在其上的整个第一幕，由第一幕的标准答案检查，尚未在模块上用客户端实机跑过
  （「已实现」里的 a1q1、a1q4、a1q5 是在它们迁到引擎上之前跑的；下面其他第一幕任务是在游戏搬进模块之前跑在引擎上的）：
  每个角色的标记存在存档里，每个任务一张声明式表，每一幕一个文件（`Modules/Core/World/QuestObjects/Acts/Act1.cs` ……，
  由 QuestObjects 模块注册、只在列出它的赛季里运行；[剧情任务](../docs/game/campaign-quests.md) 的 "Adding a quest"）、
  `/quest_flag`。
  击杀、目击、房间物件靠近、出口、清空区域、任务宝箱、房间 NPC、会变成怪物的 NPC、第二形态与镜像封锁都是规则；
  任务物品（其 `.it` 的 Quest 标记：持有、拾取、交付、移除、使用）、对话标记与脚本、会话标记与奖励都来自客户端数据。
  任务天赋点（QuestStaticRewards）由标记计入天赋树的 `extra_skill_points`，重登后保留；退点只记一次
  （悔恨之书：54 → 56）；QuestStaticRewards 的属性作为属性来源。`QuestSimulator` 把一幕的事件跑过引擎；
  `QuestEngineTests` 用 `Act1Complete`、`Act2CompleteEramir`、`Act3Complete`、`Act4Complete` 以及第五、六幕每个任务的
  「complete」行核对第一到第六幕，再用 `E3EndOfAct8` 与 `EpilogueAtlasDemo` 核对第七到第十幕与尾声。
  既有存档看到的变化：有 `a10q3-SeenBoss` 但没有获得 `EndGameAvailable` 的角色不再有第十幕的 −30% 抗性惩罚
  （它跟随 `EndGameAvailable`，QuestStaticRewards 39）；从 `/item` 拿到的第二本任务书，在第一本用完之后再用会被消耗但不给点；
  任务结束之后躺在地上的任务物品（例如 Nessa 已经有一份之后的药箱）点击时会被清掉而不是被拾取。
  代码：Gateway `Quest/`（`Quests.cs`、`QuestItems.cs`、`QuestStaticRewards.cs`、`QuestStates.cs`、`QuestSimulator.cs`）、
  Game `World/Quest/`、QuestObjects 模块。
  文档：[12](../docs/protocol/12-quests.md) §5、[剧情任务](../docs/game/campaign-quests.md)。
- a1q8「A Dirty Job」与 a1q7「The Dweller of the Deep」：
  Kadavrus 与 Dweller 在 `mapboss` 生成；看到与击杀它们会置位各自标记；恶臭水池 96 只怪物中的最后一只置位 `a1q8-Cleared`。
  Tarkleigh 的对话行、随后他的书籍奖励（报价 5、6，之前什么都打不开）可领取；使用悔恨之书给两点退点，技能之书给一点天赋点
  （`extra_skill_points` 1）。
  文档：[12](../docs/protocol/12-quests.md) §8。
- a1q2「The Caged Brute」：Brutus 在监狱长的区域（`mapboss2`）生成；Piety 的房间置位 `SeenPietyPrison`；
  他的死亡会创建通往囚徒之门的出口，并可进入；Tarkleigh 的奖励可领取。
  文档：[12](../docs/protocol/12-quests.md) §9。
- a1q6「The Marooned Mariner」：Fairgraves 站在他的船上；「Marooned」给出任务；
  船骸洞穴里那名女奴的箱子掉落 Allflame；「Thank You」交付它并把 Fairgraves 变成会战斗的 Boss；
  随后是他的死亡、Bestel 的书及其天赋点。
  文档：[12](../docs/protocol/12-quests.md) §10。
- a1q3「The Siren's Cadence」与第二幕：海妖 Merveil 在她的竞技场生成，她的死亡带来扭曲的 Merveil，
  其死亡置位 `a1q3-KilledSeaWitch` 并创建出口；角色进入了南部森林。Nessa 的帮助与奖励可领取。
  文档：[12](../docs/protocol/12-quests.md) §9。
- a1q9「The Way Forward」，第一幕：Piety 在囚徒之门的封锁置位 `a1q9-SeenBlockage` 与 `a1q9-RoadBlocked`；
  通往西部森林的路仍关闭（由第二幕打开）。第一幕全部结束后，存档拥有全部 `Act1Complete` 标记。
  文档：[12](../docs/protocol/12-quests.md) §9。
- 第二与第三幕（2026-10-30 实机，seed 1，在游戏搬进模块之前跑在引擎上：用一个 `/quest_preset Act1Complete` 起步的角色
  跑剧情跑测器；在模块上由标准答案与离线出口审计检查，尚未用客户端跑）：a2q11（Arteri、他的徽记、Thaumetic Seal、
  Bestel 的书）、a2q10、a2q6（Fidelitas 之后爬上来的 Helena、Strange Device）、a2q1–a2q3（三个盗贼领主被击杀及其护符）、
  a2q4、a2q5（地穴的祭坛、Yeena 的书）、a2q7（Eramir 收下护符：一点天赋点；The Apex）、
  a2q9（用尖刺与宝石毒化大树）、a2q8（古代封印、黑暗祭坛、瓦尔超灵、金字塔出口）、a3q1–a3q5、a3q8、a3q9、
  a3q11–a3q13 与 a3q10（史诗之门、Dominus 的两个形态、塔上的 Lady Dialla、通往水渠的出口）一趟无人值守跑完，
  在 Highgate 结束，`Act2CompleteEramir` 与 `Act3Complete` 的全部标记齐备、第二/三幕每个任务日志都是「Quest Complete」。
  多物品交付一次点击即可交出全部（Eramir 的护符、Hargan 的半身像、Siosa 的书页、Fairgraves 的李子与酒壶）。
- **再次进入古代金字塔时瓦尔超灵会重新出现（2026-11-08）。** `On.Spawns` 原来只在使用祭坛置位 `a2q8-PlacedApex`
  的那一刻创建它，所以在击杀前离开区域就永久失去这个 Boss：祭坛从此处于已切换状态（`QuestObjectTransitioned`），
  它的使用规则没有标记可再置位，于是 `UseQuestObject` 停在「无事可做」，永远到不了生成那一步。
  现在进入该区域会把它放回去（`QuestEngine.RespawnQuestSpawns`，读取 `Quests.SpawnsOf`）：
  若某个生成物的对象已处于使用后状态、且其击杀标记全都未置位，就在该对象自身所在格子创建，除非同变体的活怪已经在那里。
  瓦尔超灵（古代金字塔、放着 Apex 的黑暗祭坛）是整条剧情里唯一的 `On.Spawns` 规则，所以其他区域不受影响。
  尚未用真实客户端核对。
  图书馆的蜡烛只会为本副本打开隐藏门。
  布局禁用、由任务开启的两个出口（上神之权杖 → 水渠、图书馆 → 档案馆）会被创建
  （`scripts/re/exits --quests`：在 seed 1 与另外三个种子上为 `gated`）。
  规则点名的每个任务物件、宝箱与 NPC 都在 seed 1 摆放到位。
  驱动脚本：`scripts/dev/campaign/quests/a2q*.py`、`a3q*.py`。
  代码：`Acts/Act2.cs`、`Acts/Act3.cs`（QuestObjects 模块）。
  文档：[12](../docs/protocol/12-quests.md) §11–12；[第二幕](../docs/game/campaign/act2.md)、
  [第三幕](../docs/game/campaign/act3.md)。
- 第四、五、六幕（2026-10-30 实机，seed 1，在游戏搬进模块之前跑在引擎上：从 `/quest_preset Act3Complete` 起的新女巫，
  剧情跑测器无人值守；在模块上由标准答案检查，尚未用客户端跑）：a4q2（Voll、Deshret 的旗帜、封印开启矿坑）、
  a4q6（Deshret 的灵魂、书）、a4q3 与 a4q4（Rapture 处的 Dialla、Kaom、Daresso、两只眼睛都交付）、
  a4q5（Rapture 装置、腹中的 Piety 与她的对话、三个守卫的器官一次点击、黑核）、a4q1（Malachai 的第三形态、
  枯萎的 Dialla 打开登天之路、她的奖励）、a5q1a（共振器）、a5q1b、a5q3、a5q2、a5q4（圣堂法庭之门、
  Avarius → Innocence、Sin 的对话、被焚奥瑞亚的出口通往各自版本的区域）、a5q5（Bannon、Utula）、
  a5q7（圣物箱，一次点击）与 a5q6（骨灰堂的墓、插进孔里的印记唤醒 Kitava，`/kill` 把它打到阈值：`a5q6-CompletedBoss`，
  −30%；Lilly 航行到狮眼守望），然后是 a6q4（暮光海滩清空）、a6q5、a6q3（Tukohama）、a6q2（Shavronne）、
  a6q7（Abberath，两个阶段）、a6q6（Ryslatha）与 a6q1（三个 Nessa、点亮信标、黑旗、盐水之王；Weylam 航行到桥头营地）。
  它在 2_7_town 结束，`Act4Complete` 的全部标记与第五/六幕的完成标记齐备；每次区域切换都经由出口、传送点或 NPC 脚本传送。
  神祇的罪（Sins）会在神明死亡处出现。`a4q1-KillGuardians` 没有置位。
  信标现在有两条带护卫的燃料车、沿途遭遇、分阶段补给，以及点火与标记各自独立的使用（2026-11-06 实机核对）；
  其护卫半径与遭遇次数仍需与原版服务端比对。盐水之王现在有独立的分阶段控制器
  （75/50/25% 生命闸、洪水、潮汐、风暴、防御姿态、召唤物与 Boss 血条）：三次洪水、潮汐增益的进出、战斗
  与击杀后第七幕的切换都已在 2026-11-06 实机核对。
  队长守在入口或已经死亡时，队员也能完成；中途加入者仍能收到有效报文。
  与原版服务端的时序与伤害比对仍未做。驱动脚本：`scripts/dev/campaign/quests/a[456]q*.py`。
  代码：`Acts/Act4.cs`、`Acts/Act5.cs`、`Acts/Act6.cs`（QuestObjects 模块）、Highgate 的两个 Dialla 在 `NPC/TownNpcs.cs`。
  文档：[12](../docs/protocol/12-quests.md) §13–15；[第四幕](../docs/game/campaign/act4.md)、
  [第五幕](../docs/game/campaign/act5.md)、[第六幕](../docs/game/campaign/act6.md)。
- 万神殿（2026-10-30 实机，在游戏搬进模块之前：剧情跑测器选了盐水之王与 Tukohama；在模块上由标准答案与数据测试检查，
  尚未用客户端跑）：神明随其 Boss 的击杀标记解锁（第六幕的四个，后续幕按各自的表）；C→S 0x1ee 在城镇、藏身处、
  盗贼港或 Sin 的神祇行之后（`CanChangePantheon`）为某个 PantheonPanelLayout 行选择主神或次神，按角色保存并由
  Player 组件显示（创建 blob 与更新 0x0080）；神明的力量（以及获得 `EndGameAvailable` 之后的灵魂升级：每个角色登录时都会被授予它，
  所以只有任务置位之后才真正生效）加入属性来源。`/pantheon` 与 /state 的 `pantheon` 供驱动脚本使用。
  面板对所选的高亮尚未核对；灵魂（神圣容器）尚未实现。
  代码：万神殿模块（`Modules/Core/Player/Pantheon`）、Gateway `Player/PantheonCatalog.cs`、
  Core `InstancePantheon.cs`。文档：[万神殿](../docs/game/campaign/pantheon.md)。
- 第七到第十幕与尾声（2026-10-31 实机，seed 1，在游戏搬进模块之前跑在引擎上：一个从 `/quest_preset E3StartOfAct7` 起的女巫，
  跑测器按幕无人值守、不用 `/goto`；在模块上由标准答案与离线出口审计检查，尚未用客户端跑）：a7q5、a7q2（Maligaro 的地图、
  Reverie 装置通往圣所的传送门、Black Death 的毒）、a7q3（Silk 的黑曜石钥匙、Sins 的屏障、通往兽穴的通道）、a7q1（Greust）、
  a7q8、a7q9、a7q7（七只散落的萤火虫、Yeena 烧掉蛛网）、a7q6、a7q4（Arakaali、通往第八幕的桥）；a8q1、a8q5、a8q4、a8q6、
  a8q7（军团的多杀）、a8q3 与 a8q2（战斗触发处的两颗球、Solaris 与 Lunaris、通往第九幕的桥出口）；
  a9q3（木乃伊事件）、a9q5（把瓶装风暴带到屏障、两个 Shakari）、a9q4、a9q2（把羽毛交给 Irasha）、
  a9q1（两种原料一次点击、薄膜、三个灵魂、三位一体、Lilly 的航行）；a10q1（Plaguewing 靠近 Bannon）、a10q6、a10q4、
  a10q5、a10q2（Bannon 变成 Innocence：奥瑞亚码头的两个阶段）、a10q3（号角、Kitava 的脚本化结局：获得 `EndGameAvailable`，
  与他交手前叠加第五幕共 −60%，交手前为 −30%；Sin 的回城传送门）以及 A11q0（Lilly 航行到卡鲁海岸）。
  第七到第十幕每个任务都完成，`EpilogueAtlasDemo` 的全部标记齐备，24 点任务天赋点与 20 点退点。
  布局禁用、由任务开启的出口（灰烬之地向北的路、港桥通往第九幕、Sin 的回城传送门）会被创建
  （`scripts/re/exits --quests`：在 seed 1 与另外三个种子上为 `gated`）；被焚奥瑞亚的出口在第五幕规则点名第五幕目标的地方，
  仍保留第十幕的目标（`AreaTransitionCatalog.SameAct`）。在 Kitava 之前尾声保持关闭（`EndGameAvailable` 只在藏身处与地图里下发，
  直到获得为止：`Quests.EndgameOnly`）。规则点名的每个任务物件、宝箱与 NPC 都在 seed 1 摆放到位，除了十字路口的
  BanditBlock（它的目击是可选的）。
  驱动脚本 `scripts/dev/campaign/quests/a7q*.py` …… `a10q*.py`、`A11q0.py`、`a10q3a11.py`。
  代码：`Acts/Act7.cs` …… `Acts/Act10.cs`（QuestObjects 模块）、奥瑞亚码头 InnocenceTown 那行在 `NPC/TownNpcs.cs`。
  文档：[12](../docs/protocol/12-quests.md) §16–19；[第七幕](../docs/game/campaign/act7.md) ……
  [第十幕](../docs/game/campaign/act10.md)。

- 新角色会捡起本职业武器、与垂死的流放者对话，并击杀咬人的僵尸换取技能宝石。
  首次进入时角色躺着、朝向房间设定的起始方向，第一次点击就地播放起身。
  开场只保存一次，不会重置已有进度的角色。
  同副本里的其他玩家看到的角色始终是站立状态。
  第一次点击物件会以同样方式让角色起身，而不是走过去。
  这遵循客户端自身的服务端代码，尚未在客户端里核对。
  Gateway 回归覆盖起身回显与重试、两种房间角度公式，以及唤醒字节与瓦尔灵魂额度，含仅有灵魂的 Actor 载荷与已有进度的角色。
  辅助宝石来自它的宝箱，需要玩家走进范围。
  摆放使用抓取到的房间数据，宝石领取只保存一次；已有角色保留其物品与进度。
  完整的女巫武器、僵尸/宝石与宝箱/辅助宝石流程已实机核对。
  流放者会记住你手上的武器提示，并在被咬之前用对应的长/短对话。
  战斗在被咬的 3.1 秒结束后开始，不再有从前那段冻结等待。
  关闭已备好的对话会立刻开始被咬，只保留它 3.1 秒的动画再进入战斗。
  关闭未备武器的提示不会触发它。
  修正后的流程已用一个已装备的决斗者与一个在提示后才装备的女巫核对。
  全新的决斗者与女巫客户端核对确认了修正后的躺卧方向与起身动作。
  跳过长短对话后，被咬在关闭后 0.07 秒内开始，约 3.1 秒后进入战斗。
  战斗僵尸会移动并播放攻击动画，决斗者领取了 Double Strike。
  教学弹窗确认与 1/2 号力墙仍未实现，摆放也还需要本地抓取。
  见 [开场遭遇](../docs/protocol/12-quests.md#opening-encounter-2026-10-25)。
- 带 Transitionable 的任务出口（Merveil 的出口、竞技场大门）打开时没有播放动画（普通 AreaTransition），
  而竞技场大门根本没有被创建。
- 盗贼投票（a2q7 的帮助分支：与盗贼领主对话会通过 `DispatchRulesetEvent(BanditCallVote)`、VoteType/VoteState
  打开客户端的投票面板）尚未实现；它的报文还没有逆向。
  领主是敌对的，选择来自击杀（剧情跑测器把三个都杀了）；帮助分支奖励的属性（QuestStaticRewards 4–6）在标记置位后生效。
- 已激活的传送点若把某个任务标记当作准入标记（第一幕的 Hillock 之于海岸），这类角色会跳过该标记带来的世界变化：
  用 OfflineWitch 时，暮光海滩的力墙没有被创建，城门直接开着。

## 尚未实现

暂无。
