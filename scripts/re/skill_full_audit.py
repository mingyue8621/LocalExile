"""Every player skill checked against the client's own data: animation, effect, art/behaviour variant, hit damage.

The server reads client.db (see src/GameServer.Gateway/Skill/SkillCatalog.cs). This audit applies the same lookups in
Python so a skill that would play the wrong animation, draw no effect or deal no damage shows up before the client does:

  animation   the granted effect's ActionType -> ActionTypes Id, resolved through every rig's ACT skill actions
              (data/animation-contacts.json skillActions) the way ActorAnimations.SkillActionContactSeconds does:
              own animation, an @alias into the default attack pool, or nothing at all (the swing falls back).
  effect      the ACT effect clause of that action (Metadata/Effects/... rig.ao): the only effects the client attaches
              by animation. Everything else it draws from the skill's own art rows.
  art         GrantedEffects.__col23 (skill_art_variation), checked against the SkillArtVariations row's Variants count.
  behaviour   GrantedEffects.SkillID (skill_behaviour_variation) against the GeometryAttack row it names.
  damage      attack -> weapon hit; spell -> spell_minimum/maximum_base_*; secondary -> secondary_* per HitDamageOf.
              Also lists any other minimum_base_ stat the server's HitDamageOf would not read.
"""

import json
import sqlite3
import sys
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DB = ROOT / "data" / "client.db"
CONTACTS = ROOT / "data" / "animation-contacts.json"


def ints(value):
    """A client column holds a list, a JSON list, or a bare number (GrantedEffects.StatSet1 is the set's row)."""
    if value is None:
        return []
    if isinstance(value, (list, tuple)):
        return list(value)
    if isinstance(value, int):
        return [value]
    try:
        parsed = json.loads(value)
    except Exception:
        return []
    return parsed if isinstance(parsed, list) else [parsed] if isinstance(parsed, int) else []


def pairs(a, b):
    a, b = ints(a), ints(b)
    return list(zip(a[: len(b)], b))


def load():
    db = sqlite3.connect(DB)
    db.row_factory = sqlite3.Row
    cur = db.cursor()
    return db, cur


def tables(cur):
    stats = [r["Id"] for r in cur.execute("select Id from Stats order by _row")]
    types = [r["Id"] for r in cur.execute("select Id from ActiveSkillType order by _row")]
    actions = [r["Id"] for r in cur.execute("select Id from ActionTypes order by _row")]
    active = list(cur.execute("select _row, Id, DisplayedName, ActionType, ActiveSkillTypes from ActiveSkills"))
    effects = list(cur.execute("select * from GrantedEffects"))
    per_level = defaultdict(dict)
    for r in cur.execute("select StatSet, GemLevel, FloatStats, BaseResolvedValues, "
                         "AdditionalStats, AdditionalStatsValues, GrantedEffects from GrantedEffectStatSetsPerLevel"):
        per_level[r["StatSet"]].setdefault(r["GemLevel"], r)
    sets = {r["_row"]: r for r in cur.execute("select * from GrantedEffectStatSets")}
    gems = defaultdict(list)
    for g in cur.execute("select Name, GrantedEffect, GrantedEffect2 from GemEffects"):
        for e in (g["GrantedEffect"], g["GrantedEffect2"]):
            if isinstance(e, int) and e >= 0:
                gems[e].append(g["Name"])
    art = defaultdict(list)
    for r in cur.execute("select _row, Id, Variants, ActiveSkill, GrantedEffect from SkillArtVariations"):
        for e in ints(r["GrantedEffect"]):
            art[("effect", e)].append(r)
        for a in ints(r["ActiveSkill"]):
            art[("active", a)].append(r)
    geometry = {r["_row"]: r for r in cur.execute("select _row, Id, Animation, HitEffect from GeometryAttack")}
    return stats, types, actions, active, effects, per_level, sets, gems, art, geometry


def stats_of(effect, per_level, sets):
    """Level-1 stats of the effect's first stat set, the way Tables.StatSetLevelStats builds them."""
    set_row = ints(effect["StatSet1"])
    if not set_row:
        return {}
    set_idx = set_row[0]
    out = {}
    base = sets.get(set_idx)
    if base is not None:
        for s in ints(base["ImplicitStats"]):
            out[s] = 1
        for s, v in pairs(base["ConstantStats"], base["ConstantStatsValues"]):
            out[s] = v
    row = per_level.get(set_idx, {}).get(1)
    if row is not None:
        for s, v in pairs(row["FloatStats"], row["BaseResolvedValues"]):
            out[s] = out.get(s, 0) + v
        for s, v in pairs(row["AdditionalStats"], row["AdditionalStatsValues"]):
            out[s] = out.get(s, 0) + v
    return out


def resolve(rig, stance, action, depth=0):
    block = rig.get(stance)
    if block is None or depth > 12:
        return None
    if action in block["actions"]:
        return block["actions"][action]
    parent = block.get("parent")
    return resolve(rig, parent, action, depth + 1) if parent and parent != "@root" else None


def action_kind(entry):
    if entry is None:
        return "absent"
    segments = entry.get("segments") or []
    if not segments or segments[0]["animation"].startswith("@"):
        return "alias"
    return "own"


