using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Diagnostics;
using UnityEngine;

namespace Hollow.Performance
{
    public static class AutomatedTruthGateMode
    {
        public const string Smoke = "smoke";
        public const string Full = "full";
        public const string BuiltPlayer = "built-player";
    }

    [Serializable]
    public sealed class AutomatedTruthGateOptions
    {
        public string mode = AutomatedTruthGateMode.Smoke;
        public string jsonReportPath = AutomatedTruthGateReportGenerator.DefaultJsonReportPath;
        public string markdownReportPath = AutomatedTruthGateReportGenerator.DefaultMarkdownReportPath;
        public bool includeM138 = true;
        public bool includeM139 = true;
        public bool includeBuiltPlayer;
        public bool writeReports = true;

        public bool IsFull => string.Equals(mode, AutomatedTruthGateMode.Full, StringComparison.OrdinalIgnoreCase);
        public bool IsBuiltPlayer => string.Equals(mode, AutomatedTruthGateMode.BuiltPlayer, StringComparison.OrdinalIgnoreCase);

        public static AutomatedTruthGateOptions SmokeGate(string jsonPath = null, string markdownPath = null)
        {
            return new AutomatedTruthGateOptions
            {
                mode = AutomatedTruthGateMode.Smoke,
                jsonReportPath = string.IsNullOrWhiteSpace(jsonPath) ? AutomatedTruthGateReportGenerator.JsonReportPathForMode(AutomatedTruthGateMode.Smoke) : jsonPath,
                markdownReportPath = string.IsNullOrWhiteSpace(markdownPath) ? AutomatedTruthGateReportGenerator.MarkdownReportPathForMode(AutomatedTruthGateMode.Smoke) : markdownPath,
                includeM138 = true,
                includeM139 = true,
                includeBuiltPlayer = false,
                writeReports = true
            };
        }

        public static AutomatedTruthGateOptions FullGate(string jsonPath = null, string markdownPath = null)
        {
            return new AutomatedTruthGateOptions
            {
                mode = AutomatedTruthGateMode.Full,
                jsonReportPath = string.IsNullOrWhiteSpace(jsonPath) ? AutomatedTruthGateReportGenerator.JsonReportPathForMode(AutomatedTruthGateMode.Full) : jsonPath,
                markdownReportPath = string.IsNullOrWhiteSpace(markdownPath) ? AutomatedTruthGateReportGenerator.MarkdownReportPathForMode(AutomatedTruthGateMode.Full) : markdownPath,
                includeM138 = true,
                includeM139 = true,
                includeBuiltPlayer = false,
                writeReports = true
            };
        }

        public static AutomatedTruthGateOptions BuiltPlayerGate(string jsonPath = null, string markdownPath = null)
        {
            return new AutomatedTruthGateOptions
            {
                mode = AutomatedTruthGateMode.BuiltPlayer,
                jsonReportPath = string.IsNullOrWhiteSpace(jsonPath) ? AutomatedTruthGateReportGenerator.JsonReportPathForMode(AutomatedTruthGateMode.BuiltPlayer) : jsonPath,
                markdownReportPath = string.IsNullOrWhiteSpace(markdownPath) ? AutomatedTruthGateReportGenerator.MarkdownReportPathForMode(AutomatedTruthGateMode.BuiltPlayer) : markdownPath,
                includeM138 = false,
                includeM139 = false,
                includeBuiltPlayer = true,
                writeReports = true
            };
        }
    }

    [Serializable]
    public sealed class AutomatedTruthGateStageSummary
    {
        public string stageId;
        public string displayName;
        public string sourceLockId;
        public string result;
        public bool passed;
        public bool blockedByEnvironment;
        public bool timingAuthoritative;
        public int scenarioCount;
        public double durationMs;
        public string jsonArtifactPath;
        public string markdownArtifactPath;
        public string[] failures = Array.Empty<string>();
        public string note;
    }

