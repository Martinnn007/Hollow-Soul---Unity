using System.Collections.Generic;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone114PhysicalEnemyBodiesTests
    {
        [Test]
        public void GroundedEnemyBodyBlocksPlayerMovementAndEmitsBump()
        {
            var root = CreateHarness(out var room, out var player, out var movement, out var combat);
            try
            {
                var health = player.GetComponent<CombatantHealth>();
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyNormal");
                enemy.transform.localPosition = new Vector3(0f, 0f, 0.85f);
                combat.RegisterRuntimeEnemy(enemy);

                movement.Move(Vector2.up, 0.45f);

                Assert.Less(player.transform.localPosition.z, 0.35f);
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, health.CurrentHealth);
                Assert.AreEqual(EnemyStimulusKind.Bump, enemy.LastStimulusKind);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GroundedEnemyBodyBlocksRollTravel()
        {
            var root = CreateHarness(out var room, out var player, out var movement, out var combat);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyNormal");
                enemy.transform.localPosition = new Vector3(0f, 0f, 0.85f);
                combat.RegisterRuntimeEnemy(enemy);

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.up, 0f));
                movement.Move(Vector2.zero, PlayerWeaponController.RollTravelSeconds);

                Assert.Less(player.transform.localPosition.z, 0.35f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RollStraightIntoEnemyDeflectsToDeterministicSideWithoutPassingThrough()
        {
            var root = CreateHarness(out var room, out var player, out var movement, out var combat);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyNormal");
                enemy.transform.localPosition = new Vector3(0f, 0f, 0.85f);
                combat.RegisterRuntimeEnemy(enemy);

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.up, 0f));
                movement.Move(Vector2.zero, PlayerWeaponController.RollTravelSeconds);

                var flatDelta = player.transform.localPosition - enemy.transform.localPosition;
                flatDelta.y = 0f;
                var minimumDistance = PlaceholderPlayerController.DefaultRadiusMeters + enemy.RadiusMeters + PlayerEnemyBodyCollision.BodySkinMeters;
                Assert.Greater(player.transform.localPosition.x, 0.12f);
                Assert.Less(player.transform.localPosition.x, 0.75f);
                Assert.Less(player.transform.localPosition.z, enemy.transform.localPosition.z);
                Assert.GreaterOrEqual(flatDelta.magnitude, minimumDistance - 0.02f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DiagonalRollIntoEnemyDeflectsTowardInitialLateralSide()
        {
            var root = CreateHarness(out var room, out var player, out var movement, out var combat);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyNormal");
                enemy.transform.localPosition = new Vector3(0f, 0f, 0.85f);
                combat.RegisterRuntimeEnemy(enemy);

                Assert.IsTrue(weapon.TryRoll(new Vector2(-0.45f, 1f), Vector2.up, 0f));
                movement.Move(Vector2.zero, PlayerWeaponController.RollTravelSeconds);

                var flatDelta = player.transform.localPosition - enemy.transform.localPosition;
                flatDelta.y = 0f;
                var minimumDistance = PlaceholderPlayerController.DefaultRadiusMeters + enemy.RadiusMeters + PlayerEnemyBodyCollision.BodySkinMeters;
                Assert.Less(player.transform.localPosition.x, -0.12f);
                Assert.Less(player.transform.localPosition.z, enemy.transform.localPosition.z);
                Assert.GreaterOrEqual(flatDelta.magnitude, minimumDistance - 0.02f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RollDeflectionFailsClosedWhenChosenSideIsBlocked()
        {
            var sideBlocker = new RoomLayoutObstacle(
                "east_side_blocker",
                "rock",
                new Vector3(0.45f, 0f, 0.25f),
                new Vector3(0.3f, 1f, 1f),
                true);
            var root = CreateHarness(out var room, out var player, out var movement, out var combat, CreateRoomWithObstacles(sideBlocker));
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyNormal");
                enemy.transform.localPosition = new Vector3(0f, 0f, 0.85f);
                combat.RegisterRuntimeEnemy(enemy);

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.up, 0f));
                movement.Move(Vector2.zero, PlayerWeaponController.RollTravelSeconds);

                Assert.Less(player.transform.localPosition.x, 0.08f);
                Assert.Less(player.transform.localPosition.z, 0.35f);
                Assert.IsFalse(RoomLocalCollision.IntersectsObstacle(room, player.transform.localPosition, PlaceholderPlayerController.DefaultRadiusMeters));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LightEnemyReceivesTinyMovementNudgeButStillBlocksPlayer()
        {
            var root = CreateHarness(out var room, out var player, out var movement, out var combat);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyRat");
                var initialEnemyPosition = new Vector3(0f, 0f, 0.85f);
                enemy.transform.localPosition = initialEnemyPosition;
                combat.RegisterRuntimeEnemy(enemy);

                movement.Move(Vector2.up, 0.45f);

                var enemyDelta = enemy.transform.localPosition - initialEnemyPosition;
                var minimumDistance = PlaceholderPlayerController.DefaultRadiusMeters + enemy.RadiusMeters + PlayerEnemyBodyCollision.BodySkinMeters;
                Assert.AreEqual(EnemyBodyClass.Light, enemy.BodyClass);
                Assert.Greater(enemyDelta.z, 0f);
                Assert.Less(enemyDelta.z, 0.12f);
                Assert.GreaterOrEqual(enemy.transform.localPosition.z - player.transform.localPosition.z, minimumDistance - 0.02f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HeavyEnemyDoesNotMoveWhenPlayerWalksIntoIt()
        {
            var root = CreateHarness(out var room, out var player, out var movement, out var combat);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyHeavy");
                var initialEnemyPosition = new Vector3(0f, 0f, 0.85f);
                enemy.transform.localPosition = initialEnemyPosition;
                combat.RegisterRuntimeEnemy(enemy);

                movement.Move(Vector2.up, 0.45f);

                Assert.AreEqual(EnemyBodyClass.Heavy, enemy.BodyClass);
                Assert.AreEqual(initialEnemyPosition.z, enemy.transform.localPosition.z, 0.0001f);
                Assert.Less(player.transform.localPosition.z, 0.35f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RollCanOnlyTinyNudgeLightEnemyAndCannotPassThrough()
        {
            var root = CreateHarness(out var room, out var player, out var movement, out var combat);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyRat");
                var initialEnemyPosition = new Vector3(0f, 0f, 0.85f);
                enemy.transform.localPosition = initialEnemyPosition;
                combat.RegisterRuntimeEnemy(enemy);

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.up, 0f));
                movement.Move(Vector2.zero, PlayerWeaponController.RollTravelSeconds);

                var enemyDelta = enemy.transform.localPosition - initialEnemyPosition;
                var minimumDistance = PlaceholderPlayerController.DefaultRadiusMeters + enemy.RadiusMeters + PlayerEnemyBodyCollision.BodySkinMeters;
                Assert.Greater(enemyDelta.z, 0f);
                Assert.Less(enemyDelta.z, 0.2f);
                Assert.GreaterOrEqual(enemy.transform.localPosition.z - player.transform.localPosition.z, minimumDistance - 0.02f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FlyingEnemyBodyDoesNotBlockPlayerMovement()
        {
            var root = CreateHarness(out var room, out var player, out var movement, out var combat);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyFlying");
                enemy.transform.localPosition = new Vector3(0f, 0f, 0.85f);
                combat.RegisterRuntimeEnemy(enemy);

                movement.Move(Vector2.up, 0.45f);

                Assert.Greater(player.transform.localPosition.z, 1.2f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartingOverlappedWithGroundedEnemySeparatesWithoutDamage()
        {
            var root = CreateHarness(out var room, out var player, out var movement, out var combat);
            try
            {
                var health = player.GetComponent<CombatantHealth>();
                var enemy = CreateEnemy(root.transform, room, player, "spawnEnemyNormal");
                enemy.transform.localPosition = Vector3.zero;
                combat.RegisterRuntimeEnemy(enemy);

                movement.Move(Vector2.zero, 0.1f);

                var flatPlayer = player.transform.localPosition;
                flatPlayer.y = 0f;
                Assert.Greater(Vector3.Distance(flatPlayer, Vector3.zero), 0.55f);
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShortSoulsRollTuningUsesReducedTravelAndIFrames()
        {
            Assert.AreEqual(30f, PlayerWeaponController.RollStaminaCost, 0.0001f);
            Assert.AreEqual(0.04f, PlayerWeaponController.RollStartupSeconds, 0.0001f);
            Assert.AreEqual(0.22f, PlayerWeaponController.RollInvulnerabilitySeconds, 0.0001f);
            Assert.AreEqual(0.18f, PlayerWeaponController.RollRecoverySeconds, 0.0001f);
            Assert.AreEqual(1.25f, PlayerWeaponController.RollDistanceMeters, 0.0001f);
            Assert.AreEqual(0.44f, PlayerWeaponController.RollDurationSeconds, 0.0001f);
            Assert.AreEqual(0.32f, PlayerEnemyBodyCollision.TangentSlideMultiplier, 0.0001f);
            Assert.AreEqual(0.004f, PlayerEnemyBodyCollision.LightWalkShoveMaxMeters, 0.0001f);
            Assert.AreEqual(0.008f, PlayerEnemyBodyCollision.LightRollShoveMaxMeters, 0.0001f);
            Assert.AreEqual(0.72f, PlayerEnemyBodyCollision.RollBodyDeflectMultiplier, 0.0001f);
            Assert.AreEqual(0.11f, PlayerEnemyBodyCollision.RollBodyDeflectMaxMetersPerSubstep, 0.0001f);
            Assert.AreEqual(0.35f, PlayerEnemyBodyCollision.RollBodyDeflectPressureThreshold, 0.0001f);
        }

        private static GameObject CreateHarness(
            out RoomRuntimeRoot room,
            out PlaceholderPlayerController player,
            out PlayerMovementController movement,
            out RoomCombatController combat,
            ImportedRoomRuntimeAsset roomAsset = null)
        {
            var root = new GameObject("M114Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(roomAsset ?? CreateOpenRoom());

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            playerObject.AddComponent<CombatKnockbackReceiver>().Configure(room, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            playerObject.AddComponent<PlayerDamageFeedbackController>().Configure(room, null);
            playerObject.AddComponent<PlayerWeaponController>();
            movement = playerObject.AddComponent<PlayerMovementController>();

            combat = root.AddComponent<RoomCombatController>();
            movement.Configure(room, combat);
            return root;
        }

        private static EnemyRuntimeController CreateEnemy(
            Transform parent,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            string spawnKind)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(
                room,
                player,
                EnemyCatalog.CreateRuntimeDefault().Resolve(spawnKind),
                DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            return enemy;
        }

        private static ImportedRoomRuntimeAsset CreateOpenRoom()
        {
            return CreateRoomWithObstacles(System.Array.Empty<RoomLayoutObstacle>());
        }

        private static ImportedRoomRuntimeAsset CreateRoomWithObstacles(params RoomLayoutObstacle[] obstacles)
        {
            var bounds = Rect.MinMaxRect(-4f, -4f, 4f, 4f);
            var walkable = new List<Vector2Int>();
            for (var x = -4; x <= 4; x++)
            {
                for (var z = -4; z <= 4; z++)
                {
                    walkable.Add(new Vector2Int(x, z));
                }
            }

            var layout = new RoomLayout(
                8,
                8,
                bounds,
                walkable,
                System.Array.Empty<Vector2Int>(),
                new[] { new RoomLayoutFloorRegion("floor", Vector3.zero, new Vector2(4f, 4f)) },
                obstacles ?? System.Array.Empty<RoomLayoutObstacle>());

            return new ImportedRoomRuntimeAsset(
                "m114_open_room",
                "M114 Open Room",
                layout,
                new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(8, 8)),
                System.Array.Empty<RoomDoorPort>(),
                System.Array.Empty<ImportedSpawnPoint>(),
                System.Array.Empty<ImportedSpawnPoint>(),
                new ImportedSpawnPoint
                {
                    id = "safe_start",
                    kind = "spawn_point_safeStart",
                    position = new ImportedVector3 { x = 0f, y = 0f, z = 0f }
                },
                System.Array.Empty<ImportedRoomHazard>(),
                System.Array.Empty<ImportedRoomInteractiveObject>(),
                System.Array.Empty<ImportedRoomDecor>(),
                null);
        }
    }
}
