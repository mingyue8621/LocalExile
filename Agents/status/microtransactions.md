# 状态：时装与微交易

属于 [状态索引](../Status.md)；这个区域接下来要做什么见 [路线图](../Roadmap.md)。

## 已实现（已用真实客户端验证）

- 商城传送门效果作为游戏数据（开发用）：`/cosmetic list [filter]`、`/cosmetic portal <名称、编号或名称的一部分>`
  （`CosmeticCatalog.Matches`：输入不明确时会列出所有匹配效果及其编号，而不是直接套用第一个）、`/cosmetic off`。
  传送门时装就是传送门自身的 0x008f 变体（+0x26，一行 `MicrotransactionPortalVariations`），因此客户端会用商城效果
  顶替基础传送门的显示——对已经立着的传送门同样生效（会重新下发一遍）——不需要额外对象、仓库或余额
  （2026-10-21 实测：黑洞效果把六个传送门的基础外观全换掉了——它那个平盘就是真实美术；客户端保留每套骨架自身的
  朝向，会忽略对象的旋转和传送门类型，`/portal_rotation` 是针对确实朝向不对的骨架的开发用旋钮）。
  选择按角色保存（存档里的 `PortalCosmetic`，已验证重登后仍然保留）。
  **2026-11-08 修好一处漏掉的**：卷轴/传送技能开的传送门、以及回程门，此前下发 0x008f 时变体恒为 0，
  在地图装置没有门的时候选时装更是**一条 0x008f 都不发**，所以这些门永远显示基础外观。现在
  `PortalsFeature.StandingTownPortals` 让它们也带变体，`ResendMapPortals` 在挑时装时把「装置的门」和
  「城镇门/回程门」分成两条 0x008f 各发一遍（后一条对它所列的门是完整的，不会顶掉前一条）。
  代码：`Cosmetics.cs`、`InstanceMapDevice.Portal`、Gateway（`ResendMapPortals`、`Game.TownPortals`）、
  `PortalsFeature.StandingTownPortals`、`/cosmetic`。
  文档：[21](../docs/protocol/21-hideouts.md) §5。
- 微交易：每个账号拥有客户端能展示的全部外观（7721 条带 `MtxTypeGameSpecific` 行的 `MtxTypes` 记录）；
  微交易面板（N）、商城的「ALREADY OWNED」标记和外观页签（K）都能加载（C→S 0x1f2 → S→C 0x1f4，
  藏身处与公会列表通过 0x1f5 / 0x1f4 返回空）；在外观页签里的选择（C→S 0x1f9）按角色保存并立即穿上
  （S→C 0x1f3，进入每个区域时也会重发）。
  世界内可用的有：装备皮肤、武器特效、挂件与角色特效（走 Inventories 组件引用）、足迹与幻影（走属性）、
  传送门（地图装置那个）、光标与药剂皮肤。2026-10-23 实测。
