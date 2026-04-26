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
    public static class Milestone16Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Branches/ApprovedDesignerRoomImporter.cs",
            "Assets/_Hollow/Scripts/Hollow.Rooms/RoomFootprintShape.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone16AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone16Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone16ApprovedDesignerRoomPoolTests.cs",
            "Docs/Milestone16ApprovedDesignerRoomPool.md",
            Milestone16AssetGenerator.ApprovedRoomDirectory
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 16 Validation")]
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
                if (!File.Exists(file) && !Directory.Exists(file))
                {
                    failures.Add($"Missing M16 file or folder: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            ValidateCatalog(catalog, failures);
            ValidateApprovedRooms(catalog, failures);
            ValidateGraph(catalog, settings, failures);
            ValidateScenes(catalog, settings, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 16 validation passed.");
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
                failures.Add("M16 branch room template catalog is missing.");
                return;
            }

            if (catalog.FixtureTemplates.Any(template => template == null))
            {
                failures.Add("M16 catalog must preserve all five M13 fixture template references.");
            }

            var approvedFromFolder = Milestone16AssetGenerator.LoadApprovedTemplates()
                .Select(AssetDatabase.GetAssetPath)
                .OrderBy(path => path)
                .ToArray();
            var approvedFromCatalog = catalog.AdditionalTemplates
                .Select(AssetDatabase.GetAssetPath)
                .OrderBy(path => path)
                .ToArray();

            if (!approvedFromFolder.SequenceEqual(approvedFromCatalog))
            {
                failures.Add("M16 catalog additional templates do not match DesignerApproved folder contents. Regenerate M16 assets.");
            }
        }

        private static void ValidateApprovedRooms(BranchRoomTemplateCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                return;
            }

            var report = ApprovedDesignerRoomImporter.ImportApprovedRooms(catalog.AdditionalTemplates);
            foreach (var error in report.Errors)
            {
                failures.Add($"M16 approved room validation failed: {error}");
            }
        }

        private static void ValidateGraph(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, List<string> failures)
        {
            if (catalog == null || settings == null)
            {
                failures.Add("M16 cannot validate graph without catalog and M15 settings.");
                return;
            }

            var legacy = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "missing legacy sample room JSON";
            ImportedRoomRuntimeAsset legacyAsset = null;
            if (legacy == null || !HollowRuntimeV2Importer.TryImport(legacy.text, out legacyAsset, out importError))
            {
                failures.Add($"M16 could not import legacy sample room: {importError}");
                return;
            }

            var content = BranchSessionContent.Create(legacyAsset, catalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M16 branch content import error: {contentError}");
            }

            if (!content.HasMacroFixturePool)
            {
                failures.Add("M16 content must preserve the complete M13 fixture fallback pool.");
            }

            var graph = BranchGenerator.CreateSeededMacroBranch(content, settings, settings.DefaultSeed);
            if (graph.BranchId != BranchGenerator.SeededMacroBranchId ||
                graph.RoomCount != settings.TargetRoomCount ||
                graph.Connections.Any(connection => !connection.HasExplicitPorts))
            {
                failures.Add("M16 generated branch must remain an M15 seeded explicit-port branch.");
            }
        }

        private static void ValidateScenes(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, List<string> failures)
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

                if (branch.BranchRoomTemplateCatalog != catalog || branch.BranchGenerationSettings != settings)
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to the M16 catalog/settings.");
                }
            }
        }
    }
}
