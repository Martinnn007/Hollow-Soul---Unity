using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone135FullRunCombatReadinessLockReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public bool passed;
        public string result;
        public int totalChecks;
        public int passedChecks;
        public string[] evidencePaths;
        public string[] failures;
        public Milestone135FullRunCombatReadinessLockCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone135FullRunCombatReadinessLockCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone135FullRunCombatReadinessLockAssetGenerator
    {
        public const string LockId = M135CombatReadinessPolicy.LockId;
        public const string Title = "M135 Full-Run Combat Readiness Lock";
        public const string DocsPath = "Docs/Milestone135FullRunCombatReadinessLock.md";
        public const string M134ReportPath = "output/reports/m134_branch_pacing_reward_room_shape_pass.md";
        public const string ReportMarkdownPath = "output/reports/m135_full_run_combat_readiness_lock.md";
        public const string ReportJsonPath = "output/reports/m135_full_run_combat_readiness_lock.json";
        public const string QaChecklistPath = "output/reports/m135_full_run_combat_readiness_qa_checklist.md";
        public const string CombatReadinessPolicyPath = "Assets/_Hollow/Scripts/Hollow.Combat/M135CombatReadinessPolicy.cs";
        public const string PlayerWeaponControllerPath = "Assets/_Hollow/Scripts/Hollow.Combat/PlayerWeaponController.cs";
        public const string PlayerDamageFeedbackControllerPath = "Assets/_Hollow/Scripts/Hollow.Combat/PlayerDamageFeedbackController.cs";
        public const string BossCatalogDefinitionPath = "Assets/_Hollow/Scripts/Hollow.Combat/BossCatalogDefinition.cs";
        public const string BossDefinitionPath = "Assets/_Hollow/Scripts/Hollow.Combat/BossDefinition.cs";
        public const string BossRuntimeControllerPath = "Assets/_Hollow/Scripts/Hollow.Combat/BossRuntimeController.cs";
        public const string BossHudControllerPath = "Assets/_Hollow/Scripts/Hollow.Combat/BossHudController.cs";
        public const string BranchSessionControllerPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
        public const string EncounterResolverPath = "Assets/_Hollow/Scripts/Hollow.Branches/EncounterResolver.cs";
        public const string RoomRuntimeRootPath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomRuntimeRoot.cs";
        public const string RoomNavMeshCatalogDefinitionPath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomNavMeshCatalogDefinition.cs";
        public const string RoomNavMeshCatalogPath = "Assets/_Hollow/Resources/Navigation/RoomNavMeshCatalog.asset";
        public const string SpaceshipMetaHubTestsPath = "Assets/_Hollow/Tests/EditMode/SpaceshipMetaHubTests.cs";
        public const string M135TestsPath = "Assets/_Hollow/Tests/EditMode/Milestone135FullRunCombatReadinessLockTests.cs";

        private const string GeneratorPath = "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone135FullRunCombatReadinessLockAssetGenerator.cs";
        private const string ValidatorPath = "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone135FullRunCombatReadinessLockValidator.cs";
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        private static readonly string[] BetaWorldNames =
        {
            "Before Teeth",
            "The Sunken Cartouche",
            "The Rust Choir"
        };

        public static readonly string[] RequiredEvidencePaths =
        {
            DocsPath,
            M134ReportPath,
            QaChecklistPath,
            GeneratorPath,
            ValidatorPath,
            M135TestsPath,
            CombatReadinessPolicyPath,
            PlayerWeaponControllerPath,
            PlayerDamageFeedbackControllerPath,
            BossCatalogDefinitionPath,
            BossDefinitionPath,
            BossRuntimeControllerPath,
            BossHudControllerPath,
            BranchSessionControllerPath,
            EncounterResolverPath,
            RoomRuntimeRootPath,
            RoomNavMeshCatalogDefinitionPath,
            RoomNavMeshCatalogPath,
            SpaceshipMetaHubTestsPath,
            Milestone132BiomeWorldSelectionLockAssetGenerator.RunFramingCatalogPath
        };

        [MenuItem("Hollow/Generation/Generate Milestone 135 Full Run Combat Readiness Lock")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportMarkdownPath) ?? "output/reports");
            File.WriteAllText(DocsPath, BuildDocsMarkdown());
            File.WriteAllText(QaChecklistPath, BuildQaChecklistMarkdown());

            var report = BuildReport();
            File.WriteAllText(ReportJsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(ReportMarkdownPath, ToMarkdown(report));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var message = $"{Title} generation {report.result}: {report.passedChecks}/{report.totalChecks} checks passed. Report: {ReportMarkdownPath}";
            if (report.passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public static Milestone135FullRunCombatReadinessLockReport BuildReport()
        {
            var checks = new List<Milestone135FullRunCombatReadinessLockCheck>();
            foreach (var path in RequiredEvidencePaths)
            {
                AddCheck(
                    checks,
                    $"evidence:{Path.GetFileName(path)}",
                    "Evidence",
                    File.Exists(path),
                    File.Exists(path) ? $"Found `{path}`." : $"Missing `{path}`.");
            }

            AddDocsChecks(checks);
            AddCombatChecks(checks);
            AddBossChecks(checks);
            AddFullRunRouteChecks(checks);
            AddNavMeshReadinessChecks(checks);
            AddQaChecklistChecks(checks);
            AddTestChecks(checks);
            AddDependencyChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone135FullRunCombatReadinessLockReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                passed = failures.Length == 0,
                result = failures.Length == 0 ? "PASSED" : "FAILED",
                totalChecks = checks.Count,
                passedChecks = checks.Count(check => check.passed),
                evidencePaths = RequiredEvidencePaths.ToArray(),
                failures = failures,
                checks = checks.ToArray()
            };
        }

        public static string ToMarkdown(Milestone135FullRunCombatReadinessLockReport report)
        {
            var builder = new StringBuilder(8192);
            builder.AppendLine("# M135 Full-Run Combat Readiness Lock Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Route contract: normal run validates `Before Teeth` -> `The Sunken Cartouche` -> `The Rust Choir`.");
            builder.AppendLine("- Combat lock: gentle roll forgiveness with melee remaining primary.");
            builder.AppendLine("- Boss lock: deep polish anchors are `Stone Warden`, `Cartouche Widow`, and `Choir of Teeth`; all 10 bosses smoke-test.");
            builder.AppendLine($"- Playable QA checklist: `{QaChecklistPath}`");
            builder.AppendLine();
            builder.AppendLine("## Evidence");
            builder.AppendLine();
            foreach (var path in report.evidencePaths ?? Array.Empty<string>())
            {
                builder.AppendLine($"- `{path}`");
            }

            builder.AppendLine();
            builder.AppendLine("## Checks");
            builder.AppendLine();
            foreach (var check in report.checks ?? Array.Empty<Milestone135FullRunCombatReadinessLockCheck>())
            {
                builder.AppendLine($"- [{(check.passed ? "PASS" : "FAIL")}] `{check.id}` ({check.category}) - {check.detail}");
            }

            builder.AppendLine();
            builder.AppendLine("## Failures");
            builder.AppendLine();
            if (report.failures == null || report.failures.Length == 0)
            {
                builder.AppendLine("None.");
            }
            else
            {
                foreach (var failure in report.failures)
                {
                    builder.AppendLine($"- {failure}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("## Next Gate");
            builder.AppendLine();
            builder.AppendLine("M136 may build from this beta-readiness gate after M135 full-run combat QA has been reviewed.");
            return builder.ToString();
        }

        public static string BuildDocsMarkdown()
        {
            return
                "# M135: Full-Run Combat Readiness Lock\n\n" +
                "## Summary\n" +
                "M135 is a runtime and lock-artifact milestone focused on beta handoff readiness. It proves the normal full-run loop across the locked M132 world order, makes the melee-dodge core slightly more forgiving, and deep-polishes one anchor boss per world without expanding challenge modes.\n\n" +
                "## Full-Run Route Contract\n" +
                "- Normal runs validate the route `Before Teeth` -> `The Sunken Cartouche` -> `The Rust Choir`.\n" +
                "- The route begins from the ship `Portal Engine`, passes through normal branches and inter-branch hubs, and ends through final `Return to Ship`.\n" +
                "- Inter-branch hubs do not bank souls.\n" +
                "- Final `Return to Ship` banks souls and routes to ship Arrivals.\n" +
                "- Normal-run death routes to ship Arrivals and banks no souls.\n" +
                "- Arrivals quarantine remains the required post-run reset beat.\n" +
                "- M130-M134 branch rules for corrupted, wave, special, and Reward rooms are preserved.\n\n" +
                "## Combat Readiness\n" +
                "- Roll cost is locked to `30` stamina.\n" +
                "- Stamina regeneration delay after rolling is locked to `0.55s`.\n" +
                "- Roll startup is locked to `0.04s`.\n" +
                "- Roll invulnerability is locked to `0.26s`.\n" +
                "- Roll recovery is locked to `0.16s`.\n" +
                "- Roll distance is locked to `1.35m`.\n" +
                "- Melee remains the primary combat loop; M135 adds no new combo, ammo, or weapon system.\n" +
                "- Existing roll, hit, windup, and boss HUD feedback remain the readability base.\n\n" +
                "## Runtime Room Combat Readiness\n" +
                "- M132 biome room variants reuse the approved macro-room NavMesh bakes so combat and boss spawning stay live after the art-pack swap.\n" +
                "- Corrupted, Wave, Soul Eater, and Escapist 1x1 templates reuse the approved single-room bake until custom bakes are authored.\n" +
                "- Missing NavMesh coverage is a combat-readiness failure because rooms without a NavMesh do not spawn enemies.\n\n" +
                "## Boss Readiness\n" +
                "- Deep-polish anchors are `Stone Warden`, `Cartouche Widow`, and `Choir of Teeth`.\n" +
                "- Anchor bosses require readable attack windups, fair dodge windows, stable arena metadata, clear HUD/status, death, room-clear, and reward flow.\n" +
                "- all 10 boss catalog entries must satisfy the minimum smoke contract: catalog resolution, arena id, health, phases, attacks, attack/action profiles, HUD status support, and Boss Lab preview support.\n" +
                "- Full-roster work in M135 is readiness fixing, not deep per-boss redesign.\n\n" +
                "## Interfaces\n" +
                "- Adds `M135CombatReadinessPolicy` as a pure lock helper for roll constants and boss readiness checks.\n" +
                "- Adds M135 generator, validator, reports, and EditMode lock tests.\n" +
                "- No save schema, reward schema, economy schema, room-role, chest-kind, biome, challenge-mode, biomass, Black Orb, or companion-system changes.\n";
        }

        public static string BuildQaChecklistMarkdown()
        {
            return
                "# M135 Playable QA Checklist\n\n" +
                "## Full Run Flow\n" +
                "- Start from the ship and launch a normal run through the Portal Engine.\n" +
                "- Confirm World 1 is Before Teeth, World 2 is The Sunken Cartouche, and World 3 is The Rust Choir.\n" +
                "- Confirm inter-branch hubs do not bank souls.\n" +
                "- Complete the final Return to Ship route and confirm souls bank at Arrivals.\n" +
                "- Die during a normal run and confirm Arrivals loads with zero souls banked.\n" +
                "- Confirm quarantine blocks normal ship traversal until sterilized.\n\n" +
                "## Combat Feel\n" +
                "- Roll feels readable and forgiving without becoming free movement.\n" +
                "- Damage is blocked during roll invulnerability and not blocked during startup or recovery.\n" +
                "- Melee remains the natural primary attack in ordinary combat rooms.\n" +
                "- Hit, roll, and enemy windup feedback are visible during busy rooms.\n\n" +
                "## Boss Anchors\n" +
                "- Stone Warden: charge and burst timings have readable tells and fair dodge windows.\n" +
                "- Cartouche Widow: falling marks, lapis volley, and sigil mines read as avoidable patterns.\n" +
                "- Choir of Teeth: rotating hymn and tooth storm have readable gaps and do not flood the screen instantly.\n\n" +
                "## Roster Smoke\n" +
                "- Spawn or preview all 10 bosses through Boss Lab or Developer Lab.\n" +
                "- Confirm each boss shows name, health, status text, attacks, death, and room clear/reward flow.\n\n" +
                "## Rewards And Readability\n" +
                "- Confirm Reward rooms, Wave rooms, corrupted rooms, special encounters, treasure, and boss rooms keep their minimap/readability affordances.\n" +
                "- Confirm final extraction copy says Return to Ship.\n\n" +
                "## Room Combat Spawn Readiness\n" +
                "- Confirm World 1 biome rooms spawn ordinary enemies in Combat rooms.\n" +
                "- Confirm Wave rooms spawn waves, boss rooms spawn bosses, and special/corrupted endpoint rooms remain traversable.\n" +
                "- If a room logs missing NavMesh data, treat it as an M135 blocker.\n";
        }

        private static void AddDocsChecks(List<Milestone135FullRunCombatReadinessLockCheck> checks)
        {
            RequireAll(checks, "docs:m135-decisions", "Documentation", Read(DocsPath), new[]
            {
                "Full-Run Combat Readiness Lock",
                "`Before Teeth` -> `The Sunken Cartouche` -> `The Rust Choir`",
                "`Portal Engine`",
                "`Return to Ship`",
                "banks no souls",
                "`0.26s`",
                "`1.35m`",
                "`Stone Warden`",
                "`Cartouche Widow`",
                "`Choir of Teeth`",
                "all 10 boss catalog entries",
                "No save schema"
            });
        }

        private static void AddCombatChecks(List<Milestone135FullRunCombatReadinessLockCheck> checks)
        {
            AddCheck(
                checks,
                "combat:roll-lock",
                "Combat",
                M135CombatReadinessPolicy.ValidateRollLock(out var detail),
                detail);

            var policy = Read(CombatReadinessPolicyPath);
            RequireAll(checks, "combat:policy-helper", "Combat", policy, new[]
            {
                "LockedRollStaminaCost = 30f",
                "LockedStaminaRegenDelaySeconds = 0.55f",
                "LockedRollInvulnerabilitySeconds = 0.26f",
                "LockedRollRecoverySeconds = 0.16f",
                "LockedRollDistanceMeters = 1.35f",
                "ValidateAnchorBossPolish"
            });
        }

        private static void AddBossChecks(List<Milestone135FullRunCombatReadinessLockCheck> checks)
        {
            var roster = BossCatalogDefinition.CreateRuntimeRoster();
            AddCheck(checks, "boss:roster-size", "Bosses", roster.Length == 10, $"Runtime roster contains {roster.Length} bosses.");

            foreach (var boss in roster)
            {
                AddCheck(
                    checks,
                    $"boss:minimum:{boss.BossId}",
                    "Bosses",
                    M135CombatReadinessPolicy.ValidateMinimumBossReadiness(boss, out var detail),
                    detail);

                if (M135CombatReadinessPolicy.IsDeepPolishBoss(boss.BossId))
                {
                    AddCheck(
                        checks,
                        $"boss:anchor:{boss.BossId}",
                        "Bosses",
                        M135CombatReadinessPolicy.ValidateAnchorBossPolish(boss, out var anchorDetail),
                        anchorDetail);
                }
            }

            var runtime = Read(BossRuntimeControllerPath);
            RequireAll(checks, "boss:anchor-runtime", "Bosses", runtime, new[]
            {
                "TickStoneWarden",
                "TickCartoucheWidow",
                "TickChoirOfTeeth",
                "cartouche_lapis_volley",
                "cartouche_sigil_mines",
                "PlayBossPatternCue"
            });

            RequireAll(checks, "boss:hud-support", "Bosses", Read(BossHudControllerPath), new[]
            {
                "BossDefinition.DisplayName",
                "BossStatusText",
                "fillImage.fillAmount"
            });
        }

        private static void AddFullRunRouteChecks(List<Milestone135FullRunCombatReadinessLockCheck> checks)
        {
            try
            {
                var framingCatalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(
                    Milestone132BiomeWorldSelectionLockAssetGenerator.RunFramingCatalogPath);
                var betaBiomes = Milestone132BiomeWorldSelectionLockAssetGenerator.BetaBiomeIds.ToArray();
                var worldOrderMatches = framingCatalog != null && betaBiomes.Length == 3;
                for (var index = 0; index < 3 && worldOrderMatches; index++)
                {
                    worldOrderMatches = framingCatalog.TryGetWorld(index + 1, out var world) &&
                                        world.DisplayName == BetaWorldNames[index] &&
                                        RoomBiomeIds.Matches(world.BiomeId, betaBiomes[index]);
                }

                AddCheck(checks, "route:m132-world-order", "Route", worldOrderMatches, "M132 beta world order resolves for World 1/2/3.");

                var content = CreateContent(out var settings);
                var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath);
                var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath);
                var bossCatalog = BossCatalogDefinition.CreateRuntimeDefault();
                if (profile == null || encounterCatalog == null)
                {
                    throw new InvalidOperationException("M135 route contract requires encounter director and encounter catalog assets.");
                }

                for (var worldIndex = 1; worldIndex <= 3; worldIndex++)
                {
                    var seed = 13500 + worldIndex;
                    try
                    {
                        var selectedBoss = BossSelectionResolver.Resolve(
                            bossCatalog,
                            seed,
                            seed,
                            worldIndex,
                            "boss_01",
                            BranchGenerator.DirectedEncounterBranchId);
                        var graph = BranchGenerator.CreateDirectedEncounterBranch(
                            content,
                            settings,
                            profile,
                            worldIndex,
                            seed,
                            selectedBoss != null ? selectedBoss.Arena.arenaId : string.Empty,
                            betaBiomes[worldIndex - 1],
                            enableCorruptedChestLeaf: true,
                            enableWaveRoomLeaf: true,
                            enableSpecialEncounterLeaf: true);
                        ValidateDirectedWorldGraph(graph, encounterCatalog, profile, bossCatalog, worldIndex, seed);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidOperationException($"World {worldIndex} seed {seed}: {exception.Message}", exception);
                    }
                }

                AddCheck(checks, "route:directed-branch-contract", "Route", true, "World 1/2/3 directed branch graphs keep combat, rewards, wave rooms, boss rooms, and boss assignments.");
            }
            catch (Exception exception)
            {
                AddCheck(checks, "route:directed-branch-contract", "Route", false, exception.Message);
            }

            var session = Read(BranchSessionControllerPath);
            var shipTests = Read(SpaceshipMetaHubTestsPath);
            RequireAll(checks, "route:ship-return-soul-rules", "Route", session + shipTests, new[]
            {
                "PortalEngineDisplayName",
                "Return to Ship",
                "CompleteActiveRunIfPersistent",
                "SpaceshipArrivalReason.NormalSuccess",
                "SpaceshipArrivalReason.NormalDeath",
                "WorldLoopBranchReturnEntersInterBranchHubWithoutBankingSouls",
                "WorldLoopFinalReturnToShipBanksSoulsAndRoutesToArrival",
                "NormalRunDeathClearsActiveRunWithoutBankingSouls",
                "ReturnArrivalRequiresSterilizationBeforeMainHallDoorTraverses"
            });
        }

        private static void AddNavMeshReadinessChecks(List<Milestone135FullRunCombatReadinessLockCheck> checks)
        {
            RequireAll(checks, "navmesh:shared-bake-source", "Route", Read(RoomNavMeshCatalogDefinitionPath) + Read(RoomRuntimeRootPath), new[]
            {
                "TryResolveSharedBakeRoomId",
                "before_teeth_macro_",
                "sunken_cartouche_macro_",
                "rust_choir_macro_",
                "corrupted_chest_single_1x1",
                "wave_room_single_1x1",
                "catalog-shared"
            });

            var catalog = AssetDatabase.LoadAssetAtPath<RoomNavMeshCatalogDefinition>(RoomNavMeshCatalogPath);
            var requiredRoomIds = new[]
            {
                "before_teeth_macro_single_1x1",
                "before_teeth_macro_wide_2x1",
                "before_teeth_macro_tall_1x2",
                "before_teeth_macro_block_2x2",
                "before_teeth_macro_l_3cell",
                "sunken_cartouche_macro_single_1x1",
                "rust_choir_macro_single_1x1",
                "corrupted_chest_single_1x1",
                "wave_room_single_1x1",
                "special_soul_eater_single_1x1",
                "special_escapist_single_1x1"
            };

            var missing = requiredRoomIds
                .Where(roomId => catalog == null || !catalog.TryGetNavMeshData(roomId, out _, out _))
                .ToArray();
            AddCheck(
                checks,
                "navmesh:beta-and-special-rooms-resolve",
                "Route",
                missing.Length == 0,
                missing.Length == 0
                    ? "M132 biome and special endpoint rooms resolve usable shared NavMesh bakes."
                    : $"Missing shared NavMesh coverage: {string.Join(", ", missing)}.");
        }

        private static void ValidateDirectedWorldGraph(
            BranchFloorGraph graph,
            EncounterCatalogDefinition encounterCatalog,
            EncounterDirectorProfileDefinition profile,
            BossCatalogDefinition bossCatalog,
            int worldIndex,
            int seed)
        {
            if (!BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError))
            {
                throw new InvalidOperationException(topologyError);
            }

            if (graph.Rooms.Count(room => room.Role == BranchRoomRole.Boss) != 1)
            {
                throw new InvalidOperationException($"World {worldIndex} branch should contain one Boss room.");
            }

            if (graph.Rooms.Count(room => room.Role == BranchRoomRole.Combat) <= 0)
            {
                throw new InvalidOperationException($"World {worldIndex} branch should contain Combat rooms.");
            }

            if (graph.Rooms.Count(room => room.Role == BranchRoomRole.Reward) < 1)
            {
                throw new InvalidOperationException($"World {worldIndex} branch should contain M134 Reward rooms.");
            }

            if (graph.Rooms.Count(room => room.Role == BranchRoomRole.Wave) != 1)
            {
                throw new InvalidOperationException($"World {worldIndex} branch should contain one Wave room.");
            }

            var plan = EncounterResolver.CreateDirectedSeededPlan(graph, encounterCatalog, seed, worldIndex, profile, 0, bossCatalog);
            var bossRoom = graph.Rooms.First(room => room.Role == BranchRoomRole.Boss);
            if (!plan.TryResolve(bossRoom.Id.Value, out var bossAssignment) ||
                !bossAssignment.EnemySpawnKinds.Contains("spawnEnemyBoss") ||
                string.IsNullOrWhiteSpace(bossAssignment.BossId))
            {
                throw new InvalidOperationException($"World {worldIndex} boss room has no concrete boss assignment.");
            }

            if (!plan.Assignments.Any(assignment =>
                    graph.Rooms.Any(room => room.Id.Value == assignment.RoomId && room.Role == BranchRoomRole.Combat) &&
                    assignment.EnemySpawnKinds.Count > 0))
            {
                throw new InvalidOperationException($"World {worldIndex} branch has no populated Combat encounter.");
            }
        }

        private static void AddQaChecklistChecks(List<Milestone135FullRunCombatReadinessLockCheck> checks)
        {
            RequireAll(checks, "qa:playable-checklist", "QA", Read(QaChecklistPath), new[]
            {
                "Full Run Flow",
                "Combat Feel",
                "Boss Anchors",
                "Roster Smoke",
                "Rewards And Readability",
                "Room Combat Spawn Readiness",
                "Portal Engine",
                "Return to Ship"
            });
        }

        private static void AddTestChecks(List<Milestone135FullRunCombatReadinessLockCheck> checks)
        {
            RequireAll(checks, "tests:m135-lock-tests", "Tests", Read(M135TestsPath), new[]
            {
                "RollTuningMatchesM135GentleForgivenessLock",
                "RollInvulnerabilityBlocksDamageOnlyDuringLockedWindow",
                "FullRunRouteContractValidatesThreeWorldOrderAndBossAssignments",
                "BetaAndSpecialRoomTemplatesResolveSharedNavMeshBakes",
                "AnchorBossesMeetDeepPolishReadabilityContract",
                "FullBossRosterMeetsMinimumSmokeContract",
                "LiveReportPassesAllM135Checks",
                "GeneratedReportsAndChecklistArePresentPassingAndUseM135LockId",
                "ValidatorReportsGeneratedStateValid"
            });
        }

        private static void AddDependencyChecks(List<Milestone135FullRunCombatReadinessLockCheck> checks)
        {
            var report = Read(M134ReportPath);
            AddCheck(
                checks,
                "dependency:m134-passing-report",
                "Dependency",
                report.Contains("- Result: PASSED") && report.Contains(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.LockId),
                "M134 passing report exists and includes the M134 lock id.");
        }

        private static BranchSessionContent CreateContent(out BranchGenerationSettingsDefinition settings)
        {
            settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            if (settings == null || catalog == null)
            {
                throw new InvalidOperationException("M135 route scan requires generated branch settings and template catalog assets.");
            }

            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            return content;
        }

        private static void RequireAll(List<Milestone135FullRunCombatReadinessLockCheck> checks, string id, string category, string content, IEnumerable<string> needles)
        {
            var missing = needles.Where(needle => !content.Contains(needle)).ToArray();
            AddCheck(
                checks,
                id,
                category,
                missing.Length == 0,
                missing.Length == 0 ? "Required lock strings found." : $"Missing: {string.Join(", ", missing)}.");
        }

        private static void AddCheck(List<Milestone135FullRunCombatReadinessLockCheck> checks, string id, string category, bool passed, string detail)
        {
            checks.Add(new Milestone135FullRunCombatReadinessLockCheck
            {
                id = id,
                category = category,
                passed = passed,
                detail = detail
            });
        }

        private static string Read(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }
    }
}
