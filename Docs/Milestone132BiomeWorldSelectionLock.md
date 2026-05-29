# M132: Biome + World Selection Lock + Beta Art Pack

## Summary
- M132 locks the beta world order as `Before Teeth` -> `The Sunken Cartouche` -> `The Rust Choir`.
- `The Hollow Threshold` remains the prologue/fallback framing and is not one of the three beta worlds.
- Each selected world receives a readable 1024 PBR art pack with six source texture families: floor, wall, rock, door, organic/decor, and accent/trim.
- Chests remain global gameplay affordances; biome identity comes from rooms, doors, rocks, decor, and branch-portal trim.

## World Order
- World 1: `before_teeth` - Before Teeth.
- World 2: `sunken_cartouche` - The Sunken Cartouche.
- World 3: `rust_choir` - The Rust Choir.

## PBR Source Maps
- Every family has separate `BaseColor`, `Normal`, and packed `Mask` PNGs at 1024x1024.
- Mask channels are `R = metallic`, `G = occlusion`, `B = reserved`, `A = smoothness`.
- Base maps import as sRGB; normal maps import as normal maps; masks import as linear data.
- Materials use URP/Lit and wire `_BaseMap`, `_BumpMap`, `_MetallicGlossMap`, and `_OcclusionMap`.

## Room And Runtime Policy
- M132 creates 1x1, 2x1, 1x2, 2x2, and L-shape biome variants from existing macro fixture shapes only.
- Normal branch rooms use the active world biome when that biome has complete room-template coverage.
- Corrupted chest rooms remain on `corrupted_ashen_shrine`.
- Wave Rooms inherit the active branch biome.
- Normal, Golden, and Corrupted Chest silhouettes/material identity stay global for readability.

## Enemy Visual Deferral
- Enemy color and silhouette work is documented only in M132.
- Future enemy art passes should preserve clear silhouettes: bone/fern reads for Before Teeth, lapis/gold reads for The Sunken Cartouche, and rust/neon reads for The Rust Choir.
- M132 does not add runtime enemy material swapping.

## Non-Goals
- No save schema, reward schema, economy schema, chest-kind, room-role, or branch-generation rule changes.
- No new room layouts beyond macro fixture variants.
