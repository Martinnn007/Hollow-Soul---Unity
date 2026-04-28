# M42: Player Build UX + Pickup Clarity

M42 makes the run build readable during play. The build details move into a dedicated left-side HUD panel, while pickups and shop purchases trigger a center-right reveal card.

## Runtime UX

- `PlayerBuildHudController` lives on `PlatformShellCanvas` and shows core stats, currencies, equipment, stamina, active item/card, and active set.
- `PickupRevealController` lives on `PlatformShellCanvas` and shows temporary pickup cards with generated glyphs and rarity-colored presentation.
- The branch minimap remains top-right and no longer owns detailed player-build status.
- Combat HUD remains focused on immediate room state.

## Replacement Pickups

Weapons, armor, active items, and consumable cards still auto-equip. If they replace an existing slot, the old gear drops into the room as a swap-back pickup.

Replacement pickups are saved in the active run snapshot and restored through Continue. They are cleared when the branch/run context ends.

## Non-Goals

- No glossary/help overlay.
- No item-choice UI.
- No final icon art requirement.
- No new reward content.
