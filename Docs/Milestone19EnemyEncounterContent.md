# Milestone 19: Enemy + Encounter Content V1

M19 adds seeded encounter content on top of the M18 seeded reward branch. New runs keep the existing macro branch shape, traversal, rewards, and persistence rules, but rooms now receive deterministic encounter assignments from an encounter catalog.

## Runtime Rules
- Fresh profile-backed runs use branch id `m19_enemy_encounter_content_v1`.
- Encounter plans are deterministic from branch seed, room id, and encounter catalog id.
- Treasure rooms remain no-combat rooms and auto-clear on entry.
- Boss rooms always use the Stone Warden boss encounter.
- If a room has no encounter assignment, combat falls back to HollowRuntime V2 authored enemy spawn kinds.

## Enemy Content
- Existing enemies remain compatible: normal, flying, fast, heavy, and boss.
- New M19 enemies are `spawnEnemyCharger`, `spawnEnemyTurret`, and `spawnEnemySplitter`.
- Stone Warden uses `BossWarden` behavior with slow pursuit, charges, and a low-health projectile burst.

## Validation
- Run `Hollow/Generation/Generate Milestone 19 Assets` after content changes.
- Run `Hollow/Validation/Run Milestone 19 Validation` before handoff.
- Full EditMode and PlayMode smoke tests should remain green.
