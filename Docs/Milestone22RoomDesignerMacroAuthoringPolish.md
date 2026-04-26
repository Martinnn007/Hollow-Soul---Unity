# Milestone 22: Room Designer Macro Authoring Polish

M22 makes the Room Designer usable as a branch-ready macro-room authoring tool. Drafts are still stored separately from run/profile persistence, but the authoring flow now treats exported rooms as candidates for manual approval into the procedural room pool.

## Authoring Flow

- New drafts are created from a fixed footprint preset: `Single1x1`, `Wide2x1`, `Tall1x2`, `Block2x2`, or `L3Cell`.
- Footprints are immutable after draft creation. To change room shape, create a new draft and copy/rebuild the content intentionally.
- The designer HUD shows footprint dimensions, active tool, cursor/layer, selected door port, enabled-port count, validation status, controls, and latest action.
- The preview shows 1m grid lines, occupied 13x7 chunk outlines, internal seam guides, exposed port anchors, port IDs, host cells, lane indices, and active/secret/inactive states.

## Tools

M22 keeps keyboard/controller-first editing and expands semantic tools for encounter-ready rooms:

- Ground, hole, rock, erase, and eyedropper.
- Safe start marker placement.
- Reward spawn marker placement.
- Enemy spawn variants: normal, flying, fast, heavy, charger, turret, and splitter.
- Door port states: available, active door, secret door, and inactive.

Inactive ports remain visible in the editor but are omitted from exported `hollowRuntime.doorPorts`.

## Validation

Playtest and export are blocked until branch-ready validation passes. Draft save remains allowed for incomplete rooms.

Blocking errors:

- Unsupported footprint preset.
- No enabled door ports.
- Missing or duplicated safe start.
- No enemy spawn markers.
- Marker IDs missing or duplicated.
- Safe start, enemy spawns, or reward spawns placed outside the footprint, on holes, off ground, or on blocking rocks.
- Runtime JSON fails HollowRuntime V2 import.

Warnings:

- Low enemy-anchor density for macro rooms.
- No reward marker.
- All enabled ports on one side.
- High hole coverage.
- Legacy generic enemy markers, which export as `spawnEnemyNormal`.

## Export

Validated export writes a complete bundle:

- `designerProject.json`
- `runtime.hollowruntime.json`
- `scene.usda`
- `validation-report.json`

The runtime JSON remains the gameplay source of truth. The USDA companion is still a semantic graybox for inspection and handoff.

## Validation Command

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "<repo>" -executeMethod Hollow.Editor.Generation.Milestone22AssetGenerator.Generate -quit
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "<repo>" -executeMethod Hollow.Editor.Validation.Milestone22Validator.Validate -quit
```
