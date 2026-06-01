using System.Reflection;
using Hollow.Combat;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using Hollow.Performance;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone138CombatAiNavigationScaleTests
    {
        [Test]
        public void AiThinkPolicyMatchesMilestoneThreeCadenceBands()
        {
            var tacticalFull = EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics(
                EnemyIntelligenceLevel.Tactical,
                EnemyAiLodTier.Full,
                activeEnemyCount: 10,
                pendingPathCount: 0,
                protectResponsiveness: false);
            var simpleReduced = EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics(
                EnemyIntelligenceLevel.Simple,
                EnemyAiLodTier.Reduced,
                activeEnemyCount: 10,
                pendingPathCount: 0,
                protectResponsiveness: false);
            var background = EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics(
                EnemyIntelligenceLevel.Simple,
                EnemyAiLodTier.Background,
                activeEnemyCount: 10,
                pendingPathCount: 0,
                protectResponsiveness: false);

            Assert.That(tacticalFull, Is.InRange(
                M137PerformanceComfortPolicy.M3FullThreatMinThinkIntervalSeconds,
                M137PerformanceComfortPolicy.M3FullThreatMaxThinkIntervalSeconds));
            Assert.That(simpleReduced, Is.InRange(
                M137PerformanceComfortPolicy.M3ReducedThreatMinThinkIntervalSeconds,
                M137PerformanceComfortPolicy.M3ReducedThreatMaxThinkIntervalSeconds));
            Assert.That(background, Is.InRange(
                M137PerformanceComfortPolicy.M3BackgroundMinThinkIntervalSeconds,
                M137PerformanceComfortPolicy.M3BackgroundMaxThinkIntervalSeconds));
        }

        [Test]
        public void AiLodResolvesThreatsSupportAndBackgroundEnemies()
        {
            var root = new GameObject("M138EnemyLodHarness");
            BossDefinition bossDefinition = null;
            try
            {
                var enemy = root.AddComponent<EnemyRuntimeController>();
                enemy.Configure(null, null, EnemyDefinition.CreateRuntimeNormal(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());

                SetPrivate(enemy, "lastTacticalIntent", TacticalIntent(EnemyTacticalRole.None));
                SetPrivate(enemy, "readabilityState", EnemyReadabilityState.Idle);
                SetPrivate(enemy, "awarenessState", EnemyAwarenessState.Engaged);
                Assert.AreEqual(EnemyAiLodTier.Full, EnemyAiBrain.ResolveLodTier(enemy, 3f));

                SetPrivate(enemy, "lastTacticalIntent", TacticalIntent(EnemyTacticalRole.ActiveThreat));
                Assert.AreEqual(EnemyAiLodTier.Full, EnemyAiBrain.ResolveLodTier(enemy, 14f));

                SetPrivate(enemy, "lastTacticalIntent", TacticalIntent(EnemyTacticalRole.SupportPressure));
                Assert.AreEqual(EnemyAiLodTier.Reduced, EnemyAiBrain.ResolveLodTier(enemy, 14f));

                SetPrivate(enemy, "lastTacticalIntent", TacticalIntent(EnemyTacticalRole.Waiting));
                SetPrivate(enemy, "awarenessState", EnemyAwarenessState.Unaware);
                Assert.AreEqual(EnemyAiLodTier.Background, EnemyAiBrain.ResolveLodTier(enemy, 14f));

                SetPrivate(enemy, "readabilityState", EnemyReadabilityState.MeleeWindup);
                Assert.AreEqual(EnemyAiLodTier.Full, EnemyAiBrain.ResolveLodTier(enemy, 14f));

                bossDefinition = ScriptableObject.CreateInstance<BossDefinition>();
                SetPrivate(enemy, "bossDefinition", bossDefinition);
                SetPrivate(enemy, "readabilityState", EnemyReadabilityState.Idle);
                Assert.AreEqual(EnemyAiLodTier.Full, EnemyAiBrain.ResolveLodTier(enemy, 30f));
            }
            finally
            {
                if (bossDefinition != null)
                {
                    Object.DestroyImmediate(bossDefinition);
                }

                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NavigationBudgetUsesLodRoleIntervalsAndStaggeredOffsets()
        {
            EnemyNavigationBudget.ResetForTests();
            var fullThreat = NavigationRequest(EnemyAiLodTier.Full, EnemyTacticalRole.ActiveThreat, EnemyIntelligenceLevel.Tactical, 1);
            var reducedSupport = NavigationRequest(EnemyAiLodTier.Reduced, EnemyTacticalRole.SupportPressure, EnemyIntelligenceLevel.Simple, 2);
            var background = NavigationRequest(EnemyAiLodTier.Background, EnemyTacticalRole.Waiting, EnemyIntelligenceLevel.Simple, 3);

            Assert.Less(EnemyNavigationBudget.RepathIntervalFor(fullThreat), EnemyNavigationBudget.RepathIntervalFor(reducedSupport));
            Assert.Less(EnemyNavigationBudget.RepathIntervalFor(reducedSupport), EnemyNavigationBudget.RepathIntervalFor(background));
            Assert.IsTrue(EnemyNavigationBudget.IsHighPriority(fullThreat));
            Assert.IsFalse(EnemyNavigationBudget.IsHighPriority(reducedSupport));
            Assert.AreNotEqual(
                EnemyNavigationBudget.InitialRepathOffsetSeconds(1),
                EnemyNavigationBudget.InitialRepathOffsetSeconds(2));

            M136PerformanceOperationCounters.Reset();
            for (var index = 0; index < M137PerformanceComfortPolicy.M3NavMeshPathSolveBudgetPerFrame; index++)
            {
                Assert.IsTrue(EnemyNavigationBudget.TryAcquirePathSolve(fullThreat, force: false));
            }

            Assert.IsFalse(EnemyNavigationBudget.TryAcquirePathSolve(fullThreat, force: true));
            Assert.AreEqual(1, M136PerformanceOperationCounters.Snapshot().NavPathDeferred);
        }

        [Test]
        public void BossRoomAddsLoseHighPriorityNavigationAndUseOneSolveBudget()
        {
            EnemyNavigationBudget.ResetForTests();
            M136PerformanceOperationCounters.Reset();
            var bossRoomAdd = new EnemyNavigationRequest(
                null,
                Vector3.zero,
                Vector3.forward,
                0.4f,
                EnemyNavigationMode.GroundedLocal,
                EnemyNavigationIntent.MoveToPlayer,
                EnemyIntelligenceLevel.Tactical,
                allowPathfinding: true,
                pathSeed: 7,
                aiLodTier: EnemyAiLodTier.Full,
                tacticalRole: EnemyTacticalRole.ActiveThreat,
                roomHasActiveBoss: true,
                isBoss: false);

            Assert.IsFalse(EnemyNavigationBudget.IsHighPriority(bossRoomAdd));
            Assert.GreaterOrEqual(EnemyNavigationBudget.RepathIntervalFor(bossRoomAdd), 0.28f);
            Assert.IsTrue(EnemyNavigationBudget.TryAcquirePathSolve(bossRoomAdd, force: false));
            Assert.IsFalse(EnemyNavigationBudget.TryAcquirePathSolve(bossRoomAdd, force: false));
            Assert.AreEqual(1, M136PerformanceOperationCounters.Snapshot().NavPathDeferred);
        }

        [Test]
        public void BossRoomNonActiveAddsSkipExpensiveActionScorer()
        {
            Assert.IsFalse(EnemyAiBrain.ShouldSkipBossAddScorerForDiagnostics(
                roomHasActiveBoss: true,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.ActiveThreat));
            Assert.IsFalse(EnemyAiBrain.ShouldSkipBossAddScorerForDiagnostics(
                roomHasActiveBoss: true,
                isBoss: true,
                tacticalRole: EnemyTacticalRole.Waiting));
            Assert.IsFalse(EnemyAiBrain.ShouldSkipBossAddScorerForDiagnostics(
                roomHasActiveBoss: false,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.Waiting));

            Assert.IsTrue(EnemyAiBrain.ShouldSkipBossAddScorerForDiagnostics(
                roomHasActiveBoss: true,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.Waiting));
            Assert.IsTrue(EnemyAiBrain.ShouldSkipBossAddScorerForDiagnostics(
                roomHasActiveBoss: true,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.SupportPressure));
            Assert.IsTrue(EnemyAiBrain.ShouldSkipBossAddScorerForDiagnostics(
                roomHasActiveBoss: true,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.None));
        }

        [Test]
        public void CrowdedNonBossRoomsCapActiveThreatReservations()
        {
            Assert.AreEqual(
                3,
                RoomTacticalDirector.ResolveActiveThreatLimit(candidateCount: 8, livingCount: 8, bossPresent: false),
                "Small rooms should keep the existing non-crowd active-threat spread.");
            Assert.Greater(
                RoomTacticalDirector.ResolveActiveThreatLimit(candidateCount: 8, livingCount: 8, bossPresent: false),
                M137PerformanceComfortPolicy.M3CrowdedRoomActiveThreatSlots,
                "The crowded-room active cap should not engage below the crowd threshold.");
            Assert.AreEqual(
                M137PerformanceComfortPolicy.M3CrowdedRoomActiveThreatSlots,
                RoomTacticalDirector.ResolveActiveThreatLimit(candidateCount: 30, livingCount: 30, bossPresent: false));
            Assert.AreEqual(
                1,
                RoomTacticalDirector.ResolveActiveThreatLimit(candidateCount: 12, livingCount: 12, bossPresent: true),
                "Boss rooms keep the stricter boss-add active slot.");
        }

        [Test]
        public void CrowdedNonBossCheapCommandPolicyProtectsActiveCloseAndEndangeredEnemies()
        {
            Assert.IsTrue(EnemyAiBrain.ShouldUseCrowdedRoomCheapCommandForDiagnostics(
                activeEnemyCount: 30,
                roomHasActiveBoss: false,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.Waiting,
                readabilityState: EnemyReadabilityState.Idle,
                isEndangered: false,
                distanceToPlayer: M137PerformanceComfortPolicy.M3CrowdedRoomProtectResponsivenessDistanceMeters + 2f));

            Assert.IsFalse(EnemyAiBrain.ShouldUseCrowdedRoomCheapCommandForDiagnostics(
                activeEnemyCount: 8,
                roomHasActiveBoss: false,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.Waiting,
                readabilityState: EnemyReadabilityState.Idle,
                isEndangered: false,
                distanceToPlayer: 12f));
            Assert.IsFalse(EnemyAiBrain.ShouldUseCrowdedRoomCheapCommandForDiagnostics(
                activeEnemyCount: 30,
                roomHasActiveBoss: false,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.ActiveThreat,
                readabilityState: EnemyReadabilityState.Idle,
                isEndangered: false,
                distanceToPlayer: 12f));
            Assert.IsFalse(EnemyAiBrain.ShouldUseCrowdedRoomCheapCommandForDiagnostics(
                activeEnemyCount: 30,
                roomHasActiveBoss: false,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.Waiting,
                readabilityState: EnemyReadabilityState.Idle,
                isEndangered: true,
                distanceToPlayer: 12f));
            Assert.IsFalse(EnemyAiBrain.ShouldUseCrowdedRoomCheapCommandForDiagnostics(
                activeEnemyCount: 30,
                roomHasActiveBoss: false,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.Waiting,
                readabilityState: EnemyReadabilityState.Idle,
                isEndangered: false,
                distanceToPlayer: M137PerformanceComfortPolicy.M3CrowdedRoomNonActiveCloseProtectionDistanceMeters * 0.5f));
            Assert.IsTrue(EnemyAiBrain.ShouldUseCrowdedRoomCheapCommandForDiagnostics(
                activeEnemyCount: 30,
                roomHasActiveBoss: false,
                isBoss: false,
                tacticalRole: EnemyTacticalRole.Waiting,
                readabilityState: EnemyReadabilityState.Idle,
                isEndangered: false,
                distanceToPlayer: 2f));
        }

        [Test]
        public void StressManifestCoversCrowdedProjectileAndBossScaleTargets()
        {
            Assert.IsTrue(M138CombatScaleStressScenarioPolicy.ValidateStressManifest(out var detail), detail);

            var scenarios = M138CombatScaleStressScenarioPolicy.StressManifest;
            Assert.IsTrue(System.Array.Exists(scenarios, scenario => scenario.id == "enemy_stress_10" && scenario.targetEnemyCount == 10));
            Assert.IsTrue(System.Array.Exists(scenarios, scenario => scenario.id == "enemy_stress_20" && scenario.targetEnemyCount == 20));
            Assert.IsTrue(System.Array.Exists(scenarios, scenario => scenario.id == "enemy_stress_30" && scenario.targetEnemyCount == 30));
            Assert.IsTrue(System.Array.Exists(scenarios, scenario => scenario.id == "projectile_heavy_room" && scenario.projectileHeavy));
            Assert.IsTrue(System.Array.Exists(scenarios, scenario => scenario.id == "boss_plus_adds" && scenario.includesBoss && scenario.includesAdds));
            Assert.IsTrue(System.Array.Exists(scenarios, scenario => scenario.id == "boss_plus_projectile_adds" && scenario.includesBoss && scenario.includesAdds && scenario.projectileHeavy));
        }

        [Test]
        public void StressManifestConvertsIntoM136CaptureScenarios()
        {
            var scenarios = M138CombatScaleStressScenarioPolicy.StressManifest;
            for (var index = 0; index < scenarios.Length; index++)
            {
                var converted = M138CombatScaleStressReportGenerator.ToM136ScenarioDefinition(scenarios[index]);
                Assert.AreEqual(scenarios[index].id, converted.id);
                Assert.AreEqual(scenarios[index].displayName, converted.displayName);
                Assert.AreEqual(M138CombatScaleStressScenarioPolicy.FixedWarmupSeconds, converted.warmupSeconds);
                Assert.AreEqual(M138CombatScaleStressScenarioPolicy.FixedSampleSeconds, converted.sampleSeconds);
            }
        }

        [Test]
        public void M138ReportPassesSyntheticValidStressResults()
        {
            var scenarios = M138CombatScaleStressScenarioPolicy.StressManifest;
            var summaries = new M138CombatScaleStressScenarioSummary[scenarios.Length];
            for (var index = 0; index < scenarios.Length; index++)
            {
                var scenario = scenarios[index];
                var result = SyntheticResult(
                    scenario,
                    peakEnemies: scenario.targetEnemyCount + (scenario.includesBoss ? 1 : 0),
                    peakProjectiles: scenario.projectileHeavy ? M138CombatScaleStressReportGenerator.ProjectileHeavyMinimumPeakProjectiles : 0,
                    observedBoss: scenario.includesBoss,
                    aiThinkReduced: scenario.targetEnemyCount >= 20 || scenario.includesBoss ? 8 : 1,
                    aiCommandReuses: scenario.targetEnemyCount >= 20 ? 18 : 3);
                summaries[index] = M138CombatScaleStressReportGenerator.BuildScenarioSummary(
                    scenario,
                    result,
                    SyntheticFrameBudget(maxAiThinks: 3, maxPathSolves: M137PerformanceComfortPolicy.M3NavMeshPathSolveBudgetPerFrame),
                    bossFullLodObserved: scenario.includesBoss,
                    reducedOrBackgroundAddObserved: scenario.targetEnemyCount >= 20 || scenario.includesBoss,
                    enforceFrameTimingWhenTrusted: true);
                Assert.IsTrue(summaries[index].passed, string.Join("; ", summaries[index].failures));
            }

            var report = M138CombatScaleStressReportGenerator.BuildReport(summaries);
            Assert.IsTrue(report.passed, string.Join("; ", report.failures));
            Assert.AreEqual(M138CombatScaleStressScenarioPolicy.LockId, report.lockId);
            Assert.AreEqual(6, report.scenarioCount);
        }

        [Test]
        public void M138ReportDetectsSynchronizedAiAndNavBursts()
        {
            var scenario = System.Array.Find(M138CombatScaleStressScenarioPolicy.StressManifest, item => item.id == "enemy_stress_30");
            var result = SyntheticResult(scenario, peakEnemies: 30, peakProjectiles: 0, observedBoss: false, aiThinkReduced: 12, aiCommandReuses: 20);
            var summary = M138CombatScaleStressReportGenerator.BuildScenarioSummary(
                scenario,
                result,
                SyntheticFrameBudget(maxAiThinks: 30, maxPathSolves: M137PerformanceComfortPolicy.M3NavMeshPathSolveBudgetPerFrame + 1, activeThinkFrames: 1),
                bossFullLodObserved: false,
                reducedOrBackgroundAddObserved: true,
                enforceFrameTimingWhenTrusted: true);

            Assert.IsFalse(summary.passed);
            Assert.That(string.Join("\n", summary.failures), Does.Contain("Max path solves"));
            Assert.That(string.Join("\n", summary.failures), Does.Contain("clustered"));
        }

        [Test]
        public void M138ReportAcceptsCleanFrameCapCadenceJitter()
        {
            var scenario = System.Array.Find(M138CombatScaleStressScenarioPolicy.StressManifest, item => item.id == "boss_plus_adds");
            var result = SyntheticResult(
                scenario,
                peakEnemies: scenario.targetEnemyCount + 1,
                peakProjectiles: 8,
                observedBoss: true,
                aiThinkReduced: 12,
                aiCommandReuses: 20,
                frameP95Ms: 17.6d,
                frameMaxMs: 24.1d,
                cpuStageSummary: "tactical_director count=30 maxMs=1.328 maxGc=0; add_ai_think_scorer count=32 maxMs=1.35 maxGc=0");

            var summary = M138CombatScaleStressReportGenerator.BuildScenarioSummary(
                scenario,
                result,
                SyntheticFrameBudget(maxAiThinks: 3, maxPathSolves: M137PerformanceComfortPolicy.M3NavMeshPathSolveBudgetPerFrame),
                bossFullLodObserved: true,
                reducedOrBackgroundAddObserved: true,
                enforceFrameTimingWhenTrusted: true);

            Assert.IsTrue(summary.passed, string.Join("; ", summary.failures));
            StringAssert.Contains("Accepted borderline", summary.note);
        }

        [Test]
        public void M138StressHarnessUsesDedicatedNavMeshBakeMode()
        {
            Assert.AreEqual(
                RoomNavMeshRuntimeFallbackMode.AutomatedStressHarnessRuntimeBake,
                M138CombatScaleStressRunner.StressHarnessNavMeshModeForDiagnostics);
        }

        [Test]
        public void M138ReportDetectsMissingBossAndProjectileContent()
        {
            var scenario = System.Array.Find(M138CombatScaleStressScenarioPolicy.StressManifest, item => item.id == "boss_plus_projectile_adds");
            var result = SyntheticResult(
                scenario,
                peakEnemies: scenario.targetEnemyCount,
                peakProjectiles: 0,
                observedBoss: false,
                aiThinkReduced: 6,
                aiCommandReuses: 10);
            var summary = M138CombatScaleStressReportGenerator.BuildScenarioSummary(
                scenario,
                result,
                SyntheticFrameBudget(maxAiThinks: 3, maxPathSolves: 2),
                bossFullLodObserved: false,
                reducedOrBackgroundAddObserved: true,
                enforceFrameTimingWhenTrusted: true);

            Assert.IsFalse(summary.passed);
            var failures = string.Join("\n", summary.failures);
            Assert.That(failures, Does.Contain("Projectile-heavy"));
            Assert.That(failures, Does.Contain("active boss"));
            Assert.That(failures, Does.Contain("Full AI LOD"));
        }

        [Test]
        public void MilestoneThreeCountersCaptureAiNavigationAndAvoidanceSignals()
        {
            M136PerformanceOperationCounters.Reset();

            M136PerformanceOperationCounters.ReportActiveEnemyCount(30);
            M136PerformanceOperationCounters.ReportAiThink((int)EnemyAiLodTier.Full);
            M136PerformanceOperationCounters.ReportAiThink((int)EnemyAiLodTier.Reduced);
            M136PerformanceOperationCounters.ReportAiThink((int)EnemyAiLodTier.Background);
            M136PerformanceOperationCounters.ReportAiCommandReuse();
            M136PerformanceOperationCounters.ReportAiScorerCall(5);
            M136PerformanceOperationCounters.ReportAiBehaviorGraphTick();
            M136PerformanceOperationCounters.ReportAiLodTransition();
            M136PerformanceOperationCounters.ReportNavPathRequest();
            M136PerformanceOperationCounters.ReportNavPathSolve(2.4f);
            M136PerformanceOperationCounters.ReportNavPathDeferred();
            M136PerformanceOperationCounters.ReportNavPathFallback();
            M136PerformanceOperationCounters.ReportAvoidanceTier((int)EnemyAiLodTier.Full);
            M136PerformanceOperationCounters.ReportAvoidanceTier((int)EnemyAiLodTier.Reduced);
            M136PerformanceOperationCounters.ReportAvoidanceTier((int)EnemyAiLodTier.Background);
            M136PerformanceOperationCounters.ReportBossAddScorerSkip();
            M136PerformanceOperationCounters.ReportBossAddCachedCommandReuse();
            M136PerformanceOperationCounters.ReportTacticalCrowdReservationSkip(7);
            M136PerformanceOperationCounters.ReportTacticalCrowdCachedIntentReuse(9);
            M136PerformanceOperationCounters.ReportTacticalCrowdSupportReservationBudgetUse();
            M136PerformanceOperationCounters.ReportTacticalCrowdActiveThreatLimit(2);
            M136PerformanceOperationCounters.ReportTacticalCrowdScorerSkip(5);

            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.AreEqual(30, snapshot.ActiveEnemyMax);
            Assert.AreEqual(1, snapshot.AiThinkFull);
            Assert.AreEqual(1, snapshot.AiThinkReduced);
            Assert.AreEqual(1, snapshot.AiThinkBackground);
            Assert.AreEqual(1, snapshot.AiCommandReuses);
            Assert.AreEqual(1, snapshot.AiScorerCalls);
            Assert.AreEqual(5, snapshot.AiScorerCandidates);
            Assert.AreEqual(1, snapshot.AiBehaviorGraphTicks);
            Assert.AreEqual(1, snapshot.AiLodTransitions);
            Assert.AreEqual(1, snapshot.NavPathRequests);
            Assert.AreEqual(1, snapshot.NavPathSolves);
            Assert.AreEqual(1, snapshot.NavPathDeferred);
            Assert.AreEqual(1, snapshot.NavPathFallbacks);
            Assert.AreEqual(2.4f, snapshot.NavPathMaxSolveMilliseconds);
            Assert.AreEqual(1, snapshot.AvoidanceHigh);
            Assert.AreEqual(1, snapshot.AvoidanceReduced);
            Assert.AreEqual(1, snapshot.AvoidanceBackground);
            StringAssert.Contains("bossAddScorerSkips=1", snapshot.TacticalDirectorSummary);
            StringAssert.Contains("bossAddCachedCommandReuses=1", snapshot.TacticalDirectorSummary);
            Assert.AreEqual(7, snapshot.TacticalCrowdReservationSkips);
            Assert.AreEqual(9, snapshot.TacticalCrowdCachedIntentReuses);
            Assert.AreEqual(1, snapshot.TacticalCrowdSupportReservationBudgetUses);
            Assert.AreEqual(2, snapshot.TacticalCrowdActiveThreatLimitMax);
            Assert.AreEqual(5, snapshot.TacticalCrowdScorerSkips);
            StringAssert.Contains("crowdReservationSkips=7", snapshot.TacticalDirectorSummary);
            StringAssert.Contains("crowdScorerSkips=5", snapshot.TacticalDirectorSummary);
        }

        private static M136PerformanceScenarioResult SyntheticResult(
            M138CombatScaleStressScenarioDefinition scenario,
            int peakEnemies,
            int peakProjectiles,
            bool observedBoss,
            int aiThinkReduced,
            int aiCommandReuses,
            double frameP95Ms = 12d,
            double frameMaxMs = 15d,
            string cpuStageSummary = "")
        {
            return new M136PerformanceScenarioResult
            {
                scenarioId = scenario.id,
                displayName = scenario.displayName,
                samplingSource = M136FrameCadencePolicy.RuntimeUpdateSamplingSource,
                frameCadenceConfidence = M136FrameCadencePolicy.Trusted,
                rawSampleCount = 1800,
                sampleRateHz = 60d,
                metrics = new[]
                {
                    new M136PerformanceMetricSummary
                    {
                        id = "frame_time_ms",
                        supported = true,
                        sampleCount = 1800,
                        p50 = 10d,
                        p95 = frameP95Ms,
                        max = frameMaxMs
                    },
                    new M136PerformanceMetricSummary
                    {
                        id = "gc_allocated_bytes",
                        supported = true,
                        sampleCount = 1800,
                        p50 = 0d,
                        p95 = 0d,
                        max = 0d
                    }
                },
                operations = new M136RuntimeOperationSummary
                {
                    activeEnemyMax = peakEnemies,
                    aiThinkFull = 30,
                    aiThinkReduced = aiThinkReduced,
                    aiThinkBackground = 2,
                    aiCommandReuses = aiCommandReuses,
                    aiScorerCalls = 14,
                    aiBehaviorGraphTicks = 36,
                    aiLodTransitions = 3,
                    navPathRequests = 24,
                    navPathSolves = 18,
                    navPathDeferred = 6,
                    navPathFallbacks = 0,
                    runtimeNavMeshFallbacks = 0,
                    navPathMaxSolveMilliseconds = 0.75f,
                    avoidanceHigh = 12,
                    avoidanceReduced = 8,
                    avoidanceBackground = 4,
                    projectileActivePeak = peakProjectiles,
                    projectileSpawns = peakProjectiles,
                    projectileReturns = peakProjectiles / 2,
                    projectileCollisionChecks = peakProjectiles * 10,
                    cpuStageSummary = cpuStageSummary
                },
                objectCounts = new M136LiveObjectCountSummary
                {
                    sampleCount = 1800,
                    peakEnemies = peakEnemies,
                    peakProjectiles = peakProjectiles,
                    observedCombatController = true,
                    observedActiveCombat = true,
                    observedBoss = observedBoss,
                    source = "m138-synthetic"
                }
            };
        }

        private static M138CombatScaleStressFrameBudgetSummary SyntheticFrameBudget(int maxAiThinks, int maxPathSolves, int activeThinkFrames = 24)
        {
            return new M138CombatScaleStressFrameBudgetSummary
            {
                sampleFrameCount = 1800,
                aiThinkActiveFrameCount = activeThinkFrames,
                navSolveActiveFrameCount = 18,
                maxAiThinksInFrame = maxAiThinks,
                maxPathSolvesInFrame = maxPathSolves,
                maxPathRequestsInFrame = maxPathSolves + 1,
                maxPathDeferredInFrame = 2,
                maxScorerCallsInFrame = 2,
                maxBehaviorGraphTicksInFrame = 3
            };
        }

        private static EnemyNavigationRequest NavigationRequest(
            EnemyAiLodTier lodTier,
            EnemyTacticalRole tacticalRole,
            EnemyIntelligenceLevel intelligence,
            int pathSeed)
        {
            return new EnemyNavigationRequest(
                null,
                Vector3.zero,
                Vector3.forward,
                0.4f,
                EnemyNavigationMode.GroundedLocal,
                EnemyNavigationIntent.MoveToPlayer,
                intelligence,
                allowPathfinding: true,
                pathSeed: pathSeed,
                aiLodTier: lodTier,
                tacticalRole: tacticalRole);
        }

        private static EnemyTacticalIntent TacticalIntent(EnemyTacticalRole role)
        {
            return new EnemyTacticalIntent(
                role,
                EnemyTacticalCommitPolicy.None,
                string.Empty,
                Vector3.zero,
                false,
                -1,
                0f,
                string.Empty,
                "m138_test",
                "m138_test");
        }

        private static void SetPrivate<T>(EnemyRuntimeController enemy, string fieldName, T value)
        {
            var field = typeof(EnemyRuntimeController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(enemy, value);
        }
    }
}
