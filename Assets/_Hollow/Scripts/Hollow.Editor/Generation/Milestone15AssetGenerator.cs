using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone15AssetGenerator
    {
        public const string SettingsPath = Milestone14AssetGenerator.BranchDataDirectory + "/BranchGenerationSettings_M15.asset";
        private const string BossEnemyPath = "Assets/_Hollow/Data/Enemies/Enemy_Boss.asset";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 15 Assets")]
        public static void Generate()
        {
            Milestone14AssetGenerator.Generate();
            Directory.CreateDirectory(Milestone14AssetGenerator.BranchDataDirectory);
            var settings = GenerateSettings();
            GenerateBossEnemy();
            AssignSettingsToGameScenes(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 15 seeded procedural branch settings, boss enemy, and scene wiring.");
        }

        private static BranchGenerationSettingsDefinition GenerateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<BranchGenerationSettingsDefinition>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            settings.Configure(
                BranchGenerator.DefaultSeededMacroSeed,
                nextTargetRoomCount: 8,
                nextMaxPlacementAttempts: 250,
                nextAllowLoops: false,
                nextEnableBossLeaf: true,
                new[]
                {
                    "combat_macro_single_1x1",
                    "combat_macro_wide_2x1",
                    "combat_macro_tall_1x2",
                    "combat_macro_block_2x2",
                    "combat_macro_l_3cell"
                });
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static void GenerateBossEnemy()
        {
            var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(BossEnemyPath);
            if (boss == null)
            {
                boss = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(boss, BossEnemyPath);
            }

            boss.Configure(
                "spawnEnemyBoss",
                "Stone Warden",
                EnemyArchetypeId.Boss,
                EnemyMovementMode.Grounded,
                14,
                0.75f,
                2,
                1f,
                0.55f,
                new Color(0.42f, 0.34f, 0.28f, 1f));
            EditorUtility.SetDirty(boss);

            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyCatalogPath);
            if (catalog == null)
            {
                throw new FileNotFoundException($"Missing enemy catalog at {EnemyCatalogPath}.");
            }

            var definitions = catalog.Definitions.Where(definition => definition != null && definition.SpawnKind != "spawnEnemyBoss").Concat(new[] { boss }).ToList();
            catalog.Configure(definitions, catalog.FallbackDefinition);
            EditorUtility.SetDirty(catalog);
        }

        private static void AssignSettingsToGameScenes(BranchGenerationSettingsDefinition settings)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                throw new FileNotFoundException($"Missing M14 macro room catalog at {Milestone14AssetGenerator.CatalogPath}.");
            }

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
