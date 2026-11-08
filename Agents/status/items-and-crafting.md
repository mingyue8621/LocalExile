# 状态：物品、通货与制作

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../roadmap/items-and-crafting.md)。

## 已实现（已用真实客户端验证）

- 地面物品属于区域副本：掉落物与战利品在登出、掉线、客户端崩溃后都留在原地，直到副本过期；id 从 0x100000 起；
  每件物品只能被拾取一次。
  新掉落会播放原生物品/过滤音效；已存在的地面重放保持安静（Divine、Mirror 与任务提示用 NeverSink 追踪过）。
  代码：`InstanceStore.cs`、Gateway。
  文档：[13](../docs/protocol/13-items.md) §5。
- 生命与魔力药剂：按键（C→S 0x70）消耗药剂充能，在共用的 16 ms 回复循环里按 Flasks.dat 的量在持续时间内回复生命或魔力，
  球体以 +1 上升，并带 flask_effect 增益。
  品质、药剂的前后缀与角色的药剂属性都会计入（总量、速率、立即与部分立即、低生命、消耗其他池、结束时补魔力、
  满时不移除、额外生命），与 Path of Building 一致。
  多个回复药剂会排队，速率最高的优先（poewiki）。
  已实机核对：基础回复与额外生命后缀；其他词缀与排队由测试覆盖。
  成功使用还会发送 S→C `0x263`；客户端随后查基底类型并播放其使用音效
  `Audio/Sound Effects/ItemSounds/flaskdrink.ogg`（在官方 3.29.3b 客户端上追踪并抓取）。
  池子已满时在任何充能与增益之前拒绝，并以静默的 `0x263` 结果 `0x4e2` 与权威池子回应；
  魔力满与否按未保留的上限判断（满池拒绝已在本地实机核对）。
  客户端还会为服务端只做行走的攻击动作支付攻击技能的魔力；Gateway 用权威魔力回应，使球体与药剂一致
  （2026-10-26 探测）。
  代码：`Flasks.cs`、`FlaskRecovery.cs`、Gateway（`UseFlask`、`RunResourceRecovery`）。
  文档：[13](../docs/protocol/13-items.md) §8。
- 药剂充能：击杀按稀有度给 1 / 3.5 / 6 / 11 点（Mods.dat 的怪物稀有度词缀，poewiki），
  提高获得充能与充能恢复的数值相乘，部分充能会保留。
  造成暴击时（0.132 秒冷却）、被命中时与每 3 秒获得充能；使用充能与最大充能受词缀影响。
  普通模式的城镇、藏身处与复活会补满所有药剂；在敌对区域放进技能栏的药剂从空开始。
  `Game_FLASK_CHARGES=full` 可恢复「每次击杀补满所有药剂」。
  尚未做：第六到第十幕与地图里更少的充能；被暴击时获得充能（怪物不造成暴击）与满狂怒球时（这里没有）。
  已实机核对：击杀与暴击充能以及敌对区域的规则；被命中与每 3 秒的充能由测试覆盖。
  代码：`FlaskCharges.cs`、Gateway（`Program.FlaskCharges.cs`）。
  文档：[13](../docs/protocol/13-items.md) §8b。
- 药剂词缀组合（#75）：本条里的校验计数与标准/无情观察是历史上的 LocalExile 证据。本分支移植普通模式的计算；
  新的自动化结果与待做的原生核对在[移植记录](../docs/dev/issue75-port.md)。
  角色原始的充能获取来源会一次性解析成原生虚拟值，然后以小数进位乘以本地药剂充能恢复。通用与按资源区分的恢复速率因子相乘；
  混合生命/魔力保持独立速率、原生增益行 5/9、队列与持续时间。
  全局持续时间现在会为面板与使用各解析一次 `flask_duration_+%` 加实际玩家等级来源。
  每次使用在发送充能之前快照等级/属性输入；虚拟显示替换不会改变实际持续时间。物品本地与全局的功能持续时间因子相乘，
  随后是品质与本地更多。
  回复周期对生命/魔力独立使用同一个全局因子；只有随时间的总量按它缩放。
  运行时全局效果也一次性解析原始固定值加实际玩家等级输入，且不替换原始发布的键 1784/20244。本地与适用的魔法功能提高会加到它上面；
  持续时间、品质、恢复速率、立即/低生命阶段、标记与独立的 Onslaught 各自分开。已存在的生效效果保留已捕获的数值。
  39 个新的纯/真实处理函数用例在合并的 53/53 聚焦批次里全部通过，随后是完整 Gateway 3710/3710 无跳过。
  此前未改动的 Core 648/Data 37 仍然适用（合计 4395，并非全部重跑）。
  完整运行的诊断里仍有五个实际已释放流的任务错误，与新计算无关。不主张新的原生证据。宽效果运算、单个增益的整型饱和与
  带符号的最强比较并未加固每一个下游聚合。
  更早的面板/持续时间批次通过了 55 个新增加 80 个既有聚焦检查与 Gateway 3657/3657；未改动的 Core 648/Data 37 仍适用（合计 4342）。
  合并的原生结果覆盖 40 个完成的对照：22 个完整 PASS（12 个正来源、6 个功能与 4 个生命），另有 18 个完整零对照为 UNPROVED；
  面板字段结论合计 138 个 VERIFIED / 42 个 UNPROVED。标准功能持续时间对应 13.8/3/3.9 秒，
  无情为 4.6/2/2.6（分别为 10 级白色、100 级白色与 100 级本地持续时间的使用）。
  生命增益在 10/100 级为 标准160/34 与 无情161/34，对照固定值161/35，落在原有一个整数的发布边界内；
  这不能证明官方取整规则。最终 recovery3 清理干净，全部 769 个冻结输入保持不变，规范背包/原始存档保留、常规存储已恢复。
  更早的失败观察与所有 UNPROVED 字段仍被记录。它们不能验证新的效果解析器。
  整件传奇的原生获取/提示与条件运算符、非正持续时间策略与官方小数取整仍待处理。
  更早的 28 个自动化对照覆盖真实处理函数与两种模式。24 个原生对照验证已安装增益、混合的独立过期、
  满/结束/排队/死亡、实际恢复、充能获取与覆盖。
  无情模式已花费的充能会在城镇保留；普通的 Nessa 交互会在后续对照前补满它们。
  除药剂充能与原生移动 id 之外，原始存档与背包保持不变。条件获取与排队后的原生计时器/预测显示另属工作。
  代码：`FlaskCharges.cs`、`FlaskRecovery.cs`、`FlaskEffectScaling.cs`、`Flasks.cs`、`UtilityFlasks.cs`、
  Gateway（`UseFlask`、`UseUtilityFlask`、`CharacterPlayerStatsCore`）。
  文档：[13](../docs/protocol/13-items.md) §8。

