# Milestone 16: Approved Room Designer Pool

M16 lets Room Designer output become real branch content without making New Run fragile. Approved HollowRuntime V2 JSON files live in a repo-tracked folder and are added to the branch room catalog by generation. M13 macro fixtures remain the guaranteed fallback pool.

## Authoring Workflow

1. Export a Room Designer draft to HollowRuntime V2 JSON.
2. Review the JSON and place the approved `*.hollowruntime.json` file under:
   `Assets/_Hollow/Data/Rooms/DesignerApproved/`
3. Run `Hollow/Generation/Generate Milestone 16 Assets`.
4. Run `Hollow/Validation/Run Milestone 16 Validation`.

Designer USDA companions remain inspection/reference artifacts only. Runtime gameplay still reads the `hollowRuntime.schemaVersion = 2` JSON.

## Runtime Behavior

- New Run still uses the M15 seeded eight-room macro branch.
- The generator uses fixture rooms as the structural fallback recipe.
- Approved designer rooms are additive same-footprint candidates.
- Same seed plus same approved catalog produces the same branch, room assignments, boss leaf, and reward plan.
- If no approved rooms exist, M15 behavior remains unchanged.

## Validation Rules

Approved rooms must import through `HollowRuntimeV2Importer`, use a supported macro footprint, have unique canonical room IDs, expose valid door ports, contain walkable floor, include a safe start, and include at least one enemy spawn.

Invalid approved files fail M16 validation instead of being silently ignored.
