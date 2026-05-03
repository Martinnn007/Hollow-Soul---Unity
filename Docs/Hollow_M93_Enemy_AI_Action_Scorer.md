# M93: Enemy AI Action Scorer + Threat Director V1

M93 moves normal enemy decision-making toward a Souls-leaning action model while preserving Hollow's room-pressure roots. Behavior trees still provide high-level intent, but concrete committed actions are now selected through a deterministic action scorer.

## Contract

- Scope is non-boss runtime AI. Boss behavior remains unchanged.
- `EnemyAiBrain` owns adaptive AI LOD, cached commands, chosen action state, and blackboard diagnostics.
- `EnemyActionScorer` scores current runtime action profiles by distance, facing, awareness, intelligence, disposition, force, cooldown eligibility, deterministic variation, and room pressure.
- `RoomThreatDirector` applies soft pressure caps for melee, ranged, area, and charge lanes. It lowers scores when the room is already saturated instead of hard-disabling enemy actions.
- Existing M80 windup, active, and recovery commitment remains locked. The scorer chooses before commitment only.
- Existing M91 spacing and M92 pathfinding continue to move enemies toward action envelopes.
- Damage remains active-window-only. Ordinary contact remains harmless unless a separate hazardous body policy opts in.

## Runtime Shape

| Layer | Responsibility |
| --- | --- |
| Behavior tree | Chooses broad intent: pressure, flee, hold, guard, ranged, melee, area, signal. |
| AI brain | Decides think cadence, LOD tier, cached command reuse, and blackboard state. |
| Action scorer | Converts committed tree commands into the best current action profile. |
| Threat director | Tracks soft room pressure and applies score penalties under swarm load. |
| Enemy runtime | Executes the chosen command through existing active-window states. |

## Adaptive AI LOD

- `Full`: close, endangered, or currently committed enemies think frequently and can score actions.
- `Reduced`: far engaged, alerted, or mid-distance enemies reuse plans longer and score less often.
- `Background`: distant low-threat enemies avoid new commitments and use simple facing, hold, or idle movement until disturbed.

## Debugging

The Developer Spawn Menu now exposes an `Enemy AI Blackboard` toggle. The blackboard displays each enemy's LOD tier, tree command, chosen action, top action scores, pressure penalty, target distance, path state, and fallback/cooldown reason. Existing path tracing and path stats remain separate.

## Design Notes

- M93 favors Dark Souls-style commitment: fewer surprise hits, clearer action starts, and punishable recoveries.
- Large swarms remain supported by soft pressure scoring, not strict threat slots.
- The scorer is deterministic-weighted so repeated test setups are reproducible while still allowing variation.