- 功能药剂：每种功能药剂都消耗其充能、显示其增益与下方计时条，并在持续期间给出该增益的属性，全部来自表
  （Flasks.dat、BuffDefinitions、ComponentCharges）。
  这些属性会计入池子（行走估算、`/defences`、`/resistances`、`/hit`）并下发给客户端。
  同种的两瓶药剂各自保留计时，只有最强的一瓶生效（poewiki）；不同效果叠加。
  它们在持续时间结束、死亡、换区域与药剂离开其格子时结束；暂停会挂起它们。
  品质、药剂的持续时间与效果词缀、其「生效期间」后缀以及角色的药剂效果与持续时间都会计入。
  Stibnite 在其烟云中致盲怪物，Sulphur 的圣化地面回复生命，Basalt 嘲讽，Quartz 穿透怪物，Gold 提高掉落稀有度，
  Silver 给 Onslaught。
  已实机核对：每种药剂的使用、客户端上的属性、计时条、暂停、死亡、换区域；未核对客户端是否走得更快或穿透
  （控制桥没法让它走路）。
  Iron Flask 现在会计入权威的 Ward 池，并且只在付费使用之后才恢复破损的 Ward。
  一次原生 Antiquarian 核对花了 40 点充能、立即恢复 502 Ward，到期后回到 22。
  见 [Ward 与原生证据](../docs/game/antiquarian.md)。
  尚未做：玩家身上的异常状态、眩晕与诅咒（Aquamarine、Corundum 与异常状态后缀只到面板）、Aquamarine 的冰缓地面、
  烟云美术、偷取后缀。
  代码：`UtilityFlasks.cs`、`FlaskGrounds.cs`、Gateway（`UseUtilityFlask`、`RunUtilityFlasks`、`RunFlaskGrounds`）。
  文档：[13](../docs/protocol/13-items.md) §8a。
- 物品：初始套装（Characters.dat 的技能与辅助宝石、1–2 键上的两瓶生命药剂与 5 键上的魔力药剂）在背包里，
  以及主手持有的带两个相连孔的本职业武器（`Game_START_WEAPON=ground`：放在离出生点几格的地上，直到被拾取）；
  拾取（0x28）、放下、换位与装备（0x2a，槽位类别与属性需求由客户端校验）、单手武器副手 → 主手、
  镶嵌与取下宝石（0x30、0x2f）、在城镇外丢到地上（0x29 → WorldItem）再拾回来（背包打开时进光标，客户端 22 格范围）、
  城镇里的 Destroy（0x16 /destroy）；堆叠：把手中的堆叠合并到同类型的堆叠上，上限为 CurrencyItems 的 StackSize（0x4a）、
  用 Shift + 点击拆分（0x7c）、按住时 Shift + 点击放一个、满的碎片/裂片堆叠变成其物品、
  拾取、`/item` 与出售奖励会优先填满已有堆叠；已装备武器在模型上；装备里的主动宝石授予其技能；
  角色面板上的职业属性与职业；`/item <行号、Id 或名字> [数量]`。
  代码：`InstanceItems.cs`、Gateway（`ItemCatalog.cs`、`PlayerInventories.cs`）。
  文档：[13](../docs/protocol/13-items.md) §4a。
- 物品词缀：稀有度（普通、魔法、稀有、传奇）、已鉴定，以及 Mods 组件里的固有、显式、附魔、天灾与隐藏词缀列表；
  新物品会掷出其 BaseItemTypes 的固有词缀（浮木法杖 8-12% 提高法术伤害）；魔法名来自词缀；
  稀有度与词缀随物品保存（角色与仓库）；
  对持有的物品有 `/item_mod`、`/item_mod set`、`/item_mod remove`、`/item_mods`、`/rarity`、`/sockets`
  （颜色与连接，例如 `/sockets RRGBBB`）；
  `/item_give <JSON>` 生成整件物品（基底、稀有度、带数值的词缀、状态、孔、稀有名），供启动器的物品制作器这类工具使用；
  JSON 数组按顺序发放物品，若摆放中断会报告部分完成（[item-crafter.md](../docs/dev/item-crafter.md)）；
  `Game_SOCKET_CAPACITY=1..6`（开发用）让 `/sockets` 能给任意可开孔基底那么多孔，例如一根 6 连法杖（它自身容量是 3）。
  魔法与稀有物品：词缀按域、物品等级、物品标签与家族从 Mods.dat 掷出，稀有名来自 Words.dat；
  装备掉落为魔法/稀有且未鉴定；智慧卷轴可鉴定（C→S 0x71）；
  `/item <类型> magic|rare`、`/roll`。
  已装备物品的词缀会计入角色：像天赋一样的全局属性（生命、抗性、属性、法术伤害……）、主手的本地暴击、攻击速度与命中、
  其本地物理伤害与品质，以及其本地附加的元素/混沌伤害（`ItemWeapons`）。
  护甲/闪避/护盾的本地固定、提高与混合词缀以及乘算品质会进入共用池与面板
  （`ItemDefences`，[13](../docs/protocol/13-items.md) §2c）。
  护甲、闪避与护盾共享一个保存的变量基底掷值，并在原生 Armour 组件里下发。
  新护甲只会普通掷一次；旧物品保留其最小值，普通制作会保留该掷值。
  原生核对验证了普通/特殊基底掷值状态、品质组合、装备刷新与重登。
  神圣石的重新掷值与受支持的特殊护盾规则已实现并有自动化覆盖（见「部分实现」）。
  其他传奇与特殊词缀仍取决于各自的机制（#60）。
  Mageblood 的受支持子集见下。
  装备使用共用的 `ItemDefences.Calculate`，包含 Tabula 与显式的无护盾覆盖。
  代码：`InstanceItems.cs`、Gateway（`ModCatalog.cs`、`ItemRolls.cs`、`ItemDrops.cs`、`ItemModStats.cs`、
  `ItemWeapons.cs`、`ItemDefences.cs`、`ItemCatalog.cs`、`CharacterSaves.cs`）。
  文档：[13](../docs/protocol/13-items.md) §2a-§2c。
