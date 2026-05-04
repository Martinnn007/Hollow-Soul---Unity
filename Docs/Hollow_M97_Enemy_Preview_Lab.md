# Hollow M97: Enemy Preview Lab

## Purpose

Enemy Preview Lab is a rendered Unity scene and editor window for testing one enemy at a time with real combat runtime systems, room blockers, pathing diagnostics, range overlays, and a simulated dummy player.

## Open It

1. Open `Hollow > Enemy Authoring > Enemy Preview Lab`.
2. Click `Create / Refresh Lab Scene` once if the scene has not been created yet.
3. Use `Open Scene` or `Open With Selected Enemy`.
4. Press Play to run the live preview.

Scene path:

`Assets/_Hollow/Scenes/EnemyPreviewLab/EnemyPreviewLab.unity`

## Pick An Enemy

Use the enemy dropdown in the `Enemy Target` panel. Search supports display name, spawn kind, behavior id, and disposition.

The same lab can also be opened from:

- `Hollow > Enemy Authoring > Enemy Studio` with the `Preview Lab` button.
- `Hollow > Enemy Authoring > Enemy AI Brain Studio` with the `Open Preview Lab` button.

## Player Patterns

The lab drives a dummy player so behavior can be observed without manual input.

- `HiddenNoStimulus`: hides the player far away, useful for idle, wander, territorial, and no-player checks.
- `Stationary`: keeps the player at the safe start.
- `Circle`: loops around the room.
- `FigureEight`: crosses the center repeatedly.
- `ApproachRetreat`: tests engagement and reset behavior.
- `SweepLane`: tests lane pressure and pathing around blockers.
- `DeterministicWander`: reproducible roaming target motion.

## Overlays

Enable or disable these in the active lab controls:

- `Show Range Overlays`: hearing, sight, preferred distance, attack range, and sight cone.
- `Show Grid Overlay`: 1m room authoring grid.
- `Show Path Tracing`: path goal and next waypoint line plus global path stats.
- `Show AI Blackboard`: runtime AI blackboard labels and summaries.
- `Show Runtime Stats`: live AI/path diagnostics in the window.

## What To Check

- Does the enemy wake up only when its senses or disposition say it should?
- Does it reach action range instead of hovering outside it?
- Does it path around rocks and holes rather than sliding into blockers?
- Does it recover punishably after attacks?
- Does it behave sensibly with no visible player?

## Notes

The scene uses runtime systems, not a canvas mockup. It is safe for preview and tuning, but balance changes should still go through Enemy Studio drafts and validation before applying to assets.
