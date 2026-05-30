using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using UnityEngine;

namespace Hollow.Performance
{
    [Serializable]
    public sealed class M139LongRunSoakOptions
    {
        public string jsonReportPath = M139LongRunSoakReportGenerator.DefaultJsonReportPath;
        public string markdownReportPath = M139LongRunSoakReportGenerator.DefaultMarkdownReportPath;
        public int branches = 4;
        public int traversalsPerBranch = 8;
        public int targetFrameRate = 60;
        public bool writeReports = true;
        public bool enforceTiming = true;
        public bool ciSmoke;

        public static M139LongRunSoakOptions FullGate(string jsonPath = null, string markdownPath = null)
        {
            return new M139LongRunSoakOptions
            {
                jsonReportPath = string.IsNullOrWhiteSpace(jsonPath) ? M139LongRunSoakReportGenerator.DefaultJsonReportPath : jsonPath,
                markdownReportPath = string.IsNullOrWhiteSpace(markdownPath) ? M139LongRunSoakReportGenerator.DefaultMarkdownReportPath : markdownPath,
                branches = 4,
                traversalsPerBranch = 8,
                targetFrameRate = 60,
                writeReports = true,
                enforceTiming = true,
                ciSmoke = false
            };
        }

        public static M139LongRunSoakOptions SmokeGate(string jsonPath = null, string markdownPath = null)
        {
            return new M139LongRunSoakOptions
            {
                jsonReportPath = string.IsNullOrWhiteSpace(jsonPath) ? M139LongRunSoakReportGenerator.DefaultJsonReportPath : jsonPath,
                markdownReportPath = string.IsNullOrWhiteSpace(markdownPath) ? M139LongRunSoakReportGenerator.DefaultMarkdownReportPath : markdownPath,
                branches = 1,
                traversalsPerBranch = 4,
                targetFrameRate = 60,
                writeReports = true,
                enforceTiming = false,
                ciSmoke = true
            };
        }
    }

    [Serializable]
    public sealed class M139PoolSnapshotSummary
    {
        public int poolKeyCount;
        public int pooledInactiveCount;
        public int rentedOrActiveCount;
        public int activeLeakCount;
        public int rents;
        public int returns;
        public int misses;
        public int hardInstantiates;
        public int warmRequests;
        public int warmCompletions;
        public string[] recentMissKeys = Array.Empty<string>();

        public static M139PoolSnapshotSummary FromEnemy(EnemyRuntimePoolSnapshot snapshot)
        {
            snapshot ??= new EnemyRuntimePoolSnapshot();
            return new M139PoolSnapshotSummary
            {
                poolKeyCount = snapshot.poolKeyCount,
                pooledInactiveCount = snapshot.pooledInactiveCount,
                rentedOrActiveCount = snapshot.rentedCount,
                activeLeakCount = snapshot.activeLeakCount,
                rents = snapshot.rents,
                returns = snapshot.returns,
                misses = snapshot.misses,
                hardInstantiates = snapshot.hardInstantiates,
                warmRequests = snapshot.warmRequests,
                warmCompletions = snapshot.warmCompletions,
                recentMissKeys = snapshot.recentMissKeys ?? Array.Empty<string>()
            };
        }

        public static M139PoolSnapshotSummary FromRuntime(HollowRuntimePoolSnapshot snapshot)
        {
            snapshot ??= new HollowRuntimePoolSnapshot();
            return new M139PoolSnapshotSummary
            {
                poolKeyCount = snapshot.prefabPoolCount + snapshot.generatedPoolCount,
                pooledInactiveCount = snapshot.pooledInactiveCount,
                rentedOrActiveCount = snapshot.activeTrackedCount,
                activeLeakCount = snapshot.activeLeakCount,
                rents = snapshot.rents,
                returns = snapshot.returns,
                misses = snapshot.misses,
                hardInstantiates = snapshot.hardInstantiates,
                warmRequests = snapshot.warmRequests,
                warmCompletions = snapshot.warmCompletions,
                recentMissKeys = snapshot.recentMissKeys ?? Array.Empty<string>()
            };
        }
    }

