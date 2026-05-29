# M136: Editor Laptop Performance + Power Investigation

## Summary
M136 adds an editor/development-only performance investigation harness for the laptop heat and power problem. The target is a cool 60 FPS editor workflow, with evidence gathered before broad visual or gameplay optimizations are applied.

## Investigation Runner
- Main menu entry: `Hollow/Performance/Run Editor Laptop Power Investigation`.
- Live capture window: `Hollow/Performance/Live Gameplay Capture`.
- Manual capture records the current Play Mode session under the selected scenario label.
- Automated smoke capture cycles through the six scenario labels against the active Play Mode scene for comparable before/after data.
- Live captures are written under `output/reports/performance/live_captures/<timestamp>/` with manifest JSON, scenario JSON, CSV samples, and profiler trace status.
- The report generator prefers latest live captures and falls back to a deterministic baseline when live samples are missing.
- The runner generates a PDF report plus markdown and JSON under `output/reports`.
- The scenario manifest covers ship hub idle, normal branch idle, active combat room, wave/crowded room, anchor boss smoke, and room transition/NavMesh attach.
- Each scenario uses a fixed `3s` warmup and `30s` sample window.
- The generated lock report includes live scenario evidence where present; deterministic baselines remain clearly labelled when capture is pending.

## Metrics
- Frame time and FPS percentiles.
- Main-thread and render-thread timing through Unity `ProfilerRecorder` when supported.
- GPU frame timing is marked unsupported when the current Editor/Metal context does not expose it.
- Profiler traces are saved beside each capture when Unity exposes trace export in the current Editor context.
- GC allocation, managed memory, graphics memory, object-count snapshots, and runtime operation counters.
- Operation counters include minimap rebuilds, wall-visibility updates, combat HUD refreshes, and runtime NavMesh fallbacks.

## Capture Comparison
- Reports compare the two latest live captures per scenario when before/after data exists.
- Comparison highlights p95 frame-time and p50 FPS movement so optimization passes can prove actual improvement.

## Ranked Suspects
- Desktop profile currently targets `120 FPS` with `vSyncCount = 0`, which can make the laptop run flat out even when gameplay is not heavy.
- PC URP settings are expensive for a cool editor target: HDR, depth texture, opaque texture, 50m shadows, 4 cascades, soft shadows, and additional light shadows are all part of the snapshot.
- Per-frame HUD/debug, minimap, and wall-visibility work should be measured before refactoring.
- Runtime NavMesh fallback is treated as a spike suspect because missing bakes can force dev-only room NavMesh builds.

## Deferrals
- M136 does not lower art quality, change gameplay, or alter branch generation.
- Full-run soak, VisionOS-specific tuning, and broad optimization patches are deferred until after the report is reviewed.
- No save schema, economy schema, combat schema, room-generation rule, or gameplay-facing UI changes.
