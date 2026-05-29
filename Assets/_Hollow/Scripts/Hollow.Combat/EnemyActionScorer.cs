using System;
using UnityEngine;

namespace Hollow.Combat
{
    public static class EnemyActionScorer
    {
        public static bool TryChooseAction(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand requestedCommand,
            float timeSeconds,
            float distanceToPlayer,
            RoomThreatDirector threatDirector,
            out EnemyAiActionScore best,
            out string topScores)
        {
            best = default;
            topScores = string.Empty;
            if (enemy == null ||
                enemy.Definition == null ||
                !requestedCommand.StartsCommittedAction)
            {
                return false;
            }

            var actions = enemy.Definition.ActionProfiles;
            EnemyAiDebugOverlay.RecordScorerCall(actions.Count);
            var hasCandidate = false;
            var emitDebugScores = EnemyAiDebugOverlay.BlackboardEnabled;
            var top1 = default(EnemyAiActionScore);
            var top2 = default(EnemyAiActionScore);
            var top3 = default(EnemyAiActionScore);
            var topCount = 0;
            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index];
                if (!TryScoreAction(enemy, action, requestedCommand.Kind, timeSeconds, distanceToPlayer, threatDirector, out var score))
                {
                    continue;
                }

                if (!hasCandidate || IsBetterScore(score, best))
                {
                    best = score;
                    hasCandidate = true;
                }

                if (emitDebugScores)
                {
                    InsertDebugTopScore(score, ref top1, ref top2, ref top3, ref topCount);
                }
            }

            if (!hasCandidate)
            {
                topScores = emitDebugScores ? "no_valid_action" : string.Empty;
                return false;
            }

            if (emitDebugScores)
            {
                topScores = FormatDebugTopScores(top1, top2, top3, topCount);
            }

