# Milestone 45: Room Hazards + Interactive Physics V1

M45 adds authored hazards and deterministic room-local destructible props.

- Room Designer can author spikes, standard barrels, and explosive barrels.
- HollowRuntime V2 remains schema `2`; hazards and interactive objects are optional fields.
- Spikes are always-on environmental hazards that damage player and grounded enemies.
- Pits remain non-walkable for player and grounded enemies; flying enemies can cross pit tiles while staying inside macro floor regions.
- Standard barrels block movement/projectiles, break from attacks/projectiles/explosions, and can drop a tiny coin pickup.
- Explosive barrels chain nearby barrels and deal reduced boss damage.
- Broken barrel state and uncollected tiny coin drops persist in active-run snapshots.
- Starter/origin rooms clear hazards at runtime so they remain safe onboarding rooms.

Validation:

```text
Hollow/Generation/Generate Milestone 45 Assets
Hollow/Validation/Run Milestone 45 Validation
```
