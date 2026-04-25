# Milestone 0 - Unity Foundation

Milestone 0 establishes the clean Unity project spine for Hollow Soul.

## Baseline

- Unity Editor: `6000.4.1f1`
- Render pipeline: URP `17.4.0`
- Input package: Input System `1.19.0`
- Test package: Unity Test Framework `1.6.0`
- Addressables package: `2.7.6`

## What Exists Now

- `Assets/_Hollow` is the canonical project-owned content root.
- Runtime code is split into small assembly-definition folders.
- `Hollow.Core` owns boot, shell route, clock, event bus, stable IDs, and runtime session mode.
- `Hollow.Data` owns the first ScriptableObject definition/catalog shells.
- `Hollow.Platform` owns Windows, Vision Pro bounded, and Vision Pro immersive platform service shells.
- `Hollow.Input` owns the first input routing shell.
- `Hollow.Diagnostics` owns validation report/harness scaffolding.
- `Hollow.Editor` owns editor-only validation.
- `Assets/_Hollow/Tests/EditMode` contains a small foundation test suite.

## Validation

Run this from the project root:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m0.log \
  -executeMethod Hollow.Editor.Validation.Milestone0Validator.Validate
```

