# 路线图

接下来要做什么，给任何想帮忙的人（或智能体）。已经能用的功能见 [Status.md](Status.md)。

路线图与状态一样按区域拆成各自独立的文件（对应 [Status.md](Status.md) 里的表格），这样不同区域的 PR 不会改到同样的行：

- [环境搭建、工具与测试](roadmap/setup-and-tooling.md)
- [世界、移动与网络](roadmap/world.md)
- [怪物](roadmap/monsters.md)
- [任务](roadmap/quests.md)
- [NPC、商店与聊天](roadmap/npcs-and-chat.md)
- [物品、通货与制作](roadmap/items-and-crafting.md)
- [仓库](roadmap/stash.md)
- [藏身处、制作台与地图](roadmap/hideouts-and-maps.md)
- [技能、光环、属性与战斗](roadmap/skills-and-combat.md)
- [宝石](roadmap/gems.md)
- [经验与天赋树](roadmap/progression.md)

每个文件里的条目都保留原来的优先级分组（1. 体验优化、2. 世界、3. 玩法、4. 服务端设计、5. 项目），同一组内大致按优先级排列。
「Known」（已知）写的是已经查清的部分；「Start」（入手）写的是该从哪里开始看。

## 如何认领一个条目

1. 在 GitHub 上开一个以该条目命名的 issue（或在已有的 issue 下留言），避免两个人重复做同一件事。
2. 先读条目里链接的文档。大多数条目都从逆向开始：先弄清客户端怎么读这条消息，再由服务端发出去。
   工作流和现成套路见 [docs/re/tooling.md](docs/re/tooling.md)。
3. 用真实客户端通过 `.\scripts\dev\live.ps1` 验证，并检查 `logs\` 里的日志。
   凡是你实现的报文布局，都要补测试（见 `tests/GameServer.Core.Tests`）。
4. 在同一个 PR 里：把代码本身说明不了的东西写进 `docs/protocol/`（新的消息 id、布局、客户端地址记到
   [address-index.md](docs/protocol/address-index.md)），保持简短；把该条目以一行移进它所属区域在 `status/` 下的文件，
   并在 `roadmap/` 下对应区域的文件里删掉或更新它。

[Status.md](Status.md) 里「如何维护这份状态」一节的编辑规则同样适用于路线图文件：条目放进它所属区域的文件和分组，
改动条目而不是给它加备注，每句话另起一行。