- 物品状态（2026-10-30 实机核对）：六种势力影响、分裂、合成、腐化、镜像与破碎，以及工艺与附魔词缀都会显示在客户端提示与
  Ctrl+C 文本里，并能跨重登与 Gateway 重启存活。`/item_state` 在持有的物品上开关它们。
  势力影响会为其职业的势力词缀打开每一次掷值与丰收互换（`InfluenceTags.dat`），
  PoB 标记为 Shaper 或 Elder 的传奇会带上它，而破碎石会拒绝带势力影响与合成的物品且不消耗石头。
  目前游玩中没有任何东西创建这些状态，也没有机制读取势力影响。
  代码：`InstanceItems.cs`、Gateway（`ItemStates.cs`、`ItemRolls.cs`、`CharacterSaves.cs`）。
  文档：[13](../docs/protocol/13-items.md) §2f。
- 游戏数据覆盖（2026-10-30 实现）：`data/saves.db` 里的 SQLite 覆盖层 `mod_stat_overrides`、`limit_overrides`、
  `catalog_entries` 与 `system_settings`，通过 `/game_data` 改动词缀掷值范围、物品限制、目录与数值设置；
  Gateway 在启动时加载这些行，把参考词缀/基底列表保存在内存里，并在编辑后原子地重新发布生效快照，所以不需要重启。
  实机核对：设置一个范围与一个永恒珠宝限制、列出修订号、清除两者，以及核对后这些表又变空。
  限制目前覆盖 Historic 永恒珠宝、Impossible Escape、半径型传奇珠宝与纹身总量/分组的上下限。
  `catalog_entries` 现在会播种并重载传奇物品、半径/阈值/传奇/永恒珠宝、纹身效果与珠宝美术目录
  （实机数据库上是 1,299 / 76 / 43 / 6 / 48 / 190 行；传奇行就是完整的 PoB 目录）；代码数组是新数据库的播种值。
  `catalog_seed_keys` 记录已应用的默认值，所以管理员删掉的行保持删除，而真正新的默认值会被加上，
  新的传奇播种修订会纠正没人编辑过的行。
  `stats`/`mods`/`mod_stats` 三张表持有 23,346 / 40,355 / 60,695 条参考行，`effective_mod_stats` 把范围覆盖连接进来。
  新数据库在 Gateway 首次启动时就会得到全部这些；`extract-data.ps1` 会把 `Stats.json`、`Mods.json`、`BaseItemTypes.json`、
  `ItemClasses.json`、`Tags.json` 与 `UniqueStashLayout.json` 纳入其 `tables` 条目的检查。
  `system_settings` 播种 51 个数值开关：按物品类别的 21 个孔位上限（法杖 3、胸甲 6、头盔/手套/鞋子 4、
  盾牌 3、腰带 0、双手 6）、稀有前后缀上限（普通 3/3，珠宝 2/2）、每级天赋点、金币几率/数量/自动拾取、
  150 格投射物最大射程、六个掉落倍率、怪群密度/大小、经验倍率、最高等级、默认纹身/珠宝限制与宽限时间；
  `/game_data setting` 改一个数字并重载它。实机：把一根法杖设为六孔后接受 `RRGBBB`，再改回三孔；
  `passive_points_per_level = 2` 让 0x33 的客户端预算与服务端预算在 1198 点处一致。物品模板的 schema 已存在，
  但自定义物品模板、自定义名称与管理面板尚未实现。
  代码：`GameData.cs`、`GameDataCatalog.cs`、`GameDataStore.cs`、`ModRangeProvider.cs`、`LimitSettings.cs`、
  Gateway（`Program.Commands.cs`）。
  文档：[可编辑的游戏数据](../docs/dev/editable-game-data.md)、[存档](../docs/saves.md)。
- 神圣石：普通与 Shift 的通货使用会在符合条件的护甲上重新掷保存的共用防御基底。
  只有基底掷值改变；词缀、品质、孔、宝石、连接、稀有度与鉴定状态都保留。
  背包与仓库的配对会在一个原子操作里保存目标与一件的付款，包含掷值不变的结果。
  已穿戴物品的重新掷值会刷新角色池与面板。
  原生使用验证了原始 32 变为普通、护甲/闪避/护盾更新、Shift 重复与同值消耗。
  腐化、镜像、非护甲与固定范围的目标会拒绝使用且不付款。
  Ward 与第五个原生范围计入资格；它们的角色池机制仍是独立工作。
  均匀的普通掷值选择是本地策略；官方分布未测量。
  原生固定/零基底拒绝与仅 Ward 的重新掷值都吻合；背包与仓库跨 Gateway 重启存活。
  已验证的数值见 [13](../docs/protocol/13-items.md) 的 "Sacred Orb"。
  代码：`CurrencyEffects.cs`、`ItemCatalog.cs`、Gateway（`UseItemOn`、`ItemMoves.UseAcross`）。
- 特殊防御覆盖与护盾转生命已实现。
  原生全额/50% 转换、独立护盾、生命缩放、零护盾对照与盾牌互换都吻合。
  两个测试背包及其最终池都跨 Gateway 重启存活。
  Tabula 的隐藏覆盖与显式的无护盾只压制其物品自身的护盾，且发生在本地缩放之前。
  全额与百分比转换对每个生效的护甲槽使用最终本地护盾，含符合条件的生效盾牌。
  转换后的生命会接受全局生命提高；独立的全局护盾仍是护盾。
  PoB 导入只为四个可识别的、与原生基底匹配的传奇名恢复隐藏覆盖。
  导入器能识别显式无护盾与全额/百分比转换文本，并使用共用的本地计算器。
  腐化物品的条件护盾映射为该腐化来源的全局护盾提高，保留本地提示里的护盾值。
  仅盾牌的提高现在会影响角色防御，而不改变本地物品数值或转换后的生命。
  原生应用、清除、武器互换与 Gateway 重启检查通过，最终池按半进位取整。
  Ward 转换、戒指槽与珠宝半径机制仍是独立工作。
  代码：`ItemDefences.cs`、`Program.Equipment.cs`、`Program.Progression.cs`、`PathOfBuildingItems.cs`。
  文档：[13](../docs/protocol/13-items.md) §2、[20](../docs/protocol/20-damage.md) 的 "Player energy shield"。
