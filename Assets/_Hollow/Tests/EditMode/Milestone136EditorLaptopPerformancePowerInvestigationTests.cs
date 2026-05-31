using System;
using System.IO;
using System.Linq;
using Hollow.Core.Diagnostics;
using Hollow.Diagnostics;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone136EditorLaptopPerformancePowerInvestigationTests
    {
        [Test]
        public void TelemetrySamplerRecordsRequiredCpuFpsGcMetricsAndGracefullyMarksGpu()
        {
            var scenario = M136EditorLaptopPerformancePolicy.ScenarioManifest.First();
            using var sampler = new M136FrameTelemetrySampler();
            for (var index = 0; index < 64; index++)
            {
                sampler.RecordSyntheticFrame(
                    16.3d + index % 4 * 0.1d,
                    index % 8 == 0 ? 2048d : 0d,
                    7.5d,
                    3.25d,
                    96d,
                    320d);
            }

            M136PerformanceOperationCounters.Reset();
            M136PerformanceOperationCounters.ReportMiniMapRebuild();
            M136PerformanceOperationCounters.ReportWallVisibilityUpdate();
            M136PerformanceOperationCounters.ReportCombatHudRefresh();
            M136PerformanceOperationCounters.ReportRuntimeNavMeshFallback();

            var result = sampler.BuildScenarioResult(
                scenario,
                "editmode-synthetic",
                liveCaptured: true,
                requiresManualCapture: false,
                "EditMode synthetic sampler contract.");

            Assert.IsTrue(M136EditorLaptopPerformancePolicy.ValidateScenarioResult(result, out var detail), detail);
            AssertMetricSupported(result, "frame_time_ms");
            AssertMetricSupported(result, "fps");
            AssertMetricSupported(result, "main_thread_ms");
            AssertMetricSupported(result, "render_thread_ms");
            AssertMetricSupported(result, "gc_allocated_bytes");
            AssertMetricSupported(result, "gc_allocated_recorder_bytes");
            AssertMetricSupported(result, "gc_allocated_frame_delta_bytes");
            AssertMetricSupported(result, "managed_memory_mb");
            AssertMetricSupported(result, "graphics_memory_mb");
            var gpu = result.metrics.First(metric => metric.id == "gpu_frame_ms");
            Assert.IsFalse(gpu.supported);
            StringAssert.Contains("unsupported", gpu.note.ToLowerInvariant());
            Assert.AreEqual(1, result.operations.miniMapRebuilds);
            Assert.AreEqual(1, result.operations.wallVisibilityUpdates);
            Assert.AreEqual(1, result.operations.combatHudRefreshes);
            Assert.AreEqual(1, result.operations.runtimeNavMeshFallbacks);
        }

        [Test]
        public void OperationCountersAttributeColdCacheMissesAndProjectilePressure()
        {
            M136PerformanceOperationCounters.Reset();

            M136PerformanceOperationCounters.ReportTraversalColdCacheMiss("live-room-cache", "room_03", "runtime-root-missing");
            M136PerformanceOperationCounters.ReportBranchRuntimeCacheMiss("descriptor", "room_asset_01", "branch-load");
            M136PerformanceOperationCounters.ReportPresentationCacheMiss("prefab", "ChestReward", "reward-room");
            M136PerformanceOperationCounters.ReportProjectileActiveCount(24);
            M136PerformanceOperationCounters.ReportProjectileSpawn(6);
            M136PerformanceOperationCounters.ReportProjectileCollisionCheck(18);
            M136PerformanceOperationCounters.ReportProjectileReturn(4);
            M136PerformanceOperationCounters.ReportProjectileUpdate(0.25f);

            var snapshot = M136PerformanceOperationCounters.Snapshot();

            Assert.AreEqual(1, snapshot.TraversalColdCacheMisses);
            Assert.AreEqual(1, snapshot.BranchRuntimeCacheMisses);
            Assert.AreEqual(1, snapshot.PresentationPrefabCacheMisses);
            StringAssert.Contains("live-room-cache", snapshot.CacheMissAttributionSummary);
            StringAssert.Contains("room_asset_01", snapshot.CacheMissAttributionSummary);
            Assert.AreEqual(3, snapshot.CacheMissAttributionRows.Length);
            Assert.AreEqual(24, snapshot.ProjectileActivePeak);
            Assert.AreEqual(6, snapshot.ProjectileSpawns);
            Assert.AreEqual(18, snapshot.ProjectileCollisionChecks);
            Assert.AreEqual(4, snapshot.ProjectileReturns);
            Assert.AreEqual(0.25f, snapshot.ProjectileUpdateMaxMilliseconds);
        }

        [Test]
        public void ScenarioManifestIncludesRepresentativeFixedWindowScenarios()
        {
            Assert.IsTrue(M136EditorLaptopPerformancePolicy.ValidateScenarioManifest(out var detail), detail);
            var scenarios = M136EditorLaptopPerformancePolicy.ScenarioManifest;
            CollectionAssert.AreEquivalent(
                M136EditorLaptopPerformancePolicy.RequiredScenarioIds,
                scenarios.Select(scenario => scenario.id).ToArray());
            Assert.IsTrue(scenarios.All(scenario => Math.Abs(scenario.warmupSeconds - 3f) < 0.001f));
            Assert.IsTrue(scenarios.All(scenario => Math.Abs(scenario.sampleSeconds - 30f) < 0.001f));
        }

        [Test]
        public void DevReleaseGatingPreventsNormalReleaseTelemetry()
        {
            Assert.IsTrue(M136EditorLaptopPerformancePolicy.IsTelemetryAllowed(isEditor: true, isDebugBuild: false));
            Assert.IsTrue(M136EditorLaptopPerformancePolicy.IsTelemetryAllowed(isEditor: false, isDebugBuild: true));
            Assert.IsFalse(M136EditorLaptopPerformancePolicy.IsTelemetryAllowed(isEditor: false, isDebugBuild: false));
        }

        [Test]
        public void LiveCaptureSessionExcludesWarmupFramesAndExportsManifest()
        {
            var root = Path.Combine(Path.GetTempPath(), "hollow_m136_live_capture_tests", Guid.NewGuid().ToString("N"));
            var scenario = M136EditorLaptopPerformancePolicy.ScenarioManifest.First();
            using var session = new M136LivePerformanceCaptureSession(scenario, M136EditorLaptopPerformancePolicy.ManualCaptureMode);
            Assert.IsTrue(session.Begin(new DateTime(2026, 5, 28, 10, 0, 0, DateTimeKind.Utc)));

            Assert.IsFalse(session.Tick(1f, M136LiveObjectCountSnapshot.Empty("test")));
            Assert.IsFalse(session.Tick(1f, M136LiveObjectCountSnapshot.Empty("test")));
            Assert.IsFalse(session.Tick(1f, M136LiveObjectCountSnapshot.Empty("test")));
            Assert.AreEqual(0, session.Samples.Count, "Warmup frames should not be recorded.");

            for (var index = 0; index < 30; index++)
            {
                session.Tick(1f, new M136LiveObjectCountSnapshot
                {
                    activeEnemies = 2,
                    activeProjectiles = 1,
                    activeVfx = 3,
                    activeUiCanvases = 2,
                    source = "editmode-test"
                });
            }

            Assert.IsTrue(session.IsComplete);
            Assert.AreEqual(30, session.Samples.Count);
            var directory = Path.Combine(root, session.CaptureId);
            var manifestPath = Path.Combine(directory, M136LivePerformanceCaptureStore.ManifestFileName);
            var csvPath = Path.Combine(directory, M136LivePerformanceCaptureStore.RawSamplesCsvFileName);
            var result = session.BuildResult(
                directory,
                manifestPath,
                csvPath,
                profilerTracePath: string.Empty,
                profilerTraceSupported: false,
                profilerTraceNote: "Profiler trace export unavailable in EditMode test.",
                note: "EditMode live capture export contract.");
            var manifest = M136LivePerformanceCaptureStore.SaveCapture(
                directory,
                scenario,
                result,
                session.Samples,
                profilerTracePath: string.Empty,
                profilerTraceSupported: false,
                profilerTraceNote: "Profiler trace export unavailable in EditMode test.",
                note: "EditMode live capture export contract.");

            Assert.IsTrue(File.Exists(manifestPath));
            Assert.IsTrue(File.Exists(csvPath));
            Assert.AreEqual(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.LockId, manifest.lockId);
            Assert.AreEqual(scenario.id, manifest.scenarioId);
            Assert.AreEqual(30, manifest.sampleCount);
            Assert.IsTrue(manifest.complete);
            Assert.IsFalse(manifest.profilerTraceSupported);
            Assert.Greater(result.objectCounts.peakEnemies, 0);
        }

        [Test]
        public void ReportUsesLatestLiveCapturesAndFallsBackForMissingScenarios()
        {
            var root = Path.Combine(Path.GetTempPath(), "hollow_m136_report_live_capture_tests", Guid.NewGuid().ToString("N"));
            var scenario = M136EditorLaptopPerformancePolicy.ScenarioManifest.First();
            using (var sampler = new M136FrameTelemetrySampler())
            {
                for (var index = 0; index < 12; index++)
                {
                    sampler.RecordSyntheticFrame(
                        14d,
                        0d,
                        6d,
                        3d,
                        110d,
                        420d,
                        4d);
                }

                var directory = Path.Combine(root, "20260528_live_capture");
                var result = sampler.BuildScenarioResult(
                    scenario,
                    M136EditorLaptopPerformancePolicy.ManualCaptureMode,
                    liveCaptured: true,
                    requiresManualCapture: false,
                    "Synthetic live capture for report selection.",
                    captureId: "test_live_capture",
                    captureStartedAtUtc: "2026-05-28T10:00:00.0000000Z",
                    captureCompletedAtUtc: "2026-05-28T10:00:30.0000000Z",
                    artifactDirectory: directory,
                    manifestPath: Path.Combine(directory, M136LivePerformanceCaptureStore.ManifestFileName),
                    rawSampleCsvPath: Path.Combine(directory, M136LivePerformanceCaptureStore.RawSamplesCsvFileName),
                    profilerTracePath: Path.Combine(directory, "test_live_capture.data"),
                    profilerTraceSupported: false,
                    profilerTraceNote: "Trace unsupported in test.");
                M136LivePerformanceCaptureStore.SaveCapture(
                    directory,
                    scenario,
                    result,
                    sampler.FrameSamples,
                    Path.Combine(directory, "test_live_capture.data"),
                    profilerTraceSupported: false,
                    profilerTraceNote: "Trace unsupported in test.",
                    note: "Synthetic live capture for report selection.");
            }

            var report = Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.BuildReport(root);

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? Array.Empty<string>()));
            Assert.IsTrue(report.usedLiveCaptures);
            Assert.AreEqual(1, report.liveCaptureScenarioCount);
            Assert.AreEqual(M136EditorLaptopPerformancePolicy.ScenarioManifest.Length, report.scenarios.Length);
            Assert.IsTrue(report.scenarios.First(scenarioResult => scenarioResult.scenarioId == scenario.id).liveCaptured);
            Assert.IsTrue(report.scenarios.Any(scenarioResult => scenarioResult.captureMode == "deterministic-editor-baseline"));
            Assert.AreEqual(M136EditorLaptopPerformancePolicy.ScenarioManifest.Length, report.captureComparisons.Length);
        }

        [Test]
        public void LiveReportPassesM136ChecksAndRanksKnownSuspects()
        {
            var report = Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? Array.Empty<string>()));
            Assert.Greater(report.totalChecks, 20);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            Assert.AreEqual(M136EditorLaptopPerformancePolicy.ComfortTargetFrameRate, report.comfortTargetFrameRate);
            Assert.AreEqual(M136EditorLaptopPerformancePolicy.ScenarioManifest.Length, report.scenarioManifest.Length);
            Assert.AreEqual(M136EditorLaptopPerformancePolicy.ScenarioManifest.Length, report.scenarios.Length);
            Assert.IsTrue(report.renderSettings.targetFrameRate > 0);
            CollectionAssert.Contains(report.evidencePaths, Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.DocsPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.M135ReportPath);
            var recommendationIds = report.recommendations.Select(recommendation => recommendation.id).ToArray();
            CollectionAssert.Contains(recommendationIds, "fps-vsync-power-cap");
            CollectionAssert.Contains(recommendationIds, "pc-urp-shadow-post-cost");
            CollectionAssert.Contains(recommendationIds, "per-frame-hud-minimap-walls");
            CollectionAssert.Contains(recommendationIds, "runtime-navmesh-fallback-spikes");
        }

        [Test]
        public void LiveCaptureInsightsReportClassifiesInvalidCombatWaveBossCaptures()
        {
            var root = Path.Combine(Path.GetTempPath(), "hollow_m136a_insights_tests", Guid.NewGuid().ToString("N"));
            WriteSyntheticInsightCapture(root, "ship_hub_idle", enemies: 0, projectiles: 0, vfx: 0, renderers: 29, wallUpdates: 2400, hudRefreshes: 2400, sampleCount: 9000);
            WriteSyntheticInsightCapture(root, "normal_branch_idle", enemies: 0, projectiles: 0, vfx: 0, renderers: 40, wallUpdates: 3200, hudRefreshes: 3200, sampleCount: 15000);
            WriteSyntheticInsightCapture(root, "active_combat_room", enemies: 0, projectiles: 0, vfx: 0, renderers: 29, wallUpdates: 3200, hudRefreshes: 3200, sampleCount: 15000);
            WriteSyntheticInsightCapture(root, "wave_crowded_room", enemies: 0, projectiles: 0, vfx: 0, renderers: 29, wallUpdates: 3200, hudRefreshes: 3200, sampleCount: 15000);
            WriteSyntheticInsightCapture(root, "anchor_boss_smoke", enemies: 0, projectiles: 0, vfx: 0, renderers: 29, wallUpdates: 3200, hudRefreshes: 3200, sampleCount: 12000);
            WriteSyntheticInsightCapture(root, "room_transition_navmesh", enemies: 0, projectiles: 0, vfx: 0, renderers: 29, wallUpdates: 3200, hudRefreshes: 3200, sampleCount: 8000);

            var report = Milestone136ALiveCaptureInsightsAssetGenerator.BuildReport(root);

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? Array.Empty<string>()));
            Assert.AreEqual(6, report.coveredScenarioCount);
            Assert.AreEqual(3, report.invalidCaptureCount);
            Assert.GreaterOrEqual(report.needsRecaptureCount, 3);
            Assert.AreEqual("NEEDS_RECAPTURE_BEFORE_OPTIMIZATION", report.insightResult);
            Assert.AreEqual("Invalid", report.captures.First(capture => capture.scenarioId == "active_combat_room").validityGrade);
            Assert.AreEqual("Invalid", report.captures.First(capture => capture.scenarioId == "wave_crowded_room").validityGrade);
            Assert.AreEqual("Invalid", report.captures.First(capture => capture.scenarioId == "anchor_boss_smoke").validityGrade);
            StringAssert.Contains("Invalid", report.metricConfidence.First(confidence => confidence.metricId == "frame_time_ms/fps").confidence);
            Assert.AreEqual("fix-capture-validity", report.recommendations.First().id);
        }

        [Test]
        public void LiveCaptureInsightsMarkdownJsonPdfAreGeneratedAndExplainLimitations()
        {
            Assert.IsTrue(File.Exists(Milestone136ALiveCaptureInsightsAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone136ALiveCaptureInsightsAssetGenerator.ReportJsonPath));
            Assert.IsTrue(File.Exists(Milestone136ALiveCaptureInsightsAssetGenerator.ReportPdfPath));

            var markdown = File.ReadAllText(Milestone136ALiveCaptureInsightsAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M136A Live Capture Insights Report", markdown);
            StringAssert.Contains("Do not overfit combat/wave/boss", markdown);
            StringAssert.Contains("Metric Confidence", markdown);
            StringAssert.Contains("Recommended Next Steps", markdown);

            var json = JsonUtility.FromJson<M136ALiveCaptureInsightsReport>(
                File.ReadAllText(Milestone136ALiveCaptureInsightsAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone136ALiveCaptureInsightsAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.AreEqual(M136EditorLaptopPerformancePolicy.ScenarioManifest.Length, json.expectedScenarioCount);
            Assert.GreaterOrEqual(json.needsRecaptureCount, 3);

            var pdf = File.ReadAllBytes(Milestone136ALiveCaptureInsightsAssetGenerator.ReportPdfPath);
            Assert.Greater(pdf.Length, 4096);
            Assert.AreEqual((byte)'%', pdf[0]);
            Assert.AreEqual((byte)'P', pdf[1]);
            Assert.AreEqual((byte)'D', pdf[2]);
            Assert.AreEqual((byte)'F', pdf[3]);
        }

        [Test]
        public void GeneratedMarkdownJsonPdfArePresentPassingAndUseM136LockId()
        {
            Assert.IsTrue(File.Exists(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportJsonPath));
            Assert.IsTrue(File.Exists(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportPdfPath));

            var markdown = File.ReadAllText(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M136 Editor Laptop Performance + Power Investigation Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.LockId, markdown);
            StringAssert.Contains("Ranked Suspects And Suggested Solutions", markdown);
            StringAssert.Contains("gpu_frame_ms", markdown);

            var json = JsonUtility.FromJson<Milestone136EditorLaptopPerformancePowerInvestigationReport>(
                File.ReadAllText(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.AreEqual(M136EditorLaptopPerformancePolicy.ScenarioManifest.Length, json.scenarios.Length);

            var pdf = File.ReadAllBytes(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportPdfPath);
            Assert.Greater(pdf.Length, 4096);
            Assert.AreEqual((byte)'%', pdf[0]);
            Assert.AreEqual((byte)'P', pdf[1]);
            Assert.AreEqual((byte)'D', pdf[2]);
            Assert.AreEqual((byte)'F', pdf[3]);
        }

        [Test]
        public void ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone136EditorLaptopPerformancePowerInvestigationValidator.Validate(exitOnFailure: false));
        }

        [Test]
        public void M136BRuntimeSamplingCadenceAndValidityRejectLegacyCombatRelabels()
        {
            var scenario = M136EditorLaptopPerformancePolicy.FindScenario("active_combat_room");
            using var sampler = new M136FrameTelemetrySampler();
            for (var index = 0; index < 1800; index++)
            {
                sampler.RecordSyntheticFrame(16.67d, 0d, 6d, 2d, 128d, 512d, 7d);
            }

            var result = sampler.BuildScenarioResult(
                scenario,
                M136EditorLaptopPerformancePolicy.ManualCaptureMode,
                liveCaptured: true,
                requiresManualCapture: false,
                "Corrected runtime-frame synthetic capture.",
                samplingSource: M136RuntimeLiveCaptureDriver.SamplingSource,
                fpsOverrideApplied: true,
                fpsOverrideTarget: M136EditorLaptopPerformancePolicy.ComfortTargetFrameRate);
            result.objectCounts = new M136LiveObjectCountSummary
            {
                sampleCount = 1800,
                peakEnemies = 4,
                observedCombatController = true,
                observedActiveCombat = true
            };
            var manifest = ManifestFor(result);
            var validity = M136CaptureValidityClassifier.Classify(manifest, result);

            Assert.AreEqual(M136FrameCadencePolicy.Trusted, result.frameCadenceConfidence);
            Assert.AreEqual(1800, result.rawSampleCount);
            Assert.AreEqual(M136CaptureValidityClassifier.Valid, validity.Grade);

            result.samplingSource = M136FrameCadencePolicy.UnknownSamplingSource;
            validity = M136CaptureValidityClassifier.Classify(manifest, result);
            Assert.AreEqual(M136CaptureValidityClassifier.Directional, validity.Grade);
        }

        [Test]
        public void M136BFpsOverrideRestoresPreviousFrameSettings()
        {
            var previousTarget = Application.targetFrameRate;
            var previousVsync = QualitySettings.vSyncCount;
            try
            {
                Application.targetFrameRate = 120;
                QualitySettings.vSyncCount = 2;
                using (var fpsOverride = new M136CaptureFpsOverride(enabled: true, targetFrameRate: 60))
                {
                    Assert.IsTrue(fpsOverride.Applied);
                    Assert.AreEqual(60, Application.targetFrameRate);
                    Assert.AreEqual(0, QualitySettings.vSyncCount);
                }

                Assert.AreEqual(120, Application.targetFrameRate);
                Assert.AreEqual(2, QualitySettings.vSyncCount);
            }
            finally
            {
                Application.targetFrameRate = previousTarget;
                QualitySettings.vSyncCount = previousVsync;
            }
        }

        [Test]
        public void M136BTransitionValidityRequiresObservedTransitionEvent()
        {
            var scenario = M136EditorLaptopPerformancePolicy.FindScenario("room_transition_navmesh");
            M136PerformanceOperationCounters.Reset();
            using var sampler = new M136FrameTelemetrySampler();
            for (var index = 0; index < 1800; index++)
            {
                sampler.RecordSyntheticFrame(16.67d, 0d, 6d, 2d, 128d, 512d, 7d);
            }

            var result = sampler.BuildScenarioResult(
                scenario,
                M136EditorLaptopPerformancePolicy.ManualCaptureMode,
                liveCaptured: true,
                requiresManualCapture: false,
                "No transition event.",
                samplingSource: M136RuntimeLiveCaptureDriver.SamplingSource,
                fpsOverrideApplied: true,
                fpsOverrideTarget: 60);
            result.objectCounts = new M136LiveObjectCountSummary { sampleCount = 1800, observedBranchSession = true, peakRenderers = 50 };
            Assert.AreEqual(M136CaptureValidityClassifier.Invalid, M136CaptureValidityClassifier.Classify(ManifestFor(result), result).Grade);

            M136PerformanceOperationCounters.Reset();
            M136PerformanceOperationCounters.ReportRoomTransition();
            using var transitionSampler = new M136FrameTelemetrySampler();
            for (var index = 0; index < 1800; index++)
            {
                transitionSampler.RecordSyntheticFrame(16.67d, 0d, 6d, 2d, 128d, 512d, 7d);
            }

            var transitionResult = transitionSampler.BuildScenarioResult(
                scenario,
                M136EditorLaptopPerformancePolicy.ManualCaptureMode,
                liveCaptured: true,
                requiresManualCapture: false,
                "Observed transition event.",
                samplingSource: M136RuntimeLiveCaptureDriver.SamplingSource,
                fpsOverrideApplied: true,
                fpsOverrideTarget: 60);
            transitionResult.objectCounts = result.objectCounts;
            Assert.AreEqual(M136CaptureValidityClassifier.Valid, M136CaptureValidityClassifier.Classify(ManifestFor(transitionResult), transitionResult).Grade);
        }

        [Test]
        public void M136BReportArtifactsAreGeneratedAndMeasurementOnly()
        {
            Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.Generate();

            Assert.IsTrue(File.Exists(Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.ReportJsonPath));
            Assert.IsTrue(File.Exists(Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.ReportPdfPath));

            var markdown = File.ReadAllText(Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M136B Corrected Live Performance Capture", markdown);
            StringAssert.Contains("No optimization changes are applied", markdown);
            StringAssert.Contains("Recapture Checklist", markdown);

            var json = JsonUtility.FromJson<Milestone136BCorrectedLivePerformanceCaptureReport>(
                File.ReadAllText(Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed, string.Join("\n", json.failures ?? Array.Empty<string>()));
            Assert.AreEqual(M136EditorLaptopPerformancePolicy.ScenarioManifest.Length, json.recaptureChecklist.Length);

            var pdf = File.ReadAllBytes(Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.ReportPdfPath);
            Assert.Greater(pdf.Length, 4096);
            Assert.AreEqual((byte)'%', pdf[0]);
            Assert.AreEqual((byte)'P', pdf[1]);
            Assert.AreEqual((byte)'D', pdf[2]);
            Assert.AreEqual((byte)'F', pdf[3]);
        }

        private static void AssertMetricSupported(M136PerformanceScenarioResult result, string id)
        {
            var metric = result.metrics.First(candidate => candidate.id == id);
            Assert.IsTrue(metric.supported, id);
            Assert.Greater(metric.sampleCount, 0, id);
        }

        private static M136LiveCaptureManifest ManifestFor(M136PerformanceScenarioResult result)
        {
            return new M136LiveCaptureManifest
            {
                lockId = M136EditorLaptopPerformancePolicy.LockId,
                captureId = result.captureId ?? "editmode_capture",
                captureMode = result.captureMode,
                scenarioId = result.scenarioId,
                scenarioDisplayName = result.displayName,
                warmupSeconds = result.warmupSeconds,
                sampleSeconds = result.sampleSeconds,
                sampleCount = result.rawSampleCount,
                liveCaptured = result.liveCaptured,
                complete = result.liveCaptured && result.rawSampleCount > 0,
                profilerTraceRequested = result.profilerTraceRequested,
                profilerTraceSupported = result.profilerTraceSupported,
                fpsOverrideApplied = result.fpsOverrideApplied,
                fpsOverrideTarget = result.fpsOverrideTarget,
                samplingSource = result.samplingSource,
                sampleRateHz = result.sampleRateHz,
                expectedSampleCountMin = result.expectedSampleCountMin,
                expectedSampleCountMax = result.expectedSampleCountMax,
                frameCadenceConfidence = result.frameCadenceConfidence,
                objectCounts = result.objectCounts
            };
        }

        private static void WriteSyntheticInsightCapture(
            string root,
            string scenarioId,
            int enemies,
            int projectiles,
            int vfx,
            int renderers,
            int wallUpdates,
            int hudRefreshes,
            int sampleCount)
        {
            var scenario = M136EditorLaptopPerformancePolicy.FindScenario(scenarioId);
            Assert.IsNotNull(scenario, scenarioId);
            using var sampler = new M136FrameTelemetrySampler();
            M136PerformanceOperationCounters.Reset();
            for (var index = 0; index < wallUpdates; index++)
            {
                M136PerformanceOperationCounters.ReportWallVisibilityUpdate();
            }

            for (var index = 0; index < hudRefreshes; index++)
            {
                M136PerformanceOperationCounters.ReportCombatHudRefresh();
            }

            for (var index = 0; index < sampleCount; index++)
            {
                sampler.RecordSyntheticFrame(
                    2.8d + index % 7 * 0.03d,
                    200000d,
                    8.4d,
                    0d,
                    7000d,
                    1200d,
                    7.5d);
            }

            var directory = Path.Combine(root, $"20260528_{scenarioId}");
            var tracePath = Path.Combine(directory, $"{scenarioId}.data");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(tracePath, new byte[8192]);
            var result = sampler.BuildScenarioResult(
                scenario,
                M136EditorLaptopPerformancePolicy.AutomatedCaptureMode,
                liveCaptured: true,
                requiresManualCapture: false,
                "Synthetic insight capture.",
                captureId: $"test_{scenarioId}",
                captureStartedAtUtc: "2026-05-28T10:00:00.0000000Z",
                captureCompletedAtUtc: "2026-05-28T10:00:30.0000000Z",
                artifactDirectory: directory,
                manifestPath: Path.Combine(directory, M136LivePerformanceCaptureStore.ManifestFileName),
                rawSampleCsvPath: Path.Combine(directory, M136LivePerformanceCaptureStore.RawSamplesCsvFileName),
                profilerTracePath: tracePath,
                profilerTraceSupported: true,
                profilerTraceNote: "Synthetic trace.");
            result.objectCounts = new M136LiveObjectCountSummary
            {
                sampleCount = sampleCount,
                peakEnemies = enemies,
                peakProjectiles = projectiles,
                peakVfx = vfx,
                peakRenderers = renderers,
                peakUiCanvases = 1,
                source = "editmode-synthetic"
            };
            M136LivePerformanceCaptureStore.SaveCapture(
                directory,
                scenario,
                result,
                sampler.FrameSamples,
                tracePath,
                profilerTraceSupported: true,
                profilerTraceNote: "Synthetic trace.",
                note: "Synthetic insight capture.");
        }
    }
}
