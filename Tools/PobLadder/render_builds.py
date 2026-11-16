"""Renders builds.json (from extract_builds.py) into the pob mod's BD 说明: Markdown + HTML (dark + gold)."""
import html
import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIRS = [r"G:\POE1.1\Mods\Binaries\pob", r"G:\tmp-mods\mods\pob"]

CN_KEYSTONE = {
    "Blood Magic": "血魔法", "Eternal Youth": "永葆青春", "Glancing Blows": "擦身而过", "Pain Attunement": "苦痛共鸣",
    "Chaos Inoculation": "异灵之体", "Resolute Technique": "坚毅技法", "Acrobatics": "杂技", "Iron Will": "钢铁意志",
    "Avatar of Fire": "火焰化身", "Eldritch Battery": "异界电池", "Mind over Matter": "心灵升华",
    "Unwavering Stance": "不动如山", "Vaal Pact": "瓦尔契约", "Elemental Overload": "元素超载",
    "Point Blank": "近距离射击", "Arrow Dancing": "箭舞", "Zealot's Oath": "狂信者之誓", "Ghost Dance": "幽灵之舞",
    "Wicked Ward": "邪术护盾", "Wind Dancer": "风之舞者", "Precise Technique": "精准技法",
    "Ancestral Bond": "先祖之契", "Iron Reflexes": "钢铁反射", "Conduit": "导管", "Necromantic Aegis": "亡灵守护",
    "Crimson Dance": "猩红之舞", "Perfect Agony": "完美苦痛", "Runebinder": "符文束缚者",
}
CN_NOTABLE = {
    "Constitution": "体格", "Heart of Oak": "橡木之心", "Thick Skin": "厚皮", "Golem's Blood": "魔像之血",
    "Hired Killer": "受雇杀手", "Finesse": "精妙", "Assassination": "刺杀", "Depth Perception": "深度感知",
    "Written in Blood": "血书", "Discipline and Training": "纪律与训练", "Purity of Flesh": "血肉纯净",
    "Soul of Steel": "钢铁之魂", "Armour Master": "护甲大师", "Barbarism": "野蛮", "Devotion": "虔诚",
}


def cn(name, table):
    if name in table:
        return "%s（%s）" % (table[name], name)
    return name


def build_markdown(builds):
    lines = ["# POB 模组 · 全职业 BD 库", "",
             "腾讯《流放之路》英雄榜 S29 赛季，每个职业排名最高且详情还在的一份 BD，已抓成 PoB 分享码放进 "
             "`data/pob-import/inbox/`。", "",
             "## 怎么用", "",
             "```",
             "启动服务端 → 进角色 → 聊天框输入：",
             "  /pob file <文件名>       例如 /pob file Witch.txt",
             "依次按 Y 确认 → 角色自动登出 → 重新进入角色",
             "```", "",
             "**导入会替换**这个角色的职业、等级、天赋树；**装备、宝石、药水、珠宝不动**；"
             "升华点、精通（Mastery）、星团珠宝节点不导入（详见每份的提示）。", "",
             "## 一目了然", "",
             "| 职业 | 升华 | 等级 | 榜单 | 普通天赋 | 文件 |",
             "|---|---|---|---|---|---|"]
    for b in builds:
        lines.append("| %s %s | %s | %d | 第 %d 名 | %d 点 | `%s` |" % (
            b["cn"], b["class"], b["ascendancyCn"], b["level"], b["rank"], b["points"]["ordinary"], b["file"]))
    lines.append("")
    for b in builds:
        lines.append("## %s %s · %s" % (b["cn"], b["class"], b["ascendancyCn"]))
        lines.append("")
        lines.append("- **等级**：%d　**升华**：%s（%s）　**榜单排名**：第 %d 名" % (
            b["level"], b["ascendancyCn"], b["ascendancy"], b["rank"]))
        if b["player"]:
            lines.append("- **来源玩家**：%s（%s）" % (b["player"], b["account"]))
        if b["bandit"] and b["bandit"].lower() not in ("none", "nil"):
            lines.append("- **盗贼任务**：%s" % b["bandit"])
        lines.append("- **导入文件**：`/pob file %s`" % b["file"])
        p = b["points"]
        lines.append("- **天赋点**：普通 %d，升华 %d，精通 %d，星团 %d" % (
            p["ordinary"], p["ascendancy"], p["mastery"], p["expansion"]))
        if b["keystones"]:
            lines.append("- **关键天赋**：%s" % "、".join(cn(k, CN_KEYSTONE) for k in b["keystones"]))
        if b["notables"]:
            lines.append("- **部分核心天赋**：%s" % "、".join(cn(n, CN_NOTABLE) for n in b["notables"][:8]))
        if b["skills"]:
            lines.append("- **技能组**：")
            for group in b["skills"][:6]:
                lines.append("  - 主技能 **%s** ｜ 辅助：%s" % (group[0], "、".join(group[1:]) or "无"))
        items = b["items"]
        if items:
            shown = items[:8]
            text = "、".join((("%s（%s）" % (i["name"], i["base"])) if i["name"] else i["base"]) for i in shown)
            more = "" if len(items) <= 8 else "（另有 %d 件）" % (len(items) - 8)
            lines.append("- **装备**（导出里共 %d 件，不会导入）**：%s%s" % (len(items), text, more))
        lines.append("")
    lines.append("---")
    lines.append("")
    lines.append("数据来源：`poe.qq.com/act/a202010118poena`（英雄榜公开接口），抓取脚本 `Tools/PobLadder/fetch_ladder.py`，"
                 "转换器 `Tools/PobConverter`。整套流程：英雄榜 → PoB 分享码 → 转换器 → 服务器导入。")
    return "\n".join(lines) + "\n"


