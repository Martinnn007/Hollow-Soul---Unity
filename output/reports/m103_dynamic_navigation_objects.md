# M103 Dynamic Navigation Objects Report

- Added `RoomDynamicNavigationObjectMarker` as the shared bridge for doors, destructibles, blockers, holes, and future gates.
- Interactive blockers safely create/configure `NavMeshObstacle` carving and disable it on destruction.
- Interactive blockers are no longer baked into static room NavMesh data, so disabling carving can actually reopen paths.
- Door state changes now drive navigation carving for locked/unavailable doors.
- Rocks and holes are registered as baked navigation objects for debugging; holes also feed the shared room NavMesh bake utility.
- `RoomRuntimeRoot.DynamicNavigationObjects` provides a single runtime inspection surface, with optional debug labels toggled through `SetDynamicNavigationDebugLabelsVisible`.
