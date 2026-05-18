# Milestone 5 - Enemy Archetypes and Diagnostics

Milestone 5 keeps the M4 combat loop intact and replaces the single hardcoded chaser with a small data-driven enemy layer. The room is still the imported M3 sample room, but each authored spawn kind can now resolve through an enemy catalog, receive difficulty tuning, and report lightweight diagnostics to the combat HUD.

## Runtime Behavior

- Enemy spawns now resolve from `EnemyCatalog` using the imported spawn marker `kind`.
- The default sample room spawns one of each archetype: normal, flying, fast, and heavy.
- Grounded enemies use the M4 obstacle-aware local-space movement helper.
- Flying enemies still respect room bounds, but ignore rock obstacle blocking.
- Difficulty is represented by `DifficultyTierDefinition` and currently defaults to `Developer Sample` with `1x` multipliers.
- Enemies show a simple HP/readability label and flash when damaged.
- Projectile despawn reasons are tracked for enemy hits, obstacle hits, bounds exits, and lifetime expiry.
- The combat HUD now includes difficulty, active enemy archetype counts, and projectile diagnostics.
- A placeholder `BossEncounterService` exists so boss progression can be added without reshaping combat orchestration later.

## Default Archetypes

- `spawnEnemyNormal`: grounded chaser, HP `3`, speed `1.5m/s`, contact damage `1`.
- `spawnEnemyFlying`: flying chaser, HP `3`, speed `1.8m/s`, contact damage `1`.
- `spawnEnemyFast`: grounded chaser, HP `2`, speed `2.4m/s`, contact damage `1`.
- `spawnEnemyHeavy`: grounded chaser, HP `6`, speed `0.9m/s`, contact damage `2`.

## Generated Assets

- `Assets/_Hollow/Data/Enemies/Enemy_Normal.asset`
- `Assets/_Hollow/Data/Enemies/Enemy_Flying.asset`
- `Assets/_Hollow/Data/Enemies/Enemy_Fast.asset`
- `Assets/_Hollow/Data/Enemies/Enemy_Heavy.asset`
- `Assets/_Hollow/Data/Enemies/EnemyCatalog.asset`
- `Assets/_Hollow/Data/Enemies/Difficulty_DeveloperSample.asset`

## Platform Rule

Gameplay remains in room-local meters. Windows and immersive routes use world scale `1.0`; Vision Pro bounded scales only `WorldPresentationRoot` to `0.5`. Enemy stats, movement, collision, and diagnostics are not affected by presentation scale.

## Commands

Generate M5 assets:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m5-generate.log \
  -executeMethod Hollow.Editor.Generation.Milestone5AssetGenerator.Generate
```

Validate M5:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m5-validate.log \
  -executeMethod Hollow.Editor.Validation.Milestone5Validator.Validate
```
