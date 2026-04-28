using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone21Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Branches/HubShopCard.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/HubShopCardViewModel.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone21AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone21Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone21ShopChoiceUiTests.cs",
            "Docs/Milestone21ShopChoiceUi.md"
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 21 Validation")]
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
                    failures.Add($"Missing M21 file: {file}");
                }
            }

            ValidateShopCards(failures);
            ValidateScenes(failures);
            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 21 validation passed.");
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

        private static void ValidateShopCards(List<string> failures)
        {
            var hub = InterBranchHubState.Create(21001, 0, null);
            if (hub.ShopOffers.Count != 3 ||
                hub.ShopOffers.Count(offer => offer.OfferId == "heal_2") != 1 ||
                hub.ShopOffers.Count(offer => offer.OfferId.StartsWith("reward_")) != 2)
            {
                failures.Add("M21 shop must expose exactly one heal card and two reward cards.");
            }

            var heal = hub.ShopOffers.FirstOrDefault(offer => offer.OfferId == "heal_2");
            if (heal == null || heal.Price != 8 || heal.HealAmount != 2)
            {
                failures.Add("M21 heal card must cost 8 and heal 2 HP.");
            }

            foreach (var offer in hub.ShopOffers.Where(offer => offer.OfferId.StartsWith("reward_")))
            {
                if (offer.RewardGrant.RewardKind != RewardKind.Weapon && offer.Price != 16)
                {
                    failures.Add("M21 reward offers must keep the simple reward-card price after later currency updates.");
                }
            }
        }

        private static void ValidateScenes(List<string> failures)
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

                if (branch.HubShopPrefab == null || branch.HubShopPrefab.GetComponent<HubShopController>() == null)
                {
                    failures.Add($"{scenePath} BranchSessionController must keep a HubShop prefab with HubShopController.");
                }
            }
        }
    }
}
