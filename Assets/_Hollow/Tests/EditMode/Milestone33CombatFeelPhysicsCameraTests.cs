using System.IO;
using Hollow.Combat;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone33CombatFeelPhysicsCameraTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void PlayerMovementSubstepsLargeFramesWithoutTunnelingThroughRocks()
        {
            var root = CreateRoomHarness(out var room, out var player);
            try
            {
                var movement = player.gameObject.AddComponent<PlayerMovementController>();
                movement.Configure(room);

                player.transform.localPosition = new Vector3(0f, 0f, -2.2f);
                movement.Move(Vector2.up, 1f);

                Assert.LessOrEqual(player.transform.localPosition.z, -1.8f);
                Assert.IsFalse(RoomLocalCollision.IntersectsObstacle(room, player.transform.localPosition, PlaceholderPlayerController.DefaultRadiusMeters));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerProjectileSubstepsLargeFramesAndHitsThinObstacle()
        {
            var root = CreateRoomHarness(out var room, out _);
            try
            {
                var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectileObject.transform.SetParent(root.transform, false);
                projectileObject.transform.localPosition = new Vector3(0f, 0.45f, -1.8f);
                var projectile = projectileObject.AddComponent<ProjectileController>();
                projectile.Configure(room, null, Vector3.forward, 1, 20f, 1f);

                Assert.IsFalse(projectile.Tick(0.1f));
                Assert.IsTrue(projectileObject == null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyProjectileSubstepsLargeFramesAndHitsPlayer()
        {
            var root = CreateRoomHarness(out var room, out var player);
            try
            {
                var health = player.gameObject.AddComponent<CombatantHealth>();
                health.Configure(6);
                player.transform.localPosition = new Vector3(1f, 0f, 0f);

                var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectileObject.transform.SetParent(root.transform, false);
                projectileObject.transform.localPosition = new Vector3(1f, 0.35f, -1.8f);
                var projectile = projectileObject.AddComponent<EnemyProjectileController>();
                projectile.Configure(room, player, Vector3.forward, 2, 20f);

                Assert.IsFalse(projectile.Tick(0.1f));
                Assert.AreEqual(4, health.CurrentHealth);
                Assert.IsTrue(projectileObject == null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GameplayCameraSnapsAfterLargeRoomTraversalJump()
        {
            var rig = new GameObject("M33CameraRig");
            var target = new GameObject("PlayerCharacter");
            try
            {
                rig.transform.position = new Vector3(0f, 6f, -8f);
                target.transform.position = Vector3.zero;
                var follow = rig.AddComponent<GameplayCameraFollowController>();
                follow.Configure(target.transform, HollowPlatformKind.WindowsStandard3D);

                target.transform.position = new Vector3(20f, 0f, -12f);
                follow.Tick(0.016f);

                Assert.AreEqual(new Vector3(20f, 6f, -12f), rig.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(rig);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void CombatFeelTuningUsesSafeSubstepBudgets()
        {
            Assert.LessOrEqual(CombatFeelTuning.MovementSubstepMeters, 0.25f);
            Assert.LessOrEqual(CombatFeelTuning.ProjectileSubstepMeters, 0.2f);
            Assert.Greater(GameplayCameraFollowController.DefaultSnapDistanceMeters, 4f);
        }

        private static GameObject CreateRoomHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M33CombatFeelHarness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            return root;
        }
    }
}