    [Serializable]
    public sealed class M139BranchRuntimeCacheSnapshotSummary
    {
        public int contentEntries;
        public int graphEntries;
        public int roomDistanceMapEntries;
        public int featurePlanEntries;
        public int rewardPlanEntries;
        public int encounterPlanEntries;
        public int roomAssetEntries;
        public int totalPlanEntries;

        public static M139BranchRuntimeCacheSnapshotSummary FromSnapshot(BranchRuntimeCacheSnapshot snapshot)
        {
            return new M139BranchRuntimeCacheSnapshotSummary
            {
                contentEntries = snapshot.ContentEntries,
                graphEntries = snapshot.GraphEntries,
                roomDistanceMapEntries = snapshot.RoomDistanceMapEntries,
                featurePlanEntries = snapshot.FeaturePlanEntries,
                rewardPlanEntries = snapshot.RewardPlanEntries,
                encounterPlanEntries = snapshot.EncounterPlanEntries,
                roomAssetEntries = snapshot.RoomAssetEntries,
                totalPlanEntries = snapshot.TotalPlanEntries
            };
        }
    }

    [Serializable]
    public sealed class M139AfterWarmupGateCounters
    {
        public int branchRuntimeCacheHits;
        public int branchRuntimeCacheMisses;
        public int roomDescriptorBuilds;
        public int presentationMaterialCacheHits;
        public int presentationMaterialCacheMisses;
        public int presentationPrefabCacheHits;
        public int presentationPrefabCacheMisses;
        public int presentationBiomeCacheHits;
        public int presentationBiomeCacheMisses;
        public int traversalColdCacheMisses;
        public int runtimeNavMeshFallbacks;
        public int enemyPoolMisses;
        public int enemyPoolHardInstantiates;
        public int runtimePoolMisses;
        public int runtimePoolHardInstantiates;

        public void ObserveTraversalWindow(M136PerformanceOperationSnapshot before, M136PerformanceOperationSnapshot after)
        {
            branchRuntimeCacheHits += D(after.BranchRuntimeCacheHits, before.BranchRuntimeCacheHits);
            branchRuntimeCacheMisses += D(after.BranchRuntimeCacheMisses, before.BranchRuntimeCacheMisses);
            roomDescriptorBuilds += D(after.RoomDescriptorBuilds, before.RoomDescriptorBuilds);
            presentationMaterialCacheHits += D(after.PresentationMaterialCacheHits, before.PresentationMaterialCacheHits);
            presentationMaterialCacheMisses += D(after.PresentationMaterialCacheMisses, before.PresentationMaterialCacheMisses);
            presentationPrefabCacheHits += D(after.PresentationPrefabCacheHits, before.PresentationPrefabCacheHits);
            presentationPrefabCacheMisses += D(after.PresentationPrefabCacheMisses, before.PresentationPrefabCacheMisses);
            presentationBiomeCacheHits += D(after.PresentationBiomeCacheHits, before.PresentationBiomeCacheHits);
            presentationBiomeCacheMisses += D(after.PresentationBiomeCacheMisses, before.PresentationBiomeCacheMisses);
            traversalColdCacheMisses += D(after.TraversalColdCacheMisses, before.TraversalColdCacheMisses);
            runtimeNavMeshFallbacks += D(after.RuntimeNavMeshFallbacks, before.RuntimeNavMeshFallbacks);
            enemyPoolMisses += D(after.EnemyPoolMisses, before.EnemyPoolMisses);
            enemyPoolHardInstantiates += D(after.EnemyPoolHardInstantiates, before.EnemyPoolHardInstantiates);
            runtimePoolMisses += D(after.RuntimePoolMisses, before.RuntimePoolMisses);
            runtimePoolHardInstantiates += D(after.RuntimePoolHardInstantiates, before.RuntimePoolHardInstantiates);
        }

