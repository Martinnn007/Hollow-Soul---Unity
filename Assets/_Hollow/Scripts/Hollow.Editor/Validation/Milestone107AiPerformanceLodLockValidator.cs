using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone107AiPerformanceLodLockValidator
    {
        private static readonly string[] RequiredDocsText =
        {
            "AI Performance",
            "LOD",
            "Arena",
            "NavMesh agent",
            "pending paths",
            "Unity Behavior graph ticks",
            "action scorer calls",
            "stuck agents",
            "pressure"
        };

        [MenuItem("Hollow/Validation/Run Milestone 107 AI Performance + LOD Lock Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateRuntimeContracts(failures);
            ValidateArtifacts(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 107 AI Performance + LOD Lock validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateRuntimeContracts(List<string> failures)
        {
            EnemyAiDebugOverlay.ResetDiagnostics();
            EnemyNavigationDebugOverlay.ResetDiagnostics();
            EnemyAiDebugOverlay.ReportRoomEnemyCount(40);
            EnemyAiDebugOverlay.RecordBrainThink(1, EnemyAiLodTier.Full, 0.16f);
            EnemyAiDebugOverlay.RecordCommandReuse(2, EnemyAiLodTier.Reduced);
            EnemyAiDebugOverlay.RecordScorerCall(8);
            EnemyAiDebugOverlay.RecordBehaviorGraphTick(usedEmergencyFallback: true);
            EnemyAiDebugOverlay.RecordPressurePenalty(0.5f);
            EnemyNavigationDebugOverlay.ReportActivePathUser(3, pathPending: true, stuck: true, reason: "validator_stuck_probe");

            var aiStats = EnemyAiDebugOverlay.PerformanceStats;
            var navStats = EnemyNavigationDebugOverlay.Stats;
            if (aiStats.ActiveAiAgents < 40)
            {
                failures.Add("M107 AI diagnostics must expose active AI agent counts for Arena profiling.");
            }

            if (aiStats.ScorerCallsPerSecond <= 0 || aiStats.BehaviorGraphTicksPerSecond <= 0)
            {
                failures.Add("M107 AI diagnostics must track scorer calls and Unity Behavior graph ticks.");
            }

            if (navStats.PendingPathUsers <= 0 || navStats.StuckAgents <= 0)
            {
                failures.Add("M107 navigation diagnostics must track pending paths and stuck agents.");
            }

            var protectedInterval = EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics(
                EnemyIntelligenceLevel.Simple,
                EnemyAiLodTier.Full,
                activeEnemyCount: 40,
                pendingPathCount: 12,
                protectResponsiveness: true);
            var reducedLoadedInterval = EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics(
                EnemyIntelligenceLevel.Simple,
                EnemyAiLodTier.Reduced,
                activeEnemyCount: 40,
                pendingPathCount: 12,
                protectResponsiveness: false);
            if (protectedInterval > 0.17f)
            {
                failures.Add("M107 must not slow protected close/committed Full LOD enemies.");
            }

            if (reducedLoadedInterval <= 0.48f)
            {
                failures.Add("M107 must stretch Reduced LOD think cadence under Arena swarm/path load.");
            }
        }

        private static void ValidateArtifacts(List<string> failures)
        {
            ExpectFile(Milestone107AiPerformanceLodLockAssetGenerator.DocsPath, failures);
            ExpectFile(Milestone107AiPerformanceLodLockAssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone107AiPerformanceLodLockAssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone107AiPerformanceLodLockAssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required, System.StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"M107 docs are missing `{required}`.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M107 artifact `{path}`.");
            }
        }
    }
}
