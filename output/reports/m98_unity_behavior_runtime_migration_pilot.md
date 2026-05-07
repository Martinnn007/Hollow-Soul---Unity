# M98 Unity Behavior Runtime Migration Pilot Report

- Package dependency: `com.unity.behavior` `1.0.13`.
- Package assembly probe: `Unity.Behavior`.
- Pilot enemies: `spawnEnemyRat`, `spawnEnemySkeletonSword`.
- Runtime bridge: `EnemyUnityBehaviorGraphBridge`.
- Deterministic fallback: `EnemyUnityBehaviorPilotEvaluator`.
- Hollow systems retained: M97 NavMesh, M93 threat director, M91 spacing, M80 active windows, M79 contact rules.
- Docs: `Docs/Hollow_M98_Unity_Behavior_Runtime_Migration_Pilot.md`.
