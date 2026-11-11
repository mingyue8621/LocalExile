-- Every monster hit that comes from a shot (a bow's arrow, a spell's projectile: MonsterAttack.Ranged or .Projectile) is
-- scaled by this: SystemSettings.MonsterRangedDamageMultiplier, applied in Monsters.CommitMonsterHitLocked. Melee swings
-- keep their damage, and a spell is never evaded, so the shots alone decided Act 5-6 fights: the client's own rows give a
-- 45-50 archer 200-400 a shot against a character of that act's ~1,600 life (live 2026-11-11).
INSERT OR REPLACE INTO system_settings (key, value) VALUES ('monster_ranged_damage_multiplier', 0.75);
