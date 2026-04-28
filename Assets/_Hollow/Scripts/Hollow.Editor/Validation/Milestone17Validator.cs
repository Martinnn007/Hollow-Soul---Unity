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
    public static class Milestone17Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone17AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone17Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone17FeatureBranchTests.cs",
            "Docs/Milestone17FeatureBranchTreasureRooms.md"
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 17 Validation")]
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
                    failures.Add($"Missing M17 file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            ValidateSettings(settings, failures);
            ValidateFeatureGraph(catalog, settings, failures);
            ValidateScenes(catalog, settings, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 17 validation passed.");
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

        private static void ValidateSettings(BranchGenerationSettingsDefinition settings, List<string> failures)
        {
            if (settings == null)
            {
                failures.Add("M17 branch generation settings asset is missing.");
                return;
            }

            if (settings.DefaultSeed != BranchGenerator.DefaultSeededMacroSeed ||
                settings.TargetRoomCount != 8 ||
                settings.MaxPlacementAttempts != 250 ||
                settings.AllowLoops ||
                !settings.EnableBossLeaf ||
                !settings.EnableTreasureLeaf)
            {
                failures.Add("M17 branch generation settings must enable boss and treasure leaves while preserving M15 room-count defaults.");
            }
        }

        private static void ValidateFeatureGraph(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, List<string> failures)
        {
            if (catalog == null || settings == null)
            {
                failures.Add("M17 cannot validate graph without catalog and settings assets.");
                return;
            }

            var sample = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "missing legacy sample room JSON";
            if (sample == null || !HollowRuntimeV2Importer.TryImport(sample.text, out var sampleRoom, out importError))
            {
                failures.Add($"M17 could not import legacy sample room: {importError}");
                return;
            }

            var content = BranchSessionContent.Create(sampleRoom, catalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M17 branch content import error: {contentError}");
                return;
            }

            var graph = BranchGenerator.CreateSeededFeatureBranch(content, settings, settings.DefaultSeed);
            if (graph.BranchId != BranchGenerator.FeatureBranchId ||
                graph.RoomCount != settings.TargetRoomCount ||
                graph.Connections.Count < (settings.TargetRoomCount - 1) * 2 ||
                graph.Connections.Any(connection => !connection.HasExplicitPorts))
            {
                failures.Add("M17 feature graph must keep eight rooms, explicit port connections, and the M17 branch identity.");
            }

            if (graph.OccupancyMap.OwnerByCell.Count != graph.Rooms.Sum(room => room.Footprint?.OccupiedCellCount ?? 0))
            {
                failures.Add("M17 feature graph contains overlapping occupied branch cells.");
            }

            if (graph.Rooms.Count(room => room.Role == BranchRoomRole.Boss) != 1 ||
                graph.Rooms.Count(room => room.Role == BranchRoomRole.Treasure) != 1 ||
                graph.Rooms.Count(room => room.Role == BranchRoomRole.Origin) != 1)
            {
                failures.Add("M17 feature graph must contain one origin, one treasure room, and one boss room.");
            }

            var treasure = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Treasure);
            if (treasure == null)
            {
                failures.Add("M17 feature graph must select one treasure room.");
            }

            if (!BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError))
            {
                failures.Add($"M17 special-room topology is invalid: {topologyError}");
            }

            if (!IsGraphConnected(graph))
            {
                failures.Add("M17 feature graph must keep every generated room reachable from origin.");
            }

            var rewards = ProceduralRewardResolver.CreatePlan(graph);
            if (rewards.Rewards.Count != 7 ||
                treasure == null ||
                !rewards.TryResolve(treasure.Id.Value, out var treasureReward) ||
                treasureReward.RewardId != "treasure_cache" ||
                treasureReward.Souls != 15)
            {
                failures.Add("M17 reward plan must include one 15-soul Treasure Cache reward.");
            }
        }

        private static void ValidateScenes(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, List<string> failures)
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

                if (branch.BranchRoomTemplateCatalog != catalog ||
                    branch.BranchGenerationSettings != settings ||
                    branch.MacroBranchSeed != BranchGenerator.DefaultSeededMacroSeed)
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to M17 feature-branch settings.");
                }
            }
        }

        private static bool IsGraphConnected(BranchFloorGraph graph)
        {
            if (graph == null || graph.RoomCount == 0)
            {
                return false;
            }

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
    }
}
