using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Input;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone44ShieldArmorBehaviorV2Tests
    {
        [Test]
        public void ShieldGuardProfileDefaultLocksM44Values()
        {
            var profile = ShieldGuardProfileDefinition.CreateRuntimeDefault();

            Assert.AreEqual(0.3f, profile.ParryWindowSeconds, 0.001f);
            Assert.AreEqual(140f, profile.GuardConeDegrees, 0.001f);
            Assert.AreEqual(0f, profile.GuardDrainStaminaPerSecond, 0.001f);
            Assert.AreEqual(22f, profile.GuardHitStaminaCost, 0.001f);
            Assert.AreEqual(28f, profile.ParryStaminaCost, 0.001f);
            Assert.AreEqual(1, profile.ParryCounterDamage);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void DamageRequestDefaultsToLightThreatAndPreservesLegacyConstructor()
        {
            var source = new GameObject("Source");
            var legacy = new DamageRequest(2, source);
            var explicitThreat = new DamageRequest(3, source, DamageThreatKind.StrongProjectile);

            Assert.AreEqual(DamageThreatKind.Light, legacy.ThreatKind);
            Assert.AreEqual(DamageThreatKind.StrongProjectile, explicitThreat.ThreatKind);
            Assert.IsFalse(legacy.Feedback.HasKnockback);

            Object.DestroyImmediate(source);
        }

        [Test]
        public void GuardStartCreatesParryWindowAndHoldingDoesNotRefreshIt()
        {
            var player = CreatePlayer(out _, out _, out var defense);
            try
            {
                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 10f);

                Assert.IsTrue(defense.IsInParryWindowAt(10.2f));
                Assert.IsFalse(defense.IsInParryWindowAt(10.31f));

                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 10.5f);
                Assert.IsFalse(defense.IsInParryWindowAt(10.55f));

                defense.Tick(Snapshot(Vector2.zero, Vector2.zero, guardHeld: false), 0f, 10.6f);
                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 11f);
                Assert.IsTrue(defense.IsInParryWindowAt(11.2f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HoldingGuardDoesNotDrainStamina()
        {
            var player = CreatePlayer(out _, out var weapon, out var defense);
            try
            {
                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 2f, 4f);

                Assert.IsTrue(defense.IsGuarding);
                Assert.AreEqual(100f, weapon.CurrentStamina, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void LightThreatInsideConePerfectParriesAndCountersSourceEnemy()
        {
            var player = CreatePlayer(out var health, out var weapon, out var defense);
            var enemyObject = new GameObject("EnemySource");
            try
            {
                enemyObject.transform.position = new Vector3(0f, 0f, 1f);
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(null, player.GetComponent<PlaceholderPlayerController>(), EnemyDefinition.CreateRuntimeNormal(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());
                var enemyHealthBefore = enemy.Health.CurrentHealth;

                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 0f);
                var applied = DamageSystem.ApplyDamage(health, new DamageRequest(1, enemyObject, DamageThreatKind.Light));

                Assert.IsFalse(applied);
                Assert.AreEqual(6, health.CurrentHealth);
                Assert.AreEqual(ShieldGuardResult.PerfectParry, defense.LastGuardResult);
                Assert.AreEqual(100f - 28f, weapon.CurrentStamina, 0.001f);
                Assert.AreEqual(enemyHealthBefore - 1, enemy.Health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HitsOutsideAimConeBypassGuard()
        {
            var player = CreatePlayer(out var health, out _, out var defense);
            var source = new GameObject("BehindSource");
            try
            {
                source.transform.position = new Vector3(0f, 0f, -1f);
                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 0f);

                var applied = DamageSystem.ApplyDamage(health, new DamageRequest(1, source, DamageThreatKind.Light));

                Assert.IsTrue(applied);
                Assert.AreEqual(5, health.CurrentHealth);
                Assert.AreEqual(ShieldGuardResult.FailedOutOfCone, defense.LastGuardResult);
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HeavyThreatCanBeGuardReducedButNotParried()
        {
            var player = CreatePlayer(out var health, out var weapon, out var defense);
            var source = new GameObject("HeavySource");
            try
            {
                source.transform.position = new Vector3(0f, 0f, 1f);
                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 0f);

                var applied = DamageSystem.ApplyDamage(health, new DamageRequest(2, source, DamageThreatKind.Heavy));

                Assert.IsTrue(applied);
                Assert.AreEqual(5, health.CurrentHealth);
                Assert.AreEqual(ShieldGuardResult.RejectedThreat, defense.LastGuardResult);
                Assert.IsTrue(defense.LastHitWasGuarded);
                Assert.AreEqual(100f - 22f, weapon.CurrentStamina, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GuardSlowsMovementAndBlocksAttacks()
        {
            var player = CreatePlayer(out _, out var weapon, out var defense);
            try
            {
                var movement = player.AddComponent<PlayerMovementController>();
                movement.Configure(null);
                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 0f);

                movement.Move(Vector2.up, 1f);

                Assert.Less(player.transform.localPosition.z, PlayerMovementController.DefaultSpeedMetersPerSecond);
                Assert.Greater(player.transform.localPosition.z, 1f);
                Assert.IsFalse(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ShieldVisualAppearsWhileGuarding()
        {
            var player = CreatePlayer(out _, out _, out var defense);
            try
            {
                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 0f);
                var visual = player.GetComponent<ShieldGuardVisualController>();

                Assert.IsNotNull(visual);
                Assert.IsTrue(visual.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        private static GameObject CreatePlayer(out CombatantHealth health, out PlayerWeaponController weapon, out PlayerDefenseController defense)
        {
            var player = new GameObject("M44Player");
            player.AddComponent<PlaceholderPlayerController>();
            health = player.AddComponent<CombatantHealth>();
            health.Configure(6);
            weapon = player.AddComponent<PlayerWeaponController>();
            weapon.ConfigureBuildStats(1f, 0, 0, 100f, 0f, "starter_blade", "starter_bolt", WeaponSlot.Ranged, 100f);
            defense = player.AddComponent<PlayerDefenseController>();
            defense.Configure(0);
            defense.ConfigureShieldProfile(null);
            return player;
        }

        private static GameplayInputSnapshot Snapshot(Vector2 aim, Vector2 move, bool guardHeld)
        {
            return new GameplayInputSnapshot(
                move,
                aim,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: guardHeld);
        }
    }
}
