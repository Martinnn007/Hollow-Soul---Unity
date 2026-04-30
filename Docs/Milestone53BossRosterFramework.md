# M53: Boss Roster + Boss Framework V1

M53 adds the first real boss roster layer. Bosses are selected deterministically from the run/challenge seed and world band, use fixed HP between 20 and 50, and keep the existing boss reward flow.

## Roster
- W1 Stone Warden, 24 HP: upgraded slow pursuit, charge, stomp burst, and 4-way projectile phase.
- W1 Splinter Saint, 22 HP: hopping wooden idol with short radial splinter bursts.
- W1 Gravel Maw, 28 HP: fast snake-like chase boss with burrow/summon pressure.
- W2 Cartouche Widow, 32 HP: ancient tomb projectile boss with falling-shot style spreads.
- W2 Iron Reliquary, 36 HP: tactical cover shooter that peeks, fires, and relocates.
- W2 Mirror Husk, 34 HP: split-pressure boss that creates mirror bodies at HP thresholds.
- W2 Ash Comet, 38 HP: dash/jump boss with delayed impact burst behavior.
- W3 Choir of Teeth, 42 HP: projectile-pattern boss capped at 24 active boss projectiles.
- W3 Rust Bishop, 46 HP: turret walker with beam-style shots and mine-like rings.
- W3 Hollow Star Larva, 50 HP: mixed abyss boss with chase, summon, starfall, and desperation burst.

## Runtime Rules
- Bosses have fixed HP; no hidden world stat scaling is applied.
- Boss body contact uses boss threat damage and remains non-perfect-parryable.
- Light boss projectiles can be guarded/parried if marked light; heavy/strong/boss threats cannot be perfect-parried.
- Boss-owned projectiles are capped at 24 active projectiles.
- Boss-summoned minions use existing enemy spawn kinds and must be cleared with the boss before the room completes.
- Boss arenas are Room Designer-compatible approved runtime rooms and remain gameplay-authored data.

## Implementation Notes
- `BossCatalogDefinition` owns the roster.
- `BossSelectionResolver` chooses by world band and seed.
- `RoomEncounterAssignment` and `RoomCombatEncounterContext` preserve `bossId`, `bossArenaId`, `worldBand`, and phase metadata.
- `BossRuntimeController` supplies v1 bespoke movement/projectile/summon behavior.
- `BossHudController` renders the top-center boss name, HP bar, and compact status line on `PlatformShellCanvas`.
- Boss rewards remain unchanged and continue through the existing boss reward pool.
