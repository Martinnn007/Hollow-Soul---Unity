# Milestone 32: Full QA Gate Rebaseline + Test Runner Reliability

## Summary

M32 turns the M24 platform QA gate from a mostly report-and-command checklist into a stronger automated handoff gate. The gate now produces real EditMode test evidence and a deterministic platform scene smoke report instead of leaving test rows as `NotRun`.

## Changes

- The M24 QA runner now executes `Hollow.Tests.EditMode` through Unity's `TestRunnerApi` and writes `output/reports/m24-editmode-results.xml`.
- The platform smoke row now opens the required scenes in the editor and captures the same `PlatformRuntimeQaProbe` data used by PlayMode smoke tests.
- The smoke report is written to `output/reports/m24-playmode-smoke-editor-probe.md`.
- `Hollow.Editor.asmdef` now explicitly references `UnityEditor.TestRunner` and `UnityEngine.TestRunner`.
- Validators now share `MilestoneValidationExitPolicy`, so direct batch validator commands can still exit with a status code, while validators invoked inside Test Runner do not terminate Unity mid-suite.
- The QA runner disables Burst compilation at gate startup for batch stability. If the local editor still crashes in the Burst worker before the gate starts, add `--burst-disable-compilation` to the Unity command.
- M32 adds a validator that checks the Test Framework package, test asmdefs, latest QA report, EditMode XML output, and scene-smoke report.

## Why This Exists

The shell-level Unity command:

```bash
Unity -batchmode -projectPath <repo> -runTests -testPlatform EditMode -testResults output/reports/m31-editmode-results.xml
```

was accepted by the local Unity editor but exited without writing XML. M32 avoids making the gate depend on that flaky bridge by running EditMode tests in-process through the supported editor API.

PlayMode tests are still valuable for manual/scheduled QA, but the gate now has a stable editor-side smoke check that catches the same major scene-root regressions: missing controllers, missing presentation roots, scaled HUD, wrong Vision Pro bounded scale, and missing presentation catalog.

## Validation

Run:

```text
Hollow/Platform QA/Run Full M24 QA Gate
Hollow/Validation/Run Milestone 32 Validation
```

Batchmode:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  --burst-disable-compilation \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -executeMethod Hollow.Editor.Build.PlatformBuildQaRunner.RunFullM24Qa \
  -quit
```

Then:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  --burst-disable-compilation \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -executeMethod Hollow.Editor.Validation.Milestone32Validator.Validate \
  -quit
```

## Acceptance

- Latest platform QA report has no `NotRun` targets.
- `editmode-tests` passes and writes XML.
- `playmode-smoke-tests` passes via the editor-side scene smoke probe.
- M24 dependency audit still passes.
- Windows build support may still be `BlockedByEnvironment` on non-Windows-module installs.
