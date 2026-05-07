# M97: Unity NavMesh Navigation Replacement V1

M97 replaces Hollow's runtime grounded enemy pathfinding with native Unity NavMesh. Hollow still owns combat intent, tactical threat slots, active hit windows, action scoring, pressure, and boss exemptions.

## Runtime Contract

- Current navigation backend: `UnityNavMesh`.
- Mobile grounded non-boss enemies use `NavMeshAgent` through `EnemyNavMeshAgentBridge`.
- `EnemyNavigationAdapter` no longer calls `RoomGridAStarPathfinder` at runtime.
- Grounded locomotion destinations target M91/M96 action/reservation positions rather than the player center.
- Active attacks, lunges, charge movement, stun, death, flying, phase movement, stationary enemies, and bosses remain exempt from agent-driven locomotion.
- Ordinary contact remains harmless from M79, and attack damage remains active-window-only from M80.

## Room Baking

- Approved room source: `Assets/_Hollow/Data/Rooms/DesignerApproved`.
- NavMesh data output: `Assets/_Hollow/Data/NavMesh/Rooms`.
- Runtime catalog: `Assets/_Hollow/Resources/Navigation/RoomNavMeshCatalog.asset`.
- Approved/playable rooms without a prebaked catalog entry are invalid for play and fail with a clear console error.
- Static rocks, holes, room bounds, and blocking room objects are baked. Destructible blockers use `NavMeshObstacle` carving.

## Debugging

- Enemy path tracing draws Unity `NavMeshPath.corners`.
- Debug stats now report NavMesh users, requests, path calculations, invalid/fallback reasons, solve time, and active destination data.

## Test Focus

- Bake coverage for every approved room.
- Grounded enemies route around rocks to tactical reserved positions.
- Agents stop during committed attack windows and sync cleanly after manual displacement/knockback.
- Arena, Designer Room playtest, projectiles, room clear, split children, and boss runtime stay stable.
