using System;
using System.IO;
using System.Linq;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone3HollowRuntimeImportTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void ValidSampleJsonImportsAsSchemaV2()
        {
            var asset = ImportSample();

            Assert.AreEqual("combat_single_sample", asset.Id);
            Assert.AreEqual("Combat Single Sample", asset.DisplayName);
            Assert.AreEqual(2, asset.SourceManifest.hollowRuntime.schemaVersion);
        }

        [Test]
        public void MissingOrUnsupportedSchemaFailsClearly()
        {
            Assert.IsFalse(HollowRuntimeV2Importer.TryImport("{}", out _, out var missingError));
            Assert.That(missingError, Does.Contain("missing hollowRuntime"));

            Assert.IsFalse(HollowRuntimeV2Importer.TryImport("{\"hollowRuntime\":{\"schemaVersion\":1}}", out _, out var versionError));
            Assert.That(versionError, Does.Contain("unsupported schemaVersion 1"));
        }

        [Test]
        public void SamplePreservesRoomSemantics()
        {
            var asset = ImportSample();

            Assert.AreEqual(13, asset.Layout.WidthTiles);
            Assert.AreEqual(7, asset.Layout.HeightTiles);
            Assert.AreEqual(91, asset.Layout.WalkableTiles.Count);
            Assert.AreEqual(0, asset.Layout.HoleTiles.Count);
            Assert.AreEqual(4, asset.DoorPorts.Count);
            Assert.AreEqual(16, asset.Layout.Obstacles.Count);
            Assert.AreEqual(4, asset.EnemySpawns.Count);

            var north = asset.DoorPorts.Single(port => port.Id == "north_0");
            Assert.AreEqual("north", north.Direction);
            Assert.AreEqual(0, north.LaneIndex);
            Assert.AreEqual("door", north.Kind);
            Assert.AreEqual(new Vector2(0f, -3.5f), north.GridEdgeCenter);
            Assert.AreEqual(new Vector3(0f, 0f, -3.5f), north.Position);
        }

        [Test]
        public void RuntimeBuilderCreatesStaticFloorDoorsObstaclesAndSpawns()
        {
            var asset = ImportSample();
            var rootObject = new GameObject("RoomRuntimeRoot");

            try
            {
                var room = rootObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(asset);

                Assert.IsNotNull(FindChild(room.transform, "tileGround.floor_full_13x7"));
                Assert.IsNull(FindChild(room.transform, "originMarker_0_0"));
                Assert.AreEqual(4, CountChildrenWithPrefix(room.transform, "doorAnchorActive."));
                Assert.AreEqual(16, CountChildrenWithPrefix(room.transform, "rockTile."));
                var spawnAnchors = room.transform
                    .Cast<Transform>()
                    .Where(child => child.name.IndexOf("spawn", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
                Assert.AreEqual(5, spawnAnchors.Length);
                Assert.AreEqual(0, spawnAnchors.SelectMany(anchor => anchor.GetComponentsInChildren<Renderer>(true)).Count());
                Assert.AreEqual(0, spawnAnchors.SelectMany(anchor => anchor.GetComponentsInChildren<Collider>(true)).Count());
                Assert.IsNotNull(rootObject.GetComponentInChildren<PlayerSpawnPoint>());

                var floor = FindChild(room.transform, "tileGround.floor_full_13x7");
                Assert.AreEqual(0f, floor.localPosition.y + (floor.localScale.y * 0.5f), 0.0001f);

                foreach (var obstacle in asset.Layout.Obstacles)
                {
                    Assert.AreEqual(Mathf.Round(obstacle.Center.x), obstacle.Center.x, 0.0001f);
                    Assert.AreEqual(Mathf.Round(obstacle.Center.z), obstacle.Center.z, 0.0001f);
                }

                foreach (var collider in rootObject.GetComponentsInChildren<BoxCollider>())
                {
                    if (collider.name.StartsWith("rockTile.", StringComparison.Ordinal))
                    {
                        Assert.IsTrue(collider.enabled);
                        Assert.AreEqual(0f, collider.transform.localPosition.y - collider.transform.localScale.y * 0.5f, 0.0001f);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        private static ImportedRoomRuntimeAsset ImportSample()
        {
            return HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
        }

        private static Transform FindChild(Transform root, string childName)
        {
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static int CountChildrenWithPrefix(Transform root, string prefix)
        {
            var count = 0;
            for (var index = 0; index < root.childCount; index++)
            {
                if (root.GetChild(index).name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
