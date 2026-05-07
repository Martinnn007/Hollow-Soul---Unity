# M97 Unity NavMesh Navigation Replacement Report

- Runtime backend: `UnityNavMesh`.
- Custom `RoomGridAStarPathfinder` is not called by `EnemyNavigationAdapter`.
- Grounded non-boss runtime bridge: `EnemyNavMeshAgentBridge`.
- NavMesh bake menu: `Hollow/Navigation/Bake Runtime Room NavMeshes`.
- Catalog path: `Assets/_Hollow/Resources/Navigation/RoomNavMeshCatalog.asset`.
- Missing room bake policy: block play with a readable error.
- Dynamic blocker policy: `NavMeshObstacle` carving, no runtime full rebake in V1.
