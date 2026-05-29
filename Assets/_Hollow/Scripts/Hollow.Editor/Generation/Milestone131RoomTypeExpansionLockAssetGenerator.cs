using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Rewards;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone131RoomTypeExpansionLockReport
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
        public Milestone131RoomTypeExpansionLockCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone131RoomTypeExpansionLockCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone131RoomTypeExpansionLockAssetGenerator
    {
        public const string LockId = "m131_room_type_expansion_lock_v1";
        public const string Title = "M131 Room Type Expansion Lock + Wave Room Prototype";
        public const string DocsPath = "Docs/Milestone131RoomTypeExpansionLock.md";
        public const string M130ReportPath = "output/reports/m130_reward_chest_risk_pass.md";
        public const string BranchGeneratorPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchGenerator.cs";
        public const string BranchFeaturePlanPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchFeaturePlan.cs";
        public const string BranchRoomRolePath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchRoomRole.cs";
        public const string BranchSessionContentPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionContent.cs";
        public const string BranchSessionControllerPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
        public const string BranchRoomTemplateCatalogPath = "Assets/_Hollow/Scripts/Hollow.Data/Definitions/BranchRoomTemplateCatalogDefinition.cs";
        public const string RoomCombatControllerPath = "Assets/_Hollow/Scripts/Hollow.Combat/RoomCombatController.cs";
        public const string RoomCombatEncounterKindPath = "Assets/_Hollow/Scripts/Hollow.Combat/RoomCombatEncounterKind.cs";
        public const string RoomWaveEncounterPlanPath = "Assets/_Hollow/Scripts/Hollow.Combat/RoomWaveEncounterPlan.cs";
        public const string CombatHudModelPath = "Assets/_Hollow/Scripts/Hollow.Combat/CombatHudModel.cs";
        public const string CombatHudControllerPath = "Assets/_Hollow/Scripts/Hollow.Combat/CombatHudController.cs";
        public const string ProceduralRewardResolverPath = "Assets/_Hollow/Scripts/Hollow.Branches/ProceduralRewardResolver.cs";
        public const string BranchMiniMapControllerPath = "Assets/_Hollow/Scripts/Hollow.UI/Shell/BranchMiniMapController.cs";
        public const string WaveRoomEndpointRoomPath = "Assets/_Hollow/Data/Rooms/MacroFixtures/wave_room_single_1x1.hollowruntime.json";
        public const string M131TestsPath = "Assets/_Hollow/Tests/EditMode/Milestone131RoomTypeExpansionLockTests.cs";
        public const string ReportMarkdownPath = "output/reports/m131_room_type_expansion_lock.md";
        public const string ReportJsonPath = "output/reports/m131_room_type_expansion_lock.json";

        public static readonly string[] RequiredEvidencePaths =
        {
            DocsPath,
            M130ReportPath,
            BranchGeneratorPath,
            BranchFeaturePlanPath,
            BranchRoomRolePath,
            BranchSessionContentPath,
            BranchSessionControllerPath,
            BranchRoomTemplateCatalogPath,
            RoomCombatControllerPath,
            RoomCombatEncounterKindPath,
            RoomWaveEncounterPlanPath,
            CombatHudModelPath,
            CombatHudControllerPath,
            ProceduralRewardResolverPath,
            BranchMiniMapControllerPath,
            WaveRoomEndpointRoomPath,
            M131TestsPath
        };

        [MenuItem("Hollow/Generation/Generate Milestone 131 Room Type Expansion Lock")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportMarkdownPath) ?? "output/reports");
            File.WriteAllText(DocsPath, BuildDocsMarkdown());

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

        public static Milestone131RoomTypeExpansionLockReport BuildReport()
        {
            var checks = new List<Milestone131RoomTypeExpansionLockCheck>();
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
            AddRuntimeChecks(checks);
            AddCombatChecks(checks);
            AddRewardAndHudChecks(checks);
            AddRoomTemplateChecks(checks);
            AddTestChecks(checks);
            AddDependencyChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone131RoomTypeExpansionLockReport
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

        public static string ToMarkdown(Milestone131RoomTypeExpansionLockReport report)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# M131 Room Type Expansion Lock Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Room policy: beta rooms are Safe Start, Combat, Wave Room, Treasure, Boss, Shop/Hub, Secret, and Corrupted Chest.");
            builder.AppendLine("- Wave rule: every normal world-loop branch gets one optional terminal Wave Room leaf.");
            builder.AppendLine("- Reward rule: clearing all three waves spawns a Golden Chest.");
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
            foreach (var check in report.checks ?? Array.Empty<Milestone131RoomTypeExpansionLockCheck>())
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
            builder.AppendLine("M132 may begin after M131 is reviewed and accepted.");
            return builder.ToString();
        }

        private static void AddDocsChecks(List<Milestone131RoomTypeExpansionLockCheck> checks)
        {
            var docs = Read(DocsPath);
            RequireAll(checks, "docs:m131-decisions", "Documentation", docs, new[]
            {
                "Safe Start, Combat, Wave Room, Treasure, Boss, Shop/Hub, Secret, and Corrupted Chest",
                "one optional `Wave Room` leaf",
                "not required for boss access",
                "never eligible for boss-key placement",
                "three waves",
                "Golden Chest",
                "No save schema changes",
                "survival, trap traversal, lever rooms, defend-object rooms, life/death rooms"
            });
        }

        private static void AddRuntimeChecks(List<Milestone131RoomTypeExpansionLockCheck> checks)
        {
            var roles = Read(BranchRoomRolePath);
            var generator = Read(BranchGeneratorPath);
            var featurePlan = Read(BranchFeaturePlanPath);
            var session = Read(BranchSessionControllerPath);
            var content = Read(BranchSessionContentPath);
            var catalog = Read(BranchRoomTemplateCatalogPath);

            RequireAll(checks, "runtime:wave-public-role", "Runtime", roles + generator + catalog + content, new[]
            {
                "Wave = 7",
                "WaveRoomAssetId",
                "wave_room_single_1x1",
                "WaveRoomEndpoint",
                "WaveRoomAsset"
            });
            RequireAll(checks, "runtime:wave-terminal-leaf", "Runtime", generator, new[]
            {
                "enableWaveRoomLeaf",
                "TryPlaceEndpointRecord",
                "BranchRoomRole.Wave",
                "must be a terminal leaf",
                "must not attach to boss, secret, treasure, or corrupted endpoints"
            });
            RequireAll(checks, "runtime:wave-not-critical-path", "Runtime", featurePlan + generator, new[]
            {
                "room.Role == BranchRoomRole.Combat",
                "ApplyBossKeyLock"
            });
            RequireAll(checks, "runtime:normal-world-loop-enables-wave", "Runtime", session, new[]
            {
                "ShouldEnableWaveRoomLeaf",
                "RoomCombatEncounterKind.Wave",
                "TryGetRoomAsset(State.CurrentRoom.RuntimeRoomAssetId, ActiveBiomeId"
            });
        }

        private static void AddCombatChecks(List<Milestone131RoomTypeExpansionLockCheck> checks)
        {
            var kind = Read(RoomCombatEncounterKindPath);
            var plan = Read(RoomWaveEncounterPlanPath);
            var controller = Read(RoomCombatControllerPath);

            RequireAll(checks, "combat:wave-kind-and-plan", "Combat", kind + plan, new[]
            {
                "Wave = 2",
                "DefaultWaveCounts = { 2, 3, 4 }",
                "RoomWaveEncounterPlan",
                "StatusTextForWave"
            });
            RequireAll(checks, "combat:wave-runtime-flow", "Combat", controller, new[]
            {
                "activeWavePlan",
                "SpawnNextWave",
                "CurrentWaveStatusText",
                "activeWaveIndex + 1 < activeWavePlan.TotalWaves"
            });
        }

        private static void AddRewardAndHudChecks(List<Milestone131RoomTypeExpansionLockCheck> checks)
        {
            var rewards = Read(ProceduralRewardResolverPath);
            var minimap = Read(BranchMiniMapControllerPath);
            var hudModel = Read(CombatHudModelPath);
            var hudController = Read(CombatHudControllerPath);

            RequireAll(checks, "reward:wave-golden-chest", "Rewards", rewards, new[]
            {
                "BranchRoomRole.Wave",
                "GoldenChestGrant",
                "ChestRewardResolver.GoldenChestRewardId"
            });
            RequireAll(checks, "hud:wave-readability", "HUD", minimap + hudModel + hudController, new[]
            {
                "BranchRoomRole.Wave",
                "\"W\"",
                "StatusOverride",
                "HasStatusOverride"
            });
        }

        private static void AddRoomTemplateChecks(List<Milestone131RoomTypeExpansionLockCheck> checks)
        {
            var room = Read(WaveRoomEndpointRoomPath);
            RequireAll(checks, "room:wave-room-endpoint", "Rooms", room, new[]
            {
                "wave_room_single_1x1",
                "Wave Room Endpoint 1x1",
                "rewardType\": \"golden-chest",
                "spawn_point_goldenChest",
                "wave_spawn_northwest",
                "wave_spawn_east"
            });

            if (HollowRuntimeV2Importer.TryImport(room, out var asset, out var importError))
            {
                AddCheck(checks, "room:wave-room-imports", "Rooms", asset.Id == BranchGenerator.WaveRoomAssetId, $"Imported `{asset.Id}`.");
                AddCheck(checks, "room:wave-room-has-spawns", "Rooms", asset.EnemySpawns.Count >= 4 && asset.ItemSpawns.Any(spawn => spawn.kind == "spawn_point_goldenChest"), "Wave endpoint has enemy anchors and a golden chest marker.");
                var validation = RuntimeRoomValidator.Validate(asset);
                AddCheck(checks, "room:wave-room-validates", "Rooms", validation.IsValid, validation.IsValid ? "Wave endpoint runtime validation passed." : string.Join("; ", validation.Errors));
            }
            else
            {
                AddCheck(checks, "room:wave-room-imports", "Rooms", false, importError);
            }
        }

        private static void AddTestChecks(List<Milestone131RoomTypeExpansionLockCheck> checks)
        {
            var tests = Read(M131TestsPath);
            RequireAll(checks, "tests:m131-coverage", "Tests", tests, new[]
            {
                "NormalBranchesGetExactlyOneOptionalWaveLeaf",
                "WaveRoomsPayGoldenChestAndSplitIntoThreeWaves",
                "WaveEndpointImportsAndCanInheritBranchBiome",
                "LiveReportPassesAllRoomTypeExpansionChecks",
                "GeneratedReportsArePresentPassingAndUseM131LockId",
                "ValidatorReportsGeneratedStateValid"
            });
        }

        private static void AddDependencyChecks(List<Milestone131RoomTypeExpansionLockCheck> checks)
        {
            var report = Read(M130ReportPath);
            RequireAll(checks, "dependency:m130-pass", "Dependency", report, new[]
            {
                "# M130 Reward + Chest Risk Pass Report",
                "- Result: PASSED",
                "m130_reward_chest_risk_pass_v1"
            });
        }

        private static void RequireAll(List<Milestone131RoomTypeExpansionLockCheck> checks, string id, string category, string text, IReadOnlyList<string> needles)
        {
            var missing = needles.Where(needle => !text.Contains(needle)).ToArray();
            AddCheck(
                checks,
                id,
                category,
                missing.Length == 0,
                missing.Length == 0 ? $"Found {needles.Count} required entries." : $"Missing entries: {string.Join(", ", missing)}.");
        }

        private static void AddCheck(List<Milestone131RoomTypeExpansionLockCheck> checks, string id, string category, bool passed, string detail)
        {
            checks.Add(new Milestone131RoomTypeExpansionLockCheck
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

        private static string BuildDocsMarkdown()
        {
            return @"# M131: Room Type Expansion Lock + Wave Room Prototype

M131 is a runtime and lock-artifact milestone. It locks the beta room-type set and adds the first optional challenge endpoint: a Wave Room.

## Decisions

- Beta room whitelist: Safe Start, Combat, Wave Room, Treasure, Boss, Shop/Hub, Secret, and Corrupted Chest.
- Normal world-loop branches add one optional `Wave Room` leaf.
- Wave Rooms are not required for boss access.
- Wave Rooms are never eligible for boss-key placement.
- Wave Rooms are terminal leaves and do not replace boss, treasure, secret, or corrupted endpoints.
- Wave Rooms inherit the active branch biome; M131 does not add a new wave biome pack.
- Entering a Wave Room commits the player to the fight; doors remain locked until all waves are clear.
- Wave Rooms run three waves with a default 2/3/4 enemy shape.
- Clearing the third wave spawns a Golden Chest using existing golden chest presentation and contents.
- Combat HUD status may show `Wave 1/3`, `Wave 2/3`, and `Wave 3/3`.
- The minimap marks Wave Rooms with a readable Wave Room marker.

## Deferrals

- No save schema changes.
- No economy schema changes.
- No new chest kind.
- No biomass, Black Orb, Soul Chest, or mimic room runtime work.
- Deferred room types: survival, trap traversal, lever rooms, defend-object rooms, life/death rooms, mimic rooms, Soul Chest rooms, biomass rooms, and Black Orb rooms.

## Acceptance

- Every normal world-loop branch has exactly one optional Wave Room leaf.
- Boss keys never spawn in Wave, Secret, Corrupted Chest, Treasure, or Boss rooms.
- The Wave Room endpoint imports and validates as a 1x1 room with enemy anchors and a golden chest marker.
- Wave combat splits deterministic encounter contents into three runtime-only waves.
- M131 generated markdown and JSON reports pass with lock id `m131_room_type_expansion_lock_v1`.
";
        }
    }
}
