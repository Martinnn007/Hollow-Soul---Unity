using System.IO;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Performance;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone139LongRunSoakTests
    {
        [SetUp]
        public void SetUp()
        {
            M136PerformanceOperationCounters.Reset();
        }

        [Test]
        public void ReportGeneratorPassesBoundedSyntheticSoakData()
        {
            var warmup = M136PerformanceOperationCounters.Snapshot();
            var final = M136PerformanceOperationCounters.Snapshot();
            var gates = new M139AfterWarmupGateCounters { branchRuntimeCacheHits = 32 };

            var summary = BuildSummary(warmup, final, gates);

            Assert.IsTrue(summary.passed, string.Join("\n", summary.failures ?? new string[0]));
            Assert.AreEqual(1f, summary.branchRuntimeCacheHitRate, 0.001f);
        }

        [Test]
        public void ReportGeneratorFailsDeterministicSoakViolations()
        {
            var warmup = M136PerformanceOperationCounters.Snapshot();
            M136PerformanceOperationCounters.ReportM139StaleEnemyStateFailure();
            var final = M136PerformanceOperationCounters.Snapshot();
            var gates = new M139AfterWarmupGateCounters
            {
                traversalColdCacheMisses = 1,
                runtimeNavMeshFallbacks = 1,
                enemyPoolHardInstantiates = 1,
                runtimePoolMisses = 1,
                presentationMaterialCacheMisses = 1,
                branchRuntimeCacheHits = 1,
                branchRuntimeCacheMisses = 10
            };
            var enemyPool = new EnemyRuntimePoolSnapshot { activeLeakCount = 1 };
            var metrics = new M139SoakMetricSummary
            {
                ManagedMemoryDriftMb = M139LongRunSoakReportGenerator.ManagedMemoryDriftBudgetMb + 1d,
                GraphicsMemoryDriftMb = M139LongRunSoakReportGenerator.GraphicsMemoryDriftBudgetMb + 1d,
                RecurringGcP95Bytes = M139LongRunSoakReportGenerator.RecurringGcP95BudgetBytes + 1d
            };

            var summary = BuildSummary(warmup, final, gates, metrics, enemyPool);

            Assert.IsFalse(summary.passed);
            var failures = string.Join("\n", summary.failures);
            StringAssert.Contains("cold-cache", failures);
            StringAssert.Contains("Runtime NavMesh", failures);
            StringAssert.Contains("Enemy pool", failures);
            StringAssert.Contains("Runtime pool", failures);
            StringAssert.Contains("Stale pooled state", failures);
            StringAssert.Contains("Pool active leaks", failures);
            StringAssert.Contains("hit rate", failures);
            StringAssert.Contains("Managed memory drift", failures);
            StringAssert.Contains("Graphics memory drift", failures);
            StringAssert.Contains("Recurring GC", failures);
        }

        [Test]
        public void EnemyPooledResetContractClearsVisibleRuntimeState()
        {
            var root = new GameObject("M139PooledEnemyContract");
            try
            {
                var enemy = root.AddComponent<EnemyRuntimeController>();
                enemy.MarkBranchPooled("branch:m139");
                enemy.enabled = true;
                root.SetActive(true);

                var visual = new GameObject("RuntimePresentationVisual");
                visual.transform.SetParent(root.transform, false);
                visual.AddComponent<PresentationVisualMarker>();

                enemy.PrepareForBranchPoolReturn();

                Assert.IsTrue(enemy.ValidateBranchPoolReturnState(out var issue), issue);
                Assert.IsFalse(root.activeSelf);
                Assert.IsFalse(enemy.enabled);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimePoolSnapshotsExposeStableCounts()
        {
            var enemySnapshot = EnemyRuntimePool.Snapshot("branch:m139");
            var runtimeSnapshot = HollowRuntimePool.Snapshot();

            Assert.GreaterOrEqual(enemySnapshot.poolKeyCount, 0);
            Assert.GreaterOrEqual(runtimeSnapshot.prefabPoolCount + runtimeSnapshot.generatedPoolCount, 0);
            Assert.GreaterOrEqual(runtimeSnapshot.pooledInactiveCount, 0);
        }

        [Test]
        public void WholeGameAuditCanDetectM139ImplementationSurface()
        {
            var runner = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Performance/M139LongRunSoakRunner.cs");
            var report = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Performance/M139LongRunSoakReport.cs");

            StringAssert.Contains("BranchSessionController", runner);
            StringAssert.Contains("StartNextBranch", runner);
            StringAssert.Contains("normalTraversalColdCacheMissesAfterLoad", report);
            StringAssert.Contains("managedMemoryDriftMb", report);
        }

        private static M139LongRunSoakScenarioSummary BuildSummary(
            M136PerformanceOperationSnapshot warmup,
            M136PerformanceOperationSnapshot final,
            M139AfterWarmupGateCounters gates,
            M139SoakMetricSummary metrics = null,
            EnemyRuntimePoolSnapshot enemyPool = null)
        {
            return M139LongRunSoakReportGenerator.BuildScenarioSummary(
                "fresh_multi_branch_soak",
                "Fresh Multi-Branch Soak",
                warmup,
                final,
                metrics ?? new M139SoakMetricSummary(),
                enemyPool ?? new EnemyRuntimePoolSnapshot(),
                new HollowRuntimePoolSnapshot(),
                new BranchRuntimeCacheSnapshot(1, 1, 1, 1, 1, 1, 1, HollowRuntimeCachePolicy.Default),
                roomTraversalsCompleted: 4,
                saveLoadRestoresCompleted: 0,
                branchAbandonReentriesCompleted: 0,
                nextBranchTransitionsCompleted: 0,
                requireBossLoad: false,
                enforceTiming: false,
                ciSmoke: true,
                gateCounters: gates);
        }
    }
}
