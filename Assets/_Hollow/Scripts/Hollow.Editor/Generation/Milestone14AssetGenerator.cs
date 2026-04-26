using System.IO;
using Hollow.Branches;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone14AssetGenerator
    {
        public const string BranchDataDirectory = "Assets/_Hollow/Data/Branches";
        public const string CatalogPath = BranchDataDirectory + "/BranchRoomTemplateCatalog_MacroFixtures.asset";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 14 Assets")]
        public static void Generate()
        {
            Milestone13AssetGenerator.Generate();
            Directory.CreateDirectory(BranchDataDirectory);
            var catalog = GenerateCatalog();
            AssignCatalogToGameScenes(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 14 seeded macro branch catalog and scene wiring.");
        }

        private static BranchRoomTemplateCatalogDefinition GenerateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BranchRoomTemplateCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Configure(
                LoadFixture("combat_macro_single_1x1"),
                LoadFixture("combat_macro_wide_2x1"),
                LoadFixture("combat_macro_tall_1x2"),
                LoadFixture("combat_macro_block_2x2"),
                LoadFixture("combat_macro_l_3cell"),
                BranchGenerator.DefaultMacroFixtureSeed);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static TextAsset LoadFixture(string roomId)
        {
            var path = $"{Milestone13AssetGenerator.MacroFixtureDirectory}/{roomId}.hollowruntime.json";
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (fixture == null)
            {
                throw new FileNotFoundException($"Missing M14 macro room fixture at {path}.");
            }

            return fixture;
        }

        private static void AssignCatalogToGameScenes(BranchRoomTemplateCatalogDefinition catalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureTemplateCatalog(catalog, BranchGenerator.DefaultMacroFixtureSeed);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
