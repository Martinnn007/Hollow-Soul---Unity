# M96 Enemy AI Brain Studio Report

- Tool: `Hollow > Enemy Authoring > Enemy AI Brain Studio`
- Scope: editor-only enemy AI authoring and diagnostics
- Runtime behavior change: none
- Source of truth after apply: existing `EnemyDefinition`, `EnemySpacingProfileDefinition`, behavior tree, action, and attack assets

## Implemented

- Added `EnemyAiBrainTemplateDefinition` for global brain role templates.
- Added brain template generator with ten role templates.
- Added AI brain analysis utilities for role suggestions, validation, template application, and deterministic action score previews.
- Added dockable Enemy AI Brain Studio window with overview, individual editing, templates, score lab, threat/LOD guidance, live trace, and validation tabs.
- Added Enemy Studio shortcut button into the new AI Brain Studio.
- Added M96 EditMode coverage for templates, role analysis, draft-only template application, score preview ordering, validation notes, and docs/report existence.

## Notes

The tool is intentionally draft-first. It should be used for high-level role coherence and quick tuning before opening Behaviour Tree Studio for graph edits or Enemy Studio for lower-level asset lists.
