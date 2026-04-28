# Milestone 37: Enemy/Boss Behavior Readability Pass

M37 makes enemy intent easier to read without changing branch generation, rewards, saves, shops, room import, or platform presentation.

## Runtime Changes

- Entry grace now has a visible safe telegraph state.
- Chargers pause briefly in `ChargeWindup` before entering `Charging`.
- Turret and boss ranged attacks pause in `RangedWindup` before firing.
- Stone Warden low-health burst pauses in `BossBurstWindup` before spawning the four-direction projectile burst.
- `CombatReadabilityPresenter` creates non-blocking in-world telegraph visuals:
  - Safe ring for entry grace.
  - Aim line for charge/ranged windups.
  - Danger ring for boss burst.
  - Short state label above enemies.

## Design Notes

- Telegraph visuals are presentation-only and have disabled colliders.
- Damage, movement, collision, and projectile ownership remain authoritative in the combat controllers.
- Timings are intentionally short so enemies stay threatening but become fairer:
  - Charger windup: `0.42s`.
  - Ranged windup: `0.34s`.
  - Boss burst windup: `0.68s`.

## Validation

Run:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode --burst-disable-compilation -projectPath "$(pwd)" -executeMethod Hollow.Editor.Generation.Milestone37AssetGenerator.Generate -quit
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode --burst-disable-compilation -projectPath "$(pwd)" -executeMethod Hollow.Editor.Validation.Milestone37Validator.Validate -quit
```

Expected evidence:

- Windup constants are positive.
- Telegraph material roles resolve through prototype and ArtPass palettes.
- Chargers, turrets, and Stone Warden expose readable states before high-pressure actions.
- Telegraph primitives do not add gameplay colliders.
