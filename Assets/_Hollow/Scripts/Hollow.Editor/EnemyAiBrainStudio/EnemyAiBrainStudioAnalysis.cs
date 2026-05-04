using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.EnemyAiBrainStudio
{
    public sealed class EnemyAiBrainStudioValidationResult
    {
        private readonly List<string> errors = new();
        private readonly List<string> warnings = new();
        private readonly List<string> notes = new();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public IReadOnlyList<string> Notes => notes;
        public bool IsValid => errors.Count == 0;

        public void Error(string message) => errors.Add(message);
        public void Warning(string message) => warnings.Add(message);
        public void Note(string message) => notes.Add(message);
    }

    public readonly struct EnemyAiBrainActionPreview
    {
        public EnemyAiBrainActionPreview(string actionId, EnemyBehaviorCommandKind commandKind, float score, string reason)
        {
            ActionId = actionId ?? string.Empty;
            CommandKind = commandKind;
            Score = score;
            Reason = reason ?? string.Empty;
        }

        public string ActionId { get; }
        public EnemyBehaviorCommandKind CommandKind { get; }
        public float Score { get; }
        public string Reason { get; }
    }

    public static class EnemyAiBrainStudioAnalysis
    {
        public static EnemyAiBrainTemplateRole SuggestRole(EnemyDefinition enemy)
        {
            if (enemy == null)
            {
                return EnemyAiBrainTemplateRole.BodyPressure;
            }

            if (enemy.MovementMode == EnemyMovementMode.Flying && enemy.Disposition == EnemyInstinctDisposition.Prey)
            {
                return EnemyAiBrainTemplateRole.PreySkirmisher;
            }

            if (enemy.Disposition == EnemyInstinctDisposition.Territorial)
            {
                return EnemyAiBrainTemplateRole.TerritorialCritter;
            }

            if (enemy.SpeedMetersPerSecond <= 0.01f ||
                enemy.BehaviorId is EnemyBehaviorId.TurretShooter or EnemyBehaviorId.SpittingPod or EnemyBehaviorId.RepeaterTurret or EnemyBehaviorId.GraveLantern)
            {
                return EnemyAiBrainTemplateRole.StationarySentinel;
            }

            if (enemy.BehaviorId is EnemyBehaviorId.SkeletonSword or EnemyBehaviorId.SkeletonSpear or EnemyBehaviorId.Knight or EnemyBehaviorId.Giant)
            {
                return enemy.BodyClass is EnemyBodyClass.Heavy or EnemyBodyClass.Massive
                    ? EnemyAiBrainTemplateRole.HeavyBruiser
                    : EnemyAiBrainTemplateRole.WeaponUser;
            }

            if (enemy.BehaviorId is EnemyBehaviorId.HollowArcher or EnemyBehaviorId.PowderGunner or EnemyBehaviorId.KnifeThrower or EnemyBehaviorId.ClockworkSentry)
            {
                return EnemyAiBrainTemplateRole.RangedKiter;
            }

            if (enemy.BehaviorId is EnemyBehaviorId.HollowAcolyte or EnemyBehaviorId.Wraith or EnemyBehaviorId.SoulEater or EnemyBehaviorId.CurseBinder)
            {
                return EnemyAiBrainTemplateRole.MagicCaster;
            }

            if (enemy.BodyClass is EnemyBodyClass.Heavy or EnemyBodyClass.Massive)
            {
                return EnemyAiBrainTemplateRole.HeavyBruiser;
            }

            return EnemyAiBrainTemplateRole.BodyPressure;
        }

        public static EnemyAiBrainStudioValidationResult ValidateEnemy(EnemyDefinition enemy)
        {
            var result = new EnemyAiBrainStudioValidationResult();
            if (enemy == null)
            {
                result.Error("No enemy selected.");
                return result;
            }

            if (enemy.BehaviorTree == null || enemy.BehaviorTree.RootNode == null)
            {
                result.Error($"{enemy.DisplayName}: behavior tree is missing or has no root.");
            }

            if (enemy.ActionProfiles.Count == 0)
            {
                result.Error($"{enemy.DisplayName}: no action profiles resolve.");
            }

            if (enemy.AttackProfiles.Count == 0)
            {
                result.Error($"{enemy.DisplayName}: no attack profiles resolve.");
            }

            if (enemy.SpacingProfile == null)
            {
                result.Error($"{enemy.DisplayName}: spacing profile is missing.");
            }

            if (enemy.SightRadiusMeters <= 0f && enemy.HearingRadiusMeters <= 0f)
            {
                result.Warning($"{enemy.DisplayName}: blind and deaf enemies need explicit disturbance/damage rules.");
            }

            if (enemy.Disposition == EnemyInstinctDisposition.Prey &&
                !enemy.ActionProfiles.Any(action => action.Intent is EnemyActionIntent.Escape or EnemyActionIntent.Reposition))
            {
                result.Warning($"{enemy.DisplayName}: prey role has no explicit escape/reposition action.");
            }

            if (enemy.Disposition == EnemyInstinctDisposition.Sentinel &&
                enemy.HearingRadiusMeters < 2f &&
                enemy.SightRadiusMeters < 2f)
            {
                result.Warning($"{enemy.DisplayName}: sentinel role has very weak senses and may not activate reliably.");
            }

            if (enemy.SpeedMetersPerSecond > 2.5f && enemy.AttackRecoveryScale < 0.65f)
            {
                result.Warning($"{enemy.DisplayName}: fast movement plus short recovery may reduce Souls-like punishability.");
            }

            if (enemy.SpacingProfile != null && enemy.SpacingProfile.MaxResetCountBeforeCommit > 1)
            {
                result.Note($"{enemy.DisplayName}: reset count is above the current preferred one-reset combat feel.");
            }

            var role = SuggestRole(enemy);
            result.Note($"{enemy.DisplayName}: suggested brain role is {role}.");
            result.Note($"Senses: sight {enemy.SightRadiusMeters:0.0}m/{enemy.SightAngleDegrees:0}deg, hearing {enemy.HearingRadiusMeters:0.0}m x{enemy.HearingSensitivityMultiplier:0.00}.");
            result.Note($"Commitment: windup x{enemy.AttackWindupScale:0.00}, active x{enemy.AttackActiveScale:0.00}, recovery x{enemy.AttackRecoveryScale:0.00}.");
            return result;
        }

        public static IReadOnlyList<EnemyAiBrainActionPreview> BuildActionPreview(
            EnemyDefinition enemy,
            float distanceMeters,
            EnemyAwarenessState awareness,
            EnemyInstinctDisposition disposition,
            EnemyIntelligenceLevel intelligence,
            float meleePressure = 0f,
            float rangedPressure = 0f,
            float areaPressure = 0f,
            float chargePressure = 0f)
        {
            if (enemy == null)
            {
                return Array.Empty<EnemyAiBrainActionPreview>();
            }

            var previews = new List<EnemyAiBrainActionPreview>();
            foreach (var action in enemy.ActionProfiles.Where(action => action != null && action.UsageState == EnemyActionUsageState.CurrentRuntime))
            {
                var attack = enemy.AttackProfiles.FirstOrDefault(profile => profile != null && profile.AttackId == action.LinkedAttackId);
                var command = EnemyActionScorer.CommandKindFor(action, attack);
                if (command == EnemyBehaviorCommandKind.None)
                {
                    continue;
                }

                var score = action.BaseWeight;
                var reasons = new List<string>();
                var rangeScore = RangeScore(action, attack, distanceMeters);
                score += rangeScore;
                reasons.Add($"range {rangeScore:0.00}");

                if ((int)intelligence < (int)action.MinimumIntelligence)
                {
                    score -= 5f;
                    reasons.Add("below intelligence");
                }
                else
                {
                    var intelligenceScore = Mathf.Max(0, (int)intelligence - (int)action.MinimumIntelligence) * 0.16f;
                    score += intelligenceScore;
                    reasons.Add($"int {intelligenceScore:0.00}");
                }

                if (awareness < action.MinimumAwareness)
                {
                    score -= 3f;
                    reasons.Add("below awareness");
                }

                if (!action.AllowedDispositions.Contains(disposition))
                {
                    score -= 4f;
                    reasons.Add("disposition blocked");
                }
                else
                {
                    var dispositionScore = DispositionScore(disposition, action.Intent);
                    score += dispositionScore;
                    reasons.Add($"role {dispositionScore:0.00}");
                }

                var pressurePenalty = PressurePenaltyFor(action, attack, meleePressure, rangedPressure, areaPressure, chargePressure);
                score -= pressurePenalty;
                reasons.Add($"pressure -{pressurePenalty:0.00}");

                previews.Add(new EnemyAiBrainActionPreview(action.ActionId, command, score, string.Join(", ", reasons)));
            }

            return previews
                .OrderByDescending(preview => preview.Score)
                .ThenBy(preview => preview.ActionId, StringComparer.Ordinal)
                .ToArray();
        }

        public static void ApplyTemplateToEnemyDraft(EnemyDefinition enemyDraft, EnemyAiBrainTemplateDefinition template)
        {
            if (enemyDraft == null || template == null)
            {
                return;
            }

            var serialized = new SerializedObject(enemyDraft);
            SetEnum(serialized, "intelligence", (int)template.TargetIntelligence);
            SetEnum(serialized, "disposition", (int)template.TargetDisposition);
            Multiply(serialized, "speedMetersPerSecond", template.SpeedMultiplier, min: 0f);
            Multiply(serialized, "radiusMeters", template.RadiusMultiplier, min: 0.01f);
            Multiply(serialized, "sightRadiusMeters", template.SightRadiusMultiplier, min: 0f);
            if (template.SightAngleDegrees >= 0f)
            {
                SetFloat(serialized, "sightAngleDegrees", template.SightAngleDegrees);
            }

            Multiply(serialized, "hearingRadiusMeters", template.HearingRadiusMultiplier, min: 0f);
            SetFloat(serialized, "hearingSensitivityMultiplier", template.HearingSensitivityMultiplier);
            SetFloat(serialized, "disturbanceEscalationThreshold", template.DisturbanceEscalationThreshold);
            SetFloat(serialized, "investigationDurationSeconds", template.InvestigationDurationSeconds);
            SetBool(serialized, "allyAlertSharingEnabled", template.AllyAlertSharingEnabled);
            SetFloat(serialized, "allyAlertRadiusMeters", template.AllyAlertRadiusMeters);
            SetFloat(serialized, "allyAlertCooldownSeconds", template.AllyAlertCooldownSeconds);
            SetEnum(serialized, "allyAlertMinimumAwareness", (int)template.AllyAlertMinimumAwareness);
            SetFloat(serialized, "attackWindupScale", template.AttackWindupScale);
            SetFloat(serialized, "attackActiveScale", template.AttackActiveScale);
            SetFloat(serialized, "attackRecoveryScale", template.AttackRecoveryScale);
            SetFloat(serialized, "hitArcDegreesBonus", template.HitArcDegreesBonus);
            SetInt(serialized, "poiseBreakThresholdOffset", template.PoiseBreakThresholdOffset);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ApplyTemplateToSpacingDraft(EnemySpacingProfileDefinition spacingDraft, EnemyAiBrainTemplateDefinition template)
        {
            if (spacingDraft == null || template == null)
            {
                return;
            }

            var serialized = new SerializedObject(spacingDraft);
            Multiply(serialized, "defaultIdealDistanceMeters", template.IdealDistanceMultiplier, min: 0.05f);
            SetFloat(serialized, "defaultCloseToleranceMeters", template.CloseToleranceMeters);
            SetFloat(serialized, "defaultLongToleranceMeters", template.LongToleranceMeters);
            SetFloat(serialized, "closePressureBias", template.ClosePressureBias);
            SetInt(serialized, "maxResetCountBeforeCommit", template.MaxResetCountBeforeCommit);
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static float DispositionScore(EnemyInstinctDisposition disposition, EnemyActionIntent intent)
        {
            return disposition switch
            {
                EnemyInstinctDisposition.Predator => intent is EnemyActionIntent.Damage or EnemyActionIntent.Pressure ? 0.45f : 0f,
                EnemyInstinctDisposition.Territorial => intent is EnemyActionIntent.Feint or EnemyActionIntent.Defend ? 0.35f : 0.1f,
                EnemyInstinctDisposition.Sentinel => intent is EnemyActionIntent.Damage or EnemyActionIntent.Defend ? 0.25f : 0f,
                EnemyInstinctDisposition.Prey => intent is EnemyActionIntent.Escape or EnemyActionIntent.Reposition ? 0.45f : -0.25f,
                EnemyInstinctDisposition.Mindless => intent == EnemyActionIntent.Damage ? 0.22f : -0.1f,
                _ => 0f
            };
        }

        private static float PressurePenaltyFor(
            EnemyActionProfileDefinition action,
            EnemyAttackProfileDefinition attack,
            float meleePressure,
            float rangedPressure,
            float areaPressure,
            float chargePressure)
        {
            var lane = RoomThreatDirector.ResolveLane(action, attack);
            var pressure = lane switch
            {
                ThreatLane.Melee => meleePressure,
                ThreatLane.Ranged => rangedPressure,
                ThreatLane.Area => areaPressure,
                ThreatLane.Charge => chargePressure,
                _ => 0f
            };
            var cap = lane switch
            {
                ThreatLane.Melee => 2.6f,
                ThreatLane.Ranged => 3.2f,
                ThreatLane.Area => 1.6f,
                ThreatLane.Charge => 1.3f,
                _ => 999f
            };
            return pressure <= cap
                ? 0f
                : (pressure - cap) * Mathf.Max(0.25f, action.PressureCost) * 0.72f;
        }

        private static void Multiply(SerializedObject serialized, string propertyName, float multiplier, float min)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = Mathf.Max(min, property.floatValue * multiplier);
            }
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetEnum(SerializedObject serialized, string propertyName, int value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }
    }
}
