using System;
using Hollow.Branches;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rewards;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone6BranchRuntimeCacheTests
    {
        [SetUp]
        public void SetUp()
        {
            M136PerformanceOperationCounters.Reset();
            RoomRuntimeDescriptorCache.Clear();
            PresentationContentProvider.Reset();
        }

        [Test]
        public void BranchGraphCacheReusesBuildButReturnsFreshMutableGraphCopies()
        {
            var cache = new BranchRuntimeCache();
            var builds = 0;

            var first = cache.GetOrCreateGraph("same-branch", () =>
            {
                builds++;
                return CreateTwoRoomGraph();
            });
            first.GetRoom(BranchRoomId.Origin).MarkCleared();

            var second = cache.GetOrCreateGraph("same-branch", () =>
            {
                builds++;
                return CreateTwoRoomGraph();
            });

            Assert.AreEqual(1, builds);
            Assert.IsFalse(second.GetRoom(BranchRoomId.Origin).IsCleared);
            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.AreEqual(1, snapshot.BranchGraphBuilds);
            Assert.GreaterOrEqual(snapshot.BranchRuntimeCacheHits, 1);
            Assert.GreaterOrEqual(snapshot.BranchRuntimeCacheMisses, 1);
        }

        [Test]
        public void RewardPlanCacheReusesImmutablePlan()
        {
            var cache = new BranchRuntimeCache();
            var builds = 0;

            var first = cache.GetOrCreateRewardPlan("reward-plan", () =>
            {
                builds++;
                return new ProceduralRewardPlan(new[]
                {
                    new RewardGrant("north", "cache", "Cache", RewardKind.Currency, 0, 5, Array.Empty<RewardEffect>())
                });
            });
            var second = cache.GetOrCreateRewardPlan("reward-plan", () =>
            {
                builds++;
                return ProceduralRewardPlan.Empty;
            });

            Assert.AreSame(first, second);
            Assert.AreEqual(1, builds);
            Assert.AreEqual(1, M136PerformanceOperationCounters.Snapshot().BranchRewardPlanBuilds);
        }

        [Test]
        public void RoomDescriptorCacheBuildsOnceForSameRoomAsset()
        {
            var asset = CreateRuntimeRoomAsset("descriptor_room");

            var first = RoomRuntimeDescriptorCache.GetOrCreate(asset);
            var second = RoomRuntimeDescriptorCache.GetOrCreate(asset);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, RoomRuntimeDescriptorCache.Count);
            Assert.AreEqual(1, M136PerformanceOperationCounters.Snapshot().RoomDescriptorBuilds);
        }

        [Test]
        public void ColliderFreePresentationPrefabSkipsPerInstanceColliderStrip()
        {
            var parent = new GameObject("PresentationCacheParent");
            var prefab = new GameObject("ColliderFreePresentationPrefab", typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                var visual = PresentationPrefabResolver.InstantiateVisual(
                    PresentationPrefabRole.RoomFloor,
                    prefab,
                    parent.transform,
                    Vector3.zero,
                    Vector3.one);

                Assert.IsNotNull(visual);
                Assert.AreEqual(0, M136PerformanceOperationCounters.Snapshot().PresentationColliderStripPasses);
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(prefab);
            }
        }

        private static BranchFloorGraph CreateTwoRoomGraph()
        {
            var graph = new BranchFloorGraph("cache_test", 42);
            graph.AddRoom(new BranchRoomState(BranchRoomId.Origin, Vector2Int.zero, new BranchRoomInstanceId("origin"), "descriptor_room", null, BranchRoomRole.Origin));
            graph.AddRoom(new BranchRoomState(BranchRoomId.North, new Vector2Int(0, -1), new BranchRoomInstanceId("north"), "descriptor_room", null, BranchRoomRole.Combat));
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south", "north_0", "south_0");
            return graph;
        }

        private static ImportedRoomRuntimeAsset CreateRuntimeRoomAsset(string id)
        {
            var layout = new RoomLayout(
                3,
                3,
                Rect.MinMaxRect(-1.5f, -1.5f, 1.5f, 1.5f),
                Array.Empty<Vector2Int>(),
                Array.Empty<Vector2Int>(),
                new[] { new RoomLayoutFloorRegion("floor", Vector3.zero, new Vector2(1.5f, 1.5f)) },
                Array.Empty<RoomLayoutObstacle>());
            var safeStart = new ImportedSpawnPoint
            {
                id = "safe_start",
                kind = "spawnSafeStart",
                position = new ImportedVector3 { x = 0f, y = 0f, z = 0f }
            };
            return new ImportedRoomRuntimeAsset(
                id,
                "Descriptor Room",
                RoomBiomeIds.HollowThreshold,
                layout,
                null,
                Array.Empty<RoomDoorPort>(),
                Array.Empty<ImportedSpawnPoint>(),
                Array.Empty<ImportedSpawnPoint>(),
                safeStart,
                Array.Empty<ImportedRoomHazard>(),
                Array.Empty<ImportedRoomInteractiveObject>(),
                Array.Empty<ImportedRoomDecor>(),
                null);
        }
    }
}
