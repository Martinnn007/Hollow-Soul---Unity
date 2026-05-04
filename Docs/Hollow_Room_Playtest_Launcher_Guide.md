# Room Playtest Launcher

## What It Does

`Play This Room` launches the active Designer Room scene directly into `Game_Windows` as a temporary playtest.

It uses the current scene markers to build runtime room data, then auto-spawns:

- player at `Safe Start`
- selected loadout id
- enemies
- doors
- hazards, rocks, holes, and interactives
- normal game lighting/camera/runtime systems

The playtest is transient and does not save run progress.

## How To Use

1. Open a scene in `Assets/_Hollow/Scenes/DesignerRooms/`.
2. Open `Hollow > Designer Rooms > Room Authoring`.
3. Go to `Visual Preview`.
4. Pick a loadout.
5. Click `Play This Room`.
6. Stop Play Mode to return to the Designer Room scene.

You can also use the menu command:

`Hollow > Designer Rooms > Play This Room`

## Good Checks

- Player starts in a safe, readable position.
- Doors are aligned and usable.
- Enemies wake, path, and attack as expected.
- Rocks, holes, and hazards affect movement correctly.
- Room clear and rewards still feel fair.

## Notes

If the room has validation errors, the launcher blocks the playtest and reports the problem. Unsaved scene edits must be saved before Unity switches to the runtime scene.
