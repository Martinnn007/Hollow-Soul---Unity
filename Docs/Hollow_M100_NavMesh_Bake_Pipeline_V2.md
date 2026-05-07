# M100: NavMesh Bake Pipeline V2

M100 unifies the room NavMesh bake path so editor baking and development runtime fallback use the same source geometry, the same agent settings, and the same catalog contract.

## Contract

- Shared builder: `RoomNavMeshBuildUtility`.
- Agent settings: `radius=0.24m height=1.05m climb=0.18m slope=20deg minRegion=0.25m`.
- Preferred bake command: `Hollow/Navigation/Bake Runtime Room NavMeshes`.
- Catalog: `Assets/_Hollow/Resources/Navigation/RoomNavMeshCatalog.asset`.
- Runtime source roots: `Assets/_Hollow/Data/Rooms/DesignerApproved`, `Assets/_Hollow/Data/Rooms/MacroFixtures`, `Assets/_Hollow/Data/Rooms/DeveloperLab`.
- Catalog bakes are the normal authored-room path.
- Runtime baking is editor/development-only and logs a warning when used.
- Non-development builds fail loudly when an authored room is missing catalog NavMesh data.

## Validation

- Designer Room validation reports missing NavMesh bakes with the exact room id, catalog path, and bake command.
- Arena curated preset validation reports missing room bakes before launch.
- Generated/transient designer and arena rooms can use dev fallback while being edited, but should be promoted and baked before QA lock.
