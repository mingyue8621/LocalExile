# 第三方声明

LocalExile 以 GPL-2.0 授权（[LICENSE](LICENSE)）。发布包中还带有下列程序与库，各自遵循自己的许可。
它们的许可原文在发布包的 `licenses/` 目录里，或在表中标注的包内。

| 组件 | 用途 | 许可 |
|---|---|---|
| .NET runtime and ASP.NET Core | 运行服务端与工具 | MIT |
| Microsoft.CodeAnalysis (Roslyn) | 编译赛季与 mod 脚本 | MIT |
| Microsoft.Data.Sqlite, SQLitePCLRaw | 存档数据库 | MIT, Apache-2.0 |
| SQLite | 存档数据库 | public domain |
| LibGGPK3 | 读取 `Content.ggpk` | AGPL-3.0-or-later |
| OozSharp | 解压游戏 bundle | MIT |
| Python（嵌入式包） | 运行数据生成器 | PSF-2.0，`licenses/python-LICENSE.txt` |
| Node.js | 运行 ooz-wasm | MIT 及其他，`licenses/node-LICENSE.txt` |
| ooz-wasm | 解压游戏 bundle | GPL-3.0-or-later，`licenses/ooz-wasm-LICENSE.txt` |
| dat-schema | 游戏数据表的列布局 | MIT，`licenses/dat-schema-LICENSE.txt` |

LocalExile 与 Grinding Gear Games 没有隶属关系，也未获其背书。发布包不含任何游戏文件：
服务端从游戏读取的一切，都是在玩家自己的电脑上、从玩家自己的客户端里解包出来的。
