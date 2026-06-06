using System.Collections.Generic;
using System.Reflection;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone111PlayerAimLockTests
    {
        [Test]
        public void InputPreserves360ShootAndKeepsEightSectorRetargetHelper()
        {
            var diagonal = GameplayInputReader.QuantizeEightAxis(new Vector2(1f, 1f));
            Assert.AreEqual(0.707f, diagonal.x, 0.001f);
            Assert.AreEqual(0.707f, diagonal.y, 0.001f);
            var west = GameplayInputReader.QuantizeEightAxis(new Vector2(-0.9f, 0.1f));
            Assert.AreEqual(-1f, west.x, 0.001f);
            Assert.AreEqual(0f, west.y, 0.001f);
            Assert.AreEqual(Vector2.zero, GameplayInputReader.QuantizeEightAxis(new Vector2(0.05f, 0.05f)));

            var snapshot = new GameplayInputSnapshot(
                Vector2.zero,
                new Vector2(1f, 1f),
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: false,
                pausePressed: false,
                rollPressed: false,
                lockTargetPressed: true);

            Assert.IsTrue(snapshot.LockTargetPressed);
            Assert.AreEqual(0.707f, snapshot.Shoot.x, 0.001f);
            Assert.AreEqual(0.707f, snapshot.Shoot.y, 0.001f);

            var arbitrary = new GameplayInputSnapshot(Vector2.zero, new Vector2(0.9f, 0.1f));
            var expected = new Vector2(0.9f, 0.1f).normalized;
            Assert.AreEqual(expected.x, arbitrary.Shoot.x, 0.001f);
            Assert.AreEqual(expected.y, arbitrary.Shoot.y, 0.001f);
            Assert.IsFalse(arbitrary.MouseAimIntent);
        }

        [Test]
        public void AimControllerUsesFacingAndExplicitAimWithoutTargetLock()
        {
            var root = new GameObject("M111AimLockHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);

                aim.TickAim(Snapshot(move: new Vector2(1f, 1f), shoot: Vector2.zero, lockPressed: false), 0f);
                Assert.AreEqual(0.707f, aim.BodyFacingDirection.x, 0.001f);
                Assert.AreEqual(0.707f, aim.AttackDirection.y, 0.001f);
                Assert.IsFalse(aim.HasActiveAimIntent);

                var manual = new Vector2(-0.8f, 0.2f).normalized;
                aim.TickAim(Snapshot(move: Vector2.right, shoot: new Vector2(-0.8f, 0.2f), lockPressed: false), 0.1f);
                Assert.AreEqual(manual.x, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(manual.y, aim.AttackDirection.y, 0.001f);
                Assert.IsTrue(aim.HasActiveAimIntent);

                var eastEnemy = CreateEnemy(root.transform, new Vector3(2f, 0f, 0.75f));
                var northEnemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 3f));
                AddEnemy(combat, eastEnemy);
                AddEnemy(combat, northEnemy);

                aim.TickAim(Snapshot(move: Vector2.zero, shoot: Vector2.zero, lockPressed: false), 0.2f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsFalse(aim.IsTargetLocked);
                Assert.IsNull(aim.LockedEnemy);
                Assert.AreEqual(1f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(0f, aim.AttackDirection.y, 0.001f);
                Assert.IsFalse(aim.TryGetLockedTargetDirection(out _));

                aim.TickAim(Snapshot(move: Vector2.zero, shoot: Vector2.up, lockPressed: false), 0.5f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsNull(aim.LockedEnemy);
                Assert.AreEqual(0f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(1f, aim.AttackDirection.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemiesNeverCreateAssistOrStickyLock()
        {
            var root = new GameObject("M111ReliableAutoLockHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);

                var outsideEnemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 6f));
                AddEnemy(combat, outsideEnemy);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsFalse(aim.IsTargetLocked);
                Assert.IsNull(aim.LockedEnemy);

                var firstEnemy = CreateEnemy(root.transform, new Vector3(3f, 0f, 0f));
                AddEnemy(combat, firstEnemy);

                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0.1f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsNull(aim.LockedEnemy);

                var closerButNotEnoughEnemy = CreateEnemy(root.transform, new Vector3(2.5f, 0f, 0f));
                AddEnemy(combat, closerButNotEnoughEnemy);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0.2f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsNull(aim.LockedEnemy);

                closerButNotEnoughEnemy.transform.localPosition = new Vector3(1.8f, 0f, 0f);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0.3f);
                Assert.IsNull(aim.LockedEnemy);

                var northEnemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 2f));
                AddEnemy(combat, northEnemy);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.up, lockPressed: false), 0.6f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsNull(aim.LockedEnemy);
                Assert.AreEqual(0f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(1f, aim.AttackDirection.y, 0.001f);

                closerButNotEnoughEnemy.transform.localPosition = new Vector3(0.5f, 0f, 0f);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: true), 0.8f);
                Assert.IsFalse(aim.IsExplicitlyLocked);
                Assert.IsNull(aim.LockedEnemy);

                northEnemy.gameObject.SetActive(false);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 1f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsNull(aim.LockedEnemy);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RecentDamageDoesNotCreateTargetLock()
        {
            var root = new GameObject("M111RecentTargetExplicitRangeHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);

                var rangedEnemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 6f));
                AddEnemy(combat, rangedEnemy);

                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsFalse(aim.IsTargetLocked);

                aim.NotifyEnemyDamaged(rangedEnemy, 0.1f);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0.2f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsFalse(aim.IsTargetLocked);
                Assert.IsNull(aim.LockedEnemy);
                Assert.IsNull(aim.RecentDamagedTarget);

                rangedEnemy.transform.localPosition = new Vector3(0f, 0f, 7f);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0.3f);
                Assert.IsNull(aim.LockedEnemy);

                aim.TickAim(
                    Snapshot(Vector2.zero, Vector2.zero, lockPressed: false),
                    PlayerAimLockController.RecentTargetMemorySeconds + 0.3f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsFalse(aim.IsTargetLocked);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MouseAimDirectionIgnoresHoveredEnemy()
        {
            var root = new GameObject("M111MouseHoverAssistHarness");
            var cameraObject = new GameObject("Main Camera");
            try
            {
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
                camera.transform.position = new Vector3(0f, 10f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);

                var nearestEnemy = CreateEnemy(root.transform, new Vector3(1f, 0f, 0f));
                var hoveredEnemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 4f));
                AddEnemy(combat, nearestEnemy);
                AddEnemy(combat, hoveredEnemy);

                var pointer = camera.WorldToScreenPoint(hoveredEnemy.transform.position);
                var result = aim.ResolveAttackAssist(
                    SnapshotWithPointer(new Vector2(pointer.x, pointer.y), lockPressed: false),
                    PlayerAimLockController.ExplicitLockRangeMeters,
                    false,
                    0f);

                var expected = new Vector2(hoveredEnemy.transform.localPosition.x, hoveredEnemy.transform.localPosition.z).normalized;
                Assert.IsFalse(result.HasTarget);
                Assert.IsNull(result.Target);
                Assert.AreEqual(PlayerAimAssistSource.None, result.Source);
                Assert.AreEqual(expected.x, result.Direction.x, 0.001f);
                Assert.AreEqual(expected.y, result.Direction.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StickAimDirectionIgnoresInConeEnemy()
        {
            var root = new GameObject("M111AimConeAssistHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);

                var closerOutOfCone = CreateEnemy(root.transform, new Vector3(1f, 0f, 0f));
                var aimedEnemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 4f));
                AddEnemy(combat, closerOutOfCone);
                AddEnemy(combat, aimedEnemy);

                var result = aim.ResolveAttackAssist(
                    Snapshot(Vector2.zero, Vector2.up, lockPressed: false),
                    PlayerAimLockController.ExplicitLockRangeMeters,
                    false,
                    0f);

                Assert.IsFalse(result.HasTarget);
                Assert.IsNull(result.Target);
                Assert.AreEqual(PlayerAimAssistSource.None, result.Source);
                Assert.AreEqual(0f, result.Direction.x, 0.001f);
                Assert.AreEqual(1f, result.Direction.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MouseCursorAimStaysActiveAndLockInputDoesNothing()
        {
            var root = new GameObject("M111MouseAimHarness");
            var cameraObject = new GameObject("Main Camera");
            try
            {
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
                camera.transform.position = new Vector3(0f, 10f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);

                var pointer = camera.WorldToScreenPoint(new Vector3(3f, 0f, 1f));
                var expected = new Vector2(3f, 1f).normalized;
                aim.TickAim(SnapshotWithPointer(new Vector2(pointer.x, pointer.y), lockPressed: false), 0f);
                Assert.AreEqual(expected.x, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(expected.y, aim.AttackDirection.y, 0.001f);

                var northEnemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 2f));
                AddEnemy(combat, northEnemy);
                aim.TickAim(SnapshotWithPointer(new Vector2(pointer.x, pointer.y), lockPressed: true), 0.1f);
                Assert.IsFalse(aim.IsExplicitlyLocked);
                Assert.IsFalse(aim.IsTargetLocked);
                Assert.IsNull(aim.LockedEnemy);
                Assert.AreEqual(expected.x, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(expected.y, aim.AttackDirection.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MouseAimResolvesInRotatedGameplayRootLocalSpace()
        {
            var root = new GameObject("M111RotatedMouseAimHarness");
            var cameraObject = new GameObject("Main Camera");
            try
            {
                root.AddComponent<PlatformPresentationRoot>().Configure(Hollow.Platform.HollowPlatformKind.VisionOSBoundedTabletop);
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
                camera.transform.position = new Vector3(0f, 10f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);

                var worldTarget = root.transform.TransformPoint(new Vector3(0f, 0f, 3f));
                var pointer = camera.WorldToScreenPoint(worldTarget);
                aim.TickAim(SnapshotWithPointer(new Vector2(pointer.x, pointer.y), lockPressed: false), 0f);

                Assert.AreEqual(0f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(1f, aim.AttackDirection.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MovementFacingIgnoresStaleMouseAndPersistsWhenStopped()
        {
            var root = new GameObject("M111MovementFacingHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var visual = new GameObject("Visual");
                visual.transform.SetParent(player.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);
                aim.BindPresentation(visual);

                aim.TickAim(
                    SnapshotWithPointerAndMove(Vector2.right, new Vector2(800f, 800f), lockPressed: false, mouseAimIntent: false),
                    0f);
                Assert.AreEqual(1f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(0f, aim.AttackDirection.y, 0.001f);
                Assert.Greater(visual.transform.forward.x, 0.95f);

                aim.TickAim(
                    SnapshotWithPointerAndMove(Vector2.zero, new Vector2(200f, 800f), lockPressed: false, mouseAimIntent: false),
                    0.1f);
                Assert.AreEqual(1f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(0f, aim.AttackDirection.y, 0.001f);

                aim.TickAim(Snapshot(Vector2.zero, Vector2.up, lockPressed: false), 0.2f);
                Assert.AreEqual(0f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(1f, aim.AttackDirection.y, 0.001f);

                aim.TickAim(Snapshot(Vector2.left, Vector2.zero, lockPressed: false), 0.3f);
                Assert.AreEqual(-1f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(0f, aim.AttackDirection.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MouseAimIntentOverridesMovementBrieflyThenExpires()
        {
            var root = new GameObject("M111MouseIntentHarness");
            var cameraObject = new GameObject("Main Camera");
            try
            {
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
                camera.transform.position = new Vector3(0f, 10f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);

                var pointer = camera.WorldToScreenPoint(new Vector3(0f, 0f, 3f));
                aim.TickAim(
                    SnapshotWithPointerAndMove(Vector2.right, new Vector2(pointer.x, pointer.y), lockPressed: false, mouseAimIntent: true),
                    0f);
                Assert.IsTrue(aim.HasManualAimOverride);
                Assert.AreEqual(0f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(1f, aim.AttackDirection.y, 0.001f);

                aim.TickAim(
                    SnapshotWithPointerAndMove(Vector2.right, new Vector2(pointer.x, pointer.y), lockPressed: false, mouseAimIntent: false),
                    PlayerAimLockController.MouseAimIntentMemorySeconds - 0.1f);
                Assert.IsTrue(aim.HasManualAimOverride);
                Assert.AreEqual(0f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(1f, aim.AttackDirection.y, 0.001f);

                aim.TickAim(
                    SnapshotWithPointerAndMove(Vector2.right, new Vector2(pointer.x, pointer.y), lockPressed: false, mouseAimIntent: false),
                    PlayerAimLockController.MouseAimIntentMemorySeconds + 0.2f);
                Assert.IsFalse(aim.HasManualAimOverride);
                Assert.AreEqual(1f, aim.AttackDirection.x, 0.001f);
                Assert.AreEqual(0f, aim.AttackDirection.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerRangedAndMeleeCanAttackOnArbitrary360Axes()
        {
            var root = new GameObject("M111DiagonalCombatHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            projectilePrefab.transform.SetParent(root.transform, false);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(root.transform, false);
                var weapon = playerObject.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);

                var arbitraryAim = new Vector2(0.9f, 0.35f).normalized;
                Assert.IsTrue(weapon.TryFire(new Vector2(0.9f, 0.35f), 0f));
                weapon.TickAction(0f, WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);
                var projectile = root.transform.Find("PlayerProjectile");
                Assert.IsNotNull(projectile);
                Assert.Greater(projectile.localPosition.x, 0.2f);
                Assert.Greater(projectile.localPosition.z, 0.05f);
                Assert.Greater(
                    Vector3.Dot(projectile.forward, new Vector3(arbitraryAim.x, 0f, arbitraryAim.y)),
                    0.99f);
                Assert.AreEqual(arbitraryAim.x, weapon.LastAimDirection.x, 0.001f);
                Assert.AreEqual(arbitraryAim.y, weapon.LastAimDirection.y, 0.001f);

                var enemy = CreateEnemy(root.transform, new Vector3(0.78f, 0f, 0.30f));
                AddEnemy(combat, enemy);
                var startingHealth = enemy.Health.CurrentHealth;
                weapon.SetActiveWeaponSlot(WeaponSlot.Melee);
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, new Vector2(0.9f, 0.35f), 2f));
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
        public void LockInputRangedShotUsesFacingDirection()
        {
            AssertRangedShotIgnoresTargetAndUsesDirection(lockPressed: true);
        }

        [Test]
        public void AutoTargetRangedShotUsesFacingDirection()
        {
            AssertRangedShotIgnoresTargetAndUsesDirection(lockPressed: false);
        }

        [Test]
        public void TripleShotKeepsSpreadWithoutTargetSteering()
        {
            var root = new GameObject("M111UnlockedTripleShotHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            projectilePrefab.transform.SetParent(root.transform, false);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(root.transform, false);
                var aim = playerObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);
                var weapon = playerObject.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.ConfigureProjectilePassives(new ProjectilePassiveState(
                    ProjectilePatternKind.TripleShot,
                    1f,
                    0f,
                    ProjectileVisualStyle.Default));

                var enemy = CreateEnemy(root.transform, new Vector3(2f, 0f, 3f));
                AddEnemy(combat, enemy);

                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0f);
                Assert.IsNull(aim.LockedEnemy);

                Assert.IsTrue(weapon.TryFire(Vector2.zero, 0f));
                weapon.TickAction(0f, WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);

                var projectiles = new List<Transform>();
                for (var index = 0; index < root.transform.childCount; index++)
                {
                    var child = root.transform.GetChild(index);
                    if (child != null && child.name == "PlayerProjectile")
                    {
                        projectiles.Add(child);
                    }
                }

                Assert.AreEqual(3, projectiles.Count);
                var forwardProjectiles = 0;
                var directTargetedProjectiles = 0;
                foreach (var projectile in projectiles)
                {
                    if (Vector3.Dot(projectile.forward, Vector3.forward) > 0.999f)
                    {
                        forwardProjectiles++;
                    }

                    var expected = enemy.transform.localPosition - projectile.localPosition;
                    expected.y = 0f;
                    expected.Normalize();
                    if (Vector3.Dot(projectile.forward, expected) > 0.999f)
                    {
                        directTargetedProjectiles++;
                    }
                }

                Assert.AreEqual(1, forwardProjectiles);
                Assert.AreEqual(0, directTargetedProjectiles);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RangedShotKeepsCommittedFacingDirectionThroughWindup()
        {
            var root = new GameObject("M111RangedShotKeepFacingHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            projectilePrefab.transform.SetParent(root.transform, false);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(root.transform, false);
                var aim = playerObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);
                var weapon = playerObject.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);

                var primary = CreateEnemy(root.transform, new Vector3(0f, 0f, 3f));
                var secondary = CreateEnemy(root.transform, new Vector3(6f, 0f, 0f));
                AddEnemy(combat, primary);
                AddEnemy(combat, secondary);

                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0f);
                Assert.IsNull(aim.LockedEnemy);

                Assert.IsTrue(weapon.TryFire(Vector2.zero, 0f));
                secondary.transform.localPosition = new Vector3(0f, 0f, 2f);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0.01f);
                Assert.IsNull(aim.LockedEnemy);

                var windupSeconds = WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds;
                weapon.TickAction(0f, windupSeconds + 0.01f);
                var projectile = root.transform.Find("PlayerProjectile");
                Assert.IsNotNull(projectile);
                Assert.Greater(Vector3.Dot(projectile.forward, Vector3.forward), 0.999f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RangedShotIgnoresInvalidAndNewTargetsDuringWindup()
        {
            var root = new GameObject("M111RangedShotInvalidTargetIgnoredHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            projectilePrefab.transform.SetParent(root.transform, false);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(root.transform, false);
                var aim = playerObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);
                var weapon = playerObject.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);

                var primary = CreateEnemy(root.transform, new Vector3(0f, 0f, 3f));
                var secondary = CreateEnemy(root.transform, new Vector3(6f, 0f, 0f));
                AddEnemy(combat, primary);
                AddEnemy(combat, secondary);

                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0f);
                Assert.IsNull(aim.LockedEnemy);

                Assert.IsTrue(weapon.TryFire(Vector2.zero, 0f));
                primary.gameObject.SetActive(false);
                secondary.transform.localPosition = new Vector3(0f, 0f, 2f);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: false), 0.01f);
                Assert.IsNull(aim.LockedEnemy);

                var windupSeconds = WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds;
                weapon.TickAction(0f, windupSeconds + 0.01f);
                var projectile = root.transform.Find("PlayerProjectile");
                Assert.IsNotNull(projectile);
                Assert.Greater(Vector3.Dot(projectile.forward, Vector3.forward), 0.999f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerMovementPreservesAnalog360Direction()
        {
            var playerObject = new GameObject("M111MovementHarness");
            try
            {
                var movement = playerObject.AddComponent<PlayerMovementController>();
                var input = new Vector2(0.73f, 0.21f);
                var result = movement.Move(input, 0.5f);

                Assert.AreEqual(input.x * PlayerMovementController.DefaultSpeedMetersPerSecond * 0.5f, result.x, 0.001f);
                Assert.AreEqual(input.y * PlayerMovementController.DefaultSpeedMetersPerSecond * 0.5f, result.z, 0.001f);
                var movedDirection = new Vector2(result.x, result.z).normalized;
                Assert.AreEqual(input.normalized.x, movedDirection.x, 0.001f);
                Assert.AreEqual(input.normalized.y, movedDirection.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void GuardFacingUsesManualAimMouseAimAndBodyFacing()
        {
            var root = new GameObject("M111GuardHarness");
            var cameraObject = new GameObject("Main Camera");
            try
            {
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
                camera.transform.position = new Vector3(0f, 10f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);
                var defense = player.gameObject.AddComponent<PlayerDefenseController>();
                defense.Bind(null, combat);

                var manual = new Vector2(-0.8f, 0.2f).normalized;
                defense.Tick(Snapshot(Vector2.zero, new Vector2(-0.8f, 0.2f), lockPressed: false, guardHeld: true), 0.01f, 0f);
                Assert.AreEqual(manual.x, defense.GuardFacing.x, 0.001f);
                Assert.AreEqual(manual.y, defense.GuardFacing.z, 0.001f);

                var pointer = camera.WorldToScreenPoint(new Vector3(3f, 0f, 1f));
                var expectedMouse = new Vector2(3f, 1f).normalized;
                defense.Tick(SnapshotWithPointer(new Vector2(pointer.x, pointer.y), lockPressed: false, guardHeld: true), 0.01f, 0.1f);
                Assert.AreEqual(expectedMouse.x, defense.GuardFacing.x, 0.001f);
                Assert.AreEqual(expectedMouse.y, defense.GuardFacing.z, 0.001f);

                defense.Tick(
                    SnapshotWithPointerAndMove(Vector2.left, new Vector2(pointer.x, pointer.y), lockPressed: false, mouseAimIntent: false, guardHeld: true),
                    0.01f,
                    PlayerAimLockController.MouseAimIntentMemorySeconds + 0.3f);
                Assert.AreEqual(-1f, defense.GuardFacing.x, 0.001f);
                Assert.AreEqual(0f, defense.GuardFacing.z, 0.001f);

                defense.Tick(
                    SnapshotWithPointerAndMove(Vector2.zero, new Vector2(pointer.x, pointer.y), lockPressed: false, mouseAimIntent: false, guardHeld: true),
                    0.01f,
                    PlayerAimLockController.MouseAimIntentMemorySeconds + 0.4f);
                Assert.AreEqual(-1f, defense.GuardFacing.x, 0.001f);
                Assert.AreEqual(0f, defense.GuardFacing.z, 0.001f);

                var lockedEnemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 2f));
                AddEnemy(combat, lockedEnemy);
                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: true), PlayerAimLockController.MouseAimIntentMemorySeconds + 0.5f);
                Assert.IsFalse(aim.IsExplicitlyLocked);
                defense.Tick(SnapshotWithPointer(new Vector2(pointer.x, pointer.y), lockPressed: false, guardHeld: true), 0.01f, PlayerAimLockController.MouseAimIntentMemorySeconds + 0.6f);
                Assert.AreEqual(expectedMouse.x, defense.GuardFacing.x, 0.001f);
                Assert.AreEqual(expectedMouse.y, defense.GuardFacing.z, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RollPrefersMovementDirectionBeforeAimFallback()
        {
            var playerObject = new GameObject("M111RollHarness");
            try
            {
                var weapon = playerObject.AddComponent<PlayerWeaponController>();
                var move = new Vector2(0.2f, 0.7f);
                Assert.IsTrue(weapon.TryRoll(move, Vector2.right, 0f));
                Assert.AreEqual(move.normalized.x, weapon.RollDirection.x, 0.001f);
                Assert.AreEqual(move.normalized.y, weapon.RollDirection.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }

            playerObject = new GameObject("M111RollFallbackHarness");
            try
            {
                var weapon = playerObject.AddComponent<PlayerWeaponController>();
                var aim = new Vector2(0.9f, 0.1f).normalized;
                Assert.IsTrue(weapon.TryRoll(Vector2.zero, new Vector2(0.9f, 0.1f), 0f));
                Assert.AreEqual(aim.x, weapon.RollDirection.x, 0.001f);
                Assert.AreEqual(aim.y, weapon.RollDirection.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void RollIgnoresTargetLockAndUsesMoveOrAimDirection()
        {
            AssertRollIgnoresTargetLockDirection(Vector2.right, Vector2.right, Vector2.right);
            AssertRollIgnoresTargetLockDirection(Vector2.left, Vector2.right, Vector2.left);
            AssertRollIgnoresTargetLockDirection(Vector2.down, Vector2.right, Vector2.down);
            AssertRollIgnoresTargetLockDirection(Vector2.zero, Vector2.right, Vector2.right);
        }

        private static GameplayInputSnapshot Snapshot(Vector2 move, Vector2 shoot, bool lockPressed, bool guardHeld = false)
        {
            return new GameplayInputSnapshot(
                move,
                shoot,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: guardHeld,
                pausePressed: false,
                rollPressed: false,
                lockTargetPressed: lockPressed);
        }

        private static GameplayInputSnapshot SnapshotWithPointer(Vector2 pointerScreenPosition, bool lockPressed, bool guardHeld = false)
        {
            return SnapshotWithPointerAndMove(Vector2.zero, pointerScreenPosition, lockPressed, mouseAimIntent: true, guardHeld: guardHeld);
        }

        private static GameplayInputSnapshot SnapshotWithPointerAndMove(
            Vector2 move,
            Vector2 pointerScreenPosition,
            bool lockPressed,
            bool mouseAimIntent,
            bool guardHeld = false)
        {
            return new GameplayInputSnapshot(
                move,
                Vector2.zero,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: guardHeld,
                pausePressed: false,
                rollPressed: false,
                lockTargetPressed: lockPressed,
                pointerScreenPosition: pointerScreenPosition,
                hasPointerScreenPosition: true,
                mouseAimIntent: mouseAimIntent);
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, Vector3 localPosition)
        {
            var enemyObject = new GameObject("M111Enemy");
            enemyObject.transform.SetParent(parent, false);
            enemyObject.transform.localPosition = localPosition;
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(null, null, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"), null);
            return enemy;
        }

        private static void AddEnemy(RoomCombatController combat, EnemyRuntimeController enemy)
        {
            var field = typeof(RoomCombatController).GetField("enemies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            var enemies = (List<EnemyRuntimeController>)field.GetValue(combat);
            enemies.Add(enemy);
        }

        private static void AssertRangedShotIgnoresTargetAndUsesDirection(bool lockPressed)
        {
            var root = new GameObject(lockPressed ? "M111ManualLockIgnoredShotHarness" : "M111AutoTargetIgnoredShotHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            projectilePrefab.transform.SetParent(root.transform, false);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(root.transform, false);
                var aim = playerObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);
                var weapon = playerObject.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                var enemy = CreateEnemy(root.transform, new Vector3(1.4f, 0f, 2.2f));
                AddEnemy(combat, enemy);

                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: lockPressed), 0f);
                Assert.IsNull(aim.LockedEnemy);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);

                Assert.IsTrue(weapon.TryFire(Vector2.up, 0f));
                weapon.TickAction(0f, WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);

                var projectile = root.transform.Find("PlayerProjectile");
                Assert.IsNotNull(projectile);
                var expected = enemy.transform.localPosition - projectile.localPosition;
                expected.y = 0f;
                expected.Normalize();
                Assert.Greater(Vector3.Dot(projectile.forward, Vector3.forward), 0.999f);
                Assert.Less(Vector3.Dot(projectile.forward, expected), 0.999f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertRollIgnoresTargetLockDirection(Vector2 move, Vector2 aimDirection, Vector2 expected)
        {
            var root = new GameObject("M111TargetLockIgnoredRollHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player").AddComponent<PlaceholderPlayerController>();
                player.transform.SetParent(root.transform, false);
                var aim = player.gameObject.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);
                var weapon = player.gameObject.AddComponent<PlayerWeaponController>();
                var enemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 2f));
                AddEnemy(combat, enemy);

                aim.TickAim(Snapshot(Vector2.zero, Vector2.zero, lockPressed: true), 0f);
                Assert.AreEqual(PlayerTargetLockMode.None, aim.CurrentLockMode);
                Assert.IsNull(aim.LockedEnemy);
                Assert.IsTrue(weapon.TryRoll(move, aimDirection, 0f));
                Assert.AreEqual(expected.normalized.x, weapon.RollDirection.x, 0.001f);
                Assert.AreEqual(expected.normalized.y, weapon.RollDirection.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
