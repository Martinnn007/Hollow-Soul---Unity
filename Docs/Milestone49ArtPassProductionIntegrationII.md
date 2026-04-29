# Milestone 49: ArtPass Production Integration II

M49 is the production-art replacement gate for the current Hollow prototype. It assumes Rafal may not have production assets ready yet, so validation warns when a role still uses generated placeholder art and fails only when the active ArtPass wiring is unsafe or broken.

## Workflow

- Replace active prefabs directly under `Assets/_Hollow/Prefabs/ArtPass/`.
- Use `AP_<PresentationPrefabRole>.prefab` for entity/prop visuals and `VFX_<PresentationPrefabRole>.prefab` for VFX placeholders.
- Keep `PresentationVisualMarker` on the prefab root with the matching `PresentationPrefabRole`.
- Keep all replacement art visual-only. Do not add gameplay colliders, combat scripts, traversal scripts, reward logic, save logic, or runtime layout logic to ArtPass prefabs.
- Room Designer Scene Mode uses the same active presentation catalog as gameplay, so replacing an ArtPass prefab should be visible in both the game and Room Designer preview.

## Priority

The core vertical-slice pack is the first production-art target:

- Player
- Room floor and rock obstacle
- Active, locked, cleared, and unavailable doors
- Normal enemy and Stone Warden boss
- Player and enemy projectiles
- Reward pickup and boss key
- Hub shop, shop cards, branch portals, and next-world/final portals

All other visible roles are still tracked in the M49 report: enemy variants, equipment, active/consumable pickups, hazards, barrels, coin drops, secret doors, and VFX placeholders.

## Validation

Run:

```bash
Unity -batchmode -quit -projectPath "<project>" -executeMethod Hollow.Editor.Generation.Milestone49AssetGenerator.Generate
Unity -batchmode -quit -projectPath "<project>" -executeMethod Hollow.Editor.Validation.Milestone49Validator.Validate
```

M49 fails on:

- missing or broken `PresentationContentCatalog` prefab bindings
- prefabs outside `Assets/_Hollow/Prefabs/ArtPass/`
- missing or wrong `PresentationVisualMarker`
- gameplay colliders or gameplay scripts on visual prefabs
- unsafe scale, pivot, or renderer bounds
- missing required ArtPass Addressables labels

M49 warns on:

- generated primitive placeholder art still being used
- large-but-not-dangerous bounds that should be checked for Vision Pro comfort
- small pivot offsets that may need artist review

## Reports

Generated outputs:

- `output/reports/m49_artpass_production_status.json`
- `output/reports/m49_artpass_production_status.md`
- `output/pdf/Hollow_M49_ArtPass_Production_Integration_II.pdf`

The PDF is a practical checklist for Martin and Rafal: what to replace, how to replace it safely, and which roles are still placeholders.
