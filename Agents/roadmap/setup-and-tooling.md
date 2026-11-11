# 路线图：环境搭建、工具与测试

属于 [路线图索引](../Roadmap.md)；这个区域已经能用的功能见 [status/setup-and-tooling.md](../status/setup-and-tooling.md)。
条目沿用路线图的优先级分组，同一组内大致按优先级排列。

## 5. 项目

- 把原生图谱集成与[只读分析导出器](../docs/dev/passive-graph-data.md)分开来定义。
  标准启动流程必须保住已安装的 3.29.3c 客户端归档。
  任何对客户端归档的改动都需要维护者另行同意；自动安装图谱未获批准。
  仅有分析产物并不能提供已安装的玩法支持，也不构成原生证据。
- 用 [c2s.md](../docs/protocol/c2s.md) 生成 Gateway 的 C→S 长度表（285 个 id 有精确长度；
  `KnownClientLength` 目前有 66 个）。
  遇到未知 id 时 `SplitClientMessages` 会丢弃该次读取剩余的部分，所以那次读取里它之后的每条消息都会丢失。
- mod：在 [mod 宿主](../status/setup-and-tooling.md)之后剩下的部分（[modding](../docs/dev/modding.md)）。
  目录覆盖（mod 替换 Gateway 各目录里的行）是宿主还没有的钩子。
  赛季迁移：客户端针对已结束赛季的 Migrate 与 Migrate All 按钮，以及它们显示的差异。
  带上该账号已结束的赛季发送 0x1a，为对应的 C→S 消息记录一次实机点击（静态没找到：
  `0x140cc85f0` 经虚表 `0x1431f2cb0` 分发），然后在一个事务里把角色、仓库页签、金币与各模块保存的数据迁到父赛季。
  新系统以 `Modules/Core/` 下的模块形式起步。`Game` 模块的各个部分互相调用，所以它们是一个程序集；
  要让某个赛季能单独关掉其中一部分，就需要把对它的调用换成钩子，那部分再独立成模块。
- 从全新 clone 重建逆向环境（Ghidra 导入、REA、Frida），并把指南遗漏的地方补上。
- 未命名字段：[04](../docs/protocol/04-player-entity.md)、[05](../docs/protocol/05-inventories-and-ui.md) 与
  [09](../docs/protocol/09-area-change.md) 列出了含义未知的字段。
  给它们命名是个很好的入门任务。
- Define native graph integration separately from the [read-only analysis exporter](../docs/dev/passive-graph-data.md).
  Standard startup must preserve the installed 3.29.3c client archive.
  Any client archive mutation requires separate maintainer agreement; automatic graph installation is not approved.
  Analysis artifacts alone provide no installed gameplay support or native proof.
- Generate the Gateway's C→S length table from [c2s.md](../docs/protocol/c2s.md) (287 instance ids with an exact length;
  `KnownClientLength` has 101, beside the variable-length readers in `SplitClientMessages`).
- Integrate the reviewed C→S length candidates into the Gateway after native verification.
  The [offline export](../docs/re/tooling.md) audits the current table and lists candidates from the checked-in inventory.
  `SplitClientMessages` drops the rest of a read at an unknown id, so every message after it in that read is lost.
- Mods: the rest after the [mod host](../status/setup-and-tooling.md) ([modding](../docs/dev/modding.md)).
  Catalog overrides (a mod replacing rows of the Gateway's catalogs) are the hook the host does not have yet.
  League migration: the client's Migrate and Migrate All buttons, and the differences they show, for ended leagues. Send 0x1a with the account's ended
  leagues, log a live click for the C→S message (not found statically: `0x140cc85f0` dispatches through vtable `0x1431f2cb0`), then move characters, stash tabs, gold and
  modules' saved data to the parent in one transaction.
  New systems start as modules in `Modules/Core/`. The `Game` module's parts call one another, so they are one assembly; to let a league turn one off, a part needs hooks in place of the calls into it, and then becomes its own module.
- Rebuild the reverse-engineering setup from a fresh clone (Ghidra import, REA, Frida) and fix whatever the guide
  misses.
- Unnamed fields: [04](../docs/protocol/04-player-entity.md), [05](../docs/protocol/05-inventories-and-ui.md) and
  [09](../docs/protocol/09-area-change.md) list fields whose meaning is unknown.
  Naming them is a good first task.
