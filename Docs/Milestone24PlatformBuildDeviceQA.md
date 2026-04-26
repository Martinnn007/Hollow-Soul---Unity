# Milestone 24: Platform Build And Device QA

M24 is the platform handoff gate for the current prototype. It does not change gameplay, saves, content semantics, or branch generation. It proves that the project can be validated, packaged for a Windows development build, checked for Vision Pro bounded/immersive readiness, and handed to another tester with clear reports.

## Generated Assets

Run `Hollow/Generation/Generate Milestone 24 Assets`.

The generator calls M23 first, then creates:

- `Assets/_Hollow/Data/PlatformQA/PlatformBuildQaProfile_M24.asset`
- `output/reports/latest_platform_build_qa.json`
- `output/reports/latest_platform_build_qa.md`
- `output/builds/HollowSoul_M24_Windows/`

The profile is Addressable with `hollow.platform-qa` and `hollow.data`.

## QA Gate

Run `Hollow/Platform QA/Run Full M24 QA Gate`.

The runner:

- Runs the M0-M23 validation chain through the existing prototype audit system.
- Builds local Addressables content.
- Records the EditMode and PlayMode smoke commands expected for handoff verification.
- Builds a Windows development player when Windows build support is installed.
- Validates Vision Pro bounded/immersive scene readiness and simulator tooling without requiring signing or physical-device deploy.
- Writes JSON and Markdown reports under `output/reports`.

Missing platform modules, simulator tooling, or signing context produce `BlockedByEnvironment` results with remediation steps. Project/content/test failures remain real failures.

## Runtime QA Probe

`PlatformRuntimeQaProbe` captures lightweight scene state for smoke tests:

- Active scene and platform kind.
- World scale.
- HUD/shell separation from `WorldPresentationRoot`.
- Game/session, room runtime, main menu, Room Designer, and presentation catalog presence.
- A simple frame-time sample.

The probe is diagnostic only. Gameplay remains owned by the existing runtime controllers.

## Manual Device Checklist

Windows:

- Launch `HollowSoul.exe`.
- Create/select profile, start New Run, move/shoot/clear a room, traverse a door, buy a shop card, quit and Continue.
- Open Room Designer, create a draft, move cursor, place/erase content, export JSON/USDA.

Vision Pro bounded:

- Confirm tabletop world scale is `0.1`.
- Confirm HUD/minimap are readable and unscaled.
- Confirm ArtPass visuals do not add gameplay colliders.
- Confirm comfort/readability in a bounded volume.

Vision Pro immersive:

- Confirm full-scale world.
- Confirm comfort vignette metadata/profile is present.
- Confirm camera posture and combat spacing feel readable.
- Confirm frame-budget readiness before physical-device deployment.

## Validation

Run `Hollow/Validation/Run Milestone 24 Validation`.

The validator checks required source/docs/tests, the M24 profile, Addressables label wiring, report output conventions, Vision Pro scene/profile readiness, and generated QA bootstrap reports.

Command-line examples:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/path/to/Hollow Soul - Unity" -quit -executeMethod Hollow.Editor.Generation.Milestone24AssetGenerator.Generate
```

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/path/to/Hollow Soul - Unity" -quit -executeMethod Hollow.Editor.Validation.Milestone24Validator.Validate
```

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/path/to/Hollow Soul - Unity" -quit -executeMethod Hollow.Editor.Build.PlatformBuildQaRunner.RunFullM24Qa
```
