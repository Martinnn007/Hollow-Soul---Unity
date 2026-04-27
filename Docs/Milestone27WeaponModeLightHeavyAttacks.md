# Milestone 27: Weapon Mode + Light/Heavy Attacks

M27 makes player attacks catalog-driven. The player owns one melee weapon and one ranged weapon, swaps the active slot, and attacks with light or heavy inputs. Aim input only sets direction; it no longer fires by itself.

## Runtime Rules

- `WeaponCatalogDefinition` owns starter and rare replacement weapons.
- `starter_blade` and `starter_bolt` are the default run weapons.
- `iron_cleaver` and `ember_bolt` are rare replacement weapons for boss/shop reward testing.
- `R1` / `J` / mouse left performs light attack with the active weapon.
- `R2` / `K` / mouse right performs heavy attack with the active weapon.
- Right stick and arrow keys update aim direction only.
- Light/heavy damage, cooldown, stamina cost, and range come from `WeaponAttackDefinition`.

## Rewards And Saves

- `RewardKind.Weapon` uses `RewardId` as the weapon ID.
- Weapon rewards immediately replace the matching melee/ranged slot.
- Active weapon slot, equipped weapon IDs, and current stamina are saved in the active run snapshot.
- Shops can rarely roll deterministic weapon offers from the M27 weapon reward pool.

## Validation

Run `Hollow/Generation/Generate Milestone 27 Assets`, then `Hollow/Validation/Run Milestone 27 Validation`.
