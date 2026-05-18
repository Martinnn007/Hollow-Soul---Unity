using System;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class RoomWallRuntimeTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";
        private const string MacroLPath = "Assets/_Hollow/Data/Rooms/MacroFixtures/combat_macro_l_3cell.hollowruntime.json";

        [Test]
        public void RuntimeRootBuildsVisualOnlyPerimeterWallsWithDoorGaps()
        {
            var rootObject = new GameObject("RoomWallRuntimeRoot");
            try
            {
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
                var room = rootObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(asset);

                var controller = rootObject.GetComponent<RoomWallVisibilityController>();
                Assert.IsNotNull(controller);
                CollectionAssert.AreEquivalent(
                    new[] { RoomWallSide.North, RoomWallSide.South, RoomWallSide.East, RoomWallSide.West },
                    controller.WallBindings.Select(binding => binding.Side).Distinct().ToArray());

                var perimeterRoot = rootObject.transform.Find("PerimeterWalls");
                Assert.IsNotNull(perimeterRoot);
                Assert.IsFalse(
                    perimeterRoot.GetComponentsInChildren<Collider>(includeInactive: true).Any(collider => collider.enabled),
                    "Perimeter walls must stay visual-only; RoomLocalCollision owns gameplay boundaries.");

                foreach (var binding in controller.WallBindings)
                {
                    var wall = binding.Renderer.transform;
                    Assert.AreEqual(RoomRuntimeRoot.PerimeterWallHeightMeters, wall.localScale.y, 0.001f, wall.name);
                    Assert.AreEqual(0f, wall.localPosition.y - wall.localScale.y * 0.5f, 0.001f, wall.name);
                    Assert.IsNotNull(binding.Renderer.sharedMaterial, wall.name);
                    var meshFilter = binding.Renderer.GetComponent<MeshFilter>();
                    Assert.IsNotNull(meshFilter, wall.name);
                    Assert.AreEqual(24, meshFilter.sharedMesh.vertexCount, $"{wall.name} should use explicit per-face wall UVs.");
                    Assert.AreEqual(24, meshFilter.sharedMesh.uv.Length, $"{wall.name} should texture every room-facing face.");
                }

                foreach (var port in asset.DoorPorts)
                {
                    var side = WallSideFor(port.Direction);
                    Assert.IsFalse(
                        controller.WallBindings
                            .Where(binding => binding.Side == side)
                            .Any(binding => SegmentCoversPort(binding.Renderer.transform, side, port.Position)),
                        $"Door port {port.Id} should cut a gap through the {port.Direction} wall.");
                }
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                PresentationContentProvider.Reset();
            }
        }

        [Test]
        public void WallVisibilityControllerMakesNearestSideTransparent()
        {
            var rootObject = new GameObject("RoomWallVisibilityRoot");
            var cameraObject = new GameObject("WallVisibilityCamera");
            try
            {
                PresentationContentProvider.Reset();
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
                var room = rootObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(asset);
                var controller = rootObject.GetComponent<RoomWallVisibilityController>();
                Assert.IsNotNull(controller);

                var camera = cameraObject.AddComponent<Camera>();
                var center = new Vector3(asset.Layout.Bounds.center.x, 0.5f, asset.Layout.Bounds.center.y);
                camera.transform.position = new Vector3(asset.Layout.Bounds.center.x, 1.6f, asset.Layout.Bounds.yMin - 4f);
                camera.transform.LookAt(center);
                controller.ApplyVisibility(camera);
                AssertTransparentSide(controller, RoomWallSide.North);

                camera.transform.position = new Vector3(asset.Layout.Bounds.center.x, 1.6f, asset.Layout.Bounds.yMax + 4f);
                camera.transform.LookAt(center);
                controller.ApplyVisibility(camera);
                AssertTransparentSide(controller, RoomWallSide.South);

                camera.transform.position = new Vector3(asset.Layout.Bounds.xMax + 4f, 1.6f, asset.Layout.Bounds.center.y);
                camera.transform.LookAt(center);
                controller.ApplyVisibility(camera);
                AssertTransparentSide(controller, RoomWallSide.East);

                camera.transform.position = new Vector3(asset.Layout.Bounds.xMin - 4f, 1.6f, asset.Layout.Bounds.center.y);
                camera.transform.LookAt(center);
                controller.ApplyVisibility(camera);
                AssertTransparentSide(controller, RoomWallSide.West);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(rootObject);
                PresentationContentProvider.Reset();
            }
        }

        [Test]
        public void WallVisibilityControllerFadesTwoNearSidesForDiagonalCamera()
        {
            var rootObject = new GameObject("RoomWallDiagonalVisibilityRoot");
            var cameraObject = new GameObject("WallVisibilityDiagonalCamera");
            try
            {
                PresentationContentProvider.Reset();
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
                var room = rootObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(asset);
                var controller = rootObject.GetComponent<RoomWallVisibilityController>();
                Assert.IsNotNull(controller);

                var camera = cameraObject.AddComponent<Camera>();
                var center = new Vector3(asset.Layout.Bounds.center.x, 0.5f, asset.Layout.Bounds.center.y);
                camera.transform.position = new Vector3(asset.Layout.Bounds.xMin - 4f, 2.8f, asset.Layout.Bounds.yMin - 4f);
                camera.transform.LookAt(center);
                var arpgRotation = camera.transform.rotation;
                controller.ApplyVisibility(camera);

                CollectionAssert.AreEquivalent(
                    new[] { RoomWallSide.North, RoomWallSide.West },
                    controller.CurrentTransparentSides.ToArray());
                AssertTransparentSides(controller, RoomWallSide.North, RoomWallSide.West);

                camera.transform.position = new Vector3(asset.Layout.Bounds.xMin + 5f, 2.8f, asset.Layout.Bounds.yMin + 5f);
                camera.transform.rotation = arpgRotation;
                controller.ApplyVisibility(camera);
                CollectionAssert.AreEquivalent(
                    new[] { RoomWallSide.North, RoomWallSide.West },
                    controller.CurrentTransparentSides.ToArray());
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(rootObject);
                PresentationContentProvider.Reset();
            }
        }

        [Test]
        public void WallVisibilityControllerUsesRoomLocalViewDirectionForRotatedRoots()
        {
            var rootObject = new GameObject("RoomWallRotatedVisibilityRoot");
            var cameraObject = new GameObject("WallVisibilityRotatedCamera");
            try
            {
                PresentationContentProvider.Reset();
                rootObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
                var room = rootObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(asset);
                var controller = rootObject.GetComponent<RoomWallVisibilityController>();
                Assert.IsNotNull(controller);

                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.rotation = Quaternion.LookRotation(
                    rootObject.transform.TransformDirection(new Vector3(1f, -0.55f, 1f).normalized),
                    Vector3.up);
                controller.ApplyVisibility(camera);

                CollectionAssert.AreEquivalent(
                    new[] { RoomWallSide.North, RoomWallSide.West },
                    controller.CurrentTransparentSides.ToArray());
                AssertTransparentSides(controller, RoomWallSide.North, RoomWallSide.West);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(rootObject);
                PresentationContentProvider.Reset();
            }
        }

        [Test]
        public void RuntimeRootBuildsWallsAroundLShapedFloorRegions()
        {
            var rootObject = new GameObject("RoomWallLShapeRoot");
            try
            {
                PresentationContentProvider.Reset();
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(MacroLPath));
                var room = rootObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(asset);
                var controller = rootObject.GetComponent<RoomWallVisibilityController>();
                Assert.IsNotNull(controller);

                var halfThickness = RoomRuntimeRoot.PerimeterWallThicknessMeters * 0.5f;
                Assert.IsTrue(
                    HasSegmentCovering(controller, RoomWallSide.South, halfThickness, 2f),
                    "The open top-right 13x7 cell should create an internal south-facing notch wall along z=0.");
                Assert.IsTrue(
                    HasSegmentCovering(controller, RoomWallSide.East, halfThickness, 1f),
                    "The open top-right 13x7 cell should create an internal east-facing notch wall along x=0.");
                Assert.IsFalse(
                    HasSegmentCovering(controller, RoomWallSide.South, 7f + halfThickness, 6.5f),
                    "The missing top-right 13x7 cell should not keep the old rectangular south wall.");
                Assert.IsFalse(
                    HasSegmentCovering(controller, RoomWallSide.East, 13f + halfThickness, 3.5f),
                    "The missing top-right 13x7 cell should not keep the old rectangular east wall.");
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                PresentationContentProvider.Reset();
            }
        }

        private static void AssertTransparentSide(RoomWallVisibilityController controller, RoomWallSide expectedSide)
        {
            AssertTransparentSides(controller, expectedSide);
        }

        private static void AssertTransparentSides(RoomWallVisibilityController controller, params RoomWallSide[] expectedSides)
        {
            Assert.AreEqual(expectedSides[0], controller.CurrentTransparentSide);
            var opaque = MaterialResolver.Resolve(MaterialRole.RoomWall);
            var transparent = MaterialResolver.Resolve(MaterialRole.RoomWallTransparent);
            foreach (var binding in controller.WallBindings)
            {
                var expected = expectedSides.Contains(binding.Side) ? transparent : opaque;
                Assert.AreSame(expected, binding.Renderer.sharedMaterial, $"{binding.Renderer.name}:{binding.Side}");
            }
        }

        private static bool SegmentCoversPort(Transform segment, RoomWallSide side, Vector3 portPosition)
        {
            const float tolerance = 0.001f;
            if (side is RoomWallSide.North or RoomWallSide.South)
            {
                var min = segment.localPosition.x - segment.localScale.x * 0.5f;
                var max = segment.localPosition.x + segment.localScale.x * 0.5f;
                return portPosition.x > min + tolerance && portPosition.x < max - tolerance;
            }

            var zMin = segment.localPosition.z - segment.localScale.z * 0.5f;
            var zMax = segment.localPosition.z + segment.localScale.z * 0.5f;
            return portPosition.z > zMin + tolerance && portPosition.z < zMax - tolerance;
        }

        private static bool HasSegmentCovering(RoomWallVisibilityController controller, RoomWallSide side, float fixedCoordinate, float axisPosition)
        {
            const float tolerance = 0.02f;
            return controller.WallBindings
                .Where(binding => binding.Side == side)
                .Any(binding =>
                {
                    var segment = binding.Renderer.transform;
                    if (side is RoomWallSide.North or RoomWallSide.South)
                    {
                        var min = segment.localPosition.x - segment.localScale.x * 0.5f;
                        var max = segment.localPosition.x + segment.localScale.x * 0.5f;
                        return Mathf.Abs(segment.localPosition.z - fixedCoordinate) <= tolerance &&
                               axisPosition > min + tolerance &&
                               axisPosition < max - tolerance;
                    }

                    var zMin = segment.localPosition.z - segment.localScale.z * 0.5f;
                    var zMax = segment.localPosition.z + segment.localScale.z * 0.5f;
                    return Mathf.Abs(segment.localPosition.x - fixedCoordinate) <= tolerance &&
                           axisPosition > zMin + tolerance &&
                           axisPosition < zMax - tolerance;
                });
        }

        private static RoomWallSide WallSideFor(string direction)
        {
            return direction switch
            {
                "north" => RoomWallSide.North,
                "south" => RoomWallSide.South,
                "east" => RoomWallSide.East,
                "west" => RoomWallSide.West,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown room wall direction.")
            };
        }
    }
}
