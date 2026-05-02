# M75: Enemy Attacks + Senses V1

M75 gives every contact-capable enemy a short authored lunge attack and adds local senses that drive lightweight awareness. The intent is to fix preferred-range stalls without adding pathfinding, obstacle line of sight, alert sharing, saved awareness state, or boss behavior changes.

## Runtime Contract

- Enemy Attacks: Normal, Fast, Heavy, Flying, and Husk Splitter can start a melee lunge from the edge of their preferred range.
- Ash Charger keeps its charge attack. Bone Turret stays stationary and ranged-only. Boss runtime behavior remains unchanged.
- Senses: sight uses radius plus cone angle only, and hearing uses simple local stimulus radius checks.
- Awareness: enemies move through Unaware, Suspicious, Alerted, and Engaged. Once Engaged, they stay Engaged until death, room reset, or Continue.
- Stimuli: footsteps can raise suspicion, player attacks force engagement inside hearing range, and direct damage always engages the target.
- Budgets: melee lunges use a separate 0.30s room budget. Ranged and charge attacks keep the existing M72 pressure budget where only Tactical and Cunning gain priority bonuses.

## Current Roster Sense And Lunge Table

| Enemy | Sight | Cone | Hearing | Lunge |
|---|---:|---:|---:|---|
| Normal Chaser | 6.5m | 150deg | 4.5m | yes, 1.40m |
| Flying Chaser | 7.5m | 240deg | 6.5m | yes, 1.35m, endangered or engaged |
| Fast Chaser | 7.0m | 170deg | 5.0m | yes, 1.25m |
| Heavy Chaser | 5.0m | 110deg | 3.5m | yes, 1.70m |
| Ash Charger | 7.0m | 120deg | 5.0m | no, charge attack only |
| Bone Turret | 9.5m | 70deg | 2.5m | no, ranged-only sentinel |
| Husk Splitter | 6.5m | 160deg | 5.0m | yes, 1.60m |
| Stone Warden Spawn | 8.0m | 160deg | 4.5m | no, data completeness |

## Current Boss Sense Metadata

| Boss | Sight | Cone | Hearing | Runtime Policy |
|---|---:|---:|---:|---|
| Stone Warden | 8.0m | 140deg | 5.0m | metadata only |
| Splinter Saint | 8.0m | 180deg | 5.5m | metadata only |
| Gravel Maw | 6.5m | 110deg | 6.0m | metadata only |
| Cartouche Widow | 10.0m | 220deg | 6.5m | metadata only |
| Iron Reliquary | 8.5m | 120deg | 4.0m | metadata only |
| Mirror Husk | 9.0m | 220deg | 6.0m | metadata only |
| Ash Comet | 9.0m | 160deg | 7.0m | metadata only |
| Choir of Teeth | 10.0m | 300deg | 7.0m | metadata only |
| Rust Bishop | 9.5m | 180deg | 5.5m | metadata only |
| Hollow Star Larva | 0.0m | 0deg | 9.5m | metadata only, blind hearing-forward profile |

## Deferred Work

- No pathfinding, obstacle line of sight, squad tactics, stealth UI, or alert sharing.
- Awareness timers and stimuli reset on Continue; authored senses and lunge values come from the current catalog.
- Future milestones can add richer investigation, blind/deaf enemy variants, leash tuning, and authored attack suites once this V1 contract is stable.
