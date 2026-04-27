using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone19EnemyEncounterContentTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void RuntimeCatalogResolvesM19BehaviorDefinitions()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();

            Assert.AreEqual(EnemyBehaviorId.Charger, catalog.Resolve("spawnEnemyCharger").BehaviorId);
            Assert.AreEqual(EnemyBehaviorId.TurretShooter, catalog.Resolve("spawnEnemyTurret").BehaviorId);
            Assert.AreEqual(EnemyBehaviorId.Splitter, catalog.Resolve("spawnEnemySplitter").BehaviorId);
            Assert.AreEqual(EnemyBehaviorId.BossWarden, catalog.Resolve("spawnEnemyBoss").BehaviorId);
        }

        [Test]
        public void EncounterPlanIsSeededAndSkipsTreasureRooms()
        {
            var graph = CreateM19Graph();
            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath);

            var first = EncounterResolver.CreateSeededPlan(graph, catalog, graph.Seed);
            var second = EncounterResolver.CreateSeededPlan(graph, catalog, graph.Seed);

            Assert.AreEqual(Signature(first), Signature(second));
            Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError), topologyError);
            var treasure = graph.Rooms.Single(room => room.Role == BranchRoomRole.Treasure);
            Assert.IsFalse(first.TryResolve(treasure.Id.Value, out _));
            var boss = graph.Rooms.Single(room => room.Role == BranchRoomRole.Boss);
            Assert.AreEqual(1, graph.ConnectionsFrom(boss.Id).Count);
            Assert.IsTrue(first.TryResolve(boss.Id.Value, out var bossAssignment));
            Assert.Contains("spawnEnemyBoss", bossAssignment.EnemySpawnKinds.ToArray());
            Assert.IsTrue(first.TryResolve(BranchRoomId.Origin.Value, out var originAssignment));
            Assert.Greater(originAssignment.EnemySpawnKinds.Count, 0);
        }

        [Test]
        public void SpawnServiceUsesEncounterKindsBeforeAuthoredFallback()
        {
            var root = CreateHarness(out var room, out var player, out var enemyPrefab);
            try
            {
                var context = new RoomCombatEncounterContext("test_encounter", new[]
                {
                    "spawnEnemyCharger",
                    "spawnEnemyTurret",
                    "spawnEnemySplitter"
                });

                var result = EnemySpawnService.SpawnEnemies(new EnemySpawnRequest(
                    room,
                    root.transform,
                    enemyPrefab,
                    null,
                    player,
                    EnemyCatalog.CreateRuntimeDefault(),
                    DifficultyTierDefinition.CreateRuntimeDeveloperSample(),
                    new CombatDiagnosticsModel(),
                    context));

                Assert.AreEqual(3, result.Enemies.Count);
                Assert.AreEqual(EnemyBehaviorId.Charger, result.Enemies[0].BehaviorId);
                Assert.AreEqual(EnemyBehaviorId.TurretShooter, result.Enemies[1].BehaviorId);
                Assert.AreEqual(EnemyBehaviorId.Splitter, result.Enemies[2].BehaviorId);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TurretProjectileDamagesPlayerButNotEnemies()
        {
            var root = CreateHarness(out var room, out var player, out var enemyPrefab);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 2f);
                var playerHealth = player.GetComponent<CombatantHealth>();
                var turret = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret"));
                turret.transform.localPosition = Vector3.zero;
                turret.ConfigureSpawnContext(enemyPrefab, null, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample(), new CombatDiagnosticsModel());

                turret.Tick(0.1f, 2f);
                var projectile = root.GetComponentInChildren<EnemyProjectileController>();
                Assert.NotNull(projectile);
                projectile.Tick(0.32f);

                Assert.Less(playerHealth.CurrentHealth, playerHealth.MaxHealth);
                Assert.IsTrue(turret.IsAlive);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EntryGraceBlocksMovementContactAndRangedAttacksUntilExpired()
        {
            var root = CreateHarness(out var room, out var player, out var enemyPrefab);
            try
            {
                player.transform.localPosition = new Vector3(2f, 0f, 0f);
                var chaser = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                chaser.transform.localPosition = Vector3.zero;
                chaser.BeginEntryGrace(RoomCombatController.EntryGraceSeconds, 0f);

                chaser.Tick(0.5f, 0.5f);
                Assert.AreEqual(Vector3.zero, chaser.transform.localPosition);

                chaser.Tick(0.25f, 1.1f);
                Assert.Greater(chaser.transform.localPosition.x, 0f);

                player.transform.localPosition = Vector3.zero;
                var playerHealth = player.GetComponent<CombatantHealth>();
                playerHealth.Configure(RoomCombatController.PlayerMaxHealth);
                var contactEnemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyHeavy"));
                contactEnemy.transform.localPosition = Vector3.zero;
                contactEnemy.BeginEntryGrace(RoomCombatController.EntryGraceSeconds, 2f);

                contactEnemy.Tick(0.2f, 2.5f);
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);

                contactEnemy.Tick(0.2f, 3.1f);
                Assert.Less(playerHealth.CurrentHealth, playerHealth.MaxHealth);

                var turret = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret"));
                turret.ConfigureSpawnContext(enemyPrefab, null, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample(), new CombatDiagnosticsModel());
                turret.transform.localPosition = new Vector3(0f, 0f, -2f);
                turret.BeginEntryGrace(RoomCombatController.EntryGraceSeconds, 4f);

                turret.Tick(0.1f, 4.5f);
                Assert.IsNull(root.GetComponentInChildren<EnemyProjectileController>());

                turret.Tick(0.1f, 5.1f);
                Assert.IsNotNull(root.GetComponentInChildren<EnemyProjectileController>());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SplitterSpawnsChildrenOnDeathAndBossFiresLowHealthBurst()
        {
            var root = CreateHarness(out var room, out var player, out var enemyPrefab);
            try
            {
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var splitter = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemySplitter"));
                splitter.ConfigureSpawnContext(enemyPrefab, null, catalog, DifficultyTierDefinition.CreateRuntimeDeveloperSample(), new CombatDiagnosticsModel());
                var childCount = 0;
                var childStartedInGrace = false;
                splitter.SpawnedChild += child =>
                {
                    childCount++;
                    childStartedInGrace = child.IsInEntryGrace(Time.time);
                };
                DamageSystem.ApplyDamage(splitter.Health, new DamageRequest(99, root));
                Assert.AreEqual(2, childCount);
                Assert.IsTrue(childStartedInGrace);

                var boss = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyBoss"));
                boss.ConfigureSpawnContext(enemyPrefab, null, catalog, DifficultyTierDefinition.CreateRuntimeDeveloperSample(), new CombatDiagnosticsModel());
                DamageSystem.ApplyDamage(boss.Health, new DamageRequest(7, root));
                boss.Tick(0.1f, 3f);
                Assert.GreaterOrEqual(root.GetComponentsInChildren<EnemyProjectileController>().Length, 4);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EncounterPlanPersistsExactAssignments()
        {
            var graph = CreateM19Graph();
            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath);
            var plan = EncounterResolver.CreateSeededPlan(graph, catalog, graph.Seed);

            var restored = EncounterPlan.FromSaveState(plan.ToSaveState());

            Assert.AreEqual(Signature(plan), Signature(restored));
        }

        private static BranchFloorGraph CreateM19Graph()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            return BranchGenerator.CreateSeededEncounterBranch(content, settings, settings.DefaultSeed);
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player, out GameObject enemyPrefab)
        {
            var root = new GameObject("M19Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);

            enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyPrefab.name = "EnemyBase_TestPrefab";
            enemyPrefab.transform.SetParent(root.transform, false);
            enemyPrefab.SetActive(false);
            enemyPrefab.AddComponent<CombatantHealth>();
            enemyPrefab.AddComponent<EnemyRuntimeController>();
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

        private static string Signature(EncounterPlan plan)
        {
            return string.Join("|", plan.Assignments
                .OrderBy(assignment => assignment.RoomId)
                .Select(assignment => $"{assignment.RoomId}:{assignment.EncounterId}:{string.Join(",", assignment.EnemySpawnKinds)}"));
        }
    }
}
