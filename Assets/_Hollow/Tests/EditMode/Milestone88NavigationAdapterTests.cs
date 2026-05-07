using System.IO;
using System.Linq;
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
    public sealed class Milestone88NavigationAdapterTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void AdapterUsesLocalSteeringBackendAndDefaultModes()
        {
            Assert.AreEqual(EnemyNavigationBackend.UnityNavMesh, EnemyNavigationAdapter.CurrentBackend);
            Assert.AreEqual(EnemyNavigationMode.GroundedLocal, EnemyNavigationAdapter.DefaultModeFor(EnemyMovementMode.Grounded));
            Assert.AreEqual(EnemyNavigationMode.FlyingLocal, EnemyNavigationAdapter.DefaultModeFor(EnemyMovementMode.Flying));
        }

        [Test]
        public void GroundedLocalSteeringDetoursWhenDirectMotionStalls()
        {
            var root = CreateHarness(out var room, out _);
            try
            {
                var current = new Vector3(0f, 0f, -2.1f);
                var desired = new Vector3(0f, 0f, -0.8f);
                var direct = RoomLocalCollision.ResolveMove(room, current, desired, 0.25f);
                Assert.AreEqual(current.z, direct.z, 0.001f);

                var result = EnemyNavigationAdapter.Resolve(new EnemyNavigationRequest(
                    room,
                    current,
                    desired,
                    0.25f,
                    EnemyNavigationMode.GroundedLocal,
                    EnemyNavigationIntent.MoveToPlayer,
                    EnemyIntelligenceLevel.Basic));

                Assert.AreEqual(EnemyNavigationBackend.LocalSteering, result.Backend);
                Assert.AreEqual(EnemyNavigationIntent.MoveToPlayer, result.Intent);
                Assert.IsTrue(result.UsedFallbackSteering);
                Assert.Greater(result.ResolvedLocalPosition.z, current.z);
                Assert.IsFalse(RoomLocalCollision.IntersectsObstacle(room, result.ResolvedLocalPosition, 0.25f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FlyingAndPhaseModesAreSeparateFromGroundedObstacleDetours()
        {
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
                    EnemyIntelligenceLevel.Basic));
                var phase = EnemyNavigationAdapter.Resolve(new EnemyNavigationRequest(
                    room,
                    current,
                    desired,
                    0.25f,
                    EnemyNavigationMode.IgnoreObstaclesPhase,
                    EnemyNavigationIntent.PhaseMove,
                    EnemyIntelligenceLevel.Tactical));

                Assert.AreEqual(EnemyNavigationMode.FlyingLocal, flying.Mode);
                Assert.AreEqual(EnemyNavigationMode.IgnoreObstaclesPhase, phase.Mode);
                Assert.IsFalse(flying.UsedFallbackSteering);
                Assert.IsFalse(phase.UsedFallbackSteering);
                Assert.AreEqual(desired.z, flying.ResolvedLocalPosition.z, 0.001f);
                Assert.AreEqual(desired.z, phase.ResolvedLocalPosition.z, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimeMovementPublishesNavigationDiagnostics()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                enemy.transform.localPosition = new Vector3(0f, 0f, -2.1f);
                player.transform.localPosition = new Vector3(0f, 0f, 6f);
                enemy.ApplyIntelligenceDisposition(EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Prey);

                enemy.Tick(0.5f, 2f);

                Assert.That(enemy.LastNavigationBackend, Is.EqualTo(EnemyNavigationBackend.LocalSteering).Or.EqualTo(EnemyNavigationBackend.UnityNavMesh));
                Assert.AreEqual(EnemyNavigationMode.GroundedLocal, enemy.LastNavigationMode);
                Assert.AreEqual(EnemyNavigationIntent.PreferredRange, enemy.LastNavigationIntent);
                Assert.IsTrue(enemy.LastNavigationUsedFallbackSteering || enemy.LastNavigationBackend == EnemyNavigationBackend.UnityNavMesh);
                Assert.IsFalse(RoomLocalCollision.IntersectsObstacle(room, enemy.transform.localPosition, enemy.RadiusMeters));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CurrentRosterResolvesNavigationModes()
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null))
            {
                var mode = EnemyNavigationAdapter.DefaultModeFor(enemy.MovementMode);
                if (enemy.MovementMode == EnemyMovementMode.Flying)
                {
                    Assert.AreEqual(EnemyNavigationMode.FlyingLocal, mode, enemy.SpawnKind);
                }
                else
                {
                    Assert.AreEqual(EnemyNavigationMode.GroundedLocal, mode, enemy.SpawnKind);
                }
            }
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone88AssetGenerator.DocsPath), Milestone88AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone88AssetGenerator.ReportPath), Milestone88AssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone88AssetGenerator.DocsPath);
            StringAssert.Contains("Navigation Adapter", markdown);
            StringAssert.Contains("LocalSteering", markdown);
            StringAssert.Contains("PhaseMove", markdown);
            StringAssert.Contains("M89", markdown);
            Assert.IsTrue(Milestone88Validator.Validate());
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M88Harness");
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
