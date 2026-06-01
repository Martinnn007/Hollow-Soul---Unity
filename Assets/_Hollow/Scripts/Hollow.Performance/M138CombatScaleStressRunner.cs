using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using Hollow.Entities;
using Hollow.Rooms;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Performance
{
    public static class M138CombatScaleStressRunner
    {
        private const int RoomWidthTiles = 20;
        private const int RoomDepthTiles = 14;
        private const int ProjectilePressurePoolSize = 32;
        private const float ProjectileSpawnIntervalSeconds = 0.12f;
        private static readonly string[] MeleeSpawnKinds =
        {
            "spawnEnemyNormal",
            "spawnEnemyFast",
            "spawnEnemyHeavy",
            "spawnEnemyCharger",
            "spawnEnemySkeletonSword",
            "spawnEnemySkeletonSpear",
            "spawnEnemyKnight",
            "spawnEnemyRat",
            "spawnEnemySpider",
            "spawnEnemyHollowBeast"
        };

        private static readonly string[] ProjectileSpawnKinds =
        {
            "spawnEnemyHollowArcher",
            "spawnEnemyPowderGunner",
            "spawnEnemyKnifeThrower",
            "spawnEnemyTurret",
            "spawnEnemySpittingPod",
            "spawnEnemyRepeaterTurret",
            "spawnEnemyClockworkSentry"
        };

        public static RoomNavMeshRuntimeFallbackMode StressHarnessNavMeshModeForDiagnostics =>
            RoomNavMeshRuntimeFallbackMode.AutomatedStressHarnessRuntimeBake;

        public static IEnumerator RunAllScenarios(
            M138CombatScaleStressRunOptions options,
            Action<M138CombatScaleStressReport> onComplete = null,
            Action<M138CombatScaleStressScenarioSummary> onScenarioComplete = null)
        {
            options ??= M138CombatScaleStressRunOptions.FullGate();
            var summaries = new List<M138CombatScaleStressScenarioSummary>();
            var fpsOverride = new M136CaptureFpsOverride(true, options.targetFrameRate);
            try
            {
                foreach (var scenario in M138CombatScaleStressScenarioPolicy.StressManifest)
                {
                    M138CombatScaleStressScenarioSummary summary = null;
                    yield return RunScenario(scenario, options, nextSummary => summary = nextSummary);
                    if (summary != null)
                    {
                        summaries.Add(summary);
                        onScenarioComplete?.Invoke(summary);
                    }
                }
            }
            finally
            {
                fpsOverride.Dispose();
            }

            var report = M138CombatScaleStressReportGenerator.BuildReport(summaries);
            if (options.writeReports)
            {
                M138CombatScaleStressReportGenerator.WriteReport(report, options.jsonReportPath, options.markdownReportPath);
            }

            onComplete?.Invoke(report);
        }

        public static IEnumerator RunScenario(
            M138CombatScaleStressScenarioDefinition scenario,
            M138CombatScaleStressRunOptions options,
            Action<M138CombatScaleStressScenarioSummary> onComplete,
            Func<IEnumerator> beforeCleanup = null)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            options ??= M138CombatScaleStressRunOptions.FullGate();
            M136PerformanceOperationCounters.Reset();
            var harness = M138StressHarness.Create(scenario);
            yield return null;

            var m136Scenario = M138CombatScaleStressReportGenerator.ToM136ScenarioDefinition(scenario, options);
            using var session = new M136LivePerformanceCaptureSession(m136Scenario, M138CombatScaleStressReportGenerator.CaptureMode);
            var frameBudget = new M138CombatScaleStressFrameBudgetSummary();
            var bossFullLodObserved = false;
            var reducedOrBackgroundAddObserved = false;
            var previousOperationSnapshot = default(M136PerformanceOperationSnapshot);
            var hasPreviousOperationSnapshot = false;
            var nextProjectileSpawnTime = Time.unscaledTime;

            try
            {
                if (!session.Begin())
                {
                    var failedResult = BuildFailedScenarioResult(m136Scenario, "M136 telemetry is disabled in this runtime.");
                    var failedSummary = M138CombatScaleStressReportGenerator.BuildScenarioSummary(
                        scenario,
                        failedResult,
                        frameBudget,
                        bossFullLodObserved,
                        reducedOrBackgroundAddObserved,
                        options.enforceFrameTimingWhenTrusted);
                    if (beforeCleanup != null)
                    {
                        var cleanupRoutine = beforeCleanup();
                        if (cleanupRoutine != null)
                        {
                            yield return cleanupRoutine;
                        }
                    }

                    onComplete?.Invoke(failedSummary);
                    yield break;
                }

                while (!session.IsComplete)
                {
                    yield return null;

                    var deltaTime = Mathf.Max(Time.unscaledDeltaTime, 1f / Mathf.Max(1, options.targetFrameRate));
                    if (scenario.projectileHeavy)
                    {
                        harness.ProjectilePressure.Tick(Time.unscaledTime);
                        M136PerformanceOperationCounters.ReportProjectileActiveCount(harness.ProjectilePressure.ActiveCount);
                        if (session.IsSampling && Time.unscaledTime >= nextProjectileSpawnTime)
                        {
                            harness.ProjectilePressure.SpawnBurst(Time.unscaledTime, 3);
                            nextProjectileSpawnTime = Time.unscaledTime + ProjectileSpawnIntervalSeconds;
                        }
                    }

                    ObserveLodState(harness.Combat, ref bossFullLodObserved, ref reducedOrBackgroundAddObserved);
                    var snapshot = BuildObjectSnapshot(harness, scenario);
                    var wasSampling = session.IsSampling;
                    session.Tick(deltaTime, snapshot);
                    if (!wasSampling && session.IsSampling)
                    {
                        previousOperationSnapshot = M136PerformanceOperationCounters.Snapshot();
                        hasPreviousOperationSnapshot = true;
                        continue;
                    }

                    if (session.IsSampling)
                    {
                        var current = M136PerformanceOperationCounters.Snapshot();
                        if (hasPreviousOperationSnapshot)
                        {
                            frameBudget.Observe(previousOperationSnapshot, current);
                        }

                        previousOperationSnapshot = current;
                        hasPreviousOperationSnapshot = true;
                    }
                }

                var artifactDirectory = Path.GetDirectoryName(string.IsNullOrWhiteSpace(options.jsonReportPath)
                    ? M138CombatScaleStressReportGenerator.DefaultJsonReportPath
                    : options.jsonReportPath);
                var result = session.BuildResult(
                    artifactDirectory,
                    options.jsonReportPath,
                    rawSampleCsvPath: string.Empty,
                    profilerTracePath: string.Empty,
                    profilerTraceSupported: false,
                    profilerTraceNote: "M138 automated stress runner does not request profiler trace by default.",
                    note: "M138 automated temporary combat stress room.",
                    profilerTraceRequested: false,
                    fpsOverrideApplied: true,
                    fpsOverrideTarget: options.targetFrameRate,
                    samplingSource: M136FrameCadencePolicy.RuntimeUpdateSamplingSource);
                var summary = M138CombatScaleStressReportGenerator.BuildScenarioSummary(
                    scenario,
                    result,
                    frameBudget,
                    bossFullLodObserved,
                    reducedOrBackgroundAddObserved,
                    options.enforceFrameTimingWhenTrusted);
                if (beforeCleanup != null)
                {
                    var cleanupRoutine = beforeCleanup();
                    if (cleanupRoutine != null)
                    {
                        yield return cleanupRoutine;
                    }
                }

                onComplete?.Invoke(summary);
            }
            finally
            {
                harness.Destroy();
            }

            yield return null;
        }

        private static M136PerformanceScenarioResult BuildFailedScenarioResult(M136PerformanceScenarioDefinition scenario, string note)
        {
            return new M136PerformanceScenarioResult
            {
                scenarioId = scenario.id,
                displayName = scenario.displayName,
                captureMode = M138CombatScaleStressReportGenerator.CaptureMode,
                liveCaptured = false,
                requiresManualCapture = false,
                warmupSeconds = scenario.warmupSeconds,
                sampleSeconds = scenario.sampleSeconds,
                rawSampleCount = 0,
                samplingSource = M136FrameCadencePolicy.UnknownSamplingSource,
                frameCadenceConfidence = M136FrameCadencePolicy.Invalid,
                metrics = Array.Empty<M136PerformanceMetricSummary>(),
                operations = new M136RuntimeOperationSummary(),
                objectCounts = new M136LiveObjectCountSummary(),
                note = note
            };
        }

        private static M136LiveObjectCountSnapshot BuildObjectSnapshot(M138StressHarness harness, M138CombatScaleStressScenarioDefinition scenario)
        {
            var activeEnemies = 0;
            var observedBoss = false;
            var enemies = harness.Combat != null ? harness.Combat.Enemies : Array.Empty<EnemyRuntimeController>();
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive)
                {
                    continue;
                }

                activeEnemies++;
                observedBoss |= enemy.BossDefinition != null;
            }

            return new M136LiveObjectCountSnapshot
            {
                activeEnemies = activeEnemies,
                activeProjectiles = harness.ProjectilePressure.ActiveCount,
                activeVfx = 0,
                activeUiCanvases = 0,
                activeCameras = 0,
                activeLights = 0,
                activeRenderers = activeEnemies + harness.ProjectilePressure.ActiveCount + 2,
                activeParticleSystems = 0,
                observedCombatController = harness.Combat != null,
                observedActiveCombat = harness.Combat != null && harness.Combat.ObjectiveState == RoomObjectiveState.InCombat,
                observedBoss = scenario.includesBoss && observedBoss,
                source = "m138-automated-stress-runner"
            };
        }

        private static void ObserveLodState(RoomCombatController combat, ref bool bossFullLodObserved, ref bool reducedOrBackgroundAddObserved)
        {
            if (combat == null)
            {
                return;
            }

            var enemies = combat.Enemies;
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (enemy.BossDefinition != null)
                {
                    bossFullLodObserved |= enemy.CurrentAiLodTier == EnemyAiLodTier.Full;
                    continue;
                }

                reducedOrBackgroundAddObserved |= enemy.CurrentAiLodTier is EnemyAiLodTier.Reduced or EnemyAiLodTier.Background;
            }
        }

        private sealed class M138StressHarness
        {
            private readonly GameObject root;
            private readonly GameObject enemyPrefab;
            private readonly GameObject projectilePrefab;

            private M138StressHarness(
                GameObject root,
                RoomCombatController combat,
                M138ProjectilePressurePool projectilePressure,
                GameObject enemyPrefab,
                GameObject projectilePrefab)
            {
                this.root = root;
                Combat = combat;
                ProjectilePressure = projectilePressure;
                this.enemyPrefab = enemyPrefab;
                this.projectilePrefab = projectilePrefab;
            }

            public RoomCombatController Combat { get; }

            public M138ProjectilePressurePool ProjectilePressure { get; }

            public static M138StressHarness Create(M138CombatScaleStressScenarioDefinition scenario)
            {
                var root = new GameObject($"M138StressHarness.{scenario.id}");
                CreateCaptureCamera(root.transform);
                var roomObject = new GameObject("M138.RoomRuntimeRoot");
                roomObject.transform.SetParent(root.transform, false);
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.ConfigureDefault();
                room.BuildFrom(CreateRoomAsset(scenario), StressHarnessNavMeshModeForDiagnostics);

                var playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObject.name = "M138.Player";
                playerObject.transform.SetParent(root.transform, false);
                playerObject.transform.localPosition = Vector3.zero;
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                player.ConfigureDefault();
                var playerHealth = playerObject.AddComponent<CombatantHealth>();
                playerHealth.Configure(5000);

                var prefabRoot = new GameObject("M138.Prefabs");
                prefabRoot.transform.SetParent(root.transform, false);
                var enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyPrefab.name = "M138.EnemyPrefab";
                enemyPrefab.transform.SetParent(prefabRoot.transform, false);
                enemyPrefab.AddComponent<EnemyRuntimeController>();
                enemyPrefab.SetActive(false);

                var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectilePrefab.name = "M138.EnemyProjectilePrefab";
                projectilePrefab.transform.SetParent(prefabRoot.transform, false);
                projectilePrefab.transform.localScale = Vector3.one * 0.16f;
                projectilePrefab.AddComponent<EnemyProjectileController>();
                projectilePrefab.SetActive(false);

                var combatObject = new GameObject("M138.RoomCombatController");
                combatObject.transform.SetParent(root.transform, false);
                var combat = combatObject.AddComponent<RoomCombatController>();
                var enemyCatalog = EnemyCatalog.CreateRuntimeDefault();
                var difficulty = DifficultyTierDefinition.CreateRuntimeDeveloperSample();
                combat.Configure(enemyPrefab, projectilePrefab, enemyCatalog, difficulty);
                combat.ConfigureBossCatalog(BossCatalogDefinition.CreateRuntimeDefault());
                combat.ConfigureAutoInitialize(false);
                combat.ConfigureInspectionMode(InspectionEntityMode.LiveRuntime, ignoreRoomClear: false);
                combat.BeginRoom(
                    room,
                    player,
                    alreadyCleared: false,
                    RoomCombatEncounterKind.Standard,
                    CreateEncounterContext(scenario));

                if (scenario.includesBoss)
                {
                    var boss = EnemySpawnService.SpawnBoss(
                        room,
                        root.transform,
                        enemyPrefab,
                        projectilePrefab,
                        player,
                        enemyCatalog,
                        difficulty,
                        combat.Diagnostics,
                        BossCatalogDefinition.CreateRuntimeDefault(),
                        new RoomCombatEncounterContext(
                            $"{scenario.id}_boss",
                            new[] { "spawnEnemyBoss" },
                            0,
                            0,
                            0,
                            "stone_warden",
                            "m138_stress_arena",
                            0,
                            string.Empty));
                    combat.RegisterRuntimeEnemy(boss);
                }

                var projectilePressure = new M138ProjectilePressurePool(root.transform, ProjectilePressurePoolSize);
                return new M138StressHarness(root, combat, projectilePressure, enemyPrefab, projectilePrefab);
            }

            private static void CreateCaptureCamera(Transform parent)
            {
                var lightObject = new GameObject("M138.CaptureLight");
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;

                var cameraObject = new GameObject("M138.CaptureCamera");
                cameraObject.transform.SetParent(parent, false);
                cameraObject.transform.position = new Vector3(0f, 18f, -12f);
                cameraObject.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.04f, 0.05f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = 9f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 80f;
                camera.depth = 1000f;
            }

            public void Destroy()
            {
                if (Application.isPlaying)
                {
                    if (root != null)
                    {
                        Object.Destroy(root);
                    }

                    return;
                }

                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                if (enemyPrefab != null)
                {
                    Object.DestroyImmediate(enemyPrefab);
                }

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }
            }
        }

        private sealed class M138ProjectilePressurePool
        {
            private readonly M138ProjectilePressureSlot[] slots;
            private int nextSlot;

            public M138ProjectilePressurePool(Transform parent, int count)
            {
                slots = new M138ProjectilePressureSlot[Mathf.Max(1, count)];
                for (var index = 0; index < slots.Length; index++)
                {
                    var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    projectile.name = $"M138.Projectile.{index:00}";
                    projectile.transform.SetParent(parent, false);
                    projectile.transform.localScale = Vector3.one * 0.14f;
                    projectile.AddComponent<M138StressProjectileMarker>();
                    projectile.SetActive(false);
                    slots[index] = new M138ProjectilePressureSlot(projectile);
                }
            }

            public int ActiveCount
            {
                get
                {
                    var count = 0;
                    for (var index = 0; index < slots.Length; index++)
                    {
                        if (slots[index].Active)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }

            public void Tick(float timeSeconds)
            {
                var start = Time.realtimeSinceStartup;
                var active = 0;
                for (var index = 0; index < slots.Length; index++)
                {
                    if (slots[index].Tick(timeSeconds))
                    {
                        active++;
                    }
                }

                if (active > 0)
                {
                    M136PerformanceOperationCounters.ReportProjectileCollisionCheck(active);
                    M136PerformanceOperationCounters.ReportProjectileUpdate((Time.realtimeSinceStartup - start) * 1000f);
                }
            }

            public void SpawnBurst(float timeSeconds, int count)
            {
                var burstCount = Mathf.Max(1, count);
                for (var index = 0; index < burstCount; index++)
                {
                    var slot = slots[nextSlot];
                    nextSlot = (nextSlot + 1) % slots.Length;
                    var angle = (timeSeconds * 1.7f + index * 2.094f) % (Mathf.PI * 2f);
                    var origin = new Vector3(Mathf.Cos(angle) * 7.5f, 0.55f, Mathf.Sin(angle) * 4.5f);
                    var direction = new Vector3(-Mathf.Cos(angle), 0f, -Mathf.Sin(angle)).normalized;
                    slot.Activate(origin, direction * 7.5f, timeSeconds + 1.25f);
                    M136PerformanceOperationCounters.ReportProjectileSpawn();
                }
            }
        }

        private sealed class M138ProjectilePressureSlot
        {
            private readonly GameObject gameObject;
            private Vector3 velocity;
            private float activeUntil;

            public M138ProjectilePressureSlot(GameObject gameObject)
            {
                this.gameObject = gameObject;
            }

            public bool Active => gameObject != null && gameObject.activeSelf;

            public void Activate(Vector3 position, Vector3 nextVelocity, float nextActiveUntil)
            {
                if (gameObject == null)
                {
                    return;
                }

                velocity = nextVelocity;
                activeUntil = nextActiveUntil;
                gameObject.transform.localPosition = position;
                gameObject.SetActive(true);
            }

            public bool Tick(float timeSeconds)
            {
                if (!Active)
                {
                    return false;
                }

                if (timeSeconds >= activeUntil)
                {
                    gameObject.SetActive(false);
                    M136PerformanceOperationCounters.ReportProjectileReturn();
                    return false;
                }

                gameObject.transform.localPosition += velocity * Time.unscaledDeltaTime;
                return true;
            }
        }

        private sealed class M138StressProjectileMarker : MonoBehaviour
        {
        }

        private static ImportedRoomRuntimeAsset CreateRoomAsset(M138CombatScaleStressScenarioDefinition scenario)
        {
            var halfWidth = RoomWidthTiles * 0.5f;
            var halfDepth = RoomDepthTiles * 0.5f;
            var layout = new RoomLayout(
                RoomWidthTiles,
                RoomDepthTiles,
                Rect.MinMaxRect(-halfWidth, -halfDepth, halfWidth, halfDepth),
                BuildWalkableTiles(),
                Array.Empty<Vector2Int>(),
                new[]
                {
                    new RoomLayoutFloorRegion("m138_floor", Vector3.zero, new Vector2(halfWidth, halfDepth))
                },
                Array.Empty<RoomLayoutObstacle>());
            var footprint = new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(RoomWidthTiles, RoomDepthTiles));
            return new ImportedRoomRuntimeAsset(
                $"m138_{scenario.id}",
                $"M138 {scenario.displayName}",
                RoomBiomeIds.HollowThreshold,
                layout,
                footprint,
                Array.Empty<RoomDoorPort>(),
                BuildEnemySpawns(scenario),
                Array.Empty<ImportedSpawnPoint>(),
                new ImportedSpawnPoint
                {
                    id = "player_start",
                    kind = "playerStart",
                    position = Vector(0f, 0f, 0f)
                },
                Array.Empty<ImportedRoomHazard>(),
                Array.Empty<ImportedRoomInteractiveObject>(),
                Array.Empty<ImportedRoomDecor>(),
                null);
        }

        private static IReadOnlyList<Vector2Int> BuildWalkableTiles()
        {
            var tiles = new List<Vector2Int>(RoomWidthTiles * RoomDepthTiles);
            var xMin = -RoomWidthTiles / 2;
            var zMin = -RoomDepthTiles / 2;
            for (var x = 0; x < RoomWidthTiles; x++)
            {
                for (var z = 0; z < RoomDepthTiles; z++)
                {
                    tiles.Add(new Vector2Int(xMin + x, zMin + z));
                }
            }

            return tiles;
        }

        private static IReadOnlyList<ImportedSpawnPoint> BuildEnemySpawns(M138CombatScaleStressScenarioDefinition scenario)
        {
            var spawns = new ImportedSpawnPoint[Mathf.Max(0, scenario.targetEnemyCount)];
            for (var index = 0; index < spawns.Length; index++)
            {
                var position = SpawnPosition(index, spawns.Length);
                spawns[index] = new ImportedSpawnPoint
                {
                    id = $"spawn_{index:000}",
                    kind = SpawnKindFor(scenario, index),
                    position = Vector(position.x, 0f, position.z)
                };
            }

            return spawns;
        }

        private static Vector3 SpawnPosition(int index, int count)
        {
            var columns = count >= 24 ? 6 : 5;
            var row = index / columns;
            var column = index % columns;
            var xStep = 15f / Mathf.Max(1, columns - 1);
            var rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            var zStep = 9.5f / Mathf.Max(1, rows - 1);
            var x = -7.5f + column * xStep;
            var z = -4.75f + row * zStep;
            if (Mathf.Abs(x) < 1.25f && Mathf.Abs(z) < 1.25f)
            {
                z += 2.25f;
            }

            return new Vector3(x, 0f, z);
        }

        private static string SpawnKindFor(M138CombatScaleStressScenarioDefinition scenario, int index)
        {
            if (scenario.projectileHeavy && index % 2 == 0)
            {
                return ProjectileSpawnKinds[(index / 2) % ProjectileSpawnKinds.Length];
            }

            return MeleeSpawnKinds[index % MeleeSpawnKinds.Length];
        }

        private static RoomCombatEncounterContext CreateEncounterContext(M138CombatScaleStressScenarioDefinition scenario)
        {
            var spawnKinds = new string[scenario.targetEnemyCount];
            for (var index = 0; index < spawnKinds.Length; index++)
            {
                spawnKinds[index] = SpawnKindFor(scenario, index);
            }

            return new RoomCombatEncounterContext(
                scenario.id,
                spawnKinds,
                worldIndex: 0,
                difficultyBand: 0,
                directorPressure: scenario.targetEnemyCount >= 20 ? 2 : 1);
        }

        private static ImportedVector3 Vector(float x, float y, float z)
        {
            return new ImportedVector3
            {
                x = x,
                y = y,
                z = z
            };
        }
    }
}
