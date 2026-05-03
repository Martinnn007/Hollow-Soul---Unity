# M86: Ranged + Firearm Enemies V1

M86 adds a ranged enemy family with Dark Souls-inspired commitment: enemies draw, aim, fire during an active point, then recover. Ranged pressure is profile-driven through M76 attack profiles, M81 action metadata, M82 behavior trees, M80 active windows, and M79 harmless ordinary body contact.

## Roster Cards

| Enemy | Spawn | HP | Speed | Body | Intelligence | Disposition | Preferred Range | Sight | Hearing | Identity |
| --- | --- | ---: | ---: | --- | --- | --- | --- | --- | ---: | --- |
| Hollow Archer | `spawnEnemyHollowArcher` | 4 | 1.35m/s | Medium | 2 Basic | sentinel | 4.00-7.25m | 8.5m/135deg | 5.4m | bow user, aimed shot and volley |
| Powder Gunner | `spawnEnemyPowderGunner` | 5 | 1.05m/s | Heavy | 3 Trained | sentinel | 4.75-8.50m | 9.0m/115deg | 6.0m | firearm user, slow heavy aim and scatter |
| Knife Thrower | `spawnEnemyKnifeThrower` | 4 | 1.75m/s | Medium | 2 Basic | territorial | 2.70-5.25m | 8.0m/190deg | 6.4m | thrower skirmisher, quick knives and evasive range |
| Repeater Turret | `spawnEnemyRepeaterTurret` | 6 | 0.00m/s | Heavy | 3 Trained | sentinel | 6.00-9.25m | 10.0m/95deg | 3.2m | stationary machine turret, burst/fan pressure |
| Clockwork Sentry | `spawnEnemyClockworkSentry` | 8 | 0.65m/s | Heavy | 4 Tactical | sentinel | 4.80-7.80m | 9.0m/220deg | 6.5m | slow machine, radial and rotating projectile patterns |

## Attack Profiles

| Enemy | Attack | Runtime | Damage | Force | Range | Count | Speed | Timing | Knockback | Notes |
| --- | --- | --- | ---: | --- | ---: | ---: | ---: | --- | ---: | --- |
| Hollow Archer | `arrow_shot` Arrow Shot | Projectile | 1 | Light | 7.50m | 1 | 6.3m/s | 0.38/0.08/0.38s | 0.35m | M86 bow shot: draw, release, and punishable recovery. |
| Hollow Archer | `retreating_arrow` Retreating Arrow | Projectile | 1 | Light | 6.70m | 1 | 6.0m/s | 0.34/0.08/0.42s | 0.32m | M86 bow shot used after spacing pressure. |
| Hollow Archer | `arrow_volley` Arrow Volley | FanProjectile | 1 | Light | 7.25m | 3 | 5.8m/s | 0.48/0.08/0.46s | 0.32m | M86 three-arrow fan with visible gaps. |
| Hollow Archer | `archer_backstep` Archer Backstep | CreatureMove | 0 | Light | 2.50m | 0 | 0.0m/s | 0.08/0.20/0.20s | 0.00m | M86 non-damaging ranged spacing reset. |
| Powder Gunner | `aimed_musket_shot` Aimed Musket Shot | Projectile | 2 | Heavy | 8.80m | 1 | 10.0m/s | 0.72/0.06/0.75s | 0.75m | M86 firearm: long aim lock, fast projectile, heavy guard pressure. |
| Powder Gunner | `scatter_shot` Scatter Shot | FanProjectile | 1 | Medium | 5.40m | 5 | 7.0m/s | 0.58/0.08/0.68s | 0.45m | M86 close-range firearm fan with broad but dodgeable lanes. |
| Powder Gunner | `gunner_backstep` Gunner Backstep | CreatureMove | 0 | Light | 2.50m | 0 | 0.0m/s | 0.12/0.20/0.26s | 0.00m | M86 slow reload-space hop with no damage. |
| Knife Thrower | `throwing_knife` Throwing Knife | Projectile | 1 | Light | 5.80m | 1 | 7.0m/s | 0.22/0.06/0.24s | 0.24m | M86 quick mid-range knife throw. |
| Knife Thrower | `knife_fan` Knife Fan | FanProjectile | 1 | Light | 4.80m | 3 | 6.5m/s | 0.34/0.08/0.32s | 0.26m | M86 three-knife pressure fan. |
| Knife Thrower | `thrower_backstep` Thrower Backstep | CreatureMove | 0 | Light | 2.10m | 0 | 0.0m/s | 0.06/0.18/0.16s | 0.00m | M86 agile spacing burst for a ranged skirmisher. |
| Repeater Turret | `repeater_burst` Repeater Burst | FanProjectile | 1 | Light | 8.50m | 3 | 6.8m/s | 0.36/0.08/0.35s | 0.30m | M86 narrow three-shot turret burst. |
| Repeater Turret | `suppressing_arc` Suppressing Arc | FanProjectile | 1 | Light | 8.00m | 5 | 5.8m/s | 0.52/0.08/0.50s | 0.28m | M86 wide stationary fan with readable gaps. |
| Repeater Turret | `lock_on_dart` Lock-On Dart | Projectile | 1 | Light | 9.25m | 1 | 7.2m/s | 0.42/0.06/0.32s | 0.34m | M86 precise sentinel shot after a lock-on tell. |
| Clockwork Sentry | `clockwork_radial` Clockwork Radial | RadialProjectile | 1 | Medium | 6.50m | 8 | 4.8m/s | 0.55/0.10/0.55s | 0.42m | M86 machine radial pattern that asks the player to find a gap. |
| Clockwork Sentry | `rotating_fan` Rotating Fan | FanProjectile | 1 | Light | 7.00m | 5 | 5.0m/s | 0.38/0.08/0.42s | 0.30m | M86 rotating fan volley from a slow machine. |
| Clockwork Sentry | `gear_shot` Gear Shot | Projectile | 1 | Light | 7.80m | 1 | 5.8m/s | 0.30/0.06/0.30s | 0.32m | M86 simple machine projectile used between patterns. |

## Runtime Rules

- `StartRangedAction` can now be profile-specific, so trees can ask whether `arrow_volley`, `scatter_shot`, or `clockwork_radial` is actually in range before committing.
- Non-boss ranged attacks fire only at the active transition, then enter recovery. Windup and recovery do not spawn projectiles.
- `Projectile`, `FanProjectile`, and `RadialProjectile` are supported for normal enemies. Fan and radial patterns use projectile count, speed, range, force, and knockback from the linked profile.
- Ranged and firearm enemies respect the existing ranged/charge attack budget, including the M72 Tactical/Cunning priority tie-break without increasing total pressure.
- Ordinary body overlap remains harmless and only disturbs/alerts; no passive contact damage is reintroduced.

## Encounters And Rooms

Encounter ids: `m86_archer_gallery`, `m86_powder_checkpoint`, `m86_thrower_alley`, `m86_repeater_crossfire`, `m86_clockwork_pattern_hall`.
Curated rooms: `m86_archer_gallery_room`, `m86_powder_checkpoint_room`, `m86_thrower_alley_room`, `m86_repeater_crossfire_room`, `m86_clockwork_pattern_hall_room`.

## M87 Bridge

M86 keeps damage physical and weapon/machine based. The same profile-specific ranged path is ready for M87 magic, ghost, soul, curse, and area-pressure casters without giving those enemies a separate projectile system.
