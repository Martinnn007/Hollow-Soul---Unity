# Milestone 14: Seeded Macro Branch Generation

M14 makes the default New Run branch use the M13 macro-room fixture pool. The branch is still a five-room cross for economy and save compatibility, but each logical room can occupy multiple branch cells and connect through exact door port IDs.

## Runtime Behavior

- New profile-backed runs use `m14_macro_fixture_branch_v1`.
- Continue restores M14 snapshots by regenerating the same branch from `branchSeed`.
- Legacy `m7_five_room_cross` snapshots continue through the old single-room branch path.
- Logical room IDs stay `origin`, `north`, `south`, `east`, and `west`, so deterministic M7 rewards remain valid.

## Fixed M14 Topology

- `origin`: `combat_macro_single_1x1`, primary `(0,0)`.
- `north`: `combat_macro_tall_1x2`, primary `(0,-2)`, connected through `origin.north_0 -> north.south_0`.
- `south`: `combat_macro_l_3cell`, primary `(0,1)`, connected through `origin.south_0 -> south.north_0`.
- `east`: `combat_macro_wide_2x1`, primary `(1,0)`, connected through `origin.east_0 -> east.west_0`.
- `west`: `combat_macro_block_2x2`, primary `(-2,-1)`, connected through `origin.west_0 -> west.east_1`.

## Generated Assets

`Milestone14AssetGenerator` creates:

- `Assets/_Hollow/Data/Branches/BranchRoomTemplateCatalog_MacroFixtures.asset`
- scene wiring on all three game scenes so `BranchSessionController` references the macro fixture catalog

## Validation

Run:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "<repo>" -executeMethod Hollow.Editor.Generation.Milestone14AssetGenerator.Generate -quit
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "<repo>" -executeMethod Hollow.Editor.Validation.Milestone14Validator.Validate -quit
```

M14 validation checks catalog assignments, imported fixture health, exact port connections, non-overlapping occupied branch cells, and game scene wiring.
