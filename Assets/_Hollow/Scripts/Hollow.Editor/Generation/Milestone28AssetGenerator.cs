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
    public static class Milestone28AssetGenerator
    {
        public const string RewardDirectory = "Assets/_Hollow/Data/Rewards/M28";
        public const string StandardRewardPoolPath = RewardDirectory + "/StandardRoomRewardPool_M28.asset";
        public const string TreasureRewardPoolPath = RewardDirectory + "/TreasureRewardPool_M28.asset";
        public const string BossRewardPoolPath = RewardDirectory + "/BossRewardPool_M28.asset";
        public const string UsableItemCatalogPath = RewardDirectory + "/UsableItemCatalog_M28.asset";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 28 Assets")]
        public static void Generate()
        {
            Milestone27AssetGenerator.Generate();
            Directory.CreateDirectory(RewardDirectory);

            var vitalLocket = CreateReward("Reward_VitalLocket.asset", "vital_locket", "Vital Locket", RewardKind.PassiveItem, RewardRarity.Common, 8, 0, new[]
            {
                new RewardEffect(RewardEffectKind.MaxHealthBonus, intValue: 1),
                new RewardEffect(RewardEffectKind.Heal, intValue: 1)
            });
            var ironStitch = CreateReward("Reward_IronStitch.asset", "iron_stitch", "Iron Stitch", RewardKind.PassiveItem, RewardRarity.Common, 8, 0, new[]
            {
                new RewardEffect(RewardEffectKind.DefenseBonus, intValue: 1)
            });
            var fleetPin = CreateReward("Reward_FleetPin.asset", "fleet_pin", "Fleet Pin", RewardKind.PassiveItem, RewardRarity.Common, 8, 0, new[]
            {
                new RewardEffect(RewardEffectKind.MoveSpeedBonus, floatValue: 0.35f)
            });
            var staminaThread = CreateReward("Reward_StaminaThread.asset", "stamina_thread", "Stamina Thread", RewardKind.PassiveItem, RewardRarity.Common, 8, 0, new[]
            {
                new RewardEffect(RewardEffectKind.MaxStaminaBonus, floatValue: 20f),
                new RewardEffect(RewardEffectKind.StaminaRegenBonus, floatValue: 2f)
            });
            var bladeLesson = CreateReward("Reward_BladeLesson.asset", "blade_lesson", "Blade Lesson", RewardKind.PassiveCard, RewardRarity.Common, 8, 0, new[]
            {
                new RewardEffect(RewardEffectKind.MeleeDamageBonus, intValue: 1)
            });
            var boltLesson = CreateReward("Reward_BoltLesson.asset", "bolt_lesson", "Bolt Lesson", RewardKind.PassiveCard, RewardRarity.Common, 8, 0, new[]
            {
                new RewardEffect(RewardEffectKind.RangedDamageBonus, intValue: 1),
                new RewardEffect(RewardEffectKind.AttackCooldownMultiplier, floatValue: 0.97f)
            });
            var coinCache = CreateReward("Reward_CoinCache.asset", "coin_cache", "Coin Cache", RewardKind.Currency, RewardRarity.Common, 0, 6, System.Array.Empty<RewardEffect>());
            var treasureCoins = CreateReward("Reward_TreasureCoins.asset", "treasure_coins", "Treasure Coins", RewardKind.Currency, RewardRarity.Treasure, 0, 14, System.Array.Empty<RewardEffect>());

            var mendingCharmReward = CreateReward("Reward_MendingCharm.asset", "mending_charm", "Mending Charm", RewardKind.ActiveItem, RewardRarity.Uncommon, 0, 0, System.Array.Empty<RewardEffect>());
            var echoBurstReward = CreateReward("Reward_EchoBurst.asset", "echo_burst", "Echo Burst", RewardKind.ActiveItem, RewardRarity.Uncommon, 0, 0, System.Array.Empty<RewardEffect>());
            var emberCardReward = CreateReward("Reward_EmberCard.asset", "ember_card", "Ember Card", RewardKind.ConsumableCard, RewardRarity.Common, 0, 0, System.Array.Empty<RewardEffect>());
            var swiftCardReward = CreateReward("Reward_SwiftCard.asset", "swift_card", "Swift Card", RewardKind.ConsumableCard, RewardRarity.Common, 0, 0, System.Array.Empty<RewardEffect>());
            var mendCardReward = CreateReward("Reward_MendCard.asset", "mend_card", "Mend Card", RewardKind.ConsumableCard, RewardRarity.Common, 0, 0, System.Array.Empty<RewardEffect>());
            var bossSigil = CreateReward("Reward_BossSigil_M28.asset", "boss_sigil", "Boss Sigil", RewardKind.PassiveItem, RewardRarity.Boss, 25, 0, new[]
            {
                new RewardEffect(RewardEffectKind.MaxHealthBonus, intValue: 1),
                new RewardEffect(RewardEffectKind.RangedDamageBonus, intValue: 1)
            });

            var mendingCharm = CreateUsable("Usable_MendingCharm.asset", "mending_charm", "Mending Charm", RewardKind.ActiveItem, RewardRarity.Uncommon, maxCharges: 3, consumeOnUse: false, new[]
            {
                new RewardEffect(RewardEffectKind.Heal, intValue: 2)
            });
            var echoBurst = CreateUsable("Usable_EchoBurst.asset", "echo_burst", "Echo Burst", RewardKind.ActiveItem, RewardRarity.Uncommon, maxCharges: 3, consumeOnUse: false, System.Array.Empty<RewardEffect>());
            var emberCard = CreateUsable("Usable_EmberCard.asset", "ember_card", "Ember Card", RewardKind.ConsumableCard, RewardRarity.Common, maxCharges: 0, consumeOnUse: true, new[]
            {
                new RewardEffect(RewardEffectKind.RangedDamageBonus, intValue: 1),
                new RewardEffect(RewardEffectKind.MeleeDamageBonus, intValue: 1)
            });
            var swiftCard = CreateUsable("Usable_SwiftCard.asset", "swift_card", "Swift Card", RewardKind.ConsumableCard, RewardRarity.Common, maxCharges: 0, consumeOnUse: true, new[]
            {
                new RewardEffect(RewardEffectKind.MoveSpeedBonus, floatValue: 0.75f)
            });
            var mendCard = CreateUsable("Usable_MendCard.asset", "mend_card", "Mend Card", RewardKind.ConsumableCard, RewardRarity.Common, maxCharges: 0, consumeOnUse: true, new[]
            {
                new RewardEffect(RewardEffectKind.Heal, intValue: 2)
            });

            var standardPool = CreatePool(StandardRewardPoolPath, "m28_standard_room_rewards", new[]
            {
                vitalLocket,
                ironStitch,
                fleetPin,
                staminaThread,
                bladeLesson,
                boltLesson,
                coinCache,
                emberCardReward,
                swiftCardReward,
                mendCardReward,
                mendingCharmReward,
                echoBurstReward
            });
            var treasurePool = CreatePool(TreasureRewardPoolPath, "m28_treasure_room_rewards", new[]
            {
                treasureCoins,
                vitalLocket,
                mendingCharmReward,
                echoBurstReward
            });
            var bossPool = CreatePool(BossRewardPoolPath, "m28_boss_room_rewards", new[] { bossSigil, echoBurstReward });
            var usableCatalog = CreateUsableCatalog(new[] { mendingCharm, echoBurst, emberCard, swiftCard, mendCard });

            AssignToGameScenes(standardPool, treasurePool, bossPool, usableCatalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 28 item, card, coin, and shop reward assets.");
        }

        private static RewardDefinition CreateReward(string fileName, string rewardId, string displayName, RewardKind kind, RewardRarity rarity, int souls, int coins, IEnumerable<RewardEffect> effects)
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

        private static UsableItemDefinition CreateUsable(string fileName, string itemId, string displayName, RewardKind kind, RewardRarity rarity, int maxCharges, bool consumeOnUse, IEnumerable<RewardEffect> effects)
        {
            var path = $"{RewardDirectory}/{fileName}";
            var usable = AssetDatabase.LoadAssetAtPath<UsableItemDefinition>(path);
            if (usable == null)
            {
                usable = ScriptableObject.CreateInstance<UsableItemDefinition>();
                AssetDatabase.CreateAsset(usable, path);
            }

            usable.Configure(itemId, displayName, kind, rarity, maxCharges, consumeOnUse, effects);
            EditorUtility.SetDirty(usable);
            return usable;
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

        private static UsableItemCatalogDefinition CreateUsableCatalog(IEnumerable<UsableItemDefinition> items)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UsableItemCatalogDefinition>(UsableItemCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UsableItemCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, UsableItemCatalogPath);
            }

            catalog.Configure("m28_usable_item_catalog_v1", items);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AssignToGameScenes(RewardPoolDefinition standardPool, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool, UsableItemCatalogDefinition usableCatalog)
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
                branch.ConfigureUsableItemCatalog(usableCatalog);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
