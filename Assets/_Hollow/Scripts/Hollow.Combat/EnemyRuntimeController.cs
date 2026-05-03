using Hollow.Entities;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using System;
using UnityEngine;

namespace Hollow.Combat
{
    public class EnemyRuntimeController : MonoBehaviour, IIncomingDamageModifier
    {
        public const float ChargeWindupSeconds = 0.42f;
        public const float RangedWindupSeconds = 0.34f;
        public const float BossBurstWindupSeconds = 0.68f;
        public const float ChargeActiveSeconds = 0.38f;
        public const float RecentDamageEndangeredSeconds = 3f;
        public const float CloseThreatDistanceMeters = 1.25f;
        public const float CloseThreatSeconds = 0.65f;
        public const float RetreatBurstSeconds = 0.75f;
        public const float RetreatReassessSeconds = 0.35f;
        public const float RatTerritorialWarningSeconds = 0.45f;
        public const float CritterDecisionSeconds = 0.55f;
        public const float PodBallisticArcHeightMeters = 1.35f;
        public const float PodBallisticSplashRadiusMeters = 0.55f;

        [SerializeField] private float speedMetersPerSecond = ChaserEnemyController.DefaultSpeedMetersPerSecond;
        [SerializeField] private int contactDamage = ChaserEnemyController.DefaultContactDamage;
        [SerializeField] private float contactCooldownSeconds = ChaserEnemyController.DefaultContactCooldownSeconds;
        [SerializeField] private float radiusMeters = 0.32f;
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
        [SerializeField] private EnemyArchetypeId archetypeId = EnemyArchetypeId.Normal;
        [SerializeField] private EnemyBehaviorId behaviorId = EnemyBehaviorId.Chaser;
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.Grounded;
        [SerializeField] private EnemyBodyClass bodyClass = EnemyBodyClass.Medium;
        [SerializeField] private EnemyIntelligenceLevel intelligence = EnemyIntelligenceLevel.Simple;
        [SerializeField] private EnemyInstinctDisposition disposition = EnemyInstinctDisposition.Predator;
        [SerializeField] private EnemyContactDamagePolicy contactDamagePolicy = EnemyContactDamagePolicy.ActiveOnly;
        [SerializeField] private EnemyPassiveContactHazardType passiveContactHazardType = EnemyPassiveContactHazardType.None;
        [SerializeField] private EnemyAwarenessState awarenessState = EnemyAwarenessState.Engaged;
        [SerializeField] private float attackWindupScale = 1f;
        [SerializeField] private float attackActiveScale = 1f;
        [SerializeField] private float attackRecoveryScale = 1f;
        [SerializeField] private float hitArcDegreesBonus;
        [SerializeField] private int poiseBreakThresholdOffset;

        private RoomRuntimeRoot roomRuntimeRoot;
        private PlaceholderPlayerController playerController;
        private CombatantHealth playerHealth;
        private float nextAllowedContactTime;
        private float nextAllowedAttackTime;
        private float nextAllowedChargeTime;
        private float chargeEndTime;
        private float chargeRecoveryEndTime;
        private float entryGraceEndTime;
        private float readabilityStateEndTime;
        private bool firedLowHealthBossBurst;
        private Vector3 activeChargeDirection = Vector3.forward;
        private Vector3 telegraphDirection = Vector3.forward;
        private EnemyReadabilityState readabilityState = EnemyReadabilityState.Idle;
        private GameObject enemyPrefab;
        private GameObject enemyProjectilePrefab;
        private EnemyCatalog enemyCatalog;
        private DifficultyTierDefinition difficultyTier;
        private CombatFeelProfileDefinition combatFeelProfile;
        private CombatDiagnosticsModel diagnostics;
        private RoomCombatController roomCombatController;
        private BossDefinition bossDefinition;
        private BossRuntimeController bossRuntime;
        private InspectionEntityMode inspectionMode = InspectionEntityMode.LiveRuntime;
        private Vector3 homeLocalPosition;
        private Vector3 instinctMoveDirection = Vector3.forward;
        private float lastDamagedTime = float.NegativeInfinity;
        private float lastTickTime;
        private float closeThreatTimer;
        private float nextInstinctDecisionTime;
        private float retreatBurstEndTime;
        private float nextRetreatBurstAllowedTime;
        private float nextAllowedLungeTime;
        private float lungeEndTime;
        private float lungeRecoveryEndTime;
        private float rangedActiveEndTime;
        private float rangedRecoveryEndTime;
        private bool lungeContactAttempted;
        private bool chargeContactAttempted;
        private bool rangedProjectileFired;
        private Vector3 activeLungeDirection = Vector3.forward;
        private Vector3 facingDirection = Vector3.forward;
        private EnemyAttackProfileDefinition activeChargeProfile;
        private EnemyAttackProfileDefinition activeRangedProfile;
        private EnemyAttackProfileDefinition activeMeleeProfile;
        private EnemyAttackProfileDefinition activeBossContactProfile;
        private float bossActiveContactEndTime;
        private bool bossActiveContactAttempted;
        private float engagedStartTime = float.NegativeInfinity;
        private float nextCritterDecisionTime;
        private bool critterFightDecision;
        private int spawnIndex = -1;

        public event Action<EnemyRuntimeController> SpawnedChild;

        public CombatantHealth Health { get; private set; }

        public EnemyDefinition Definition { get; private set; }

        public EnemyArchetypeId ArchetypeId => archetypeId;

        public EnemyBehaviorId BehaviorId => behaviorId;

        public EnemyMovementMode MovementMode => movementMode;

        public EnemyBodyClass BodyClass => bodyClass;

        public EnemyIntelligenceLevel Intelligence => EnemyIntelligenceLevelExtensions.Clamp((int)intelligence);

        public EnemyInstinctDisposition Disposition => EnemyInstinctDispositionExtensions.Clamp((int)disposition);

        public EnemyContactDamagePolicy ContactDamagePolicy => contactDamagePolicy;

        public EnemyPassiveContactHazardType PassiveContactHazardType => passiveContactHazardType;

        public int SpawnIndex => spawnIndex;

        public float SpeedMetersPerSecond => speedMetersPerSecond;

        public int ContactDamage => contactDamage;

        public float RadiusMeters => radiusMeters;

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

        public EnemyAwarenessState AwarenessState => awarenessState;

        public float AttackWindupScale => Mathf.Clamp(attackWindupScale <= 0f ? 1f : attackWindupScale, 0.35f, 2.5f);

        public float AttackActiveScale => Mathf.Clamp(attackActiveScale <= 0f ? 1f : attackActiveScale, 0.35f, 2.5f);

        public float AttackRecoveryScale => Mathf.Clamp(attackRecoveryScale <= 0f ? 1f : attackRecoveryScale, 0.35f, 2.5f);

        public float HitArcDegreesBonus => Mathf.Clamp(hitArcDegreesBonus, -90f, 120f);

        public int PoiseBreakThresholdOffset => Mathf.Clamp(poiseBreakThresholdOffset, -3, 3);

        public Vector3 FacingDirection => facingDirection.sqrMagnitude > 0.001f ? facingDirection.normalized : Vector3.forward;

        public bool IsAlive => Health != null && Health.IsAlive;

        public bool IsInEntryGrace(float timeSeconds) => timeSeconds < entryGraceEndTime;

        public EnemyReadabilityState ReadabilityState => readabilityState;

        public BossDefinition BossDefinition => bossDefinition;

        public string BossStatusText => bossRuntime != null ? bossRuntime.StatusText : "Engaging";

        public float ReadabilityStateEndTime => readabilityStateEndTime;

        public Vector3 TelegraphDirection => telegraphDirection.sqrMagnitude < 0.001f ? Vector3.forward : telegraphDirection.normalized;

        public InspectionEntityMode InspectionMode => inspectionMode;

        public bool IsInspectionFrozen => inspectionMode == InspectionEntityMode.FrozenRuntime;

        public EnemyReadabilityState ReadabilityStateAt(float timeSeconds)
        {
            if (IsInEntryGrace(timeSeconds))
            {
                return EnemyReadabilityState.EntryGrace;
            }

            if (readabilityState is EnemyReadabilityState.EntryGrace && timeSeconds >= readabilityStateEndTime)
            {
                return EnemyReadabilityState.Idle;
            }

            return readabilityState;
        }

        public float ReadabilitySecondsRemaining(float timeSeconds)
        {
            return Mathf.Max(0f, readabilityStateEndTime - timeSeconds);
        }

        public void BeginEntryGrace(float seconds, float currentTimeSeconds)
        {
            var graceEndTime = currentTimeSeconds + Mathf.Max(0f, seconds);
            entryGraceEndTime = Mathf.Max(entryGraceEndTime, graceEndTime);
            nextAllowedContactTime = Mathf.Max(nextAllowedContactTime, entryGraceEndTime);
            nextAllowedAttackTime = Mathf.Max(nextAllowedAttackTime, entryGraceEndTime);
            nextAllowedChargeTime = Mathf.Max(nextAllowedChargeTime, entryGraceEndTime);
            nextAllowedLungeTime = Mathf.Max(nextAllowedLungeTime, entryGraceEndTime);
            StartReadabilityState(EnemyReadabilityState.EntryGrace, Mathf.Max(0f, seconds), currentTimeSeconds, Vector3.forward);
        }

