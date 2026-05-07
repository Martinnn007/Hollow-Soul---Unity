# M103: Dynamic Navigation Objects V1

M103 makes runtime navigation blockers predictable under the Unity NavMesh replacement.

## Contract

- Static rocks and authored holes are baked into room NavMesh data and exposed as debug navigation markers.
- Barrels, destructible blockers, and future gates use `RoomDynamicNavigationObjectMarker` plus `NavMeshObstacle` carving when they block movement at runtime; they are not baked into the static room NavMesh.
- Destroyed interactive objects disable carving immediately through `RoomInteractiveObjectMarker.MarkDestroyed`.
- Doors are visual by default, but `Locked` and `Unavailable` states enable carving while `Active` and `Cleared` states disable it.
- Runtime code should not assume a `NavMeshObstacle` already exists; dynamic navigation markers create and configure one safely.

## Debugging

- `RoomRuntimeRoot.DynamicNavigationObjects` lists rocks, holes, doors, and interactive blockers with category, carving state, and last reason.
- Each marker exposes `StatusSummary` and optional compact scene labels via `SetDynamicNavigationDebugLabelsVisible`.
- This layer does not rebake rooms at runtime; it only toggles Unity carving obstacles for dynamic blockers.
