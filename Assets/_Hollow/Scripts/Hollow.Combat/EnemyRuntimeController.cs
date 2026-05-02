using Hollow.Entities;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using System;
using UnityEngine;

namespace Hollow.Combat
{
    public class EnemyRuntimeController : MonoBehaviour
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

        [SerializeField] private float speedMetersPerSecond = ChaserEnemyController.DefaultSpeedMetersPerSecond;
        [SerializeField] private int contactDamage = ChaserEnemyController.DefaultContactDamage;
        [SerializeField] private float contactCooldownSeconds = ChaserEnemyController.DefaultContactCooldownSeconds;
        [SerializeField] private float radiusMeters = 0.32f;
        [SerializeField] private float preferredRangeMinMeters = 1.05f;
        [SerializeField] private float preferredRangeMaxMeters = 1.75f;
        [SerializeField] private EnemyArchetypeId archetypeId = EnemyArchetypeId.Normal;
        [SerializeField] private EnemyBehaviorId behaviorId = EnemyBehaviorId.Chaser;
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.Grounded;
        [SerializeField] private EnemyBodyClass bodyClass = EnemyBodyClass.Medium;
        [SerializeField] private EnemyIntelligenceLevel intelligence = EnemyIntelligenceLevel.Simple;
        [SerializeField] private EnemyInstinctDisposition disposition = EnemyInstinctDisposition.Predator;

        private RoomRuntimeRoot roomRuntimeRoot;
        private PlaceholderPlayerController playerController;
        private CombatantHealth playerHealth;
        private float nextAllowedContactTime;
        private float nextAllowedAttackTime;
        private float nextAllowedChargeTime;
        private float chargeEndTime;
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

        public int SpawnIndex => spawnIndex;

        public float SpeedMetersPerSecond => speedMetersPerSecond;

        public int ContactDamage => contactDamage;

        public float RadiusMeters => radiusMeters;

        public float PreferredRangeMinMeters => Mathf.Max(0f, preferredRangeMinMeters);

        public float PreferredRangeMaxMeters => Mathf.Max(PreferredRangeMinMeters + 0.05f, preferredRangeMaxMeters);

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
            speedMetersPerSecond = tuning.ApplySpeed(Definition.SpeedMetersPerSecond);
            contactDamage = tuning.ApplyContactDamage(Definition.ContactDamage);
            contactCooldownSeconds = Definition.ContactCooldownSeconds;
            radiusMeters = Definition.RadiusMeters;
            preferredRangeMinMeters = Definition.PreferredRangeMinMeters;
            preferredRangeMaxMeters = Definition.PreferredRangeMaxMeters;
            homeLocalPosition = transform.localPosition;
            instinctMoveDirection = Vector3.forward;
            closeThreatTimer = 0f;
            nextInstinctDecisionTime = 0f;
            retreatBurstEndTime = 0f;
            nextRetreatBurstAllowedTime = 0f;
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
            speedMetersPerSecond = bossDefinition.SpeedMetersPerSecond;
            contactDamage = bossDefinition.ContactDamage;
            contactCooldownSeconds = bossDefinition.ContactCooldownSeconds;
            radiusMeters = bossDefinition.RadiusMeters;
            var bossRange = EnemyDefinition.DefaultPreferredRangeFor(EnemyArchetypeId.Boss, EnemyBehaviorId.BossWarden, EnemyMovementMode.Grounded);
            preferredRangeMinMeters = bossRange.x;
            preferredRangeMaxMeters = bossRange.y;
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

            if (ResolvePendingReadabilityState(timeSeconds))
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

            if (behaviorId == EnemyBehaviorId.Charger && TickCharge(deltaTime, timeSeconds))
            {
                TryApplyContactDamage(timeSeconds);
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

            if (behaviorId == EnemyBehaviorId.TurretShooter)
            {
                return CanStartRangedAttack(timeSeconds);
            }

            return behaviorId == EnemyBehaviorId.Charger && CanStartChargeAttack(timeSeconds);
        }

        public float AttackPriorityScore(float timeSeconds)
        {
            var distance = DistanceToPlayer();
            var distanceScore = Mathf.Clamp(8f - distance, 0f, 8f);
            var behaviorScore = behaviorId == EnemyBehaviorId.Charger ? 1.25f : 1f;
            var intelligenceBonus = Intelligence switch
            {
                EnemyIntelligenceLevel.Cunning => 0.65f,
                EnemyIntelligenceLevel.Tactical => 0.45f,
                _ => 0f
            };

            return distanceScore + behaviorScore + intelligenceBonus;
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
            if (timeSeconds < chargeEndTime)
            {
                readabilityState = EnemyReadabilityState.Charging;
                readabilityStateEndTime = Mathf.Max(readabilityStateEndTime, chargeEndTime);
                var desired = transform.localPosition + activeChargeDirection * Definition.ChargeSpeedMetersPerSecond * deltaTime;
                transform.localPosition = RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);
                return true;
            }

