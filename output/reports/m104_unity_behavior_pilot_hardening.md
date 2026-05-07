# M104 Unity Behavior Pilot Hardening Report

- Package: `com.unity.behavior` `1.0.13`.
- Schema version: `104`.
- Runtime bridge: `EnemyUnityBehaviorGraphBridge`.
- Stable schema source: `EnemyUnityBehaviorBlackboardSchema`.
- Trace source: `EnemyUnityBehaviorTraceEntry`.
- Rat graph contract: `m104_spawnEnemyRat_unity_behavior` (`EmergencyOnly`).
- Skeleton Sword graph contract: `m104_spawnEnemySkeletonSword_unity_behavior` (`EmergencyOnly`).
- Required outputs: `OutputCommandKind`, `OutputActionId`, `OutputSpeedMultiplier`, `OutputReason`.
- Emergency fallback: explicit and trace-visible; no silent deterministic override.
- Boss runtime: unchanged.
- Docs: `Docs/Hollow_M104_Unity_Behavior_Pilot_Hardening.md`.
