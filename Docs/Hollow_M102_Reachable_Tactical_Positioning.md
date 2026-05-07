# M102: Reachable Tactical Positioning V1

M102 upgrades tactical reservations from geometric player rings to NavMesh-validated combat positions.

## Contract

- `RoomTacticalDirector` samples candidate combat positions around the player, snaps them to Unity NavMesh, and accepts only complete paths from the enemy to the sampled point.
- Reserved positions still respect room collision, enemy radius, clearance scoring, and existing anti-dogpile spacing.
- Active threats without a reachable reservation are downgraded into support positioning instead of committing from an impossible slot.
- `EnemyTacticalIntent` now records reservation path status, corner count, path length, and reachability for debug overlays/tests.
- M101 locomotion ownership remains unchanged: NavMesh owns movement to the reservation, Hollow owns attacks, knockback, stun, death, lunges, charges, and recovery.

## Runtime Impact

- Enemies path toward reachable attack starts rather than sliding against rocks to reach mathematically nice circles.
- Rooms missing usable NavMesh data cannot produce reachable tactical reservations.
- Boss runtime behavior remains unchanged.
