# M100 NavMesh Bake Pipeline V2 Report

- Shared bake builder: `RoomNavMeshBuildUtility`.
- Runtime compatibility wrapper: `RoomRuntimeNavMeshBuilder`.
- Editor baker: `RoomNavMeshBakeUtility`.
- Preferred bake command: `Hollow/Navigation/Bake Runtime Room NavMeshes`.
- Catalog path: `Assets/_Hollow/Resources/Navigation/RoomNavMeshCatalog.asset`.
- Runtime roots: `Assets/_Hollow/Data/Rooms/DesignerApproved`, `Assets/_Hollow/Data/Rooms/MacroFixtures`, `Assets/_Hollow/Data/Rooms/DeveloperLab`.
- Agent settings: `radius=0.24m height=1.05m climb=0.18m slope=20deg minRegion=0.25m`.
- Fallback policy: `RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake` is allowed only in editor/development; `RequireCatalogBake` blocks with exact missing-bake diagnostics.
