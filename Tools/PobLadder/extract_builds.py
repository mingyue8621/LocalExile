"""Reads the seven class builds in the pob mod's inbox and writes a Chinese BD 说明 (Markdown + HTML).

Data: the inbox share codes (from 腾讯英雄榜 via fetch_ladder.py) decoded to PoB XML, the ladder's player name and rank,
and the passive table for node kinds and keystone names.
"""
import base64
import html
import json
import os
import urllib.request
import xml.etree.ElementTree as ET
import zlib

BASE = "https://poe.qq.com/act/a202010118poena"
CLASSES = ["Scion", "Marauder", "Ranger", "Witch", "Duelist", "Templar", "Shadow"]
CN_CLASS = {"Scion": "贵族", "Marauder": "野蛮人", "Ranger": "游侠", "Witch": "女巫",
            "Duelist": "决斗者", "Templar": "圣堂武僧", "Shadow": "暗影"}
CN_ASC = {"Reliquarian": "圣物使", "Chieftain": "酋长", "Deadeye": "死眼", "Elementalist": "元素使",
          "Slayer": "杀手", "Hierophant": "圣宗", "Assassin": "刺客", "Juggernaut": "永战者",
          "Berserker": "狂战士", "Necromancer": "死灵师", "Occultist": "秘术家", "Pathfinder": "追猎者",
          "Warden": "守护者", "Gladiator": "角斗士", "Champion": "冠军", "Inquisitor": "审判官",
          "Guardian": "守护者", "Saboteur": "破坏者", "Trickster": "诡术师", "Ascendant": "升华使徒"}
SNAPSHOT = "s29_normal"
INBOX = r"G:\POE1.1\data\pob-import\inbox"
TABLE = r"G:\POE1.1\data\tables\PassiveSkills.json"
HERE = os.path.dirname(os.path.abspath(__file__))


def djb2(text):
    value = 0
    for ch in text:
        value = ((value << 5) - value + ord(ch)) & 0xFFFFFFFF
    return "%08x" % value


def get(url):
    request = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.loads(response.read().decode("utf-8"))


def decode(code):
    text = code.strip()
    if text.startswith("<"):
        return text
    normalised = text.replace("-", "+").replace("_", "/").rstrip("=")
    normalised += "=" * (-len(normalised) % 4)
    return zlib.decompress(base64.b64decode(normalised)).decode("utf-8")


def ladder_owners():
    ladder = get("%s/js/rankinfo_%s.json?overview=%s" % (BASE, SNAPSHOT, SNAPSHOT))
    accounts, names = ladder["accounts"], ladder["names"]
    found = {}
    for index in range(len(accounts)):
        account, name = accounts[index], names[index]
        try:
            data = get("%s/js/char/%s/%s.json" % (BASE, SNAPSHOT, djb2(account + "_" + name)))
        except Exception:
            continue
        base = data.get("baseClass")
        if base is None or base in found or not 0 <= base < len(CLASSES):
            continue
        skills = []
        for group in data.get("skills") or []:
            gems = [gem.get("itemData", {}).get("baseType") or "" for gem in group.get("allGems") or []]
            gems = [gem for gem in gems if gem]
            if not gems or "（辅）" in gems[0] or "(辅)" in gems[0]:
                continue
            skills.append(gems)
        found[base] = {"account": account, "name": name, "rank": index + 1, "ascendancy": data.get("class"),
                       "level": data.get("level"), "bandit": data.get("banditChoice"), "skills": skills,
                       "items": [(item.get("itemData", {}).get("baseType") or "", item.get("itemData", {}).get("name") or "")
                                 for item in data.get("items") or [] if item.get("itemData")],
                       "defence": data.get("defensiveStats") or {}}
        if len(found) == len(CLASSES):
            break
    return found


def passive_table():
    kinds, keystones, names = {}, {}, {}
    notables = set()
    with open(TABLE, encoding="utf-8-sig") as handle:
        for row in json.load(handle):
            node = row.get("PassiveSkillGraphId")
            if not isinstance(node, int):
                continue
            names[node] = (row.get("Name") or "").strip()
            if row.get("IsKeystone"):
                keystones[node] = names[node]
            if row.get("IsNotable"):
                notables.add(node)
            if row.get("AscendancyKey") is not None or row.get("IsAscendancyStartingNode"):
                kinds[node] = "ascendancy"
            elif row.get("IsMastery") or row.get("IsJustIcon"):
                kinds[node] = "mastery"
            elif row.get("IsExpansion") or row.get("IsProxyPassive"):
                kinds[node] = "expansion"
            elif row.get("Characters"):
                kinds[node] = "start"
            else:
                kinds[node] = "ordinary"
    return kinds, keystones, names, notables


def items_of(root):
    result = []
    for element in root.findall("./Items/Item"):
        lines = [line.strip() for line in (element.text or "").splitlines() if line.strip()]
        if not lines:
            continue
        rarity = lines[0].replace("Rarity:", "").strip().upper()
        if rarity == "NORMAL":
            name, base = "", lines[1] if len(lines) > 1 else ""
        else:
            name = lines[1] if len(lines) > 1 else ""
            base = lines[2] if len(lines) > 2 else name
        if base.lower().startswith(("unique id", "item level", "quality", "sockets")):
            continue
        result.append({"rarity": rarity, "name": name, "base": base})
    return result


def main():
    owners = ladder_owners()
    kinds, keystones, names, notables = passive_table()
    builds = []
    for base, slug in enumerate(CLASSES):
        path = os.path.join(INBOX, slug + ".txt")
        if not os.path.exists(path):
            continue
        root = ET.fromstring(decode(open(path, encoding="utf-8-sig").read()))
        build = root.find("Build")
        nodes = [int(x) for x in (root.find("./Tree/Spec").get("nodes") or "").split(",") if x.strip().isdigit()]
        counted = {"ordinary": 0, "ascendancy": 0, "mastery": 0, "expansion": 0, "start": 0, "unknown": 0}
        for node in nodes:
            counted[kinds.get(node, "unknown")] += 1
        owner = owners.get(base, {})
        ascendancy = build.get("ascendClassName") or owner.get("ascendancy") or ""
        builds.append({
            "class": slug,
            "cn": CN_CLASS[slug],
            "ascendancy": ascendancy,
            "ascendancyCn": CN_ASC.get(ascendancy, ascendancy),
            "level": int(build.get("level") or owner.get("level") or 100),
            "bandit": build.get("bandit") or owner.get("bandit") or "",
            "player": owner.get("name", ""),
            "account": owner.get("account", ""),
            "rank": owner.get("rank", 0),
            "points": counted,
            "keystones": sorted({keystones[n] for n in nodes if n in keystones}),
            "notables": sorted({names[n] for n in nodes if n in notables and n not in keystones})[:12],
            "skills": owner.get("skills", []),
            "items": items_of(root),
            "ladderItems": owner.get("items", []),
            "file": slug + ".txt",
        })
    with open(os.path.join(HERE, "builds.json"), "w", encoding="utf-8") as handle:
        json.dump(builds, handle, ensure_ascii=False, indent=2)
    print(json.dumps([{k: b[k] for k in ("class", "ascendancy", "points", "keystones", "file")} for b in builds],
                     ensure_ascii=False, indent=1)[:2000])


if __name__ == "__main__":
    main()
