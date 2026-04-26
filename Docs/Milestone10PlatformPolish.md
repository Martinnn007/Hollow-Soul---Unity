# Milestone 10: Platform Polish

Milestone 10 adds the first platform-specific presentation polish pass while keeping gameplay logic shared across Windows, Vision Pro bounded tabletop, and Vision Pro immersive routes.

## Platform Profiles

Generated platform polish profiles live in `Assets/_Hollow/Data/Platform/Polish`.

- `PlatformPolish_WindowsStandard3D.asset`: full-scale Windows camera framing, 120 FPS target, no comfort vignette.
- `PlatformPolish_VisionOSBoundedTabletop.asset`: `0.1` tabletop world scale, closer camera framing, 90 FPS target, no immersive vignette.
- `PlatformPolish_VisionOSImmersive.asset`: full-scale immersive framing, reduced FOV/tilt, 90 FPS target, comfort vignette metadata enabled.

Each profile owns world scale, camera position/rotation/FOV, clip planes, background/ambient color, frame pacing, render-scale budget, and comfort-vignette settings.

## Runtime Presentation

- `PlatformPolishApplier` applies a profile to the active camera, `PlatformPresentationRoot`, `Application.targetFrameRate`, and `QualitySettings.vSyncCount`.
- `ComfortVignettePresenter` stores immersive comfort metadata on the camera. It is intentionally lightweight for now; final rendered vignette art/shader can replace it later without changing profile data.
- `PlatformPresentationRoot` now supports explicit profile-driven world scale while preserving the M2 default scale policy.

## Generated Scene Wiring

Run `Hollow/Generation/Generate Milestone 10 Assets` to regenerate M1-M9 content, create M10 profiles, update camera rig prefabs, stamp game scenes with profile appliers, and mark platform profiles addressable under `hollow.platform`.

## Validation

Run `Hollow/Validation/Run Milestone 10 Validation`.

The validator checks required files, profile values, Addressables labels, scene appliers, camera comfort metadata, world scale, and that HUD/shell UI remains outside `WorldPresentationRoot`.

## Scope

M10 is presentation polish only. It does not fork gameplay logic, alter save/progression, add final Vision Pro compositor features, or implement production vignette shaders. M11 should handle prototype lock, QA, performance budgets, and build handoff.