        public float CacheHitRate()
        {
            var hits = branchRuntimeCacheHits + presentationMaterialCacheHits + presentationPrefabCacheHits + presentationBiomeCacheHits;
            var misses = branchRuntimeCacheMisses + presentationMaterialCacheMisses + presentationPrefabCacheMisses + presentationBiomeCacheMisses + roomDescriptorBuilds;
            var total = hits + misses;
            return total <= 0 ? 1f : hits / (float)total;
        }

        public int ShaderMaterialFirstUseMisses =>
            presentationMaterialCacheMisses + presentationPrefabCacheMisses + presentationBiomeCacheMisses;

        private static int D(int current, int baseline)
        {
            return Math.Max(0, current - baseline);
        }
    }

    [Serializable]
    public sealed class M139LongRunSoakScenarioSummary
    {
        public string scenarioId;
        public string displayName;
        public int branchLoadsCompleted;
        public int roomTraversalsCompleted;
        public int saveLoadRestoresCompleted;
        public int branchAbandonReentriesCompleted;
        public int bossLoadsCompleted;
        public int nextBranchTransitionsCompleted;
        public int normalTraversalColdCacheMissesAfterLoad;
        public int runtimeNavMeshFallbacks;
        public int enemyPoolMissesAfterWarmup;
        public int enemyPoolHardInstantiatesAfterWarmup;
        public int runtimePoolMissesAfterWarmup;
        public int runtimePoolHardInstantiatesAfterWarmup;
        public int staleEnemyStateFailures;
        public int staleRuntimePoolStateFailures;
        public int poolActiveLeaks;
        public int shaderMaterialFirstUseMissesAfterLoad;
        public float branchRuntimeCacheHitRate;
        public double frameP95Ms;
        public double frameMaxMs;
        public double recurringGcP95Bytes;
        public double managedMemoryDriftMb;
        public double graphicsMemoryDriftMb;
        public M139PoolSnapshotSummary enemyPool;
        public M139PoolSnapshotSummary runtimePool;
        public M139BranchRuntimeCacheSnapshotSummary branchRuntimeCache;
        public bool passed;
        public string[] failures;
        public string note;
    }

