# M76: Enemy Attack Profiles + Impact Catalogue V1

M76 moves enemy and boss impact tuning into authored attack profile assets. Runtime behavior remains behavior-specific, but damage type, delivery, element, force class, knockback, guard recoil, cooldown, and range now come from attack profiles.

## Runtime Contract

- Separate `EnemyAttackProfileDefinition` assets are the source of truth for attack impact data.
- Runtime keeps existing enemy and boss behavior patterns while resolving contact, lunge, charge, projectile, split, and summon profiles by attack id.
- `DamageClassification` remains the shared taxonomy: channel, delivery, element, and force class.
- Guarded non-parry hits apply reduced recoil from `guardKnockbackMultiplier`; perfect parry prevents player recoil.
- Existing `ActiveStability` thresholds still reduce or cancel received knockback after guard recoil is calculated.
- Balance stays readability-first: average M75 damage and pressure should remain familiar.

## Enemy Attack Profiles

| Owner | Attack | Runtime | Classification | Force | Threat | Damage | Knockback | Guard Recoil | Cooldown | Range | Notes |
| --- | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| Normal Chaser | Claw Lunge | MeleeLunge | Physical/Melee | Light | Light | 1 | 0.35m | x0.35 | 1.15s | 1.40m | Primary M75 lunge profile. |
| Normal Chaser | Desperate Bite | Contact | Physical/Contact | Light | Light | 1 | 0.30m | x0.35 | 1.00s | 0.70m | Fallback body/contact bite. |
| Flying Chaser | Panic Peck | MeleeLunge | Physical/Melee | Light | Light | 1 | 0.25m | x0.35 | 1.15s | 1.35m | Prey panic lunge when endangered. |
| Flying Chaser | Dive Scratch | MeleeLunge | Physical/Melee | Medium | Heavy | 1 | 0.45m | x0.35 | 1.25s | 1.35m | Sharper engaged dive; no new pathfinding. |
| Flying Chaser | Wing Buffet | Area | Physical/Melee | Medium | Heavy | 1 | 0.50m | x0.35 | 1.80s | 1.05m | Catalogue pressure profile for close-range shove. |
| Fast Chaser | Quick Pounce | MeleeLunge | Physical/Melee | Light | Light | 1 | 0.30m | x0.35 | 1.05s | 1.25m | Fast primary lunge. |
| Fast Chaser | Needle Rush | Contact | Physical/Contact | Medium | Heavy | 1 | 0.45m | x0.35 | 0.85s | 0.65m | Contact pressure while weaving through the player. |
| Fast Chaser | Snap Followup | MeleeLunge | Physical/Melee | Light | Light | 1 | 0.25m | x0.35 | 1.35s | 1.05m | Short fallback bite profile. |
| Heavy Chaser | Body Slam | Contact | Physical/Contact | Heavy | Heavy | 2 | 0.75m | x0.35 | 1.10s | 0.80m | High-stability contact threat. |
| Heavy Chaser | Maul Lunge | MeleeLunge | Physical/Melee | Heavy | Heavy | 2 | 0.70m | x0.35 | 1.35s | 1.70m | Slow readable heavy lunge. |
| Heavy Chaser | Heavy Shove | MeleeLunge | Physical/Melee | Medium | Heavy | 1 | 0.60m | x0.35 | 1.45s | 1.25m | Lower-damage control hit. |
| Ash Charger | Ash Charge | Charge | Elemental/Contact/Fire | Heavy | Heavy | 1 | 0.80m | x0.35 | 2.00s | 5.50m | Existing charge with explicit fire identity. |
| Ash Charger | Ember Clash | Contact | Physical/Melee | Medium | Heavy | 1 | 0.50m | x0.35 | 1.00s | 0.70m | Fallback body clash outside active charge. |
| Bone Turret | Bone Dart | Projectile | Physical/Projectile | Light | Light | 1 | 0.35m | x0.35 | 1.35s | 8.00m | Standard sentinel shot. |
| Bone Turret | Braced Spike | Projectile | Physical/Projectile | Medium | Heavy | 1 | 0.48m | x0.35 | 1.80s | 8.00m | Slower stronger ranged shot. |
| Bone Turret | Rattle Volley | Projectile | Physical/Projectile | Light | Light | 1 | 0.30m | x0.35 | 1.60s | 8.00m | Catalogue volley profile; current runtime fires one projectile in V1. |
| Husk Splitter | Husk Cleave | MeleeLunge | Physical/Melee | Medium | Heavy | 1 | 0.50m | x0.35 | 1.25s | 1.45m | Basic predator cleave. |
| Husk Splitter | Splinter Lunge | MeleeLunge | Physical/Melee | Medium | Heavy | 1 | 0.45m | x0.35 | 1.15s | 1.60m | Primary M75 splitter lunge. |
| Husk Splitter | Death Split | Split | Physical/Area | Medium | Heavy | 0 | 0.35m | x0.35 | 0.10s | 1.20m | Metadata/runtime event profile for split children. |
| Stone Warden Spawn | Stone Warden Contact | Contact | Physical/Contact | Massive | Boss | 2 | 0.90m | x0.35 | 3.20s | 2.20m | Legacy generic boss spawn fallback contact profile. |
| Stone Warden Spawn | Stone Warden Burst Shards | RadialProjectile | Physical/Projectile | Heavy | StrongProjectile | 1 | 0.55m | x0.35 | 3.80s | 6.00m | Legacy generic boss spawn fallback low-health burst. |