- **装备皮肤的美术与挂点（2026-11-08）**：时装的 `ItemVisualIdentity` 行里，`AOFile` 基本是通货宝珠占位符
  （有美术值的 3214 行里 3046 行是 `Metadata/Items/Currency/CurrencyOrb.ao`），真正的**手持模型**在别处：
  单类别的皮肤放 `AOFile2`（弓皮肤 `.../Bows/Microtransactions/ArcOfCourageHeld.ao`、盾牌皮肤
  `.../ZodiacDragonShield/ZodiacDragonShieldHeld.ao`），一把武器多类别的皮肤放在几个未命名列里
  （`__col34`–`__col42`，例如 Malice Weapon 的 `Malice1hSword.ao` / `Malice2hSword.ao` / `MaliceSickle.ao`），
  客户端按装备条目的类别自行挑用。所以**皮肤的美术必须发**：08:54 实测只发 `MtxTypeGameSpecific` 的哈希、
  不发美术时，客户端什么也不画（「使用了外观，人物模型的外观没有变化」）。
  皮肤还带适配类别清单（`ItemisedVisualEffect.ItemClasses`，629 件武器/副手时装里 627 件有限制：盾 93 件、
  弓 33 件、双手 16 件、箭袋 11 件、法杖 15 件……），贴到清单外的物品上没有对应模型、会退回占位美术
  （「部分武器幻化后在脚下」）；现在先按类别判断，不合格就不贴（`CosmeticEffects.FitsItem`）。
  同时修了弓的挂点：拿弓时模型槽的布局是「0 = 箭、1 = 弓、7 = 箭袋」（`BowSetup`），而外观页签仍按普通规则给
  「武器 0、副手 1」，所以武器皮肤贴到了箭上、副手皮肤贴到了弓上；现在这两个槽跟着重映射（0→1、1→7）。
  待实机验证。
  已实现、待实机验证的有：宠物（生成在玩家身边、跟随玩家、不挡路；拾取玩家附近的堆叠物——见下；
  尚未做它们的待机放松动画，例如哥布林剧团的合唱）、头像（角色面板与选人界面）、技能特效
  （外观页签里的技能槽，用 C→S 0x1fa 选择，把效果属性挂到该技能上，正义之火的环与增益视觉、
  燃烧中被改外观会立刻生效，以及火球这类投射物技能的弹药与爆炸）。
  穿戴中的宠物也会捡东西（`Game.Item` 下的 `PetLoot.cs`，2026-11-08 按用户要求加入——国服客户端自带的拾取宠物，
  国际服没有）：每 0.4 秒，把玩家周围 80 格内最多三件地面物品（由近到远）收进主背包。
  只捡可堆叠的（`MaxStack > 1`：通货、碎片、精华、命运卡、圣甲虫）与金币（走 `PickUpGold`，所以进仓库的金库）；
  装备、宝石和任务物品不碰。物品走的是与玩家自己点拾取完全相同的一套取件/入包/保存，以及同样的 0x2bd + 0x00bf
  两个报文，不走路、不播玩家动作，所以客户端只看到物品进了包；背包满时留在地上，下一轮再试。
  `RunPetLoot` 随 `SpawnPets` 启动（进入区域和更换宠物时），随连接结束。
  头像框（社交框槽位）：选择会被保存，并在下次登录时生效，正如客户端自己的聊天提示所说；角色列表会立刻显示。
  2026-10-31 实测：客户端在选人界面和游戏内都能加载 `SupporterIconFrame` 美术，组队中另一名玩家的客户端也能看到；
  换区域时头像框保持不变，直到下次登录；清空槽位则回到默认。
  代码：`InstanceMicrotransactions.cs`、`Microtransactions.cs`（`MtxCatalog`、`CosmeticEffects`）、Gateway
  （`AnswerMtxRequest`、`PickCosmetic`、`Looks`、`CosmeticStats`）、`CharacterSaves.Cosmetics`、`PetObject`、
  `SpawnPets`、`RunPetFollow`、`SkillSlots`。
  文档：[23](../docs/protocol/23-microtransactions.md)。

## 部分实现

- 外观技能的目标生命周期与请求校验（issue [#306](https://github.com/Wrathen/LocalExile/issues/306)）。
  换宝石时复用旧 key、以及接受过期/不兼容选择的问题，已在 Gateway 测试里复现。
  现在目标只在未变化期间保留 key；替换和重新出现会拿到新的、连接内唯一的 key。
  排队的 0x1f3 刷新会在发送闸门内重新采样当前目标，并打对应的绑定日志。
  0x1fa 在改动「按宝石、按槽位」保存的选择之前，会先校验当前目标与效果是否可用。
  自动化覆盖包括：已配置/已清空的 A → B → A、移除后重新出现、排队、多技能、主手/图腾槽位，
  以及正义之火已保存的美术输入保持不变。
  报告中提到的原生选择器不匹配仍需用户验证；报文测试不能证明界面已修好。
  协议证据、限制与客户端侧的验收步骤见：[23](../docs/protocol/23-microtransactions.md) §4。

## 尚未实现

暂无。
