# M135: Full-Run Combat Readiness Lock

## Summary
M135 is a runtime and lock-artifact milestone focused on beta handoff readiness. It proves the normal full-run loop across the locked M132 world order, makes the melee-dodge core slightly more forgiving, and deep-polishes one anchor boss per world without expanding challenge modes.

## Full-Run Route Contract
- Normal runs validate the route `Before Teeth` -> `The Sunken Cartouche` -> `The Rust Choir`.
- The route begins from the ship `Portal Engine`, passes through normal branches and inter-branch hubs, and ends through final `Return to Ship`.
- Inter-branch hubs do not bank souls.
- Final `Return to Ship` banks souls and routes to ship Arrivals.
- Normal-run death routes to ship Arrivals and banks no souls.
- Arrivals quarantine remains the required post-run reset beat.
- M130-M134 branch rules for corrupted, wave, special, and Reward rooms are preserved.

## Combat Readiness
- Roll cost is locked to `30` stamina.
- Stamina regeneration delay after rolling is locked to `0.55s`.
- Roll startup is locked to `0.04s`.
- Roll invulnerability is locked to `0.26s`.
- Roll recovery is locked to `0.16s`.
- Roll distance is locked to `1.35m`.
- Melee remains the primary combat loop; M135 adds no new combo, ammo, or weapon system.
- Existing roll, hit, windup, and boss HUD feedback remain the readability base.

## Runtime Room Combat Readiness
- M132 biome room variants reuse the approved macro-room NavMesh bakes so combat and boss spawning stay live after the art-pack swap.
- Corrupted, Wave, Soul Eater, and Escapist 1x1 templates reuse the approved single-room bake until custom bakes are authored.
- Missing NavMesh coverage is a combat-readiness failure because rooms without a NavMesh do not spawn enemies.

## Boss Readiness
- Deep-polish anchors are `Stone Warden`, `Cartouche Widow`, and `Choir of Teeth`.
- Anchor bosses require readable attack windups, fair dodge windows, stable arena metadata, clear HUD/status, death, room-clear, and reward flow.
- all 10 boss catalog entries must satisfy the minimum smoke contract: catalog resolution, arena id, health, phases, attacks, attack/action profiles, HUD status support, and Boss Lab preview support.
- Full-roster work in M135 is readiness fixing, not deep per-boss redesign.

## Interfaces
- Adds `M135CombatReadinessPolicy` as a pure lock helper for roll constants and boss readiness checks.
- Adds M135 generator, validator, reports, and EditMode lock tests.
- No save schema, reward schema, economy schema, room-role, chest-kind, biome, challenge-mode, biomass, Black Orb, or companion-system changes.
