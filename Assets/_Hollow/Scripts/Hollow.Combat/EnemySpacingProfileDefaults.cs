using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public static class EnemySpacingProfileDefaults
    {
        private static readonly Dictionary<string, EnemySpacingProfileDefinition> EnemyCache = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, EnemySpacingProfileDefinition> BossCache = new(StringComparer.Ordinal);

        public static EnemySpacingProfileDefinition CreateEnemyProfile(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return CreateEnemyProfile(
                    "spawnEnemyNormal",
                    "Normal Chaser",
                    EnemyBehaviorId.Chaser,
                    EnemyMovementMode.Grounded,
                    1.05f,
                    1.75f);
            }

            return CreateEnemyProfile(
                definition.SpawnKind,
                definition.DisplayName,
                definition.BehaviorId,
                definition.MovementMode,
                definition.PreferredRangeMinMeters,
                definition.PreferredRangeMaxMeters);
        }

        public static EnemySpacingProfileDefinition CreateEnemyProfile(
            string spawnKind,
            string displayName,
            EnemyBehaviorId behaviorId,
            EnemyMovementMode movementMode,
            float preferredRangeMinMeters,
            float preferredRangeMaxMeters)
        {
            spawnKind = string.IsNullOrWhiteSpace(spawnKind) ? "spawnEnemyNormal" : spawnKind;
            if (EnemyCache.TryGetValue(spawnKind, out var cached) && cached != null)
            {
                return cached;
            }

            var preferredMin = Mathf.Max(0f, preferredRangeMinMeters);
            var preferredMax = Mathf.Max(preferredMin + 0.05f, preferredRangeMaxMeters);
            var defaultIdeal = Mathf.Lerp(preferredMin, preferredMax, 0.48f);
            var fallbackMode = FallbackRecoveryFor(behaviorId, movementMode);
            var profile = ScriptableObject.CreateInstance<EnemySpacingProfileDefinition>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            profile.Configure(
                $"{spawnKind}_m91_spacing",
                spawnKind,
                $"{displayName} M91 Spacing",
                defaultIdeal,
                CloseToleranceFor(behaviorId),
                LongToleranceFor(behaviorId),
                ClosePressureBiasFor(behaviorId),
                RetreatBurstFor(behaviorId),
                ReassessFor(behaviorId),
                MaxResetCountFor(behaviorId),
                fallbackMode,
                RecoveryDistanceFor(fallbackMode, behaviorId),
                RecoverySpeedFor(fallbackMode, behaviorId),
                BuildOverrides(spawnKind, behaviorId, movementMode));
            EnemyCache[spawnKind] = profile;
            return profile;
        }

        public static EnemySpacingProfileDefinition CreateBossMetadataProfile(BossDefinition definition)
        {
            if (definition == null)
            {
                return CreateBossMetadataProfile("stone_warden", "Stone Warden");
            }

            return CreateBossMetadataProfile(definition.BossId, definition.DisplayName);
        }

        public static EnemySpacingProfileDefinition CreateBossMetadataProfile(string bossId, string displayName)
        {
            bossId = string.IsNullOrWhiteSpace(bossId) ? "stone_warden" : bossId;
            if (BossCache.TryGetValue(bossId, out var cached) && cached != null)
            {
                return cached;
            }

            var profile = ScriptableObject.CreateInstance<EnemySpacingProfileDefinition>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            profile.Configure(
                $"{bossId}_m91_spacing_metadata",
                bossId,
                $"{displayName} M91 Spacing Metadata",
                3.5f,
                0.25f,
                0.45f,
                0.2f,
                0.45f,
                0.4f,
                0,
                EnemySpacingRecoveryMode.Planted,
                0f,
                0.4f,
                BuildBossOverrides(bossId));
            BossCache[bossId] = profile;
            return profile;
        }

        private static IEnumerable<EnemyActionSpacingOverride> BuildOverrides(string spawnKind, EnemyBehaviorId behaviorId, EnemyMovementMode movementMode)
        {
            return EnemyActionProfileDefaults.AllEnemySpecs
                .Where(spec => spec.UsageState == EnemyActionUsageState.CurrentRuntime)
                .Where(spec => string.Equals(spec.OwnerId, spawnKind, StringComparison.Ordinal))
                .Select(spec => CreateOverride(spec, behaviorId, movementMode))
                .ToArray();
        }

        private static IEnumerable<EnemyActionSpacingOverride> BuildBossOverrides(string bossId)
        {
            return EnemyActionProfileDefaults.AllBossSpecs
                .Where(spec => spec.UsageState == EnemyActionUsageState.CurrentRuntime)
                .Where(spec => string.Equals(spec.OwnerId, bossId, StringComparison.Ordinal))
                .Select(spec => CreateOverride(spec, EnemyBehaviorId.BossWarden, EnemyMovementMode.Grounded))
                .ToArray();
        }

        private static EnemyActionSpacingOverride CreateOverride(EnemyActionProfileSpec spec, EnemyBehaviorId behaviorId, EnemyMovementMode movementMode)
        {
            var mode = RecoveryModeFor(spec, behaviorId, movementMode);
            var min = Mathf.Max(0f, spec.MinRangeMeters);
            var max = Mathf.Max(min + 0.05f, spec.MaxRangeMeters);
            var ideal = Mathf.Clamp(spec.IdealRangeMeters > 0f ? spec.IdealRangeMeters : Mathf.Lerp(min, max, 0.5f), min, max);
            var spacingOverride = new EnemyActionSpacingOverride();
            spacingOverride.Configure(
                spec.ActionId,
                ideal,
                min,
                max,
                CloseToleranceFor(behaviorId, spec.Category),
                LongToleranceFor(behaviorId, spec.Category),
                mode,
                RecoveryDistanceFor(mode, behaviorId),
                RecoverySpeedFor(mode, behaviorId),
                MaxResetCountFor(behaviorId, spec.Category));
            return spacingOverride;
        }

        private static EnemySpacingRecoveryMode RecoveryModeFor(EnemyActionProfileSpec spec, EnemyBehaviorId behaviorId, EnemyMovementMode movementMode)
        {
            if (behaviorId is EnemyBehaviorId.Giant or EnemyBehaviorId.TurretShooter or EnemyBehaviorId.SpittingPod or EnemyBehaviorId.RepeaterTurret or EnemyBehaviorId.GraveLantern)
            {
                return EnemySpacingRecoveryMode.Planted;
            }

            if (spec.Category is EnemyActionCategory.Defense)
            {
                return EnemySpacingRecoveryMode.Planted;
            }

            if (spec.Category is EnemyActionCategory.Ranged or EnemyActionCategory.Projectile or EnemyActionCategory.Magic or EnemyActionCategory.Hazard)
            {
                return behaviorId is EnemyBehaviorId.Wraith or EnemyBehaviorId.CurseBinder
                    ? EnemySpacingRecoveryMode.PhaseDrift
                    : EnemySpacingRecoveryMode.RangedReset;
            }

            if (spec.Category is EnemyActionCategory.Movement or EnemyActionCategory.GhostSoul)
            {
                return behaviorId is EnemyBehaviorId.Wraith or EnemyBehaviorId.SoulEater or EnemyBehaviorId.CurseBinder
                    ? EnemySpacingRecoveryMode.PhaseDrift
                    : EnemySpacingRecoveryMode.ShortBackstep;
            }

            if (IsWeaponUser(behaviorId))
            {
                return EnemySpacingRecoveryMode.MinimalDrift;
            }

            if (IsCreature(behaviorId) || movementMode == EnemyMovementMode.Flying)
            {
                return EnemySpacingRecoveryMode.Recoil;
            }

            return behaviorId == EnemyBehaviorId.Charger
                ? EnemySpacingRecoveryMode.ShortBackstep
                : EnemySpacingRecoveryMode.Recoil;
        }

        private static EnemySpacingRecoveryMode FallbackRecoveryFor(EnemyBehaviorId behaviorId, EnemyMovementMode movementMode)
        {
            if (behaviorId is EnemyBehaviorId.Giant or EnemyBehaviorId.TurretShooter or EnemyBehaviorId.SpittingPod or EnemyBehaviorId.RepeaterTurret or EnemyBehaviorId.GraveLantern)
            {
                return EnemySpacingRecoveryMode.Planted;
            }

            if (behaviorId is EnemyBehaviorId.Wraith or EnemyBehaviorId.CurseBinder)
            {
                return EnemySpacingRecoveryMode.PhaseDrift;
            }

            if (behaviorId is EnemyBehaviorId.HollowArcher or EnemyBehaviorId.PowderGunner or EnemyBehaviorId.KnifeThrower or EnemyBehaviorId.ClockworkSentry or EnemyBehaviorId.HollowAcolyte)
            {
                return EnemySpacingRecoveryMode.RangedReset;
            }

            if (IsWeaponUser(behaviorId))
            {
                return EnemySpacingRecoveryMode.MinimalDrift;
            }

            return IsCreature(behaviorId) || movementMode == EnemyMovementMode.Flying
                ? EnemySpacingRecoveryMode.Recoil
                : EnemySpacingRecoveryMode.ShortBackstep;
        }

        private static float CloseToleranceFor(EnemyBehaviorId behaviorId, EnemyActionCategory category = EnemyActionCategory.Body)
        {
            if (category is EnemyActionCategory.Ranged or EnemyActionCategory.Projectile or EnemyActionCategory.Magic or EnemyActionCategory.Hazard)
            {
                return IsStationaryRanged(behaviorId) ? 0.35f : 0.28f;
            }

            if (behaviorId is EnemyBehaviorId.Giant)
            {
                return 0.3f;
            }

            if (IsCreature(behaviorId))
            {
                return 0.18f;
            }

            return IsWeaponUser(behaviorId) ? 0.22f : 0.2f;
        }

        private static float LongToleranceFor(EnemyBehaviorId behaviorId, EnemyActionCategory category = EnemyActionCategory.Body)
        {
            if (category is EnemyActionCategory.Ranged or EnemyActionCategory.Projectile or EnemyActionCategory.Magic or EnemyActionCategory.Hazard)
            {
                return IsStationaryRanged(behaviorId) ? 0.45f : 0.38f;
            }

            if (behaviorId is EnemyBehaviorId.SkeletonSpear or EnemyBehaviorId.Giant)
            {
                return 0.32f;
            }

            return IsCreature(behaviorId) ? 0.24f : 0.28f;
        }

        private static float ClosePressureBiasFor(EnemyBehaviorId behaviorId)
        {
            if (IsStationaryRanged(behaviorId))
            {
                return 0f;
            }

            if (behaviorId is EnemyBehaviorId.Rat or EnemyBehaviorId.Spider or EnemyBehaviorId.FlyingChaser)
            {
                return 0.22f;
            }

            if (behaviorId is EnemyBehaviorId.Giant)
            {
                return 0.18f;
            }

            if (behaviorId is EnemyBehaviorId.PowderGunner or EnemyBehaviorId.HollowAcolyte or EnemyBehaviorId.CurseBinder)
            {
                return 0.12f;
            }

            return 0.35f;
        }

        private static float RetreatBurstFor(EnemyBehaviorId behaviorId)
        {
            if (behaviorId is EnemyBehaviorId.Rat or EnemyBehaviorId.Spider)
            {
                return 0.42f;
            }

            if (behaviorId is EnemyBehaviorId.Wraith or EnemyBehaviorId.CurseBinder)
            {
                return 0.5f;
            }

            return IsWeaponUser(behaviorId) ? 0.35f : 0.55f;
        }

        private static float ReassessFor(EnemyBehaviorId behaviorId)
        {
            if (IsCreature(behaviorId))
            {
                return 0.35f;
            }

            if (IsStationaryRanged(behaviorId))
            {
                return 0.2f;
            }

            return 0.45f;
        }

        private static int MaxResetCountFor(EnemyBehaviorId behaviorId, EnemyActionCategory category = EnemyActionCategory.Body)
        {
            if (IsStationaryRanged(behaviorId) || behaviorId == EnemyBehaviorId.Giant)
            {
                return 0;
            }

            if (category is EnemyActionCategory.Ranged or EnemyActionCategory.Projectile or EnemyActionCategory.Magic or EnemyActionCategory.Hazard or EnemyActionCategory.Movement or EnemyActionCategory.GhostSoul)
            {
                return 1;
            }

            return IsWeaponUser(behaviorId) ? 0 : 1;
        }

        private static float RecoveryDistanceFor(EnemySpacingRecoveryMode mode, EnemyBehaviorId behaviorId)
        {
            return mode switch
            {
                EnemySpacingRecoveryMode.Planted => 0f,
                EnemySpacingRecoveryMode.MinimalDrift => behaviorId == EnemyBehaviorId.Knight ? 0.06f : 0.1f,
                EnemySpacingRecoveryMode.Recoil => IsCreature(behaviorId) ? 0.28f : 0.22f,
                EnemySpacingRecoveryMode.ShortBackstep => 0.42f,
                EnemySpacingRecoveryMode.RangedReset => 0.55f,
                EnemySpacingRecoveryMode.PhaseDrift => 0.65f,
                _ => 0f
            };
        }

        private static float RecoverySpeedFor(EnemySpacingRecoveryMode mode, EnemyBehaviorId behaviorId)
        {
            return mode switch
            {
                EnemySpacingRecoveryMode.Planted => 0f,
                EnemySpacingRecoveryMode.MinimalDrift => 0.25f,
                EnemySpacingRecoveryMode.Recoil => IsCreature(behaviorId) ? 0.75f : 0.55f,
                EnemySpacingRecoveryMode.ShortBackstep => 0.7f,
                EnemySpacingRecoveryMode.RangedReset => 0.65f,
                EnemySpacingRecoveryMode.PhaseDrift => 0.85f,
                _ => 0f
            };
        }

        private static bool IsCreature(EnemyBehaviorId behaviorId)
        {
            return behaviorId is EnemyBehaviorId.Rat
                or EnemyBehaviorId.Spider
                or EnemyBehaviorId.HollowBird
                or EnemyBehaviorId.HollowBeast
                or EnemyBehaviorId.FlyingChaser
                or EnemyBehaviorId.Chaser
                or EnemyBehaviorId.Charger
                or EnemyBehaviorId.Splitter;
        }

        private static bool IsWeaponUser(EnemyBehaviorId behaviorId)
        {
            return behaviorId is EnemyBehaviorId.SkeletonSword
                or EnemyBehaviorId.SkeletonSpear
                or EnemyBehaviorId.Knight
                or EnemyBehaviorId.Giant;
        }

        private static bool IsStationaryRanged(EnemyBehaviorId behaviorId)
        {
            return behaviorId is EnemyBehaviorId.TurretShooter
                or EnemyBehaviorId.SpittingPod
                or EnemyBehaviorId.RepeaterTurret
                or EnemyBehaviorId.GraveLantern;
        }
    }
}
