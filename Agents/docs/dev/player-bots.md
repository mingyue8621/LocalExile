# Player bots (the PlayerBots module)

`Modules/PlayerBots/` makes a new server look lived in: server-driven characters that are created over time, play
through the campaign, visit the towns and leave again. Off by default: no league lists it.

## Turning it on

List it in a league, as `data/league-modules.json` does with
`{"Standard": {"add": ["PlayerBots"]}}` ([modules](modules.md)), then restart the Gateway. Its settings are in
`settings.json` beside it, and a user's values in `data/modules/PlayerBots.json` ([modding](modding.md), "Settings"); a
change reloads the module, so the bots start over from `state/bots.json`.

Each setting's default, limits and meaning are in its `settings.json`.

## What a bot is

- **An account and a character** in the save database: account `<name>_bot`, a random Russian or English character name,
  a random class. Its progress (level, last town) is written to the character record.
- **A body in towns.** A bot is a published player without a connection (`InstancePlayer` with no owner) of every live
  instance of its town (`Program.InGame` lists the connections in game). Each client there replicates it as it
  replicates another player (`LivingCollision.SyncInstancePlayers`): created with its class model and gear, walking by
  the Move action (0x1ab, skill 0x2909) toward its destination, which the client paths and animates.
  It is only shown where a player is. Party followers instead use controlled player connections and the ordinary combat pipeline.
- **Gear** (`BotGear`): real base items of the game data for its class and level (weapon by class, armour by the class's
  attributes, a belt), drawn as the server draws a player's equipment. Party builds include connected passives and compatible linked gems.
- **The route**: `story-main.md` (the main story, 52 quests) and `story-side.md` (side quests, 30), act by act, main
  quests first. Both are tables the module reads; the order is the campaign driver's (`scripts/dev/campaign/order.py`).
  A quest is simulated out of sight for `questMinutes`; the zone, kills, deaths and gear changes are logged.
- **Town visits**: after a quest, a quick visit (out of its own portal by an NPC, hands the quest in, leaves by portal)
  or a stay (by the stash and the guild stash, a few things to do). Towns maintain two to five residents when the configured
  global bot limit permits; a bot that finds no room goes on to its next quest. Places to stand are walkable and 20 cells apart.
- **The end**: after every quest of both guides the character and its account are deleted, and a new bot follows.

A restart resumes every bot where it was (`state/bots.json`: phase, town, place, target, time left, gear). A bot account
carries a mark in place of its password hash (`PlayerBots:`, which no base64 hash holds); a marked account the file
does not know is deleted, and no other account ever is, whatever its name.

## Files

| Path | What |
|---|---|
| `logs/bots-<date>.log` | one line per event: `date time \| account \| character \| what` (errors too) |
| `logs/status.txt` | every bot, its level, act and what it is doing, rewritten every few seconds |
| `state/bots.json` | the bots' state for a restart (local, not committed) |
| `economy/<league>/` | a poe.ninja snapshot (prices in chaos, divine, alchemy, regal; exchange turnover and daily history) for trading bots, made locally by `tools/collect_economy.py` (not committed) |
| `tools/trace-stance.js` | Frida: which animation set the client picks per actor (`0x141e9da30`) |

## Client findings

- **Walking**: a glide (0x1ba MoveTo) slides a standing body; a Move action (0x1ab) makes the client walk it.
- **Stance**: the client picks an actor's animation set (`FUN_141e9da30`: `"Town"` in a town or with `use_town_stance`,
  else the weapon's) when its Inventories component changes, not when the object is created (Frida, 2026-11-01).
  An observer therefore gets the peer's equipment (0x2bc) right after creating it, for the town stance (not yet verified live).
- **Party kick**: the native member-menu action sends C→S `0x94` with a compact wide string containing the selected
  account (`0x140ab8ed0`), not `0x97` (promote leader). Confirmed through the actual party portrait menu.

## Level and gear of a party bot

A bot in town is a published body with the gear `BotGear` picked for its class and level (`Template`). A party follower is
a controlled player instead, so before this it carried no items at all: `WeaponFor` fell back to the class's unarmed
skill (its hits were fists) and every other client drew it bare.

- **Level**: when the follower connection is made, its level becomes the level of the area's monsters (the median of the
  living ones, ±2), and only where monsters live — a town or a hideout leaves it alone. A change of three levels or more
  re-picks its gear and saves the character.
