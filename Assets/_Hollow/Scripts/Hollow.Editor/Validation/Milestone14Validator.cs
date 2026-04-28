using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone14Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/BranchRoomTemplateCatalogDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionContent.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone14AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone14Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone14MacroBranchTests.cs",
            "Docs/Milestone14MacroBranchGeneration.md",
            Milestone14AssetGenerator.CatalogPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 14 Validation")]
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
                    failures.Add($"Missing M14 file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            ValidateCatalog(catalog, failures);
            ValidateGraph(catalog, failures);
            ValidateScenes(catalog, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 14 validation passed.");
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

        private static void ValidateCatalog(BranchRoomTemplateCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("M14 branch room template catalog is missing.");
                return;
            }

            if (catalog.DefaultSeed != BranchGenerator.DefaultMacroFixtureSeed)
            {
                failures.Add("M14 catalog default seed does not match BranchGenerator.DefaultMacroFixtureSeed.");
            }

            foreach (var template in catalog.AllTemplates)
            {
                if (template == null)
                {
                    failures.Add("M14 catalog has an unassigned room template fixture.");
                    continue;
                }

                if (!HollowRuntimeV2Importer.TryImport(template.text, out _, out var error))
                {
                    failures.Add($"M14 catalog fixture failed import: {template.name}: {error}");
                }
            }
        }

        private static void ValidateGraph(BranchRoomTemplateCatalogDefinition catalog, List<string> failures)
        {
            var legacy = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var legacyError = "missing legacy sample room JSON";
            ImportedRoomRuntimeAsset legacyAsset = null;
            if (legacy == null || !HollowRuntimeV2Importer.TryImport(legacy.text, out legacyAsset, out legacyError))
            {
                failures.Add($"M14 could not import legacy sample room: {legacyError}");
                return;
            }

            var content = BranchSessionContent.Create(legacyAsset, catalog, BranchGenerator.DefaultMacroFixtureSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M14 content import error: {contentError}");
            }

            if (!content.HasMacroFixturePool)
            {
                failures.Add("M14 content catalog does not contain the complete macro fixture pool.");
                return;
            }

            var graph = BranchGenerator.CreateMacroFixtureBranch(content.MacroRoomPool, content.BranchSeed);
            if (graph.BranchId != BranchGenerator.MacroFixtureBranchId || graph.Seed != BranchGenerator.DefaultMacroFixtureSeed)
            {
                failures.Add("M14 macro graph has invalid branch identity or seed.");
            }

            if (graph.RoomCount != 5 || graph.OccupancyMap.OwnerByCell.Count != 12)
            {
                failures.Add("M14 macro graph must keep five logical rooms and twelve occupied branch cells.");
            }

            if (graph.Connections.Count < 8 || graph.Connections.Any(connection => !connection.HasExplicitPorts))
            {
                failures.Add("M14 macro graph must keep explicit port-to-port connections, including successor auto-linked adjacent doors.");
            }

            AssertPortConnection(graph, BranchRoomId.Origin, "north_0", BranchRoomId.North, failures);
            AssertPortConnection(graph, BranchRoomId.Origin, "south_0", BranchRoomId.South, failures);
            AssertPortConnection(graph, BranchRoomId.Origin, "east_0", BranchRoomId.East, failures);
            AssertPortConnection(graph, BranchRoomId.Origin, "west_0", BranchRoomId.West, failures);
            AssertPortConnection(graph, BranchRoomId.West, "east_1", BranchRoomId.Origin, failures);
            ValidateNoDuplicatePortPairs(graph, failures);
        }

        private static void ValidateScenes(BranchRoomTemplateCatalogDefinition catalog, List<string> failures)
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

                var hasForwardCompatibleSeed = branch.MacroBranchSeed == BranchGenerator.DefaultMacroFixtureSeed ||
                                               branch.MacroBranchSeed == BranchGenerator.DefaultSeededMacroSeed;
                if (branch.BranchRoomTemplateCatalog != catalog || !hasForwardCompatibleSeed)
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to the M14 macro room catalog.");
                }
            }
        }

        private static void AssertPortConnection(BranchFloorGraph graph, BranchRoomId from, string portId, BranchRoomId expectedTo, List<string> failures)
        {
            if (!graph.TryGetConnectionByPort(from, portId, out var connection) || connection.ToRoomId != expectedTo)
            {
                failures.Add($"M14 macro graph missing expected port connection {from}.{portId} -> {expectedTo}.");
            }
        }

        private static void ValidateNoDuplicatePortPairs(BranchFloorGraph graph, List<string> failures)
        {
            var duplicate = graph.Connections
                .GroupBy(connection => $"{connection.FromRoomId.Value}:{connection.FromPortId}->{connection.ToRoomId.Value}:{connection.ToPortId}")
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                failures.Add($"M14 macro graph contains duplicate explicit port connection: {duplicate.Key}.");
            }
        }
    }
}
