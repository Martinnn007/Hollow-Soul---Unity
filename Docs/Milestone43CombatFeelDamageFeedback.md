# M43 Combat Feel V2 + Damage Feedback

M43 adds the first dedicated combat-feel layer without changing branch generation, rewards, shops, saves, or encounter content.

## Locked Feel Direction

- Readable arcade combat with no hit-stop, no camera shake, and no damage-number UI.
- Player gets `0.6s` invulnerability after taking damage.
- Enemy and player hits apply room-local knockback.
- Bosses and heavy enemies resist knockback but still flash/react.
- Enemy death leaves a visual-only corpse ghost for `1.5s`.
- Enemy windups stay subtle: material/ring/aim-line cues are preferred over loud labels.

## Runtime Pieces

- `CombatFeelProfileDefinition` owns i-frame, knockback, flash, windup, and corpse-ghost tuning.
- `DamageFeedbackContext` extends `DamageRequest` with optional knockback metadata while preserving old constructors.
- `PlayerDamageFeedbackController` handles player i-frames, minimal flash, and blocked-damage feedback.
- `CombatKnockbackReceiver` applies local-space, collision-safe knockback to players and enemies.
- `CorpseGhostPresenter` creates non-colliding, fading death ghosts.

## Content Hooks

M43 adds VFX/audio cue IDs for `PlayerInvulnerable`, `KnockbackImpact`, `EnemyWindup`, `EnemyCorpseGhost`, and `DamageBlocked`. Generated VFX cues use primitive fallbacks so Rafal can replace them later through the existing ArtPass/presentation catalog.

## Validation

Use `Hollow/Generation/Generate Milestone 43 Assets` to refresh the profile/cues, then `Hollow/Validation/Run Milestone 43 Validation`.
