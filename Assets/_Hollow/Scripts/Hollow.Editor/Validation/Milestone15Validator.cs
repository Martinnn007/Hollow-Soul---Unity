using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
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
    public static class Milestone15Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/BranchGenerationSettingsDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/BranchRoomRole.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/ProceduralRewardResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/ProceduralRewardPlan.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone15AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone15Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone15SeededProceduralBranchTests.cs",
            "Docs/Milestone15SeededProceduralBranches.md",
            Milestone15AssetGenerator.SettingsPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 15 Validation")]
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
                    failures.Add($"Missing M15 file: {file}");
                }
            }

            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            ValidateSettings(settings, failures);
            ValidateBossEnemy(failures);
            ValidateGraph(catalog, settings, failures);
            ValidateScenes(catalog, settings, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 15 validation passed.");
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
                failures.Add("M15 branch generation settings asset is missing.");
                return;
            }

            if (settings.DefaultSeed != BranchGenerator.DefaultSeededMacroSeed ||
                settings.TargetRoomCount != 8 ||
                settings.MaxPlacementAttempts != 250 ||
                settings.AllowLoops ||
                !settings.EnableBossLeaf)
            {
                failures.Add("M15 branch generation settings do not match the milestone defaults.");
            }
        }

        private static void ValidateBossEnemy(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>("Assets/_Hollow/Data/Enemies/EnemyCatalog.asset");
            var boss = catalog != null ? catalog.Resolve("spawnEnemyBoss") : null;
            if (boss == null ||
                boss.ArchetypeId != EnemyArchetypeId.Boss ||
                boss.MaxHealth != 14 ||
                boss.ContactDamage != 2 ||
                boss.RadiusMeters < 0.54f)
            {
                failures.Add("M15 enemy catalog must contain the Stone Warden boss definition.");
            }
        }

        private static void ValidateGraph(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, List<string> failures)
        {
            if (catalog == null || settings == null)
            {
                failures.Add("M15 cannot validate graph without catalog and settings assets.");
                return;
            }

            var legacy = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "missing legacy sample room JSON";
            ImportedRoomRuntimeAsset legacyAsset = null;
            if (legacy == null || !HollowRuntimeV2Importer.TryImport(legacy.text, out legacyAsset, out importError))
            {
                failures.Add($"M15 could not import legacy sample room: {importError}");
                return;
            }

            var content = BranchSessionContent.Create(legacyAsset, catalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M15 content import warning: {contentError}");
            }

            var graph = BranchGenerator.CreateSeededMacroBranch(content, settings, settings.DefaultSeed);
            if (graph.BranchId != BranchGenerator.SeededMacroBranchId ||
                graph.Seed != BranchGenerator.DefaultSeededMacroSeed ||
                graph.RoomCount != 8 ||
                graph.Connections.Count != 14 ||
                graph.Connections.Any(connection => !connection.HasExplicitPorts))
            {
                failures.Add("M15 seeded graph must have eight rooms, fourteen directed explicit-port connections, and the M15 branch identity.");
            }

            if (graph.OccupancyMap.OwnerByCell.Count != graph.Rooms.Sum(room => room.Footprint?.OccupiedCellCount ?? 0))
            {
                failures.Add("M15 seeded graph contains overlapping occupied branch cells.");
            }

            if (graph.Rooms.Count(room => room.Role == BranchRoomRole.Boss) != 1 ||
                graph.Rooms.Count(room => room.Role == BranchRoomRole.Origin) != 1)
            {
                failures.Add("M15 seeded graph must contain one origin and one boss room.");
            }

            var boss = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Boss);
            if (boss == null || graph.ConnectionsFrom(boss.Id).Count != 1)
            {
                failures.Add("M15 boss room must be a leaf room.");
            }

            var rewards = ProceduralRewardResolver.CreatePlan(graph);
            if (rewards.Rewards.Count != 7 || !rewards.TryResolve("boss_01", out var bossReward) || bossReward.Souls != 25)
            {
                failures.Add("M15 procedural reward plan must include six standard room rewards and the boss reward.");
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
                    failures.Add($"{scenePath} BranchSessionController is not wired to the M15 generation settings.");
                }
            }
        }
    }
}
