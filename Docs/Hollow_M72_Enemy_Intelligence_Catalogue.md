# M72: Enemy Intelligence + Instinct Disposition V1

M72 adds a 0-5 intelligence scale and a companion instinct disposition to Hollow's current enemy roster. The feature is intentionally conservative: no pathfinding, no line of sight, no squad tactics, and no boss behavior changes.

## Intelligence Scale

| Value | Label | Runtime Intent |
|---:|---|---|
| 0 | Instinctive | Disposition-driven creature behavior. |
| 1 | Simple | Direct pressure with little adjustment. |
| 2 | Basic | Keeps current direct combat with modest intent support. |
| 3 | Trained | Uses authored role cleanly, especially sentinel timing. |
| 4 | Tactical | Gains a small attack-priority bonus and cleaner spacing. |
| 5 | Cunning | Highest V1 priority bonus without bypassing pressure caps. |

## Dispositions

| Disposition | Definition |
|---|---|
| prey | Wanders or backs away until endangered. |
| predator | Attacks directly without clever spacing at low intelligence. |
| sentinel | Holds territory and attacks when approached. |
| mindless | Uses simple direct or wandering pressure. |

## Current Base Enemy Table

| Enemy | Intelligence | Disposition |
|---|---:|---|
| Normal Chaser | 1 Simple | predator |
| Flying Chaser | 0 Instinctive | prey |
| Fast Chaser | 1 Simple | predator |
| Heavy Chaser | 1 Simple | mindless |
| Ash Charger | 0 Instinctive | predator |
| Bone Turret | 3 Trained | sentinel |
| Husk Splitter | 2 Basic | predator |
| Stone Warden Spawn | 2 Basic | sentinel |

## Current Boss Metadata Table

| Boss | Intelligence |
|---|---:|
| Stone Warden | 2 Basic |
| Splinter Saint | 3 Trained |
| Gravel Maw | 2 Basic |
| Cartouche Widow | 5 Cunning |
| Iron Reliquary | 4 Tactical |
| Mirror Husk | 5 Cunning |
| Ash Comet | 3 Trained |
| Choir of Teeth | 4 Tactical |
| Rust Bishop | 5 Cunning |
| Hollow Star Larva | 5 Cunning |

## Runtime Behavior Effects

- Instinctive prey backs away or wanders until endangered.
- Endangered means damaged in the last 3 seconds or kept very close briefly.
- Instinctive predators pressure directly.
- Sentinels hold territory until approached.
- Mindless enemies use direct or short wandering pressure.
- Tactical and Cunning enemies receive a small attack-priority bonus while the room budget still limits standard enemy attack starts.

## Save/Continue Compatibility

Encounter saves now snapshot resolved intelligence and disposition values beside spawn kinds. Continue restores those exact values by spawn index. Legacy saves without M72 fields fall back to current enemy catalog defaults. Runtime instinct timers reset on Continue.

## Remaining Limitations And V3 Recommendations

- No pathfinding, line of sight, squad coordination, or boss behavior changes are included in M72.
- V3 should revisit richer movement intents only after base combat readability and room-clear pacing are stable.
- Low-intelligence fleeing remains short and readable so room clears do not become hide-and-seek.
