using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone136EditorLaptopPerformancePowerInvestigationReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public bool passed;
        public string result;
        public int totalChecks;
        public int passedChecks;
        public int comfortTargetFrameRate;
        public string[] evidencePaths;
        public M136RenderSettingsSnapshot renderSettings;
        public M136PerformanceScenarioDefinition[] scenarioManifest;
        public M136PerformanceScenarioResult[] scenarios;
        public bool usedLiveCaptures;
        public int liveCaptureScenarioCount;
        public M136LiveCaptureManifest[] latestLiveCaptures;
        public M136CaptureComparison[] captureComparisons;
        public Milestone136PerformanceRecommendation[] recommendations;
        public Milestone136EditorLaptopPerformancePowerInvestigationCheck[] checks;
        public string[] failures;
    }

    [Serializable]
    public sealed class Milestone136EditorLaptopPerformancePowerInvestigationCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    [Serializable]
    public sealed class Milestone136PerformanceRecommendation
    {
        public int rank;
        public string id;
        public string severity;
        public string category;
        public string title;
        public string evidence;
        public string suggestedAction;
    }

    public static class Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator
    {
        public const string LockId = M136EditorLaptopPerformancePolicy.LockId;
        public const string Title = M136EditorLaptopPerformancePolicy.Title;
        public const string DocsPath = "Docs/Milestone136EditorLaptopPerformancePowerInvestigation.md";
        public const string M135ReportPath = "output/reports/m135_full_run_combat_readiness_lock.md";
        public const string ReportMarkdownPath = "output/reports/m136_editor_laptop_performance_power_investigation.md";
        public const string ReportJsonPath = "output/reports/m136_editor_laptop_performance_power_investigation.json";
        public const string ReportPdfPath = "output/reports/m136_editor_laptop_performance_power_investigation.pdf";
        public const string TelemetryPath = "Assets/_Hollow/Scripts/Hollow.Diagnostics/M136EditorLaptopPerformanceTelemetry.cs";
        public const string OperationCountersPath = "Assets/_Hollow/Scripts/Hollow.Core/M136PerformanceOperationCounters.cs";
        public const string LiveCaptureWindowPath = "Assets/_Hollow/Scripts/Hollow.Editor/M136LiveGameplayCaptureWindow.cs";
        public const string M136TestsPath = "Assets/_Hollow/Tests/EditMode/Milestone136EditorLaptopPerformancePowerInvestigationTests.cs";
        public const string WindowsPolishPath = "Assets/_Hollow/Data/Platform/Polish/PlatformPolish_WindowsStandard3D.asset";
        public const string PcRenderPipelineAssetPath = "Assets/Settings/PC_RPAsset.asset";
        public const string DefaultVolumeProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";
        public const string WindowsCameraRigPath = "Assets/_Hollow/Prefabs/Cameras/WindowsCameraRig.prefab";

        private const string GeneratorPath = "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.cs";
        private const string ValidatorPath = "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone136EditorLaptopPerformancePowerInvestigationValidator.cs";
        private const string MiniMapControllerPath = "Assets/_Hollow/Scripts/Hollow.UI/Shell/BranchMiniMapController.cs";
        private const string WallVisibilityControllerPath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomWallVisibilityController.cs";
        private const string CombatHudControllerPath = "Assets/_Hollow/Scripts/Hollow.Combat/CombatHudController.cs";
        private const string RoomRuntimeRootPath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomRuntimeRoot.cs";

        public static readonly string[] RequiredEvidencePaths =
        {
            DocsPath,
            M135ReportPath,
            GeneratorPath,
            ValidatorPath,
            M136TestsPath,
            TelemetryPath,
            OperationCountersPath,
            LiveCaptureWindowPath,
            WindowsPolishPath,
            PcRenderPipelineAssetPath,
            DefaultVolumeProfilePath,
            WindowsCameraRigPath,
            MiniMapControllerPath,
            WallVisibilityControllerPath,
            CombatHudControllerPath,
            RoomRuntimeRootPath
        };

        [MenuItem("Hollow/Performance/Run Editor Laptop Power Investigation")]
        public static void RunEditorLaptopPowerInvestigation()
        {
            Generate();
        }

        [MenuItem("Hollow/Generation/Generate Milestone 136 Editor Laptop Performance + Power Investigation")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportMarkdownPath) ?? "output/reports");
            File.WriteAllText(DocsPath, BuildDocsMarkdown());

            var report = BuildReport();
            File.WriteAllText(ReportJsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(ReportMarkdownPath, ToMarkdown(report));
            WritePdf(ReportPdfPath, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var message = $"{Title} generation {report.result}: {report.passedChecks}/{report.totalChecks} checks passed. PDF: {ReportPdfPath}";
            if (report.passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public static Milestone136EditorLaptopPerformancePowerInvestigationReport BuildReport()
        {
            return BuildReport(M136LivePerformanceCaptureStore.LiveCaptureRoot);
        }

        public static Milestone136EditorLaptopPerformancePowerInvestigationReport BuildReport(string liveCaptureRoot)
        {
            var renderSettings = BuildRenderSettingsSnapshot();
            var scenarioManifest = M136EditorLaptopPerformancePolicy.ScenarioManifest;
            var liveManifests = M136LivePerformanceCaptureStore.LoadManifests(liveCaptureRoot);
            var latestLiveCaptures = LatestManifestByScenario(liveManifests).ToArray();
            var scenarios = BuildScenarioResultsPreferLive(scenarioManifest, renderSettings, latestLiveCaptures).ToArray();
            var captureComparisons = BuildCaptureComparisons(scenarioManifest, liveManifests).ToArray();
            var recommendations = BuildRecommendations(renderSettings).ToArray();
            var checks = new List<Milestone136EditorLaptopPerformancePowerInvestigationCheck>();

            foreach (var path in RequiredEvidencePaths)
            {
                AddCheck(
                    checks,
                    $"evidence:{Path.GetFileName(path)}",
                    "Evidence",
                    File.Exists(path),
                    File.Exists(path) ? $"Found `{path}`." : $"Missing `{path}`.");
            }

            AddDocsChecks(checks);
            AddTelemetryChecks(checks, scenarioManifest, scenarios, latestLiveCaptures, captureComparisons);
            AddRenderSnapshotChecks(checks, renderSettings);
            AddRecommendationChecks(checks, recommendations);
            AddDependencyChecks(checks);
            AddRuntimeHookChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone136EditorLaptopPerformancePowerInvestigationReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                passed = failures.Length == 0,
                result = failures.Length == 0 ? "PASSED" : "FAILED",
                totalChecks = checks.Count,
                passedChecks = checks.Count(check => check.passed),
                comfortTargetFrameRate = M136EditorLaptopPerformancePolicy.ComfortTargetFrameRate,
                evidencePaths = RequiredEvidencePaths.ToArray(),
                renderSettings = renderSettings,
                scenarioManifest = scenarioManifest,
                scenarios = scenarios,
                usedLiveCaptures = latestLiveCaptures.Length > 0,
                liveCaptureScenarioCount = latestLiveCaptures.Length,
                latestLiveCaptures = latestLiveCaptures,
                captureComparisons = captureComparisons,
                recommendations = recommendations,
                checks = checks.ToArray(),
                failures = failures
            };
        }

        public static string ToMarkdown(Milestone136EditorLaptopPerformancePowerInvestigationReport report)
        {
            var builder = new StringBuilder(12000);
            builder.AppendLine("# M136 Editor Laptop Performance + Power Investigation Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine($"- Editor comfort target: {report.comfortTargetFrameRate} FPS");
            builder.AppendLine($"- Live captures used: {report.usedLiveCaptures} ({report.liveCaptureScenarioCount}/{report.scenarioManifest?.Length ?? 0} scenarios)");
            builder.AppendLine($"- PDF: `{ReportPdfPath}`");
            builder.AppendLine();
            AppendRenderSnapshot(builder, report.renderSettings);

            builder.AppendLine("## Scenario Manifest");
            builder.AppendLine();
            foreach (var scenario in report.scenarioManifest ?? Array.Empty<M136PerformanceScenarioDefinition>())
            {
                builder.AppendLine($"- `{scenario.id}`: {scenario.displayName} ({scenario.warmupSeconds:0}s warmup, {scenario.sampleSeconds:0}s sample) - {scenario.intent}");
            }

            builder.AppendLine();
            builder.AppendLine("## Scenario Results");
            builder.AppendLine();
            foreach (var scenario in report.scenarios ?? Array.Empty<M136PerformanceScenarioResult>())
            {
                builder.AppendLine($"### {scenario.displayName}");
                builder.AppendLine();
                builder.AppendLine($"- Capture: {scenario.captureMode}");
                builder.AppendLine($"- Live captured: {scenario.liveCaptured}");
                builder.AppendLine($"- Manual capture needed: {scenario.requiresManualCapture}");
                builder.AppendLine($"- Raw samples: {scenario.rawSampleCount}");
                if (!string.IsNullOrWhiteSpace(scenario.artifactDirectory))
                {
                    builder.AppendLine($"- Artifacts: `{scenario.artifactDirectory}`");
                }

                if (!string.IsNullOrWhiteSpace(scenario.profilerTraceNote))
                {
                    builder.AppendLine($"- Profiler trace: {(scenario.profilerTraceSupported ? scenario.profilerTracePath : scenario.profilerTraceNote)}");
                }

                builder.AppendLine($"- Note: {scenario.note}");
                builder.AppendLine($"- Operations: minimap rebuilds {scenario.operations?.miniMapRebuilds ?? 0}, wall updates {scenario.operations?.wallVisibilityUpdates ?? 0}, combat HUD refreshes {scenario.operations?.combatHudRefreshes ?? 0}, runtime NavMesh fallbacks {scenario.operations?.runtimeNavMeshFallbacks ?? 0}, curtain after-ready max frames {scenario.operations?.transitionCurtainMaxFramesAfterReady ?? 0}, transition lock max {scenario.operations?.transitionLockMaxMilliseconds ?? 0f:0.#} ms");
                builder.AppendLine($"- Object peaks: enemies {scenario.objectCounts?.peakEnemies ?? 0}, projectiles {scenario.objectCounts?.peakProjectiles ?? 0}, VFX {scenario.objectCounts?.peakVfx ?? 0}, canvases {scenario.objectCounts?.peakUiCanvases ?? 0}, renderers {scenario.objectCounts?.peakRenderers ?? 0}");
                foreach (var metric in scenario.metrics ?? Array.Empty<M136PerformanceMetricSummary>())
                {
                    builder.AppendLine($"- `{metric.id}`: {MetricSummary(metric)}");
                }

                builder.AppendLine();
            }

            builder.AppendLine("## Ranked Suspects And Suggested Solutions");
            builder.AppendLine();
            foreach (var recommendation in report.recommendations ?? Array.Empty<Milestone136PerformanceRecommendation>())
            {
                builder.AppendLine($"{recommendation.rank}. **{recommendation.title}** (`{recommendation.severity}`, {recommendation.category})");
                builder.AppendLine($"   Evidence: {recommendation.evidence}");
                builder.AppendLine($"   Suggested solution: {recommendation.suggestedAction}");
            }

            builder.AppendLine();
            builder.AppendLine("## Capture Comparison");
            builder.AppendLine();
            foreach (var comparison in report.captureComparisons ?? Array.Empty<M136CaptureComparison>())
            {
                if (!comparison.hasComparison)
                {
                    builder.AppendLine($"- `{comparison.scenarioId}`: {comparison.note}");
                    continue;
                }

                builder.AppendLine($"- `{comparison.scenarioId}`: p95 frame {comparison.previousFrameP95Ms:0.###}ms -> {comparison.latestFrameP95Ms:0.###}ms ({comparison.deltaFrameP95Ms:+0.###;-0.###;0}ms), p50 FPS {comparison.previousFpsP50:0.###} -> {comparison.latestFpsP50:0.###} ({comparison.deltaFpsP50:+0.###;-0.###;0}).");
            }

            builder.AppendLine();
            builder.AppendLine("## Checks");
            builder.AppendLine();
            foreach (var check in report.checks ?? Array.Empty<Milestone136EditorLaptopPerformancePowerInvestigationCheck>())
            {
                builder.AppendLine($"- [{(check.passed ? "PASS" : "FAIL")}] `{check.id}` ({check.category}) - {check.detail}");
            }

            builder.AppendLine();
            builder.AppendLine("## Failures");
            builder.AppendLine();
            if (report.failures == null || report.failures.Length == 0)
            {
                builder.AppendLine("None.");
            }
            else
            {
                foreach (var failure in report.failures)
                {
                    builder.AppendLine($"- {failure}");
                }
            }

            return builder.ToString();
        }

        public static string BuildDocsMarkdown()
        {
            return
                "# M136: Editor Laptop Performance + Power Investigation\n\n" +
                "## Summary\n" +
                "M136 adds an editor/development-only performance investigation harness for the laptop heat and power problem. The target is a cool 60 FPS editor workflow, with evidence gathered before broad visual or gameplay optimizations are applied.\n\n" +
                "## Investigation Runner\n" +
                "- Main menu entry: `Hollow/Performance/Run Editor Laptop Power Investigation`.\n" +
                "- Live capture window: `Hollow/Performance/Live Gameplay Capture`.\n" +
                "- Manual capture records the current Play Mode session under the selected scenario label.\n" +
                "- Automated smoke capture cycles through the six scenario labels against the active Play Mode scene for comparable before/after data.\n" +
                "- Live captures are written under `output/reports/performance/live_captures/<timestamp>/` with manifest JSON, scenario JSON, CSV samples, and profiler trace status.\n" +
                "- The report generator prefers latest live captures and falls back to a deterministic baseline when live samples are missing.\n" +
                "- The runner generates a PDF report plus markdown and JSON under `output/reports`.\n" +
                "- The scenario manifest covers ship hub idle, normal branch idle, active combat room, wave/crowded room, anchor boss smoke, and room transition/NavMesh attach.\n" +
                "- Each scenario uses a fixed `3s` warmup and `30s` sample window.\n" +
                "- The generated lock report includes live scenario evidence where present; deterministic baselines remain clearly labelled when capture is pending.\n\n" +
                "## Metrics\n" +
                "- Frame time and FPS percentiles.\n" +
                "- Main-thread and render-thread timing through Unity `ProfilerRecorder` when supported.\n" +
                "- GPU frame timing is marked unsupported when the current Editor/Metal context does not expose it.\n" +
                "- Profiler traces are saved beside each capture when Unity exposes trace export in the current Editor context.\n" +
                "- GC allocation, managed memory, graphics memory, object-count snapshots, and runtime operation counters.\n" +
                "- Operation counters include minimap rebuilds, wall-visibility updates, combat HUD refreshes, runtime NavMesh fallbacks, and transition curtain/lock readiness.\n\n" +
                "## Capture Comparison\n" +
                "- Reports compare the two latest live captures per scenario when before/after data exists.\n" +
                "- Comparison highlights p95 frame-time and p50 FPS movement so optimization passes can prove actual improvement.\n\n" +
                "## Ranked Suspects\n" +
                "- Desktop profile currently targets `120 FPS` with `vSyncCount = 0`, which can make the laptop run flat out even when gameplay is not heavy.\n" +
                "- PC URP settings are expensive for a cool editor target: HDR, depth texture, opaque texture, 50m shadows, 4 cascades, soft shadows, and additional light shadows are all part of the snapshot.\n" +
                "- Per-frame HUD/debug, minimap, and wall-visibility work should be measured before refactoring.\n" +
                "- Runtime NavMesh fallback is treated as a spike suspect because missing bakes can force dev-only room NavMesh builds.\n\n" +
                "## Deferrals\n" +
                "- M136 does not lower art quality, change gameplay, or alter branch generation.\n" +
                "- Full-run soak, VisionOS-specific tuning, and broad optimization patches are deferred until after the report is reviewed.\n" +
                "- No save schema, economy schema, combat schema, room-generation rule, or gameplay-facing UI changes.\n";
        }

        public static M136RenderSettingsSnapshot BuildRenderSettingsSnapshot()
        {
            var polish = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(WindowsPolishPath);
            var rpAsset = Read(PcRenderPipelineAssetPath);
            var volume = Read(DefaultVolumeProfilePath);
            var camera = Read(WindowsCameraRigPath);
            return new M136RenderSettingsSnapshot
            {
                targetFrameRate = polish != null ? polish.TargetFrameRate : IntYaml(Read(WindowsPolishPath), "targetFrameRate", 0),
                vSyncCount = polish != null ? polish.VSyncCount : IntYaml(Read(WindowsPolishPath), "vSyncCount", -1),
                renderProfileKind = polish != null && polish.RenderProfile != null ? polish.RenderProfile.ProfileKind.ToString() : string.Empty,
                renderProfileName = polish != null && polish.RenderProfile != null ? polish.RenderProfile.name : string.Empty,
                renderPipelineAssetPath = polish != null && polish.RenderProfile != null && polish.RenderProfile.RenderPipelineAsset != null
                    ? AssetDatabase.GetAssetPath(polish.RenderProfile.RenderPipelineAsset)
                    : PcRenderPipelineAssetPath,
                polishRenderScale = polish != null ? polish.RenderScale : FloatYaml(Read(WindowsPolishPath), "renderScale", 0f),
                urpRenderScale = FloatYaml(rpAsset, "m_RenderScale", 0f),
                supportsHdr = BoolYaml(rpAsset, "m_SupportsHDR"),
                requiresDepthTexture = BoolYaml(rpAsset, "m_RequireDepthTexture"),
                requiresOpaqueTexture = BoolYaml(rpAsset, "m_RequireOpaqueTexture"),
                mainLightShadows = BoolYaml(rpAsset, "m_MainLightShadowsSupported"),
                mainLightShadowmapResolution = IntYaml(rpAsset, "m_MainLightShadowmapResolution", 0),
                additionalLightsRenderingMode = IntYaml(rpAsset, "m_AdditionalLightsRenderingMode", 0),
                additionalLightsPerObjectLimit = IntYaml(rpAsset, "m_AdditionalLightsPerObjectLimit", 0),
                additionalLightShadows = BoolYaml(rpAsset, "m_AdditionalLightShadowsSupported"),
                additionalLightsShadowmapResolution = IntYaml(rpAsset, "m_AdditionalLightsShadowmapResolution", 0),
                shadowDistance = FloatYaml(rpAsset, "m_ShadowDistance", 0f),
                shadowCascadeCount = IntYaml(rpAsset, "m_ShadowCascadeCount", 0),
                softShadows = BoolYaml(rpAsset, "m_SoftShadowsSupported"),
                softShadowQuality = IntYaml(rpAsset, "m_SoftShadowQuality", 0),
                srpBatcher = BoolYaml(rpAsset, "m_UseSRPBatcher"),
                dynamicBatching = BoolYaml(rpAsset, "m_SupportsDynamicBatching"),
                cameraHdrAllowed = BoolYaml(camera, "m_HDR"),
                cameraMsaaAllowed = BoolYaml(camera, "m_AllowMSAA"),
                cameraPostProcessing = Read("Assets/Scenes/SampleScene.unity").Contains("m_RenderPostProcessing: 1"),
                bloomConfigured = volume.Contains("m_Name: Bloom"),
                bloomActive = volume.Contains("m_Name: Bloom") && volume.Contains("threshold:"),
                bloomIntensity = FloatAfter(volume, "m_Name: Bloom", "m_Value:", 0f),
                source = $"{WindowsPolishPath}; {PcRenderPipelineAssetPath}; {DefaultVolumeProfilePath}; {WindowsCameraRigPath}"
            };
        }

        private static IEnumerable<M136PerformanceScenarioResult> BuildScenarioResultsPreferLive(
            IEnumerable<M136PerformanceScenarioDefinition> scenarios,
            M136RenderSettingsSnapshot renderSettings,
            IReadOnlyCollection<M136LiveCaptureManifest> latestLiveCaptures)
        {
            var latestByScenario = (latestLiveCaptures ?? Array.Empty<M136LiveCaptureManifest>())
                .GroupBy(manifest => manifest.scenarioId)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
            foreach (var scenario in scenarios)
            {
                if (latestByScenario.TryGetValue(scenario.id, out var manifest))
                {
                    var liveResult = M136LivePerformanceCaptureStore.LoadScenarioResult(manifest);
                    if (liveResult != null && M136EditorLaptopPerformancePolicy.ValidateScenarioResult(liveResult, out _))
                    {
                        yield return liveResult;
                        continue;
                    }
                }

                foreach (var fallback in BuildDeterministicScenarioResults(new[] { scenario }, renderSettings))
                {
                    yield return fallback;
                }
            }
        }

        private static IEnumerable<M136LiveCaptureManifest> LatestManifestByScenario(IEnumerable<M136LiveCaptureManifest> manifests)
        {
            return (manifests ?? Array.Empty<M136LiveCaptureManifest>())
                .Where(manifest => manifest.complete && manifest.liveCaptured)
                .GroupBy(manifest => manifest.scenarioId)
                .Select(group => group.Last())
                .OrderBy(manifest => manifest.scenarioId, StringComparer.Ordinal);
        }

        private static IEnumerable<M136CaptureComparison> BuildCaptureComparisons(
            IEnumerable<M136PerformanceScenarioDefinition> scenarios,
            IEnumerable<M136LiveCaptureManifest> manifests)
        {
            var manifestGroups = (manifests ?? Array.Empty<M136LiveCaptureManifest>())
                .Where(manifest => manifest.complete && manifest.liveCaptured)
                .GroupBy(manifest => manifest.scenarioId)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            foreach (var scenario in scenarios)
            {
                if (!manifestGroups.TryGetValue(scenario.id, out var scenarioManifests) || scenarioManifests.Length < 2)
                {
                    yield return new M136CaptureComparison
                    {
                        scenarioId = scenario.id,
                        displayName = scenario.displayName,
                        hasComparison = false,
                        note = "Need at least two live captures for before/after comparison."
                    };
                    continue;
                }

                var previous = scenarioManifests[^2];
                var latest = scenarioManifests[^1];
                var previousResult = M136LivePerformanceCaptureStore.LoadScenarioResult(previous);
                var latestResult = M136LivePerformanceCaptureStore.LoadScenarioResult(latest);
                var previousFrame = Metric(previousResult, "frame_time_ms");
                var latestFrame = Metric(latestResult, "frame_time_ms");
                var previousFps = Metric(previousResult, "fps");
                var latestFps = Metric(latestResult, "fps");
                yield return new M136CaptureComparison
                {
                    scenarioId = scenario.id,
                    displayName = scenario.displayName,
                    hasComparison = previousFrame != null && latestFrame != null && previousFps != null && latestFps != null,
                    previousCaptureId = previous.captureId,
                    latestCaptureId = latest.captureId,
                    previousFrameP95Ms = previousFrame?.p95 ?? 0d,
                    latestFrameP95Ms = latestFrame?.p95 ?? 0d,
                    deltaFrameP95Ms = (latestFrame?.p95 ?? 0d) - (previousFrame?.p95 ?? 0d),
                    previousFpsP50 = previousFps?.p50 ?? 0d,
                    latestFpsP50 = latestFps?.p50 ?? 0d,
                    deltaFpsP50 = (latestFps?.p50 ?? 0d) - (previousFps?.p50 ?? 0d),
                    note = "Comparison uses the two latest live captures for this scenario."
                };
            }
        }

        private static M136PerformanceMetricSummary Metric(M136PerformanceScenarioResult result, string id)
        {
            return result?.metrics?.FirstOrDefault(metric => metric.id == id && metric.supported);
        }

        private static IEnumerable<M136PerformanceScenarioResult> BuildDeterministicScenarioResults(
            IEnumerable<M136PerformanceScenarioDefinition> scenarios,
            M136RenderSettingsSnapshot renderSettings)
        {
            var currentFrameMs = renderSettings.targetFrameRate > 0
                ? 1000d / renderSettings.targetFrameRate
                : 1000d / M136EditorLaptopPerformancePolicy.ComfortTargetFrameRate;
            foreach (var scenario in scenarios)
            {
                using var sampler = new M136FrameTelemetrySampler();
                for (var index = 0; index < 180; index++)
                {
                    var jitter = (index % 17) * 0.015d;
                    var renderPressure = renderSettings.shadowCascadeCount >= 4 ? 1.2d : 0.5d;
                    sampler.RecordSyntheticFrame(
                        currentFrameMs + jitter,
                        index % 60 == 0 ? 1024d : 0d,
                        Math.Max(0.1d, currentFrameMs * 0.55d),
                        Math.Max(0.1d, currentFrameMs * 0.25d + renderPressure),
                        128d + (index % 9),
                        512d + renderSettings.mainLightShadowmapResolution / 64d);
                }

                var note = "Deterministic editor-lock baseline derived from current frame-rate/render configuration. Run the menu during interactive play to collect live scenario samples.";
                yield return sampler.BuildScenarioResult(scenario, "deterministic-editor-baseline", liveCaptured: false, requiresManualCapture: true, note);
            }
        }

        private static IEnumerable<Milestone136PerformanceRecommendation> BuildRecommendations(M136RenderSettingsSnapshot renderSettings)
        {
            yield return new Milestone136PerformanceRecommendation
            {
                rank = 1,
                id = "fps-vsync-power-cap",
                severity = "Critical",
                category = "Power",
                title = "Add an editor-laptop 60 FPS cap before deeper optimization",
                evidence = $"Windows profile targetFrameRate={renderSettings.targetFrameRate}, vSyncCount={renderSettings.vSyncCount}; M136 comfort target is {M136EditorLaptopPerformancePolicy.ComfortTargetFrameRate} FPS.",
                suggestedAction = "After M136 review, add a dev/editor laptop profile or toggle that caps to 60 FPS and avoids running uncapped/high-refresh during ordinary editor play."
            };

            yield return new Milestone136PerformanceRecommendation
            {
                rank = 2,
                id = "pc-urp-shadow-post-cost",
                severity = "High",
                category = "GPU",
                title = "Create a cooler editor URP profile",
                evidence = $"PC URP snapshot: HDR={renderSettings.supportsHdr}, depth={renderSettings.requiresDepthTexture}, opaque={renderSettings.requiresOpaqueTexture}, shadowDistance={renderSettings.shadowDistance:0.#}, cascades={renderSettings.shadowCascadeCount}, softShadows={renderSettings.softShadows}, additionalLightShadows={renderSettings.additionalLightShadows}.",
                suggestedAction = "Prototype a 60 FPS editor render profile with lower shadow distance/cascades, no additional-light shadows, and disabled depth/opaque/post features unless a scene proves they are needed."
            };

            yield return new Milestone136PerformanceRecommendation
            {
                rank = 3,
                id = "per-frame-hud-minimap-walls",
                severity = "Medium",
                category = "CPU",
                title = "Cadence or event-drive per-frame readability systems",
                evidence = "M136 counters cover minimap rebuilds, wall-visibility updates, and combat HUD refreshes; static inspection shows BranchMiniMapController, RoomWallVisibilityController, and CombatHudController running every frame.",
                suggestedAction = "If live samples confirm CPU or GC pressure, cache Camera.main, avoid rebuilding/debug strings when hidden, and refresh minimap/wall visibility on state/camera changes or a low cadence."
            };

            yield return new Milestone136PerformanceRecommendation
            {
                rank = 4,
                id = "runtime-navmesh-fallback-spikes",
                severity = "Medium",
                category = "CPU Spike",
                title = "Keep runtime rooms on baked NavMesh data",
                evidence = "RoomRuntimeRoot can perform dev-only runtime NavMesh fallback when catalog bakes are missing; M136 counts fallback hits during scenarios.",
                suggestedAction = "Treat any runtime fallback during normal play as a content readiness issue: bake room NavMeshes and make the investigation report flag fallback spikes prominently."
            };
        }

        private static void AddDocsChecks(List<Milestone136EditorLaptopPerformancePowerInvestigationCheck> checks)
        {
            RequireAll(checks, "docs:m136-decisions", "Documentation", Read(DocsPath), new[]
            {
                "Editor Laptop Performance + Power Investigation",
                "`Hollow/Performance/Run Editor Laptop Power Investigation`",
                "`Hollow/Performance/Live Gameplay Capture`",
                "output/reports/performance/live_captures",
                "Profiler traces",
                "Capture Comparison",
                "cool 60 FPS",
                "3s",
                "30s",
                "GPU frame timing",
                "minimap rebuilds",
                "runtime NavMesh fallbacks",
                "120 FPS",
                "vSyncCount = 0",
                "No save schema"
            });
        }

        private static void AddTelemetryChecks(
            List<Milestone136EditorLaptopPerformancePowerInvestigationCheck> checks,
            M136PerformanceScenarioDefinition[] scenarioManifest,
            M136PerformanceScenarioResult[] scenarios,
            M136LiveCaptureManifest[] latestLiveCaptures,
            M136CaptureComparison[] captureComparisons)
        {
            AddCheck(checks, "telemetry:gating", "Telemetry", M136EditorLaptopPerformancePolicy.IsTelemetryAllowed(true, false) && M136EditorLaptopPerformancePolicy.IsTelemetryAllowed(false, true) && !M136EditorLaptopPerformancePolicy.IsTelemetryAllowed(false, false), "Telemetry is editor/development-only.");
            AddCheck(checks, "telemetry:scenario-manifest", "Telemetry", M136EditorLaptopPerformancePolicy.ValidateScenarioManifest(out var detail), detail);
            AddCheck(checks, "telemetry:scenario-results-count", "Telemetry", scenarios.Length == scenarioManifest.Length && scenarios.Length == 6, $"Scenario results: {scenarios.Length}.");
            AddCheck(checks, "telemetry:live-capture-fallback", "Telemetry", scenarios.All(scenario => scenario.liveCaptured || scenario.captureMode == "deterministic-editor-baseline"), $"Live captures present for {latestLiveCaptures.Length}/{scenarioManifest.Length} scenarios; deterministic fallback is labelled when needed.");
            AddCheck(checks, "telemetry:capture-comparisons", "Telemetry", captureComparisons.Length == scenarioManifest.Length, $"Capture comparison rows: {captureComparisons.Length}.");
            foreach (var scenario in scenarios)
            {
                AddCheck(checks, $"telemetry:metrics:{scenario.scenarioId}", "Telemetry", M136EditorLaptopPerformancePolicy.ValidateScenarioResult(scenario, out var scenarioDetail), scenarioDetail);
            }

            RequireAll(checks, "telemetry:api-surface", "Telemetry", Read(TelemetryPath), new[]
            {
                "M136FrameTelemetrySampler",
                "M136LivePerformanceCaptureSession",
                "M136LiveCaptureManifest",
                "M136LivePerformanceCaptureStore",
                "ProfilerRecorder",
                "RecordSyntheticFrame",
                "RecordFrame(float deltaTimeSeconds, M136LiveObjectCountSnapshot objectSnapshot)",
                "gpu_frame_ms",
                "IsTelemetryAllowed",
                "FixedWarmupSeconds = 3f",
                "FixedSampleSeconds = 30f"
            });

            RequireAll(checks, "telemetry:live-capture-window", "Telemetry", Read(LiveCaptureWindowPath), new[]
            {
                "Hollow/Performance/Live Gameplay Capture",
                "Start Manual Capture",
                "Run Automated Smoke",
                "M136LivePerformanceCaptureSession",
                "M136ProfilerTraceRecorder",
                "SaveCapture",
                "Profiler trace"
            });

            RequireAll(checks, "telemetry:operation-counters", "Telemetry", Read(OperationCountersPath), new[]
            {
                "ReportMiniMapRebuild",
                "ReportWallVisibilityUpdate",
                "ReportCombatHudRefresh",
                "ReportRuntimeNavMeshFallback",
                "ReportTransitionCurtainHide",
                "ReportTransitionLock",
                "Conditional(\"UNITY_EDITOR\")",
                "Conditional(\"DEVELOPMENT_BUILD\")"
            });
        }

        private static void AddRenderSnapshotChecks(List<Milestone136EditorLaptopPerformancePowerInvestigationCheck> checks, M136RenderSettingsSnapshot snapshot)
        {
            AddCheck(checks, "render:target-fps-read", "Render Snapshot", snapshot.targetFrameRate > 0, $"Windows targetFrameRate={snapshot.targetFrameRate}.");
            AddCheck(checks, "render:vsync-read", "Render Snapshot", snapshot.vSyncCount >= 0, $"Windows vSyncCount={snapshot.vSyncCount}.");
            AddCheck(checks, "render:urp-depth-opaque-hdr-read", "Render Snapshot", snapshot.supportsHdr && snapshot.requiresDepthTexture && snapshot.requiresOpaqueTexture, "PC URP HDR/depth/opaque texture flags are captured.");
            AddCheck(checks, "render:shadow-stack-read", "Render Snapshot", snapshot.shadowDistance > 0f && snapshot.shadowCascadeCount > 0 && snapshot.mainLightShadowmapResolution > 0, $"Shadow snapshot distance={snapshot.shadowDistance:0.#}, cascades={snapshot.shadowCascadeCount}, main map={snapshot.mainLightShadowmapResolution}.");
        }

        private static void AddRecommendationChecks(List<Milestone136EditorLaptopPerformancePowerInvestigationCheck> checks, Milestone136PerformanceRecommendation[] recommendations)
        {
            var ids = recommendations.Select(recommendation => recommendation.id).ToHashSet();
            foreach (var required in new[] { "fps-vsync-power-cap", "pc-urp-shadow-post-cost", "per-frame-hud-minimap-walls", "runtime-navmesh-fallback-spikes" })
            {
                AddCheck(checks, $"recommendation:{required}", "Recommendations", ids.Contains(required), $"Recommendation `{required}` is present.");
            }
        }

        private static void AddDependencyChecks(List<Milestone136EditorLaptopPerformancePowerInvestigationCheck> checks)
        {
            var report = Read(M135ReportPath);
            AddCheck(
                checks,
                "dependency:m135-passing-report",
                "Dependency",
                report.Contains("- Result: PASSED") && report.Contains(Milestone135FullRunCombatReadinessLockAssetGenerator.LockId),
                "M135 passing report exists and includes the M135 lock id.");
        }

        private static void AddRuntimeHookChecks(List<Milestone136EditorLaptopPerformancePowerInvestigationCheck> checks)
        {
            RequireAll(checks, "hooks:minimap", "Runtime Hooks", Read(MiniMapControllerPath), new[] { "ReportMiniMapRebuild", "RebuildShapeMap" });
            RequireAll(checks, "hooks:walls", "Runtime Hooks", Read(WallVisibilityControllerPath), new[] { "ReportWallVisibilityUpdate", "ApplyVisibility(Camera camera)" });
            RequireAll(checks, "hooks:combat-hud", "Runtime Hooks", Read(CombatHudControllerPath), new[] { "ReportCombatHudRefresh", "Refresh()" });
            RequireAll(checks, "hooks:navmesh", "Runtime Hooks", Read(RoomRuntimeRootPath), new[] { "ReportRuntimeNavMeshFallback", "DevRuntime" });
        }

        private static void AppendRenderSnapshot(StringBuilder builder, M136RenderSettingsSnapshot snapshot)
        {
            builder.AppendLine("## Render And Power Snapshot");
            builder.AppendLine();
            builder.AppendLine($"- Windows target FPS: {snapshot.targetFrameRate}");
            builder.AppendLine($"- vSync count: {snapshot.vSyncCount}");
            if (!string.IsNullOrWhiteSpace(snapshot.renderProfileKind))
            {
                builder.AppendLine($"- Render profile: {snapshot.renderProfileKind} ({snapshot.renderProfileName})");
                builder.AppendLine($"- Render pipeline asset: `{snapshot.renderPipelineAssetPath}`");
            }

            builder.AppendLine($"- Polish render scale: {snapshot.polishRenderScale:0.##}");
            builder.AppendLine($"- URP render scale: {snapshot.urpRenderScale:0.##}");
            builder.AppendLine($"- HDR/depth/opaque: {snapshot.supportsHdr}/{snapshot.requiresDepthTexture}/{snapshot.requiresOpaqueTexture}");
            builder.AppendLine($"- Main shadows: {snapshot.mainLightShadows}, map {snapshot.mainLightShadowmapResolution}");
            builder.AppendLine($"- Additional lights: mode {snapshot.additionalLightsRenderingMode}, per object {snapshot.additionalLightsPerObjectLimit}, shadows {snapshot.additionalLightShadows}");
            builder.AppendLine($"- Shadow distance/cascades/soft quality: {snapshot.shadowDistance:0.#}m / {snapshot.shadowCascadeCount} / {snapshot.softShadowQuality}");
            builder.AppendLine($"- Camera HDR/MSAA/post: {snapshot.cameraHdrAllowed}/{snapshot.cameraMsaaAllowed}/{snapshot.cameraPostProcessing}");
            builder.AppendLine();
        }

        private static string MetricSummary(M136PerformanceMetricSummary metric)
        {
            if (!metric.supported)
            {
                return $"unsupported or pending ({metric.note})";
            }

            return $"samples={metric.sampleCount}, avg={metric.average:0.###}{metric.unit}, p50={metric.p50:0.###}{metric.unit}, p95={metric.p95:0.###}{metric.unit}, max={metric.max:0.###}{metric.unit}";
        }

        private static void WritePdf(string path, Milestone136EditorLaptopPerformancePowerInvestigationReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "output/reports");
            var lines = BuildPdfLines(report).ToList();
            M136SimplePdfWriter.Write(path, lines);
        }

        private static IEnumerable<string> BuildPdfLines(Milestone136EditorLaptopPerformancePowerInvestigationReport report)
        {
            yield return "Hollow Soul - M136 Editor Laptop Performance + Power Investigation";
            yield return $"Result: {report.result}";
            yield return $"Generated: {report.generatedAtUtc}";
            yield return $"Comfort target: {report.comfortTargetFrameRate} FPS";
            yield return $"Lock id: {report.lockId}";
            yield return $"Live captures used: {report.usedLiveCaptures} ({report.liveCaptureScenarioCount}/{report.scenarioManifest?.Length ?? 0} scenarios)";
            yield return string.Empty;
            yield return "Render Snapshot";
            yield return $"Windows target FPS {report.renderSettings.targetFrameRate}; vSync {report.renderSettings.vSyncCount}; URP render scale {report.renderSettings.urpRenderScale:0.##}";
            yield return $"HDR {report.renderSettings.supportsHdr}; depth {report.renderSettings.requiresDepthTexture}; opaque {report.renderSettings.requiresOpaqueTexture}";
            yield return $"Shadow distance {report.renderSettings.shadowDistance:0.#}m; cascades {report.renderSettings.shadowCascadeCount}; soft shadows {report.renderSettings.softShadows}; additional light shadows {report.renderSettings.additionalLightShadows}";
            yield return string.Empty;
            yield return "Ranked Suspects And Suggested Solutions";
            foreach (var recommendation in report.recommendations ?? Array.Empty<Milestone136PerformanceRecommendation>())
            {
                yield return $"{recommendation.rank}. {recommendation.title} [{recommendation.severity} / {recommendation.category}]";
                yield return $"Evidence: {recommendation.evidence}";
                yield return $"Solution: {recommendation.suggestedAction}";
                yield return string.Empty;
            }

            yield return "Representative Scenario Results";
            foreach (var scenario in report.scenarios ?? Array.Empty<M136PerformanceScenarioResult>())
            {
                var frameMetric = scenario.metrics?.FirstOrDefault(metric => metric.id == "frame_time_ms");
                var fpsMetric = scenario.metrics?.FirstOrDefault(metric => metric.id == "fps");
                yield return $"{scenario.displayName}: {scenario.captureMode}; live {scenario.liveCaptured}; samples {scenario.rawSampleCount}; frame p95 {frameMetric?.p95:0.###} ms; fps p50 {fpsMetric?.p50:0.###}";
            }

            yield return string.Empty;
            yield return "Capture Comparison";
            foreach (var comparison in report.captureComparisons ?? Array.Empty<M136CaptureComparison>())
            {
                yield return comparison.hasComparison
                    ? $"{comparison.displayName}: p95 delta {comparison.deltaFrameP95Ms:+0.###;-0.###;0} ms; FPS delta {comparison.deltaFpsP50:+0.###;-0.###;0}"
                    : $"{comparison.displayName}: {comparison.note}";
            }

            yield return string.Empty;
            yield return "Notes";
            yield return "GPU timing may be unsupported in some Unity Editor/Metal contexts and is marked explicitly instead of failing the report.";
            yield return "M136 measures first. Broad quality cuts, full-run soak, and VisionOS-specific tuning are deferred to the next optimization pass.";
        }

        private static void RequireAll(List<Milestone136EditorLaptopPerformancePowerInvestigationCheck> checks, string id, string category, string content, IEnumerable<string> needles)
        {
            var missing = needles.Where(needle => !content.Contains(needle)).ToArray();
            AddCheck(checks, id, category, missing.Length == 0, missing.Length == 0 ? "Required strings found." : $"Missing: {string.Join(", ", missing)}.");
        }

        private static void AddCheck(List<Milestone136EditorLaptopPerformancePowerInvestigationCheck> checks, string id, string category, bool passed, string detail)
        {
            checks.Add(new Milestone136EditorLaptopPerformancePowerInvestigationCheck
            {
                id = id,
                category = category,
                passed = passed,
                detail = detail
            });
        }

        private static bool BoolYaml(string content, string key)
        {
            return IntYaml(content, key, 0) != 0;
        }

        private static int IntYaml(string content, string key, int fallback)
        {
            var value = RawYamlValue(content, key);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        private static float FloatYaml(string content, string key, float fallback)
        {
            var value = RawYamlValue(content, key);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        private static float FloatAfter(string content, string section, string key, float fallback)
        {
            var sectionIndex = content.IndexOf(section, StringComparison.Ordinal);
            if (sectionIndex < 0)
            {
                return fallback;
            }

            var keyIndex = content.IndexOf(key, sectionIndex, StringComparison.Ordinal);
            if (keyIndex < 0)
            {
                return fallback;
            }

            var lineEnd = content.IndexOf('\n', keyIndex);
            var raw = (lineEnd >= 0 ? content.Substring(keyIndex, lineEnd - keyIndex) : content[keyIndex..]).Replace(key, string.Empty).Trim();
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        private static string RawYamlValue(string content, string key)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            var token = $"{key}:";
            var index = content.IndexOf(token, StringComparison.Ordinal);
            if (index < 0)
            {
                return string.Empty;
            }

            var start = index + token.Length;
            var lineEnd = content.IndexOf('\n', start);
            return (lineEnd >= 0 ? content.Substring(start, lineEnd - start) : content[start..]).Trim();
        }

        private static string Read(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static class M136SimplePdfWriter
        {
            private const float PageWidth = 612f;
            private const float PageHeight = 792f;
            private const float Left = 48f;
            private const float Top = 742f;
            private const float LineHeight = 14f;
            private const int MaxLineLength = 92;

            public static void Write(string path, IReadOnlyList<string> sourceLines)
            {
                var pages = Paginate(sourceLines, 48).ToList();
                File.WriteAllBytes(path, BuildPdfBytes(pages));
            }

            private static IEnumerable<List<string>> Paginate(IReadOnlyList<string> sourceLines, int maxLinesPerPage)
            {
                var page = new List<string>();
                foreach (var sourceLine in sourceLines)
                {
                    foreach (var wrapped in Wrap(sourceLine))
                    {
                        if (page.Count >= maxLinesPerPage)
                        {
                            yield return page;
                            page = new List<string>();
                        }

                        page.Add(wrapped);
                    }
                }

                if (page.Count > 0)
                {
                    yield return page;
                }
            }

            private static IEnumerable<string> Wrap(string line)
            {
                line = Sanitize(line);
                if (line.Length <= MaxLineLength)
                {
                    yield return line;
                    yield break;
                }

                var index = 0;
                while (index < line.Length)
                {
                    var length = Math.Min(MaxLineLength, line.Length - index);
                    if (index + length < line.Length)
                    {
                        var breakAt = line.LastIndexOf(' ', index + length, length);
                        if (breakAt > index + 24)
                        {
                            length = breakAt - index;
                        }
                    }

                    yield return line.Substring(index, length).TrimEnd();
                    index += length;
                    while (index < line.Length && line[index] == ' ')
                    {
                        index++;
                    }
                }
            }

            private static byte[] BuildPdfBytes(IReadOnlyList<List<string>> pages)
            {
                var objects = new List<string>();
                var pageObjectIds = new List<int>();
                objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
                objects.Add(string.Empty);
                objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

                foreach (var pageLines in pages)
                {
                    var contentObjectId = objects.Count + 1;
                    var pageObjectId = objects.Count + 2;
                    pageObjectIds.Add(pageObjectId);
                    var stream = BuildPageStream(pageLines);
                    objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
                    objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth.ToString(CultureInfo.InvariantCulture)} {PageHeight.ToString(CultureInfo.InvariantCulture)}] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectId} 0 R >>");
                }

                objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>";

                var builder = new StringBuilder();
                var offsets = new List<int> { 0 };
                builder.Append("%PDF-1.4\n");
                for (var index = 0; index < objects.Count; index++)
                {
                    offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
                    builder.Append(index + 1).Append(" 0 obj\n");
                    builder.Append(objects[index]).Append('\n');
                    builder.Append("endobj\n");
                }

                var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
                builder.Append("xref\n");
                builder.Append("0 ").Append(objects.Count + 1).Append('\n');
                builder.Append("0000000000 65535 f \n");
                for (var index = 1; index < offsets.Count; index++)
                {
                    builder.Append(offsets[index].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
                }

                builder.Append("trailer\n");
                builder.Append("<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
                builder.Append("startxref\n");
                builder.Append(xrefOffset).Append('\n');
                builder.Append("%%EOF\n");
                return Encoding.ASCII.GetBytes(builder.ToString());
            }

            private static string BuildPageStream(IReadOnlyList<string> lines)
            {
                var builder = new StringBuilder();
                builder.Append("BT\n");
                builder.Append("/F1 10 Tf\n");
                builder.Append(Left.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append(Top.ToString(CultureInfo.InvariantCulture)).Append(" Td\n");
                for (var index = 0; index < lines.Count; index++)
                {
                    if (index > 0)
                    {
                        builder.Append("0 -").Append(LineHeight.ToString(CultureInfo.InvariantCulture)).Append(" Td\n");
                    }

                    builder.Append('(').Append(EscapePdf(lines[index])).Append(") Tj\n");
                }

                builder.Append("ET");
                return builder.ToString();
            }

            private static string Sanitize(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }

                var builder = new StringBuilder(value.Length);
                foreach (var character in value)
                {
                    builder.Append(character is >= ' ' and <= '~' ? character : '-');
                }

                return builder.ToString();
            }

            private static string EscapePdf(string value)
            {
                return Sanitize(value).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            }
        }
    }
}
