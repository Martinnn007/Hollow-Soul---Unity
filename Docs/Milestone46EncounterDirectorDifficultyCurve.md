# Milestone 46: Encounter Director + Difficulty Curve V1

M46 adds a deterministic encounter-director layer for normal runs and challenge runs.

- Fresh generated runs use branch identity `m46_encounter_director_curve_v1`.
- World 1, 2, and 3 target `8`, `10`, and `12` rooms respectively.
- Difficulty increases through weighted encounter composition, not hidden enemy stat scaling.
- Origin/starter, treasure, and secret rooms remain no-combat.
- Boss rooms continue using the existing Stone Warden encounter.
- Non-boss encounter requests are capped at six enemies before authored room anchors place them.
- Encounter assignment metadata is saved with world index, difficulty band, and director pressure.
- The combat HUD shows a compact debug line such as `Director: W2 B4 | turret_crossfire`.
- Challenge mode uses the same directed curve with fixed challenge seeds.

Validation:

```text
Hollow/Generation/Generate Milestone 46 Assets
Hollow/Validation/Run Milestone 46 Validation
```
