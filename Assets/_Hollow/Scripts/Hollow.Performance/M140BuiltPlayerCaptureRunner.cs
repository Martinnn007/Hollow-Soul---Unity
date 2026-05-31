using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core.Diagnostics;
using Hollow.Diagnostics;
using Hollow.Entities;
using Hollow.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hollow.Performance
{
    public sealed class M140BuiltPlayerCaptureRunner : MonoBehaviour
    {
        private const string CaptureMode = "m140-built-player";
        private const float GenericWarmupSeconds = 0.5f;
        private const float GenericSampleSeconds = 2f;
        private static bool startedFromCommandLine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFromCommandLine()
        {
            if (startedFromCommandLine)
            {
                return;
            }

            if (!M140BuiltPlayerCaptureOptions.TryParse(Environment.GetCommandLineArgs(), out var options))
            {
                return;
            }

            startedFromCommandLine = true;
            var host = new GameObject("M140BuiltPlayerCaptureRunner");
            DontDestroyOnLoad(host);
            host.AddComponent<M140BuiltPlayerCaptureRunner>().StartCoroutine(RunAndExit(options));
        }

        public static IEnumerator RunAllScenarios(
            M140BuiltPlayerCaptureOptions options,
            Action<M140BuildRealReport> onComplete = null,
            Action<M140ScenarioSummary> onScenarioComplete = null)
        {
            var host = new GameObject("M140BuiltPlayerCaptureRunner.Manual");
            var runner = host.AddComponent<M140BuiltPlayerCaptureRunner>();
            M140BuildRealReport report = null;
            yield return runner.Run(options, next => report = next, onScenarioComplete);
            Object.Destroy(host);
            onComplete?.Invoke(report);
        }

        private static IEnumerator RunAndExit(M140BuiltPlayerCaptureOptions options)
        {
            var runner = FindAnyObjectByType<M140BuiltPlayerCaptureRunner>();
            M140BuildRealReport report = null;
            yield return runner.Run(options, next => report = next);

            if (options.autoExit)
            {
#if !UNITY_EDITOR
                Application.Quit(report != null && report.passed ? 0 : 1);
#endif
            }
        }

        private IEnumerator Run(
            M140BuiltPlayerCaptureOptions options,
            Action<M140BuildRealReport> onComplete,
            Action<M140ScenarioSummary> onScenarioComplete = null)
        {
            options ??= M140BuiltPlayerCaptureOptions.Development();
            Directory.CreateDirectory(options.ResolvedOutputRoot);
            var summaries = new List<M140ScenarioSummary>();
            var fpsOverride = new M136CaptureFpsOverride(true, options.targetFrameRate);
            M140RenderRuntimeSnapshot renderRuntime = null;
            try
            {
                yield return WaitForBootSettle();
                var scenarioIds = options.scenarioIds ?? Array.Empty<string>();
                for (var index = 0; index < scenarioIds.Length; index++)
                {
                    var scenarioId = scenarioIds[index];
                    M140ScenarioSummary summary = null;
                    yield return RunScenario(scenarioId, options, next => summary = next);
                    if (summary != null)
                    {
                        summaries.Add(summary);
                        onScenarioComplete?.Invoke(summary);
                    }
                }

                var operations = M136PerformanceOperationCounters.Snapshot();
                renderRuntime = M140RenderRuntimeSnapshot.Capture(operations);
            }
            finally
            {
                fpsOverride.Dispose();
            }

            var playerLog = M140PlayerLogValidator.Validate(ResolvePlayerLogPath());
            var report = M140BuildRealReportGenerator.BuildReport(
                options.platformId,
                options.buildKind,
                options.ResolvedOutputRoot,
                summaries,
                renderRuntime,
                playerLog);
            if (options.writeReports)
            {
                M140BuildRealReportGenerator.WriteReport(report, options.JsonReportPath, options.MarkdownReportPath);
            }

            onComplete?.Invoke(report);
        }

        private static IEnumerator WaitForBootSettle()
        {
            var deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline)
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && !string.Equals(activeScene.name, "Boot", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                yield return null;
            }

            yield return null;
        }

        private IEnumerator RunScenario(string scenarioId, M140BuiltPlayerCaptureOptions options, Action<M140ScenarioSummary> onComplete)
        {
            scenarioId = scenarioId ?? string.Empty;
            switch (scenarioId)
            {
                case "boot_loading_screen":
                    yield return RunBootScenario(options, onComplete);
                    break;
                case "enemy_stress_30":
                case "projectile_heavy_room":
                case "boss_plus_adds":
                    yield return RunM138Scenario(scenarioId, options, onComplete);
                    break;
                case "long_run_smoke":
                    yield return RunM139Scenario(options, onComplete);
                    break;
                case "branch_entry_loading":
                case "normal_traversal":
                case "return_to_previous_room":
                case "reward_room":
                case "boss_entry":
                case "next_branch_entry":
                    yield return RunBranchBackedScenario(scenarioId, options, onComplete);
                    break;
                default:
                    yield return RunGenericRuntimeScenario(scenarioId, DisplayNameFor(scenarioId), options, onComplete);
                    break;
            }
        }

        private IEnumerator RunBootScenario(M140BuiltPlayerCaptureOptions options, Action<M140ScenarioSummary> onComplete)
        {
            yield return new WaitForEndOfFrame();
            var screenshot = CaptureScreenshot("boot_loading_screen", options);
            var visual = M140VisualScreenshotValidator.Validate(screenshot, "boot_loading_screen");
            var operations = M136PerformanceOperationCounters.Snapshot();
            onComplete?.Invoke(M140BuildRealReportGenerator.BuildBootScenarioSummary(
                options.platformId,
                options.buildKind,
                operations,
                visual,
                options.enforceTiming));
        }

        private IEnumerator RunGenericRuntimeScenario(
            string scenarioId,
            string displayName,
            M140BuiltPlayerCaptureOptions options,
            Action<M140ScenarioSummary> onComplete)
        {
            var scenario = new M136PerformanceScenarioDefinition
            {
                id = scenarioId,
                displayName = displayName,
                intent = "M140 built-player runtime smoke capture.",
                warmupSeconds = GenericWarmupSeconds,
                sampleSeconds = GenericSampleSeconds
            };
            using var session = new M136LivePerformanceCaptureSession(scenario, CaptureMode);
            if (!session.Begin())
            {
                var failed = new M140ScenarioSummary
                {
                    scenarioId = scenarioId,
                    displayName = displayName,
                    platformId = options.platformId,
                    buildKind = options.buildKind,
                    passed = false,
                    failures = new[] { "M136 telemetry is unavailable in this built-player runtime." }
                };
                onComplete?.Invoke(failed);
                yield break;
            }

            while (!session.IsComplete)
            {
                session.Tick(Mathf.Max(Time.unscaledDeltaTime, 1f / Mathf.Max(1, options.targetFrameRate)), CollectObjectSnapshot());
                yield return null;
            }

            yield return new WaitForEndOfFrame();
            var screenshot = CaptureScreenshot(scenarioId, options);
            var result = session.BuildResult(
                options.ResolvedOutputRoot,
                manifestPath: options.JsonReportPath,
                rawSampleCsvPath: string.Empty,
                profilerTracePath: string.Empty,
                profilerTraceSupported: false,
                profilerTraceNote: "M140 built-player generic scenario does not request profiler traces.",
                note: $"M140 built-player generic capture for {scenarioId}.",
                profilerTraceRequested: false,
                fpsOverrideApplied: true,
                fpsOverrideTarget: options.targetFrameRate,
                samplingSource: M136FrameCadencePolicy.RuntimeUpdateSamplingSource);
            onComplete?.Invoke(M140BuildRealReportGenerator.FromM136Result(
                scenarioId,
                displayName,
                options.platformId,
                options.buildKind,
                result,
                M140VisualScreenshotValidator.Validate(screenshot, scenarioId),
                options.enforceTiming));
        }

        private IEnumerator RunM138Scenario(string scenarioId, M140BuiltPlayerCaptureOptions options, Action<M140ScenarioSummary> onComplete)
        {
            var scenario = M138CombatScaleStressScenarioPolicy.StressManifest.FirstOrDefault(item => string.Equals(item.id, scenarioId, StringComparison.Ordinal));
            if (scenario == null)
            {
                onComplete?.Invoke(new M140ScenarioSummary
                {
                    scenarioId = scenarioId,
                    displayName = DisplayNameFor(scenarioId),
                    platformId = options.platformId,
                    buildKind = options.buildKind,
                    passed = false,
                    failures = new[] { $"M138 scenario `{scenarioId}` is not in the stress manifest." }
                });
                yield break;
            }

            var jsonPath = Path.Combine(options.ResolvedOutputRoot, $"{scenarioId}_m138.json");
            var markdownPath = Path.Combine(options.ResolvedOutputRoot, $"{scenarioId}_m138.md");
            var stressOptions = options.IsDevelopment
                ? M138CombatScaleStressRunOptions.FullGate(jsonPath, markdownPath)
                : M138CombatScaleStressRunOptions.SmokeGate(jsonPath, markdownPath);
            stressOptions.writeReports = false;
            stressOptions.targetFrameRate = options.targetFrameRate;
            stressOptions.enforceFrameTimingWhenTrusted = options.enforceTiming && options.IsDevelopment;

            M138CombatScaleStressScenarioSummary stressSummary = null;
            CapturedScenarioEvidence evidence = null;
            yield return M138CombatScaleStressRunner.RunScenario(
                scenario,
                stressOptions,
                summary => stressSummary = summary,
                () => CaptureScenarioEvidence(scenarioId, options, next => evidence = next));
            var screenshot = !string.IsNullOrWhiteSpace(evidence?.screenshotPath)
                ? evidence.screenshotPath
                : CaptureScreenshot(scenarioId, options);
            onComplete?.Invoke(M140BuildRealReportGenerator.FromM138Summary(
                stressSummary,
                options.platformId,
                options.buildKind,
                M140VisualScreenshotValidator.Validate(screenshot, scenarioId),
                options.enforceTiming && options.IsDevelopment,
                evidence?.objectSnapshot));
        }

        private IEnumerator RunM139Scenario(M140BuiltPlayerCaptureOptions options, Action<M140ScenarioSummary> onComplete)
        {
            var jsonPath = Path.Combine(options.ResolvedOutputRoot, "long_run_smoke_m139.json");
            var markdownPath = Path.Combine(options.ResolvedOutputRoot, "long_run_smoke_m139.md");
            M139LongRunSoakReport soakReport = null;
            CapturedScenarioEvidence evidence = null;
            yield return M139LongRunSoakRunner.RunAllScenarios(
                M139LongRunSoakOptions.SmokeGate(jsonPath, markdownPath),
                report => soakReport = report,
                beforeScenarioCleanup: _ =>
                {
                    if (evidence != null)
                    {
                        return null;
                    }

                    return CaptureScenarioEvidence("long_run_smoke", options, next => evidence = next);
                });
            var screenshot = !string.IsNullOrWhiteSpace(evidence?.screenshotPath)
                ? evidence.screenshotPath
                : CaptureScreenshot("long_run_smoke", options);
            onComplete?.Invoke(M140BuildRealReportGenerator.FromM139Report(
                soakReport,
                options.platformId,
                options.buildKind,
                M140VisualScreenshotValidator.Validate(screenshot, "long_run_smoke"),
                evidence?.objectSnapshot));
        }

        private IEnumerator RunBranchBackedScenario(string scenarioId, M140BuiltPlayerCaptureOptions options, Action<M140ScenarioSummary> onComplete)
        {
            var displayName = DisplayNameFor(scenarioId);
            var includeNextBranch = string.Equals(scenarioId, "next_branch_entry", StringComparison.Ordinal);
            var includeBoss = string.Equals(scenarioId, "boss_entry", StringComparison.Ordinal);
            var branchScenario = new M139LongRunSoakRunner.M139SoakScenarioDefinition(
                scenarioId,
                displayName,
                includeBossRoom: includeBoss,
                includeNextBranch: includeNextBranch);
            var jsonPath = Path.Combine(options.ResolvedOutputRoot, $"{scenarioId}_m139_slice.json");
            var markdownPath = Path.Combine(options.ResolvedOutputRoot, $"{scenarioId}_m139_slice.md");
            var soakOptions = M139LongRunSoakOptions.SmokeGate(jsonPath, markdownPath);
            soakOptions.writeReports = false;
            soakOptions.traversalsPerBranch = scenarioId == "return_to_previous_room" ? 2 : 1;
            soakOptions.targetFrameRate = options.targetFrameRate;
            soakOptions.enforceTiming = false;

            M139LongRunSoakScenarioSummary soakSummary = null;
            CapturedScenarioEvidence evidence = null;
            yield return M139LongRunSoakRunner.RunScenario(
                branchScenario,
                soakOptions,
                summary => soakSummary = summary,
                () => CaptureScenarioEvidence(scenarioId, options, next => evidence = next));
            var screenshot = !string.IsNullOrWhiteSpace(evidence?.screenshotPath)
                ? evidence.screenshotPath
                : CaptureScreenshot(scenarioId, options);
            onComplete?.Invoke(M140BuildRealReportGenerator.FromM139ScenarioSummary(
                scenarioId,
                displayName,
                soakSummary,
                options.platformId,
                options.buildKind,
                M140VisualScreenshotValidator.Validate(screenshot, scenarioId),
                evidence?.objectSnapshot));
        }

        private IEnumerator CaptureScenarioEvidence(
            string scenarioId,
            M140BuiltPlayerCaptureOptions options,
            Action<CapturedScenarioEvidence> onCaptured)
        {
            ScenarioCanvasScope canvasScope = null;
            try
            {
                canvasScope = ScenarioCanvasScope.Begin(M140BuildRealReportGenerator.RequiresGameplayEvidence(scenarioId));
                yield return new WaitForEndOfFrame();
                var evidence = new CapturedScenarioEvidence
                {
                    objectSnapshot = CollectObjectSnapshot(),
                    screenshotPath = CaptureScreenshot(scenarioId, options)
                };
                onCaptured?.Invoke(evidence);
            }
            finally
            {
                canvasScope?.Dispose();
            }
        }

        private static string CaptureScreenshot(string scenarioId, M140BuiltPlayerCaptureOptions options)
        {
            var safeId = string.IsNullOrWhiteSpace(scenarioId) ? "scenario" : scenarioId;
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                safeId = safeId.Replace(invalid, '_');
            }

            var path = Path.Combine(options.ResolvedOutputRoot, $"{safeId}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            try
            {
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture != null)
                {
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    Object.Destroy(texture);
                }
            }
            catch
            {
                // In batch/headless test contexts there may be no backbuffer. The validator will report the missing artifact.
            }

            return path;
        }

        private static M136LiveObjectCountSnapshot CollectObjectSnapshot()
        {
            var enemies = Object.FindObjectsByType<EnemyRuntimeController>(FindObjectsInactive.Exclude);
            var playerProjectiles = Object.FindObjectsByType<ProjectileController>(FindObjectsInactive.Exclude);
            var enemyProjectiles = Object.FindObjectsByType<EnemyProjectileController>(FindObjectsInactive.Exclude);
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            var particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude);
            var combat = Object.FindAnyObjectByType<RoomCombatController>(FindObjectsInactive.Exclude);

            var activeEnemies = 0;
            var observedBoss = false;
            for (var index = 0; index < enemies.Length; index++)
            {
                var enemy = enemies[index];
                if (enemy != null && enemy.isActiveAndEnabled && enemy.IsAlive)
                {
                    activeEnemies++;
                    observedBoss |= enemy.BossDefinition != null;
                }
            }

            return new M136LiveObjectCountSnapshot
            {
                activeEnemies = activeEnemies,
                activeProjectiles = playerProjectiles.Length + enemyProjectiles.Length,
                activeVfx = particles.Length,
                activeUiCanvases = canvases.Length,
                activeCameras = Camera.allCamerasCount,
                activeLights = lights.Length,
                activeRenderers = renderers.Length,
                activeParticleSystems = particles.Length,
                observedBranchSession = Object.FindAnyObjectByType<BranchSessionController>(FindObjectsInactive.Exclude) != null,
                observedCombatController = combat != null,
                observedActiveCombat = combat != null && combat.ObjectiveState == RoomObjectiveState.InCombat,
                observedBoss = observedBoss,
                source = "m140-built-player"
            };
        }

        private sealed class CapturedScenarioEvidence
        {
            public string screenshotPath;
            public M136LiveObjectCountSnapshot objectSnapshot;
        }

        private sealed class ScenarioCanvasScope : IDisposable
        {
            private readonly List<Canvas> disabledCanvases = new();

            private ScenarioCanvasScope()
            {
            }

            public static ScenarioCanvasScope Begin(bool disableUiCanvases)
            {
                var scope = new ScenarioCanvasScope();
                if (!disableUiCanvases)
                {
                    return scope;
                }

                var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
                for (var index = 0; index < canvases.Length; index++)
                {
                    var canvas = canvases[index];
                    if (canvas == null || !canvas.enabled || !LooksLikeMenuOverlay(canvas.transform))
                    {
                        continue;
                    }

                    canvas.enabled = false;
                    scope.disabledCanvases.Add(canvas);
                }

                return scope;
            }

            private static bool LooksLikeMenuOverlay(Transform transform)
            {
                while (transform != null)
                {
                    var name = transform.name ?? string.Empty;
                    if (name.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("MenuRoot", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }

                    transform = transform.parent;
                }

                return false;
            }

            public void Dispose()
            {
                for (var index = 0; index < disabledCanvases.Count; index++)
                {
                    var canvas = disabledCanvases[index];
                    if (canvas != null)
                    {
                        canvas.enabled = true;
                    }
                }

                disabledCanvases.Clear();
            }
        }

        private static string DisplayNameFor(string scenarioId)
        {
            return scenarioId switch
            {
                "branch_entry_loading" => "Branch Entry Loading",
                "normal_traversal" => "Normal Traversal",
                "return_to_previous_room" => "Return To Previous Room",
                "reward_room" => "Reward Room",
                "boss_entry" => "Boss Entry",
                "next_branch_entry" => "Next Branch Entry",
                _ => scenarioId ?? string.Empty
            };
        }

        private static string ResolvePlayerLogPath()
        {
            var platform = Application.platform;
            if (platform == RuntimePlatform.OSXPlayer || platform == RuntimePlatform.OSXEditor)
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "Library",
                    "Logs",
                    Application.companyName,
                    Application.productName,
                    "Player.log");
            }

            if (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor)
            {
                var localLow = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).Replace("Local", "LocalLow");
                return Path.Combine(localLow, Application.companyName, Application.productName, "Player.log");
            }

            return string.Empty;
        }
    }
}
