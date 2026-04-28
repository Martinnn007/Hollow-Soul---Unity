# Milestone 40: Vertical Slice Re-Lock + External Handoff

M40 turns the current validated prototype into a concise external handoff gate. It does not add gameplay systems. It verifies that the latest platform QA, vertical-slice lock, and M39 run-framing layer are ready enough to share for manual testing and art/content collaboration.

## What Changed

- Added `ExternalHandoffDefinition` at `Assets/_Hollow/Data/Handoff/M40/ExternalHandoff_M40.asset`.
- Added `ExternalHandoffReport` as a machine-readable handoff report format.
- Added M40 generation and validation commands.
- Added latest handoff reports:
  - `output/reports/latest_m40_external_handoff.json`
  - `output/reports/latest_m40_external_handoff.md`

## Handoff Policy

- `Passed` means the prototype has no validation, test, smoke, or content-lock failures.
- `PassedWithEnvironmentBlocks` is acceptable when the only issue is an explicitly documented local environment gap.
- The current accepted environment block is `windows-development-build`, because this Unity editor does not have Windows Build Support installed.
- Content/gameplay failures are never silently accepted.

## Unity Commands

```text
Hollow/Generation/Generate Milestone 40 Assets
Hollow/Validation/Run Milestone 40 Validation
```

## Manual Handoff Checklist

- Run the latest M24 manual platform checklist before external sharing.
- Install Windows Build Support if a `.exe` is required.
- Confirm the M39 run-framing HUD is readable and does not obscure combat HUD/minimap.
- Use the M38 ArtPass target catalog as Rafal's visual-production source of truth.
- Keep ArtPass visuals non-authoritative for gameplay.

## Non-Goals

- No new branch generation rules.
- No combat balance changes.
- No new save schema.
- No installer packaging.
- No physical Vision Pro signing/deployment requirement.
