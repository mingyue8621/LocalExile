# 状态：登录、角色与存档

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../Roadmap.md)。

## 已实现（已用真实客户端验证）

- 「记住密码」现在会回传并校验官方客户端保存的 32 字节登录凭据，走账号登录 0x03 / 结果 0x04。
  它能跨服务端重启存活，与设备和账号绑定，改密码即失效。
  有协议/会话测试覆盖，并已由用户在真实客户端上确认可用。
  见 [01](../docs/protocol/01-login.md)。
- 登录：自定义 TCP，Diffie-Hellman + DSA 签名的服务端密钥 + Salsa20，账号登录、角色列表、Play。
  代码：`GameServer.Auth`、`GameServer.Core/Protocol/Login`。
  文档：[01](../docs/protocol/01-login.md)、[re/008](../docs/re/008-login-handshake.md)、
  [re/009](../docs/re/009-login-messages.md)。
- 免控制台注册账号：`GameServer.Auth` 的 `RegisterListener`（tcp/20497，环境变量 `Game_AUTH_REGISTER_PORT`，
  绑定地址跟随 `Game_BIND`）逐行应答 `{"op":"register","name":…,"password":…}` 与 `{"op":"count"}`，
  走的是登录界面校验所用的同一个 `AccountService.Register`，所以 流放之路登录器.exe 既能在主机本机建号，
  也能在主机对外放开该地址后由玩家远程建号。2026-11-08 在本地用临时数据库验证过（注册、NameTaken、计数）。
- 角色保存：物品、装备、孔位与技能栏按角色存进 `data/saves.db`（新技能栏：普通攻击在左键，宝石技能只进空槽，
  右键优先）；登录后与换区域后用 S→C 0x53 恢复（键位绑定是 C→S 0x4e）。
  代码：`CharacterSaves.cs`、`PlayerInventories.cs`、Gateway。
  文档：[11](../docs/protocol/11-skills.md) §11、[13](../docs/protocol/13-items.md) §7。
- 可进游戏的登录会从角色上次所在城镇的重生点开始（登出、退回角色选择、崩溃、重启，或死亡状态下掉线之后都是如此）；
  Auth 在城镇票据解析成功时记录该城镇（`data/saves.db` 里的 `Character.LastTown`）。
  代码：Auth（`InstanceDirectory`）、`WorldAreaTowns.cs`。
  文档：[09](../docs/protocol/09-area-change.md) §4。
- 赛季与角色创建：标准与赛季模块（Allflame、无情）及其变体在创建界面都能选（S→C 0x1b），24 个角色位，
  任意赛季可选任意职业（C→S 0x12 → 0x15、0x13），直接进游戏；游戏内区域名下方显示所属赛季（0x1d）；
  硬核死亡界面来自角色所属赛季；环境变量 `Game_LEAGUES`。
  代码：`LeagueCatalog.cs`、`LoginServerSession.cs`、Auth、Gateway。
  文档：[01](../docs/protocol/01-login.md) §7。
- 在角色界面删除角色（C→S 0x0c → 新列表 0x15 + 结果 0x0d）；存档在 `data/saves.db` 里被移到一边
  （`deleted_characters`）。
  代码：`LoginServerSession`、Auth。
  文档：[01](../docs/protocol/01-login.md) §5。
- 游戏内全部职业：Characters.dat 那一行的对象类型与模型、职业字节、属性、徒手攻击与初始装备
  （两个相连白孔加宝石的武器）；已用圣堂武僧验证，地上的武器与保存的宝石也用游侠和暗影验证过。
  代码：Gateway（`SkillCatalog.cs`、`PlayerInventories.cs`）。
  文档：[01](../docs/protocol/01-login.md) §7、[04](../docs/protocol/04-player-entity.md)。
- 退回角色选择：Esc →「退回角色选择」以及硬核死亡界面的退出（C→S 0x153）；Gateway 关闭副本连接，
  客户端在同一条登录连接上回到角色列表，可玩角色能再次进入。
  代码：Gateway。
  文档：[09](../docs/protocol/09-area-change.md) §6。

## 部分实现

- 选定的赛季模式已有服务端规则，并具备自动化覆盖以及原生的进入、退出、迁移验证。
  普通硬核死亡会迁移到对应的软核基础赛季，同时保留 SSF；硬核无情死亡则保持死亡。
  携带的进度与死亡记录在同一事务里提交，来源仓库/金币留在原处，过期写入无法把它撤销。
  自动化覆盖包括普通与无情模式下的致命点燃、过期死亡回调与一次性死亡效果。
  Auth、Gateway 与开发服入口都会拒绝已死亡角色和过期票据；瞬时失败导致的死亡会在进入前重试。
  原生角色列表曾把一个已死亡的硬核无情角色带着终止标记返回到了虚空（Void）；
  一次普通硬核 SSF 死亡则回到了 SSF 标准，并且能再次进入。
  完整的赛季模式对齐仍取决于服务端尚未完成的玩法系统。
  SSF 隔离与无情规则见 [赛季模式](../docs/game/league-modes.md)。
- 本地启动会在拉起客户端之前，显式把缓存的登录网关重置为回环地址，避免过期的远程地址导致 DSA 握手失败
  （General Rhoa Failure #3）。见 protocol/01-login.md。
- **存档放在 SQLite（已测试，尚未用真实客户端核对）。** 账号、角色、角色存档与仓库都是 `data/saves.db` 里的行
  （WAL，synchronous=FULL）；一个仓库是一行表头加每个页签一行。
  每次物品、仓库、金币、天赋、任务标记与传送点的变化都会在一个事务里立即保存。
  仓库移动、购买、出售、在物品上使用通货、金币洗点、地图装置与删除角色，都是要么一次事务保存全部，要么全不保存；
  保存失败会回滚、重新载入被改动的部分并关闭连接。
  经验与击杀回充的药剂每 5 秒、升级时以及断线时保存。
  挂售中的物品保留其背包格子，与 3.29 的灰色印记一致。
  等级来自保存的经验值。
  未知的存档字段会被保留；若存档里有游戏数据不认识的物品，登录会被阻止。
  JSON 存档与 accounts.json 只会被导入一次（`Saves.cs`、`ItemMoves.cs`、`GameServer.Storage/SaveDatabase.cs`、
  `SqliteAccountStore.cs`；[存档](../docs/saves.md)）。

## 尚未实现

暂无。
