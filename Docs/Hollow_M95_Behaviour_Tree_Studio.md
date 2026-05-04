# M95: Behaviour Tree Studio V1

## Summary
M95 adds a dedicated Unity Editor tool for authoring Hollow enemy behavior trees without changing runtime AI. The Studio opens from `Hollow > Enemy Authoring > Behaviour Tree Studio` or from Enemy Studio's behavior-tree tab.

## What The Tool Provides
- Graph-first editing for existing `EnemyBehaviorTreeDefinition` assets, with pan, zoom, minimap, auto-layout, copy/paste, duplicate, delete, and undo-aware graph operations.
- Draft-first workflow using the M94 protection/apply path.
- Global template assets for chaser, prey, stationary ranged, weapon-user, creature, caster/ranged, and boss-metadata roles.
- Node summaries, action/attack badges, search, validation, readability notes, tree diff, and EN/PL labels.
- Editor-only sandbox preview for stepping synthetic awareness, distance, budget, and disposition states.
- Play Mode live trace for selected runtime enemies using the M93 AI blackboard.

## Workflow
1. Open `Hollow > Enemy Authoring > Behaviour Tree Studio`.
2. Select a runtime tree or template in the left browser.
3. Use the graph toolbar to add nodes, auto-layout, zoom, and frame the canvas.
4. Select a node to edit serialized fields, set root, duplicate, delete, or connect parent-to-child.
5. Use `Templates` to apply a role template into the current tree draft. The source asset is not overwritten until `Apply Draft`.
6. Run `Validation` before applying. Errors block safe apply; warnings/readability notes help design review.
7. In Play Mode, select an enemy instance and use `Live Trace` to inspect LOD, command, action, path status, and top scores.

## Guarantees
- Runtime behavior format remains unchanged.
- Boss trees remain metadata-only.
- Template application edits the draft only.
- Applying a draft writes a report under `output/reports/enemy_authoring/` and protects the asset from future generator overwrite.

## Follow-Up
- M96 can add graph-node position persistence and richer room-sandbox playback.
- A later boss milestone can enable boss runtime trees after boss-specific QA.
