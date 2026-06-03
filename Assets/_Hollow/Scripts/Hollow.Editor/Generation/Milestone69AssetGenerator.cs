using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone69AssetGenerator
    {
        public const string EquipmentDirectory = "Assets/_Hollow/Data/Equipment/M69";
        public const string RewardDirectory = "Assets/_Hollow/Data/Rewards/M69";
        public const string ShieldCatalogPath = EquipmentDirectory + "/ShieldCatalog_M69.asset";
        public const string TreasureRewardPoolPath = RewardDirectory + "/TreasureRewardPool_M69.asset";
        public const string BossRewardPoolPath = RewardDirectory + "/BossRewardPool_M69.asset";
        public const string DocsPath = "Docs/Milestone69EquipmentLoadShieldsStability.md";
        public const string ReportPath = "output/reports/m69_equipment_load_shields_stability.md";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 69 Assets")]
        public static void Generate()
        {
            Milestone54AssetGenerator.Generate();
            Directory.CreateDirectory(EquipmentDirectory);
            Directory.CreateDirectory(RewardDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");

            RetuneWeaponLoadClasses();
            RetuneArmorLoadClasses();

            var starterBuckler = SaveShield(
                "Shield_StarterBuckler.asset",
                ShieldDefinition.StarterShieldId,
                "Starter Buckler",
                ArmorRarity.Common,
                EquipmentLoadClass.Light);
            var ironKiteShield = SaveShield(
                "Shield_IronKite.asset",
                "iron_kite_shield",
                "Iron Kite Shield",
                ArmorRarity.Uncommon,
                EquipmentLoadClass.Medium);
            var stoneWallShield = SaveShield(
                "Shield_StoneWall.asset",
                "stone_wall_shield",
                "Stone Wall Shield",
                ArmorRarity.Rare,
                EquipmentLoadClass.Heavy);

            var shieldCatalog = SaveShieldCatalog(new[] { starterBuckler, ironKiteShield, stoneWallShield });
            var shieldRewards = new[]
            {
                SaveShieldReward("Reward_IronKiteShield.asset", ironKiteShield, RewardRarity.Uncommon),
                SaveShieldReward("Reward_StoneWallShield.asset", stoneWallShield, RewardRarity.Rare)
            };

            var standardPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone52AssetGenerator.StandardRewardPoolPath);
            var treasurePool = SavePool(
                TreasureRewardPoolPath,
                "m69_treasure_equipment_load_rewards",
                LoadRewards(Milestone54AssetGenerator.TreasureRewardPoolPath).Concat(shieldRewards));
            var bossPool = SavePool(
                BossRewardPoolPath,
                "m69_boss_equipment_load_rewards",
                LoadRewards(Milestone54AssetGenerator.BossRewardPoolPath).Concat(shieldRewards));

            AssignToGameScenes(standardPool, treasurePool, bossPool, shieldCatalog);
            WriteDocs();
            WriteReport(shieldCatalog, treasurePool, bossPool);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 69 equipment load, shield, and stability assets.");
        }

        private static ShieldDefinition SaveShield(
            string fileName,
            string shieldId,
            string displayName,
            ArmorRarity rarity,
            EquipmentLoadClass loadClass)
        {
            var path = $"{EquipmentDirectory}/{fileName}";
            var shield = AssetDatabase.LoadAssetAtPath<ShieldDefinition>(path);
            if (shield == null)
            {
                shield = ScriptableObject.CreateInstance<ShieldDefinition>();
                AssetDatabase.CreateAsset(shield, path);
            }

            shield.Configure(shieldId, displayName, rarity, loadClass, default, new[] { BuildTag.Defense });
            EditorUtility.SetDirty(shield);
            return shield;
        }

        private static ShieldCatalogDefinition SaveShieldCatalog(IEnumerable<ShieldDefinition> shields)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShieldCatalogDefinition>(ShieldCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ShieldCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, ShieldCatalogPath);
            }

            catalog.Configure("m69_shield_catalog_v1", shields);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static RewardDefinition SaveShieldReward(string fileName, ShieldDefinition shield, RewardRarity rarity)
        {
            var path = $"{RewardDirectory}/{fileName}";
            var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (reward == null)
            {
                reward = ScriptableObject.CreateInstance<RewardDefinition>();
                AssetDatabase.CreateAsset(reward, path);
            }

            reward.Configure(shield.ShieldId, shield.DisplayName, RewardKind.Shield, rarity, 0, 0, System.Array.Empty<RewardEffect>(), shield.Tags);
            EditorUtility.SetDirty(reward);
            return reward;
        }

        private static RewardPoolDefinition SavePool(string path, string poolId, IEnumerable<RewardDefinition> rewards)
        {
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(path);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<RewardPoolDefinition>();
                AssetDatabase.CreateAsset(pool, path);
            }

            pool.Configure(poolId, DistinctByRewardId(rewards));
            EditorUtility.SetDirty(pool);
            return pool;
        }

        private static IEnumerable<RewardDefinition> LoadRewards(string poolPath)
        {
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(poolPath);
            return pool?.Rewards ?? System.Array.Empty<RewardDefinition>();
        }

        private static IEnumerable<RewardDefinition> DistinctByRewardId(IEnumerable<RewardDefinition> rewards)
        {
            return (rewards ?? System.Array.Empty<RewardDefinition>())
                .Where(reward => reward != null && !string.IsNullOrWhiteSpace(reward.RewardId))
                .GroupBy(reward => reward.RewardId)
                .Select(group => group.First())
                .OrderBy(reward => reward.RewardId);
        }

        private static void RetuneWeaponLoadClasses()
        {
            RetuneWeapon($"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_StarterBlade.asset", EquipmentLoadClass.Light);
            RetuneWeapon($"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_StarterBolt.asset", EquipmentLoadClass.Light);
            RetuneWeapon($"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_IronCleaver.asset", EquipmentLoadClass.Heavy);
            RetuneWeapon($"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_EmberBolt.asset", EquipmentLoadClass.Medium);
            RetuneWeapon($"{Milestone30AssetGenerator.SetWeaponDirectory}/Weapon_SkeletalSword.asset", EquipmentLoadClass.Medium);
            RetuneWeapon($"{Milestone30AssetGenerator.SetWeaponDirectory}/Weapon_BonePistol.asset", EquipmentLoadClass.Medium);
            RetuneWeapon($"{Milestone30AssetGenerator.SetWeaponDirectory}/Weapon_DragonFang.asset", EquipmentLoadClass.Medium);
            RetuneWeapon($"{Milestone30AssetGenerator.SetWeaponDirectory}/Weapon_DragonPistol.asset", EquipmentLoadClass.Medium);
        }

        private static void RetuneWeapon(string path, EquipmentLoadClass loadClass)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon == null)
            {
                return;
            }

            weapon.Configure(
                weapon.WeaponId,
                weapon.DisplayName,
                weapon.Slot,
                weapon.Category,
                weapon.Tags,
                weapon.LightAttack,
                weapon.HeavyAttack,
                loadClass,
                nextIsDoubleHandedForPresentation: weapon.IsDoubleHandedForPresentation);
            EditorUtility.SetDirty(weapon);
        }

        private static void RetuneArmorLoadClasses()
        {
            RetuneArmor($"{Milestone30AssetGenerator.EquipmentDirectory}/Armor_SkeletalArmor.asset", EquipmentLoadClass.Medium);
            RetuneArmor($"{Milestone30AssetGenerator.EquipmentDirectory}/Armor_DragonScaleArmor.asset", EquipmentLoadClass.Heavy);
        }

        private static void RetuneArmor(string path, EquipmentLoadClass loadClass)
        {
            var armor = AssetDatabase.LoadAssetAtPath<ArmorDefinition>(path);
            if (armor == null)
            {
                return;
            }

            armor.Configure(armor.ArmorId, armor.DisplayName, armor.Rarity, armor.StatModifier, armor.Tags, loadClass);
            EditorUtility.SetDirty(armor);
        }

        private static void AssignToGameScenes(
            RewardPoolDefinition standardPool,
            RewardPoolDefinition treasurePool,
            RewardPoolDefinition bossPool,
            ShieldCatalogDefinition shieldCatalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureShieldCatalog(shieldCatalog);
                branch.ConfigureRewardPools(standardPool, treasurePool, bossPool);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteDocs()
        {
            File.WriteAllText(DocsPath, @"# M69: Equipment Load, Shields, Stability, And Attack Taxonomy V1

M69 adds a first equipment-load model and real shield equipment.

- Armor, shield, melee weapon, and ranged weapon each use `Light`, `Medium`, or `Heavy` load.
- Total load ranges from 4-12 and resolves into Light, Medium, or Heavy encumbrance.
- Medium/heavy load softly reduces speed and increases attack/guard stamina costs.
- Stability is a derived stat used for knockback resistance, not damage reduction.
- Starter or legacy saves fall back to `starter_buckler`.
- New shields: `starter_buckler`, `iron_kite_shield`, and `stone_wall_shield`.
- Damage now has composable channel/delivery/force/element classification metadata while preserving old `DamageThreatKind` behavior.
");
        }

        private static void WriteReport(ShieldCatalogDefinition shieldCatalog, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool)
        {
            File.WriteAllText(ReportPath, $@"# M69 Equipment Load + Shields + Stability Report

- Shield catalog: `{shieldCatalog.CatalogId}` with {shieldCatalog.Shields.Count} shields.
- Starter shield fallback: `{ShieldDefinition.StarterShieldId}`.
- Treasure pool: `{treasurePool.PoolId}` with {treasurePool.Rewards.Count} rewards.
- Boss pool: `{bossPool.PoolId}` with {bossPool.Rewards.Count} rewards.
- Load thresholds: Light 4-6, Medium 7-9, Heavy 10-12.
- Medium penalties: speed x0.92, attack stamina x1.10, guard stamina x1.10.
- Heavy penalties: speed x0.80, attack stamina x1.25, guard stamina x1.25, stamina regen x0.90.
- Stability: base 1, medium armor +1, heavy armor +2, active light/medium/heavy shield guard +1/+2/+3.
");
        }
    }
}
