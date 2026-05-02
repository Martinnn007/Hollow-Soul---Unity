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
        [SerializeField] private float radiusMeters = 0.32f;
        [SerializeField] private float attackRangeMeters = 5f;
        [SerializeField] private float preferredRangeMinMeters = 1.05f;
        [SerializeField] private float preferredRangeMaxMeters = 1.75f;
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

        public float RadiusMeters => radiusMeters;

        public float AttackRangeMeters => attackRangeMeters;

        public float PreferredRangeMinMeters => Mathf.Max(0f, preferredRangeMinMeters);

        public float PreferredRangeMaxMeters => Mathf.Max(PreferredRangeMinMeters + 0.05f, preferredRangeMaxMeters);

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

            if (nextArchetypeId == EnemyArchetypeId.Heavy || nextBehaviorId == EnemyBehaviorId.TurretShooter)
            {
                return EnemyBodyClass.Heavy;
            }

            if (nextBehaviorId == EnemyBehaviorId.Charger || nextBehaviorId == EnemyBehaviorId.Splitter)
            {
                return EnemyBodyClass.Medium;
            }

            if (nextArchetypeId == EnemyArchetypeId.Fast || nextMovementMode == EnemyMovementMode.Flying)
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
                _ when nextArchetypeId == EnemyArchetypeId.Fast => new Vector2(0.9f, 1.45f),
                _ when nextArchetypeId == EnemyArchetypeId.Heavy => new Vector2(1.35f, 2.15f),
                _ => new Vector2(1.05f, 1.75f)
            };
        }

        private static Vector2 SanitizePreferredRange(float minMeters, float maxMeters)
        {
            var safeMin = Mathf.Max(0f, minMeters);
            var safeMax = Mathf.Max(safeMin + 0.05f, maxMeters);
            return new Vector2(safeMin, safeMax);
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
