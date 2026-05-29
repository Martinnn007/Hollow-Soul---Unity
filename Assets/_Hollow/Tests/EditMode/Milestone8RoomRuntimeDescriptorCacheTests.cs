using System.Collections;
using System.Collections.Generic;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone8RoomRuntimeDescriptorCacheTests
    {
        [SetUp]
        public void SetUp()
        {
            M136PerformanceOperationCounters.Reset();
            RoomRuntimeDescriptorCache.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            RoomRuntimeDescriptorCache.Clear();
        }

        [Test]
        public void DescriptorCacheReturnsSameDescriptorForUnchangedRoomAsset()
        {
            var asset = CreateRuntimeRoomAsset("m8_cached_room");

            var first = RoomRuntimeDescriptorCache.GetOrCreate(asset);
            var second = RoomRuntimeDescriptorCache.GetOrCreate(asset);

            Assert.AreSame(first, second);
            Assert.AreNotSame(asset, first.Asset);
            Assert.AreEqual(asset.Id, first.AssetId);
            Assert.AreEqual(1, RoomRuntimeDescriptorCache.Count);
            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.AreEqual(1, snapshot.RoomDescriptorBuilds);
            Assert.GreaterOrEqual(snapshot.BranchRuntimeCacheHits, 1);
            Assert.GreaterOrEqual(snapshot.BranchRuntimeCacheMisses, 1);
        }

        [Test]
        public void DescriptorSnapshotsMutableImportedCollectionsAndModels()
        {
            var doors = new List<RoomDoorPort>
            {
                CreateDoorPort("north_0", "north", 0f)
            };
            var hazards = new List<ImportedRoomHazard>
            {
                CreateHazard("hazard_a", new Vector3(0f, 0f, 1f))
            };
            var asset = CreateRuntimeRoomAsset("m8_snapshot_room", doorPorts: doors, hazards: hazards);

            var descriptor = RoomRuntimeDescriptorCache.GetOrCreate(asset);
            doors.Add(CreateDoorPort("east_0", "east", 1f));
            hazards[0].id = "mutated_hazard";
            hazards[0].center.x = 99f;

            Assert.AreEqual(1, descriptor.DoorPorts.Count);
            Assert.AreEqual(1, descriptor.Asset.DoorPorts.Count);
            Assert.AreEqual("hazard_a", descriptor.Hazards[0].id);
            Assert.AreEqual("hazard_a", descriptor.Asset.Hazards[0].id);
            Assert.AreEqual(0f, descriptor.Hazards[0].center.x);
        }

        [Test]
        public void DescriptorKeyChangesForSameIdWhenAuthoringDataChanges()
        {
            var first = CreateRuntimeRoomAsset("m8_key_room", doorX: 0f);
            var movedDoor = CreateRuntimeRoomAsset("m8_key_room", doorX: 1f);
            var otherBiome = CreateRuntimeRoomAsset("m8_key_room", biomeId: RoomBiomeIds.VerdantRuins);

            RoomRuntimeDescriptorCache.GetOrCreate(first);
            RoomRuntimeDescriptorCache.GetOrCreate(movedDoor);
            RoomRuntimeDescriptorCache.GetOrCreate(otherBiome);

            Assert.AreEqual(3, RoomRuntimeDescriptorCache.Count);
            Assert.AreEqual(3, M136PerformanceOperationCounters.Snapshot().RoomDescriptorBuilds);
        }

        [Test]
        public void ImmediateAndStagedBuildsProduceEquivalentRuntimeState()
        {
            var asset = CreateRuntimeRoomAsset("m8_build_equivalence");
            var directObject = new GameObject("M8DirectRoom");
            var stagedObject = new GameObject("M8StagedRoom");
            try
            {
                var direct = directObject.AddComponent<RoomRuntimeRoot>();
                var staged = stagedObject.AddComponent<RoomRuntimeRoot>();

                direct.BuildFrom(asset);
                RunToCompletion(staged.BuildFromStaged(asset, RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake));

                Assert.AreEqual(direct.LastBuiltAsset.Id, staged.LastBuiltAsset.Id);
                Assert.AreEqual(direct.BiomeId, staged.BiomeId);
                Assert.AreEqual(direct.RoomSizeMeters, staged.RoomSizeMeters);
                Assert.AreEqual(direct.LocalBounds, staged.LocalBounds);
                Assert.AreEqual(direct.DoorPorts.Count, staged.DoorPorts.Count);
                Assert.AreEqual(direct.EnemySpawns.Count, staged.EnemySpawns.Count);
                Assert.AreEqual(direct.HazardMarkers.Count, staged.HazardMarkers.Count);
                Assert.AreEqual(direct.InteractiveObjectMarkers.Count, staged.InteractiveObjectMarkers.Count);
                Assert.AreEqual(direct.DynamicNavigationObjects.Count, staged.DynamicNavigationObjects.Count);
                Assert.AreEqual(direct.transform.childCount, staged.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(directObject);
                Object.DestroyImmediate(stagedObject);
            }
        }

        [Test]
        public void CachedDescriptorDoesNotReuseLiveRoomObjects()
        {
            var asset = CreateRuntimeRoomAsset("m8_live_object_room");
            var firstObject = new GameObject("M8FirstRoom");
            var secondObject = new GameObject("M8SecondRoom");
            try
            {
                var descriptor = RoomRuntimeDescriptorCache.GetOrCreate(asset);
                var first = firstObject.AddComponent<RoomRuntimeRoot>();
                var second = secondObject.AddComponent<RoomRuntimeRoot>();

                first.BuildFrom(asset);
                second.BuildFrom(asset);

                Assert.AreSame(descriptor, RoomRuntimeDescriptorCache.GetOrCreate(asset));
                Assert.Greater(first.transform.childCount, 0);
                Assert.Greater(second.transform.childCount, 0);
                Assert.AreNotSame(first.transform.GetChild(0).gameObject, second.transform.GetChild(0).gameObject);
                Assert.AreEqual(1, M136PerformanceOperationCounters.Snapshot().RoomDescriptorBuilds);
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void NullAssetDoesNotBuildDescriptor()
        {
            var descriptor = RoomRuntimeDescriptorCache.GetOrCreate(null);

            Assert.IsNull(descriptor);
            Assert.AreEqual(0, RoomRuntimeDescriptorCache.Count);
            Assert.AreEqual(0, M136PerformanceOperationCounters.Snapshot().RoomDescriptorBuilds);
        }

        private static void RunToCompletion(IEnumerator routine)
        {
            var guard = 0;
            while (routine.MoveNext())
            {
                guard++;
                Assert.Less(guard, 64);
            }
        }

        private static ImportedRoomRuntimeAsset CreateRuntimeRoomAsset(
            string id,
            string biomeId = RoomBiomeIds.HollowThreshold,
            int width = 5,
            int height = 5,
            float doorX = 0f,
            IReadOnlyList<RoomDoorPort> doorPorts = null,
            IReadOnlyList<ImportedRoomHazard> hazards = null)
        {
            var bounds = Rect.MinMaxRect(width * -0.5f, height * -0.5f, width * 0.5f, height * 0.5f);
            var layout = new RoomLayout(
                width,
                height,
                bounds,
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                },
                new[]
                {
                    new Vector2Int(-1, -1)
                },
                new[]
                {
                    new RoomLayoutFloorRegion("floor_a", Vector3.zero, new Vector2(width * 0.5f, height * 0.5f))
                },
                new[]
                {
                    new RoomLayoutObstacle("rock_a", "roomObstacleRock", new Vector3(1f, 0.5f, 0f), Vector3.one, true)
                });
            var footprint = new RoomInstanceFootprint(
                Vector2Int.zero,
                new[] { Vector2Int.zero },
                new Vector2Int(width, height));
            return new ImportedRoomRuntimeAsset(
                id,
                "M8 Room",
                biomeId,
                layout,
                footprint,
                doorPorts ?? new[] { CreateDoorPort("north_0", "north", doorX) },
                new[]
                {
                    new ImportedSpawnPoint
                    {
                        id = "enemy_a",
                        kind = "spawnEnemyRat",
                        position = CreateVector3(-1f, 0f, 0f)
                    }
                },
                new[]
                {
                    new ImportedSpawnPoint
                    {
                        id = "item_a",
                        kind = "spawnReward",
                        position = CreateVector3(1f, 0f, 0f)
                    }
                },
                new ImportedSpawnPoint
                {
                    id = "safe_start",
                    kind = "spawnSafeStart",
                    position = CreateVector3(0f, 0f, -1f)
                },
                hazards ?? new[] { CreateHazard("hazard_a", new Vector3(0f, 0f, 1f)) },
                new[]
                {
                    new ImportedRoomInteractiveObject
                    {
                        id = "barrel_a",
                        kind = RoomInteractiveObjectKind.StandardBarrel,
                        center = CreateVector3(-1f, 0.5f, 1f),
                        size = CreateVector3(0.6f, 1f, 0.6f),
                        blocksMovement = true,
                        blocksProjectiles = true
                    }
                },
                new[]
                {
                    new ImportedRoomDecor
                    {
                        id = "decor_a",
                        kind = "crystal_cluster",
                        center = CreateVector3(1f, 0f, 1f),
                        size = CreateVector3(1f, 1f, 1f),
                        blocking = false,
                        blocksProjectiles = false
                    }
                },
                null);
        }

        private static RoomDoorPort CreateDoorPort(string id, string direction, float x)
        {
            return new RoomDoorPort(
                id,
                direction,
                0,
                Vector2Int.zero,
                new Vector2(x, 2.5f),
                new Vector3(x, 0f, 2.5f),
                "door");
        }

        private static ImportedRoomHazard CreateHazard(string id, Vector3 center)
        {
            return new ImportedRoomHazard
            {
                id = id,
                kind = RoomHazardKind.Spike,
                center = CreateVector3(center.x, center.y, center.z),
                radius = 0.45f
            };
        }

        private static ImportedVector3 CreateVector3(float x, float y, float z)
        {
            return new ImportedVector3
            {
                x = x,
                y = y,
                z = z
            };
        }
    }
}
