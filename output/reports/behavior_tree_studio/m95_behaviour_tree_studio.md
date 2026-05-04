# M95 Behaviour Tree Studio V1 Report

- Added runtime-safe `EnemyBehaviorTreeTemplateDefinition` assets and template roles.
- Added `Behaviour Tree Studio` under `Hollow > Enemy Authoring`.
- Added graph canvas authoring, node inspector, template application, validation, synthetic sandbox, diff, and Play Mode live trace.
- Added M94 draft/apply support for behavior-tree templates.
- Added Enemy Studio quick-open integration.
- Added docs at `Docs/Hollow_M95_Behaviour_Tree_Studio.md`.

## Validation Contract
- Missing roots, duplicate ids, empty composites, cycles, unreachable nodes, invalid committed action ids, weighted-selector problems, and boss metadata warnings are surfaced in the Studio.
- Readability notes flag missing fallback hold/face branches, over-repeated committed actions, and missing endangered branches.

## Runtime Impact
None. M95 is editor tooling only; existing behavior trees, M93 scoring, M80 active windows, and boss runtime behavior remain unchanged.
