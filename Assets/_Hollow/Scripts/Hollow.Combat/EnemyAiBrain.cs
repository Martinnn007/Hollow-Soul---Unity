using UnityEngine;
using Hollow.Core.Diagnostics;

namespace Hollow.Combat
{
    public sealed class EnemyAiBrain
    {
        private EnemyBehaviorCommand cachedCommand = EnemyBehaviorCommand.None("ai_uninitialized");
        private float nextThinkTime;
        private string topScores = string.Empty;

        public EnemyAiLodTier LodTier { get; private set; } = EnemyAiLodTier.Full;

        public EnemyAiBlackboard Blackboard { get; private set; } = EnemyAiBlackboard.Empty;

        public void Reset()
        {
            cachedCommand = EnemyBehaviorCommand.None("ai_reset");
            nextThinkTime = 0f;
            topScores = string.Empty;
            LodTier = EnemyAiLodTier.Full;
            Blackboard = EnemyAiBlackboard.Empty;
        }

        public bool TryReuseCommand(
            EnemyRuntimeController enemy,
            float timeSeconds,
            float distanceToPlayer,
            out EnemyBehaviorCommand command)
        {
            command = EnemyBehaviorCommand.None("ai_no_cached_command");
            SetLodTier(ResolveLodTier(enemy, distanceToPlayer));
            if (enemy != null)
            {
                EnemyAiDebugOverlay.ReportBrainAgent(enemy.GetInstanceID(), LodTier);
            }

            if (enemy == null ||
                enemy.ReadabilityState != EnemyReadabilityState.Idle ||
                timeSeconds >= nextThinkTime ||
                cachedCommand.Kind == EnemyBehaviorCommandKind.None ||
                cachedCommand.StartsCommittedAction)
            {
                return false;
            }

            command = SimplifyForLod(cachedCommand, enemy);
            UpdateBlackboard(enemy, cachedCommand, command, distanceToPlayer, 0f, "cached_plan");
            EnemyAiDebugOverlay.RecordCommandReuse(enemy.GetInstanceID(), LodTier);
            return true;
        }

        public EnemyBehaviorCommand ChooseCommand(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand treeCommand,
            float timeSeconds,
            float distanceToPlayer,
            RoomThreatDirector threatDirector)
        {
            SetLodTier(ResolveLodTier(enemy, distanceToPlayer));
            var thinkInterval = ThinkInterval(enemy, LodTier);
            nextThinkTime = timeSeconds + thinkInterval + ThinkJitter(enemy);
            if (enemy != null)
            {
                EnemyAiDebugOverlay.RecordBrainThink(enemy.GetInstanceID(), LodTier, thinkInterval);
                M136PerformanceOperationCounters.ReportAiThink((int)LodTier);
            }

            var chosen = SimplifyForLod(treeCommand, enemy);
            var pressurePenalty = 0f;
            var cooldownReason = "tree";
            topScores = string.Empty;

            if (chosen.StartsCommittedAction &&
                LodTier != EnemyAiLodTier.Background &&
                EnemyActionScorer.TryChooseAction(enemy, chosen, timeSeconds, distanceToPlayer, threatDirector, out var scored, out topScores))
            {
                chosen = new EnemyBehaviorCommand(scored.CommandKind, scored.ActionId, Mathf.Max(0.1f, treeCommand.SpeedMultiplier), "ai_scorer");
                pressurePenalty = scored.PressurePenalty;
                cooldownReason = scored.Reason;
                EnemyAiDebugOverlay.RecordPressurePenalty(pressurePenalty);
            }
            else if (chosen.StartsCommittedAction && LodTier == EnemyAiLodTier.Background)
            {
                chosen = new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "ai_background_hold");
                cooldownReason = "background_lod";
            }
            else if (chosen.StartsCommittedAction)
            {
                cooldownReason = string.IsNullOrWhiteSpace(topScores) ? "no_scorer_candidate" : topScores;
            }

            cachedCommand = chosen;
            UpdateBlackboard(enemy, treeCommand, chosen, distanceToPlayer, pressurePenalty, cooldownReason);
            return chosen;
        }

