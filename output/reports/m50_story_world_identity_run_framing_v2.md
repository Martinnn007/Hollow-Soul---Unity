# M50 Story, World Identity, And Run Framing V2

- Generated: 2026-05-02T00:16:46.9171620Z
- Catalog: `Assets/_Hollow/Data/Worlds/M50/RunFramingCatalog_M50.asset`
- Catastrophe: `The Hollow Star` has eaten worlds and spat out mixed timelines.
- Scope: seeded three-world itinerary, cryptic world text, biome metadata, entry toasts, and hub branch echo labels.
- Non-goals: no biome filtering, encounter changes, rewards, saves, materials, difficulty, or branch mechanics.

## Active World Identities

- `broken_meridian` - The Broken Meridian: A mixed threshold where timelines scrape against the same door.
- `before_teeth` - Before Teeth: Prehistoric hunger preserved before language learned mercy.
- `sunken_cartouche` - The Sunken Cartouche: A drowned royal afterlife, rewritten by impossible tides.
- `black_keep` - The Black Keep: Medieval terror built from siege smoke and failed prayers.
- `rust_choir` - The Rust Choir: A fallen future still singing through broken machines.
- `choir_below` - The Choir Below: Hell and heaven collided, and both kept singing.
- `last_hour` - The Last Hour: The end of times, looped until even endings are tired.
- `blind_deep` - The Blind Deep: An abyss without horizon, hungry enough to become a god.

## Sample Seed 15001 Itinerary

- World 1: The Choir Below
- World 2: The Sunken Cartouche
- World 3: Before Teeth

## Runtime Notes

- `RunWorldItineraryService` resolves three distinct world identities from the root run seed.
- `RunFramingHudController` shows compact framing plus a short world-entry toast on world changes.
- Hub branch portals use current-world branch echo names when an M50 catalog is wired.
