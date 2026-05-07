# M101 NavMesh Locomotion Ownership Report

- Added `EnemyLocomotionOwnership`.
- `EnemyNavMeshAgentBridge` now tracks ownership, sync reasons, and sync count.
- Agent-owned movement uses `NavMeshAgent.Move`.
- `EnemyRuntimeController` applies navigation moves through one helper and syncs after Hollow-owned movement.
- Knockback now stops/syncs enemy NavMesh agents while Hollow displacement owns motion.
- Committed attacks, bump separation, recovery movement, death, and disabled states remain Hollow-owned.
