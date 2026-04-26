using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Editor.Generation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone13MacroRoomTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void MacroFixturesImportWithExpectedFootprintsAndPorts()
        {
            foreach (var fixture in Milestone13AssetGenerator.MacroFixtures)
            {
                Assert.IsTrue(File.Exists(fixture.AssetPath), fixture.AssetPath);
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(fixture.AssetPath));

                Assert.AreEqual(HollowRuntimeV2Importer.SupportedSchemaVersion, asset.SourceManifest.hollowRuntime.schemaVersion);
                Assert.AreEqual(fixture.WidthTiles, asset.Layout.WidthTiles, fixture.RoomId);
                Assert.AreEqual(fixture.HeightTiles, asset.Layout.HeightTiles, fixture.RoomId);
                Assert.AreEqual(fixture.OccupiedCellCount, asset.Footprint.OccupiedCellCount, fixture.RoomId);
                Assert.AreEqual(fixture.DoorPortCount, asset.DoorPorts.Count, fixture.RoomId);
                Assert.AreEqual(fixture.OccupiedCellCount * 13 * 7, asset.Layout.WalkableTiles.Count, fixture.RoomId);
                Assert.IsNotNull(asset.SafeStart);
                foreach (var obstacle in asset.Layout.Obstacles.Where(obstacle => obstacle.Kind == RoomDesignerCellKinds.Rock && Mathf.Approximately(obstacle.Size.y, 1f)))
                {
                    Assert.AreEqual(0f, obstacle.Center.y - obstacle.Size.y * 0.5f, 0.0001f, $"{fixture.RoomId}:{obstacle.Id}");
                }
            }
        }

        [Test]
        public void DoorPortsRepresentOnlyExposedChunkFaces()
        {
            foreach (var fixture in Milestone13AssetGenerator.MacroFixtures)
            {
                var asset = RoomDesignerCompiler.Compile(RoomDesignerProject.CreateDefault(fixture.Preset, fixture.DisplayName));
                Assert.AreEqual(fixture.DoorPortCount, HollowRuntimeV2Importer.ExpectedExposedPortCount(asset.Footprint), fixture.RoomId);

                foreach (var port in asset.DoorPorts)
                {
                    Assert.IsTrue(asset.Footprint.ContainsCell(port.HostCell), port.Id);
                    Assert.IsFalse(asset.Footprint.ContainsCell(port.HostCell + DirectionOffset(port.Direction)), $"{fixture.RoomId}:{port.Id}");
                }
            }
        }

        [Test]
        public void DoorPortsKeepStableLaneOrderingForAllMacroShapes()
        {
            foreach (var fixture in Milestone13AssetGenerator.MacroFixtures)
            {
                var asset = RoomDesignerCompiler.Compile(RoomDesignerProject.CreateDefault(fixture.Preset, fixture.DisplayName));
                foreach (var group in asset.DoorPorts.GroupBy(port => port.Direction))
                {
                    var sorted = group.Key is "north" or "south"
                        ? group.OrderBy(port => port.GridEdgeCenter.x).ToArray()
                        : group.OrderBy(port => port.GridEdgeCenter.y).ToArray();
                    for (var index = 0; index < sorted.Length; index++)
                    {
                        Assert.AreEqual(index, sorted[index].LaneIndex, $"{fixture.RoomId}:{sorted[index].Id}");
                        Assert.AreEqual($"{group.Key}_{index}", sorted[index].Id, fixture.RoomId);
                    }
                }
            }
        }

        [Test]
        public void RoomDesignerPresetsCreateCorrectDimensionsOccupiedCellsGroundAndDoorAnchors()
        {
            foreach (var fixture in Milestone13AssetGenerator.MacroFixtures)
            {
                var project = RoomDesignerProject.CreateDefault(fixture.Preset, fixture.DisplayName);
                var occupiedCells = RoomDesignerFootprintUtility.OccupiedCells(fixture.Preset);
                var groundCount = project.cells.Count(cell => cell.kind == RoomDesignerCellKinds.Ground && cell.layer == 0);

                Assert.AreEqual(fixture.WidthTiles, project.widthTiles, fixture.RoomId);
                Assert.AreEqual(fixture.HeightTiles, project.heightTiles, fixture.RoomId);
                Assert.AreEqual(fixture.OccupiedCellCount, occupiedCells.Count, fixture.RoomId);
                Assert.AreEqual(fixture.OccupiedCellCount * 13 * 7, groundCount, fixture.RoomId);
                Assert.AreEqual(fixture.DoorPortCount, project.doorPorts.Count, fixture.RoomId);
                Assert.IsTrue(project.cells
                    .Where(cell => cell.kind == RoomDesignerCellKinds.Rock)
                    .All(cell => cell.layer == 0), fixture.RoomId);
            }
        }

        [Test]
        public void RuntimeRootRendersMacroFloorRegionsAndMultipleSameSideDoorPorts()
        {
            var asset = RoomDesignerCompiler.Compile(RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Wide2x1, "Wide Runtime Test"));
            var rootObject = new GameObject("M13RuntimeRoot");
            try
            {
                var room = rootObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(asset);

                Assert.AreEqual(2, CountChildrenWithPrefix(room.transform, "tileGround.designer_floor_cell_"));
                Assert.AreEqual(6, CountChildrenWithPrefix(room.transform, "doorAnchorActive."));
                Assert.IsTrue(room.TryGetDoorPortById("north_0", out var north0));
                Assert.IsTrue(room.TryGetDoorPortById("north_1", out var north1));
                Assert.AreNotEqual(north0.HostCell, north1.HostCell);
                Assert.IsTrue(room.TryGetDoorPort("north", out _));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void BranchCellOccupancyMapMapsMacroCellsToOneLogicalRoomInstance()
        {
            var asset = RoomDesignerCompiler.Compile(RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.L3Cell, "L Runtime Test"));
            var map = new BranchCellOccupancyMap();
            var instanceId = new BranchRoomInstanceId("macro_l_room");

            Assert.IsTrue(map.TryRegister(instanceId, asset.Footprint));
            Assert.AreEqual(3, map.OwnerByCell.Count);
            foreach (var cell in asset.Footprint.OccupiedCells)
            {
                Assert.IsTrue(map.TryGetOwner(cell, out var owner));
                Assert.AreEqual(instanceId, owner);
            }
        }

        [Test]
        public void CurrentFiveRoomBranchAndDefaultDesignerStayCompatible()
        {
            var defaultProject = RoomDesignerProject.CreateDefault();
            Assert.AreEqual(RoomDesignerFootprintPreset.Single1x1, defaultProject.footprintPreset);
            Assert.AreEqual(13, defaultProject.widthTiles);
            Assert.AreEqual(7, defaultProject.heightTiles);
            Assert.AreEqual(4, defaultProject.doorPorts.Count);

            var graph = BranchGenerator.CreateFiveRoomCross(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));
            Assert.AreEqual(5, graph.RoomCount);
            Assert.IsTrue(graph.TryGetConnection(BranchRoomId.Origin, "north", out _));
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

        private static int CountChildrenWithPrefix(Transform root, string prefix)
        {
            var count = 0;
            for (var index = 0; index < root.childCount; index++)
            {
                if (root.GetChild(index).name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
