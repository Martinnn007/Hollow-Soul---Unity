# M87: Magic/Ghost/Soul Enemies V1

M87 adds caster, ghost, soul-drain, curse, and magical pattern enemies while preserving M79 harmless ordinary body contact, M80 active windows, M82 idle-gated behavior trees, and M86 budgeted projectile pressure. The feel target is Dark Souls-style readable commitment in a faster top-down room.

## Roster Cards

| Enemy | Spawn | HP | Speed | Body | Intelligence | Disposition | Preferred Range | Sight | Hearing | Identity |
| --- | --- | ---: | ---: | --- | --- | --- | --- | --- | ---: | --- |
| Hollow Acolyte | `spawnEnemyHollowAcolyte` | 4 | 1.05m/s | Medium | 3 Trained | sentinel | 3.80-6.80m | 8.4m/180deg | 6.2m | caster, slow soul orb and radial rune pressure |
| Wraith | `spawnEnemyWraith` | 3 | 1.75m/s | Light | 4 Tactical | predator | 2.20-5.20m | 8.8m/300deg | 7.0m | ghost, phase movement, soul bolt, curse touch |
| Soul Eater | `spawnEnemySoulEater` | 7 | 1.20m/s | Heavy | 3 Trained | predator | 2.40-4.80m | 7.6m/170deg | 6.0m | drain predator, beam lane and soul burst |
| Curse Binder | `spawnEnemyCurseBinder` | 5 | 0.85m/s | Medium | 4 Tactical | territorial | 4.00-7.00m | 8.2m/150deg | 5.8m | territorial curse caster, sigil fan and curse field |
| Grave Lantern | `spawnEnemyGraveLantern` | 6 | 0.00m/s | Heavy | 2 Basic | sentinel | 5.50-8.50m | 9.2m/240deg | 7.2m | stationary magical pattern turret |

## Attack Profiles

| Enemy | Attack | Runtime | Channel | Element | Damage | Force | Range | Count | Timing | Knockback | Notes |
| --- | --- | --- | --- | --- | ---: | --- | ---: | ---: | --- | ---: | --- |
| Hollow Acolyte | `slow_soul_orb` Slow Soul Orb | Projectile | Elemental/Projectile | Soul | 1 | Light | 7.20m | 1 | 0.48/0.09/0.42s | 0.32m | M87 slow caster orb with a readable cast point and generous sidestep counter. |
| Hollow Acolyte | `rune_burst` Rune Burst | RadialProjectile | Elemental/Projectile | Soul | 1 | Medium | 5.80m | 6 | 0.62/0.10/0.58s | 0.42m | M87 radial soul pattern used when the player pressures the caster. |
| Hollow Acolyte | `veil_step` Veil Step | PhaseMove | Elemental/Area | Soul | 0 | Light | 3.20m | 0 | 0.12/0.24/0.24s | 0.00m | M87 non-damaging phase retreat that repositions locally without pathfinding. |
| Wraith | `phase_shift` Phase Shift | PhaseMove | Elemental/Area | Soul | 0 | Light | 4.40m | 0 | 0.08/0.26/0.16s | 0.00m | M87 ghost reposition with a transparency-style tell and no damage. |
| Wraith | `wraith_bolt` Wraith Bolt | Projectile | Elemental/Projectile | Soul | 1 | Light | 6.40m | 1 | 0.32/0.07/0.28s | 0.30m | M87 quick soul projectile after phase or side pressure. |
| Wraith | `curse_touch` Curse Touch | MeleeLunge | Elemental/Melee | Cursed | 1 | Light | 1.25m | 0 | 0.20/0.14/0.24s | 0.28m | M87 short active-window ghost touch; ordinary overlap remains harmless. |
| Soul Eater | `soul_drain` Soul Drain | Beam | Elemental/Area | Soul | 1 | Medium | 4.80m | 0 | 0.62/0.16/0.62s | 0.48m | M87 committed lane drain: dodge perpendicular during the cast lock. |
| Soul Eater | `soul_burst` Soul Burst | RadialProjectile | Elemental/Projectile | Soul | 1 | Medium | 5.20m | 8 | 0.50/0.10/0.52s | 0.45m | M87 radial soul release that creates readable gaps around the eater. |
| Soul Eater | `eater_phase_step` Eater Phase Step | PhaseMove | Elemental/Area | Soul | 0 | Light | 3.60m | 0 | 0.14/0.22/0.28s | 0.00m | M87 short non-damaging phase sidestep to set up drain angle. |
| Curse Binder | `binding_bolt` Binding Bolt | Projectile | Elemental/Projectile | Cursed | 1 | Light | 7.40m | 1 | 0.42/0.08/0.40s | 0.30m | M87 cursed straight projectile for territorial pressure. |
| Curse Binder | `curse_field` Curse Field | Area | Elemental/Area | Cursed | 1 | Heavy | 2.35m | 0 | 0.78/0.22/0.78s | 0.62m | M87 circular curse area: leave the sigil, then punish the long recovery. |
| Curse Binder | `sigil_fan` Sigil Fan | FanProjectile | Elemental/Projectile | Cursed | 1 | Medium | 6.80m | 5 | 0.58/0.08/0.56s | 0.40m | M87 fan of curse marks with visible lanes between shots. |
| Grave Lantern | `lantern_soul_ring` Lantern Soul Ring | RadialProjectile | Elemental/Projectile | Soul | 1 | Medium | 6.40m | 10 | 0.48/0.10/0.48s | 0.38m | M87 stationary soul ring pattern with slow projectile lanes. |
| Grave Lantern | `lantern_curse_fan` Lantern Curse Fan | FanProjectile | Elemental/Projectile | Cursed | 1 | Light | 7.60m | 5 | 0.56/0.08/0.52s | 0.32m | M87 stationary curse fan that asks for lane reading instead of contact avoidance. |
| Grave Lantern | `grave_orb` Grave Orb | Projectile | Elemental/Projectile | Soul | 1 | Light | 8.50m | 1 | 0.36/0.08/0.34s | 0.30m | M87 simple lantern shot between pattern casts. |

## Runtime Rules

- `Beam` is a profile runtime kind for committed magical lane damage. It resolves damage once at the active transition using authored range, facing arc, force, knockback, and elemental classification.
- `PhaseMove` is a non-damaging reposition action. It uses local burst movement, can ignore obstacles inside the same room bounds, and still has windup/active/recovery.
- Soul and curse attacks are elemental metadata now. M87 does not add resistances, status buildup, stealth UI, pathfinding, LOS, alert sharing, or boss runtime changes.
- Magic projectiles use the M86 projectile/fan/radial budget. Curse fields use the melee/area budget. Tactical/Cunning priority only breaks ties and does not increase total pressure.
- Ordinary body overlap remains harmless and disturbing; Wraith and Soul Eater damage only through explicit active attacks.

## Encounters And Rooms

Encounter ids: `m87_acolyte_rite`, `m87_wraith_crossing`, `m87_soul_eater_chapel`, `m87_curse_binder_sigil`, `m87_grave_lantern_pattern`.
Curated rooms: `m87_acolyte_rite_room`, `m87_wraith_crossing_room`, `m87_soul_eater_chapel_room`, `m87_curse_binder_sigil_room`, `m87_grave_lantern_pattern_room`.

## M88 Bridge

M87 deliberately keeps movement local. M88 should wrap pathfinding/local navigation behind an adapter so future casters can choose destinations, retreat points, and obstacle-aware lanes without replacing the combat action system.
