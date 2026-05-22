# M44 Shield / Armor Behavior V2

M44 upgrades defense into an active shield loop while keeping armor as run-only stat and playstyle equipment.

## Player Shield Rules
- Guard is universal and uses the existing guard input: keyboard `Shift`, gamepad left trigger.
- Guard is aim-facing. The shield uses the latest aim direction, then movement direction, then north as fallback.
- The first `0.30s` after guard starts is a perfect-parry window.
- The guard cone is `140 degrees`; attacks outside the cone bypass active guard/parry.
- Guarding slows movement to `55%` speed and blocks player attacks until guard is released.

## Damage And Threats
- `DamageRequest` now carries `DamageThreatKind`, defaulting legacy calls to `Light`.
- `Light` threats can be perfect-parried.
- `Heavy`, `Boss`, and `StrongProjectile` threats can be guard-reduced but not perfect-parried.
- Perfect parry fully blocks damage, spends `28` stamina, and counter-damages the source for `1`.
- Holding guard does not drain stamina by itself; stamina regeneration is slowed while guarding.
- Normal guard spends `22` stamina per guarded hit, reduces damage by `1`, and applies only minimal source push.

## Presentation
- `ShieldGuardVisualController` creates a generated non-colliding shield prop while guarding.
- Shield materials/cues distinguish guard, parry, block, and unavailable states.
- ArtPass can replace the generated shield later; the placeholder has no gameplay collider.

## Boundaries
- No shield inventory slot is added.
- Armor remains stat-only in this milestone.
- Projectiles are destroyed on parry but not reflected.
- No hit-stop, camera shake, new enemies, rewards, room-generation changes, or save schema changes are introduced.