        public void Configure(RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition, DifficultyTierDefinition difficultyTier)
        {
            roomRuntimeRoot = room;
            playerController = player;
            playerHealth = playerController != null ? playerController.GetComponent<CombatantHealth>() : null;
            Definition = definition != null ? definition : EnemyDefinition.CreateRuntimeNormal();
            bossDefinition = null;
            bossRuntime = null;
            var tuning = difficultyTier != null ? difficultyTier.Tuning : DifficultyTierDefinition.CreateRuntimeDeveloperSample().Tuning;

            archetypeId = Definition.ArchetypeId;
            behaviorId = Definition.BehaviorId;
            movementMode = Definition.MovementMode;
            bodyClass = Definition.BodyClass;
            intelligence = Definition.Intelligence;
            disposition = Definition.Disposition;
            contactDamagePolicy = Definition.ContactDamagePolicy;
            passiveContactHazardType = Definition.PassiveContactHazardType;
            attackWindupScale = Definition.AttackWindupScale;
            attackActiveScale = Definition.AttackActiveScale;
            attackRecoveryScale = Definition.AttackRecoveryScale;
            hitArcDegreesBonus = Definition.HitArcDegreesBonus;
            poiseBreakThresholdOffset = Definition.PoiseBreakThresholdOffset;
            speedMetersPerSecond = tuning.ApplySpeed(Definition.SpeedMetersPerSecond);
            contactDamage = tuning.ApplyContactDamage(Definition.ContactDamage);
            contactCooldownSeconds = Definition.ContactCooldownSeconds;
            radiusMeters = Definition.RadiusMeters;
            preferredRangeMinMeters = Definition.PreferredRangeMinMeters;
            preferredRangeMaxMeters = Definition.PreferredRangeMaxMeters;
            sightRadiusMeters = Definition.SightRadiusMeters;
            sightAngleDegrees = Definition.SightAngleDegrees;
            hearingRadiusMeters = Definition.HearingRadiusMeters;
            lungeAttackEnabled = Definition.LungeAttackEnabled;
            lungeTriggerRangeMeters = Definition.LungeTriggerRangeMeters;
            lungeWindupSeconds = Definition.LungeWindupSeconds;
            lungeActiveSeconds = Definition.LungeActiveSeconds;
            lungeDistanceMeters = Definition.LungeDistanceMeters;
            lungeCooldownSeconds = Definition.LungeCooldownSeconds;
            awarenessState = InitialAwarenessFor(disposition);
            homeLocalPosition = transform.localPosition;
            instinctMoveDirection = Vector3.forward;
            facingDirection = Vector3.forward;
            closeThreatTimer = 0f;
            nextInstinctDecisionTime = 0f;
            retreatBurstEndTime = 0f;
            nextRetreatBurstAllowedTime = 0f;
            nextAllowedLungeTime = 0f;
            lungeEndTime = 0f;
            lungeRecoveryEndTime = 0f;
            chargeEndTime = 0f;
            chargeRecoveryEndTime = 0f;
            rangedActiveEndTime = 0f;
            rangedRecoveryEndTime = 0f;
            lungeContactAttempted = false;
            chargeContactAttempted = false;
            rangedProjectileFired = false;
            activeLungeDirection = Vector3.forward;
            activeChargeProfile = null;
            activeRangedProfile = null;
            activeMeleeProfile = null;
            activeBossContactProfile = null;
            bossActiveContactEndTime = 0f;
            bossActiveContactAttempted = false;
            engagedStartTime = awarenessState == EnemyAwarenessState.Engaged ? 0f : float.NegativeInfinity;
            nextCritterDecisionTime = 0f;
            critterFightDecision = false;
            lastDamagedTime = float.NegativeInfinity;

            Health = GetComponent<CombatantHealth>() ?? gameObject.AddComponent<CombatantHealth>();
            Health.Configure(tuning.ApplyHealth(Definition.MaxHealth));
            Health.Damaged -= OnDamaged;
            Health.Damaged += OnDamaged;
            Health.Died -= OnDied;
            Health.Died += OnDied;
            ApplyVisualMaterial(RoleForDefinition(Definition));
            PresentationPrefabResolver.InstantiateVisual(PrefabRoleForDefinition(Definition), transform, Vector3.zero, Vector3.one);
            ConfigureCombatFeel(null);
        }

        public void ConfigureBoss(BossDefinition definition)
        {
            bossDefinition = definition;
            if (bossDefinition == null || Health == null)
            {
                return;
            }

            archetypeId = EnemyArchetypeId.Boss;
            behaviorId = EnemyBehaviorId.BossWarden;
            movementMode = EnemyMovementMode.Grounded;
            bodyClass = bossDefinition.BodyClass;
            intelligence = bossDefinition.Intelligence;
            disposition = EnemyInstinctDisposition.Sentinel;
            contactDamagePolicy = bossDefinition.ContactDamagePolicy;
            passiveContactHazardType = bossDefinition.PassiveContactHazardType;
            var execution = EnemyDefinition.DefaultAttackExecutionFor(EnemyArchetypeId.Boss, EnemyBehaviorId.BossWarden, EnemyMovementMode.Grounded);
            attackWindupScale = execution.windupScale;
            attackActiveScale = execution.activeScale;
            attackRecoveryScale = execution.recoveryScale;
            hitArcDegreesBonus = execution.hitArcDegreesBonus;
            poiseBreakThresholdOffset = execution.poiseBreakThresholdOffset;
            speedMetersPerSecond = bossDefinition.SpeedMetersPerSecond;
            contactDamage = bossDefinition.ContactDamage;
            contactCooldownSeconds = bossDefinition.ContactCooldownSeconds;
            radiusMeters = bossDefinition.RadiusMeters;
            var bossRange = EnemyDefinition.DefaultPreferredRangeFor(EnemyArchetypeId.Boss, EnemyBehaviorId.BossWarden, EnemyMovementMode.Grounded);
            preferredRangeMinMeters = bossRange.x;
            preferredRangeMaxMeters = bossRange.y;
            sightRadiusMeters = bossDefinition.SightRadiusMeters;
            sightAngleDegrees = bossDefinition.SightAngleDegrees;
            hearingRadiusMeters = bossDefinition.HearingRadiusMeters;
            lungeAttackEnabled = false;
            lungeContactAttempted = false;
            chargeContactAttempted = false;
            rangedProjectileFired = false;
            chargeEndTime = 0f;
            chargeRecoveryEndTime = 0f;
            lungeEndTime = 0f;
            lungeRecoveryEndTime = 0f;
            rangedActiveEndTime = 0f;
            rangedRecoveryEndTime = 0f;
            activeBossContactProfile = null;
            bossActiveContactEndTime = 0f;
            bossActiveContactAttempted = false;
            awarenessState = EnemyAwarenessState.Engaged;
            facingDirection = Vector3.forward;
            gameObject.name = $"Enemy.Boss.{bossDefinition.BossId}";
            transform.localScale = Vector3.one * bossDefinition.VisualScale;
            Health.Configure(bossDefinition.MaxHealth);
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.EnemyBoss, transform, Vector3.zero, Vector3.one);
            bossRuntime = GetComponent<BossRuntimeController>() ?? gameObject.AddComponent<BossRuntimeController>();
            bossRuntime.Configure(this, bossDefinition, roomRuntimeRoot, playerController, enemyProjectilePrefab, combatFeelProfile);
            ConfigureCombatFeel(combatFeelProfile);
        }

        public void ConfigureCombatFeel(CombatFeelProfileDefinition profile)
        {
            combatFeelProfile = CombatFeelProfileDefinition.Resolve(profile);
            var resistance = EnemyKnockbackResolver.ResolveBodyMultiplier(
                archetypeId == EnemyArchetypeId.Boss ? EnemyBodyClass.Massive : bodyClass,
                combatFeelProfile);

            var knockback = GetComponent<CombatKnockbackReceiver>() ?? gameObject.AddComponent<CombatKnockbackReceiver>();
            knockback.Configure(roomRuntimeRoot, radiusMeters, movementMode == EnemyMovementMode.Flying, resistance);
            var presenter = GetComponent<CombatReadabilityPresenter>() ?? gameObject.AddComponent<CombatReadabilityPresenter>();
            presenter.Bind(this, combatFeelProfile);
        }

        public void ConfigureSpawnContext(
            GameObject nextEnemyPrefab,
            GameObject nextEnemyProjectilePrefab,
            EnemyCatalog nextCatalog,
            DifficultyTierDefinition nextDifficultyTier,
            CombatDiagnosticsModel nextDiagnostics,
            int nextSpawnIndex = -1)
        {
            enemyPrefab = nextEnemyPrefab;
            enemyProjectilePrefab = nextEnemyProjectilePrefab;
            enemyCatalog = nextCatalog;
            difficultyTier = nextDifficultyTier;
            diagnostics = nextDiagnostics;
            spawnIndex = nextSpawnIndex;
        }

        public void ApplyIntelligenceDisposition(EnemyIntelligenceLevel nextIntelligence, EnemyInstinctDisposition nextDisposition)
        {
            intelligence = EnemyIntelligenceLevelExtensions.Clamp((int)nextIntelligence);
            disposition = EnemyInstinctDispositionExtensions.Clamp((int)nextDisposition);
            awarenessState = InitialAwarenessFor(disposition);
            engagedStartTime = awarenessState == EnemyAwarenessState.Engaged ? Time.time : float.NegativeInfinity;
        }

        public void ArmBossActiveContactWindow(EnemyAttackProfileDefinition profile, float timeSeconds)
        {
            if (bossDefinition == null)
            {
                return;
            }

            activeBossContactProfile = profile;
            bossActiveContactEndTime = timeSeconds + Mathf.Max(0.01f, profile != null ? profile.ActiveSeconds : 0.22f);
            bossActiveContactAttempted = false;
        }

        public void BindRoomCombatController(RoomCombatController controller)
        {
            roomCombatController = controller;
        }

