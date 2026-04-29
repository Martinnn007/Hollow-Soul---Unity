using System;
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
    public static class Milestone46AssetGenerator
    {
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M46";
        public const string EncounterCatalogPath = EncounterDirectory + "/EncounterCatalog_M46.asset";
        public const string DirectorProfilePath = "Assets/_Hollow/Resources/Hollow/Branches/EncounterDirectorProfile_M46.asset";
        public const string ReportPath = "output/reports/m46_encounter_director_difficulty_curve_v1.md";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        public static IReadOnlyList<string> EncounterIds => new[]
        {
            "origin_intro",
            "skirmish_chasers",
            "lane_chargers",
            "turret_crossfire",
            "splitter_brood",
            "macro_mixup",
            "reward_guard",
            "reward_watchers",
            "reward_brood_guard",
            "world2_crossfire_mix",
            "world2_splitter_pressure",
            "world3_macro_pressure",
            "world3_reward_lockdown",
            "stone_warden_boss"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 46 Assets")]
        public static void Generate()
        {
            Milestone45AssetGenerator.Generate();
            Directory.CreateDirectory(EncounterDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DirectorProfilePath) ?? "Assets/_Hollow/Resources/Hollow/Branches");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var profile = GenerateDirectorProfile();
            var catalog = GenerateEncounterCatalog();
            AssignToGameScenes(catalog, profile);
            WriteReport(profile, catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 46 encounter director profile, encounter catalog, scene wiring, and report.");
        }

        private static EncounterDirectorProfileDefinition GenerateDirectorProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(DirectorProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<EncounterDirectorProfileDefinition>();
                AssetDatabase.CreateAsset(profile, DirectorProfilePath);
            }

            profile.ConfigureM46Defaults();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static EncounterCatalogDefinition GenerateEncounterCatalog()
        {
            var originIntro = SaveEncounter("Encounter_OriginIntro.asset", "origin_intro", "Origin Intro", BranchRoomRole.Origin, 0, 99, 1, 99, 1, new[]
            {
                new EncounterSpawnEntry("spawnEnemyNormal", 1),
                new EncounterSpawnEntry("spawnEnemyFast", 1)
            });
            var skirmish = SaveEncounter("Encounter_SkirmishChasers.asset", "skirmish_chasers", "Skirmish Chasers", BranchRoomRole.Combat, 1, 99, 1, 2, 3, new[]
            {
                new EncounterSpawnEntry("spawnEnemyNormal", 2),
                new EncounterSpawnEntry("spawnEnemyFast", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var laneChargers = SaveEncounter("Encounter_LaneChargers.asset", "lane_chargers", "Lane Chargers", BranchRoomRole.Combat, 1, 99, 2, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyCharger", 2),
                new EncounterSpawnEntry("spawnEnemyNormal", 2),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var turretCrossfire = SaveEncounter("Encounter_TurretCrossfire.asset", "turret_crossfire", "Turret Crossfire", BranchRoomRole.Combat, 1, 99, 2, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyTurret", 2),
                new EncounterSpawnEntry("spawnEnemyFast", 2),
                new EncounterSpawnEntry("spawnEnemyNormal", 1)
            });
            var splitterBrood = SaveEncounter("Encounter_SplitterBrood.asset", "splitter_brood", "Splitter Brood", BranchRoomRole.Combat, 2, 99, 1, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemySplitter", 2),
                new EncounterSpawnEntry("spawnEnemyNormal", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var macroMixup = SaveEncounter("Encounter_MacroMixup.asset", "macro_mixup", "Macro Mixup", BranchRoomRole.Combat, 2, 99, 3, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyHeavy", 1),
                new EncounterSpawnEntry("spawnEnemyCharger", 1),
                new EncounterSpawnEntry("spawnEnemyTurret", 1),
                new EncounterSpawnEntry("spawnEnemySplitter", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var rewardGuard = SaveEncounter("Encounter_RewardGuard.asset", "reward_guard", "Reward Guard", BranchRoomRole.Reward, 1, 99, 1, 99, 3, new[]
            {
                new EncounterSpawnEntry("spawnEnemyTurret", 1),
                new EncounterSpawnEntry("spawnEnemyCharger", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var rewardWatchers = SaveEncounter("Encounter_RewardWatchers.asset", "reward_watchers", "Reward Watchers", BranchRoomRole.Reward, 1, 99, 2, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyTurret", 2),
                new EncounterSpawnEntry("spawnEnemyNormal", 1),
                new EncounterSpawnEntry("spawnEnemyFast", 1)
            });
            var rewardBrood = SaveEncounter("Encounter_RewardBroodGuard.asset", "reward_brood_guard", "Reward Brood Guard", BranchRoomRole.Reward, 2, 99, 1, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemySplitter", 1),
                new EncounterSpawnEntry("spawnEnemyHeavy", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var world2Mix = SaveEncounter("Encounter_World2CrossfireMix.asset", "world2_crossfire_mix", "World 2 Crossfire Mix", BranchRoomRole.Combat, 2, 99, 1, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyTurret", 1),
                new EncounterSpawnEntry("spawnEnemyCharger", 1),
                new EncounterSpawnEntry("spawnEnemyNormal", 2),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var world2Splitter = SaveEncounter("Encounter_World2SplitterPressure.asset", "world2_splitter_pressure", "World 2 Splitter Pressure", BranchRoomRole.Combat, 2, 99, 1, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemySplitter", 2),
                new EncounterSpawnEntry("spawnEnemyFast", 1),
                new EncounterSpawnEntry("spawnEnemyNormal", 2)
            });
            var world3Macro = SaveEncounter("Encounter_World3MacroPressure.asset", "world3_macro_pressure", "World 3 Macro Pressure", BranchRoomRole.Combat, 3, 99, 2, 99, 3, new[]
            {
                new EncounterSpawnEntry("spawnEnemyHeavy", 1),
                new EncounterSpawnEntry("spawnEnemyCharger", 2),
                new EncounterSpawnEntry("spawnEnemyTurret", 1),
                new EncounterSpawnEntry("spawnEnemySplitter", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var world3Reward = SaveEncounter("Encounter_World3RewardLockdown.asset", "world3_reward_lockdown", "World 3 Reward Lockdown", BranchRoomRole.Reward, 3, 99, 1, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyHeavy", 1),
                new EncounterSpawnEntry("spawnEnemyTurret", 2),
                new EncounterSpawnEntry("spawnEnemySplitter", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var boss = SaveEncounter("Encounter_StoneWarden.asset", "stone_warden_boss", "Stone Warden", BranchRoomRole.Boss, 1, 99, 1, 99, 1, new[]
            {
                new EncounterSpawnEntry("spawnEnemyBoss", 1)
            });

            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(EncounterCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EncounterCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, EncounterCatalogPath);
            }

            catalog.Configure(
                "m46_encounter_director_catalog_v1",
                new[] { originIntro, skirmish, laneChargers, turretCrossfire, splitterBrood, macroMixup, rewardGuard, rewardWatchers, rewardBrood, world2Mix, world2Splitter, world3Macro, world3Reward, boss },
                boss);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static EncounterDefinition SaveEncounter(
            string fileName,
            string encounterId,
            string displayName,
            BranchRoomRole role,
            int minDifficultyBand,
            int maxDifficultyBand,
            int minFootprintCells,
            int maxFootprintCells,
            int weight,
            IEnumerable<EncounterSpawnEntry> spawns)
        {
            var path = $"{EncounterDirectory}/{fileName}";
            var encounter = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(path);
            if (encounter == null)
            {
                encounter = ScriptableObject.CreateInstance<EncounterDefinition>();
                AssetDatabase.CreateAsset(encounter, path);
            }

            encounter.Configure(encounterId, displayName, role, minDifficultyBand, maxDifficultyBand, minFootprintCells, maxFootprintCells, weight, spawns);
            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static void AssignToGameScenes(EncounterCatalogDefinition catalog, EncounterDirectorProfileDefinition profile)
        {
            var roomCatalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            if (roomCatalog == null || settings == null)
            {
                throw new FileNotFoundException("M46 requires the M14 room catalog and M15 branch generation settings.");
            }

            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureTemplateCatalog(roomCatalog, settings.DefaultSeed);
                branch.ConfigureGenerationSettings(settings);
                branch.ConfigureEncounterCatalog(catalog);
                branch.ConfigureEncounterDirectorProfile(profile);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteReport(EncounterDirectorProfileDefinition profile, EncounterCatalogDefinition catalog)
        {
            File.WriteAllText(
                ReportPath,
                "# M46 Encounter Director + Difficulty Curve V1\n\n" +
                $"- Branch identity: `{BranchGenerator.DirectedEncounterBranchId}`.\n" +
                "- World room counts: W1 `8`, W2 `10`, W3 `12`.\n" +
                $"- Max non-boss requested enemies: `{profile.MaxNonBossEnemySpawns}`.\n" +
                $"- Encounter catalog: `{catalog.CatalogId}` with `{catalog.Encounters.Count}` templates.\n" +
                "- Difficulty increases through seeded weighted encounter composition only; enemy stats are not scaled.\n" +
                "- Boss remains the Stone Warden encounter.\n");
        }
    }
}
