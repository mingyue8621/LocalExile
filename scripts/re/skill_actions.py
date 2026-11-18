"""
每个职业 ACT（Characters.dat ACTFile）里**技能动作**的动画段：客户端播放一个技能时用的动画序列，
按 stance（手部武器组合）分别定义（`HeavyStrike = "bash1_mace";`、`GlacialHammer = "attack_maul_01a:attack_time=0.6"
then "attack_maul_01b:attack_time=0.8" then "bash1_mace" effect "Metadata/Effects/.../rig.ao" "Str_bash1_maul";`）。
默认攻击的 stance 池由 anim_contacts.default_attack_pools 读取；这里是其余动作（技能、引导、投掷……）。

一个动作的值是若干段（`then` 分隔，可以另起一行）的动画名（rig 的 clip 或 <Class>NoWind.ao 的 view），
每段可带 `attack_time=<秒>` 预算与 `item_animation` / `with_main_hand` / `OffHand` 标志，后面可跟任意个
`effect "<ao 路径>" "<effect id>"` 附加特效。`@Name` 引用同 stance 的另一个动作（`@Melee` 是默认攻击池）。

各 stance 继承其父块的动作：ACT 的 stance 是嵌套块（OneHandSwordShield 在 OneHandSword 里），子块没有的动作
沿用外层。表里每个 stance 只存**本块显式声明**的动作和父 stance 名，服务端沿父链查找，与客户端一致。
"""
import re

