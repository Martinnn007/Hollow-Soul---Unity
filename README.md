# Hollow Soul - Unity

Unity project for Hollow Soul.

## Current Foundation

Milestone 0 has started under `Assets/_Hollow`.

- `Assets/_Hollow/Scripts` contains the initial assembly split.
- `Assets/_Hollow/Data`, `Art`, `Audio`, `Prefabs`, `Scenes`, `Settings`, `Shaders`, `Tools`, and `Tests` are the canonical Hollow-owned roots.
- `Docs/Milestone0Foundation.md` describes the current foundation and validation command.
- `Docs/Milestone1MenuProfilesAndRouting.md` describes the first menu/profile/platform routing layer.
- `Docs/Milestone2SharedRuntimeShell.md` describes the shared game-world shell, placeholder room, player spawn, and platform presentation scale.
- `Docs/Milestone3HollowRuntimeV2Import.md` describes the imported static sample room and HollowRuntime V2 importer.
- `Docs/Milestone4PlayableCombatLoop.md` describes the first playable movement, shooting, enemy, room-clear, and HUD loop.
- `Docs/Milestone5EnemyArchetypesAndDiagnostics.md` describes data-driven enemy archetypes, difficulty tuning, readability, diagnostics, and boss-shell groundwork.
- `Docs/Milestone6BranchTraversalRewards.md` describes the deterministic five-room branch, door traversal, minimap, runtime rewards, and hub-return portal.
- `Docs/Milestone7RunEconomyPersistence.md` describes run-local rewards, active-run save/load, profile meta banking, and Continue/New Run flow.
- `Docs/Milestone8RoomDesigner.md` describes the first in-game room designer, V2 JSON/USDA export, and transient playtest flow.
- `Docs/Milestone9ContentPipeline.md` describes prototype material roles, VFX/audio cue definitions, Addressables labels, and content import validation.
- `Docs/Milestone10PlatformPolish.md` describes Windows, Vision Pro bounded tabletop, and Vision Pro immersive presentation polish profiles.
- `Docs/Milestone11PrototypeLock.md` describes the QA checklist, performance budgets, save/load coverage, content validation, and build handoff gate.
- `Docs/Milestone12BuildAutomation.md` describes the full prototype audit runner, output conventions, build manifests, and Windows development build entrypoint.
- `Docs/Milestone13MacroRooms.md` describes macro-room footprints, exposed door-port validation, room designer presets, and additive branch occupancy foundations.
- `Docs/Milestone14MacroBranchGeneration.md` describes the seeded macro-room branch, exact port traversal, M14 catalog wiring, and legacy save compatibility.
- `Docs/Milestone15SeededProceduralBranches.md` describes the first seeded eight-room procedural macro branch, procedural rewards, and the Stone Warden boss room.
- `Docs/Milestone16ApprovedDesignerRoomPool.md` describes the approved Room Designer JSON intake folder and additive branch room pool.
- `Docs/Milestone17FeatureBranchTreasureRooms.md` describes the first special feature-room role, treasure-room auto-clear behavior, and M17 branch identity.
- `Docs/Milestone18SeededRandomRewards.md` describes seeded automatic reward pools, role-specific reward rolls, and reward effect persistence.
- `Docs/Milestone19EnemyEncounterContent.md` describes seeded encounter tables and the first distinct enemy behavior content pass.
- `Docs/Milestone20BranchFeaturesShopsSecretsKeys.md` describes boss-key locks, visible debug secrets, the inter-branch hub, hub shops, and seeded next-branch portals.
- `Docs/Milestone21ShopChoiceUi.md` describes visible inter-branch hub shop cards and card-specific purchasable rewards.
- `Docs/Milestone22RoomDesignerMacroAuthoringPolish.md` describes branch-ready macro-room authoring, validation-gated playtest/export, and validated export bundles.
- `Docs/Milestone23ArtContentReplacementPipeline.md` describes the catalog-driven ArtPass prefab replacement layer, visual-only import rules, and Addressables packaging.

## Required Setup

- Unity Editor `6000.4.1f1`
- Git
- Git LFS

Install and enable Git LFS before cloning:

```bash
git lfs install
```

Clone the project:

```bash
git clone https://github.com/Martinnn007/Hollow-Soul---Unity.git
cd "Hollow-Soul---Unity"
git lfs pull
```

Open the cloned folder in Unity Hub using Unity `6000.4.1f1`.

## Collaboration Rules

- Commit `Assets/`, `Packages/`, `ProjectSettings/`, `.gitignore`, `.gitattributes`, and `README.md`.
- Do not commit generated folders such as `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, or `UserSettings/`.
- Keep `.meta` files committed. Unity uses them to preserve asset references.
- Pull before starting work and push when a task is complete.
- Prefer separate branches for feature work:

```bash
git checkout main
git pull
git checkout -b feature/short-description
```

- Avoid editing the same scene or prefab at the same time unless you coordinate first. Unity text serialization is enabled, but scene and prefab merges can still be awkward.

## Large Files

This repo uses Git LFS for large binary assets such as textures, models, audio, video, and fonts. If a large asset appears as a tiny pointer file after cloning, run:

```bash
git lfs pull
```
