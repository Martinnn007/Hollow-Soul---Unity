# Milestone 48: Content Expansion Lock V1

M48 locks the first larger playable content pool without adding new mechanics. It adds five approved Room Designer-compatible rooms, mirrors them into the curated Room Designer library, extends the M46 encounter catalog with eight new encounter templates, and wires normal runs plus M47 challenges to the successor content catalog.

## Added Approved Rooms

- `approved_cover_arena_single_1x1`: 1x1 cover arena with light spike pressure and standard barrels.
- `approved_pressure_lane_wide_2x1`: 2x1 horizontal lane with charger anchors, barrels, pits, and spikes.
- `approved_turret_spire_tall_1x2`: 1x2 vertical turret room with pit/spike routing and flying anchors.
- `approved_hazard_quadrant_block_2x2`: 2x2 macro room with mixed hazards and explosive barrel chain opportunities.
- `approved_ambush_l_3cell`: L-room ambush with traversal gaps, splitter pressure, charger pressure, and barrels.

All five rooms are generated through Room Designer data and exported as HollowRuntime V2 JSON under `Assets/_Hollow/Data/Rooms/DesignerApproved/`.

## Encounter Catalog

The M48 encounter catalog is a successor to M46. It keeps all M46 templates, keeps Stone Warden as the boss encounter, and adds:

- `m48_cover_scramble`
- `m48_lane_pursuit`
- `m48_turret_spire`
- `m48_splitter_pit`
- `m48_barrel_chain`
- `m48_reward_hazard_guard`
- `m48_world2_pressure_mix`
- `m48_world3_hazard_macro`

The new templates use only existing enemy spawn kinds and keep the six-enemy non-boss cap.

## Runtime Wiring

M48 does not create a new branch identity. Normal runs and M47 challenges continue through `m46_encounter_director_curve_v1`, but scenes reference the M48 successor encounter catalog and the refreshed approved room pool.

Starter/origin/prologue safety remains unchanged: starter rooms stay free of enemies, rewards, and hazards.

## Validation

Run:

```bash
Unity -batchmode -quit -projectPath "<project>" -executeMethod Hollow.Editor.Generation.Milestone48AssetGenerator.Generate
Unity -batchmode -quit -projectPath "<project>" -executeMethod Hollow.Editor.Validation.Milestone48Validator.Validate
```

Expected outputs:

- `output/reports/m48_content_expansion_lock_v1.json`
- `output/reports/m48_content_expansion_lock_v1.md`
- `output/pdf/Hollow_M48_Content_Expansion_Lock_V1.pdf`

The PDF is a team handoff document for Martin and Rafal, not a public pitch deck.
