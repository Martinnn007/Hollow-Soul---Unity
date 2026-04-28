# Milestone 35: Challenge Mode V1

M35 adds the first challenge-mode layer: fixed-seed, curated-rule runs launched from the selected-profile main menu. Challenge sessions are transient and do not overwrite active runs, bank souls, or mutate profile progress.

## Scope

- Adds a generated challenge catalog with three fixed-seed challenges:
  - `Blade Trial`: seed `35001`, Balanced, +1 melee damage, -1 max HP, start with 8 coins.
  - `Glass Runner`: seed `35002`, Balanced, +0.45 speed, +10 stamina, -2 max HP, start with 12 coins.
  - `Stone Oath`: seed `35003`, Heavy, +2 defense, -0.25 speed, -1 stamina regen, start with 6 coins.
- Adds a `Challenges` entry to the selected-profile menu.
- Challenge cards launch Windows, VisionOS bounded, or VisionOS immersive routes.
- Challenge runs use `RuntimeSessionMode.TransientChallenge`.
- Challenge fixed seeds drive the same branch/reward/encounter/shop generation path as normal runs.

## Safety Rules

- Challenge launches do not call `MarkRunStarted`.
- Challenge launches do not clear active profile runs.
- Challenge sessions do not checkpoint active runs because `TransientSessionGuard` only permits profile-backed sessions.
- Continue Run remains profile-backed and ignores challenge selection.

## Validation

Run:

```bash
Hollow/Generation/Generate Milestone 35 Assets
Hollow/Validation/Run Milestone 35 Validation
```

For full confidence, rerun the platform QA gate after generation.
