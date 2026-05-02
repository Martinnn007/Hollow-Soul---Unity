# M70: Player-To-Enemy Knockback + Enemy Body Weight V1

M70 makes player attacks physically move enemies using authored attack force and enemy body weight.

- `WeaponAttackDefinition` now exposes impact force and knockback distance.
- `EnemyDefinition` and `BossDefinition` now expose body class.
- Light enemies move more, medium enemies move normally, heavy enemies resist, and bosses use massive nudge-only resistance.
- Knockback is move-only: no stun, no stagger, no attack cancellation, and no save-state mutation.
- Frozen Developer Lab enemies ignore knockback.
