# M50 Story, World Identity, And Run Framing V2

- Generated: manual implementation report
- Catalog: `Assets/_Hollow/Data/Worlds/M50/RunFramingCatalog_M50.asset`
- Catastrophe: `The Hollow Star` has eaten worlds and spat out mixed timelines.
- Scope: seeded three-world itinerary, cryptic world text, biome metadata, entry toasts, and hub branch echo labels.
- Non-goals: no biome filtering, encounter changes, rewards, saves, materials, difficulty, or branch mechanics.

## Active World Identities

- `broken_meridian` - The Broken Meridian
- `before_teeth` - Before Teeth
- `sunken_cartouche` - The Sunken Cartouche
- `black_keep` - The Black Keep
- `rust_choir` - The Rust Choir
- `choir_below` - The Choir Below
- `last_hour` - The Last Hour
- `blind_deep` - The Blind Deep

## Runtime Notes

- `RunWorldItineraryService` resolves three distinct world identities from the root run seed.
- `RunFramingHudController` shows compact framing plus a short world-entry toast on world changes.
- Hub branch portals use current-world branch echo names when an M50 catalog is wired.
