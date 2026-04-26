using System.IO;
using Hollow.Combat;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone5EnemyArchetypeTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void CatalogResolvesEverySampleSpawnKind()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();

            Assert.AreEqual(EnemyArchetypeId.Normal, catalog.Resolve("spawnEnemyNormal").ArchetypeId);
            Assert.AreEqual(EnemyArchetypeId.Flying, catalog.Resolve("spawnEnemyFlying").ArchetypeId);
            Assert.AreEqual(EnemyArchetypeId.Fast, catalog.Resolve("spawnEnemyFast").ArchetypeId);
            Assert.AreEqual(EnemyArchetypeId.Heavy, catalog.Resolve("spawnEnemyHeavy").ArchetypeId);

            var fallback = EnemyDefinitionResolver.Resolve(catalog, "spawnEnemyUnknown", out var usedFallback);
            Assert.IsTrue(usedFallback);
            Assert.AreEqual(EnemyArchetypeId.Normal, fallback.ArchetypeId);
        }

        [Test]
        public void SpawnServiceCreatesDistinctArchetypeStats()
        {
            var root = CreateHarness(out var room, out var player, out var enemyPrefab);
            try
            {
                var result = EnemySpawnService.SpawnEnemies(new EnemySpawnRequest(
                    room,
                    root.transform,
                    enemyPrefab,
                    player,
                    EnemyCatalog.CreateRuntimeDefault(),
                    DifficultyTierDefinition.CreateRuntimeDeveloperSample(),
                    new CombatDiagnosticsModel()));

                Assert.AreEqual(4, result.Enemies.Count);
                Assert.AreEqual(EnemyArchetypeId.Normal, result.Enemies[0].ArchetypeId);
                Assert.AreEqual(EnemyMovementMode.Flying, result.Enemies[1].MovementMode);
                Assert.AreEqual(2.4f, result.Enemies[2].SpeedMetersPerSecond, 0.0001f);
                Assert.AreEqual(6, result.Enemies[3].Health.MaxHealth);
                Assert.AreEqual(2, result.Enemies[3].ContactDamage);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FlyingEnemiesIgnoreRockCollisionButRespectBounds()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 2.5f);

                var grounded = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                grounded.transform.localPosition = Vector3.zero;
                grounded.Tick(1f, 0f);

                var flying = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                flying.transform.localPosition = Vector3.zero;
                flying.Tick(1f, 0f);

                Assert.Less(grounded.transform.localPosition.z, 0.25f);
                Assert.Greater(flying.transform.localPosition.z, 1.2f);
                Assert.LessOrEqual(flying.transform.localPosition.z, room.LocalBounds.yMax);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HeavyEnemySurvivesMoreProjectileDamageThanFastEnemy()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var fast = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyFast"));
                var heavy = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyHeavy"));

                for (var index = 0; index < 3; index++)
                {
                    DamageSystem.ApplyDamage(fast.Health, new DamageRequest(1, root));
                    DamageSystem.ApplyDamage(heavy.Health, new DamageRequest(1, root));
                }

                Assert.IsFalse(fast.IsAlive);
                Assert.IsTrue(heavy.IsAlive);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DifficultyTierAppliesDeterministicStatMultipliers()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var tier = ScriptableObject.CreateInstance<DifficultyTierDefinition>();
                tier.Configure("Test Tier", 2f, 0.5f, 2f);

                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"), tier);

                Assert.AreEqual(6, enemy.Health.MaxHealth);
                Assert.AreEqual(0.75f, enemy.SpeedMetersPerSecond, 0.0001f);
                Assert.AreEqual(2, enemy.ContactDamage);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DiagnosticsTrackArchetypesAndProjectileReasons()
        {
            var diagnostics = new CombatDiagnosticsModel();
            diagnostics.RecordProjectileDespawn(ProjectileDespawnReason.EnemyHit);
            diagnostics.RecordProjectileDespawn(ProjectileDespawnReason.EnemyHit);
            diagnostics.RecordProjectileDespawn(ProjectileDespawnReason.ObstacleHit);

            Assert.AreEqual(2, diagnostics.ProjectileDespawnsFor(ProjectileDespawnReason.EnemyHit));
            Assert.AreEqual(1, diagnostics.ProjectileDespawnsFor(ProjectileDespawnReason.ObstacleHit));
            Assert.That(diagnostics.ProjectileSummary(), Does.Contain("EnemyHit:2"));
        }

        [Test]
        public void HudModelReportsDifficultyAndArchetypeSummary()
        {
            var model = new CombatHudModel(5, 6, 4, RoomObjectiveState.InCombat, "Developer Sample", "Normal:1  Flying:1", "EnemyHit:2");

            Assert.AreEqual("Developer Sample", model.DifficultyName);
            Assert.That(model.ArchetypeSummary, Does.Contain("Normal:1"));
            Assert.That(model.ProjectileSummary, Does.Contain("EnemyHit:2"));
        }

        [Test]
        public void BossEncounterShellSupportsPlaceholderStateFlow()
        {
            var service = new BossEncounterService();

            Assert.AreEqual(BossEncounterState.None, service.State);
            service.PreparePlaceholderEncounter();
            Assert.AreEqual(BossEncounterState.Prepared, service.State);
            service.StartPlaceholderEncounter();
            Assert.AreEqual(BossEncounterState.Active, service.State);
            service.MarkCleared();
            Assert.AreEqual(BossEncounterState.Cleared, service.State);
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player, out GameObject enemyPrefab)
        {
            var root = new GameObject("M5Harness");

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
            enemyPrefab.AddComponent<ChaserEnemyController>();

            return root;
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition)
        {
            return CreateEnemy(parent, room, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition, DifficultyTierDefinition tier)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(room, player, definition, tier);
            return enemy;
        }
    }
}
