# M102 Reachable Tactical Positioning Report

- `RoomTacticalDirector` validates tactical reservation candidates with `NavMesh.SamplePosition` and `NavMesh.CalculatePath`.
- Accepted reservations require `EnemyPathStatus.Ready`.
- Reservation scoring now includes path length, clearance, desired action distance, and existing slot separation.
- `EnemyTacticalIntent` exposes reservation path status, corner count, and length.
- Active tactical slots without reachable positions are downgraded before they can start committed attacks.