## Boss Attack Profiles

| Owner | Attack | Runtime | Classification | Force | Threat | Damage | Knockback | Guard Recoil | Cooldown | Range | Notes |
| --- | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| Stone Warden | Stone Charge | Charge | Physical/Contact | Massive | Boss | 1 | 0.90m | x0.35 | 3.20s | 2.20m | Stone Warden dash contact. |
| Stone Warden | Stone Stomp Burst | RadialProjectile | Physical/Projectile | Heavy | Boss | 2 | 0.65m | x0.35 | 4.60s | 6.00m | Radial stomp debris. |
| Stone Warden | Stone Four-Way Burst | RadialProjectile | Physical/Projectile | Heavy | StrongProjectile | 1 | 0.55m | x0.35 | 3.80s | 6.00m | Low-health cardinal burst. |
| Splinter Saint | Side-Hop Radial | RadialProjectile | Physical/Projectile | Light | Light | 1 | 0.35m | x0.35 | 1.75s | 5.00m | Side hop shot ring. |
| Gravel Maw | Burrow Summon | Summon | Physical/Area | Medium | Heavy | 0 | 0.35m | x0.35 | 7.00s | 1.20m | Summons a small gravel pack. |
| Gravel Maw | Rubble Spray | RadialProjectile | Physical/Projectile | Light | Light | 1 | 0.35m | x0.35 | 7.00s | 5.00m | Rubble spray after summon. |
| Cartouche Widow | Falling Marks | FanProjectile | Physical/Projectile | Light | Light | 1 | 0.35m | x0.35 | 1.65s | 6.00m | Fan pressure pattern. |
| Iron Reliquary | Peek Shot | FanProjectile | Physical/Projectile | Light | Light | 1 | 0.35m | x0.35 | 1.90s | 6.00m | Three-shot cover peek. |
| Iron Reliquary | Relocate Bash | Movement | Physical/Contact | Heavy | Heavy | 1 | 0.55m | x0.35 | 4.20s | 1.10m | Retreat bash profile for relocate movement. |
| Mirror Husk | Mirror Chase Contact | Contact | Physical/Contact | Heavy | Boss | 1 | 0.55m | x0.35 | 1.00s | 0.80m | Chase contact body profile. |
| Mirror Husk | Mirror Split | Split | Physical/Area | Medium | Heavy | 0 | 0.35m | x0.35 | 0.10s | 1.20m | Split event profile. |
| Ash Comet | Comet Dash | Charge | Elemental/Contact/Fire | Massive | Boss | 2 | 0.95m | x0.35 | 2.60s | 2.20m | Fire-aspected dash contact. |
| Ash Comet | Ash Fire Radial | RadialProjectile | Elemental/Projectile/Fire | Heavy | Boss | 2 | 0.65m | x0.35 | 2.60s | 6.00m | Fire radial after dash. |
| Choir of Teeth | Rotating Hymn | RadialProjectile | Physical/Projectile | Light | Light | 1 | 0.32m | x0.35 | 2.20s | 6.00m | Rotating tooth ring. |
| Choir of Teeth | Tooth Storm | RadialProjectile | Physical/Projectile | Heavy | StrongProjectile | 2 | 0.60m | x0.35 | 4.20s | 6.00m | Low-health dense tooth storm. |
| Rust Bishop | Rust Beam | FanProjectile | Physical/Projectile | Heavy | StrongProjectile | 2 | 0.65m | x0.35 | 2.80s | 6.00m | Narrow heavy fan used as beam V1. |
| Rust Bishop | Mine Pattern | RadialProjectile | Physical/Projectile | Light | Light | 1 | 0.35m | x0.35 | 3.60s | 5.00m | Radial mine-like pressure pattern. |
| Hollow Star Larva | Abyss Call | Summon | Elemental/Area/Cosmic | Medium | Heavy | 0 | 0.40m | x0.35 | 0.10s | 1.20m | Cosmic summon event. |
| Hollow Star Larva | Starfall | FanProjectile | Elemental/Projectile/Cosmic | Light | Light | 1 | 0.38m | x0.35 | 2.10s | 6.00m | Cosmic fan pattern. |
| Hollow Star Larva | Desperation | RadialProjectile | Elemental/Projectile/Cosmic | Heavy | StrongProjectile | 2 | 0.65m | x0.35 | 3.10s | 6.00m | Cosmic desperation storm. |

## Compatibility

- No save schema change; attack profiles resolve from current catalog data on Continue.
- No elemental resistance system is added in M76.
- No generic attack planner is added; behavior-specific AI remains.
