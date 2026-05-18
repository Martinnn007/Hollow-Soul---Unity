# Milestone 11: Prototype Lock

Milestone 11 turns the current prototype into a handoff-ready build target. It does not add new gameplay systems; it creates the release gate that proves M1-M10 are wired, validated, documented, and safe to continue from.

## Generated Assets

Run `Hollow/Generation/Generate Milestone 11 Assets`.

The generator calls M10 first, then creates:

- `Assets/_Hollow/Data/PrototypeLock/PrototypeLockChecklist.asset`
- `Assets/_Hollow/Data/PrototypeLock/PerformanceBudget_Prototype.asset`
- `Assets/_Hollow/Data/PrototypeLock/BuildHandoff_Prototype.asset`

These assets are marked Addressable under `hollow.prototype-lock` and `hollow.data`.

## Prototype Lock Checklist

The checklist records the current lock criteria across:

- QA coverage for menu/profile flow, branch combat, Room Designer, and transient-session safety.
- Save/load coverage for New Run, checkpoints, Continue, completion banking, and death clear.
- Content validation for materials, cue definitions, Addressables labels, prefab references, and naming.
- Platform budgets for Windows, Vision Pro bounded tabletop, and Vision Pro immersive.
- Build handoff requirements, validation commands, and deferred post-prototype scope.

Required checklist items must be `Passed`. Deferred items are allowed only for clearly documented post-prototype scope.

## Performance Budgets

The prototype budget is intentionally simple and practical:

- Windows standard 3D: 120 FPS target, render scale up to `1.0`.
- Vision Pro bounded tabletop: 90 FPS target, render scale up to `0.9`, world root scale remains `0.5`.
- Vision Pro immersive: 90 FPS target, render scale up to `0.85`, comfort vignette metadata enabled.

The validator compares these budgets against the M10 platform polish profiles.

## Save/Load Coverage

`PrototypeLockValidationHarness` performs a real temp-store save/load pass using `JsonProfileStore`:

- Creates a profile.
- Starts a New Run and verifies `totalRuns`.
- Saves and reloads an active run snapshot.
- Completes the run and verifies banked souls, completed runs, and active-run clearing.
- Starts another run, clears it like a death path, and verifies meta progression is not banked.

This is deliberately separate from live player data and uses a temporary validation directory.

## Content And Build Handoff

M11 composes the M9 content validator, M10 profile checks, and build scene checks into one gate. Required build scenes are:

- `Boot.unity`
- `MainMenu.unity`
- `Game_Windows.unity`
- `Game_VisionOS_Bounded.unity`
- `Game_VisionOS_Immersive.unity`
- `RoomDesigner.unity`

The build handoff asset also stores validation commands and project assumptions so the prototype can be opened by another engineer without relying on hidden context.

## Validation

Run `Hollow/Validation/Run Milestone 11 Validation`.

The validator checks required files, generated assets, checklist status, performance budgets, Addressables labels, save/load coverage, M9 content validation, enabled build scenes, and handoff notes.

For command-line verification, run M11 validation and the EditMode test suite before handoff.

## Scope

M11 is a lock and handoff milestone. It intentionally does not add procedural branches, production art/audio, remote Addressables, certification work, cloud saves, or final platform packaging.
