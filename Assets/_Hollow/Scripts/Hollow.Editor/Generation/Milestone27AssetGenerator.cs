using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone27AssetGenerator
    {
        public const string WeaponDirectory = "Assets/_Hollow/Data/Weapons/M27";
        public const string WeaponCatalogPath = WeaponDirectory + "/WeaponCatalog_M27.asset";
        public const string WeaponRewardPoolPath = WeaponDirectory + "/WeaponRewardPool_M27.asset";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 27 Assets")]
        public static void Generate()
        {
            Milestone25AssetGenerator.Generate();
            Directory.CreateDirectory(WeaponDirectory);

            var starterBlade = SaveWeapon(
                "Weapon_StarterBlade.asset",
                "starter_blade",
                "Practice Blade",
                WeaponSlot.Melee,
                WeaponCategory.Blade,
                new[] { BuildTag.Melee, BuildTag.Fast },
                new WeaponAttackDefinition(AttackKind.Light, 1, 0.67f, 6f, 0.95f),
                new WeaponAttackDefinition(AttackKind.Heavy, 2, 3.5f, 40f, 1.15f));
            var starterBolt = SaveWeapon(
                "Weapon_StarterBolt.asset",
                "starter_bolt",
                "Practice Bolt",
                WeaponSlot.Ranged,
                WeaponCategory.Wand,
                new[] { BuildTag.Ranged, BuildTag.Magic },
                new WeaponAttackDefinition(AttackKind.Light, 1, 1f, 0f, 6f),
                new WeaponAttackDefinition(AttackKind.Heavy, 2, 10f, 35f, 6f));
            var starterBow = SaveWeapon(
                "Weapon_StarterBow.asset",
                "starter_bow",
                "Practice Bow",
                WeaponSlot.Ranged,
                WeaponCategory.Bow,
                new[] { BuildTag.Ranged, BuildTag.Fast },
                new WeaponAttackDefinition(AttackKind.Light, 1, 1f, 2f, 6.5f, ImpactForceClass.Light, 0.32f, 0.01f, 0.03f, 0.16f, 1f, 1f),
                new WeaponAttackDefinition(AttackKind.Heavy, 2, 8f, 28f, 6.75f, ImpactForceClass.Medium, 0.55f, 0.01f, 0.04f, 0.32f, 1f, 1.35f));
            var ironCleaver = SaveWeapon(
                "Weapon_IronCleaver.asset",
                "iron_cleaver",
                "Iron Cleaver",
                WeaponSlot.Melee,
                WeaponCategory.Blade,
                new[] { BuildTag.Melee, BuildTag.Heavy },
                new WeaponAttackDefinition(AttackKind.Light, 2, 0.85f, 9f, 1.05f),
                new WeaponAttackDefinition(AttackKind.Heavy, 4, 3.15f, 42f, 1.35f));
            var emberBolt = SaveWeapon(
                "Weapon_EmberBolt.asset",
                "ember_bolt",
                "Ember Bolt",
                WeaponSlot.Ranged,
                WeaponCategory.Wand,
                new[] { BuildTag.Ranged, BuildTag.Fire, BuildTag.Magic },
                new WeaponAttackDefinition(AttackKind.Light, 2, 0.85f, 4f, 6.33f),
                new WeaponAttackDefinition(AttackKind.Heavy, 4, 8f, 38f, 5.67f));

            var catalog = SaveCatalog(new[] { starterBlade, starterBolt, starterBow, ironCleaver, emberBolt });
            var weaponRewardPool = SaveWeaponRewardPool(ironCleaver, emberBolt);
            AssignToGameScenes(catalog, weaponRewardPool);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 27 weapon catalog, rare weapon rewards, and scene wiring.");
        }

        private static WeaponDefinition SaveWeapon(
            string fileName,
            string weaponId,
            string displayName,
            WeaponSlot slot,
            WeaponCategory category,
            IEnumerable<BuildTag> tags,
            WeaponAttackDefinition lightAttack,
            WeaponAttackDefinition heavyAttack)
        {
            var path = $"{WeaponDirectory}/{fileName}";
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(weaponId, displayName, slot, category, tags, lightAttack, heavyAttack);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static WeaponCatalogDefinition SaveCatalog(IEnumerable<WeaponDefinition> weapons)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(WeaponCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WeaponCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, WeaponCatalogPath);
            }

            catalog.Configure("m27_weapon_catalog_v1", weapons);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static RewardPoolDefinition SaveWeaponRewardPool(WeaponDefinition meleeWeapon, WeaponDefinition rangedWeapon)
        {
            var meleeReward = SaveWeaponReward("Reward_IronCleaver.asset", meleeWeapon, RewardRarity.Rare);
            var rangedReward = SaveWeaponReward("Reward_EmberBolt.asset", rangedWeapon, RewardRarity.Rare);
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(WeaponRewardPoolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<RewardPoolDefinition>();
                AssetDatabase.CreateAsset(pool, WeaponRewardPoolPath);
            }

            pool.Configure("m27_rare_weapon_rewards", new[] { meleeReward, rangedReward });
            EditorUtility.SetDirty(pool);
            return pool;
        }

        private static RewardDefinition SaveWeaponReward(string fileName, WeaponDefinition weapon, RewardRarity rarity)
        {
            var path = $"{WeaponDirectory}/{fileName}";
            var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (reward == null)
            {
                reward = ScriptableObject.CreateInstance<RewardDefinition>();
                AssetDatabase.CreateAsset(reward, path);
            }

            reward.Configure(weapon.WeaponId, weapon.DisplayName, RewardKind.Weapon, rarity, 0, 0, System.Array.Empty<RewardEffect>());
            EditorUtility.SetDirty(reward);
            return reward;
        }

        private static void AssignToGameScenes(WeaponCatalogDefinition catalog, RewardPoolDefinition weaponRewardPool)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureWeaponCatalog(catalog);
                branch.ConfigureWeaponRewardPool(weaponRewardPool);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
