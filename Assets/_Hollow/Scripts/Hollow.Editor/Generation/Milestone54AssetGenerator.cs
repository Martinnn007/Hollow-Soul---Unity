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
    public static class Milestone54AssetGenerator
    {
        public const string RewardDirectory = "Assets/_Hollow/Data/Rewards/M54";
        public const string TreasureRewardPoolPath = RewardDirectory + "/TreasureRewardPool_M54.asset";
        public const string BossRewardPoolPath = RewardDirectory + "/BossRewardPool_M54.asset";
        public const string DocsPath = "Docs/Milestone54ItemCatalogueProjectilePassives.md";
        public const string ReportPath = "output/reports/m54_item_catalogue_projectile_passives.md";
        public const string PdfPath = "output/pdf/Hollow_M54_Item_Catalogue.pdf";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 54 Assets")]
        public static void Generate()
        {
            Milestone53AssetGenerator.Generate();
            Directory.CreateDirectory(RewardDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var doubleBarrel = SaveProjectilePassive(
                "Reward_DoubleBarrel.asset",
                ProjectilePassiveResolver.DoubleBarrelId,
                "Double-Barrel",
                RewardRarity.Rare,
                1,
                new[] { new RewardEffect(RewardEffectKind.ProjectilePatternRank, intValue: 2) },
                new[] { BuildTag.Ranged });
            var tripleShot = SaveProjectilePassive(
                "Reward_TripleShot.asset",
                ProjectilePassiveResolver.TripleShotId,
                "Triple-Shot",
                RewardRarity.Rare,
                1,
                new[] { new RewardEffect(RewardEffectKind.ProjectilePatternRank, intValue: 3) },
                new[] { BuildTag.Ranged, BuildTag.Fast });
            var quadShot = SaveProjectilePassive(
                "Reward_QuadShot.asset",
                ProjectilePassiveResolver.QuadShotId,
                "Quad-Shot",
                RewardRarity.Epic,
                1,
                new[] { new RewardEffect(RewardEffectKind.ProjectilePatternRank, intValue: 4) },
                new[] { BuildTag.Ranged, BuildTag.Magic });
            var powerUp = SaveProjectilePassive(
                "Reward_PowerUp.asset",
                ProjectilePassiveResolver.PowerUpId,
                "Power-up",
                RewardRarity.Rare,
                1,
                new[] { new RewardEffect(RewardEffectKind.RangedDamageMultiplier, floatValue: 2f) },
                new[] { BuildTag.Ranged, BuildTag.Fire });
            var fireRateUp = SaveProjectilePassive(
                "Reward_FireRateUp.asset",
                ProjectilePassiveResolver.FireRateUpId,
                "Fire-rate Up",
                RewardRarity.Uncommon,
                ProjectilePassiveResolver.FireRateUpMaxStacks,
                new[] { new RewardEffect(RewardEffectKind.RangedLightFireRateBonusPerSecond, floatValue: 1f) },
                new[] { BuildTag.Ranged, BuildTag.Fast });

            var m54Rewards = new[] { doubleBarrel, tripleShot, quadShot, powerUp, fireRateUp };
            var standardPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone52AssetGenerator.StandardRewardPoolPath) ??
                               AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone51AssetGenerator.StandardRewardPoolPath);
            var treasurePool = SavePool(
                TreasureRewardPoolPath,
                "m54_treasure_projectile_passive_rewards",
                LoadRewards(Milestone51AssetGenerator.TreasureRewardPoolPath).Concat(m54Rewards));
            var bossPool = SavePool(
                BossRewardPoolPath,
                "m54_boss_projectile_passive_rewards",
                LoadRewards(Milestone51AssetGenerator.BossRewardPoolPath).Concat(m54Rewards));

            AssignToGameScenes(standardPool, treasurePool, bossPool);
            WriteDocs();
            WriteReport(standardPool, treasurePool, bossPool);
            WritePdf(standardPool, treasurePool, bossPool);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 54 item catalogue and projectile passive assets.");
        }

        private static RewardDefinition SaveProjectilePassive(
            string fileName,
            string rewardId,
            string displayName,
            RewardRarity rarity,
            int maxStacks,
            IEnumerable<RewardEffect> effects,
            IEnumerable<BuildTag> tags)
        {
            var path = $"{RewardDirectory}/{fileName}";
            var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (reward == null)
            {
                reward = ScriptableObject.CreateInstance<RewardDefinition>();
                AssetDatabase.CreateAsset(reward, path);
            }

            reward.Configure(rewardId, displayName, RewardKind.PassiveItem, rarity, 0, 0, effects, tags, maxStacks);
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

        private static void AssignToGameScenes(RewardPoolDefinition standardPool, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureRewardPools(standardPool, treasurePool, bossPool);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WritePdf(RewardPoolDefinition standardPool, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool)
        {
            ItemCataloguePdfExporter.WritePdf(
                PdfPath,
                standardPool,
                treasurePool,
                bossPool,
                AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone27AssetGenerator.WeaponRewardPoolPath),
                AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath),
                AssetDatabase.LoadAssetAtPath<ArmorCatalogDefinition>(Milestone30AssetGenerator.ArmorCatalogPath),
                AssetDatabase.LoadAssetAtPath<UsableItemCatalogDefinition>(Milestone28AssetGenerator.UsableItemCatalogPath));
        }

        private static void WriteDocs()
        {
            File.WriteAllText(DocsPath, @"# M54: Item Catalogue + Projectile Passive Items

M54 adds five special passive ranged-build items and a generated item catalogue PDF.

- New passives appear only in treasure rooms, boss rewards, and hub shops.
- Standard combat/reward rooms keep M51/M52 sparse reward balance.
- Double-Barrel, Triple-Shot, and Quad-Shot use strongest-wins projectile pattern logic.
- Power-up doubles ranged projectile damage once and turns player shots red.
- Fire-rate Up stacks to 3 and adds +1 ranged light shot per second per stack.
- Catalogue PDF: `output/pdf/Hollow_M54_Item_Catalogue.pdf`.
");
        }

        private static void WriteReport(RewardPoolDefinition standardPool, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool)
        {
            File.WriteAllText(ReportPath, $@"# M54 Item Catalogue + Projectile Passives Report

- Standard room pool unchanged: `{standardPool?.PoolId}`.
- Treasure pool: `{treasurePool.PoolId}` with {treasurePool.Rewards.Count} rewards.
- Boss pool: `{bossPool.PoolId}` with {bossPool.Rewards.Count} rewards.
- Projectile passive IDs: `{string.Join("`, `", ProjectilePassiveResolver.AllProjectilePassiveIds)}`.
- Projectile passive display names: `Double-Barrel`, `Triple-Shot`, `Quad-Shot`, `Power-up`, `Fire-rate Up`.
- Fire-rate Up max stacks: {ProjectilePassiveResolver.FireRateUpMaxStacks}.
- PDF: `{PdfPath}`.
");
        }
    }
}
