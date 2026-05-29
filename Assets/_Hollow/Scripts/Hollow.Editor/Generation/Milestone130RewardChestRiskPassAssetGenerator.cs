using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Branches;
using Hollow.Rewards;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone130RewardChestRiskPassReport
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
        public Milestone130RewardChestRiskPassCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone130RewardChestRiskPassCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone130RewardChestRiskPassAssetGenerator
    {
        public const string LockId = "m130_reward_chest_risk_pass_v1";
        public const string Title = "M130 Reward + Chest Risk Pass";
        public const string DocsPath = "Docs/Milestone130RewardChestRiskPass.md";
        public const string M129ReportPath = "output/reports/m129_ship_soul_loop_greybox.md";
        public const string BranchGeneratorPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchGenerator.cs";
        public const string BranchRoomRolePath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchRoomRole.cs";
        public const string BranchSessionControllerPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
        public const string BranchSessionContentPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionContent.cs";
        public const string BranchRoomTemplateCatalogPath = "Assets/_Hollow/Scripts/Hollow.Data/Definitions/BranchRoomTemplateCatalogDefinition.cs";
        public const string ChestKindPath = "Assets/_Hollow/Scripts/Hollow.Rewards/ChestKind.cs";
        public const string ChestRewardResolverPath = "Assets/_Hollow/Scripts/Hollow.Rewards/ChestRewardResolver.cs";
        public const string EncounterResolverPath = "Assets/_Hollow/Scripts/Hollow.Branches/EncounterResolver.cs";
        public const string ProceduralRewardResolverPath = "Assets/_Hollow/Scripts/Hollow.Branches/ProceduralRewardResolver.cs";
        public const string RoomDesignerProjectPath = "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerProject.cs";
        public const string RoomDesignerCompilerPath = "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerCompiler.cs";
        public const string RoomDesignerScenePreviewPath = "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerScenePreviewBuilder.cs";
        public const string MeshyEnvironmentPropGeneratorPath = "Assets/_Hollow/Scripts/Hollow.Editor/Generation/MeshyEnvironmentPropAssetGenerator.cs";
        public const string M130TestsPath = "Assets/_Hollow/Tests/EditMode/Milestone130RewardChestRiskPassTests.cs";
        public const string CorruptedRewardPoolPath = "Assets/_Hollow/Data/Rewards/M130/CorruptedChestRewardPool_M130.asset";
        public const string CorruptedChestEndpointRoomPath = "Assets/_Hollow/Data/Rooms/MacroFixtures/corrupted_chest_single_1x1.hollowruntime.json";
        public const string ReportMarkdownPath = "output/reports/m130_reward_chest_risk_pass.md";
        public const string ReportJsonPath = "output/reports/m130_reward_chest_risk_pass.json";

        public static readonly string[] RequiredEvidencePaths =
        {
            DocsPath,
            M129ReportPath,
            BranchGeneratorPath,
            BranchRoomRolePath,
            BranchSessionControllerPath,
            BranchSessionContentPath,
            BranchRoomTemplateCatalogPath,
            ChestKindPath,
            ChestRewardResolverPath,
            EncounterResolverPath,
            ProceduralRewardResolverPath,
            RoomDesignerProjectPath,
            RoomDesignerCompilerPath,
            RoomDesignerScenePreviewPath,
            MeshyEnvironmentPropGeneratorPath,
            M130TestsPath,
            CorruptedRewardPoolPath,
            CorruptedChestEndpointRoomPath
        };

        public static readonly string[] CuratedCorruptedRewardIds =
        {
            "vital_locket",
            "iron_stitch",
            "fleet_pin",
            "stamina_thread",
            "blade_lesson",
            "bolt_lesson",
            "mending_charm",
            "echo_burst",
            "mend_card"
        };

        private static readonly string[] CuratedRewardAssetPaths =
        {
            "Assets/_Hollow/Data/Rewards/M28/Reward_VitalLocket.asset",
            "Assets/_Hollow/Data/Rewards/M28/Reward_IronStitch.asset",
            "Assets/_Hollow/Data/Rewards/M28/Reward_FleetPin.asset",
            "Assets/_Hollow/Data/Rewards/M28/Reward_StaminaThread.asset",
            "Assets/_Hollow/Data/Rewards/M28/Reward_BladeLesson.asset",
            "Assets/_Hollow/Data/Rewards/M28/Reward_BoltLesson.asset",
            "Assets/_Hollow/Data/Rewards/M28/Reward_MendingCharm.asset",
            "Assets/_Hollow/Data/Rewards/M28/Reward_EchoBurst.asset",
            "Assets/_Hollow/Data/Rewards/M28/Reward_MendCard.asset"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 130 Reward Chest Risk Pass")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportMarkdownPath) ?? "output/reports");
            File.WriteAllText(DocsPath, BuildDocsMarkdown());

            EnsureCorruptedRewardPool();

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

        public static RewardPoolDefinition EnsureCorruptedRewardPool()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CorruptedRewardPoolPath) ?? "Assets/_Hollow/Data/Rewards/M130");
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(CorruptedRewardPoolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<RewardPoolDefinition>();
                AssetDatabase.CreateAsset(pool, CorruptedRewardPoolPath);
            }

            var rewards = CuratedRewardAssetPaths
                .Select(AssetDatabase.LoadAssetAtPath<RewardDefinition>)
                .Where(reward => reward != null)
                .ToArray();
            pool.Configure("m130_corrupted_chest_rewards", rewards);
            EditorUtility.SetDirty(pool);
            return pool;
        }

        public static Milestone130RewardChestRiskPassReport BuildReport()
        {
            var checks = new List<Milestone130RewardChestRiskPassCheck>();
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
            AddRoomDesignerChecks(checks);
            AddSpecialRoomChecks(checks);
            AddRewardPoolChecks(checks);
            AddTestChecks(checks);
            AddDependencyChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone130RewardChestRiskPassReport
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

        public static string ToMarkdown(Milestone130RewardChestRiskPassReport report)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# M130 Reward + Chest Risk Pass Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Reward policy: ordinary rooms stay sparse; treasure, boss, shop, and optional-risk rooms carry build value.");
            builder.AppendLine("- Optional risk: Corrupted Chest rooms are rare terminal leaves with two-step consent.");
            builder.AppendLine("- Consequence: opening a Corrupted Chest gives a rare reward plus coins and applies -1 max HP for the run.");
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
            foreach (var check in report.checks ?? Array.Empty<Milestone130RewardChestRiskPassCheck>())
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
            builder.AppendLine("M131 Room Type Expansion Lock may begin after M130 is reviewed and accepted.");
            return builder.ToString();
        }

        private static void AddDocsChecks(List<Milestone130RewardChestRiskPassCheck> checks)
        {
            var docs = Read(DocsPath);
            RequireAll(checks, "docs:reward-policy", "Documentation", docs, new[]
            {
                "ordinary rooms stay sparse",
                "Normal Chests",
                "Golden Chests",
                "Corrupted Chest",
                "two-step consent",
                "-1 max HP",
                "No Soul Chest",
                "Corrupted Chest designer marker",
                "corrupted_chest_single_1x1",
                "No save schema changes"
            });
        }

        private static void AddRuntimeChecks(List<Milestone130RewardChestRiskPassCheck> checks)
        {
            var branchRoles = Read(BranchRoomRolePath);
            var chestKind = Read(ChestKindPath);
            var chestResolver = Read(ChestRewardResolverPath);
            var branchGenerator = Read(BranchGeneratorPath);
            var branchSession = Read(BranchSessionControllerPath);
            var branchSessionContent = Read(BranchSessionContentPath);
            var branchCatalog = Read(BranchRoomTemplateCatalogPath);
            var procedural = Read(ProceduralRewardResolverPath);
            var encounters = Read(EncounterResolverPath);

            RequireAll(checks, "runtime:public-interfaces", "Runtime", branchRoles + chestKind + chestResolver + branchGenerator, new[]
            {
                "CorruptedChest",
                "Corrupted",
                "CorruptedChestRewardId",
                "CorruptedChestRollPercent = 10",
                "ShouldRollCorruptedChestLeaf",
                "CorruptedChestRoomAssetId"
            });
            RequireAll(checks, "runtime:corrupted-topology", "Runtime", branchGenerator, new[]
            {
                "enableCorruptedChestLeaf",
                "TryPlaceEndpointRecord",
                "BranchRoomRole.CorruptedChest",
                "must be a terminal leaf",
                "must not attach to boss, secret, or treasure"
            });
            RequireAll(checks, "runtime:corrupted-chest-behavior", "Runtime", branchSession, new[]
            {
                "CorruptedChestWarningMessage",
                "Interact again to confirm",
                "CorruptedChestCurseSourcePrefix",
                "maxHealth = -1",
                "Corrupted Chest:"
            });
            RequireAll(checks, "runtime:reward-baseline", "Runtime", procedural + chestResolver, new[]
            {
                "roll < 2",
                "roll < 14",
                "roll < 52",
                "roll < 76",
                "8 + StableHash",
                "15 + StableHash"
            });
            RequireAll(checks, "runtime:corrupted-no-encounter", "Runtime", encounters, new[]
            {
                "BranchRoomRole.CorruptedChest"
            });
            RequireAll(checks, "runtime:special-corrupted-room", "Runtime", branchGenerator + branchSession + branchSessionContent + branchCatalog, new[]
            {
                "corrupted_chest_single_1x1",
                "CorruptedChestRoomAsset",
                "CorruptedChestEndpoint",
                "spawn_point_corruptedChest"
            });
        }

        private static void AddRoomDesignerChecks(List<Milestone130RewardChestRiskPassCheck> checks)
        {
            var project = Read(RoomDesignerProjectPath);
            var compiler = Read(RoomDesignerCompilerPath);
            var preview = Read(RoomDesignerScenePreviewPath);
            var meshy = Read(MeshyEnvironmentPropGeneratorPath);
            RequireAll(checks, "room-designer:corrupted-chest-marker", "Room Designer", project + compiler + preview, new[]
            {
                "CorruptedChestSpawn",
                "spawn_point_corruptedChest",
                "RoomDesignerMarkerKinds.IsChest",
                "PresentationPrefabRole.ChestCorrupted"
            });
            RequireAll(checks, "artpass:corrupted-chest-prefab-spec", "ArtPass", meshy, new[]
            {
                "AP_ChestCorrupted.prefab",
                "MeshySigilboundCorruptedChestModel",
                "PresentationPrefabRole.ChestCorrupted",
                "Meshy_AI_Sigilbound_Chest_0524182705_texture"
            });
        }

        private static void AddSpecialRoomChecks(List<Milestone130RewardChestRiskPassCheck> checks)
        {
            var room = Read(CorruptedChestEndpointRoomPath);
            RequireAll(checks, "room:corrupted-chest-endpoint", "Rooms", room, new[]
            {
                "corrupted_chest_single_1x1",
                "Corrupted Chest Endpoint 1x1",
                "rewardType\": \"corrupted-chest",
                "spawn_point_corruptedChest",
                "altar_rock_north",
                "decorCrystalCluster",
                "corrupt_ruin_backdrop",
                "\"enemySpawns\": []"
            });
        }

        private static void AddRewardPoolChecks(List<Milestone130RewardChestRiskPassCheck> checks)
        {
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(CorruptedRewardPoolPath);
            AddCheck(checks, "asset:corrupted-reward-pool", "Assets", pool != null, pool != null ? "Corrupted reward pool exists." : "Corrupted reward pool is missing.");
            if (pool == null)
            {
                return;
            }

            var ids = pool.Rewards.Select(reward => reward.RewardId).ToHashSet();
            var missing = CuratedCorruptedRewardIds.Where(id => !ids.Contains(id)).ToArray();
            AddCheck(
                checks,
                "asset:curated-corrupted-rewards",
                "Assets",
                missing.Length == 0,
                missing.Length == 0 ? "Corrupted reward pool contains the curated set." : $"Missing rewards: {string.Join(", ", missing)}.");
        }

        private static void AddTestChecks(List<Milestone130RewardChestRiskPassCheck> checks)
        {
            var tests = Read(M130TestsPath);
            RequireAll(checks, "tests:m130-coverage", "Tests", tests, new[]
            {
                "CorruptedChestRoomsAreRareTerminalLeaves",
                "CorruptedRoomsSkipEncountersAndUseCorruptedChestReward",
                "CorruptedChestContentsGrantCuratedRewardCoinsAndRunLongCurse",
                "RoomDesignerCorruptedChestMarkerRoundtripsAndPreviews",
                "CorruptedChestEndpointRoomImportsAsSpecialRoom",
                "GeneratedReportsArePresentPassingAndUseM130LockId"
            });
        }

        private static void AddDependencyChecks(List<Milestone130RewardChestRiskPassCheck> checks)
        {
            var report = Read(M129ReportPath);
            RequireAll(checks, "dependency:m129-pass", "Dependency", report, new[]
            {
                "# M129 Ship-Soul Loop Greybox Report",
                "- Result: PASSED",
                "m129_ship_soul_loop_greybox_v1"
            });
        }

        private static void RequireAll(List<Milestone130RewardChestRiskPassCheck> checks, string id, string category, string text, IReadOnlyList<string> needles)
        {
            var missing = needles.Where(needle => !text.Contains(needle)).ToArray();
            AddCheck(
                checks,
                id,
                category,
                missing.Length == 0,
                missing.Length == 0 ? $"Found {needles.Count} required entries." : $"Missing entries: {string.Join(", ", missing)}.");
        }

        private static void AddCheck(List<Milestone130RewardChestRiskPassCheck> checks, string id, string category, bool passed, string detail)
        {
            checks.Add(new Milestone130RewardChestRiskPassCheck
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
            return @"# M130: Reward + Chest Risk Pass

M130 is a runtime and lock-artifact milestone. It keeps the beta reward economy lean and readable, while adding one optional-risk endpoint prototype.

## Decisions

- ordinary rooms stay sparse: coins, HP refill, normal/golden chest, or nothing.
- Normal Chests remain practical rewards with coins or HP refill.
- Golden Chests remain stronger rewards with coins, healing, or cards.
- Corrupted Chest rooms are rare extra branch-ending leaves.
- Corrupted Chest rooms roll at 10% on normal world-loop procedural branches.
- Corrupted Chest rooms never replace boss, secret, or treasure rooms.
- Corrupted Chest rooms prefer the dedicated `corrupted_chest_single_1x1` endpoint room when that template is available.
- The dedicated room is a small shrine-style endpoint with no enemies, clear chest access, altar rocks, and nonblocking corrupted decor.
- The Room Designer exposes a Corrupted Chest designer marker that previews with the corrupted chest prefab role.
- Corrupted Chests use two-step consent before opening.
- Opening a Corrupted Chest grants a curated rare build reward plus coins.
- Opening a Corrupted Chest applies -1 max HP for the rest of the run.

## Runtime Copy

- Warning: `Open Corrupted Chest? Gain a rare reward. Lose 1 max HP for this run. Interact again to confirm.`
- Reward result: `Corrupted Chest: <reward> gained. -1 max HP for this run.`

## Deferrals

- No Soul Chest runtime work.
- No Mimic Chest runtime work.
- No Demonic Chest runtime work.
- No biomass, Black Orb, or generic-resource runtime work.
- No deck UI work.
- No save schema changes.

## Acceptance

- Ordinary room rewards stay sparse and M52-compatible.
- Corrupted rooms appear rarely as terminal optional-risk endpoints.
- Corrupted Chest designer marker exports as `spawn_point_corruptedChest`.
- The player must explicitly confirm the corrupted chest before accepting the risk.
- The reward and -1 max HP consequence are readable in HUD/reveal copy.
- The max HP loss persists through branch hubs and ends with the run.
";
        }
    }
}
