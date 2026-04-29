using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class CuratedRoomDesignerDraftValidator
    {
        [MenuItem("Hollow/Validation/Run Curated Room Designer Draft Validation")]
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
            ValidateSourceCoverage(failures);
            ValidateCatalog(failures);
            ValidateSceneWiring(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Curated Room Designer draft validation passed.");
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

        private static void ValidateSourceCoverage(List<string> failures)
        {
            var sourcePaths = CuratedRoomDesignerDraftGenerator.SourceRuntimeRoomPaths();
            var expectedCount = Milestone13AssetGenerator.MacroFixtures.Length +
                                Milestone36AssetGenerator.ApprovedRoomIds.Count +
                                Milestone48AssetGenerator.ApprovedRoomIds.Count;
            if (sourcePaths.Count != expectedCount)
            {
                failures.Add($"Expected {expectedCount} curated runtime room sources, found {sourcePaths.Count}.");
            }

            foreach (var sourcePath in sourcePaths)
            {
                if (!File.Exists(sourcePath))
                {
                    failures.Add($"Missing curated runtime source: {sourcePath}");
                    continue;
                }

                if (!HollowRuntimeV2Importer.TryImport(File.ReadAllText(sourcePath), out _, out var error))
                {
                    failures.Add($"Curated runtime source failed HollowRuntime import '{sourcePath}': {error}");
                }
            }
        }

        private static void ValidateCatalog(List<string> failures)
        {
            if (!File.Exists(CuratedRoomDesignerDraftGenerator.CuratedDraftCatalogPath))
            {
                failures.Add($"Missing curated Room Designer catalog: {CuratedRoomDesignerDraftGenerator.CuratedDraftCatalogPath}");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RoomDesignerCuratedDraftCatalogDefinition>(CuratedRoomDesignerDraftGenerator.CuratedDraftCatalogPath);
            if (catalog == null)
            {
                failures.Add("Curated Room Designer catalog asset could not be loaded.");
                return;
            }

            if (catalog.CuratedDrafts.Length != CuratedRoomDesignerDraftGenerator.SourceRuntimeRoomPaths().Count)
            {
                failures.Add($"Curated Room Designer catalog has {catalog.CuratedDrafts.Length} drafts; expected {CuratedRoomDesignerDraftGenerator.SourceRuntimeRoomPaths().Count}.");
            }

            var catalogIds = new HashSet<string>();
            foreach (var textAsset in catalog.CuratedDrafts)
            {
                if (textAsset == null)
                {
                    failures.Add("Curated Room Designer catalog contains a null draft reference.");
                    continue;
                }

                var project = JsonUtility.FromJson<RoomDesignerProject>(textAsset.text);
                if (project == null)
                {
                    failures.Add($"Curated draft '{textAsset.name}' could not be parsed.");
                    continue;
                }

                if (!catalogIds.Add(project.projectId))
                {
                    failures.Add($"Curated draft project id is duplicated: {project.projectId}");
                }

                var report = RoomDesignerDraftValidator.Validate(project);
                foreach (var error in report.Errors)
                {
                    failures.Add($"Curated draft '{project.projectId}' validation error: {error}");
                }

                var runtimeJson = RoomDesignerCompiler.ExportRuntimeJson(project, prettyPrint: false);
                if (!HollowRuntimeV2Importer.TryImport(runtimeJson, out _, out var importError))
                {
                    failures.Add($"Curated draft '{project.projectId}' did not round-trip through HollowRuntime V2: {importError}");
                }
            }

            foreach (var sourcePath in CuratedRoomDesignerDraftGenerator.SourceRuntimeRoomPaths())
            {
                var expectedId = CuratedRoomDesignerDraftGenerator.CuratedProjectIdForRuntimeRoom(File.ReadAllText(sourcePath));
                if (!catalogIds.Contains(expectedId))
                {
                    failures.Add($"Missing curated draft for runtime source '{sourcePath}' with expected project id '{expectedId}'.");
                }
            }
        }

        private static void ValidateSceneWiring(List<string> failures)
        {
            if (!File.Exists(CuratedRoomDesignerDraftGenerator.RoomDesignerScenePath))
            {
                failures.Add($"Missing Room Designer scene: {CuratedRoomDesignerDraftGenerator.RoomDesignerScenePath}");
                return;
            }

            var previousScenePath = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(CuratedRoomDesignerDraftGenerator.RoomDesignerScenePath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<RoomDesignerController>();
            if (controller == null)
            {
                failures.Add("RoomDesigner scene does not contain a RoomDesignerController.");
            }
            else if (controller.CuratedDraftCatalog == null)
            {
                failures.Add("RoomDesignerController is missing the curated draft catalog reference.");
            }

            if (!string.IsNullOrWhiteSpace(previousScenePath) && previousScenePath != scene.path && File.Exists(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }
    }
}
