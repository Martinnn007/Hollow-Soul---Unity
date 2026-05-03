# M84: Weapon-User Enemies V1

M84 adds Skeleton Sword, Skeleton Spear, Knight, and Giant as the first weapon-user enemy family. They use M80 windup/active/recovery commitment, M79 harmless ordinary body contact, M76 attack impact profiles, M81 action metadata, and M82 behavior trees.

## Roster Cards

| Enemy | Spawn | HP | Speed | Body | Intelligence | Disposition | Preferred Range | Sight | Hearing | Shield |
| --- | --- | ---: | ---: | --- | --- | --- | --- | --- | ---: | --- |
| Skeleton Sword | `spawnEnemySkeletonSword` | 4 | 1.55m/s | Medium | Basic | predator | 1.15-1.85m | 6.5m/160deg | 5.0m | None |
| Skeleton Spear | `spawnEnemySkeletonSpear` | 4 | 1.45m/s | Medium | Basic | sentinel | 1.75-2.75m | 7.0m/150deg | 5.2m | None |
| Knight | `spawnEnemyKnight` | 8 | 1.15m/s | Heavy | Trained | sentinel | 1.35-2.35m | 7.0m/140deg | 5.0m | Medium |
| Giant | `spawnEnemyGiant` | 14 | 0.75m/s | Massive | Basic | mindless | 1.85-3.10m | 6.0m/115deg | 4.5m | None |

## Movesets

| Enemy | Attack | Runtime | Damage | Force | Range | Arc | Timing | Knockback | Combo |
| --- | --- | --- | ---: | --- | ---: | ---: | --- | ---: | --- |
| Skeleton Sword | `rusty_slash` Rusty Slash | WeaponMelee | 1 | Light | 1.45m | 120deg | 0.28/0.14/0.24s | 0.35m | `backhand_slash` |
| Skeleton Sword | `backhand_slash` Backhand Slash | WeaponMelee | 1 | Light | 1.35m | 140deg | 0.18/0.14/0.34s | 0.30m | - |
| Skeleton Spear | `spear_thrust` Spear Thrust | WeaponMelee | 1 | Medium | 2.40m | 55deg | 0.34/0.12/0.34s | 0.45m | - |
| Skeleton Spear | `spear_sweep` Spear Sweep | WeaponMelee | 1 | Light | 1.65m | 160deg | 0.30/0.16/0.38s | 0.35m | - |
| Knight | `shield_guard` Shield Guard | Defense | 0 | Medium | 2.25m | 150deg | 0.12/0.65/0.28s | 0.00m | - |
| Knight | `knight_slash` Knight Slash | WeaponMelee | 1 | Medium | 1.65m | 120deg | 0.36/0.16/0.36s | 0.50m | `shield_bash` |
| Knight | `knight_thrust` Knight Thrust | WeaponMelee | 1 | Medium | 2.15m | 65deg | 0.34/0.13/0.38s | 0.45m | - |
| Knight | `shield_bash` Shield Bash | WeaponMelee | 1 | Medium | 1.15m | 90deg | 0.28/0.14/0.50s | 0.65m | - |
| Giant | `club_sweep` Club Sweep | WeaponMelee | 2 | Heavy | 2.25m | 190deg | 0.65/0.22/0.75s | 0.90m | - |
| Giant | `overhead_slam` Overhead Slam | Area | 2 | Heavy | 1.55m | 360deg | 0.78/0.20/0.90s | 1.10m | - |
| Giant | `stomp` Giant Stomp | Area | 1 | Heavy | 1.25m | 360deg | 0.50/0.18/0.60s | 0.80m | - |

## Shield Tier Contract

| Tier | Frontal Arc | Light/Medium Physical | Heavy Physical | Massive Physical | Break Threshold |
| --- | ---: | ---: | ---: | ---: | --- |
| Small Shield | 135deg | 50% | 25% | 0% | Heavy+ |
| Medium Shield | 150deg | 75% | 50% | 25% | Heavy+ |
| Heavy Shield | 170deg | 100% | 80% | 55% | Massive+ |

## Runtime Rules

- `EnemyAttackRuntimeKind.WeaponMelee` uses forward arcs/ranges during active frames and does not require harmful body overlap.
- One follow-up combo is allowed when authored, alive, engaged, in range, not interrupted, and allowed by the melee budget. No 3-hit chains are added.
- Knight uses the medium shield guard profile. Frontal guarded hits reduce physical damage; flank/back hits bypass guard.
- Heavy or stronger physical attacks can break medium guard into punishable recovery.
- Boss runtime behavior remains unchanged.

## Encounters And Rooms

Encounter ids: `m84_skeleton_patrol`, `m84_spear_lane`, `m84_knight_shield_line`, `m84_giant_pressure`, `m84_weapon_battlefield`.

Battlefield rooms: `m84_skeleton_patrol_field`, `m84_spear_lane_field`, `m84_knight_shield_line_field`, `m84_giant_pressure_field`, `m84_mixed_weapon_battlefield`.
