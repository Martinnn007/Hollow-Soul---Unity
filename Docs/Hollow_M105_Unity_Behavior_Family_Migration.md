# M105: Unity Behavior Family Migration

M105 migrates normal enemies from one-off Unity Behavior pilots into family-level intent graphs. Unity Behavior chooses intent; Hollow keeps damage math, action profiles, spacing, NavMesh locomotion, pressure budgets, active windows, and boss exemptions.

## Family Contracts

| Family | Spawn kinds | Intent responsibility |
| --- | --- | --- |
| Critters | Rat, Spider, Hollow Bird, Hollow Beast | Wander, startle, warn/signal, flee, or request body attack intent. |
| Chasers | Normal, Flying, Fast, Heavy, Ash Charger, Husk Splitter | Pressure, close to action envelope, flee if prey, request melee/area/charge intent. |
| Weapon Users | Skeleton Sword, Skeleton Spear, Knight, Giant | Face/approach, guard when appropriate, request weapon melee or area intent. |
| Ranged + Firearm | Turrets, Pod, Archer, Gunner, Thrower, Repeater, Clockwork | Hold lines, reset distance, request ranged fire intent. |
| Magic + Ghost | Acolyte, Wraith, Soul Eater, Curse Binder, Grave Lantern | Cast, phase/reposition, apply area pressure, or hold occult range. |

## Source Of Truth

- Unity Behavior outputs `EnemyBehaviorCommand` intent only.
- Empty action ids are intentional for family graphs; `EnemyActionScorer` selects the concrete Hollow action profile.
- Runtime damage, knockback, guard recoil, windup/active/recovery, cooldowns, projectile data, and pressure budgets stay in Hollow profiles.
- Emergency fallback remains explicit and trace-visible until official family graph assets are authored and assigned.
- Boss runtime remains unchanged.