- 金币：由同一赛季的账号角色共享（S→C 0x2aa），随该赛季的仓库保存，进入区域时下发；每个赛季有自己的仓库与金币，
  标准赛季保留账号在此之前的仓库（`AccountStash.Key`）；怪物掉落成堆金币（普通 40%、魔法 75%、稀有与传奇必掉；
  （3 + 1.2 × 等级）× 1/2/5/15，我们自己的数值，`ItemDrops.Gold`），拾取一堆就加到金币上；
  玩家 15 格内的金币会自动拾取（玩家 tick，`GoldAutoLootRange`）；
  旧的按角色金币在登录时并入账号；`/gold <±数量>`。
  代码：`InstanceEntities.Gold`、`AccountStash`、Gateway。
  文档：[04](../docs/protocol/04-player-entity.md) §6。
- 掉落：死亡怪物在尸体周围掉落通货、宝石、药剂与装备，按阶段掷出（与游戏类似，数值是我们自己的）：
  数量（按怪物稀有度，乘以物品数量：赛季的、mod 的、玩家的提高数量，无上限）、种类、装备的槽位（护甲常见、首饰少见）、
  然后是该等级允许的任意基底，最后是稀有度：传奇（该基底某个传奇，没有就稀有）、稀有、魔法，各自几率由物品稀有度提高。
  标记为 not_for_sale 的基底（仅传奇的基底，如「Ring」、Stygian Vise、夺宝、仪式、远征基底）排除在外，atlas 基底保留。
  通货从智慧卷轴一直到镜子，各自按其 DropLevel。
  传奇的权重是其分解价值的倒数（`VillageUniqueDisenchantValues`，无情有自己的列），所以 Mageblood 比 Wanderlust 稀有约 840 倍，
  而一个基底其传奇权重合计小于 1 时多数会给稀有。
  1,527 个参考权重被播种进 `unique_weights`；`/game_data unique_weight` 与 `clear_unique_weight` 实机编辑覆盖层。
  schema 升级、反复重建参考与重开数据库都会保留覆盖；非法或非有限的权重在写入前被拒绝。解包出的 JSON 在播种之前仍是回落。
  词缀生成/几率权重（`SpawnWeight_*`、`GenerationWeight_*`、`Mods.json` 里的家族与标签）仍从 JSON 读取；
  迁移它们还需要在重载时清空词缀池缓存。掉落的物品等级取怪物等级，魔法 +1、稀有 +2、传奇 +3。
  一次掉落事件里同类型的可堆叠物品会合成不超过其 `CurrencyItems` StackSize 的堆叠（二十一颗神圣石躺成 10、10 与 1），
  而不是每单位一个世界物品（`ItemDrops.Stacked`）。
  代码：`ItemDrops.cs`、Gateway（`DropLoot`、`CommitProjectileKill`）。
  文档：[15](../docs/protocol/15-progression.md) §3。

## 部分实现

- **传奇物品的孔位（2026-11-08）。** `UniqueCatalog.Create` 原来用 `ItemCatalog.Create(type)` 构建每件传奇且不带孔位参数，
  所以每件传奇胸甲与武器都没有孔，而同一基底的普通掉落会得到 `ItemCatalog.DefaultSockets` 的两个相连孔。
  传奇现在也取那两个，并且游戏固定的三种布局已建模：Tabula Rasa 一个连接里的六个白孔、Skin of the Loyal 六个相连孔且各用自己掷出的颜色、
  Actum 没有孔。只有新创建的物品会看到这一点——已经在仓库里或角色身上的传奇保留它被创建时的孔位。尚未用真实客户端核对。
- **击杀爆炸与属性减伤（2026-11-04 实现；2026-11-05 用官方客户端核对原生行为，服务端实机核对待做）：**
  「你击杀的敌人爆炸，造成其生命 X% 的物理伤害」（十字军胸甲前缀与合成固有词缀）会把被击杀怪物最大生命的 X% 作为物理伤害，
  打给 25 格内每个敌对怪物，各按目标抗性减免，且由爆炸造成的击杀可以再触发一次。半径是固定的 25 格：
  用官方 3.29.3b 客户端测得，一次爆炸在 22.85 与 24.35 格处击杀，而提高效果范围不会扩大它。
  爆炸会消耗尸体（立刻 0x2bd）并播放原生 `HolyExplosion` 美术（MiscAnimated 2877、
  `Metadata/Effects/Spells/holy_explode/holy_explode.ao`）两秒；其尸体在死亡后 0 到 6 ms 被移除。
  世界吞噬者胸甲固有词缀「每 X 点力量/敏捷/智力少承受 1% 伤害」会把命中、点燃与其他持续伤害作为最终乘数降低。
  未建模：基于几率与条件（点燃、感电、流血）的爆炸变体，以及次要伤害按全局物理/范围伤害缩放。
  代码：`KillExplosions.cs`、`IncomingResourceDefences.cs`、`IncomingDamage.cs`、`PlayerAilments.cs`、`Player.cs`。
- 条件性物品门控会喂给实机池与属性发布；Kintsugi 的受击、过期与生命周期已核对。
  武器/光环、被追踪事件、数值阈值与受支持的运行时状态门控有自动化覆盖。
  未被追踪、统计近期与复合运算符仍是部分实现（[13](../docs/protocol/13-items.md)）。

- **物品词缀效果（2026-11-03 实现，自动化验证；实机客户端待做）：** 物品本地的提高效果属性会缩放该物品自身的显式词缀——
  全部显式词缀（`local_explicit_mod_effect_+%`：Simplex Amulet 100%、Focused Amulet 100%、Subsume the Source 120–240%），
  或只缩放一侧（`local_prefix_mod_effect_+%` / `local_suffix_mod_effect_+%`：Cogwork、Geodesic、Composite、Manifold、
  Ratcheting 与 Helical 戒指，25% 或 50%）。固有、附魔与这些效果属性自身保持原值，数值向下取整，与 The Adorned 的珠宝效果一致。
  同样这些固有词缀的 `local_maximum_prefixes_allowed_+` 与 `local_maximum_suffixes_allowed_+` 会移动通货与制作台制作的
  稀有词缀上限（Composite 6/0、Manifold 4/1、Simplex 1/2、Helical 1/4 ……）。
  未建模：武器树伤害类别效果属性（Mods.dat 没有伤害类型标签）、揭示/酊剂效果，以及按已镶嵌深渊珠宝数量的变体。
  代码：`ItemModStats.cs`、`ItemRolls.cs`、`BenchCrafting.cs`、`ItemWeaponStats.cs`。

