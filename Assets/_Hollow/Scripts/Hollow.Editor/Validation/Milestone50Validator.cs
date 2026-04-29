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
    public static class Milestone50Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/WorldBiomeTag.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/RunWorldItineraryService.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone50AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone50Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone50StoryWorldIdentityTests.cs",
            Milestone50AssetGenerator.DocsPath,
            Milestone50AssetGenerator.RunFramingCatalogPath,
            Milestone50AssetGenerator.ReportPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 50 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M50 file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone50AssetGenerator.RunFramingCatalogPath);
            ValidateCatalog(catalog, failures);
            ValidateItinerary(catalog, failures);
            ValidateScenes(catalog, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 50 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateCatalog(RunFramingCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add($"Missing M50 run framing catalog: {Milestone50AssetGenerator.RunFramingCatalogPath}");
                return;
            }

            if (catalog.CatalogId != Milestone50AssetGenerator.CatalogId)
            {
                failures.Add("M50 catalog id must match the Hollow Star world identity catalog id.");
            }

            if (catalog.Worlds.Count != 8)
            {
                failures.Add($"M50 catalog must contain exactly 8 world identities, found {catalog.Worlds.Count}.");
            }

            foreach (var world in catalog.Worlds.Where(world => world != null))
            {
                if (string.IsNullOrWhiteSpace(world.IdentityId) ||
                    string.IsNullOrWhiteSpace(world.DisplayName) ||
                    string.IsNullOrWhiteSpace(world.Subtitle) ||
                    string.IsNullOrWhiteSpace(world.PaletteHint) ||
                    string.IsNullOrWhiteSpace(world.LightingHint) ||
                    string.IsNullOrWhiteSpace(world.MaterialNotes) ||
                    string.IsNullOrWhiteSpace(world.PrologueLine) ||
                    string.IsNullOrWhiteSpace(world.BranchLine) ||
                    string.IsNullOrWhiteSpace(world.HubLine) ||
                    string.IsNullOrWhiteSpace(world.BossLine) ||
                    string.IsNullOrWhiteSpace(world.ExtractionLine))
                {
                    failures.Add($"M50 world '{world.name}' is missing required identity/framing metadata.");
                }

                if (world.BiomeTags.Count == 0)
                {
                    failures.Add($"M50 world '{world.DisplayName}' must have at least one hidden biome tag.");
                }

                if (world.BranchEchoNames.Count < 3)
                {
                    failures.Add($"M50 world '{world.DisplayName}' must expose at least three branch echo names.");
                }
            }
        }

        private static void ValidateItinerary(RunFramingCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                return;
            }

            var first = RunWorldItineraryService.ResolveItinerary(catalog, 15001, 3).Select(world => world.IdentityId).ToArray();
            var second = RunWorldItineraryService.ResolveItinerary(catalog, 15001, 3).Select(world => world.IdentityId).ToArray();
            if (!first.SequenceEqual(second))
            {
                failures.Add("M50 world itinerary must be deterministic for the same seed.");
            }

            if (first.Length != 3 || first.Distinct().Count() != first.Length)
            {
                failures.Add("M50 world itinerary must resolve three distinct world identities.");
            }

            if (RunWorldItineraryService.Resolve(catalog, 15001, 1) == null ||
                string.IsNullOrWhiteSpace(RunWorldItineraryService.ResolveBranchEcho(catalog, 15001, 1, 0)))
            {
                failures.Add("M50 itinerary service must resolve a world identity and branch echo for normal runs.");
            }
        }

        private static void ValidateScenes(RunFramingCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                return;
            }

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
                    failures.Add($"{scenePath} BranchSessionController must reference the M50 run framing catalog.");
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
                if (hud == null || hud.Catalog != catalog)
                {
                    failures.Add($"{scenePath} RunFramingHudController must reference the M50 run framing catalog.");
                }
            }
        }
    }
}
