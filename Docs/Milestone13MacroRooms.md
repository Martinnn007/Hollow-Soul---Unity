# Milestone 13: Macro-Room Runtime Foundations

M13 adds macro-room foundations without changing the current playable five-room branch. Imported and designer-authored rooms can now represent `1x1`, `2x1`, `1x2`, `2x2`, and `L` footprints as one logical room instance.

## Runtime Model

- `ImportedRoomRuntimeAsset.Footprint` preserves `hollowRuntime.footprint` as `RoomInstanceFootprint`.
- `RoomInstanceFootprint` stores `primaryCell`, `occupiedCells`, and `chunkBasisTiles`.
- `BranchRoomInstanceId` and `BranchCellOccupancyMap` are additive topology types for future explicit port-to-port branch migration.
- Current `BranchRoomId` directional traversal remains unchanged for legacy five-room gameplay.

## Door Ports

Door ports are generated and validated from exposed `13x7` chunk faces only:

- `Single1x1`: 4 ports.
- `Wide2x1`: 6 ports.
- `Tall1x2`: 6 ports.
- `Block2x2`: 8 ports.
- `L3Cell`: 8 ports.

Internal seams between occupied chunk cells never produce runtime ports. Multiple same-side ports keep stable lane IDs such as `north_0` and `north_1`.

## Room Designer

`RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset, displayName)` creates macro drafts with:

- dimensions derived from the preset footprint,
- default ground tiles for each occupied chunk,
- safe start snapped to the nearest contained tile,
- four enemy markers and one reward marker snapped into the footprint,
- available door anchors generated from exposed chunk faces.

The existing no-argument `CreateDefault()` still returns the current `13x7` single-room draft.

## Fixtures

`Milestone13AssetGenerator` writes macro runtime fixtures under:

`Assets/_Hollow/Data/Rooms/MacroFixtures/`

These files are import/compiler/runtime test fixtures only. They do not replace the M6/M7 deterministic five-room branch.

## Validation

Run:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "<repo>" -executeMethod Hollow.Editor.Generation.Milestone13AssetGenerator.Generate -quit
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "<repo>" -executeMethod Hollow.Editor.Validation.Milestone13Validator.Validate -quit
```

M13 validation checks fixture imports, footprint counts, door-port counts, lane ordering, internal seam rejection, designer defaults, and current five-room branch compatibility.
