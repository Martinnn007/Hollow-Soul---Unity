using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone31Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone31AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone31Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone26PlayerBuildFoundationTests.cs",
            "Docs/Milestone31ValidationDebtRecovery.md"
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 31 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M31 baseline file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var content = LoadContent(catalog, settings, failures);
            if (content != null && settings != null)
            {
                ValidateMacroFixtureGraph(content, failures);
                ValidateSeededMacroGraph(content, settings, failures);
                ValidateFeatureGraph(content, settings, failures);
            }

            ValidateRewardPoolCompatibility(failures);
            ValidateScenes(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 31 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static BranchSessionContent LoadContent(
            BranchRoomTemplateCatalogDefinition catalog,
            BranchGenerationSettingsDefinition settings,
            List<string> failures)
        {
            if (catalog == null || settings == null)
            {
                failures.Add("M31 baseline requires M14 catalog and M15 generation settings.");
                return null;
            }

            var sample = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "missing sample room";
            if (sample == null || !HollowRuntimeV2Importer.TryImport(sample.text, out var sampleRoom, out importError))
            {
                failures.Add($"M31 could not import legacy sample room: {importError}");
                return null;
            }

            var content = BranchSessionContent.Create(sampleRoom, catalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M31 branch content import error: {contentError}");
                return null;
            }

            return content;
        }

        private static void ValidateMacroFixtureGraph(BranchSessionContent content, List<string> failures)
        {
            var graph = BranchGenerator.CreateMacroFixtureBranch(content.MacroRoomPool, BranchGenerator.DefaultMacroFixtureSeed);
            if (graph.BranchId != BranchGenerator.MacroFixtureBranchId || graph.RoomCount != 5)
            {
                failures.Add("M31 baseline expects the M14 macro fixture graph to keep five logical rooms.");
            }

            if (graph.OccupancyMap.OwnerByCell.Count != 12 || HasFootprintOverlap(graph))
            {
                failures.Add("M31 baseline expects M14 macro fixture rooms to occupy twelve non-overlapping branch cells.");
            }

            if (!HasPortConnection(graph, BranchRoomId.Origin, "north_0", BranchRoomId.North) ||
                !HasPortConnection(graph, BranchRoomId.Origin, "south_0", BranchRoomId.South) ||
                !HasPortConnection(graph, BranchRoomId.Origin, "east_0", BranchRoomId.East) ||
                !HasPortConnection(graph, BranchRoomId.Origin, "west_0", BranchRoomId.West))
            {
                failures.Add("M31 baseline expects M14 origin ports to preserve the four primary macro branch exits.");
            }

            if (graph.Connections.Any(connection => !connection.HasExplicitPorts) || HasDuplicatePortPair(graph))
            {
                failures.Add("M31 baseline expects M14 macro connections to be explicit and duplicate-free.");
            }
        }

        private static void ValidateSeededMacroGraph(BranchSessionContent content, BranchGenerationSettingsDefinition settings, List<string> failures)
        {
            var graph = BranchGenerator.CreateSeededMacroBranch(content, settings, BranchGenerator.DefaultSeededMacroSeed);
            if (graph.BranchId != BranchGenerator.SeededMacroBranchId || graph.RoomCount != 8)
            {
                failures.Add("M31 baseline expects M15 seeded macro generation to keep eight rooms and the M15 branch identity.");
            }

            ValidateCommonGeneratedGraph(graph, failures, "M15");
            if (graph.Rooms.Count(room => room.Role == BranchRoomRole.Boss) != 1)
            {
                failures.Add("M31 baseline expects M15 seeded macro generation to keep exactly one boss room.");
            }
        }

        private static void ValidateFeatureGraph(BranchSessionContent content, BranchGenerationSettingsDefinition settings, List<string> failures)
        {
            var graph = BranchGenerator.CreateSeededFeatureBranch(content, settings, BranchGenerator.DefaultSeededMacroSeed);
            if (graph.BranchId != BranchGenerator.FeatureBranchId || graph.RoomCount != 8)
            {
                failures.Add("M31 baseline expects M17 feature generation to keep eight rooms and the M17 branch identity.");
            }

            ValidateCommonGeneratedGraph(graph, failures, "M17");
            var treasure = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Treasure);
            var rewards = ProceduralRewardResolver.CreatePlan(graph);
            if (treasure == null || !rewards.TryResolve(treasure.Id.Value, out var treasureReward) || treasureReward.RewardId != "treasure_cache")
            {
                failures.Add("M31 baseline expects M17 feature generation to keep one treasure room with the Treasure Cache reward.");
            }
        }

        private static void ValidateCommonGeneratedGraph(BranchFloorGraph graph, List<string> failures, string label)
        {
            if (graph == null)
            {
                failures.Add($"M31 {label} graph is null.");
                return;
            }

            if (HasFootprintOverlap(graph))
            {
                failures.Add($"M31 {label} graph contains overlapping macro footprint cells.");
            }

            if (graph.Connections.Count < (graph.RoomCount - 1) * 2 || graph.Connections.Any(connection => !connection.HasExplicitPorts))
            {
                failures.Add($"M31 {label} graph must keep explicit reachable port connections.");
            }

            if (!IsGraphConnected(graph))
            {
                failures.Add($"M31 {label} graph must keep all rooms reachable from origin.");
            }

            if (!BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError))
            {
                failures.Add($"M31 {label} special-room topology is invalid: {topologyError}");
            }
        }

        private static void ValidateRewardPoolCompatibility(List<string> failures)
        {
            var standard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.StandardRewardPoolPath);
            var treasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.TreasureRewardPoolPath);
            var boss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.BossRewardPoolPath);
            if (!IsRewardPoolUsable(standard, minRewards: 6, requiredRarity: null) ||
                !IsRewardPoolUsable(treasure, minRewards: 1, requiredRarity: RewardRarity.Treasure) ||
                !IsRewardPoolUsable(boss, minRewards: 1, requiredRarity: RewardRarity.Boss))
            {
                failures.Add("M31 baseline expects successor M28/M30 reward pools to remain M18-compatible.");
            }
        }

        private static void ValidateScenes(List<string> failures)
        {
            foreach (var scenePath in GameScenes)
            {
                EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (!IsRewardPoolUsable(branch.StandardRewardPool, minRewards: 6, requiredRarity: null) ||
                    !IsRewardPoolUsable(branch.TreasureRewardPool, minRewards: 1, requiredRarity: RewardRarity.Treasure) ||
                    !IsRewardPoolUsable(branch.BossRewardPool, minRewards: 1, requiredRarity: RewardRarity.Boss))
                {
                    failures.Add($"{scenePath} BranchSessionController is missing M18-compatible successor reward pools.");
                }
            }
        }

        private static bool HasPortConnection(BranchFloorGraph graph, BranchRoomId from, string portId, BranchRoomId to)
        {
            return graph.TryGetConnectionByPort(from, portId, out var connection) && connection.ToRoomId == to;
        }

        private static bool HasFootprintOverlap(BranchFloorGraph graph)
        {
            return graph.OccupancyMap.OwnerByCell.Count != graph.Rooms.Sum(room => room.Footprint?.OccupiedCellCount ?? 0);
        }

        private static bool HasDuplicatePortPair(BranchFloorGraph graph)
        {
            return graph.Connections
                .GroupBy(connection => $"{connection.FromRoomId.Value}:{connection.FromPortId}->{connection.ToRoomId.Value}:{connection.ToPortId}")
                .Any(group => group.Count() > 1);
        }

        private static bool IsGraphConnected(BranchFloorGraph graph)
        {
            var visited = new HashSet<BranchRoomId>();
            var queue = new Queue<BranchRoomId>();
            queue.Enqueue(BranchRoomId.Origin);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current))
                {
                    continue;
                }

                foreach (var connection in graph.ConnectionsFrom(current))
                {
                    if (!visited.Contains(connection.ToRoomId))
                    {
                        queue.Enqueue(connection.ToRoomId);
                    }
                }
            }

            return visited.Count == graph.RoomCount;
        }

        private static bool IsRewardPoolUsable(RewardPoolDefinition pool, int minRewards, RewardRarity? requiredRarity)
        {
            if (pool == null || pool.Rewards.Count < minRewards)
            {
                return false;
            }

            if (pool.Rewards.Any(reward => reward == null || string.IsNullOrWhiteSpace(reward.RewardId) || string.IsNullOrWhiteSpace(reward.DisplayName)))
            {
                return false;
            }

            return !requiredRarity.HasValue || pool.Rewards.Any(reward => reward != null && reward.Rarity == requiredRarity.Value);
        }
    }
}
