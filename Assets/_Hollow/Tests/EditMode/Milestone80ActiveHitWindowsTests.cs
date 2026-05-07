using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Validation;
using Hollow.Entities;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone80ActiveHitWindowsTests
    {
        [Test]
        public void AttackProfilesAndPlayerDefaultsCarryActiveWindowMetadata()
        {
            foreach (var profile in EnemyAttackProfileDefaults.AllEnemySpecs.Select(spec => EnemyAttackProfileDefaults.ResolveEnemyProfile(spec.OwnerId, spec.AttackId)))
            {
                Assert.NotNull(profile);
                Assert.Greater(profile.RecoverySeconds, 0f, profile.AttackId);
                Assert.Greater(profile.HitArcDegrees, 0f, profile.AttackId);
                Assert.LessOrEqual(profile.HitArcDegrees, 360f, profile.AttackId);
            }

            foreach (var attack in new[]
                     {
                         WeaponAttackDefinition.DefaultLight(WeaponSlot.Melee),
                         WeaponAttackDefinition.DefaultHeavy(WeaponSlot.Melee),
                         WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged),
                         WeaponAttackDefinition.DefaultHeavy(WeaponSlot.Ranged)
                     })
            {
                Assert.GreaterOrEqual(attack.WindupSeconds, 0f);
                Assert.Greater(attack.ActiveSeconds, 0f);
                Assert.Greater(attack.RecoverySeconds, 0f);
                Assert.LessOrEqual(attack.WindupSeconds + attack.ActiveSeconds + attack.RecoverySeconds, attack.CooldownSeconds);
            }
        }

        [Test]
        public void EnemyLungeDamagesOnlyDuringActiveArcAndRecoveryIsHarmless()
        {
            var root = CreateEnemyHarness(out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal");
                var profile = definition.ResolveAttackProfile("claw_lunge");
                player.transform.localPosition = new Vector3(0f, 0f, 1.35f);
                var enemy = CreateEnemy(root.transform, player, definition);
                enemy.transform.localPosition = Vector3.zero;

                enemy.Tick(0.05f, 4f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, enemy.ReadabilityState);
                Assert.IsFalse(enemy.TryApplyContactDamage(4.05f));
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);

                enemy.Tick(0.05f, 4f + profile.WindupSeconds + 0.01f);
                enemy.transform.localPosition = new Vector3(0f, 0f, 0.82f);
                Assert.AreEqual(EnemyReadabilityState.MeleeLunge, enemy.ReadabilityState);
                Assert.IsTrue(enemy.TryApplyContactDamage(4f + profile.WindupSeconds + 0.08f));
                Assert.IsFalse(enemy.TryApplyContactDamage(4f + profile.WindupSeconds + 0.09f));

                enemy.Tick(0.05f, 4f + profile.WindupSeconds + profile.ActiveSeconds + 0.04f);
                playerHealth.Restore(playerHealth.MaxHealth, playerHealth.MaxHealth);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.2f);
                Assert.AreEqual(EnemyReadabilityState.MeleeRecovery, enemy.ReadabilityState);
                Assert.IsFalse(enemy.TryApplyContactDamage(4f + profile.WindupSeconds + profile.ActiveSeconds + 0.05f));
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyRangedAttackSpawnsProjectileAtActiveTransitionThenRecovers()
        {
            var root = CreateEnemyHarness(out var player);
            try
            {
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret");
                var profile = definition.ResolveAttackProfile("bone_dart");
                player.transform.localPosition = new Vector3(0f, 0f, 5f);
                var enemy = CreateEnemy(root.transform, player, definition);
                enemy.transform.localPosition = Vector3.zero;

                enemy.Tick(0.05f, 8f);
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, enemy.ReadabilityState);
                Assert.AreEqual(0, root.transform.Cast<Transform>().Count(child => child.name.StartsWith("EnemyProjectile.")));

                enemy.Tick(0.05f, 8f + profile.WindupSeconds + 0.01f);
                Assert.AreEqual(EnemyReadabilityState.RangedActive, enemy.ReadabilityState);
                Assert.AreEqual(1, root.transform.Cast<Transform>().Count(child => child.name.StartsWith("EnemyProjectile.")));

                enemy.Tick(0.05f, 8f + profile.WindupSeconds + profile.ActiveSeconds + 0.04f);
                Assert.AreEqual(EnemyReadabilityState.RangedRecovery, enemy.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PoiseBreaksOnlyInterruptWindupWhenForceMeetsThreshold()
        {
            var root = CreateEnemyHarness(out var player);
            try
            {
                var fast = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFast"));
                player.transform.localPosition = new Vector3(0f, 0f, 1.2f);
                fast.transform.localPosition = Vector3.zero;
                fast.Tick(0.05f, 2f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, fast.ReadabilityState);

                DamageSystem.ApplyDamage(
                    fast.Health,
                    new DamageRequest(1, player.gameObject, DamageFeedbackContext.None, DamageClassification.PhysicalMelee(ImpactForceClass.Light)));
                Assert.AreEqual(EnemyReadabilityState.MeleeRecovery, fast.ReadabilityState);

                var heavy = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyHeavy"));
                player.transform.localPosition = new Vector3(0f, 0f, 1.6f);
                heavy.transform.localPosition = Vector3.zero;
                heavy.Tick(0.05f, 5f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, heavy.ReadabilityState);

                DamageSystem.ApplyDamage(
                    heavy.Health,
                    new DamageRequest(1, player.gameObject, DamageFeedbackContext.None, DamageClassification.PhysicalMelee(ImpactForceClass.Medium)));
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, heavy.ReadabilityState);

                DamageSystem.ApplyDamage(
                    heavy.Health,
                    new DamageRequest(1, player.gameObject, DamageFeedbackContext.None, DamageClassification.PhysicalMelee(ImpactForceClass.Heavy)));
                Assert.AreEqual(EnemyReadabilityState.MeleeRecovery, heavy.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerRangedAndMeleeDamageResolveAtActiveTransition()
        {
            var root = new GameObject("M80PlayerAttackHarness");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            combat.transform.SetParent(root.transform, false);
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            projectilePrefab.transform.SetParent(root.transform, false);
            var player = CreatePlayer(root.transform);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                Assert.IsTrue(weapon.TryFire(Vector2.up, 0f));
                Assert.AreEqual(0, root.transform.Cast<Transform>().Count(child => child.name == "PlayerProjectile"));
                weapon.TickAction(0f, WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);
                Assert.AreEqual(1, root.transform.Cast<Transform>().Count(child => child.name == "PlayerProjectile"));

                var enemy = CreateEnemy(root.transform, player.GetComponent<PlaceholderPlayerController>(), EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                enemy.transform.localPosition = new Vector3(0f, 0f, 0.7f);
                RegisterEnemy(combat, enemy);
                weapon.SetActiveWeaponSlot(WeaponSlot.Melee);
                var startingHealth = enemy.Health.CurrentHealth;
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 2f));
                Assert.AreEqual(startingHealth, enemy.Health.CurrentHealth);
                weapon.TickAction(0f, 2f + WeaponAttackDefinition.DefaultLight(WeaponSlot.Melee).WindupSeconds + 0.01f);
                Assert.Less(enemy.Health.CurrentHealth, startingHealth);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerAttackCommitmentSlowsMovementAndRollGrantsIFrames()
        {
            var root = new GameObject("M80PlayerRollHarness");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            combat.transform.SetParent(root.transform, false);
            var player = CreatePlayer(root.transform);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var movement = player.GetComponent<PlayerMovementController>();
                weapon.Configure(null, combat, null);
                weapon.SetActiveWeaponSlot(WeaponSlot.Melee);

                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                movement.Move(Vector2.up, 1f);
                Assert.AreEqual(PlayerMovementController.DefaultSpeedMetersPerSecond * PlayerWeaponController.AttackMovementMultiplier, player.transform.localPosition.z, 0.02f);

                var roller = CreatePlayer(root.transform);
                var rollWeapon = roller.GetComponent<PlayerWeaponController>();
                var rollMovement = roller.GetComponent<PlayerMovementController>();
                var rollHealth = roller.GetComponent<CombatantHealth>();
                roller.AddComponent<PlayerDamageFeedbackController>().Configure(null, null);
                Assert.IsTrue(rollWeapon.TryRoll(Vector2.up, Vector2.zero, 0f));
                Assert.AreEqual(100f - PlayerWeaponController.RollStaminaCost, rollWeapon.CurrentStamina, 0.001f);
                rollMovement.Move(Vector2.zero, PlayerWeaponController.RollDurationSeconds * 0.5f);
                Assert.Greater(roller.transform.localPosition.z, 0.8f);

                var damageSource = new GameObject("M80RollDamageSource");
                damageSource.transform.SetParent(root.transform, false);
                rollWeapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds + 0.01f);
                var applied = DamageSystem.ApplyDamage(rollHealth, new DamageRequest(1, damageSource));
                Assert.IsFalse(applied);
                Assert.AreEqual(rollHealth.MaxHealth, rollHealth.CurrentHealth);
                rollWeapon.TickAction(0f, PlayerWeaponController.RollDurationSeconds + 0.01f);
                Assert.IsFalse(rollWeapon.IsRolling);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_M80_Active_Hit_Windows.md"));
            Assert.IsTrue(File.Exists("output/reports/m80_active_hit_windows.md"));
            var markdown = File.ReadAllText("Docs/Hollow_M80_Active_Hit_Windows.md");
            StringAssert.Contains("Active Hit Windows", markdown);
            StringAssert.Contains("windup", markdown);
            StringAssert.Contains("active", markdown);
            StringAssert.Contains("recovery", markdown);
            StringAssert.Contains("poise", markdown);
            StringAssert.Contains("roll", markdown);
            Assert.IsTrue(Milestone80Validator.Validate());
        }

        private static GameObject CreateEnemyHarness(out PlaceholderPlayerController player)
        {
            var root = new GameObject("M80EnemyHarness");
            player = CreatePlayer(root.transform).GetComponent<PlaceholderPlayerController>();
            return root;
        }

        private static GameObject CreatePlayer(Transform parent)
        {
            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(parent, false);
            var player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<PlayerWeaponController>();
            playerObject.AddComponent<PlayerMovementController>();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            playerObject.AddComponent<CombatKnockbackReceiver>().Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            return playerObject;
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, PlaceholderPlayerController player, EnemyDefinition definition)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(null, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            return enemy;
        }

        private static void RegisterEnemy(RoomCombatController combat, EnemyRuntimeController enemy)
        {
            var field = typeof(RoomCombatController).GetField("enemies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var enemies = (List<EnemyRuntimeController>)field.GetValue(combat);
            enemies.Add(enemy);
        }
    }
}