- 功能药剂宝珠：Enkindling 与 Instilling 使用原生的加权附魔池，替换先前的附魔。
  Enkindling 的系数与生效期间禁获充能已接线；Instilling 使用付费的药剂使用事件。
  自动化检查通过。原生核对确认了两笔通货交易、附魔替换、最大充能钳制、生效期间充能锁与命中稀有怪物后的自动使用。
  受到的流血/中毒来源仍缺，尽管其附魔条件已表示。
  见 [13-items](../docs/protocol/13-items.md) 的功能药剂宝珠附魔。

- 神圣石重新掷值：普通与 Shift 重复的通货路径只替换共用的防御基底掷值（0..31）。
  需要 Armour/Mods 组件与已知的变量原生范围。腐化、镜像与固定范围的目标被拒绝。
  同一掷值可能重复出现，且仍消耗一颗；词缀、品质、孔、宝石与其他属性不变。
  自动化覆盖检查资格、持久化、存档回滚与已穿戴物品的池刷新。
  在 `63c1039` 记录的十四次原生抓取验证了普通/重复使用、拒绝、背包/仓库组合、结果不变与重启持久化；
  合并分支的新检查仍待做。Ward 范围计入资格，但 Ward 的角色计算与回复仍未实现。
  代码：`CurrencyEffects.Sacred`、Gateway（`Program.Items.cs`）。
  文档：[13](../docs/protocol/13-items.md) 的 "Sacred Orb"。
- 特殊防御规则：本地零护盾词缀只对该物品的基底、词缀与品质中的护盾生效。
  受支持的装备护盾转生命会截断每件物品的贡献，移除全部装备护盾，并在生命提高之前加上基础生命；
  独立的全局/光环护盾仍是护盾，被排除的盾牌不给转换后的生命。
  腐化物品的条件护盾提高进入全局护盾桶，不影响提示与转换后的生命。
  隐藏的无属性需求词缀会保留等级门槛。
  自动化覆盖通过；历史上的原生转换与腐化物品护盾对照记录在 `0bae37a` 与 `c9060e4`（数值见协议参考）。
  合并分支的新实机客户端核对与其他传奇/条件机制仍待做。
  代码：`ItemDefences.cs`、`ItemModStats.cs`、Gateway（`Program.Equipment.cs`）。
  文档：[13](../docs/protocol/13-items.md) §2-§2d。
- **传奇物品与 Mageblood（2026-10-26 实机核对）。** 客户端数据把传奇的名字（`Words` 词表 6）与美术
  （`UniqueStashLayout` → `ItemVisualIdentity`）联系起来，但没有词缀，所以 `UniqueCatalog` 保存每份定义：
  1,299 份由 Path of Building 生成，带其当前基底、固有词缀与掷值（`scripts/re/unique_items.py`），
  与 poedb 的物品卡对比过，剩下的差异记在路线图上。神圣石与祝福石会在一件传奇自身的掷值范围内重掷，
  管理员的 `/game_data mod` 范围优先于它们。
  除五个酊剂之外，每份定义都按测试创建、写出并重载（服务端写不了 Tincture 组件）。
  2026-10-30 在客户端见到过：Tulfall、Shimmeron、Replica Atziri's Foible、Kalandra's Touch、Emperor's Vigilance、
  The Broken Elegy、Gorgon's Gaze、Grand Spectrum 与 Atziri's Reign，带其名字、美术、基底、词缀与物品等级。
  `/item <名字> <基底名>` 可达有多个基底的传奇中的某一个（`/item Grand Spectrum Cobalt Jewel`）。
  `/item Mageblood` 会创建该 Heavy Belt 传奇，带其名字、美术与固定词缀（力量是基底固有词缀）；客户端显示传奇提示。
  穿戴 Mageblood 时，药剂栏最左侧的 2 到 4 瓶魔法功能药剂会永久生效，不需要充能也没有计时，使用它们会被拒绝
  （客户端 mod，服务端兜底）；腰带或药剂栏变化时属性会刷新。它的药剂在客户端不带计时地保持增益（尚未实机见到）。
  Headhunter：击杀稀有怪物会把它的每条已掷词缀作为一层角色属性给出 60 秒（同一条会重置时间），
  一个 Headhunter 增益显示层数与最长时间，以及其已掷的对稀有提高伤害（尚未实机见到；它们会在换区域时结束，而游戏里会保留）。
  尚未做：其他传奇的机制、点化（chancing）「效果无法被移除」的兜底、Foulborn 最右变体、Replica Headhunter 与 Enkindling
  （`UniqueCatalog`、`Program.Mageblood`、`StolenMods`、`Program.UniqueBelts`；
  [13](../docs/protocol/13-items.md) §2e、§8a）。

- 无情模式使用原生的词缀/药剂替代项、降低的可调掉落与商人回收，并拒绝制作台制作。
  进入城镇或藏身处不会补满其池子/药剂；由受支持的商人补满。
  生效中的功能药剂无法获得充能，并且原生的无情持续时间/Onslaught 数值生效。
  自动化池恢复与会话归属检查覆盖换区域、重连与商人补给的持久化。
  掉落、配方、制作台与药剂规则的计算有自动化覆盖；完整的零售平衡仍是部分实现。
  见 [赛季模式](../docs/game/league-modes.md)。
- 精华制作（2026-10-25，自动化验证；实机客户端使用待做）：全部 105 种可制作精华都会从 Essences.dat 解析出其精确的
  按类别保证词缀，含 8 阶与 Desolation。
  普通/稀有资格、随机词缀等级上限、破碎、元词缀拒绝与从精华页签反复消耗都有覆盖。
  普通与 Shift 应用走既有通货路径。
  见 [13](../docs/protocol/13-items.md)。
- 箭袋资格修复（2026-10-25，待客户端确认）：生效的弓会发布 `main_hand_weapon_type = 9`，那是客户端背包门控所需的；
  现在每只手都会发送其武器类型（见 [13](../docs/protocol/13-items.md) 的 "Client weapon-type stats"）。
- **工匠石：2026-10-21 实机核对。** 普通使用与 Shift 共用通货处理函数；重新掷出 1/2/3/4/6 个孔与连接组，
  受类别、尺寸与物品等级限制，消耗一颗。
  3.29：重新掷值保留的孔保留其颜色，只有新增的孔是白色。
  已镶嵌宝石、不受支持的目标与最大基底容量会被拒绝且不消耗。
  实机发现的 1 级鞋子/手套拒绝两孔的问题已修复并实机核对。
  数量与连接概率是近似值；特殊的孔位覆盖未建模。
  见 [13](../docs/protocol/13-items.md) 的 "Jeweller's Orb"。
