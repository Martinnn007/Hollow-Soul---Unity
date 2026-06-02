using System.Collections.Generic;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Input;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class ManualAimProjectileDirectionTests
    {
        [TestCase(0f, 1f)]
        [TestCase(45f, 1f)]
        [TestCase(90f, 0.5f)]
        public void MouseAimProjectionUsesSharedGameplayProjectionForTransformedRoots(float rootYawDegrees, float rootScale)
        {
            var root = new GameObject("ManualAimProjectionRoot");
            var cameraObject = new GameObject("Main Camera");
            var hiddenCameras = HideExistingCameras();
            try
            {
                root.AddComponent<PlatformPresentationRoot>().Configure(HollowPlatformKind.VisionOSBoundedTabletop);
                root.transform.localRotation = Quaternion.Euler(0f, rootYawDegrees, 0f);
                root.transform.localScale = Vector3.one * rootScale;

                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 8f;
                camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
                camera.transform.position = new Vector3(0f, 12f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player");
                player.transform.SetParent(root.transform, false);
                var aim = player.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);

                var localTarget = new Vector3(2f, 0f, 3f);
                var pointer = camera.WorldToScreenPoint(root.transform.TransformPoint(localTarget));
                var input = SnapshotWithPointer(new Vector2(pointer.x, pointer.y));
                var direction = aim.ResolveAttackDirection(input, 0f);
                var expected = new Vector2(localTarget.x, localTarget.z).normalized;

                Assert.AreEqual(expected.x, direction.x, 0.001f);
                Assert.AreEqual(expected.y, direction.y, 0.001f);
                Assert.IsFalse(aim.IsTargetLocked);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
                RestoreHiddenCameras(hiddenCameras);
            }
        }

        private static HiddenCameraState[] HideExistingCameras()
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            var states = new HiddenCameraState[cameras.Length];
            for (var index = 0; index < cameras.Length; index++)
            {
                states[index] = new HiddenCameraState(cameras[index], cameras[index].gameObject.activeSelf);
                cameras[index].gameObject.SetActive(false);
            }

            return states;
        }

        private static void RestoreHiddenCameras(HiddenCameraState[] states)
        {
            for (var index = 0; index < states.Length; index++)
            {
                if (states[index].Camera != null)
                {
                    states[index].Camera.gameObject.SetActive(states[index].WasActive);
                }
            }
        }

        private readonly struct HiddenCameraState
        {
            public HiddenCameraState(Camera camera, bool wasActive)
            {
                Camera = camera;
                WasActive = wasActive;
            }

            public Camera Camera { get; }

            public bool WasActive { get; }
        }

        [Test]
        public void BodyFacingShotFiresAlongResolvedFacingDirection()
        {
            var root = CreateProjectileHarness(out var aim, out var weapon, out var projectilePrefab);
            try
            {
                aim.TickAim(Snapshot(Vector2.right, Vector2.zero), 0f);
                var direction = aim.ResolveAttackDirection(Snapshot(Vector2.zero, Vector2.zero), 0.1f);
                Assert.AreEqual(Vector2.right, direction);

                var projectile = FireSingleProjectile(root, weapon, direction, 0.2f);
                AssertProjectileForward(projectile, Vector2.right);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ManualStickShotFiresAlongRequestedDirection()
        {
            var root = CreateProjectileHarness(out _, out var weapon, out var projectilePrefab);
            try
            {
                var requested = new Vector2(0.8f, 0.25f).normalized;
                var projectile = FireSingleProjectile(root, weapon, requested, 0f);
                AssertProjectileForward(projectile, requested);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TripleShotPreservesSpreadAroundManualBaseDirection()
        {
            var root = CreateProjectileHarness(out _, out var weapon, out var projectilePrefab);
            try
            {
                weapon.ConfigureProjectilePassives(new ProjectilePassiveState(
                    ProjectilePatternKind.TripleShot,
                    1f,
                    0f,
                    ProjectileVisualStyle.Default));

                FireProjectiles(root, weapon, Vector2.up, 0f);
                var projectiles = FindPlayerProjectiles(root);
                Assert.AreEqual(3, projectiles.Count);

                var absoluteAngles = new List<float>();
                foreach (var projectile in projectiles)
                {
                    var direction = new Vector2(projectile.forward.x, projectile.forward.z).normalized;
                    absoluteAngles.Add(Mathf.Abs(Vector2.SignedAngle(Vector2.up, direction)));
                }

                absoluteAngles.Sort();
                Assert.AreEqual(0f, absoluteAngles[0], 0.001f);
                Assert.AreEqual(30f, absoluteAngles[1], 0.001f);
                Assert.AreEqual(30f, absoluteAngles[2], 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DrawAndReleaseKeepsReleaseDirectionThroughWindup()
        {
            var root = CreateProjectileHarness(out _, out var weapon, out var projectilePrefab);
            var catalog = ScriptableObject.CreateInstance<WeaponCatalogDefinition>();
            var bow = ScriptableObject.CreateInstance<WeaponDefinition>();
            try
            {
                bow.Configure(
                    "diagnostic_bow",
                    "Diagnostic Bow",
                    WeaponSlot.Ranged,
                    WeaponCategory.Bow,
                    nextLightAttack: new WeaponAttackDefinition(AttackKind.Light, 1, 0.05f, 0f, 6.5f, ImpactForceClass.Light, 0.32f, 0.01f, 0.03f, 0.16f, 1f, 0.02f));
                catalog.Configure("manual_aim_projectile_direction_tests", new[] { bow });
                weapon.ConfigureWeaponCatalog(catalog);
                weapon.ConfigureBuildStats(
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

                var drawDirection = Vector2.up;
                var releaseDirection = new Vector2(-0.6f, 0.8f).normalized;
                Assert.IsTrue(weapon.TryFire(drawDirection, 0f));
                Assert.IsTrue(weapon.TryReleaseRangedDraw(releaseDirection, 0.03f));

                weapon.TickAction(0f, 0.03f + 0.02f);
                var projectile = root.transform.Find("PlayerProjectile");
                Assert.IsNotNull(projectile);
                AssertProjectileForward(projectile, releaseDirection);
            }
            finally
            {
                Object.DestroyImmediate(bow);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateProjectileHarness(
            out PlayerAimLockController aim,
            out PlayerWeaponController weapon,
            out GameObject projectilePrefab)
        {
            var root = new GameObject("ManualAimProjectileHarness");
            var combat = root.AddComponent<RoomCombatController>();
            var player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            aim = player.AddComponent<PlayerAimLockController>();
            aim.Configure(combat);
            weapon = player.AddComponent<PlayerWeaponController>();
            projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            projectilePrefab.transform.SetParent(root.transform, false);
            weapon.Configure(null, combat, projectilePrefab);
            weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
            return root;
        }

        private static Transform FireSingleProjectile(GameObject root, PlayerWeaponController weapon, Vector2 direction, float timeSeconds)
        {
            FireProjectiles(root, weapon, direction, timeSeconds);
            var projectile = root.transform.Find("PlayerProjectile");
            Assert.IsNotNull(projectile);
            return projectile;
        }

        private static void FireProjectiles(GameObject root, PlayerWeaponController weapon, Vector2 direction, float timeSeconds)
        {
            Assert.IsTrue(weapon.TryFire(direction, timeSeconds));
            weapon.TickAction(0f, timeSeconds + WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);
            Assert.Greater(FindPlayerProjectiles(root).Count, 0);
        }

        private static List<Transform> FindPlayerProjectiles(GameObject root)
        {
            var projectiles = new List<Transform>();
            for (var index = 0; index < root.transform.childCount; index++)
            {
                var child = root.transform.GetChild(index);
                if (child != null && child.name == "PlayerProjectile")
                {
                    projectiles.Add(child);
                }
            }

            return projectiles;
        }

        private static void AssertProjectileForward(Transform projectile, Vector2 expectedDirection)
        {
            var expected = new Vector3(expectedDirection.x, 0f, expectedDirection.y).normalized;
            Assert.Greater(Vector3.Dot(projectile.forward, expected), 0.999f);
        }

        private static GameplayInputSnapshot Snapshot(Vector2 move, Vector2 shoot)
        {
            return new GameplayInputSnapshot(move, shoot);
        }

        private static GameplayInputSnapshot SnapshotWithPointer(Vector2 pointerScreenPosition)
        {
            return new GameplayInputSnapshot(
                Vector2.zero,
                Vector2.zero,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: false,
                pausePressed: false,
                rollPressed: false,
                lockTargetPressed: false,
                pointerScreenPosition: pointerScreenPosition,
                hasPointerScreenPosition: true,
                mouseAimIntent: true);
        }
    }
}
