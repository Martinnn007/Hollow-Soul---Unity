# M96: Enemy AI Brain Studio V1

## Summary

M96 adds a dedicated Unity Editor studio for authoring and auditing enemy AI brain contracts. It is a design/authoring layer above the current runtime systems: M93 action scoring, M82 behavior trees, M91 spacing, M83 disturbance, M89 alert sharing, and M80 active hit windows remain the runtime source of behavior.

Open it from `Hollow > Enemy Authoring > Enemy AI Brain Studio`.

## What The Tool Does

- Shows a global roster matrix with suggested brain role, intelligence, disposition, senses, action counts, and validation warnings.
- Supports individual draft editing for enemy identity, senses, disturbance, alert sharing, attack execution modifiers, behavior tree links, and spacing links.
- Provides global brain templates for body-pressure, prey skirmisher, territorial critter, stationary sentinel, weapon user, ranged kiter, magic caster, heavy bruiser, swarm background, and boss metadata planning.
- Includes an action score lab to preview how distance, awareness, intelligence, disposition, and room pressure affect action ranking.
- Shows threat director and AI LOD guidance so swarms stay readable instead of dogpiling.
- Streams Play Mode blackboard diagnostics for selected runtime enemies: LOD, tree command, chosen action, pressure penalty, top scores, and path status.
- Uses the existing M94 draft/apply protection workflow so source assets are changed only after explicit apply.

## AAA Direction

The studio treats enemy AI as a role contract rather than a pile of chase rules:

- Role first: every enemy should have a clear combat job.
- Commitment first: attacks are selected while idle, then windup/active/recovery stays locked.
- Pressure first: large groups lower action scores through soft caps instead of disabling enemies abruptly.
- Debug evidence first: designers should see why an enemy chose an action before changing balance.
- Template first, individual last: global templates establish family behavior, then individual enemies get bespoke overrides.

## Current Scope

M96 is editor tooling only. It does not change runtime combat behavior by itself.

The tool can edit non-boss enemy assets. Bosses remain metadata/planning-only until a later boss AI milestone enables boss behavior trees.

## Suggested Workflow

1. Open `Hollow > Enemy Authoring > Enemy AI Brain Studio`.
2. Select an enemy from the roster.
3. Review the suggested role and warnings in `Overview`.
4. Use `Templates` to apply a role template to the draft.
5. Use `Score Lab` to verify action ranking at likely combat distances.
6. Use `Individual` for precise senses, disturbance, commitment, and spacing edits.
7. Enter Play Mode, select a runtime enemy, and inspect `Live Trace`.
8. Use `Validation` and apply only when the draft is clean.

## Future Extensions

- Batch apply templates to families with per-field conflict review.
- Enemy subtype profiles for biome variants.
- Visual heatmaps for action score envelopes in designer rooms.
- Boss AI Brain Studio once boss behavior trees become runtime-enabled.
- Automated capture of manual playtest traces into tuning suggestions.
