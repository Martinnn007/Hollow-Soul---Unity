using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone16AssetGenerator
    {
        public const string ApprovedRoomDirectory = "Assets/_Hollow/Data/Rooms/DesignerApproved";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 16 Assets")]
        public static void Generate()
        {
            Milestone15AssetGenerator.Generate();
            Directory.CreateDirectory(ApprovedRoomDirectory);
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            if (catalog == null)
            {
                throw new FileNotFoundException($"Missing branch room template catalog at {Milestone14AssetGenerator.CatalogPath}.");
            }

            if (settings == null)
            {
                throw new FileNotFoundException($"Missing M15 branch generation settings at {Milestone15AssetGenerator.SettingsPath}.");
            }

            var approvedTemplates = LoadApprovedTemplates();
            catalog.Configure(
                catalog.Single1x1,
                catalog.Wide2x1,
                catalog.Tall1x2,
                catalog.Block2x2,
                catalog.L3Cell,
                catalog.DefaultSeed,
                approvedTemplates);
            EditorUtility.SetDirty(catalog);
            AssignCatalogAndSettingsToGameScenes(catalog, settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 16 approved designer room catalog with {approvedTemplates.Length} approved templates.");
        }

        public static TextAsset[] LoadApprovedTemplates()
        {
            Directory.CreateDirectory(ApprovedRoomDirectory);
            return AssetDatabase.FindAssets("t:TextAsset", new[] { ApprovedRoomDirectory })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".hollowruntime.json", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path)
                .Select(AssetDatabase.LoadAssetAtPath<TextAsset>)
                .Where(asset => asset != null)
                .ToArray();
        }

        private static void AssignCatalogAndSettingsToGameScenes(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings)
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