- **孔位颜色（3.29；2026-10-26 实机核对）。** 物品孔位会生成白色，也可能按基底属性需求的比例掷出颜色，
  几率随物品等级上升；镶嵌在匹配的红/绿/蓝孔里的宝石获得 +10% 品质（属性路径见
  [25](../docs/protocol/25-gem-supports.md)）。
  幻色石强制一个孔非白并掷其余孔，物品品质会提高其几率；制作台的「至少两个/三个/四个非白孔」工艺
  （`NN`/`NNN`/`NNNN`）会一直重掷直到保证那么多；瓦尔腐化会给一个白孔上色。
  `/item`、掉落与商店使用同一个生成器，初始武器的两个相连孔是白色。
  每 1% 品质 +0.25 点（幻色石/制作台）；`SocketColours`、[13](../docs/protocol/13-items.md) 的 "Socket colours (3.29)"。
  未建模：污染幻色石/工匠石、束缚宝珠、腐化之地、25 次制作台奖励、特殊物品覆盖。
- **点金石：** 一件可修改的普通物品变成已鉴定的稀有，带 4 到 6 条加权词缀（CoE 的 2/3、1/4、1/12；珠宝 3 到 4 条、
  每侧两条）与一个稀有名。药剂只接受魔法；未知/不受支持/不可修改、非普通、腐化/镜像与空池目标在消耗之前被拒绝。
  CoE 数量/珠宝检查、普通/Shift 的背包/仓库派发与存档重载有自动化覆盖。
  旧的实机核对（2026-10-21）早于这些规则；原生重新验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Orb of Alchemy" 与[证据](../docs/re/alchemy-coe.md)。
- **混沌石：** 一件可修改稀有物品的未保护词缀会被重掷（CoE 4/5/6 条数，珠宝 3 到 4 条、每侧两条；新阶级、新稀有名）；
  前缀/后缀元词缀锁会保留其侧。药剂只接受魔法；非稀有、不可修改/不受支持与空池目标在消耗之前被拒绝。
  CoE 数量/元词缀检查、普通/Shift 的背包/仓库派发与存档重载有自动化覆盖。
  旧的实机核对（2026-10-21）早于这些规则；原生重新验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Chaos Orb" 与[证据](../docs/re/chaos-coe.md)。
- **神圣石（实机核对，2026-10-21）：** 保留词缀、阶级与顺序，只重掷数值。Shift 可用。
  见 [13](../docs/protocol/13-items.md) 的 "Divine Orb"。
- **重铸石（实机核对，2026-10-21）：** 魔法/稀有/传奇物品变成普通，只保留基底词缀。
  见 [13](../docs/protocol/13-items.md) 的 "Orb of Scouring"。
- **蜕变石：** 普通变已鉴定魔法，以等概率命中 1 到 2 条加权词缀。
  未知、不受支持、不可修改与空池基底在消耗之前被拒绝。
  CoE 数量/池对比、普通/Shift 的背包/仓库派发与存档重载有自动化覆盖。
  2026-10-29 通过了对法杖/药剂的原生升级、重复/不可修改拒绝与重登。
  新的原生 Shift/仓库与手动通货界面核对另属工作。
  见 [13](../docs/protocol/13-items.md) 的 "Orb of Transmutation" 与[证据](../docs/re/transmutation-coe.md)。
- **改造石：** 一件可修改的魔法物品得到 1 到 2 条新加权词缀（最多一条前缀、一条后缀），
  准入与其他词缀通货相同：未知/不受支持/`Unmodifiable` 基底、腐化/镜像物品与空池在消耗之前被拒绝。
  CoE 数量/药剂检查、普通/Shift 的背包/仓库派发与存档重载有自动化覆盖。
  旧的实机核对（2026-10-21）早于这些准入规则；原生重新验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Orb of Alteration" 与[证据](../docs/re/alteration-coe.md)。
- **增幅石：** 在魔法物品上加 1 条加权词缀，最多一条前缀与一条后缀，准入检查同蜕变石：
  已知、可序列化、非 `Unmodifiable` 且带 Mods 的基底；腐化/镜像、已满物品与没有普通词缀的空侧在消耗之前被拒绝。
  CoE beta 池与模拟概率检查、普通/Shift 的背包/仓库派发与存档重载有自动化覆盖。
  旧的实机核对（2026-10-21）早于这些准入检查；原生重新验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Orb of Augmentation" 与[证据](../docs/re/augmentation-coe.md)。
- **富豪石：** 一件可修改魔法物品变稀有，保留词缀，从稀有池加 1 条加权词缀（每侧最多三条），换稀有名。
  药剂被拒绝（只接受魔法），不可修改/不受支持基底与装不下的目标同样被拒绝且不消耗。
  CoE 稀有池/应用检查、普通/Shift 的背包/仓库派发与存档重载有自动化覆盖。
  旧的实机核对（2026-10-21）早于这些准入规则；原生重新验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Regal Orb" 与[证据](../docs/re/regal-coe.md)。
- **崇高石：** 在可修改稀有物品上加 1 条加权词缀，最多 6 条（每侧三条；珠宝每侧两条），名字保留。
  药剂、不可修改/不受支持基底、非稀有与已满目标在消耗之前被拒绝。
  CoE 池/珠宝检查、普通/Shift 的背包/仓库派发与存档重载有自动化覆盖。
  旧的实机核对（2026-10-21）早于这些规则；原生重新验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Exalted Orb" 与[证据](../docs/re/exalted-coe.md)。
- **剥离石：** 从魔法或稀有物品移除一条随机未保护词缀；稀有度、名字、固有词缀与其他词缀保留。
  元词缀/破碎过滤会排除受保护词缀；腐化/镜像/不受支持/`Unmodifiable` 与没有合格显式词缀的目标在消耗之前被拒绝。
  CoE 稀有度/元词缀检查、普通/Shift 的背包/仓库派发与存档重载有自动化覆盖。
  旧的实机核对（2026-10-21）早于这些规则；原生重新验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Orb of Annulment" 与[证据](../docs/re/annulment-coe.md)。
