-- MapWorldsColosseum and MapWorldsMarshes were the only two map areas the Gateway did not generate itself
-- (every other one of the 489 map areas is in server_level_areas with generated = 1). A map area outside it gets a
-- random seed from Auth and the Gateway passes that seed on unchecked, so the client is handed a layout it cannot
-- build and stops with "Dungeon generation failed: No suitable room found for room at (1, 10) with type
-- cage_turn" (seed 519077920, live 2026-11-25 09:47); seed 1 fails too, at (4, 3), which is why the area was
-- left out. Both areas do generate: about a third of their seeds cannot be laid out, the rest build in 0.2-1.6 s.
--
-- With these rows, ServerLevels.PlayableSeed checks the instance's seed and plays the first of its Replacements the
-- port can generate (seed 519077920 becomes 1670335246: 3335x2093 walk grid, 9 exits), so the client never gets a
-- seed it fails on, and 0x1d still carries the replaced seed the client builds.
INSERT INTO server_level_areas (area_id, generated, seed_in_place_of_one) VALUES ('MapWorldsColosseum', 1, NULL)
ON CONFLICT(area_id) DO UPDATE SET generated = 1, seed_in_place_of_one = NULL;

INSERT INTO server_level_areas (area_id, generated, seed_in_place_of_one) VALUES ('MapWorldsMarshes', 1, NULL)
ON CONFLICT(area_id) DO UPDATE SET generated = 1, seed_in_place_of_one = NULL;
