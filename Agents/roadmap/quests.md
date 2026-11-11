# 路线图：任务

属于 [路线图索引](../Roadmap.md)；这个区域已经能用的功能见 [status/quests.md](../status/quests.md)。
条目沿用路线图的优先级分组，同一组内大致按优先级排列。

## 3. 玩法

### 任务

- **已知：** 任务引擎按每个任务一张声明式表驱动，覆盖第一到第十幕与尾声
  （[12](../docs/protocol/12-quests.md) §5–19），并有对照用的标准答案（`QuestSimulator`、`QuestEngineTests`）；
  新增一幕就是加一个表文件（[剧情任务](../docs/game/campaign-quests.md) 的 "Adding an act" 一节）。
  符合条件的普通 NPC 介绍、任务续接与奖励共用同一套引擎及其保存的标记；
  原生核对覆盖 Nessa、Bestel 与 Tarkleigh，而后续幕里的上下文相关对话仍缺覆盖。
- **入手：** 用客户端在这套模块上完整跑一遍整个剧情：第二到第十幕、尾声与万神殿还没有跑过一遍
  （顺带核对万神殿面板会高亮所选的神）；盗贼投票（a2q7 的帮助分支）与万神殿的灵魂
  （神圣容器、Sin 的「Unlock Power」：[万神殿](../docs/game/campaign/pantheon.md) §1）尚未实现。
  开场遭遇剩下的工作是弹窗确认与 1/2 号力墙
  （[开场遭遇](../docs/protocol/12-quests.md#opening-encounter-2026-10-25)）。
  海滩的视觉部分（大门的动画、门房；[12](../docs/protocol/12-quests.md) §6）与 a1q4 的瀑布动画（§4）优先级较低。
- **完成标准：** 剧情任务能够完整跑通，包含它们带来的世界变化、日志与奖励。
- **Known:** the quest engine with declarative tables per quest, Acts 1–10 and the Epilogue
  ([12](../docs/protocol/12-quests.md) §5–19), the oracle (`QuestSimulator`, with no test running it yet); adding an act is a
  file of tables ([campaign quests](../docs/game/campaign-quests.md) "Adding an act").
  Eligible ordinary NPC introductions, quest continuations and rewards share that engine and its saved flags;
  native checks cover Nessa, Bestel and Tarkleigh, while contextual later-act dialogue still needs coverage.
- **Start:** a run of the whole campaign on the modules with the client: Acts 2 to 10, the Epilogue and the Pantheon
  have not had one yet (check that the Pantheon panel highlights the chosen gods); the bandit vote (a2q7's help path)
  and the Pantheon's souls (Divine Vessels, Sin's "Unlock Power": [pantheon](../docs/game/campaign/pantheon.md) §1) are
  not built.
  The opening encounter's remaining work is popup acknowledgements and force walls 1/2
  ([opening encounter](../docs/protocol/12-quests.md#opening-encounter-2026-10-25)).
  The Strand's visual pieces (the gate's animation, the Doorman;
  [12](../docs/protocol/12-quests.md) §6) and a1q4's waterfall animation (§4) are low priority.
- **Done when:** the campaign's quests can be played through, with their world changes, logs and rewards.
