# Milestone 6 - Branch Traversal, Rewards, and Hub Return

Milestone 6 expands the M5 combat slice into the first multi-room branch. The branch is deterministic for now: one origin room connected to north, south, east, and west rooms.

## Runtime Behavior

- The branch graph is a five-room cross with independent visited, cleared, and reward state per room.
- All rooms reuse `combat_single_sample.hollowruntime.json` in M6.
- Doors stay locked while combat is active.
- After a room clears, connected doors turn green and can be used with `E` / gamepad south button when the player is near the door.
- The destination room rebuilds from the imported room asset and places the player just inside the opposite door.
- Cleared rooms do not respawn enemies when revisited.
- Non-origin rooms spawn one runtime-only reward after clear.
- Rewards can be claimed once and only increment the in-memory `RuntimeRewardCounter`.
- After all five rooms are cleared and the four non-origin rewards are claimed, a hub return portal appears.
- The portal safely returns to `MainMenu`; it does not write profile, run, slot summary, or meta-progression data.

## Presentation

- `BranchMiniMapController` renders on `PlatformShellCanvas`, outside `WorldPresentationRoot`.
- The minimap shows current, visited, cleared, and reward-pending rooms.
- Vision Pro bounded still scales only `WorldPresentationRoot` to `0.5`; branch logic, combat, rewards, and UI stay in normal runtime units.

## Controls

- `WASD` / left stick: move.
- Arrow keys / right stick: shoot.
- `E` / gamepad south button: interact with doors, rewards, and the hub return portal.

## Commands

Generate M6 assets:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m6-generate.log \
  -executeMethod Hollow.Editor.Generation.Milestone6AssetGenerator.Generate
```

Validate M6:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m6-validate.log \
  -executeMethod Hollow.Editor.Validation.Milestone6Validator.Validate
```
