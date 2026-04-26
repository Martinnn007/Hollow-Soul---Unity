# Milestone 17: Feature Branch Treasure Rooms

M17 adds the first special non-combat room role to seeded macro branches while keeping the M13-M16 room import and approved-room pipeline intact.

## Runtime Behavior

- Fresh New Run now uses `m17_feature_branch_v1` when `BranchGenerationSettingsDefinition.EnableTreasureLeaf` is enabled.
- The branch still contains eight logical rooms, exact port-to-port traversal, one origin, one boss leaf, and no loops.
- One non-boss leaf is promoted to `BranchRoomRole.Treasure`.
- Treasure rooms skip enemy spawning, mark themselves cleared when entered, unlock connected doors immediately, and expose a one-time reward pickup.
- The reward plan adds `Treasure Cache`, a 15-soul currency reward, while boss rooms still grant `Boss Sigil` and 25 souls.

## Compatibility

- `BranchGenerator.CreateSeededMacroBranch(...)` still produces the M15 branch identity and never assigns treasure rooms.
- Existing M15, M14, and M7 active-run snapshots continue through their legacy restore paths.
- Approved Room Designer templates remain additive room candidates. M17 changes branch role assignment, not the HollowRuntime V2 gameplay schema.

## Validation

Run:

```bash
Hollow/Generation/Generate Milestone 17 Assets
Hollow/Validation/Run Milestone 17 Validation
```

Validation checks scene wiring, feature-branch settings, one treasure room, one boss room, explicit port connections, no footprint overlap, and the Treasure Cache reward.
