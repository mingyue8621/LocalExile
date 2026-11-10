# 为 LocalExile 做贡献

人类和 AI 智能体都欢迎。先从 [AGENTS.md](AGENTS.md) 看规矩，然后看
[Agents/Status.md](Agents/Status.md)（哪些已经能用）和 [Agents/Roadmap.md](Agents/Roadmap.md)（接下来做什么、
怎么认领一个条目）。协议参考与各种笔记在
[Agents/docs/](Agents/docs/README.md)。

## 给人看的部分

- 打开一个前沿模型（例如 Claude Opus 5.5），让它在你机器上把这个项目搭起来。
- 让它把 `Agents/` 整个目录和所有 MD 文件读一遍。

## 开始搭建

**完整指南：[Agents/GettingStarted.md](Agents/GettingStarted.md)。** 它逐步列出每一步该看到什么输出、边界情况
以及一张排错表。这份指南同时面向人和面向正在做搭建的 AI 智能体。

简版（Windows、.NET 10 SDK、一份独立的流放之路 **3.29.3c** 客户端；Linux 见完整指南）：

```powershell
dotnet build GameServer.slnx
robocopy "<你的 PoE 3.29.3c 目录>" Client /E; Copy-Item Client\PathOfExile.exe Client\Game.exe
dotnet run --project src/GameServer.Auth -- keygen Client/Game.exe
dotnet run --project src/GameServer.Auth -- patch Client/Game.exe Client/Game.patched.exe
dotnet run --project src/GameServer.Patch -- gateway --login 127.0.0.1:20493
# 需要 Node.js（在 Tools/ooz 里跑 npm install）与 Python 3.9+：live.ps1 会先解包游戏数据（scripts/dev/extract-data.ps1）
# 建账号 + 角色：GameServer.Host 与 GameServer.DevClient（Agents/GettingStarted.md 第 8 步）
.\scripts\dev\live.ps1 -Walkmaps   # -Walkmaps 可选：需要 pip install frida，用于抓取行走地图
```

**启动客户端一律走 `live.ps1`。** 用别的方式启动，客户端会去连官方补丁服务器，可能把自己更新掉。

## 项目构成

| 项目 | 作用 | 端口 |
|---|---|---|
| `GameServer.Auth` | 登录服；也用于生成密钥、把客户端打补丁成信任这些密钥 | 20493（登录）、127.0.0.1:20495（控制） |
| `GameServer.Gateway` | 副本（游戏）服务器 | 6112 |
| `GameServer.Patch` | 本地补丁服，让客户端永不去连真正的补丁服务器；也用于修改 GGPK 里的网关列表 | 12995、http 8080 |
| `GameServer.Host` + `GameServer.DevClient` | 开发用账号服务（注册账号、创建角色） | 7000 |
| `GameServer.Core` | 协议、加密、账号、副本注册表 | |
| `GameServer.Data` | GGPK / bundle / `.dat` 读取器 | |
| `GameServer.Storage` | 账号与存档存储 | |
| `GameServer.Modding` | mod 与模块所基于的加载器契约：`IMod`、`IModule`、`League` 与各自的上下文 | |
| `LocalExile` | 发布包里给玩家的启动器（命令行） | |
| `Modules/` | 游戏系统，只对在联盟里列出它的玩家装载：`Core/`（当前游戏）、`Legacy/`（GGG 过去的赛季）、`Custom/`（自定义联盟） | |
| `Leagues/` | 赛季，每个赛季是一组模块 | |
| `Mods/` | 玩家自行选择安装的 mod（[LocalExile-Mods](https://github.com/LocalExile/LocalExile-Mods)），网关运行期间可热重载 | |

某处改动该归哪一层，见 [AGENTS.md](AGENTS.md)（「代码放在哪一层」）。
