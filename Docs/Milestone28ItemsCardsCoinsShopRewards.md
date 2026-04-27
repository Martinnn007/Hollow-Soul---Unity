# Milestone 28: Items, Cards, Coins, And Shop Rewards

M28 adds the first usable item/card layer on top of M27 weapons. Room rewards remain seeded and automatic, while the hub shop now supports mixed currencies: coins for most shop content and souls for rare weapons.

## Runtime Rules

- `RewardKind.Card` remains a backward-compatible passive-card alias.
- Passive items and passive cards are added to run inventory immediately.
- Active item pickups replace the active item slot and start with three charges.
- Consumable card pickups replace the consumable card slot.
- Active items are used with `Q` or gamepad north and spend one charge.
- Consumable cards are used with `F` or gamepad west and are consumed immediately.
- Active items regain one charge when entering/clearing rooms, capped at three.
- Temporary card buffs last eight seconds and are not serialized mid-buff.

## M28 Content

- Passive/stat rewards: Vital Locket, Iron Stitch, Fleet Pin, Stamina Thread, Blade Lesson, Bolt Lesson.
- Active items: Mending Charm, Echo Burst.
- Consumable cards: Ember Card, Swift Card, Mend Card.
- Coin rewards: common Coin Cache and treasure/secret Treasure Coins.

## Shop Pricing

- Heal: 8 coins.
- Consumables, passives, and active item rewards: 16 coins unless otherwise tuned by a future milestone.
- Rare weapon offers: 22 souls.

## Validation

Run `Hollow/Generation/Generate Milestone 28 Assets`, then `Hollow/Validation/Run Milestone 28 Validation`.