        public void SetInspectionMode(InspectionEntityMode mode)
        {
            inspectionMode = mode;
            if (bossRuntime != null)
            {
                bossRuntime.SetInspectionMode(mode);
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Tick(float deltaTime, float timeSeconds)
        {
            lastTickTime = timeSeconds;
            if (!IsAlive || playerController == null || IsInspectionFrozen)
            {
                return;
            }

            if (IsInEntryGrace(timeSeconds))
            {
                return;
            }

            if (ResolvePendingReadabilityState(deltaTime, timeSeconds))
            {
                return;
            }

            if (bossRuntime != null)
            {
                bossRuntime.Tick(deltaTime, timeSeconds);
                TryApplyContactDamage(timeSeconds);
                return;
            }

            var distanceToPlayer = DistanceToPlayer();
            UpdateInstinctThreat(deltaTime, distanceToPlayer);
            UpdateAwareness(timeSeconds, distanceToPlayer);
            if (behaviorId == EnemyBehaviorId.SpittingPod)
            {
                if (ShouldSentinelEngage(distanceToPlayer, timeSeconds))
                {
                    TryRangedAttack(timeSeconds);
                }

                TryApplyContactDamage(timeSeconds);
                return;
            }

            if (behaviorId == EnemyBehaviorId.TurretShooter)
            {
                if (!ShouldSentinelEngage(distanceToPlayer, timeSeconds))
                {
                    TickSentinelHold(deltaTime);
                    TryApplyContactDamage(timeSeconds);
                    return;
                }

                TryRangedAttack(timeSeconds);
                TryApplyContactDamage(timeSeconds);
                return;
            }

            if (behaviorId == EnemyBehaviorId.BossWarden)
            {
                TickBoss(deltaTime, timeSeconds);
                TryApplyContactDamage(timeSeconds);
                return;
            }

            if (IsCritterBehavior())
            {
                TickCritter(deltaTime, timeSeconds, distanceToPlayer);
                TryApplyContactDamage(timeSeconds);
                return;
            }

            if (behaviorId == EnemyBehaviorId.Charger && TickCharge(deltaTime, timeSeconds))
            {
                TryApplyContactDamage(timeSeconds);
                return;
            }

            if (TryMeleeLunge(timeSeconds))
            {
                return;
            }

            TickIntelligenceMovement(deltaTime, timeSeconds, distanceToPlayer);
            TryApplyContactDamage(timeSeconds);
        }

        public bool CanStartBudgetedAttack(float timeSeconds)
        {
            if (!IsAlive || playerController == null || IsInspectionFrozen || IsInEntryGrace(timeSeconds) || bossRuntime != null || behaviorId == EnemyBehaviorId.BossWarden)
            {
                return false;
            }

            if (behaviorId == EnemyBehaviorId.TurretShooter || behaviorId == EnemyBehaviorId.SpittingPod)
            {
                return CanStartRangedAttack(timeSeconds);
            }

            return behaviorId == EnemyBehaviorId.Charger && CanStartChargeAttack(timeSeconds);
        }

        public bool CanStartBudgetedMeleeAttack(float timeSeconds)
        {
            return CanStartMeleeLunge(timeSeconds);
        }

        public float AttackPriorityScore(float timeSeconds)
        {
            var distance = DistanceToPlayer();
            var distanceScore = Mathf.Clamp(8f - distance, 0f, 8f);
            var behaviorScore = behaviorId switch
            {
                EnemyBehaviorId.Charger => 1.25f,
                EnemyBehaviorId.SpittingPod => 0.95f,
                _ => 1f
            };
            var intelligenceBonus = Intelligence switch
            {
                EnemyIntelligenceLevel.Cunning => 0.65f,
                EnemyIntelligenceLevel.Tactical => 0.45f,
                _ => 0f
            };

            return distanceScore + behaviorScore + intelligenceBonus;
        }

        public float MeleeAttackPriorityScore(float timeSeconds)
        {
            var distance = DistanceToPlayer();
            var triggerFit = Mathf.Clamp(LungeTriggerRangeMeters - distance, -1f, 1.5f);
            var dispositionScore = Disposition == EnemyInstinctDisposition.Prey ? -0.35f : 0f;
            var intelligenceBonus = Intelligence switch
            {
                EnemyIntelligenceLevel.Cunning => 0.65f,
                EnemyIntelligenceLevel.Tactical => 0.45f,
                _ => 0f
            };

            return triggerFit + dispositionScore + intelligenceBonus;
        }

        private void TickIntelligenceMovement(float deltaTime, float timeSeconds, float distanceToPlayer)
        {
            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var endangered = IsEndangered(timeSeconds);
            if (Intelligence == EnemyIntelligenceLevel.Instinctive && Disposition == EnemyInstinctDisposition.Prey && !endangered)
            {
                var direction = ResolvePreyMovementDirection(delta.normalized, distanceToPlayer, timeSeconds);
                MoveInDirection(direction, deltaTime, 0.85f);
                return;
            }

            if (Disposition == EnemyInstinctDisposition.Sentinel && !ShouldSentinelEngage(distanceToPlayer, timeSeconds))
            {
                TickSentinelHold(deltaTime);
                return;
            }

            if (Disposition == EnemyInstinctDisposition.Mindless && ShouldMindlessWander(timeSeconds, distanceToPlayer))
            {
                MoveInDirection(ResolveInstinctWanderDirection(timeSeconds), deltaTime, 0.55f);
                return;
            }

            if (UsesDirectInstinctPressure())
            {
                TickChase(deltaTime);
                return;
            }

            var rangeDirection = ResolvePreferredRangeDirection(delta.normalized, distanceToPlayer);
            var speedMultiplier = RangeIntentSpeedMultiplier(rangeDirection, delta.normalized, distanceToPlayer);
            MoveInDirection(rangeDirection, deltaTime, speedMultiplier);
        }

        private void TickCritter(float deltaTime, float timeSeconds, float distanceToPlayer)
        {
            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var toPlayer = delta.normalized;
            var endangered = IsEndangered(timeSeconds);
            if (behaviorId == EnemyBehaviorId.Rat)
            {
                if (endangered && timeSeconds >= nextRetreatBurstAllowedTime)
                {
                    retreatBurstEndTime = timeSeconds + RetreatBurstSeconds;
                    nextRetreatBurstAllowedTime = retreatBurstEndTime + RetreatReassessSeconds;
                }

                if (timeSeconds < retreatBurstEndTime)
                {
                    MoveInDirection(-toPlayer, deltaTime, 1.15f);
                    return;
                }

                if (awarenessState == EnemyAwarenessState.Alerted && distanceToPlayer <= PreferredRangeMaxMeters + 0.4f)
                {
                    ForceEngaged();
                }

                if (TryMeleeLunge(timeSeconds))
                {
                    return;
                }

                if (awarenessState == EnemyAwarenessState.Engaged && distanceToPlayer > LungeTriggerRangeMeters)
                {
                    var flank = ResolveCritterWanderDirection(timeSeconds);
                    var pressure = (toPlayer * 0.75f + flank * 0.45f).normalized;
                    MoveInDirection(pressure, deltaTime, 0.95f);
                    return;
                }

                MoveInDirection(ResolveCritterWanderDirection(timeSeconds), deltaTime, 0.9f);
                return;
            }

            if (behaviorId == EnemyBehaviorId.Spider)
            {
                if (timeSeconds >= nextCritterDecisionTime)
                {
                    var seed = Mathf.Abs((spawnIndex + 1) * 31 + Mathf.FloorToInt(timeSeconds * 10f) * 17);
                    var fightThreshold = awarenessState == EnemyAwarenessState.Engaged || endangered ? 6 : 3;
                    critterFightDecision = seed % 10 < fightThreshold;
                    nextCritterDecisionTime = timeSeconds + CritterDecisionSeconds;
                    if (!critterFightDecision && timeSeconds >= nextRetreatBurstAllowedTime)
                    {
                        retreatBurstEndTime = timeSeconds + RetreatBurstSeconds;
                        nextRetreatBurstAllowedTime = retreatBurstEndTime + RetreatReassessSeconds;
                    }
                }

                if (timeSeconds < retreatBurstEndTime)
                {
                    var jitter = ResolveCritterWanderDirection(timeSeconds);
                    MoveInDirection((-toPlayer + jitter * 0.35f).normalized, deltaTime, 1.1f);
                    return;
                }

                if ((critterFightDecision || endangered) && TryMeleeLunge(timeSeconds))
                {
                    return;
                }

                var direction = critterFightDecision && awarenessState == EnemyAwarenessState.Engaged
                    ? (toPlayer + ResolveCritterWanderDirection(timeSeconds) * 0.35f).normalized
                    : ResolveCritterWanderDirection(timeSeconds);
                MoveInDirection(direction, deltaTime, 1f);
            }
        }

        private void TickBoss(float deltaTime, float timeSeconds)
        {
            if (Health != null &&
                !firedLowHealthBossBurst &&
                Health.CurrentHealth <= Mathf.CeilToInt(Health.MaxHealth * 0.5f))
            {
                StartReadabilityState(EnemyReadabilityState.BossBurstWindup, BossBurstWindupSeconds, timeSeconds, Vector3.forward);
                return;
            }

            if (TickCharge(deltaTime, timeSeconds))
            {
                return;
            }

            TickChase(deltaTime, allowSteering: false);
            TryRangedAttack(timeSeconds);
        }

        private bool TickCharge(float deltaTime, float timeSeconds)
        {
            if (timeSeconds < nextAllowedChargeTime || playerController == null)
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (!CanStartChargeAttack(timeSeconds))
            {
                return false;
            }

            if (RequiresAttackBudget() && !TryReserveAttackBudget(timeSeconds))
            {
                return false;
            }

            activeChargeProfile = ResolveChargeAttackProfile();
            StartReadabilityState(
                EnemyReadabilityState.ChargeWindup,
                ResolvedWindupSeconds(activeChargeProfile, ChargeWindupSeconds),
                timeSeconds,
                delta.normalized);
            return true;
        }

        private void TickChase(float deltaTime, bool allowSteering = true)
        {
            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            MoveInDirection(delta.normalized, deltaTime, 1f, allowSteering);
        }

        private bool TryRangedAttack(float timeSeconds)
        {
            if (!CanStartRangedAttack(timeSeconds))
            {
                return false;
            }

            if (RequiresAttackBudget() && !TryReserveAttackBudget(timeSeconds))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            activeRangedProfile = ResolveRangedAttackProfile(timeSeconds);
            StartReadabilityState(
                EnemyReadabilityState.RangedWindup,
                ResolvedWindupSeconds(activeRangedProfile, RangedWindupSeconds),
                timeSeconds,
                delta.normalized);
            return true;
        }

        private bool TryMeleeLunge(float timeSeconds)
        {
            if (!CanStartMeleeLunge(timeSeconds))
            {
                return false;
            }

            if (RequiresAttackBudget() && !TryReserveMeleeAttackBudget(timeSeconds))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            activeMeleeProfile = ResolveLungeAttackProfile(timeSeconds);
            StartReadabilityState(
                EnemyReadabilityState.MeleeWindup,
                ResolvedWindupSeconds(activeMeleeProfile, LungeWindupSeconds),
                timeSeconds,
                delta.normalized);
            return true;
        }

        private bool CanStartMeleeLunge(float timeSeconds)
        {
            if (!IsAlive ||
                playerController == null ||
                IsInspectionFrozen ||
                IsInEntryGrace(timeSeconds) ||
                bossRuntime != null ||
                behaviorId == EnemyBehaviorId.BossWarden ||
                behaviorId == EnemyBehaviorId.Charger ||
                behaviorId == EnemyBehaviorId.TurretShooter ||
                !LungeAttackEnabled ||
                readabilityState != EnemyReadabilityState.Idle ||
                timeSeconds < nextAllowedLungeTime)
            {
                return false;
            }

            if (!CanUseMeleeLungeForAwareness(timeSeconds))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            var distance = delta.magnitude;
            var minimumReadableRange = radiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.22f;
            return delta.sqrMagnitude >= 0.01f &&
                   distance >= minimumReadableRange &&
                   distance <= LungeTriggerRangeMeters;
        }

        private bool CanUseMeleeLungeForAwareness(float timeSeconds)
        {
            if (Intelligence == EnemyIntelligenceLevel.Instinctive && Disposition == EnemyInstinctDisposition.Prey)
            {
                return IsEndangered(timeSeconds) || awarenessState == EnemyAwarenessState.Engaged;
            }

            if (Disposition == EnemyInstinctDisposition.Mindless)
            {
                return awarenessState is EnemyAwarenessState.Alerted or EnemyAwarenessState.Engaged;
            }

            if (Disposition == EnemyInstinctDisposition.Territorial)
            {
                return awarenessState == EnemyAwarenessState.Engaged &&
                       (IsEndangered(timeSeconds) || timeSeconds - engagedStartTime >= RatTerritorialWarningSeconds);
            }

            return awarenessState == EnemyAwarenessState.Engaged;
        }

        private bool CanStartChargeAttack(float timeSeconds)
        {
            if (playerController == null ||
                readabilityState != EnemyReadabilityState.Idle ||
                timeSeconds < nextAllowedChargeTime)
            {
                return false;
            }

            if (Intelligence == EnemyIntelligenceLevel.Instinctive &&
                Disposition == EnemyInstinctDisposition.Prey &&
                !IsEndangered(timeSeconds) &&
                awarenessState != EnemyAwarenessState.Engaged)
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            return delta.sqrMagnitude >= 0.01f && delta.magnitude <= Definition.AttackRangeMeters;
        }

        private bool CanStartRangedAttack(float timeSeconds)
        {
            if (playerController == null ||
                readabilityState != EnemyReadabilityState.Idle ||
                timeSeconds < nextAllowedAttackTime)
            {
                return false;
            }

            var distance = DistanceToPlayer();
            if (!ShouldSentinelEngage(distance, timeSeconds))
            {
                return false;
            }

            if (Intelligence == EnemyIntelligenceLevel.Instinctive &&
                Disposition == EnemyInstinctDisposition.Prey &&
                !IsEndangered(timeSeconds) &&
                awarenessState != EnemyAwarenessState.Engaged)
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            return delta.sqrMagnitude >= 0.01f && delta.magnitude <= Definition.AttackRangeMeters;
        }

        private bool RequiresAttackBudget()
        {
            return bossRuntime == null && behaviorId != EnemyBehaviorId.BossWarden && archetypeId != EnemyArchetypeId.Boss;
        }

        private bool TryReserveAttackBudget(float timeSeconds)
        {
            return roomCombatController == null || roomCombatController.TryReserveEnemyAttack(this, timeSeconds);
        }

        private bool TryReserveMeleeAttackBudget(float timeSeconds)
        {
            return roomCombatController == null || roomCombatController.TryReserveEnemyMeleeAttack(this, timeSeconds);
        }

        private bool ResolvePendingReadabilityState(float deltaTime, float timeSeconds)
        {
            if (readabilityState == EnemyReadabilityState.EntryGrace)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                return false;
            }

            if (readabilityState == EnemyReadabilityState.ChargeWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                activeChargeDirection = TelegraphDirection;
                chargeEndTime = timeSeconds + ResolvedActiveSeconds(activeChargeProfile, ChargeActiveSeconds);
                nextAllowedChargeTime = timeSeconds + (activeChargeProfile != null ? activeChargeProfile.CooldownSeconds : Definition.ChargeCooldownSeconds);
                chargeContactAttempted = false;
                readabilityState = EnemyReadabilityState.Charging;
                readabilityStateEndTime = chargeEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.Charging)
            {
                if (timeSeconds < chargeEndTime)
                {
                    MoveActiveCharge(deltaTime);
                    TryApplyContactDamage(timeSeconds);
                    return true;
                }

                chargeRecoveryEndTime = timeSeconds + ResolvedRecoverySeconds(activeChargeProfile);
                chargeContactAttempted = false;
                readabilityState = EnemyReadabilityState.ChargeRecovery;
                readabilityStateEndTime = chargeRecoveryEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.ChargeRecovery)
            {
                if (timeSeconds < chargeRecoveryEndTime)
                {
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                chargeEndTime = 0f;
                chargeRecoveryEndTime = 0f;
                activeChargeProfile = null;
                chargeContactAttempted = false;
                return false;
            }

            if (readabilityState == EnemyReadabilityState.RangedWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                var profile = activeRangedProfile ?? ResolveRangedAttackProfile(timeSeconds);
                nextAllowedAttackTime = timeSeconds + (profile != null ? profile.CooldownSeconds : Definition.AttackCooldownSeconds);
                if (!rangedProjectileFired)
                {
                    FireProjectile(TelegraphDirection, profile);
                    rangedProjectileFired = true;
                }

                rangedActiveEndTime = timeSeconds + ResolvedActiveSeconds(profile, 0.08f);
                readabilityState = EnemyReadabilityState.RangedActive;
                readabilityStateEndTime = rangedActiveEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.RangedActive)
            {
                if (timeSeconds < rangedActiveEndTime)
                {
                    return true;
                }

                rangedRecoveryEndTime = timeSeconds + ResolvedRecoverySeconds(activeRangedProfile);
                readabilityState = EnemyReadabilityState.RangedRecovery;
                readabilityStateEndTime = rangedRecoveryEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.RangedRecovery)
            {
                if (timeSeconds < rangedRecoveryEndTime)
                {
                    return true;
                }

                activeRangedProfile = null;
                rangedProjectileFired = false;
                rangedActiveEndTime = 0f;
                rangedRecoveryEndTime = 0f;
                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                return false;
            }

            if (readabilityState == EnemyReadabilityState.MeleeWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                activeLungeDirection = TelegraphDirection;
                var profile = activeMeleeProfile ?? ResolveLungeAttackProfile(timeSeconds);
                lungeEndTime = timeSeconds + ResolvedActiveSeconds(profile, LungeActiveSeconds);
                nextAllowedLungeTime = lungeEndTime + (profile != null ? profile.CooldownSeconds : LungeCooldownSeconds);
                lungeContactAttempted = false;
                readabilityState = EnemyReadabilityState.MeleeLunge;
                readabilityStateEndTime = lungeEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.MeleeLunge)
            {
                if (timeSeconds < lungeEndTime)
                {
                    MoveActiveLunge(deltaTime, timeSeconds);
                    return true;
                }

                lungeRecoveryEndTime = timeSeconds + ResolvedRecoverySeconds(activeMeleeProfile);
                readabilityState = EnemyReadabilityState.MeleeRecovery;
                readabilityStateEndTime = lungeRecoveryEndTime;
                lungeEndTime = 0f;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.MeleeRecovery)
            {
                if (timeSeconds < lungeRecoveryEndTime)
                {
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                lungeRecoveryEndTime = 0f;
                activeMeleeProfile = null;
                lungeContactAttempted = false;
                return false;
            }

            if (readabilityState == EnemyReadabilityState.BossBurstWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                firedLowHealthBossBurst = true;
                var profile = ResolveBossBurstProfile();
                FireProjectile(Vector3.forward, profile);
                FireProjectile(Vector3.back, profile);
                FireProjectile(Vector3.left, profile);
                FireProjectile(Vector3.right, profile);
                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                return true;
            }

            return false;
        }

        private void StartReadabilityState(EnemyReadabilityState state, float durationSeconds, float timeSeconds, Vector3 direction)
        {
            readabilityState = state;
            readabilityStateEndTime = timeSeconds + Mathf.Max(0f, durationSeconds);
            if (state != EnemyReadabilityState.EntryGrace && state != EnemyReadabilityState.Idle)
            {
                VfxPresenter.Play(VfxCueId.EnemyWindup, transform.position, transform.parent);
                AudioPresenter.Play(AudioCueId.EnemyWindup, transform.position);
            }

            if (direction.sqrMagnitude > 0.001f)
            {
                direction.y = 0f;
                telegraphDirection = direction.normalized;
            }
        }

        public void ReceiveStimulus(EnemyStimulusKind kind, Vector3 stimulusLocalPosition, float timeSeconds)
        {
            if (!IsAlive || bossRuntime != null)
            {
                return;
            }

            if (kind == EnemyStimulusKind.Damage || kind == EnemyStimulusKind.Proximity)
            {
                ForceEngaged();
                return;
            }

            var hearingRadius = HearingRadiusMeters * HearingMultiplierFor(kind);
            if (hearingRadius <= 0f)
            {
                return;
            }

            var distance = Vector3.Distance(Flat(transform.localPosition), Flat(stimulusLocalPosition));
            if (distance > hearingRadius)
            {
                return;
            }

            if (kind == EnemyStimulusKind.Footstep)
            {
                if (behaviorId == EnemyBehaviorId.SpittingPod)
                {
                    ForceEngaged();
                    return;
                }

                if (awarenessState == EnemyAwarenessState.Unaware)
                {
                    awarenessState = EnemyAwarenessState.Suspicious;
                }
                else if (awarenessState == EnemyAwarenessState.Suspicious)
                {
                    awarenessState = EnemyAwarenessState.Alerted;
                }
                else if (Disposition == EnemyInstinctDisposition.Territorial && awarenessState == EnemyAwarenessState.Alerted)
                {
                    ForceEngaged();
                }

                return;
            }

            ForceEngaged();
        }

        private void UpdateAwareness(float timeSeconds, float distanceToPlayer)
        {
            if (bossRuntime != null || playerController == null)
            {
                return;
            }

            if (closeThreatTimer >= CloseThreatSeconds)
            {
                ForceEngaged();
                return;
            }

            var canSeePlayer = CanSeePlayer(distanceToPlayer);
            if (canSeePlayer)
            {
                if (Disposition == EnemyInstinctDisposition.Prey && !IsEndangered(timeSeconds))
                {
                    awarenessState = MaxAwareness(awarenessState, EnemyAwarenessState.Alerted);
                    FacePlayer();
                    return;
                }

                if (Disposition == EnemyInstinctDisposition.Sentinel &&
                    !IsEndangered(timeSeconds) &&
                    !IsWithinSentinelApproachRange(distanceToPlayer))
                {
                    awarenessState = MaxAwareness(awarenessState, EnemyAwarenessState.Alerted);
                    FacePlayer();
                    return;
                }

                if (Disposition == EnemyInstinctDisposition.Territorial &&
                    !IsEndangered(timeSeconds) &&
                    distanceToPlayer > PreferredRangeMaxMeters + 0.4f)
                {
                    awarenessState = MaxAwareness(awarenessState, EnemyAwarenessState.Alerted);
                    FacePlayer();
                    return;
                }

                if (awarenessState == EnemyAwarenessState.Unaware)
                {
                    awarenessState = EnemyAwarenessState.Alerted;
                }
                else
                {
                    ForceEngaged();
                }
            }

            if (awarenessState is EnemyAwarenessState.Alerted or EnemyAwarenessState.Engaged)
            {
                FacePlayer();
            }
        }

        private bool CanSeePlayer(float distanceToPlayer)
        {
            if (playerController == null || SightRadiusMeters <= 0f || SightAngleDegrees <= 0f || distanceToPlayer > SightRadiusMeters)
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return true;
            }

            var angle = Vector3.Angle(FacingDirection, delta.normalized);
            return angle <= SightAngleDegrees * 0.5f;
        }

        private void FacePlayer()
        {
            if (playerController == null)
            {
                return;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f)
            {
                facingDirection = delta.normalized;
            }
        }

        private void ForceEngaged()
        {
            if (awarenessState != EnemyAwarenessState.Engaged)
            {
                engagedStartTime = lastTickTime > 0f ? lastTickTime : Time.time;
            }

            awarenessState = EnemyAwarenessState.Engaged;
        }

        private static EnemyAwarenessState MaxAwareness(EnemyAwarenessState left, EnemyAwarenessState right)
        {
            return (EnemyAwarenessState)Mathf.Max((int)left, (int)right);
        }

        private static EnemyAwarenessState InitialAwarenessFor(EnemyInstinctDisposition nextDisposition)
        {
            return nextDisposition switch
            {
                EnemyInstinctDisposition.Prey => EnemyAwarenessState.Unaware,
                EnemyInstinctDisposition.Sentinel => EnemyAwarenessState.Alerted,
                EnemyInstinctDisposition.Mindless => EnemyAwarenessState.Alerted,
                EnemyInstinctDisposition.Territorial => EnemyAwarenessState.Suspicious,
                _ => EnemyAwarenessState.Engaged
            };
        }

        private static float HearingMultiplierFor(EnemyStimulusKind kind)
        {
            return kind switch
            {
                EnemyStimulusKind.Footstep => 0.75f,
                EnemyStimulusKind.MeleeAttack => 1.05f,
                EnemyStimulusKind.RangedAttack => 1.25f,
                _ => 1f
            };
        }

        private void UpdateInstinctThreat(float deltaTime, float distanceToPlayer)
        {
            if (distanceToPlayer <= CloseThreatDistanceMeters)
            {
                closeThreatTimer = Mathf.Min(CloseThreatSeconds, closeThreatTimer + Mathf.Max(0f, deltaTime));
                return;
            }

            closeThreatTimer = Mathf.Max(0f, closeThreatTimer - Mathf.Max(0f, deltaTime) * 1.5f);
        }

        private bool IsEndangered(float timeSeconds)
        {
            return timeSeconds - lastDamagedTime <= RecentDamageEndangeredSeconds || closeThreatTimer >= CloseThreatSeconds;
        }

        private bool ShouldSentinelEngage(float distanceToPlayer, float timeSeconds)
        {
            if (Disposition != EnemyInstinctDisposition.Sentinel)
            {
                return true;
            }

            if (awarenessState == EnemyAwarenessState.Engaged || IsEndangered(timeSeconds))
            {
                return true;
            }

            return IsWithinSentinelApproachRange(distanceToPlayer);
        }

        private bool IsWithinSentinelApproachRange(float distanceToPlayer)
        {
            var approachRange = behaviorId == EnemyBehaviorId.TurretShooter
                ? Mathf.Min(Definition.AttackRangeMeters, 5.5f)
                : Mathf.Max(2.2f, Definition.AttackRangeMeters * 0.55f);
            return distanceToPlayer <= approachRange;
        }

        private void TickSentinelHold(float deltaTime)
        {
            var homeDelta = homeLocalPosition - transform.localPosition;
            homeDelta.y = 0f;
            if (homeDelta.sqrMagnitude <= 0.0025f)
            {
                return;
            }

            MoveInDirection(homeDelta.normalized, deltaTime, 0.55f);
        }

        private bool ShouldMindlessWander(float timeSeconds, float distanceToPlayer)
        {
            if (distanceToPlayer <= 2f)
            {
                return false;
            }

            return Mathf.FloorToInt(timeSeconds / 1.4f) % 5 == 0;
        }

        private Vector3 ResolveInstinctWanderDirection(float timeSeconds)
        {
            if (timeSeconds >= nextInstinctDecisionTime || instinctMoveDirection.sqrMagnitude <= 0.01f)
            {
                var step = Mathf.FloorToInt(timeSeconds * 0.7f);
                var angle = Mathf.Abs(spawnIndex * 37 + step * 91) % 360;
                instinctMoveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                nextInstinctDecisionTime = timeSeconds + 0.9f;
            }

            return instinctMoveDirection.normalized;
        }

        private Vector3 ResolveCritterWanderDirection(float timeSeconds)
        {
            if (timeSeconds >= nextInstinctDecisionTime || instinctMoveDirection.sqrMagnitude <= 0.01f)
            {
                var step = Mathf.FloorToInt(timeSeconds * 2.8f);
                var spawnSeed = Mathf.Max(1, spawnIndex + 3);
                var angle = Mathf.Abs(spawnSeed * 73 + step * 137 + (behaviorId == EnemyBehaviorId.Spider ? 41 : 0)) % 360;
                instinctMoveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                nextInstinctDecisionTime = timeSeconds + (behaviorId == EnemyBehaviorId.Spider ? 0.28f : 0.36f);
            }

            return instinctMoveDirection.normalized;
        }

        private Vector3 ResolvePreyMovementDirection(Vector3 toPlayerDirection, float distanceToPlayer, float timeSeconds)
        {
            if (timeSeconds < retreatBurstEndTime)
            {
                return -toPlayerDirection;
            }

            if (distanceToPlayer <= PreferredRangeMaxMeters && timeSeconds >= nextRetreatBurstAllowedTime)
            {
                retreatBurstEndTime = timeSeconds + RetreatBurstSeconds;
                nextRetreatBurstAllowedTime = retreatBurstEndTime + RetreatReassessSeconds;
                return -toPlayerDirection;
            }

            return ResolveInstinctWanderDirection(timeSeconds);
        }

        private bool UsesDirectInstinctPressure()
        {
            return Intelligence == EnemyIntelligenceLevel.Instinctive ||
                   Disposition == EnemyInstinctDisposition.Mindless;
        }

        private bool IsCritterBehavior()
        {
            return behaviorId == EnemyBehaviorId.Rat || behaviorId == EnemyBehaviorId.Spider;
        }

        private Vector3 ResolvePreferredRangeDirection(Vector3 toPlayerDirection, float distanceToPlayer)
        {
            var min = PreferredRangeMinMeters;
            var max = PreferredRangeMaxMeters;
            var slack = PreferredRangeSlack();
            if (distanceToPlayer < min)
            {
                return -toPlayerDirection;
            }

            if (distanceToPlayer > max + slack)
            {
                return toPlayerDirection;
            }

            if (Intelligence == EnemyIntelligenceLevel.Simple && distanceToPlayer > min + slack)
            {
                return toPlayerDirection;
            }

            return Vector3.zero;
        }

        private float RangeIntentSpeedMultiplier(Vector3 direction, Vector3 toPlayerDirection, float distanceToPlayer)
        {
            if (direction.sqrMagnitude <= 0.01f)
            {
                return 0.5f;
            }

            var dotToPlayer = Vector3.Dot(direction.normalized, toPlayerDirection);
            if (dotToPlayer < -0.25f)
            {
                return 0.65f;
            }

            if (distanceToPlayer >= PreferredRangeMinMeters && distanceToPlayer <= PreferredRangeMaxMeters)
            {
                return 0.55f;
            }

            return 1f;
        }

        private float PreferredRangeSlack()
        {
            return Intelligence switch
            {
                EnemyIntelligenceLevel.Cunning => 0.05f,
                EnemyIntelligenceLevel.Tactical => 0.08f,
                EnemyIntelligenceLevel.Trained => 0.12f,
                EnemyIntelligenceLevel.Basic => 0.18f,
                EnemyIntelligenceLevel.Simple => 0.45f,
                _ => 0.65f
            };
        }

        private float ResolvedWindupSeconds(EnemyAttackProfileDefinition profile, float fallbackSeconds)
        {
            var baseSeconds = profile != null ? profile.WindupSeconds : fallbackSeconds;
            return Mathf.Max(0f, baseSeconds * AttackWindupScale);
        }

        private float ResolvedActiveSeconds(EnemyAttackProfileDefinition profile, float fallbackSeconds)
        {
            var baseSeconds = profile != null ? profile.ActiveSeconds : fallbackSeconds;
            return Mathf.Max(0.01f, baseSeconds * AttackActiveScale);
        }

        private float ResolvedRecoverySeconds(EnemyAttackProfileDefinition profile)
        {
            var baseSeconds = profile != null ? profile.RecoverySeconds : 0.12f;
            return Mathf.Max(0.01f, baseSeconds * AttackRecoveryScale);
        }

        private float ResolvedHitArcDegrees(EnemyAttackProfileDefinition profile)
        {
            var baseDegrees = profile != null
                ? profile.HitArcDegrees
                : EnemyAttackProfileDefinition.DefaultHitArcDegrees(EnemyAttackRuntimeKind.MeleeLunge, DamageDelivery.Melee);
            return Mathf.Clamp(baseDegrees + HitArcDegreesBonus, 1f, 360f);
        }

        private ImpactForceClass ResolvedPoiseBreakThreshold(EnemyAttackProfileDefinition profile)
        {
            var baseValue = (int)(profile != null ? profile.PoiseBreakThreshold : ImpactForceClass.Medium);
            return (ImpactForceClass)Mathf.Clamp(baseValue + PoiseBreakThresholdOffset, (int)ImpactForceClass.Light, (int)ImpactForceClass.Massive);
        }

        private void MoveActiveCharge(float deltaTime)
        {
            var direction = activeChargeDirection.sqrMagnitude > 0.001f ? activeChargeDirection.normalized : TelegraphDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            facingDirection = direction.normalized;
            var desired = transform.localPosition + direction.normalized * Definition.ChargeSpeedMetersPerSecond * Mathf.Max(0f, deltaTime);
            transform.localPosition = RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);
        }

        private void MoveActiveLunge(float deltaTime, float timeSeconds)
        {
            var direction = activeLungeDirection.sqrMagnitude > 0.001f ? activeLungeDirection.normalized : TelegraphDirection;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            direction.y = 0f;
            facingDirection = direction.normalized;
            var activeSeconds = ResolvedActiveSeconds(activeMeleeProfile, LungeActiveSeconds);
            var speed = LungeDistanceMeters / activeSeconds;
            var desired = transform.localPosition + direction.normalized * speed * Mathf.Max(0f, deltaTime);
            transform.localPosition = movementMode == EnemyMovementMode.Flying
                ? RoomLocalCollision.ResolveFlyingMove(roomRuntimeRoot, desired, radiusMeters)
                : RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);
            if (!lungeContactAttempted && TryApplyContactDamage(timeSeconds))
            {
                lungeContactAttempted = true;
            }
        }

