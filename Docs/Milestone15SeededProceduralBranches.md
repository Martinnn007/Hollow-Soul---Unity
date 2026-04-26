# Milestone 15: Seeded Procedural Macro Branches

M15 replaces the fixed M14 macro cross for new runs with a seeded eight-room macro branch. It still uses the M13 fixture pool only, but placement is now generated from a deterministic seed, rooms connect through exact door port IDs, and one farthest leaf becomes the first boss room.

## Runtime Behavior

- New profile-backed runs use `m15_seeded_macro_branch_v1`.
- Continue restores M15 runs by regenerating the branch from `branchSeed` and restoring the saved procedural reward plan.
- M14 and M7 active-run snapshots continue through their legacy graph paths.
- The generated branch is a tree with eight logical rooms: `origin`, `room_01` through `room_06`, and `boss_01`.
- All generated connections are explicit port-to-port links; direction-only traversal remains a legacy fallback.

## Rewards And Boss

- Every generated non-origin, non-boss room gets one deterministic reward from the prototype pool: Stone Heart, Quick Draw, Fleet Step, and Ember Charm.
- `boss_01` grants `25 souls + Boss Sigil`.
- The boss room suppresses normal room spawns and creates one `spawnEnemyBoss` Stone Warden using existing enemy movement and damage systems.
- Souls still bank only after all rooms are cleared, all rewards are claimed, and the hub portal is used.

## Generated Assets

Run `Hollow/Generation/Generate Milestone 15 Assets`.

This calls the M14 generator chain, creates `BranchGenerationSettings_M15.asset`, adds `Enemy_Boss.asset`, updates `EnemyCatalog.asset`, and wires all three game scenes to the M15 settings.

## Validation

Run `Hollow/Validation/Run Milestone 15 Validation`.

The validator checks settings, scene wiring, boss enemy data, deterministic seeded generation, explicit port connections, no occupied-cell overlap, one boss leaf, and procedural reward-plan health.
