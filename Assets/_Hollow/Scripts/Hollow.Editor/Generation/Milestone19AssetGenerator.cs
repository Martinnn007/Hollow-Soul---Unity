using System.Collections.Generic;
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
    public static class Milestone19AssetGenerator
    {
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M19";
        public const string EncounterCatalogPath = EncounterDirectory + "/EncounterCatalog_M19.asset";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 19 Assets")]
        public static void Generate()
        {
            Milestone18AssetGenerator.Generate();
            Directory.CreateDirectory(EncounterDirectory);
            var enemyCatalog = GenerateEnemyContent();
            var encounterCatalog = GenerateEncounterCatalog();
            AssignEncounterCatalogToGameScenes(encounterCatalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 19 enemy content, encounter catalog, and scene wiring. Enemy catalog: {enemyCatalog.Definitions.Count} definitions.");
        }

        private static EnemyCatalog GenerateEnemyContent()
        {
            var normal = SaveEnemy("Enemy_Normal.asset", "spawnEnemyNormal", "Normal Chaser", EnemyArchetypeId.Normal, EnemyBehaviorId.Chaser, EnemyMovementMode.Grounded, 3, 1.5f, 1, 1f, 0.32f, 4f, 1.4f, 1, 4f, 4f, 2f, "spawnEnemyNormal", 0, new Color(0.85f, 0.16f, 0.14f, 1f));
            var flying = SaveEnemy("Enemy_Flying.asset", "spawnEnemyFlying", "Flying Chaser", EnemyArchetypeId.Flying, EnemyBehaviorId.FlyingChaser, EnemyMovementMode.Flying, 3, 1.8f, 1, 1f, 0.32f, 4f, 1.4f, 1, 4f, 4f, 2f, "spawnEnemyNormal", 0, new Color(0.25f, 0.65f, 1f, 1f));
            var fast = SaveEnemy("Enemy_Fast.asset", "spawnEnemyFast", "Fast Chaser", EnemyArchetypeId.Fast, EnemyBehaviorId.Chaser, EnemyMovementMode.Grounded, 2, 2.4f, 1, 0.8f, 0.28f, 4f, 1.4f, 1, 4f, 4f, 2f, "spawnEnemyNormal", 0, new Color(1f, 0.66f, 0.18f, 1f));
            var heavy = SaveEnemy("Enemy_Heavy.asset", "spawnEnemyHeavy", "Heavy Chaser", EnemyArchetypeId.Heavy, EnemyBehaviorId.Chaser, EnemyMovementMode.Grounded, 6, 0.9f, 2, 1.1f, 0.42f, 4f, 1.4f, 1, 4f, 4f, 2f, "spawnEnemyNormal", 0, new Color(0.62f, 0.22f, 0.82f, 1f));
            var charger = SaveEnemy("Enemy_Charger.asset", "spawnEnemyCharger", "Ash Charger", EnemyArchetypeId.Fast, EnemyBehaviorId.Charger, EnemyMovementMode.Grounded, 4, 1.2f, 1, 0.9f, 0.34f, 5.5f, 1.2f, 1, 5f, 5.5f, 2f, "spawnEnemyNormal", 0, new Color(1f, 0.34f, 0.12f, 1f));
            var turret = SaveEnemy("Enemy_Turret.asset", "spawnEnemyTurret", "Bone Turret", EnemyArchetypeId.Heavy, EnemyBehaviorId.TurretShooter, EnemyMovementMode.Grounded, 4, 0f, 1, 1f, 0.34f, 8f, 1.35f, 1, 4.8f, 0f, 2f, "spawnEnemyNormal", 0, new Color(0.72f, 0.86f, 0.94f, 1f));
            var splitter = SaveEnemy("Enemy_Splitter.asset", "spawnEnemySplitter", "Husk Splitter", EnemyArchetypeId.Normal, EnemyBehaviorId.Splitter, EnemyMovementMode.Grounded, 5, 1.1f, 1, 1f, 0.36f, 4f, 1.4f, 1, 4f, 4f, 2f, "spawnEnemyNormal", 2, new Color(0.55f, 0.95f, 0.35f, 1f));
            var boss = SaveEnemy("Enemy_Boss.asset", "spawnEnemyBoss", "Stone Warden", EnemyArchetypeId.Boss, EnemyBehaviorId.BossWarden, EnemyMovementMode.Grounded, 14, 0.75f, 2, 1f, 0.55f, 6f, 1.35f, 1, 4.5f, 4.5f, 2.4f, "spawnEnemyNormal", 0, new Color(0.42f, 0.34f, 0.28f, 1f));

            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
                AssetDatabase.CreateAsset(catalog, EnemyCatalogPath);
            }

            catalog.Configure(new[] { normal, flying, fast, heavy, charger, turret, splitter, boss }, normal);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static EnemyDefinition SaveEnemy(
            string fileName,
            string spawnKind,
            string displayName,
            EnemyArchetypeId archetypeId,
            EnemyBehaviorId behaviorId,
            EnemyMovementMode movementMode,
            int health,
            float speed,
            int contactDamage,
            float contactCooldown,
            float radius,
            float attackRange,
            float attackCooldown,
            int projectileDamage,
            float projectileSpeed,
            float chargeSpeed,
            float chargeCooldown,
            string splitSpawnKind,
            int splitCount,
            Color color)
        {
            var path = $"Assets/_Hollow/Data/Enemies/{fileName}";
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(spawnKind, displayName, archetypeId, behaviorId, movementMode, health, speed, contactDamage, contactCooldown, radius, attackRange, attackCooldown, projectileDamage, projectileSpeed, chargeSpeed, chargeCooldown, splitSpawnKind, splitCount, color);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static EncounterCatalogDefinition GenerateEncounterCatalog()
        {
            var intro = SaveEncounter("Encounter_OriginIntro.asset", "origin_intro", "Origin Intro", BranchRoomRole.Origin, 0, 99, 1, 99, 1, new[]
            {
                new EncounterSpawnEntry("spawnEnemyNormal", 1),
                new EncounterSpawnEntry("spawnEnemyFast", 1)
            });
            var chargerPack = SaveEncounter("Encounter_ChargerPack.asset", "charger_pack", "Charger Pack", BranchRoomRole.Combat, 1, 99, 1, 99, 3, new[]
            {
                new EncounterSpawnEntry("spawnEnemyCharger", 1),
                new EncounterSpawnEntry("spawnEnemyNormal", 2),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var turretNest = SaveEncounter("Encounter_TurretNest.asset", "turret_nest", "Turret Nest", BranchRoomRole.Combat, 1, 99, 1, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyTurret", 1),
                new EncounterSpawnEntry("spawnEnemyFast", 1),
                new EncounterSpawnEntry("spawnEnemyNormal", 1)
            });
            var splitterHusk = SaveEncounter("Encounter_SplitterHusk.asset", "splitter_husk", "Splitter Husk", BranchRoomRole.Combat, 1, 99, 1, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemySplitter", 1),
                new EncounterSpawnEntry("spawnEnemyNormal", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var rewardGuard = SaveEncounter("Encounter_RewardGuard.asset", "reward_guard", "Reward Guard", BranchRoomRole.Reward, 1, 99, 1, 99, 3, new[]
            {
                new EncounterSpawnEntry("spawnEnemyTurret", 1),
                new EncounterSpawnEntry("spawnEnemyCharger", 1),
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

            catalog.Configure("m19_encounter_catalog_v1", new[] { intro, chargerPack, turretNest, splitterHusk, rewardGuard, boss }, boss);
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

        private static void AssignEncounterCatalogToGameScenes(EncounterCatalogDefinition catalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureEncounterCatalog(catalog);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
