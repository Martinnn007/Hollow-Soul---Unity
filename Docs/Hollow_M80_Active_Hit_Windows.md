# M80: Active Hit Windows V1

M80 moves combat toward explicit readable action phases. Enemy and player attacks now resolve as `Windup -> Active -> Recovery -> Idle`; damage lands only during active frames, ordinary body overlap remains harmless from M79, and recovery creates a small punish window.

## Runtime Contract

- Enemy melee, charge, and ranged attacks use windup, active, and recovery states.
- Ranged projectiles spawn on the active transition, then the enemy remains in recovery.
- Melee and lunge attacks use a simple forward arc plus range during active frames; each activation can hit once.
- Windup can be broken by incoming player damage whose `ImpactForceClass` meets the resolved poise threshold.
- Player light/heavy attacks pay stamina on start, then use windup, active, and recovery timing.
- The debug 2x light attack toggle still halves final light cooldowns only.
- Player attack commitment slows movement to `55%` and cannot be cancelled by roll, guard, or another attack in V1.
- Player roll uses Space / gamepad east, costs stamina, travels in the move or aim direction, and grants dedicated roll i-frames separate from post-damage invulnerability.
- Boss runtime remains largely unchanged; M79 dash/bash active contact bridges stay protected by explicit active windows.

## Enemy Execution Modifiers

Enemy attack profiles provide base timing, hit arc, and poise threshold. Enemy definitions apply final modifiers so the same style of attack can feel fragile, fast, heavy, or trained by user.

| Enemy | Windup | Active | Recovery | Arc Bonus | Poise Offset |
| --- | ---: | ---: | ---: | ---: | ---: |
| Normal Chaser | x1.00 | x1.00 | x1.00 | 0deg | 0 |
| Flying Chaser | x0.90 | x0.95 | x0.85 | 20deg | -1 |
| Fast Chaser | x0.80 | x0.90 | x0.80 | -5deg | -1 |
| Heavy Chaser | x1.25 | x1.05 | x1.15 | 10deg | +1 |
| Ash Charger | x1.00 | x1.00 | x1.00 | 0deg | +1 |
| Bone Turret | x1.00 | x1.00 | x1.00 | 0deg | +1 |
| Husk Splitter | x1.05 | x1.00 | x1.00 | 0deg | 0 |
| Spitting Pod | x0.90 | x1.00 | x0.90 | 0deg | 0 |
| Rat | x0.75 | x0.85 | x0.65 | 30deg | -1 |
| Spider | x0.70 | x0.80 | x0.60 | 45deg | -1 |

## Player Timing Defaults

| Attack | Windup | Active | Recovery | Arc |
| --- | ---: | ---: | ---: | ---: |
| Melee Light | 0.06s | 0.08s | 0.10s | 115deg |
| Melee Heavy | 0.22s | 0.14s | 0.34s | 135deg |
| Ranged Light | 0.06s | 0.03s | 0.08s | 1deg |
| Ranged Heavy | 0.28s | 0.04s | 0.36s | 1deg |

## Deferred

No combo trees, animation hitboxes, weapon-user enemy overhaul, full boss action rewrite, pathfinding, LOS, or behavior tree system is added in M80.
