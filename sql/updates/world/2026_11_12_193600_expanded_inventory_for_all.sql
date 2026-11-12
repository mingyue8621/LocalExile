-- The client's own 4x5 side backpack (inventory row 84) for every character, not only the Primalist ascendancy's
-- "Might of the Bear" (primalist_ascendancy_expanded_main_inventory, Stats row 19295). 0 restores the ascendancy-only
-- behaviour. Operators can change the value in system_settings without a new release.
INSERT INTO system_settings (key, value) VALUES ('expanded_inventory_for_all', 1.0);
