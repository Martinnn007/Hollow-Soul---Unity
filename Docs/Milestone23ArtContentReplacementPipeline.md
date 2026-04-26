# Milestone 23: Art/Content Replacement Pipeline

M23 adds the first visible art-replacement layer while keeping gameplay data authoritative. Room layouts, collision, traversal, combat, rewards, shops, saves, and branch generation still come from Hollow runtime models and controllers. ArtPass prefabs are presentation-only children attached to those runtime objects.

## What Changed

- `PresentationPrefabRole` defines stable visual roles for player, enemies, projectiles, room floor, rocks, door states, rewards, boss keys, shops, portals, and core VFX placeholders.
- `PresentationContentCatalog` now stores prefab bindings beside material, VFX, and audio cue references.
- `PresentationPrefabResolver` resolves ArtPass prefabs from the active catalog, or creates primitive fallbacks when a binding is missing.
- `RoomRuntimeRoot`, combat objects, projectiles, rewards, boss key pickups, shops, and portals attach visual-only children without replacing gameplay components.
- `MaterialPalette_ArtPass.asset` provides a dark toy-diorama palette while `MaterialPalette_Prototype.asset` remains the fallback baseline.
- ArtPass prefabs live under `Assets/_Hollow/Prefabs/ArtPass/`.
- Raw model drops should land under `Assets/_Hollow/Art/Models/...` and only become runtime-approved after being wrapped as visual prefabs.
- Placeholder VFX prefabs and generated placeholder SFX clips are wired through the existing cue definitions.
- Local Addressables use the `Hollow ArtPass Content` group and `hollow.artpass.*` labels.

## Runtime Rule

Art never owns gameplay. A replacement prefab must not define collision, movement, damage, layout, traversal, spawn logic, save state, or required gameplay scripts. Runtime objects keep their own authoritative colliders/components, then receive an ArtPass visual child.

## Import Rules

- Runtime-approved visual prefabs must be under `Assets/_Hollow/Prefabs/ArtPass/`.
- Prefabs use `AP_` for role visuals and `VFX_` for VFX placeholders.
- Root objects include `PresentationVisualMarker`.
- Prefabs should have meter-aligned pivots, local origin placement, simple bounds, and low hierarchy counts.
- Prefabs must not include gameplay colliders or gameplay scripts.
- Missing optional replacement art is allowed only when the primitive fallback path remains valid.

## Validation

Run:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" -executeMethod Hollow.Editor.Validation.Milestone23Validator.Validate
```

The validator checks the ArtPass palette, prefab bindings, visual-only prefab constraints, placeholder cue wiring, Addressables group/labels, and required milestone files.
