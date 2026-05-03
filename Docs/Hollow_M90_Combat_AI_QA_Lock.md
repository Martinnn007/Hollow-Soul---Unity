# M90: Combat AI QA Lock

M90 is a QA and feel lock over the modern enemy combat stack from M72 through M89. It does not add a new enemy family. It proves that the foundation is coherent: ordinary contact is harmless, attacks use active windows, senses and disturbance feed behavior trees, navigation remains adapter-bound, weapon users keep recovery windows, ranged and magic enemies stay budgeted, knockback data remains profile-driven, and bosses stay stable.

## Lock Criteria

- Unity compiles without Safe Mode errors.
- Focused EditMode regressions pass for contact, active windows, attack profiles, movement, senses, disturbance, behavior trees, weapon users, creature actions, ranged enemies, magic enemies, navigation, alert sharing, knockback, room clear, projectiles, split children, and bosses.
- M72 priority remains strict: only Tactical and Cunning intelligence receive attack-budget tie bonuses.
- M79 contact contract remains intact: normal body overlap disturbs or alerts, but does not damage.
- M80 active windows remain intact: windup and recovery are safe from damage, active frames apply damage once per activation unless authored otherwise.
- M83-M89 AI layers remain local and readable: no hidden pathfinding dependency, no room-wide alert chains, no boss runtime behavior rewrite.

## QA Surface

| Surface | Desired Result |
| --- | --- |
| contact | Ordinary enemy overlap separates and disturbs only; passive hazards are explicit opt-ins. |
| active windows | Melee, lunge, charge, ranged, area, weapon, creature, and magic actions commit through windup, active, recovery. |
| weapon users | Skeletons, knights, and giants use ranges, arcs, guard windows, combos, and recovery instead of contact damage. |
| senses | Sight, hearing, noise tiers, proximity, and bump stimuli produce disposition-specific responses. |
| movement | Enemies use preferred range, local steering, and the M88 navigation adapter without hard pathfinding assumptions. |
| knockback | M76 attack profiles carry damage classification, force class, knockback, and guard recoil. |
| bosses | Existing boss behavior and HUD remain unchanged except active contact bridges already added in M79. |

## Feel Notes

- Dark Souls-style combat should not mean slow combat; it means readable commitment, recovery, and counterplay.
- The old min/max range idea should become preferred-distance behavior. Enemies can prefer a band, but attacks, lunges, charges, guards, and retreats must intentionally break the band when their action calls for it.
- Ranged enemies should reposition for line pressure later, but M90 keeps the M88 adapter boundary and avoids committing to a pathfinding backend.
- Alert sharing should make rooms feel awake, not unfair. It is a local nudge into the same behavior rules, not squad command.

## Suggested Next Milestones

1. M91 Preferred Distance + Commitment Tuning V2: replace any remaining rigid min/max behavior with soft preferred-distance envelopes, action-specific range overrides, retreat caps, and punishable recovery spacing.
2. M92 Pathfinding Backend Adapter V1: add a real optional backend behind M88 for selected grounded enemies, with local steering fallback and no behavior-tree rewrite.
3. M93 Boss Behavior Trees + Active Windows V1: move boss decision metadata into controlled runtime trees while preserving boss identities and explicit attack windows.
4. M94 Combat Feedback + Feel Integration V1: improve telegraphs, hit sparks, shield reactions, poise break feedback, damage health bars, and audio cue hooks.
5. M95 Advanced Attack Families + Status V1: add poison, bleed, curse, fire, frost, soul, grab, and hazard-zone actions with explicit counters.
6. M96 Encounter Director Pressure Budgets V2: tune melee, ranged, magic, alert, and boss pressure budgets together for mixed rooms.
7. M97 Enemy AI Personality Pass V1: author per-enemy aggression, courage, patience, retreat bias, combo bias, and disturbance tolerance over the behavior tree layer.
8. M98 New Boss Integration V1: add a new boss built on active windows, action profiles, behavior trees, and feedback from the start.
9. M99 Combat AI Metrics Rooms V1: add designer test rooms for distance, alert, projectile, weapon, creature, magic, pathing, and boss regressions.
10. M100 Combat AI QA Lock 2: full-suite and manual feel lock after pathfinding, boss trees, statuses, and feedback are online.

## M90 Output

M90 is complete only when focused regressions are green. If Unity licensing, Safe Mode, or broad legacy tests block the full suite, record the blocker and leave the milestone unlocked.
