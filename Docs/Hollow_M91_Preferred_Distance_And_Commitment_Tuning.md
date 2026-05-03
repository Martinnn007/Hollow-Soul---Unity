# M91: Preferred Distance + Commitment Tuning V2

M91 turns preferred distance into an action-first spacing contract. The old `preferredRangeMinMeters` and `preferredRangeMaxMeters` fields remain valid serialized fallback metadata, but behavior-tree decisions and runtime spacing now read `EnemySpacingProfileDefinition` envelopes and per-action range overrides.

## Contract

- Preferred Distance is a soft authored envelope, not a rigid hover band.
- action-specific range overrides define desired start distance, commit range, tolerances, recovery spacing, and retreat caps.
- If an action is blocked by budget or range, an enemy may use one short reset when authored, then it must commit, hold, face, or remain punishable.
- Recovery spacing is identity-specific: weapon users stay mostly planted, creatures recoil briefly, ranged and magic enemies get one short reset or phase drift, and giants/heavies remain punishable.
- Boss spacing profiles are metadata only in M91. Boss runtime spacing remains unchanged.
- Contact damage remains M79 active-window-only.

## Current Roster Spacing Table

| Enemy | Spawn Kind | Deprecated Fallback Range | Ideal | Tolerance | Reset Cap | Fallback Recovery | Action Overrides |
| --- | --- | ---: | ---: | --- | ---: | --- | ---: |
| Normal Chaser | `spawnEnemyNormal` | 1.05-1.75m | 1.39m | -0.18/+0.24m | 1 | Recoil | 4 |
| Flying Chaser | `spawnEnemyFlying` | 2.75-4.25m | 3.47m | -0.18/+0.24m | 1 | Recoil | 5 |
| Fast Chaser | `spawnEnemyFast` | 0.90-1.45m | 1.16m | -0.18/+0.24m | 1 | Recoil | 6 |
| Heavy Chaser | `spawnEnemyHeavy` | 1.35-2.15m | 1.73m | -0.18/+0.24m | 1 | Recoil | 6 |
| Ash Charger | `spawnEnemyCharger` | 0.80-1.35m | 1.06m | -0.18/+0.24m | 1 | Recoil | 4 |
| Bone Turret | `spawnEnemyTurret` | 5.25-7.50m | 6.33m | -0.20/+0.28m | 0 | Planted | 3 |
| Husk Splitter | `spawnEnemySplitter` | 1.25-2.00m | 1.61m | -0.18/+0.24m | 1 | Recoil | 5 |
| Spitting Pod | `spawnEnemySpittingPod` | 5.50-8.00m | 6.70m | -0.20/+0.28m | 0 | Planted | 1 |
| Rat | `spawnEnemyRat` | 1.20-2.20m | 1.68m | -0.18/+0.24m | 1 | Recoil | 5 |
| Spider | `spawnEnemySpider` | 1.00-1.90m | 1.43m | -0.18/+0.24m | 1 | Recoil | 5 |
| Hollow Bird | `spawnEnemyHollowBird` | 1.80-3.60m | 2.66m | -0.18/+0.24m | 1 | Recoil | 4 |
| Hollow Beast | `spawnEnemyHollowBeast` | 1.15-2.10m | 1.61m | -0.18/+0.24m | 1 | Recoil | 4 |
| Skeleton Sword | `spawnEnemySkeletonSword` | 1.15-1.85m | 1.49m | -0.22/+0.28m | 0 | MinimalDrift | 2 |
| Skeleton Spear | `spawnEnemySkeletonSpear` | 1.75-2.75m | 2.23m | -0.22/+0.32m | 0 | MinimalDrift | 2 |
| Knight | `spawnEnemyKnight` | 1.35-2.35m | 1.83m | -0.22/+0.28m | 0 | MinimalDrift | 4 |
| Giant | `spawnEnemyGiant` | 1.85-3.10m | 2.45m | -0.30/+0.32m | 0 | Planted | 3 |
| Hollow Archer | `spawnEnemyHollowArcher` | 4.00-7.25m | 5.56m | -0.20/+0.28m | 1 | RangedReset | 4 |
| Powder Gunner | `spawnEnemyPowderGunner` | 4.75-8.50m | 6.55m | -0.20/+0.28m | 1 | RangedReset | 3 |
| Knife Thrower | `spawnEnemyKnifeThrower` | 2.70-5.25m | 3.92m | -0.20/+0.28m | 1 | RangedReset | 3 |
| Repeater Turret | `spawnEnemyRepeaterTurret` | 6.00-9.25m | 7.56m | -0.20/+0.28m | 0 | Planted | 3 |
| Clockwork Sentry | `spawnEnemyClockworkSentry` | 4.80-7.80m | 6.24m | -0.20/+0.28m | 1 | RangedReset | 3 |
| Hollow Acolyte | `spawnEnemyHollowAcolyte` | 3.80-6.80m | 5.24m | -0.20/+0.28m | 1 | RangedReset | 3 |
| Wraith | `spawnEnemyWraith` | 2.20-5.20m | 3.64m | -0.20/+0.28m | 1 | PhaseDrift | 3 |
| Soul Eater | `spawnEnemySoulEater` | 2.40-4.80m | 3.55m | -0.20/+0.28m | 1 | ShortBackstep | 3 |
| Curse Binder | `spawnEnemyCurseBinder` | 4.00-7.00m | 5.44m | -0.20/+0.28m | 1 | PhaseDrift | 3 |
| Grave Lantern | `spawnEnemyGraveLantern` | 5.50-8.50m | 6.94m | -0.20/+0.28m | 0 | Planted | 3 |

