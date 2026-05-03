# M83: Noise + Disturbance V2

M83 turns the M75 stimulus spine into a small Souls-like disturbance layer. Footsteps, rolls, light and heavy attacks, guard impacts, proximity, and harmless body bumps now carry a tier, accumulate local disturbance, and resolve through enemy disposition instead of a universal aggro switch.

## Runtime Contract

- Stimuli use `EnemyStimulusTier`: `Quiet`, `Normal`, `Loud`, and `Violent`.
- Footsteps are `Quiet`; rolls and light attacks are `Normal`; heavy attacks and guard/block/parry impacts are `Loud`; direct damage is `Violent`.
- Enemy definitions author `hearingSensitivityMultiplier`, `disturbanceEscalationThreshold`, and `investigationDurationSeconds`.
- Predators investigate and then commit; prey startle/flee or panic when close/attacked; sentinels face/hold until disturbance warrants attacks; territorial enemies warn before aggression; mindless enemies pressure simply.
- Ordinary M79 body contact remains harmless. Bumps emit `EnemyStimulusKind.Bump`, separate bodies lightly, and feed the same disturbance rules.
- Runtime diagnostics expose the last stimulus kind/tier/time/position, awareness reason, and current disturbance score for debug tooling only.
- Bosses are metadata/docs only for M83; boss runtime behavior is unchanged.

## Tier Table

| Source | Kind | Tier |
| --- | --- | --- |
| Player footstep pulse | `Footstep` | `Quiet` |
| Player roll | `Roll` | `Normal` |
| Light melee / ranged | `MeleeAttack` / `RangedAttack` | `Normal` |
| Heavy melee / ranged | `MeleeAttack` / `RangedAttack` | `Loud` |
| Guard, block, or parry impact | `GuardImpact` | `Loud` |
| Passive overlap bump | `Bump` | `Normal` |
| Direct damage | `Damage` | `Violent` |

## Roster Tuning

| Enemy | Hearing Sensitivity | Escalation Threshold | Investigation | Notes |
| --- | ---: | ---: | ---: | --- |
| Normal Chaser | x1.00 | 1.50 | 1.40s | near-default predator investigation |
| Flying Chaser | x1.20 | 1.20 | 1.00s | startles, flees, then panics when close or attacked |
| Fast Chaser | x1.05 | 1.45 | 1.20s | near-default predator investigation |
| Heavy Chaser | x0.80 | 1.80 | 1.20s | simple disturbance pressure |
| Ash Charger | x1.00 | 1.40 | 1.10s | near-default predator investigation |
| Bone Turret | x1.35 | 1.20 | 1.70s | faces/holds until disturbance warrants fire |
| Husk Splitter | x1.00 | 1.45 | 1.20s | near-default predator investigation |
| Spitting Pod | x1.60 | 0.45 | 1.60s | faces/holds until disturbance warrants fire |
| Rat | x1.35 | 1.10 | 0.85s | warns/paces before committing |
| Spider | x1.45 | 0.90 | 0.70s | startles, flees, then panics when close or attacked |

## Deferred

- No stealth UI, pathfinding, obstacle LOS, alert sharing, save migration, or boss behavior changes are included.
- Later milestones can add noise surfaces, richer investigation paths, limited alert sharing, and dedicated stealth feedback without replacing this local disturbance contract.
