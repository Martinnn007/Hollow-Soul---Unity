using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.EnemyAuthoring;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone94EnemyAuthoringSuiteTests
    {
        [Test]
        public void EnemyDefinition_CanAuthorPresentationRoles()
        {
            var enemy = EnemyDefinition.CreateRuntimeNormal();

            enemy.ConfigurePresentationRoles(
                true,
                PresentationPrefabRole.EnemyKnight,
                true,
                PresentationPrefabRole.WeaponMelee,
                true,
                PresentationPrefabRole.Armor,
                true,
                PresentationPrefabRole.EnemyProjectile,
                true,
                PresentationPrefabRole.VfxEnemyHit);

            Assert.That(enemy.HasPresentationPrefabRoleOverride, Is.True);
            Assert.That(enemy.PresentationPrefabRole, Is.EqualTo(PresentationPrefabRole.EnemyKnight));
            Assert.That(enemy.HasWeaponPrefabRoleOverride, Is.True);
            Assert.That(enemy.WeaponPrefabRole, Is.EqualTo(PresentationPrefabRole.WeaponMelee));
            Assert.That(enemy.HasOffhandPrefabRoleOverride, Is.True);
            Assert.That(enemy.OffhandPrefabRole, Is.EqualTo(PresentationPrefabRole.Armor));
        }

        [Test]
        public void RuntimeTuningOverride_AppliesTransientSpeedAndSenses()
        {
            var go = new GameObject("enemy-runtime-tuning-test");
            try
            {
                var runtime = go.AddComponent<EnemyRuntimeController>();
                runtime.ApplyDebugTuningOverride(new EnemyRuntimeTuningOverride
                {
                    overrideSpeed = true,
                    speedMetersPerSecond = 2.75f,
                    overrideSenses = true,
                    sightRadiusMeters = 8f,
                    sightAngleDegrees = 200f,
                    hearingRadiusMeters = 6f
                });

                Assert.That(runtime.SpeedMetersPerSecond, Is.EqualTo(2.75f).Within(0.001f));
                Assert.That(runtime.SightRadiusMeters, Is.EqualTo(8f).Within(0.001f));
                Assert.That(runtime.SightAngleDegrees, Is.EqualTo(200f).Within(0.001f));
                Assert.That(runtime.HearingRadiusMeters, Is.EqualTo(6f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EnemyAuthoringValidator_AcceptsRuntimeEnemyDefaults()
        {
            var enemy = EnemyDefinition.CreateRuntimeNormal();

            var result = EnemyAuthoringValidator.Validate(enemy);

            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ProtectionRegistry_RecordsManualAssets()
        {
            var registry = ScriptableObject.CreateInstance<EnemyAuthoringProtectionRegistry>();
            try
            {
                registry.Record(
                    "guid-1",
                    "Assets/_Hollow/Data/Enemies/Enemy_Normal.asset",
                    "spawnEnemyNormal",
                    "enemy",
                    "abc123",
                    "test",
                    42);

                Assert.That(registry.ContainsGuid("guid-1"), Is.True);
                Assert.That(registry.ProtectedAssets[0].OwnerId, Is.EqualTo("spawnEnemyNormal"));
            }
            finally
            {
                Object.DestroyImmediate(registry);
            }
        }
    }
}