- **护甲片：** 品质随物品等级缩放，最高 20%；修订后的缩放尚未实机核对。
  见 [13](../docs/protocol/13-items.md) 的 "Armourer's Scrap"。
- **磨刀石：** 品质随物品等级缩放，最高 20%；修订后的缩放尚未实机核对。
  见 [13](../docs/protocol/13-items.md) 的 "Blacksmith's Whetstone"。
- **宝石匠的棱镜（无测试，未实机核对）：** 宝石品质 +1%，最高 20%（[13](../docs/protocol/13-items.md)）。
- **远古石：** 把一件未腐化的传奇装备按传奇掉落权重重铸为该物品类别的另一件传奇，保留物品等级、品质、孔与连接。
  只出同背包尺寸的传奇；不会有腐化或 Replica 结果（`UniqueCatalog.Reforge`）。尚未在游戏里核对。
- **机会石：** 文档化的本地规则：普通武器/护甲 → 稀有（30%，4 到 6 条词缀）或魔法（2 到 4 条词缀，每侧最多两条），已鉴定。
  腐化/镜像/不受支持/`Unmodifiable` 与空池目标在消耗之前被拒绝。
  CoE beta 没有 Chance 处理函数，所以官方传奇/稀有度结果另属工作。
  普通/Shift 的背包/仓库派发与存档重载有自动化覆盖；原生重新验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Orb of Chance" 与[准入](../docs/re/chance-coe.md)。
- **瓦尔宝珠与腐化：** 本地规则会腐化可修改的武器、护甲或首饰（Base +0xc7 第 0 位，已保存）；普通/魔法 → 稀有 4 到 6 条词缀，
  稀有物品有 1 到 5 条或全部词缀重掷，一个白孔被上色。腐化/镜像/传奇、不受支持/`Unmodifiable` 与空池目标在消耗之前被拒绝；
  腐化物品除了智慧卷轴不接受任何通货。
  CoE 的官方瓦尔结果表另属工作；准入与边界已文档化。
  普通/Shift 的背包/仓库派发与存档重载有自动化覆盖；原生重新验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Vaal Orb and corruption" 与[准入](../docs/re/vaal-coe.md)。
- **腐化灵液（次级、高级、宏伟、卓越）：** 为胸甲、鞋子、手套或头盔加上该阶的世界吞噬者固有词缀，替换除灼热圣战者之外的其他固有词缀；
  只接受普通/魔法/稀有。传奇/腐化/镜像、非护甲与不受支持/不可修改的目标在消耗之前被拒绝。
  CoE 基底键/阶/存在性检查、四个阶级后缀与普通/Shift 的背包/仓库派发有自动化覆盖。
  独立的腐化支配状态与余烬/冲突家族另属工作，原生提示验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Eldritch Ichors" 与[证据](../docs/re/eldritch-ichor-coe.md)。
- **腐化余烬（次级、高级、宏伟、卓越）：** 灵液的灼热圣战者镜像；为胸甲、鞋子、手套或头盔加上该阶的圣战者固有词缀，
  保留吞噬者固有词缀并移除其他固有词缀；只接受普通/魔法/稀有。传奇/腐化/镜像、非护甲与不受支持/不可修改的目标被拒绝。
  CoE 基底键/阶/存在性检查、四个阶级后缀与普通/Shift 的背包/仓库派发有自动化覆盖。
  独立的腐化支配状态与冲突宝珠另属工作，原生提示验收待做。
  见 [13](../docs/protocol/13-items.md) 的 "Eldritch Embers" 与[证据](../docs/re/eldritch-ember-coe.md)。
- **制图师凿子、腐化混沌/崇高石与废除宝珠（2026-11-05 加入，未实机核对）：** 凿子现在通过其 `CurrencyItems.Action`
  （`add_map_alt_quality`）进入既有的地图品质效果，所以一把凿子或 Maven 的凿子会给地图加上其 AlternateQualityTypes 行的 20%。
  腐化混沌与腐化崇高石只在该物品带世界吞噬者或灼热圣战者固有词缀时才重掷既有稀有的显式词缀（与混沌/崇高一样），
  该固有词缀保留。废除宝珠从背包使用（C→S 0x58），退还三套预设里每一个已分配的图鉴天赋并被消耗；一个都没分配时它保留。
  这三条本地规则不是官方规则。两个 Gateway 测试覆盖凿子准入与腐化准入。
  代码：`CurrencyEffects.Chisel/TryAction`、`EldritchChaos/EldritchExalted`、`Game.UseOrbOfUnmaking`。
- **束缚宝珠、腐化剥离石与污染（天灾）家族（2026-11-05 加入，未实机核对）：** 束缚宝珠把一件可开四孔的未腐化普通武器或护甲
  变成稀有（同点金石），带四个相连孔，颜色来自基底需求。腐化剥离石跟随另外两颗腐化宝珠：
  在带世界吞噬者或灼热圣战者固有词缀的物品上做剥离，该固有词缀保留。这个类别应用的七种污染通货
  （混沌、崇高、幻色、连接、工匠、护甲片、磨刀石）作用于腐化物品并使其保持腐化；污染品质通货加 1%。
  物品以清除腐化状态交接，结果再次被腐化，所以未腐化物品在消耗之前就被拒绝，普通效果会拒绝的任何东西也一样。
  污染神话石、污染神圣泪滴、污染触媒、污染油与污染祝福另属工作。是本地规则，不是官方规则。
  两个 Gateway 测试覆盖束缚准入与污染/腐化准入。
  代码：`CurrencyEffects.Binding/EldritchAnnulment/Tainted`、`Game.CurrencyResult`。
- **Enkindling 宝珠：** 任意稀有度的功能药剂得到五种 GenerationType 21 附魔之一（提高效果/持续时间/充能恢复/最大充能，
  或减少每次使用的充能，都带「生效期间不获得充能」），权重为游戏的 250/100/100/100/100。
  生命/魔力、腐化/镜像、非药剂与不受支持/不可修改的目标在消耗之前被拒绝。已有的 Enkindling 会被替换；
  已有的 Instilling 与药剂的显式词缀、品质、充能、稀有度与名字保留。丰收/制作台药剂附魔（GenerationType 3，
  家族 `FlaskEnchantment`）会被移除。「生效期间不获得充能」的代价由 `FlaskEnchantments.BlocksGain` 经
  `CanGainFlaskCharges` 在击杀、受击、暴击、每 3 秒恢复、补给与投射物上强制执行，只要效果在生效；
  已过期效果与暂停都有覆盖。CoE 池/权重与原生共存检查有自动化覆盖；见
  [13](../docs/protocol/13-items.md) 的 "Enkindling Orb and Instilling Orb" 与[证据](../docs/re/enkindling-coe.md)。
