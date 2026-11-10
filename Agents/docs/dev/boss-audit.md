# Boss audit: every campaign boss, from the client's own data

`GameServer.Gateway values bosses` prints one JSON line per campaign unique boss — an area's `WorldAreas
Bosses_MonsterVarietiesKeys` whose Id starts with a digit (the campaign; maps and hideouts do not). It runs without
starting a server, reads the same catalogs a live spawn does, and answers the two questions a boss fight asks:

- **Where does it stand?** The client's own boss points (`data/room-points.json`: `mapboss`, `mapboss<n>`,
  `mapboss<n>&<m>`, `force:mapboss`, `mapbosspack`, with a `WG` suffix for Prisoner's Gate / The Oasis), fitted to the
  area's layout at **seed 1** the way `AreaPopulation.FitRooms` fits a live area, and the point `BossStanding` answers
  for that boss (the same call the quest spawn uses).
- **What does it do?** `MavenNucleus.CombatEffect`'s pick from the variety's granted effects, whether that reads as a
  ranged attack (`MonsterAttackIsRanged`, with the shared "Melee" row's ranged types dropped when it has no projectile
  art), the projectile art the server flies, the cast and swing times, the action id the client is given, and whether the
  client can be given it at all (`FindByRow`).

Fields, in order: `area`, `level` (the area's `AreaLevel`), `place` (which of the area's bosses), `row` (the
MonsterVarieties row), `supported` (the client record can be built — `MonsterObject.CanSerialize`), `spawn` (`quest` when
the area leaves the boss out and a quest brings it: `Quests.QuestSpawned`), `point` (the fitted cell `BossStanding`
answers, or `none`), `fitted` (every boss point the seed-1 fit found), `declared` (every boss point the area's candidate
rooms carry, before fitting), `level1` (whether the Gateway generates that area at seed 1 at all), `notes` (the fit's
notes: which room landed where, or why it did not), `effect`, `ranged`, `shot`, `cast`, `swing`, `skill` (the 0x1ab
action id), `built`, `bossbar`, `reach` (cells).

## Reading it

- `spawn=quest point=<cell>`: the quest that brings the boss creates it there (`QuestEngine.QuestSpawned`'s
  spawn path, `AreaPopulation.BossStanding`). The Cathedral Rooftop's Kitava is the case this was written for: his
  `force:mapboss` is 100 cells past the Sign of Purity's socket he used to appear on (live 2026-11-10).
- `point=none` with `spawn=area` is not always a fault: the boss may come with a `%area%-<pack>` point (the pack's own
  `BossMonster_MonsterVarietiesKeys`, e.g. the Imperial Gardens' `1_3_15-bosspack` carries SpikerBoss), stand at a spawner
  object (`AreaPopulation.BossSpawners`, The Quay's Tolman), or belong to a Labyrinth trial (`Aspirant's Trial`, whose
  bosses Izaro's rooms place).
- `level1=no` (no generated level at seed 1) means `fitted` is empty because the audit could not lay the area out, not
  because the area has no points: those areas fit at the seed a live instance uses.
- `built=true` everywhere is the expected state; a `false` would mean the effect has no stat set and the client would be
  handed an action it cannot play.

## When a boss still misbehaves

The Gateway logs one line per boss spawn with the same facts it acted on:

```
[gateway] <remote> boss Kitava, the Insatiable (Metadata/Monsters/KitavaBoss/Kitava1, row 5747)@45: effect 6598
(KitavaSlamMiddle), skill 0x249e built, reach 6, ranged False, projectile none, cast False s, swing 4.511 s
```

So a boss that stands still is read off that line: `skill not built` (the client cannot be given the action), `ranged
True` with `projectile none` (it will hold the far reach and swing at nothing), or a `reach` far larger than the arena
(the variety's `AttackRange`/agro row). And `area <id> monsters: no room point for unique boss(es) …` says the layout put
no boss point for it, which is a room-placement question, not a skill one.
