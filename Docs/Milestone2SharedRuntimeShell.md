# Milestone 2 - Shared Runtime Shell

Milestone 2 proves that Hollow can enter a shared game-world shell from the menu routes without forking gameplay logic by platform.

`MainMenu -> Profile Slot -> Windows / VisionOS Bounded / VisionOS Immersive -> GameSessionRoot`

## Scope

- `Hollow.World` owns the basic `GameSessionController` and immutable `GameSessionState`.
- `Hollow.Presentation` owns platform presentation scale and camera metadata.
- `Hollow.Rooms` owns the placeholder `RoomRuntimeRoot`.
- `Hollow.Entities` owns the placeholder player and player spawn marker.
- Windows, Vision Pro bounded tabletop, and Vision Pro immersive scenes all load the same logical runtime shell.

Combat, imported room layouts, real traversal, and branch generation stay out of M2. Those begin in later milestones.

## Platform Rule

Gameplay coordinates remain authored in real meters. Presentation can scale the visible world for the target platform.

- Windows standard 3D: world scale `1.0`.
- Vision Pro bounded tabletop: world scale `0.1`.
- Vision Pro immersive: world scale `1.0`.

HUD and shell UI must stay outside `WorldPresentationRoot`, so tabletop scaling never shrinks menus or overlays.

## Generated Runtime Scene Shape

Each game scene contains:

- `AppRoot`
- platform camera rig
- `GameSessionRoot`
- `WorldPresentationRoot`
- `RoomRuntimeRoot`
- `PlayerSpawn_Center`
- `PlayerCharacter`
- unscaled `PlatformShellCanvas`

The current room is a graybox `13m x 7m` Isaac-style single room centered at origin. The player spawn point is exactly at room-local `(0, 0, 0)`.

## Commands

Generate M2 assets:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m2-generate.log \
  -executeMethod Hollow.Editor.Generation.Milestone2AssetGenerator.Generate
```

Validate M2:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m2-validate.log \
  -executeMethod Hollow.Editor.Validation.Milestone2Validator.Validate
```
