# M96: Tactical Navigation + AI Tool Bake-Off V1

M96 adds a tactical intent layer above the existing behavior tree, action scorer, threat director, spacing profiles, and M92 pathfinding backend. The target feel is Pure Souls leaning: only 2-4 enemies become active tactical threats while the rest hold, reposition, investigate, or wait.

## Runtime Contract

- `RoomTacticalDirector` limits active non-boss tactical threats to `2-4` slots.
- `EnemyTacticalIntent` records role, commit policy, action id, reserved local position, path backend, pressure lane, and debug reason.
- `EnemyLocomotionAgent` remains behind `EnemyRuntimeController` movement and can recover from blocked tactical path steps with small sidestep attempts.
- Behavior trees remain personality/role gates; the scorer and tactical director decide concrete action and position ownership.
- Path goals should move enemies toward reserved action positions rather than the player center or a rigid preferred band.
- Boss runtime behavior remains unchanged.

## Tool Bake-Off

Source of truth: `Hollow enemy/action/spacing/behavior data`.

| Candidate | Role | Paid | Risk | Gate |
| --- | --- | --- | ---: | --- |
| Current Custom RoomGridAStar | baseline navigation backend | no | 2 | Must stop rock scraping, hold stable frame time, and feed tactical slots. |
| Unity AI Navigation | built-in NavMesh candidate | no | 3 | Only adopt if runtime/generated-room baking is deterministic and cheaper than custom corridors. |
| A* Pathfinding Project Pro | paid navigation/local-avoidance candidate | yes | 3 | Only adopt if it clearly beats custom A* in obstacle feel and 20-40 enemy performance. |
| Behavior Designer Pro 3 | paid behavior-authoring candidate | yes | 4 | Only adopt if it mirrors Hollow data cleanly without replacing our source-of-truth assets. |

## Evaluation Rooms

- `Room_Small_RatRoom_001`: critter swarm with rocks and narrow lanes.
- Rock-heavy designer rooms: obstacle routing, stuck recovery, and path-corridor quality.
- Weapon-user rooms: Souls-like approach, attack range, and punishable recovery spacing.
- Ranged/caster rooms: backline reservations and non-dogpile pressure.
- Arena swarms: 20-40 enemy frame stability, solve counts, fallback reasons, and readability.

## Current Roster Shape

- Non-boss enemies: 26.
- Grounded mobile enemies: 19.
- Stationary enemies: 4.
- Flying enemies: 3.

## Adoption Rule

External packages are adopted only if they clearly improve rock/obstacle navigation, 20-40 enemy performance, designer debugging speed, and integration cost while keeping Hollow data as the source of truth.
