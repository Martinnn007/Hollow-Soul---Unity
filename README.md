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
- `Docs/Milestone24PlatformBuildDeviceQA.md` describes the platform build/device QA gate, Windows development build output, Vision Pro readiness checks, runtime smoke probes, and handoff reports.
- `Docs/Milestone25VerticalSliceContentLock.md` describes the locked vertical-slice seed, ArtPass/content checks, platform checklist, and PDF handoff.
- `Docs/Milestone27WeaponModeLightHeavyAttacks.md` describes catalog-backed melee/ranged weapon modes, light/heavy attacks, stamina/cooldown checks, and rare weapon rewards.
- `Docs/Milestone28ItemsCardsCoinsShopRewards.md` describes run coins, usable active items/cards, mixed-currency shops, and the first item/card reward pool.
- `Docs/Milestone29CharactersPassiveIdentitySkills.md` describes the first selectable characters, run-start passive identity skills, and character picker launch flow.
- `Docs/Milestone30SynergyTagsStarterBuildVariety.md` describes internal build tags, armor equipment, Skeletal/Dragon set pieces, and run-only set synergy bonuses.
- `Docs/Milestone31ValidationDebtRecovery.md` describes the validator rebaseline after Isaac-style macro connectivity, successor reward pools, and M30 build-system additions.
- `Docs/Milestone32FullQaGateRebaseline.md` describes the QA gate rebaseline, in-process EditMode test execution, and editor-side platform scene smoke evidence.
- `Docs/Milestone33CombatFeelPhysicsCameraPolish.md` describes movement/projectile substeps, obstacle sliding, and traversal-safe gameplay camera follow polish.
- `Docs/Milestone34ShieldDefenseArmorBehavior.md` describes passive defense mitigation, shield guard stamina costs, contact pushback, and HUD defense status.
- `Docs/Milestone35ChallengeModeV1.md` describes fixed-seed transient challenge runs, curated stat/currency rules, and challenge launch safety.
- `Docs/Milestone36RoomEncounterContentExpansion.md` describes the first approved room-content pack and expanded seeded encounter catalog.
- `Docs/Milestone37EnemyBossBehaviorReadability.md` describes enemy attack windups, boss burst telegraphs, and non-blocking combat readability visuals.
- `Docs/Milestone38ArtPassRafalPipeline.md` describes Rafal's ArtPass intake folders, target catalog, visual-only prefab rules, and production handoff PDF.
- `Docs/Milestone39StoryWorldIdentityRunFraming.md` describes the first world identity catalog and compact run-framing HUD.
- `Docs/Milestone40VerticalSliceRelockExternalHandoff.md` describes the external handoff readiness gate that checks latest QA, vertical-slice lock, and world-framing evidence.
- `Docs/Milestone41CurrentMilestoneAudit.md` describes the current M31-M40 audit layer that complements the historical M0-M23 dependency audit.
- `Docs/Milestone42PlayerBuildUxPickupClarity.md` describes the player-build HUD, pickup reveal cards, rarity colors, and saved swap-back replacement pickups.
- `Docs/Milestone43CombatFeelDamageFeedback.md` describes player i-frames, collision-safe knockback, subtle enemy windups, M43 feedback cues, and visual-only corpse ghosts.
- `Docs/Milestone44ShieldArmorBehaviorV2.md` describes universal aim-facing guard, perfect parry timing, threat kinds, shield visuals, and armor-as-stat-equipment behavior.
- `Docs/Milestone45RoomHazardsInteractivePhysicsV1.md` describes authored spikes, pit-aware movement, destructible barrels, explosive barrel chains, and room-local hazard persistence.
- `Docs/Milestone46EncounterDirectorDifficultyCurve.md` describes directed world-length branches, weighted encounter pressure, deterministic encounter metadata, and the combat HUD director debug line.
- `Docs/Milestone47ChallengeModeV2CuratedSeeds.md` describes six curated fixed-seed full-run challenges, transient challenge safety, V2 loadouts/rules, and profile challenge result records.
- `Docs/Milestone48ContentExpansionLockV1.md` describes the first larger locked room/encounter content pool, M48 successor catalog wiring, curated draft mirroring, and team handoff report outputs.
- `Docs/Milestone49ArtPassProductionIntegrationII.md` describes the direct ArtPass production-replacement workflow, visual-only safety gate, role status report, and Rafal/Martin checklist PDF.
- `Docs/Milestone50StoryWorldIdentityRunFramingV2.md` describes the Hollow Star world identity catalog, seeded three-world itinerary, entry toast, and hub branch echo labels.
- `Docs/Milestone51PreBetaRewardHealthRebalance.md` describes sparse ordinary-room rewards, treasure/boss/shop item gating, and the lower pre-beta character health baseline.
- `Docs/Milestone52ChestsCoinDrops.md` describes interactable normal/golden chests, visible copper/silver/gold coins, Room Designer chest markers, and M52 persistence rules.
- `Docs/Milestone53BossRosterFramework.md` describes the 10-boss roster, seeded boss selection, boss arenas, boss HUD, and fixed-HP boss framework.
- `Docs/Milestone54ItemCatalogueProjectilePassives.md` describes the item catalogue PDF, projectile passive item pool, strongest-wins multi-shot logic, Power-up red shots, and Fire-rate Up stacks.
- `Docs/Milestone55DeveloperInspectionBranch.md` describes the Developer Lab branch, frozen inspection entities, and bottom-right debug spawn menu.
- `Docs/Milestone56ArtPassWrapperCalibrationAssetIntakeQA.md` describes the ArtPass wrapper calibration standard, bounds/material/catalog checks, and asset acceptance reporting.
- `Docs/Milestone57DeveloperLabCoverageLock.md` describes the Developer Lab coverage lock for every visible ArtPass/runtime role.
- `Docs/Milestone58BetaRewardEconomyChestBalance.md` describes the beta reward economy and chest pacing expectations after the sparse M51/M52 reward pass.
- `Docs/Milestone59CombatInputControllerReliability.md` describes the controller/keyboard reliability gate and legacy input cleanup target.
- `Docs/Milestone60BossPolishBossLabV2.md` describes the beta boss polish subset and Boss Lab V2 inspection goals.
- `Docs/Milestone61RoomPoolQualityApprovalWorkflow.md` describes the Draft -> Reviewed -> Approved Runtime room promotion workflow.
- `Docs/Milestone62RunReadabilityBetaHudCleanup.md` describes the beta HUD/readability pass and debug-text hiding rules.
- `Docs/Milestone63BetaContentSelectionLock.md` describes the beta content whitelist and catalogue/report outputs.
- `Docs/Milestone64VerticalSliceBetaLockGate.md` describes the beta lock validation gate and report model.
- `Docs/Milestone65BetaHandoffBuildQaPack.md` describes the QA handoff pack, tester routes, and build/readiness checklist.
- `Docs/CuratedRoomDesignerRuntimeRooms.md` describes the curated runtime-room Room Designer library and safe edit-copy workflow.

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
