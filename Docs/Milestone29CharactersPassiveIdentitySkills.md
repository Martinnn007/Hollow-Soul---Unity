# Milestone 29: Characters + Passive Identity Skills

M29 adds the first run-only character selection layer. The main menu keeps platform-first launch buttons, but each New Run platform button now opens a character picker before the run starts.

## Runtime Rules

- Character choice is session/run-only and is saved inside the active run snapshot.
- Continue Run restores the saved character from `RunSaveSnapshot.runBuild.selectedCharacterId`.
- Character choice is not written to profile metadata in M29.
- Both first characters are unlocked immediately.
- Character passive skills are always-on run-start stat modifiers, not active abilities.

## Characters

- `Balanced`: default base stats, `starter_blade`, `starter_bolt`, and `Steady Form` for `+10 max stamina` and `+1 stamina regen`.
- `Heavy`: `9 HP`, `3.15 speed`, `2 strength`, `130 stamina`, `15 stamina regen`, `2 defense`, `+1 melee damage`, `starter_blade`, `starter_bolt`, and `Crushing Grip` for another `+1 melee damage`.

## Validation

Use `Hollow/Generation/Generate Milestone 29 Assets` to regenerate character assets, then `Hollow/Validation/Run Milestone 29 Validation` to verify catalog content and scene wiring.