    [Serializable]
    public sealed class M139LongRunSoakReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public bool ciSmoke;
        public bool passed;
        public int scenarioCount;
        public string[] failures;
        public M139LongRunSoakScenarioSummary[] scenarios;
    }

    public static class M139LongRunSoakReportGenerator
    {
        public const string LockId = "M139";
        public const string DefaultJsonReportPath = "output/reports/m139_long_run_soak.json";
        public const string DefaultMarkdownReportPath = "output/reports/m139_long_run_soak.md";
        public const double ManagedMemoryDriftBudgetMb = 32d;
        public const double GraphicsMemoryDriftBudgetMb = 64d;
        public const double RecurringGcP95BudgetBytes = 1024d;
        public const float BranchCacheHitRateBudget = 0.85f;

        public static readonly string[] ScenarioIds =
        {
            "fresh_multi_branch_soak",
            "save_load_restore_soak",
            "branch_abandon_reenter_soak",
            "boss_room_soak",
            "next_branch_soak"
        };

        public static M139LongRunSoakScenarioSummary BuildScenarioSummary(
            string scenarioId,
            string displayName,
            M136PerformanceOperationSnapshot warmupBaseline,
            M136PerformanceOperationSnapshot finalSnapshot,
            M139SoakMetricSummary metrics,
            EnemyRuntimePoolSnapshot enemyPool,
            HollowRuntimePoolSnapshot runtimePool,
            BranchRuntimeCacheSnapshot branchCache,
            int roomTraversalsCompleted,
            int saveLoadRestoresCompleted,
            int branchAbandonReentriesCompleted,
            int nextBranchTransitionsCompleted,
            bool requireBossLoad,
            bool enforceTiming,
            bool ciSmoke,
            M139AfterWarmupGateCounters gateCounters = null,
            string note = "")
        {
            var afterWarmup = OperationDelta.Between(warmupBaseline, finalSnapshot);
            var shaderMissesAfterLoad = gateCounters?.ShaderMaterialFirstUseMisses ??
                (afterWarmup.PresentationMaterialCacheMisses +
                 afterWarmup.PresentationPrefabCacheMisses +
                 afterWarmup.PresentationBiomeCacheMisses);
            if (shaderMissesAfterLoad > 0)
            {
                M136PerformanceOperationCounters.ReportM139ShaderMaterialFirstUseMissAfterLoad();
            }

            var cacheHitRate = gateCounters?.CacheHitRate() ?? CacheHitRate(afterWarmup);
            M136PerformanceOperationCounters.ReportM139CacheHitRateWindow(cacheHitRate < BranchCacheHitRateBudget);

            var managedDrift = metrics?.ManagedMemoryDriftMb ?? 0d;
            var graphicsDrift = metrics?.GraphicsMemoryDriftMb ?? 0d;
            M136PerformanceOperationCounters.ReportM139MemoryDrift((float)managedDrift, (float)graphicsDrift);

            var finalWithM139Counters = M136PerformanceOperationCounters.Snapshot();
            var failures = new List<string>();
            var traversalColdCacheMisses = gateCounters?.traversalColdCacheMisses ?? afterWarmup.TraversalColdCacheMisses;
            var runtimeNavMeshFallbacks = gateCounters?.runtimeNavMeshFallbacks ?? finalSnapshot.RuntimeNavMeshFallbacks;
            var enemyPoolMisses = gateCounters?.enemyPoolMisses ?? afterWarmup.EnemyPoolMisses;
            var enemyPoolHardInstantiates = gateCounters?.enemyPoolHardInstantiates ?? afterWarmup.EnemyPoolHardInstantiates;
            var runtimePoolMisses = gateCounters?.runtimePoolMisses ?? afterWarmup.RuntimePoolMisses;
            var runtimePoolHardInstantiates = gateCounters?.runtimePoolHardInstantiates ?? afterWarmup.RuntimePoolHardInstantiates;

            if (traversalColdCacheMisses != 0)
            {
                failures.Add($"Normal traversal cold-cache misses after branch load: {traversalColdCacheMisses}; expected 0.");
            }

            if (runtimeNavMeshFallbacks != 0)
            {
                failures.Add($"Runtime NavMesh fallback count: {runtimeNavMeshFallbacks}; expected 0.");
            }

            if (enemyPoolMisses != 0 || enemyPoolHardInstantiates != 0)
            {
                failures.Add($"Enemy pool missed after warmup: misses={enemyPoolMisses}, hard={enemyPoolHardInstantiates}; expected 0 for normal enemies.");
            }

            if (runtimePoolMisses != 0 || runtimePoolHardInstantiates != 0)
            {
                failures.Add($"Runtime pool missed after warmup: misses={runtimePoolMisses}, hard={runtimePoolHardInstantiates}; expected 0 for warmed runtime objects.");
            }

            if (finalWithM139Counters.M139StaleEnemyStateFailures != 0 || finalWithM139Counters.M139StaleRuntimePoolStateFailures != 0)
            {
                failures.Add($"Stale pooled state failures: enemies={finalWithM139Counters.M139StaleEnemyStateFailures}, runtime={finalWithM139Counters.M139StaleRuntimePoolStateFailures}; expected 0.");
            }

            var activeLeaks = (enemyPool?.activeLeakCount ?? 0) + (runtimePool?.activeLeakCount ?? 0) + finalWithM139Counters.M139PoolActiveLeaks;
            if (activeLeaks != 0)
            {
                failures.Add($"Pool active leaks: {activeLeaks}; expected 0 after cleanup windows.");
            }

            if (shaderMissesAfterLoad != 0)
            {
                failures.Add($"Shader/material/presentation first-use misses after branch load: {shaderMissesAfterLoad}; expected 0.");
            }

            if (cacheHitRate < BranchCacheHitRateBudget)
            {
                failures.Add($"Branch/runtime cache hit rate {cacheHitRate:P1} is below {BranchCacheHitRateBudget:P0}.");
            }

            if (managedDrift > ManagedMemoryDriftBudgetMb)
            {
                failures.Add($"Managed memory drift {managedDrift:0.0} MB exceeds {ManagedMemoryDriftBudgetMb:0} MB.");
            }

            if (graphicsDrift > GraphicsMemoryDriftBudgetMb)
            {
                failures.Add($"Graphics memory drift {graphicsDrift:0.0} MB exceeds {GraphicsMemoryDriftBudgetMb:0} MB.");
            }

            if (metrics != null && metrics.RecurringGcP95Bytes > RecurringGcP95BudgetBytes)
            {
                failures.Add($"Recurring GC p95 {metrics.RecurringGcP95Bytes:0} bytes exceeds {RecurringGcP95BudgetBytes:0} bytes.");
            }

            if (requireBossLoad && finalSnapshot.BossLoadingCompletions <= 0)
            {
                failures.Add("Boss room soak did not complete a boss loading screen.");
            }

            if (roomTraversalsCompleted <= 0)
            {
                failures.Add("No real BranchSessionController room traversals completed.");
            }

            if (enforceTiming && metrics != null && metrics.FrameP95Ms > M138CombatScaleStressReportGenerator.WindowsFrameTimeBudgetMs)
            {
                failures.Add($"Frame p95 {metrics.FrameP95Ms:0.00} ms exceeds {M138CombatScaleStressReportGenerator.WindowsFrameTimeBudgetMs:0.0} ms.");
            }

            return new M139LongRunSoakScenarioSummary
            {
                scenarioId = scenarioId ?? string.Empty,
                displayName = string.IsNullOrWhiteSpace(displayName) ? scenarioId : displayName,
                branchLoadsCompleted = finalSnapshot.BranchLoadingCompletions,
                roomTraversalsCompleted = roomTraversalsCompleted,
                saveLoadRestoresCompleted = saveLoadRestoresCompleted,
                branchAbandonReentriesCompleted = branchAbandonReentriesCompleted,
                bossLoadsCompleted = finalSnapshot.BossLoadingCompletions,
                nextBranchTransitionsCompleted = nextBranchTransitionsCompleted,
                normalTraversalColdCacheMissesAfterLoad = traversalColdCacheMisses,
                runtimeNavMeshFallbacks = runtimeNavMeshFallbacks,
                enemyPoolMissesAfterWarmup = enemyPoolMisses,
                enemyPoolHardInstantiatesAfterWarmup = enemyPoolHardInstantiates,
                runtimePoolMissesAfterWarmup = runtimePoolMisses,
                runtimePoolHardInstantiatesAfterWarmup = runtimePoolHardInstantiates,
                staleEnemyStateFailures = finalWithM139Counters.M139StaleEnemyStateFailures,
                staleRuntimePoolStateFailures = finalWithM139Counters.M139StaleRuntimePoolStateFailures,
                poolActiveLeaks = activeLeaks,
                shaderMaterialFirstUseMissesAfterLoad = shaderMissesAfterLoad,
                branchRuntimeCacheHitRate = cacheHitRate,
                frameP95Ms = metrics?.FrameP95Ms ?? 0d,
                frameMaxMs = metrics?.FrameMaxMs ?? 0d,
                recurringGcP95Bytes = metrics?.RecurringGcP95Bytes ?? 0d,
                managedMemoryDriftMb = managedDrift,
                graphicsMemoryDriftMb = graphicsDrift,
                enemyPool = M139PoolSnapshotSummary.FromEnemy(enemyPool),
                runtimePool = M139PoolSnapshotSummary.FromRuntime(runtimePool),
                branchRuntimeCache = M139BranchRuntimeCacheSnapshotSummary.FromSnapshot(branchCache),
                passed = failures.Count == 0,
                failures = failures.ToArray(),
                note = ciSmoke ? "CI smoke uses deterministic gates and treats frame timing as directional." : note ?? string.Empty
            };
        }

        public static M139LongRunSoakReport BuildReport(IEnumerable<M139LongRunSoakScenarioSummary> scenarioSummaries, bool ciSmoke)
        {
            var summaries = scenarioSummaries?.Where(summary => summary != null).ToArray() ?? Array.Empty<M139LongRunSoakScenarioSummary>();
            var failures = new List<string>();
            foreach (var id in ScenarioIds)
            {
                if (!summaries.Any(summary => string.Equals(summary.scenarioId, id, StringComparison.Ordinal)))
                {
                    failures.Add($"Missing M139 soak scenario `{id}`.");
                }
            }

            foreach (var summary in summaries)
            {
                if (!summary.passed)
                {
                    failures.Add($"{summary.scenarioId}: {string.Join("; ", summary.failures ?? Array.Empty<string>())}");
                }
            }

            return new M139LongRunSoakReport
            {
                lockId = LockId,
                title = "M139 Long-Run Cache, Pool, And Memory Soak",
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                ciSmoke = ciSmoke,
                passed = failures.Count == 0 && summaries.Length == ScenarioIds.Length,
                scenarioCount = summaries.Length,
                failures = failures.ToArray(),
                scenarios = summaries
            };
        }

        public static void WriteReport(M139LongRunSoakReport report, string jsonPath = null, string markdownPath = null)
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

        public static string ToMarkdown(M139LongRunSoakReport report)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("# M139 Long-Run Cache, Pool, And Memory Soak");
            builder.AppendLine();
            builder.AppendLine($"- Lock: `{report?.lockId ?? LockId}`");
            builder.AppendLine($"- Status: {(report != null && report.passed ? "PASS" : "FAIL")}");
            builder.AppendLine($"- Mode: {(report != null && report.ciSmoke ? "CI smoke" : "Full gate")}");
            builder.AppendLine($"- Scenarios: {report?.scenarioCount ?? 0}/{ScenarioIds.Length}");
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
            foreach (var scenario in report?.scenarios ?? Array.Empty<M139LongRunSoakScenarioSummary>())
            {
                builder.AppendLine($"### {scenario.displayName}");
                builder.AppendLine($"- Status: {(scenario.passed ? "PASS" : "FAIL")}");
                builder.AppendLine($"- Branch loads/traversals: {scenario.branchLoadsCompleted}/{scenario.roomTraversalsCompleted}");
                builder.AppendLine($"- Special actions: save-load {scenario.saveLoadRestoresCompleted}, abandon/re-enter {scenario.branchAbandonReentriesCompleted}, boss loads {scenario.bossLoadsCompleted}, next branch {scenario.nextBranchTransitionsCompleted}");
                builder.AppendLine($"- Cache: cold misses {scenario.normalTraversalColdCacheMissesAfterLoad}, hit rate {scenario.branchRuntimeCacheHitRate:P1}, shader/material misses {scenario.shaderMaterialFirstUseMissesAfterLoad}");
                builder.AppendLine($"- Nav/pools: nav fallback {scenario.runtimeNavMeshFallbacks}, enemy misses/hard {scenario.enemyPoolMissesAfterWarmup}/{scenario.enemyPoolHardInstantiatesAfterWarmup}, runtime misses/hard {scenario.runtimePoolMissesAfterWarmup}/{scenario.runtimePoolHardInstantiatesAfterWarmup}, leaks {scenario.poolActiveLeaks}");
                if (scenario.enemyPool?.recentMissKeys != null && scenario.enemyPool.recentMissKeys.Length > 0)
                {
                    builder.AppendLine($"- Enemy pool miss keys: {string.Join(", ", scenario.enemyPool.recentMissKeys)}");
                }

                if (scenario.runtimePool?.recentMissKeys != null && scenario.runtimePool.recentMissKeys.Length > 0)
                {
                    builder.AppendLine($"- Runtime pool miss keys: {string.Join(", ", scenario.runtimePool.recentMissKeys)}");
                }

                builder.AppendLine($"- Memory/GC: managed drift {scenario.managedMemoryDriftMb:0.0} MB, graphics drift {scenario.graphicsMemoryDriftMb:0.0} MB, GC p95 {scenario.recurringGcP95Bytes:0} bytes");
                builder.AppendLine($"- Frame p95/max: {scenario.frameP95Ms:0.00} ms / {scenario.frameMaxMs:0.00} ms");
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

        private static float CacheHitRate(OperationDelta snapshot)
        {
            var hits = snapshot.BranchRuntimeCacheHits +
                snapshot.PresentationMaterialCacheHits +
                snapshot.PresentationPrefabCacheHits +
                snapshot.PresentationBiomeCacheHits;
            var misses = snapshot.BranchRuntimeCacheMisses +
                snapshot.PresentationMaterialCacheMisses +
                snapshot.PresentationPrefabCacheMisses +
                snapshot.PresentationBiomeCacheMisses +
                snapshot.RoomDescriptorBuilds;
            var total = hits + misses;
            return total <= 0 ? 1f : hits / (float)total;
        }

        private sealed class OperationDelta
        {
            public int RuntimePoolMisses;
            public int RuntimePoolHardInstantiates;
            public int RuntimeNavMeshFallbacks;
            public int RoomTransitionEvents;
            public int BranchRuntimeCacheHits;
            public int BranchRuntimeCacheMisses;
            public int RoomDescriptorBuilds;
            public int PresentationMaterialCacheHits;
            public int PresentationMaterialCacheMisses;
            public int PresentationPrefabCacheHits;
            public int PresentationPrefabCacheMisses;
            public int PresentationBiomeCacheHits;
            public int PresentationBiomeCacheMisses;
            public int TraversalColdCacheMisses;
            public int EnemyPoolMisses;
            public int EnemyPoolHardInstantiates;

            public static OperationDelta Between(M136PerformanceOperationSnapshot baseline, M136PerformanceOperationSnapshot current)
            {
                return new OperationDelta
                {
                    RuntimePoolMisses = D(current.RuntimePoolMisses, baseline.RuntimePoolMisses),
                    RuntimePoolHardInstantiates = D(current.RuntimePoolHardInstantiates, baseline.RuntimePoolHardInstantiates),
                    RuntimeNavMeshFallbacks = D(current.RuntimeNavMeshFallbacks, baseline.RuntimeNavMeshFallbacks),
                    RoomTransitionEvents = D(current.RoomTransitionEvents, baseline.RoomTransitionEvents),
                    BranchRuntimeCacheHits = D(current.BranchRuntimeCacheHits, baseline.BranchRuntimeCacheHits),
                    BranchRuntimeCacheMisses = D(current.BranchRuntimeCacheMisses, baseline.BranchRuntimeCacheMisses),
                    RoomDescriptorBuilds = D(current.RoomDescriptorBuilds, baseline.RoomDescriptorBuilds),
                    PresentationMaterialCacheHits = D(current.PresentationMaterialCacheHits, baseline.PresentationMaterialCacheHits),
                    PresentationMaterialCacheMisses = D(current.PresentationMaterialCacheMisses, baseline.PresentationMaterialCacheMisses),
                    PresentationPrefabCacheHits = D(current.PresentationPrefabCacheHits, baseline.PresentationPrefabCacheHits),
                    PresentationPrefabCacheMisses = D(current.PresentationPrefabCacheMisses, baseline.PresentationPrefabCacheMisses),
                    PresentationBiomeCacheHits = D(current.PresentationBiomeCacheHits, baseline.PresentationBiomeCacheHits),
                    PresentationBiomeCacheMisses = D(current.PresentationBiomeCacheMisses, baseline.PresentationBiomeCacheMisses),
                    TraversalColdCacheMisses = D(current.TraversalColdCacheMisses, baseline.TraversalColdCacheMisses),
                    EnemyPoolMisses = D(current.EnemyPoolMisses, baseline.EnemyPoolMisses),
                    EnemyPoolHardInstantiates = D(current.EnemyPoolHardInstantiates, baseline.EnemyPoolHardInstantiates)
                };
            }

            private static int D(int current, int baseline)
            {
                return Math.Max(0, current - baseline);
            }
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

    [Serializable]
    public sealed class M139SoakMetricSummary
    {
        public double FrameP95Ms;
        public double FrameMaxMs;
        public double RecurringGcP95Bytes;
        public double ManagedMemoryDriftMb;
        public double GraphicsMemoryDriftMb;
    }
}
