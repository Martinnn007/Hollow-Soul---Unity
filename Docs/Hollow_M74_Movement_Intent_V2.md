# M74: Movement Intent V2

M74 adds authored preferred range bands and a lightweight local steering pass for Hollow's non-boss enemy movement. The feature sharpens roster identity while staying deliberately local: no pathfinding, no line of sight, no squad tactics, no home leash system, and no boss behavior changes.

## Runtime Contract

- Preferred range steering applies only during ordinary chase, wander, and hold movement.
- Windups, active charges, stun, death, entry grace, ranged attacks, contact damage, and boss behavior remain unchanged.
- Intelligence controls precision: Instinctive enemies use bands mainly for prey retreat and anti-shove smoothing, Simple enemies are loose, and Basic or higher enemies respect bands more cleanly.
- separation is a soft nudge away from nearby living non-boss enemies.
- Player contact smoothing uses a small contact buffer so enemies stop constantly shoving into the player while still allowing hits and brief overlaps.
- Retreat behavior uses short readable bursts of about 0.75 seconds, then reassesses.

## Current Roster Range Table

| Enemy | Preferred Min | Preferred Max | Notes |
|---|---:|---:|---|
| Normal Chaser | 1.05m | 1.75m | Loose direct pressure. |
| Flying Chaser | 2.75m | 4.25m | Prey retreat and wander band. |
| Fast Chaser | 0.90m | 1.45m | Close fast pressure. |
| Heavy Chaser | 1.35m | 2.15m | Slower mindless pressure with more body room. |
| Ash Charger | 0.80m | 1.35m | Instinctive predator; charge behavior unchanged. |
| Bone Turret | 5.25m | 7.50m | Stationary data envelope only. |
| Husk Splitter | 1.25m | 2.00m | Basic predator spacing. |
| Stone Warden Spawn | 4.50m | 6.50m | Data completeness only; boss behavior unchanged. |

## Deferred Work

- Home leash behavior is intentionally deferred.
- Obstacle steering, pathfinding, line of sight, and squad coordination are outside M74.
- Future movement milestones can add authored leash clarity only after this local steering layer is stable.
