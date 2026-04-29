using System;
using System.Collections.Generic;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone45Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Combat/SpikeHazardController.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/DestructibleRoomObjectController.cs",
            "Assets/_Hollow/Scripts/Hollow.Rooms/RoomHazardMarker.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/RoomHazardTuningProfileDefinition.cs"
        };

        [MenuItem("Hollow/Validation/Run Milestone 45 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            foreach (var path in RequiredFiles)
            {
                if (!System.IO.File.Exists(path))
                {
                    failures.Add($"Missing required M45 file: {path}");
                }
            }

            var profile = AssetDatabase.LoadAssetAtPath<RoomHazardTuningProfileDefinition>(Generation.Milestone45AssetGenerator.ProfilePath)
                          ?? RoomHazardTuningProfileDefinition.CreateRuntimeDefault();
            if (profile.SpikeDamage != 1 || profile.ExplosionRadiusMeters < 1.79f)
            {
                failures.Add("M45 hazard tuning profile does not match expected V1 values.");
            }

            if (!Enum.IsDefined(typeof(DamageThreatKind), DamageThreatKind.Environmental))
            {
                failures.Add("DamageThreatKind.Environmental is required.");
            }

            if (!Enum.IsDefined(typeof(RoomDesignerTool), RoomDesignerTool.Spike) ||
                !Enum.IsDefined(typeof(RoomDesignerTool), RoomDesignerTool.StandardBarrel) ||
                !Enum.IsDefined(typeof(RoomDesignerTool), RoomDesignerTool.ExplosiveBarrel))
            {
                failures.Add("Room Designer hazard/barrel tools are required.");
            }

            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "M45 Validation Room");
            project.cells.Add(new RoomDesignerCell(1, 0, 0, RoomDesignerCellKinds.Spike));
            project.markers.Add(new RoomDesignerMarker("barrel_validation", RoomDesignerMarkerKinds.StandardBarrel, 2, 0f, 0));
            var json = RoomDesignerCompiler.ExportRuntimeJson(project);
            if (!HollowRuntimeV2Importer.TryImport(json, out var asset, out var error))
            {
                failures.Add($"M45 compiler/import roundtrip failed: {error}");
            }
            else
            {
                if (asset.Hazards.Count != 1 || asset.InteractiveObjects.Count != 1)
                {
                    failures.Add("M45 compiler/import roundtrip did not preserve one spike and one barrel.");
                }
            }

            if (failures.Count > 0)
            {
                foreach (var failure in failures)
                {
                    Debug.LogError(failure);
                }

                return false;
            }

            Debug.Log("Milestone 45 validation passed.");
            return true;
        }
    }
}
