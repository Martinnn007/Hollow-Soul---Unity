using System.IO;
using System.Reflection;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone37EnemyBossReadabilityTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void ChargerTelegraphsBeforeCharging()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                player.transform.localPosition = new Vector3(3f, 0f, 0f);
                var charger = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyCharger"));
                charger.transform.localPosition = Vector3.zero;

                charger.Tick(0.1f, 2f);

                Assert.AreEqual(EnemyReadabilityState.ChargeWindup, charger.ReadabilityState);
                Assert.AreEqual(Vector3.zero, charger.transform.localPosition);

                charger.Tick(0.1f, 2f + EnemyRuntimeController.ChargeWindupSeconds + 0.01f);
                Assert.AreEqual(EnemyReadabilityState.Charging, charger.ReadabilityState);

                charger.Tick(0.1f, 2f + EnemyRuntimeController.ChargeWindupSeconds + 0.12f);
                Assert.Greater(charger.transform.localPosition.x, 0f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TurretTelegraphsBeforeFiring()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 3f);
                var turret = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret"));
                turret.ConfigureSpawnContext(null, null, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample(), new CombatDiagnosticsModel());
                turret.transform.localPosition = Vector3.zero;

                turret.Tick(0.1f, 4f);

                Assert.AreEqual(EnemyReadabilityState.RangedWindup, turret.ReadabilityState);
                Assert.IsNull(root.GetComponentInChildren<EnemyProjectileController>());

                turret.Tick(0.1f, 4f + EnemyRuntimeController.RangedWindupSeconds + 0.01f);
                Assert.NotNull(root.GetComponentInChildren<EnemyProjectileController>());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BossTelegraphsLowHealthBurstBeforeProjectiles()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                player.transform.localPosition = new Vector3(2f, 0f, 2f);
                var boss = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyBoss"));
                boss.ConfigureSpawnContext(null, null, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample(), new CombatDiagnosticsModel());
                DamageSystem.ApplyDamage(boss.Health, new DamageRequest(7, root));

                boss.Tick(0.1f, 5f);

                Assert.AreEqual(EnemyReadabilityState.BossBurstWindup, boss.ReadabilityState);
                Assert.AreEqual(0, root.GetComponentsInChildren<EnemyProjectileController>().Length);

                boss.Tick(0.1f, 5f + EnemyRuntimeController.BossBurstWindupSeconds + 0.01f);
                Assert.GreaterOrEqual(root.GetComponentsInChildren<EnemyProjectileController>().Length, 4);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReadabilityPresenterCreatesNonBlockingTelegraphVisuals()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyCharger"));

                var ring = enemy.transform.Find("EnemyReadabilityTelegraphRing");
                var aim = enemy.transform.Find("EnemyReadabilityAimLine");
                var label = enemy.transform.Find("EnemyReadabilityStateLabel");

                Assert.NotNull(ring);
                Assert.NotNull(aim);
                Assert.NotNull(label);
                Assert.IsFalse(ring.GetComponent<Collider>().enabled);
                Assert.IsFalse(aim.GetComponent<Collider>().enabled);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReadabilityPresenterShowsEnemyDisplayNameInsteadOfArchetypeHp()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));

                var nameLabel = enemy.transform.Find("EnemyNameLabel");
                var legacyHpLabel = enemy.transform.Find("EnemyHpLabel");
                var text = nameLabel != null ? nameLabel.GetComponent<TextMesh>() : null;

                Assert.NotNull(nameLabel);
                Assert.IsNull(legacyHpLabel);
                Assert.NotNull(text);
                Assert.AreEqual("Flying Chaser", text.text);
                Assert.IsFalse(text.text.Contains("/"));
                Assert.IsFalse(text.text.Contains(enemy.ArchetypeId.ToString()));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DamageHealthBarAppearsShrinksAndExpiresAfterDamage()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                var presenter = enemy.GetComponent<CombatReadabilityPresenter>();
                var bar = enemy.transform.Find("EnemyDamageHealthBar");
                var fill = enemy.transform.Find("EnemyDamageHealthBar/EnemyDamageHealthBarFill");

                Assert.NotNull(presenter);
                Assert.NotNull(bar);
                Assert.NotNull(fill);
                Assert.IsFalse(bar.gameObject.activeSelf);

                DamageSystem.ApplyDamage(enemy.Health, new DamageRequest(1, root));

                Assert.IsTrue(bar.gameObject.activeSelf);
                Assert.AreEqual(0.48f, fill.localScale.x, 0.01f);

                TickHealthBarReveal(presenter, 2.6f);

                Assert.IsFalse(bar.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BossesDoNotCreateOverheadNameOrDamageHealthBar()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var boss = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyBoss"));
                DamageSystem.ApplyDamage(boss.Health, new DamageRequest(1, root));

                Assert.IsNull(boss.transform.Find("EnemyNameLabel"));
                Assert.IsNull(boss.transform.Find("EnemyDamageHealthBar"));
                Assert.NotNull(boss.transform.Find("EnemyReadabilityTelegraphRing"));
                Assert.NotNull(boss.transform.Find("EnemyReadabilityAimLine"));
                Assert.NotNull(boss.transform.Find("EnemyReadabilityStateLabel"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TelegraphMaterialRolesResolve()
        {
            Assert.NotNull(MaterialResolver.Resolve(MaterialRole.CombatTelegraphSafe));
            Assert.NotNull(MaterialResolver.Resolve(MaterialRole.CombatTelegraphWarning));
            Assert.NotNull(MaterialResolver.Resolve(MaterialRole.CombatTelegraphDanger));
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M37Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            return root;
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = $"Enemy.{definition.SpawnKind}";
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(room, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            return enemy;
        }

        private static void TickHealthBarReveal(CombatReadabilityPresenter presenter, float deltaTime)
        {
            var method = typeof(CombatReadabilityPresenter).GetMethod("TickHealthBarReveal", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(presenter, new object[] { deltaTime });
        }
    }
}
