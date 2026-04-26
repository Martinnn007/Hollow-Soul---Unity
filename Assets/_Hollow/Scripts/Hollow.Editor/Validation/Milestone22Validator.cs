using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone22Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerDraftLibraryState.cs",
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerValidationReport.cs",
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerExportBundle.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone22AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone22Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone22RoomDesignerMacroAuthoringPolishTests.cs",
            "Docs/Milestone22RoomDesignerMacroAuthoringPolish.md"
        };

        [MenuItem("Hollow/Validation/Run Milestone 22 Validation")]
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
                    failures.Add($"Missing M22 file: {file}");
                }
            }

            ValidatePresets(failures);
            ValidateBranchReadyRules(failures);
            ValidateExports(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 22 validation passed.");
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

        private static void ValidatePresets(List<string> failures)
        {
            foreach (var fixture in Milestone13AssetGenerator.MacroFixtures)
            {
                var project = RoomDesignerProject.CreateDefault(fixture.Preset, fixture.DisplayName);
                var report = RoomDesignerDraftValidator.Validate(project);
                if (!report.IsValid)
                {
                    failures.Add($"M22 default preset {fixture.Preset} must be branch-ready: {string.Join("; ", report.Errors)}");
                }

                var asset = RoomDesignerCompiler.Compile(project);
                if (asset.Layout.WidthTiles != fixture.WidthTiles ||
                    asset.Layout.HeightTiles != fixture.HeightTiles ||
                    asset.Footprint.OccupiedCellCount != fixture.OccupiedCellCount ||
                    asset.DoorPorts.Count != fixture.DoorPortCount)
                {
                    failures.Add($"M22 preset {fixture.Preset} does not preserve M13 macro dimensions/ports.");
                }
            }
        }

        private static void ValidateBranchReadyRules(List<string> failures)
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "M22 Validation");
            project.markers.RemoveAll(marker => marker.kind == RoomDesignerMarkerKinds.SafeStart);
            if (!RoomDesignerDraftValidator.Validate(project).Errors.Any(error => error.Contains("safe-start")))
            {
                failures.Add("M22 validation must block missing safe start markers.");
            }

            project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "M22 Validation");
            foreach (var port in project.doorPorts)
            {
                port.state = RoomDesignerDoorKinds.Inactive;
            }

            if (!RoomDesignerDraftValidator.Validate(project).Errors.Any(error => error.Contains("door port")))
            {
                failures.Add("M22 validation must block rooms with zero enabled door ports.");
            }

            project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "M22 Validation");
            project.markers.RemoveAll(marker => RoomDesignerMarkerKinds.IsEnemy(marker.kind));
            if (!RoomDesignerDraftValidator.Validate(project).Errors.Any(error => error.Contains("enemy spawn")))
            {
                failures.Add("M22 validation must block rooms with no enemy spawn markers.");
            }
        }

        private static void ValidateExports(List<string> failures)
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Wide2x1, "M22 Export");
            project.doorPorts[0].state = RoomDesignerDoorKinds.Inactive;
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_turret_m22", RoomDesignerMarkerKinds.EnemyTurret, 0f, 0f, 0f));
            var tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m22_validator");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }

            var bundle = RoomDesignerExportBundle.Export(project, tempRoot);
            if (!File.Exists(bundle.projectJsonPath) ||
                !File.Exists(bundle.runtimeJsonPath) ||
                !File.Exists(bundle.usdaPath) ||
                !File.Exists(bundle.validationReportPath))
            {
                failures.Add("M22 export bundle must write project JSON, runtime JSON, USDA, and validation report.");
            }

            if (!HollowRuntimeV2Importer.TryImport(File.ReadAllText(bundle.runtimeJsonPath), out var asset, out var error))
            {
                failures.Add($"M22 exported runtime JSON must reimport: {error}");
            }
            else if (asset.DoorPorts.Count != project.doorPorts.Count - 1 ||
                     !asset.EnemySpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.EnemyTurret))
            {
                failures.Add("M22 exported runtime must omit inactive ports and preserve encounter-ready enemy spawn kinds.");
            }
        }
    }
}
