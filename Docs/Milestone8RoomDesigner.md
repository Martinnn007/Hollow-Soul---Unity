# Milestone 8: Room Designer Mode

Milestone 8 adds the first in-game Room Designer for Unity. It is a separate authoring route from the selected-profile main menu and does not mutate run saves, active snapshots, banked souls, or meta progression.

## Designer Flow

- Select or create a profile, then choose `Room Designer` from the slot menu.
- The designer loads per-slot drafts from local designer storage and creates a default 13x7 draft if none exist.
- Drafts are stored separately from profile/run persistence.
- The visible scene is a generated 1m grid with semantic blocks, door anchors, spawn markers, cursor highlight, labels, and a small HUD.

## Controls

- `WASD` / arrow keys: move cursor on X/Z.
- `Q` / `E`: cycle tool.
- `Z` / `X`: change active layer.
- `Space` / `Enter`: place current tool.
- `Backspace` / `Delete`: erase current cell/entity.
- `F`: eyedropper.
- `Tab`: toggle semantic labels.
- `P`: launch transient playtest.
- `J`: export designer project JSON and runtime JSON.
- `U`: export USDA companion.
- `Escape`: return to main menu.

Gamepad support uses the same controller intent: stick/D-pad cursor movement, shoulders for tools, triggers for layer, south button place, east button erase, west button eyedropper, north button labels, and menu/start for back.

## Runtime Truth

The editable draft compiles into `hollowRuntime.schemaVersion = 2`. The compiler output is used for preview validation, JSON export, USDA companion export, and transient playtest. Gameplay import never infers semantics from preview meshes.

## Export

Exports are written to `Application.persistentDataPath/room_designer_exports/{projectId}`.

- `designerProject.json`
- `runtime.hollowruntime.json`
- `scene.usda`

The USDA file is a simple semantic graybox companion for inspection and handoff. The runtime JSON remains the canonical gameplay source.

## Playtest Safety

Playtest uses `RuntimeSessionMode.TransientRoomDesignerPlaytest`. The active run persistence guard blocks save, checkpoint, profile summary, active-run clear, and meta banking paths for this mode.

## Validation

Run `Hollow/Generation/Generate Milestone 8 Assets`, then `Hollow/Validation/Run Milestone 8 Validation`. The full EditMode suite covers menu routing, draft storage, compiler output, input semantics, export, transient handoff, and previous milestone regressions.
