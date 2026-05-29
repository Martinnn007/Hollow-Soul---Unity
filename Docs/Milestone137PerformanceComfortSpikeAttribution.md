# M137: Performance Comfort + Spike Attribution

## Summary
M137 is the first low-risk optimization pass after M136B corrected live capture. It locks Windows standard play to 60 FPS, reduces obvious per-frame HUD/wall churn, and adds attribution markers for boss and transition spikes.

## Locked Runtime Policy
- Windows standard target frame rate: `60`.
- Wall visibility max refresh: `10 Hz`.
- Combat HUD max refresh: `10 Hz`.
- Boss HUD max refresh: `15 Hz`.
- VisionOS profiles retain their existing frame targets.

## Measurement Policy
- M136B live capture remains the source of truth for before/after evidence.
- M137 adds profiler markers for wall visibility, combat HUD, boss HUD, room transitions, boss spawn/activation, and minimap rebuilds.
- Profiler traces stay optional and should be used only for focused boss/transition spike investigation.

## Deferrals
- No URP/shadow/lighting quality changes are applied in M137.
- No gameplay, reward, economy, save-data, combat stat, or room-generation behavior changes are applied in M137.