- **Gear**: `ControlledPlayers.Gear` (filled by the PlayerBots module with `BotGear`) names base item ids; the game module
  puts them on the character as real items at that level (`ControlledEquipment.EquipControlledGear`), replacing whatever
  the rows held, one row per piece (a dual wielder's second one-hander takes the off hand). The model, the weapon's damage
  and the armour then all come from the items.
  The pick also checks attribute requirements: `ControlledPlayers.GearFitsClass` answers with the strength, dexterity and
  intelligence the class's own allocation reaches at that level (`ControlledBuilds.ControlledAttributes`, cached per class
  and level), so a base whose `ComponentAttributeRequirements` exceed them is skipped rather than worn with "requirements
  not met".
- **Cosmetics** (`BotCosmetics`): every worn item gets a skin whose own `ItemClasses` name that item's class — armour by
  its row, a bow only for a bow. The pick is stable per character and item (an FNV hash of the name and the base id), so a
  bot keeps its look. A party follower takes its skins through the worn table the client's model is built from
  (`Equipment.Looks`: `SkinVisual` + the hash); a town body, which has no connection, asks for one skin per item through
  `ControlledPlayers.BotSkin` and draws it in its own model entries. The class → candidates table and the picks are cached,
  since a town body is republished every tick.

## Party combat and temperament

A follower runs on a real controlled-player connection (`ControlledPlayers.StepControlledPlayer`) and decides three
times a second. What follows is its own judgement rather than an order from the leader (`ControlledMind`,
`ControlledCombat`): five followers used to be five copies of one script.

- **Who it is**: a temperament grown from an FNV-1a hash of its name, so the same bot is the same person in every area
  and session. *Charge* (55–87 cells from the leader), *caution* (drinks at 20–40% of its life, gives ground sooner),
  *loyalty* (how readily it turns on what is on the player's back) and *talk* (whether it says anything).
- **What to hit**: the hostile worth most — rarity first, then one on the leader's back (`+45..75` by loyalty) or on its
  own (`+20`), then the nearest — inside 80 cells of itself and inside the leader's range. Invulnerable, asleep and dead
  bodies are skipped. A plain monster another bot is already fighting is passed over while another target exists; rares
  and uniques are shared.
- **With what**: the gems that would actually hit (`SkillHitPlan.CanHit`, not a disabled gem instance), in socket order,
  keeping the ones whose reach covers the distance (`ControlledInPlaceReach`: weapon range for a melee skill, its radius
  for a self-targeted one, 80 for a projectile). An area gem goes first when three monsters stand within 15 cells of the
  target. A gem on cooldown hands the turn to the next one instead of standing still on it.
- **Where it stands**: in the trail behind the leader, 9, 13 or 17 cells back (each bot keeps its own rank, so the queue is
  a column rather than a rank), and never inside the corridor the leader is walking down (26 cells ahead, 5 to either
  side). A follower that ends up ahead of a moving leader steps 7 cells off the road and waits there instead of standing
  in it: another player's body is a blocker the leader's own pathfinder has to walk round.
- **Where it walks to fight**: when nothing reaches, it walks to just inside the longest reach, on walkable ground and
  inside the party (`ControlledMoveFits`), instead of waiting for the monster to come to it. While it has a target the
  leash is the engage one (90) and it holds its ground instead of running to the leader's formation cell.
- **When to drink**: at its own line a life flask, and a mana flask once a fifth of the unreserved mana is left.
- **Giving ground**: a bot that fights from a distance (reach ≥ 25) steps back out of an enemy that got inside 6–14 cells
  of it, and further back under its life line. A melee bot stands and drinks instead.
- **Falling behind**: beyond the leash (60, or 90 in a fight) it first runs after the leader, and only blinks back when
  the gap stops closing for 1.5 s (the leader keeps running at the same speed, or the way is blocked). A leader that stops
  or turns is caught on foot.
- **Saying something**: a line in the area's local chat, at most one a minute per bot: a rare it meets, the player dropping
  under 30% life, and — from a talkative one — something while nothing else happens.

It still cannot pick a flask for its resists or use a movement skill to close a gap.

## Not done yet

Party combat uses the ordinary player pipeline; offscreen quests, loot and crafting remain simulated.
Trading, chat with other players and advanced tactical/ascendancy builds remain open. Current implementation status is in
[NPCs and chat](../../status/npcs-and-chat.md).

## External party control

The optional authenticated MCP endpoint exposes per-bot observations and leased tactical orders.
See [connection, tools and fallback rules](../../../Modules/PlayerBots/MCP.md).
