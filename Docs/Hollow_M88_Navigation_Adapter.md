# M88: Navigation Adapter V1

M88 introduces a navigation wrapper between behavior decisions and room movement. It adds no full pathfinding. The active backend is `LocalSteering`, but enemy runtime movement now asks an adapter to resolve desired local positions, modes, and intents so future pathfinding or local-navigation systems can be swapped in without rewriting combat AI.

## Runtime Contract

- `EnemyNavigationRequest` carries room, current position, desired position, radius, mode, intent, intelligence, and whether local detours are allowed.
- `EnemyNavigationResult` records backend, mode, intent, requested/resolved positions, steering direction, reached/blocked flags, and whether fallback steering was used.
- `EnemyNavigationAdapter` is the only place normal enemy runtime movement resolves room collision for chase, preferred range, flee, wander, investigation, return-home, active attacks, creature bursts, phase moves, and bump separation.
- Current backend: `LocalSteering`. It samples short local detours for non-committed grounded movement when direct motion stalls on rocks or blockers.
- Committed attacks remain committed: charges and lunges do not pathfind around obstacles during active frames.
- Flying movement keeps ignoring rocks while respecting floor-region bounds. Phase movement can ignore obstacles while staying inside room bounds.
- No A*, navmesh, obstacle LOS, squad navigation, boss behavior changes, save migration, or new enemy roster is included.

## Movement Intent Table

| Intent | Current Handling | Future Hook |
| --- | --- | --- |
| `MoveToPlayer` | local collision + optional detour | chase destination through path adapter |
| `PreferredRange` | local collision + optional detour | range-band destination scoring |
| `Flee` | local collision + optional detour | retreat destination scoring with caps |
| `Wander` | deterministic local steering | idle patrol/roam destinations |
| `Investigate` | move/facing toward last disturbance | noise-source path target |
| `ReturnHome` | local return to spawn/home | leash/path return target |
| `ActiveCharge` / `ActiveLunge` | no detour, collision constrained | animation-authored movement lanes |
| `CreatureBurst` | local burst with optional detour | burst destination validation |
| `PhaseMove` | ignores obstacles, clamps to valid room | ghost/caster phase target picker |
| `BumpSeparation` | tiny local separation | body-resolution policy |

## Current Roster Modes

| Enemy | Movement | Default Mode | Notes |
| --- | --- | --- | --- |
| Normal Chaser | Grounded | GroundedLocal | uses conservative local steering only |
| Flying Chaser | Flying | FlyingLocal | flying adapter ignores rocks and respects floor bounds |
| Fast Chaser | Grounded | GroundedLocal | uses conservative local steering only |
| Heavy Chaser | Grounded | GroundedLocal | uses conservative local steering only |
| Ash Charger | Grounded | GroundedLocal | uses conservative local steering only |
| Bone Turret | Grounded | GroundedLocal | stationary behavior tree; adapter still available for bump/phase-safe resolution |
| Husk Splitter | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Spitting Pod | Grounded | GroundedLocal | stationary behavior tree; adapter still available for bump/phase-safe resolution |
| Rat | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Spider | Grounded | GroundedLocal | uses conservative local steering only |
| Hollow Bird | Flying | FlyingLocal | flying adapter ignores rocks and respects floor bounds |
| Hollow Beast | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Skeleton Sword | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Skeleton Spear | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Knight | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Giant | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Hollow Archer | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Powder Gunner | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Knife Thrower | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Repeater Turret | Grounded | GroundedLocal | stationary behavior tree; adapter still available for bump/phase-safe resolution |
| Clockwork Sentry | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Hollow Acolyte | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Wraith | Flying | FlyingLocal | flying adapter ignores rocks and respects floor bounds |
| Soul Eater | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Curse Binder | Grounded | GroundedLocal | uses local detour sampling when non-committed movement stalls |
| Grave Lantern | Grounded | GroundedLocal | stationary behavior tree; adapter still available for bump/phase-safe resolution |

## M89 Bridge

M89 Limited Alert Sharing should emit awareness/stimulus decisions separately from movement. When an ally wakes another enemy, that enemy should still use M88 navigation intents for investigate, face, return-home, or attack-range movement instead of receiving direct position edits.
