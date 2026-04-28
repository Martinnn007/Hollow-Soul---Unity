# Milestone 38: ArtPass Integration Sprint With Rafal Pipeline

## Summary

M38 turns the current generated ArtPass placeholders into a practical art-production handoff. The runtime still uses gameplay-authored rooms, colliders, enemies, rewards, shops, and projectiles as the source of truth. Art assets remain visual-only prefab children resolved through `PresentationContentCatalog`.

## What Changed

- Added an ArtPass target catalog at `Assets/_Hollow/Data/ArtPass/M38/ArtPassTargetCatalog_M38.asset`.
- Added per-target handoff assets under `Assets/_Hollow/Data/ArtPass/M38/Targets/`.
- Added Rafal intake folders under `Assets/_Hollow/Art/Intake/Rafal/M38/`.
- Added new presentation prefab roles for shop cards, generic weapons, armor, active item pickups, and consumable card pickups.
- Generated ArtPass placeholder prefabs for the new roles through the existing M23 catalog pipeline.
- Integrated `HubShopCard` and `NextBranchPortal` with ArtPass visual child attachment while preserving gameplay ownership.
- Added M38 validation and EditMode coverage.

## Rafal Handoff Rules

- Raw/source files go into `Assets/_Hollow/Art/Intake/Rafal/M38/<group>/<target>/`.
- Runtime-ready wrappers go under `Assets/_Hollow/Prefabs/ArtPass/`.
- Prefab names should stay `AP_<PresentationPrefabRole>` or `VFX_<PresentationPrefabRole>`.
- Visual prefabs must not contain gameplay colliders.
- Visual prefabs must not contain gameplay scripts except `PresentationVisualMarker`.
- Pivots should be centered; floor and door bottoms should sit at `y = 0`.
- Use low-poly, low-hierarchy, Vision-safe geometry.

## Critical Targets

- Player body.
- Room floor tile and 1m rock obstacle.
- Active, locked, cleared, and unavailable doors.
- Normal enemy and Stone Warden boss.
- Player and enemy projectiles.
- Reward pickup and boss key.
- Hub shop stand, shop card, branch portal, and next-world portal.

## Secondary Targets

- Flying, fast, heavy, charger, turret, and splitter enemies.
- Secret debug door.
- Generic melee weapon, ranged weapon, armor, active item, and consumable card pickup wrappers.
- VFX placeholders for projectile fire, hits, deaths, rewards, doors, room clear, and portals.

## Validation

Run:

```bash
Hollow/Generation/Generate Milestone 38 Assets
Hollow/Validation/Run Milestone 38 Validation
```

The validator checks that the target catalog exists, every `PresentationPrefabRole` has a target, critical runtime targets are marked as vertical-slice required, ArtPass prefabs resolve through the presentation catalog, and existing ArtPass content rules still pass.

## PDF Handoff

The working handoff PDF is generated separately at:

`output/pdf/Hollow_M38_ArtPass_Rafal_Handoff.pdf`

It contains target goals, folder rules, acceptance checks, and a prioritized production list.
