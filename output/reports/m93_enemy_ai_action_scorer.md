# M93 Enemy AI Action Scorer Report

- Milestone: `M93: Enemy AI Action Scorer + Threat Director V1`.
- Runtime scope: non-boss enemies.
- Boss runtime behavior: unchanged.
- Added AI brain: adaptive LOD, cached commands, selected action, blackboard diagnostics.
- Added action scorer: deterministic weighted scoring over current runtime action profiles.
- Added threat director: soft pressure caps for melee, ranged, area, and charge lanes.
- Debug menu: `Enemy AI Blackboard` toggle plus aggregate AI LOD/action summary.
- Preserved systems: M80 active windows, M91 action spacing, M92 pathfinding, room attack budgets, harmless ordinary contact.
- Tests: scorer selection, LOD promotion/reduction, soft pressure penalty, debug blackboard, docs/report existence.
