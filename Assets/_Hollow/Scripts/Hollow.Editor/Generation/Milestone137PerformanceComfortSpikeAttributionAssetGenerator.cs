using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone137PerformanceComfortSpikeAttributionReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public bool passed;
        public string result;
        public int totalChecks;
        public int passedChecks;
        public int windowsTargetFrameRate;
        public int windowsVSyncCount;
        public int baselineCaptureCount;
        public int validBaselineCaptureCount;
        public M137CaptureSummary[] captures;
        public M137Check[] checks;
        public string[] recaptureChecklist;
        public string[] failures;
    }

    [Serializable]
    public sealed class M137CaptureSummary
    {
        public string scenarioId;
        public string captureId;
        public string validityGrade;
        public string frameCadenceConfidence;
        public double sampleRateHz;
        public int sampleCount;
        public int enemies;
        public int projectiles;
        public int vfx;
        public int renderers;
        public int wallVisibilityUpdates;
        public int combatHudRefreshes;
        public int miniMapRebuilds;
        public int roomTransitionEvents;
        public int runtimeNavMeshFallbacks;
        public double mainThreadP95Ms;
        public double gpuFrameP95Ms;
        public double frameMaxMs;
        public double gcAverageKb;
        public double gcMaxKb;
    }

    [Serializable]
    public sealed class M137Check
    {
        public string id;
        public bool passed;
        public string detail;
    }

    public static class Milestone137PerformanceComfortSpikeAttributionAssetGenerator
    {
        public const string LockId = M137PerformanceComfortPolicy.LockId;
        public const string Title = "M137 Performance Comfort + Spike Attribution";
        public const string DocsPath = "Docs/Milestone137PerformanceComfortSpikeAttribution.md";
        public const string ReportMarkdownPath = "output/reports/m137_performance_comfort_spike_attribution.md";
        public const string ReportJsonPath = "output/reports/m137_performance_comfort_spike_attribution.json";
        public const string ReportPdfPath = "output/reports/m137_performance_comfort_spike_attribution.pdf";

        private const string WindowsPolishPath = "Assets/_Hollow/Data/Platform/Polish/PlatformPolish_WindowsStandard3D.asset";
        private const string VisionOSImmersivePolishPath = "Assets/_Hollow/Data/Platform/Polish/PlatformPolish_VisionOSImmersive.asset";
        private const string M136BReportPath = "output/reports/m136b_corrected_live_performance_capture.json";
        private const string PolicyPath = "Assets/_Hollow/Scripts/Hollow.Core/M137PerformanceComfortPolicy.cs";
        private const string WallPath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomWallVisibilityController.cs";
        private const string CombatHudPath = "Assets/_Hollow/Scripts/Hollow.Combat/CombatHudController.cs";
        private const string BossHudPath = "Assets/_Hollow/Scripts/Hollow.Combat/BossHudController.cs";
        private const string TelemetryPath = "Assets/_Hollow/Scripts/Hollow.Diagnostics/M136EditorLaptopPerformanceTelemetry.cs";

        [MenuItem("Hollow/Performance/Generate M137 Performance Comfort + Spike Attribution Report")]
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

        public static Milestone137PerformanceComfortSpikeAttributionReport BuildReport(string liveCaptureRoot = M136LivePerformanceCaptureStore.LiveCaptureRoot)
        {
            var windowsProfile = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(WindowsPolishPath);
            var captures = LoadLatestCaptureSummaries(liveCaptureRoot);
            var checks = BuildChecks(windowsProfile, captures).ToArray();
            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();
            return new Milestone137PerformanceComfortSpikeAttributionReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                passed = failures.Length == 0,
                result = failures.Length == 0 ? "PASSED" : "FAILED",
                totalChecks = checks.Length,
                passedChecks = checks.Count(check => check.passed),
                windowsTargetFrameRate = windowsProfile != null ? windowsProfile.TargetFrameRate : 0,
                windowsVSyncCount = windowsProfile != null ? windowsProfile.VSyncCount : -1,
                baselineCaptureCount = captures.Length,
                validBaselineCaptureCount = captures.Count(capture => capture.validityGrade == M136CaptureValidityClassifier.Valid),
                captures = captures,
                checks = checks,
                recaptureChecklist = RecaptureChecklist(),
                failures = failures
            };
        }

        public static string ToMarkdown(Milestone137PerformanceComfortSpikeAttributionReport report)
        {
            var builder = new StringBuilder(12000);
            builder.AppendLine("# M137 Performance Comfort + Spike Attribution");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks}");
            builder.AppendLine($"- Windows target FPS: {report.windowsTargetFrameRate}");
            builder.AppendLine($"- Windows vSync: {report.windowsVSyncCount}");
            builder.AppendLine($"- Baseline captures: {report.validBaselineCaptureCount}/{report.baselineCaptureCount} valid");
            builder.AppendLine();
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine("- Windows standard play is locked to the 60 FPS comfort target.");
            builder.AppendLine("- Wall visibility, combat HUD, and boss HUD now have cadence/change gates instead of unconditional frame-by-frame UI/material writes.");
            builder.AppendLine("- Capture samples use value-type frame records with preallocated buffers so future GC readings are less contaminated by the capture tool.");
            builder.AppendLine("- Profiler markers were added for wall visibility, combat HUD, boss HUD, room transitions, boss spawn/activation, and minimap rebuilds.");
            builder.AppendLine();
            builder.AppendLine("## Latest M136B Baseline Captures");
            builder.AppendLine();
            builder.AppendLine("| Scenario | Valid | Hz | Main p95 | GPU p95 | Max frame | GC avg/max KB | Wall/HUD/Mini | Trans/Nav |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (var capture in report.captures ?? Array.Empty<M137CaptureSummary>())
            {
                builder.AppendLine($"| `{capture.scenarioId}` | {capture.validityGrade} | {capture.sampleRateHz:0.#} | {capture.mainThreadP95Ms:0.#} | {capture.gpuFrameP95Ms:0.#} | {capture.frameMaxMs:0.#} | {capture.gcAverageKb:0.#}/{capture.gcMaxKb:0.#} | {capture.wallVisibilityUpdates}/{capture.combatHudRefreshes}/{capture.miniMapRebuilds} | {capture.roomTransitionEvents}/{capture.runtimeNavMeshFallbacks} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Post-Fix Recapture Checklist");
            builder.AppendLine();
            foreach (var item in report.recaptureChecklist ?? Array.Empty<string>())
            {
                builder.AppendLine($"- [ ] {item}");
            }

            builder.AppendLine();
            builder.AppendLine("## Checks");
            builder.AppendLine();
            foreach (var check in report.checks ?? Array.Empty<M137Check>())
            {
                builder.AppendLine($"- [{(check.passed ? "PASS" : "FAIL")}] `{check.id}` - {check.detail}");
            }

            builder.AppendLine();
            builder.AppendLine("## Failures");
            builder.AppendLine(report.failures == null || report.failures.Length == 0
                ? "None."
                : string.Join("\n", report.failures.Select(failure => $"- {failure}")));
            return builder.ToString();
        }

        public static string BuildDocsMarkdown()
        {
            return
                "# M137: Performance Comfort + Spike Attribution\n\n" +
                "## Summary\n" +
                "M137 is the first low-risk optimization pass after M136B corrected live capture. It locks Windows standard play to 60 FPS, reduces obvious per-frame HUD/wall churn, and adds attribution markers for boss and transition spikes.\n\n" +
                "## Locked Runtime Policy\n" +
                $"- Windows standard target frame rate: `{M137PerformanceComfortPolicy.WindowsComfortTargetFrameRate}`.\n" +
                $"- Wall visibility max refresh: `{M137PerformanceComfortPolicy.WallVisibilityMaxRefreshHz} Hz`.\n" +
                $"- Combat HUD max refresh: `{M137PerformanceComfortPolicy.CombatHudMaxRefreshHz} Hz`.\n" +
                $"- Boss HUD max refresh: `{M137PerformanceComfortPolicy.BossHudMaxRefreshHz} Hz`.\n" +
                "- VisionOS profiles retain their existing frame targets.\n\n" +
                "## Measurement Policy\n" +
                "- M136B live capture remains the source of truth for before/after evidence.\n" +
                "- M137 adds profiler markers for wall visibility, combat HUD, boss HUD, room transitions, boss spawn/activation, and minimap rebuilds.\n" +
                "- Profiler traces stay optional and should be used only for focused boss/transition spike investigation.\n\n" +
                "## Deferrals\n" +
                "- No URP/shadow/lighting quality changes are applied in M137.\n" +
                "- No gameplay, reward, economy, save-data, combat stat, or room-generation behavior changes are applied in M137.\n";
        }

        private static M137CaptureSummary[] LoadLatestCaptureSummaries(string liveCaptureRoot)
        {
            return M136LivePerformanceCaptureStore.LoadManifests(liveCaptureRoot)
                .Where(manifest => manifest.complete && manifest.liveCaptured)
                .GroupBy(manifest => manifest.scenarioId)
                .Select(group => group.Last())
                .OrderBy(manifest => manifest.scenarioId, StringComparer.Ordinal)
                .Select(BuildCaptureSummary)
                .Where(summary => summary != null)
                .ToArray();
        }

        private static M137CaptureSummary BuildCaptureSummary(M136LiveCaptureManifest manifest)
        {
            var result = M136LivePerformanceCaptureStore.LoadScenarioResult(manifest);
            if (result == null)
            {
                return null;
            }

            var validity = M136CaptureValidityClassifier.Classify(manifest, result);
            return new M137CaptureSummary
            {
                scenarioId = manifest.scenarioId,
                captureId = manifest.captureId,
                validityGrade = validity.Grade,
                frameCadenceConfidence = result.frameCadenceConfidence,
                sampleRateHz = result.sampleRateHz,
                sampleCount = result.rawSampleCount,
                enemies = result.objectCounts?.peakEnemies ?? 0,
                projectiles = result.objectCounts?.peakProjectiles ?? 0,
                vfx = result.objectCounts?.peakVfx ?? 0,
                renderers = result.objectCounts?.peakRenderers ?? 0,
                wallVisibilityUpdates = result.operations?.wallVisibilityUpdates ?? 0,
                combatHudRefreshes = result.operations?.combatHudRefreshes ?? 0,
                miniMapRebuilds = result.operations?.miniMapRebuilds ?? 0,
                roomTransitionEvents = result.operations?.roomTransitionEvents ?? 0,
                runtimeNavMeshFallbacks = result.operations?.runtimeNavMeshFallbacks ?? 0,
                mainThreadP95Ms = Metric(result, "main_thread_ms").p95,
                gpuFrameP95Ms = Metric(result, "gpu_frame_ms").p95,
                frameMaxMs = Metric(result, "frame_time_ms").max,
                gcAverageKb = Metric(result, "gc_allocated_bytes").average / 1024d,
                gcMaxKb = Metric(result, "gc_allocated_bytes").max / 1024d
            };
        }

        private static M136PerformanceMetricSummary Metric(M136PerformanceScenarioResult result, string id)
        {
            return (result.metrics ?? Array.Empty<M136PerformanceMetricSummary>()).FirstOrDefault(metric => metric.id == id) ??
                new M136PerformanceMetricSummary { id = id };
        }

        private static IEnumerable<M137Check> BuildChecks(PlatformPolishProfileDefinition windowsProfile, M137CaptureSummary[] captures)
        {
            var visionProfile = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(VisionOSImmersivePolishPath);
            yield return Check("m136b-dependency", M136BIsPassing(), "M136B corrected live capture report is present and passing.");
            yield return Check("windows-60fps", windowsProfile != null && windowsProfile.TargetFrameRate == M137PerformanceComfortPolicy.WindowsComfortTargetFrameRate && windowsProfile.VSyncCount == 0, $"Windows profile target={windowsProfile?.TargetFrameRate ?? 0}, vSync={windowsProfile?.VSyncCount ?? -1}.");
            yield return Check("visionos-unchanged", visionProfile != null && visionProfile.TargetFrameRate == 90, $"VisionOS immersive target={visionProfile?.TargetFrameRate ?? 0}.");
            yield return Check("policy-constants", Read(PolicyPath).Contains("WallVisibilityMaxRefreshHz = 10") && Read(PolicyPath).Contains("BossHudMaxRefreshHz = 15"), "M137 comfort constants are present.");
            yield return Check("wall-cadence", Read(WallPath).Contains("ShouldRefresh") && Read(WallPath).Contains("WallVisibilityMinRefreshIntervalSeconds") && Read(WallPath).Contains("WallVisibilityRefresh"), "Wall visibility has dirty/throttled refresh and profiler marker.");
            yield return Check("combat-hud-cadence", Read(CombatHudPath).Contains("CombatHudMinRefreshIntervalSeconds") && Read(CombatHudPath).Contains("ModelsEquivalent") && Read(CombatHudPath).Contains("CombatHudRefresh"), "Combat HUD has cadence/change gate and profiler marker.");
            yield return Check("boss-hud-cadence", Read(BossHudPath).Contains("BossHudMinRefreshIntervalSeconds") && Read(BossHudPath).Contains("BossHudRefresh"), "Boss HUD has cadence/change gate and profiler marker.");
            yield return Check("capture-value-samples", typeof(M136LiveFrameSample).IsValueType && Read(TelemetryPath).Contains("FrameSampleCapacity"), "Live capture frame samples are value-type and expose capacity.");
            yield return Check("capture-preallocation", new M136FrameTelemetrySampler(256).FrameSampleCapacity >= 256, "Live capture sampler preallocates expected sample buffers.");
            yield return Check("profiler-markers", Read(PolicyPath).Contains("RoomTransitionLoad") && Read(PolicyPath).Contains("BossSpawnActivate") && Read(PolicyPath).Contains("MiniMapRebuild"), "M137 profiler markers cover spike suspects.");
            yield return Check("baseline-valid-captures", captures.Length >= 6 && captures.All(capture => capture.validityGrade == M136CaptureValidityClassifier.Valid), $"Latest valid baseline captures: {captures.Count(capture => capture.validityGrade == M136CaptureValidityClassifier.Valid)}/{captures.Length}.");
        }

        private static bool M136BIsPassing()
        {
            if (!File.Exists(M136BReportPath))
            {
                return false;
            }

            var report = JsonUtility.FromJson<Milestone136BCorrectedLivePerformanceCaptureReport>(File.ReadAllText(M136BReportPath));
            return report != null &&
                report.passed &&
                report.lockId == Milestone136BCorrectedLivePerformanceCaptureAssetGenerator.LockId &&
                report.validCaptureCount >= 6;
        }

        private static string[] RecaptureChecklist()
        {
            return M136EditorLaptopPerformancePolicy.ScenarioManifest
                .Select(scenario => $"{scenario.displayName}: recapture after M137 with trace off; use focused trace only if spikes remain.")
                .ToArray();
        }

        private static M137Check Check(string id, bool passed, string detail)
        {
            return new M137Check
            {
                id = id,
                passed = passed,
                detail = detail
            };
        }

        private static string Read(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static void WritePdf(string path, Milestone137PerformanceComfortSpikeAttributionReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "output/reports");
            M137SimplePdfWriter.Write(path, BuildPdfLines(report).ToArray());
        }

        private static IEnumerable<string> BuildPdfLines(Milestone137PerformanceComfortSpikeAttributionReport report)
        {
            yield return "Hollow Soul - M137 Performance Comfort + Spike Attribution";
            yield return $"Result: {report.result}";
            yield return $"Lock id: {report.lockId}";
            yield return $"Windows target FPS: {report.windowsTargetFrameRate}; vSync: {report.windowsVSyncCount}";
            yield return $"Checks: {report.passedChecks}/{report.totalChecks}; valid baseline captures: {report.validBaselineCaptureCount}/{report.baselineCaptureCount}";
            yield return string.Empty;
            yield return "What changed";
            yield return "Windows standard play now targets 60 FPS for cooler laptop development and predictable desktop play.";
            yield return "Wall visibility and combat/boss HUD refreshes are no longer unconditional frame-by-frame writes.";
            yield return "Capture samples use value-type records and preallocated buffers to reduce measurement overhead.";
            yield return "Profiler markers identify wall, HUD, boss, transition, and minimap spike candidates.";
            yield return string.Empty;
            yield return "Latest baseline captures";
            foreach (var capture in report.captures ?? Array.Empty<M137CaptureSummary>())
            {
                yield return $"{capture.scenarioId}: {capture.validityGrade}, {capture.sampleRateHz:0.#} Hz, main p95 {capture.mainThreadP95Ms:0.#} ms, gpu p95 {capture.gpuFrameP95Ms:0.#} ms, max {capture.frameMaxMs:0.#} ms, wall/hud/mini {capture.wallVisibilityUpdates}/{capture.combatHudRefreshes}/{capture.miniMapRebuilds}";
            }

            yield return string.Empty;
            yield return "Post-fix recapture checklist";
            foreach (var item in report.recaptureChecklist ?? Array.Empty<string>())
            {
                yield return $"- {item}";
            }
        }

        private static class M137SimplePdfWriter
        {
            private const float PageWidth = 612f;
            private const float PageHeight = 792f;
            private const float Left = 48f;
            private const float Top = 742f;
            private const float LineHeight = 14f;
            private const int MaxLineLength = 92;

            public static void Write(string path, IReadOnlyList<string> sourceLines)
            {
                File.WriteAllBytes(path, BuildPdfBytes(Paginate(sourceLines, 48).ToArray()));
            }

            private static IEnumerable<List<string>> Paginate(IReadOnlyList<string> sourceLines, int maxLinesPerPage)
            {
                var page = new List<string>();
                foreach (var sourceLine in sourceLines)
                {
                    foreach (var line in Wrap(sourceLine))
                    {
                        if (page.Count >= maxLinesPerPage)
                        {
                            yield return page;
                            page = new List<string>();
                        }

                        page.Add(line);
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
                objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
                objects.Add($"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(0, pages.Count).Select(index => $"{3 + index * 2} 0 R"))}] /Count {pages.Count} >>");
                for (var index = 0; index < pages.Count; index++)
                {
                    var pageObject = 3 + index * 2;
                    var contentObject = pageObject + 1;
                    objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] /Resources << /Font << /F1 {3 + pages.Count * 2} 0 R >> >> /Contents {contentObject} 0 R >>");
                    var stream = BuildPageStream(pages[index]);
                    objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream");
                }

                objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
                var builder = new StringBuilder();
                builder.Append("%PDF-1.4\n");
                var offsets = new List<int> { 0 };
                for (var index = 0; index < objects.Count; index++)
                {
                    offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
                    builder.Append(index + 1).Append(" 0 obj\n").Append(objects[index]).Append("\nendobj\n");
                }

                var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
                builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n");
                builder.Append("0000000000 65535 f \n");
                foreach (var offset in offsets.Skip(1))
                {
                    builder.Append(offset.ToString("0000000000", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
                }

                builder.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
                builder.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");
                return Encoding.ASCII.GetBytes(builder.ToString());
            }

            private static string BuildPageStream(IReadOnlyList<string> lines)
            {
                var builder = new StringBuilder();
                builder.Append("BT\n/F1 10 Tf\n");
                for (var index = 0; index < lines.Count; index++)
                {
                    var y = Top - index * LineHeight;
                    builder.Append("1 0 0 1 ").Append(Left.ToString("0.#", CultureInfo.InvariantCulture)).Append(' ')
                        .Append(y.ToString("0.#", CultureInfo.InvariantCulture)).Append(" Tm\n(")
                        .Append(Escape(lines[index])).Append(") Tj\n");
                }

                builder.Append("ET\n");
                return builder.ToString();
            }

            private static string Escape(string value)
            {
                return value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            }

            private static string Sanitize(string value)
            {
                var builder = new StringBuilder(value?.Length ?? 0);
                foreach (var character in value ?? string.Empty)
                {
                    builder.Append(character >= 32 && character <= 126 ? character : '-');
                }

                return builder.ToString();
            }
        }
    }
}
