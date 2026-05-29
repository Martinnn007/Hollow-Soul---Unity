using System.Reflection;
using Hollow.Combat;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
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
