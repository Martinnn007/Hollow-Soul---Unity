# M79: Contact Damage Rework V1

M79 changes enemy body overlap from automatic damage into a disturbance event. Ordinary enemies can be bumped or overlapped without hurting the player; explicit attacks, projectiles, room hazards, and future hazardous bodies remain the sources of damage.

## Runtime Contract

- `EnemyContactDamagePolicy.ActiveOnly`: body damage is allowed only during an explicit active attack window.
- `EnemyContactDamagePolicy.PassiveHazard`: body overlap can tick damage on contact cooldowns when a non-None hazard type is authored.
- `EnemyContactDamagePolicy.Disabled`: body overlap never applies contact damage.
- Current normal enemies and bosses are `ActiveOnly` with `EnemyPassiveContactHazardType.None`.
- Idle, chase, hold, wander, retreat, windup, entry grace, stun/death, ranged windup, and ordinary overlap do not damage the player.
- `MeleeLunge`, `Charging`, and armed boss dash/bash windows can damage once per activation using their M76 attack profiles.
- Non-hazard overlap emits `EnemyStimulusKind.Proximity`, so bumping an enemy can alert, engage, or startle it without reducing player HP.
- Existing projectile, room hazard, guard, knockback, split child, and boss projectile behavior remains unchanged.

## Current Roster Contact Policy

| Enemy Spawn | Policy | Passive Hazard |
| --- | --- | --- |
| `spawnEnemyBoss` | ActiveOnly | None |
| `spawnEnemyCharger` | ActiveOnly | None |
| `spawnEnemyFast` | ActiveOnly | None |
| `spawnEnemyFlying` | ActiveOnly | None |
| `spawnEnemyHeavy` | ActiveOnly | None |
| `spawnEnemyNormal` | ActiveOnly | None |
| `spawnEnemyRat` | ActiveOnly | None |
| `spawnEnemySpider` | ActiveOnly | None |
| `spawnEnemySpittingPod` | ActiveOnly | None |
| `spawnEnemySplitter` | ActiveOnly | None |
| `spawnEnemyTurret` | ActiveOnly | None |

## Boss Contact Bridge

- Stone Warden `stone_charge`, Iron Reliquary `iron_relocate_bash`, and Ash Comet `ash_comet_dash` arm short active contact windows.
- Mirror Husk chase overlap is harmless in M79 until a later milestone gives it an explicit active attack window.
- Boss HUD/readability and projectile attacks stay unchanged.

## Compatibility

- No save schema change; Continue derives contact policy and hazard type from the current catalog.
- The legacy contact damage and cooldown fields remain for tuning active body attacks and future passive hazards.
- M79 does not add behavior trees, pathfinding, line of sight, alert sharing, new attacks, or new enemies.
