# M106: Behavior Graph Subgraph Library

M106 adds reusable Unity Behavior subgraph contracts for common Hollow enemy intent. These subgraphs choose intent only; Hollow action profiles, attack profiles, active windows, NavMesh locomotion, tactical slots, pressure budgets, and damage math remain authoritative.

## Reusable Subgraphs

| Subgraph | Output | Purpose |
| --- | --- | --- |
| Notice Player | `FacePlayer` | First readable acknowledgement beat. No damage, no direct movement. |
| Investigate Noise | `Wander` | Moves through Hollow investigation/local navigation toward the latest disturbance. |
| Flee | `Flee` | Prey, critters, and damaged enemies can request a capped Hollow flee/reset. |
| Circle | `MovePreferredRange` | Requests tactical circling/repositioning; NavMesh and spacing profiles own motion. |
| Approach Action Range | `MovePreferredRange` | Moves toward M91/M102 reachable action envelopes instead of player center. |
| Request Attack Slot | `StartMeleeAction` | Asks Hollow scorer/director to choose or approve a concrete attack. Empty action id is intentional. |
| Start Action | `StartMeleeAction` | Starts an explicit Hollow action id while preserving active windows and budgets. |
| Recover / Hold | `Hold` | Non-damaging recovery/hold branch used after failed or deferred commits. |

## Authoring Contract

- Every contract has an official Unity `BehaviorGraph` slot for the visual subgraph asset.
- Required Hollow nodes include notice player, investigate noise, flee, circle, approach action range, request attack slot, start action, and recover/hold wrappers.
- `Request Attack Slot` may output an empty action id; `EnemyActionScorer` and `RoomTacticalDirector` choose the concrete Hollow action later.
- The subgraph library is reusable by family graphs from M105 and by future enemy-specific Unity Behavior graphs.
