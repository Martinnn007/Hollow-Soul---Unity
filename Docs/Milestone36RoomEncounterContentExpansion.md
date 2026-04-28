# Milestone 36: Room and Encounter Content Expansion

M36 expands the current playable prototype content without changing branch rules, saves, traversal, rewards, shops, characters, weapons, or platform presentation.

## Scope

- Adds five manually curated, branch-approved HollowRuntime V2 room templates under `Assets/_Hollow/Data/Rooms/DesignerApproved/`.
- Covers every supported macro footprint: `1x1`, `2x1`, `1x2`, `2x2`, and `L`.
- Adds a successor encounter catalog under `Assets/_Hollow/Data/Encounters/M36/`.
- Keeps M13 macro fixtures as the guaranteed fallback pool.
- Keeps approved rooms additive; seeded generation can select them by matching footprint shape.

## Approved Rooms

- `approved_crossroads_single_1x1`: compact 13x7 crossroads with chasers, flying pressure, and corner rocks.
- `approved_lane_wide_2x1`: wide lane room for chargers and turret pressure.
- `approved_watchtower_tall_1x2`: tall room with watchtower-style turret and splitter anchors.
- `approved_quadrant_block_2x2`: larger arena with quadrant obstacle structure and mixed enemy anchors.
- `approved_broken_l_3cell`: L-shaped room that exercises missing-quadrant boundaries and mixed enemy placement.

## Encounter Expansion

The M36 encounter catalog adds more seed-selectable assignments while preserving the M19 behaviors:

- Origin: intro and larger crossfire variants.
- Combat: chaser skirmish, lane chargers, turret crossfire, splitter brood, and macro mixup.
- Reward: guard, watcher, and brood guard variants.
- Boss: Stone Warden remains the single boss encounter.

Treasure and secret rooms still skip combat through the existing encounter resolver.

## Validation

Run:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode --burst-disable-compilation -projectPath "$(pwd)" -executeMethod Hollow.Editor.Generation.Milestone36AssetGenerator.Generate -quit
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode --burst-disable-compilation -projectPath "$(pwd)" -executeMethod Hollow.Editor.Validation.Milestone36Validator.Validate -quit
```

Expected evidence:

- All five approved rooms import through `HollowRuntimeV2Importer`.
- The branch catalog includes the approved rooms as additional templates.
- Seeded branch generation can select approved rooms while preserving boss endpoint and single-room secret rules.
- Encounter plans remain deterministic for the same seed and vary across different seeds.
- Game scenes reference the M36 encounter catalog.
