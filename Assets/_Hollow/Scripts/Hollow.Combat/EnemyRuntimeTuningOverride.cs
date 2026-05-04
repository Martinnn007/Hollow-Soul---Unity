using System;
using UnityEngine;

namespace Hollow.Combat
{
    [Serializable]
    public struct EnemyRuntimeTuningOverride
    {
        public bool overrideMaxHealth;
        public int maxHealth;
        public bool overrideSpeed;
        public float speedMetersPerSecond;
        public bool overrideRadius;
        public float radiusMeters;
        public bool overrideSenses;
        public float sightRadiusMeters;
        public float sightAngleDegrees;
        public float hearingRadiusMeters;
        public bool overrideIntelligenceDisposition;
        public EnemyIntelligenceLevel intelligence;
        public EnemyInstinctDisposition disposition;
        public bool overrideAttackExecution;
        public float attackWindupScale;
        public float attackActiveScale;
        public float attackRecoveryScale;
        public float hitArcDegreesBonus;
        public int poiseBreakThresholdOffset;

        public static EnemyRuntimeTuningOverride FromDefinition(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return default;
            }

            return new EnemyRuntimeTuningOverride
            {
                overrideMaxHealth = true,
                maxHealth = definition.MaxHealth,
                overrideSpeed = true,
                speedMetersPerSecond = definition.SpeedMetersPerSecond,
                overrideRadius = true,
                radiusMeters = definition.RadiusMeters,
                overrideSenses = true,
                sightRadiusMeters = definition.SightRadiusMeters,
                sightAngleDegrees = definition.SightAngleDegrees,
                hearingRadiusMeters = definition.HearingRadiusMeters,
                overrideIntelligenceDisposition = true,
                intelligence = definition.Intelligence,
                disposition = definition.Disposition,
                overrideAttackExecution = true,
                attackWindupScale = definition.AttackWindupScale,
                attackActiveScale = definition.AttackActiveScale,
                attackRecoveryScale = definition.AttackRecoveryScale,
                hitArcDegreesBonus = definition.HitArcDegreesBonus,
                poiseBreakThresholdOffset = definition.PoiseBreakThresholdOffset
            };
        }

        public readonly bool HasAnyOverride =>
            overrideMaxHealth ||
            overrideSpeed ||
            overrideRadius ||
            overrideSenses ||
            overrideIntelligenceDisposition ||
            overrideAttackExecution;

        public readonly EnemyRuntimeTuningOverride Sanitized()
        {
            var sanitized = this;
            sanitized.maxHealth = Mathf.Max(1, maxHealth);
            sanitized.speedMetersPerSecond = Mathf.Max(0f, speedMetersPerSecond);
            sanitized.radiusMeters = Mathf.Max(0.01f, radiusMeters);
            sanitized.sightRadiusMeters = Mathf.Max(0f, sightRadiusMeters);
            sanitized.sightAngleDegrees = sanitized.sightRadiusMeters <= 0f ? 0f : Mathf.Clamp(sightAngleDegrees, 0f, 360f);
            sanitized.hearingRadiusMeters = Mathf.Max(0f, hearingRadiusMeters);
            sanitized.intelligence = EnemyIntelligenceLevelExtensions.Clamp((int)intelligence);
            sanitized.disposition = EnemyInstinctDispositionExtensions.Clamp((int)disposition);
            sanitized.attackWindupScale = Mathf.Clamp(attackWindupScale <= 0f ? 1f : attackWindupScale, 0.35f, 2.5f);
            sanitized.attackActiveScale = Mathf.Clamp(attackActiveScale <= 0f ? 1f : attackActiveScale, 0.35f, 2.5f);
            sanitized.attackRecoveryScale = Mathf.Clamp(attackRecoveryScale <= 0f ? 1f : attackRecoveryScale, 0.35f, 2.5f);
            sanitized.hitArcDegreesBonus = Mathf.Clamp(hitArcDegreesBonus, -90f, 120f);
            sanitized.poiseBreakThresholdOffset = Mathf.Clamp(poiseBreakThresholdOffset, -3, 3);
            return sanitized;
        }
    }
}
