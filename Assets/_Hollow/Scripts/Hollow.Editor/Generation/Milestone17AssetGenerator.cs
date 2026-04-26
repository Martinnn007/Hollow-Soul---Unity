using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone17AssetGenerator
    {
        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        private static readonly IReadOnlyList<string> FixtureIds = new[]
        {
            "combat_macro_single_1x1",
            "combat_macro_wide_2x1",
            "combat_macro_tall_1x2",
            "combat_macro_block_2x2",
            "combat_macro_l_3cell"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 17 Assets")]
        public static void Generate()
        {
            Milestone16AssetGenerator.Generate();
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            if (catalog == null)
            {
                throw new FileNotFoundException($"Missing branch room template catalog at {Milestone14AssetGenerator.CatalogPath}.");
            }

            if (settings == null)
            {
                throw new FileNotFoundException($"Missing branch generation settings at {Milestone15AssetGenerator.SettingsPath}.");
            }

            settings.Configure(
                BranchGenerator.DefaultSeededMacroSeed,
                nextTargetRoomCount: 8,
                nextMaxPlacementAttempts: 250,
                nextAllowLoops: false,
                nextEnableBossLeaf: true,
                nextEnableTreasureLeaf: true,
                nextAllowedFixtureIds: FixtureIds);
            EditorUtility.SetDirty(settings);
            AssignSettingsToGameScenes(catalog, settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 17 feature-branch settings and scene wiring.");
        }

        private static void AssignSettingsToGameScenes(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureTemplateCatalog(catalog, settings.DefaultSeed);
                branch.ConfigureGenerationSettings(settings);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
