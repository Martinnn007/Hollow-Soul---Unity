using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hollow.Tests.PlayMode
{
    public sealed class LiveManualAimProjectileInputPlayModeTests : InputTestFixture
    {
        private const float MaxAngleErrorDegrees = 1.25f;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            GameplayPauseState.SetPaused(false);
            GameplayTransitionState.ResetForTests();
            PlayerAimShotTelemetry.Reset();
            GameplayInputReader.SetExternalMoveOverride(Vector2.zero);
        }

        [TearDown]
        public override void TearDown()
        {
            GameplayPauseState.SetPaused(false);
            GameplayTransitionState.ResetForTests();
            GameplayInputReader.SetExternalMoveOverride(Vector2.zero);
            PlayerAimShotTelemetry.Reset();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator WeaponUpdateFiresExpectedManualDirectionAcrossLiveInputScenarios()
        {
            yield return RunLiveScenario(LiveScenario.WindowsRightStick());
            yield return RunLiveScenario(LiveScenario.KeyboardArrows());
            yield return RunLiveScenario(LiveScenario.MouseClick());
            yield return RunLiveScenario(LiveScenario.MovingRightStick());
            yield return RunLiveScenario(LiveScenario.VisionOSBoundedRightStick());
            yield return RunLiveScenario(LiveScenario.WaveTurretLayout());
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator PooledPlayerProjectileSpawnResetsDirectionSpeedLifetimeAndScale()
        {
            var context = CreateContext("PooledProjectileReset", Vector3.zero, Vector3.zero, 1f, 0f, false);
            GameObject firstObject = null;
            GameObject secondObject = null;
            try
            {
                var first = FireDeterministic(context.Weapon, Vector2.right, 10f);
                firstObject = first.gameObject;
                first.transform.localScale = Vector3.one * 3f;
                first.Configure(null, null, Vector3.left, 1, 2f, 0.2f);
                first.Tick(0.19f);
                HollowRuntimePool.Return(firstObject);

                var second = FireDeterministic(context.Weapon, Vector2.up, 11f);
                secondObject = second.gameObject;
                second.Tick(0.05f);

                Assert.AreSame(firstObject, secondObject);
                Assert.IsTrue(second.gameObject.activeInHierarchy);
                Assert.AreEqual(1f, second.transform.localScale.x, 0.001f);
                Assert.AreEqual(1f, second.transform.localScale.y, 0.001f);
                Assert.AreEqual(1f, second.transform.localScale.z, 0.001f);
                Assert.AreEqual(Vector3.forward.x, second.ConfiguredLocalDirection.x, 0.001f);
                Assert.AreEqual(Vector3.forward.z, second.ConfiguredLocalDirection.z, 0.001f);
                Assert.AreEqual(PlayerWeaponController.PlayerProjectileSpeedMetersPerSecond, second.ConfiguredSpeedMetersPerSecond, 0.001f);
                Assert.Greater(second.ConfiguredLifetimeSeconds, 0.2f);
                Assert.AreEqual(0.05f, second.AgeSeconds, 0.001f);
                Assert.AreEqual(0, PlayerAimShotTelemetry.LockedShotCount);
            }
            finally
            {
                if (secondObject != null && secondObject.activeInHierarchy)
                {
                    HollowRuntimePool.Return(secondObject);
                }

                if (firstObject != null)
                {
                    Object.DestroyImmediate(firstObject);
                }

                if (secondObject != null && secondObject != firstObject)
                {
                    Object.DestroyImmediate(secondObject);
                }

                DestroyContext(context);
            }

            yield break;
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator ActiveEnemyProjectileDoesNotRedirectOrSuppressManualPlayerShot()
        {
            var context = CreateContext("ActiveEnemyProjectileManualShot", Vector3.zero, Vector3.zero, 1f, 0f, false);
            GameObject enemyProjectileObject = null;
            try
            {
                enemyProjectileObject = HollowRuntimePool.RentPrimitive("EnemyProjectile.ActiveManualAimDiagnostic", PrimitiveType.Sphere, context.Root.transform);
                enemyProjectileObject.name = "EnemyProjectile.ActiveManualAimDiagnostic";
                enemyProjectileObject.transform.localPosition = new Vector3(-4f, 0.35f, -4f);
                enemyProjectileObject.transform.localScale = Vector3.one * 0.22f;
                var enemyProjectile = enemyProjectileObject.GetComponent<EnemyProjectileController>() ?? enemyProjectileObject.AddComponent<EnemyProjectileController>();
                enemyProjectile.Configure(context.Room, context.Player, Vector3.left, 1, 3f, 5f);
                enemyProjectile.ConfigureThreat(DamageThreatKind.Light);

                var expectedDirection = new Vector2(0.6f, 0.8f).normalized;
                context.Weapon.TickInput(
                    new GameplayInputSnapshot(Vector2.zero, expectedDirection, false, false, true, false),
                    0.016f,
                    30f);
                context.Weapon.TickAction(0f, 30f + WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);
                var projectiles = CollectPlayerProjectiles(context.Root);
                var projectile = projectiles.FirstOrDefault();

                Assert.AreEqual(1, projectiles.Count);
                Assert.IsNotNull(projectile);
                Assert.IsTrue(enemyProjectileObject.activeInHierarchy);
                Assert.IsNotNull(enemyProjectileObject.GetComponent<EnemyProjectileController>());
                Assert.IsNull(projectile.GetComponent<EnemyProjectileController>());
                Assert.LessOrEqual(Vector2.Angle(expectedDirection, ToPlanarDirection(projectile.ConfiguredLocalDirection)), MaxAngleErrorDegrees);
                Assert.AreEqual(PlayerWeaponController.PlayerProjectileSpeedMetersPerSecond, projectile.ConfiguredSpeedMetersPerSecond, 0.001f);
                Assert.AreEqual(1, PlayerAimShotTelemetry.ShotCount);
                Assert.AreEqual(0, PlayerAimShotTelemetry.LockedShotCount);
                Assert.AreEqual(PlayerShotAimSource.ManualAim, PlayerAimShotTelemetry.LastShot.AimSource);
                Assert.LessOrEqual(Vector2.Angle(expectedDirection, PlayerAimShotTelemetry.LastShot.AimDirection), MaxAngleErrorDegrees);
            }
            finally
            {
                if (enemyProjectileObject != null && enemyProjectileObject.activeInHierarchy)
                {
                    HollowRuntimePool.Return(enemyProjectileObject);
                }

                DestroyContext(context);
            }

            yield break;
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator EnemyProjectilePoolDoesNotBleedOwnershipIntoPlayerProjectileRent()
        {
            var context = CreateContext("EnemyProjectilePoolIsolation", Vector3.zero, Vector3.zero, 1f, 0f, false);
            GameObject enemyProjectileObject = null;
            GameObject playerProjectileObject = null;
            try
            {
                enemyProjectileObject = HollowRuntimePool.RentPrimitive("EnemyProjectile.PoolIsolation", PrimitiveType.Sphere, context.Root.transform);
                enemyProjectileObject.name = "EnemyProjectile.PoolIsolation";
                enemyProjectileObject.transform.localPosition = new Vector3(2f, 0.35f, 2f);
                enemyProjectileObject.transform.localScale = Vector3.one * 3f;
                var enemyProjectile = enemyProjectileObject.GetComponent<EnemyProjectileController>() ?? enemyProjectileObject.AddComponent<EnemyProjectileController>();
                enemyProjectile.Configure(context.Room, context.Player, Vector3.right, 1, 7f, 0.8f);
                enemyProjectile.ConfigureThreat(DamageThreatKind.Light);
                HollowRuntimePool.Return(enemyProjectileObject);

                var playerProjectile = FireDeterministic(context.Weapon, Vector2.up, 40f);
                playerProjectileObject = playerProjectile.gameObject;

                Assert.AreNotSame(enemyProjectileObject, playerProjectileObject);
                Assert.AreEqual("PlayerProjectile", playerProjectileObject.name);
                Assert.IsNull(playerProjectileObject.GetComponent<EnemyProjectileController>());
                Assert.IsNotNull(playerProjectileObject.GetComponent<ProjectileController>());
                Assert.AreEqual(1f, playerProjectileObject.transform.localScale.x, 0.001f);
                Assert.AreEqual(1f, playerProjectileObject.transform.localScale.y, 0.001f);
                Assert.AreEqual(1f, playerProjectileObject.transform.localScale.z, 0.001f);
                Assert.AreEqual(Vector3.forward.x, playerProjectile.ConfiguredLocalDirection.x, 0.001f);
                Assert.AreEqual(Vector3.forward.z, playerProjectile.ConfiguredLocalDirection.z, 0.001f);
                Assert.AreEqual(PlayerWeaponController.PlayerProjectileSpeedMetersPerSecond, playerProjectile.ConfiguredSpeedMetersPerSecond, 0.001f);
                var expectedLifetimeSeconds = WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).RangeMeters / PlayerWeaponController.PlayerProjectileSpeedMetersPerSecond;
                Assert.AreEqual(expectedLifetimeSeconds, playerProjectile.ConfiguredLifetimeSeconds, 0.001f);
                Assert.That(playerProjectile.ConfiguredLifetimeSeconds, Is.Not.EqualTo(0.8f).Within(0.001f));
            }
            finally
            {
                if (playerProjectileObject != null && playerProjectileObject.activeInHierarchy)
                {
                    HollowRuntimePool.Return(playerProjectileObject);
                }

                if (enemyProjectileObject != null && enemyProjectileObject.activeInHierarchy)
                {
                    HollowRuntimePool.Return(enemyProjectileObject);
                }

                DestroyContext(context);
            }

            yield break;
        }

        private IEnumerator RunLiveScenario(LiveScenario spec)
        {
            PlayerAimShotTelemetry.Reset();
            var context = CreateContext(spec.Id, spec.CameraPosition, spec.CameraEulerAngles, spec.RootScale, spec.RootYawDegrees, spec.SpawnEnemies);
            try
            {
                var expectedDirection = spec.ExpectedDirection(context.Camera, context.Root.transform);
                ApplyLiveInput(spec, context.Camera, context.Root.transform);
                InputSystem.Update();
                InvokeWeaponUpdate(context.Weapon);
                context.Weapon.TickAction(0f, Time.time + WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);

                var projectile = CollectPlayerProjectiles(context.Root).FirstOrDefault();
                Assert.IsNotNull(projectile, spec.Id);

                var configured = ToPlanarDirection(projectile.ConfiguredLocalDirection);
                Assert.LessOrEqual(Vector2.Angle(expectedDirection, configured), MaxAngleErrorDegrees, spec.Id);
                Assert.AreEqual(PlayerWeaponController.PlayerProjectileSpeedMetersPerSecond, projectile.ConfiguredSpeedMetersPerSecond, 0.001f, spec.Id);
                Assert.AreEqual(0, PlayerAimShotTelemetry.LockedShotCount, spec.Id);
                Assert.AreNotEqual(PlayerShotAimSource.AutoLock, PlayerAimShotTelemetry.LastShot.AimSource, spec.Id);
                Assert.AreNotEqual(PlayerShotAimSource.ManualLock, PlayerAimShotTelemetry.LastShot.AimSource, spec.Id);
            }
            finally
            {
                DestroyContext(context);
            }

            yield break;
        }

        private static ProjectileController FireDeterministic(PlayerWeaponController weapon, Vector2 direction, float timeSeconds)
        {
            Assert.IsTrue(weapon.TryFire(direction, timeSeconds));
            weapon.TickAction(0f, timeSeconds + WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);
            return CollectPlayerProjectiles(weapon.transform.parent.gameObject).First(projectile => projectile.gameObject.activeInHierarchy);
        }

        private static TestContextObjects CreateContext(
            string id,
            Vector3 cameraPosition,
            Vector3 cameraEulerAngles,
            float rootScale,
            float rootYawDegrees,
            bool spawnEnemies)
        {
            var root = new GameObject(id);
            var presentation = root.AddComponent<PlatformPresentationRoot>();
            presentation.Configure(HollowPlatformKind.WindowsStandard3D, rootScale, rootYawDegrees);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.Euler(cameraEulerAngles);

            var room = root.AddComponent<RoomRuntimeRoot>();
            room.ConfigureDefault();
            var combat = root.AddComponent<RoomCombatController>();
            var player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            var playerController = player.AddComponent<PlaceholderPlayerController>();
            playerController.ConfigureDefault();
            player.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            var aim = player.AddComponent<PlayerAimLockController>();
            aim.Configure(combat);
            var weapon = player.AddComponent<PlayerWeaponController>();
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.name = $"{id}.ProjectilePrefab";
            projectilePrefab.SetActive(false);
            projectilePrefab.AddComponent<ProjectileController>();
            weapon.Configure(room, combat, projectilePrefab);
            weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);

            if (spawnEnemies)
            {
                SpawnDiagnosticEnemy(combat, root.transform, new Vector3(0f, 0f, 4f), "DiagnosticTurret");
                SpawnDiagnosticEnemy(combat, root.transform, new Vector3(2.5f, 0f, 3.5f), "DiagnosticWaveEnemy");
            }

            return new TestContextObjects(root, cameraObject, camera, room, combat, playerController, projectilePrefab, weapon);
        }

        private static void DestroyContext(TestContextObjects context)
        {
            if (context.ProjectilePrefab != null)
            {
                Object.DestroyImmediate(context.ProjectilePrefab);
            }

            if (context.CameraObject != null)
            {
                Object.DestroyImmediate(context.CameraObject);
            }

            if (context.Root != null)
            {
                Object.DestroyImmediate(context.Root);
            }
        }

        private void ApplyLiveInput(LiveScenario spec, Camera camera, Transform gameplayRoot)
        {
            RemoveInputDevices();
            if (spec.UseMouse)
            {
                var mouse = InputSystem.AddDevice<Mouse>();
                var targetLocal = new Vector3(0.35f, 0f, 0.9f).normalized * 4f;
                var pointer = camera.WorldToScreenPoint(gameplayRoot.TransformPoint(targetLocal));
                Set(mouse.position, new Vector2(pointer.x, pointer.y));
                Set(mouse.delta, new Vector2(6f, 2f));
                Press(mouse.leftButton);
                return;
            }

            if (spec.UseKeyboard)
            {
                var keyboard = InputSystem.AddDevice<Keyboard>();
                Press(keyboard.upArrowKey);
                return;
            }

            var gamepad = InputSystem.AddDevice<Gamepad>();
            if (spec.Moving)
            {
                Set(gamepad.leftStick, new Vector2(-0.35f, 0.15f));
            }

            Set(gamepad.rightStick, spec.StickAim);
            Press(gamepad.rightShoulder);
        }

        private static void RemoveInputDevices()
        {
            for (var index = InputSystem.devices.Count - 1; index >= 0; index--)
            {
                InputSystem.RemoveDevice(InputSystem.devices[index]);
            }
        }

        private static void InvokeWeaponUpdate(PlayerWeaponController weapon)
        {
            typeof(PlayerWeaponController)
                .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(weapon, null);
        }

        private static List<ProjectileController> CollectPlayerProjectiles(GameObject root)
        {
            return root.GetComponentsInChildren<ProjectileController>()
                .Where(projectile => projectile != null &&
                    projectile.gameObject.activeInHierarchy &&
                    projectile.name == "PlayerProjectile")
                .OrderBy(projectile => projectile.transform.localPosition.x)
                .ThenBy(projectile => projectile.transform.localPosition.z)
                .ToList();
        }

        private static Vector2 ExpectedProjectedDirection(Vector3 cameraAxis, Transform gameplayRoot)
        {
            var world = Vector3.ProjectOnPlane(cameraAxis, Vector3.up).normalized;
            var local = gameplayRoot.InverseTransformDirection(world);
            return new Vector2(local.x, local.z).normalized;
        }

        private static Vector2 ToPlanarDirection(Vector3 value)
        {
            var direction = new Vector2(value.x, value.z);
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
        }

        private static EnemyRuntimeController SpawnDiagnosticEnemy(RoomCombatController combat, Transform parent, Vector3 localPosition, string name)
        {
            var enemyObject = new GameObject(name);
            enemyObject.transform.SetParent(parent, false);
            enemyObject.transform.localPosition = localPosition;
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(null, null, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret"), null);
            var field = typeof(RoomCombatController).GetField("enemies", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(combat) is List<EnemyRuntimeController> enemies)
            {
                enemies.Add(enemy);
            }

            return enemy;
        }

        private sealed class TestContextObjects
        {
            public TestContextObjects(
                GameObject root,
                GameObject cameraObject,
                Camera camera,
                RoomRuntimeRoot room,
                RoomCombatController combat,
                PlaceholderPlayerController player,
                GameObject projectilePrefab,
                PlayerWeaponController weapon)
            {
                Root = root;
                CameraObject = cameraObject;
                Camera = camera;
                Room = room;
                Combat = combat;
                Player = player;
                ProjectilePrefab = projectilePrefab;
                Weapon = weapon;
            }

            public GameObject Root { get; }

            public GameObject CameraObject { get; }

            public Camera Camera { get; }

            public RoomRuntimeRoot Room { get; }

            public RoomCombatController Combat { get; }

            public PlaceholderPlayerController Player { get; }

            public GameObject ProjectilePrefab { get; }

            public PlayerWeaponController Weapon { get; }
        }

        private readonly struct LiveScenario
        {
            private LiveScenario(
                string id,
                Vector3 cameraPosition,
                Vector3 cameraEulerAngles,
                float rootScale,
                float rootYawDegrees,
                Vector2 stickAim,
                bool useKeyboard = false,
                bool useMouse = false,
                bool moving = false,
                bool spawnEnemies = false)
            {
                Id = id;
                CameraPosition = cameraPosition;
                CameraEulerAngles = cameraEulerAngles;
                RootScale = rootScale;
                RootYawDegrees = rootYawDegrees;
                StickAim = stickAim;
                UseKeyboard = useKeyboard;
                UseMouse = useMouse;
                Moving = moving;
                SpawnEnemies = spawnEnemies;
            }

            public string Id { get; }

            public Vector3 CameraPosition { get; }

            public Vector3 CameraEulerAngles { get; }

            public float RootScale { get; }

            public float RootYawDegrees { get; }

            public bool SpawnEnemies { get; }

            public Vector2 StickAim { get; }

            public bool UseKeyboard { get; }

            public bool UseMouse { get; }

            public bool Moving { get; }

            public Vector2 ExpectedDirection(Camera camera, Transform gameplayRoot)
            {
                if (UseMouse)
                {
                    return new Vector2(0.35f, 0.9f).normalized;
                }

                if (UseKeyboard || StickAim == Vector2.up)
                {
                    return ExpectedProjectedDirection(camera.transform.up, gameplayRoot);
                }

                var right = ExpectedProjectedDirection(camera.transform.right, gameplayRoot);
                var up = ExpectedProjectedDirection(camera.transform.up, gameplayRoot);
                return (right * StickAim.x + up * StickAim.y).normalized;
            }

            public static LiveScenario WindowsRightStick()
            {
                return new LiveScenario(
                    "windows_profile_right_stick",
                    new Vector3(-6.5f, 8.25f, -6.5f),
                    new Vector3(42f, 45f, 0f),
                    1f,
                    0f,
                    new Vector2(0.7f, 0.35f));
            }

            public static LiveScenario KeyboardArrows()
            {
                return new LiveScenario(
                    "keyboard_arrow_profile_aim",
                    new Vector3(-6.5f, 8.25f, -6.5f),
                    new Vector3(42f, 45f, 0f),
                    1f,
                    0f,
                    Vector2.up,
                    useKeyboard: true);
            }

            public static LiveScenario MouseClick()
            {
                return new LiveScenario(
                    "mouse_click_shared_projection",
                    new Vector3(0f, 12f, 0f),
                    new Vector3(90f, 0f, 0f),
                    1f,
                    0f,
                    Vector2.zero,
                    useMouse: true);
            }

            public static LiveScenario MovingRightStick()
            {
                return new LiveScenario(
                    "moving_right_stick_profile_aim",
                    new Vector3(-6.5f, 8.25f, -6.5f),
                    new Vector3(42f, 45f, 0f),
                    1f,
                    0f,
                    new Vector2(-0.8f, 0.2f),
                    moving: true);
            }

            public static LiveScenario VisionOSBoundedRightStick()
            {
                return new LiveScenario(
                    "visionos_bounded_scaled_root",
                    new Vector3(0f, 1.35f, -2.4f),
                    new Vector3(24f, 0f, 0f),
                    0.5f,
                    45f,
                    Vector2.up);
            }

            public static LiveScenario WaveTurretLayout()
            {
                return new LiveScenario(
                    "wave_turret_layout_right_stick",
                    new Vector3(-6.5f, 8.25f, -6.5f),
                    new Vector3(42f, 45f, 0f),
                    1f,
                    0f,
                    new Vector2(0.2f, 0.98f),
                    spawnEnemies: true);
            }
        }
    }
}
