using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Performance;
using Hollow.Editor.Validation;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Hollow.Editor.Build
{
    [Serializable]
    public sealed class PerformanceComparisonScenario
    {
        public string scenarioId;
        public string source;
        public bool required;
        public bool passed;
        public string status;
        public double baselineFrameP95Ms;
        public double candidateFrameP95Ms;
        public double frameP95DeltaMs;
        public double baselineFrameMaxMs;
        public double candidateFrameMaxMs;
        public double frameMaxDeltaMs;
        public int baselineScorerCalls;
        public int candidateScorerCalls;
        public double scorerCallReductionPercent;
        public int baselineBehaviorGraphTicks;
        public int candidateBehaviorGraphTicks;
        public double behaviorGraphTickReductionPercent;
        public int baselinePathRequests;
        public int candidatePathRequests;
        public double pathRequestReductionPercent;
        public int baselinePathSolves;
        public int candidatePathSolves;
        public double pathSolveReductionPercent;
        public int baselineReservationCandidates;
        public int candidateReservationCandidates;
        public double reservationCandidateReductionPercent;
        public int baselineReservationPathSolves;
        public int candidateReservationPathSolves;
        public double reservationPathSolveReductionPercent;
        public bool baselineReservationCountersAvailable;
        public int candidateCrowdReservationSkips;
        public int candidateCrowdCachedIntentReuses;
        public int candidateCrowdScorerSkips;
        public string note;
        public string failure;
    }

    [Serializable]
    public sealed class PerformanceComparisonReport
    {
        public string generatedAtUtc;
        public string baselineLabel;
        public string candidateLabel;
        public bool passed;
        public string baselineRoot;
        public string candidateRoot;
        public string[] failures = Array.Empty<string>();
        public PerformanceComparisonScenario[] scenarios = Array.Empty<PerformanceComparisonScenario>();
    }

    public static class PerformanceComparisonReportGenerator
    {
        public const string Root = "output/reports/performance_comparisons";
        public const string BaselineRoot = Root + "/baseline";
        public const string LatestJsonPath = Root + "/latest_comparison.json";
        public const string LatestMarkdownPath = Root + "/latest_comparison.md";
        public const string FocusedEditModeTestResultsPath = Root + "/non_boss_crowd_scale_editmode.xml";
        public const double RequiredCrowdReductionPercent = 70d;

        private const string M138BaselinePath = BaselineRoot + "/m138_combat_scale_stress.json";
        private const string M140BaselinePath = BaselineRoot + "/m140_build_real_gate.json";
        private const string DefaultM138CandidatePath = "output/reports/truth_gate/m138_full.json";
        private const string FallbackM138CandidatePath = M138CombatScaleStressReportGenerator.DefaultJsonReportPath;
        private const string DefaultM140CandidatePath = "output/reports/m140/macos-apple-silicon/development/m140_build_real_gate.json";

        [MenuItem("Hollow/Performance/Capture Performance Baseline From Latest Reports")]
        public static void CaptureBaselineFromLatestReportsMenu()
        {
            CaptureBaselineFromLatestReports("baseline");
        }

        [MenuItem("Hollow/Performance/Compare Latest Performance Against Baseline")]
        public static void CompareLatestAgainstBaselineMenu()
        {
            var report = CompareLatestAgainstBaseline("baseline", "candidate");
            Debug.Log($"{(report.passed ? "Performance comparison passed" : "Performance comparison failed")}. Report: {LatestMarkdownPath}");
        }

        [MenuItem("Hollow/Performance/Run Non-Boss Crowd Scale EditMode Tests")]
        public static void RunFocusedEditModeTestsMenu()
        {
            RunFocusedEditModeTests(exitEditorOnComplete: false);
        }

        public static void RunFocusedEditModeTestsBatch()
        {
            RunFocusedEditModeTests(exitEditorOnComplete: true);
        }

        public static bool RunFocusedEditModeTests(bool exitEditorOnComplete)
        {
            Directory.CreateDirectory(Root);
            var callbacks = ScriptableObject.CreateInstance<FocusedEditModeCallbacks>();
            callbacks.Configure(FocusedEditModeTestResultsPath);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(callbacks, priority: 1000);

            var previousExitSuppression = MilestoneValidationExitPolicy.SuppressEditorExit;
            MilestoneValidationExitPolicy.SuppressEditorExit = true;
            try
            {
                var settings = new ExecutionSettings(new Filter
                {
                    testMode = TestMode.EditMode,
                    assemblyNames = new[] { "Hollow.Tests.EditMode" },
                    groupNames = new[]
                    {
                        "^Hollow\\.Tests\\.EditMode\\.(Milestone138CombatAiNavigationScaleTests|Milestone140BuildRealGateTests)(\\.|$)"
                    }
                })
                {
                    runSynchronously = true
                };

                api.Execute(settings);
            }
            finally
            {
                MilestoneValidationExitPolicy.SuppressEditorExit = previousExitSuppression;
                api.UnregisterCallbacks(callbacks);
                UnityEngine.Object.DestroyImmediate(api);
            }

            var result = callbacks.Result;
            UnityEngine.Object.DestroyImmediate(callbacks);
            var passed = result != null && result.FailCount == 0 && result.InconclusiveCount == 0 && result.TestStatus != TestStatus.Failed;
            var total = result == null ? 0 : result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
            var summary = result == null
                ? "Non-boss crowd scale EditMode tests returned no result."
                : $"Non-boss crowd scale EditMode tests: {result.PassCount}/{total} passed, {result.FailCount} failed, {result.InconclusiveCount} inconclusive, {result.SkipCount} skipped.";

            if (passed)
            {
                Debug.Log($"{summary} Results: {FocusedEditModeTestResultsPath}");
            }
            else
            {
                Debug.LogError($"{summary} Results: {FocusedEditModeTestResultsPath}");
            }

            if (exitEditorOnComplete && Application.isBatchMode)
            {
                EditorApplication.Exit(passed ? 0 : 1);
            }

            return passed;
        }

        public static void CaptureBaselineFromLatestReports(string label)
        {
            Directory.CreateDirectory(BaselineRoot);
            CopyIfExists(ResolveLatestM138Path(), M138BaselinePath);
            CopyIfExists(ResolveLatestM140Path(), M140BaselinePath);
            File.WriteAllText(Path.Combine(BaselineRoot, "baseline_label.txt"), string.IsNullOrWhiteSpace(label) ? "baseline" : label);
            AssetDatabase.Refresh();
            Debug.Log($"Captured performance baseline `{label}` in `{BaselineRoot}`.");
        }

        public static PerformanceComparisonReport CompareLatestAgainstBaseline(string baselineLabel, string candidateLabel)
        {
            var baselineM138 = ReadJson<M138CombatScaleStressReport>(M138BaselinePath);
            var candidateM138 = ReadJson<M138CombatScaleStressReport>(ResolveLatestM138Path());
            var baselineM140 = ReadJson<M140BuildRealReport>(M140BaselinePath);
            var candidateM140 = ReadJson<M140BuildRealReport>(ResolveLatestM140Path());
            var report = BuildComparison(
                baselineLabel,
                candidateLabel,
                baselineM138,
                candidateM138,
                baselineM140,
                candidateM140,
                BaselineRoot,
                "output/reports");

            WriteReport(report, LatestJsonPath, LatestMarkdownPath);
            return report;
        }

        public static PerformanceComparisonReport BuildComparison(
            string baselineLabel,
            string candidateLabel,
            M138CombatScaleStressReport baselineM138,
            M138CombatScaleStressReport candidateM138,
            M140BuildRealReport baselineM140,
            M140BuildRealReport candidateM140,
            string baselineRoot = "",
            string candidateRoot = "")
        {
            var scenarios = new List<PerformanceComparisonScenario>
            {
                CompareStressScenario(baselineM138, candidateM138, "enemy_stress_30", required: true),
                CompareStressScenario(baselineM138, candidateM138, "projectile_heavy_room", required: true),
                CompareM140Scenario(baselineM140, candidateM140, "normal_traversal", required: true),
                CompareM140Scenario(baselineM140, candidateM140, "return_to_previous_room", required: true),
                CompareM140Scenario(baselineM140, candidateM140, "reward_room", required: true),
                CompareM140Scenario(baselineM140, candidateM140, "boss_entry", required: true),
                CompareM140Scenario(baselineM140, candidateM140, "boss_plus_adds", required: true)
            };

            var failures = scenarios
                .Where(scenario => scenario != null && scenario.required && !scenario.passed)
                .Select(scenario => $"{scenario.scenarioId}: {scenario.failure}")
                .ToArray();
            var report = new PerformanceComparisonReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                baselineLabel = string.IsNullOrWhiteSpace(baselineLabel) ? "baseline" : baselineLabel,
                candidateLabel = string.IsNullOrWhiteSpace(candidateLabel) ? "candidate" : candidateLabel,
                baselineRoot = baselineRoot ?? string.Empty,
                candidateRoot = candidateRoot ?? string.Empty,
                scenarios = scenarios.Where(scenario => scenario != null).ToArray(),
                failures = failures,
                passed = failures.Length == 0
            };
            return report;
        }

        public static string ToMarkdown(PerformanceComparisonReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Performance V-vs-V Comparison");
            builder.AppendLine();
            builder.AppendLine($"- Status: {(report != null && report.passed ? "PASS" : "FAIL")}");
            builder.AppendLine($"- Baseline: `{report?.baselineLabel ?? string.Empty}`");
            builder.AppendLine($"- Candidate: `{report?.candidateLabel ?? string.Empty}`");
            builder.AppendLine($"- Generated: `{report?.generatedAtUtc ?? string.Empty}`");
            builder.AppendLine();
            if (report?.failures != null && report.failures.Length > 0)
            {
                builder.AppendLine("## Failures");
                foreach (var failure in report.failures)
                {
                    builder.AppendLine($"- {failure}");
                }

                builder.AppendLine();
            }

            builder.AppendLine("## Scenario Deltas");
            foreach (var scenario in report?.scenarios ?? Array.Empty<PerformanceComparisonScenario>())
            {
                builder.AppendLine($"### {scenario.scenarioId}");
                builder.AppendLine($"- Source: `{scenario.source}`");
                builder.AppendLine($"- Status: {scenario.status}");
                builder.AppendLine($"- Frame p95: {scenario.baselineFrameP95Ms:0.00} -> {scenario.candidateFrameP95Ms:0.00} ms ({scenario.frameP95DeltaMs:+0.00;-0.00;0.00})");
                builder.AppendLine($"- Frame max: {scenario.baselineFrameMaxMs:0.00} -> {scenario.candidateFrameMaxMs:0.00} ms ({scenario.frameMaxDeltaMs:+0.00;-0.00;0.00})");
                builder.AppendLine($"- Scorer calls: {scenario.baselineScorerCalls} -> {scenario.candidateScorerCalls} ({scenario.scorerCallReductionPercent:0.0}% reduction)");
                builder.AppendLine($"- Behavior graph ticks: {scenario.baselineBehaviorGraphTicks} -> {scenario.candidateBehaviorGraphTicks} ({scenario.behaviorGraphTickReductionPercent:0.0}% reduction)");
                builder.AppendLine($"- Nav path solves: {scenario.baselinePathSolves} -> {scenario.candidatePathSolves} ({scenario.pathSolveReductionPercent:0.0}% reduction)");
                if (scenario.baselineReservationCountersAvailable)
                {
                    builder.AppendLine($"- Tactical reservation candidates: {scenario.baselineReservationCandidates} -> {scenario.candidateReservationCandidates} ({scenario.reservationCandidateReductionPercent:0.0}% reduction)");
                    builder.AppendLine($"- Tactical reservation path solves: {scenario.baselineReservationPathSolves} -> {scenario.candidateReservationPathSolves} ({scenario.reservationPathSolveReductionPercent:0.0}% reduction)");
                }
                else
                {
                    builder.AppendLine($"- Tactical reservation candidates: legacy baseline unavailable -> {scenario.candidateReservationCandidates}");
                    builder.AppendLine($"- Tactical reservation path solves: legacy baseline unavailable -> {scenario.candidateReservationPathSolves}");
                }

                builder.AppendLine($"- Candidate crowd counters: reservation skips {scenario.candidateCrowdReservationSkips}, cached intents {scenario.candidateCrowdCachedIntentReuses}, scorer skips {scenario.candidateCrowdScorerSkips}");
                if (!string.IsNullOrWhiteSpace(scenario.note))
                {
                    builder.AppendLine($"- Note: {scenario.note}");
                }

                if (!string.IsNullOrWhiteSpace(scenario.failure))
                {
                    builder.AppendLine($"- Failure: {scenario.failure}");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static PerformanceComparisonScenario CompareStressScenario(
            M138CombatScaleStressReport baselineReport,
            M138CombatScaleStressReport candidateReport,
            string scenarioId,
            bool required)
        {
            var baseline = FindScenario(baselineReport?.scenarios, scenarioId);
            var candidate = FindScenario(candidateReport?.scenarios, scenarioId);
            var comparison = new PerformanceComparisonScenario
            {
                scenarioId = scenarioId,
                source = "M138",
                required = required
            };

            if (baseline == null || candidate == null)
            {
                comparison.passed = false;
                comparison.status = "MISSING";
                comparison.failure = $"Missing {(baseline == null ? "baseline" : "candidate")} M138 scenario.";
                return comparison;
            }

            comparison.baselineFrameP95Ms = baseline.frameP95Ms;
            comparison.candidateFrameP95Ms = candidate.frameP95Ms;
            comparison.frameP95DeltaMs = candidate.frameP95Ms - baseline.frameP95Ms;
            comparison.baselineFrameMaxMs = baseline.frameMaxMs;
            comparison.candidateFrameMaxMs = candidate.frameMaxMs;
            comparison.frameMaxDeltaMs = candidate.frameMaxMs - baseline.frameMaxMs;
            comparison.baselineScorerCalls = baseline.aiScorerCalls;
            comparison.candidateScorerCalls = candidate.aiScorerCalls;
            comparison.scorerCallReductionPercent = ReductionPercent(baseline.aiScorerCalls, candidate.aiScorerCalls);
            comparison.baselineBehaviorGraphTicks = baseline.aiBehaviorGraphTicks;
            comparison.candidateBehaviorGraphTicks = candidate.aiBehaviorGraphTicks;
            comparison.behaviorGraphTickReductionPercent = ReductionPercent(baseline.aiBehaviorGraphTicks, candidate.aiBehaviorGraphTicks);
            comparison.baselinePathRequests = baseline.navPathRequests;
            comparison.candidatePathRequests = candidate.navPathRequests;
            comparison.pathRequestReductionPercent = ReductionPercent(baseline.navPathRequests, candidate.navPathRequests);
            comparison.baselinePathSolves = baseline.navPathSolves;
            comparison.candidatePathSolves = candidate.navPathSolves;
            comparison.pathSolveReductionPercent = ReductionPercent(baseline.navPathSolves, candidate.navPathSolves);
            comparison.baselineReservationCandidates = ResolveTacticalCounter(baseline, "reservationCandidatesChecked");
            comparison.candidateReservationCandidates = ResolveTacticalCounter(candidate, "reservationCandidatesChecked");
            comparison.reservationCandidateReductionPercent = ReductionPercent(comparison.baselineReservationCandidates, comparison.candidateReservationCandidates);
            comparison.baselineReservationPathSolves = ResolveTacticalCounter(baseline, "reservationPathSolves");
            comparison.candidateReservationPathSolves = ResolveTacticalCounter(candidate, "reservationPathSolves");
            comparison.reservationPathSolveReductionPercent = ReductionPercent(comparison.baselineReservationPathSolves, comparison.candidateReservationPathSolves);
            comparison.baselineReservationCountersAvailable =
                comparison.baselineReservationCandidates > 0 ||
                comparison.baselineReservationPathSolves > 0;
            comparison.candidateCrowdReservationSkips = candidate.tacticalCrowdReservationSkips;
            comparison.candidateCrowdCachedIntentReuses = candidate.tacticalCrowdCachedIntentReuses;
            comparison.candidateCrowdScorerSkips = candidate.tacticalCrowdScorerSkips;

            var failures = new List<string>();
            if (!candidate.passed)
            {
                failures.Add($"candidate scenario failed: {string.Join("; ", candidate.failures ?? Array.Empty<string>())}");
            }

            if (comparison.baselineReservationCountersAvailable &&
                comparison.reservationCandidateReductionPercent < RequiredCrowdReductionPercent)
            {
                failures.Add($"reservation candidate reduction {comparison.reservationCandidateReductionPercent:0.0}% is below required {RequiredCrowdReductionPercent:0.#}%.");
            }

            if (comparison.baselineReservationCountersAvailable &&
                comparison.reservationPathSolveReductionPercent < RequiredCrowdReductionPercent)
            {
                failures.Add($"reservation path-solve reduction {comparison.reservationPathSolveReductionPercent:0.0}% is below required {RequiredCrowdReductionPercent:0.#}%.");
            }

            if (!comparison.baselineReservationCountersAvailable)
            {
                comparison.note = "Baseline predates tactical reservation counters; candidate crowd counters are reported, but reservation-reduction percent is not used as a pass/fail gate for this comparison.";
                if (comparison.candidateCrowdReservationSkips <= 0 &&
                    comparison.candidateCrowdCachedIntentReuses <= 0 &&
                    comparison.candidateCrowdScorerSkips <= 0)
                {
                    failures.Add("legacy baseline lacks tactical counters and candidate produced no crowd-LOD counter evidence.");
                }
            }

            comparison.failure = string.Join(" ", failures);
            comparison.passed = failures.Count == 0;
            comparison.status = comparison.passed ? "PASS" : "FAIL";
            return comparison;
        }

        private static PerformanceComparisonScenario CompareM140Scenario(
            M140BuildRealReport baselineReport,
            M140BuildRealReport candidateReport,
            string scenarioId,
            bool required)
        {
            var baseline = FindScenario(baselineReport?.scenarios, scenarioId);
            var candidate = FindScenario(candidateReport?.scenarios, scenarioId);
            var comparison = new PerformanceComparisonScenario
            {
                scenarioId = scenarioId,
                source = "M140",
                required = required
            };

            if (baseline == null || candidate == null)
            {
                comparison.passed = false;
                comparison.status = "MISSING";
                comparison.failure = $"Missing {(baseline == null ? "baseline" : "candidate")} M140 scenario.";
                return comparison;
            }

            comparison.baselineFrameP95Ms = baseline.frameP95Ms;
            comparison.candidateFrameP95Ms = candidate.frameP95Ms;
            comparison.frameP95DeltaMs = candidate.frameP95Ms - baseline.frameP95Ms;
            comparison.baselineFrameMaxMs = baseline.frameMaxMs;
            comparison.candidateFrameMaxMs = candidate.frameMaxMs;
            comparison.frameMaxDeltaMs = candidate.frameMaxMs - baseline.frameMaxMs;
            comparison.baselineScorerCalls = baseline.aiScorerCalls;
            comparison.candidateScorerCalls = candidate.aiScorerCalls;
            comparison.scorerCallReductionPercent = ReductionPercent(baseline.aiScorerCalls, candidate.aiScorerCalls);
            comparison.baselineBehaviorGraphTicks = baseline.aiBehaviorGraphTicks;
            comparison.candidateBehaviorGraphTicks = candidate.aiBehaviorGraphTicks;
            comparison.behaviorGraphTickReductionPercent = ReductionPercent(baseline.aiBehaviorGraphTicks, candidate.aiBehaviorGraphTicks);
            comparison.baselinePathRequests = baseline.navPathRequests;
            comparison.candidatePathRequests = candidate.navPathRequests;
            comparison.pathRequestReductionPercent = ReductionPercent(baseline.navPathRequests, candidate.navPathRequests);
            comparison.baselinePathSolves = baseline.navPathSolves;
            comparison.candidatePathSolves = candidate.navPathSolves;
            comparison.pathSolveReductionPercent = ReductionPercent(baseline.navPathSolves, candidate.navPathSolves);
            comparison.baselineReservationCandidates = ResolveTacticalCounter(baseline.tacticalDirectorSummary, "reservationCandidatesChecked");
            comparison.candidateReservationCandidates = ResolveTacticalCounter(candidate.tacticalDirectorSummary, "reservationCandidatesChecked");
            comparison.reservationCandidateReductionPercent = ReductionPercent(comparison.baselineReservationCandidates, comparison.candidateReservationCandidates);
            comparison.baselineReservationPathSolves = ResolveTacticalCounter(baseline.tacticalDirectorSummary, "reservationPathSolves");
            comparison.candidateReservationPathSolves = ResolveTacticalCounter(candidate.tacticalDirectorSummary, "reservationPathSolves");
            comparison.reservationPathSolveReductionPercent = ReductionPercent(comparison.baselineReservationPathSolves, comparison.candidateReservationPathSolves);
            comparison.baselineReservationCountersAvailable =
                comparison.baselineReservationCandidates > 0 ||
                comparison.baselineReservationPathSolves > 0;
            comparison.candidateCrowdReservationSkips = candidate.tacticalCrowdReservationSkips;
            comparison.candidateCrowdCachedIntentReuses = candidate.tacticalCrowdCachedIntentReuses;
            comparison.candidateCrowdScorerSkips = candidate.tacticalCrowdScorerSkips;

            if (candidate.passed)
            {
                comparison.passed = true;
                comparison.status = "PASS";
            }
            else
            {
                comparison.passed = false;
                comparison.status = "FAIL";
                comparison.failure = $"candidate scenario failed: {string.Join("; ", candidate.failures ?? Array.Empty<string>())}";
            }

            return comparison;
        }

        private static void WriteReport(PerformanceComparisonReport report, string jsonPath, string markdownPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath) ?? Root);
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(markdownPath, ToMarkdown(report));
            AssetDatabase.Refresh();
        }

        private static string ResolveLatestM138Path()
        {
            return File.Exists(DefaultM138CandidatePath) ? DefaultM138CandidatePath : FallbackM138CandidatePath;
        }

        private static string ResolveLatestM140Path()
        {
            return DefaultM140CandidatePath;
        }

        private static void CopyIfExists(string source, string destination)
        {
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
            {
                Debug.LogWarning($"Performance baseline source report is missing: {source}");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? BaselineRoot);
            File.Copy(source, destination, overwrite: true);
        }

        private static T ReadJson<T>(string path) where T : class
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read performance comparison input `{path}`: {exception.Message}");
                return null;
            }
        }

        private static M138CombatScaleStressScenarioSummary FindScenario(
            M138CombatScaleStressScenarioSummary[] scenarios,
            string scenarioId)
        {
            return (scenarios ?? Array.Empty<M138CombatScaleStressScenarioSummary>())
                .FirstOrDefault(scenario => string.Equals(scenario.scenarioId, scenarioId, StringComparison.Ordinal));
        }

        private static M140ScenarioSummary FindScenario(M140ScenarioSummary[] scenarios, string scenarioId)
        {
            return (scenarios ?? Array.Empty<M140ScenarioSummary>())
                .FirstOrDefault(scenario => string.Equals(scenario.scenarioId, scenarioId, StringComparison.Ordinal));
        }

        private static int ResolveTacticalCounter(M138CombatScaleStressScenarioSummary scenario, string key)
        {
            return ResolveTacticalCounter(scenario?.tacticalDirectorSummary, key);
        }

        private static int ResolveTacticalCounter(string summary, string key)
        {
            if (string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }

            var token = ResolveTacticalSummaryToken(key);
            var index = summary.IndexOf(token, StringComparison.Ordinal);
            if (index < 0 && !string.Equals(token, key + "=", StringComparison.Ordinal))
            {
                token = key + "=";
                index = summary.IndexOf(token, StringComparison.Ordinal);
            }

            if (index < 0)
            {
                return 0;
            }

            var start = index + token.Length;
            var end = start;
            while (end < summary.Length && char.IsDigit(summary[end]))
            {
                end++;
            }

            return end > start && int.TryParse(summary.Substring(start, end - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private static string ResolveTacticalSummaryToken(string key)
        {
            return key switch
            {
                "reservationCandidatesChecked" => "candidates=",
                "reservationPathSolves" => "pathSolves=",
                _ => key + "="
            };
        }

        private static double ReductionPercent(int baseline, int candidate)
        {
            if (baseline <= 0)
            {
                return candidate <= 0 ? 100d : 0d;
            }

            return Math.Max(0d, (baseline - candidate) * 100d / baseline);
        }

        private sealed class FocusedEditModeCallbacks : ScriptableObject, ICallbacks
        {
            private string outputPath = string.Empty;

            public ITestResultAdaptor Result { get; private set; }

            public void Configure(string nextOutputPath)
            {
                outputPath = nextOutputPath ?? string.Empty;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Result = result;
                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    TestRunnerApi.SaveResultToFile(result, outputPath);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
