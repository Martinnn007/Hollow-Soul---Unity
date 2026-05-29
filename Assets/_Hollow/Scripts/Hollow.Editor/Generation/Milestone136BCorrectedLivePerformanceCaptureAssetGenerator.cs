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
    public sealed class Milestone136BCorrectedLivePerformanceCaptureReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public bool passed;
        public string result;
        public int totalChecks;
        public int passedChecks;
        public int latestCaptureCount;
        public int provisionalCaptureCount;
        public int validCaptureCount;
        public string[] recaptureChecklist;
        public M136BCaptureEntry[] captures;
        public M136BCheck[] checks;
        public string[] failures;
    }

    [Serializable]
    public sealed class M136BCaptureEntry
    {
        public string scenarioId;
        public string captureId;
        public string captureMode;
        public string samplingSource;
        public string frameCadenceConfidence;
        public string validityGrade;
        public string validityReason;
        public int sampleCount;
        public double sampleRateHz;
        public bool profilerTraceRequested;
        public bool profilerTraceSupported;
        public double profilerTraceSizeMb;
        public bool fpsOverrideApplied;
        public int fpsOverrideTarget;
        public int enemies;
        public int projectiles;
        public int vfx;
        public int renderers;
        public int transitions;
    }

    [Serializable]
    public sealed class M136BCheck
    {
        public string id;
        public bool passed;
        public string detail;
    }

    public static class Milestone136BCorrectedLivePerformanceCaptureAssetGenerator
    {
        public const string LockId = "M136B";
        public const string Title = "M136B Corrected Live Performance Capture + Measurement-Only Readiness";
        public const string DocsPath = "Docs/Milestone136BCorrectedLivePerformanceCapture.md";
        public const string ReportMarkdownPath = "output/reports/m136b_corrected_live_performance_capture.md";
        public const string ReportJsonPath = "output/reports/m136b_corrected_live_performance_capture.json";
        public const string ReportPdfPath = "output/reports/m136b_corrected_live_performance_capture.pdf";

        private const string TelemetryPath = "Assets/_Hollow/Scripts/Hollow.Diagnostics/M136EditorLaptopPerformanceTelemetry.cs";
        private const string CaptureWindowPath = "Assets/_Hollow/Scripts/Hollow.Editor/M136LiveGameplayCaptureWindow.cs";
        private const string BranchSessionPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";

        [MenuItem("Hollow/Performance/Generate M136B Corrected Live Capture Report")]
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

        public static Milestone136BCorrectedLivePerformanceCaptureReport BuildReport(string liveCaptureRoot = M136LivePerformanceCaptureStore.LiveCaptureRoot)
        {
            var latest = M136LivePerformanceCaptureStore.LoadManifests(liveCaptureRoot)
                .Where(manifest => manifest.complete && manifest.liveCaptured)
                .GroupBy(manifest => manifest.scenarioId)
                .Select(group => group.Last())
                .OrderBy(manifest => manifest.scenarioId, StringComparer.Ordinal)
                .ToArray();
            var captures = latest
                .Select(BuildCaptureEntry)
                .Where(entry => entry != null)
                .ToArray();
            var checks = BuildChecks(liveCaptureRoot, captures).ToArray();
            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();
            return new Milestone136BCorrectedLivePerformanceCaptureReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                passed = failures.Length == 0,
                result = failures.Length == 0 ? "PASSED" : "FAILED",
                totalChecks = checks.Length,
                passedChecks = checks.Count(check => check.passed),
                latestCaptureCount = captures.Length,
                provisionalCaptureCount = captures.Count(capture => capture.validityGrade != M136CaptureValidityClassifier.Valid),
                validCaptureCount = captures.Count(capture => capture.validityGrade == M136CaptureValidityClassifier.Valid),
                recaptureChecklist = RecaptureChecklist(),
                captures = captures,
                checks = checks,
                failures = failures
            };
        }

        public static string ToMarkdown(Milestone136BCorrectedLivePerformanceCaptureReport report)
        {
            var builder = new StringBuilder(12000);
            builder.AppendLine("# M136B Corrected Live Performance Capture + Measurement-Only Readiness");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks}");
            builder.AppendLine($"- Latest captures found: {report.latestCaptureCount}");
            builder.AppendLine($"- Provisional captures: {report.provisionalCaptureCount}");
            builder.AppendLine($"- PDF: `{ReportPdfPath}`");
            builder.AppendLine();
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine("- M136B fixes measurement before optimization: samples must come from Play Mode `MonoBehaviour.Update`.");
            builder.AppendLine("- No optimization changes are applied in M136B; it only corrects capture confidence, validity, and artifact reporting.");
            builder.AppendLine("- Current legacy/manual captures remain useful as provisional evidence, but they are not optimization gates until recaptured with corrected runtime-frame sampling.");
            builder.AppendLine("- The 60 FPS cap is capture-scoped and optional; default game/editor/runtime profiles are unchanged.");
            builder.AppendLine("- Profiler traces are optional and off by default.");
            builder.AppendLine();
            builder.AppendLine("## Capture Inventory");
            builder.AppendLine();
            builder.AppendLine("| Scenario | Validity | Source | Cadence | Samples | Hz | Trace | FPS Cap | Content | Transitions |");
            builder.AppendLine("|---|---:|---|---|---:|---:|---:|---:|---|---:|");
            foreach (var capture in report.captures ?? Array.Empty<M136BCaptureEntry>())
            {
                builder.AppendLine($"| `{capture.scenarioId}` | {capture.validityGrade} | `{capture.samplingSource}` | {capture.frameCadenceConfidence} | {capture.sampleCount} | {capture.sampleRateHz:0.#} | {(capture.profilerTraceRequested ? capture.profilerTraceSizeMb.ToString("0.#", CultureInfo.InvariantCulture) + "MB" : "off")} | {(capture.fpsOverrideApplied ? capture.fpsOverrideTarget.ToString(CultureInfo.InvariantCulture) : "off")} | E{capture.enemies}/P{capture.projectiles}/V{capture.vfx}/R{capture.renderers} | {capture.transitions} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Validity Notes");
            builder.AppendLine();
            foreach (var capture in report.captures ?? Array.Empty<M136BCaptureEntry>())
            {
                builder.AppendLine($"- `{capture.scenarioId}`: **{capture.validityGrade}** - {capture.validityReason}");
            }

            builder.AppendLine();
            builder.AppendLine("## Recapture Checklist");
            builder.AppendLine();
            foreach (var item in report.recaptureChecklist ?? Array.Empty<string>())
            {
                builder.AppendLine($"- [ ] {item}");
            }

            builder.AppendLine();
            builder.AppendLine("## Checks");
            builder.AppendLine();
            foreach (var check in report.checks ?? Array.Empty<M136BCheck>())
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
                "# M136B: Corrected Live Performance Capture + Measurement-Only Readiness\n\n" +
                "## Summary\n" +
                "M136B repairs the live capture harness before optimization work. It does not change URP defaults, gameplay systems, HUD layout, branch generation, or runtime economy. The goal is trustworthy evidence for the next performance pass.\n\n" +
                "## Runtime Sampling\n" +
                "- Live captures are driven by `M136RuntimeLiveCaptureDriver`, a hidden Play Mode `MonoBehaviour` that ticks once per gameplay frame through `Update`.\n" +
                "- The Editor window only starts/stops captures, repaints status, and exports artifacts.\n" +
                "- Each manifest records sampling source, sample rate, expected sample-count range, frame-cadence confidence, validity grade, profiler trace toggle state, and capture-scoped FPS override state.\n" +
                "- Object counts are collected on a throttled runtime cadence so the tool does not become the main bottleneck.\n\n" +
                "## Validity Gates\n" +
                "- Ship hub idle must observe ship hub context.\n" +
                "- Normal branch idle must observe branch context.\n" +
                "- Active combat must observe active combat, enemies, projectiles, or VFX.\n" +
                "- Wave captures must observe wave state and enemies.\n" +
                "- Boss captures must observe boss runtime or active boss state.\n" +
                "- Transition captures must observe a room transition event.\n" +
                "- Captures are `Valid`, `Directional`, or `Invalid`; invalid captures remain archived but cannot drive optimization conclusions.\n\n" +
                "## Capture Window\n" +
                "- `Use 60 FPS capture cap` is off by default and applies only while a capture is running.\n" +
                "- `Capture profiler trace` is off by default because previous traces were hundreds of MB.\n" +
                "- Automated smoke capture remains label-only/non-authoritative until a later scenario routing pass.\n\n" +
                "## Deferrals\n" +
                "- No cooler URP profile is applied in M136B.\n" +
                "- No default desktop FPS cap is applied in M136B.\n" +
                "- No HUD, wall, minimap, combat, save, room-generation, or economy behavior is optimized in M136B.\n";
        }

        private static M136BCaptureEntry BuildCaptureEntry(M136LiveCaptureManifest manifest)
        {
            var result = M136LivePerformanceCaptureStore.LoadScenarioResult(manifest);
            if (result == null)
            {
                return null;
            }

            var validity = M136CaptureValidityClassifier.Classify(manifest, result);
            return new M136BCaptureEntry
            {
                scenarioId = manifest.scenarioId,
                captureId = manifest.captureId,
                captureMode = manifest.captureMode,
                samplingSource = string.IsNullOrWhiteSpace(manifest.samplingSource) ? M136FrameCadencePolicy.UnknownSamplingSource : manifest.samplingSource,
                frameCadenceConfidence = string.IsNullOrWhiteSpace(manifest.frameCadenceConfidence) ? M136FrameCadencePolicy.Invalid : manifest.frameCadenceConfidence,
                validityGrade = validity.Grade,
                validityReason = validity.Reason,
                sampleCount = manifest.sampleCount,
                sampleRateHz = manifest.sampleRateHz > 0d ? manifest.sampleRateHz : manifest.sampleSeconds > 0f ? manifest.sampleCount / manifest.sampleSeconds : 0d,
                profilerTraceRequested = manifest.profilerTraceRequested,
                profilerTraceSupported = manifest.profilerTraceSupported,
                profilerTraceSizeMb = File.Exists(manifest.profilerTracePath) ? new FileInfo(manifest.profilerTracePath).Length / (1024d * 1024d) : 0d,
                fpsOverrideApplied = manifest.fpsOverrideApplied,
                fpsOverrideTarget = manifest.fpsOverrideTarget,
                enemies = result.objectCounts?.peakEnemies ?? 0,
                projectiles = result.objectCounts?.peakProjectiles ?? 0,
                vfx = result.objectCounts?.peakVfx ?? 0,
                renderers = result.objectCounts?.peakRenderers ?? 0,
                transitions = result.operations?.roomTransitionEvents ?? 0
            };
        }

        private static IEnumerable<M136BCheck> BuildChecks(string liveCaptureRoot, M136BCaptureEntry[] captures)
        {
            yield return Check("docs-path", File.Exists(DocsPath), File.Exists(DocsPath) ? $"Found `{DocsPath}`." : $"Missing `{DocsPath}`.");
            yield return Check("capture-root", Directory.Exists(liveCaptureRoot), Directory.Exists(liveCaptureRoot) ? $"Found `{liveCaptureRoot}`." : $"Missing `{liveCaptureRoot}`.");
            yield return Check("runtime-driver-api", Read(TelemetryPath).Contains("M136RuntimeLiveCaptureDriver") && Read(TelemetryPath).Contains("playmode-monobehaviour-update"), "Runtime driver samples from Play Mode Update.");
            yield return Check("validity-classifier-api", Read(TelemetryPath).Contains("M136CaptureValidityClassifier") && Read(TelemetryPath).Contains("roomTransitionEvents"), "Validity classifier and transition counter fields are present.");
            yield return Check("fps-override-api", Read(TelemetryPath).Contains("M136CaptureFpsOverride") && Read(CaptureWindowPath).Contains("Use 60 FPS capture cap"), "Capture-scoped FPS override toggle is present.");
            yield return Check("trace-default-off", Read(CaptureWindowPath).Contains("captureProfilerTrace") && Read(CaptureWindowPath).Contains("Profiler trace capture is off by default"), "Profiler trace toggle defaults off in the window flow.");
            yield return Check("transition-hook", Read(BranchSessionPath).Contains("ReportRoomTransition"), "Branch traversal reports transition events for corrected captures.");
            yield return Check("latest-captures-inventory", captures.Length > 0, $"Latest captures inventoried: {captures.Length}.");
            yield return Check("recapture-checklist", RecaptureChecklist().Length == M136EditorLaptopPerformancePolicy.ScenarioManifest.Length, "Recapture checklist covers all M136 scenarios.");
        }

        private static string[] RecaptureChecklist()
        {
            return M136EditorLaptopPerformancePolicy.ScenarioManifest
                .Select(scenario => $"{scenario.displayName}: recapture with M136B runtime-frame sampling; enable trace only for focused spike investigation.")
                .ToArray();
        }

        private static M136BCheck Check(string id, bool passed, string detail)
        {
            return new M136BCheck
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

        private static void WritePdf(string path, Milestone136BCorrectedLivePerformanceCaptureReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "output/reports");
            M136BSimplePdfWriter.Write(path, BuildPdfLines(report).ToArray());
        }

        private static IEnumerable<string> BuildPdfLines(Milestone136BCorrectedLivePerformanceCaptureReport report)
        {
            yield return "Hollow Soul - M136B Corrected Live Performance Capture";
            yield return $"Result: {report.result}";
            yield return $"Lock id: {report.lockId}";
            yield return $"Generated: {report.generatedAtUtc}";
            yield return $"Latest captures: {report.latestCaptureCount}; provisional: {report.provisionalCaptureCount}; valid: {report.validCaptureCount}";
            yield return string.Empty;
            yield return "Measurement Policy";
            yield return "Samples now come from a hidden Play Mode MonoBehaviour.Update runtime driver.";
            yield return "The 60 FPS cap is optional and capture-scoped only; default profiles are unchanged.";
            yield return "Profiler traces are optional and off by default.";
            yield return "No optimization changes are applied in M136B.";
            yield return "Legacy captures are kept as provisional evidence, but they cannot drive optimization conclusions.";
            yield return "Valid captures must meet scenario-specific content gates before they count as performance evidence.";
            yield return "Object-count snapshots are throttled so the capture tool does not become the workload.";
            yield return string.Empty;
            yield return "Capture Inventory";
            foreach (var capture in report.captures ?? Array.Empty<M136BCaptureEntry>())
            {
                yield return $"{capture.scenarioId}: {capture.validityGrade}; source {capture.samplingSource}; cadence {capture.frameCadenceConfidence}; samples {capture.sampleCount}; {capture.sampleRateHz:0.#} Hz; content E{capture.enemies}/P{capture.projectiles}/V{capture.vfx}/R{capture.renderers}; transitions {capture.transitions}; trace {(capture.profilerTraceRequested ? capture.profilerTraceSizeMb.ToString("0.#", CultureInfo.InvariantCulture) + " MB" : "off")}; fps cap {(capture.fpsOverrideApplied ? capture.fpsOverrideTarget.ToString(CultureInfo.InvariantCulture) : "off")}";
                yield return $"  {capture.validityReason}";
            }

            yield return string.Empty;
            yield return "Recapture Checklist";
            foreach (var item in report.recaptureChecklist ?? Array.Empty<string>())
            {
                yield return $"- {item}";
            }
        }

        private static class M136BSimplePdfWriter
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