def build_html(builds):
    def esc(text):
        return html.escape(str(text))

    cards = []
    for b in builds:
        points = b["points"]
        chips = "".join('<span class="chip">%s %d</span>' % (esc(label), value) for label, value in (
            ("普通", points["ordinary"]), ("升华", points["ascendancy"]),
            ("精通", points["mastery"]), ("星团", points["expansion"])) if value)
        keystones = "".join('<span class="tag gold">%s</span>' % esc(cn(k, CN_KEYSTONE)) for k in b["keystones"])
        notables = "".join('<span class="tag">%s</span>' % esc(cn(n, CN_NOTABLE)) for n in b["notables"][:8])
        rows = ""
        if b["skills"]:
            rows += "<tr><th>技能组</th><td>" + "".join(
                '<div class="skill"><b>%s</b> ← %s</div>' % (esc(g[0]), esc("、".join(g[1:]) or "无")) for g in b["skills"][:6]) + "</td></tr>"
        if b["items"]:
            shown = b["items"][:8]
            text = "、".join((("%s<i>（%s）</i>" % (esc(i["name"]), esc(i["base"]))) if i["name"] else esc(i["base"])) for i in shown)
            more = "" if len(b["items"]) <= 8 else "（另有 %d 件）" % (len(b["items"]) - 8)
            rows += '<tr><th>装备（%d 件）</th><td class="dim">%s%s　<span class="warn">不导入</span></td></tr>' % (
                len(b["items"]), text, more)
        meta = '等级 %d　·　榜单第 %d 名' % (b["level"], b["rank"])
        if b["player"]:
            meta += "　·　%s" % b["player"]
        cards.append("""
    <article class="card">
      <header>
        <h3>%s <span class="sub">%s</span></h3>
        <p class="meta">%s</p>
      </header>
      <p class="cmd">/pob file %s</p>
      <div class="chips">%s</div>
      %s
      %s
      <table>%s</table>
    </article>""" % (esc(b["cn"]), esc(b["ascendancyCn"]), esc(meta), esc(b["file"]), chips,
                     ('<div class="tags">%s</div>' % keystones) if keystones else "",
                     ('<div class="tags">%s</div>' % notables) if notables else "", rows))

    summary = "".join(
        "<tr><td>%s</td><td>%s</td><td>%d</td><td>第 %d 名</td><td>%d</td><td><code>%s</code></td></tr>" % (
            esc(b["cn"]), esc(b["ascendancyCn"]), b["level"], b["rank"], b["points"]["ordinary"], esc(b["file"]))
        for b in builds)

    return """<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>POB 模组 · 全职业 BD 库</title>
<style>
  :root { --bg:#0d0b09; --panel:#161311; --line:#3a2f1d; --gold:#d8b46a; --gold-dim:#9a7f45; --text:#e8e0d0; --dim:#9c9182; --red:#c2452f; }
  * { box-sizing: border-box; }
  body { margin:0; background:radial-gradient(circle at 50% -10%, #241d13 0%, var(--bg) 60%); color:var(--text);
         font-family:"Microsoft YaHei","PingFang SC",system-ui,sans-serif; line-height:1.65; }
  .wrap { max-width:1080px; margin:0 auto; padding:48px 24px 80px; }
  h1 { font-size:34px; letter-spacing:2px; margin:0 0 6px; color:var(--gold);
       text-shadow:0 0 18px rgba(216,180,106,.25); }
  .lead { color:var(--dim); margin:0 0 28px; }
  h2 { font-size:20px; color:var(--gold); border-left:4px solid var(--gold); padding-left:12px; margin:42px 0 16px; }
  pre { background:#0a0908; border:1px solid var(--line); border-radius:10px; padding:14px 16px; overflow:auto;
        color:#cbb98e; font-size:13px; }
  table { width:100%; border-collapse:collapse; font-size:14px; }
  th, td { border-bottom:1px solid var(--line); padding:9px 10px; text-align:left; }
  thead th { color:var(--gold-dim); font-weight:600; }
  code { background:#0a0908; border:1px solid var(--line); border-radius:6px; padding:1px 6px; color:#e2c98d; }
  .grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(320px,1fr)); gap:18px; margin-top:20px; }
  .card { background:linear-gradient(180deg,#1a1613,#12100d); border:1px solid var(--line); border-radius:14px;
          padding:18px 18px 14px; box-shadow:0 10px 26px rgba(0,0,0,.45); }
  .card header { border-bottom:1px dashed var(--line); padding-bottom:10px; margin-bottom:12px; }
  .card h3 { margin:0; font-size:19px; color:var(--gold); }
  .card h3 .sub { font-size:13px; color:var(--gold-dim); font-weight:400; margin-left:6px; }
  .meta { margin:4px 0 0; color:var(--dim); font-size:12.5px; }
  .cmd { margin:0 0 10px; font-family:Consolas,monospace; font-size:13px; color:#eadfd0;
         background:#0a0908; border:1px solid var(--line); border-radius:8px; padding:6px 10px; }
  .chips { display:flex; flex-wrap:wrap; gap:6px; margin-bottom:10px; }
  .chip { font-size:12px; color:#d9c7a0; background:#221c15; border:1px solid var(--line); border-radius:999px; padding:2px 10px; }
  .tags { display:flex; flex-wrap:wrap; gap:6px; margin-bottom:10px; }
  .tag { font-size:12px; color:#bfae94; border:1px solid var(--line); border-radius:6px; padding:2px 8px; }
  .tag.gold { color:#1a1409; background:linear-gradient(180deg,#e2c88a,#c9a75c); border-color:#8a6f33; font-weight:600; }
  .card table { font-size:12.5px; }
  .card th { width:82px; color:var(--gold-dim); font-weight:500; vertical-align:top; }
  .card td { border-bottom:none; padding:4px 6px; }
  .dim { color:var(--dim); }
  .warn { color:var(--red); font-size:11.5px; }
  .skill { margin-bottom:4px; }
  footer { margin-top:48px; color:#6f665a; font-size:12.5px; border-top:1px solid var(--line); padding-top:16px; }
</style>
</head>
<body>
<div class="wrap">
  <h1>POB 模组 · 全职业 BD 库</h1>
  <p class="lead">腾讯《流放之路》英雄榜 S29 赛季，每个职业排名最高且详情还在的一份 BD，已抓成 PoB 分享码放进
     <code>data/pob-import/inbox/</code>。</p>

  <h2>怎么用</h2>
  <pre>启动服务端 → 进角色 → 聊天框输入 /pob file &lt;文件名&gt;
  例如：/pob file Witch.txt
按 Y 确认 → 角色自动登出 → 重新进入角色

导入会替换：职业、等级、天赋树
不会改动：装备、宝石、药水、珠宝
不导入：升华点、精通（Mastery）、星团珠宝节点</pre>

  <h2>一目了然</h2>
  <table>
    <thead><tr><th>职业</th><th>升华</th><th>等级</th><th>榜单</th><th>普通天赋</th><th>文件</th></tr></thead>
    <tbody>{{SUMMARY}}</tbody>
  </table>

  <h2>每个职业一份</h2>
  <div class="grid">{{CARDS}}
  </div>

  <footer>数据来源：poe.qq.com 英雄榜公开接口　|　抓取脚本 Tools/PobLadder/fetch_ladder.py　|　转换器 Tools/PobConverter
  　|　流程：英雄榜 → PoB 分享码 → 转换器 → 服务器导入</footer>
</div>
</body>
</html>
""".replace("{{SUMMARY}}", summary).replace("{{CARDS}}", "".join(cards))


def main():
    with open(os.path.join(HERE, "builds.json"), encoding="utf-8") as handle:
        builds = json.load(handle)
    markdown, page = build_markdown(builds), build_html(builds)
    for folder in OUT_DIRS:
        if not os.path.isdir(folder):
            continue
        with open(os.path.join(folder, "BD库.md"), "w", encoding="utf-8", newline="\n") as handle:
            handle.write(markdown)
        with open(os.path.join(folder, "BD库.html"), "w", encoding="utf-8", newline="\n") as handle:
            handle.write(page)
        print("写入", folder)


if __name__ == "__main__":
    main()
