using System.IO;
using Hollow.Combat;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone4CombatLoopTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void GameplayInputCardinalizesShootDirection()
        {
            Assert.AreEqual(new Vector2(1f, 0f), GameplayInputReader.CardinalizeShoot(new Vector2(0.8f, 0.2f)));
            Assert.AreEqual(new Vector2(-1f, 0f), GameplayInputReader.CardinalizeShoot(new Vector2(-0.8f, 0.2f)));
            Assert.AreEqual(new Vector2(0f, 1f), GameplayInputReader.CardinalizeShoot(new Vector2(0.1f, 0.9f)));
            Assert.AreEqual(Vector2.zero, GameplayInputReader.CardinalizeShoot(new Vector2(0.05f, 0.05f)));
        }

        [Test]
        public void PlayerMovementStaysInsideBoundsAndBlocksAgainstRocks()
        {
            var root = CreateCombatHarness(out var room, out var player, out _, out _);
            try
            {
                var movement = player.gameObject.AddComponent<PlayerMovementController>();
                movement.Configure(room);

                player.transform.localPosition = new Vector3(6.2f, 0f, 0f);
                movement.Move(Vector2.right, 1f);
                Assert.LessOrEqual(player.transform.localPosition.x, 6.22f);

                player.transform.localPosition = Vector3.zero;
                movement.Move(Vector2.up, 0.3f);
                Assert.Less(player.transform.localPosition.z, 0.3f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemySpawnerCreatesFourChasersFromImportedMarkers()
        {
            var root = CreateCombatHarness(out var room, out var player, out var enemyPrefab, out _);
            try
            {
                var enemies = EnemySpawnService.SpawnChasers(room, root.transform, enemyPrefab, player);

                Assert.AreEqual(4, enemies.Count);
                Assert.IsTrue(enemies[0].IsAlive);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ChaserPassiveContactDisturbsWithoutDamage()
        {
            var root = CreateCombatHarness(out var room, out var player, out var enemyPrefab, out _);
            try
            {
                var playerHealth = player.gameObject.AddComponent<CombatantHealth>();
                playerHealth.Configure(RoomCombatController.PlayerMaxHealth);

                var enemyObject = Object.Instantiate(enemyPrefab, root.transform);
                enemyObject.transform.localPosition = Vector3.zero;
                var enemy = enemyObject.GetComponent<ChaserEnemyController>();
                enemy.Configure(room, player);

                player.transform.localPosition = new Vector3(0f, 0f, 0.2f);

                Assert.IsFalse(enemy.TryApplyContactDamage(0f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);
                Assert.AreEqual(EnemyAwarenessState.Engaged, enemy.AwarenessState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProjectileDamagesEnemyAndDespawnsOnHit()
        {
            var root = CreateCombatHarness(out _, out _, out var enemyPrefab, out var projectilePrefab);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                combat.Configure(enemyPrefab, projectilePrefab);
                combat.InitializeCombat();

                var enemy = combat.Enemies[0];
                var healthBefore = enemy.Health.CurrentHealth;
                var projectileObject = Object.Instantiate(projectilePrefab, root.transform);
                projectileObject.transform.localPosition = enemy.transform.localPosition;
                var projectile = projectileObject.GetComponent<ProjectileController>();
                projectile.Configure(combat.GetComponentInChildren<RoomRuntimeRoot>(), combat, Vector3.forward);
                projectile.Tick(0f);

                Assert.Less(enemy.Health.CurrentHealth, healthBefore);
                Assert.IsTrue(projectileObject == null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoomObjectiveClearsWhenAllEnemiesDie()
        {
            var root = CreateCombatHarness(out _, out _, out var enemyPrefab, out var projectilePrefab);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                combat.Configure(enemyPrefab, projectilePrefab);
                combat.InitializeCombat();

                Assert.AreEqual(RoomObjectiveState.InCombat, combat.ObjectiveState);
                foreach (var enemy in combat.Enemies)
                {
                    DamageSystem.ApplyDamage(enemy.Health, new DamageRequest(99, root));
                }

                combat.EvaluateRoomState();
                Assert.AreEqual(RoomObjectiveState.Cleared, combat.ObjectiveState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CombatHudModelReportsState()
        {
            var model = new CombatHudModel(4, 6, 2, RoomObjectiveState.InCombat);

            Assert.AreEqual(4, model.PlayerHealth);
            Assert.AreEqual(6, model.PlayerMaxHealth);
            Assert.AreEqual(2, model.EnemiesRemaining);
            Assert.AreEqual("In Combat", model.StatusText);
        }

        private static GameObject CreateCombatHarness(
            out RoomRuntimeRoot room,
            out PlaceholderPlayerController player,
            out GameObject enemyPrefab,
            out GameObject projectilePrefab)
        {
            var root = new GameObject("CombatHarness");

            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();

            enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyPrefab.name = "EnemyBase_TestPrefab";
            enemyPrefab.transform.SetParent(root.transform, false);
            enemyPrefab.SetActive(false);
            enemyPrefab.AddComponent<CombatantHealth>();
            enemyPrefab.AddComponent<ChaserEnemyController>();

            projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.name = "ProjectileBase_TestPrefab";
            projectilePrefab.transform.SetParent(root.transform, false);
            projectilePrefab.SetActive(false);
            projectilePrefab.AddComponent<ProjectileController>();

            return root;
        }
    }
}