    [Serializable]
    public sealed class AutomatedTruthGateReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public string mode;
        public string result;
        public bool passed;
        public bool environmentBlocked;
        public int stageCount;
        public string[] failures = Array.Empty<string>();
        public AutomatedTruthGateStageSummary[] stages = Array.Empty<AutomatedTruthGateStageSummary>();
    }

    public static class AutomatedTruthGatePlayModeRunner
    {
        public static IEnumerator Run(AutomatedTruthGateOptions options, Action<AutomatedTruthGateReport> onComplete = null)
        {
            options ??= AutomatedTruthGateOptions.SmokeGate();
            var stages = new List<AutomatedTruthGateStageSummary>();
            if (options.includeM138)
            {
                var json = StageJsonPath("m138", options.mode);
                var markdown = StageMarkdownPath("m138", options.mode);
                var stopwatch = Stopwatch.StartNew();
                M138CombatScaleStressReport report = null;
                var runOptions = options.IsFull
                    ? M138CombatScaleStressRunOptions.FullGate(json, markdown)
                    : M138CombatScaleStressRunOptions.SmokeGate(json, markdown);
                runOptions.enforceFrameTimingWhenTrusted = false;
                yield return M138CombatScaleStressRunner.RunAllScenarios(runOptions, next => report = next);
                stopwatch.Stop();
                stages.Add(AutomatedTruthGateReportGenerator.FromM138Report(report, json, markdown, stopwatch.Elapsed.TotalMilliseconds));
            }

            if (options.includeM139)
            {
                var json = StageJsonPath("m139", options.mode);
                var markdown = StageMarkdownPath("m139", options.mode);
                var stopwatch = Stopwatch.StartNew();
                M139LongRunSoakReport report = null;
                var runOptions = options.IsFull
                    ? M139LongRunSoakOptions.FullGate(json, markdown)
                    : M139LongRunSoakOptions.SmokeGate(json, markdown);
                yield return M139LongRunSoakRunner.RunAllScenarios(runOptions, next => report = next);
                stopwatch.Stop();
                stages.Add(AutomatedTruthGateReportGenerator.FromM139Report(report, json, markdown, stopwatch.Elapsed.TotalMilliseconds));
            }

            var combined = AutomatedTruthGateReportGenerator.BuildReport(options, stages);
            if (options.writeReports)
            {
                AutomatedTruthGateReportGenerator.WriteReport(combined, options.jsonReportPath, options.markdownReportPath);
            }

            onComplete?.Invoke(combined);
        }

        private static string StageJsonPath(string stagePrefix, string mode)
        {
            return Path.Combine("output/reports/truth_gate", $"{stagePrefix}_{SafeMode(mode)}.json");
        }

        private static string StageMarkdownPath(string stagePrefix, string mode)
        {
            return Path.Combine("output/reports/truth_gate", $"{stagePrefix}_{SafeMode(mode)}.md");
        }

        private static string SafeMode(string mode)
        {
            return string.IsNullOrWhiteSpace(mode) ? AutomatedTruthGateMode.Smoke : mode.Replace(' ', '_');
        }
    }

    public static class AutomatedTruthGateReportGenerator
    {
        public const string LockId = "AutomatedTruthGate";
        public const string ReportDirectory = "output/reports/truth_gate";
        public const string DefaultJsonReportPath = "output/reports/truth_gate/automated_truth_gate_smoke.json";
        public const string DefaultMarkdownReportPath = "output/reports/truth_gate/automated_truth_gate_smoke.md";

        public static string JsonReportPathForMode(string mode)
        {
            return Path.Combine(ReportDirectory, $"automated_truth_gate_{SafeMode(mode)}.json");
        }

        public static string MarkdownReportPathForMode(string mode)
        {
            return Path.Combine(ReportDirectory, $"automated_truth_gate_{SafeMode(mode)}.md");
        }

        public static AutomatedTruthGateStageSummary FromM138Report(
            M138CombatScaleStressReport report,
            string jsonArtifactPath,
            string markdownArtifactPath,
            double durationMs)
        {
            var failures = new List<string>();
            if (report == null)
            {
                failures.Add("M138 report was not produced.");
            }
            else
            {
                if (!report.passed)
                {
                    failures.AddRange(report.failures ?? Array.Empty<string>());
                }

                if (report.scenarioCount != M138CombatScaleStressScenarioPolicy.StressManifest.Length)
                {
                    failures.Add($"M138 scenario count {report.scenarioCount} does not match manifest count {M138CombatScaleStressScenarioPolicy.StressManifest.Length}.");
                }

                foreach (var scenario in report.scenarios ?? Array.Empty<M138CombatScaleStressScenarioSummary>())
                {
                    foreach (var failure in scenario.failures ?? Array.Empty<string>())
                    {
                        failures.Add($"{scenario.scenarioId}: {failure}");
                    }
                }

                var enemy30 = FindScenario(report.scenarios, "enemy_stress_30");
                if (enemy30 == null || enemy30.peakActiveEnemies < 30)
                {
                    failures.Add($"enemy_stress_30 peak enemies {enemy30?.peakActiveEnemies ?? 0}; expected >= 30.");
                }

                var projectile = FindScenario(report.scenarios, "projectile_heavy_room");
                if (projectile == null || projectile.projectileActivePeak < M138CombatScaleStressReportGenerator.ProjectileHeavyMinimumPeakProjectiles)
                {
                    failures.Add($"projectile_heavy_room projectile peak {projectile?.projectileActivePeak ?? 0}; expected >= {M138CombatScaleStressReportGenerator.ProjectileHeavyMinimumPeakProjectiles}.");
                }

                var boss = FindScenario(report.scenarios, "boss_plus_adds");
                if (boss == null || !boss.observedBoss || !boss.bossFullLodObserved)
                {
                    failures.Add("boss_plus_adds did not prove boss presence with Full LOD.");
                }
            }

            return Stage(
                "m138_combat_scale_stress",
                "M138 Combat Scale Stress",
                "M138",
                failures,
                report?.scenarioCount ?? 0,
                timingAuthoritative: false,
                jsonArtifactPath,
                markdownArtifactPath,
                durationMs,
                "Editor PlayMode combat stress; timing is diagnostic unless cadence is trusted per scenario.");
        }

        public static AutomatedTruthGateStageSummary FromM139Report(
            M139LongRunSoakReport report,
            string jsonArtifactPath,
            string markdownArtifactPath,
            double durationMs)
        {
            var failures = new List<string>();
            if (report == null)
            {
                failures.Add("M139 report was not produced.");
            }
            else
            {
                if (!report.passed)
                {
                    failures.AddRange(report.failures ?? Array.Empty<string>());
                }

                if (report.scenarioCount != M139LongRunSoakReportGenerator.ScenarioIds.Length)
                {
                    failures.Add($"M139 scenario count {report.scenarioCount} does not match required count {M139LongRunSoakReportGenerator.ScenarioIds.Length}.");
                }

                foreach (var scenario in report.scenarios ?? Array.Empty<M139LongRunSoakScenarioSummary>())
                {
                    foreach (var failure in scenario.failures ?? Array.Empty<string>())
                    {
                        failures.Add($"{scenario.scenarioId}: {failure}");
                    }

                    if (scenario.normalTraversalColdCacheMissesAfterLoad != 0)
                    {
                        failures.Add($"{scenario.scenarioId}: cold-cache misses after load {scenario.normalTraversalColdCacheMissesAfterLoad}; expected 0.{AttributionSuffix(scenario.cacheMissAttributionSummary)}");
                    }

                    if (scenario.runtimeNavMeshFallbacks != 0)
                    {
                        failures.Add($"{scenario.scenarioId}: runtime NavMesh fallbacks {scenario.runtimeNavMeshFallbacks}; expected 0.");
                    }

                    if (scenario.staleEnemyStateFailures != 0 || scenario.staleRuntimePoolStateFailures != 0)
                    {
                        failures.Add($"{scenario.scenarioId}: stale pooled state enemy={scenario.staleEnemyStateFailures}, runtime={scenario.staleRuntimePoolStateFailures}; expected 0.");
                    }

                    if (scenario.poolActiveLeaks != 0)
                    {
                        failures.Add($"{scenario.scenarioId}: pool active leaks {scenario.poolActiveLeaks}; expected 0.");
                    }

                    if (scenario.enemyPoolHardInstantiatesAfterWarmup != 0 || scenario.runtimePoolHardInstantiatesAfterWarmup != 0)
                    {
                        failures.Add($"{scenario.scenarioId}: post-warmup hard instantiates enemy={scenario.enemyPoolHardInstantiatesAfterWarmup}, runtime={scenario.runtimePoolHardInstantiatesAfterWarmup}; expected 0.");
                    }
                }
            }

            return Stage(
                "m139_long_run_soak",
                "M139 Long-Run Cache, Pool, And Memory Soak",
                "M139",
                failures,
                report?.scenarioCount ?? 0,
                timingAuthoritative: false,
                jsonArtifactPath,
                markdownArtifactPath,
                durationMs,
                "Editor PlayMode branch soak; deterministic cache/pool gates are authoritative.");
        }

        public static AutomatedTruthGateStageSummary FromM140EditorResult(
            string result,
            int scenarioCount,
            string[] failures,
            string jsonArtifactPath,
            string markdownArtifactPath,
            double durationMs)
        {
            var blocked = string.Equals(result, M140GateResult.BlockedByEnvironment, StringComparison.Ordinal);
            return new AutomatedTruthGateStageSummary
            {
                stageId = "m140_build_real_gate",
                displayName = "M140 Build-Real macOS Apple Silicon Gate",
                sourceLockId = "M140",
                result = blocked ? M140GateResult.BlockedByEnvironment : string.Equals(result, M140GateResult.Passed, StringComparison.Ordinal) ? M140GateResult.Passed : M140GateResult.Failed,
                passed = string.Equals(result, M140GateResult.Passed, StringComparison.Ordinal),
                blockedByEnvironment = blocked,
                timingAuthoritative = true,
                scenarioCount = Math.Max(0, scenarioCount),
                durationMs = Math.Max(0d, durationMs),
                jsonArtifactPath = jsonArtifactPath ?? string.Empty,
                markdownArtifactPath = markdownArtifactPath ?? string.Empty,
                failures = failures ?? Array.Empty<string>(),
                note = "Built-player development capture is the local CPU truth gate."
            };
        }

        public static AutomatedTruthGateReport BuildReport(AutomatedTruthGateOptions options, IEnumerable<AutomatedTruthGateStageSummary> stageSummaries)
        {
            options ??= AutomatedTruthGateOptions.SmokeGate();
            var stages = (stageSummaries ?? Array.Empty<AutomatedTruthGateStageSummary>())
                .Where(stage => stage != null)
                .ToArray();
            var failures = new List<string>();
            var blocked = false;
            foreach (var stage in stages)
            {
                if (stage.blockedByEnvironment)
                {
                    blocked = true;
                    failures.Add($"{stage.stageId}: blocked by environment.");
                }

                if (!stage.passed && !stage.blockedByEnvironment)
                {
                    failures.AddRange((stage.failures ?? Array.Empty<string>())
                        .Select(failure => $"{stage.stageId}: {failure}"));
                    if ((stage.failures ?? Array.Empty<string>()).Length == 0)
                    {
                        failures.Add($"{stage.stageId}: failed without a detailed failure message.");
                    }
                }
            }

            var result = failures.Count == 0
                ? M140GateResult.Passed
                : blocked && stages.All(stage => stage.passed || stage.blockedByEnvironment)
                    ? M140GateResult.BlockedByEnvironment
                    : M140GateResult.Failed;
            return new AutomatedTruthGateReport
            {
                lockId = LockId,
                title = "Automated Truth Gate",
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                mode = options.mode ?? AutomatedTruthGateMode.Smoke,
                result = result,
                passed = string.Equals(result, M140GateResult.Passed, StringComparison.Ordinal),
                environmentBlocked = string.Equals(result, M140GateResult.BlockedByEnvironment, StringComparison.Ordinal),
                stageCount = stages.Length,
                failures = failures.ToArray(),
                stages = stages
            };
        }

        public static void WriteReport(AutomatedTruthGateReport report, string jsonPath = null, string markdownPath = null)
        {
            jsonPath = string.IsNullOrWhiteSpace(jsonPath) ? DefaultJsonReportPath : jsonPath;
            markdownPath = string.IsNullOrWhiteSpace(markdownPath) ? DefaultMarkdownReportPath : markdownPath;
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath) ?? ".");
            Directory.CreateDirectory(Path.GetDirectoryName(markdownPath) ?? ".");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, prettyPrint: true));
            File.WriteAllText(markdownPath, ToMarkdown(report));
        }

        public static string ToMarkdown(AutomatedTruthGateReport report)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# Automated Truth Gate");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report?.result ?? "NotRun"}");
            builder.AppendLine($"- Mode: {report?.mode ?? string.Empty}");
            builder.AppendLine($"- Generated: {report?.generatedAtUtc ?? string.Empty}");
            builder.AppendLine($"- Stages: {report?.stageCount ?? 0}");
            builder.AppendLine();

            foreach (var stage in report?.stages ?? Array.Empty<AutomatedTruthGateStageSummary>())
            {
                builder.AppendLine($"## {stage.displayName}");
                builder.AppendLine();
                builder.AppendLine($"- Result: {stage.result}");
                builder.AppendLine($"- Scenarios: {stage.scenarioCount}");
                builder.AppendLine($"- Duration: {stage.durationMs:0} ms");
                builder.AppendLine($"- Timing authoritative: {(stage.timingAuthoritative ? "yes" : "no")}");
                builder.AppendLine($"- JSON: {stage.jsonArtifactPath}");
                builder.AppendLine($"- Markdown: {stage.markdownArtifactPath}");
                if (!string.IsNullOrWhiteSpace(stage.note))
                {
                    builder.AppendLine($"- Note: {stage.note}");
                }

                var failures = stage.failures ?? Array.Empty<string>();
                if (failures.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("Failures:");
                    foreach (var failure in failures)
                    {
                        builder.AppendLine($"- {failure}");
                    }
                }

                builder.AppendLine();
            }

            var reportFailures = report?.failures ?? Array.Empty<string>();
            if (reportFailures.Length > 0)
            {
                builder.AppendLine("## Combined Failures");
                builder.AppendLine();
                foreach (var failure in reportFailures)
                {
                    builder.AppendLine($"- {failure}");
                }
            }

            return builder.ToString();
        }

        private static AutomatedTruthGateStageSummary Stage(
            string stageId,
            string displayName,
            string sourceLockId,
            List<string> failures,
            int scenarioCount,
            bool timingAuthoritative,
            string jsonArtifactPath,
            string markdownArtifactPath,
            double durationMs,
            string note)
        {
            failures ??= new List<string>();
            return new AutomatedTruthGateStageSummary
            {
                stageId = stageId,
                displayName = displayName,
                sourceLockId = sourceLockId,
                result = failures.Count == 0 ? M140GateResult.Passed : M140GateResult.Failed,
                passed = failures.Count == 0,
                blockedByEnvironment = false,
                timingAuthoritative = timingAuthoritative,
                scenarioCount = Math.Max(0, scenarioCount),
                durationMs = Math.Max(0d, durationMs),
                jsonArtifactPath = jsonArtifactPath ?? string.Empty,
                markdownArtifactPath = markdownArtifactPath ?? string.Empty,
                failures = failures.Distinct(StringComparer.Ordinal).ToArray(),
                note = note ?? string.Empty
            };
        }

        private static M138CombatScaleStressScenarioSummary FindScenario(M138CombatScaleStressScenarioSummary[] scenarios, string scenarioId)
        {
            return (scenarios ?? Array.Empty<M138CombatScaleStressScenarioSummary>())
                .FirstOrDefault(scenario => string.Equals(scenario.scenarioId, scenarioId, StringComparison.Ordinal));
        }

        private static string AttributionSuffix(string attribution)
        {
            return string.IsNullOrWhiteSpace(attribution) ? string.Empty : $" Misses: {attribution}";
        }

        private static string SafeMode(string mode)
        {
            return string.IsNullOrWhiteSpace(mode)
                ? AutomatedTruthGateMode.Smoke
                : mode.Replace(' ', '_').Replace('/', '_');
        }
    }
}
