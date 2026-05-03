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

        public IReadOnlyList<EnemyAttackProfileDefinition> AttackProfiles
        {
            get
            {
                var authored = attackProfiles?.Where(profile => profile != null).ToArray() ?? System.Array.Empty<EnemyAttackProfileDefinition>();
                return authored.Length > 0 ? authored : EnemyAttackProfileDefaults.CreateEnemyProfiles(SpawnKind);
            }
        }

        public IReadOnlyList<EnemyActionProfileDefinition> ActionProfiles
        {
            get
            {
                var authored = actionProfiles?.Where(profile => profile != null).ToArray() ?? System.Array.Empty<EnemyActionProfileDefinition>();
                return authored.Length > 0 ? authored : EnemyActionProfileDefaults.CreateEnemyActions(SpawnKind);
            }
        }

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

            if (nextArchetypeId == EnemyArchetypeId.Heavy ||
                nextBehaviorId == EnemyBehaviorId.TurretShooter ||
                nextBehaviorId == EnemyBehaviorId.SpittingPod)
            {
                return EnemyBodyClass.Heavy;
            }

            if (nextBehaviorId == EnemyBehaviorId.Charger || nextBehaviorId == EnemyBehaviorId.Splitter)
            {
                return EnemyBodyClass.Medium;
            }

            if (nextBehaviorId == EnemyBehaviorId.Rat ||
                nextBehaviorId == EnemyBehaviorId.Spider ||
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
                nextBehaviorId == EnemyBehaviorId.SpittingPod)
            {
                return new LungeDefaults(false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            }

            var triggerRange = nextBehaviorId switch
            {
                EnemyBehaviorId.FlyingChaser => 1.35f,
                EnemyBehaviorId.Splitter => 1.6f,
                EnemyBehaviorId.Rat => 0.95f,
                EnemyBehaviorId.Spider => 1.15f,
                _ when nextArchetypeId == EnemyArchetypeId.Fast => 1.25f,
                _ when nextArchetypeId == EnemyArchetypeId.Heavy => 1.7f,
                _ => 1.4f
            };
            return nextBehaviorId switch
            {
                EnemyBehaviorId.Rat => new LungeDefaults(true, triggerRange, 0.14f, 0.14f, 0.55f, 0.9f),
                EnemyBehaviorId.Spider => new LungeDefaults(true, triggerRange, 0.12f, 0.16f, 0.7f, 0.85f),
                _ => new LungeDefaults(true, triggerRange, 0.22f, 0.18f, 0.75f, 1.15f)
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
