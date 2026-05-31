using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Hollow.Core.Diagnostics;
using Hollow.Diagnostics;
using UnityEngine;

namespace Hollow.Performance
{
    [Serializable]
    public sealed class M138CombatScaleStressRunOptions
    {
        public string jsonReportPath = M138CombatScaleStressReportGenerator.DefaultJsonReportPath;
        public string markdownReportPath = M138CombatScaleStressReportGenerator.DefaultMarkdownReportPath;
        public float warmupSeconds = M138CombatScaleStressScenarioPolicy.FixedWarmupSeconds;
        public float sampleSeconds = M138CombatScaleStressScenarioPolicy.FixedSampleSeconds;
        public int targetFrameRate = M138CombatScaleStressScenarioPolicy.WindowsComfortTargetFrameRate;
        public bool writeReports = true;
        public bool enforceFrameTimingWhenTrusted = true;

        public static M138CombatScaleStressRunOptions FullGate(string jsonPath = null, string markdownPath = null)
        {
            return new M138CombatScaleStressRunOptions
            {
                jsonReportPath = string.IsNullOrWhiteSpace(jsonPath) ? M138CombatScaleStressReportGenerator.DefaultJsonReportPath : jsonPath,
                markdownReportPath = string.IsNullOrWhiteSpace(markdownPath) ? M138CombatScaleStressReportGenerator.DefaultMarkdownReportPath : markdownPath,
                warmupSeconds = M138CombatScaleStressScenarioPolicy.FixedWarmupSeconds,
                sampleSeconds = M138CombatScaleStressScenarioPolicy.FixedSampleSeconds,
                targetFrameRate = M138CombatScaleStressScenarioPolicy.WindowsComfortTargetFrameRate,
                writeReports = true,
                enforceFrameTimingWhenTrusted = true
            };
        }

        public static M138CombatScaleStressRunOptions SmokeGate(string jsonPath = null, string markdownPath = null)
        {
            return new M138CombatScaleStressRunOptions
            {
                jsonReportPath = string.IsNullOrWhiteSpace(jsonPath) ? M138CombatScaleStressReportGenerator.DefaultJsonReportPath : jsonPath,
                markdownReportPath = string.IsNullOrWhiteSpace(markdownPath) ? M138CombatScaleStressReportGenerator.DefaultMarkdownReportPath : markdownPath,
                warmupSeconds = 1f,
                sampleSeconds = 4f,
                targetFrameRate = M138CombatScaleStressScenarioPolicy.WindowsComfortTargetFrameRate,
                writeReports = true,
                enforceFrameTimingWhenTrusted = false
            };
        }
    }

    [Serializable]
    public sealed class M138CombatScaleStressFrameBudgetSummary
    {
        public int sampleFrameCount;
        public int aiThinkActiveFrameCount;
        public int navSolveActiveFrameCount;
        public int maxAiThinksInFrame;
        public int maxPathSolvesInFrame;
        public int maxPathRequestsInFrame;
        public int maxPathDeferredInFrame;
        public int maxScorerCallsInFrame;
        public int maxBehaviorGraphTicksInFrame;

        public void Observe(M136PerformanceOperationSnapshot previous, M136PerformanceOperationSnapshot current)
        {
            sampleFrameCount++;
            var aiThinks = Delta(current.AiThinkFull, previous.AiThinkFull) +
                Delta(current.AiThinkReduced, previous.AiThinkReduced) +
                Delta(current.AiThinkBackground, previous.AiThinkBackground);
            var pathSolves = Delta(current.NavPathSolves, previous.NavPathSolves);
            var pathRequests = Delta(current.NavPathRequests, previous.NavPathRequests);
            var pathDeferred = Delta(current.NavPathDeferred, previous.NavPathDeferred);
            var scorerCalls = Delta(current.AiScorerCalls, previous.AiScorerCalls);
            var graphTicks = Delta(current.AiBehaviorGraphTicks, previous.AiBehaviorGraphTicks);

            if (aiThinks > 0)
            {
                aiThinkActiveFrameCount++;
            }

            if (pathSolves > 0)
            {
                navSolveActiveFrameCount++;
            }

            maxAiThinksInFrame = Mathf.Max(maxAiThinksInFrame, aiThinks);
            maxPathSolvesInFrame = Mathf.Max(maxPathSolvesInFrame, pathSolves);
            maxPathRequestsInFrame = Mathf.Max(maxPathRequestsInFrame, pathRequests);
            maxPathDeferredInFrame = Mathf.Max(maxPathDeferredInFrame, pathDeferred);
            maxScorerCallsInFrame = Mathf.Max(maxScorerCallsInFrame, scorerCalls);
            maxBehaviorGraphTicksInFrame = Mathf.Max(maxBehaviorGraphTicksInFrame, graphTicks);
        }

