# 状态

项目当前的进度，对应客户端 **3.29.3c**。
接下来要做什么见 [Roadmap.md](Roadmap.md)。
协议如何运作见 [docs/README.md](docs/README.md)。

状态按区域拆成各自独立的文件，这样不同区域的 PR 不会改到同样的行。
每个文件的章节结构一致：已实现（Works，已用真实客户端验证）、部分实现（Partly working）、尚未实现（Not working yet）。

| 区域 | 状态 | 路线图 |
|---|---|---|
| 环境搭建、工具与测试 | [status/setup-and-tooling.md](status/setup-and-tooling.md) | [roadmap/setup-and-tooling.md](roadmap/setup-and-tooling.md) |
| 登录、角色与存档 | [status/login-and-characters.md](status/login-and-characters.md) | |
| 世界、移动与网络 | [status/world.md](status/world.md) | [roadmap/world.md](roadmap/world.md) |
| 怪物 | [status/monsters.md](status/monsters.md) | [roadmap/monsters.md](roadmap/monsters.md) |
| 任务 | [status/quests.md](status/quests.md) | [roadmap/quests.md](roadmap/quests.md) |
| NPC、商店与聊天 | [status/npcs-and-chat.md](status/npcs-and-chat.md) | [roadmap/npcs-and-chat.md](roadmap/npcs-and-chat.md) |
| 物品、通货与制作 | [status/items-and-crafting.md](status/items-and-crafting.md) | [roadmap/items-and-crafting.md](roadmap/items-and-crafting.md) |
| 仓库 | [status/stash.md](status/stash.md) | [roadmap/stash.md](roadmap/stash.md) |
| 藏身处、制作台与地图 | [status/hideouts-and-maps.md](status/hideouts-and-maps.md) | [roadmap/hideouts-and-maps.md](roadmap/hideouts-and-maps.md) |
| 时装与微交易 | [status/microtransactions.md](status/microtransactions.md) | |
| 技能、光环、属性与战斗 | [status/skills-and-combat.md](status/skills-and-combat.md) | [roadmap/skills-and-combat.md](roadmap/skills-and-combat.md) |
| 宝石 | [status/gems.md](status/gems.md) | [roadmap/gems.md](roadmap/gems.md) |
| 经验与天赋树 | [status/progression.md](status/progression.md) | [roadmap/progression.md](roadmap/progression.md) |

## 如何维护这份状态

任何让某功能变为可用的改动、或发现某处损坏的排查，都要在同一个 PR 里更新该区域的文件。
下面这些规则用来避免 PR 之间互相冲突：

- 条目放到它所属区域的文件里，放进与其状态对应的章节。
  不要在文件开头或结尾加备注，也不要往这份索引里加内容。
- 功能状态变化时，改动或移动它原有的条目，而不是针对它新加一条带日期的备注。
- 每个条目保持一行：说明实现了什么、文档在哪里。机制写进文档，证据写在 PR 里。
- 每句话另起一行，行宽控制在 120 字符以内。
  不要重排你没有改动的行。
- 不要加「最后更新」日期或任何每个 PR 都会改动的汇总数字。
- 新区域在 `status/`（以及 `roadmap/`）下建自己的文件，并在上面的表格里加一行。
