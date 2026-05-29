# M131: Room Type Expansion Lock + Wave Room Prototype

M131 is a runtime and lock-artifact milestone. It locks the beta room-type set and adds the first optional challenge endpoint: a Wave Room.

## Decisions

- Beta room whitelist: Safe Start, Combat, Wave Room, Treasure, Boss, Shop/Hub, Secret, and Corrupted Chest.
- Normal world-loop branches add one optional `Wave Room` leaf.
- Wave Rooms are not required for boss access.
- Wave Rooms are never eligible for boss-key placement.
- Wave Rooms are terminal leaves and do not replace boss, treasure, secret, or corrupted endpoints.
- Wave Rooms inherit the active branch biome; M131 does not add a new wave biome pack.
- Entering a Wave Room commits the player to the fight; doors remain locked until all waves are clear.
- Wave Rooms run three waves with a default 2/3/4 enemy shape.
- Clearing the third wave spawns a Golden Chest using existing golden chest presentation and contents.
- Combat HUD status may show `Wave 1/3`, `Wave 2/3`, and `Wave 3/3`.
- The minimap marks Wave Rooms with a readable Wave Room marker.

## Deferrals

- No save schema changes.
- No economy schema changes.
- No new chest kind.
- No biomass, Black Orb, Soul Chest, or mimic room runtime work.
- Deferred room types: survival, trap traversal, lever rooms, defend-object rooms, life/death rooms, mimic rooms, Soul Chest rooms, biomass rooms, and Black Orb rooms.

## Acceptance

- Every normal world-loop branch has exactly one optional Wave Room leaf.
- Boss keys never spawn in Wave, Secret, Corrupted Chest, Treasure, or Boss rooms.
- The Wave Room endpoint imports and validates as a 1x1 room with enemy anchors and a golden chest marker.
- Wave combat splits deterministic encounter contents into three runtime-only waves.
- M131 generated markdown and JSON reports pass with lock id `m131_room_type_expansion_lock_v1`.
