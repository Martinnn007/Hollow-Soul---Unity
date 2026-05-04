using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public enum EnemyAiBrainTemplateRole
    {
        BodyPressure = 0,
        PreySkirmisher = 1,
        TerritorialCritter = 2,
        StationarySentinel = 3,
        WeaponUser = 4,
        RangedKiter = 5,
        MagicCaster = 6,
        HeavyBruiser = 7,
        SwarmBackground = 8,
        BossMetadata = 9
    }

    [CreateAssetMenu(menuName = "Hollow/Combat/Enemy AI Brain Template", fileName = "EnemyAiBrainTemplate")]
    public sealed class EnemyAiBrainTemplateDefinition : ScriptableObject
    {
        [SerializeField] private string templateId = "enemy_ai_brain_template";
        [SerializeField] private string displayName = "Enemy AI Brain Template";
        [TextArea(2, 6)]
        [SerializeField] private string description = string.Empty;
        [SerializeField] private EnemyAiBrainTemplateRole role = EnemyAiBrainTemplateRole.BodyPressure;
        [SerializeField] private List<EnemyBehaviorId> recommendedBehaviors = new();
        [SerializeField] private List<EnemyInstinctDisposition> recommendedDispositions = new();
        [SerializeField] private EnemyIntelligenceLevel targetIntelligence = EnemyIntelligenceLevel.Basic;
        [SerializeField] private EnemyInstinctDisposition targetDisposition = EnemyInstinctDisposition.Predator;
        [SerializeField] private bool bossMetadataOnly;
        [Header("Identity Tuning")]
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private float radiusMultiplier = 1f;
        [SerializeField] private float sightRadiusMultiplier = 1f;
        [SerializeField] private float sightAngleDegrees = -1f;
        [SerializeField] private float hearingRadiusMultiplier = 1f;
        [SerializeField] private float hearingSensitivityMultiplier = 1f;
        [SerializeField] private float disturbanceEscalationThreshold = 1f;
        [SerializeField] private float investigationDurationSeconds = 1.2f;
        [Header("Alert Sharing")]
        [SerializeField] private bool allyAlertSharingEnabled;
        [SerializeField] private float allyAlertRadiusMeters = 3.5f;
        [SerializeField] private float allyAlertCooldownSeconds = 2.5f;
        [SerializeField] private EnemyAwarenessState allyAlertMinimumAwareness = EnemyAwarenessState.Engaged;
        [Header("Commitment")]
        [SerializeField] private float attackWindupScale = 1f;
        [SerializeField] private float attackActiveScale = 1f;
        [SerializeField] private float attackRecoveryScale = 1f;
        [SerializeField] private float hitArcDegreesBonus;
        [SerializeField] private int poiseBreakThresholdOffset;
        [Header("Spacing Guidance")]
        [SerializeField] private float idealDistanceMultiplier = 1f;
        [SerializeField] private float closeToleranceMeters = 0.2f;
        [SerializeField] private float longToleranceMeters = 0.35f;
        [SerializeField] private float closePressureBias = 0.35f;
        [SerializeField] private int maxResetCountBeforeCommit = 1;
        [TextArea(2, 6)]
        [SerializeField] private string designerNotes = string.Empty;

        public string TemplateId => string.IsNullOrWhiteSpace(templateId) ? "enemy_ai_brain_template" : templateId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TemplateId : displayName;
        public string Description => description ?? string.Empty;
        public EnemyAiBrainTemplateRole Role => role;
        public IReadOnlyList<EnemyBehaviorId> RecommendedBehaviors => recommendedBehaviors?
            .Distinct()
            .ToArray() ?? Array.Empty<EnemyBehaviorId>();
        public IReadOnlyList<EnemyInstinctDisposition> RecommendedDispositions => recommendedDispositions?
            .Select(disposition => EnemyInstinctDispositionExtensions.Clamp((int)disposition))
            .Distinct()
            .ToArray() ?? Array.Empty<EnemyInstinctDisposition>();
        public EnemyIntelligenceLevel TargetIntelligence => EnemyIntelligenceLevelExtensions.Clamp((int)targetIntelligence);
        public EnemyInstinctDisposition TargetDisposition => EnemyInstinctDispositionExtensions.Clamp((int)targetDisposition);
        public bool BossMetadataOnly => bossMetadataOnly;
        public float SpeedMultiplier => Mathf.Clamp(speedMultiplier <= 0f ? 1f : speedMultiplier, 0.1f, 4f);
        public float RadiusMultiplier => Mathf.Clamp(radiusMultiplier <= 0f ? 1f : radiusMultiplier, 0.1f, 4f);
        public float SightRadiusMultiplier => Mathf.Clamp(sightRadiusMultiplier < 0f ? 1f : sightRadiusMultiplier, 0f, 4f);
        public float SightAngleDegrees => sightAngleDegrees < 0f ? -1f : Mathf.Clamp(sightAngleDegrees, 0f, 360f);
        public float HearingRadiusMultiplier => Mathf.Clamp(hearingRadiusMultiplier < 0f ? 1f : hearingRadiusMultiplier, 0f, 4f);
        public float HearingSensitivityMultiplier => Mathf.Clamp(hearingSensitivityMultiplier <= 0f ? 1f : hearingSensitivityMultiplier, 0.1f, 3f);
        public float DisturbanceEscalationThreshold => Mathf.Clamp(disturbanceEscalationThreshold <= 0f ? 1f : disturbanceEscalationThreshold, 0.05f, 8f);
        public float InvestigationDurationSeconds => Mathf.Clamp(investigationDurationSeconds <= 0f ? 1f : investigationDurationSeconds, 0.05f, 8f);
        public bool AllyAlertSharingEnabled => allyAlertSharingEnabled;
        public float AllyAlertRadiusMeters => Mathf.Clamp(allyAlertRadiusMeters, 0f, 12f);
        public float AllyAlertCooldownSeconds => Mathf.Clamp(allyAlertCooldownSeconds <= 0f ? 2f : allyAlertCooldownSeconds, 0.25f, 12f);
        public EnemyAwarenessState AllyAlertMinimumAwareness => allyAlertMinimumAwareness;
        public float AttackWindupScale => Mathf.Clamp(attackWindupScale <= 0f ? 1f : attackWindupScale, 0.35f, 2.5f);
        public float AttackActiveScale => Mathf.Clamp(attackActiveScale <= 0f ? 1f : attackActiveScale, 0.35f, 2.5f);
        public float AttackRecoveryScale => Mathf.Clamp(attackRecoveryScale <= 0f ? 1f : attackRecoveryScale, 0.35f, 2.5f);
        public float HitArcDegreesBonus => Mathf.Clamp(hitArcDegreesBonus, -90f, 120f);
        public int PoiseBreakThresholdOffset => Mathf.Clamp(poiseBreakThresholdOffset, -3, 3);
        public float IdealDistanceMultiplier => Mathf.Clamp(idealDistanceMultiplier <= 0f ? 1f : idealDistanceMultiplier, 0.2f, 4f);
        public float CloseToleranceMeters => Mathf.Max(0f, closeToleranceMeters);
        public float LongToleranceMeters => Mathf.Max(0f, longToleranceMeters);
        public float ClosePressureBias => Mathf.Clamp01(closePressureBias);
        public int MaxResetCountBeforeCommit => Mathf.Max(0, maxResetCountBeforeCommit);
        public string DesignerNotes => designerNotes ?? string.Empty;

        public void Configure(
            string nextTemplateId,
            string nextDisplayName,
            string nextDescription,
            EnemyAiBrainTemplateRole nextRole,
            IEnumerable<EnemyBehaviorId> nextRecommendedBehaviors,
            IEnumerable<EnemyInstinctDisposition> nextRecommendedDispositions,
            EnemyIntelligenceLevel nextTargetIntelligence,
            EnemyInstinctDisposition nextTargetDisposition,
            bool nextBossMetadataOnly,
            float nextSpeedMultiplier,
            float nextRadiusMultiplier,
            float nextSightRadiusMultiplier,
            float nextSightAngleDegrees,
            float nextHearingRadiusMultiplier,
            float nextHearingSensitivityMultiplier,
            float nextDisturbanceEscalationThreshold,
            float nextInvestigationDurationSeconds,
            bool nextAllyAlertSharingEnabled,
            float nextAllyAlertRadiusMeters,
            float nextAllyAlertCooldownSeconds,
            EnemyAwarenessState nextAllyAlertMinimumAwareness,
            float nextAttackWindupScale,
            float nextAttackActiveScale,
            float nextAttackRecoveryScale,
            float nextHitArcDegreesBonus,
            int nextPoiseBreakThresholdOffset,
            float nextIdealDistanceMultiplier,
            float nextCloseToleranceMeters,
            float nextLongToleranceMeters,
            float nextClosePressureBias,
            int nextMaxResetCountBeforeCommit,
            string nextDesignerNotes)
        {
            templateId = string.IsNullOrWhiteSpace(nextTemplateId) ? "enemy_ai_brain_template" : nextTemplateId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? templateId : nextDisplayName;
            description = nextDescription ?? string.Empty;
            role = nextRole;
            recommendedBehaviors = nextRecommendedBehaviors?.Distinct().ToList() ?? new List<EnemyBehaviorId>();
            recommendedDispositions = nextRecommendedDispositions?
                .Select(disposition => EnemyInstinctDispositionExtensions.Clamp((int)disposition))
                .Distinct()
                .ToList() ?? new List<EnemyInstinctDisposition>();
            targetIntelligence = EnemyIntelligenceLevelExtensions.Clamp((int)nextTargetIntelligence);
            targetDisposition = EnemyInstinctDispositionExtensions.Clamp((int)nextTargetDisposition);
            bossMetadataOnly = nextBossMetadataOnly;
            speedMultiplier = Mathf.Clamp(nextSpeedMultiplier <= 0f ? 1f : nextSpeedMultiplier, 0.1f, 4f);
            radiusMultiplier = Mathf.Clamp(nextRadiusMultiplier <= 0f ? 1f : nextRadiusMultiplier, 0.1f, 4f);
            sightRadiusMultiplier = Mathf.Clamp(nextSightRadiusMultiplier < 0f ? 1f : nextSightRadiusMultiplier, 0f, 4f);
            sightAngleDegrees = nextSightAngleDegrees < 0f ? -1f : Mathf.Clamp(nextSightAngleDegrees, 0f, 360f);
            hearingRadiusMultiplier = Mathf.Clamp(nextHearingRadiusMultiplier < 0f ? 1f : nextHearingRadiusMultiplier, 0f, 4f);
            hearingSensitivityMultiplier = Mathf.Clamp(nextHearingSensitivityMultiplier <= 0f ? 1f : nextHearingSensitivityMultiplier, 0.1f, 3f);
            disturbanceEscalationThreshold = Mathf.Clamp(nextDisturbanceEscalationThreshold <= 0f ? 1f : nextDisturbanceEscalationThreshold, 0.05f, 8f);
            investigationDurationSeconds = Mathf.Clamp(nextInvestigationDurationSeconds <= 0f ? 1f : nextInvestigationDurationSeconds, 0.05f, 8f);
            allyAlertSharingEnabled = nextAllyAlertSharingEnabled;
            allyAlertRadiusMeters = Mathf.Clamp(nextAllyAlertRadiusMeters, 0f, 12f);
            allyAlertCooldownSeconds = Mathf.Clamp(nextAllyAlertCooldownSeconds <= 0f ? 2f : nextAllyAlertCooldownSeconds, 0.25f, 12f);
            allyAlertMinimumAwareness = nextAllyAlertMinimumAwareness;
            attackWindupScale = Mathf.Clamp(nextAttackWindupScale <= 0f ? 1f : nextAttackWindupScale, 0.35f, 2.5f);
            attackActiveScale = Mathf.Clamp(nextAttackActiveScale <= 0f ? 1f : nextAttackActiveScale, 0.35f, 2.5f);
            attackRecoveryScale = Mathf.Clamp(nextAttackRecoveryScale <= 0f ? 1f : nextAttackRecoveryScale, 0.35f, 2.5f);
            hitArcDegreesBonus = Mathf.Clamp(nextHitArcDegreesBonus, -90f, 120f);
            poiseBreakThresholdOffset = Mathf.Clamp(nextPoiseBreakThresholdOffset, -3, 3);
            idealDistanceMultiplier = Mathf.Clamp(nextIdealDistanceMultiplier <= 0f ? 1f : nextIdealDistanceMultiplier, 0.2f, 4f);
            closeToleranceMeters = Mathf.Max(0f, nextCloseToleranceMeters);
            longToleranceMeters = Mathf.Max(0f, nextLongToleranceMeters);
            closePressureBias = Mathf.Clamp01(nextClosePressureBias);
            maxResetCountBeforeCommit = Mathf.Max(0, nextMaxResetCountBeforeCommit);
            designerNotes = nextDesignerNotes ?? string.Empty;
        }
    }
}
