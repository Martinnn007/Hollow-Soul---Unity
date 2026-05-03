using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string spawnKind = "spawnEnemyNormal";
        [SerializeField] private string displayName = "Normal Chaser";
        [SerializeField] private EnemyArchetypeId archetypeId = EnemyArchetypeId.Normal;
        [SerializeField] private EnemyBehaviorId behaviorId = EnemyBehaviorId.Chaser;
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.Grounded;
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float speedMetersPerSecond = 1.5f;
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float contactCooldownSeconds = 1f;
        [SerializeField] private EnemyContactDamagePolicy contactDamagePolicy = EnemyContactDamagePolicy.ActiveOnly;
        [SerializeField] private EnemyPassiveContactHazardType passiveContactHazardType = EnemyPassiveContactHazardType.None;
        [SerializeField] private float radiusMeters = 0.32f;
        [SerializeField] private float attackRangeMeters = 5f;
        [SerializeField] private float preferredRangeMinMeters = 1.05f;
        [SerializeField] private float preferredRangeMaxMeters = 1.75f;
        [SerializeField] private float sightRadiusMeters = 6.5f;
        [SerializeField] private float sightAngleDegrees = 150f;
        [SerializeField] private float hearingRadiusMeters = 4.5f;
        [SerializeField] private float hearingSensitivityMultiplier;
        [SerializeField] private float disturbanceEscalationThreshold;
        [SerializeField] private float investigationDurationSeconds;
        [SerializeField] private bool allyAlertSharingEnabled;
        [SerializeField] private float allyAlertRadiusMeters;
        [SerializeField] private float allyAlertCooldownSeconds;
        [SerializeField] private EnemyAwarenessState allyAlertMinimumAwareness = EnemyAwarenessState.Engaged;
        [SerializeField] private bool lungeAttackEnabled = true;
        [SerializeField] private float lungeTriggerRangeMeters = 1.4f;
        [SerializeField] private float lungeWindupSeconds = 0.22f;
        [SerializeField] private float lungeActiveSeconds = 0.18f;
        [SerializeField] private float lungeDistanceMeters = 0.75f;
        [SerializeField] private float lungeCooldownSeconds = 1.15f;
        [SerializeField] private float attackCooldownSeconds = 1.4f;
        [SerializeField] private int projectileDamage = 1;
        [SerializeField] private float projectileSpeedMetersPerSecond = 5f;
        [SerializeField] private float chargeSpeedMetersPerSecond = 5f;
        [SerializeField] private float chargeCooldownSeconds = 2.25f;
        [SerializeField] private string splitSpawnKind = "spawnEnemyNormal";
        [SerializeField] private int splitCount;
        [SerializeField] private EnemyBodyClass bodyClass = EnemyBodyClass.Medium;
        [SerializeField] private EnemyIntelligenceLevel intelligence = EnemyIntelligenceLevel.Simple;
        [SerializeField] private EnemyInstinctDisposition disposition = EnemyInstinctDisposition.Predator;
        [SerializeField] private float attackWindupScale;
        [SerializeField] private float attackActiveScale;
        [SerializeField] private float attackRecoveryScale;
        [SerializeField] private float hitArcDegreesBonus;
        [SerializeField] private int poiseBreakThresholdOffset;
        [SerializeField] private List<EnemyAttackProfileDefinition> attackProfiles = new();
        [SerializeField] private List<EnemyActionProfileDefinition> actionProfiles = new();
        [SerializeField] private EnemyGuardProfileDefinition guardProfile;
        [SerializeField] private EnemyBehaviorTreeDefinition behaviorTree;
        [SerializeField] private Color color = new(0.85f, 0.16f, 0.14f, 1f);

        public string SpawnKind => spawnKind;

        public string DisplayName => displayName;

        public EnemyArchetypeId ArchetypeId => archetypeId;

        public EnemyBehaviorId BehaviorId => behaviorId;

        public EnemyMovementMode MovementMode => movementMode;

        public int MaxHealth => maxHealth;

        public float SpeedMetersPerSecond => speedMetersPerSecond;

        public int ContactDamage => contactDamage;

        public float ContactCooldownSeconds => contactCooldownSeconds;

        public EnemyContactDamagePolicy ContactDamagePolicy => contactDamagePolicy;

        public EnemyPassiveContactHazardType PassiveContactHazardType => passiveContactHazardType;

        public float RadiusMeters => radiusMeters;

        public float AttackRangeMeters => attackRangeMeters;

        public float PreferredRangeMinMeters => Mathf.Max(0f, preferredRangeMinMeters);

        public float PreferredRangeMaxMeters => Mathf.Max(PreferredRangeMinMeters + 0.05f, preferredRangeMaxMeters);

        public float SightRadiusMeters => Mathf.Max(0f, sightRadiusMeters);

        public float SightAngleDegrees => SightRadiusMeters <= 0f ? 0f : Mathf.Clamp(sightAngleDegrees, 0f, 360f);

        public float HearingRadiusMeters => Mathf.Max(0f, hearingRadiusMeters);

        public float HearingSensitivityMultiplier => Mathf.Clamp(hearingSensitivityMultiplier <= 0f ? DefaultDisturbanceTuning.x : hearingSensitivityMultiplier, 0.1f, 3f);

        public float DisturbanceEscalationThreshold => Mathf.Clamp(disturbanceEscalationThreshold <= 0f ? DefaultDisturbanceTuning.y : disturbanceEscalationThreshold, 0.05f, 8f);

        public float InvestigationDurationSeconds => Mathf.Clamp(investigationDurationSeconds <= 0f ? DefaultDisturbanceTuning.z : investigationDurationSeconds, 0.05f, 8f);

        public bool AllyAlertSharingEnabled => allyAlertSharingEnabled;

        public float AllyAlertRadiusMeters => Mathf.Max(0f, allyAlertRadiusMeters);

        public float AllyAlertCooldownSeconds => Mathf.Clamp(allyAlertCooldownSeconds <= 0f ? DefaultAllyAlertSharing.cooldownSeconds : allyAlertCooldownSeconds, 0.25f, 12f);

        public EnemyAwarenessState AllyAlertMinimumAwareness => allyAlertMinimumAwareness;

        public bool LungeAttackEnabled => lungeAttackEnabled;

        public float LungeTriggerRangeMeters => Mathf.Max(0.05f, lungeTriggerRangeMeters);

        public float LungeWindupSeconds => Mathf.Max(0f, lungeWindupSeconds);

        public float LungeActiveSeconds => Mathf.Max(0.01f, lungeActiveSeconds);

        public float LungeDistanceMeters => Mathf.Max(0f, lungeDistanceMeters);

        public float LungeCooldownSeconds => Mathf.Max(0.05f, lungeCooldownSeconds);

        public float AttackCooldownSeconds => attackCooldownSeconds;

        public int ProjectileDamage => projectileDamage;

        public float ProjectileSpeedMetersPerSecond => projectileSpeedMetersPerSecond;

        public float ChargeSpeedMetersPerSecond => chargeSpeedMetersPerSecond;

        public float ChargeCooldownSeconds => chargeCooldownSeconds;

        public string SplitSpawnKind => splitSpawnKind;

        public int SplitCount => splitCount;

        public EnemyBodyClass BodyClass => bodyClass;

        public EnemyIntelligenceLevel Intelligence => EnemyIntelligenceLevelExtensions.Clamp((int)intelligence);

        public EnemyInstinctDisposition Disposition => EnemyInstinctDispositionExtensions.Clamp((int)disposition);

        public float AttackWindupScale => Mathf.Clamp(attackWindupScale <= 0f ? DefaultExecution.windupScale : attackWindupScale, 0.35f, 2.5f);

        public float AttackActiveScale => Mathf.Clamp(attackActiveScale <= 0f ? DefaultExecution.activeScale : attackActiveScale, 0.35f, 2.5f);

        public float AttackRecoveryScale => Mathf.Clamp(attackRecoveryScale <= 0f ? DefaultExecution.recoveryScale : attackRecoveryScale, 0.35f, 2.5f);

        public float HitArcDegreesBonus => Mathf.Clamp(!Mathf.Approximately(hitArcDegreesBonus, 0f) ? hitArcDegreesBonus : DefaultExecution.hitArcDegreesBonus, -90f, 120f);

        public int PoiseBreakThresholdOffset => Mathf.Clamp(poiseBreakThresholdOffset != 0 ? poiseBreakThresholdOffset : DefaultExecution.poiseBreakThresholdOffset, -3, 3);

        private AttackExecutionDefaults DefaultExecution => DefaultAttackExecutionFor(archetypeId, behaviorId, movementMode);

        private Vector3 DefaultDisturbanceTuning => DefaultDisturbanceTuningFor(archetypeId, behaviorId, movementMode);

        private AllyAlertDefaults DefaultAllyAlertSharing => DefaultAllyAlertFor(archetypeId, behaviorId, movementMode);

        public IReadOnlyList<EnemyAttackProfileDefinition> AttackProfiles
        {
            get
            {
                var authored = attackProfiles?.Where(profile => profile != null).ToArray() ?? System.Array.Empty<EnemyAttackProfileDefinition>();
                var fallback = EnemyAttackProfileDefaults.CreateEnemyProfiles(SpawnKind);
                return authored
                    .Concat(fallback.Where(profile => authored.All(existing => existing.AttackId != profile.AttackId)))
                    .ToArray();
            }
        }

        public IReadOnlyList<EnemyActionProfileDefinition> ActionProfiles
        {
            get
            {
                var authored = actionProfiles?.Where(profile => profile != null).ToArray() ?? System.Array.Empty<EnemyActionProfileDefinition>();
                var fallback = EnemyActionProfileDefaults.CreateEnemyActions(SpawnKind);
                return authored
                    .Concat(fallback.Where(profile => authored.All(existing => existing.ActionId != profile.ActionId)))
                    .ToArray();
            }
        }

        public EnemyBehaviorTreeDefinition BehaviorTree => behaviorTree != null
            ? behaviorTree
            : EnemyBehaviorTreeDefaults.ResolveEnemyTree(SpawnKind);

        public EnemyGuardProfileDefinition GuardProfile => guardProfile != null
            ? guardProfile
            : EnemyGuardProfileDefinition.DefaultForBehavior(BehaviorId);

        public Color Color => color;

        public void Configure(
            string nextSpawnKind,
            string nextDisplayName,
            EnemyArchetypeId nextArchetypeId,
            EnemyMovementMode nextMovementMode,
            int nextMaxHealth,
            float nextSpeedMetersPerSecond,
            int nextContactDamage,
            float nextContactCooldownSeconds,
            float nextRadiusMeters,
            Color nextColor)
        {
            Configure(
                nextSpawnKind,
                nextDisplayName,
                nextArchetypeId,
                DefaultBehaviorFor(nextArchetypeId, nextMovementMode),
                nextMovementMode,
                nextMaxHealth,
                nextSpeedMetersPerSecond,
                nextContactDamage,
                nextContactCooldownSeconds,
                nextRadiusMeters,
                nextAttackRangeMeters: 5f,
                nextAttackCooldownSeconds: 1.4f,
                nextProjectileDamage: 1,
                nextProjectileSpeedMetersPerSecond: 5f,
                nextChargeSpeedMetersPerSecond: 5f,
                nextChargeCooldownSeconds: 2.25f,
                nextSplitSpawnKind: "spawnEnemyNormal",
                nextSplitCount: 0,
                nextBodyClass: DefaultBodyClassFor(nextArchetypeId, DefaultBehaviorFor(nextArchetypeId, nextMovementMode), nextMovementMode),
                nextIntelligence: DefaultIntelligenceFor(nextArchetypeId, DefaultBehaviorFor(nextArchetypeId, nextMovementMode), nextMovementMode),
                nextDisposition: DefaultDispositionFor(nextArchetypeId, DefaultBehaviorFor(nextArchetypeId, nextMovementMode), nextMovementMode),
                nextColor);
        }

        public void Configure(
            string nextSpawnKind,
            string nextDisplayName,
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode,
            int nextMaxHealth,
            float nextSpeedMetersPerSecond,
            int nextContactDamage,
            float nextContactCooldownSeconds,
            float nextRadiusMeters,
            float nextAttackRangeMeters,
            float nextAttackCooldownSeconds,
            int nextProjectileDamage,
            float nextProjectileSpeedMetersPerSecond,
            float nextChargeSpeedMetersPerSecond,
            float nextChargeCooldownSeconds,
            string nextSplitSpawnKind,
            int nextSplitCount,
            Color nextColor)
        {
            Configure(
                nextSpawnKind,
                nextDisplayName,
                nextArchetypeId,
                nextBehaviorId,
                nextMovementMode,
                nextMaxHealth,
                nextSpeedMetersPerSecond,
                nextContactDamage,
                nextContactCooldownSeconds,
                nextRadiusMeters,
                nextAttackRangeMeters,
                nextAttackCooldownSeconds,
                nextProjectileDamage,
                nextProjectileSpeedMetersPerSecond,
                nextChargeSpeedMetersPerSecond,
                nextChargeCooldownSeconds,
                nextSplitSpawnKind,
                nextSplitCount,
                DefaultBodyClassFor(nextArchetypeId, nextBehaviorId, nextMovementMode),
                DefaultIntelligenceFor(nextArchetypeId, nextBehaviorId, nextMovementMode),
                DefaultDispositionFor(nextArchetypeId, nextBehaviorId, nextMovementMode),
                nextColor);
        }

        public void Configure(
            string nextSpawnKind,
            string nextDisplayName,
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode,
            int nextMaxHealth,
            float nextSpeedMetersPerSecond,
            int nextContactDamage,
            float nextContactCooldownSeconds,
            float nextRadiusMeters,
            float nextAttackRangeMeters,
            float nextAttackCooldownSeconds,
            int nextProjectileDamage,
            float nextProjectileSpeedMetersPerSecond,
            float nextChargeSpeedMetersPerSecond,
            float nextChargeCooldownSeconds,
            string nextSplitSpawnKind,
            int nextSplitCount,
            EnemyBodyClass nextBodyClass,
            Color nextColor)
        {
            Configure(
                nextSpawnKind,
                nextDisplayName,
                nextArchetypeId,
                nextBehaviorId,
                nextMovementMode,
                nextMaxHealth,
                nextSpeedMetersPerSecond,
                nextContactDamage,
                nextContactCooldownSeconds,
                nextRadiusMeters,
                nextAttackRangeMeters,
                nextAttackCooldownSeconds,
                nextProjectileDamage,
                nextProjectileSpeedMetersPerSecond,
                nextChargeSpeedMetersPerSecond,
                nextChargeCooldownSeconds,
                nextSplitSpawnKind,
                nextSplitCount,
                nextBodyClass,
                DefaultIntelligenceFor(nextArchetypeId, nextBehaviorId, nextMovementMode),
                DefaultDispositionFor(nextArchetypeId, nextBehaviorId, nextMovementMode),
                nextColor);
        }

        public void Configure(
            string nextSpawnKind,
            string nextDisplayName,
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode,
            int nextMaxHealth,
            float nextSpeedMetersPerSecond,
            int nextContactDamage,
            float nextContactCooldownSeconds,
            float nextRadiusMeters,
            float nextAttackRangeMeters,
            float nextAttackCooldownSeconds,
            int nextProjectileDamage,
            float nextProjectileSpeedMetersPerSecond,
            float nextChargeSpeedMetersPerSecond,
            float nextChargeCooldownSeconds,
            string nextSplitSpawnKind,
            int nextSplitCount,
            EnemyBodyClass nextBodyClass,
            EnemyIntelligenceLevel nextIntelligence,
            EnemyInstinctDisposition nextDisposition,
            Color nextColor)
        {
            spawnKind = nextSpawnKind;
            displayName = nextDisplayName;
            archetypeId = nextArchetypeId;
            behaviorId = nextBehaviorId;
            movementMode = nextMovementMode;
            maxHealth = Mathf.Max(1, nextMaxHealth);
            speedMetersPerSecond = Mathf.Max(0f, nextSpeedMetersPerSecond);
            contactDamage = Mathf.Max(0, nextContactDamage);
            contactCooldownSeconds = Mathf.Max(0.01f, nextContactCooldownSeconds);
            contactDamagePolicy = EnemyContactDamagePolicy.ActiveOnly;
            passiveContactHazardType = EnemyPassiveContactHazardType.None;
            radiusMeters = Mathf.Max(0.01f, nextRadiusMeters);
            attackRangeMeters = Mathf.Max(0.1f, nextAttackRangeMeters);
            attackCooldownSeconds = Mathf.Max(0.05f, nextAttackCooldownSeconds);
            projectileDamage = Mathf.Max(0, nextProjectileDamage);
            projectileSpeedMetersPerSecond = Mathf.Max(0.1f, nextProjectileSpeedMetersPerSecond);
            chargeSpeedMetersPerSecond = Mathf.Max(0f, nextChargeSpeedMetersPerSecond);
            chargeCooldownSeconds = Mathf.Max(0.05f, nextChargeCooldownSeconds);
            splitSpawnKind = string.IsNullOrWhiteSpace(nextSplitSpawnKind) ? "spawnEnemyNormal" : nextSplitSpawnKind;
            splitCount = Mathf.Max(0, nextSplitCount);
            bodyClass = nextBodyClass;
            intelligence = EnemyIntelligenceLevelExtensions.Clamp((int)nextIntelligence);
            disposition = EnemyInstinctDispositionExtensions.Clamp((int)nextDisposition);
            var preferredRange = DefaultPreferredRangeFor(nextArchetypeId, nextBehaviorId, nextMovementMode);
            preferredRangeMinMeters = preferredRange.x;
            preferredRangeMaxMeters = preferredRange.y;
            var senses = DefaultSensesFor(nextArchetypeId, nextBehaviorId, nextMovementMode);
            var lunge = DefaultLungeFor(nextArchetypeId, nextBehaviorId, nextMovementMode);
            var execution = DefaultAttackExecutionFor(nextArchetypeId, nextBehaviorId, nextMovementMode);
            var disturbance = DefaultDisturbanceTuningFor(nextArchetypeId, nextBehaviorId, nextMovementMode);
            var allyAlert = DefaultAllyAlertFor(nextArchetypeId, nextBehaviorId, nextMovementMode);
            ConfigureSenseAndLunge(
                senses.x,
                senses.y,
                senses.z,
                lunge.enabled,
                lunge.triggerRange,
                lunge.windup,
                lunge.active,
                lunge.distance,
                lunge.cooldown);
            ConfigureAttackExecutionModifiers(
                execution.windupScale,
                execution.activeScale,
                execution.recoveryScale,
                execution.hitArcDegreesBonus,
                execution.poiseBreakThresholdOffset);
            ConfigureDisturbance(disturbance.x, disturbance.y, disturbance.z);
            ConfigureAllyAlertSharing(allyAlert.enabled, allyAlert.radiusMeters, allyAlert.cooldownSeconds, allyAlert.minimumAwareness);
            color = nextColor;
        }

        public void Configure(
            string nextSpawnKind,
            string nextDisplayName,
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode,
            int nextMaxHealth,
            float nextSpeedMetersPerSecond,
            int nextContactDamage,
            float nextContactCooldownSeconds,
            float nextRadiusMeters,
            float nextAttackRangeMeters,
            float nextAttackCooldownSeconds,
            int nextProjectileDamage,
            float nextProjectileSpeedMetersPerSecond,
            float nextChargeSpeedMetersPerSecond,
            float nextChargeCooldownSeconds,
            string nextSplitSpawnKind,
            int nextSplitCount,
            EnemyBodyClass nextBodyClass,
            EnemyIntelligenceLevel nextIntelligence,
            EnemyInstinctDisposition nextDisposition,
            float nextPreferredRangeMinMeters,
            float nextPreferredRangeMaxMeters,
            Color nextColor)
        {
            Configure(
                nextSpawnKind,
                nextDisplayName,
                nextArchetypeId,
                nextBehaviorId,
                nextMovementMode,
                nextMaxHealth,
                nextSpeedMetersPerSecond,
                nextContactDamage,
                nextContactCooldownSeconds,
                nextRadiusMeters,
                nextAttackRangeMeters,
                nextAttackCooldownSeconds,
                nextProjectileDamage,
                nextProjectileSpeedMetersPerSecond,
                nextChargeSpeedMetersPerSecond,
                nextChargeCooldownSeconds,
                nextSplitSpawnKind,
                nextSplitCount,
                nextBodyClass,
                nextIntelligence,
                nextDisposition,
                nextColor);

            var preferredRange = SanitizePreferredRange(nextPreferredRangeMinMeters, nextPreferredRangeMaxMeters);
            preferredRangeMinMeters = preferredRange.x;
            preferredRangeMaxMeters = preferredRange.y;
        }

        public void ConfigureSenseAndLunge(
            float nextSightRadiusMeters,
            float nextSightAngleDegrees,
            float nextHearingRadiusMeters,
            bool nextLungeAttackEnabled,
            float nextLungeTriggerRangeMeters,
            float nextLungeWindupSeconds,
            float nextLungeActiveSeconds,
            float nextLungeDistanceMeters,
            float nextLungeCooldownSeconds)
        {
            sightRadiusMeters = Mathf.Max(0f, nextSightRadiusMeters);
            sightAngleDegrees = sightRadiusMeters <= 0f ? 0f : Mathf.Clamp(nextSightAngleDegrees, 0f, 360f);
            hearingRadiusMeters = Mathf.Max(0f, nextHearingRadiusMeters);
            lungeAttackEnabled = nextLungeAttackEnabled;
            lungeTriggerRangeMeters = Mathf.Max(0.05f, nextLungeTriggerRangeMeters);
            lungeWindupSeconds = Mathf.Max(0f, nextLungeWindupSeconds);
            lungeActiveSeconds = Mathf.Max(0.01f, nextLungeActiveSeconds);
            lungeDistanceMeters = Mathf.Max(0f, nextLungeDistanceMeters);
            lungeCooldownSeconds = Mathf.Max(0.05f, nextLungeCooldownSeconds);
        }

        public void ConfigureContactPolicy(
            EnemyContactDamagePolicy nextContactDamagePolicy,
            EnemyPassiveContactHazardType nextPassiveContactHazardType)
        {
            contactDamagePolicy = nextContactDamagePolicy;
            passiveContactHazardType = contactDamagePolicy == EnemyContactDamagePolicy.PassiveHazard
                ? nextPassiveContactHazardType
                : EnemyPassiveContactHazardType.None;
        }

        public void ConfigureDisturbance(
            float nextHearingSensitivityMultiplier,
            float nextDisturbanceEscalationThreshold,
            float nextInvestigationDurationSeconds)
        {
            hearingSensitivityMultiplier = Mathf.Clamp(nextHearingSensitivityMultiplier <= 0f ? 1f : nextHearingSensitivityMultiplier, 0.1f, 3f);
            disturbanceEscalationThreshold = Mathf.Clamp(nextDisturbanceEscalationThreshold <= 0f ? 1f : nextDisturbanceEscalationThreshold, 0.05f, 8f);
            investigationDurationSeconds = Mathf.Clamp(nextInvestigationDurationSeconds <= 0f ? 1f : nextInvestigationDurationSeconds, 0.05f, 8f);
        }

        public void ConfigureAllyAlertSharing(
            bool enabled,
            float radiusMeters,
            float cooldownSeconds,
            EnemyAwarenessState minimumAwareness)
        {
            allyAlertSharingEnabled = enabled;
            allyAlertRadiusMeters = enabled ? Mathf.Clamp(radiusMeters, 0.1f, 12f) : 0f;
            allyAlertCooldownSeconds = Mathf.Clamp(cooldownSeconds <= 0f ? 2f : cooldownSeconds, 0.25f, 12f);
            allyAlertMinimumAwareness = minimumAwareness;
        }

        public void ConfigureAttackExecutionModifiers(
            float nextAttackWindupScale,
            float nextAttackActiveScale,
            float nextAttackRecoveryScale,
            float nextHitArcDegreesBonus,
            int nextPoiseBreakThresholdOffset)
        {
            attackWindupScale = Mathf.Clamp(nextAttackWindupScale <= 0f ? 1f : nextAttackWindupScale, 0.35f, 2.5f);
            attackActiveScale = Mathf.Clamp(nextAttackActiveScale <= 0f ? 1f : nextAttackActiveScale, 0.35f, 2.5f);
            attackRecoveryScale = Mathf.Clamp(nextAttackRecoveryScale <= 0f ? 1f : nextAttackRecoveryScale, 0.35f, 2.5f);
            hitArcDegreesBonus = Mathf.Clamp(nextHitArcDegreesBonus, -90f, 120f);
            poiseBreakThresholdOffset = Mathf.Clamp(nextPoiseBreakThresholdOffset, -3, 3);
        }

        public void ConfigureAttackProfiles(IEnumerable<EnemyAttackProfileDefinition> nextAttackProfiles)
        {
            attackProfiles = nextAttackProfiles?.Where(profile => profile != null).ToList() ?? new List<EnemyAttackProfileDefinition>();
        }

        public void ConfigureActionProfiles(IEnumerable<EnemyActionProfileDefinition> nextActionProfiles)
        {
            actionProfiles = nextActionProfiles?.Where(profile => profile != null).ToList() ?? new List<EnemyActionProfileDefinition>();
        }

        public void ConfigureBehaviorTree(EnemyBehaviorTreeDefinition nextBehaviorTree)
        {
            behaviorTree = nextBehaviorTree;
        }

        public void ConfigureGuardProfile(EnemyGuardProfileDefinition nextGuardProfile)
        {
            guardProfile = nextGuardProfile;
        }

        public EnemyAttackProfileDefinition ResolveAttackProfile(string attackId)
        {
            if (attackProfiles != null)
            {
                var authored = attackProfiles.FirstOrDefault(profile =>
                    profile != null &&
                    string.Equals(profile.AttackId, attackId, System.StringComparison.Ordinal));
                if (authored != null)
                {
                    return authored;
                }
            }

            return EnemyAttackProfileDefaults.ResolveEnemyProfile(SpawnKind, attackId) ?? AttackProfiles.FirstOrDefault();
        }

        public EnemyActionProfileDefinition ResolveActionProfile(string actionId)
        {
            if (actionProfiles != null)
            {
                var authored = actionProfiles.FirstOrDefault(profile =>
                    profile != null &&
                    string.Equals(profile.ActionId, actionId, System.StringComparison.Ordinal));
                if (authored != null)
                {
                    return authored;
                }
            }

            return EnemyActionProfileDefaults.ResolveEnemyAction(SpawnKind, actionId) ?? ActionProfiles.FirstOrDefault();
        }

        public static EnemyDefinition CreateRuntime(
            string spawnKind,
            string displayName,
            EnemyArchetypeId archetypeId,
            EnemyMovementMode movementMode,
            int maxHealth,
            float speedMetersPerSecond,
            int contactDamage,
            Color color)
        {
            return CreateRuntime(
                spawnKind,
                displayName,
                archetypeId,
                movementMode,
                maxHealth,
                speedMetersPerSecond,
                contactDamage,
                DefaultIntelligenceFor(archetypeId, DefaultBehaviorFor(archetypeId, movementMode), movementMode),
                DefaultDispositionFor(archetypeId, DefaultBehaviorFor(archetypeId, movementMode), movementMode),
                color);
        }

        public static EnemyDefinition CreateRuntime(
            string spawnKind,
            string displayName,
            EnemyArchetypeId archetypeId,
            EnemyMovementMode movementMode,
            int maxHealth,
            float speedMetersPerSecond,
            int contactDamage,
            EnemyIntelligenceLevel intelligence,
            EnemyInstinctDisposition disposition,
            Color color)
        {
            var definition = CreateInstance<EnemyDefinition>();
            definition.Configure(
                spawnKind,
                displayName,
                archetypeId,
                DefaultBehaviorFor(archetypeId, movementMode),
                movementMode,
                maxHealth,
                speedMetersPerSecond,
                contactDamage,
                1f,
                0.32f,
                5f,
                1.4f,
                1,
                5f,
                5f,
                2.25f,
                "spawnEnemyNormal",
                0,
                DefaultBodyClassFor(archetypeId, DefaultBehaviorFor(archetypeId, movementMode), movementMode),
                intelligence,
                disposition,
                color);
            return definition;
        }

        public static EnemyDefinition CreateRuntimeNormal()
        {
            return CreateRuntime("spawnEnemyNormal", "Normal Chaser", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 3, 1.5f, 1, new Color(0.85f, 0.16f, 0.14f, 1f));
        }

        public static EnemyDefinition CreateRuntimeBoss()
        {
            var definition = CreateRuntime("spawnEnemyBoss", "Stone Warden", EnemyArchetypeId.Boss, EnemyMovementMode.Grounded, 14, 0.75f, 2, new Color(0.42f, 0.34f, 0.28f, 1f));
            definition.Configure(
                "spawnEnemyBoss",
                "Stone Warden",
                EnemyArchetypeId.Boss,
                EnemyBehaviorId.BossWarden,
                EnemyMovementMode.Grounded,
                14,
                0.75f,
                2,
                1f,
                0.55f,
                6f,
                1.35f,
                1,
                4.5f,
                4.5f,
                2.4f,
                "spawnEnemyNormal",
                0,
                EnemyBodyClass.Massive,
                EnemyIntelligenceLevel.Basic,
                EnemyInstinctDisposition.Sentinel,
                new Color(0.42f, 0.34f, 0.28f, 1f));
            return definition;
        }

        public static EnemyBodyClass DefaultBodyClassFor(
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss)
            {
                return EnemyBodyClass.Massive;
            }

            if (nextBehaviorId == EnemyBehaviorId.Giant)
            {
                return EnemyBodyClass.Massive;
            }

            if (nextBehaviorId is EnemyBehaviorId.PowderGunner or EnemyBehaviorId.RepeaterTurret or EnemyBehaviorId.ClockworkSentry or EnemyBehaviorId.SoulEater or EnemyBehaviorId.GraveLantern)
            {
                return EnemyBodyClass.Heavy;
            }

            if (nextArchetypeId == EnemyArchetypeId.Heavy ||
                nextBehaviorId == EnemyBehaviorId.TurretShooter ||
                nextBehaviorId == EnemyBehaviorId.SpittingPod ||
                nextBehaviorId == EnemyBehaviorId.Knight)
            {
                return EnemyBodyClass.Heavy;
            }

            if (nextBehaviorId == EnemyBehaviorId.Charger || nextBehaviorId == EnemyBehaviorId.Splitter)
            {
                return EnemyBodyClass.Medium;
            }

            if (nextBehaviorId == EnemyBehaviorId.HollowBeast)
            {
                return EnemyBodyClass.Medium;
            }

            if (nextBehaviorId is EnemyBehaviorId.HollowArcher or EnemyBehaviorId.KnifeThrower or EnemyBehaviorId.HollowAcolyte or EnemyBehaviorId.CurseBinder)
            {
                return EnemyBodyClass.Medium;
            }

            if (nextBehaviorId == EnemyBehaviorId.Rat ||
                nextBehaviorId == EnemyBehaviorId.Spider ||
                nextBehaviorId == EnemyBehaviorId.HollowBird ||
                nextBehaviorId == EnemyBehaviorId.Wraith ||
                nextArchetypeId == EnemyArchetypeId.Fast ||
                nextMovementMode == EnemyMovementMode.Flying)
            {
                return EnemyBodyClass.Light;
            }

            return EnemyBodyClass.Medium;
        }

        public static EnemyIntelligenceLevel DefaultIntelligenceFor(
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss)
            {
                return EnemyIntelligenceLevel.Basic;
            }

            return nextBehaviorId switch
            {
                EnemyBehaviorId.FlyingChaser => EnemyIntelligenceLevel.Instinctive,
                EnemyBehaviorId.Charger => EnemyIntelligenceLevel.Instinctive,
                EnemyBehaviorId.Splitter => EnemyIntelligenceLevel.Basic,
                EnemyBehaviorId.TurretShooter => EnemyIntelligenceLevel.Trained,
                EnemyBehaviorId.SpittingPod => EnemyIntelligenceLevel.Simple,
                EnemyBehaviorId.Rat => EnemyIntelligenceLevel.Basic,
                EnemyBehaviorId.Spider => EnemyIntelligenceLevel.Simple,
                EnemyBehaviorId.HollowBird => EnemyIntelligenceLevel.Simple,
                EnemyBehaviorId.HollowBeast => EnemyIntelligenceLevel.Basic,
                EnemyBehaviorId.SkeletonSword or EnemyBehaviorId.SkeletonSpear => EnemyIntelligenceLevel.Basic,
                EnemyBehaviorId.Knight => EnemyIntelligenceLevel.Trained,
                EnemyBehaviorId.Giant => EnemyIntelligenceLevel.Basic,
                EnemyBehaviorId.HollowArcher => EnemyIntelligenceLevel.Basic,
                EnemyBehaviorId.PowderGunner => EnemyIntelligenceLevel.Trained,
                EnemyBehaviorId.KnifeThrower => EnemyIntelligenceLevel.Basic,
                EnemyBehaviorId.RepeaterTurret => EnemyIntelligenceLevel.Trained,
                EnemyBehaviorId.ClockworkSentry => EnemyIntelligenceLevel.Tactical,
                EnemyBehaviorId.HollowAcolyte => EnemyIntelligenceLevel.Trained,
                EnemyBehaviorId.Wraith => EnemyIntelligenceLevel.Tactical,
                EnemyBehaviorId.SoulEater => EnemyIntelligenceLevel.Trained,
                EnemyBehaviorId.CurseBinder => EnemyIntelligenceLevel.Tactical,
                EnemyBehaviorId.GraveLantern => EnemyIntelligenceLevel.Basic,
                _ => EnemyIntelligenceLevel.Simple
            };
        }

        public static EnemyInstinctDisposition DefaultDispositionFor(
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss)
            {
                return EnemyInstinctDisposition.Sentinel;
            }

            if (nextBehaviorId == EnemyBehaviorId.FlyingChaser)
            {
                return EnemyInstinctDisposition.Prey;
            }

            if (nextBehaviorId == EnemyBehaviorId.TurretShooter)
            {
                return EnemyInstinctDisposition.Sentinel;
            }

            if (nextBehaviorId == EnemyBehaviorId.SpittingPod)
            {
                return EnemyInstinctDisposition.Sentinel;
            }

            if (nextBehaviorId == EnemyBehaviorId.Rat)
            {
                return EnemyInstinctDisposition.Territorial;
            }

            if (nextBehaviorId == EnemyBehaviorId.Spider)
            {
                return EnemyInstinctDisposition.Prey;
            }

            if (nextBehaviorId == EnemyBehaviorId.HollowBird || nextBehaviorId == EnemyBehaviorId.HollowBeast)
            {
                return EnemyInstinctDisposition.Predator;
            }

            if (nextBehaviorId == EnemyBehaviorId.SkeletonSpear || nextBehaviorId == EnemyBehaviorId.Knight)
            {
                return EnemyInstinctDisposition.Sentinel;
            }

            if (nextBehaviorId is EnemyBehaviorId.HollowArcher or EnemyBehaviorId.PowderGunner or EnemyBehaviorId.RepeaterTurret or EnemyBehaviorId.ClockworkSentry or EnemyBehaviorId.HollowAcolyte or EnemyBehaviorId.GraveLantern)
            {
                return EnemyInstinctDisposition.Sentinel;
            }

            if (nextBehaviorId == EnemyBehaviorId.KnifeThrower || nextBehaviorId == EnemyBehaviorId.CurseBinder)
            {
                return EnemyInstinctDisposition.Territorial;
            }

            if (nextBehaviorId is EnemyBehaviorId.Wraith or EnemyBehaviorId.SoulEater)
            {
                return EnemyInstinctDisposition.Predator;
            }

            if (nextBehaviorId == EnemyBehaviorId.Giant)
            {
                return EnemyInstinctDisposition.Mindless;
            }

            if (nextArchetypeId == EnemyArchetypeId.Heavy && nextBehaviorId == EnemyBehaviorId.Chaser)
            {
                return EnemyInstinctDisposition.Mindless;
            }

            return EnemyInstinctDisposition.Predator;
        }

        public static Vector2 DefaultPreferredRangeFor(
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss)
            {
                return new Vector2(4.5f, 6.5f);
            }

            return nextBehaviorId switch
            {
                EnemyBehaviorId.FlyingChaser => new Vector2(2.75f, 4.25f),
                EnemyBehaviorId.Charger => new Vector2(0.8f, 1.35f),
                EnemyBehaviorId.Splitter => new Vector2(1.25f, 2f),
                EnemyBehaviorId.TurretShooter => new Vector2(5.25f, 7.5f),
                EnemyBehaviorId.SpittingPod => new Vector2(5.5f, 8f),
                EnemyBehaviorId.Rat => new Vector2(1.2f, 2.2f),
                EnemyBehaviorId.Spider => new Vector2(1f, 1.9f),
                EnemyBehaviorId.HollowBird => new Vector2(1.8f, 3.6f),
                EnemyBehaviorId.HollowBeast => new Vector2(1.15f, 2.1f),
                EnemyBehaviorId.SkeletonSword => new Vector2(1.15f, 1.85f),
                EnemyBehaviorId.SkeletonSpear => new Vector2(1.75f, 2.75f),
                EnemyBehaviorId.Knight => new Vector2(1.35f, 2.35f),
                EnemyBehaviorId.Giant => new Vector2(1.85f, 3.1f),
                EnemyBehaviorId.HollowArcher => new Vector2(4.0f, 7.25f),
                EnemyBehaviorId.PowderGunner => new Vector2(4.75f, 8.5f),
                EnemyBehaviorId.KnifeThrower => new Vector2(2.7f, 5.25f),
                EnemyBehaviorId.RepeaterTurret => new Vector2(6.0f, 9.25f),
                EnemyBehaviorId.ClockworkSentry => new Vector2(4.8f, 7.8f),
                EnemyBehaviorId.HollowAcolyte => new Vector2(3.8f, 6.8f),
                EnemyBehaviorId.Wraith => new Vector2(2.2f, 5.2f),
                EnemyBehaviorId.SoulEater => new Vector2(2.4f, 4.8f),
                EnemyBehaviorId.CurseBinder => new Vector2(4f, 7f),
                EnemyBehaviorId.GraveLantern => new Vector2(5.5f, 8.5f),
                _ when nextArchetypeId == EnemyArchetypeId.Fast => new Vector2(0.9f, 1.45f),
                _ when nextArchetypeId == EnemyArchetypeId.Heavy => new Vector2(1.35f, 2.15f),
                _ => new Vector2(1.05f, 1.75f)
            };
        }

        public static Vector3 DefaultSensesFor(
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss)
            {
                return new Vector3(8f, 160f, 4.5f);
            }

            return nextBehaviorId switch
            {
                EnemyBehaviorId.FlyingChaser => new Vector3(7.5f, 240f, 6.5f),
                EnemyBehaviorId.Charger => new Vector3(7f, 120f, 5f),
                EnemyBehaviorId.Splitter => new Vector3(6.5f, 160f, 5f),
                EnemyBehaviorId.TurretShooter => new Vector3(9.5f, 70f, 2.5f),
                EnemyBehaviorId.SpittingPod => new Vector3(0f, 0f, 9f),
                EnemyBehaviorId.Rat => new Vector3(8f, 260f, 7.5f),
                EnemyBehaviorId.Spider => new Vector3(8.5f, 300f, 8f),
                EnemyBehaviorId.HollowBird => new Vector3(8.2f, 235f, 6.4f),
                EnemyBehaviorId.HollowBeast => new Vector3(7.2f, 165f, 6.2f),
                EnemyBehaviorId.SkeletonSword => new Vector3(6.5f, 160f, 5f),
                EnemyBehaviorId.SkeletonSpear => new Vector3(7f, 150f, 5.2f),
                EnemyBehaviorId.Knight => new Vector3(7f, 140f, 5f),
                EnemyBehaviorId.Giant => new Vector3(6f, 115f, 4.5f),
                EnemyBehaviorId.HollowArcher => new Vector3(8.5f, 135f, 5.4f),
                EnemyBehaviorId.PowderGunner => new Vector3(9f, 115f, 6f),
                EnemyBehaviorId.KnifeThrower => new Vector3(8f, 190f, 6.4f),
                EnemyBehaviorId.RepeaterTurret => new Vector3(10f, 95f, 3.2f),
                EnemyBehaviorId.ClockworkSentry => new Vector3(9f, 220f, 6.5f),
                EnemyBehaviorId.HollowAcolyte => new Vector3(8.4f, 180f, 6.2f),
                EnemyBehaviorId.Wraith => new Vector3(8.8f, 300f, 7f),
                EnemyBehaviorId.SoulEater => new Vector3(7.6f, 170f, 6f),
                EnemyBehaviorId.CurseBinder => new Vector3(8.2f, 150f, 5.8f),
                EnemyBehaviorId.GraveLantern => new Vector3(9.2f, 240f, 7.2f),
                _ when nextArchetypeId == EnemyArchetypeId.Fast => new Vector3(7f, 170f, 5f),
                _ when nextArchetypeId == EnemyArchetypeId.Heavy => new Vector3(5f, 110f, 3.5f),
                _ => new Vector3(6.5f, 150f, 4.5f)
            };
        }

        public static LungeDefaults DefaultLungeFor(
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss ||
                nextBehaviorId == EnemyBehaviorId.BossWarden ||
                nextBehaviorId == EnemyBehaviorId.Charger ||
                nextBehaviorId == EnemyBehaviorId.TurretShooter ||
                nextBehaviorId == EnemyBehaviorId.SpittingPod ||
                nextBehaviorId == EnemyBehaviorId.HollowArcher ||
                nextBehaviorId == EnemyBehaviorId.PowderGunner ||
                nextBehaviorId == EnemyBehaviorId.KnifeThrower ||
                nextBehaviorId == EnemyBehaviorId.RepeaterTurret ||
                nextBehaviorId == EnemyBehaviorId.ClockworkSentry ||
                nextBehaviorId == EnemyBehaviorId.HollowAcolyte ||
                nextBehaviorId == EnemyBehaviorId.Wraith ||
                nextBehaviorId == EnemyBehaviorId.SoulEater ||
                nextBehaviorId == EnemyBehaviorId.CurseBinder ||
                nextBehaviorId == EnemyBehaviorId.GraveLantern)
            {
                return new LungeDefaults(false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            }

            var triggerRange = nextBehaviorId switch
            {
                EnemyBehaviorId.FlyingChaser => 1.35f,
                EnemyBehaviorId.Splitter => 1.6f,
                EnemyBehaviorId.Rat => 0.95f,
                EnemyBehaviorId.Spider => 1.15f,
                EnemyBehaviorId.HollowBird => 1.55f,
                EnemyBehaviorId.HollowBeast => 1.65f,
                EnemyBehaviorId.SkeletonSword => 1.45f,
                EnemyBehaviorId.SkeletonSpear => 2.4f,
                EnemyBehaviorId.Knight => 2.15f,
                EnemyBehaviorId.Giant => 2.25f,
                EnemyBehaviorId.KnifeThrower => 1.05f,
                _ when nextArchetypeId == EnemyArchetypeId.Fast => 1.25f,
                _ when nextArchetypeId == EnemyArchetypeId.Heavy => 1.7f,
                _ => 1.4f
            };
            return nextBehaviorId switch
            {
                EnemyBehaviorId.Rat => new LungeDefaults(true, triggerRange, 0.14f, 0.14f, 0.55f, 0.9f),
                EnemyBehaviorId.Spider => new LungeDefaults(true, triggerRange, 0.12f, 0.16f, 0.7f, 0.85f),
                EnemyBehaviorId.HollowBird => new LungeDefaults(true, triggerRange, 0.18f, 0.15f, 0.95f, 1.1f),
                EnemyBehaviorId.HollowBeast => new LungeDefaults(true, triggerRange, 0.22f, 0.16f, 0.8f, 1.15f),
                EnemyBehaviorId.HollowArcher or EnemyBehaviorId.PowderGunner or EnemyBehaviorId.RepeaterTurret or EnemyBehaviorId.ClockworkSentry => new LungeDefaults(false, triggerRange, 0.22f, 0.18f, 0.75f, 1.15f),
                EnemyBehaviorId.KnifeThrower => new LungeDefaults(false, triggerRange, 0.18f, 0.14f, 0.45f, 1.1f),
                _ => new LungeDefaults(true, triggerRange, 0.22f, 0.18f, 0.75f, 1.15f)
            };
        }

        public static Vector3 DefaultDisturbanceTuningFor(
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss)
            {
                return new Vector3(1f, 1.5f, 1.2f);
            }

            return nextBehaviorId switch
            {
                EnemyBehaviorId.FlyingChaser => new Vector3(1.2f, 1.2f, 1f),
                EnemyBehaviorId.Charger => new Vector3(1f, 1.4f, 1.1f),
                EnemyBehaviorId.TurretShooter => new Vector3(1.35f, 1.2f, 1.7f),
                EnemyBehaviorId.Splitter => new Vector3(1f, 1.45f, 1.2f),
                EnemyBehaviorId.SpittingPod => new Vector3(1.6f, 0.45f, 1.6f),
                EnemyBehaviorId.Rat => new Vector3(1.35f, 1.1f, 0.85f),
                EnemyBehaviorId.Spider => new Vector3(1.45f, 0.9f, 0.7f),
                EnemyBehaviorId.HollowBird => new Vector3(1.2f, 1.25f, 0.95f),
                EnemyBehaviorId.HollowBeast => new Vector3(1.05f, 1.35f, 1.1f),
                EnemyBehaviorId.SkeletonSword => new Vector3(1f, 1.45f, 1.2f),
                EnemyBehaviorId.SkeletonSpear => new Vector3(1.05f, 1.35f, 1.3f),
                EnemyBehaviorId.Knight => new Vector3(0.95f, 1.25f, 1.5f),
                EnemyBehaviorId.Giant => new Vector3(0.75f, 1.9f, 1.5f),
                EnemyBehaviorId.HollowArcher => new Vector3(1.05f, 1.35f, 1.4f),
                EnemyBehaviorId.PowderGunner => new Vector3(1f, 1.45f, 1.55f),
                EnemyBehaviorId.KnifeThrower => new Vector3(1.15f, 1.25f, 1.1f),
                EnemyBehaviorId.RepeaterTurret => new Vector3(1.25f, 1.1f, 1.8f),
                EnemyBehaviorId.ClockworkSentry => new Vector3(1.1f, 1.25f, 1.5f),
                EnemyBehaviorId.HollowAcolyte => new Vector3(1.15f, 1.25f, 1.45f),
                EnemyBehaviorId.Wraith => new Vector3(1.3f, 1.05f, 1f),
                EnemyBehaviorId.SoulEater => new Vector3(1.2f, 1.2f, 1.25f),
                EnemyBehaviorId.CurseBinder => new Vector3(1.1f, 1.15f, 1.55f),
                EnemyBehaviorId.GraveLantern => new Vector3(1.4f, 1.05f, 1.7f),
                _ when nextArchetypeId == EnemyArchetypeId.Fast => new Vector3(1.05f, 1.45f, 1.2f),
                _ when nextArchetypeId == EnemyArchetypeId.Heavy => new Vector3(0.8f, 1.8f, 1.2f),
                _ => new Vector3(1f, 1.5f, 1.4f)
            };
        }

        public static AllyAlertDefaults DefaultAllyAlertFor(
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss)
            {
                return AllyAlertDefaults.Disabled;
            }

            return nextBehaviorId switch
            {
                EnemyBehaviorId.TurretShooter => new AllyAlertDefaults(true, 4f, 2.25f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.SpittingPod => new AllyAlertDefaults(true, 4.75f, 2.25f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.SkeletonSword => new AllyAlertDefaults(true, 3.5f, 2f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.SkeletonSpear => new AllyAlertDefaults(true, 4.25f, 2f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.Knight => new AllyAlertDefaults(true, 5f, 2.4f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.HollowArcher => new AllyAlertDefaults(true, 5.25f, 2.25f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.PowderGunner => new AllyAlertDefaults(true, 5.5f, 2.5f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.KnifeThrower => new AllyAlertDefaults(true, 4.25f, 2.1f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.RepeaterTurret => new AllyAlertDefaults(true, 4.5f, 2.25f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.ClockworkSentry => new AllyAlertDefaults(true, 6f, 2.75f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.HollowAcolyte => new AllyAlertDefaults(true, 5f, 2.25f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.SoulEater => new AllyAlertDefaults(true, 4.5f, 2.4f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.CurseBinder => new AllyAlertDefaults(true, 5.75f, 2.6f, EnemyAwarenessState.Engaged),
                EnemyBehaviorId.GraveLantern => new AllyAlertDefaults(true, 6f, 2.5f, EnemyAwarenessState.Engaged),
                _ => AllyAlertDefaults.Disabled
            };
        }

        public static AttackExecutionDefaults DefaultAttackExecutionFor(
            EnemyArchetypeId nextArchetypeId,
            EnemyBehaviorId nextBehaviorId,
            EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss)
            {
                return new AttackExecutionDefaults(1.2f, 1f, 1.2f, 0f, 2);
            }

            return nextBehaviorId switch
            {
                EnemyBehaviorId.FlyingChaser => new AttackExecutionDefaults(0.9f, 0.95f, 0.85f, 20f, -1),
                EnemyBehaviorId.Charger => new AttackExecutionDefaults(1f, 1f, 1f, 0f, 1),
                EnemyBehaviorId.TurretShooter => new AttackExecutionDefaults(1f, 1f, 1f, 0f, 1),
                EnemyBehaviorId.Splitter => new AttackExecutionDefaults(1.05f, 1f, 1f, 0f, 0),
                EnemyBehaviorId.SpittingPod => new AttackExecutionDefaults(0.9f, 1f, 0.9f, 0f, 0),
                EnemyBehaviorId.Rat => new AttackExecutionDefaults(0.75f, 0.85f, 0.65f, 30f, -1),
                EnemyBehaviorId.Spider => new AttackExecutionDefaults(0.7f, 0.8f, 0.6f, 45f, -1),
                EnemyBehaviorId.HollowBird => new AttackExecutionDefaults(0.85f, 0.9f, 0.75f, 25f, -1),
                EnemyBehaviorId.HollowBeast => new AttackExecutionDefaults(0.95f, 1f, 0.9f, 10f, 0),
                EnemyBehaviorId.SkeletonSword => new AttackExecutionDefaults(1f, 1f, 1f, 0f, 0),
                EnemyBehaviorId.SkeletonSpear => new AttackExecutionDefaults(1.05f, 1f, 1f, 0f, 0),
                EnemyBehaviorId.Knight => new AttackExecutionDefaults(1.08f, 1f, 1.1f, 0f, 1),
                EnemyBehaviorId.Giant => new AttackExecutionDefaults(1.2f, 1f, 1.15f, 0f, 1),
                EnemyBehaviorId.HollowArcher => new AttackExecutionDefaults(1f, 1f, 1.05f, 0f, 0),
                EnemyBehaviorId.PowderGunner => new AttackExecutionDefaults(1.08f, 1f, 1.18f, 0f, 1),
                EnemyBehaviorId.KnifeThrower => new AttackExecutionDefaults(0.88f, 0.95f, 0.88f, 0f, -1),
                EnemyBehaviorId.RepeaterTurret => new AttackExecutionDefaults(1f, 1f, 0.95f, 0f, 1),
                EnemyBehaviorId.ClockworkSentry => new AttackExecutionDefaults(1.05f, 1f, 1.05f, 0f, 1),
                EnemyBehaviorId.HollowAcolyte => new AttackExecutionDefaults(1.08f, 1f, 1.08f, 0f, 0),
                EnemyBehaviorId.Wraith => new AttackExecutionDefaults(0.92f, 0.95f, 0.85f, 18f, -1),
                EnemyBehaviorId.SoulEater => new AttackExecutionDefaults(1.05f, 1f, 1.05f, 6f, 0),
                EnemyBehaviorId.CurseBinder => new AttackExecutionDefaults(1.15f, 1f, 1.12f, 0f, 1),
                EnemyBehaviorId.GraveLantern => new AttackExecutionDefaults(1f, 1f, 1f, 0f, 0),
                _ when nextArchetypeId == EnemyArchetypeId.Fast => new AttackExecutionDefaults(0.8f, 0.9f, 0.8f, -5f, -1),
                _ when nextArchetypeId == EnemyArchetypeId.Heavy => new AttackExecutionDefaults(1.25f, 1.05f, 1.15f, 10f, 1),
                _ => new AttackExecutionDefaults(1f, 1f, 1f, 0f, 0)
            };
        }

        private static Vector2 SanitizePreferredRange(float minMeters, float maxMeters)
        {
            var safeMin = Mathf.Max(0f, minMeters);
            var safeMax = Mathf.Max(safeMin + 0.05f, maxMeters);
            return new Vector2(safeMin, safeMax);
        }

        public readonly struct LungeDefaults
        {
            public LungeDefaults(
                bool enabled,
                float triggerRange,
                float windup,
                float active,
                float distance,
                float cooldown)
            {
                this.enabled = enabled;
                this.triggerRange = triggerRange;
                this.windup = windup;
                this.active = active;
                this.distance = distance;
                this.cooldown = cooldown;
            }

            public bool enabled { get; }

            public float triggerRange { get; }

            public float windup { get; }

            public float active { get; }

            public float distance { get; }

            public float cooldown { get; }
        }

        public readonly struct AllyAlertDefaults
        {
            public static AllyAlertDefaults Disabled { get; } = new(false, 0f, 2f, EnemyAwarenessState.Engaged);

            public AllyAlertDefaults(bool enabled, float radiusMeters, float cooldownSeconds, EnemyAwarenessState minimumAwareness)
            {
                this.enabled = enabled;
                this.radiusMeters = radiusMeters;
                this.cooldownSeconds = cooldownSeconds;
                this.minimumAwareness = minimumAwareness;
            }

            public readonly bool enabled;
            public readonly float radiusMeters;
            public readonly float cooldownSeconds;
            public readonly EnemyAwarenessState minimumAwareness;
        }

        public readonly struct AttackExecutionDefaults
        {
            public AttackExecutionDefaults(
                float windupScale,
                float activeScale,
                float recoveryScale,
                float hitArcDegreesBonus,
                int poiseBreakThresholdOffset)
            {
                this.windupScale = windupScale;
                this.activeScale = activeScale;
                this.recoveryScale = recoveryScale;
                this.hitArcDegreesBonus = hitArcDegreesBonus;
                this.poiseBreakThresholdOffset = poiseBreakThresholdOffset;
            }

            public float windupScale { get; }

            public float activeScale { get; }

            public float recoveryScale { get; }

            public float hitArcDegreesBonus { get; }

            public int poiseBreakThresholdOffset { get; }
        }

        private static EnemyBehaviorId DefaultBehaviorFor(EnemyArchetypeId nextArchetypeId, EnemyMovementMode nextMovementMode)
        {
            if (nextArchetypeId == EnemyArchetypeId.Boss)
            {
                return EnemyBehaviorId.BossWarden;
            }

            return nextMovementMode == EnemyMovementMode.Flying ? EnemyBehaviorId.FlyingChaser : EnemyBehaviorId.Chaser;
        }
    }
}
