using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Input;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hollow.Tests.PlayMode
{
    public sealed class ManualAimProjectileDiagnosticsPlayModeTests
    {
        private const float MaxAngleErrorDegrees = 1.25f;
        private const float MaxSpeedErrorMetersPerSecond = 0.25f;
        private const string JsonPath = "output/reports/aim_diagnostics/manual_aim_projectile_diagnostics.json";
        private const string MarkdownPath = "output/reports/aim_diagnostics/manual_aim_projectile_diagnostics.md";

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator ManualAimingProjectilePathsStayAlignedAndReportMeasurements()
        {
            var report = new ManualAimProjectileDiagnosticsReport
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                inputReaderSection = "Raw InputSystem edge timing is covered by ManualAimInputReaderTests. This PlayMode harness drives PlayerWeaponController.TickInput deterministically.",
                maxAngleErrorDegrees = MaxAngleErrorDegrees,
                maxSpeedErrorMetersPerSecond = MaxSpeedErrorMetersPerSecond
            };

            yield return RunScenario(report, ScenarioSpec.BodyFacing());
            yield return RunScenario(report, ScenarioSpec.RightStick());
            yield return RunScenario(report, ScenarioSpec.MouseCursor());
            yield return RunScenario(report, ScenarioSpec.MovingPlayerRightStick());
            yield return RunScenario(report, ScenarioSpec.RotatedRootMouseCursor());
            yield return RunScenario(report, ScenarioSpec.ScaledBoundedRootRightStick());
            yield return RunScenario(report, ScenarioSpec.WaveTurretLayoutRightStick());
            yield return RunScenario(report, ScenarioSpec.TripleShotSpread());

            report.passed = report.scenarios.All(scenario => scenario.passed);
            WriteReport(report);

            Assert.IsTrue(report.passed, string.Join("\n", report.scenarios.Where(scenario => !scenario.passed).Select(scenario => scenario.summary)));
            Assert.IsTrue(File.Exists(JsonPath), JsonPath);
            Assert.IsTrue(File.Exists(MarkdownPath), MarkdownPath);
        }

        private static IEnumerator RunScenario(ManualAimProjectileDiagnosticsReport report, ScenarioSpec spec)
        {
            PlayerAimShotTelemetry.Reset();
            var root = new GameObject(spec.id);
            var cameraObject = new GameObject("Main Camera");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                var presentation = root.AddComponent<PlatformPresentationRoot>();
                presentation.Configure(HollowPlatformKind.VisionOSBoundedTabletop);
                root.transform.localRotation = Quaternion.Euler(0f, spec.rootYawDegrees, 0f);
                root.transform.localScale = Vector3.one * spec.rootScale;

                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 8f;
                camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
                camera.transform.position = new Vector3(0f, 12f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var room = root.AddComponent<RoomRuntimeRoot>();
                room.ConfigureDefault();
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("Player");
                player.transform.SetParent(root.transform, false);
                var aim = player.AddComponent<PlayerAimLockController>();
                aim.Configure(combat);
                var weapon = player.AddComponent<PlayerWeaponController>();
                projectilePrefab.AddComponent<ProjectileController>();
                projectilePrefab.transform.SetParent(root.transform, false);
                weapon.Configure(room, combat, projectilePrefab);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);

                if (spec.patternKind != ProjectilePatternKind.Single)
                {
                    weapon.ConfigureProjectilePassives(new ProjectilePassiveState(spec.patternKind, 1f, 0f, ProjectileVisualStyle.Default));
                }

                if (spec.spawnEnemies)
                {
                    SpawnDiagnosticEnemy(combat, root.transform, new Vector3(0f, 0f, 4f), "DiagnosticTurret");
                    SpawnDiagnosticEnemy(combat, root.transform, new Vector3(2.5f, 0f, 3.5f), "DiagnosticWaveEnemy");
                }

                var fireTimeSeconds = 10f;
                if (spec.inputKind == DiagnosticInputKind.BodyFacing)
                {
                    weapon.TickInput(
                        Snapshot(spec.expectedDirection, Vector2.zero, lightAttackPressed: false),
                        0.016f,
                        fireTimeSeconds - 0.05f);
                }

                var fireInput = BuildFireInput(spec, camera, root.transform);
                weapon.TickInput(fireInput, 0.016f, fireTimeSeconds);
                weapon.TickAction(0f, fireTimeSeconds + WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);

                var projectiles = CollectPlayerProjectiles(root);
                if (projectiles.Count == 0)
                {
                    report.scenarios.Add(BuildNoProjectileScenarioResult(spec, aim, weapon));
                    yield break;
                }

                yield return null;
                var startPositions = projectiles.ToDictionary(projectile => projectile, projectile => projectile.transform.localPosition);
                var startWorldPositions = projectiles.ToDictionary(projectile => projectile, projectile => projectile.transform.position);
                var startSampleTime = Time.time;
                for (var frame = 0; frame < 30; frame++)
                {
                    yield return null;
                }

                var elapsedSeconds = Mathf.Max(0.0001f, Time.time - startSampleTime);
                var scenario = BuildScenarioResult(spec, aim, weapon, projectiles, startPositions, startWorldPositions, elapsedSeconds);
                report.scenarios.Add(scenario);
            }
            finally
            {
                Object.Destroy(projectilePrefab);
                Object.Destroy(cameraObject);
                Object.Destroy(root);
            }
        }

        private static GameplayInputSnapshot BuildFireInput(ScenarioSpec spec, Camera camera, Transform gameplayRoot)
        {
            if (spec.inputKind == DiagnosticInputKind.MouseCursor)
            {
                var targetLocal = new Vector3(spec.expectedDirection.x, 0f, spec.expectedDirection.y) * 4f;
                var pointer = camera.WorldToScreenPoint(gameplayRoot.TransformPoint(targetLocal));
                return SnapshotWithPointer(
                    spec.moveWhileShooting ? new Vector2(-0.35f, 0.15f) : Vector2.zero,
                    new Vector2(pointer.x, pointer.y),
                    lightAttackPressed: true);
            }

            var move = spec.moveWhileShooting
                ? new Vector2(-0.35f, 0.15f)
                : spec.inputKind == DiagnosticInputKind.BodyFacing
                    ? Vector2.zero
                    : Vector2.zero;
            var shoot = spec.inputKind == DiagnosticInputKind.RightStick ? spec.expectedDirection : Vector2.zero;
            return Snapshot(move, shoot, lightAttackPressed: true);
        }

        private static ManualAimScenarioResult BuildNoProjectileScenarioResult(
            ScenarioSpec spec,
            PlayerAimLockController aim,
            PlayerWeaponController weapon)
        {
            return new ManualAimScenarioResult
            {
                id = spec.id,
                inputKind = spec.inputKind.ToString(),
                projectilePattern = spec.patternKind.ToString(),
                weaponInputSection = "No projectile spawned from deterministic weapon input.",
                projectilePhysicsSection = "Not measured.",
                requestedAim = FormatVector2(spec.expectedDirection),
                resolvedAim = FormatVector2(aim.AttackDirection),
                attackDirection = FormatVector2(weapon.VisualAimDirection),
                outcome = "NoProjectileSpawned",
                pathLocalAngleErrorDegrees = 999f,
                configuredLocalAngleErrorDegrees = 999f,
                worldForwardAngleErrorDegrees = 999f,
                speedErrorMetersPerSecond = 999f,
                passed = false,
                summary = $"{spec.id}: no projectile spawned; resolved={FormatVector2(aim.AttackDirection)} attack={FormatVector2(weapon.VisualAimDirection)}"
            };
        }

        private static ManualAimScenarioResult BuildScenarioResult(
            ScenarioSpec spec,
            PlayerAimLockController aim,
            PlayerWeaponController weapon,
            List<ProjectileController> projectiles,
            Dictionary<ProjectileController, Vector3> startPositions,
            Dictionary<ProjectileController, Vector3> startWorldPositions,
            float elapsedSeconds)
        {
            var result = new ManualAimScenarioResult
            {
                id = spec.id,
                inputKind = spec.inputKind.ToString(),
                projectilePattern = spec.patternKind.ToString(),
                weaponInputSection = "PlayerWeaponController.TickInput consumed a deterministic GameplayInputSnapshot.",
                projectilePhysicsSection = "Measured local path, configured local direction, parent-transformed world forward, and speed.",
                requestedAim = FormatVector2(spec.expectedDirection),
                resolvedAim = FormatVector2(aim.AttackDirection),
                attackDirection = FormatVector2(weapon.VisualAimDirection),
                telemetryAimSource = PlayerAimShotTelemetry.LastShot.AimSource.ToString(),
                telemetryDirection = FormatVector2(PlayerAimShotTelemetry.LastShot.AimDirection),
                telemetrySpeed = PlayerAimShotTelemetry.LastShot.ProjectileSpeedMetersPerSecond,
                lockedShotCount = PlayerAimShotTelemetry.LockedShotCount,
                outcome = "Measured"
            };

            var allowedOffsets = AllowedSpreadOffsets(spec.patternKind);
            foreach (var projectile in projectiles)
            {
                var start = startPositions.TryGetValue(projectile, out var startPosition)
                    ? startPosition
                    : projectile.transform.localPosition;
                var end = projectile.transform.localPosition;
                var pathDelta = end - start;
                var worldStart = startWorldPositions.TryGetValue(projectile, out var startWorldPosition)
                    ? startWorldPosition
                    : projectile.transform.position;
                var worldEnd = projectile.transform.position;
                var worldPathDelta = worldEnd - worldStart;
                var localPathDirection = ToPlanarDirection(pathDelta);
                var worldPathDirection = ToPlanarDirection(worldPathDelta);
                var configuredLocalDirection = ToPlanarDirection(projectile.ConfiguredLocalDirection);
                var expectedWorldVelocity = projectile.transform.parent != null
                    ? projectile.transform.parent.TransformVector(projectile.ConfiguredLocalDirection * projectile.ConfiguredSpeedMetersPerSecond)
                    : projectile.ConfiguredLocalDirection * projectile.ConfiguredSpeedMetersPerSecond;
                var expectedWorldDirection = ToPlanarDirection(expectedWorldVelocity);
                var worldForwardDirection = ToPlanarDirection(projectile.transform.forward);
                var expectedWorldForwardDirection = ToPlanarDirection(
                    projectile.transform.parent != null
                        ? projectile.transform.parent.TransformDirection(projectile.ConfiguredLocalDirection)
                        : projectile.ConfiguredLocalDirection);
                var measuredSpeed = elapsedSeconds > 0.0001f ? pathDelta.magnitude / elapsedSeconds : 0f;
                var measuredWorldSpeed = elapsedSeconds > 0.0001f ? worldPathDelta.magnitude / elapsedSeconds : 0f;
                var expectedWorldSpeed = expectedWorldVelocity.magnitude;

                result.shots.Add(new ManualAimShotResult
                {
                    expectedLocalDirection = FormatVector2(spec.expectedDirection),
                    configuredLocalDirection = FormatVector2(configuredLocalDirection),
                    localPathDirection = FormatVector2(localPathDirection),
                    worldPathDirection = FormatVector2(worldPathDirection),
                    expectedWorldDirection = FormatVector2(expectedWorldDirection),
                    worldForwardDirection = FormatVector2(worldForwardDirection),
                    pathDelta = FormatVector3(pathDelta),
                    worldPathDelta = FormatVector3(worldPathDelta),
                    measuredSpeed = measuredSpeed,
                    measuredWorldSpeed = measuredWorldSpeed,
                    configuredSpeed = projectile.ConfiguredSpeedMetersPerSecond,
                    expectedWorldSpeed = expectedWorldSpeed,
                    pathLocalAngleErrorDegrees = SpreadOffsetErrorDegrees(spec.expectedDirection, localPathDirection, allowedOffsets),
                    configuredLocalAngleErrorDegrees = SpreadOffsetErrorDegrees(spec.expectedDirection, configuredLocalDirection, allowedOffsets),
                    worldPathAngleErrorDegrees = Vector2.Angle(expectedWorldDirection, worldPathDirection),
                    worldForwardAngleErrorDegrees = Vector2.Angle(expectedWorldForwardDirection, worldForwardDirection),
                    speedErrorMetersPerSecond = Mathf.Abs(measuredSpeed - projectile.ConfiguredSpeedMetersPerSecond),
                    worldSpeedErrorMetersPerSecond = Mathf.Abs(measuredWorldSpeed - expectedWorldSpeed)
                });
            }

            result.pathLocalAngleErrorDegrees = result.shots.Count > 0 ? result.shots.Max(shot => shot.pathLocalAngleErrorDegrees) : 999f;
            result.configuredLocalAngleErrorDegrees = result.shots.Count > 0 ? result.shots.Max(shot => shot.configuredLocalAngleErrorDegrees) : 999f;
            result.worldPathAngleErrorDegrees = result.shots.Count > 0 ? result.shots.Max(shot => shot.worldPathAngleErrorDegrees) : 999f;
            result.worldForwardAngleErrorDegrees = result.shots.Count > 0 ? result.shots.Max(shot => shot.worldForwardAngleErrorDegrees) : 999f;
            result.measuredSpeed = result.shots.Count > 0 ? result.shots.Average(shot => shot.measuredSpeed) : 0f;
            result.measuredWorldSpeed = result.shots.Count > 0 ? result.shots.Average(shot => shot.measuredWorldSpeed) : 0f;
            result.expectedWorldSpeed = result.shots.Count > 0 ? result.shots.Average(shot => shot.expectedWorldSpeed) : 0f;
            result.speedErrorMetersPerSecond = result.shots.Count > 0 ? result.shots.Max(shot => shot.speedErrorMetersPerSecond) : 999f;
            result.worldSpeedErrorMetersPerSecond = result.shots.Count > 0 ? result.shots.Max(shot => shot.worldSpeedErrorMetersPerSecond) : 999f;
            result.passed =
                result.pathLocalAngleErrorDegrees <= MaxAngleErrorDegrees &&
                result.configuredLocalAngleErrorDegrees <= MaxAngleErrorDegrees &&
                result.worldPathAngleErrorDegrees <= MaxAngleErrorDegrees &&
                result.worldForwardAngleErrorDegrees <= MaxAngleErrorDegrees &&
                result.speedErrorMetersPerSecond <= MaxSpeedErrorMetersPerSecond &&
                result.worldSpeedErrorMetersPerSecond <= MaxSpeedErrorMetersPerSecond &&
                result.lockedShotCount == 0 &&
                result.telemetryAimSource != PlayerShotAimSource.AutoLock.ToString() &&
                result.telemetryAimSource != PlayerShotAimSource.ManualLock.ToString();
            result.summary =
                $"{result.id}: localPath={result.pathLocalAngleErrorDegrees:0.00}deg localConfigured={result.configuredLocalAngleErrorDegrees:0.00}deg worldPath={result.worldPathAngleErrorDegrees:0.00}deg worldForward={result.worldForwardAngleErrorDegrees:0.00}deg localSpeed={result.measuredSpeed:0.00}m/s worldSpeed={result.measuredWorldSpeed:0.00}/{result.expectedWorldSpeed:0.00}m/s aim={result.telemetryAimSource} passed={result.passed}";
            return result;
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

        private static float[] AllowedSpreadOffsets(ProjectilePatternKind patternKind)
        {
            return patternKind switch
            {
                ProjectilePatternKind.TripleShot => new[] { -30f, 0f, 30f },
                ProjectilePatternKind.QuadShot => new[] { -30f, 0f, 30f },
                _ => new[] { 0f }
            };
        }

        private static float SpreadOffsetErrorDegrees(Vector2 baseDirection, Vector2 actualDirection, float[] allowedOffsets)
        {
            if (actualDirection.sqrMagnitude <= 0.0001f)
            {
                return 999f;
            }

            var signedOffset = Vector2.SignedAngle(baseDirection.normalized, actualDirection.normalized);
            var best = 999f;
            foreach (var allowedOffset in allowedOffsets)
            {
                best = Mathf.Min(best, Mathf.Abs(Mathf.DeltaAngle(allowedOffset, signedOffset)));
            }

            return best;
        }

        private static Vector2 ToPlanarDirection(Vector3 value)
        {
            var direction = new Vector2(value.x, value.z);
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
        }

        private static void WriteReport(ManualAimProjectileDiagnosticsReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, JsonUtility.ToJson(report, true));

            var markdown = new StringBuilder();
            markdown.AppendLine("# Manual Aim Projectile Diagnostics");
            markdown.AppendLine();
            markdown.AppendLine($"Generated UTC: `{report.generatedUtc}`");
            markdown.AppendLine($"Passed: `{report.passed}`");
            markdown.AppendLine();
            markdown.AppendLine("## InputReader");
            markdown.AppendLine(report.inputReaderSection);
            markdown.AppendLine();
            markdown.AppendLine("## WeaponInput");
            markdown.AppendLine("Scenarios use `PlayerWeaponController.TickInput(...)` with deterministic `GameplayInputSnapshot` values.");
            markdown.AppendLine();
            markdown.AppendLine("## ProjectilePhysics");
            markdown.AppendLine("| Scenario | Input | Pattern | Local Path | Local Config | World Path | World Forward | Local Speed | World Speed | Telemetry | Locked Shots | Passed |");
            markdown.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---|---:|---|");
            foreach (var scenario in report.scenarios)
            {
                markdown.AppendLine(
                    $"| {scenario.id} | {scenario.inputKind} | {scenario.projectilePattern} | {scenario.pathLocalAngleErrorDegrees:0.00} | {scenario.configuredLocalAngleErrorDegrees:0.00} | {scenario.worldPathAngleErrorDegrees:0.00} | {scenario.worldForwardAngleErrorDegrees:0.00} | {scenario.measuredSpeed:0.00} | {scenario.measuredWorldSpeed:0.00}/{scenario.expectedWorldSpeed:0.00} | {scenario.telemetryAimSource} | {scenario.lockedShotCount} | {scenario.passed} |");
            }

            markdown.AppendLine();
            markdown.AppendLine("## Details");
            foreach (var scenario in report.scenarios)
            {
                markdown.AppendLine();
                markdown.AppendLine($"### {scenario.id}");
                markdown.AppendLine($"- Summary: {scenario.summary}");
                markdown.AppendLine($"- Weapon input: {scenario.weaponInputSection}");
                markdown.AppendLine($"- Projectile physics: {scenario.projectilePhysicsSection}");
                markdown.AppendLine($"- Requested aim: `{scenario.requestedAim}`");
                markdown.AppendLine($"- Resolved aim: `{scenario.resolvedAim}`");
                markdown.AppendLine($"- Attack direction: `{scenario.attackDirection}`");
                markdown.AppendLine($"- Outcome: `{scenario.outcome}`");
                markdown.AppendLine($"- Telemetry direction: `{scenario.telemetryDirection}`");
                markdown.AppendLine($"- Telemetry speed: `{scenario.telemetrySpeed:0.00}`");
                foreach (var shot in scenario.shots)
                {
                    markdown.AppendLine($"- Shot: localPath `{shot.localPathDirection}`, configuredLocal `{shot.configuredLocalDirection}`, worldPath `{shot.worldPathDirection}`, worldForward `{shot.worldForwardDirection}`, expectedWorld `{shot.expectedWorldDirection}`, localSpeed `{shot.measuredSpeed:0.00}`, worldSpeed `{shot.measuredWorldSpeed:0.00}/{shot.expectedWorldSpeed:0.00}`");
                }
            }

            File.WriteAllText(MarkdownPath, markdown.ToString());
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

        private static GameplayInputSnapshot Snapshot(Vector2 move, Vector2 shoot, bool lightAttackPressed)
        {
            return new GameplayInputSnapshot(
                move,
                shoot,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: lightAttackPressed,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: false,
                pausePressed: false,
                rollPressed: false,
                lockTargetPressed: false);
        }

        private static GameplayInputSnapshot SnapshotWithPointer(Vector2 move, Vector2 pointerScreenPosition, bool lightAttackPressed)
        {
            return new GameplayInputSnapshot(
                move,
                Vector2.zero,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: lightAttackPressed,
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

        private static string FormatVector2(Vector2 value)
        {
            return $"{value.x:0.000},{value.y:0.000}";
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"{value.x:0.000},{value.y:0.000},{value.z:0.000}";
        }

        [Serializable]
        private sealed class ManualAimProjectileDiagnosticsReport
        {
            public string generatedUtc;
            public bool passed;
            public string inputReaderSection;
            public float maxAngleErrorDegrees;
            public float maxSpeedErrorMetersPerSecond;
            public List<ManualAimScenarioResult> scenarios = new();
        }

        [Serializable]
        private sealed class ManualAimScenarioResult
        {
            public string id;
            public string inputKind;
            public string projectilePattern;
            public bool passed;
            public string summary;
            public string weaponInputSection;
            public string projectilePhysicsSection;
            public string requestedAim;
            public string resolvedAim;
            public string attackDirection;
            public string outcome;
            public string telemetryAimSource;
            public string telemetryDirection;
            public float telemetrySpeed;
            public int lockedShotCount;
            public float measuredSpeed;
            public float measuredWorldSpeed;
            public float expectedWorldSpeed;
            public float speedErrorMetersPerSecond;
            public float worldSpeedErrorMetersPerSecond;
            public float pathLocalAngleErrorDegrees;
            public float configuredLocalAngleErrorDegrees;
            public float worldPathAngleErrorDegrees;
            public float worldForwardAngleErrorDegrees;
            public List<ManualAimShotResult> shots = new();
        }

        [Serializable]
        private sealed class ManualAimShotResult
        {
            public string expectedLocalDirection;
            public string configuredLocalDirection;
            public string localPathDirection;
            public string worldPathDirection;
            public string expectedWorldDirection;
            public string worldForwardDirection;
            public string pathDelta;
            public string worldPathDelta;
            public float measuredSpeed;
            public float measuredWorldSpeed;
            public float configuredSpeed;
            public float expectedWorldSpeed;
            public float pathLocalAngleErrorDegrees;
            public float configuredLocalAngleErrorDegrees;
            public float worldPathAngleErrorDegrees;
            public float worldForwardAngleErrorDegrees;
            public float speedErrorMetersPerSecond;
            public float worldSpeedErrorMetersPerSecond;
        }

        private readonly struct ScenarioSpec
        {
            private ScenarioSpec(
                string id,
                DiagnosticInputKind inputKind,
                Vector2 expectedDirection,
                ProjectilePatternKind patternKind = ProjectilePatternKind.Single,
                float rootYawDegrees = 0f,
                float rootScale = 1f,
                bool moveWhileShooting = false,
                bool spawnEnemies = false)
            {
                this.id = id;
                this.inputKind = inputKind;
                this.expectedDirection = expectedDirection.normalized;
                this.patternKind = patternKind;
                this.rootYawDegrees = rootYawDegrees;
                this.rootScale = rootScale;
                this.moveWhileShooting = moveWhileShooting;
                this.spawnEnemies = spawnEnemies;
            }

            public readonly string id;
            public readonly DiagnosticInputKind inputKind;
            public readonly Vector2 expectedDirection;
            public readonly ProjectilePatternKind patternKind;
            public readonly float rootYawDegrees;
            public readonly float rootScale;
            public readonly bool moveWhileShooting;
            public readonly bool spawnEnemies;

            public static ScenarioSpec BodyFacing() => new("body_facing_east", DiagnosticInputKind.BodyFacing, Vector2.right);

            public static ScenarioSpec RightStick() => new("right_stick_diagonal", DiagnosticInputKind.RightStick, new Vector2(0.7f, 0.35f));

            public static ScenarioSpec MouseCursor() => new("mouse_cursor_north_east", DiagnosticInputKind.MouseCursor, new Vector2(0.35f, 0.9f));

            public static ScenarioSpec MovingPlayerRightStick() => new("moving_player_right_stick", DiagnosticInputKind.RightStick, new Vector2(-0.8f, 0.2f), moveWhileShooting: true);

            public static ScenarioSpec RotatedRootMouseCursor() => new("rotated_root_mouse_cursor", DiagnosticInputKind.MouseCursor, new Vector2(0.55f, 0.75f), rootYawDegrees: 45f);

            public static ScenarioSpec ScaledBoundedRootRightStick() => new("scaled_bounded_root_right_stick", DiagnosticInputKind.RightStick, new Vector2(-0.25f, 0.95f), rootScale: 0.5f);

            public static ScenarioSpec WaveTurretLayoutRightStick() => new("wave_turret_layout_right_stick", DiagnosticInputKind.RightStick, new Vector2(0.2f, 0.98f), spawnEnemies: true);

            public static ScenarioSpec TripleShotSpread() => new("triple_shot_spread", DiagnosticInputKind.RightStick, Vector2.up, ProjectilePatternKind.TripleShot);
        }

        private enum DiagnosticInputKind
        {
            BodyFacing,
            RightStick,
            MouseCursor
        }
    }
}
