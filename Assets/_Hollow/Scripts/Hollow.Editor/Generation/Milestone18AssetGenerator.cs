using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone18AssetGenerator
    {
        public const string RewardPoolDirectory = "Assets/_Hollow/Data/Rewards/M18";
        public const string StandardRewardPoolPath = RewardPoolDirectory + "/StandardRoomRewardPool.asset";
        public const string TreasureRewardPoolPath = RewardPoolDirectory + "/TreasureRewardPool.asset";
        public const string BossRewardPoolPath = RewardPoolDirectory + "/BossRewardPool.asset";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 18 Assets")]
        public static void Generate()
        {
            Milestone17AssetGenerator.Generate();
            Directory.CreateDirectory(RewardPoolDirectory);

            var stoneHeart = CreateReward("Reward_StoneHeart.asset", "stone_heart", "Stone Heart", RewardKind.PassiveItem, RewardRarity.Common, 10, RewardEffect.DefaultsForRewardId("stone_heart"));
            var quickDraw = CreateReward("Reward_QuickDraw.asset", "quick_draw", "Quick Draw", RewardKind.Card, RewardRarity.Common, 10, RewardEffect.DefaultsForRewardId("quick_draw"));
            var fleetStep = CreateReward("Reward_FleetStep.asset", "fleet_step", "Fleet Step", RewardKind.PassiveItem, RewardRarity.Common, 10, RewardEffect.DefaultsForRewardId("fleet_step"));
            var emberCharm = CreateReward("Reward_EmberCharm.asset", "ember_charm", "Ember Charm", RewardKind.PassiveItem, RewardRarity.Uncommon, 10, RewardEffect.DefaultsForRewardId("ember_charm"));
            var soulCache = CreateReward("Reward_SoulCache.asset", "soul_cache", "Soul Cache", RewardKind.Currency, RewardRarity.Common, 15, new RewardEffect[0]);
            var heavySpark = CreateReward("Reward_HeavySpark.asset", "heavy_spark", "Heavy Spark", RewardKind.PassiveItem, RewardRarity.Uncommon, 10, RewardEffect.DefaultsForRewardId("heavy_spark"));
            var swiftCharge = CreateReward("Reward_SwiftCharge.asset", "swift_charge", "Swift Charge", RewardKind.Card, RewardRarity.Uncommon, 10, RewardEffect.DefaultsForRewardId("swift_charge"));
            var treasureCache = CreateReward("Reward_TreasureCache.asset", "treasure_cache", "Treasure Cache", RewardKind.Currency, RewardRarity.Treasure, 25, new RewardEffect[0]);
            var bossSigil = CreateReward("Reward_BossSigil.asset", "boss_sigil", "Boss Sigil", RewardKind.PassiveItem, RewardRarity.Boss, 30, new[]
            {
                new RewardEffect(RewardEffectKind.MaxHealthBonus, intValue: 1),
                new RewardEffect(RewardEffectKind.ProjectileDamageBonus, intValue: 1)
            });

            var standardPool = CreatePool(StandardRewardPoolPath, "standard_room_rewards", new[]
            {
                stoneHeart,
                quickDraw,
                fleetStep,
                emberCharm,
                soulCache,
                heavySpark,
                swiftCharge
            });
            var treasurePool = CreatePool(TreasureRewardPoolPath, "treasure_room_rewards", new[] { treasureCache, stoneHeart, soulCache });
            var bossPool = CreatePool(BossRewardPoolPath, "boss_room_rewards", new[] { bossSigil });

            AssignPoolsToGameScenes(standardPool, treasurePool, bossPool);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 18 seeded reward pool assets and scene wiring.");
        }

        private static RewardDefinition CreateReward(
            string fileName,
            string rewardId,
            string displayName,
            RewardKind kind,
            RewardRarity rarity,
            int souls,
            IEnumerable<RewardEffect> effects)
        {
            var path = $"{RewardPoolDirectory}/{fileName}";
            var definition = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<RewardDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(rewardId, displayName, kind, rarity, souls, effects);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static RewardPoolDefinition CreatePool(string path, string poolId, IEnumerable<RewardDefinition> rewards)
        {
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(path);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<RewardPoolDefinition>();
                AssetDatabase.CreateAsset(pool, path);
            }

            pool.Configure(poolId, rewards);
            EditorUtility.SetDirty(pool);
            return pool;
        }

        private static void AssignPoolsToGameScenes(RewardPoolDefinition standardPool, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool)
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
    }
}