# 与 anim_contacts.act_tokens / closing_brace 同一套记号化（引号内的注释与括号不参与嵌套），
# 这里自带一份以免模块循环导入。
TOKENS = re.compile(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"|[A-Za-z_][A-Za-z_0-9]*|[^\s]', re.S)


def tokens(text):
    return [m.group(0) for m in TOKENS.finditer(text) if not m.group(0).startswith(("//", "/*"))]


def closing_brace(tokens_, start):
    depth = 1
    for i in range(start + 1, len(tokens_)):
        depth += (tokens_[i] == "{") - (tokens_[i] == "}")
        if depth == 0:
            return i
    raise ValueError("Unclosed ACT block")


def parse_value(expr):
    """一个动作值 -> (段 [(name, options dict)], 特效路径 [str])。"""
    segments, effects = [], []
    i = 0
    while i < len(expr):
        token = expr[i]
        if token == "then":
            i += 1
            continue
        if token == "effect":
            if i + 1 < len(expr) and expr[i + 1].startswith('"'):
                effects.append(expr[i + 1][1:-1])
            i += 3
            continue
        if token.startswith('"'):
            body = token[1:-1]
            name, _, options = body.partition(":")
            values = {}
            for option in options.split(":"):
                key, _, value = option.partition("=")
                if key:
                    values[key] = value
            segments.append((name, values))
        i += 1
    return segments, effects


def parse_block(body, stance, parent, out):
    """把一个块的动作写进 out[stance] = (父 stance, {action: 值})：只记本块显式声明的。"""
    actions = {}
    i = 0
    while i < len(body):
        token = body[i]
        if token == "stance" and body[i + 2:i + 3] == ["{"]:
            end = closing_brace(body, i + 2)
            parse_block(body[i + 3:end], body[i + 1], stance, out)
            i = end + 1
            continue
        if token == "{":
            i = closing_brace(body, i) + 1
            continue
        if i + 1 < len(body) and body[i + 1] == "=":
            end = i + 2
            while end < len(body) and body[end] != ";":
                end += 1
            expr = body[i + 2:end]
            # 一个动作的后续段另起一行："then "clip2:attack_time=0.3";"（GlacialHammer 的三段就是这样写的）。
            while end + 1 < len(body) and body[end + 1] == "then":
                following = end + 1
                stop = following + 1
                while stop < len(body) and body[stop] != ";":
                    stop += 1
                expr = expr + body[following:stop]
                end = stop
            if expr and expr[0].startswith('"'):
                actions[token] = parse_value(expr)
            i = end + 1
            continue
        i += 1
    out[stance] = (parent, actions)


def act_actions(text):
    """{stance: (父 stance, {action: (段, 特效)})}，含 @root（没有 stance 的动作的默认值）。"""
    tokens_ = tokens(text)
    start = tokens_.index("{")
    out = {}
    parse_block(tokens_[start + 1:closing_brace(tokens_, start)], "@root", None, out)
    return out


def lookup(raw, stance, action):
    """(拥有该动作的 stance, 动作值)：沿父链找，与客户端一样由近及远；没有时 (None, None)。"""
    seen = set()
    while stance is not None and stance not in seen:
        seen.add(stance)
        parent, actions = raw.get(stance, (None, {}))
        if action in actions:
            return stance, actions[action]
        stance = parent
    return None, None


def resolve(raw, stance, action, seen=None):
    """一个动作的段与特效，沿 @Name 引用解开；None 表示客户端沿用默认攻击（@Melee）或没有这个动作。"""
    seen = seen or set()
    owner, entry = lookup(raw, stance, action)
    if entry is None:
        return None
    segments, effects = entry
    if not segments:
        return None
    if len(segments) == 1 and segments[0][0].startswith("@"):
        target = segments[0][0][1:]
        if target == "Melee" or (owner, target) in seen:
            return None
        seen.add((owner, target))
        return resolve(raw, owner, target, seen)
    return segments, effects


def animation_entry(name, options, budget, clips, views):
    """一个动画名 -> 条目（与默认攻击的池条目同形）；视图先在 .ao 里找，找不到再把名字当 rig 的 clip。"""
    if name in views:
        source, begin, end = views[name]
    elif name in clips:
        source, begin, end = name, 0, None
    else:
        return None
    if source not in clips:
        return None
    index, clip = clips[source]
    end = clip["length"] if end is None else end
    if not 0 <= begin < end <= clip["length"]:
        return None
    events = [(e, t, p) for e, t, p in clip["events"] if begin <= t < end]
    hits = [(t, p) for e, t, p in events if e == "contact"]
    contact, position = (min(hits, key=lambda h: h[0]) if hits else (None, None))
    slow_begin = next((t for e, t, _ in events if e == "slow_begin"), None)
    slow_end = next((t for e, t, _ in events if e == "slow_end"), None)
    cancel = next((t for e, t, _ in events if e == "lock_cancelling"), None)
    return {"animation": name, "source": source, "index": index, "begin": begin, "length": end - begin,
            "contact": None if contact is None else contact - begin,
            "cancel": None if cancel is None else cancel - begin,
            "slow": [slow_begin - begin, slow_end - begin] if slow_begin is not None and slow_end is not None else None,
            "budget": budget, "item": "item_animation" in options, "pair": False, "position": position}


def skill_actions(text, animations, views):
    """{stance: {"parent": 父 stance 或 None, "actions": {action: {"segments": [条目], "effects": [...]}}}}

    只含本块显式声明的动作；Melee（默认攻击池，anim_contacts.default_attack_pools 读取）与只有 @Melee 引用的
    动作跳过——它们按当前武器 stance 的默认攻击播放，服务端用 defaultAttack 的池算。"""
    raw = act_actions(text)
    clips = {a["name"]: (i, a) for i, a in enumerate(animations)}
    out = {}
    for stance, (parent, actions) in raw.items():
        explicit = {}
        for action in actions:
            if action in ("Melee", "MeleeWithStep", "OffMelee"):
                continue
            resolved = resolve(raw, stance, action)
            if resolved is None:
                continue
            body, effects = resolved
            entries, ok = [], True
            for name, options in body:
                budget = float(options["attack_time"]) if "attack_time" in options else None
                entry = animation_entry(name, options, budget, clips, views)
                if entry is None:
                    ok = False
                    break
                entries.append(entry)
            if not ok or not entries:
                continue
            explicit[action] = {"segments": entries, "effects": effects}
        out[stance] = {"parent": parent, "actions": explicit}
    return out
