using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    [Ignore("Superseded by M97 Unity NavMesh runtime replacement tests.")]
    public sealed class Milestone92PathfindingBackendTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void GroundedRequestUsesRoomGridAStarAroundObstacle()
        {
            RoomGridAStarPathfinder.ResetRuntimeStateForTests();
            var root = CreateHarness(out var room, out _);
            try
            {
                var current = new Vector3(0f, 0f, -2.1f);
                var finalGoal = new Vector3(0f, 0f, -0.8f);
                var direct = RoomLocalCollision.ResolveMove(room, current, finalGoal, 0.25f);
                Assert.AreEqual(current.z, direct.z, 0.001f);

                var result = EnemyNavigationAdapter.Resolve(new EnemyNavigationRequest(
                    room,
                    current,
                    new Vector3(0f, 0f, -1.85f),
                    0.25f,
                    EnemyNavigationMode.GroundedLocal,
                    EnemyNavigationIntent.MoveToPlayer,
                    EnemyIntelligenceLevel.Basic,
                    allowLocalDetour: true,
                    allowPathfinding: true,
                    finalGoalLocalPosition: finalGoal,
                    maxStepDistanceMeters: 0.25f));

                Assert.AreEqual(EnemyNavigationBackend.RoomGridAStar, result.Backend);
                Assert.That(result.PathStatus, Is.EqualTo(EnemyPathStatus.Ready).Or.EqualTo(EnemyPathStatus.Partial));
                Assert.Greater(result.PathWaypointCount, 0);
                Assert.IsFalse(RoomLocalCollision.IntersectsObstacle(room, result.ResolvedLocalPosition, 0.25f));
                Assert.IsTrue(result.ResolvedLocalPosition.z > current.z || Mathf.Abs(result.ResolvedLocalPosition.x - current.x) > 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ExemptMovementModesAndCommittedIntentsRemainLocal()
        {
            RoomGridAStarPathfinder.ResetRuntimeStateForTests();
            var root = CreateHarness(out var room, out _);
            try
            {
                var current = new Vector3(0f, 0f, -2.1f);
                var desired = new Vector3(0f, 0f, -0.8f);
                var flying = EnemyNavigationAdapter.Resolve(new EnemyNavigationRequest(
                    room,
                    current,
                    desired,
                    0.25f,
                    EnemyNavigationMode.FlyingLocal,
                    EnemyNavigationIntent.MoveToPlayer,
                    EnemyIntelligenceLevel.Basic,
                    allowLocalDetour: true,
                    allowPathfinding: true,
                    finalGoalLocalPosition: desired));
                var active = EnemyNavigationAdapter.Resolve(new EnemyNavigationRequest(
                    room,
                    current,
                    desired,
                    0.25f,
                    EnemyNavigationMode.GroundedLocal,
                    EnemyNavigationIntent.ActiveLunge,
                    EnemyIntelligenceLevel.Basic,
                    allowLocalDetour: false,
                    allowPathfinding: true,
                    finalGoalLocalPosition: desired));

                Assert.AreEqual(EnemyNavigationBackend.LocalSteering, flying.Backend);
                Assert.AreEqual(EnemyNavigationBackend.LocalSteering, active.Backend);
                Assert.AreEqual(EnemyPathStatus.NotRequested, flying.PathStatus);
                Assert.AreEqual(EnemyPathStatus.NotRequested, active.PathStatus);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimeGroundedEnemyPublishesPathDiagnostics()
        {
            RoomGridAStarPathfinder.ResetRuntimeStateForTests();
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                enemy.transform.localPosition = new Vector3(0f, 0f, -2.1f);
                player.transform.localPosition = new Vector3(0f, 0f, 2.7f);

                enemy.Tick(0.2f, 2f);

                Assert.AreEqual(EnemyNavigationBackend.RoomGridAStar, enemy.LastNavigationBackend);
                Assert.That(enemy.LastNavigationPathStatus, Is.EqualTo(EnemyPathStatus.Ready).Or.EqualTo(EnemyPathStatus.Partial));
                Assert.Greater(enemy.LastNavigationWaypointCount, 0);
                Assert.AreEqual(EnemyNavigationMode.GroundedLocal, enemy.LastNavigationMode);
                Assert.IsFalse(RoomLocalCollision.IntersectsObstacle(room, enemy.transform.localPosition, enemy.RadiusMeters));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ActionEnvelopeGoalSamplesAroundBlockedDirectPoint()
        {
            RoomGridAStarPathfinder.ResetRuntimeStateForTests();
            var root = CreateHarness(out var room, out _);
            try
            {
                var current = new Vector3(0f, 0f, -2.8f);
                var playerAnchor = new Vector3(0f, 0f, 0f);
                var directGoal = new Vector3(0f, 0f, -1.35f);
                Assert.IsTrue(RoomLocalCollision.IntersectsObstacle(room, directGoal, 0.25f));

                var resolved = RoomGridAStarPathfinder.TryResolveActionEnvelopeGoal(
                    room,
                    current,
                    playerAnchor,
                    desiredDistanceMeters: 1.35f,
                    minDistanceMeters: 0.85f,
                    maxDistanceMeters: 1.85f,
                    radiusMeters: 0.25f,
                    intelligence: EnemyIntelligenceLevel.Basic,
                    pathSeed: 2,
                    goalLocalPosition: out var smartGoal);

                Assert.IsTrue(resolved);
                Assert.IsFalse(RoomLocalCollision.IntersectsObstacle(room, smartGoal, 0.25f));
                Assert.Greater(Mathf.Abs(smartGoal.x), 0.2f);
                Assert.LessOrEqual(EnemyNavigationDebugOverlay.Stats.FreshSolvesPerSecond, 1);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RepeatedRequestsReuseOccupancyCache()
        {
            RoomGridAStarPathfinder.ResetRuntimeStateForTests();
            var root = CreateHarness(out var room, out _);
            try
            {
                var current = new Vector3(-2.2f, 0f, -2.1f);
                var desired = new Vector3(-1.95f, 0f, -1.85f);
                var finalGoal = new Vector3(2.2f, 0f, 2.1f);
                for (var index = 0; index < 2; index++)
                {
                    var result = EnemyNavigationAdapter.Resolve(new EnemyNavigationRequest(
                        room,
                        current + new Vector3(index * 0.05f, 0f, 0f),
                        desired + new Vector3(index * 0.05f, 0f, 0f),
                        0.25f,
                        EnemyNavigationMode.GroundedLocal,
                        EnemyNavigationIntent.MoveToPlayer,
                        EnemyIntelligenceLevel.Basic,
                        allowLocalDetour: true,
                        allowPathfinding: true,
                        finalGoalLocalPosition: finalGoal,
                        maxStepDistanceMeters: 0.25f));

                    Assert.AreEqual(EnemyNavigationBackend.RoomGridAStar, result.Backend);
                }

                var stats = EnemyNavigationDebugOverlay.Stats;
                Assert.AreEqual(1, stats.OccupancyBuildsPerSecond);
                Assert.GreaterOrEqual(stats.CacheHitsPerSecond, 1);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PathBudgetDefersLowPriorityRequestsButKeepsHighPriorityReserve()
        {
            RoomGridAStarPathfinder.ResetRuntimeStateForTests();
            var root = CreateHarness(out var room, out _);
            try
            {
                var current = new Vector3(-2.4f, 0f, -2.1f);
                var desired = new Vector3(-2.15f, 0f, -1.85f);
                var finalGoal = new Vector3(2.2f, 0f, 2.1f);
                var deferred = 0;
                for (var index = 0; index < 30; index++)
                {
                    var result = EnemyNavigationAdapter.Resolve(new EnemyNavigationRequest(
                        room,
                        current + new Vector3(index * 0.01f, 0f, 0f),
                        desired + new Vector3(index * 0.01f, 0f, 0f),
                        0.25f,
                        EnemyNavigationMode.GroundedLocal,
                        EnemyNavigationIntent.MoveToPlayer,
                        EnemyIntelligenceLevel.Basic,
                        allowLocalDetour: true,
                        allowPathfinding: true,
                        finalGoalLocalPosition: finalGoal,
                        maxStepDistanceMeters: 0.25f));
                    if (result.PathStatus == EnemyPathStatus.FallbackLocal &&
                        result.FallbackReason == "path_budget_deferred")
                    {
                        deferred++;
                    }
                }

                var highPriority = EnemyNavigationAdapter.Resolve(new EnemyNavigationRequest(
                    room,
                    new Vector3(-2.1f, 0f, -2.1f),
                    new Vector3(-1.85f, 0f, -1.85f),
                    0.25f,
                    EnemyNavigationMode.GroundedLocal,
                    EnemyNavigationIntent.MoveToPlayer,
                    EnemyIntelligenceLevel.Cunning,
                    allowLocalDetour: true,
                    allowPathfinding: true,
                    finalGoalLocalPosition: finalGoal,
                    maxStepDistanceMeters: 0.25f));

                Assert.Greater(deferred, 0);
                Assert.AreEqual(EnemyNavigationBackend.RoomGridAStar, highPriority.Backend);
                Assert.That(highPriority.PathStatus, Is.EqualTo(EnemyPathStatus.Ready).Or.EqualTo(EnemyPathStatus.Partial));
                Assert.Greater(EnemyNavigationDebugOverlay.Stats.BudgetDeferredPerSecond, 0);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone92AssetGenerator.DocsPath), Milestone92AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone92AssetGenerator.ReportPath), Milestone92AssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone92AssetGenerator.DocsPath);
            StringAssert.Contains("Pathfinding Backend Adapter", markdown);
            StringAssert.Contains("RoomGridAStar", markdown);
            StringAssert.Contains("action envelopes", markdown);
            StringAssert.Contains("local steering fallback", markdown);
            Assert.IsTrue(Milestone92Validator.Validate());
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M92Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            playerObject.AddComponent<CombatKnockbackReceiver>().Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            return root;
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(room, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            return enemy;
        }
    }
}