## Action-Specific Range Examples

| Enemy | Action | Desired Start | Commit Range | Recovery Spacing | Reset Cap |
| --- | --- | ---: | ---: | --- | ---: |
| Normal Chaser | `claw_lunge` | 1.05m | 0.00-1.40m | Recoil 0.28m | 1 |
| Normal Chaser | `desperate_bite` | 0.56m | 0.00-0.75m | Recoil 0.28m | 1 |
| Normal Chaser | `short_backstep` | 1.50m | 0.00-2.00m | ShortBackstep 0.42m | 1 |
| Normal Chaser | `warning_feint` | 1.80m | 0.00-2.40m | Recoil 0.28m | 1 |
| Flying Chaser | `panic_peck` | 1.01m | 0.00-1.35m | Recoil 0.28m | 1 |
| Flying Chaser | `dive_scratch` | 1.01m | 0.00-1.35m | Recoil 0.28m | 1 |
| Flying Chaser | `wing_buffet` | 0.79m | 0.00-1.05m | RangedReset 0.55m | 1 |
| Flying Chaser | `fly_strafe` | 3.38m | 0.00-4.50m | ShortBackstep 0.42m | 1 |
| Fast Chaser | `quick_pounce` | 0.94m | 0.00-1.25m | Recoil 0.28m | 1 |
| Fast Chaser | `side_pounce` | 1.09m | 0.00-1.45m | Recoil 0.28m | 1 |
| Fast Chaser | `needle_rush` | 0.56m | 0.00-0.75m | Recoil 0.28m | 1 |
| Fast Chaser | `snap_followup` | 0.79m | 0.00-1.05m | Recoil 0.28m | 1 |
| Heavy Chaser | `body_slam` | 0.60m | 0.00-0.80m | Recoil 0.28m | 1 |
| Heavy Chaser | `stomp` | 0.94m | 0.00-1.25m | Recoil 0.28m | 1 |
| Heavy Chaser | `maul_lunge` | 1.28m | 0.00-1.70m | Recoil 0.28m | 1 |
| Heavy Chaser | `heavy_shove` | 0.94m | 0.00-1.25m | Recoil 0.28m | 1 |
| Ash Charger | `ash_charge` | 4.13m | 0.00-5.50m | Recoil 0.28m | 1 |
| Ash Charger | `ember_clash` | 0.56m | 0.00-0.75m | Recoil 0.28m | 1 |
| Ash Charger | `short_recover_hop` | 1.35m | 0.00-1.80m | ShortBackstep 0.42m | 1 |
| Ash Charger | `shoulder_check` | 0.94m | 0.00-1.25m | Recoil 0.28m | 1 |
| Bone Turret | `bone_dart` | 5.60m | 2.00-8.00m | Planted 0.00m | 0 |
| Bone Turret | `braced_spike` | 5.60m | 2.00-8.00m | Planted 0.00m | 0 |
| Bone Turret | `rattle_volley` | 5.60m | 2.00-8.00m | Planted 0.00m | 0 |
| Husk Splitter | `husk_cleave` | 1.09m | 0.00-1.45m | Recoil 0.28m | 1 |
| Husk Splitter | `splinter_lunge` | 1.20m | 0.00-1.60m | Recoil 0.28m | 1 |
| Husk Splitter | `death_split` | 0.90m | 0.00-1.20m | Recoil 0.28m | 1 |
| Husk Splitter | `splitter_backstep` | 1.50m | 0.00-2.00m | ShortBackstep 0.42m | 1 |
| Spitting Pod | `spit_lob` | 5.60m | 2.00-8.00m | Planted 0.00m | 0 |
| Rat | `warning_squeal` | 1.88m | 0.00-2.50m | Recoil 0.28m | 1 |
| Rat | `rat_bite` | 0.71m | 0.00-0.95m | Recoil 0.28m | 1 |
| Rat | `skitter_retreat` | 1.65m | 0.00-2.20m | ShortBackstep 0.42m | 1 |
| Rat | `panic_pounce` | 0.90m | 0.00-1.20m | Recoil 0.28m | 1 |
| Spider | `startle_hop` | 0.86m | 0.00-1.15m | Recoil 0.28m | 1 |
| Spider | `close_bite` | 0.56m | 0.00-0.75m | Recoil 0.28m | 1 |
| Spider | `side_hop_bite` | 0.83m | 0.00-1.10m | Recoil 0.28m | 1 |
| Spider | `panic_flee` | 1.50m | 0.00-2.00m | ShortBackstep 0.42m | 1 |
| Hollow Bird | `swoop_peck` | 1.01m | 0.00-1.35m | Recoil 0.28m | 1 |
| Hollow Bird | `claw_dive` | 1.16m | 0.00-1.55m | Recoil 0.28m | 1 |
| Hollow Bird | `wing_retreat` | 2.85m | 0.00-3.80m | ShortBackstep 0.42m | 1 |
| Hollow Bird | `caw_signal` | 3.90m | 0.00-5.20m | Recoil 0.28m | 1 |
| Hollow Beast | `leap_bite` | 1.09m | 0.00-1.45m | Recoil 0.28m | 1 |
| Hollow Beast | `body_check` | 1.24m | 0.00-1.65m | Recoil 0.28m | 1 |
| Hollow Beast | `leap_back` | 1.65m | 0.00-2.20m | ShortBackstep 0.42m | 1 |
| Hollow Beast | `howl_signal` | 4.13m | 0.00-5.50m | Recoil 0.28m | 1 |
| Skeleton Sword | `rusty_slash` | 1.09m | 0.00-1.45m | MinimalDrift 0.10m | 0 |
| Skeleton Sword | `backhand_slash` | 1.01m | 0.00-1.35m | MinimalDrift 0.10m | 0 |
| Skeleton Spear | `spear_thrust` | 1.80m | 0.00-2.40m | MinimalDrift 0.10m | 0 |
| Skeleton Spear | `spear_sweep` | 1.24m | 0.00-1.65m | MinimalDrift 0.10m | 0 |
| Knight | `shield_guard` | 1.69m | 0.00-2.25m | Planted 0.00m | 0 |
| Knight | `knight_slash` | 1.24m | 0.00-1.65m | MinimalDrift 0.06m | 0 |
| Knight | `knight_thrust` | 1.61m | 0.00-2.15m | MinimalDrift 0.06m | 0 |
| Knight | `shield_bash` | 0.86m | 0.00-1.15m | MinimalDrift 0.06m | 0 |
| Giant | `club_sweep` | 1.69m | 0.00-2.25m | Planted 0.00m | 0 |
| Giant | `overhead_slam` | 1.16m | 0.00-1.55m | Planted 0.00m | 0 |
| Giant | `stomp` | 0.94m | 0.00-1.25m | Planted 0.00m | 0 |
| Hollow Archer | `arrow_shot` | 5.25m | 2.00-7.50m | RangedReset 0.55m | 1 |
| Hollow Archer | `retreating_arrow` | 4.69m | 2.00-6.70m | RangedReset 0.55m | 1 |
| Hollow Archer | `arrow_volley` | 5.08m | 2.00-7.25m | RangedReset 0.55m | 1 |
| Hollow Archer | `archer_backstep` | 1.88m | 0.00-2.50m | ShortBackstep 0.42m | 1 |
| Powder Gunner | `aimed_musket_shot` | 6.16m | 2.00-8.80m | RangedReset 0.55m | 1 |
| Powder Gunner | `scatter_shot` | 3.78m | 1.89-5.40m | RangedReset 0.55m | 1 |
| Powder Gunner | `gunner_backstep` | 1.88m | 0.00-2.50m | ShortBackstep 0.42m | 1 |
| Knife Thrower | `throwing_knife` | 4.06m | 2.00-5.80m | RangedReset 0.55m | 1 |
| Knife Thrower | `knife_fan` | 3.36m | 1.68-4.80m | RangedReset 0.55m | 1 |
| Knife Thrower | `thrower_backstep` | 1.58m | 0.00-2.10m | ShortBackstep 0.42m | 1 |
| Repeater Turret | `repeater_burst` | 5.95m | 2.00-8.50m | Planted 0.00m | 0 |
| Repeater Turret | `suppressing_arc` | 5.60m | 2.00-8.00m | Planted 0.00m | 0 |
| Repeater Turret | `lock_on_dart` | 6.48m | 2.00-9.25m | Planted 0.00m | 0 |
| Clockwork Sentry | `clockwork_radial` | 4.55m | 2.00-6.50m | RangedReset 0.55m | 1 |
| Clockwork Sentry | `rotating_fan` | 4.90m | 2.00-7.00m | RangedReset 0.55m | 1 |
| Clockwork Sentry | `gear_shot` | 5.46m | 2.00-7.80m | RangedReset 0.55m | 1 |
| Hollow Acolyte | `slow_soul_orb` | 5.04m | 2.00-7.20m | RangedReset 0.55m | 1 |
| Hollow Acolyte | `rune_burst` | 4.06m | 2.00-5.80m | RangedReset 0.55m | 1 |
| Hollow Acolyte | `veil_step` | 1.76m | 0.40-3.20m | ShortBackstep 0.42m | 1 |
| Wraith | `phase_shift` | 2.42m | 0.40-4.40m | PhaseDrift 0.65m | 1 |
| Wraith | `wraith_bolt` | 4.48m | 2.00-6.40m | PhaseDrift 0.65m | 1 |
| Wraith | `curse_touch` | 0.94m | 0.00-1.25m | PhaseDrift 0.65m | 1 |
| Soul Eater | `soul_drain` | 3.36m | 1.68-4.80m | PhaseDrift 0.65m | 1 |
| Soul Eater | `soul_burst` | 3.64m | 1.82-5.20m | PhaseDrift 0.65m | 1 |
| Soul Eater | `eater_phase_step` | 1.98m | 0.40-3.60m | PhaseDrift 0.65m | 1 |
| Curse Binder | `binding_bolt` | 5.18m | 2.00-7.40m | PhaseDrift 0.65m | 1 |
| Curse Binder | `curse_field` | 1.76m | 0.00-2.35m | PhaseDrift 0.65m | 1 |
| Curse Binder | `sigil_fan` | 4.76m | 2.00-6.80m | PhaseDrift 0.65m | 1 |
| Grave Lantern | `lantern_soul_ring` | 4.48m | 2.00-6.40m | Planted 0.00m | 0 |
| Grave Lantern | `lantern_curse_fan` | 5.32m | 2.00-7.60m | Planted 0.00m | 0 |
| Grave Lantern | `grave_orb` | 5.95m | 2.00-8.50m | Planted 0.00m | 0 |

