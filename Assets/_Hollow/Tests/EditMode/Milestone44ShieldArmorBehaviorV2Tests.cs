using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEditor;
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
        public void HeldRangedShootingUsesGuardWalkSpeedButIdleRangedDoesNot()
        {
            var root = new GameObject("M44RangedShootingMoveHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("M44RangedMovePlayer");
                player.transform.SetParent(root.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
                var movement = player.AddComponent<PlayerMovementController>();
                movement.Configure(null);

                movement.Move(Vector2.up, 1f);

                Assert.AreEqual(PlayerMovementController.DefaultSpeedMetersPerSecond, player.transform.localPosition.z, 0.001f);

                player.transform.localPosition = Vector3.zero;
                weapon.TickInput(HeldRangedLightSnapshot(Vector2.up), 0f, 0f);
                Assert.IsTrue(weapon.IsRangedHeldAttackPoseActive);
                movement.Move(Vector2.up, 1f);

                Assert.AreEqual(
                    PlayerMovementController.DefaultSpeedMetersPerSecond * ShieldGuardProfileDefinition.Resolve(null).GuardMoveMultiplier,
                    player.transform.localPosition.z,
                    0.001f);

                player.transform.localPosition = Vector3.zero;
                weapon.TickAction(0.21f, 0.21f);
                Assert.IsFalse(weapon.IsRangedAttackCommitted);
                Assert.IsTrue(weapon.IsRangedHeldAttackPoseActive);
                movement.Move(Vector2.up, 1f);

                Assert.AreEqual(
                    PlayerMovementController.DefaultSpeedMetersPerSecond * ShieldGuardProfileDefinition.Resolve(null).GuardMoveMultiplier,
                    player.transform.localPosition.z,
                    0.001f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShieldVisualAppearsWhileGuarding()
        {
            var presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            Assert.IsNotNull(presentationCatalog);
            PresentationContentProvider.Configure(presentationCatalog);
            var player = CreatePlayer(out _, out _, out var defense);
            try
            {
                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 0f);
                var visual = player.GetComponent<ShieldGuardVisualController>();

                Assert.IsNotNull(visual);
                Assert.IsTrue(visual.IsVisible);

                var marker = player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .SingleOrDefault(candidate => candidate.Role == PresentationPrefabRole.Armor);
                Assert.IsNotNull(marker);
                Assert.IsFalse(marker.IsFallback);
                Assert.Greater(Vector3.Dot(marker.transform.parent.forward, Vector3.forward), 0.98f);
                Assert.IsTrue(marker.GetComponentsInChildren<Renderer>(includeInactive: true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Any(material => AssetDatabase.GetAssetPath(material) == DefaultEquipmentMeshyAssetGenerator.MeshyShieldMaterialPath));

                visual.ShowFeedback(ShieldGuardResult.GuardBlocked);
                Assert.IsTrue(visual.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(player);
                PresentationContentProvider.Reset();
            }
        }

        [Test]
        public void ShieldGuardUsesEquippedShieldVisualWhenAvailable()
        {
            var presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            Assert.IsNotNull(presentationCatalog);
            PresentationContentProvider.Configure(presentationCatalog);
            var player = CreatePlayer(out _, out var weapon, out var defense);
            try
            {
                var heldWeaponVisual = player.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.Bind(weapon);
                Assert.IsNotNull(heldWeaponVisual.EquippedShieldVisual);
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(candidate => candidate.Role == PresentationPrefabRole.Armor));

                defense.Tick(Snapshot(Vector2.up, Vector2.zero, guardHeld: true), 0f, 0f);
                var visual = player.GetComponent<ShieldGuardVisualController>();

                Assert.IsNotNull(visual);
                Assert.IsTrue(visual.IsVisible);
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(candidate => candidate.Role == PresentationPrefabRole.Armor));
                var marker = heldWeaponVisual.EquippedShieldVisual.GetComponentInChildren<PresentationVisualMarker>(includeInactive: true);
                Assert.IsNotNull(marker);
                Assert.AreEqual(PresentationPrefabRole.Armor, marker.Role);
                Assert.IsFalse(marker.IsFallback);

                visual.ShowFeedback(ShieldGuardResult.GuardBlocked);
                Assert.IsTrue(visual.IsVisible);
                defense.Tick(Snapshot(Vector2.zero, Vector2.zero, guardHeld: false), 0.1f, 0.1f);
                Assert.IsFalse(visual.IsVisible);
                Assert.IsTrue(heldWeaponVisual.EquippedShieldVisual.activeInHierarchy);
            }
            finally
            {
                Object.DestroyImmediate(player);
                PresentationContentProvider.Reset();
            }
        }

        [Test]
        public void ShieldVisualFallsBackWithoutPresentationCatalog()
        {
            PresentationContentProvider.Configure(null);
            var player = CreatePlayer(out _, out _, out var defense);
            try
            {
                defense.Tick(Snapshot(Vector2.right, Vector2.zero, guardHeld: true), 0f, 0f);
                var visual = player.GetComponent<ShieldGuardVisualController>();

                Assert.IsNotNull(visual);
                Assert.IsTrue(visual.IsVisible);
                var marker = player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .SingleOrDefault(candidate => candidate.Role == PresentationPrefabRole.Armor);
                Assert.IsNotNull(marker);
                Assert.IsTrue(marker.IsFallback);
                Assert.Greater(marker.GetComponentsInChildren<Renderer>(includeInactive: true).Length, 0);
                Assert.Greater(Vector3.Dot(marker.transform.parent.forward, Vector3.right), 0.98f);
            }
            finally
            {
                Object.DestroyImmediate(player);
                PresentationContentProvider.Reset();
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
            PlayerAnimationProfileTestHelpers.ForceSwordShieldProfile(player, weapon);
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

        private static GameplayInputSnapshot HeldRangedLightSnapshot(Vector2 aim)
        {
            return new GameplayInputSnapshot(
                Vector2.zero,
                aim,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: true,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: false,
                pausePressed: false,
                rollPressed: false,
                lockTargetPressed: false,
                pointerScreenPosition: Vector2.zero,
                hasPointerScreenPosition: false,
                mouseAimIntent: false,
                lightAttackHeld: true,
                lightAttackReleased: false,
                heavyAttackHeld: false,
                heavyAttackReleased: false);
        }
    }
}
