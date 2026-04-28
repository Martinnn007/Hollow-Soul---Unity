# Milestone 33: Combat Feel, Physics, Collision, and Camera Polish

## Summary

M33 tightens the current prototype feel without changing branch generation, rewards, weapons, saves, room data, or platform presentation rules.

## Changes

- Player movement now resolves large frame steps through small room-local substeps so the player cannot tunnel through rocks or thin blocked tiles during spikes.
- Projectile movement now uses substeps for player and enemy projectiles, so fast shots hit obstacles and targets along the path instead of only checking the final position.
- Room-local collision now chooses the slide axis that makes the most progress toward the intended movement vector when both single-axis slides are valid.
- Melee hit height is centralized in `CombatFeelTuning`.
- Gameplay camera follow now keeps the current player-centered behavior, adds small look-ahead during normal motion, and snaps immediately after large traversal jumps so new rooms do not feel like the camera is catching up from the previous room.
- The QA gate remains Burst-safe for batchmode validation.

## Validation

Run:

```text
Hollow/Generation/Generate Milestone 33 Assets
Hollow/Validation/Run Milestone 33 Validation
Hollow/Platform QA/Run Full M24 QA Gate
```

Batchmode:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  --burst-disable-compilation \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -executeMethod Hollow.Editor.Generation.Milestone33AssetGenerator.Generate \
  -quit
```

Then:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  --burst-disable-compilation \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -executeMethod Hollow.Editor.Validation.Milestone33Validator.Validate \
  -quit
```

## Acceptance

- Large player movement steps stop at obstacles instead of passing through them.
- Fast player and enemy projectiles collide along their path.
- Camera remains centered on the player, preserves camera child offsets, and snaps after major room traversal jumps.
- M32 QA gate continues to pass with environment blocks only for missing local platform modules.
