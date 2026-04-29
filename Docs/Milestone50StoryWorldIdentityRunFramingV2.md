# M50: Story, World Identity + Run Framing V2

M50 gives Hollow its first real identity layer without changing branch mechanics. The run is still a three-world mechanical loop, but each normal run and fixed-seed challenge resolves a deterministic itinerary from an eight-world Hollow Star catalog.

## Core Fiction

The catastrophe is `The Hollow Star`: a black-hole-like collapse that ate worlds, timelines, and myths, then spat them out as mixed room-branches. The player is a nameless remnant carrying identity deeper through the collapse. The hub is a memory anchor, not a permanent safe city.

## Active World Identities

- `The Broken Meridian`: mixed threshold and shattered timelines.
- `Before Teeth`: prehistoric hunger before language.
- `The Sunken Cartouche`: ancient Egypt drowned into impossible afterlife water.
- `The Black Keep`: medieval terror, siege smoke, iron, and failed prayer.
- `The Rust Choir`: fallen future machines still singing after death.
- `The Choir Below`: hell and heaven collided and kept singing.
- `The Last Hour`: the end of times looped until endings are tired.
- `The Blind Deep`: abyssal pressure, memory, and no horizon.

## Runtime Behavior

- `RunWorldItineraryService` resolves three distinct world identities from the root run seed.
- Continue remains deterministic because it restores the same root run seed.
- M47 challenges use their fixed root seeds and the same itinerary resolver.
- `RunFramingHudController` still lives on `PlatformShellCanvas`, outside `WorldPresentationRoot`.
- World entry now shows a short toast/card when the resolved world identity changes.
- Hub branch portals use branch echo labels from the current resolved world instead of generic branch labels.

## Non-Goals

- No biome filtering for rooms, encounters, rewards, materials, hazards, or difficulty.
- No new save fields unless future seed data proves insufficient.
- No changes to branch generation, rewards, combat, shops, or final extraction mechanics.
- No final story cutscenes or public-facing lore codex yet.

## Future Hooks

The M50 metadata intentionally includes hidden biome tags, palette hints, lighting hints, material notes, and branch echo names. Later milestones can use those fields to drive ArtPass overrides, room-pool weighting, encounter palettes, boss variants, and story fragments.
