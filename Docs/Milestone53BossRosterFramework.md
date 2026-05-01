# M53: Boss Roster + Boss Framework V1

M53 adds a data-driven boss roster and top-center boss HUD.

- Bosses use fixed HP from 20-50 with no hidden world scaling.
- Boss selection is deterministic from run/challenge seed and world band.
- Each boss owns a Room Designer-compatible approved arena.
- Boss rewards remain on the existing boss reward path.
- Boss projectiles are capped at 24 active boss-owned projectiles.
- Boss-summoned minions use existing enemy kinds and count for room clear.
- Boss Lab is represented by the generated boss catalog and validator routes; any boss can be launched by selecting its `bossId` and arena.

Roster:
- W1: Stone Warden (`stone_warden`), HP 24, arena `boss_arena_broken_gateyard`.
- W1: Splinter Saint (`splinter_saint`), HP 22, arena `boss_arena_narrow_shrine`.
- W1: Gravel Maw (`gravel_maw`), HP 28, arena `boss_arena_sandy_pit_ring`.
- W2: Cartouche Widow (`cartouche_widow`), HP 32, arena `boss_arena_open_tomb`.
- W2: Iron Reliquary (`iron_reliquary`), HP 36, arena `boss_arena_cover_maze`.
- W2: Mirror Husk (`mirror_husk`), HP 34, arena `boss_arena_symmetric_mirror`.
- W2: Ash Comet (`ash_comet`), HP 38, arena `boss_arena_charred_crossing`.
- W3: Choir of Teeth (`choir_of_teeth`), HP 42, arena `boss_arena_hell_heaven_dais`.
- W3: Rust Bishop (`rust_bishop`), HP 46, arena `boss_arena_industrial_cover_grid`.
- W3: Hollow Star Larva (`hollow_star_larva`), HP 50, arena `boss_arena_blind_deep`.
