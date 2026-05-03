# Milestone 47: Challenge Mode V2 + Curated Seeds

M47 upgrades Challenge Mode from a small transient test list into seven curated full-run fixed-seed challenges. Challenge runs still never write active-run snapshots, clear profile runs, bank souls, or grant meta rewards. They do record lightweight profile results so later milestones can add unlocks or rewards safely.

## Challenge Catalog

The generated catalog id is `m47_challenge_catalog_v2`.

| Challenge | Seed | Character | Intent |
| --- | ---: | --- | --- |
| Blade Trial | `47001` | Balanced | Melee-lean starter gear with shops closed. |
| Glass Runner | `47002` | Balanced | Speed/stamina lean with healing rewards blocked. |
| Stone Oath | `47003` | Heavy | Defense/guard lean with armor and mending charm. |
| Macro Maze | `47004` | Balanced | Macro-room traversal and positioning showcase. |
| Splitter Swarm | `47005` | Balanced | Harder M46 encounter bands and Echo Burst starter. |
| Merchant's Debt | `47006` | Balanced | Shop/economy showcase with low coins and starter souls. |
| Small Monsters | `47007` | Balanced | Non-boss rooms spawn only Rats and Spiders; boss rooms remain unchanged. |

## Runtime Rules

- Challenge sessions use `RuntimeSessionMode.TransientChallenge`.
- The fixed challenge seed becomes the root run seed.
- The branch runtime uses `m46_encounter_director_curve_v1`, including world lengths `8/10/12`.
- Completion is only through the existing final extraction portal after World 3.
- Death ends the attempt without completion.
- Rule entries are narrow run modifiers: block shops, block healing rewards, increase encounter pressure, or remap non-boss encounter spawns to small monsters.
- `Small Monsters` preserves boss encounters and remaps non-boss enemy spawn kinds to `spawnEnemyRat` and `spawnEnemySpider`.

## Result Records

Challenge results are stored separately from active runs in the profile JSON:

- `attempts`
- `completions`
- `bestClearTimeSeconds`
- `lastResult`
- `lastPlayedSeed`

These records are intentionally not meta progression. M47 only captures evidence that a profile played or cleared a challenge.

## Validation

Run:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "<project>" -executeMethod Hollow.Editor.Generation.Milestone47AssetGenerator.Generate
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "<project>" -executeMethod Hollow.Editor.Validation.Milestone47Validator.Validate
```

The validator checks catalog identity, all seven fixed seeds, V2 rules/loadouts, scene wiring, menu wiring, and challenge-record persistence without active-run mutation.
