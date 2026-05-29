using System;
using System.Linq;
using Hollow.Branches;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Rewards;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone10And11BranchPreloadCacheBudgetTests
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
        public void PreloadPlannerWarmsCurrentRoomThenReachableAdjacentRoomsWithinBudget()
        {
            var graph = CreatePreloadGraph();
            var state = BranchSessionState.Create(graph);
            state.CurrentRoom.MarkCleared();

            var rooms = BranchPreloadPlanner.BuildPreloadRoomList(state, maxRooms: 3);

            CollectionAssert.AreEqual(
                new[] { "origin", "east", "north" },
                rooms.Select(room => room.Id.Value).ToArray());
        }

        [Test]
        public void PreloadPlannerReprioritizesWhenCurrentRoomChanges()
        {
            var graph = CreatePreloadGraph();
            var state = BranchSessionState.Create(graph);
            state.CurrentRoom.MarkCleared();
            state.EnterRoom(BranchRoomId.North);

            var rooms = BranchPreloadPlanner.BuildPreloadRoomList(state, maxRooms: 2);

            CollectionAssert.AreEqual(
                new[] { "north", "origin" },
                rooms.Select(room => room.Id.Value).ToArray());
        }

        [Test]
        public void BranchRuntimeCacheAppliesPolicyBudgetsAndReportsDebugSummary()
        {
            var policy = new HollowRuntimeCachePolicy(
                maxBranchGraphEntries: 2,
                maxBranchPlanEntries: 2,
                maxRoomAssetEntries: 2,
                maxRoomDescriptorEntries: 96,
                maxPredictivePreloadRooms: 3);
            var cache = new BranchRuntimeCache(policy);

            for (var index = 0; index < 3; index++)
            {
                var roomIndex = index;
                cache.GetOrCreateGraph($"graph_{index}", () => CreateSingleRoomGraph($"branch_{roomIndex}", $"room_{roomIndex}"));
                cache.GetOrCreateRewardPlan($"reward_{index}", () => ProceduralRewardPlan.Empty);
                cache.GetOrCreateRoomAsset($"room_{index}", () => CreateRuntimeRoomAsset($"room_{roomIndex}"));
            }

            var snapshot = cache.Snapshot();
            Assert.AreEqual(2, snapshot.GraphEntries);
            Assert.AreEqual(2, snapshot.RewardPlanEntries);
            Assert.AreEqual(2, snapshot.RoomAssetEntries);
            Assert.AreEqual(3, snapshot.Policy.MaxPredictivePreloadRooms);
            Assert.GreaterOrEqual(M136PerformanceOperationCounters.Snapshot().BranchRuntimeCacheEvictions, 3);

            var summary = cache.CreateDebugSummary();
            StringAssert.Contains("graphs=2/2", summary);
            StringAssert.Contains("roomAssets=2/2", summary);
            StringAssert.Contains("preloadRooms=3", summary);
            StringAssert.Contains("evictionPolicy=lru-count-budget", summary);
        }

        [Test]
        public void RoomDescriptorCacheUsesSharedDefaultDescriptorBudget()
        {
            var maxDescriptors = RoomRuntimeDescriptorCache.MaxDescriptors;
            for (var index = 0; index < maxDescriptors + 1; index++)
            {
                RoomRuntimeDescriptorCache.GetOrCreate(CreateRuntimeRoomAsset($"descriptor_budget_{index}"));
            }

            Assert.AreEqual(maxDescriptors, RoomRuntimeDescriptorCache.Count);
            Assert.GreaterOrEqual(M136PerformanceOperationCounters.Snapshot().BranchRuntimeCacheEvictions, 1);
        }

        private static BranchFloorGraph CreatePreloadGraph()
        {
            var graph = new BranchFloorGraph("m10_preload", 10);
            graph.AddRoom(CreateRoom(BranchRoomId.Origin, "origin_asset", BranchRoomRole.Origin));
            graph.AddRoom(CreateRoom(BranchRoomId.North, "north_asset", BranchRoomRole.Combat));
            graph.AddRoom(CreateRoom(BranchRoomId.East, "east_asset", BranchRoomRole.Reward));
            graph.AddRoom(CreateRoom(BranchRoomId.West, "west_asset", BranchRoomRole.Boss));
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south", "north_0", "south_0", BranchConnectionLockKind.None);
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.East, "east", "west", "east_0", "west_0", BranchConnectionLockKind.None);
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.West, "west", "east", "west_0", "east_0", BranchConnectionLockKind.BossKey);
            return graph;
        }

        private static BranchFloorGraph CreateSingleRoomGraph(string branchId, string roomAssetId)
        {
            var graph = new BranchFloorGraph(branchId, 1);
            graph.AddRoom(CreateRoom(BranchRoomId.Origin, roomAssetId, BranchRoomRole.Origin));
            return graph;
        }

        private static BranchRoomState CreateRoom(BranchRoomId id, string roomAssetId, BranchRoomRole role)
        {
            return new BranchRoomState(
                id,
                Vector2Int.zero,
                new BranchRoomInstanceId(id.Value),
                roomAssetId,
                null,
                role);
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
            return new ImportedRoomRuntimeAsset(
                id,
                id,
                RoomBiomeIds.HollowThreshold,
                layout,
                new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(3, 3)),
                Array.Empty<RoomDoorPort>(),
                Array.Empty<ImportedSpawnPoint>(),
                Array.Empty<ImportedSpawnPoint>(),
                new ImportedSpawnPoint
                {
                    id = "safe_start",
                    kind = "spawnSafeStart",
                    position = new ImportedVector3 { x = 0f, y = 0f, z = 0f }
                },
                Array.Empty<ImportedRoomHazard>(),
                Array.Empty<ImportedRoomInteractiveObject>(),
                Array.Empty<ImportedRoomDecor>(),
                null);
        }
    }
}
