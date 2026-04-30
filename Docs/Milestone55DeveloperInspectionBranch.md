# M55 Developer Inspection Branch + Debug Spawn Menu

M55 adds a non-persistent Developer Lab launched from the selected-profile main menu. It creates a fixed ten-room left-to-right Wide2x1 branch for inspecting environment pieces, pickups, enemies, bosses, VFX, portals, doors, and hazards.

## Runtime Rules
- `RuntimeSessionMode.DeveloperLab` never writes active run saves, challenge attempts, completions, or banked rewards.
- Lab rooms are pre-cleared so traversal remains open while frozen runtime entities are visible.
- Generated lab room sources live under `Assets/_Hollow/Data/Rooms/DeveloperLab/` and are mirrored into curated Room Designer drafts when the M55 generator runs.
- Lab enemies and bosses are real runtime entities in `FrozenRuntime` inspection mode: they keep visuals and health, but do not move, attack, contact-damage, summon, or block room clear.
- The bottom-right `Debug Spawn` button opens the debug spawn menu in editor/development gameplay routes. Menu buttons change group/entity, spawn in front of the player, and toggle live/frozen mode.
- Manual debug spawns are live by default but non-authoritative: they never count for room clear, persistence, challenge records, rewards, or branch progression.

## Room Layout
1. Environment basics.
2. Economy and sustain.
3. Weapons, armor, items, cards, set pieces.
4. Normal enemy gallery.
5. Projectile/VFX/audio cue gallery.
6. Live hazard/physics lane.
7. Hub/progression props.
8. World 1 boss gallery.
9. World 2 boss gallery.
10. World 3 boss gallery.