        private static int Delta(int current, int previous)
        {
            return Mathf.Max(0, current - previous);
        }
    }

    [Serializable]
    public sealed class M138CombatScaleStressScenarioSummary
    {
        public string scenarioId;
        public string displayName;
        public string intent;
        public int targetEnemyCount;
        public int expectedPeakEnemies;
        public int peakActiveEnemies;
        public int peakProjectiles;
        public int projectileActivePeak;
        public int projectileSpawns;
        public int projectileReturns;
        public int projectileCollisionChecks;
        public int projectilePoolMisses;
        public int projectileHardInstantiates;
        public float projectileUpdateMaxMilliseconds;
        public bool projectileHeavy;
        public bool bossPresent;
        public bool observedBoss;
        public bool bossFullLodObserved;
        public bool reducedOrBackgroundAddObserved;
        public bool timingAuthoritative;
        public string frameCadenceConfidence;
        public int rawSampleCount;
        public double sampleRateHz;
        public double frameP50Ms;
        public double frameP95Ms;
        public double frameMaxMs;
        public long gcMaxBytes;
        public int aiThinkFull;
        public int aiThinkReduced;
        public int aiThinkBackground;
        public int aiCommandReuses;
        public int aiScorerCalls;
        public int aiBehaviorGraphTicks;
        public int aiLodTransitions;
        public int navPathRequests;
        public int navPathSolves;
        public int navPathDeferred;
        public int navPathFallbacks;
        public int runtimeNavMeshFallbacks;
        public float navPathMaxSolveMilliseconds;
        public int avoidanceHigh;
        public int avoidanceReduced;
        public int avoidanceBackground;
        public int sampleFrameCount;
        public int aiThinkActiveFrameCount;
        public int navSolveActiveFrameCount;
        public int maxAiThinksInFrame;
        public int maxPathSolvesInFrame;
        public int maxPathRequestsInFrame;
        public int maxPathDeferredInFrame;
        public int maxScorerCallsInFrame;
        public int maxBehaviorGraphTicksInFrame;
        public bool passed;
        public string[] failures;
        public string note;
    }

