using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Core.Diagnostics;
using Hollow.Diagnostics;
using Hollow.Editor.Generation;
using Hollow.Performance;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Hollow.Editor
{
    public sealed class M136LiveGameplayCaptureWindow : EditorWindow
    {
        private readonly Queue<M136PerformanceScenarioDefinition> automatedQueue = new();
        private M136PerformanceScenarioDefinition[] scenarios = Array.Empty<M136PerformanceScenarioDefinition>();
        private M136LiveCaptureManifest[] latestCaptures = Array.Empty<M136LiveCaptureManifest>();
        private M136LivePerformanceCaptureSession activeSession;
        private M136RuntimeLiveCaptureDriver activeDriver;
        private M136ProfilerTraceRecorder activeTrace;
        private M136CaptureFpsOverride activeFpsOverride;
        private string activeDirectory;
        private bool automatedRunning;
        private bool use60FpsCaptureCap;
        private bool captureProfilerTrace;
        private int selectedScenarioIndex;
        private Vector2 scroll;

        [MenuItem("Hollow/Performance/Live Gameplay Capture")]
        public static void Open()
        {
            GetWindow<M136LiveGameplayCaptureWindow>("Live Performance Capture");
        }

        private void OnEnable()
        {
            scenarios = M136EditorLaptopPerformancePolicy.ScenarioManifest;
            RefreshLatestCaptures();
            EditorApplication.update += TickCapture;
        }

        private void OnDisable()
        {
            EditorApplication.update -= TickCapture;
            activeDriver?.StopCapture();
            DestroyRuntimeDriver();

            activeTrace?.Dispose();
            activeSession?.Dispose();
            activeFpsOverride?.Dispose();
            activeDriver = null;
            activeTrace = null;
            activeSession = null;
            activeFpsOverride = null;
        }

        private void OnGUI()
        {
            if (scenarios.Length == 0)
            {
                scenarios = M136EditorLaptopPerformancePolicy.ScenarioManifest;
            }

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("M136 Live Gameplay Performance Capture", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Enter Play Mode, select a scenario label, then record live gameplay. The capture ignores the fixed 3s warmup and samples the next 30s.", MessageType.Info);

                using (new EditorGUI.DisabledScope(activeSession != null))
                {
                    selectedScenarioIndex = EditorGUILayout.Popup(
                        "Scenario",
                        Mathf.Clamp(selectedScenarioIndex, 0, Math.Max(0, scenarios.Length - 1)),
                        scenarios.Select(scenario => scenario.displayName).ToArray());
                    EditorGUILayout.LabelField("Warmup", $"{M136EditorLaptopPerformancePolicy.FixedWarmupSeconds:0.#}s");
                    EditorGUILayout.LabelField("Sample", $"{M136EditorLaptopPerformancePolicy.FixedSampleSeconds:0.#}s");
                    use60FpsCaptureCap = EditorGUILayout.Toggle("Use 60 FPS capture cap", use60FpsCaptureCap);
                    captureProfilerTrace = EditorGUILayout.Toggle("Capture profiler trace", captureProfilerTrace);
                }

                EditorGUILayout.HelpBox(
                    "M136B samples from a hidden Play Mode MonoBehaviour.Update driver. The 60 FPS cap and profiler trace are capture-scoped toggles only; defaults are restored after export.",
                    MessageType.Info);
                if (!captureProfilerTrace)
                {
                    EditorGUILayout.HelpBox("Profiler trace capture is off by default because previous traces were hundreds of MB. Enable it only for focused spike investigations.", MessageType.None);
                }

                if (!EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox("Live gameplay capture is available in Play Mode. Reports can still be generated from existing captures or the deterministic baseline.", MessageType.Warning);
                }

                DrawActiveCapture();
                DrawCaptureButtons();
                DrawLatestCaptures();
                DrawLatestM138StressSummary();
                DrawLatestM139SoakSummary();
                DrawLatestM140BuildRealSummary();
            }
        }

        private void DrawActiveCapture()
        {
            if (activeSession == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active Capture", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mode", activeSession.CaptureMode);
            EditorGUILayout.LabelField("Scenario", activeSession.Scenario.displayName);
            EditorGUILayout.LabelField("Phase", activeSession.IsSampling ? "Sampling" : "Warmup");
            EditorGUILayout.Slider("Warmup Progress", Mathf.Min(activeSession.ElapsedSeconds, activeSession.Scenario.warmupSeconds), 0f, activeSession.Scenario.warmupSeconds);
            EditorGUILayout.Slider("Sample Progress", activeSession.SampledSeconds, 0f, activeSession.Scenario.sampleSeconds);
            EditorGUILayout.LabelField("Raw Samples", activeSession.Samples.Count.ToString());
            EditorGUILayout.LabelField("Sample Source", M136RuntimeLiveCaptureDriver.SamplingSource);
            EditorGUILayout.LabelField("Sample Rate", $"{activeDriver?.SampleRateHz ?? 0d:0.#} Hz");
            EditorGUILayout.LabelField("FPS Override", activeFpsOverride?.Applied == true ? $"{activeFpsOverride.TargetFrameRate} FPS during capture" : "off");
            EditorGUILayout.LabelField("Trace", captureProfilerTrace ? activeTrace?.Status ?? "starting" : "off by default");
            DrawValidityHints(activeDriver?.LatestSnapshot);
        }

        private void DrawCaptureButtons()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(activeSession != null || !EditorApplication.isPlaying))
                {
                    if (GUILayout.Button("Start Manual Capture"))
                    {
                        StartManualCapture();
                    }

                    if (GUILayout.Button("Run Automated Smoke"))
                    {
                        StartAutomatedSmoke();
                    }
                }

                using (new EditorGUI.DisabledScope(activeSession == null))
                {
                    if (GUILayout.Button("Stop + Export"))
                    {
                        CompleteActiveCapture("Capture stopped manually and exported.");
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Captures"))
                {
                    RefreshLatestCaptures();
                }

                if (GUILayout.Button("Generate M136 Report"))
                {
                    Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.Generate();
                    RefreshLatestCaptures();
                }

                if (GUILayout.Button("Generate M136B Report"))
                {
                    Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.Generate();
                    RefreshLatestCaptures();
                }
            }

            EditorGUILayout.HelpBox("Automated smoke still records scenario labels against the current Play Mode state. Treat it as non-authoritative until automated routing/spawning is added.", MessageType.Warning);
        }

        private void DrawLatestCaptures()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Latest Live Captures", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(140f));
            if (latestCaptures.Length == 0)
            {
                EditorGUILayout.LabelField("No live captures found yet.");
            }
            else
            {
                foreach (var manifest in latestCaptures.Reverse())
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(manifest.scenarioDisplayName, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Capture", manifest.captureId);
                        EditorGUILayout.LabelField("Mode", manifest.captureMode);
                        EditorGUILayout.LabelField("Samples", manifest.sampleCount.ToString());
                        EditorGUILayout.LabelField("Profiler Trace", manifest.profilerTraceSupported ? manifest.profilerTracePath : manifest.profilerTraceNote);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void StartManualCapture()
        {
            var scenario = scenarios[Mathf.Clamp(selectedScenarioIndex, 0, scenarios.Length - 1)];
            StartCapture(scenario, M136EditorLaptopPerformancePolicy.ManualCaptureMode);
        }

        private void StartAutomatedSmoke()
        {
            automatedQueue.Clear();
            foreach (var scenario in scenarios)
            {
                automatedQueue.Enqueue(scenario);
            }

            automatedRunning = true;
            StartNextAutomatedCapture();
        }

        private void StartNextAutomatedCapture()
        {
            if (!automatedRunning || activeSession != null)
            {
                return;
            }

            if (automatedQueue.Count == 0)
            {
                automatedRunning = false;
                RefreshLatestCaptures();
                ShowNotification(new GUIContent("Automated M136 smoke capture complete."));
                return;
            }

            StartCapture(automatedQueue.Dequeue(), M136EditorLaptopPerformancePolicy.AutomatedCaptureMode);
        }

        private void StartCapture(M136PerformanceScenarioDefinition scenario, string captureMode)
        {
            if (!EditorApplication.isPlaying)
            {
                ShowNotification(new GUIContent("Enter Play Mode before recording live gameplay."));
                return;
            }

            activeSession = new M136LivePerformanceCaptureSession(scenario, captureMode);
            activeDirectory = Path.Combine(M136LivePerformanceCaptureStore.LiveCaptureRoot, activeSession.CaptureId);
            Directory.CreateDirectory(activeDirectory);
            activeFpsOverride = new M136CaptureFpsOverride(use60FpsCaptureCap);
            activeTrace = captureProfilerTrace ? M136ProfilerTraceRecorder.Begin(activeDirectory, activeSession.CaptureId) : null;
            activeDriver = M136RuntimeLiveCaptureDriver.StartCapture(activeSession);
            if (activeDriver.Session == null)
            {
                activeTrace?.Dispose();
                activeFpsOverride?.Dispose();
                DestroyRuntimeDriver();

                activeTrace = null;
                activeFpsOverride = null;
                activeDriver = null;
                activeSession.Dispose();
                activeSession = null;
                ShowNotification(new GUIContent("Telemetry is disabled for this build."));
                return;
            }

            Repaint();
        }

        private void TickCapture()
        {
            if (activeSession == null)
            {
                return;
            }

            if (activeSession.IsComplete)
            {
                CompleteActiveCapture("Capture completed the fixed M136 warmup/sample window.");
            }

            Repaint();
        }

        private void CompleteActiveCapture(string note)
        {
            if (activeSession == null)
            {
                return;
            }

            activeDriver?.StopCapture();
            activeSession.Complete();
            var trace = activeTrace;
            activeTrace = null;
            if (captureProfilerTrace)
            {
                trace?.End();
            }

            var manifestPath = Path.Combine(activeDirectory, M136LivePerformanceCaptureStore.ManifestFileName);
            var rawSampleCsvPath = Path.Combine(activeDirectory, M136LivePerformanceCaptureStore.RawSamplesCsvFileName);
            var result = activeSession.BuildResult(
                activeDirectory,
                manifestPath,
                rawSampleCsvPath,
                trace?.TracePath,
                trace?.TraceSupported ?? false,
                captureProfilerTrace ? trace?.Status ?? "Profiler trace recorder was not available." : "Profiler trace capture was disabled for this capture.",
                note,
                profilerTraceRequested: captureProfilerTrace,
                fpsOverrideApplied: activeFpsOverride?.Applied ?? false,
                fpsOverrideTarget: activeFpsOverride?.Applied == true ? activeFpsOverride.TargetFrameRate : 0,
                samplingSource: M136RuntimeLiveCaptureDriver.SamplingSource);
            M136LivePerformanceCaptureStore.SaveCapture(
                activeDirectory,
                activeSession.Scenario,
                result,
                activeSession.Samples,
                trace?.TracePath,
                trace?.TraceSupported ?? false,
                captureProfilerTrace ? trace?.Status ?? "Profiler trace recorder was not available." : "Profiler trace capture was disabled for this capture.",
                note);

            trace?.Dispose();
            activeFpsOverride?.Dispose();
            DestroyRuntimeDriver();

            activeSession.Dispose();
            activeSession = null;
            activeDriver = null;
            activeFpsOverride = null;
            activeDirectory = null;
            RefreshLatestCaptures();

            if (automatedRunning)
            {
                EditorApplication.delayCall += StartNextAutomatedCapture;
            }
        }

        private static void DrawValidityHints(M136LiveObjectCountSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Validity Hints", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Context", $"branch={snapshot.observedBranchSession}, ship={snapshot.observedSpaceshipHub}, combat={snapshot.observedCombatController}, active={snapshot.observedActiveCombat}, wave={snapshot.observedWaveEncounter}, boss={snapshot.observedBoss}");
            EditorGUILayout.LabelField("Objects", $"enemies={snapshot.activeEnemies}, projectiles={snapshot.activeProjectiles}, vfx={snapshot.activeVfx}, renderers={snapshot.activeRenderers}");
            var operations = M136PerformanceOperationCounters.Snapshot();
            EditorGUILayout.LabelField("Events", $"transitions={operations.RoomTransitionEvents}, navmeshFallbacks={operations.RuntimeNavMeshFallbacks}");
            EditorGUILayout.LabelField("Transition Curtain", $"shows={operations.TransitionCurtainShows}, hides={operations.TransitionCurtainHides}, maxVisibleMs={operations.TransitionCurtainMaxVisibleMilliseconds:0.#}, afterReadyFrames={operations.TransitionCurtainMaxFramesAfterReady}, lockMs={operations.TransitionLockMaxMilliseconds:0.#}, orphans={operations.TransitionOrphanCurtainsRemoved}");
            EditorGUILayout.LabelField("Branch Cache", $"hits={operations.BranchRuntimeCacheHits}, misses={operations.BranchRuntimeCacheMisses}, graphs={operations.BranchGraphBuilds}, distances={operations.BranchRoomDistanceMapBuilds}, rooms={operations.BranchRoomAssetResolves}, descriptors={operations.RoomDescriptorBuilds}");
            EditorGUILayout.LabelField("Branch Loading", $"branch={operations.BranchLoadingStarts}/{operations.BranchLoadingCompletions} maxMs={operations.BranchLoadingMaxMilliseconds:0.#}, boss={operations.BossLoadingStarts}/{operations.BossLoadingCompletions} maxMs={operations.BossLoadingMaxMilliseconds:0.#}, preloadRooms={operations.FullBranchPreloadRooms}, coldMisses={operations.TraversalColdCacheMisses}");
            EditorGUILayout.LabelField("Enemy Pool", $"warm={operations.EnemyPoolWarmRequests}/{operations.EnemyPoolWarmCompletions}, rents={operations.EnemyPoolRents}, returns={operations.EnemyPoolReturns}, misses={operations.EnemyPoolMisses}, hard={operations.EnemyPoolHardInstantiates}");
            EditorGUILayout.LabelField("M139 Soak", $"staleEnemy={operations.M139StaleEnemyStateFailures}, stalePool={operations.M139StaleRuntimePoolStateFailures}, leaks={operations.M139PoolActiveLeaks}, cacheWindows={operations.M139CacheHitRateWindows}/{operations.M139CacheHitRateWindowFailures}, firstUseMiss={operations.M139ShaderMaterialFirstUseMissesAfterLoad}, drift={operations.M139ManagedMemoryDriftMaxMb:0.#}/{operations.M139GraphicsMemoryDriftMaxMb:0.#}MB");
            EditorGUILayout.LabelField("Boot Loading", $"boot={operations.BootLoadingStarts}/{operations.BootLoadingCompletions}, fail={operations.BootLoadingFailures}, maxMs={operations.BootLoadingMaxMilliseconds:0.#}, stages={operations.BootLoadingStageCount}, resources={operations.BootPreloadResourceLoads}, warm={operations.BootPreloadWarmRequests}/{operations.BootPreloadWarmCompletions}, shaders={operations.BootPreloadShaderWarmSuccesses}/{operations.BootPreloadShaderWarmAttempts}, shaderMiss={operations.BootPreloadShaderWarmMisses}, shaderMaxMs={operations.BootPreloadShaderWarmMaxMilliseconds:0.#}");
        }

        private void RefreshLatestCaptures()
        {
            latestCaptures = M136LivePerformanceCaptureStore.LoadManifests()
                .GroupBy(manifest => manifest.scenarioId)
                .Select(group => group.Last())
                .OrderBy(manifest => manifest.scenarioId, StringComparer.Ordinal)
                .ToArray();
        }

        private static void DrawLatestM138StressSummary()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("M138 Combat Scale Stress", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scenarios", string.Join(", ", M138CombatScaleStressScenarioPolicy.StressManifest.Select(scenario => scenario.id)));
            if (!File.Exists(M138CombatScaleStressReportGenerator.DefaultJsonReportPath))
            {
                EditorGUILayout.HelpBox("No M138 automated stress report has been generated yet.", MessageType.None);
                return;
            }

            try
            {
                var report = JsonUtility.FromJson<M138CombatScaleStressReport>(File.ReadAllText(M138CombatScaleStressReportGenerator.DefaultJsonReportPath));
                EditorGUILayout.LabelField("Latest Report", report != null && report.passed ? "PASS" : "FAIL");
                EditorGUILayout.LabelField("Coverage", $"{report?.scenarioCount ?? 0}/{M138CombatScaleStressScenarioPolicy.StressManifest.Length}");
                if (report?.failures != null && report.failures.Length > 0)
                {
                    EditorGUILayout.HelpBox(report.failures[0], MessageType.Warning);
                }
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox($"Could not read M138 report: {exception.Message}", MessageType.Warning);
            }
        }

        private static void DrawLatestM139SoakSummary()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("M139 Long-Run Soak", EditorStyles.boldLabel);
            if (!File.Exists(M139LongRunSoakReportGenerator.DefaultJsonReportPath))
            {
                EditorGUILayout.HelpBox("No M139 long-run soak report has been generated yet.", MessageType.None);
                return;
            }

            try
            {
                var report = JsonUtility.FromJson<M139LongRunSoakReport>(File.ReadAllText(M139LongRunSoakReportGenerator.DefaultJsonReportPath));
                EditorGUILayout.LabelField("Latest Report", report != null && report.passed ? "PASS" : "FAIL");
                EditorGUILayout.LabelField("Coverage", $"{report?.scenarioCount ?? 0}/{M139LongRunSoakReportGenerator.ScenarioIds.Length}");
                if (report?.failures != null && report.failures.Length > 0)
                {
                    EditorGUILayout.HelpBox(report.failures[0], MessageType.Warning);
                }
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox($"Could not read M139 report: {exception.Message}", MessageType.Warning);
            }
        }

        private static void DrawLatestM140BuildRealSummary()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("M140 Build-Real Gate", EditorStyles.boldLabel);
            var path = Path.Combine("output/reports/m140", Hollow.Editor.Build.M140BuildRealGateRunner.LatestEditorJsonFileName);
            if (!File.Exists(path))
            {
                EditorGUILayout.HelpBox("No M140 built-player gate report has been generated yet.", MessageType.None);
                return;
            }

            try
            {
                var report = JsonUtility.FromJson<Hollow.Editor.Build.M140BuildRealGateEditorReport>(File.ReadAllText(path));
                EditorGUILayout.LabelField("Latest Report", report?.result ?? "Unknown");
                EditorGUILayout.LabelField("Targets", (report?.targets?.Count ?? 0).ToString());
                if (report?.failures != null && report.failures.Length > 0)
                {
                    EditorGUILayout.HelpBox(report.failures[0], MessageType.Warning);
                }
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox($"Could not read M140 report: {exception.Message}", MessageType.Warning);
            }
        }

        private void DestroyRuntimeDriver()
        {
            if (activeDriver == null)
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                Destroy(activeDriver.gameObject);
            }
            else
            {
                DestroyImmediate(activeDriver.gameObject);
            }

            activeDriver = null;
        }

        private sealed class M136ProfilerTraceRecorder : IDisposable
        {
            private bool disposed;

            private M136ProfilerTraceRecorder(string tracePath)
            {
                TracePath = tracePath;
            }

            public string TracePath { get; }

            public bool TraceSupported { get; private set; }

            public string Status { get; private set; }

            public static M136ProfilerTraceRecorder Begin(string directory, string captureId)
            {
                Directory.CreateDirectory(directory);
                var recorder = new M136ProfilerTraceRecorder(Path.Combine(directory, $"{captureId}.data"));
                recorder.TryBegin();
                return recorder;
            }

            public void End()
            {
                if (disposed)
                {
                    return;
                }

                try
                {
                    var driver = Type.GetType("UnityEditorInternal.ProfilerDriver,UnityEditor");
                    var saveMethod = driver?.GetMethod("SaveProfile", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
                    saveMethod?.Invoke(null, new object[] { TracePath });
                    TraceSupported = File.Exists(TracePath);
                    Status = TraceSupported
                        ? $"Profiler trace saved: {TracePath}"
                        : "Profiler trace export API was unavailable or produced no file in this Editor context.";
                }
                catch (Exception exception)
                {
                    TraceSupported = false;
                    Status = $"Profiler trace export unavailable: {exception.GetType().Name}.";
                }
            }

            public void Dispose()
            {
                disposed = true;
            }

            private void TryBegin()
            {
                try
                {
                    Profiler.enabled = true;
                    var driver = Type.GetType("UnityEditorInternal.ProfilerDriver,UnityEditor");
                    SetStaticProperty(driver, "enabled", true);
                    SetStaticProperty(driver, "profileEditor", true);
                    driver?.GetMethod("ClearAllFrames", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, null);
                    Status = $"Profiler trace recording started: {TracePath}";
                }
                catch (Exception exception)
                {
                    Status = $"Profiler trace start unavailable: {exception.GetType().Name}.";
                }
            }

            private static void SetStaticProperty(Type type, string propertyName, object value)
            {
                var property = type?.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (property?.CanWrite == true)
                {
                    property.SetValue(null, value);
                }
            }
        }
    }
}