            if (readabilityState == EnemyReadabilityState.Charging)
            {
                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
            }

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

            StartReadabilityState(EnemyReadabilityState.ChargeWindup, ChargeWindupSeconds, timeSeconds, delta.normalized);
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
            StartReadabilityState(EnemyReadabilityState.RangedWindup, RangedWindupSeconds, timeSeconds, delta.normalized);
            return true;
        }

        private bool CanStartChargeAttack(float timeSeconds)
        {
            if (playerController == null || timeSeconds < nextAllowedChargeTime)
            {
                return false;
            }

            if (Intelligence == EnemyIntelligenceLevel.Instinctive && Disposition == EnemyInstinctDisposition.Prey && !IsEndangered(timeSeconds))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            return delta.sqrMagnitude >= 0.01f && delta.magnitude <= Definition.AttackRangeMeters;
        }

        private bool CanStartRangedAttack(float timeSeconds)
        {
            if (playerController == null || timeSeconds < nextAllowedAttackTime)
            {
                return false;
            }

            var distance = DistanceToPlayer();
            if (!ShouldSentinelEngage(distance, timeSeconds))
            {
                return false;
            }

            if (Intelligence == EnemyIntelligenceLevel.Instinctive && Disposition == EnemyInstinctDisposition.Prey && !IsEndangered(timeSeconds))
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

        private bool ResolvePendingReadabilityState(float timeSeconds)
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
                chargeEndTime = timeSeconds + ChargeActiveSeconds;
                nextAllowedChargeTime = timeSeconds + Definition.ChargeCooldownSeconds;
                readabilityState = EnemyReadabilityState.Charging;
                readabilityStateEndTime = chargeEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.RangedWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                nextAllowedAttackTime = timeSeconds + Definition.AttackCooldownSeconds;
                FireProjectile(
                    TelegraphDirection,
                    behaviorId == EnemyBehaviorId.BossWarden ? DamageThreatKind.StrongProjectile : DamageThreatKind.Light);
                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.BossBurstWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                firedLowHealthBossBurst = true;
                FireProjectile(Vector3.forward, DamageThreatKind.StrongProjectile);
                FireProjectile(Vector3.back, DamageThreatKind.StrongProjectile);
                FireProjectile(Vector3.left, DamageThreatKind.StrongProjectile);
                FireProjectile(Vector3.right, DamageThreatKind.StrongProjectile);
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

            if (IsEndangered(timeSeconds))
            {
                return true;
            }

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

        private void MoveInDirection(Vector3 direction, float deltaTime, float speedMultiplier, bool allowSteering = true)
        {
            direction.y = 0f;
            var movementDirection = allowSteering ? ResolveLocalSteeringDirection(direction) : direction.normalized;
            if (movementDirection.sqrMagnitude <= 0.01f)
            {
                return;
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
            if (!IsAlive || IsInspectionFrozen || playerHealth == null || !playerHealth.IsAlive || IsInEntryGrace(timeSeconds) || timeSeconds < nextAllowedContactTime)
            {
                return false;
            }

            if (Intelligence == EnemyIntelligenceLevel.Instinctive && Disposition == EnemyInstinctDisposition.Prey && !IsEndangered(timeSeconds))
            {
                return false;
            }

            var distance = Vector3.Distance(Flat(transform.localPosition), Flat(playerController.transform.localPosition));
            if (distance > radiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.12f)
            {
                return false;
            }

            nextAllowedContactTime = timeSeconds + contactCooldownSeconds;
            var profile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
            var direction = playerController.transform.localPosition - transform.localPosition;
            var damaged = DamageSystem.ApplyDamage(
                playerHealth,
                new DamageRequest(
                    contactDamage,
                    gameObject,
                    DamageFeedbackContext.Knockback(direction, profile.PlayerKnockbackMeters, profile.KnockbackSeconds),
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
        }

        private void FireProjectile(Vector3 direction, DamageThreatKind threatKind)
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
                Definition.ProjectileDamage,
                Definition.ProjectileSpeedMetersPerSecond);
            projectile.ConfigureCombatFeel(combatFeelProfile);
            projectile.ConfigureThreat(threatKind);
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
