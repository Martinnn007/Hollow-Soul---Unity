# Milestone 21: Shop Choice UI + Purchasable Rewards

M21 turns the M20 inter-branch hub shop into visible, player-facing shop cards.

Behavior:
- The shop always shows three in-world cards in the inter-branch hub: Heal 2 HP plus two seeded reward offers.
- Players walk near a specific card and press Interact to buy that exact offer.
- Purchases are one press, use run-local souls, apply immediately, and checkpoint immediately.
- Purchased cards stay visible as dimmed `SOLD` cards and cannot be purchased again.
- Unaffordable cards show the missing soul amount and no-op safely.

Out of scope:
- Normal room reward choice UI.
- Next-branch portal choice UI.
- Pointer, gaze, or mouse card selection.
- Rarity/depth-scaled shop prices.
