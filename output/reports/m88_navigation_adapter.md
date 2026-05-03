# M88 Navigation Adapter Report

- Added `EnemyNavigationBackend`, `EnemyNavigationMode`, and `EnemyNavigationIntent`.
- Added request/result structs and `EnemyNavigationAdapter`.
- Routed non-boss enemy movement through the adapter for chase, range, flee, wander, investigate, return-home, active attack movement, creature bursts, phase moves, and bump separation.
- Current backend remains local and non-pathfinding: `LocalSteering`.
- Documentation: `Docs/Hollow_M88_Navigation_Adapter.md`.
- Report: `output/reports/m88_navigation_adapter.md`.
- Next milestone M89 can add limited alert sharing without directly coupling ally wake-up logic to movement resolution.