## Boss Metadata

| Boss | Metadata Profile | Action Overrides | Runtime Use |
| --- | --- | ---: | --- |
| Stone Warden | `stone_warden_m91_spacing_metadata` | 3 | ignored by boss runtime in M91 |
| Splinter Saint | `splinter_saint_m91_spacing_metadata` | 1 | ignored by boss runtime in M91 |
| Gravel Maw | `gravel_maw_m91_spacing_metadata` | 2 | ignored by boss runtime in M91 |
| Cartouche Widow | `cartouche_widow_m91_spacing_metadata` | 1 | ignored by boss runtime in M91 |
| Iron Reliquary | `iron_reliquary_m91_spacing_metadata` | 2 | ignored by boss runtime in M91 |
| Mirror Husk | `mirror_husk_m91_spacing_metadata` | 2 | ignored by boss runtime in M91 |
| Ash Comet | `ash_comet_m91_spacing_metadata` | 2 | ignored by boss runtime in M91 |
| Choir of Teeth | `choir_of_teeth_m91_spacing_metadata` | 2 | ignored by boss runtime in M91 |
| Rust Bishop | `rust_bishop_m91_spacing_metadata` | 2 | ignored by boss runtime in M91 |
| Hollow Star Larva | `hollow_star_larva_m91_spacing_metadata` | 3 | ignored by boss runtime in M91 |

## Tuning Notes

- Chasers and creature enemies are allowed to enter attack range instead of hovering at fallback min/max edges.
- Ranged/firearm/caster profiles prefer a readable reset/backstep once, then hold or fire rather than endlessly retreating.
- Weapon-user recovery is intentionally planted or tiny drift so whiffs stay punishable.
- Phase-drift recovery is reserved for ghost/magic identities and remains local; no new pathfinding backend is introduced.
- The next useful feel pass should tune action profiles and behavior tree weights together, not reintroduce hard distance gates.