def main():
    db, cur = load()
    stats, types, actions, active, effects, per_level, sets, gems, art, geometry = tables(cur)
    contacts = json.loads(CONTACTS.read_text(encoding="utf-8"))

    rows = []
    for e, row in enumerate(effects):
        if row["IsSupport"] or e not in gems:
            continue
        active_row = active[row["ActiveSkill"]] if isinstance(row["ActiveSkill"], int) and 0 <= row["ActiveSkill"] < len(active) else None
        labels = [types[t] for t in ints(active_row["ActiveSkillTypes"]) if 0 <= t < len(types)] if active_row else []
        action = None
        if active_row and isinstance(active_row["ActionType"], int) and 0 <= active_row["ActionType"] < len(actions):
            action = actions[active_row["ActionType"]]
        elif isinstance(row["Animation"], str) and row["Animation"]:
            action = row["Animation"]

        # Animation: best coverage across every rig/class, and how many rigs know an own animation for it.
        kinds, own_rigs, aliases, effects_seen = Counter(), [], set(), set()
        for rig_path, rig in contacts.items():
            for stance in (rig.get("skillActions") or {}):
                entry = resolve(rig["skillActions"], stance, action) if action else None
                kind = action_kind(entry)
                kinds[kind] += 1
                if kind == "own":
                    own_rigs.append(f"{rig_path.split('/')[-1].replace('.ao', '')}/{stance}")
                elif kind == "alias" and entry and entry.get("segments"):
                    aliases.add(entry["segments"][0]["animation"])
                if entry:
                    for eff in entry.get("effects") or []:
                        effects_seen.add(eff)

        # Art variant.
        art_value = row["__col23"] or 0
        art_rows = art.get(("effect", e)) or art.get(("active", row["ActiveSkill"])) or []
        art_counts = [len(ints(r["Variants"])) for r in art_rows]
        art_ok = art_value <= 0 or any(art_value < c for c in art_counts)

        # Behaviour variant.
        behaviour = row["SkillID"] or 0
        geometry_row = geometry.get(behaviour) if behaviour else None

        # Damage.
        name_of_stat = lambda i: stats[i] if 0 <= i < len(stats) else f"#{i}"
        own = {name_of_stat(k): v for k, v in stats_of(row, per_level, sets).items()}
        prefixes = sorted({k.split("minimum_base_")[0] for k in own if "minimum_base_" in k})
        spell_base = any(k.startswith(("spell_minimum_base_", "minimum_base_")) and "per_removable" not in k for k in own)
        secondary_base = any(k.startswith("secondary_minimum_base_") for k in own)
        per_charge = any("per_removable" in k and "minimum_base_" in k for k in own)
        attack = "Attack" in labels
        spell = "Spell" in labels
        if attack:
            damage = "attack"
        elif spell_base:
            damage = "spell"
        elif secondary_base or per_charge:
            damage = "secondary"
        else:
            damage = "none"
        unread = [p for p in prefixes if p not in ("spell_", "secondary_", "")]

        rows.append(dict(
            effect=e, id=row["Id"], names=sorted(set(n.split(" of ")[0] for n in gems[e]))[:3],
            action=action, labels=labels, kinds=dict(kinds), own_rigs=own_rigs,
            aliases=sorted(aliases), effects=sorted(effects_seen),
            art=art_value, art_counts=art_counts, art_ok=art_ok,
            behaviour=behaviour, geometry=bool(geometry_row),
            geometry_animation=geometry_row["Animation"] if geometry_row else None,
            geometry_hit_effect=geometry_row["HitEffect"] if geometry_row else None,
            damage=damage, prefixes=prefixes, unread_prefixes=unread,
        ))

    print(f"player effects: {len(rows)}  (gems {len(gems)})")
    print("labels:", Counter(tuple(r["labels"]) for r in rows).most_common(8))
    print("damage:", Counter(r["damage"] for r in rows))
    print("animation coverage:", Counter(("own" if r["kinds"].get("own") else "alias" if r["kinds"].get("alias") else "absent") for r in rows))
    print("ACT effects:", sum(1 for r in rows if r["effects"]), "skills")
    for r in rows:
        if r["effects"]:
            print("   ", r["names"][0] if r["names"] else r["id"], r["action"], r["effects"])
    print("art variants set:", sum(1 for r in rows if r["art"]), "invalid:", [r["names"][:1] for r in rows if r["art"] and not r["art_ok"]])
    print("behaviour set:", sum(1 for r in rows if r["behaviour"]), "with GeometryAttack row:",
          sum(1 for r in rows if r["behaviour"] and r["geometry"]))
    print("unread damage stat prefixes:")
    for r in rows:
        if r["unread_prefixes"]:
            print("   ", r["names"][:2], r["action"], r["unread_prefixes"], [p for p in r["prefixes"]])
    print("no damage (non-attack):", sum(1 for r in rows if r["damage"] == "none"))
    out = ROOT / "artifacts" / "re" / "skill-full-audit.json"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(rows, ensure_ascii=False, indent=1), encoding="utf-8")
    print("wrote", out)


if __name__ == "__main__":
    main()
