# M94: Enemy Authoring Suite V1

M94 adds an editor-only Enemy Studio for fast iteration on Hollow enemies and bosses.

Open it from `Hollow > Enemy Authoring > Enemy Studio`.

## Workflow

1. Select an enemy or boss from the roster.
2. Edit the draft in the relevant tab.
3. Validate the draft.
4. Apply the draft to write the source ScriptableObject asset.

Edits are draft-first. Source assets are not changed until `Apply Root Draft To Asset` or `Apply Linked Draft To Asset` is pressed.

## Tabs

- `Roster`: choose current enemy or boss assets.
- `Stats & Senses`: HP, speed, radius, intelligence, disposition, senses, contact policy, lunge values, disturbance, alert sharing, and execution modifiers.
- `Attacks`: linked `EnemyAttackProfileDefinition` assets for damage, timing, damage type, force class, knockback, guard recoil, range, projectile values, and combos.
- `Actions`: linked `EnemyActionProfileDefinition` assets for action meaning, scoring, counterplay, awareness, dispositions, and tags.
- `Spacing`: linked M91 spacing profiles and action-specific spacing overrides.
- `Behavior Tree`: selector, sequence, weighted selector, condition, and action node editing.
- `Visuals`: presentation role overrides for body, weapon, offhand, projectile, and VFX roles.
- `Live Tuning`: Play Mode-only transient tuning for active non-boss enemies.
- `Validation & Apply`: validation, diff summaries, protected-asset registration, and reports.

## Protection

When an asset is applied through Enemy Studio, it is registered in `EnemyAuthoringProtectionRegistry`. Future generators should treat registered assets as manual designer edits and avoid overwriting them unless explicitly forced.

## Notes

V1 uses presentation prefab roles rather than direct arbitrary prefab references. That keeps art binding aligned with the current `PresentationContentCatalog` pipeline.
