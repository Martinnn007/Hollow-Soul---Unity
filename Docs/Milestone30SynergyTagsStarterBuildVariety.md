# Milestone 30: Synergy Tags + Starter Build Variety

M30 adds the first run-only set synergy layer without changing branch generation, shops, room designer exports, or permanent progression. Tags stay internal for now, while the runtime HUD exposes only the active set display name and a short activation message.

## Runtime Rules

- The player now has a real armor equipment slot in addition to melee weapon, ranged weapon, active item, consumable card, passive items, and passive cards.
- Set synergies require at least 3 matching pieces across 3 different categories.
- Matching categories include character, equipped melee weapon, equipped ranged weapon, equipped armor, equipped active item, passive items, and passive cards.
- Consumable cards and temporary buffs never count toward set activation.
- Only one synergy can be active. Ties resolve by highest matching piece count, then priority, then synergy ID.
- Synergy bonuses are recalculated from current build state and applied as runtime-derived stat modifiers. They are not saved as permanent base stat changes.

## Generated Sets

- Skeletal pieces: `skeletal_sword`, `bone_bow`, `skeletal_armor`, `cursed_skull`, and `bone_totem`.
- Dragon pieces: `dragon_fang`, `dragon_bow`, `dragon_scale_armor`, `dragon_tooth`, and `dragon_heart`.
- Skeletal Set bonus: `+1 melee damage` and attack cooldown `x0.98`.
- Dragon Set bonus: `+1 ranged damage` and `+10 max stamina`.

## Content Pipeline

- `ArmorCatalog_M30.asset` owns armor definitions and their derived stat modifiers.
- `SynergyCatalog_M30.asset` owns set triggers and bonuses.
- M30 extends the existing M27 weapon catalog and M28 reward/usable pools so older validators and scene references remain stable.
- Reward, usable, weapon, armor, and character content now carries internal `BuildTag` data for future filtering and UI.

## Validation

Run from Unity:

```bash
Hollow/Generation/Generate Milestone 30 Assets
Hollow/Validation/Run Milestone 30 Validation
```

Batchmode validation entrypoint:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "$(pwd)" -executeMethod Hollow.Editor.Validation.Milestone30Validator.Validate -quit
```
