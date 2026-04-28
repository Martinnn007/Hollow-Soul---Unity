using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.UI.Shell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone39Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/RunFramingDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/RunFramingCatalogDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/RunFramingService.cs",
            "Assets/_Hollow/Scripts/Hollow.UI/Shell/RunFramingHudController.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone39AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone39Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone39StoryWorldIdentityTests.cs",
            "Docs/Milestone39StoryWorldIdentityRunFraming.md",
            Milestone39AssetGenerator.RunFramingCatalogPath,
            Milestone39AssetGenerator.ReportPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 39 Validation")]
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
                    failures.Add($"Missing M39 file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone39AssetGenerator.RunFramingCatalogPath);
            ValidateCatalog(catalog, failures);
            ValidateScenes(catalog, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 39 validation passed.");
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

        private static void ValidateCatalog(RunFramingCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add($"Missing M39 run framing catalog: {Milestone39AssetGenerator.RunFramingCatalogPath}");
                return;
            }

            if (catalog.Worlds.Count < 3)
            {
                failures.Add("M39 run framing catalog must include at least three prototype worlds.");
            }

            foreach (var expectedWorld in new[] { 1, 2, 3 })
            {
                if (!catalog.TryGetWorld(expectedWorld, out var world) || world == null || world.WorldIndex != expectedWorld)
                {
                    failures.Add($"M39 run framing catalog missing world {expectedWorld}.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(world.DisplayName) ||
                    string.IsNullOrWhiteSpace(world.Subtitle) ||
                    string.IsNullOrWhiteSpace(world.PrologueLine) ||
                    string.IsNullOrWhiteSpace(world.BranchLine) ||
                    string.IsNullOrWhiteSpace(world.HubLine) ||
                    string.IsNullOrWhiteSpace(world.BossLine) ||
                    string.IsNullOrWhiteSpace(world.ExtractionLine))
                {
                    failures.Add($"M39 world {expectedWorld} is missing framing text.");
                }
            }
        }

        private static void ValidateScenes(RunFramingCatalogDefinition catalog, List<string> failures)
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

                if (branch.RunFramingCatalog != catalog)
                {
                    failures.Add($"{scenePath} BranchSessionController must reference the M39 run framing catalog.");
                }

                var shellCanvas = GameObject.Find("PlatformShellCanvas");
                if (shellCanvas == null)
                {
                    failures.Add($"{scenePath} is missing PlatformShellCanvas.");
                    continue;
                }

                if (shellCanvas.transform.IsChildOf(GameObject.Find("WorldPresentationRoot")?.transform))
                {
                    failures.Add($"{scenePath} PlatformShellCanvas must remain outside WorldPresentationRoot.");
                }

                var hud = shellCanvas.GetComponent<RunFramingHudController>();
                if (hud == null)
                {
                    failures.Add($"{scenePath} PlatformShellCanvas must include RunFramingHudController.");
                }
                else if (hud.Catalog != catalog)
                {
                    failures.Add($"{scenePath} RunFramingHudController must reference the M39 run framing catalog.");
                }
            }
        }
    }
}
