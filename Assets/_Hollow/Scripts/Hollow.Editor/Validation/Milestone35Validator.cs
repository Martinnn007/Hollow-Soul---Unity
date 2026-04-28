using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.UI.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone35Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ChallengeDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ChallengeCatalogDefinition.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone35ChallengeModeTests.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone35AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone35Validator.cs",
            "Docs/Milestone35ChallengeModeV1.md",
            Milestone35AssetGenerator.ChallengeCatalogPath,
            Milestone35AssetGenerator.BaselineReportPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 35 Validation")]
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
                    failures.Add($"Missing M35 file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalogDefinition>(Milestone35AssetGenerator.ChallengeCatalogPath);
            ValidateCatalog(catalog, failures);
            ValidateSceneWiring(catalog, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 35 validation passed.");
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

        private static void ValidateCatalog(ChallengeCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("Missing M35 challenge catalog asset.");
                return;
            }

            if (catalog.Challenges.Count < 3)
            {
                failures.Add("M35 challenge catalog must contain at least three curated challenges.");
            }

            ValidateChallenge(catalog, "blade_trial", 35001, failures);
            ValidateChallenge(catalog, "glass_runner", 35002, failures);
            ValidateChallenge(catalog, "stone_oath", 35003, failures);
        }

        private static void ValidateChallenge(ChallengeCatalogDefinition catalog, string challengeId, int expectedSeed, List<string> failures)
        {
            if (!catalog.TryGetChallenge(challengeId, out var challenge))
            {
                failures.Add($"Missing M35 challenge: {challengeId}");
                return;
            }

            if (challenge.FixedRunSeed != expectedSeed || string.IsNullOrWhiteSpace(challenge.DisplayName) || challenge.Rules.Count == 0)
            {
                failures.Add($"M35 challenge {challengeId} must have expected seed, display name, and rules.");
            }
        }

        private static void ValidateSceneWiring(ChallengeCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                return;
            }

            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null || branch.ChallengeCatalog != catalog)
                {
                    failures.Add($"{scenePath} BranchSessionController must reference the M35 challenge catalog.");
                }
            }

            var menuScene = EditorSceneManager.OpenScene("Assets/_Hollow/Scenes/MainMenu.unity");
            var mainMenu = Object.FindFirstObjectByType<MainMenuController>();
            if (mainMenu == null || mainMenu.ChallengeCatalog != catalog)
            {
                failures.Add("MainMenuController must reference the M35 challenge catalog.");
            }
        }
    }
}
