using System;
using Hollow.Branches;
using Hollow.Core.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone7DeterministicBranchPlanCacheTests
    {
        [SetUp]
        public void SetUp()
        {
            M136PerformanceOperationCounters.Reset();
        }

        [Test]
        public void RoomDistanceMapCacheBuildsOnceAndFeedsFeaturePlan()
        {
            var cache = new BranchRuntimeCache();
            var graph = CreateFeatureGraph(7001);

            var first = cache.GetOrCreateFeaturePlan(
                graph,
                () => BranchFeaturePlan.Create(graph, cache.GetOrCreateRoomDistanceMap(graph)));
            var second = cache.GetOrCreateFeaturePlan(
                graph,
                () => BranchFeaturePlan.Create(graph, cache.GetOrCreateRoomDistanceMap(graph)));

            Assert.AreSame(first, second);
            Assert.AreEqual("north", first.BossKeyRoomId);
            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.AreEqual(1, snapshot.BranchFeaturePlanBuilds);
            Assert.AreEqual(1, snapshot.BranchRoomDistanceMapBuilds);
            Assert.GreaterOrEqual(snapshot.BranchRuntimeCacheHits, 1);
        }

        [Test]
        public void EncounterPlanCacheReusesDeterministicPlanAndDistanceMap()
        {
            var cache = new BranchRuntimeCache();
            var graph = CreateFeatureGraph(7002);
            var catalog = CreateEncounterCatalog();
            try
            {
                var key = $"encounter|{BranchRuntimeCache.GraphSignature(graph)}|{BranchRuntimeCache.DefinitionKey(catalog)}";
                var first = cache.GetOrCreateEncounterPlan(
                    key,
                    () => EncounterResolver.CreateSeededPlan(graph, catalog, graph.Seed, Array.Empty<string>(), cache.GetOrCreateRoomDistanceMap(graph)));
                var second = cache.GetOrCreateEncounterPlan(
                    key,
                    () => EncounterResolver.CreateSeededPlan(graph, catalog, graph.Seed, Array.Empty<string>(), cache.GetOrCreateRoomDistanceMap(graph)));

                Assert.AreSame(first, second);
                Assert.IsTrue(first.TryResolve("north", out var assignment));
                Assert.AreEqual("cache_combat", assignment.EncounterId);
                var snapshot = M136PerformanceOperationCounters.Snapshot();
                Assert.AreEqual(1, snapshot.BranchEncounterPlanBuilds);
                Assert.AreEqual(1, snapshot.BranchRoomDistanceMapBuilds);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void GraphCacheSeparatesDifferentSessionKeys()
        {
            var cache = new BranchRuntimeCache();
            var first = cache.GetOrCreateGraph("run:1|branch:7003|world:1|biome:hollow", () => CreateFeatureGraph(7003));
            var second = cache.GetOrCreateGraph("run:1|branch:7004|world:1|biome:hollow", () => CreateFeatureGraph(7004));

            Assert.AreEqual(7003, first.Seed);
            Assert.AreEqual(7004, second.Seed);
            Assert.AreEqual(2, M136PerformanceOperationCounters.Snapshot().BranchGraphBuilds);
        }

        private static BranchFloorGraph CreateFeatureGraph(int seed)
        {
            var graph = new BranchFloorGraph(BranchGenerator.BranchFeaturesId, seed);
            graph.AddRoom(new BranchRoomState(BranchRoomId.Origin, Vector2Int.zero, new BranchRoomInstanceId("origin"), "origin_room", null, BranchRoomRole.Origin));
            graph.AddRoom(new BranchRoomState(BranchRoomId.North, new Vector2Int(0, 1), new BranchRoomInstanceId("north"), "combat_room", null, BranchRoomRole.Combat));
            graph.AddRoom(new BranchRoomState(BranchRoomId.East, new Vector2Int(1, 1), new BranchRoomInstanceId("east"), "boss_room", null, BranchRoomRole.Boss));
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south", "north_0", "south_0");
            graph.AddBidirectionalConnection(BranchRoomId.North, BranchRoomId.East, "east", "west", "east_0", "west_0");
            return graph;
        }

        private static EncounterCatalogDefinition CreateEncounterCatalog()
        {
            var combat = ScriptableObject.CreateInstance<EncounterDefinition>();
            combat.Configure(
                "cache_combat",
                "Cache Combat",
                BranchRoomRole.Combat,
                0,
                99,
                1,
                99,
                1,
                new[] { new EncounterSpawnEntry("spawnEnemyNormal", 1) });

            var boss = ScriptableObject.CreateInstance<EncounterDefinition>();
            boss.Configure(
                "cache_boss",
                "Cache Boss",
                BranchRoomRole.Boss,
                0,
                99,
                1,
                99,
                1,
                new[] { new EncounterSpawnEntry("spawnEnemyBoss", 1) });

            var catalog = ScriptableObject.CreateInstance<EncounterCatalogDefinition>();
            catalog.Configure("cache_catalog", new[] { combat }, boss);
            return catalog;
        }
    }
}
