[加入 Discord](https://discord.gg/pd2Fd5M6Hd)

# LocalExile

一个本地运行的《流放之路》**3.29.3c** 客户端服务端，完全从零写起。它用 C#（.NET 10）从零实现客户端所需的网络协议，
你可以在自己机器上对着跑在 127.0.0.1 的服务端游玩。

流放之路3293b客户端
链接: https://pan.baidu.com/s/1fwade5rl2KMadeFzIWEunw 提取码: rawm 
解压后用客户端的game.exe更新到官方最新版本

然后用服务端LocalExile.exe启动

本仓库**不含任何游戏文件、密钥或游戏数据**。你需要自己已安装好的客户端，服务端需要的一切都在本地从客户端生成。

## 怎么玩？

从 [Releases](https://github.com/LocalExile/LocalExile/releases) 下载 zip，解压，运行 `LocalExile.exe`。
你需要自己有一份独立的《流放之路》3.29.3c 客户端，除此之外不需要安装别的东西。
**始终用 `LocalExile.exe` 启动游戏**：用其他方式启动可能会让客户端更新，从而破坏整套环境。

想要图形启动器？[Waypoint](https://github.com/LocalExile/Waypoint) 是面向 LocalExile 的 Windows 工作台：
它能下载发布包（或配置源码工作区）、启停服务端与客户端，并管理 mod、备份和账号。它是独立项目，并且用安全的方式启动游戏。

## 演示（由新到旧）

https://github.com/user-attachments/assets/e20dd43d-4453-40d4-8f81-eb902a87817f

物品、NPC、仓库、传送点 [(youtube)](https://youtu.be/-QDRFEWCmpU)

https://github.com/user-attachments/assets/fead9d6a-b2ce-4908-9e73-ccaaae78a08a

怪物、掉落、正义之火、等级/经验、天赋树（更高画质见 [youtube](https://www.youtube.com/watch?v=rfnCaXQaxBo)）

https://github.com/user-attachments/assets/7b732013-2d04-4e81-a2fa-5437099f4b6d

在第一幕城镇刷怪

https://github.com/user-attachments/assets/ebb49e70-93aa-464f-a6cd-c6c130dd8c5a

角色选择、狮眼守望、从海岸出口离场，以及在海滩使用 `/set_speed` —— 全部对应本地服务端。

## 已实现的功能

这里只是概览；每个模块的完整清单（已实现、部分实现、尚未实现）见 [Agents/Status.md](Agents/Status.md)。
「（部分）」表示该功能可用，但还不完整，或者尚未用真实客户端从头到尾验证过。

- **登录与角色：** 客户端自带的登录、账号与记住密码；全部职业；在标准（Standard）、硬核（Hardcore）、无情
  （Ruthless）、独狼（SSF）、Allflame 及其变体赛季中创建角色；删除角色；退回角色选择界面；角色、物品与技能栏均可保存。
- **世界：** 所有区域之间的传送、传送点、回城卷轴与传送门技能、副本管理器（Ctrl+点击出口）；副本会保留其中的怪物
  与地面物品直到过期；城镇外可暂停。客户端的地图生成器已移植（部分），迷宫与升华试炼也已移植（部分）。
- **剧情：** 任务引擎跑完整第一幕，其中希洛克（Hillock）、打破一些蛋（Breaking Some Eggs）、仁慈任务
  （Mercy Mission）已完整实机通过；第二至第十章与尾声、盗贼任务、万神殿也已用剧情跑测器走通（部分）。
- **NPC 与玩家：** 各幕城镇的全部 NPC 及其对白、库存持久的商人、聊天文本与频道；同一副本内玩家之间的交易、
  组队与好友。
- **怪物：** 按区域等级生成怪物群、带词缀的魔法与稀有怪、会用自己的技能还击的怪物、会破土而出的埋地怪物、
  受击反应，以及从希洛克到多米纳斯（Dominus）的各个 Boss。
- **技能与战斗：** 普通攻击、宝石技能与弓类攻击、法术、光环与增益（正义之火、骄傲、坚定、优雅、纪律）、充能球，
  以及角色面板上约 150 项属性（生命、魔力、回复、抗性、护甲、闪避、格挡、法术抑制、命中、移动速度）。
- **宝石：** 掉落、任务奖励与宝石商人、宝石经验与等级、需求与连携辅助（部分）。
- **成长：** 经验与等级、天赋树与精通、珠宝孔、集群/永恒/范围珠宝，以及升华。
- **物品与制作：** 按怪物稀有度掉落、金币、生命/魔力/功能药剂及其充能、按稀有度的物品词缀、势力影响与其他物品
  状态、传奇物品、通货（点金石、混沌石、工匠石、神圣石、精华）以及制作台（部分）。
- **仓库与藏身处：** 个人仓库与公会仓库及其特殊页签；每个角色的藏身处支持编辑、制作台、园艺台、夺宝柜、
  Niko 与 Kirac；深渊（Delve）的初步实现与首批地图（部分）。
- **时装：** 每个账号拥有客户端能展示的全部外观。
- **赛季、模块与 mod：** 赛季（含 Survivors 与 Phrecia）就是一组游戏模块，mod 可以调整玩法并在运行时热加载
  （[赛季与模块](Agents/docs/dev/modules.md)、[mod](Agents/docs/dev/modding.md)）。
- **聊天命令：** `/commands` 会列出全部命令，从 `/set_level`、`/spawn_mob`、`/goto` 到 `/mod` 与 `/module`。
- **发布：** 自带 `LocalExile.exe` 启动器的免安装玩家 zip 包，支持 Windows 与 Linux。

## 参与贡献

想一起做？见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 许可

以 [GPL-2.0](LICENSE) 授权；随附的第三方组件见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

## 重要声明

请注意：LocalExile 不是 Grinding Gear Games 的官方产品，与《流放之路》及 Grinding Gear Games 没有任何隶属关系，
也未获其认可。LocalExile 在任何情况下都不赞助、不支持非法的公开服务器。如果你用本项目去开非法公开服务器，
而不是用于测试与学习，那是你个人的选择。

本项目仅供娱乐与学习，定位是与 MMO Solutions（TrinityCore、AzerothCore、MangOS）类似的、无需联网的本地游玩。
它由一个使用 AI 代理（例如 Claude Opus 5.5）协作的贡献者社区构建。欢迎并期待你提交 Pull Request。
