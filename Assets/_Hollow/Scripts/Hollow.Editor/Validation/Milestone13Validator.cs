using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Editor.Generation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone13Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Rooms/RoomInstanceFootprint.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/BranchRoomInstanceId.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/BranchCellOccupancyMap.cs",
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerFootprintPreset.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone13AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone13Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone13MacroRoomTests.cs",
            "Docs/Milestone13MacroRooms.md"
        };

        [MenuItem("Hollow/Validation/Run Milestone 13 Validation")]
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
                    failures.Add($"Missing M13 file: {file}");
                }
            }

            ValidateFixtures(failures);
            ValidateDesignerDefaults(failures);
            ValidateCompatibility(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 13 validation passed.");
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

        private static void ValidateFixtures(List<string> failures)
        {
            foreach (var fixture in Milestone13AssetGenerator.MacroFixtures)
            {
                if (!File.Exists(fixture.AssetPath))
                {
                    failures.Add($"Missing M13 macro fixture: {fixture.AssetPath}");
                    continue;
                }

                if (!HollowRuntimeV2Importer.TryImport(File.ReadAllText(fixture.AssetPath), out var asset, out var error))
                {
                    failures.Add($"M13 macro fixture failed import {fixture.AssetPath}: {error}");
                    continue;
                }

                if (asset.Layout.WidthTiles != fixture.WidthTiles || asset.Layout.HeightTiles != fixture.HeightTiles)
                {
                    failures.Add($"{fixture.RoomId} dimensions were {asset.Layout.WidthTiles}x{asset.Layout.HeightTiles}, expected {fixture.WidthTiles}x{fixture.HeightTiles}.");
                }

                if (asset.Footprint.OccupiedCellCount != fixture.OccupiedCellCount)
                {
                    failures.Add($"{fixture.RoomId} occupied {asset.Footprint.OccupiedCellCount} branch cells, expected {fixture.OccupiedCellCount}.");
                }

                if (asset.DoorPorts.Count != fixture.DoorPortCount)
                {
                    failures.Add($"{fixture.RoomId} exported {asset.DoorPorts.Count} door ports, expected {fixture.DoorPortCount}.");
                }

                ValidateDoorPortOrdering(asset, failures);
                ValidateNoInternalSeams(asset, failures);
                if (asset.Layout.WalkableTiles.Count < fixture.OccupiedCellCount * RoomDesignerFootprintUtility.ChunkWidthTiles * RoomDesignerFootprintUtility.ChunkHeightTiles)
                {
                    failures.Add($"{fixture.RoomId} walkable tile count is lower than its occupied footprint.");
                }
            }
        }

        private static void ValidateDesignerDefaults(List<string> failures)
        {
            foreach (var fixture in Milestone13AssetGenerator.MacroFixtures)
            {
                var project = RoomDesignerProject.CreateDefault(fixture.Preset, fixture.DisplayName);
                var groundCount = project.cells.Count(cell => cell.kind == RoomDesignerCellKinds.Ground && cell.layer == 0);
                if (project.widthTiles != fixture.WidthTiles || project.heightTiles != fixture.HeightTiles)
                {
                    failures.Add($"Designer preset {fixture.Preset} created {project.widthTiles}x{project.heightTiles}, expected {fixture.WidthTiles}x{fixture.HeightTiles}.");
                }

                if (groundCount != fixture.OccupiedCellCount * RoomDesignerFootprintUtility.ChunkWidthTiles * RoomDesignerFootprintUtility.ChunkHeightTiles)
                {
                    failures.Add($"Designer preset {fixture.Preset} created {groundCount} ground tiles for {fixture.OccupiedCellCount} occupied cells.");
                }

                if (project.doorPorts.Count != fixture.DoorPortCount)
                {
                    failures.Add($"Designer preset {fixture.Preset} created {project.doorPorts.Count} door ports, expected {fixture.DoorPortCount}.");
                }
            }
        }

        private static void ValidateCompatibility(List<string> failures)
        {
            var defaultProject = RoomDesignerProject.CreateDefault();
            if (defaultProject.footprintPreset != RoomDesignerFootprintPreset.Single1x1 || defaultProject.widthTiles != 13 || defaultProject.heightTiles != 7 || defaultProject.doorPorts.Count != 4)
            {
                failures.Add("RoomDesignerProject.CreateDefault() must remain a 13x7 single-room default.");
            }

            var samplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";
            if (File.Exists(samplePath) && HollowRuntimeV2Importer.TryImport(File.ReadAllText(samplePath), out var sample, out _))
            {
                var graph = BranchGenerator.CreateFiveRoomCross(sample);
                if (graph.RoomCount != 5)
                {
                    failures.Add("Current five-room branch compatibility failed after M13.");
                }
            }
        }

        private static void ValidateDoorPortOrdering(ImportedRoomRuntimeAsset asset, List<string> failures)
        {
            foreach (var group in asset.DoorPorts.GroupBy(port => port.Direction))
            {
                var sorted = group.Key is "north" or "south"
                    ? group.OrderBy(port => port.GridEdgeCenter.x).ToArray()
                    : group.OrderBy(port => port.GridEdgeCenter.y).ToArray();
                for (var index = 0; index < sorted.Length; index++)
                {
                    if (sorted[index].LaneIndex != index || sorted[index].Id != $"{group.Key}_{index}")
                    {
                        failures.Add($"{asset.Id} has unstable lane ordering for port {sorted[index].Id}.");
                    }
                }
            }
        }

        private static void ValidateNoInternalSeams(ImportedRoomRuntimeAsset asset, List<string> failures)
        {
            foreach (var port in asset.DoorPorts)
            {
                var adjacent = port.HostCell + DirectionOffset(port.Direction);
                if (asset.Footprint.ContainsCell(adjacent))
                {
                    failures.Add($"{asset.Id} has internal seam door port {port.Id}.");
                }
            }
        }

        private static Vector2Int DirectionOffset(string direction)
        {
            return direction switch
            {
                "north" => new Vector2Int(0, -1),
                "south" => new Vector2Int(0, 1),
                "east" => new Vector2Int(1, 0),
                "west" => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }
    }
}
