using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rewards;
using Hollow.UI.Shell;
using Hollow.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone7Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Rewards/RewardResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/RunEconomy.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/PlayerRunStats.cs",
            "Assets/_Hollow/Scripts/Hollow.Persistence/RunPersistenceModels.cs",
            "Assets/_Hollow/Scripts/Hollow.Persistence/IRunSaveStore.cs",
            "Assets/_Hollow/Scripts/Hollow.Persistence/TransientSessionGuard.cs",
            "Assets/_Hollow/Scripts/Hollow.Core/Runtime/RunLaunchMode.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone7AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone7Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone7RunEconomyPersistenceTests.cs",
            "Docs/Milestone7RunEconomyPersistence.md",
            "Assets/_Hollow/Data/Rewards/north_stone_heart.asset",
            "Assets/_Hollow/Data/Rewards/south_quick_draw.asset",
            "Assets/_Hollow/Data/Rewards/east_fleet_step.asset",
            "Assets/_Hollow/Data/Rewards/west_ember_charm.asset"
        };

        private static readonly (string ScenePath, HollowPlatformKind PlatformKind)[] GameScenes =
        {
            ("Assets/_Hollow/Scenes/Game_Windows.unity", HollowPlatformKind.WindowsStandard3D),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", HollowPlatformKind.VisionOSBoundedTabletop),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", HollowPlatformKind.VisionOSImmersive)
        };

        [MenuItem("Hollow/Validation/Run Milestone 7 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: Application.isBatchMode);
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();

            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M7 file: {file}");
                }
            }

            ValidateRewards(failures);
            ValidateProfileStore(failures);
            foreach (var (scenePath, platformKind) in GameScenes)
            {
                ValidateScene(scenePath, platformKind, failures);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 7 validation passed.");
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

        private static void ValidateRewards(List<string> failures)
        {
            AssertReward("north", "stone_heart", 10, failures);
            AssertReward("south", "quick_draw", 10, failures);
            AssertReward("east", "fleet_step", 10, failures);
            AssertReward("west", "ember_charm", 10, failures);
        }

        private static void AssertReward(string roomId, string rewardId, int souls, List<string> failures)
        {
            var grant = RewardResolver.Resolve(roomId);
            if (grant.RewardId != rewardId || grant.Souls != souls)
            {
                failures.Add($"M7 reward resolver returned invalid grant for {roomId}.");
            }
        }

        private static void ValidateProfileStore(List<string> failures)
        {
            var tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m7_validator", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
            try
            {
                var store = new JsonProfileStore(tempRoot);
                var profile = store.CreateOrLoadProfile(new ProfileSlotId(0), "Validator");
                store.MarkRunStarted(new ProfileSlotId(profile.SlotIndex));
                store.SaveActiveRun(new ProfileSlotId(profile.SlotIndex), new RunSaveSnapshot { currentRoomId = "east" });
                if (!store.TryLoadActiveRun(new ProfileSlotId(profile.SlotIndex), out var snapshot) || snapshot.currentRoomId != "east")
                {
                    failures.Add("M7 profile store failed to save/load active run snapshots.");
                }

                store.CompleteActiveRun(new ProfileSlotId(profile.SlotIndex), new RunCompletionSummary { soulsToBank = 40, rewardsClaimed = 4 });
                var summary = store.LoadSlotSummaries()[profile.SlotIndex];
                if (summary.HasActiveRun || summary.BankedSouls != 40 || summary.CompletedRuns != 1)
                {
                    failures.Add("M7 profile store failed to bank completion rewards and clear active run.");
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        private static void ValidateScene(string scenePath, HollowPlatformKind expectedPlatformKind, List<string> failures)
        {
            if (!File.Exists(scenePath))
            {
                failures.Add($"Missing M7 game scene: {scenePath}");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
            var session = Object.FindFirstObjectByType<GameSessionController>();
            var branch = Object.FindFirstObjectByType<BranchSessionController>();
            var presentationRoot = Object.FindFirstObjectByType<PlatformPresentationRoot>();
            if (session == null || branch == null)
            {
                failures.Add($"{scenePath} must contain GameSessionController and BranchSessionController.");
            }

            if (presentationRoot == null || Mathf.Abs(presentationRoot.WorldScale - PresentationScalePolicy.WorldScaleFor(expectedPlatformKind)) > 0.0001f)
            {
                failures.Add($"{scenePath} has invalid M7 presentation scaling.");
            }

            var shellCanvas = GameObject.Find("PlatformShellCanvas");
            if (shellCanvas == null || shellCanvas.GetComponent<BranchMiniMapController>() == null)
            {
                failures.Add($"{scenePath} PlatformShellCanvas must keep BranchMiniMapController for M7 economy HUD.");
            }
            else if (presentationRoot != null && shellCanvas.transform.IsChildOf(presentationRoot.transform))
            {
                failures.Add($"{scenePath} PlatformShellCanvas must remain outside WorldPresentationRoot.");
            }
        }
    }
}
