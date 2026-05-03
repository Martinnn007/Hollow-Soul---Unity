# M92: Pathfinding Backend Adapter V1

M92 adds an optional custom `RoomGridAStar` backend behind the existing M88 `EnemyNavigationAdapter`. Behavior trees still issue the same movement intents; navigation converts those intents into path-aware goals and keeps local steering as the mandatory fallback.

## Contract

- `RoomGridAStar` uses a cached 0.5m room graph built from `RoomRuntimeRoot` bounds, walkable tiles, holes, obstacles, and blocking interactive objects.
- Grounded mobile non-boss enemies can path for approach, preferred/action-envelope spacing, flee/reset, investigate, wander, and return-home intents.
- Path goals target M91 action envelopes instead of blindly routing to the player center.
- Action-envelope goals are sampled around the player/anchor so enemies do not pin themselves against rocks when the direct start point is blocked.
- M92.1 resolves those sampled action-envelope goals with one bounded candidate-set search instead of many repeated A* searches.
- M92.1 caches room occupancy by room and radius bucket, then invalidates conservatively when layout/blocker signatures change.
- M92.1 applies a per-frame fresh path budget; lower-priority enemies fall back to cached/local steering while Tactical/Cunning enemies keep a small reserve.
- Active attacks, lunges, charges, creature bursts, recovery commitment, bump separation, flying movement, phase movement, stationary enemies, and bosses keep existing local movement rules.
- Enemy and player bodies remain local steering/separation concerns, not A* blockers.
- If a full path fails, the backend attempts a partial reachable node toward the goal; if that fails, local steering fallback remains authoritative.
- Runtime diagnostics expose backend, path status, final goal, next waypoint, path age, waypoint count, and fallback reason.
- The Developer Spawn Menu exposes an `Enemy Path Tracing` toggle plus path stats for requests, solves, cache hits, occupancy builds, deferrals, fallbacks, budget, and solve time.

## Backend Status

- M88 compatibility backend constant: `LocalSteering`.
- Optional runtime backend: `RoomGridAStar`.
- Grid cell size: `0.50m`.
- Repath cadence is staggered per enemy by intelligence and spawn index; smarter enemies refresh faster.

## Roster Routing Table

| Enemy | Movement | Speed | Pathfinding Runtime | Notes |
| --- | --- | ---: | --- | --- |
| Normal Chaser | Grounded | 1.50 | RoomGridAStar + local fallback | grounded mobile |
| Flying Chaser | Flying | 1.80 | local rules only | flying exempt |
| Fast Chaser | Grounded | 2.40 | RoomGridAStar + local fallback | grounded mobile |
| Heavy Chaser | Grounded | 0.90 | RoomGridAStar + local fallback | grounded mobile |
| Ash Charger | Grounded | 1.20 | RoomGridAStar + local fallback | grounded mobile |
| Bone Turret | Grounded | 0.00 | local rules only | stationary diagnostics only |
| Husk Splitter | Grounded | 1.10 | RoomGridAStar + local fallback | grounded mobile |
| Spitting Pod | Grounded | 0.00 | local rules only | stationary diagnostics only |
| Rat | Grounded | 2.65 | RoomGridAStar + local fallback | grounded mobile |
| Spider | Grounded | 2.90 | RoomGridAStar + local fallback | grounded mobile |
| Hollow Bird | Flying | 2.25 | local rules only | flying exempt |
| Hollow Beast | Grounded | 1.90 | RoomGridAStar + local fallback | grounded mobile |
| Skeleton Sword | Grounded | 1.55 | RoomGridAStar + local fallback | grounded mobile |
| Skeleton Spear | Grounded | 1.45 | RoomGridAStar + local fallback | grounded mobile |
| Knight | Grounded | 1.15 | RoomGridAStar + local fallback | grounded mobile |
| Giant | Grounded | 0.75 | RoomGridAStar + local fallback | grounded mobile |
| Hollow Archer | Grounded | 1.35 | RoomGridAStar + local fallback | grounded mobile |
| Powder Gunner | Grounded | 1.05 | RoomGridAStar + local fallback | grounded mobile |
| Knife Thrower | Grounded | 1.75 | RoomGridAStar + local fallback | grounded mobile |
| Repeater Turret | Grounded | 0.00 | local rules only | stationary diagnostics only |
| Clockwork Sentry | Grounded | 0.65 | RoomGridAStar + local fallback | grounded mobile |
| Hollow Acolyte | Grounded | 1.05 | RoomGridAStar + local fallback | grounded mobile |
| Wraith | Flying | 1.75 | local rules only | flying exempt |
| Soul Eater | Grounded | 1.20 | RoomGridAStar + local fallback | grounded mobile |
| Curse Binder | Grounded | 0.85 | RoomGridAStar + local fallback | grounded mobile |
| Grave Lantern | Grounded | 0.00 | local rules only | stationary diagnostics only |

## Intent Coverage

| Intent | M92 Backend | Goal Source |
| --- | --- | --- |
| MoveToPlayer | path-aware | M91 current action envelope near player |
| PreferredRange | path-aware | M91 current action envelope near player |
| Flee / Reset | path-aware | short capped retreat goal |
| Wander | path-aware | short local wander goal |
| Investigate | path-aware | last stimulus position |
| ReturnHome | path-aware | spawn/home position |
| ActiveCharge / ActiveLunge / CreatureBurst / PhaseMove / BumpSeparation | local rules | existing commitment or separation logic |

## Exemptions

- Flying enemies keep floor-region local movement.
- Phase enemies keep obstacle-ignoring local movement.
- Stationary enemies such as turrets, pods, and lanterns keep valid metadata and diagnostics but do not request movement paths.
- Boss runtime behavior remains unchanged.

## Current Counts

- Grounded mobile path users: 19.
- Grounded stationary exemptions: 4.
- Flying exemptions: 3.

## Tuning Notes

- M92 does not rewrite behavior trees. Trees still decide what to do; navigation decides how to reach the chosen movement goal.
- The backend paths toward action start positions, so enemies should stop shoving into rocks while trying to reach melee, ranged, investigate, flee, or return-home positions.
- When the direct action start point is blocked, smart goal sampling scores nearby valid envelope positions inside the same bounded path search.
- Under load, enemies degrade gracefully by reusing cached paths or local steering for a frame instead of forcing every enemy to solve a fresh path at once.
- Local steering remains necessary for final approach, crowd separation, player/enemy body smoothing, and fallback.
