using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone107AiPerformanceLodLockTests
    {
        [Test]
        public void AiPerformanceOverlayTracksArenaScaleSignals()
        {
            EnemyAiDebugOverlay.ResetDiagnostics();
            EnemyNavigationDebugOverlay.ResetDiagnostics();

            EnemyAiDebugOverlay.ReportRoomEnemyCount(32);
            EnemyAiDebugOverlay.RecordBrainThink(1, EnemyAiLodTier.Full, 0.16f);
            EnemyAiDebugOverlay.RecordCommandReuse(2, EnemyAiLodTier.Reduced);
            EnemyAiDebugOverlay.RecordScorerCall(9);
            EnemyAiDebugOverlay.RecordBehaviorGraphTick(usedEmergencyFallback: true);
            EnemyAiDebugOverlay.RecordPressurePenalty(0.6f);
            EnemyNavigationDebugOverlay.ReportActivePathUser(3, pathPending: true, stuck: true, reason: "test_stuck");

            var aiStats = EnemyAiDebugOverlay.PerformanceStats;
            Assert.AreEqual(32, aiStats.ActiveAiAgents);
            Assert.AreEqual(1, aiStats.BrainThinksPerSecond);
            Assert.AreEqual(1, aiStats.CommandReusesPerSecond);
            Assert.AreEqual(1, aiStats.ScorerCallsPerSecond);
            Assert.AreEqual(9, aiStats.ScorerCandidatesPerSecond);
            Assert.AreEqual(1, aiStats.BehaviorGraphTicksPerSecond);
            Assert.AreEqual(1, aiStats.BehaviorGraphFallbacksPerSecond);
            Assert.AreEqual(1, aiStats.StuckAgents);
            Assert.Greater(aiStats.MaxPressurePenalty, 0f);

            StringAssert.Contains("scorer/s", EnemyAiDebugOverlay.DiagnosticsSummary);
            StringAssert.Contains("UB/s", EnemyAiDebugOverlay.DiagnosticsSummary);
            StringAssert.Contains("stuck", EnemyNavigationDebugOverlay.DiagnosticsSummary);
            Assert.AreEqual(1, EnemyNavigationDebugOverlay.Stats.PendingPathUsers);
        }

        [Test]
        public void AdaptiveThinkCadenceProtectsCloseThreatsButStretchesBackliners()
        {
            var protectedFull = EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics(
                EnemyIntelligenceLevel.Simple,
                EnemyAiLodTier.Full,
                activeEnemyCount: 40,
                pendingPathCount: 14,
                protectResponsiveness: true);
            var loadedFull = EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics(
                EnemyIntelligenceLevel.Simple,
                EnemyAiLodTier.Full,
                activeEnemyCount: 40,
                pendingPathCount: 14,
                protectResponsiveness: false);
            var loadedReduced = EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics(
                EnemyIntelligenceLevel.Simple,
                EnemyAiLodTier.Reduced,
                activeEnemyCount: 40,
                pendingPathCount: 14,
                protectResponsiveness: false);
            var loadedBackground = EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics(
                EnemyIntelligenceLevel.Simple,
                EnemyAiLodTier.Background,
                activeEnemyCount: 40,
                pendingPathCount: 14,
                protectResponsiveness: false);

            Assert.That(protectedFull, Is.EqualTo(0.16f).Within(0.001f));
            Assert.Greater(loadedFull, protectedFull);
            Assert.Greater(loadedReduced, 0.48f);
            Assert.LessOrEqual(loadedReduced, 0.82f);
            Assert.Greater(loadedBackground, 0.95f);
            Assert.LessOrEqual(loadedBackground, 1.65f);
        }

        [Test]
        public void M107ArtifactsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone107AiPerformanceLodLockAssetGenerator.DocsPath), Milestone107AiPerformanceLodLockAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone107AiPerformanceLodLockAssetGenerator.ReportPath), Milestone107AiPerformanceLodLockAssetGenerator.ReportPath);
            var docs = File.ReadAllText(Milestone107AiPerformanceLodLockAssetGenerator.DocsPath);
            StringAssert.Contains("AI Performance", docs);
            StringAssert.Contains("20-40", docs);
            StringAssert.Contains("Unity Behavior graph ticks", docs);
            StringAssert.Contains("action scorer calls", docs);
            Assert.IsTrue(Milestone107AiPerformanceLodLockValidator.Validate());
        }
    }
}
