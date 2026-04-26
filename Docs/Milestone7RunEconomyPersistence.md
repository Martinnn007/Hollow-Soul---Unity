# Milestone 7: Run Economy, Persistence, Meta Progression

Milestone 7 turns the deterministic five-room branch into the first profile-backed run loop. The branch is still the M6 cross layout and still uses the imported `combat_single_sample.hollowruntime.json`, but rewards now affect the active run, active runs checkpoint into the selected profile slot, and souls bank only when the branch is completed through the hub-return portal.

## Runtime Loop

- New Run clears any old active snapshot for the selected slot, increments `totalRuns`, launches the requested platform route, and checkpoints immediately after branch initialization.
- Continue Run restores the active snapshot for the selected slot, including current room, visited/cleared state, pending or claimed rewards, run souls, collected reward records, player stats, and current HP.
- Transient sessions remain blocked from persistence by `TransientSessionGuard`; developer samples and designer playtests must never write or clear profile-backed snapshots.
- Player death clears the active run without banking souls and routes safely back to the main menu.
- Hub portal completion banks run souls into profile meta, increments completed runs, clears the active run, and returns to the main menu.

## Deterministic Reward Table

| Room | Souls | Reward | Effect |
| --- | ---: | --- | --- |
| North | 10 | Stone Heart | +1 max HP and heal 1 |
| South | 10 | Quick Draw | -10% shot cooldown |
| East | 10 | Fleet Step | +0.5m/s movement speed |
| West | 10 | Ember Charm | +1 projectile damage |

## Save Snapshot

`RunSaveSnapshot` stores only gameplay state. Visual presentation and platform-specific scaling are rebuilt from scene/platform settings on load.

- Branch identity and current room.
- Per-room visited, cleared, and reward state.
- Run-local souls and collected reward records.
- Player run stats and current HP.
- Platform kind string for diagnostics.

## Profile Schema V2

`JsonProfileStore` now writes schema version `2`.

- Existing schema `1` profiles migrate with empty active run data, `bankedSouls = 0`, and `completedRuns = 0`.
- Each slot stores one optional active run snapshot.
- Slot summaries expose `hasActiveRun`, `bankedSouls`, `completedRuns`, and `totalRuns` for menu display.

## Validation

Run `Hollow/Generation/Generate Milestone 7 Assets`, then `Hollow/Validation/Run Milestone 7 Validation`.

The validator checks deterministic reward definitions, schema v2 save/load/complete behavior, scene wiring, and platform scaling. The EditMode suite adds economy idempotency, stat effects, active-run snapshot persistence, completion banking, death clearing, and transient-session safety coverage.
