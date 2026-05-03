# DesignerRooms Manual Edit Handoff

Ten editable Unity scene copies were generated under `Assets/_Hollow/Scenes/DesignerRooms`.

These scenes are handoff copies for manual layout work. They do not replace the source `.hollowruntime.json` room templates. Each scene contains a `DesignerRoomRoot.*` object with child folders for floor regions, door ports, spawn points, obstacles, hazards, interactive objects, and hole tiles.

Every editable marker uses `Hollow.Rooms.DesignerRoomSceneMarker`, which stores the source room id, source template path, marker id, runtime kind, and notes. The component also draws simple colored gizmos in the Scene view:

- Green: player safe start.
- Red: enemy spawn.
- Blue: door port.
- Yellow: item/reward spawn.
- Gray: obstacle.
- Orange: hazard.
- Brown: interactive object.
- Dark: hole tile.

Generated scenes:

| Scene | Source Template |
| --- | --- |
| `DesignerRoom_01_Crossroads_Single` | `Assets/_Hollow/Data/Rooms/DesignerApproved/approved_crossroads_single_1x1.hollowruntime.json` |
| `DesignerRoom_02_Cover_Arena_Single` | `Assets/_Hollow/Data/Rooms/DesignerApproved/approved_cover_arena_single_1x1.hollowruntime.json` |
| `DesignerRoom_03_Lane_Wide` | `Assets/_Hollow/Data/Rooms/DesignerApproved/approved_lane_wide_2x1.hollowruntime.json` |
| `DesignerRoom_04_Pressure_Lane_Wide` | `Assets/_Hollow/Data/Rooms/DesignerApproved/approved_pressure_lane_wide_2x1.hollowruntime.json` |
| `DesignerRoom_05_Quadrant_Block` | `Assets/_Hollow/Data/Rooms/DesignerApproved/approved_quadrant_block_2x2.hollowruntime.json` |
| `DesignerRoom_06_Ambush_L` | `Assets/_Hollow/Data/Rooms/DesignerApproved/approved_ambush_l_3cell.hollowruntime.json` |
| `DesignerRoom_07_Spider_Brood_Den` | `Assets/_Hollow/Data/Rooms/DesignerApproved/M77/m77_spider_brood_den_wide.hollowruntime.json` |
| `DesignerRoom_08_Rat_Warren` | `Assets/_Hollow/Data/Rooms/DesignerApproved/M77/m77_rat_warren_single.hollowruntime.json` |
| `DesignerRoom_09_Mixed_Weapon_Battlefield` | `Assets/_Hollow/Data/Rooms/DesignerApproved/M84/m84_mixed_weapon_battlefield.hollowruntime.json` |
| `DesignerRoom_10_Archer_Gallery` | `Assets/_Hollow/Data/Rooms/DesignerApproved/M86/m86_archer_gallery_room.hollowruntime.json` |

Manual edit guidance:

- Move `SafeStart.*` to change the player entry point.
- Move, duplicate, or delete `EnemySpawn.*` objects to tune encounters.
- Move `DoorPort.*` objects to reposition door anchors.
- Resize `FloorRegion.*`, `Obstacle.*`, `Hazard.*`, and `Interactive.*` objects with transform scale.
- Keep object names and `DesignerRoomSceneMarker.runtimeKind` values intact when possible, since those values map back to runtime spawn and room concepts.