        private void MoveInDirection(Vector3 direction, float deltaTime, float speedMultiplier, bool allowSteering = true)
        {
            direction.y = 0f;
            var movementDirection = allowSteering ? ResolveLocalSteeringDirection(direction) : direction.normalized;
            if (movementDirection.sqrMagnitude <= 0.01f)
            {
                return;
            }

            if (awarenessState is EnemyAwarenessState.Alerted or EnemyAwarenessState.Engaged)
            {
                FacePlayer();
            }
            else
            {
                facingDirection = movementDirection.normalized;
            }
            var desired = transform.localPosition + movementDirection.normalized * speedMetersPerSecond * Mathf.Max(0f, speedMultiplier) * deltaTime;
            transform.localPosition = movementMode == EnemyMovementMode.Flying
                ? RoomLocalCollision.ResolveFlyingMove(roomRuntimeRoot, desired, radiusMeters)
                : RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);
        }

        private Vector3 ResolveLocalSteeringDirection(Vector3 intentDirection)
        {
            intentDirection.y = 0f;
            var result = intentDirection.sqrMagnitude > 0.01f ? intentDirection.normalized : Vector3.zero;
            var separation = ResolveEnemySeparationDirection();
            if (separation.sqrMagnitude > 0.01f)
            {
                result = result.sqrMagnitude > 0.01f
                    ? (result + separation.normalized * SeparationWeight()).normalized
                    : separation.normalized;
            }

            var playerDelta = playerController != null
                ? playerController.transform.localPosition - transform.localPosition
                : Vector3.zero;
            playerDelta.y = 0f;
            if (playerDelta.sqrMagnitude <= 0.01f || result.sqrMagnitude <= 0.01f)
            {
                return result;
            }

            var toPlayer = playerDelta.normalized;
            var contactBufferDistance = radiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.22f;
            if (playerDelta.magnitude > contactBufferDistance)
            {
                return result;
            }

            var towardPlayer = Vector3.Dot(result, toPlayer);
            if (towardPlayer <= 0f)
            {
                return result;
            }

            var lateral = result - toPlayer * towardPlayer;
            var buffered = lateral + -toPlayer * 0.35f;
            return buffered.sqrMagnitude > 0.01f ? buffered.normalized : -toPlayer;
        }

        private Vector3 ResolveEnemySeparationDirection()
        {
            if (roomCombatController == null)
            {
                return Vector3.zero;
            }

            var away = Vector3.zero;
            foreach (var enemy in roomCombatController.Enemies)
            {
                if (enemy == null || enemy == this || !enemy.IsAlive || enemy.BossDefinition != null)
                {
                    continue;
                }

                var delta = transform.localPosition - enemy.transform.localPosition;
                delta.y = 0f;
                var distance = delta.magnitude;
                var separationDistance = radiusMeters + enemy.RadiusMeters + 0.35f;
                if (distance <= 0.001f || distance >= separationDistance)
                {
                    continue;
                }

                away += delta.normalized * (1f - distance / separationDistance);
            }

            return away;
        }

        private float SeparationWeight()
        {
            return Intelligence switch
            {
                EnemyIntelligenceLevel.Cunning => 0.65f,
                EnemyIntelligenceLevel.Tactical => 0.58f,
                EnemyIntelligenceLevel.Trained => 0.52f,
                EnemyIntelligenceLevel.Basic => 0.45f,
                EnemyIntelligenceLevel.Simple => 0.35f,
                _ => 0.25f
            };
        }

        private float DistanceToPlayer()
        {
            if (playerController == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(Flat(transform.localPosition), Flat(playerController.transform.localPosition));
        }

        public bool TryApplyContactDamage(float timeSeconds)
        {
            if (!IsAlive || IsInspectionFrozen || playerHealth == null || !playerHealth.IsAlive || IsInEntryGrace(timeSeconds))
            {
                return false;
            }

            var distance = Vector3.Distance(Flat(transform.localPosition), Flat(playerController.transform.localPosition));
            var contactReach = BodyContactReachMeters();
            if (readabilityState == EnemyReadabilityState.MeleeLunge)
            {
                contactReach = Mathf.Max(contactReach, activeMeleeProfile != null ? activeMeleeProfile.RangeMeters : LungeTriggerRangeMeters);
            }

            if (distance > contactReach)
            {
                return false;
            }

            if (readabilityState == EnemyReadabilityState.MeleeLunge && !IsPlayerInsideActiveHitArc())
            {
                return false;
            }

            if (!CanApplyBodyDamageNow(timeSeconds))
            {
                TryApplyBumpDisturbance(timeSeconds);
                return false;
            }

            if (timeSeconds < nextAllowedContactTime)
            {
                return false;
            }

            nextAllowedContactTime = timeSeconds + contactCooldownSeconds;
            MarkBodyContactDamageAttempted();
            var feelProfile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
            var attackProfile = ResolveContactAttackProfile();
            var direction = playerController.transform.localPosition - transform.localPosition;
            var damaged = DamageSystem.ApplyDamage(
                playerHealth,
                attackProfile != null
                    ? attackProfile.CreateDamageRequest(gameObject, direction, feelProfile.KnockbackSeconds)
                    : new DamageRequest(
                    contactDamage,
                    gameObject,
                    DamageFeedbackContext.Knockback(direction, feelProfile.PlayerKnockbackMeters, feelProfile.KnockbackSeconds),
                    ContactThreatKind(),
                    DamageClassification.PhysicalContact(ForceClassForThreat(ContactThreatKind()))));
            if (damaged)
            {
                VfxPresenter.Play(VfxCueId.PlayerHit, playerController.transform.position, playerController.transform.parent);
                AudioPresenter.Play(AudioCueId.PlayerHit, playerController.transform.position);
            }

            return damaged;
        }

        public EnemyRuntimeController SpawnChildEnemy(string spawnKind, Vector3 localPosition)
        {
            if (enemyPrefab == null ||
                roomRuntimeRoot == null ||
                playerController == null)
            {
                return null;
            }

            var catalog = enemyCatalog != null ? enemyCatalog : EnemyCatalog.CreateRuntimeDefault();
            var definition = EnemyDefinitionResolver.Resolve(catalog, spawnKind, out _);
            var difficulty = difficultyTier != null ? difficultyTier : DifficultyTierDefinition.CreateRuntimeDeveloperSample();
            var childObject = Instantiate(enemyPrefab, transform.parent);
            childObject.name = $"Enemy.BossSummon.{definition.SpawnKind}";
            childObject.SetActive(true);
            childObject.transform.localPosition = RoomLocalCollision.ResolveMoveIgnoringObstacles(roomRuntimeRoot, localPosition, definition.RadiusMeters);
            var child = childObject.GetComponent<EnemyRuntimeController>() ?? childObject.AddComponent<EnemyRuntimeController>();
            child.Configure(roomRuntimeRoot, playerController, definition, difficulty);
            child.ConfigureSpawnContext(enemyPrefab, enemyProjectilePrefab, catalog, difficulty, diagnostics);
            child.BeginEntryGrace(RoomCombatController.EntryGraceSeconds, Time.time);
            SpawnedChild?.Invoke(child);
            return child;
        }

        private void OnDied(CombatantHealth _)
        {
            SpawnSplitChildren();
            CorpseGhostPresenter.SpawnFrom(this, combatFeelProfile);
            VfxPresenter.Play(VfxCueId.EnemyDeath, transform.position, transform.parent);
            AudioPresenter.Play(AudioCueId.EnemyDeath, transform.position);
            gameObject.SetActive(false);
        }

        private void OnDamaged(CombatantHealth _)
        {
            lastDamagedTime = lastTickTime > 0f ? lastTickTime : Time.time;
            ForceEngaged();
        }

        public int ModifyIncomingDamage(DamageRequest request, int currentAmount)
        {
            if (currentAmount <= 0 || bossRuntime != null || !IsAlive || !IsPlayerAuthoredDamageSource(request.Source))
            {
                return currentAmount;
            }

            var profile = ActiveWindupProfile();
            if (profile == null || !IsInterruptibleWindup())
            {
                return currentAmount;
            }

            if ((int)request.Classification.ForceClass < (int)ResolvedPoiseBreakThreshold(profile))
            {
                return currentAmount;
            }

            InterruptWindupIntoRecovery(profile);
            return currentAmount;
        }

        private bool IsInterruptibleWindup()
        {
            return readabilityState is EnemyReadabilityState.MeleeWindup
                or EnemyReadabilityState.ChargeWindup
                or EnemyReadabilityState.RangedWindup;
        }

        private EnemyAttackProfileDefinition ActiveWindupProfile()
        {
            return readabilityState switch
            {
                EnemyReadabilityState.MeleeWindup => activeMeleeProfile,
                EnemyReadabilityState.ChargeWindup => activeChargeProfile,
                EnemyReadabilityState.RangedWindup => activeRangedProfile,
                _ => null
            };
        }

        private void InterruptWindupIntoRecovery(EnemyAttackProfileDefinition profile)
        {
            var timeSeconds = lastTickTime > 0f ? lastTickTime : Time.time;
            var recoveryEnd = timeSeconds + ResolvedRecoverySeconds(profile);
            if (readabilityState == EnemyReadabilityState.MeleeWindup)
            {
                readabilityState = EnemyReadabilityState.MeleeRecovery;
                lungeRecoveryEndTime = recoveryEnd;
                lungeEndTime = 0f;
                lungeContactAttempted = false;
            }
            else if (readabilityState == EnemyReadabilityState.ChargeWindup)
            {
                readabilityState = EnemyReadabilityState.ChargeRecovery;
                chargeRecoveryEndTime = recoveryEnd;
                chargeEndTime = 0f;
                chargeContactAttempted = false;
            }
            else if (readabilityState == EnemyReadabilityState.RangedWindup)
            {
                readabilityState = EnemyReadabilityState.RangedRecovery;
                rangedRecoveryEndTime = recoveryEnd;
                rangedActiveEndTime = 0f;
                rangedProjectileFired = false;
            }

            readabilityStateEndTime = recoveryEnd;
        }

        private static bool IsPlayerAuthoredDamageSource(GameObject source)
        {
            return source != null &&
                   (source.GetComponent<PlayerWeaponController>() != null ||
                    source.GetComponent<ProjectileController>() != null);
        }

        private void FireProjectile(Vector3 direction, EnemyAttackProfileDefinition profile)
        {
            var projectileObject = enemyProjectilePrefab != null
                ? Instantiate(enemyProjectilePrefab, transform.parent)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = $"EnemyProjectile.{Definition.SpawnKind}";
            projectileObject.transform.SetParent(transform.parent, worldPositionStays: false);
            projectileObject.transform.localPosition = transform.localPosition + direction.normalized * (radiusMeters + 0.22f) + new Vector3(0f, 0.35f, 0f);
            projectileObject.transform.localScale = Vector3.one * 0.22f;
            var playerProjectile = projectileObject.GetComponent<ProjectileController>();
            if (playerProjectile != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(playerProjectile);
                }
                else
                {
                    DestroyImmediate(playerProjectile);
                }
            }

            var collider = projectileObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var projectile = projectileObject.GetComponent<EnemyProjectileController>() ?? projectileObject.AddComponent<EnemyProjectileController>();
            projectile.Configure(
                roomRuntimeRoot,
                playerController,
                direction,
                profile != null ? profile.Damage : Definition.ProjectileDamage,
                profile != null ? profile.ProjectileSpeedMetersPerSecond : Definition.ProjectileSpeedMetersPerSecond);
            projectile.ConfigureCombatFeel(combatFeelProfile);
            if (profile != null)
            {
                projectile.ConfigureAttackProfile(profile);
                if (behaviorId == EnemyBehaviorId.SpittingPod && string.Equals(profile.AttackId, "spit_lob", StringComparison.Ordinal))
                {
                    var target = playerController != null ? playerController.transform.localPosition : transform.localPosition + direction.normalized * Definition.AttackRangeMeters;
                    projectile.ConfigureBallisticLanding(
                        target,
                        profile.ActiveSeconds,
                        PodBallisticArcHeightMeters,
                        PodBallisticSplashRadiusMeters);
                }
            }
            else
            {
                projectile.ConfigureThreat(behaviorId == EnemyBehaviorId.BossWarden ? DamageThreatKind.StrongProjectile : DamageThreatKind.Light);
            }
        }

        private EnemyAttackProfileDefinition ResolveContactAttackProfile()
        {
            if (bossDefinition != null)
            {
                return activeBossContactProfile != null
                    ? activeBossContactProfile
                    : bossDefinition.ResolveAttackProfile(BossContactAttackId());
            }

            if (readabilityState == EnemyReadabilityState.MeleeLunge && activeMeleeProfile != null)
            {
                return activeMeleeProfile;
            }

            if (readabilityState == EnemyReadabilityState.Charging && activeChargeProfile != null)
            {
                return activeChargeProfile;
            }

            return Definition.ResolveAttackProfile(ContactAttackId());
        }

        private float BodyContactReachMeters()
        {
            return radiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.12f;
        }

        private bool IsPlayerInsideActiveHitArc()
        {
            if (playerController == null)
            {
                return false;
            }

            var toPlayer = playerController.transform.localPosition - transform.localPosition;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= 0.001f)
            {
                return true;
            }

            var forward = activeLungeDirection.sqrMagnitude > 0.001f ? activeLungeDirection : TelegraphDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = FacingDirection;
            }

            var arc = ResolvedHitArcDegrees(activeMeleeProfile);
            return Vector3.Angle(forward.normalized, toPlayer.normalized) <= arc * 0.5f;
        }

        private bool CanApplyBodyDamageNow(float timeSeconds)
        {
            if (IsPassiveHazardBody())
            {
                return true;
            }

            if (contactDamagePolicy != EnemyContactDamagePolicy.ActiveOnly)
            {
                return false;
            }

            if (readabilityState == EnemyReadabilityState.MeleeLunge)
            {
                return !lungeContactAttempted;
            }

            if (readabilityState == EnemyReadabilityState.Charging)
            {
                return !chargeContactAttempted;
            }

            if (bossDefinition != null)
            {
                if (timeSeconds > bossActiveContactEndTime)
                {
                    activeBossContactProfile = null;
                    return false;
                }

                return activeBossContactProfile != null && !bossActiveContactAttempted;
            }

            return false;
        }

        private bool IsPassiveHazardBody()
        {
            return contactDamagePolicy == EnemyContactDamagePolicy.PassiveHazard &&
                   passiveContactHazardType != EnemyPassiveContactHazardType.None;
        }

        private void TryApplyBumpDisturbance(float timeSeconds)
        {
            if (bossRuntime != null || IsPassiveHazardBody() || playerController == null)
            {
                return;
            }

            ReceiveStimulus(EnemyStimulusKind.Proximity, playerController.transform.localPosition, timeSeconds);
        }

        private void MarkBodyContactDamageAttempted()
        {
            if (IsPassiveHazardBody())
            {
                return;
            }

            if (readabilityState == EnemyReadabilityState.MeleeLunge)
            {
                lungeContactAttempted = true;
            }
            else if (readabilityState == EnemyReadabilityState.Charging)
            {
                chargeContactAttempted = true;
            }
            else if (bossDefinition != null && activeBossContactProfile != null)
            {
                bossActiveContactAttempted = true;
            }
        }

        private EnemyAttackProfileDefinition ResolveLungeAttackProfile(float timeSeconds)
        {
            if (behaviorId == EnemyBehaviorId.FlyingChaser)
            {
                return Definition.ResolveAttackProfile(awarenessState == EnemyAwarenessState.Engaged || IsEndangered(timeSeconds)
                    ? "dive_scratch"
                    : "panic_peck");
            }

            if (behaviorId == EnemyBehaviorId.Rat)
            {
                return Definition.ResolveAttackProfile("rat_bite");
            }

            if (behaviorId == EnemyBehaviorId.Spider)
            {
                return Definition.ResolveAttackProfile(DistanceToPlayer() <= 0.82f ? "close_bite" : "startle_hop");
            }

            return behaviorId switch
            {
                EnemyBehaviorId.Splitter => Definition.ResolveAttackProfile("splinter_lunge"),
                _ when archetypeId == EnemyArchetypeId.Fast => Definition.ResolveAttackProfile("quick_pounce"),
                _ when archetypeId == EnemyArchetypeId.Heavy => Definition.ResolveAttackProfile("maul_lunge"),
                _ => Definition.ResolveAttackProfile("claw_lunge")
            };
        }

        private EnemyAttackProfileDefinition ResolveChargeAttackProfile()
        {
            return Definition.ResolveAttackProfile(behaviorId == EnemyBehaviorId.Charger ? "ash_charge" : ContactAttackId());
        }

        private EnemyAttackProfileDefinition ResolveRangedAttackProfile(float timeSeconds)
        {
            if (behaviorId == EnemyBehaviorId.SpittingPod)
            {
                return Definition.ResolveAttackProfile("spit_lob");
            }

            if (behaviorId != EnemyBehaviorId.TurretShooter)
            {
                return Definition.ResolveAttackProfile("bone_dart");
            }

            var safeSpawnIndex = Mathf.Max(0, spawnIndex);
            var slot = Mathf.Abs(Mathf.FloorToInt(timeSeconds * 0.7f) + safeSpawnIndex) % 5;
            if (slot == 0)
            {
                return Definition.ResolveAttackProfile("braced_spike");
            }

            if (slot == 2)
            {
                return Definition.ResolveAttackProfile("rattle_volley");
            }

            return Definition.ResolveAttackProfile("bone_dart");
        }

        private EnemyAttackProfileDefinition ResolveBossBurstProfile()
        {
            return bossDefinition != null
                ? bossDefinition.ResolveAttackProfile("stone_four_way_burst")
                : Definition.ResolveAttackProfile("stone_four_way_burst");
        }

        private string ContactAttackId()
        {
            if (behaviorId == EnemyBehaviorId.BossWarden || archetypeId == EnemyArchetypeId.Boss)
            {
                return "stone_charge";
            }

            if (behaviorId == EnemyBehaviorId.Charger)
            {
                return readabilityState == EnemyReadabilityState.Charging ? "ash_charge" : "ember_clash";
            }

            if (behaviorId == EnemyBehaviorId.FlyingChaser)
            {
                return "panic_peck";
            }

            if (behaviorId == EnemyBehaviorId.Splitter)
            {
                return "husk_cleave";
            }

            if (behaviorId == EnemyBehaviorId.Rat)
            {
                return "rat_bite";
            }

            if (behaviorId == EnemyBehaviorId.Spider)
            {
                return "close_bite";
            }

            if (archetypeId == EnemyArchetypeId.Fast)
            {
                return "needle_rush";
            }

            if (archetypeId == EnemyArchetypeId.Heavy)
            {
                return "body_slam";
            }

            return "desperate_bite";
        }

        private string BossContactAttackId()
        {
            return bossDefinition != null
                ? bossDefinition.BehaviorId switch
                {
                    BossBehaviorId.AshComet => "ash_comet_dash",
                    BossBehaviorId.MirrorHusk => "mirror_chase_contact",
                    BossBehaviorId.IronReliquary => "iron_relocate_bash",
                    _ => "stone_charge"
                }
                : "stone_charge";
        }

        private DamageThreatKind ContactThreatKind()
        {
            if (behaviorId == EnemyBehaviorId.BossWarden || archetypeId == EnemyArchetypeId.Boss)
            {
                return DamageThreatKind.Boss;
            }

            if (behaviorId == EnemyBehaviorId.Charger || archetypeId == EnemyArchetypeId.Heavy || readabilityState == EnemyReadabilityState.Charging)
            {
                return DamageThreatKind.Heavy;
            }

            return DamageThreatKind.Light;
        }

        private static ImpactForceClass ForceClassForThreat(DamageThreatKind threatKind)
        {
            return threatKind switch
            {
                DamageThreatKind.Boss => ImpactForceClass.Massive,
                DamageThreatKind.Heavy or DamageThreatKind.StrongProjectile => ImpactForceClass.Heavy,
                DamageThreatKind.Environmental => ImpactForceClass.Medium,
                _ => ImpactForceClass.Light
            };
        }

        private void SpawnSplitChildren()
        {
            if (IsInspectionFrozen ||
                behaviorId != EnemyBehaviorId.Splitter ||
                Definition.SplitCount <= 0 ||
                enemyPrefab == null ||
                roomRuntimeRoot == null ||
                playerController == null)
            {
                return;
            }

            var catalog = enemyCatalog != null ? enemyCatalog : EnemyCatalog.CreateRuntimeDefault();
            var definition = EnemyDefinitionResolver.Resolve(catalog, Definition.SplitSpawnKind, out _);
            var difficulty = difficultyTier != null ? difficultyTier : DifficultyTierDefinition.CreateRuntimeDeveloperSample();
            var angleStep = 360f / Definition.SplitCount;
            for (var index = 0; index < Definition.SplitCount; index++)
            {
                var childObject = Instantiate(enemyPrefab, transform.parent);
                childObject.name = $"Enemy.Split.{index:00}.{definition.SpawnKind}";
                childObject.SetActive(true);
                var offset = Quaternion.Euler(0f, angleStep * index, 0f) * Vector3.forward * 0.48f;
                childObject.transform.localPosition = RoomLocalCollision.ResolveMoveIgnoringObstacles(roomRuntimeRoot, transform.localPosition + offset, definition.RadiusMeters);
                var child = childObject.GetComponent<EnemyRuntimeController>() ?? childObject.AddComponent<EnemyRuntimeController>();
                child.Configure(roomRuntimeRoot, playerController, definition, difficulty);
                child.ConfigureSpawnContext(enemyPrefab, enemyProjectilePrefab, catalog, difficulty, diagnostics);
                child.BeginEntryGrace(RoomCombatController.EntryGraceSeconds, Time.time);
                SpawnedChild?.Invoke(child);
            }
        }

        private void ApplyVisualMaterial(MaterialRole role)
        {
            var renderer = GetComponentInChildren<Renderer>();
            MaterialResolver.ApplyTo(renderer, role);
        }

        private static MaterialRole RoleForDefinition(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return MaterialRole.EnemyNormal;
            }

            return definition.BehaviorId switch
            {
                EnemyBehaviorId.Charger => MaterialRole.EnemyCharger,
                EnemyBehaviorId.TurretShooter => MaterialRole.EnemyTurret,
                EnemyBehaviorId.Splitter => MaterialRole.EnemySplitter,
                EnemyBehaviorId.SpittingPod => MaterialRole.EnemySpittingPod,
                EnemyBehaviorId.Rat => MaterialRole.EnemyRat,
                EnemyBehaviorId.Spider => MaterialRole.EnemySpider,
                EnemyBehaviorId.BossWarden => MaterialRole.EnemyBoss,
                EnemyBehaviorId.FlyingChaser => MaterialRole.EnemyFlying,
                _ => definition.ArchetypeId switch
                {
                    EnemyArchetypeId.Fast => MaterialRole.EnemyFast,
                    EnemyArchetypeId.Heavy => MaterialRole.EnemyHeavy,
                    EnemyArchetypeId.Boss => MaterialRole.EnemyBoss,
                    _ => MaterialRole.EnemyNormal
                }
            };
        }

        private static PresentationPrefabRole PrefabRoleForDefinition(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return PresentationPrefabRole.EnemyNormal;
            }

            return definition.BehaviorId switch
            {
                EnemyBehaviorId.Charger => PresentationPrefabRole.EnemyCharger,
                EnemyBehaviorId.TurretShooter => PresentationPrefabRole.EnemyTurret,
                EnemyBehaviorId.Splitter => PresentationPrefabRole.EnemySplitter,
                EnemyBehaviorId.SpittingPod => PresentationPrefabRole.EnemySpittingPod,
                EnemyBehaviorId.Rat => PresentationPrefabRole.EnemyRat,
                EnemyBehaviorId.Spider => PresentationPrefabRole.EnemySpider,
                EnemyBehaviorId.BossWarden => PresentationPrefabRole.EnemyBoss,
                EnemyBehaviorId.FlyingChaser => PresentationPrefabRole.EnemyFlying,
                _ => definition.ArchetypeId switch
                {
                    EnemyArchetypeId.Fast => PresentationPrefabRole.EnemyFast,
                    EnemyArchetypeId.Heavy => PresentationPrefabRole.EnemyHeavy,
                    EnemyArchetypeId.Boss => PresentationPrefabRole.EnemyBoss,
                    _ => PresentationPrefabRole.EnemyNormal
                }
            };
        }

        private static Vector3 Flat(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }
    }
}