            return true;
        }

        private static bool IsBetterScore(EnemyAiActionScore candidate, EnemyAiActionScore current)
        {
            if (candidate.Score > current.Score)
            {
                return true;
            }

            return Mathf.Approximately(candidate.Score, current.Score) &&
                string.Compare(candidate.ActionId, current.ActionId, StringComparison.Ordinal) < 0;
        }

        private static void InsertDebugTopScore(
            EnemyAiActionScore score,
            ref EnemyAiActionScore top1,
            ref EnemyAiActionScore top2,
            ref EnemyAiActionScore top3,
            ref int topCount)
        {
            if (topCount == 0 || IsBetterScore(score, top1))
            {
                top3 = top2;
                top2 = top1;
                top1 = score;
                topCount = Mathf.Min(3, topCount + 1);
                return;
            }

            if (topCount == 1 || IsBetterScore(score, top2))
            {
                top3 = top2;
                top2 = score;
                topCount = Mathf.Min(3, topCount + 1);
                return;
            }

            if (topCount == 2 || IsBetterScore(score, top3))
            {
                top3 = score;
                topCount = Mathf.Min(3, topCount + 1);
            }
        }

        private static string FormatDebugTopScores(EnemyAiActionScore top1, EnemyAiActionScore top2, EnemyAiActionScore top3, int topCount)
        {
            var line = $"{top1.ActionId}:{top1.Score:0.00}";
            if (topCount >= 2)
            {
                line += $", {top2.ActionId}:{top2.Score:0.00}";
            }

            if (topCount >= 3)
            {
                line += $", {top3.ActionId}:{top3.Score:0.00}";
            }

            return line;
        }

        public static bool TryScoreAction(
            EnemyRuntimeController enemy,
            EnemyActionProfileDefinition action,
            EnemyBehaviorCommandKind requestedKind,
            float timeSeconds,
            float distanceToPlayer,
            RoomThreatDirector threatDirector,
            out EnemyAiActionScore score)
        {
            score = default;
            if (enemy == null ||
                action == null ||
                action.UsageState != EnemyActionUsageState.CurrentRuntime ||
                (int)enemy.Intelligence < (int)action.MinimumIntelligence ||
                enemy.AwarenessState < action.MinimumAwareness ||
                !ActionAllowsDisposition(action, enemy.Disposition))
            {
                return false;
            }

            var attack = enemy.ResolveAttackProfileForAi(action.LinkedAttackId);
            var commandKind = CommandKindFor(action, attack);
            if (commandKind != requestedKind)
            {
                return false;
            }

            var actionId = attack != null ? attack.AttackId : action.ActionId;
            if (!enemy.CanStartBehaviorCommand(commandKind, actionId, timeSeconds))
            {
                return false;
            }

            var rangeScore = RangeScore(action, attack, distanceToPlayer);
            var facingScore = FacingScore(enemy, action);
            var awarenessScore = AwarenessScore(enemy, action);
            var dispositionScore = DispositionScore(enemy, action);
            var intelligenceScore = Mathf.Max(0, (int)enemy.Intelligence - (int)action.MinimumIntelligence) * 0.16f;
            var threatScore = ThreatScore(attack);
            var recoveryPenalty = Mathf.Clamp(action.PunishabilityRating * 0.05f, 0f, 0.3f);
            var pressurePenalty = threatDirector != null ? threatDirector.PressurePenaltyFor(enemy, action, attack) : 0f;
            var deterministic = DeterministicVariation(enemy.SpawnIndex, actionId, timeSeconds);
            var total = action.BaseWeight +
                        rangeScore +
                        facingScore +
                        awarenessScore +
                        dispositionScore +
                        intelligenceScore +
                        threatScore +
                        deterministic -
                        recoveryPenalty -
                        pressurePenalty;
            score = new EnemyAiActionScore(
                action,
                attack,
                commandKind,
                total,
                pressurePenalty,
                $"range {rangeScore:0.00} face {facingScore:0.00} pressure -{pressurePenalty:0.00}");
            return true;
        }

        public static EnemyBehaviorCommandKind CommandKindFor(EnemyActionProfileDefinition action, EnemyAttackProfileDefinition attack)
        {
            if (attack != null)
            {
                return attack.RuntimeKind switch
                {
                    EnemyAttackRuntimeKind.MeleeLunge or EnemyAttackRuntimeKind.Contact or EnemyAttackRuntimeKind.WeaponMelee => EnemyBehaviorCommandKind.StartMeleeAction,
                    EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile or EnemyAttackRuntimeKind.SequentialRadialProjectile or EnemyAttackRuntimeKind.Beam or EnemyAttackRuntimeKind.LockingBeam => EnemyBehaviorCommandKind.StartRangedAction,
                    EnemyAttackRuntimeKind.Charge => EnemyBehaviorCommandKind.StartChargeAction,
                    EnemyAttackRuntimeKind.Area => EnemyBehaviorCommandKind.StartAreaAction,
                    EnemyAttackRuntimeKind.Defense => EnemyBehaviorCommandKind.StartGuardAction,
                    EnemyAttackRuntimeKind.CreatureMove or EnemyAttackRuntimeKind.PhaseMove => EnemyBehaviorCommandKind.StartCreatureMoveAction,
                    EnemyAttackRuntimeKind.CreatureSignal => EnemyBehaviorCommandKind.StartCreatureSignalAction,
                    _ => EnemyBehaviorCommandKind.None
                };
            }

            if (action == null)
            {
                return EnemyBehaviorCommandKind.None;
            }

            return action.Intent switch
            {
                EnemyActionIntent.Defend => EnemyBehaviorCommandKind.StartGuardAction,
                EnemyActionIntent.Escape or EnemyActionIntent.Reposition => EnemyBehaviorCommandKind.StartCreatureMoveAction,
                _ => EnemyBehaviorCommandKind.None
            };
        }

        private static bool ActionAllowsDisposition(EnemyActionProfileDefinition action, EnemyInstinctDisposition disposition)
        {
            var dispositions = action.AllowedDispositions;
            if (dispositions.Count == 0)
            {
                return true;
            }

            for (var index = 0; index < dispositions.Count; index++)
            {
                if (dispositions[index] == disposition)
                {
                    return true;
                }
            }

            return false;
        }

        private static float RangeScore(EnemyActionProfileDefinition action, EnemyAttackProfileDefinition attack, float distance)
        {
            var min = action.MinRangeMeters;
            var ideal = action.IdealRangeMeters;
            var max = Mathf.Max(action.MaxRangeMeters, attack != null ? attack.RangeMeters : 0f);
            if (distance < min)
            {
                return -Mathf.Clamp((min - distance) * 1.5f, 0f, 3f);
            }

            if (distance > max)
            {
                return -Mathf.Clamp((distance - max) * 0.95f, 0f, 4f);
            }

            var fit = 1f - Mathf.Clamp01(Mathf.Abs(distance - ideal) / Mathf.Max(0.1f, max - min));
            return 1.25f + fit * 1.4f;
        }

        private static float FacingScore(EnemyRuntimeController enemy, EnemyActionProfileDefinition action)
        {
            if (!action.RequiresFacing)
            {
                return 0.45f;
            }

            var toPlayer = enemy.DirectionToPlayer;
            if (toPlayer.sqrMagnitude <= 0.01f)
            {
                return 0.25f;
            }

            var angle = Vector3.Angle(enemy.FacingDirection, toPlayer.normalized);
            if (angle <= action.FacingArcDegrees * 0.5f)
            {
                return 0.8f;
            }

            return -Mathf.Clamp(angle / 120f, 0f, 1.25f);
        }

        private static float AwarenessScore(EnemyRuntimeController enemy, EnemyActionProfileDefinition action)
        {
            return enemy.AwarenessState switch
            {
                EnemyAwarenessState.Engaged => 0.8f,
                EnemyAwarenessState.Alerted => action.Intent == EnemyActionIntent.Feint ? 0.55f : 0.25f,
                EnemyAwarenessState.Suspicious => -0.45f,
                _ => -1f
            };
        }

        private static float DispositionScore(EnemyRuntimeController enemy, EnemyActionProfileDefinition action)
        {
            return enemy.Disposition switch
            {
                EnemyInstinctDisposition.Predator => action.Intent is EnemyActionIntent.Damage or EnemyActionIntent.Pressure ? 0.45f : 0f,
                EnemyInstinctDisposition.Territorial => action.Intent is EnemyActionIntent.Feint or EnemyActionIntent.Defend ? 0.35f : 0.1f,
                EnemyInstinctDisposition.Sentinel => action.Intent is EnemyActionIntent.Damage or EnemyActionIntent.Defend ? 0.25f : 0f,
                EnemyInstinctDisposition.Prey => enemy.IsEndangeredNow ? 0.15f : -0.45f,
                EnemyInstinctDisposition.Mindless => action.Intent == EnemyActionIntent.Damage ? 0.22f : -0.1f,
                _ => 0f
            };
        }

        private static float ThreatScore(EnemyAttackProfileDefinition attack)
        {
            if (attack == null)
            {
                return 0f;
            }

            return attack.ForceClass switch
            {
                Hollow.Data.Definitions.ImpactForceClass.Massive => 0.75f,
                Hollow.Data.Definitions.ImpactForceClass.Heavy => 0.55f,
                Hollow.Data.Definitions.ImpactForceClass.Medium => 0.28f,
                _ => 0.08f
            };
        }

        private static float DeterministicVariation(int spawnIndex, string actionId, float timeSeconds)
        {
            unchecked
            {
                var hash = 23;
                var value = actionId ?? string.Empty;
                for (var index = 0; index < value.Length; index++)
                {
                    hash = hash * 31 + value[index];
                }

                hash = hash * 31 + (spawnIndex + 11) * 97;
                hash = hash * 31 + Mathf.FloorToInt(timeSeconds * 1.35f) * 53;
                return (Mathf.Abs(hash % 1000) / 1000f - 0.5f) * 0.24f;
            }
        }
    }
}
