# Milestone 18: Seeded Random Rewards

M18 replaces fixed procedural reward cycling with seeded automatic reward rolls. Players do not choose rewards yet; shops and choice UI remain later milestones.

## Runtime Behavior

- Fresh M17 feature branches generate a `ProceduralRewardPlan` from branch ID, branch seed, room ID, and room role.
- Standard rooms roll from `StandardRoomRewardPool`.
- Treasure rooms roll from `TreasureRewardPool`.
- Boss rooms roll from `BossRewardPool`.
- Reward pickup still applies immediately on interaction, marks the room reward claimed, updates run stats/souls, and checkpoints the active run.
- The minimap/HUD now shows the latest automatic reward message.

## Persistence

Generated rewards are serialized into `RunSaveSnapshot.proceduralRewardPlan`, including effect metadata. Continue restores the saved reward plan and does not reroll.

Legacy M15 snapshots without a reward plan still use the old fixed M15 fallback path.

## Validation

Run:

```bash
Hollow/Generation/Generate Milestone 18 Assets
Hollow/Validation/Run Milestone 18 Validation
```

Validation checks reward pool assets, role-specific reward coverage, deterministic seeded plans, effect save/restore support, and scene wiring for all game routes.
