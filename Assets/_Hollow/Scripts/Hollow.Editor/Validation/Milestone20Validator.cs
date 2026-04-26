using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone20Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Branches/BranchFeaturePlan.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/BossKeyPickup.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/HubShopOffer.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/InterBranchHubState.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/HubShopController.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/NextBranchChoice.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/NextBranchPortal.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone20AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone20Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone20BranchFeaturesTests.cs",
            "Docs/Milestone20BranchFeaturesShopsSecretsKeys.md",
            Milestone20AssetGenerator.BossKeyPickupPrefabPath,
            Milestone20AssetGenerator.HubShopPrefabPath,
            Milestone20AssetGenerator.NextBranchPortalPrefabPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 20 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: Application.isBatchMode);
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M20 file: {file}");
                }
            }

            ValidateGraph(failures);
            ValidateScenes(failures);
            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 20 validation passed.");
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

        private static void ValidateGraph(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "missing sample room";
            if (catalog == null || settings == null || sample == null || !HollowRuntimeV2Importer.TryImport(sample.text, out var sampleRoom, out importError))
            {
                failures.Add($"M20 could not import branch content: {importError}");
                return;
            }

            var content = BranchSessionContent.Create(sampleRoom, catalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M20 branch content import error: {contentError}");
                return;
            }

            var graph = BranchGenerator.CreateSeededBranchFeatures(content, settings, settings.DefaultSeed);
            if (graph.BranchId != BranchGenerator.BranchFeaturesId)
            {
                failures.Add("M20 graph must use m20_branch_features_v1.");
            }

            if (graph.Rooms.Count(room => room.Role == BranchRoomRole.Secret) != 1)
            {
                failures.Add("M20 graph must contain exactly one secret room.");
            }

            var boss = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Boss);
            if (boss == null)
            {
                failures.Add("M20 graph must contain one boss room.");
            }
            else if (!graph.Connections.Any(connection => connection.ToRoomId == boss.Id && connection.LockKind == BranchConnectionLockKind.BossKey))
            {
                failures.Add("M20 graph must lock the connection into the boss room with BossKey.");
            }

            var plan = BranchFeaturePlan.Create(graph);
            if (!plan.HasBossKeyRoom)
            {
                failures.Add("M20 feature plan must select a boss-key source room.");
            }
        }

        private static void ValidateScenes(List<string> failures)
        {
            var bossKey = AssetDatabase.LoadAssetAtPath<GameObject>(Milestone20AssetGenerator.BossKeyPickupPrefabPath);
            var shop = AssetDatabase.LoadAssetAtPath<GameObject>(Milestone20AssetGenerator.HubShopPrefabPath);
            var portal = AssetDatabase.LoadAssetAtPath<GameObject>(Milestone20AssetGenerator.NextBranchPortalPrefabPath);
            foreach (var scenePath in GameScenes)
            {
                EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (branch.BossKeyPickupPrefab != bossKey || branch.HubShopPrefab != shop || branch.NextBranchPortalPrefab != portal)
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to M20 branch feature prefabs.");
                }
            }
        }
    }
}
