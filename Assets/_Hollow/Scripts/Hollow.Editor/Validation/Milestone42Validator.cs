using System.Collections.Generic;
using System.IO;
using Hollow.Editor.Generation;
using Hollow.UI.Shell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone42Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Rewards/PlayerBuildHudModel.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/PickupRevealModel.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/RewardPresentationResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/ReplacementPickupState.cs",
            "Assets/_Hollow/Scripts/Hollow.UI/Shell/PlayerBuildHudController.cs",
            "Assets/_Hollow/Scripts/Hollow.UI/Shell/PickupRevealController.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone42AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone42Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone42PlayerBuildUxTests.cs",
            "Docs/Milestone42PlayerBuildUxPickupClarity.md",
            Milestone42AssetGenerator.ReportPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 42 Validation")]
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
                    failures.Add($"Missing M42 file: {file}");
                }
            }

            ValidateScenes(failures);
            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 42 validation passed.");
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

        private static void ValidateScenes(List<string> failures)
        {
            foreach (var scenePath in GameScenes)
            {
                if (!File.Exists(scenePath))
                {
                    failures.Add($"Missing game scene: {scenePath}");
                    continue;
                }

                EditorSceneManager.OpenScene(scenePath);
                var shellCanvas = GameObject.Find("PlatformShellCanvas");
                if (shellCanvas == null)
                {
                    failures.Add($"{scenePath} is missing PlatformShellCanvas.");
                    continue;
                }

                var shellController = shellCanvas.GetComponent<PlatformShellController>();
                if (shellCanvas.GetComponent<PlayerBuildHudController>() == null && shellController == null)
                {
                    failures.Add($"{scenePath} PlatformShellCanvas is missing PlayerBuildHudController and runtime shell fallback.");
                }

                if (shellCanvas.GetComponent<PickupRevealController>() == null && shellController == null)
                {
                    failures.Add($"{scenePath} PlatformShellCanvas is missing PickupRevealController and runtime shell fallback.");
                }

                var worldRoot = GameObject.Find("WorldPresentationRoot");
                if (worldRoot != null &&
                    (shellCanvas.transform.IsChildOf(worldRoot.transform) || worldRoot.transform.IsChildOf(shellCanvas.transform)))
                {
                    failures.Add($"{scenePath} HUD controllers must remain outside WorldPresentationRoot.");
                }
            }
        }
    }
}
