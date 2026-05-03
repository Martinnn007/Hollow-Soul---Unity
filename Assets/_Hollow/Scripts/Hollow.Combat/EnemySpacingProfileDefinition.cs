using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Combat
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Enemy Spacing Profile", fileName = "EnemySpacingProfile")]
    public sealed class EnemySpacingProfileDefinition : ScriptableObject
    {
        [SerializeField] private string spacingProfileId = string.Empty;
        [SerializeField] private string ownerSpawnKind = string.Empty;
        [SerializeField] private string displayName = "Enemy Spacing";
        [SerializeField] private float defaultIdealDistanceMeters = 1.5f;
        [SerializeField] private float defaultCloseToleranceMeters = 0.2f;
        [SerializeField] private float defaultLongToleranceMeters = 0.3f;
        [SerializeField] private float closePressureBias = 0.35f;
        [SerializeField] private float retreatBurstSeconds = 0.55f;
        [SerializeField] private float retreatReassessSeconds = 0.45f;
        [SerializeField] private int maxResetCountBeforeCommit = 1;
        [SerializeField] private EnemySpacingRecoveryMode fallbackRecoveryMovementMode = EnemySpacingRecoveryMode.Planted;
        [SerializeField] private float fallbackRecoveryDistanceMeters = 0f;
        [SerializeField] private float fallbackRecoverySpeedMultiplier = 0.6f;
        [SerializeField] private List<EnemyActionSpacingOverride> actionOverrides = new();

        public string SpacingProfileId => spacingProfileId ?? string.Empty;
        public string OwnerSpawnKind => ownerSpawnKind ?? string.Empty;
        public string DisplayName => displayName ?? "Enemy Spacing";
        public float DefaultIdealDistanceMeters => Mathf.Max(0.05f, defaultIdealDistanceMeters);
        public float DefaultCloseToleranceMeters => Mathf.Max(0f, defaultCloseToleranceMeters);
        public float DefaultLongToleranceMeters => Mathf.Max(0f, defaultLongToleranceMeters);
        public float ClosePressureBias => Mathf.Clamp01(closePressureBias);
        public float RetreatBurstSeconds => Mathf.Max(0.05f, retreatBurstSeconds);
        public float RetreatReassessSeconds => Mathf.Max(0f, retreatReassessSeconds);
        public int MaxResetCountBeforeCommit => Mathf.Max(0, maxResetCountBeforeCommit);
        public EnemySpacingRecoveryMode FallbackRecoveryMovementMode => fallbackRecoveryMovementMode;
        public float FallbackRecoveryDistanceMeters => Mathf.Max(0f, fallbackRecoveryDistanceMeters);
        public float FallbackRecoverySpeedMultiplier => Mathf.Max(0f, fallbackRecoverySpeedMultiplier);
        public IReadOnlyList<EnemyActionSpacingOverride> ActionOverrides => actionOverrides != null
            ? actionOverrides
            : Array.Empty<EnemyActionSpacingOverride>();

        public void Configure(
            string nextSpacingProfileId,
            string nextOwnerSpawnKind,
            string nextDisplayName,
            float nextDefaultIdealDistanceMeters,
            float nextDefaultCloseToleranceMeters,
            float nextDefaultLongToleranceMeters,
            float nextClosePressureBias,
            float nextRetreatBurstSeconds,
            float nextRetreatReassessSeconds,
            int nextMaxResetCountBeforeCommit,
            EnemySpacingRecoveryMode nextFallbackRecoveryMovementMode,
            float nextFallbackRecoveryDistanceMeters,
            float nextFallbackRecoverySpeedMultiplier,
            IEnumerable<EnemyActionSpacingOverride> nextActionOverrides)
        {
            spacingProfileId = nextSpacingProfileId ?? string.Empty;
            ownerSpawnKind = nextOwnerSpawnKind ?? string.Empty;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? "Enemy Spacing" : nextDisplayName;
            defaultIdealDistanceMeters = Mathf.Max(0.05f, nextDefaultIdealDistanceMeters);
            defaultCloseToleranceMeters = Mathf.Max(0f, nextDefaultCloseToleranceMeters);
            defaultLongToleranceMeters = Mathf.Max(0f, nextDefaultLongToleranceMeters);
            closePressureBias = Mathf.Clamp01(nextClosePressureBias);
            retreatBurstSeconds = Mathf.Max(0.05f, nextRetreatBurstSeconds);
            retreatReassessSeconds = Mathf.Max(0f, nextRetreatReassessSeconds);
            maxResetCountBeforeCommit = Mathf.Max(0, nextMaxResetCountBeforeCommit);
            fallbackRecoveryMovementMode = nextFallbackRecoveryMovementMode;
            fallbackRecoveryDistanceMeters = Mathf.Max(0f, nextFallbackRecoveryDistanceMeters);
            fallbackRecoverySpeedMultiplier = Mathf.Max(0f, nextFallbackRecoverySpeedMultiplier);
            actionOverrides = nextActionOverrides != null
                ? new List<EnemyActionSpacingOverride>(nextActionOverrides)
                : new List<EnemyActionSpacingOverride>();
        }

        public EnemyActionSpacingOverride ResolveOverride(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId) || actionOverrides == null)
            {
                return null;
            }

            for (var i = 0; i < actionOverrides.Count; i++)
            {
                var spacingOverride = actionOverrides[i];
                if (spacingOverride == null)
                {
                    continue;
                }

                if (string.Equals(spacingOverride.ActionId, actionId, StringComparison.OrdinalIgnoreCase))
                {
                    return spacingOverride;
                }
            }

            return null;
        }

        public EnemyResolvedActionSpacing ResolveActionSpacing(EnemyActionProfileDefinition actionProfile, EnemyAttackProfileDefinition attackProfile)
        {
            var actionId = actionProfile != null && !string.IsNullOrWhiteSpace(actionProfile.ActionId)
                ? actionProfile.ActionId
                : attackProfile != null ? attackProfile.AttackId : string.Empty;
            var spacingOverride = ResolveOverride(actionId);

            var baseMax = ResolveBaseMaxRange(actionProfile, attackProfile);
            var baseMin = actionProfile != null ? Mathf.Max(0f, actionProfile.MinRangeMeters) : 0f;
            var baseIdeal = actionProfile != null && actionProfile.IdealRangeMeters > 0f
                ? actionProfile.IdealRangeMeters
                : Mathf.Clamp(DefaultIdealDistanceMeters, baseMin, Mathf.Max(baseMin, baseMax));

            if (spacingOverride == null)
            {
                return new EnemyResolvedActionSpacing(
                    actionId,
                    baseIdeal,
                    baseMin,
                    baseMax,
                    DefaultCloseToleranceMeters,
                    DefaultLongToleranceMeters,
                    ClosePressureBias,
                    FallbackRecoveryMovementMode,
                    FallbackRecoveryDistanceMeters,
                    FallbackRecoverySpeedMultiplier,
                    MaxResetCountBeforeCommit,
                    isFallback: true);
            }

            return new EnemyResolvedActionSpacing(
                actionId,
                spacingOverride.DesiredStartDistanceMeters > 0f ? spacingOverride.DesiredStartDistanceMeters : baseIdeal,
                spacingOverride.CommitRangeMinMeters,
                spacingOverride.CommitRangeMaxMeters > 0f ? spacingOverride.CommitRangeMaxMeters : baseMax,
                spacingOverride.CloseToleranceMeters > 0f ? spacingOverride.CloseToleranceMeters : DefaultCloseToleranceMeters,
                spacingOverride.LongToleranceMeters > 0f ? spacingOverride.LongToleranceMeters : DefaultLongToleranceMeters,
                ClosePressureBias,
                spacingOverride.RecoveryMovementMode,
                spacingOverride.RecoveryMovementDistanceMeters,
                spacingOverride.RecoverySpeedMultiplier > 0f ? spacingOverride.RecoverySpeedMultiplier : FallbackRecoverySpeedMultiplier,
                spacingOverride.MaxResetCountBeforeCommit,
                isFallback: false);
        }

        public EnemyResolvedActionSpacing ResolveFallbackSpacing(float preferredRangeMinMeters, float preferredRangeMaxMeters)
        {
            var min = Mathf.Max(0f, preferredRangeMinMeters);
            var max = Mathf.Max(min + 0.05f, preferredRangeMaxMeters);
            var ideal = Mathf.Clamp(DefaultIdealDistanceMeters, min, max);
            return new EnemyResolvedActionSpacing(
                string.Empty,
                ideal,
                min,
                max,
                DefaultCloseToleranceMeters,
                DefaultLongToleranceMeters,
                ClosePressureBias,
                FallbackRecoveryMovementMode,
                FallbackRecoveryDistanceMeters,
                FallbackRecoverySpeedMultiplier,
                MaxResetCountBeforeCommit,
                isFallback: true);
        }

        private float ResolveBaseMaxRange(EnemyActionProfileDefinition actionProfile, EnemyAttackProfileDefinition attackProfile)
        {
            if (actionProfile != null && actionProfile.MaxRangeMeters > 0f)
            {
                return Mathf.Max(actionProfile.MinRangeMeters, actionProfile.MaxRangeMeters);
            }

            if (attackProfile != null && attackProfile.RangeMeters > 0f)
            {
                return attackProfile.RangeMeters;
            }

            return DefaultIdealDistanceMeters + DefaultLongToleranceMeters;
        }
    }
}