- **Instilling 宝珠：** 灼热圣战者的对应物：功能药剂得到十五种可生成 GenerationType 22 触发附魔之一
  （充能满、冰冻/冰缓/感电/点燃/流血/中毒、格挡、猛击、防卫或位移技能、命中稀有/传奇、相邻药剂、效果结束、生命药剂），
  等权重。第十六行（`Used when you lose a Guard Skill Buff`）在 3.29.3b 里生成权重为 0，被排除，制作台配方也排除。
  已有的 Instilling 会被替换；已有的 Enkindling 与物品其余部分保留。见
  [13](../docs/protocol/13-items.md) 的 "Enkindling Orb and Instilling Orb" 与[证据](../docs/re/instilling-coe.md)。
- **客户端视觉验收（2026-10-30）：** 所有已实现通货的主要成功使用都在 3.29.3b 客户端里核对过：
  蜕变、增幅、富豪、改造、点金、崇高、混沌、瓦尔、机会、剥离、四种腐化灵液与四种腐化余烬、Enkindling 与 Instilling。
  瓦尔把普通物品变成稀有（黄色）且提示显示 `Corrupted`；吞噬者/圣战者固有词缀的名字、数值与阶级可见。
  Enkindling 与 Instilling 同时出现在一瓶药剂上，重复使用只替换自己那一类，魔法显式词缀保留。没有发现视觉缺陷。
  对珠宝使用瓦尔仍在本地规则之外；官方珠宝/瓦尔结果另属工作。仓库/Shift 与其余拒绝路径仍在自动化覆盖之下。
- **玻璃吹制宝珠：** 品质随物品等级缩放，最高 20%；修订后的缩放尚未实机核对。
  见 [13](../docs/protocol/13-items.md) 的 "Glassblower's Bauble"。
- **破碎石（2026-10-21 实机核对；2026-10-30 重新核对规则）：** 一件有 4 条以上显式词缀的稀有物品，会有一条随机显式词缀被破碎，
  并被其他所有通货（含重铸石）保留；显示为破碎（魔法属性 u8 +0x7a）。准入检查现在还要求是受支持的、非 `Unmodifiable`、
  且 `ItemClasses.CanBeFractured` 为真的基底，并拒绝腐化/镜像/已破碎物品；受元词缀保护的词缀仍合格；
  带势力影响与合成的物品被拒绝。未建模被遮蔽的词缀。原生 2026-10-30：一件四条词缀的稀有法杖被破碎，第二次使用被拒绝且不消耗。
  见 [13](../docs/protocol/13-items.md) 的 "Fracturing Orb" 与[游戏规则](../docs/re/fracturing-mirror-mechanics.md)。
- **卡兰德的镜子（2026-10-21 实机核对；2026-10-30 重新核对规则）：** 一件非传奇、未腐化、未镜像且可修改的装备会在光标上
  产生一份镜像副本（Mods 标记第 1 位）；原物品不变。传奇物品与非装备类别（药剂、珠宝、地图、通货）被拒绝，
  不可修改/未知/无 `Mods` 的基底同样。镜像物品不接受任何通货，包括智慧卷轴；反光油与分裂/圣化物品未建模。
  原生 2026-10-30：一件稀有法杖在光标上产生了镜像副本，而对副本使用传奇 Mageblood、钻石药剂、深红珠宝与智慧卷轴
  都被拒绝且不消耗。见 [13](../docs/protocol/13-items.md) 的 "Mirror of Kalandra" 与
  [游戏规则](../docs/re/fracturing-mirror-mechanics.md)。
- **Hinekora's Lock（服务端规则 2026-10-31 实机核对）：** 通过 C→S 0x71 对稀有物品施加 Lock 会置位 Mods 标记第 8 位
  （0x0100）与一个非零种子；客户端显示紫光与 Lock 提示文本。对该物品使用通货时客户端发送 C→S 0x29d（u16 键、
  通货/目标 id），S→C 0x2a0 用预览物品应答（keyA = 请求键，status 0 = 预览，0x04FA = 失败）；
  接下来的 0x71 会精确提交该预览、消耗通货并清除状态。普通、魔法、宝石、腐化、镜像、不可修改与已预见的目标
  在消耗之前被拒绝；制作台/丰收/精华与任何成功使用都会清除该状态。客户端的预览分支是物品弹窗的预见元素
  （`HinekoraForesightPopupIcon`、`HinekorasLockFutureSight`）；status 0 显示它，任何非零 status 播放 `HinekorasLockFail`。
  提交结果与预览逐字节一致。见 [13](../docs/protocol/13-items.md) 的 "Hinekora's Lock" 与[证据](../docs/re/hinekora-lock.md)。
- **连接石：2026-10-21 实机核对。** 抓取到的 0x71/0x72 报文交给同一个处理函数。
  只改变连接；孔、颜色、宝石与物品属性保持不变。拒绝少于两个孔的物品，并保护已全连的 5/6 孔物品；2 到 4 孔的仍可重掷。
  品质有利于更长的连接组且不被消耗。连接/品质概率是近似值，特殊覆盖/腐化未建模。
  见 [13](../docs/protocol/13-items.md) 的 "Orb of Fusing"。
- 通货元词缀：重铸/混沌/神圣/剥离会尊重前缀/后缀锁；剥离与词缀追加会尊重攻击/施法限制。
  重铸保留品质并拒绝传奇。真实数据回归测试覆盖两个锁方向。

## 尚未实现

- 多数传奇与特殊词缀（#60）以及点化（chancing）；Mageblood 与普通 Crown of Eyes 有定义；
  智慧卷轴、幻色石、工匠石、连接石、点金石、混沌石、神圣石、重铸石、蜕变石、改造石、增幅石、富豪石、崇高石、剥离石、
  护甲片、磨刀石、机会石、瓦尔宝珠、四种腐化灵液与余烬、Enkindling 宝珠、Instilling 宝珠、玻璃吹制宝珠、破碎石、
  卡兰德的镜子、Hinekora's Lock、宝石匠的棱镜、神圣石与悔恨宝珠之外的通货；辅助宝石与连接
  （[13](../docs/protocol/13-items.md) 的 "Open"）。
