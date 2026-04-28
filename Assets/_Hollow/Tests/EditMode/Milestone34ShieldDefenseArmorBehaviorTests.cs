using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Input;
using Hollow.Rewards;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone34ShieldDefenseArmorBehaviorTests
    {
        [Test]
        public void DefenseReducesIncomingPlayerDamageWithoutMutatingSourceDamage()
        {
            var player = new GameObject("M34Player");
            try
            {
                var health = player.AddComponent<CombatantHealth>();
                health.Configure(6);
                var defense = player.AddComponent<PlayerDefenseController>();
                defense.Configure(4);

                var applied = DamageSystem.ApplyDamage(health, new DamageRequest(3, null));

                Assert.IsTrue(applied);
                Assert.AreEqual(5, health.CurrentHealth);
                Assert.AreEqual(2, defense.LastDamageReduction);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GuardCanFullyBlockSmallHitAndSpendStamina()
        {
            var player = new GameObject("M34Player");
            try
            {
                var health = player.AddComponent<CombatantHealth>();
                health.Configure(6);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.ConfigureBuildStats(1f, 0, 0, 100f, 0f, "starter_blade", "starter_bolt", WeaponSlot.Ranged, 40f);
                var defense = player.AddComponent<PlayerDefenseController>();
                defense.Configure(0);
                defense.Tick(guardHeld: true, deltaTime: 0.1f);
                var staminaBeforeHit = weapon.CurrentStamina;

                var applied = DamageSystem.ApplyDamage(health, new DamageRequest(1, null));

                Assert.IsFalse(applied);
                Assert.AreEqual(6, health.CurrentHealth);
                Assert.IsTrue(defense.LastHitWasGuarded);
                Assert.Greater(staminaBeforeHit, weapon.CurrentStamina);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GuardPushesContactEnemyAwayFromPlayer()
        {
            var player = new GameObject("M34Player");
            var enemyObject = new GameObject("M34Enemy");
            try
            {
                var health = player.AddComponent<CombatantHealth>();
                health.Configure(6);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.ConfigureBuildStats(1f, 0, 0, 100f, 0f, "starter_blade", "starter_bolt", WeaponSlot.Ranged, 40f);
                var defense = player.AddComponent<PlayerDefenseController>();
                defense.Configure(0);
                defense.Tick(guardHeld: true, deltaTime: 0f);

                enemyObject.transform.localPosition = new Vector3(0f, 0f, 0.4f);
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(null, player.AddComponent<Hollow.Entities.PlaceholderPlayerController>(), EnemyDefinition.CreateRuntimeNormal(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());

                DamageSystem.ApplyDamage(health, new DamageRequest(1, enemyObject));

                Assert.Greater(enemyObject.transform.localPosition.z, 0.4f);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void PlayerBuildApplierConfiguresDefenseControllerFromDerivedStats()
        {
            var player = new GameObject("M34Player");
            try
            {
                player.AddComponent<CombatantHealth>().Configure(6);
                var defense = player.AddComponent<PlayerDefenseController>();
                var build = new PlayerRunBuild();
                build.AddModifier(new PlayerStatModifier { sourceId = "test_defense", defense = 3 });

                PlayerBuildApplier.Apply(build, player);

                Assert.AreEqual(3, defense.Defense);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GameplayInputSnapshotCarriesGuardHeld()
        {
            var snapshot = new GameplayInputSnapshot(
                Vector2.zero,
                Vector2.zero,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: true);

            Assert.IsTrue(snapshot.GuardHeld);
        }
    }
}
