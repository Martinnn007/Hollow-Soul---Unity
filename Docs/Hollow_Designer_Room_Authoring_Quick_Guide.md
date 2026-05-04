# Hollow Designer Room Authoring - Quick Guide

For Martin, Rafal, and Pawel. This is the short workflow for editing `Assets/_Hollow/Scenes/DesignerRooms` scenes directly in Unity without entering Play Mode.

## 1. Open And Dock The Tool

1. Open a scene from `Assets/_Hollow/Scenes/DesignerRooms`.
2. Use `Hollow > Designer Rooms > Room Authoring`.
3. Drag the `Room Authoring` window by its title bar.
4. Drop it onto the Unity layout where you want it docked:
   - beside `Inspector` for a narrow side panel,
   - beside `Scene` for a larger editing panel,
   - or keep it floating while placing markers.
5. Save the Unity layout if desired via `Window > Layouts > Save Layout`.

## 2. Navigate The Room

- Use the normal Unity Scene View controls.
- Hold right mouse and use WASD to fly around.
- Middle mouse pans.
- Scroll zooms.
- Click a marker in the Scene View or Hierarchy to edit it.
- Use the tool's `Top-Down Fit Active Room` button for a clean top-down authoring view.

## 3. Switch Language

- Use the `EN / PL` switch in the top-right of the `Room Authoring` window.
- `PL` translates the main tool UI and marker inspector labels.
- Runtime ids, export paths, and technical data stay unchanged for safe export.

## 4. Place Entities On The Grid

1. In `Room Authoring`, open the `Palette` tab.
2. Pick a `Marker Type`, for example `Enemy Spawn`, `Obstacle`, `Hazard`, `Door Port`, or `Item Spawn`.
3. Pick the `Runtime Kind`, for example `Rat`, `Skeleton Spear`, `Spike`, or `Chest`.
4. Press `Arm Placement`.
5. Click in the Scene View.
6. The marker is created under the correct folder, such as `EnemySpawns`, `DoorPorts`, or `Obstacles`.
7. Press `Snap Selected` if needed.

Non-door markers snap to the nearest 1m grid point. Doors snap to valid room edges.

## 5. Edit A Marker

1. Select a marker in the Scene View or Hierarchy.
2. Open the `Selection` tab.
3. Edit:
   - `Marker Id`
   - `Marker Kind`
   - `Runtime Kind`
   - `Display Name Override`
   - `Show Scene Label`
   - door direction/state/lane when editing a door
4. For enemy spawns, the panel shows HP, intelligence, disposition, senses, spacing, and attack summary.

Use `Lock Layer` when you want to avoid accidental movement while still viewing the marker.

## 6. What The Designer Rooms Menu Does

- `Room Authoring`: opens the main editor window.
- `Snap Selected`: snaps selected markers to grid or valid door edge.
- `Snap All In Active Scene`: snaps all editable markers in the open scene.
- `Build Visual Preview`: creates the temporary runtime-style prefab/material preview.
- `Clear Visual Preview`: removes the temporary preview hierarchy.
- `Diff Active Scene Against Source`: prints what changed compared with the source approved JSON.
- `Refresh Active Scene From Source JSON`: rebuilds editable markers from the source template. Use carefully because it removes current editable marker changes.
- `Export Active DesignerRoom Scene`: validates and exports the open scene to a new manual runtime JSON draft.
- `Export All DesignerRooms`: validates and exports every scene in `Assets/_Hollow/Scenes/DesignerRooms`.

## 7. Preview Actual Models And Lighting

1. Open the `Preview` tab.
2. Leave `Preview Lighting` on if you want the room lit in Scene View.
3. Press `Visual Preview: OFF` to switch it on.
4. Use Scene View `Shaded` or `Lit` mode to see materials, fallback meshes, and any bound art-pass prefabs.
5. Press `Refresh Preview` after moving markers.
6. Press `Visual Preview: ON` or `Clear Preview` to remove it.

The preview appears as `RuntimePreview_DO_NOT_EXPORT`. It is temporary, has no authoring markers, and does not export to JSON.

## 8. Validate And Export

1. Open the `Validation` tab.
2. Click `Validate Active DesignerRoom Scene`.
3. Fix errors such as missing safe start, missing enemy spawn, duplicate ids, invalid doors, or off-grid markers.
4. Open the `Export` tab.
5. Click `Export Active DesignerRoom Scene`.

Exports go to:

`Assets/_Hollow/Data/Rooms/DesignerDrafts/ManualSceneExports/`

Approved source templates are not overwritten.

## 9. Practical Room Editing Tips

- Move enemy spawn markers first; then tune rocks, hazards, and items.
- Keep at least one safe start and one enemy spawn.
- Do not place safe start or enemy spawns on rocks, holes, or spikes.
- Use `Walkability` overlay to see blocked, hole, and hazard cells.
- Use `Enemy Range` overlay to preview selected enemy sight, hearing, and spacing.
- Use `Visual Preview` for art, scale, lighting, and readability checks.
- Use `Diff Against Source` before exporting if you want a quick review of what changed.

## 10. Handoff Rule

Scene edits are authoring drafts. After export, review the generated JSON before promoting it into the approved designer room pool.