    [Serializable]
    public sealed class M138CombatScaleStressReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public bool passed;
        public int scenarioCount;
        public string[] failures;
        public M138CombatScaleStressScenarioSummary[] scenarios;
    }

    public static class M138CombatScaleStressReportGenerator
    {
        public const string DefaultJsonReportPath = "output/reports/m138_combat_scale_stress.json";
        public const string DefaultMarkdownReportPath = "output/reports/m138_combat_scale_stress.md";
        public const string CaptureMode = "m138-automated-playmode";
        public const double WindowsFrameTimeBudgetMs = 16.7d;
        public const double RecurringGcP95BudgetBytes = 1024d;
        public const int ProjectileHeavyMinimumPeakProjectiles = M136EditorLaptopPerformancePolicy.ProjectileHeavyMinimumPeakProjectiles;

        public static M136PerformanceScenarioDefinition ToM136ScenarioDefinition(M138CombatScaleStressScenarioDefinition scenario, M138CombatScaleStressRunOptions options = null)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            return new M136PerformanceScenarioDefinition
            {
                id = scenario.id,
                displayName = scenario.displayName,
                intent = scenario.intent,
                warmupSeconds = Mathf.Max(0f, options?.warmupSeconds ?? scenario.warmupSeconds),
                sampleSeconds = Mathf.Max(0.1f, options?.sampleSeconds ?? scenario.sampleSeconds)
            };
        }

        public static M138CombatScaleStressReport BuildReport(IEnumerable<M138CombatScaleStressScenarioSummary> scenarioSummaries)
        {
            var summaries = scenarioSummaries?.Where(summary => summary != null).ToArray() ?? Array.Empty<M138CombatScaleStressScenarioSummary>();
            var failures = new List<string>();
            var manifest = M138CombatScaleStressScenarioPolicy.StressManifest;
            foreach (var scenario in manifest)
            {
                if (!summaries.Any(summary => string.Equals(summary.scenarioId, scenario.id, StringComparison.Ordinal)))
                {
                    failures.Add($"Missing M138 automated capture for `{scenario.id}`.");
                }
            }

            foreach (var summary in summaries)
            {
                if (!summary.passed)
                {
                    failures.Add($"{summary.scenarioId}: {string.Join("; ", summary.failures ?? Array.Empty<string>())}");
                }
            }

            return new M138CombatScaleStressReport
            {
                lockId = M138CombatScaleStressScenarioPolicy.LockId,
                title = "M138 Combat Scale Stress Gate",
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                passed = failures.Count == 0 && summaries.Length == manifest.Length,
                scenarioCount = summaries.Length,
                failures = failures.ToArray(),
                scenarios = summaries
            };
        }

        public static M138CombatScaleStressScenarioSummary BuildScenarioSummary(
            M138CombatScaleStressScenarioDefinition scenario,
            M136PerformanceScenarioResult result,
            M138CombatScaleStressFrameBudgetSummary frameBudget,
            bool bossFullLodObserved,
            bool reducedOrBackgroundAddObserved,
            bool enforceFrameTimingWhenTrusted = true)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var operations = result.operations ?? new M136RuntimeOperationSummary();
            var objectCounts = result.objectCounts ?? new M136LiveObjectCountSummary();
            var frameMetric = Metric(result, "frame_time_ms");
            var gcMetric = Metric(result, "gc_allocated_bytes");
            var timingAuthoritative = string.Equals(result.samplingSource, M136FrameCadencePolicy.RuntimeUpdateSamplingSource, StringComparison.Ordinal) &&
                string.Equals(result.frameCadenceConfidence, M136FrameCadencePolicy.Trusted, StringComparison.Ordinal) &&
                frameMetric.supported &&
                frameMetric.sampleCount > 0;
            var expectedEnemies = scenario.targetEnemyCount + (scenario.includesBoss ? 1 : 0);
            var failures = new List<string>();

            if (objectCounts.peakEnemies < expectedEnemies)
            {
                failures.Add($"Peak active enemies {objectCounts.peakEnemies} is below expected {expectedEnemies}.");
            }

            if (scenario.id == "enemy_stress_30" && objectCounts.peakEnemies < 30)
            {
                failures.Add("Primary 30-enemy gate did not reach 30 active enemies.");
            }

            var projectilePeak = Mathf.Max(objectCounts.peakProjectiles, operations.projectileActivePeak);
            if (scenario.projectileHeavy && projectilePeak < ProjectileHeavyMinimumPeakProjectiles)
            {
                failures.Add($"Projectile-heavy scenario peak projectiles {projectilePeak} is below required {ProjectileHeavyMinimumPeakProjectiles}.");
            }

            if (scenario.includesBoss)
            {
                if (!objectCounts.observedBoss)
                {
                    failures.Add("Boss scenario did not observe an active boss.");
                }

                if (!bossFullLodObserved)
                {
                    failures.Add("Boss scenario did not keep the boss at Full AI LOD.");
                }

                if (!reducedOrBackgroundAddObserved)
                {
                    failures.Add("Boss scenario did not downgrade any adds before the boss.");
                }
            }

            if (operations.runtimeNavMeshFallbacks != 0)
            {
                failures.Add($"Runtime NavMesh fallback count during sample is {operations.runtimeNavMeshFallbacks}; expected 0.");
            }

            var pathBudget = M137PerformanceComfortPolicy.M3NavMeshPathSolveBudgetPerFrame;
            if (frameBudget != null && frameBudget.maxPathSolvesInFrame > pathBudget)
            {
                failures.Add($"Max path solves in one frame {frameBudget.maxPathSolvesInFrame} exceeds budget {pathBudget}.");
            }

            var totalAiThinks = operations.aiThinkFull + operations.aiThinkReduced + operations.aiThinkBackground;
            if (objectCounts.peakEnemies > 0 && totalAiThinks <= 0)
            {
                failures.Add("No AI think counters were recorded during the stress sample.");
            }

            if (frameBudget != null && frameBudget.sampleFrameCount > 2 && totalAiThinks > 0)
            {
                var allowedBurst = Mathf.Max(6, Mathf.CeilToInt(objectCounts.peakEnemies * 0.5f));
                if (frameBudget.maxAiThinksInFrame > allowedBurst)
                {
                    failures.Add($"Max AI thinks in one frame {frameBudget.maxAiThinksInFrame} exceeds burst allowance {allowedBurst}.");
                }

                if (frameBudget.aiThinkActiveFrameCount <= 1)
                {
                    failures.Add("AI think activity clustered into one sampled frame instead of being staggered.");
                }
            }

            if (scenario.targetEnemyCount >= 20 && operations.aiThinkFull > 0)
            {
                var lowerTierThinks = operations.aiThinkReduced + operations.aiThinkBackground + operations.aiCommandReuses;
                if (lowerTierThinks <= 0)
                {
                    failures.Add("Crowded scenario did not show Reduced/Background LOD thinking or command reuse.");
                }
            }

            if (timingAuthoritative && enforceFrameTimingWhenTrusted && frameMetric.p95 > WindowsFrameTimeBudgetMs)
            {
                failures.Add($"Trusted frame p95 {frameMetric.p95:0.00} ms exceeds {WindowsFrameTimeBudgetMs:0.0} ms.");
            }

            if (gcMetric.supported && gcMetric.max > 0d && gcMetric.p95 > RecurringGcP95BudgetBytes)
            {
                failures.Add($"Recurring GC p95 is {gcMetric.p95:0} bytes; expected near-zero after warmup.");
            }

            var note = timingAuthoritative
                ? "Frame timing is trusted for this automated PlayMode sample."
                : "Editor timing is directional; deterministic M138 counter gates are authoritative.";
            return new M138CombatScaleStressScenarioSummary
            {
                scenarioId = scenario.id,
                displayName = scenario.displayName,
                intent = scenario.intent,
                targetEnemyCount = scenario.targetEnemyCount,
                expectedPeakEnemies = expectedEnemies,
                peakActiveEnemies = objectCounts.peakEnemies,
                peakProjectiles = projectilePeak,
                projectileActivePeak = operations.projectileActivePeak,
                projectileSpawns = operations.projectileSpawns,
                projectileReturns = operations.projectileReturns,
                projectileCollisionChecks = operations.projectileCollisionChecks,
                projectilePoolMisses = operations.projectilePoolMisses,
                projectileHardInstantiates = operations.projectileHardInstantiates,
                projectileUpdateMaxMilliseconds = operations.projectileUpdateMaxMilliseconds,
                projectileHeavy = scenario.projectileHeavy,
                bossPresent = scenario.includesBoss,
                observedBoss = objectCounts.observedBoss,
                bossFullLodObserved = bossFullLodObserved,
                reducedOrBackgroundAddObserved = reducedOrBackgroundAddObserved,
                timingAuthoritative = timingAuthoritative,
                frameCadenceConfidence = result.frameCadenceConfidence,
                rawSampleCount = result.rawSampleCount,
                sampleRateHz = result.sampleRateHz,
                frameP50Ms = frameMetric.p50,
                frameP95Ms = frameMetric.p95,
                frameMaxMs = frameMetric.max,
                gcMaxBytes = gcMetric.supported ? (long)Math.Round(gcMetric.max) : 0,
                aiThinkFull = operations.aiThinkFull,
                aiThinkReduced = operations.aiThinkReduced,
                aiThinkBackground = operations.aiThinkBackground,
                aiCommandReuses = operations.aiCommandReuses,
                aiScorerCalls = operations.aiScorerCalls,
                aiBehaviorGraphTicks = operations.aiBehaviorGraphTicks,
                aiLodTransitions = operations.aiLodTransitions,
                navPathRequests = operations.navPathRequests,
                navPathSolves = operations.navPathSolves,
                navPathDeferred = operations.navPathDeferred,
                navPathFallbacks = operations.navPathFallbacks,
                runtimeNavMeshFallbacks = operations.runtimeNavMeshFallbacks,
                navPathMaxSolveMilliseconds = operations.navPathMaxSolveMilliseconds,
                avoidanceHigh = operations.avoidanceHigh,
                avoidanceReduced = operations.avoidanceReduced,
                avoidanceBackground = operations.avoidanceBackground,
                sampleFrameCount = frameBudget?.sampleFrameCount ?? 0,
                aiThinkActiveFrameCount = frameBudget?.aiThinkActiveFrameCount ?? 0,
                navSolveActiveFrameCount = frameBudget?.navSolveActiveFrameCount ?? 0,
                maxAiThinksInFrame = frameBudget?.maxAiThinksInFrame ?? 0,
                maxPathSolvesInFrame = frameBudget?.maxPathSolvesInFrame ?? 0,
                maxPathRequestsInFrame = frameBudget?.maxPathRequestsInFrame ?? 0,
                maxPathDeferredInFrame = frameBudget?.maxPathDeferredInFrame ?? 0,
                maxScorerCallsInFrame = frameBudget?.maxScorerCallsInFrame ?? 0,
                maxBehaviorGraphTicksInFrame = frameBudget?.maxBehaviorGraphTicksInFrame ?? 0,
                passed = failures.Count == 0,
                failures = failures.ToArray(),
                note = note
            };
        }

        public static void WriteReport(M138CombatScaleStressReport report, string jsonPath = null, string markdownPath = null)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var resolvedJsonPath = string.IsNullOrWhiteSpace(jsonPath) ? DefaultJsonReportPath : jsonPath;
            var resolvedMarkdownPath = string.IsNullOrWhiteSpace(markdownPath) ? DefaultMarkdownReportPath : markdownPath;
            CreateParentDirectory(resolvedJsonPath);
            CreateParentDirectory(resolvedMarkdownPath);
            File.WriteAllText(resolvedJsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(resolvedMarkdownPath, ToMarkdown(report));
        }

        public static string ToMarkdown(M138CombatScaleStressReport report)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("# M138 Combat Scale Stress Gate");
            builder.AppendLine();
            builder.AppendLine($"- Lock: `{report?.lockId ?? M138CombatScaleStressScenarioPolicy.LockId}`");
            builder.AppendLine($"- Status: {(report != null && report.passed ? "PASS" : "FAIL")}");
            builder.AppendLine($"- Scenarios: {report?.scenarioCount ?? 0}/{M138CombatScaleStressScenarioPolicy.StressManifest.Length}");
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

            builder.AppendLine("## Scenario Results");
            foreach (var scenario in report?.scenarios ?? Array.Empty<M138CombatScaleStressScenarioSummary>())
            {
                builder.AppendLine($"### {scenario.displayName}");
                builder.AppendLine($"- Status: {(scenario.passed ? "PASS" : "FAIL")}");
                builder.AppendLine($"- Enemies: peak {scenario.peakActiveEnemies}, expected {scenario.expectedPeakEnemies}");
                builder.AppendLine($"- Projectiles: peak {scenario.peakProjectiles}, spawns {scenario.projectileSpawns}, returns {scenario.projectileReturns}, collision checks {scenario.projectileCollisionChecks}, pool misses {scenario.projectilePoolMisses}, hard instantiates {scenario.projectileHardInstantiates}, update max {scenario.projectileUpdateMaxMilliseconds:0.###} ms");
                builder.AppendLine($"- Frame p95/max: {scenario.frameP95Ms:0.00} ms / {scenario.frameMaxMs:0.00} ms ({scenario.frameCadenceConfidence})");
                builder.AppendLine($"- AI: full {scenario.aiThinkFull}, reduced {scenario.aiThinkReduced}, background {scenario.aiThinkBackground}, reuse {scenario.aiCommandReuses}, scorer {scenario.aiScorerCalls}");
                builder.AppendLine($"- Nav: requests {scenario.navPathRequests}, solves {scenario.navPathSolves}, deferred {scenario.navPathDeferred}, fallback {scenario.navPathFallbacks}, max solves/frame {scenario.maxPathSolvesInFrame}");
                builder.AppendLine($"- Avoidance: high {scenario.avoidanceHigh}, reduced {scenario.avoidanceReduced}, background {scenario.avoidanceBackground}");
                builder.AppendLine($"- Note: {scenario.note}");
                if (scenario.failures != null && scenario.failures.Length > 0)
                {
                    foreach (var failure in scenario.failures)
                    {
                        builder.AppendLine($"- Failure: {failure}");
                    }
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static M136PerformanceMetricSummary Metric(M136PerformanceScenarioResult result, string id)
        {
            return (result.metrics ?? Array.Empty<M136PerformanceMetricSummary>()).FirstOrDefault(metric => string.Equals(metric.id, id, StringComparison.Ordinal)) ??
                new M136PerformanceMetricSummary { id = id, supported = false };
        }

        private static void CreateParentDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
