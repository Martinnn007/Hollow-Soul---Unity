# M98: Unity Behavior Runtime Migration Pilot V1

M98 adds Unity's official `com.unity.behavior` package to the M96 bake-off and pilots Unity Behavior as the high-level decision graph for Rat and Skeleton Sword.

## Contract

- Hollow remains authoritative for attack profiles, action profiles, spacing profiles, active hit windows, damage, NavMesh locomotion, tactical slots, pressure budgets, saves, and boss exemptions.
- Unity Behavior graphs output `EnemyBehaviorCommand` intent only while the enemy is idle.
- `EnemyUnityBehaviorGraphBridge` feeds distance, awareness, disposition, endangered state, tactical role, and path status into the graph blackboard.
- Official graphs can use custom Hollow nodes or write blackboard outputs: `OutputCommandKind`, `OutputActionId`, `OutputSpeedMultiplier`, `OutputReason`.
- If a graph asset is not authored yet, the deterministic pilot fallback preserves the same Rat/Skeleton Sword behavior contract for tests and runtime.
- Boss runtime behavior remains unchanged.

## Pilot Graphs

| Enemy | Runtime mode | V1 behavior |
| --- | --- | --- |
| Rat | Unity Behavior Graph | Random idle wander, territorial warning/pressure, bite when engaged and close, skitter/flee after damage or endangered. |
| Skeleton Sword | Unity Behavior Graph | Idle/face, close to slash range, start `rusty_slash`; Hollow combo/recovery handles follow-up commitment. |

## Custom Nodes

- Conditions: engaged, endangered, should flee, can start action, in action range.
- Actions: set command, wander, chase/approach, flee, hold/face, start linked Hollow action.

## M96 Bake-Off Addition

`Unity Behavior` is now evaluated separately from paid `Behavior Designer Pro 3`; it is a free official Unity graph/runtime candidate for designer-readable enemy decision flow.
