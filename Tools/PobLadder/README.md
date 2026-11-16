# pob-ladder

从腾讯《流放之路》英雄榜（公开页面，不需要登录）给**每个职业**抓一份排名最高的 BD，写成 `pob` 模组要的 inbox 文件。

## 数据从哪来

```
榜单     https://poe.qq.com/act/a202010118poena/js/rankinfo_<snapshot>.json?overview=<snapshot>
角色详情 https://poe.qq.com/act/a202010118poena/js/char/<snapshot>/<djb2(账号_角色名)>.json
```

角色详情里有 `pathOfBuildingExport` —— 官方算好的 PoB 分享码，正好是 `pob` 模组的转换器吃的东西。所以整条链路是：
英雄榜 → 分享码 → `pob-converter` → 服务器导入。

## 用法

```
python Tools/PobLadder/fetch_ladder.py [snapshot] [inbox 目录]
python Tools/PobLadder/fetch_ladder.py s29_normal G:\POE1.1\data\pob-import\inbox
```

默认快照 `s29_normal`、默认写进服务器的 inbox。跑完 inbox 里就有 `Scion.txt`、`Marauder.txt` … 七个文件，
游戏里 `/pob file Scion.txt` 即可导入。

## 注意

- 榜单里有些角色详情已经下架（404）或者 TLS 被中断，脚本会跳过并继续；只要每个职业拿到一份就停。
- 抓来的是**天赋树 + 职业 + 等级**，装备/宝石/精通/星团跟转换器的规则一样不导入。
- 快照名可以换（页面默认 `s29_normal`），换赛季先看前端 `bulid.js` 里的 `snapshotName` 默认值。
