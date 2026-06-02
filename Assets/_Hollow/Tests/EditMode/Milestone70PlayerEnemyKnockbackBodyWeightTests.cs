using System.Collections.Generic;
using System.Reflection;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone70PlayerEnemyKnockbackBodyWeightTests
    {
        [Test]
        public void WeaponAttackDefinitionPreservesOldConstructorWithSafeKnockback()
        {
            var legacy = new WeaponAttackDefinition(AttackKind.Light, 1, 1f, 0f, 6f);
            var authored = new WeaponAttackDefinition(AttackKind.Heavy, 2, 3.5f, 40f, 1.15f, ImpactForceClass.Heavy, 0.85f);

            Assert.AreEqual(ImpactForceClass.Light, legacy.ImpactForceClass);
            Assert.Greater(legacy.KnockbackMeters, 0f);
            Assert.AreEqual(ImpactForceClass.Heavy, authored.ImpactForceClass);
            Assert.AreEqual(0.85f, authored.KnockbackMeters, 0.001f);
        }

        [Test]
        public void EnemyDefinitionDefaultsBodyClassFromArchetypeAndBehavior()
        {
            Assert.AreEqual(EnemyBodyClass.Light, EnemyDefinition.DefaultBodyClassFor(EnemyArchetypeId.Flying, EnemyBehaviorId.FlyingChaser, EnemyMovementMode.Flying));
            Assert.AreEqual(EnemyBodyClass.Light, EnemyDefinition.DefaultBodyClassFor(EnemyArchetypeId.Fast, EnemyBehaviorId.Chaser, EnemyMovementMode.Grounded));
            Assert.AreEqual(EnemyBodyClass.Medium, EnemyDefinition.DefaultBodyClassFor(EnemyArchetypeId.Fast, EnemyBehaviorId.Charger, EnemyMovementMode.Grounded));
            Assert.AreEqual(EnemyBodyClass.Medium, EnemyDefinition.DefaultBodyClassFor(EnemyArchetypeId.Normal, EnemyBehaviorId.Splitter, EnemyMovementMode.Grounded));
            Assert.AreEqual(EnemyBodyClass.Heavy, EnemyDefinition.DefaultBodyClassFor(EnemyArchetypeId.Heavy, EnemyBehaviorId.TurretShooter, EnemyMovementMode.Grounded));
            Assert.AreEqual(EnemyBodyClass.Massive, EnemyDefinition.DefaultBodyClassFor(EnemyArchetypeId.Boss, EnemyBehaviorId.BossWarden, EnemyMovementMode.Grounded));
        }

        [Test]
        public void BodyClassMultiplierMakesLightMoveMoreAndBossesNudge()
        {
            var profile = CombatFeelProfileDefinition.CreateRuntimeDefault();

            Assert.AreEqual(1.2f, EnemyKnockbackResolver.ResolveBodyMultiplier(EnemyBodyClass.Light, profile), 0.001f);
            Assert.AreEqual(1f, EnemyKnockbackResolver.ResolveBodyMultiplier(EnemyBodyClass.Medium, profile), 0.001f);
            Assert.AreEqual(0.55f, EnemyKnockbackResolver.ResolveBodyMultiplier(EnemyBodyClass.Heavy, profile), 0.001f);
            Assert.AreEqual(0.18f, EnemyKnockbackResolver.ResolveBodyMultiplier(EnemyBodyClass.Massive, profile), 0.001f);
        }

        [Test]
        public void KnockbackReceiverAppliesBodyResistanceWithoutStunState()
        {
            var lightTarget = new GameObject("M70LightTarget");
            var massiveTarget = new GameObject("M70MassiveTarget");
            try
            {
                var light = lightTarget.AddComponent<CombatKnockbackReceiver>();
                var massive = massiveTarget.AddComponent<CombatKnockbackReceiver>();
                light.Configure(null, 0.3f, false, 1.2f);
                massive.Configure(null, 0.3f, false, 0.18f);

                light.ApplyKnockback(Vector3.forward, 1f, 0.1f, DamageClassification.PhysicalMelee(ImpactForceClass.Heavy));
                massive.ApplyKnockback(Vector3.forward, 1f, 0.1f, DamageClassification.PhysicalMelee(ImpactForceClass.Heavy));
                light.Tick(0.1f);
                massive.Tick(0.1f);

                Assert.Greater(lightTarget.transform.localPosition.z, massiveTarget.transform.localPosition.z);
                Assert.AreEqual(1.2f, lightTarget.transform.localPosition.z, 0.001f);
                Assert.AreEqual(0.18f, massiveTarget.transform.localPosition.z, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(lightTarget);
                Object.DestroyImmediate(massiveTarget);
            }
        }

        [Test]
        public void RootedStaticEnemyIgnoresDamageKnockback()
        {
            var enemyObject = new GameObject("M70RootedTurretKnockbackHarness");
            try
            {
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(null, null, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret"), DifficultyTierDefinition.CreateRuntimeDeveloperSample());
                var start = enemy.transform.localPosition;
                Assert.IsTrue(enemy.IsRootedStaticEnemy);

                var knockback = enemyObject.GetComponent<CombatKnockbackReceiver>();
                Assert.IsNotNull(knockback);
                knockback.ApplyKnockback(Vector3.forward, 1f, 0.1f, DamageClassification.PhysicalMelee(ImpactForceClass.Heavy));
                knockback.Tick(0.1f);

                Assert.AreEqual(start, enemy.transform.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void RootedStaticEnemyIgnoresPlayerBodyNudge()
        {
            var enemyObject = new GameObject("M70RootedTurretBodyHarness");
            try
            {
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(null, null, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret"), DifficultyTierDefinition.CreateRuntimeDeveloperSample());
                SetPrivate(enemy, "bodyClass", EnemyBodyClass.Light);
                var start = enemy.transform.localPosition;
                Assert.IsTrue(enemy.IsRootedStaticEnemy);
                Assert.AreEqual(EnemyBodyClass.Light, enemy.BodyClass);

                PlayerEnemyBodyCollision.Resolve(
                    null,
                    new List<EnemyRuntimeController> { enemy },
                    new Vector3(0f, 0f, -0.7f),
                    new Vector3(0f, 0f, 0.05f),
                    0.3f);

                Assert.AreEqual(start, enemy.transform.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void Milestone70ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone70Validator.Validate());
        }

        private static void SetPrivate<T>(EnemyRuntimeController enemy, string fieldName, T value)
        {
            var field = typeof(EnemyRuntimeController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(enemy, value);
        }
    }
}
