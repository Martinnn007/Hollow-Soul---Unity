# Curated Runtime Rooms in Room Designer

The Room Designer includes a repo-tracked curated library generated from the runtime room pool used by normal branch generation.

## Source Rooms

- M13 macro fixtures from `Assets/_Hollow/Data/Rooms/MacroFixtures/`.
- M36 approved designer rooms from `Assets/_Hollow/Data/Rooms/DesignerApproved/`.

The legacy `combat_single_sample` room is intentionally excluded from this curated editing set.

## Generated Drafts

Curated Room Designer drafts are written to:

`Assets/_Hollow/Data/Rooms/DesignerDrafts/CuratedRuntime/`

The catalog asset is:

`Assets/_Hollow/Data/Rooms/DesignerDrafts/CuratedRoomDesignerDraftCatalog.asset`

Use `Hollow/Generation/Generate Curated Room Designer Drafts` after changing macro fixtures or approved runtime room JSON.

## Editing Workflow

Curated rows appear in the Room Designer library under `Curated Runtime Rooms`. Opening one creates an editable copy in the selected profile slot. The curated source JSON is not modified.

Edited copies are normal Room Designer drafts. Exporting them writes a bundle with the designer project JSON, HollowRuntime V2 JSON, USDA companion, and validation report. Promotion back into `DesignerApproved` remains a manual review step.

## Validation

Run `Hollow/Validation/Run Curated Room Designer Draft Validation` to verify that:

- Every current generatable runtime room has a curated draft.
- Every curated draft passes Room Designer validation.
- Every curated draft recompiles and reimports through `HollowRuntimeV2Importer`.
- The Room Designer scene is wired to the curated catalog.