        public static EnemyAiLodTier ResolveLodTier(EnemyRuntimeController enemy, float distanceToPlayer)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                return EnemyAiLodTier.Background;
            }

            if (enemy.BossDefinition != null)
            {
                return EnemyAiLodTier.Full;
            }

            if (enemy.ReadabilityState != EnemyReadabilityState.Idle ||
                enemy.IsEndangeredNow ||
                enemy.LastTacticalIntent.Role == EnemyTacticalRole.ActiveThreat ||
                distanceToPlayer <= 4.75f)
            {
                return EnemyAiLodTier.Full;
            }

            if (!enemy.IsVisibleToCamera &&
                distanceToPlayer > 8f &&
                enemy.LastTacticalIntent.Role is EnemyTacticalRole.Waiting or EnemyTacticalRole.Hold or EnemyTacticalRole.StationarySentinel or EnemyTacticalRole.None)
            {
                return EnemyAiLodTier.Background;
            }

            if (enemy.AwarenessState == EnemyAwarenessState.Engaged ||
                enemy.AwarenessState is EnemyAwarenessState.Alerted or EnemyAwarenessState.Suspicious ||
                enemy.LastTacticalIntent.Role is EnemyTacticalRole.SupportPressure or EnemyTacticalRole.Reposition or EnemyTacticalRole.Investigate ||
                distanceToPlayer <= 11f)
            {
                return EnemyAiLodTier.Reduced;
            }

            return EnemyAiLodTier.Background;
        }

        private static EnemyBehaviorCommand SimplifyForLod(EnemyBehaviorCommand command, EnemyRuntimeController enemy)
        {
            if (enemy == null)
            {
                return command;
            }

            var tier = ResolveLodTier(enemy, enemy.DistanceToPlayerMeters);
            if (tier != EnemyAiLodTier.Background)
            {
                return command;
            }

            return command.Kind switch
            {
                EnemyBehaviorCommandKind.StartMeleeAction or
                    EnemyBehaviorCommandKind.StartRangedAction or
                    EnemyBehaviorCommandKind.StartChargeAction or
                    EnemyBehaviorCommandKind.StartAreaAction or
                    EnemyBehaviorCommandKind.StartGuardAction or
                    EnemyBehaviorCommandKind.StartCreatureMoveAction or
                    EnemyBehaviorCommandKind.StartCreatureSignalAction => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "ai_background_no_commit"),
                EnemyBehaviorCommandKind.MoveToPlayer or EnemyBehaviorCommandKind.MovePreferredRange => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "ai_background_face"),
                _ => command
            };
        }

        public static float ResolveAdaptiveThinkIntervalForDiagnostics(
            EnemyIntelligenceLevel intelligence,
            EnemyAiLodTier tier,
            int activeEnemyCount,
            int pendingPathCount,
            bool protectResponsiveness)
        {
            var baseInterval = BaseThinkInterval(intelligence, tier);
            if (protectResponsiveness)
            {
                return baseInterval;
            }

            var swarmLoad = Mathf.Clamp01((Mathf.Max(0, activeEnemyCount) - 18) / 22f);
            var pathLoad = Mathf.Clamp01(Mathf.Max(0, pendingPathCount) / 16f);
            var load = Mathf.Max(swarmLoad, pathLoad * 0.75f);
            var multiplier = tier switch
            {
                EnemyAiLodTier.Full => 1f + load * 0.22f,
                EnemyAiLodTier.Reduced => 1f + load * 0.55f,
                _ => 1f + load * 0.85f
            };
            return Mathf.Min(baseInterval * multiplier, tier switch
            {
                EnemyAiLodTier.Full => 0.24f,
                EnemyAiLodTier.Reduced => 0.82f,
                _ => 1.65f
            });
        }

        private static float ThinkInterval(EnemyRuntimeController enemy, EnemyAiLodTier tier)
        {
            var intelligence = enemy != null ? enemy.Intelligence : EnemyIntelligenceLevel.Simple;
            var navStats = EnemyNavigationDebugOverlay.Stats;
            var protectResponsiveness = enemy != null &&
                (enemy.ReadabilityState != EnemyReadabilityState.Idle ||
                    enemy.IsEndangeredNow ||
                    enemy.DistanceToPlayerMeters <= 4.25f);
            return ResolveAdaptiveThinkIntervalForDiagnostics(
                intelligence,
                tier,
                EnemyAiDebugOverlay.EstimatedActiveAiAgents,
                navStats.PendingPathUsers,
                protectResponsiveness);
        }

        private static float BaseThinkInterval(EnemyIntelligenceLevel intelligence, EnemyAiLodTier tier)
        {
            return tier switch
            {
                EnemyAiLodTier.Full => intelligence is EnemyIntelligenceLevel.Tactical or EnemyIntelligenceLevel.Cunning
                    ? M137PerformanceComfortPolicy.M3FullThreatMinThinkIntervalSeconds
                    : M137PerformanceComfortPolicy.M3FullThreatMaxThinkIntervalSeconds,
                EnemyAiLodTier.Reduced => intelligence is EnemyIntelligenceLevel.Tactical or EnemyIntelligenceLevel.Cunning
                    ? M137PerformanceComfortPolicy.M3ReducedThreatMinThinkIntervalSeconds
                    : M137PerformanceComfortPolicy.M3ReducedThreatMaxThinkIntervalSeconds,
                _ => M137PerformanceComfortPolicy.M3BackgroundMaxThinkIntervalSeconds
            };
        }

        private static float ThinkJitter(EnemyRuntimeController enemy)
        {
            var seed = Mathf.Abs(enemy != null ? enemy.SpawnIndex : 0);
            return (seed % 11) * 0.011f;
        }

        private void SetLodTier(EnemyAiLodTier nextTier)
        {
            if (LodTier != nextTier)
            {
                M136PerformanceOperationCounters.ReportAiLodTransition();
            }

            LodTier = nextTier;
        }

        private void UpdateBlackboard(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand treeCommand,
            EnemyBehaviorCommand chosenCommand,
            float distanceToPlayer,
            float pressurePenalty,
            string cooldownReason)
        {
            Blackboard = new EnemyAiBlackboard(
                LodTier,
                treeCommand.Kind,
                chosenCommand.Kind,
                chosenCommand.ActionId,
                ExtractTopScore(topScores),
                pressurePenalty,
                distanceToPlayer,
                enemy != null ? enemy.LastNavigationPathStatus : EnemyPathStatus.NotRequested,
                cooldownReason,
                topScores);
        }

        private static float ExtractTopScore(string scores)
        {
            if (string.IsNullOrWhiteSpace(scores))
            {
                return 0f;
            }

            var colon = scores.IndexOf(':');
            if (colon < 0 || colon + 1 >= scores.Length)
            {
                return 0f;
            }

            var comma = scores.IndexOf(',', colon + 1);
            var value = comma >= 0
                ? scores.Substring(colon + 1, comma - colon - 1)
                : scores.Substring(colon + 1);
            return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0f;
        }
    }
}
