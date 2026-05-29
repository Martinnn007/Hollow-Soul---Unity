using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Rewards;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone134BranchPacingRewardRoomShapePassReport
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
        public Milestone134BranchPacingRewardRoomShapePassCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone134BranchPacingRewardRoomShapePassCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone134BranchPacingRewardRoomShapePassAssetGenerator
    {
        public const string LockId = "m134_branch_pacing_reward_room_shape_pass_v1";
        public const string Title = "M134 Branch Pacing + Reward Room Shape Pass";
        public const string DocsPath = "Docs/Milestone134BranchPacingRewardRoomShapePass.md";
        public const string M133ReportPath = "output/reports/m133_npc_special_encounter_prototype_set.md";
        public const string BranchGeneratorPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchGenerator.cs";
        public const string BranchFeaturePlanPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchFeaturePlan.cs";
        public const string BranchSessionControllerPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
        public const string EncounterResolverPath = "Assets/_Hollow/Scripts/Hollow.Branches/EncounterResolver.cs";
        public const string ProceduralRewardResolverPath = "Assets/_Hollow/Scripts/Hollow.Branches/ProceduralRewardResolver.cs";
        public const string M134TestsPath = "Assets/_Hollow/Tests/EditMode/Milestone134BranchPacingRewardRoomShapePassTests.cs";
        public const string ReportMarkdownPath = "output/reports/m134_branch_pacing_reward_room_shape_pass.md";
        public const string ReportJsonPath = "output/reports/m134_branch_pacing_reward_room_shape_pass.json";
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        public static readonly string[] RequiredEvidencePaths =
        {
            DocsPath,
            M133ReportPath,
            BranchGeneratorPath,
            BranchFeaturePlanPath,
            BranchSessionControllerPath,
            EncounterResolverPath,
            ProceduralRewardResolverPath,
            M134TestsPath
        };

        [MenuItem("Hollow/Generation/Generate Milestone 134 Branch Pacing Reward Room Shape Pass")]
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

        public static Milestone134BranchPacingRewardRoomShapePassReport BuildReport()
        {
            var checks = new List<Milestone134BranchPacingRewardRoomShapePassCheck>();
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
            AddLivePolicyChecks(checks);
            AddTestChecks(checks);
            AddDependencyChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone134BranchPacingRewardRoomShapePassReport
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

        public static string ToMarkdown(Milestone134BranchPacingRewardRoomShapePassReport report)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# M134 Branch Pacing + Reward Room Shape Pass Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Branch pacing rule: one guaranteed non-combat Reward room plus a deterministic 50% second Reward room.");
            builder.AppendLine("- Shape rule: standard body rooms use 1x1/2x1/1x2-biased weights while 2x2 and L rooms stay medium rarity.");
            builder.AppendLine("- Cache rule: Reward rooms use the M134 wooden-cache roll; Combat rooms keep M52 sparse rewards.");
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
            foreach (var check in report.checks ?? Array.Empty<Milestone134BranchPacingRewardRoomShapePassCheck>())
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
            builder.AppendLine("M135 may build on the locked branch pacing and Reward-room cache behavior after M134 is reviewed and accepted.");
            return builder.ToString();
        }

        public static string BuildDocsMarkdown()
        {
            return
                "# M134: Branch Pacing + Reward Room Shape Pass\n\n" +
                "## Summary\n" +
                "M134 adds ordinary non-combat `Reward` rooms to normal world-loop branches as pacing breaks. These rooms are not special encounters, do not run combat, and keep doors open while offering a modest supply-cache reward chance.\n\n" +
                "## Branch Pacing\n" +
                "- Normal world-loop branches get `1` guaranteed Reward room on the main route.\n" +
                "- Each branch rolls a deterministic `50%` chance for a second Reward room.\n" +
                "- Reward rooms are selected from ordinary origin-to-boss-path body rooms.\n" +
                "- Reward rooms never replace Origin, Boss, Treasure, Secret, Corrupted Chest, Wave, or Special Encounter rooms.\n" +
                "- Reward rooms are excluded from boss-key placement.\n\n" +
                "## Shape Policy\n" +
                "- Standard body-room placement favors smaller readable rooms.\n" +
                "- Default body shape weights are `1x1 30`, `2x1 25`, `1x2 20`, `2x2 15`, and `L 10`.\n" +
                "- `2x2` and L rooms remain present as medium-rarity layout texture.\n\n" +
                "## Runtime Behavior\n" +
                "- `BranchRoomRole.Reward` is a true non-combat room.\n" +
                "- On entry, Reward rooms immediately mark cleared and reward pending.\n" +
                "- Doors do not lock for Reward-room combat.\n" +
                "- Encounter planning skips Reward rooms, so enemies do not spawn even when reused templates contain enemy markers.\n" +
                "- Hazards, spikes, obstacles, decor, pickups, and reward/chest markers remain active.\n\n" +
                "## Reward Cache Roll\n" +
                "- `2%` Golden Chest.\n" +
                "- `30%` Normal Chest.\n" +
                "- `34%` loose coins.\n" +
                "- `24%` HP refill.\n" +
                "- `10%` nothing.\n" +
                "- Combat rooms keep the M52 standard-room sparse reward baseline.\n\n" +
                "## Interfaces\n" +
                "- Reuses `BranchRoomRole.Reward`.\n" +
                "- Adds `BranchPacingPolicy` constants for Reward-room count and body-room shape weights.\n" +
                "- Adds `ProceduralRewardResolver.RollM134RewardRoomCacheReward`.\n" +
                "- No save schema, reward schema, economy schema, chest-kind, biome, or room-template API changes.\n";
        }

        private static void AddDocsChecks(List<Milestone134BranchPacingRewardRoomShapePassCheck> checks)
        {
            var docs = Read(DocsPath);
            RequireAll(checks, "docs:m134-decisions", "Documentation", docs, new[]
            {
                "`1` guaranteed Reward room",
                "deterministic `50%`",
                "origin-to-boss-path",
                "`1x1`",
                "`30`",
                "`2x1`",
                "`25`",
                "`1x2`",
                "`20`",
                "`2x2`",
                "`15`",
                "`L`",
                "`10`",
                "`30%` Normal Chest",
                "Combat rooms keep the M52",
                "No save schema"
            });
        }

        private static void AddRuntimeChecks(List<Milestone134BranchPacingRewardRoomShapePassCheck> checks)
        {
            var generator = Read(BranchGeneratorPath);
            var featurePlan = Read(BranchFeaturePlanPath);
            var session = Read(BranchSessionControllerPath);
            var encounter = Read(EncounterResolverPath);
            var rewards = Read(ProceduralRewardResolverPath);

            RequireAll(checks, "runtime:pacing-policy", "Runtime", generator, new[]
            {
                "BranchPacingPolicy",
                "RewardRoomGuaranteedCount = 1",
                "RewardRoomSecondRollPercent = 50",
                "Single1x1Weight = 30",
                "Wide2x1Weight = 25",
                "Tall1x2Weight = 20",
                "Block2x2Weight = 15",
                "L3CellWeight = 10",
                "ChooseBodyFixtureId",
                "SelectRewardRoomTempIndices",
                "rewardTempIndices.Contains"
            });
            RequireAll(checks, "runtime:reward-noncombat", "Runtime", session + encounter, new[]
            {
                "BranchRoomRole.Reward or BranchRoomRole.Treasure",
                "State.CurrentRoom.MarkCleared()",
                "State.CurrentRoom.MarkRewardPending()",
                "BranchRoomRole.Origin or BranchRoomRole.Reward"
            });
            RequireAll(checks, "runtime:reward-not-boss-key", "Runtime", featurePlan, new[]
            {
                "room.Role == BranchRoomRole.Combat"
            });
            RequireAll(checks, "runtime:cache-roll", "Rewards", rewards, new[]
            {
                "RewardRoomCacheRollId",
                "RollM134RewardRoomCacheReward",
                "roll < 32",
                "roll < 66",
                "roll < 90",
                "room.Role == BranchRoomRole.Combat"
            });
        }

        private static void AddLivePolicyChecks(List<Milestone134BranchPacingRewardRoomShapePassCheck> checks)
        {
            try
            {
                var content = CreateContent(out var settings);
                var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath);
                var sawOneReward = false;
                var sawTwoRewards = false;
                var smallShapes = 0;
                var largeShapes = 0;

                for (var seed = 13400; seed < 13480; seed++)
                {
                    var graph = BranchGenerator.CreateSeededBranchFeatures(
                        content,
                        settings,
                        seed,
                        RoomBiomeIds.HollowThreshold,
                        enableCorruptedChestLeaf: true,
                        enableWaveRoomLeaf: true,
                        enableSpecialEncounterLeaf: true);
                    AccumulatePolicyEvidence(graph, ref sawOneReward, ref sawTwoRewards, ref smallShapes, ref largeShapes);

                    if (profile != null)
                    {
                        var directed = BranchGenerator.CreateDirectedEncounterBranch(
                            content,
                            settings,
                            profile,
                            worldIndex: 1,
                            seed: seed,
                            bossRoomAssetId: string.Empty,
                            biomeId: RoomBiomeIds.HollowThreshold,
                            enableCorruptedChestLeaf: true,
                            enableWaveRoomLeaf: true,
                            enableSpecialEncounterLeaf: true);
                        AccumulatePolicyEvidence(directed, ref sawOneReward, ref sawTwoRewards, ref smallShapes, ref largeShapes);
                    }
                }

                AddCheck(checks, "live:reward-count-policy", "Live", sawOneReward && sawTwoRewards, "Seed scan observed both one-Reward and two-Reward branches.");
                AddCheck(checks, "live:small-shape-majority", "Live", smallShapes > largeShapes, $"Small body rooms: {smallShapes}; large body rooms: {largeShapes}.");
            }
            catch (Exception exception)
            {
                AddCheck(checks, "live:policy-scan", "Live", false, exception.Message);
            }
        }

        private static void AccumulatePolicyEvidence(BranchFloorGraph graph, ref bool sawOneReward, ref bool sawTwoRewards, ref int smallShapes, ref int largeShapes)
        {
            var rewards = graph.Rooms.Where(room => room.Role == BranchRoomRole.Reward).ToArray();
            if (rewards.Length == 1)
            {
                sawOneReward = true;
            }
            else if (rewards.Length == 2)
            {
                sawTwoRewards = true;
            }

            var featurePlan = BranchFeaturePlan.Create(graph);
            if (rewards.Length < 1 || rewards.Length > 2 ||
                rewards.Any(room => featurePlan.BossKeyRoomId == room.Id.Value) ||
                rewards.Any(room => graph.ConnectionsFrom(room.Id).Select(connection => connection.ToRoomId).Distinct().Count() < 2))
            {
                throw new InvalidOperationException($"M134 Reward-room policy failed for seed {graph.Seed}.");
            }

            foreach (var room in graph.Rooms.Where(room => room.Role is BranchRoomRole.Combat or BranchRoomRole.Reward))
            {
                var shape = RoomFootprintShapeUtility.Classify(room.Footprint);
                if (shape is RoomFootprintShape.Single1x1 or RoomFootprintShape.Wide2x1 or RoomFootprintShape.Tall1x2)
                {
                    smallShapes++;
                }
                else if (shape is RoomFootprintShape.Block2x2 or RoomFootprintShape.L3Cell)
                {
                    largeShapes++;
                }
            }
        }

        private static BranchSessionContent CreateContent(out BranchGenerationSettingsDefinition settings)
        {
            settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            if (settings == null || catalog == null)
            {
                throw new InvalidOperationException("M134 policy scan requires generated branch settings and template catalog assets.");
            }

            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            return content;
        }

        private static void AddTestChecks(List<Milestone134BranchPacingRewardRoomShapePassCheck> checks)
        {
            var tests = Read(M134TestsPath);
            RequireAll(checks, "tests:m134-lock-tests", "Tests", tests, new[]
            {
                "NormalBranchesGetOneOrTwoMainRouteRewardRooms",
                "RewardRoomsSkipEncounterPlansAndBossKeyPlacement",
                "RewardRoomCacheRollUsesWoodenCacheBump",
                "ShapeWeightsFavorSmallAndMediumRooms",
                "GeneratedReportsArePresentPassingAndUseM134LockId",
                "ValidatorReportsGeneratedStateValid"
            });
        }

        private static void AddDependencyChecks(List<Milestone134BranchPacingRewardRoomShapePassCheck> checks)
        {
            var m133Report = Read(M133ReportPath);
            AddCheck(
                checks,
                "dependency:m133-passing-report",
                "Dependency",
                m133Report.Contains("- Result: PASSED") && m133Report.Contains(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.LockId),
                "M133 passing report exists and includes the M133 lock id.");
        }

        private static void RequireAll(List<Milestone134BranchPacingRewardRoomShapePassCheck> checks, string id, string category, string content, IEnumerable<string> needles)
        {
            var missing = needles.Where(needle => !content.Contains(needle)).ToArray();
            AddCheck(
                checks,
                id,
                category,
                missing.Length == 0,
                missing.Length == 0 ? "Required lock strings found." : $"Missing: {string.Join(", ", missing)}.");
        }

        private static void AddCheck(List<Milestone134BranchPacingRewardRoomShapePassCheck> checks, string id, string category, bool passed, string detail)
        {
            checks.Add(new Milestone134BranchPacingRewardRoomShapePassCheck
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
