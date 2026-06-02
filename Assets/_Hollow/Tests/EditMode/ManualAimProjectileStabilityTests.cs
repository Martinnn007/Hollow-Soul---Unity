using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class ManualAimProjectileStabilityTests
    {
        [Test]
        public void ProjectileTravelEqualsConfiguredSpeedAcrossFixedDeltas()
        {
            var projectileObject = new GameObject("Projectile");
            try
            {
                var projectile = projectileObject.AddComponent<ProjectileController>();
                projectile.Configure(null, null, Vector3.forward, 1, PlayerWeaponController.PlayerProjectileSpeedMetersPerSecond, 10f);

                const int steps = 12;
                const float deltaTime = 0.025f;
                for (var index = 0; index < steps; index++)
                {
                    projectile.Tick(deltaTime);
                }

                var expectedDistance = PlayerWeaponController.PlayerProjectileSpeedMetersPerSecond * steps * deltaTime;
                Assert.AreEqual(expectedDistance, projectileObject.transform.localPosition.z, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(projectileObject);
            }
        }

        [Test]
        public void ProjectileTravelEqualsConfiguredSpeedAcrossVariableDeltas()
        {
            var projectileObject = new GameObject("Projectile");
            try
            {
                var direction = new Vector3(0.6f, 0f, 0.8f).normalized;
                var projectile = projectileObject.AddComponent<ProjectileController>();
                projectile.Configure(null, null, direction, 1, PlayerWeaponController.PlayerProjectileSpeedMetersPerSecond, 10f);

                var elapsed = 0f;
                var deltas = new[] { 0.016f, 0.033f, 0.008f, 0.041f, 0.025f };
                for (var index = 0; index < deltas.Length; index++)
                {
                    elapsed += deltas[index];
                    projectile.Tick(deltas[index]);
                }

                var expected = direction * PlayerWeaponController.PlayerProjectileSpeedMetersPerSecond * elapsed;
                Assert.AreEqual(expected.x, projectileObject.transform.localPosition.x, 0.001f);
                Assert.AreEqual(expected.z, projectileObject.transform.localPosition.z, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(projectileObject);
            }
        }

        [Test]
        public void LightEnemyProjectileDamageDoesNotCancelCommittedRangedWindup()
        {
            var harness = CreateAttackHarness("LightProjectileInterruptionHarness");
            try
            {
                Assert.IsTrue(harness.Weapon.TryFire(Vector2.up, 10f));
                Assert.IsTrue(harness.Weapon.IsAttackCommitted);

                ApplyProjectileDamage(harness.Health, DamageThreatKind.Light, ImpactForceClass.Light);

                Assert.IsTrue(harness.Weapon.IsAttackCommitted);
                harness.Weapon.TickAction(0f, 10f + WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);

                var projectile = harness.Root.transform.Find("PlayerProjectile");
                Assert.IsNotNull(projectile);
                Assert.AreEqual(Vector3.forward.x, projectile.GetComponent<ProjectileController>().ConfiguredLocalDirection.x, 0.001f);
                Assert.AreEqual(Vector3.forward.z, projectile.GetComponent<ProjectileController>().ConfiguredLocalDirection.z, 0.001f);
            }
            finally
            {
                DestroyHarness(harness);
            }
        }

        [Test]
        public void StrongEnemyProjectileDamageCancelsCommittedRangedWindup()
        {
            var harness = CreateAttackHarness("StrongProjectileInterruptionHarness");
            try
            {
                Assert.IsTrue(harness.Weapon.TryFire(Vector2.up, 12f));
                Assert.IsTrue(harness.Weapon.IsAttackCommitted);

                ApplyProjectileDamage(harness.Health, DamageThreatKind.StrongProjectile, ImpactForceClass.Heavy);

                Assert.IsFalse(harness.Weapon.IsAttackCommitted);
                harness.Weapon.TickAction(0f, 12f + WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);
                Assert.IsNull(harness.Root.transform.Find("PlayerProjectile"));
            }
            finally
            {
                DestroyHarness(harness);
            }
        }

        [Test]
        public void UnreleasedRangedDrawStillCancelsOnLightProjectileDamage()
        {
            var harness = CreateAttackHarness("RangedDrawInterruptionHarness");
            var catalog = ScriptableObject.CreateInstance<WeaponCatalogDefinition>();
            var bow = ScriptableObject.CreateInstance<WeaponDefinition>();
            try
            {
                bow.Configure(
                    "diagnostic_bow",
                    "Diagnostic Bow",
                    WeaponSlot.Ranged,
                    WeaponCategory.Bow,
                    nextLightAttack: new WeaponAttackDefinition(AttackKind.Light, 1, 0.05f, 0f, 6.5f, ImpactForceClass.Light, 0.32f, 0.01f, 0.03f, 0.16f, 1f, 0.2f));
                catalog.Configure("manual_aim_projectile_interruption_tests", new[] { bow });
                harness.Weapon.ConfigureWeaponCatalog(catalog);
                harness.Weapon.ConfigureBuildStats(
                    1f,
                    0,
                    0,
                    100f,
                    0f,
                    "starter_blade",
                    "diagnostic_bow",
                    WeaponSlot.Ranged,
                    100f,
                    catalog);

                Assert.IsTrue(harness.Weapon.TryFire(Vector2.up, 20f));
                Assert.IsTrue(harness.Weapon.IsRangedDrawActive);

                ApplyProjectileDamage(harness.Health, DamageThreatKind.Light, ImpactForceClass.Light);

                Assert.IsFalse(harness.Weapon.IsRangedDrawActive);
                Assert.IsFalse(harness.Weapon.IsAttackCommitted);
            }
            finally
            {
                Object.DestroyImmediate(bow);
                Object.DestroyImmediate(catalog);
                DestroyHarness(harness);
            }
        }

        [Test]
        public void RoomCombatControllerKeepsEnemyProjectilePrefabSeparateFromPlayerPrefab()
        {
            var combatObject = new GameObject("RoomCombatProjectilePrefabHarness");
            var enemyPrefab = new GameObject("EnemyPrefab");
            var playerProjectilePrefab = new GameObject("PlayerProjectilePrefab");
            var enemyProjectilePrefab = new GameObject("EnemyProjectilePrefab");
            try
            {
                var combat = combatObject.AddComponent<RoomCombatController>();
                combat.Configure(enemyPrefab, playerProjectilePrefab, enemyProjectilePrefab);

                Assert.AreSame(playerProjectilePrefab, combat.ProjectilePrefab);
                Assert.AreSame(enemyProjectilePrefab, combat.EnemyProjectilePrefab);
            }
            finally
            {
                Object.DestroyImmediate(enemyProjectilePrefab);
                Object.DestroyImmediate(playerProjectilePrefab);
                Object.DestroyImmediate(enemyPrefab);
                Object.DestroyImmediate(combatObject);
            }
        }

        private static AttackHarness CreateAttackHarness(string name)
        {
            var root = new GameObject(name);
            var room = root.AddComponent<RoomRuntimeRoot>();
            room.ConfigureDefault();
            var combat = root.AddComponent<RoomCombatController>();

            var player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            player.AddComponent<PlayerAimLockController>().Configure(combat);
            var health = player.AddComponent<CombatantHealth>();
            health.Configure(10);
            var weapon = player.AddComponent<PlayerWeaponController>();

            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.name = $"{name}.PlayerProjectilePrefab";
            projectilePrefab.transform.SetParent(root.transform, false);
            projectilePrefab.AddComponent<ProjectileController>();
            weapon.Configure(room, combat, projectilePrefab);
            weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);

            return new AttackHarness(root, projectilePrefab, health, weapon);
        }

        private static void ApplyProjectileDamage(CombatantHealth health, DamageThreatKind threatKind, ImpactForceClass forceClass)
        {
            var source = new GameObject($"{threatKind}.SourceProjectile");
            try
            {
                Assert.IsTrue(DamageSystem.ApplyDamage(
                    health,
                    new DamageRequest(
                        1,
                        source,
                        DamageFeedbackContext.None,
                        threatKind,
                        DamageClassification.PhysicalProjectile(forceClass))));
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        private static void DestroyHarness(AttackHarness harness)
        {
            if (harness.ProjectilePrefab != null)
            {
                Object.DestroyImmediate(harness.ProjectilePrefab);
            }

            if (harness.Root != null)
            {
                Object.DestroyImmediate(harness.Root);
            }
        }

        private readonly struct AttackHarness
        {
            public AttackHarness(GameObject root, GameObject projectilePrefab, CombatantHealth health, PlayerWeaponController weapon)
            {
                Root = root;
                ProjectilePrefab = projectilePrefab;
                Health = health;
                Weapon = weapon;
            }

            public GameObject Root { get; }

            public GameObject ProjectilePrefab { get; }

            public CombatantHealth Health { get; }

            public PlayerWeaponController Weapon { get; }
        }
    }
}
