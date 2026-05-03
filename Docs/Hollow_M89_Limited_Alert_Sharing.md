# M89: Limited Alert Sharing V1

M89 adds a restrained ally-alert layer over the M83 disturbance system. Selected non-boss enemies can wake nearby allies when they meaningfully escalate to combat or are damaged, but the recipient still resolves the warning through its own disposition, awareness, hearing, and behavior tree. This is not squad tactics.

## Runtime Contract

- New stimulus kind: `EnemyStimulusKind.AllyAlert`.
- Alert sharing is authored on `EnemyDefinition` with enable flag, radius, cooldown, and minimum source awareness.
- Sources broadcast only through `RoomCombatController.EmitEnemyAllyAlert`; they never directly edit another enemy's movement or state.
- Recipients hear the alert through normal hearing sensitivity and disposition logic.
- `AllyAlert` and `CreatureSignal` do not recursively trigger another ally-alert broadcast, preventing room-wide chains.
- Bosses are exempt as sources and recipients.
- M88 remains the movement boundary: awakened enemies investigate, face, return-home, or attack through navigation intents.
- No pathfinding, obstacle LOS, squad tactics, formation behavior, boss runtime change, or save schema change is included.

## Selected Alert Sources

| Enemy | Enabled | Radius | Cooldown | Minimum Awareness | Notes |
| --- | ---: | ---: | ---: | --- | --- |
| Normal Chaser | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Flying Chaser | False | 0.00m | 2.00s | Engaged | solo/startle behavior; no ally broadcast |
| Fast Chaser | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Heavy Chaser | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Ash Charger | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Bone Turret | True | 4.00m | 2.25s | Engaged | stationary sentinel warning radius |
| Husk Splitter | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Spitting Pod | True | 4.75m | 2.25s | Engaged | stationary sentinel warning radius |
| Rat | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Spider | False | 0.00m | 2.00s | Engaged | solo/startle behavior; no ally broadcast |
| Hollow Bird | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Hollow Beast | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Skeleton Sword | True | 3.50m | 2.00s | Engaged | short practical local warning |
| Skeleton Spear | True | 4.25m | 2.00s | Engaged | short practical local warning |
| Knight | True | 5.00m | 2.40s | Engaged | disciplined local wake-up call |
| Giant | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Hollow Archer | True | 5.25m | 2.25s | Engaged | short practical local warning |
| Powder Gunner | True | 5.50m | 2.50s | Engaged | disciplined local wake-up call |
| Knife Thrower | True | 4.25m | 2.10s | Engaged | short practical local warning |
| Repeater Turret | True | 4.50m | 2.25s | Engaged | stationary sentinel warning radius |
| Clockwork Sentry | True | 6.00m | 2.75s | Engaged | disciplined local wake-up call |
| Hollow Acolyte | True | 5.00m | 2.25s | Engaged | disciplined local wake-up call |
| Wraith | False | 0.00m | 2.00s | Engaged | stays local or uses existing creature-family signals |
| Soul Eater | True | 4.50m | 2.40s | Engaged | disciplined local wake-up call |
| Curse Binder | True | 5.75m | 2.60s | Engaged | disciplined local wake-up call |
| Grave Lantern | True | 6.00m | 2.50s | Engaged | stationary sentinel warning radius |

## Disposition Responses

- `Predator`: investigates the warning; loud or repeated pressure can commit it.
- `Prey`: startles or raises suspicion instead of becoming a pure chaser.
- `Sentinel`: faces/holds first, then attacks only if disturbance pressure warrants it.
- `Territorial`: warns or paces before committing.
- `Mindless`: turns toward pressure simply, with less nuance.

## M90 QA Notes

M90 should manually check mixed rooms with weapon users, ranged enemies, and casters to ensure alert sharing makes rooms feel alive without producing instant dogpiles. The desired feel is Dark Souls-like: allies notice noise and combat nearby, but each enemy still commits to readable individual actions.
