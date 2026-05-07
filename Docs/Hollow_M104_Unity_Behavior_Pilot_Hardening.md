# M104: Unity Behavior Pilot Hardening

M104 hardens the Rat and Skeleton Sword Unity Behavior pilot so it behaves like a real migration contract instead of a silent deterministic fallback.

## Runtime Contract

- Rat and Skeleton Sword are the hardened first pilot enemies; later family migrations may route additional non-boss enemies through the same bridge.
- Hollow remains authoritative for attack/action/spacing profiles, active hit windows, damage, NavMesh locomotion, tactical pressure, saves, and boss exemptions.
- Unity Behavior graphs output commands only while the enemy is idle; windup, active, recovery, stun, death, knockback, lunges, charges, and combos stay locked in Hollow runtime.
- Emergency fallback is allowed only when the official graph is missing, uncompiled, missing required variables, or throws during evaluation.
- Every emergency fallback evaluation is trace-visible through `EnemyUnityBehaviorGraphBridge.TraceHistory` and `UsedEmergencyFallbackLastEvaluation`.

## Stable Blackboard Schema

| Direction | Variables |
| --- | --- |
| Inputs | `DistanceToPlayer`, `Awareness`, `Disposition`, `Endangered`, `IsIdle`, `TacticalRole`, `PathStatus` |
| Optional inputs | `Enemy`, `Player`, `TimeSeconds`, `DeltaTime` |
| Outputs | `OutputCommandKind`, `OutputActionId`, `OutputSpeedMultiplier`, `OutputReason` |

## Pilot Graphs

| Enemy | Expected graph behavior | Emergency fallback |
| --- | --- | --- |
| Rat | Wander, warn, pressure territory, bite when committed, flee when damaged/endangered. | Same command sequence, marked `unity_behavior_emergency_fallback`. |
| Skeleton Sword | Face/approach, start `rusty_slash` from idle, Hollow handles combo/recovery. | Same command sequence, marked `unity_behavior_emergency_fallback`. |

## Validation

The M104 validator checks package availability, schema version, required input/output variable names, emergency fallback policy, pilot enemy wiring, docs/report artifacts, and runtime trace hooks.

## Authoring Note

Official Unity Behavior graph assets should be authored in Unity's Behavior Graph editor and assigned into the Rat/Skeleton pilot definitions. Until those graph assets are compiled and contain the required schema, the runtime uses the emergency guard and reports it explicitly.
