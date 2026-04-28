using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone28Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Rewards/RewardApplicationService.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/UsableItemDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/UsableItemCatalogDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone28AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone28Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone28ItemsCardsCoinsTests.cs",
            "Docs/Milestone28ItemsCardsCoinsShopRewards.md",
            Milestone28AssetGenerator.StandardRewardPoolPath,
            Milestone28AssetGenerator.TreasureRewardPoolPath,
            Milestone28AssetGenerator.BossRewardPoolPath,
            Milestone28AssetGenerator.UsableItemCatalogPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 28 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M28 file: {file}");
                }
            }

            var standardPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.StandardRewardPoolPath);
            var treasurePool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.TreasureRewardPoolPath);
            var bossPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.BossRewardPoolPath);
            var usableCatalog = AssetDatabase.LoadAssetAtPath<UsableItemCatalogDefinition>(Milestone28AssetGenerator.UsableItemCatalogPath);
            ValidateRewardPools(standardPool, treasurePool, bossPool, failures);
            ValidateUsables(usableCatalog, failures);
            ValidateShopPricing(failures);
            ValidateScenes(standardPool, treasurePool, bossPool, usableCatalog, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 28 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateRewardPools(RewardPoolDefinition standardPool, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool, List<string> failures)
        {
            if (standardPool == null || treasurePool == null || bossPool == null)
            {
                failures.Add("M28 reward pools are missing.");
                return;
            }

            foreach (var rewardId in new[] { "vital_locket", "iron_stitch", "fleet_pin", "stamina_thread", "blade_lesson", "bolt_lesson", "coin_cache", "ember_card", "swift_card", "mend_card", "mending_charm", "echo_burst" })
            {
                if (!standardPool.Rewards.Any(reward => reward != null && reward.RewardId == rewardId))
                {
                    failures.Add($"M28 standard reward pool missing {rewardId}.");
                }
            }

            if (!standardPool.Rewards.Any(reward => reward != null && reward.Coins >= 5 && reward.Coins <= 8))
            {
                failures.Add("M28 standard pool must include a 5-8 coin reward.");
            }

            if (!treasurePool.Rewards.Any(reward => reward != null && reward.Coins >= 12 && reward.Coins <= 16))
            {
                failures.Add("M28 treasure pool must include a 12-16 coin reward.");
            }

            if (!bossPool.Rewards.Any(reward => reward != null && reward.RewardId == "boss_sigil"))
            {
                failures.Add("M28 boss reward pool must retain boss_sigil.");
            }
        }

        private static void ValidateUsables(UsableItemCatalogDefinition usableCatalog, List<string> failures)
        {
            if (usableCatalog == null)
            {
                failures.Add("M28 usable item catalog is missing.");
                return;
            }

            foreach (var itemId in new[] { "mending_charm", "echo_burst", "ember_card", "swift_card", "mend_card" })
            {
                if (!usableCatalog.TryGet(itemId, out var item) || string.IsNullOrWhiteSpace(item.DisplayName))
                {
                    failures.Add($"M28 usable catalog missing {itemId}.");
                    continue;
                }

                if (item.RewardKind == RewardKind.ActiveItem && item.MaxCharges != 3)
                {
                    failures.Add($"M28 active item {itemId} must use three charges.");
                }
            }
        }

        private static void ValidateShopPricing(List<string> failures)
        {
            var offers = HubShopOffer.CreateSeededOffers(28001, 0, null, null);
            var heal = offers.FirstOrDefault(offer => offer.OfferId == "heal_2");
            if (heal == null || heal.Price != 8 || heal.PriceCurrency != ShopPriceCurrency.Coins)
            {
                failures.Add("M28 heal shop offer must cost 8 coins.");
            }

            foreach (var offer in offers.Where(offer => offer.OfferId.StartsWith("reward_")))
            {
                if (offer.PriceCurrency != ShopPriceCurrency.Coins || offer.Price != 16)
                {
                    failures.Add("M28 non-weapon shop reward offers must cost 16 coins.");
                }
            }
        }

        private static void ValidateScenes(RewardPoolDefinition standardPool, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool, UsableItemCatalogDefinition usableCatalog, List<string> failures)
        {
            foreach (var scenePath in GameScenes)
            {
                EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (branch.StandardRewardPool != standardPool || branch.TreasureRewardPool != treasurePool || branch.BossRewardPool != bossPool || branch.UsableItemCatalog != usableCatalog)
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to M28 reward pools/catalog.");
                }
            }
        }
    }
}
