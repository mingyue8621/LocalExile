# pob-converter

`pob` 模组要的「外部转换器」：把 PoB 的分享码换算成服务器能吃的导入 JSON（契约见模组 README）。

## 用法

模组设置 `data/mods/pob.json`：

```json
{
  "converterExecutable": "G:\\POE1.1\\tools\\pob-converter.exe",
  "converterArguments": []
}
```

游戏里 `/pob <分享码>` → 服务器把请求写进它的 stdin，读 stdout 的 JSON 做预览，`Y` 确认。

## 编译

```
dotnet publish Tools/PobConverter/PobConverter.csproj -c Release -o Tools/PobConverter/out
```

产物是单文件自包含 `out/pob-converter.exe`（约 36 MB），拷到 `G:\POE1.1\tools\` 即可，不依赖 .NET 运行时。

## 现在导入了什么

| 项 | 状态 |
|---|---|
| 职业 + 等级 | ✓ 取自 `<Build className level>` |
| 普通天赋树 | ✓ 取 `<Tree>` 里当前 spec 的节点，按 `PassiveSkills.json` 的 `AscendancyKey` 分拣 |
| 升华（升华点/升华职业） | ✗ 只报警告：点数来自迷宫奖励，转换器给不出 |
| 装备、宝石、药水、珠宝 | ✗ 只报警告：`Save.Inventories` 留空，`Merge` 会保留角色原有装备 |
| 其他 spec / 第二套武器 | ✗ |

不在服务器 `PassiveSkills` 里的节点会单独列成警告（通常是 PoB 与客户端版本不一致）。

## 输入输出

stdin 一个 JSON：`{"Version":1,"Code":"…","Name":"…","Data":"…","Record":{…},"SourceFile":null}`
stdout 一个 JSON：`{"Version":1,"Record":{…},"Save":{"Inventories":{},"Passives":[…]}, "Summary":"…","Warnings":[…]}`
出错时往 stderr 写原因并返回非 0 退出码（模组会把这句话显示给玩家）。
