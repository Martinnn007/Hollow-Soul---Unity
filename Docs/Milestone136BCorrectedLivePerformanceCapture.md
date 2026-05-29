# M136B: Corrected Live Performance Capture + Measurement-Only Readiness

## Summary
M136B repairs the live capture harness before optimization work. It does not change URP defaults, gameplay systems, HUD layout, branch generation, or runtime economy. The goal is trustworthy evidence for the next performance pass.

## Runtime Sampling
- Live captures are driven by `M136RuntimeLiveCaptureDriver`, a hidden Play Mode `MonoBehaviour` that ticks once per gameplay frame through `Update`.
- The Editor window only starts/stops captures, repaints status, and exports artifacts.
- Each manifest records sampling source, sample rate, expected sample-count range, frame-cadence confidence, validity grade, profiler trace toggle state, and capture-scoped FPS override state.
- Object counts are collected on a throttled runtime cadence so the tool does not become the main bottleneck.

## Validity Gates
- Ship hub idle must observe ship hub context.
- Normal branch idle must observe branch context.
- Active combat must observe active combat, enemies, projectiles, or VFX.
- Wave captures must observe wave state and enemies.
- Boss captures must observe boss runtime or active boss state.
- Transition captures must observe a room transition event.
- Captures are `Valid`, `Directional`, or `Invalid`; invalid captures remain archived but cannot drive optimization conclusions.

## Capture Window
- `Use 60 FPS capture cap` is off by default and applies only while a capture is running.
- `Capture profiler trace` is off by default because previous traces were hundreds of MB.
- Automated smoke capture remains label-only/non-authoritative until a later scenario routing pass.

## Deferrals
- No cooler URP profile is applied in M136B.
- No default desktop FPS cap is applied in M136B.
- No HUD, wall, minimap, combat, save, room-generation, or economy behavior is optimized in M136B.
