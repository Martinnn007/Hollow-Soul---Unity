# M92 Pathfinding Backend Adapter Report

- Backend added: `RoomGridAStar`.
- Grid cell size: `0.50m`.
- Grounded mobile non-boss enemies using path-aware movement: 19.
- Grounded stationary non-boss enemies remaining local/diagnostic-only: 4.
- Flying enemies remaining local/floor-region movement: 3.
- Bosses remaining runtime-exempt: 10.
- Path-aware intents: approach, preferred/action-envelope spacing, flee/reset, investigate, wander, return-home.
- Smart action-envelope goal sampling: enabled via one bounded candidate-set search.
- Occupancy cache: enabled per room/radius bucket with conservative layout/blocker invalidation.
- Fresh path budget: enabled with Tactical/Cunning reserve and graceful local fallback.
- Debug path diagnostics: requests, solves, cache hits, occupancy builds, deferrals, fallbacks, budget, and solve timing.
- Exempt intents: active attacks, lunges, charges, creature bursts, phase movement, bump separation, recovery commitment.
- Debug menu path tracing overlay: enabled.
- Local steering fallback: enabled.
- Docs: `Docs/Hollow_M92_Pathfinding_Backend_Adapter.md`.
- Report: `output/reports/m92_pathfinding_backend_adapter.md`.
