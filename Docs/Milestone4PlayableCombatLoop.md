# Milestone 4 - Playable Combat Loop

Milestone 4 turns the imported M3 sample room into the first playable combat slice.

## Runtime Behavior

- Player movement uses room-local meters: WASD / left stick moves on X/Z.
- Shooting uses cardinal directions: arrow keys / right stick fires projectiles.
- The imported room remains authoritative for bounds, rocks, and enemy spawn markers.
- Four spawned enemies use the same simple chaser archetype.
- Projectiles damage enemies and despawn on enemy, rock, bounds, or lifetime expiry.
- The room transitions from `WaitingToStart` to `InCombat` to `Cleared`.
- Doors remain decorative, but tint green when the room clears.

## Defaults

- Player speed: `4m/s`.
- Player HP: `6`.
- Projectile speed: `9m/s`.
- Projectile damage: `1`.
- Projectile lifetime: `1.5s`.
- Projectile cooldown: `0.22s`.
- Chaser HP: `3`.
- Chaser speed: `1.5m/s`.
- Chaser contact damage: `1` every `1s`.

## Platform Rule

Gameplay stays in local room meters. Vision Pro bounded still scales only `WorldPresentationRoot` to `0.5`; the HUD stays on `PlatformShellCanvas` outside the scaled world.

## Commands

Generate M4 assets:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m4-generate.log \
  -executeMethod Hollow.Editor.Generation.Milestone4AssetGenerator.Generate
```

Validate M4:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m4-validate.log \
  -executeMethod Hollow.Editor.Validation.Milestone4Validator.Validate
```
