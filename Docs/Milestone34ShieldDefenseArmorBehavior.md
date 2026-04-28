# Milestone 34: Shield / Defense / Armor Behavior V1

M34 turns the existing defense and armor stats into a first playable defensive layer without changing branch generation, rewards, saves, room data, or platform presentation.

## Scope

- Passive defense mitigation reduces incoming player damage by `1` for every `2` defense, while preserving a minimum of `1` damage for unguarded positive hits.
- Holding guard raises a prototype shield, drains stamina over time, and spends a stamina block cost when a hit lands.
- A guarded hit reduces damage by an additional `1`; small hits can be fully blocked.
- Guarded contact hits push the enemy source away from the player.
- The combat HUD shows current defense and whether the shield is up.

## Controls

- Keyboard: hold `Left Shift` or `Right Shift` to guard.
- Gamepad: hold left trigger to guard.

## Notes

- Shield/parry timing windows, perfect parries, armor visuals, block VFX, directional shields, and armor-specific abilities are intentionally deferred.
- Defense remains a run-only derived stat from characters, rewards, armor, and synergies.
- Existing enemies/projectiles keep using `DamageSystem`; player defense is applied by the player-owned `PlayerDefenseController`.

## Validation

Run:

```bash
Hollow/Generation/Generate Milestone 34 Assets
Hollow/Validation/Run Milestone 34 Validation
```

For full confidence, rerun the platform QA gate after generation.
