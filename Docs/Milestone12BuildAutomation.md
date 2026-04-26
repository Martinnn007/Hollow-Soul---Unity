# Milestone 12: Build Automation And Full Prototype Audit

Milestone 12 is the first post-lock milestone. It does not add gameplay. It makes the prototype repeatable for another engineer by adding a full audit runner, output conventions, build manifest generation, and a Windows development build entrypoint.

## Generated Assets

Run `Hollow/Generation/Generate Milestone 12 Assets`.

The generator calls M11 first, then creates:

- `Assets/_Hollow/Data/BuildAutomation/BuildAutomationProfile_Prototype.asset`
- `output/builds/`
- `output/reports/latest_prototype_audit.json`
- `output/reports/latest_prototype_audit.md`
- `output/reports/latest_build_manifest.json`

The profile is Addressable under `hollow.build-automation` and `hollow.data`.

## Full Prototype Audit

Run `Hollow/Validation/Run Full Prototype Audit`.

This invokes the M0-M11 validators through their no-exit validation path and writes JSON/Markdown reports under `output/reports`. It captures validator errors without calling `EditorApplication.Exit`, so it can be reused from menus, tests, and future command-line automation.

## Build Manifest

Run `Hollow/Build/Write Prototype Build Manifest`.

The manifest records:

- Prototype version.
- Unity version.
- Git branch and short commit hash.
- Build target.
- Planned build path.
- Audit result and audit report path.
- Addressables profile name.
- Required build scenes.

## Windows Development Build

Run `Hollow/Build/Build Windows Development Prototype`.

The build command runs the full prototype audit first. If the audit fails, the build is blocked and a manifest is still written with `BlockedByAudit`. If the audit passes, Unity builds a development Windows player to `output/builds/HollowSoul_Prototype_Windows/HollowSoul.exe`.

If the local Unity install does not have Windows build support installed, this command may fail at Unity's build step. That is acceptable for M12; the command path and manifest gate are now defined.

## Vision Pro Build Profile Placeholder

M12 does not package a final Vision Pro build. It validates that the Vision Pro bounded and immersive scenes stay in the build scene list and that both M10 platform polish profiles exist. Production PolySpatial packaging remains a later platform milestone.

## Validation

Run `Hollow/Validation/Run Milestone 12 Validation`.

The validator checks required files, the build automation profile, Addressables labels, output folders, generated reports, build scenes, Vision Pro placeholder profile coverage, and the full M0-M11 prototype audit.

## Tests

M12 adds EditMode coverage for the build automation profile, audit runner, build manifest writer, and Vision Pro placeholder checks. It also adds a PlayMode smoke test that loads `MainMenu` and `Game_Windows` and verifies the main menu/session/room roots appear.

## Command-Line Examples

Generate M12 assets:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/path/to/Hollow Soul - Unity" -quit -executeMethod Hollow.Editor.Generation.Milestone12AssetGenerator.Generate
```

Run M12 validation:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/path/to/Hollow Soul - Unity" -quit -executeMethod Hollow.Editor.Validation.Milestone12Validator.Validate
```

Run EditMode tests:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/path/to/Hollow Soul - Unity" -runTests -testPlatform EditMode -testResults output/reports/editmode-results.xml
```
