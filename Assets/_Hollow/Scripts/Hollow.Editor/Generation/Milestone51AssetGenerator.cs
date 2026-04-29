using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone51AssetGenerator
    {
        public const string RewardDirectory = "Assets/_Hollow/Data/Rewards/M51";
        public const string StandardRewardPoolPath = RewardDirectory + "/StandardRoomRewardPool_M51.asset";
        public const string TreasureRewardPoolPath = RewardDirectory + "/TreasureRewardPool_M51.asset";
        public const string BossRewardPoolPath = RewardDirectory + "/BossRewardPool_M51.asset";
        public const string DocsPath = "Docs/Milestone51PreBetaRewardHealthRebalance.md";
        public const string ReportPath = "output/reports/m51_pre_beta_reward_health_rebalance.md";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 51 Assets")]
        public static void Generate()
        {
            Milestone50AssetGenerator.Generate();
            Directory.CreateDirectory(RewardDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var smallCoins = SaveReward("Reward_SmallCoinPouch.asset", "small_coin_pouch", "Small Coin Pouch", RewardKind.Currency, RewardRarity.Common, 0, 6, System.Array.Empty<RewardEffect>());
            var hpRefill = SaveReward("Reward_HpRefill.asset", "hp_refill", "HP Refill", RewardKind.Heal, RewardRarity.Common, 0, 0, new[] { new RewardEffect(RewardEffectKind.Heal, intValue: 99) });
            var chest = SaveReward("Reward_StandardTreasureChest.asset", "standard_treasure_chest", "Treasure Chest", RewardKind.Currency, RewardRarity.Common, 0, 10, System.Array.Empty<RewardEffect>());

            var standardPool = SavePool(StandardRewardPoolPath, ProceduralRewardResolver.PreBetaStandardPoolId, new[] { smallCoins, hpRefill, chest });
            var treasurePool = SavePool(TreasureRewardPoolPath, "m51_treasure_item_rewards", TreasureRewards());
            var bossPool = SavePool(BossRewardPoolPath, "m51_boss_item_rewards", BossRewards());

            AssignToGameScenes(standardPool, treasurePool, bossPool);
            WriteDocs();
            WriteReport(standardPool, treasurePool, bossPool);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 51 pre-beta reward and health rebalance assets.");
        }

        private static RewardDefinition SaveReward(string fileName, string rewardId, string displayName, RewardKind kind, RewardRarity rarity, int souls, int coins, IEnumerable<RewardEffect> effects)
        {
            var path = $"{RewardDirectory}/{fileName}";
            var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (reward == null)
            {
                reward = ScriptableObject.CreateInstance<RewardDefinition>();
                AssetDatabase.CreateAsset(reward, path);
            }

            reward.Configure(rewardId, displayName, kind, rarity, souls, coins, effects);
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

        private static IEnumerable<RewardDefinition> TreasureRewards()
        {
            return DistinctByRewardId(LoadPools(
                    Milestone28AssetGenerator.StandardRewardPoolPath,
                    Milestone28AssetGenerator.TreasureRewardPoolPath)
                .Where(reward => IsBuildChangingReward(reward) || reward.RewardId == "treasure_coins"));
        }

        private static IEnumerable<RewardDefinition> BossRewards()
        {
            return DistinctByRewardId(LoadPools(
                    Milestone28AssetGenerator.BossRewardPoolPath,
                    Milestone28AssetGenerator.TreasureRewardPoolPath,
                    Milestone28AssetGenerator.StandardRewardPoolPath)
                .Where(reward => IsBuildChangingReward(reward) || reward.Rarity == RewardRarity.Boss));
        }

        private static IEnumerable<RewardDefinition> LoadPools(params string[] paths)
        {
            return paths
                .Select(path => AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(path))
                .Where(pool => pool != null)
                .SelectMany(pool => pool.Rewards)
                .Where(reward => reward != null);
        }

        private static IEnumerable<RewardDefinition> DistinctByRewardId(IEnumerable<RewardDefinition> rewards)
        {
            return (rewards ?? Enumerable.Empty<RewardDefinition>())
                .Where(reward => reward != null && !string.IsNullOrWhiteSpace(reward.RewardId))
                .GroupBy(reward => reward.RewardId)
                .Select(group => group.First());
        }

        private static bool IsBuildChangingReward(RewardDefinition reward)
        {
            return reward != null && reward.RewardKind is RewardKind.PassiveItem or RewardKind.Card or RewardKind.PassiveCard or RewardKind.ActiveItem or RewardKind.ConsumableCard or RewardKind.Weapon or RewardKind.Armor;
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

        private static void WriteDocs()
        {
            File.WriteAllText(DocsPath, @"# M51: Pre-Beta Reward + Health Rebalance

M51 makes ordinary room rewards sparse before the beta vertical-slice lock. Normal combat/reward rooms now produce only small coin pouches, HP refill, a simple treasure chest placeholder, or nothing. Build-changing rewards are reserved for treasure rooms, boss rewards, and hub shops.

Balanced starts with 3 max HP. Heavy is rescaled to 5 max HP while keeping its slower, tougher melee identity. Challenge HP penalties are clamped at runtime so curated challenges remain playable after the lower health baseline.

The standard treasure chest is intentionally simple in M51: it is a reward pickup/card that grants extra coins. It is not a locked chest, key system, or interactable container yet.
");
        }

        private static void WriteReport(RewardPoolDefinition standardPool, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool)
        {
            File.WriteAllText(ReportPath, $@"# M51 Pre-Beta Reward + Health Rebalance Report

- Standard pool: `{standardPool.PoolId}` with {standardPool.Rewards.Count} sustain/economy rewards.
- Treasure pool: `{treasurePool.PoolId}` with {treasurePool.Rewards.Count} item-capable rewards.
- Boss pool: `{bossPool.PoolId}` with {bossPool.Rewards.Count} item-capable rewards.
- Balanced max HP: 3.
- Heavy max HP: 5.
- No-reward rooms are represented by omitted reward grants and are marked unavailable at runtime.
");
        }
    }
}
