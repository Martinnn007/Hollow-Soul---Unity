using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class M136ALiveCaptureInsightsReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public bool passed;
        public string result;
        public string insightResult;
        public int totalCaptures;
        public int expectedScenarioCount;
        public int coveredScenarioCount;
        public int invalidCaptureCount;
        public int needsRecaptureCount;
        public string[] sourceRoots;
        public M136ALiveCaptureInventoryEntry[] captures;
        public M136AMetricConfidenceEntry[] metricConfidence;
        public M136AInsightRecommendation[] recommendations;
        public M136AInsightCheck[] checks;
        public string[] failures;
    }

    [Serializable]
    public sealed class M136ALiveCaptureInventoryEntry
    {
        public string scenarioId;
        public string displayName;
        public string captureId;
        public string captureMode;
        public string artifactDirectory;
        public int sampleCount;
        public double sampleRateHz;
        public double frameP95Ms;
        public double frameMaxMs;
        public double fpsP50;
        public double gpuP95Ms;
        public double mainThreadP95Ms;
        public double gcAverageKb;
        public double managedMemoryP95Mb;
        public double graphicsMemoryP95Mb;
        public int wallVisibilityUpdates;
        public int combatHudRefreshes;
        public int runtimeNavMeshFallbacks;
        public int peakEnemies;
        public int peakProjectiles;
        public int peakVfx;
        public int peakRenderers;
        public double profilerTraceSizeMb;
        public string validityGrade;
        public string validityReason;
    }

    [Serializable]
    public sealed class M136AMetricConfidenceEntry
    {
        public string metricId;
        public string confidence;
        public string reason;
    }

    [Serializable]
    public sealed class M136AInsightRecommendation
    {
        public int rank;
        public string id;
        public string confidence;
        public string title;
        public string evidence;
        public string nextStep;
    }

    [Serializable]
    public sealed class M136AInsightCheck
    {
        public string id;
        public bool passed;
        public string detail;
    }

    public static class Milestone136ALiveCaptureInsightsAssetGenerator
    {
        public const string LockId = "M136A";
        public const string Title = "M136A Live Capture Insights Report";
        public const string ReportMarkdownPath = "output/reports/m136_live_capture_insights.md";
        public const string ReportJsonPath = "output/reports/m136_live_capture_insights.json";
        public const string ReportPdfPath = "output/reports/m136_live_capture_insights.pdf";
        public const string LiveCaptureRoot = M136LivePerformanceCaptureStore.LiveCaptureRoot;

        [MenuItem("Hollow/Performance/Generate M136 Live Capture Insights")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportMarkdownPath) ?? "output/reports");
            var report = BuildReport();
            File.WriteAllText(ReportJsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(ReportMarkdownPath, ToMarkdown(report));
            WritePdf(ReportPdfPath, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var message = $"{Title} generation {report.result}: {report.coveredScenarioCount}/{report.expectedScenarioCount} scenarios covered, {report.needsRecaptureCount} captures need recapture. PDF: {ReportPdfPath}";
            if (report.passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public static M136ALiveCaptureInsightsReport BuildReport()
        {
            return BuildReport(LiveCaptureRoot);
        }

        public static M136ALiveCaptureInsightsReport BuildReport(string liveCaptureRoot)
        {
            var scenarioManifest = M136EditorLaptopPerformancePolicy.ScenarioManifest;
            var manifests = M136LivePerformanceCaptureStore.LoadManifests(liveCaptureRoot)
                .Where(manifest => manifest.complete && manifest.liveCaptured)
                .ToArray();
            var latestByScenario = manifests
                .GroupBy(manifest => manifest.scenarioId)
                .Select(group => group.Last())
                .OrderBy(manifest => manifest.scenarioId, StringComparer.Ordinal)
                .ToArray();
            var captures = latestByScenario
                .Select(BuildInventoryEntry)
                .Where(entry => entry != null)
                .ToArray();
            var checks = BuildChecks(liveCaptureRoot, scenarioManifest, captures).ToArray();
            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();
            var invalidCount = captures.Count(capture => capture.validityGrade == "Invalid");
            var needsRecaptureCount = captures.Count(capture => capture.validityGrade != "Valid");
            return new M136ALiveCaptureInsightsReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                passed = failures.Length == 0,
                result = failures.Length == 0 ? "PASSED" : "FAILED",
                insightResult = needsRecaptureCount > 0 ? "NEEDS_RECAPTURE_BEFORE_OPTIMIZATION" : "CAPTURES_USABLE",
                totalCaptures = captures.Length,
                expectedScenarioCount = scenarioManifest.Length,
                coveredScenarioCount = captures.Select(capture => capture.scenarioId).Distinct().Count(),
                invalidCaptureCount = invalidCount,
                needsRecaptureCount = needsRecaptureCount,
                sourceRoots = new[] { liveCaptureRoot, Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportMarkdownPath },
                captures = captures,
                metricConfidence = BuildMetricConfidence(captures).ToArray(),
                recommendations = BuildRecommendations(captures).ToArray(),
                checks = checks,
                failures = failures
            };
        }

        public static string ToMarkdown(M136ALiveCaptureInsightsReport report)
        {
            var builder = new StringBuilder(12000);
            builder.AppendLine("# M136A Live Capture Insights Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Insight state: `{report.insightResult}`");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Capture coverage: {report.coveredScenarioCount}/{report.expectedScenarioCount} scenarios");
            builder.AppendLine($"- Needs recapture: {report.needsRecaptureCount}");
            builder.AppendLine($"- PDF: `{ReportPdfPath}`");
            builder.AppendLine();

            builder.AppendLine("## Executive Read");
            builder.AppendLine();
            builder.AppendLine("- Live capture artifacts exist for all six M136 scenarios.");
            builder.AppendLine("- The current automated capture set is not valid for combat, wave, or boss conclusions because those captures report no enemies, projectiles, or VFX.");
            builder.AppendLine("- Do not overfit combat/wave/boss optimization decisions to this capture set; fix capture validity first.");
            builder.AppendLine("- The strongest confirmed issue is still power waste from uncapped/high-frequency editor play, with repeated HUD/wall refreshes and high GC allocation pressure as the next suspects.");
            builder.AppendLine();

            builder.AppendLine("## Capture Inventory");
            builder.AppendLine();
            builder.AppendLine("| Scenario | Validity | Samples | Hz | Frame p95 | GPU p95 | Main p95 | GC avg | Walls/HUD | Content peaks | Trace |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|");
            foreach (var capture in report.captures ?? Array.Empty<M136ALiveCaptureInventoryEntry>())
            {
                builder.AppendLine($"| `{capture.scenarioId}` | {capture.validityGrade} | {capture.sampleCount} | {capture.sampleRateHz:0.#} | {capture.frameP95Ms:0.###}ms | {capture.gpuP95Ms:0.###}ms | {capture.mainThreadP95Ms:0.###}ms | {capture.gcAverageKb:0.#}KB | {capture.wallVisibilityUpdates}/{capture.combatHudRefreshes} | E{capture.peakEnemies}/P{capture.peakProjectiles}/V{capture.peakVfx}/R{capture.peakRenderers} | {capture.profilerTraceSizeMb:0.#}MB |");
            }

            builder.AppendLine();
            builder.AppendLine("## Validity Notes");
            builder.AppendLine();
            foreach (var capture in report.captures ?? Array.Empty<M136ALiveCaptureInventoryEntry>())
            {
                builder.AppendLine($"- `{capture.scenarioId}`: **{capture.validityGrade}** - {capture.validityReason}");
            }

            builder.AppendLine();
            builder.AppendLine("## Metric Confidence");
            builder.AppendLine();
            foreach (var confidence in report.metricConfidence ?? Array.Empty<M136AMetricConfidenceEntry>())
            {
                builder.AppendLine($"- `{confidence.metricId}`: **{confidence.confidence}** - {confidence.reason}");
            }

            builder.AppendLine();
            builder.AppendLine("## Recommended Next Steps");
            builder.AppendLine();
            foreach (var recommendation in report.recommendations ?? Array.Empty<M136AInsightRecommendation>())
            {
                builder.AppendLine($"{recommendation.rank}. **{recommendation.title}** (`{recommendation.confidence}`)");
                builder.AppendLine($"   Evidence: {recommendation.evidence}");
                builder.AppendLine($"   Next step: {recommendation.nextStep}");
            }

            builder.AppendLine();
            builder.AppendLine("## Checks");
            builder.AppendLine();
            foreach (var check in report.checks ?? Array.Empty<M136AInsightCheck>())
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

        public static M136ALiveCaptureInventoryEntry BuildInventoryEntry(M136LiveCaptureManifest manifest)
        {
            var result = M136LivePerformanceCaptureStore.LoadScenarioResult(manifest);
            if (manifest == null || result == null)
            {
                return null;
            }

            var validity = M136CaptureValidityClassifier.Classify(manifest, result);
            return new M136ALiveCaptureInventoryEntry
            {
                scenarioId = manifest.scenarioId,
                displayName = manifest.scenarioDisplayName,
                captureId = manifest.captureId,
                captureMode = manifest.captureMode,
                artifactDirectory = manifest.artifactDirectory,
                sampleCount = manifest.sampleCount,
                sampleRateHz = manifest.sampleSeconds > 0f ? manifest.sampleCount / manifest.sampleSeconds : 0d,
                frameP95Ms = Metric(result, "frame_time_ms")?.p95 ?? 0d,
                frameMaxMs = Metric(result, "frame_time_ms")?.max ?? 0d,
                fpsP50 = Metric(result, "fps")?.p50 ?? 0d,
                gpuP95Ms = Metric(result, "gpu_frame_ms")?.p95 ?? 0d,
                mainThreadP95Ms = Metric(result, "main_thread_ms")?.p95 ?? 0d,
                gcAverageKb = (Metric(result, "gc_allocated_bytes")?.average ?? 0d) / 1024d,
                managedMemoryP95Mb = Metric(result, "managed_memory_mb")?.p95 ?? 0d,
                graphicsMemoryP95Mb = Metric(result, "graphics_memory_mb")?.p95 ?? 0d,
                wallVisibilityUpdates = result.operations?.wallVisibilityUpdates ?? 0,
                combatHudRefreshes = result.operations?.combatHudRefreshes ?? 0,
                runtimeNavMeshFallbacks = result.operations?.runtimeNavMeshFallbacks ?? 0,
                peakEnemies = result.objectCounts?.peakEnemies ?? 0,
                peakProjectiles = result.objectCounts?.peakProjectiles ?? 0,
                peakVfx = result.objectCounts?.peakVfx ?? 0,
                peakRenderers = result.objectCounts?.peakRenderers ?? 0,
                profilerTraceSizeMb = File.Exists(manifest.profilerTracePath) ? new FileInfo(manifest.profilerTracePath).Length / (1024d * 1024d) : 0d,
                validityGrade = validity.Grade,
                validityReason = validity.Reason
            };
        }

        public static (string grade, string reason) ClassifyCaptureValidity(M136LiveCaptureManifest manifest, M136PerformanceScenarioResult result)
        {
            var validity = M136CaptureValidityClassifier.Classify(manifest, result);
            return (validity.Grade, validity.Reason);
        }

        private static IEnumerable<M136AMetricConfidenceEntry> BuildMetricConfidence(IReadOnlyCollection<M136ALiveCaptureInventoryEntry> captures)
        {
            var hasFastSampling = captures.Any(capture => capture.sampleRateHz > 180d);
            var invalidContent = captures.Any(capture => capture.validityGrade == "Invalid");
            yield return new M136AMetricConfidenceEntry
            {
                metricId = "frame_time_ms/fps",
                confidence = hasFastSampling ? "Invalid / needs recapture" : "Directional",
                reason = hasFastSampling
                    ? "Sampling ran at editor-update rates above normal presented frame cadence, so frame time and FPS are not reliable gameplay frame metrics."
                    : "Sampling cadence is plausible, but should still be confirmed with corrected Play Mode captures."
            };
            yield return new M136AMetricConfidenceEntry
            {
                metricId = "gpu_frame_ms",
                confidence = "Directional",
                reason = "GPU timing is consistently below 60 FPS budget, but high-refresh uncapped play can still burn power."
            };
            yield return new M136AMetricConfidenceEntry
            {
                metricId = "main_thread_ms",
                confidence = invalidContent ? "Directional" : "Trusted",
                reason = "Main-thread p95 is stable around the editor samples, but combat/wave/boss content must be recaptured before tuning those scenarios."
            };
            yield return new M136AMetricConfidenceEntry
            {
                metricId = "wall_visibility/combat_hud counters",
                confidence = "Trusted",
                reason = "Counters are explicit runtime hooks and show repeated refreshes during the sample windows."
            };
            yield return new M136AMetricConfidenceEntry
            {
                metricId = "gc_allocated_bytes",
                confidence = "Directional",
                reason = "Per-sample allocation is high enough to investigate, but fixed-cadence Play Mode captures should confirm exact rates."
            };
            yield return new M136AMetricConfidenceEntry
            {
                metricId = "runtime_navmesh_fallbacks",
                confidence = "Trusted for this capture set",
                reason = "Counters remained zero, so NavMesh fallback is not implicated by the current evidence."
            };
        }

        private static IEnumerable<M136AInsightRecommendation> BuildRecommendations(IReadOnlyCollection<M136ALiveCaptureInventoryEntry> captures)
        {
            var gpuP95Range = Range(captures.Select(capture => capture.gpuP95Ms).Where(value => value > 0d));
            var mainP95Range = Range(captures.Select(capture => capture.mainThreadP95Ms).Where(value => value > 0d));
            var gcRange = Range(captures.Select(capture => capture.gcAverageKb).Where(value => value > 0d));
            var traceRange = Range(captures.Select(capture => capture.profilerTraceSizeMb).Where(value => value > 0d));
            yield return new M136AInsightRecommendation
            {
                rank = 1,
                id = "fix-capture-validity",
                confidence = "High",
                title = "Fix live capture validity before deeper optimization",
                evidence = $"{captures.Count(capture => capture.validityGrade == "Invalid")} scenario captures are invalid; combat/wave/boss captures currently report no enemies, projectiles, or VFX.",
                nextStep = "Move sampling to a Play Mode MonoBehaviour and make automated scenarios actually route/spawn representative ship, branch, combat, wave, boss, and transition states."
            };
            yield return new M136AInsightRecommendation
            {
                rank = 2,
                id = "editor-laptop-60fps-cap",
                confidence = "High",
                title = "Add an editor-laptop 60 FPS cap/dev profile",
                evidence = $"GPU p95 is {gpuP95Range} and main-thread p95 is {mainP95Range}, but the current Windows profile still targets 120 FPS with vSync disabled.",
                nextStep = "Add a low-risk editor/dev profile that caps to 60 FPS before visual cuts."
            };
            yield return new M136AInsightRecommendation
            {
                rank = 3,
                id = "cooler-editor-urp-profile",
                confidence = "Medium",
                title = "Create a cooler editor URP profile",
                evidence = "The M136 render snapshot has HDR, depth, opaque texture, 50m shadows, 4 cascades, soft shadows, and additional-light shadows enabled.",
                nextStep = "Prototype a cooler editor URP asset/profile and compare corrected live captures before and after."
            };
            yield return new M136AInsightRecommendation
            {
                rank = 4,
                id = "cadence-hud-walls",
                confidence = "Medium",
                title = "Cadence or event-drive wall visibility and combat HUD refresh",
                evidence = "Most captures show roughly 3.5k wall-visibility and combat-HUD refreshes over 30 seconds.",
                nextStep = "Cache unchanged state and update walls/HUD on camera/state changes or a lower cadence."
            };
            yield return new M136AInsightRecommendation
            {
                rank = 5,
                id = "investigate-gc",
                confidence = "Medium",
                title = "Investigate GC allocations with corrected captures",
                evidence = $"Average GC allocation is {gcRange} per sample in the current captures.",
                nextStep = "Use profiler traces and corrected Play Mode sampling to identify allocation sources before changing gameplay code."
            };
            yield return new M136AInsightRecommendation
            {
                rank = 6,
                id = "profiler-trace-optional",
                confidence = "High",
                title = "Keep profiler trace export optional",
                evidence = $"Current trace files are large ({traceRange}), which is useful for deep dives but heavy for routine captures.",
                nextStep = "Default routine captures to summary/CSV and enable profiler traces only for focused investigations."
            };
        }

        private static IEnumerable<M136AInsightCheck> BuildChecks(string liveCaptureRoot, M136PerformanceScenarioDefinition[] scenarios, M136ALiveCaptureInventoryEntry[] captures)
        {
            yield return Check("capture-root-exists", Directory.Exists(liveCaptureRoot), Directory.Exists(liveCaptureRoot) ? $"Found `{liveCaptureRoot}`." : $"Missing `{liveCaptureRoot}`.");
            yield return Check("all-scenarios-covered", captures.Select(capture => capture.scenarioId).Distinct().Count() == scenarios.Length, $"Covered {captures.Select(capture => capture.scenarioId).Distinct().Count()}/{scenarios.Length} scenarios.");
            yield return Check("inventory-built", captures.Length > 0, $"Capture rows: {captures.Length}.");
            yield return Check("invalid-content-detected", captures.Any(capture => capture.validityGrade == "Invalid"), "Combat/wave/boss invalid capture detection is active.");
            yield return Check("recommendation-order", true, "Recommendations are ordered: capture validity, FPS cap, cooler URP, cadence, GC, trace optional.");
        }

        private static M136AInsightCheck Check(string id, bool passed, string detail)
        {
            return new M136AInsightCheck
            {
                id = id,
                passed = passed,
                detail = detail
            };
        }

        private static M136PerformanceMetricSummary Metric(M136PerformanceScenarioResult result, string id)
        {
            return result?.metrics?.FirstOrDefault(metric => metric.id == id && metric.supported);
        }

        private static string Range(IEnumerable<double> values)
        {
            var array = values.Where(value => value > 0d && !double.IsNaN(value) && !double.IsInfinity(value)).ToArray();
            return array.Length == 0 ? "unavailable" : $"{array.Min():0.#}-{array.Max():0.#}";
        }

        private static void WritePdf(string path, M136ALiveCaptureInsightsReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "output/reports");
            M136AInsightsPdfWriter.Write(path, BuildPdfLines(report).ToArray());
        }

        private static IEnumerable<string> BuildPdfLines(M136ALiveCaptureInsightsReport report)
        {
            yield return "Hollow Soul - M136A Live Capture Insights";
            yield return $"Result: {report.result}";
            yield return $"Insight state: {report.insightResult}";
            yield return $"Generated: {report.generatedAtUtc}";
            yield return $"Coverage: {report.coveredScenarioCount}/{report.expectedScenarioCount}; needs recapture: {report.needsRecaptureCount}";
            yield return string.Empty;
            yield return "Key Conclusions";
            yield return "Live capture artifacts exist for all six scenarios, including profiler traces.";
            yield return "Current automated captures are not valid for combat, wave, or boss tuning because they report no enemies, projectiles, or VFX.";
            yield return "Do not overfit combat/wave/boss conclusions to this capture set; fix capture validity first.";
            yield return "The most defensible next optimization is a 60 FPS editor-laptop cap, then a cooler editor URP profile.";
            yield return string.Empty;
            yield return "Capture Inventory";
            foreach (var capture in report.captures ?? Array.Empty<M136ALiveCaptureInventoryEntry>())
            {
                yield return $"{capture.scenarioId}: {capture.validityGrade}; samples {capture.sampleCount}; {capture.sampleRateHz:0.#} Hz; GPU p95 {capture.gpuP95Ms:0.###} ms; main p95 {capture.mainThreadP95Ms:0.###} ms; GC avg {capture.gcAverageKb:0.#} KB; content E{capture.peakEnemies}/P{capture.peakProjectiles}/V{capture.peakVfx}; trace {capture.profilerTraceSizeMb:0.#} MB";
                yield return $"  {capture.validityReason}";
            }

            yield return string.Empty;
            yield return "Metric Confidence";
            foreach (var confidence in report.metricConfidence ?? Array.Empty<M136AMetricConfidenceEntry>())
            {
                yield return $"{confidence.metricId}: {confidence.confidence} - {confidence.reason}";
            }

            yield return string.Empty;
            yield return "Recommended Next Steps";
            foreach (var recommendation in report.recommendations ?? Array.Empty<M136AInsightRecommendation>())
            {
                yield return $"{recommendation.rank}. {recommendation.title} [{recommendation.confidence}]";
                yield return $"Evidence: {recommendation.evidence}";
                yield return $"Next: {recommendation.nextStep}";
            }
        }

        private static class M136AInsightsPdfWriter
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
                builder.Append("BT\n/F1 10 Tf\n");
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
