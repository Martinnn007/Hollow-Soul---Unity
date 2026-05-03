# M77: Critter Roster + Ballistic Creature Behaviors V1

M77 adds Spitting Pod, Rat, and Spider as early mixed enemies. They use the existing intelligence, senses, movement intent, and M76 attack profile systems, plus a small shared critter behavior layer for readable chaotic movement.

## Enemy Stat Cards

| Enemy | Spawn Kind | Behavior | HP | Speed | Radius | Intelligence | Disposition | Sight | Hearing | Preferred Range | Attacks |
| --- | --- | --- | ---: | ---: | ---: | --- | --- | --- | ---: | --- | --- |
| Spitting Pod | `spawnEnemySpittingPod` | SpittingPod | 10 | 0.00m/s | 0.44m | Simple | sentinel | 0.0m/0deg | 9.0m | 5.5-8.0m | spit_lob |
| Rat | `spawnEnemyRat` | Rat | 3 | 2.65m/s | 0.20m | Basic | territorial | 8.0m/260deg | 7.5m | 1.2-2.2m | rat_bite |
| Spider | `spawnEnemySpider` | Spider | 2 | 2.90m/s | 0.22m | Simple | prey | 8.5m/300deg | 8.0m | 1.0-1.9m | startle_hop, close_bite |

## Attack Profiles

| Owner | Attack | Classification | Force | Damage | Knockback | Cooldown | Notes |
| --- | --- | --- | --- | ---: | ---: | ---: | --- |
| Spitting Pod | spit_lob | Physical/Projectile | Light | 1 | 0.35m | 1.00s | Visible ballistic arc with a small non-lingering splash. |
| Rat | rat_bite | Physical/Melee | Light | 1 | 0.22m | 0.90s | Territorial close bite after warning or disturbance. |
| Spider | startle_hop | Physical/Melee | Light | 1 | 0.30m | 0.85s | Chaotic hop-forward attack chosen after startle. |
| Spider | close_bite | Physical/Melee | Light | 1 | 0.22m | 0.75s | Very close panic bite. |

## Runtime Behavior Contract

- Spitting Pod is stationary, blind, hearing-driven, and fires visible ballistic lob projectiles with a small non-lingering splash.
- Rat uses territorial awareness: it roams chaotically, warns/pressures before biting, and retreats readily after damage.
- Spider uses readable chaotic fight-or-flight decisions, with fast retreat bursts and quick hop or bite attacks.
- New attacks remain Physical/Natural metadata; no poison, acid, elemental resistance, pathfinding, obstacle LOS, or squad behavior is added.

## Encounter And Room Coverage

- Early mixed encounter rotation adds `m77_pod_warning`, `m77_rat_scramble`, `m77_spider_scuttle`, and `m77_critter_mix`.
- Curated showcase rooms are generated under `Assets/_Hollow/Data/Rooms/DesignerApproved/M77/`.
- Bespoke critter encounter rooms: `m77_spider_brood_den_wide`, `m77_rat_warren_single`, `m77_rocky_spider_pod_wide`, and `m77_rocky_rat_pod_wide`.
- Presentation roles and material roles are added for art-pass-ready placeholder replacement.

## Compatibility

- No save schema change; enemy type, intelligence, disposition, senses, and attack profiles derive from current catalog data.
- Boss runtime behavior remains unchanged.
- Existing M72/M74/M75/M76 systems remain the shared contract for awareness, movement intent, lunge/ranged budgets, and attack profile impact data.
