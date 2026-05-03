using UnityEngine;

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
            LodTier = ResolveLodTier(enemy, distanceToPlayer);
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
            return true;
        }

        public EnemyBehaviorCommand ChooseCommand(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand treeCommand,
            float timeSeconds,
            float distanceToPlayer,
            RoomThreatDirector threatDirector)
        {
            LodTier = ResolveLodTier(enemy, distanceToPlayer);
            nextThinkTime = timeSeconds + ThinkInterval(enemy, LodTier) + ThinkJitter(enemy);
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
            if (enemy == null || !enemy.IsAlive || enemy.BossDefinition != null)
            {
                return EnemyAiLodTier.Background;
            }

            if (enemy.ReadabilityState != EnemyReadabilityState.Idle ||
                enemy.IsEndangeredNow ||
                distanceToPlayer <= 5.5f)
            {
                return EnemyAiLodTier.Full;
            }

            if (enemy.AwarenessState == EnemyAwarenessState.Engaged ||
                enemy.AwarenessState is EnemyAwarenessState.Alerted or EnemyAwarenessState.Suspicious ||
                distanceToPlayer <= 10f)
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

        private static float ThinkInterval(EnemyRuntimeController enemy, EnemyAiLodTier tier)
        {
            var intelligence = enemy != null ? enemy.Intelligence : EnemyIntelligenceLevel.Simple;
            var baseInterval = tier switch
            {
                EnemyAiLodTier.Full => intelligence is EnemyIntelligenceLevel.Tactical or EnemyIntelligenceLevel.Cunning ? 0.11f : 0.16f,
                EnemyAiLodTier.Reduced => intelligence is EnemyIntelligenceLevel.Tactical or EnemyIntelligenceLevel.Cunning ? 0.34f : 0.48f,
                _ => 0.95f
            };
            return baseInterval;
        }

        private static float ThinkJitter(EnemyRuntimeController enemy)
        {
            var seed = Mathf.Abs(enemy != null ? enemy.SpawnIndex : 0);
            return (seed % 7) * 0.018f;
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
