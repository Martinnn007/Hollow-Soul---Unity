# M85: Creature Action Expansion V1

M85 expands body-only creature combat toward Souls-lite readable commitment. Damage remains physical and active-window-only, and every damaging creature move still lands through an explicit active window. Ordinary body overlap stays harmless from M79. Movement actions are local bursts only and same-family signals affect only matching living non-boss enemies.

## New Creature Roster

| Enemy | Spawn | HP | Speed | Body | Intelligence | Disposition | Preferred Range | Senses | Core Actions |
| --- | --- | ---: | ---: | --- | --- | --- | --- | --- | --- |
| Hollow Bird | `spawnEnemyHollowBird` | 3 | 2.25m/s | Light | 1 Simple | predator | 1.80-3.60m | 8.2m/235deg, hearing 6.4m | `swoop_peck`, `claw_dive`, `wing_retreat`, `caw_signal` |
| Hollow Beast | `spawnEnemyHollowBeast` | 5 | 1.90m/s | Medium | 2 Basic | predator | 1.15-2.10m | 7.2m/165deg, hearing 6.2m | `leap_bite`, `body_check`, `leap_back`, `howl_signal` |

## Creature Action Cards

| Owner | Action | Runtime | Damage | Force | Range | Timing | Move | Signal/Notes |
| --- | --- | --- | ---: | --- | ---: | --- | ---: | --- |
| Normal Chaser | `short_backstep` Short Backstep | CreatureMove | 0 | Light | 2.00m | 0.08/0.22/0.16s | 0.72m | M85 non-damaging reset hop; ordinary overlap remains harmless. |
| Normal Chaser | `warning_feint` Warning Feint | CreatureSignal | 0 | Light | 2.40m | 0.16/0.12/0.20s | - | M85 non-damaging shoulder feint that telegraphs pressure without passive contact damage. |
| Flying Chaser | `fly_strafe` Fly Strafe | CreatureMove | 0 | Light | 4.50m | 0.06/0.28/0.12s | 0.95m | M85 lateral flying reposition with no hit window. |
| Flying Chaser | `dive_feint` Dive Feint | CreatureSignal | 0 | Light | 3.00m | 0.14/0.12/0.18s | - | M85 false dive tell; prey identity stays readable. |
| Fast Chaser | `evasive_skitter` Evasive Skitter | CreatureMove | 0 | Light | 2.20m | 0.06/0.22/0.12s | 0.85m | M85 diagonal skitter that creates a whiff/punish rhythm. |
| Fast Chaser | `snap_combo` Snap Combo | MeleeLunge | 1 | Light | 1.15m | 0.14/0.12/0.20s | 0.45m | M85 body-only snap option; still one active hit window. |
| Heavy Chaser | `guarded_shove` Guarded Shove | MeleeLunge | 1 | Medium | 1.55m | 0.30/0.16/0.34s | 0.28m | M85 braced shove; a readable check rather than passive body contact. |
| Heavy Chaser | `slow_overhead_slam` Slow Overhead Slam | Area | 2 | Heavy | 1.45m | 0.52/0.20/0.55s | - | M85 slow body-only slam with long punishable recovery. |
| Ash Charger | `short_recover_hop` Short Recover Hop | CreatureMove | 0 | Light | 1.80m | 0.08/0.22/0.18s | 0.78m | M85 short reset after close pressure or a whiff. |
| Ash Charger | `shoulder_check` Shoulder Check | MeleeLunge | 1 | Medium | 1.25m | 0.24/0.15/0.32s | 0.38m | M85 close physical check when the charge line is too tight. |
| Husk Splitter | `splitter_backstep` Splitter Backstep | CreatureMove | 0 | Light | 2.00m | 0.08/0.22/0.16s | 0.80m | M85 short backstep that frames the next cleave. |
| Husk Splitter | `cleave_feint` Cleave Feint | CreatureSignal | 0 | Light | 2.20m | 0.18/0.12/0.22s | - | M85 non-damaging cleave tell used to bait a dodge. |
| Rat | `skitter_retreat` Skitter Retreat | CreatureMove | 0 | Light | 2.20m | 0.06/0.22/0.12s | 0.90m | M85 fast retreat burst after damage or threat. |
| Rat | `panic_pounce` Panic Pounce | MeleeLunge | 1 | Light | 1.20m | 0.16/0.14/0.18s | 0.70m | M85 territorial panic leap after warning fails. |
| Rat | `alarm_squeal` Alarm Squeal | CreatureSignal | 0 | Light | 5.00m | 0.18/0.12/0.28s | - | M85 same-family signal affecting nearby living rats only. |
| Spider | `panic_flee` Panic Flee | CreatureMove | 0 | Light | 2.00m | 0.05/0.22/0.10s | 1.00m | M85 erratic non-damaging flee burst. |
| Spider | `web_feint` Web Feint | CreatureSignal | 0 | Light | 2.00m | 0.12/0.12/0.16s | - | M85 non-damaging rear-up feint; no web slow or status effect. |
| Hollow Bird | `swoop_peck` Swoop Peck | MeleeLunge | 1 | Light | 1.35m | 0.18/0.15/0.22s | 1.00m | M85 Hollow Bird committed swoop-peck. |
| Hollow Bird | `claw_dive` Claw Dive | MeleeLunge | 1 | Medium | 1.55m | 0.24/0.17/0.28s | 1.15m | M85 Hollow Bird heavier dive with clear recovery. |
| Hollow Bird | `wing_retreat` Wing Retreat | CreatureMove | 0 | Light | 3.80m | 0.08/0.25/0.15s | 1.25m | M85 flying local burst away from pressure. |
| Hollow Bird | `caw_signal` Caw Signal | CreatureSignal | 0 | Light | 5.20m | 0.22/0.12/0.25s | - | M85 same-family signal affecting nearby living Hollow Birds only. |
| Hollow Beast | `leap_bite` Leap Bite | MeleeLunge | 1 | Light | 1.45m | 0.22/0.16/0.28s | 0.90m | M85 Hollow Beast punish leap bite. |
| Hollow Beast | `body_check` Body Check | MeleeLunge | 1 | Medium | 1.65m | 0.30/0.18/0.38s | 0.75m | M85 grounded beast shoulder/body check with punishable recovery. |
| Hollow Beast | `leap_back` Leap Back | CreatureMove | 0 | Light | 2.20m | 0.10/0.24/0.20s | 1.00m | M85 grounded local retreat burst. |
| Hollow Beast | `howl_signal` Howl Signal | CreatureSignal | 0 | Light | 5.50m | 0.28/0.14/0.35s | - | M85 same-family signal affecting nearby living Hollow Beasts only. |

