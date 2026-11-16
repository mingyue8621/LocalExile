"""Fetches the top-ranked build of every class from 腾讯流放之路英雄榜 and writes each one's PoB share code
into the pob mod's inbox, so the game command /pob file <Class>.txt can import it.

The ladder pages are public and need no login:
  rankinfo   https://poe.qq.com/act/a202010118poena/js/rankinfo_<snapshot>.json?overview=<snapshot>
  character  https://poe.qq.com/act/a202010118poena/js/char/<snapshot>/<djb2(account_name)>.json
The character record carries pathOfBuildingExport, a PoB share code — exactly what the pob mod's converter eats.

Usage: python fetch_ladder.py [snapshot] [inbox folder]
  python fetch_ladder.py s29_normal G:\\POE1.1\\data\\pob-import\\inbox
"""
import json
import os
import sys
import urllib.request

BASE = "https://poe.qq.com/act/a202010118poena"
CLASSES = ["Scion", "Marauder", "Ranger", "Witch", "Duelist", "Templar", "Shadow"]
HEADERS = {"User-Agent": "Mozilla/5.0"}


def djb2(text):
    value = 0
    for ch in text:
        value = ((value << 5) - value + ord(ch)) & 0xFFFFFFFF
    return "%08x" % value


def get(url):
    request = urllib.request.Request(url, headers=HEADERS)
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.loads(response.read().decode("utf-8"))


def main():
    snapshot = sys.argv[1] if len(sys.argv) > 1 else "s29_normal"
    inbox = sys.argv[2] if len(sys.argv) > 2 else r"G:\POE1.1\data\pob-import\inbox"
    ladder = get("%s/js/rankinfo_%s.json?overview=%s" % (BASE, snapshot, snapshot))
    accounts, names = ladder["accounts"], ladder["names"]
    os.makedirs(inbox, exist_ok=True)
    found = {}
    for index in range(len(accounts)):
        account, name = accounts[index], names[index]
        try:
            character = get("%s/js/char/%s/%s.json" % (BASE, snapshot, djb2(account + "_" + name)))
        except Exception as error:
            print("跳过 %s: %s" % (name, error))
            continue
        base = character.get("baseClass")
        if base is None or base in found or base < 0 or base >= len(CLASSES):
            continue
        code = character.get("pathOfBuildingExport")
        if not code:
            continue
        found[base] = (name, character.get("class"), character.get("level"), code)
        print("%-9s %-18s %s 级  %s  (第 %d 名)" % (CLASSES[base], character.get("class"), character.get("level"),
                                                    name, index + 1))
        if len(found) == len(CLASSES):
            break
    for base, (name, ascendancy, level, code) in sorted(found.items()):
        path = os.path.join(inbox, CLASSES[base] + ".txt")
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(code.strip())
        print("写入 %s  %s（%s，%s 级，%d 字符）" % (path, CLASSES[base], ascendancy, level, len(code)))


if __name__ == "__main__":
    main()
