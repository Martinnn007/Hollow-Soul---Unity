# Milestone 3 - HollowRuntime V2 Import

Milestone 3 replaces the M2 placeholder graybox room with a default imported `hollowRuntime.schemaVersion = 2` sample room.

## Runtime Behavior

- All platform game routes load `combat_single_sample.hollowruntime.json`.
- `HollowRuntimeV2Importer` decodes gameplay data from the `hollowRuntime` block only.
- `RoomRuntimeRoot.BuildFrom` creates static floor, door markers, obstacle blocks, and spawn markers.
- Floor top is authored at `y = 0`.
- Rock obstacles are `1m x 1m x 1m` blocking cubes snapped to meter-center positions.
- Door markers are decorative anchors only; no traversal or opening behavior exists in M3.
- Enemy markers are visual spawn anchors only; enemy behavior begins later.

## Sample Room

The default sample is a `13m x 7m` single combat room centered at origin:

- Full walkable floor, `91` tile centers.
- Four door ports: north, south, east, and west.
- Sixteen rock obstacles.
- Safe start at `(0, 0, 0)`.
- Four enemy spawn markers near the far corners.

## Platform Rule

M2 presentation behavior remains unchanged:

- Windows: world scale `1.0`.
- Vision Pro bounded tabletop: `WorldPresentationRoot` scale `0.5`.
- Vision Pro immersive: world scale `1.0`.
- HUD and shell UI stay outside `WorldPresentationRoot`.

## Commands

Generate M3 assets:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m3-generate.log \
  -executeMethod Hollow.Editor.Generation.Milestone3AssetGenerator.Generate
```

Validate M3:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m3-validate.log \
  -executeMethod Hollow.Editor.Validation.Milestone3Validator.Validate
```