## Signal Rules

- `alarm_squeal`, `caw_signal`, and `howl_signal` emit `EnemyStimulusKind.CreatureSignal` only to nearby living non-boss enemies with the same creature family.
- Signals do not wake bosses, unrelated enemies, or the entire room. They bias awareness/action choice but do not increase melee or ranged attack budget pressure.
- Web feint and warning feint are non-damaging readable tells. They add no poison, bleed, web slow, or status effects.

## Local Burst Rules

- Swoop, strafe, skitter, leap-back, circle, and hop-back actions run through windup, active, and recovery states.
- Movement bursts never deal damage by themselves. Only linked melee/area active windows can damage.
- No pathfinding, obstacle LOS, squad navigation, passive creature body damage, boss runtime changes, or save schema changes are added.

## Existing Body-Only Upgrades

| Enemy | Upgrade Actions |
| --- | --- |
| Normal Chaser | `short_backstep`, `warning_feint` |
| Flying Chaser | `fly_strafe`, `dive_feint` |
| Fast Chaser | `evasive_skitter`, `snap_combo` |
| Heavy Chaser | `guarded_shove`, `slow_overhead_slam` |
| Ash Charger | `short_recover_hop`, `shoulder_check` |
| Husk Splitter | `splitter_backstep`, `cleave_feint` |
| Rat | `skitter_retreat`, `panic_pounce`, `alarm_squeal` |
| Spider | `panic_flee`, `web_feint` |

## Curated Rooms

- `m85_hollow_bird_perch_room`
- `m85_hollow_beast_den`
- `m85_rat_spider_signal_room`
- `m85_mixed_body_creature_scramble`
