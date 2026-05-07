# M101: NavMesh Locomotion Ownership V1

M101 defines the movement contract between Unity NavMesh and Hollow combat execution.

## Ownership Contract

- Unity `NavMeshAgent` owns grounded non-boss locomotion during normal movement states: approach, spacing, flee, wander, investigate, and return-home.
- Hollow owns committed combat motion: windups, active lunges, charges, creature bursts, recovery movement, bump separation, knockback, death, and disabled/stationary states.
- Hollow-owned motion stops or clears the agent path first, moves through Hollow collision rules, then syncs/warps the agent back to the enemy transform.
- Agent-owned movement uses `NavMeshAgent.Move` and returns the agent-owned next position to the runtime controller.
- Damage still lands only through M79/M80 active windows; M101 changes ownership and synchronization, not attack balance.

## Debug Contract

- `EnemyNavMeshAgentBridge.CurrentOwnership` records `UnityNavMeshAgent`, `HollowManual`, or `Disabled`.
- `LastOwnershipReason`, `LastSyncReason`, and `SyncToTransformCount` expose handoff reasons for tests and debug overlays.
