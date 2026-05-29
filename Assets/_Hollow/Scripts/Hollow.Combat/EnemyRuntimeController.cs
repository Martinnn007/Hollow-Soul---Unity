using Hollow.Entities;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using System;
using System.Collections.Generic;
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
        public const float LockingBeamTrackingSeconds = 1f;
        public const float LockingBeamLockedSeconds = 1f;

        [SerializeField] private float speedMetersPerSecond = ChaserEnemyController.DefaultSpeedMetersPerSecond;
        [SerializeField] private int contactDamage = ChaserEnemyController.DefaultContactDamage;
        [SerializeField] private float contactCooldownSeconds = ChaserEnemyController.DefaultContactCooldownSeconds;
        [SerializeField] private float radiusMeters = 0.32f;
        [SerializeField] private float preferredRangeMinMeters = 1.05f;
        [SerializeField] private float preferredRangeMaxMeters = 1.75f;
        [SerializeField] private float sightRadiusMeters = 6.5f;
        [SerializeField] private float sightAngleDegrees = 150f;
        [SerializeField] private float hearingRadiusMeters = 4.5f;
        [SerializeField] private float hearingSensitivityMultiplier = 1f;
        [SerializeField] private float disturbanceEscalationThreshold = 1.5f;
        [SerializeField] private float investigationDurationSeconds = 1.2f;
        [SerializeField] private bool allyAlertSharingEnabled;
        [SerializeField] private float allyAlertRadiusMeters;
        [SerializeField] private float allyAlertCooldownSeconds = 2f;
        [SerializeField] private EnemyAwarenessState allyAlertMinimumAwareness = EnemyAwarenessState.Engaged;
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
        [SerializeField] private EnemyBehaviorTreeDefinition behaviorTreeDefinition;
        [SerializeField] private EnemyBehaviorRuntimeMode behaviorRuntimeMode = EnemyBehaviorRuntimeMode.HollowBehaviorTree;
        [SerializeField] private EnemyUnityBehaviorPilotGraphDefinition unityBehaviorGraphDefinition;
        [SerializeField] private EnemySpacingProfileDefinition spacingProfile;

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
        private float lockingBeamLockTime;
        private bool lungeContactAttempted;
        private bool chargeContactAttempted;
        private bool rangedProjectileFired;
        private bool lockingBeamDirectionLocked;
        private EnemyRangedTelegraphPhase rangedTelegraphPhase = EnemyRangedTelegraphPhase.None;
        private Vector3 activeLungeDirection = Vector3.forward;
        private Vector3 facingDirection = Vector3.forward;
        private EnemyAttackProfileDefinition activeChargeProfile;
        private EnemyAttackProfileDefinition activeRangedProfile;
        private EnemyAttackProfileDefinition activeMeleeProfile;
        private EnemyAttackProfileDefinition activeAreaProfile;
        private EnemyAttackProfileDefinition activeWarningProfile;
        private EnemyAttackProfileDefinition activeGuardActionProfile;
        private EnemyAttackProfileDefinition activeCreatureMoveProfile;
        private EnemyAttackProfileDefinition activeCreatureSignalProfile;
        private EnemyGuardProfileDefinition resolvedGuardProfile;
        private EnemyGuardProfileDefinition activeGuardProfile;
        private EnemyAttackProfileDefinition activeBossContactProfile;
        private float bossActiveContactEndTime;
        private bool bossActiveContactAttempted;
        private float areaActiveEndTime;
        private float areaRecoveryEndTime;
        private bool areaDamageAttempted;
        private float warningEndTime;
        private float guardActiveEndTime;
        private float guardRecoveryEndTime;
        private float creatureMoveActiveEndTime;
        private float creatureMoveRecoveryEndTime;
        private float creatureSignalActiveEndTime;
        private float creatureSignalRecoveryEndTime;
        private bool creatureSignalEmitted;
        private Vector3 activeCreatureMoveDirection = Vector3.forward;
        private int activeMeleeComboDepth;
        private float engagedStartTime = float.NegativeInfinity;
        private float nextCritterDecisionTime;
        private bool critterFightDecision;
        private int spawnIndex = -1;
        private string lastBehaviorTreeNodeId = string.Empty;
        private string lastBehaviorCommand = string.Empty;
        private string lastBehaviorReason = string.Empty;
        private EnemyStimulusKind lastStimulusKind = EnemyStimulusKind.Footstep;
        private EnemyStimulusTier lastStimulusTier = EnemyStimulusTier.Quiet;
        private float lastStimulusTime = float.NegativeInfinity;
        private Vector3 lastStimulusLocalPosition;
        private string lastAwarenessReason = string.Empty;
        private float currentDisturbanceScore;
        private float investigationEndTime = float.NegativeInfinity;
        private readonly EnemyAiBrain aiBrain = new();
        private readonly EnemyLocomotionAgent locomotionAgent = new();
        private EnemyNavMeshAgentBridge navMeshAgentBridge;
        private CombatKnockbackReceiver knockbackReceiver;
        private EnemyUnityBehaviorGraphBridge unityBehaviorGraphBridge;
        private EnemyTacticalIntent lastTacticalIntent = EnemyTacticalIntent.Empty;
        private EnemyNavigationResult lastNavigationResult;
        private string lastDesignerDebugBlockedReason = string.Empty;
        private EnemyAttackReachabilityResult lastAttackReachability = EnemyAttackReachabilityResult.Clear(EnemyAttackObstructionPolicy.RuntimeDefault);
        private bool lastNavigationMoveRequiresAgentSync;
        private Vector3 cachedPathGoalLocalPosition;
        private Vector3 cachedPathNextWaypointLocalPosition;
        private EnemyNavigationIntent cachedPathIntent = EnemyNavigationIntent.None;
        private EnemyPathStatus cachedPathStatus = EnemyPathStatus.NotRequested;
        private float cachedPathCreatedTime = float.NegativeInfinity;
        private float nextPathRefreshTime;
        private int cachedPathWaypointCount;
        private string cachedPathFallbackReason = string.Empty;
        private Vector3[] cachedPathWaypointsLocalPositions = Array.Empty<Vector3>();
        private LineRenderer navigationDebugLine;
        private TextMesh aiDebugText;
        private Renderer[] cachedVisibilityRenderers = Array.Empty<Renderer>();
        private float nextAllowedAllyAlertTime;
        private float lastAllyAlertSharedTime = float.NegativeInfinity;
        private int lastAllyAlertRecipientCount;
        private string lastSpacingResetActionId = string.Empty;
        private int spacingResetCount;
        private float recoveryMovementRemainingMeters;
        private string recoveryMovementActionId = string.Empty;
        private EnemySpacingRecoveryMode recoveryMovementMode = EnemySpacingRecoveryMode.Planted;
        private readonly Dictionary<string, int> sequentialRadialSteps = new();

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

        public float HearingSensitivityMultiplier => Mathf.Clamp(hearingSensitivityMultiplier <= 0f ? 1f : hearingSensitivityMultiplier, 0.1f, 3f);

        public float DisturbanceEscalationThreshold => Mathf.Clamp(disturbanceEscalationThreshold <= 0f ? 1f : disturbanceEscalationThreshold, 0.05f, 8f);

        public float InvestigationDurationSeconds => Mathf.Clamp(investigationDurationSeconds <= 0f ? 1f : investigationDurationSeconds, 0.05f, 8f);

        public bool AllyAlertSharingEnabled => allyAlertSharingEnabled && bossRuntime == null && bossDefinition == null && archetypeId != EnemyArchetypeId.Boss;

        public float AllyAlertRadiusMeters => Mathf.Max(0f, allyAlertRadiusMeters);

        public float AllyAlertCooldownSeconds => Mathf.Clamp(allyAlertCooldownSeconds <= 0f ? 2f : allyAlertCooldownSeconds, 0.25f, 12f);

        public EnemyAwarenessState AllyAlertMinimumAwareness => allyAlertMinimumAwareness;

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

        public EnemyBehaviorTreeDefinition BehaviorTreeDefinition => behaviorTreeDefinition;

        public EnemyBehaviorRuntimeMode BehaviorRuntimeMode => behaviorRuntimeMode;

        public EnemyUnityBehaviorPilotGraphDefinition UnityBehaviorGraphDefinition => unityBehaviorGraphDefinition;

        public EnemyUnityBehaviorGraphBridge UnityBehaviorGraphBridge => unityBehaviorGraphBridge;

        public EnemySpacingProfileDefinition SpacingProfile => spacingProfile != null
            ? spacingProfile
            : Definition != null
                ? Definition.SpacingProfile
                : EnemySpacingProfileDefaults.CreateEnemyProfile(
                    "spawnEnemyNormal",
                    "Normal Chaser",
                    EnemyBehaviorId.Chaser,
                    EnemyMovementMode.Grounded,
                    PreferredRangeMinMeters,
                    PreferredRangeMaxMeters);

        public EnemyGuardProfileDefinition GuardProfile => resolvedGuardProfile;

        public EnemyGuardProfileDefinition ActiveGuardProfile => activeGuardProfile;

        public string LastBehaviorTreeNodeId => lastBehaviorTreeNodeId;

        public string LastBehaviorCommand => lastBehaviorCommand;

        public string LastBehaviorReason => lastBehaviorReason;

        public EnemyStimulusKind LastStimulusKind => lastStimulusKind;

        public EnemyStimulusTier LastStimulusTier => lastStimulusTier;

        public float LastStimulusTime => lastStimulusTime;

        public Vector3 LastStimulusLocalPosition => lastStimulusLocalPosition;

        public string LastAwarenessReason => lastAwarenessReason;

        public float CurrentDisturbanceScore => currentDisturbanceScore;

        public EnemyNavigationResult LastNavigationResult => lastNavigationResult;

        public EnemyNavigationBackend LastNavigationBackend => lastNavigationResult.Backend;

        public EnemyNavigationMode LastNavigationMode => lastNavigationResult.Mode;

        public EnemyNavigationIntent LastNavigationIntent => lastNavigationResult.Intent;

        public bool LastNavigationUsedFallbackSteering => lastNavigationResult.UsedFallbackSteering;

        public EnemyPathStatus LastNavigationPathStatus => lastNavigationResult.PathStatus;

        public Vector3 LastNavigationFinalGoal => lastNavigationResult.FinalGoalLocalPosition;

        public Vector3 LastNavigationNextWaypoint => lastNavigationResult.NextWaypointLocalPosition;

        public float LastNavigationPathAgeSeconds => lastNavigationResult.PathAgeSeconds;

        public int LastNavigationWaypointCount => lastNavigationResult.PathWaypointCount;

        public string LastNavigationFallbackReason => lastNavigationResult.FallbackReason;

        public EnemyAiLodTier CurrentAiLodTier => aiBrain.LodTier;

        public EnemyAiBlackboard AiBlackboard => aiBrain.Blackboard;

        public EnemyTacticalIntent LastTacticalIntent => lastTacticalIntent;

        public EnemyNavMeshAgentBridge NavMeshAgentBridge => navMeshAgentBridge;

        public bool IsVisibleToCamera
        {
            get
            {
                for (var index = 0; index < cachedVisibilityRenderers.Length; index++)
                {
                    var renderer = cachedVisibilityRenderers[index];
                    if (renderer != null && renderer.enabled && renderer.isVisible)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public GameObject PlayerGameObject => playerController != null ? playerController.gameObject : null;

        public int LocomotionBlockedFrames => locomotionAgent.BlockedFrames;

        public string LastLocomotionBlockedReason => locomotionAgent.LastBlockedReason;

        public string CurrentThreatActionDebugId => CurrentThreatActionId;

        public string LastDesignerDebugBlockedReason => lastDesignerDebugBlockedReason;

        public EnemyAttackReachabilityResult LastAttackReachability => lastAttackReachability;

        public string LastAttackReachabilityReason => lastAttackReachability.Reason;

        public bool IsInActiveAttackWindow => readabilityState is EnemyReadabilityState.MeleeLunge
            or EnemyReadabilityState.Charging
            or EnemyReadabilityState.RangedActive
            or EnemyReadabilityState.AreaActive
            or EnemyReadabilityState.GuardActive
            or EnemyReadabilityState.CreatureMoveActive
            or EnemyReadabilityState.CreatureSignalActive;

        public string ActiveAttackWindowDebugLine
        {
            get
            {
                var remaining = Mathf.Max(0f, readabilityStateEndTime - (lastTickTime > 0f ? lastTickTime : Time.time));
                var phase = IsInActiveAttackWindow
                    ? "ACTIVE"
                    : readabilityState switch
                    {
                        EnemyReadabilityState.MeleeWindup or
                            EnemyReadabilityState.ChargeWindup or
                            EnemyReadabilityState.RangedWindup or
                            EnemyReadabilityState.AreaWindup or
                            EnemyReadabilityState.GuardWindup or
                            EnemyReadabilityState.CreatureMoveWindup or
                            EnemyReadabilityState.CreatureSignalWindup or
                            EnemyReadabilityState.BossBurstWindup => "windup",
                        EnemyReadabilityState.MeleeRecovery or
                            EnemyReadabilityState.ChargeRecovery or
                            EnemyReadabilityState.RangedRecovery or
                            EnemyReadabilityState.AreaRecovery or
                            EnemyReadabilityState.GuardRecovery or
                            EnemyReadabilityState.CreatureMoveRecovery or
                            EnemyReadabilityState.CreatureSignalRecovery => "recovery",
                        _ => "inactive"
                    };
                return string.IsNullOrWhiteSpace(CurrentThreatActionDebugId)
                    ? $"{phase} {remaining:0.00}s"
                    : $"{phase} {CurrentThreatActionDebugId} {remaining:0.00}s";
            }
        }

        public float DistanceToPlayerMeters => DistanceToPlayer();

        public Vector3 DirectionToPlayer
        {
            get
            {
                if (playerController == null)
                {
                    return Vector3.zero;
                }

                var delta = playerController.transform.localPosition - transform.localPosition;
                delta.y = 0f;
                return delta.sqrMagnitude > 0.001f ? delta.normalized : Vector3.zero;
            }
        }

        public bool IsEndangeredNow => IsEndangered(lastTickTime > 0f ? lastTickTime : Time.time);

        public ThreatLane CurrentThreatLane => RoomThreatDirector.ResolveLane(FindActionProfile(CurrentThreatActionId), CurrentThreatAttackProfile);

        public float CurrentThreatPressureCost
        {
            get
            {
                var profile = CurrentThreatAttackProfile;
                if (profile == null)
                {
                    return 0f;
                }

                var baseCost = profile.ForceClass switch
                {
                    Hollow.Data.Definitions.ImpactForceClass.Massive => 2.2f,
                    Hollow.Data.Definitions.ImpactForceClass.Heavy => 1.7f,
                    Hollow.Data.Definitions.ImpactForceClass.Medium => 1.15f,
                    _ => 0.75f
                };
                return readabilityState is EnemyReadabilityState.MeleeWindup
                    or EnemyReadabilityState.MeleeLunge
                    or EnemyReadabilityState.ChargeWindup
                    or EnemyReadabilityState.Charging
                    or EnemyReadabilityState.RangedWindup
                    or EnemyReadabilityState.RangedActive
                    or EnemyReadabilityState.AreaWindup
                    or EnemyReadabilityState.AreaActive
                    ? baseCost
                    : 0f;
            }
        }

        private string CurrentThreatActionId => CurrentThreatAttackProfile != null
            ? CurrentThreatAttackProfile.AttackId
            : string.Empty;

        private EnemyAttackProfileDefinition CurrentThreatAttackProfile
        {
            get
            {
                return readabilityState switch
                {
                    EnemyReadabilityState.MeleeWindup or EnemyReadabilityState.MeleeLunge or EnemyReadabilityState.MeleeRecovery => activeMeleeProfile,
                    EnemyReadabilityState.ChargeWindup or EnemyReadabilityState.Charging or EnemyReadabilityState.ChargeRecovery => activeChargeProfile,
                    EnemyReadabilityState.RangedWindup or EnemyReadabilityState.RangedActive or EnemyReadabilityState.RangedRecovery => activeRangedProfile,
                    EnemyReadabilityState.AreaWindup or EnemyReadabilityState.AreaActive or EnemyReadabilityState.AreaRecovery => activeAreaProfile,
                    EnemyReadabilityState.FeintWarning => activeWarningProfile,
                    EnemyReadabilityState.GuardWindup or EnemyReadabilityState.GuardActive or EnemyReadabilityState.GuardRecovery => activeGuardActionProfile,
                    EnemyReadabilityState.CreatureMoveWindup or EnemyReadabilityState.CreatureMoveActive or EnemyReadabilityState.CreatureMoveRecovery => activeCreatureMoveProfile,
                    EnemyReadabilityState.CreatureSignalWindup or EnemyReadabilityState.CreatureSignalActive or EnemyReadabilityState.CreatureSignalRecovery => activeCreatureSignalProfile,
                    _ => null
                };
            }
        }

        public float LastAllyAlertSharedTime => lastAllyAlertSharedTime;

        public int LastAllyAlertRecipientCount => lastAllyAlertRecipientCount;

        public float ActiveAreaRangeMeters => activeAreaProfile != null ? activeAreaProfile.RangeMeters : Mathf.Max(1.2f, RadiusMeters * 4f);

        public string BossStatusText => bossRuntime != null ? bossRuntime.StatusText : "Engaging";

        public float ReadabilityStateEndTime => readabilityStateEndTime;

        public Vector3 TelegraphDirection => telegraphDirection.sqrMagnitude < 0.001f ? Vector3.forward : telegraphDirection.normalized;

        public EnemyRangedTelegraphPhase RangedTelegraphPhase => rangedTelegraphPhase;

        public bool IsRangedTelegraphLocked => rangedTelegraphPhase == EnemyRangedTelegraphPhase.Locked;

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

        public bool IsTooCloseForCurrentSpacing(float distanceToPlayer)
        {
            return ResolveCurrentActionSpacing(distanceToPlayer).IsTooClose(distanceToPlayer);
        }

        public bool IsTooFarForCurrentSpacing(float distanceToPlayer)
        {
            return ResolveCurrentActionSpacing(distanceToPlayer).IsTooFar(distanceToPlayer);
        }

        public bool IsInCurrentSpacingEnvelope(float distanceToPlayer)
        {
            return ResolveCurrentActionSpacing(distanceToPlayer).IsInsideEnvelope(distanceToPlayer);
        }

        public EnemyResolvedActionSpacing ResolveActionSpacingForTests(string actionId)
        {
            return ResolveSpacingForActionId(actionId);
        }

        public EnemyResolvedActionSpacing ResolveActionSpacingForTacticalIntent(string actionId)
        {
            return ResolveSpacingForActionId(actionId);
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
            behaviorTreeDefinition = Definition.BehaviorTree;
            behaviorRuntimeMode = Definition.BehaviorRuntimeMode;
            unityBehaviorGraphDefinition = Definition.UnityBehaviorGraph;
            spacingProfile = Definition.SpacingProfile;
            resolvedGuardProfile = Definition.GuardProfile;
            speedMetersPerSecond = tuning.ApplySpeed(Definition.SpeedMetersPerSecond);
            contactDamage = tuning.ApplyContactDamage(Definition.ContactDamage);
            contactCooldownSeconds = Definition.ContactCooldownSeconds;
            radiusMeters = Definition.RadiusMeters;
            preferredRangeMinMeters = Definition.PreferredRangeMinMeters;
            preferredRangeMaxMeters = Definition.PreferredRangeMaxMeters;
            sightRadiusMeters = Definition.SightRadiusMeters;
            sightAngleDegrees = Definition.SightAngleDegrees;
            hearingRadiusMeters = Definition.HearingRadiusMeters;
            hearingSensitivityMultiplier = Definition.HearingSensitivityMultiplier;
            disturbanceEscalationThreshold = Definition.DisturbanceEscalationThreshold;
            investigationDurationSeconds = Definition.InvestigationDurationSeconds;
            allyAlertSharingEnabled = Definition.AllyAlertSharingEnabled;
            allyAlertRadiusMeters = Definition.AllyAlertRadiusMeters;
            allyAlertCooldownSeconds = Definition.AllyAlertCooldownSeconds;
            allyAlertMinimumAwareness = Definition.AllyAlertMinimumAwareness;
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
            lockingBeamLockTime = 0f;
            lungeContactAttempted = false;
            chargeContactAttempted = false;
            rangedProjectileFired = false;
            lockingBeamDirectionLocked = false;
            rangedTelegraphPhase = EnemyRangedTelegraphPhase.None;
            activeLungeDirection = Vector3.forward;
            activeChargeProfile = null;
            activeRangedProfile = null;
            activeMeleeProfile = null;
            activeAreaProfile = null;
            activeWarningProfile = null;
            activeGuardActionProfile = null;
            activeCreatureMoveProfile = null;
            activeCreatureSignalProfile = null;
            activeGuardProfile = null;
            activeBossContactProfile = null;
            bossActiveContactEndTime = 0f;
            bossActiveContactAttempted = false;
            sequentialRadialSteps.Clear();
            areaActiveEndTime = 0f;
            areaRecoveryEndTime = 0f;
            areaDamageAttempted = false;
            warningEndTime = 0f;
            guardActiveEndTime = 0f;
            guardRecoveryEndTime = 0f;
            creatureMoveActiveEndTime = 0f;
            creatureMoveRecoveryEndTime = 0f;
            creatureSignalActiveEndTime = 0f;
            creatureSignalRecoveryEndTime = 0f;
            creatureSignalEmitted = false;
            activeCreatureMoveDirection = Vector3.forward;
            activeMeleeComboDepth = 0;
            engagedStartTime = awarenessState == EnemyAwarenessState.Engaged ? 0f : float.NegativeInfinity;
            nextCritterDecisionTime = 0f;
            critterFightDecision = false;
            lastDamagedTime = float.NegativeInfinity;
            lastBehaviorTreeNodeId = string.Empty;
            lastBehaviorCommand = string.Empty;
            lastBehaviorReason = string.Empty;
            lastStimulusKind = EnemyStimulusKind.Footstep;
            lastStimulusTier = EnemyStimulusTier.Quiet;
            lastStimulusTime = float.NegativeInfinity;
            lastStimulusLocalPosition = Vector3.zero;
            lastAwarenessReason = string.Empty;
            currentDisturbanceScore = 0f;
            investigationEndTime = float.NegativeInfinity;
            nextAllowedAllyAlertTime = 0f;
            lastAllyAlertSharedTime = float.NegativeInfinity;
            lastAllyAlertRecipientCount = 0;
            lastSpacingResetActionId = string.Empty;
            spacingResetCount = 0;
            recoveryMovementRemainingMeters = 0f;
            recoveryMovementActionId = string.Empty;
            recoveryMovementMode = EnemySpacingRecoveryMode.Planted;
            lastTacticalIntent = EnemyTacticalIntent.Empty;
            lastDesignerDebugBlockedReason = string.Empty;
            lastAttackReachability = EnemyAttackReachabilityResult.Clear(EnemyAttackObstructionPolicy.RuntimeDefault);
            ResetPathCache();
            locomotionAgent.Reset();
            aiBrain.Reset();
            ConfigureNavMeshAgentBridge(IsRuntimeNavMeshEligible());
            ConfigureUnityBehaviorGraphBridge();

            Health = GetComponent<CombatantHealth>() ?? gameObject.AddComponent<CombatantHealth>();
            Health.Configure(tuning.ApplyHealth(Definition.MaxHealth));
            Health.Damaged -= OnDamaged;
            Health.Damaged += OnDamaged;
            Health.Died -= OnDied;
            Health.Died += OnDied;
            ApplyDefinitionPresentation(Definition);
            InstantiateOptionalEnemyVisuals(Definition);
            RefreshVisibilityRenderers();
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
            behaviorTreeDefinition = bossDefinition.BehaviorTreeMetadata;
            behaviorRuntimeMode = EnemyBehaviorRuntimeMode.HollowBehaviorTree;
            unityBehaviorGraphDefinition = null;
            spacingProfile = bossDefinition.SpacingProfileMetadata;
            resolvedGuardProfile = null;
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
            var bossDisturbance = EnemyDefinition.DefaultDisturbanceTuningFor(EnemyArchetypeId.Boss, EnemyBehaviorId.BossWarden, EnemyMovementMode.Grounded);
            hearingSensitivityMultiplier = bossDisturbance.x;
            disturbanceEscalationThreshold = bossDisturbance.y;
            investigationDurationSeconds = bossDisturbance.z;
            allyAlertSharingEnabled = false;
            allyAlertRadiusMeters = 0f;
            allyAlertCooldownSeconds = 2f;
            allyAlertMinimumAwareness = EnemyAwarenessState.Engaged;
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
            activeChargeProfile = null;
            activeRangedProfile = null;
            activeMeleeProfile = null;
            activeAreaProfile = null;
            activeWarningProfile = null;
            activeGuardActionProfile = null;
            activeCreatureMoveProfile = null;
            activeCreatureSignalProfile = null;
            activeGuardProfile = null;
            activeBossContactProfile = null;
            bossActiveContactEndTime = 0f;
            bossActiveContactAttempted = false;
            areaActiveEndTime = 0f;
            areaRecoveryEndTime = 0f;
            areaDamageAttempted = false;
            warningEndTime = 0f;
            guardActiveEndTime = 0f;
            guardRecoveryEndTime = 0f;
            creatureMoveActiveEndTime = 0f;
            creatureMoveRecoveryEndTime = 0f;
            creatureSignalActiveEndTime = 0f;
            creatureSignalRecoveryEndTime = 0f;
            creatureSignalEmitted = false;
            activeCreatureMoveDirection = Vector3.forward;
            activeMeleeComboDepth = 0;
            awarenessState = EnemyAwarenessState.Engaged;
            facingDirection = Vector3.forward;
            lastStimulusKind = EnemyStimulusKind.Footstep;
            lastStimulusTier = EnemyStimulusTier.Quiet;
            lastStimulusTime = float.NegativeInfinity;
            lastStimulusLocalPosition = Vector3.zero;
            lastAwarenessReason = string.Empty;
            currentDisturbanceScore = 0f;
            investigationEndTime = float.NegativeInfinity;
            nextAllowedAllyAlertTime = 0f;
            lastAllyAlertSharedTime = float.NegativeInfinity;
            lastAllyAlertRecipientCount = 0;
            lastSpacingResetActionId = string.Empty;
            spacingResetCount = 0;
            recoveryMovementRemainingMeters = 0f;
            recoveryMovementActionId = string.Empty;
            recoveryMovementMode = EnemySpacingRecoveryMode.Planted;
            lastAttackReachability = EnemyAttackReachabilityResult.Clear(EnemyAttackObstructionPolicy.RuntimeDefault);
            ResetPathCache();
            aiBrain.Reset();
            DisableNavMeshAgent();
            DisableUnityBehaviorGraphBridge();
            gameObject.name = $"Enemy.Boss.{bossDefinition.BossId}";
            transform.localScale = Vector3.one * bossDefinition.VisualScale;
            Health.Configure(bossDefinition.MaxHealth);
            PresentationPrefabResolver.InstantiateVisual(bossDefinition.PresentationPrefabRole, transform, Vector3.zero, Vector3.one);
            InstantiateOptionalBossVisuals(bossDefinition);
            RefreshVisibilityRenderers();
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

            knockbackReceiver = GetComponent<CombatKnockbackReceiver>() ?? gameObject.AddComponent<CombatKnockbackReceiver>();
            knockbackReceiver.Configure(roomRuntimeRoot, radiusMeters, movementMode == EnemyMovementMode.Flying, resistance);
            var presenter = GetComponent<CombatReadabilityPresenter>() ?? gameObject.AddComponent<CombatReadabilityPresenter>();
            presenter.Bind(this, combatFeelProfile);
        }

        private bool IsRuntimeNavMeshEligible()
        {
            return roomRuntimeRoot != null &&
                roomRuntimeRoot.HasNavMeshBake &&
                movementMode == EnemyMovementMode.Grounded &&
                speedMetersPerSecond > 0f &&
                bossRuntime == null &&
                bossDefinition == null &&
                archetypeId != EnemyArchetypeId.Boss;
        }

        private void ConfigureNavMeshAgentBridge(bool enabledForGroundedNavigation)
        {
            navMeshAgentBridge = GetComponent<EnemyNavMeshAgentBridge>() ?? gameObject.AddComponent<EnemyNavMeshAgentBridge>();
            navMeshAgentBridge.Configure(roomRuntimeRoot, radiusMeters, speedMetersPerSecond, enabledForGroundedNavigation);
        }

        private void StopNavMeshAgent(bool clearPath)
        {
            navMeshAgentBridge?.ClaimHollowManual(clearPath, clearPath ? "hollow_stop_clear_path" : "hollow_stop_keep_path");
        }

        private void DisableNavMeshAgent()
        {
            navMeshAgentBridge?.DisableAgent();
        }

        public void SyncNavMeshAgentAfterExternalDisplacement(string reason)
        {
            StopNavMeshAgent(clearPath: true);
            SyncNavMeshAgentAfterHollowOwnedMove(reason);
        }

        private void SyncNavMeshAgentAfterHollowOwnedMove(string reason)
        {
            navMeshAgentBridge?.SyncAfterHollowOwnedMotion(reason);
        }

        private void ApplyNavigationMove(Vector3 resolvedLocalPosition, string reason)
        {
            transform.localPosition = resolvedLocalPosition;
            if (lastNavigationMoveRequiresAgentSync)
            {
                SyncNavMeshAgentAfterHollowOwnedMove(reason);
            }
        }

        private void ConfigureUnityBehaviorGraphBridge()
        {
            if (behaviorRuntimeMode != EnemyBehaviorRuntimeMode.UnityBehaviorGraph ||
                bossRuntime != null ||
                bossDefinition != null ||
                archetypeId == EnemyArchetypeId.Boss)
            {
                DisableUnityBehaviorGraphBridge();
                return;
            }

            unityBehaviorGraphBridge = GetComponent<EnemyUnityBehaviorGraphBridge>() ?? gameObject.AddComponent<EnemyUnityBehaviorGraphBridge>();
            unityBehaviorGraphBridge.Configure(this, unityBehaviorGraphDefinition);
        }

        private void DisableUnityBehaviorGraphBridge()
        {
            unityBehaviorGraphBridge?.DisableBridge();
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

        public void ApplyDebugTuningOverride(EnemyRuntimeTuningOverride tuning)
        {
            if (!tuning.HasAnyOverride || bossDefinition != null)
            {
                return;
            }

            tuning = tuning.Sanitized();
            if (tuning.overrideMaxHealth && Health != null)
            {
                Health.SetMaxHealthPreservingCurrent(tuning.maxHealth, 0);
            }

            if (tuning.overrideSpeed)
            {
                speedMetersPerSecond = tuning.speedMetersPerSecond;
            }

            if (tuning.overrideRadius)
            {
                radiusMeters = tuning.radiusMeters;
            }

            if (tuning.overrideSenses)
            {
                sightRadiusMeters = tuning.sightRadiusMeters;
                sightAngleDegrees = tuning.sightAngleDegrees;
                hearingRadiusMeters = tuning.hearingRadiusMeters;
            }

            if (tuning.overrideIntelligenceDisposition)
            {
                ApplyIntelligenceDisposition(tuning.intelligence, tuning.disposition);
            }

            if (tuning.overrideAttackExecution)
            {
                attackWindupScale = tuning.attackWindupScale;
                attackActiveScale = tuning.attackActiveScale;
                attackRecoveryScale = tuning.attackRecoveryScale;
                hitArcDegreesBonus = tuning.hitArcDegreesBonus;
                poiseBreakThresholdOffset = tuning.poiseBreakThresholdOffset;
            }

            ConfigureNavMeshAgentBridge(IsRuntimeNavMeshEligible());
            ConfigureCombatFeel(combatFeelProfile);
        }

        public void ApplyDebugTuningOverride(EnemyDefinition tunedDefinition)
        {
            if (tunedDefinition == null || bossDefinition != null)
            {
                return;
            }

            Definition = tunedDefinition;
            archetypeId = tunedDefinition.ArchetypeId;
            behaviorId = tunedDefinition.BehaviorId;
            movementMode = tunedDefinition.MovementMode;
            bodyClass = tunedDefinition.BodyClass;
            contactDamagePolicy = tunedDefinition.ContactDamagePolicy;
            passiveContactHazardType = tunedDefinition.PassiveContactHazardType;
            contactDamage = tunedDefinition.ContactDamage;
            contactCooldownSeconds = tunedDefinition.ContactCooldownSeconds;
            preferredRangeMinMeters = tunedDefinition.PreferredRangeMinMeters;
            preferredRangeMaxMeters = tunedDefinition.PreferredRangeMaxMeters;
            hearingSensitivityMultiplier = tunedDefinition.HearingSensitivityMultiplier;
            disturbanceEscalationThreshold = tunedDefinition.DisturbanceEscalationThreshold;
            investigationDurationSeconds = tunedDefinition.InvestigationDurationSeconds;
            allyAlertSharingEnabled = tunedDefinition.AllyAlertSharingEnabled;
            allyAlertRadiusMeters = tunedDefinition.AllyAlertRadiusMeters;
            allyAlertCooldownSeconds = tunedDefinition.AllyAlertCooldownSeconds;
            allyAlertMinimumAwareness = tunedDefinition.AllyAlertMinimumAwareness;
            lungeAttackEnabled = tunedDefinition.LungeAttackEnabled;
            lungeTriggerRangeMeters = tunedDefinition.LungeTriggerRangeMeters;
            lungeWindupSeconds = tunedDefinition.LungeWindupSeconds;
            lungeActiveSeconds = tunedDefinition.LungeActiveSeconds;
            lungeDistanceMeters = tunedDefinition.LungeDistanceMeters;
            lungeCooldownSeconds = tunedDefinition.LungeCooldownSeconds;
            behaviorTreeDefinition = tunedDefinition.BehaviorTree;
            behaviorRuntimeMode = tunedDefinition.BehaviorRuntimeMode;
            unityBehaviorGraphDefinition = tunedDefinition.UnityBehaviorGraph;
            spacingProfile = tunedDefinition.SpacingProfile;
            resolvedGuardProfile = tunedDefinition.GuardProfile;
            ApplyDebugTuningOverride(EnemyRuntimeTuningOverride.FromDefinition(tunedDefinition));
            ConfigureNavMeshAgentBridge(IsRuntimeNavMeshEligible());
            ConfigureUnityBehaviorGraphBridge();
            ApplyDefinitionPresentation(tunedDefinition);
            InstantiateOptionalEnemyVisuals(tunedDefinition);
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

        private void LateUpdate()
        {
            UpdateNavigationDebugOverlay();
            UpdateAiDebugOverlay();
        }

        private void OnDisable()
        {
            DisableNavMeshAgent();
            SetNavigationDebugLineVisible(false);
            SetAiDebugTextVisible(false);
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

            if (knockbackReceiver != null && knockbackReceiver.IsKnockbackActive)
            {
                StopNavMeshAgent(clearPath: true);
                SyncNavMeshAgentAfterHollowOwnedMove("knockback_active");
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

            if (behaviorId == EnemyBehaviorId.BossWarden || archetypeId == EnemyArchetypeId.Boss)
            {
                TickBoss(deltaTime, timeSeconds);
                TryApplyContactDamage(timeSeconds);
                return;
            }

            var distanceToPlayer = DistanceToPlayer();
            UpdateInstinctThreat(deltaTime, distanceToPlayer);
            UpdateAwareness(timeSeconds, distanceToPlayer);
            if (!TryTickBehaviorTree(deltaTime, timeSeconds, distanceToPlayer))
            {
                TickIntelligenceMovement(deltaTime, timeSeconds, distanceToPlayer);
            }

            TryApplyContactDamage(timeSeconds);
        }

        private bool TryTickBehaviorTree(float deltaTime, float timeSeconds, float distanceToPlayer)
        {
            if (bossRuntime != null || behaviorId == EnemyBehaviorId.BossWarden || archetypeId == EnemyArchetypeId.Boss)
            {
                return false;
            }

            if (aiBrain.TryReuseCommand(this, timeSeconds, distanceToPlayer, out var cachedCommand))
            {
                if (roomCombatController != null)
                {
                    cachedCommand = roomCombatController.TacticalDirector.PlanCommand(
                        this,
                        cachedCommand,
                        timeSeconds,
                        distanceToPlayer,
                        out lastTacticalIntent);
                }

                if (ExecuteBehaviorCommand(cachedCommand, deltaTime, timeSeconds, distanceToPlayer))
                {
                    lastBehaviorTreeNodeId = cachedCommand.Reason;
                    lastBehaviorCommand = cachedCommand.Kind.ToString();
                    lastBehaviorReason = cachedCommand.ActionId;
                    return true;
                }
            }

            var context = new EnemyBehaviorTreeContext(
                this,
                deltaTime,
                timeSeconds,
                distanceToPlayer,
                readabilityState == EnemyReadabilityState.Idle,
                IsEndangered(timeSeconds),
                ShouldSentinelEngage(distanceToPlayer, timeSeconds),
                CanStartRangedAttack(timeSeconds),
                CanStartChargeAttack(timeSeconds));

            if (behaviorRuntimeMode == EnemyBehaviorRuntimeMode.UnityBehaviorGraph)
            {
                if (unityBehaviorGraphBridge == null)
                {
                    ConfigureUnityBehaviorGraphBridge();
                }

                if (unityBehaviorGraphBridge == null ||
                    !unityBehaviorGraphBridge.TryEvaluate(context, out var unityBehaviorCommand))
                {
                    lastBehaviorTreeNodeId = "unity_behavior_missing_bridge";
                    lastBehaviorCommand = EnemyBehaviorCommandKind.None.ToString();
                    lastBehaviorReason = "unity_behavior_failed";
                    return false;
                }

                lastBehaviorTreeNodeId = unityBehaviorGraphBridge.LastEvaluationReason;
                lastBehaviorCommand = unityBehaviorCommand.Kind.ToString();
                lastBehaviorReason = unityBehaviorCommand.ActionId;
                return ChoosePlanAndExecuteBehaviorCommand(unityBehaviorCommand, deltaTime, timeSeconds, distanceToPlayer);
            }

            var tree = behaviorTreeDefinition != null ? behaviorTreeDefinition : Definition != null ? Definition.BehaviorTree : null;
            if (tree == null || tree.BossMetadataOnly)
            {
                return false;
            }

            if (!tree.TryEvaluate(context, out var command))
            {
                lastBehaviorTreeNodeId = tree.RootNode != null ? tree.RootNode.NodeId : string.Empty;
                lastBehaviorCommand = EnemyBehaviorCommandKind.None.ToString();
                lastBehaviorReason = "tree_failed";
                return false;
            }

            lastBehaviorTreeNodeId = command.Reason;
            lastBehaviorCommand = command.Kind.ToString();
            lastBehaviorReason = command.ActionId;
            return ChoosePlanAndExecuteBehaviorCommand(command, deltaTime, timeSeconds, distanceToPlayer);
        }

        private bool ChoosePlanAndExecuteBehaviorCommand(
            EnemyBehaviorCommand command,
            float deltaTime,
            float timeSeconds,
            float distanceToPlayer)
        {
            command = aiBrain.ChooseCommand(
                this,
                command,
                timeSeconds,
                distanceToPlayer,
                roomCombatController != null ? roomCombatController.ThreatDirector : null);
            if (roomCombatController != null)
            {
                command = roomCombatController.TacticalDirector.PlanCommand(
                    this,
                    command,
                    timeSeconds,
                    distanceToPlayer,
                    out lastTacticalIntent);
            }
            else
            {
                lastTacticalIntent = EnemyTacticalIntent.Empty;
            }

            lastBehaviorCommand = command.Kind.ToString();
            lastBehaviorReason = command.ActionId;
            lastDesignerDebugBlockedReason = string.Empty;

            if (ExecuteBehaviorCommand(command, deltaTime, timeSeconds, distanceToPlayer))
            {
                return true;
            }

            if (command.StartsCommittedAction)
            {
                HandleBlockedCommittedAction(command, deltaTime, timeSeconds, distanceToPlayer);
                return true;
            }

            return false;
        }

        private void HandleBlockedCommittedAction(EnemyBehaviorCommand command, float deltaTime, float timeSeconds, float distanceToPlayer)
        {
            if (playerController == null)
            {
                return;
            }

            if (TryEvaluateCommandReachability(command, timeSeconds, canReposition: true, out var reachability) &&
                reachability.Status == EnemyAttackReachabilityStatus.NeedsReposition)
            {
                lastDesignerDebugBlockedReason = $"reposition_for_line:{command.ActionId}:{reachability.Reason}";
                if (TryMoveTowardTacticalReservation(deltaTime, Mathf.Max(0.65f, command.SpeedMultiplier), EnemyNavigationIntent.PreferredRange))
                {
                    return;
                }

                if (TryMoveTowardClearAttackReposition(
                        ResolveCommittedActionId(command, timeSeconds),
                        deltaTime,
                        Mathf.Max(0.65f, command.SpeedMultiplier),
                        EnemyNavigationIntent.PreferredRange,
                        "blocked_commit"))
                {
                    return;
                }

                MovePreferredRange(deltaTime, distanceToPlayer, Mathf.Max(0.65f, command.SpeedMultiplier));
                return;
            }

            var spacing = ResolveSpacingForActionId(command.ActionId);
            if (spacing.IsTooFar(distanceToPlayer))
            {
                lastDesignerDebugBlockedReason = $"too_far_for_{command.ActionId}:{distanceToPlayer:0.0}m";
                if (TryMoveTowardTacticalReservation(deltaTime, Mathf.Max(0.65f, command.SpeedMultiplier), EnemyNavigationIntent.PreferredRange))
                {
                    return;
                }

                MovePreferredRange(deltaTime, distanceToPlayer, Mathf.Max(0.65f, command.SpeedMultiplier));
                return;
            }

            if (spacing.IsTooClose(distanceToPlayer) && TryStartSpacingReset(command.ActionId, spacing, timeSeconds))
            {
                lastDesignerDebugBlockedReason = $"too_close_reset_{command.ActionId}:{distanceToPlayer:0.0}m";
                MoveSpacingReset(deltaTime, Mathf.Max(0.55f, command.SpeedMultiplier), spacing);
                return;
            }

            lastDesignerDebugBlockedReason = $"blocked_face_only_{command.Kind}:{command.ActionId}";
            FacePlayer();
        }

        private bool ExecuteBehaviorCommand(EnemyBehaviorCommand command, float deltaTime, float timeSeconds, float distanceToPlayer)
        {
            switch (command.Kind)
            {
                case EnemyBehaviorCommandKind.None:
                    StopNavMeshAgent(clearPath: false);
                    return true;
                case EnemyBehaviorCommandKind.Hold:
                    StopNavMeshAgent(clearPath: false);
                    if (Disposition == EnemyInstinctDisposition.Sentinel)
                    {
                        if (HasActiveInvestigation(timeSeconds))
                        {
                            FacePosition(lastStimulusLocalPosition);
                        }

                        TickSentinelHold(deltaTime);
                    }

                    return true;
                case EnemyBehaviorCommandKind.FacePlayer:
                    StopNavMeshAgent(clearPath: false);
                    FacePlayer();
                    return true;
                case EnemyBehaviorCommandKind.MoveToPlayer:
                    TickChase(deltaTime, timeSeconds: timeSeconds);
                    return true;
                case EnemyBehaviorCommandKind.MovePreferredRange:
                    MovePreferredRange(deltaTime, distanceToPlayer, command.SpeedMultiplier);
                    return true;
                case EnemyBehaviorCommandKind.Flee:
                    MoveFleeIntent(deltaTime, timeSeconds, distanceToPlayer, Mathf.Max(0.1f, command.SpeedMultiplier));
                    return true;
                case EnemyBehaviorCommandKind.Wander:
                    if (TryMoveInvestigationIntent(deltaTime, timeSeconds, Mathf.Max(0.1f, command.SpeedMultiplier)))
                    {
                        return true;
                    }

                    MoveInDirection(IsCritterBehavior() ? ResolveCritterWanderDirection(timeSeconds) : ResolveInstinctWanderDirection(timeSeconds), deltaTime, Mathf.Max(0.1f, command.SpeedMultiplier), intent: EnemyNavigationIntent.Wander);
                    return true;
                case EnemyBehaviorCommandKind.StartMeleeAction:
                    return TryMeleeLunge(timeSeconds, command.ActionId);
                case EnemyBehaviorCommandKind.StartRangedAction:
                    return TryRangedAttack(timeSeconds, command.ActionId);
                case EnemyBehaviorCommandKind.StartChargeAction:
                    return TryChargeAttack(timeSeconds, command.ActionId);
                case EnemyBehaviorCommandKind.StartAreaAction:
                    return TryAreaAttack(timeSeconds, command.ActionId);
                case EnemyBehaviorCommandKind.StartFeintWarning:
                    return TryStartFeintWarning(timeSeconds, command.ActionId);
                case EnemyBehaviorCommandKind.StartGuardAction:
                    return TryStartGuardAction(timeSeconds, command.ActionId);
                case EnemyBehaviorCommandKind.StartCreatureMoveAction:
                    return TryStartCreatureMoveAction(timeSeconds, command.ActionId);
                case EnemyBehaviorCommandKind.StartCreatureSignalAction:
                    return TryStartCreatureSignalAction(timeSeconds, command.ActionId);
                default:
                    return false;
            }
        }

        private void MovePreferredRange(float deltaTime, float distanceToPlayer, float speedMultiplier)
        {
            if (playerController == null)
            {
                return;
            }

            if (TryMoveTowardTacticalReservation(deltaTime, Mathf.Max(0.1f, speedMultiplier), EnemyNavigationIntent.PreferredRange))
            {
                return;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var direction = ResolvePreferredRangeDirection(delta.normalized, distanceToPlayer);
            if (direction.sqrMagnitude <= 0.01f)
            {
                return;
            }

            MoveInDirection(direction, deltaTime, RangeIntentSpeedMultiplier(direction, delta.normalized, distanceToPlayer) * Mathf.Max(0.1f, speedMultiplier), intent: EnemyNavigationIntent.PreferredRange);
        }

        private bool TryMoveTowardTacticalReservation(float deltaTime, float speedMultiplier, EnemyNavigationIntent intent)
        {
            if (!lastTacticalIntent.HasReservedPosition ||
                playerController == null ||
                lastTacticalIntent.Role is EnemyTacticalRole.Hold or EnemyTacticalRole.Waiting or EnemyTacticalRole.StationarySentinel ||
                readabilityState != EnemyReadabilityState.Idle)
            {
                return false;
            }

            var delta = lastTacticalIntent.ReservedLocalPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.0625f)
            {
                FacePlayer();
                return true;
            }

            MoveInDirection(
                delta.normalized,
                deltaTime,
                Mathf.Max(0.1f, speedMultiplier),
                allowSteering: true,
                intent: intent,
                finalGoalLocalPosition: lastTacticalIntent.ReservedLocalPosition);
            return true;
        }

        private bool TryMoveTowardClearAttackReposition(
            string actionId,
            float deltaTime,
            float speedMultiplier,
            EnemyNavigationIntent intent,
            string debugPrefix)
        {
            if (playerController == null ||
                roomRuntimeRoot == null ||
                readabilityState != EnemyReadabilityState.Idle ||
                string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            var resolved = false;
            var reserved = transform.localPosition;
            var reason = string.Empty;
            if (roomCombatController != null &&
                movementMode == EnemyMovementMode.Grounded &&
                roomCombatController.TacticalDirector.TryResolveClearAttackReposition(
                    this,
                    roomRuntimeRoot,
                    playerController,
                    actionId,
                    out reserved,
                    out reason))
            {
                resolved = true;
            }
            else if (TryResolveLocalClearAttackReposition(actionId, out reserved, out reason))
            {
                resolved = true;
            }

            if (!resolved)
            {
                return false;
            }

            var delta = reserved - transform.localPosition;
            delta.y = 0f;
            lastDesignerDebugBlockedReason = $"{debugPrefix}:reposition_for_line:{actionId}:{reason}";
            if (delta.sqrMagnitude <= 0.0625f)
            {
                FacePlayer();
                return true;
            }

            MoveInDirection(
                delta.normalized,
                deltaTime,
                Mathf.Max(0.1f, speedMultiplier),
                allowSteering: true,
                intent: intent,
                finalGoalLocalPosition: reserved);
            return true;
        }

        private bool TryResolveLocalClearAttackReposition(string actionId, out Vector3 reserved, out string reason)
        {
            reserved = transform.localPosition;
            reason = string.Empty;
            if (Definition == null || playerController == null || roomRuntimeRoot == null || string.IsNullOrWhiteSpace(actionId))
            {
                reason = "missing_local_attack_reposition_context";
                return false;
            }

            var attack = Definition.ResolveAttackProfile(actionId);
            if (attack == null)
            {
                reason = "missing_attack_profile";
                return false;
            }

            var spacing = ResolveSpacingForAttackProfile(attack);
            var playerLocal = playerController.transform.localPosition;
            var currentDelta = transform.localPosition - playerLocal;
            currentDelta.y = 0f;
            var currentAngle = currentDelta.sqrMagnitude > 0.01f
                ? Mathf.Atan2(currentDelta.x, currentDelta.z) * Mathf.Rad2Deg
                : 0f;
            var desiredDistance = Mathf.Max(
                RadiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.18f,
                spacing.DesiredStartDistanceMeters);
            var angleOffsets = new[] { 0f, -24f, 24f, -48f, 48f, 72f, -72f, 112f, -112f, 160f, -160f, 180f };
            var distances = new[] { desiredDistance, desiredDistance + 0.45f, Mathf.Max(0.35f, desiredDistance - 0.35f), desiredDistance + 0.9f };
            var bestScore = float.NegativeInfinity;
            var best = reserved;
            var bestReason = string.Empty;

            for (var distanceIndex = 0; distanceIndex < distances.Length; distanceIndex++)
            {
                for (var angleIndex = 0; angleIndex < angleOffsets.Length; angleIndex++)
                {
                    var angle = currentAngle + angleOffsets[angleIndex];
                    var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    var candidate = playerLocal + direction.normalized * distances[distanceIndex];
                    candidate.y = transform.localPosition.y;
                    if (!IsLocalAttackRepositionCandidateValid(candidate))
                    {
                        reason = "local_candidate_blocked";
                        continue;
                    }

                    var reachability = EnemyAttackReachabilityService.Evaluate(
                        roomRuntimeRoot,
                        candidate,
                        playerLocal,
                        RadiusMeters,
                        PlaceholderPlayerController.DefaultRadiusMeters,
                        attack,
                        attack.RuntimeKind,
                        canReposition: false);
                    if (!reachability.CanCommit)
                    {
                        reason = reachability.Reason;
                        continue;
                    }

                    var distanceFit = -Mathf.Abs(distances[distanceIndex] - desiredDistance) * 0.55f;
                    var travelCost = -Vector3.Distance(candidate, transform.localPosition) * 0.05f;
                    var score = distanceFit + travelCost - angleIndex * 0.015f;
                    if (score <= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    best = candidate;
                    bestReason = reachability.Reason;
                }
            }

            if (bestScore <= float.NegativeInfinity)
            {
                reason = string.IsNullOrWhiteSpace(reason) ? "no_local_clear_attack_candidate" : reason;
                return false;
            }

            reserved = best;
            reason = string.IsNullOrWhiteSpace(bestReason) ? "local_clear_attack_reposition" : $"local_clear_attack_reposition:{bestReason}";
            return true;
        }

        private bool IsLocalAttackRepositionCandidateValid(Vector3 candidate)
        {
            if (movementMode == EnemyMovementMode.Flying)
            {
                return !RoomLocalCollision.IsOutsideFloorRegions(roomRuntimeRoot, candidate, RadiusMeters);
            }

            return RoomLocalCollision.CanOccupy(roomRuntimeRoot, candidate, RadiusMeters);
        }

        private bool TryStartSpacingReset(string actionId, EnemyResolvedActionSpacing spacing, float timeSeconds)
        {
            var key = string.IsNullOrWhiteSpace(actionId) ? spacing.ActionId : actionId;
            if (!string.Equals(lastSpacingResetActionId, key, StringComparison.Ordinal))
            {
                lastSpacingResetActionId = key;
                spacingResetCount = 0;
            }

            if (spacingResetCount >= spacing.MaxResetCountBeforeCommit || timeSeconds < nextRetreatBurstAllowedTime)
            {
                return false;
            }

            spacingResetCount++;
            var profile = SpacingProfile;
            retreatBurstEndTime = timeSeconds + (profile != null ? profile.RetreatBurstSeconds : RetreatBurstSeconds);
            nextRetreatBurstAllowedTime = retreatBurstEndTime + (profile != null ? profile.RetreatReassessSeconds : RetreatReassessSeconds);
            return true;
        }

        private void MoveSpacingReset(float deltaTime, float speedMultiplier, EnemyResolvedActionSpacing spacing)
        {
            if (playerController == null)
            {
                return;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var direction = -delta.normalized;
            var intent = spacing.RecoveryMovementMode == EnemySpacingRecoveryMode.PhaseDrift
                ? EnemyNavigationIntent.PhaseMove
                : EnemyNavigationIntent.Flee;
            MoveInDirection(direction, deltaTime, speedMultiplier, allowSteering: spacing.RecoveryMovementMode != EnemySpacingRecoveryMode.PhaseDrift, intent);
        }

        private void ResetSpacingResetChain()
        {
            lastSpacingResetActionId = string.Empty;
            spacingResetCount = 0;
        }

        private void MoveFleeIntent(float deltaTime, float timeSeconds, float distanceToPlayer, float speedMultiplier)
        {
            if (playerController == null)
            {
                return;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            if (Intelligence == EnemyIntelligenceLevel.Instinctive && Disposition == EnemyInstinctDisposition.Prey && !IsEndangered(timeSeconds))
            {
                MoveInDirection(ResolvePreyMovementDirection(delta.normalized, distanceToPlayer, timeSeconds), deltaTime, speedMultiplier, intent: EnemyNavigationIntent.Flee);
                return;
            }

            var before = transform.localPosition;
            MoveAwayFromPlayer(deltaTime, speedMultiplier);
            if (IsCritterBehavior() && (transform.localPosition - before).sqrMagnitude <= 0.0001f)
            {
                var direction = -delta.normalized;
                facingDirection = delta.normalized;
                var desired = before + direction * speedMetersPerSecond * speedMultiplier * Mathf.Max(0f, deltaTime);
                ApplyNavigationMove(ResolveNavigationMove(desired, EnemyNavigationIntent.Flee), "flee_local_recovery");
            }
        }

        private bool TryMoveInvestigationIntent(float deltaTime, float timeSeconds, float speedMultiplier)
        {
            if (!HasActiveInvestigation(timeSeconds) || awarenessState == EnemyAwarenessState.Engaged)
            {
                return false;
            }

            var delta = lastStimulusLocalPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.04f)
            {
                FacePosition(lastStimulusLocalPosition);
                return Disposition is EnemyInstinctDisposition.Sentinel or EnemyInstinctDisposition.Territorial;
            }

            if (Disposition == EnemyInstinctDisposition.Prey)
            {
                MoveInDirection(-delta.normalized, deltaTime, Mathf.Max(0.55f, speedMultiplier), intent: EnemyNavigationIntent.Flee);
                return true;
            }

            if (Disposition == EnemyInstinctDisposition.Sentinel)
            {
                FacePosition(lastStimulusLocalPosition);
                return true;
            }

            MoveInDirection(delta.normalized, deltaTime, Mathf.Min(0.85f, speedMultiplier), intent: EnemyNavigationIntent.Investigate);
            return true;
        }

        private bool HasActiveInvestigation(float timeSeconds)
        {
            return timeSeconds <= investigationEndTime && lastStimulusTime > float.NegativeInfinity;
        }

        private void MoveAwayFromPlayer(float deltaTime, float speedMultiplier)
        {
            if (playerController == null)
            {
                return;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            MoveInDirection(-delta.normalized, deltaTime, speedMultiplier, intent: EnemyNavigationIntent.Flee);
        }

        public bool CanStartBudgetedAttack(float timeSeconds)
        {
            if (!IsAlive || playerController == null || IsInspectionFrozen || IsInEntryGrace(timeSeconds) || bossRuntime != null || behaviorId == EnemyBehaviorId.BossWarden)
            {
                return false;
            }

            if (IsRangedBudgetBehavior())
            {
                return CanStartRangedAttack(timeSeconds);
            }

            return behaviorId == EnemyBehaviorId.Charger && CanStartChargeAttack(timeSeconds);
        }

        public bool CanStartBudgetedMeleeAttack(float timeSeconds)
        {
            return CanStartMeleeLunge(timeSeconds);
        }

        public bool CanStartBehaviorMeleeAction(string actionId, float timeSeconds)
        {
            return CanStartMeleeAction(actionId, timeSeconds);
        }

        public bool CanStartBehaviorAreaAction(string actionId, float timeSeconds)
        {
            return CanStartAreaAction(actionId, timeSeconds);
        }

        public bool CanStartBehaviorGuardAction(string actionId, float timeSeconds)
        {
            return CanStartGuardAction(actionId, timeSeconds);
        }

        public bool CanStartBehaviorCreatureMoveAction(string actionId, float timeSeconds)
        {
            return CanStartCreatureMoveAction(actionId, timeSeconds);
        }

        public bool CanStartBehaviorCreatureSignalAction(string actionId, float timeSeconds)
        {
            return CanStartCreatureSignalAction(actionId, timeSeconds);
        }

        public bool CanStartBehaviorRangedAction(string actionId, float timeSeconds)
        {
            return CanStartRangedAction(actionId, timeSeconds);
        }

        public bool CanStartBehaviorCommand(EnemyBehaviorCommandKind commandKind, string actionId, float timeSeconds)
        {
            return commandKind switch
            {
                EnemyBehaviorCommandKind.StartMeleeAction => CanStartMeleeAction(actionId, timeSeconds),
                EnemyBehaviorCommandKind.StartRangedAction => CanStartRangedAction(actionId, timeSeconds),
                EnemyBehaviorCommandKind.StartChargeAction => CanStartChargeAttack(timeSeconds),
                EnemyBehaviorCommandKind.StartAreaAction => CanStartAreaAction(actionId, timeSeconds),
                EnemyBehaviorCommandKind.StartGuardAction => CanStartGuardAction(actionId, timeSeconds),
                EnemyBehaviorCommandKind.StartCreatureMoveAction => CanStartCreatureMoveAction(actionId, timeSeconds),
                EnemyBehaviorCommandKind.StartCreatureSignalAction => CanStartCreatureSignalAction(actionId, timeSeconds),
                _ => false
            };
        }

        public EnemyAttackProfileDefinition ResolveAttackProfileForAi(string attackId)
        {
            if (Definition == null || string.IsNullOrWhiteSpace(attackId))
            {
                return null;
            }

            return Definition.ResolveAttackProfile(attackId);
        }

        public float AttackPriorityScore(float timeSeconds)
        {
            var distance = DistanceToPlayer();
            var distanceScore = Mathf.Clamp(8f - distance, 0f, 8f);
            var behaviorScore = behaviorId switch
            {
                EnemyBehaviorId.Charger => 1.25f,
                EnemyBehaviorId.SpittingPod => 0.95f,
                EnemyBehaviorId.PowderGunner => 1.18f,
                EnemyBehaviorId.HollowArcher => 1.05f,
                EnemyBehaviorId.KnifeThrower => 1.08f,
                EnemyBehaviorId.RepeaterTurret => 1.12f,
                EnemyBehaviorId.ClockworkSentry => 1.1f,
                EnemyBehaviorId.HollowAcolyte => 1.08f,
                EnemyBehaviorId.Wraith => 1.12f,
                EnemyBehaviorId.SoulEater => 1.16f,
                EnemyBehaviorId.CurseBinder => 1.2f,
                EnemyBehaviorId.GraveLantern => 1.15f,
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
                MoveInDirection(direction, deltaTime, 0.85f, intent: EnemyNavigationIntent.Flee);
                return;
            }

            if (Disposition == EnemyInstinctDisposition.Sentinel && !ShouldSentinelEngage(distanceToPlayer, timeSeconds))
            {
                TickSentinelHold(deltaTime);
                return;
            }

            if (Disposition == EnemyInstinctDisposition.Mindless && ShouldMindlessWander(timeSeconds, distanceToPlayer))
            {
                MoveInDirection(ResolveInstinctWanderDirection(timeSeconds), deltaTime, 0.55f, intent: EnemyNavigationIntent.Wander);
                return;
            }

            if (UsesDirectInstinctPressure())
            {
                TickChase(deltaTime, timeSeconds: timeSeconds);
                return;
            }

            var rangeDirection = ResolvePreferredRangeDirection(delta.normalized, distanceToPlayer);
            var speedMultiplier = RangeIntentSpeedMultiplier(rangeDirection, delta.normalized, distanceToPlayer);
            MoveInDirection(rangeDirection, deltaTime, speedMultiplier, intent: EnemyNavigationIntent.PreferredRange);
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
                    MoveInDirection(-toPlayer, deltaTime, 1.15f, intent: EnemyNavigationIntent.Flee);
                    return;
                }

                var spacing = ResolveCurrentActionSpacing(distanceToPlayer);
                if (awarenessState == EnemyAwarenessState.Alerted && distanceToPlayer <= spacing.CommitRangeMaxMeters + spacing.LongToleranceMeters + 0.4f)
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
                    MoveInDirection(pressure, deltaTime, 0.95f, intent: EnemyNavigationIntent.PreferredRange);
                    return;
                }

                MoveInDirection(ResolveCritterWanderDirection(timeSeconds), deltaTime, 0.9f, intent: EnemyNavigationIntent.Wander);
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
                    MoveInDirection((-toPlayer + jitter * 0.35f).normalized, deltaTime, 1.1f, intent: EnemyNavigationIntent.Flee);
                    return;
                }

                if ((critterFightDecision || endangered) && TryMeleeLunge(timeSeconds))
                {
                    return;
                }

                var direction = critterFightDecision && awarenessState == EnemyAwarenessState.Engaged
                    ? (toPlayer + ResolveCritterWanderDirection(timeSeconds) * 0.35f).normalized
                    : ResolveCritterWanderDirection(timeSeconds);
                MoveInDirection(direction, deltaTime, 1f, intent: critterFightDecision ? EnemyNavigationIntent.MoveToPlayer : EnemyNavigationIntent.Wander);
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
            return TryChargeAttack(timeSeconds, behaviorId == EnemyBehaviorId.Charger ? "ash_charge" : ContactAttackId());
        }

        private bool TryChargeAttack(float timeSeconds, string actionId)
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

            activeChargeProfile = !string.IsNullOrWhiteSpace(actionId)
                ? Definition.ResolveAttackProfile(actionId)
                : ResolveChargeAttackProfile();
            ResetSpacingResetChain();
            StartReadabilityState(
                EnemyReadabilityState.ChargeWindup,
                ResolvedWindupSeconds(activeChargeProfile, ChargeWindupSeconds),
                timeSeconds,
                delta.normalized);
            return true;
        }

        private void TickChase(float deltaTime, bool allowSteering = true, float timeSeconds = float.NaN)
        {
            if (allowSteering && TryMoveTowardTacticalReservation(deltaTime, 1f, EnemyNavigationIntent.MoveToPlayer))
            {
                return;
            }

            if (allowSteering &&
                behaviorId == EnemyBehaviorId.Charger &&
                readabilityState == EnemyReadabilityState.Idle &&
                awarenessState >= EnemyAwarenessState.Alerted &&
                !float.IsNaN(timeSeconds) &&
                timeSeconds >= nextAllowedChargeTime &&
                IsAttackLineRepositionNeeded("ash_charge", EnemyAttackRuntimeKind.Charge) &&
                TryMoveTowardClearAttackReposition(
                    "ash_charge",
                    deltaTime,
                    0.9f,
                    EnemyNavigationIntent.PreferredRange,
                    "charge_line_blocked"))
            {
                return;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            MoveInDirection(delta.normalized, deltaTime, 1f, allowSteering, EnemyNavigationIntent.MoveToPlayer);
        }

        private bool TryRangedAttack(float timeSeconds)
        {
            return TryRangedAttack(timeSeconds, string.Empty);
        }

        private bool TryRangedAttack(float timeSeconds, string actionId)
        {
            if (!CanStartRangedAction(actionId, timeSeconds))
            {
                return false;
            }

            if (RequiresAttackBudget() && !TryReserveAttackBudget(timeSeconds))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            activeRangedProfile = !string.IsNullOrWhiteSpace(actionId)
                ? Definition.ResolveAttackProfile(actionId)
                : ResolveRangedAttackProfile(timeSeconds);
            ResetSpacingResetChain();
            StartReadabilityState(
                EnemyReadabilityState.RangedWindup,
                ResolvedWindupSeconds(activeRangedProfile, RangedWindupSeconds),
                timeSeconds,
                delta.normalized);
            BeginRangedTelegraph(activeRangedProfile, timeSeconds);
            return true;
        }

        private bool TryMeleeLunge(float timeSeconds)
        {
            return TryMeleeLunge(timeSeconds, string.Empty);
        }

        private bool TryMeleeLunge(float timeSeconds, string actionId)
        {
            if (!CanStartMeleeAction(actionId, timeSeconds))
            {
                return false;
            }

            if (RequiresAttackBudget() && !TryReserveMeleeAttackBudget(timeSeconds))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            activeMeleeProfile = !string.IsNullOrWhiteSpace(actionId)
                ? Definition.ResolveAttackProfile(actionId)
                : ResolveLungeAttackProfile(timeSeconds);
            activeMeleeComboDepth = 0;
            ResetSpacingResetChain();
            StartReadabilityState(
                EnemyReadabilityState.MeleeWindup,
                ResolvedWindupSeconds(activeMeleeProfile, LungeWindupSeconds),
                timeSeconds,
                delta.normalized);
            return true;
        }

        private bool TryStartGuardAction(float timeSeconds, string actionId)
        {
            if (!CanStartGuardAction(actionId, timeSeconds))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            activeGuardActionProfile = Definition.ResolveAttackProfile(actionId);
            activeGuardProfile = null;
            ResetSpacingResetChain();
            StartReadabilityState(
                EnemyReadabilityState.GuardWindup,
                ResolvedWindupSeconds(activeGuardActionProfile, 0.12f),
                timeSeconds,
                delta.sqrMagnitude > 0.01f ? delta.normalized : FacingDirection);
            return true;
        }

        private bool TryStartCreatureMoveAction(float timeSeconds, string actionId)
        {
            if (!CanStartCreatureMoveAction(actionId, timeSeconds))
            {
                return false;
            }

            activeCreatureMoveProfile = Definition.ResolveAttackProfile(actionId);
            activeCreatureMoveDirection = ResolveCreatureMoveDirection(actionId, timeSeconds);
            nextAllowedAttackTime = timeSeconds + (activeCreatureMoveProfile != null ? activeCreatureMoveProfile.CooldownSeconds : 0.9f);
            ResetSpacingResetChain();
            StartReadabilityState(
                EnemyReadabilityState.CreatureMoveWindup,
                ResolvedWindupSeconds(activeCreatureMoveProfile, 0.08f),
                timeSeconds,
                activeCreatureMoveDirection);
            return true;
        }

        private bool TryStartCreatureSignalAction(float timeSeconds, string actionId)
        {
            if (!CanStartCreatureSignalAction(actionId, timeSeconds))
            {
                return false;
            }

            activeCreatureSignalProfile = Definition.ResolveAttackProfile(actionId);
            creatureSignalEmitted = false;
            nextAllowedAttackTime = timeSeconds + (activeCreatureSignalProfile != null ? activeCreatureSignalProfile.CooldownSeconds : 1.5f);
            ResetSpacingResetChain();
            StartReadabilityState(
                EnemyReadabilityState.CreatureSignalWindup,
                ResolvedWindupSeconds(activeCreatureSignalProfile, 0.18f),
                timeSeconds,
                playerController != null ? playerController.transform.localPosition - transform.localPosition : FacingDirection);
            return true;
        }

        private bool TryStartMeleeComboFollowUp(float timeSeconds)
        {
            if (activeMeleeComboDepth >= 1 ||
                activeMeleeProfile == null ||
                Definition == null ||
                playerController == null ||
                !IsAlive ||
                IsInspectionFrozen ||
                IsInEntryGrace(timeSeconds) ||
                awarenessState != EnemyAwarenessState.Engaged ||
                string.IsNullOrWhiteSpace(activeMeleeProfile.ComboFollowUpAttackId))
            {
                return false;
            }

            var followUp = Definition.ResolveAttackProfile(activeMeleeProfile.ComboFollowUpAttackId);
            if (followUp == null || !IsMeleeRuntimeKind(followUp.RuntimeKind))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f || delta.magnitude > Mathf.Max(0.1f, followUp.RangeMeters))
            {
                return false;
            }

            if (RequiresAttackBudget() && !TryReserveMeleeAttackBudget(timeSeconds))
            {
                return false;
            }

            activeMeleeComboDepth++;
            activeMeleeProfile = followUp;
            lungeContactAttempted = false;
            lungeEndTime = 0f;
            ResetSpacingResetChain();
            StartReadabilityState(
                EnemyReadabilityState.MeleeWindup,
                ResolvedWindupSeconds(followUp, LungeWindupSeconds),
                timeSeconds,
                delta.normalized);
            return true;
        }

        private static bool IsMeleeRuntimeKind(EnemyAttackRuntimeKind runtimeKind)
        {
            return runtimeKind is EnemyAttackRuntimeKind.MeleeLunge
                or EnemyAttackRuntimeKind.Contact
                or EnemyAttackRuntimeKind.WeaponMelee;
        }

        private bool TryAreaAttack(float timeSeconds, string actionId)
        {
            if (!CanStartAreaAction(actionId, timeSeconds))
            {
                return false;
            }

            if (RequiresAttackBudget() && !TryReserveMeleeAttackBudget(timeSeconds))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            activeAreaProfile = Definition.ResolveAttackProfile(actionId);
            areaDamageAttempted = false;
            ResetSpacingResetChain();
            StartReadabilityState(
                EnemyReadabilityState.AreaWindup,
                ResolvedWindupSeconds(activeAreaProfile, LungeWindupSeconds),
                timeSeconds,
                delta.sqrMagnitude > 0.01f ? delta.normalized : FacingDirection);
            return true;
        }

        private bool TryStartFeintWarning(float timeSeconds, string actionId)
        {
            if (playerController == null ||
                Definition == null ||
                readabilityState != EnemyReadabilityState.Idle ||
                timeSeconds < nextAllowedAttackTime ||
                IsInEntryGrace(timeSeconds))
            {
                return false;
            }

            var profile = Definition.ResolveAttackProfile(actionId);
            if (profile == null || profile.RuntimeKind != EnemyAttackRuntimeKind.Movement)
            {
                return false;
            }

            if (DistanceToPlayer() > Mathf.Max(0.1f, profile.RangeMeters))
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            activeWarningProfile = profile;
            nextAllowedAttackTime = timeSeconds + profile.CooldownSeconds;
            warningEndTime = timeSeconds +
                ResolvedWindupSeconds(profile, 0.2f) +
                ResolvedActiveSeconds(profile, 0.1f) +
                ResolvedRecoverySeconds(profile);
            ResetSpacingResetChain();
            ForceEngaged();
            StartReadabilityState(
                EnemyReadabilityState.FeintWarning,
                warningEndTime - timeSeconds,
                timeSeconds,
                delta.sqrMagnitude > 0.01f ? delta.normalized : FacingDirection);
            return true;
        }

        private bool CanStartMeleeLunge(float timeSeconds)
        {
            return CanStartMeleeAction(string.Empty, timeSeconds);
        }

        private bool CanStartMeleeAction(string actionId, float timeSeconds)
        {
            var profile = !string.IsNullOrWhiteSpace(actionId) && Definition != null ? Definition.ResolveAttackProfile(actionId) : null;
            var hasExplicitMeleeProfile = profile != null &&
                                          profile.RuntimeKind is EnemyAttackRuntimeKind.MeleeLunge
                                              or EnemyAttackRuntimeKind.Contact
                                              or EnemyAttackRuntimeKind.WeaponMelee;
            if (!IsAlive ||
                playerController == null ||
                Definition == null ||
                IsInspectionFrozen ||
                IsInEntryGrace(timeSeconds) ||
                bossRuntime != null ||
                behaviorId == EnemyBehaviorId.TurretShooter ||
                readabilityState != EnemyReadabilityState.Idle ||
                timeSeconds < nextAllowedLungeTime)
            {
                return false;
            }

            if (profile != null && !hasExplicitMeleeProfile)
            {
                return false;
            }

            if (profile == null && (behaviorId == EnemyBehaviorId.Charger || !LungeAttackEnabled))
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
            var triggerRange = Mathf.Max(LungeTriggerRangeMeters, profile != null ? profile.RangeMeters : 0f);
            var requiresReadableFloor = profile == null || profile.RuntimeKind == EnemyAttackRuntimeKind.MeleeLunge;
            return delta.sqrMagnitude >= 0.01f &&
                   (!requiresReadableFloor || distance >= minimumReadableRange) &&
                   CanCommitActionAtDistance(actionId, profile, distance, triggerRange) &&
                   CanReachPlayerWithAttack(actionId, profile, profile != null ? profile.RuntimeKind : EnemyAttackRuntimeKind.MeleeLunge);
        }

        private bool CanStartAreaAction(string actionId, float timeSeconds)
        {
            if (!IsAlive ||
                playerController == null ||
                Definition == null ||
                IsInspectionFrozen ||
                IsInEntryGrace(timeSeconds) ||
                bossRuntime != null ||
                readabilityState != EnemyReadabilityState.Idle ||
                timeSeconds < nextAllowedLungeTime ||
                !CanUseMeleeLungeForAwareness(timeSeconds))
            {
                return false;
            }

            var profile = Definition.ResolveAttackProfile(actionId);
            if (profile == null || profile.RuntimeKind != EnemyAttackRuntimeKind.Area)
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            return delta.sqrMagnitude >= 0.01f &&
                   CanCommitActionAtDistance(actionId, profile, delta.magnitude, profile.RangeMeters) &&
                   CanReachPlayerWithAttack(actionId, profile, EnemyAttackRuntimeKind.Area);
        }

        private bool CanStartGuardAction(string actionId, float timeSeconds)
        {
            if (!IsAlive ||
                playerController == null ||
                Definition == null ||
                GuardProfile == null ||
                IsInspectionFrozen ||
                IsInEntryGrace(timeSeconds) ||
                bossRuntime != null ||
                behaviorId == EnemyBehaviorId.BossWarden ||
                readabilityState != EnemyReadabilityState.Idle ||
                timeSeconds < nextAllowedAttackTime)
            {
                return false;
            }

            var profile = Definition.ResolveAttackProfile(actionId);
            if (profile == null || profile.RuntimeKind != EnemyAttackRuntimeKind.Defense)
            {
                return false;
            }

            if (awarenessState < EnemyAwarenessState.Alerted)
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            return delta.sqrMagnitude >= 0.01f &&
                   CanCommitActionAtDistance(actionId, profile, delta.magnitude, profile.RangeMeters);
        }

        private bool CanStartCreatureMoveAction(string actionId, float timeSeconds)
        {
            if (!IsAlive ||
                playerController == null ||
                Definition == null ||
                IsInspectionFrozen ||
                IsInEntryGrace(timeSeconds) ||
                bossRuntime != null ||
                readabilityState != EnemyReadabilityState.Idle ||
                timeSeconds < nextAllowedAttackTime)
            {
                return false;
            }

            var profile = Definition.ResolveAttackProfile(actionId);
            if (profile == null || profile.RuntimeKind is not (EnemyAttackRuntimeKind.CreatureMove or EnemyAttackRuntimeKind.PhaseMove))
            {
                return false;
            }

            if (awarenessState < EnemyAwarenessState.Alerted && !IsEndangered(timeSeconds))
            {
                return false;
            }

            return CanCommitActionAtDistance(actionId, profile, DistanceToPlayer(), profile.RangeMeters);
        }

        private bool CanStartCreatureSignalAction(string actionId, float timeSeconds)
        {
            if (!IsAlive ||
                playerController == null ||
                Definition == null ||
                IsInspectionFrozen ||
                IsInEntryGrace(timeSeconds) ||
                bossRuntime != null ||
                readabilityState != EnemyReadabilityState.Idle ||
                timeSeconds < nextAllowedAttackTime)
            {
                return false;
            }

            var profile = Definition.ResolveAttackProfile(actionId);
            if (profile == null || profile.RuntimeKind != EnemyAttackRuntimeKind.CreatureSignal)
            {
                return false;
            }

            if (awarenessState < EnemyAwarenessState.Alerted && !IsEndangered(timeSeconds))
            {
                return false;
            }

            return CanCommitActionAtDistance(actionId, profile, DistanceToPlayer(), profile.RangeMeters);
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
                Definition == null ||
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
            var profile = Definition != null
                ? Definition.ResolveAttackProfile(behaviorId == EnemyBehaviorId.Charger ? "ash_charge" : ContactAttackId())
                : null;
            return delta.sqrMagnitude >= 0.01f &&
                   CanCommitActionAtDistance(profile != null ? profile.AttackId : string.Empty, profile, delta.magnitude, Definition.AttackRangeMeters) &&
                   CanReachPlayerWithAttack(profile != null ? profile.AttackId : string.Empty, profile, EnemyAttackRuntimeKind.Charge);
        }

        private bool CanStartRangedAttack(float timeSeconds)
        {
            return CanStartRangedAction(string.Empty, timeSeconds);
        }

        private bool CanStartRangedAction(string actionId, float timeSeconds)
        {
            if (!IsAlive ||
                playerController == null ||
                Definition == null ||
                IsInspectionFrozen ||
                IsInEntryGrace(timeSeconds) ||
                bossRuntime != null ||
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
            var profile = !string.IsNullOrWhiteSpace(actionId) ? Definition.ResolveAttackProfile(actionId) : ResolveRangedAttackProfile(timeSeconds);
            if (!string.IsNullOrWhiteSpace(actionId) && profile == null)
            {
                return false;
            }

            if (profile != null &&
                profile.RuntimeKind is not (EnemyAttackRuntimeKind.Projectile
                    or EnemyAttackRuntimeKind.FanProjectile
                    or EnemyAttackRuntimeKind.RadialProjectile
                    or EnemyAttackRuntimeKind.Beam
                    or EnemyAttackRuntimeKind.SequentialRadialProjectile
                    or EnemyAttackRuntimeKind.LockingBeam))
            {
                return false;
            }

            var range = profile != null ? profile.RangeMeters : Definition.AttackRangeMeters;
            return delta.sqrMagnitude >= 0.01f &&
                   CanCommitActionAtDistance(actionId, profile, delta.magnitude, range) &&
                   CanReachPlayerWithAttack(actionId, profile, profile != null ? profile.RuntimeKind : EnemyAttackRuntimeKind.Projectile);
        }

        private bool RequiresAttackBudget()
        {
            return bossRuntime == null && behaviorId != EnemyBehaviorId.BossWarden && archetypeId != EnemyArchetypeId.Boss;
        }

        private bool IsRangedBudgetBehavior()
        {
            return behaviorId is EnemyBehaviorId.TurretShooter
                or EnemyBehaviorId.SpittingPod
                or EnemyBehaviorId.HollowArcher
                or EnemyBehaviorId.PowderGunner
                or EnemyBehaviorId.KnifeThrower
                or EnemyBehaviorId.RepeaterTurret
                or EnemyBehaviorId.ClockworkSentry
                or EnemyBehaviorId.HollowAcolyte
                or EnemyBehaviorId.Wraith
                or EnemyBehaviorId.SoulEater
                or EnemyBehaviorId.CurseBinder
                or EnemyBehaviorId.GraveLantern
                or EnemyBehaviorId.OctantSentry
                or EnemyBehaviorId.RailSpider;
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
                BeginRecoveryMovement(activeChargeProfile);
                return true;
            }

            if (readabilityState == EnemyReadabilityState.ChargeRecovery)
            {
                if (timeSeconds < chargeRecoveryEndTime)
                {
                    TickRecoveryMovement(deltaTime, activeChargeProfile);
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                chargeEndTime = 0f;
                chargeRecoveryEndTime = 0f;
                activeChargeProfile = null;
                chargeContactAttempted = false;
                ClearRecoveryMovement();
                return false;
            }

            if (readabilityState == EnemyReadabilityState.RangedWindup)
            {
                TickLockingBeamTelegraph(timeSeconds);
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                var profile = activeRangedProfile ?? ResolveRangedAttackProfile(timeSeconds);
                nextAllowedAttackTime = timeSeconds + (profile != null ? profile.CooldownSeconds : Definition.AttackCooldownSeconds);
                if (!rangedProjectileFired)
                {
                    FireRangedPattern(TelegraphDirection, profile, timeSeconds);
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
                BeginRecoveryMovement(activeRangedProfile);
                return true;
            }

            if (readabilityState == EnemyReadabilityState.RangedRecovery)
            {
                if (timeSeconds < rangedRecoveryEndTime)
                {
                    TickRecoveryMovement(deltaTime, activeRangedProfile);
                    return true;
                }

                activeRangedProfile = null;
                rangedProjectileFired = false;
                rangedActiveEndTime = 0f;
                rangedRecoveryEndTime = 0f;
                ClearRangedTelegraphState();
                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                ClearRecoveryMovement();
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

                if (TryStartMeleeComboFollowUp(timeSeconds))
                {
                    return true;
                }

                lungeRecoveryEndTime = timeSeconds + ResolvedRecoverySeconds(activeMeleeProfile);
                readabilityState = EnemyReadabilityState.MeleeRecovery;
                readabilityStateEndTime = lungeRecoveryEndTime;
                lungeEndTime = 0f;
                BeginRecoveryMovement(activeMeleeProfile);
                return true;
            }

            if (readabilityState == EnemyReadabilityState.MeleeRecovery)
            {
                if (timeSeconds < lungeRecoveryEndTime)
                {
                    TickRecoveryMovement(deltaTime, activeMeleeProfile);
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                lungeRecoveryEndTime = 0f;
                activeMeleeProfile = null;
                lungeContactAttempted = false;
                activeMeleeComboDepth = 0;
                ClearRecoveryMovement();
                return false;
            }

            if (readabilityState == EnemyReadabilityState.AreaWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                var profile = activeAreaProfile;
                areaActiveEndTime = timeSeconds + ResolvedActiveSeconds(profile, 0.18f);
                nextAllowedLungeTime = areaActiveEndTime + (profile != null ? profile.CooldownSeconds : LungeCooldownSeconds);
                areaDamageAttempted = false;
                readabilityState = EnemyReadabilityState.AreaActive;
                readabilityStateEndTime = areaActiveEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.AreaActive)
            {
                if (timeSeconds < areaActiveEndTime)
                {
                    TryApplyAreaDamage(timeSeconds);
                    return true;
                }

                areaRecoveryEndTime = timeSeconds + ResolvedRecoverySeconds(activeAreaProfile);
                readabilityState = EnemyReadabilityState.AreaRecovery;
                readabilityStateEndTime = areaRecoveryEndTime;
                areaActiveEndTime = 0f;
                BeginRecoveryMovement(activeAreaProfile);
                return true;
            }

            if (readabilityState == EnemyReadabilityState.AreaRecovery)
            {
                if (timeSeconds < areaRecoveryEndTime)
                {
                    TickRecoveryMovement(deltaTime, activeAreaProfile);
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                areaRecoveryEndTime = 0f;
                activeAreaProfile = null;
                areaDamageAttempted = false;
                ClearRecoveryMovement();
                return false;
            }

            if (readabilityState == EnemyReadabilityState.FeintWarning)
            {
                if (timeSeconds < warningEndTime)
                {
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                warningEndTime = 0f;
                activeWarningProfile = null;
                return false;
            }

            if (readabilityState == EnemyReadabilityState.GuardWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                activeGuardProfile = GuardProfile;
                var profile = activeGuardActionProfile;
                guardActiveEndTime = timeSeconds + ResolvedActiveSeconds(profile, 0.65f);
                nextAllowedAttackTime = timeSeconds + (profile != null ? profile.CooldownSeconds : 1.25f);
                readabilityState = EnemyReadabilityState.GuardActive;
                readabilityStateEndTime = guardActiveEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.GuardActive)
            {
                if (timeSeconds < guardActiveEndTime)
                {
                    return true;
                }

                activeGuardProfile = null;
                guardRecoveryEndTime = timeSeconds + ResolvedRecoverySeconds(activeGuardActionProfile);
                readabilityState = EnemyReadabilityState.GuardRecovery;
                readabilityStateEndTime = guardRecoveryEndTime;
                BeginRecoveryMovement(activeGuardActionProfile);
                return true;
            }

            if (readabilityState == EnemyReadabilityState.GuardRecovery)
            {
                if (timeSeconds < guardRecoveryEndTime)
                {
                    TickRecoveryMovement(deltaTime, activeGuardActionProfile);
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                guardActiveEndTime = 0f;
                guardRecoveryEndTime = 0f;
                activeGuardActionProfile = null;
                activeGuardProfile = null;
                ClearRecoveryMovement();
                return false;
            }

            if (readabilityState == EnemyReadabilityState.CreatureMoveWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                creatureMoveActiveEndTime = timeSeconds + ResolvedActiveSeconds(activeCreatureMoveProfile, 0.22f);
                readabilityState = EnemyReadabilityState.CreatureMoveActive;
                readabilityStateEndTime = creatureMoveActiveEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.CreatureMoveActive)
            {
                if (timeSeconds < creatureMoveActiveEndTime)
                {
                    MoveActiveCreatureBurst(deltaTime);
                    return true;
                }

                creatureMoveRecoveryEndTime = timeSeconds + ResolvedRecoverySeconds(activeCreatureMoveProfile);
                readabilityState = EnemyReadabilityState.CreatureMoveRecovery;
                readabilityStateEndTime = creatureMoveRecoveryEndTime;
                BeginRecoveryMovement(activeCreatureMoveProfile);
                return true;
            }

            if (readabilityState == EnemyReadabilityState.CreatureMoveRecovery)
            {
                if (timeSeconds < creatureMoveRecoveryEndTime)
                {
                    TickRecoveryMovement(deltaTime, activeCreatureMoveProfile);
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                creatureMoveActiveEndTime = 0f;
                creatureMoveRecoveryEndTime = 0f;
                activeCreatureMoveProfile = null;
                activeCreatureMoveDirection = Vector3.forward;
                ClearRecoveryMovement();
                return false;
            }

            if (readabilityState == EnemyReadabilityState.CreatureSignalWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                creatureSignalActiveEndTime = timeSeconds + ResolvedActiveSeconds(activeCreatureSignalProfile, 0.12f);
                readabilityState = EnemyReadabilityState.CreatureSignalActive;
                readabilityStateEndTime = creatureSignalActiveEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.CreatureSignalActive)
            {
                if (!creatureSignalEmitted)
                {
                    EmitSameFamilyCreatureSignal(timeSeconds);
                    creatureSignalEmitted = true;
                }

                if (timeSeconds < creatureSignalActiveEndTime)
                {
                    return true;
                }

                creatureSignalRecoveryEndTime = timeSeconds + ResolvedRecoverySeconds(activeCreatureSignalProfile);
                readabilityState = EnemyReadabilityState.CreatureSignalRecovery;
                readabilityStateEndTime = creatureSignalRecoveryEndTime;
                BeginRecoveryMovement(activeCreatureSignalProfile);
                return true;
            }

            if (readabilityState == EnemyReadabilityState.CreatureSignalRecovery)
            {
                if (timeSeconds < creatureSignalRecoveryEndTime)
                {
                    TickRecoveryMovement(deltaTime, activeCreatureSignalProfile);
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                creatureSignalActiveEndTime = 0f;
                creatureSignalRecoveryEndTime = 0f;
                creatureSignalEmitted = false;
                activeCreatureSignalProfile = null;
                ClearRecoveryMovement();
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
            if (state != EnemyReadabilityState.RangedWindup && state != EnemyReadabilityState.RangedActive)
            {
                ClearRangedTelegraphState();
            }

            if (state != EnemyReadabilityState.Idle)
            {
                StopNavMeshAgent(clearPath: true);
            }

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
            ReceiveStimulus(kind, stimulusLocalPosition, timeSeconds, EnemyStimulusTierExtensions.DefaultFor(kind), string.Empty);
        }

        public void ReceiveStimulus(EnemyStimulusKind kind, Vector3 stimulusLocalPosition, float timeSeconds, EnemyStimulusTier tier)
        {
            ReceiveStimulus(kind, stimulusLocalPosition, timeSeconds, tier, string.Empty);
        }

        public void ReceiveStimulus(EnemyStimulusKind kind, Vector3 stimulusLocalPosition, float timeSeconds, EnemyStimulusTier tier, string context)
        {
            if (!CanReceiveStimulus(kind, stimulusLocalPosition, tier))
            {
                return;
            }

            var previousAwareness = awarenessState;
            var distance = Vector3.Distance(Flat(transform.localPosition), Flat(stimulusLocalPosition));
            var hearingRadius = EffectiveHearingRadius(kind, tier);

            RecordStimulus(kind, tier, stimulusLocalPosition, timeSeconds, distance, hearingRadius, context);

            if (kind == EnemyStimulusKind.Damage || tier == EnemyStimulusTier.Violent)
            {
                ForceEngaged("violent_disturbance");
                TryShareAllyAlertAfterStimulus(previousAwareness, kind, tier, timeSeconds, "violent_disturbance");
                return;
            }

            if (kind is EnemyStimulusKind.Proximity or EnemyStimulusKind.Bump)
            {
                ApplyCloseDisturbance(kind, tier);
                TryShareAllyAlertAfterStimulus(previousAwareness, kind, tier, timeSeconds, kind.ToString());
                return;
            }

            if (kind == EnemyStimulusKind.AllyAlert)
            {
                ApplyAllyAlertDisturbance(tier);
                return;
            }

            if (kind == EnemyStimulusKind.Footstep && tier == EnemyStimulusTier.Quiet)
            {
                ApplyFootstepDisturbance();
                TryShareAllyAlertAfterStimulus(previousAwareness, kind, tier, timeSeconds, "footstep");
                return;
            }

            ApplyActionDisturbance(kind, tier);
            TryShareAllyAlertAfterStimulus(previousAwareness, kind, tier, timeSeconds, kind.ToString());
        }

        public bool CanReceiveStimulus(EnemyStimulusKind kind, Vector3 stimulusLocalPosition, EnemyStimulusTier tier)
        {
            if (!IsAlive || bossRuntime != null || bossDefinition != null || archetypeId == EnemyArchetypeId.Boss)
            {
                return false;
            }

            var distance = Vector3.Distance(Flat(transform.localPosition), Flat(stimulusLocalPosition));
            var bypassHearing = kind is EnemyStimulusKind.Damage or EnemyStimulusKind.Proximity or EnemyStimulusKind.Bump ||
                                tier == EnemyStimulusTier.Violent;
            var hearingRadius = EffectiveHearingRadius(kind, tier);
            return bypassHearing || (hearingRadius > 0f && distance <= hearingRadius);
        }

        private void RecordStimulus(
            EnemyStimulusKind kind,
            EnemyStimulusTier tier,
            Vector3 stimulusLocalPosition,
            float timeSeconds,
            float distance,
            float hearingRadius,
            string context)
        {
            if (lastStimulusTime > float.NegativeInfinity &&
                timeSeconds - lastStimulusTime > InvestigationDurationSeconds * 1.75f)
            {
                currentDisturbanceScore *= 0.35f;
            }

            var distanceWeight = kind is EnemyStimulusKind.Proximity or EnemyStimulusKind.Bump
                ? 1.25f
                : Mathf.Lerp(1.25f, 0.75f, Mathf.Clamp01(distance / Mathf.Max(0.1f, hearingRadius)));
            currentDisturbanceScore = Mathf.Clamp(
                currentDisturbanceScore + tier.Score() * distanceWeight,
                0f,
                DisturbanceEscalationThreshold * 3f);
            lastStimulusKind = kind;
            lastStimulusTier = tier;
            lastStimulusTime = timeSeconds;
            lastStimulusLocalPosition = stimulusLocalPosition;
            investigationEndTime = timeSeconds + InvestigationDurationSeconds;
            lastAwarenessReason = string.IsNullOrWhiteSpace(context)
                ? $"{kind}:{tier}"
                : $"{kind}:{tier}:{context}";
            FacePosition(stimulusLocalPosition);
        }

        private void ApplyFootstepDisturbance()
        {
            switch (Disposition)
            {
                case EnemyInstinctDisposition.Prey:
                    StepSoftAwareness("prey_footstep");
                    return;
                case EnemyInstinctDisposition.Sentinel:
                    SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "sentinel_heard_footstep");
                    if (currentDisturbanceScore >= DisturbanceEscalationThreshold)
                    {
                        ForceEngaged("sentinel_footstep_threshold");
                    }

                    return;
                case EnemyInstinctDisposition.Territorial:
                    if (awarenessState == EnemyAwarenessState.Alerted &&
                        currentDisturbanceScore >= DisturbanceEscalationThreshold)
                    {
                        ForceEngaged("territorial_repeated_footstep");
                    }
                    else
                    {
                        SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "territorial_warning_footstep");
                    }

                    return;
                case EnemyInstinctDisposition.Mindless:
                    SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "mindless_heard_footstep");
                    if (currentDisturbanceScore >= DisturbanceEscalationThreshold)
                    {
                        ForceEngaged("mindless_footstep_pressure");
                    }

                    return;
                default:
                    if (currentDisturbanceScore >= DisturbanceEscalationThreshold)
                    {
                        ForceEngaged("predator_footstep_threshold");
                    }
                    else
                    {
                        SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "predator_investigate_footstep");
                    }

                    return;
            }
        }

        private void ApplyCloseDisturbance(EnemyStimulusKind kind, EnemyStimulusTier tier)
        {
            switch (Disposition)
            {
                case EnemyInstinctDisposition.Prey:
                    ForceEngaged($"{kind}_prey_panic");
                    return;
                case EnemyInstinctDisposition.Sentinel:
                    SetAwarenessAtLeast(EnemyAwarenessState.Alerted, $"{kind}_sentinel_hold");
                    if (currentDisturbanceScore >= DisturbanceEscalationThreshold || tier >= EnemyStimulusTier.Loud)
                    {
                        ForceEngaged($"{kind}_sentinel_threshold");
                    }

                    return;
                case EnemyInstinctDisposition.Territorial:
                    if (awarenessState == EnemyAwarenessState.Alerted ||
                        currentDisturbanceScore >= DisturbanceEscalationThreshold * 1.8f ||
                        tier >= EnemyStimulusTier.Loud)
                    {
                        ForceEngaged($"{kind}_territorial_commit");
                    }
                    else
                    {
                        SetAwarenessAtLeast(EnemyAwarenessState.Alerted, $"{kind}_territorial_warning");
                    }

                    return;
                case EnemyInstinctDisposition.Mindless:
                    ForceEngaged($"{kind}_mindless_pressure");
                    return;
                default:
                    ForceEngaged($"{kind}_predator_pressure");
                    return;
            }
        }

        private void ApplyActionDisturbance(EnemyStimulusKind kind, EnemyStimulusTier tier)
        {
            var isAttackLike = kind is EnemyStimulusKind.MeleeAttack or EnemyStimulusKind.RangedAttack or EnemyStimulusKind.GuardImpact;
            switch (Disposition)
            {
                case EnemyInstinctDisposition.Prey:
                    if (isAttackLike || currentDisturbanceScore >= DisturbanceEscalationThreshold || tier >= EnemyStimulusTier.Loud)
                    {
                        ForceEngaged("prey_panic_action");
                    }
                    else
                    {
                        SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "prey_startled_action");
                    }

                    return;
                case EnemyInstinctDisposition.Sentinel:
                    SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "sentinel_action_attention");
                    if (tier >= EnemyStimulusTier.Loud || currentDisturbanceScore >= DisturbanceEscalationThreshold)
                    {
                        ForceEngaged("sentinel_action_threshold");
                    }

                    return;
                case EnemyInstinctDisposition.Territorial:
                    if (tier >= EnemyStimulusTier.Loud ||
                        (awarenessState == EnemyAwarenessState.Alerted && currentDisturbanceScore >= DisturbanceEscalationThreshold))
                    {
                        ForceEngaged("territorial_action_commit");
                    }
                    else
                    {
                        SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "territorial_action_warning");
                    }

                    return;
                case EnemyInstinctDisposition.Mindless:
                    ForceEngaged("mindless_action_pressure");
                    return;
                default:
                    ForceEngaged("predator_action_engage");
                    return;
            }
        }

        private void ApplyAllyAlertDisturbance(EnemyStimulusTier tier)
        {
            switch (Disposition)
            {
                case EnemyInstinctDisposition.Prey:
                    if (tier >= EnemyStimulusTier.Loud)
                    {
                        SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "prey_ally_alert_panic");
                    }
                    else
                    {
                        StepSoftAwareness("prey_ally_alert_startle");
                    }

                    return;
                case EnemyInstinctDisposition.Sentinel:
                    SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "sentinel_ally_alert_hold");
                    return;
                case EnemyInstinctDisposition.Territorial:
                    SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "territorial_ally_alert_warning");
                    return;
                case EnemyInstinctDisposition.Mindless:
                    SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "mindless_ally_alert_pressure");
                    return;
                default:
                    SetAwarenessAtLeast(EnemyAwarenessState.Alerted, "predator_ally_alert_investigate");
                    return;
            }
        }

        private void UpdateAwareness(float timeSeconds, float distanceToPlayer)
        {
            if (bossRuntime != null || playerController == null)
            {
                return;
            }

            var previousAwareness = awarenessState;
            if (closeThreatTimer >= CloseThreatSeconds)
            {
                ForceEngaged();
                TryShareAllyAlertAfterStimulus(previousAwareness, EnemyStimulusKind.Proximity, EnemyStimulusTier.Loud, timeSeconds, "close_threat");
                return;
            }

            var canSeePlayer = CanSeePlayer(distanceToPlayer);
            if (canSeePlayer)
            {
                if (Disposition == EnemyInstinctDisposition.Prey && !IsEndangered(timeSeconds))
                {
                    awarenessState = MaxAwareness(awarenessState, EnemyAwarenessState.Alerted);
                    FacePlayer();
                    TryShareAllyAlertAfterStimulus(previousAwareness, EnemyStimulusKind.Proximity, EnemyStimulusTier.Normal, timeSeconds, "sight_prey");
                    return;
                }

                if (Disposition == EnemyInstinctDisposition.Sentinel &&
                    !IsEndangered(timeSeconds) &&
                    !IsWithinSentinelApproachRange(distanceToPlayer))
                {
                    awarenessState = MaxAwareness(awarenessState, EnemyAwarenessState.Alerted);
                    FacePlayer();
                    TryShareAllyAlertAfterStimulus(previousAwareness, EnemyStimulusKind.Proximity, EnemyStimulusTier.Normal, timeSeconds, "sight_sentinel_hold");
                    return;
                }

                if (Disposition == EnemyInstinctDisposition.Territorial &&
                    !IsEndangered(timeSeconds) &&
                    distanceToPlayer > PreferredRangeMaxMeters + 0.4f)
                {
                    awarenessState = MaxAwareness(awarenessState, EnemyAwarenessState.Alerted);
                    FacePlayer();
                    TryShareAllyAlertAfterStimulus(previousAwareness, EnemyStimulusKind.Proximity, EnemyStimulusTier.Normal, timeSeconds, "sight_territorial_warning");
                    return;
                }

                if (awarenessState == EnemyAwarenessState.Unaware)
                {
                    awarenessState = EnemyAwarenessState.Alerted;
                    TryShareAllyAlertAfterStimulus(previousAwareness, EnemyStimulusKind.Proximity, EnemyStimulusTier.Normal, timeSeconds, "sight_alert");
                }
                else
                {
                    ForceEngaged();
                    TryShareAllyAlertAfterStimulus(previousAwareness, EnemyStimulusKind.Proximity, EnemyStimulusTier.Loud, timeSeconds, "sight_engage");
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

        private void FacePosition(Vector3 localPosition)
        {
            var delta = localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f)
            {
                facingDirection = delta.normalized;
            }
        }

        private void ForceEngaged(string reason = "engaged")
        {
            if (awarenessState != EnemyAwarenessState.Engaged)
            {
                engagedStartTime = lastTickTime > 0f ? lastTickTime : Time.time;
            }

            awarenessState = EnemyAwarenessState.Engaged;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                lastAwarenessReason = reason;
            }
        }

        private void StepSoftAwareness(string reason)
        {
            if (awarenessState == EnemyAwarenessState.Unaware)
            {
                awarenessState = EnemyAwarenessState.Suspicious;
            }
            else if (awarenessState == EnemyAwarenessState.Suspicious)
            {
                awarenessState = EnemyAwarenessState.Alerted;
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                lastAwarenessReason = reason;
            }
        }

        private void SetAwarenessAtLeast(EnemyAwarenessState minimum, string reason)
        {
            var next = MaxAwareness(awarenessState, minimum);
            if (next != awarenessState)
            {
                awarenessState = next;
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                lastAwarenessReason = reason;
            }
        }

        private void TryShareAllyAlertAfterStimulus(
            EnemyAwarenessState previousAwareness,
            EnemyStimulusKind sourceKind,
            EnemyStimulusTier sourceTier,
            float timeSeconds,
            string reason)
        {
            if (sourceKind is EnemyStimulusKind.AllyAlert or EnemyStimulusKind.CreatureSignal)
            {
                return;
            }

            if (awarenessState < AllyAlertMinimumAwareness)
            {
                return;
            }

            var crossedThreshold = previousAwareness < AllyAlertMinimumAwareness;
            var strongDisturbance = sourceKind == EnemyStimulusKind.Damage || sourceTier >= EnemyStimulusTier.Loud;
            if (!crossedThreshold && !strongDisturbance)
            {
                return;
            }

            var alertTier = sourceKind == EnemyStimulusKind.Damage || sourceTier == EnemyStimulusTier.Violent
                ? EnemyStimulusTier.Loud
                : EnemyStimulusTier.Normal;
            TryShareAllyAlert(timeSeconds, alertTier, reason);
        }

        private bool TryShareAllyAlert(float timeSeconds, EnemyStimulusTier tier, string reason)
        {
            if (!AllyAlertSharingEnabled ||
                roomCombatController == null ||
                AllyAlertRadiusMeters <= 0f ||
                awarenessState < AllyAlertMinimumAwareness ||
                timeSeconds < nextAllowedAllyAlertTime)
            {
                return false;
            }

            nextAllowedAllyAlertTime = timeSeconds + AllyAlertCooldownSeconds;
            lastAllyAlertSharedTime = timeSeconds;
            lastAllyAlertRecipientCount = roomCombatController.EmitEnemyAllyAlert(
                this,
                transform.localPosition,
                AllyAlertRadiusMeters,
                timeSeconds,
                tier,
                $"{Definition?.SpawnKind ?? BehaviorId.ToString()}:{reason}");
            return lastAllyAlertRecipientCount > 0;
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

        private float EffectiveHearingRadius(EnemyStimulusKind kind, EnemyStimulusTier tier)
        {
            return HearingRadiusMeters * HearingSensitivityMultiplier * HearingMultiplierFor(kind) * tier.HearingMultiplier();
        }

        private static float HearingMultiplierFor(EnemyStimulusKind kind)
        {
            return kind switch
            {
                EnemyStimulusKind.Footstep => 0.75f,
                EnemyStimulusKind.MeleeAttack => 1.05f,
                EnemyStimulusKind.RangedAttack => 1.25f,
                EnemyStimulusKind.Roll => 0.95f,
                EnemyStimulusKind.GuardImpact => 1.15f,
                EnemyStimulusKind.CreatureSignal => 1.2f,
                EnemyStimulusKind.AllyAlert => 1.15f,
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
            var approachRange = behaviorId is EnemyBehaviorId.TurretShooter or EnemyBehaviorId.RepeaterTurret or EnemyBehaviorId.GraveLantern
                ? Mathf.Min(Definition.AttackRangeMeters, 6.25f)
                : behaviorId == EnemyBehaviorId.ClockworkSentry
                    ? Mathf.Min(Definition.AttackRangeMeters, 5.75f)
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

            MoveInDirection(homeDelta.normalized, deltaTime, 0.55f, intent: EnemyNavigationIntent.ReturnHome);
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

            var spacing = ResolveCurrentActionSpacing(distanceToPlayer);
            if (distanceToPlayer <= spacing.DesiredStartDistanceMeters && timeSeconds >= nextRetreatBurstAllowedTime)
            {
                var profile = SpacingProfile;
                retreatBurstEndTime = timeSeconds + (profile != null ? profile.RetreatBurstSeconds : RetreatBurstSeconds);
                nextRetreatBurstAllowedTime = retreatBurstEndTime + (profile != null ? profile.RetreatReassessSeconds : RetreatReassessSeconds);
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
            return behaviorId == EnemyBehaviorId.Rat ||
                   behaviorId == EnemyBehaviorId.Spider ||
                   behaviorId == EnemyBehaviorId.HollowBird ||
                   behaviorId == EnemyBehaviorId.HollowBeast;
        }

        private Vector3 ResolvePreferredRangeDirection(Vector3 toPlayerDirection, float distanceToPlayer)
        {
            var spacing = ResolveCurrentActionSpacing(distanceToPlayer);
            if (spacing.IsTooClose(distanceToPlayer))
            {
                return -toPlayerDirection;
            }

            if (spacing.IsTooFar(distanceToPlayer))
            {
                return toPlayerDirection;
            }

            var closePressureFloor = Mathf.Lerp(spacing.CommitRangeMinMeters, spacing.DesiredStartDistanceMeters, Mathf.Clamp01(spacing.ClosePressureBias));
            if (spacing.ClosePressureBias > 0f && distanceToPlayer > closePressureFloor)
            {
                return toPlayerDirection;
            }

            return Vector3.zero;
        }

        private EnemyResolvedActionSpacing ResolveCurrentActionSpacing(float distanceToPlayer)
        {
            var action = ResolvePrimarySpacingAction(distanceToPlayer);
            if (action != null)
            {
                return ResolveSpacingForActionProfile(action);
            }

            return SpacingProfile.ResolveFallbackSpacing(PreferredRangeMinMeters, PreferredRangeMaxMeters);
        }

        private EnemyResolvedActionSpacing ResolveSpacingForActionId(string actionId)
        {
            if (!string.IsNullOrWhiteSpace(actionId))
            {
                var action = FindActionProfile(actionId);
                if (action != null)
                {
                    return ResolveSpacingForActionProfile(action);
                }

                var attack = FindAttackProfile(actionId);
                if (attack != null)
                {
                    return ResolveSpacingForAttackProfile(attack);
                }
            }

            return ResolveCurrentActionSpacing(DistanceToPlayer());
        }

        private EnemyResolvedActionSpacing ResolveSpacingForActionProfile(EnemyActionProfileDefinition actionProfile)
        {
            var attackProfile = ResolveAttackProfileForAction(actionProfile);
            return SpacingProfile.ResolveActionSpacing(actionProfile, attackProfile);
        }

        private EnemyResolvedActionSpacing ResolveSpacingForAttackProfile(EnemyAttackProfileDefinition attackProfile)
        {
            if (attackProfile == null)
            {
                return SpacingProfile.ResolveFallbackSpacing(PreferredRangeMinMeters, PreferredRangeMaxMeters);
            }

            var actionProfile = FindActionProfile(attackProfile.AttackId);
            return SpacingProfile.ResolveActionSpacing(actionProfile, attackProfile);
        }

        private EnemyActionProfileDefinition ResolvePrimarySpacingAction(float distanceToPlayer)
        {
            if (Definition == null)
            {
                return null;
            }

            if ((awarenessState == EnemyAwarenessState.Engaged || IsEndangeredNow) &&
                TryResolvePrimarySpacingAction(distanceToPlayer, requireDamageDriver: true, out var combatAction))
            {
                return combatAction;
            }

            return TryResolvePrimarySpacingAction(distanceToPlayer, requireDamageDriver: false, out var fallbackAction)
                ? fallbackAction
                : null;
        }

        private bool TryResolvePrimarySpacingAction(float distanceToPlayer, bool requireDamageDriver, out EnemyActionProfileDefinition best)
        {
            best = null;
            var bestScore = float.NegativeInfinity;
            var actions = Definition.ActionProfiles;
            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index];
                if (!IsSpacingCandidateAction(action) ||
                    (requireDamageDriver && !IsCombatSpacingDriverAction(action)))
                {
                    continue;
                }

                var spacing = ResolveSpacingForActionProfile(action);
                var distanceFit = -Mathf.Abs(distanceToPlayer - spacing.DesiredStartDistanceMeters);
                var envelopeBonus = spacing.IsInsideEnvelope(distanceToPlayer) ? 0.35f : 0f;
                var pressureBonus = action.Intent is EnemyActionIntent.Damage or EnemyActionIntent.Pressure or EnemyActionIntent.Interrupt ? 0.25f : 0f;
                var score = distanceFit + envelopeBonus + pressureBonus + action.BaseWeight * 0.08f;
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                best = action;
            }

            return best != null;
        }

        private bool IsSpacingCandidateAction(EnemyActionProfileDefinition action)
        {
            if (action == null || action.UsageState != EnemyActionUsageState.CurrentRuntime)
            {
                return false;
            }

            if ((int)Intelligence < (int)action.MinimumIntelligence || AwarenessState < action.MinimumAwareness)
            {
                return false;
            }

            if (action.ExplicitlyNonDamaging && action.Intent == EnemyActionIntent.Feint)
            {
                return false;
            }

            var dispositions = action.AllowedDispositions;
            for (var index = 0; index < dispositions.Count; index++)
            {
                if (dispositions[index] == Disposition)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCombatSpacingDriverAction(EnemyActionProfileDefinition action)
        {
            return action != null &&
                   !action.ExplicitlyNonDamaging &&
                   action.Intent is EnemyActionIntent.Damage or EnemyActionIntent.Pressure or EnemyActionIntent.Interrupt or EnemyActionIntent.HazardSetup;
        }

        private bool CanCommitActionAtDistance(string actionId, EnemyAttackProfileDefinition profile, float distanceToPlayer, float fallbackMaxRange)
        {
            var spacing = profile != null ? ResolveSpacingForAttackProfile(profile) : ResolveSpacingForActionId(actionId);
            var maxRange = Mathf.Max(0.1f, fallbackMaxRange, spacing.CommitRangeMaxMeters + spacing.LongToleranceMeters);
            return distanceToPlayer <= maxRange;
        }

        public EnemyAttackReachabilityResult EvaluateAttackReachabilityForAi(
            EnemyAttackProfileDefinition profile,
            EnemyAttackRuntimeKind fallbackRuntimeKind,
            bool canReposition)
        {
            return EvaluateAttackReachability(profile, fallbackRuntimeKind, canReposition);
        }

        private bool TryEvaluateCommandReachability(
            EnemyBehaviorCommand command,
            float timeSeconds,
            bool canReposition,
            out EnemyAttackReachabilityResult reachability)
        {
            reachability = EnemyAttackReachabilityResult.Clear(EnemyAttackObstructionPolicy.RuntimeDefault);
            var profile = ResolveAttackProfileForCommand(command.Kind, command.ActionId, timeSeconds, out var fallbackRuntimeKind);
            if (profile == null &&
                command.Kind is not (EnemyBehaviorCommandKind.StartMeleeAction or EnemyBehaviorCommandKind.StartRangedAction or EnemyBehaviorCommandKind.StartChargeAction))
            {
                return false;
            }

            reachability = EvaluateAttackReachability(profile, fallbackRuntimeKind, canReposition);
            return true;
        }

        private string ResolveCommittedActionId(EnemyBehaviorCommand command, float timeSeconds)
        {
            if (!string.IsNullOrWhiteSpace(command.ActionId))
            {
                return command.ActionId;
            }

            var profile = ResolveAttackProfileForCommand(command.Kind, command.ActionId, timeSeconds, out _);
            return profile != null ? profile.AttackId : string.Empty;
        }

        private bool IsAttackLineRepositionNeeded(string actionId, EnemyAttackRuntimeKind fallbackRuntimeKind)
        {
            if (Definition == null || playerController == null || string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            var profile = Definition.ResolveAttackProfile(actionId);
            if (profile == null)
            {
                return false;
            }

            var result = EvaluateAttackReachability(profile, fallbackRuntimeKind, canReposition: true);
            lastAttackReachability = result;
            if (result.Status != EnemyAttackReachabilityStatus.NeedsReposition)
            {
                return false;
            }

            lastDesignerDebugBlockedReason = $"reposition_for_line:{actionId}:{result.Reason}";
            return true;
        }

        private bool CanReachPlayerWithAttack(string actionId, EnemyAttackProfileDefinition profile, EnemyAttackRuntimeKind fallbackRuntimeKind)
        {
            var result = EvaluateAttackReachability(profile, fallbackRuntimeKind, canReposition: true);
            lastAttackReachability = result;
            if (result.CanCommit)
            {
                return true;
            }

            var id = !string.IsNullOrWhiteSpace(actionId)
                ? actionId
                : profile != null
                    ? profile.AttackId
                    : fallbackRuntimeKind.ToString();
            lastDesignerDebugBlockedReason = $"{result.Status.ToString().ToLowerInvariant()}_{id}:{result.Reason}";
            return false;
        }

        private EnemyAttackReachabilityResult EvaluateAttackReachability(
            EnemyAttackProfileDefinition profile,
            EnemyAttackRuntimeKind fallbackRuntimeKind,
            bool canReposition)
        {
            if (playerController == null)
            {
                return EnemyAttackReachabilityResult.Clear(EnemyAttackObstructionPolicy.RuntimeDefault, "missing_player");
            }

            return EnemyAttackReachabilityService.Evaluate(
                roomRuntimeRoot,
                transform.localPosition,
                playerController.transform.localPosition,
                RadiusMeters,
                PlaceholderPlayerController.DefaultRadiusMeters,
                profile,
                fallbackRuntimeKind,
                canReposition);
        }

        private EnemyAttackProfileDefinition ResolveAttackProfileForCommand(
            EnemyBehaviorCommandKind commandKind,
            string actionId,
            float timeSeconds,
            out EnemyAttackRuntimeKind fallbackRuntimeKind)
        {
            fallbackRuntimeKind = EnemyAttackRuntimeKind.Contact;
            if (Definition == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(actionId))
            {
                var explicitProfile = Definition.ResolveAttackProfile(actionId);
                if (explicitProfile != null)
                {
                    fallbackRuntimeKind = explicitProfile.RuntimeKind;
                    return explicitProfile;
                }
            }

            switch (commandKind)
            {
                case EnemyBehaviorCommandKind.StartMeleeAction:
                    fallbackRuntimeKind = EnemyAttackRuntimeKind.MeleeLunge;
                    return ResolveLungeAttackProfile(timeSeconds);
                case EnemyBehaviorCommandKind.StartRangedAction:
                    fallbackRuntimeKind = EnemyAttackRuntimeKind.Projectile;
                    return ResolveRangedAttackProfile(timeSeconds);
                case EnemyBehaviorCommandKind.StartChargeAction:
                    fallbackRuntimeKind = EnemyAttackRuntimeKind.Charge;
                    return ResolveChargeAttackProfile();
                case EnemyBehaviorCommandKind.StartAreaAction:
                    fallbackRuntimeKind = EnemyAttackRuntimeKind.Area;
                    return null;
                default:
                    return null;
            }
        }

        private EnemyActionProfileDefinition FindActionProfile(string actionId)
        {
            if (Definition == null || string.IsNullOrWhiteSpace(actionId))
            {
                return null;
            }

            var actions = Definition.ActionProfiles;
            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index];
                if (action != null && string.Equals(action.ActionId, actionId, StringComparison.Ordinal))
                {
                    return action;
                }
            }

            return null;
        }

        private EnemyAttackProfileDefinition FindAttackProfile(string attackId)
        {
            if (Definition == null || string.IsNullOrWhiteSpace(attackId))
            {
                return null;
            }

            var attacks = Definition.AttackProfiles;
            for (var index = 0; index < attacks.Count; index++)
            {
                var attack = attacks[index];
                if (attack != null && string.Equals(attack.AttackId, attackId, StringComparison.Ordinal))
                {
                    return attack;
                }
            }

            return null;
        }

        private EnemyAttackProfileDefinition ResolveAttackProfileForAction(EnemyActionProfileDefinition actionProfile)
        {
            if (actionProfile == null)
            {
                return null;
            }

            if (actionProfile.LinkedAttackProfile != null)
            {
                return actionProfile.LinkedAttackProfile;
            }

            var linkedAttackId = !string.IsNullOrWhiteSpace(actionProfile.LinkedAttackId)
                ? actionProfile.LinkedAttackId
                : actionProfile.ActionId;
            return FindAttackProfile(linkedAttackId);
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

            if (IsInCurrentSpacingEnvelope(distanceToPlayer))
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

        private void BeginRecoveryMovement(EnemyAttackProfileDefinition profile)
        {
            if (bossRuntime != null || bossDefinition != null || archetypeId == EnemyArchetypeId.Boss)
            {
                recoveryMovementRemainingMeters = 0f;
                recoveryMovementActionId = string.Empty;
                recoveryMovementMode = EnemySpacingRecoveryMode.Planted;
                return;
            }

            var spacing = ResolveSpacingForAttackProfile(profile);
            recoveryMovementRemainingMeters = spacing.RecoveryMovementDistanceMeters;
            recoveryMovementActionId = spacing.ActionId;
            recoveryMovementMode = spacing.RecoveryMovementMode;
        }

        private void TickRecoveryMovement(float deltaTime, EnemyAttackProfileDefinition profile)
        {
            if (bossRuntime != null ||
                playerController == null ||
                recoveryMovementRemainingMeters <= 0.001f ||
                recoveryMovementMode == EnemySpacingRecoveryMode.Planted)
            {
                return;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var spacing = ResolveSpacingForAttackProfile(profile);
            var speedMultiplier = Mathf.Max(0f, spacing.RecoverySpeedMultiplier);
            if (speedMultiplier <= 0f || speedMetersPerSecond <= 0f)
            {
                return;
            }

            var maxStep = speedMetersPerSecond * speedMultiplier * Mathf.Max(0f, deltaTime);
            if (maxStep <= 0.001f)
            {
                return;
            }

            var cappedMultiplier = speedMultiplier;
            if (maxStep > recoveryMovementRemainingMeters)
            {
                cappedMultiplier *= recoveryMovementRemainingMeters / maxStep;
            }

            var before = transform.localPosition;
            var intent = recoveryMovementMode switch
            {
                EnemySpacingRecoveryMode.PhaseDrift => EnemyNavigationIntent.PhaseMove,
                EnemySpacingRecoveryMode.RangedReset or EnemySpacingRecoveryMode.ShortBackstep or EnemySpacingRecoveryMode.Recoil => EnemyNavigationIntent.Flee,
                _ => EnemyNavigationIntent.PreferredRange
            };
            MoveInDirection(-delta.normalized, deltaTime, cappedMultiplier, allowSteering: recoveryMovementMode != EnemySpacingRecoveryMode.PhaseDrift, intent);
            var moved = transform.localPosition - before;
            moved.y = 0f;
            recoveryMovementRemainingMeters = Mathf.Max(0f, recoveryMovementRemainingMeters - moved.magnitude);
        }

        private void ClearRecoveryMovement()
        {
            recoveryMovementRemainingMeters = 0f;
            recoveryMovementActionId = string.Empty;
            recoveryMovementMode = EnemySpacingRecoveryMode.Planted;
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
            ApplyNavigationMove(ResolveNavigationMove(desired, EnemyNavigationIntent.ActiveCharge, allowLocalDetour: false), "active_charge");
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
            var moveDistance = activeMeleeProfile != null && activeMeleeProfile.HasAuthoredActiveMovementDistance
                ? activeMeleeProfile.ActiveMovementDistanceMeters
                : LungeDistanceMeters;
            var speed = moveDistance / activeSeconds;
            var desired = transform.localPosition + direction.normalized * speed * Mathf.Max(0f, deltaTime);
            ApplyNavigationMove(ResolveNavigationMove(desired, EnemyNavigationIntent.ActiveLunge, allowLocalDetour: false), "active_lunge");
            if (!lungeContactAttempted && TryApplyContactDamage(timeSeconds))
            {
                lungeContactAttempted = true;
            }
        }

        private void MoveActiveCreatureBurst(float deltaTime)
        {
            var direction = activeCreatureMoveDirection.sqrMagnitude > 0.001f ? activeCreatureMoveDirection.normalized : TelegraphDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            facingDirection = direction.normalized;
            var activeSeconds = ResolvedActiveSeconds(activeCreatureMoveProfile, 0.22f);
            var moveDistance = activeCreatureMoveProfile != null && activeCreatureMoveProfile.HasAuthoredActiveMovementDistance
                ? activeCreatureMoveProfile.ActiveMovementDistanceMeters
                : speedMetersPerSecond * activeSeconds * 0.75f;
            var speed = moveDistance / Mathf.Max(0.01f, activeSeconds);
            var desired = transform.localPosition + direction.normalized * speed * Mathf.Max(0f, deltaTime);
            var mode = activeCreatureMoveProfile != null && activeCreatureMoveProfile.RuntimeKind == EnemyAttackRuntimeKind.PhaseMove
                ? EnemyNavigationMode.IgnoreObstaclesPhase
                : EnemyNavigationAdapter.DefaultModeFor(movementMode);
            var intent = mode == EnemyNavigationMode.IgnoreObstaclesPhase
                ? EnemyNavigationIntent.PhaseMove
                : EnemyNavigationIntent.CreatureBurst;
            ApplyNavigationMove(
                ResolveNavigationMove(desired, intent, mode, allowLocalDetour: mode != EnemyNavigationMode.IgnoreObstaclesPhase),
                "active_creature_burst");
        }

        private Vector3 ResolveCreatureMoveDirection(string actionId, float timeSeconds)
        {
            var delta = playerController != null
                ? playerController.transform.localPosition - transform.localPosition
                : FacingDirection;
            delta.y = 0f;
            var toPlayer = delta.sqrMagnitude > 0.01f ? delta.normalized : FacingDirection;
            var id = actionId ?? string.Empty;
            if (id.IndexOf("retreat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("flee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("wing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("veil", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return -toPlayer;
            }

            if (id.IndexOf("phase", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("blink", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("strafe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("skitter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("side", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("circle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var lateral = Vector3.Cross(Vector3.up, toPlayer).normalized;
                var seed = Mathf.Abs((spawnIndex + 5) * 41 + Mathf.FloorToInt(timeSeconds * 2.2f) * 29 + StableActionHash(id));
                return seed % 2 == 0 ? lateral : -lateral;
            }

            if (id.IndexOf("hop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return (-toPlayer + ResolveCritterWanderDirection(timeSeconds) * 0.35f).normalized;
            }

            return toPlayer;
        }

        private static int StableActionHash(string value)
        {
            var hash = 17;
            for (var index = 0; index < value.Length; index++)
            {
                hash = hash * 31 + value[index];
            }

            return hash;
        }

        private void EmitSameFamilyCreatureSignal(float timeSeconds)
        {
            if (roomCombatController == null || activeCreatureSignalProfile == null)
            {
                return;
            }

            if (!IsFamilySignalAction(activeCreatureSignalProfile.AttackId))
            {
                return;
            }

            var family = CreatureFamilyId(behaviorId);
            if (string.IsNullOrEmpty(family))
            {
                return;
            }

            var range = Mathf.Max(0.1f, activeCreatureSignalProfile.RangeMeters);
            var sourcePosition = transform.localPosition;
            foreach (var enemy in roomCombatController.Enemies)
            {
                if (enemy == null ||
                    enemy == this ||
                    !enemy.IsAlive ||
                    enemy.BossDefinition != null ||
                    !string.Equals(CreatureFamilyId(enemy.BehaviorId), family, StringComparison.Ordinal))
                {
                    continue;
                }

                var distance = Vector3.Distance(Flat(sourcePosition), Flat(enemy.transform.localPosition));
                if (distance > range)
                {
                    continue;
                }

                enemy.ReceiveStimulus(
                    EnemyStimulusKind.CreatureSignal,
                    sourcePosition,
                    timeSeconds,
                    EnemyStimulusTier.Normal,
                    $"{family}:{activeCreatureSignalProfile.AttackId}");
            }
        }

        private static bool IsFamilySignalAction(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            return actionId.IndexOf("signal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   actionId.IndexOf("squeal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   actionId.IndexOf("howl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   actionId.IndexOf("caw", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string CreatureFamilyId(EnemyBehaviorId id)
        {
            return id switch
            {
                EnemyBehaviorId.Rat => "rat",
                EnemyBehaviorId.Spider => "spider",
                EnemyBehaviorId.HollowBird => "hollow_bird",
                EnemyBehaviorId.HollowBeast => "hollow_beast",
                _ => string.Empty
            };
        }

        private void MoveInDirection(
            Vector3 direction,
            float deltaTime,
            float speedMultiplier,
            bool allowSteering = true,
            EnemyNavigationIntent intent = EnemyNavigationIntent.None,
            Vector3? finalGoalLocalPosition = null)
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
            var requestedSpeed = speedMetersPerSecond * Mathf.Max(0f, speedMultiplier);
            var desired = transform.localPosition + movementDirection.normalized * requestedSpeed * deltaTime;
            ApplyNavigationMove(
                ResolveNavigationMove(
                    desired,
                    intent,
                    allowLocalDetour: allowSteering,
                    finalGoalLocalPosition: finalGoalLocalPosition,
                    desiredSpeedMetersPerSecond: requestedSpeed),
                $"movement:{intent}");
        }

        private Vector3 ResolveNavigationMove(
            Vector3 desiredLocalPosition,
            EnemyNavigationIntent intent,
            EnemyNavigationMode? overrideMode = null,
            bool allowLocalDetour = true,
            Vector3? finalGoalLocalPosition = null,
            float desiredSpeedMetersPerSecond = 0f)
        {
            var mode = overrideMode ?? EnemyNavigationAdapter.DefaultModeFor(movementMode);
            var current = transform.localPosition;
            var movementDelta = desiredLocalPosition - current;
            movementDelta.y = 0f;
            var actionEnvelopeAnchor = Vector3.zero;
            var actionEnvelopeDesired = 0f;
            var actionEnvelopeMin = 0f;
            var actionEnvelopeMax = 0f;
            var actionEnvelopeFallbackGoal = Vector3.zero;
            var hasActionEnvelope = finalGoalLocalPosition == null &&
                TryResolveActionEnvelopeForIntent(
                    intent,
                    current,
                    out actionEnvelopeAnchor,
                    out actionEnvelopeDesired,
                    out actionEnvelopeMin,
                    out actionEnvelopeMax,
                    out actionEnvelopeFallbackGoal);
            var finalGoal = finalGoalLocalPosition ??
                (hasActionEnvelope
                    ? actionEnvelopeFallbackGoal
                    : ResolveNavigationGoalForIntent(
                    intent,
                    movementDelta.sqrMagnitude > 0.0001f ? movementDelta.normalized : Vector3.zero,
                    desiredLocalPosition));
            var maxStep = movementDelta.magnitude;
            var allowPathfinding = CanUsePathfindingForRuntimeMove(mode, intent, allowLocalDetour);
            if (!allowPathfinding)
            {
                StopNavMeshAgent(clearPath: intent != EnemyNavigationIntent.BumpSeparation);
            }

            var pathAgeSeconds = ResolvePathAgeForRequest(finalGoal, intent, allowPathfinding);
            var request = new EnemyNavigationRequest(
                roomRuntimeRoot,
                current,
                desiredLocalPosition,
                radiusMeters,
                mode,
                intent,
                Intelligence,
                allowLocalDetour,
                allowPathfinding,
                finalGoal,
                lastTickTime,
                spawnIndex,
                maxStep,
                pathAgeSeconds,
                hasActionEnvelope,
                actionEnvelopeAnchor,
                actionEnvelopeDesired,
                actionEnvelopeMin,
                actionEnvelopeMax,
                navMeshAgentBridge,
                desiredSpeedMetersPerSecond,
                aiBrain.LodTier,
                lastTacticalIntent.Role);
            lastNavigationResult = locomotionAgent.Resolve(request, lastTacticalIntent);
            lastNavigationMoveRequiresAgentSync = !allowPathfinding || lastNavigationResult.Backend != EnemyNavigationBackend.UnityNavMesh;
            UpdatePathCacheAfterResult(lastNavigationResult, finalGoal, intent, allowPathfinding);
            return lastNavigationResult.ResolvedLocalPosition;
        }

        private bool TryResolveActionEnvelopeForIntent(
            EnemyNavigationIntent intent,
            Vector3 current,
            out Vector3 anchor,
            out float desiredDistance,
            out float minDistance,
            out float maxDistance,
            out Vector3 fallbackGoal)
        {
            anchor = Vector3.zero;
            desiredDistance = 0f;
            minDistance = 0f;
            maxDistance = 0f;
            fallbackGoal = Vector3.zero;
            if (intent is not (EnemyNavigationIntent.MoveToPlayer or EnemyNavigationIntent.PreferredRange) ||
                playerController == null)
            {
                return false;
            }

            anchor = playerController.transform.localPosition;
            var delta = anchor - current;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return false;
            }

            var spacing = ResolveCurrentActionSpacing(delta.magnitude);
            desiredDistance = Mathf.Max(
                radiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.12f,
                spacing.DesiredStartDistanceMeters);
            fallbackGoal = anchor - delta.normalized * desiredDistance;
            minDistance = Mathf.Max(
                radiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.12f,
                spacing.CommitRangeMinMeters - spacing.CloseToleranceMeters);
            maxDistance = Mathf.Max(
                minDistance + 0.1f,
                spacing.CommitRangeMaxMeters + spacing.LongToleranceMeters);
            return true;
        }

        private Vector3 ResolveNavigationGoalForIntent(EnemyNavigationIntent intent, Vector3 movementDirection, Vector3 desiredLocalPosition)
        {
            var current = transform.localPosition;
            movementDirection.y = 0f;
            switch (intent)
            {
                case EnemyNavigationIntent.MoveToPlayer:
                case EnemyNavigationIntent.PreferredRange:
                    if (playerController == null)
                    {
                        return desiredLocalPosition;
                    }

                    var delta = playerController.transform.localPosition - current;
                    delta.y = 0f;
                    if (delta.sqrMagnitude <= 0.01f)
                    {
                        return desiredLocalPosition;
                    }

                    var spacing = ResolveCurrentActionSpacing(delta.magnitude);
                    var desiredStart = Mathf.Max(
                        radiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.12f,
                        spacing.DesiredStartDistanceMeters);
                    return playerController.transform.localPosition - delta.normalized * desiredStart;
                case EnemyNavigationIntent.Flee:
                    return movementDirection.sqrMagnitude > 0.01f
                        ? current + movementDirection.normalized * Mathf.Max(2.2f, speedMetersPerSecond * 1.35f)
                        : desiredLocalPosition;
                case EnemyNavigationIntent.Wander:
                    return movementDirection.sqrMagnitude > 0.01f
                        ? current + movementDirection.normalized * Mathf.Max(1.4f, speedMetersPerSecond * 0.9f)
                        : desiredLocalPosition;
                case EnemyNavigationIntent.Investigate:
                    return lastStimulusTime > float.NegativeInfinity ? lastStimulusLocalPosition : desiredLocalPosition;
                case EnemyNavigationIntent.ReturnHome:
                    return homeLocalPosition;
                default:
                    return desiredLocalPosition;
            }
        }

        private bool CanUsePathfindingForRuntimeMove(EnemyNavigationMode mode, EnemyNavigationIntent intent, bool allowLocalDetour)
        {
            if (!allowLocalDetour ||
                mode != EnemyNavigationMode.GroundedLocal ||
                roomRuntimeRoot == null ||
                movementMode != EnemyMovementMode.Grounded ||
                speedMetersPerSecond <= 0f ||
                bossRuntime != null ||
                bossDefinition != null ||
                archetypeId == EnemyArchetypeId.Boss ||
                IsInspectionFrozen ||
                readabilityState != EnemyReadabilityState.Idle)
            {
                return false;
            }

            return intent is EnemyNavigationIntent.MoveToPlayer
                or EnemyNavigationIntent.PreferredRange
                or EnemyNavigationIntent.Flee
                or EnemyNavigationIntent.Wander
                or EnemyNavigationIntent.Investigate
                or EnemyNavigationIntent.ReturnHome;
        }

        private bool TryUseCachedPathStep(
            Vector3 requestedLocalPosition,
            Vector3 finalGoalLocalPosition,
            EnemyNavigationIntent intent,
            EnemyNavigationMode mode,
            float maxStepDistanceMeters,
            out Vector3 resolvedLocalPosition)
        {
            resolvedLocalPosition = transform.localPosition;
            if (cachedPathStatus is not (EnemyPathStatus.Ready or EnemyPathStatus.Partial) ||
                cachedPathWaypointCount <= 0 ||
                cachedPathIntent != intent ||
                lastTickTime >= nextPathRefreshTime ||
                FlatDistance(cachedPathGoalLocalPosition, finalGoalLocalPosition) > PathGoalRefreshToleranceFor(Intelligence))
            {
                return false;
            }

            var current = transform.localPosition;
            var toWaypoint = cachedPathNextWaypointLocalPosition - current;
            toWaypoint.y = 0f;
            if (toWaypoint.sqrMagnitude <= 0.04f)
            {
                return false;
            }

            var maxStep = maxStepDistanceMeters > 0f ? maxStepDistanceMeters : FlatDistance(current, requestedLocalPosition);
            if (maxStep <= 0.001f)
            {
                return false;
            }

            var desired = current + toWaypoint.normalized * Mathf.Min(maxStep, toWaypoint.magnitude);
            var resolved = RoomLocalCollision.ResolveMove(roomRuntimeRoot, current, desired, radiusMeters);
            var moved = resolved - current;
            moved.y = 0f;
            if (moved.sqrMagnitude <= 0.0001f)
            {
                ResetPathCache();
                return false;
            }

            var steering = moved.normalized;
            var reached = FlatDistance(resolved, requestedLocalPosition) <= EnemyNavigationAdapter.DefaultReachedToleranceMeters;
            lastNavigationResult = new EnemyNavigationResult(
                EnemyNavigationBackend.UnityNavMesh,
                mode,
                intent,
                requestedLocalPosition,
                resolved,
                steering,
                reached,
                usedFallbackSteering: false,
                blocked: false,
                cachedPathStatus,
                finalGoalLocalPosition,
                cachedPathNextWaypointLocalPosition,
                Mathf.Max(0f, lastTickTime - cachedPathCreatedTime),
                cachedPathWaypointCount,
                cachedPathFallbackReason,
                cachedPathWaypointsLocalPositions);
            resolvedLocalPosition = resolved;
            return true;
        }

        private float ResolvePathAgeForRequest(Vector3 finalGoalLocalPosition, EnemyNavigationIntent intent, bool allowPathfinding)
        {
            if (!allowPathfinding ||
                cachedPathIntent != intent ||
                cachedPathStatus is not (EnemyPathStatus.Ready or EnemyPathStatus.Partial) ||
                FlatDistance(cachedPathGoalLocalPosition, finalGoalLocalPosition) > PathGoalRefreshToleranceFor(Intelligence) ||
                cachedPathCreatedTime <= float.NegativeInfinity)
            {
                cachedPathCreatedTime = Mathf.Max(0f, lastTickTime);
                return 0f;
            }

            return Mathf.Max(0f, lastTickTime - cachedPathCreatedTime);
        }

        private void UpdatePathCacheAfterResult(EnemyNavigationResult result, Vector3 finalGoalLocalPosition, EnemyNavigationIntent intent, bool allowPathfinding)
        {
            if (!allowPathfinding ||
                result.Backend != EnemyNavigationBackend.UnityNavMesh ||
                result.PathStatus is not (EnemyPathStatus.Ready or EnemyPathStatus.Partial) ||
                result.PathWaypointCount <= 0)
            {
                if (result.Blocked || result.PathStatus is EnemyPathStatus.Unreachable or EnemyPathStatus.InvalidRequest)
                {
                    ResetPathCache();
                }

                return;
            }

            cachedPathGoalLocalPosition = finalGoalLocalPosition;
            cachedPathNextWaypointLocalPosition = result.NextWaypointLocalPosition;
            cachedPathIntent = intent;
            cachedPathStatus = result.PathStatus;
            cachedPathCreatedTime = Mathf.Max(0f, lastTickTime);
            cachedPathWaypointCount = result.PathWaypointCount;
            cachedPathFallbackReason = result.FallbackReason;
            cachedPathWaypointsLocalPositions = result.PathWaypointsLocalPositions;
            nextPathRefreshTime = cachedPathCreatedTime + PathRefreshIntervalFor(Intelligence) + PathRefreshJitterSeconds();
        }

        private void ResetPathCache()
        {
            cachedPathGoalLocalPosition = Vector3.zero;
            cachedPathNextWaypointLocalPosition = Vector3.zero;
            cachedPathIntent = EnemyNavigationIntent.None;
            cachedPathStatus = EnemyPathStatus.NotRequested;
            cachedPathCreatedTime = float.NegativeInfinity;
            nextPathRefreshTime = 0f;
            cachedPathWaypointCount = 0;
            cachedPathFallbackReason = string.Empty;
            cachedPathWaypointsLocalPositions = Array.Empty<Vector3>();
        }

        private float PathRefreshIntervalFor(EnemyIntelligenceLevel level)
        {
            return level switch
            {
                EnemyIntelligenceLevel.Cunning => 0.25f,
                EnemyIntelligenceLevel.Tactical => 0.32f,
                EnemyIntelligenceLevel.Trained => 0.48f,
                EnemyIntelligenceLevel.Basic => 0.62f,
                EnemyIntelligenceLevel.Simple => 0.82f,
                _ => 0.95f
            };
        }

        private float PathRefreshJitterSeconds()
        {
            var seed = Mathf.Abs(spawnIndex >= 0 ? spawnIndex : GetInstanceID());
            return (seed % 11) * 0.035f;
        }

        private static float PathGoalRefreshToleranceFor(EnemyIntelligenceLevel level)
        {
            return level switch
            {
                EnemyIntelligenceLevel.Cunning => 0.34f,
                EnemyIntelligenceLevel.Tactical => 0.38f,
                EnemyIntelligenceLevel.Trained => 0.46f,
                EnemyIntelligenceLevel.Basic => 0.55f,
                EnemyIntelligenceLevel.Simple => 0.68f,
                _ => 0.8f
            };
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

        private bool TryApplyAreaDamage(float timeSeconds)
        {
            if (areaDamageAttempted ||
                readabilityState != EnemyReadabilityState.AreaActive ||
                activeAreaProfile == null ||
                playerHealth == null ||
                playerController == null ||
                !playerHealth.IsAlive ||
                IsInEntryGrace(timeSeconds))
            {
                return false;
            }

            var distance = Vector3.Distance(Flat(transform.localPosition), Flat(playerController.transform.localPosition));
            if (distance > activeAreaProfile.RangeMeters + PlaceholderPlayerController.DefaultRadiusMeters)
            {
                return false;
            }

            areaDamageAttempted = true;
            var direction = playerController.transform.localPosition - transform.localPosition;
            var feelProfile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
            var damaged = DamageSystem.ApplyDamage(
                playerHealth,
                activeAreaProfile.CreateDamageRequest(gameObject, direction, feelProfile.KnockbackSeconds));
            if (damaged)
            {
                VfxPresenter.Play(VfxCueId.PlayerHit, playerController.transform.position, playerController.transform.parent);
                AudioPresenter.Play(AudioCueId.PlayerHit, playerController.transform.position);
            }

            return damaged;
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

            if (IsPassiveHazardBody() && timeSeconds < nextAllowedContactTime)
            {
                return false;
            }

            if (IsPassiveHazardBody())
            {
                nextAllowedContactTime = timeSeconds + contactCooldownSeconds;
            }

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
            DisableNavMeshAgent();
            DisableUnityBehaviorGraphBridge();
            ClearRecoveryMovement();
            ResetSpacingResetChain();
            SpawnSplitChildren();
            CorpseGhostPresenter.SpawnFrom(this, combatFeelProfile);
            VfxPresenter.Play(VfxCueId.EnemyDeath, transform.position, transform.parent);
            AudioPresenter.Play(AudioCueId.EnemyDeath, transform.position);
            gameObject.SetActive(false);
        }

        private void OnDamaged(CombatantHealth _)
        {
            lastDamagedTime = lastTickTime > 0f ? lastTickTime : Time.time;
            if (bossRuntime == null && bossDefinition == null && archetypeId != EnemyArchetypeId.Boss)
            {
                var stimulusPosition = playerController != null ? playerController.transform.localPosition : transform.localPosition;
                RecordStimulus(
                    EnemyStimulusKind.Damage,
                    EnemyStimulusTier.Violent,
                    stimulusPosition,
                    lastDamagedTime,
                    Vector3.Distance(Flat(transform.localPosition), Flat(stimulusPosition)),
                    EffectiveHearingRadius(EnemyStimulusKind.Damage, EnemyStimulusTier.Violent),
                    "health_damaged");
            }

            if (IsCritterBehavior())
            {
                retreatBurstEndTime = lastDamagedTime + RetreatBurstSeconds;
                nextRetreatBurstAllowedTime = retreatBurstEndTime + RetreatReassessSeconds;
                CancelReadableActionForStartle();
            }

            ForceEngaged("health_damaged");
            TryShareAllyAlert(lastDamagedTime, EnemyStimulusTier.Loud, "health_damaged");
        }

        private void CancelReadableActionForStartle()
        {
            if (readabilityState == EnemyReadabilityState.Idle ||
                readabilityState == EnemyReadabilityState.EntryGrace ||
                readabilityState == EnemyReadabilityState.BossBurstWindup)
            {
                return;
            }

            readabilityState = EnemyReadabilityState.Idle;
            readabilityStateEndTime = 0f;
            chargeEndTime = 0f;
            chargeRecoveryEndTime = 0f;
            lungeEndTime = 0f;
            lungeRecoveryEndTime = 0f;
            rangedActiveEndTime = 0f;
            rangedRecoveryEndTime = 0f;
            ClearRangedTelegraphState();
            areaActiveEndTime = 0f;
            areaRecoveryEndTime = 0f;
            warningEndTime = 0f;
            guardActiveEndTime = 0f;
            guardRecoveryEndTime = 0f;
            creatureMoveActiveEndTime = 0f;
            creatureMoveRecoveryEndTime = 0f;
            creatureSignalActiveEndTime = 0f;
            creatureSignalRecoveryEndTime = 0f;
            lungeContactAttempted = false;
            chargeContactAttempted = false;
            rangedProjectileFired = false;
            areaDamageAttempted = false;
            creatureSignalEmitted = false;
            activeMeleeComboDepth = 0;
            activeChargeProfile = null;
            activeRangedProfile = null;
            activeMeleeProfile = null;
            activeAreaProfile = null;
            activeWarningProfile = null;
            activeGuardActionProfile = null;
            activeGuardProfile = null;
            activeCreatureMoveProfile = null;
            activeCreatureSignalProfile = null;
            activeCreatureMoveDirection = Vector3.forward;
            ClearRecoveryMovement();
            ResetSpacingResetChain();
            SyncNavMeshAgentAfterHollowOwnedMove("readability_cancelled");
        }

        public int ModifyIncomingDamage(DamageRequest request, int currentAmount)
        {
            if (currentAmount <= 0 || bossRuntime != null || !IsAlive || !IsPlayerAuthoredDamageSource(request.Source))
            {
                return currentAmount;
            }

            if (readabilityState == EnemyReadabilityState.GuardActive &&
                activeGuardProfile != null &&
                IsIncomingHitInsideGuardArc(request))
            {
                var reducedAmount = activeGuardProfile.ApplyReduction(request, currentAmount);
                if (activeGuardProfile.BreaksOn(request))
                {
                    InterruptGuardIntoRecovery(activeGuardProfile);
                }

                return reducedAmount;
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

        private bool IsIncomingHitInsideGuardArc(DamageRequest request)
        {
            if (request.Source == null || activeGuardProfile == null)
            {
                return false;
            }

            var toSource = request.Source.transform.position - transform.position;
            toSource.y = 0f;
            if (toSource.sqrMagnitude <= 0.001f)
            {
                return true;
            }

            var forward = TelegraphDirection.sqrMagnitude > 0.001f ? TelegraphDirection : FacingDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Vector3.forward;
            }

            return Vector3.Angle(forward.normalized, toSource.normalized) <= activeGuardProfile.FrontalArcDegrees * 0.5f;
        }

        private bool IsInterruptibleWindup()
        {
            return readabilityState is EnemyReadabilityState.MeleeWindup
                or EnemyReadabilityState.ChargeWindup
                or EnemyReadabilityState.RangedWindup
                or EnemyReadabilityState.AreaWindup
                or EnemyReadabilityState.CreatureMoveWindup
                or EnemyReadabilityState.CreatureSignalWindup;
        }

        private EnemyAttackProfileDefinition ActiveWindupProfile()
        {
            return readabilityState switch
            {
                EnemyReadabilityState.MeleeWindup => activeMeleeProfile,
                EnemyReadabilityState.ChargeWindup => activeChargeProfile,
                EnemyReadabilityState.RangedWindup => activeRangedProfile,
                EnemyReadabilityState.AreaWindup => activeAreaProfile,
                EnemyReadabilityState.CreatureMoveWindup => activeCreatureMoveProfile,
                EnemyReadabilityState.CreatureSignalWindup => activeCreatureSignalProfile,
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
                ClearRangedTelegraphState();
            }
            else if (readabilityState == EnemyReadabilityState.AreaWindup)
            {
                readabilityState = EnemyReadabilityState.AreaRecovery;
                areaRecoveryEndTime = recoveryEnd;
                areaActiveEndTime = 0f;
                areaDamageAttempted = false;
            }
            else if (readabilityState == EnemyReadabilityState.CreatureMoveWindup)
            {
                readabilityState = EnemyReadabilityState.CreatureMoveRecovery;
                creatureMoveRecoveryEndTime = recoveryEnd;
                creatureMoveActiveEndTime = 0f;
            }
            else if (readabilityState == EnemyReadabilityState.CreatureSignalWindup)
            {
                readabilityState = EnemyReadabilityState.CreatureSignalRecovery;
                creatureSignalRecoveryEndTime = recoveryEnd;
                creatureSignalActiveEndTime = 0f;
                creatureSignalEmitted = true;
            }

            readabilityStateEndTime = recoveryEnd;
            BeginRecoveryMovement(profile);
        }

        private void InterruptGuardIntoRecovery(EnemyGuardProfileDefinition profile)
        {
            var timeSeconds = lastTickTime > 0f ? lastTickTime : Time.time;
            var recoveryEnd = timeSeconds + (profile != null ? profile.GuardBreakRecoverySeconds : 0.35f);
            var interruptedActionProfile = activeGuardActionProfile;
            activeGuardProfile = null;
            activeGuardActionProfile = null;
            guardActiveEndTime = 0f;
            guardRecoveryEndTime = recoveryEnd;
            readabilityState = EnemyReadabilityState.GuardRecovery;
            readabilityStateEndTime = recoveryEnd;
            BeginRecoveryMovement(interruptedActionProfile);
        }

        private static bool IsPlayerAuthoredDamageSource(GameObject source)
        {
            return source != null &&
                   (source.GetComponent<PlayerWeaponController>() != null ||
                    source.GetComponent<ProjectileController>() != null);
        }

        private void FireRangedPattern(Vector3 direction, EnemyAttackProfileDefinition profile, float timeSeconds)
        {
            var resolvedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : FacingDirection;
            var count = Mathf.Max(1, profile != null ? profile.ProjectileCount : 1);
            switch (profile != null ? profile.RuntimeKind : EnemyAttackRuntimeKind.Projectile)
            {
                case EnemyAttackRuntimeKind.Beam:
                    TryApplyBeamDamage(profile, timeSeconds);
                    return;
                case EnemyAttackRuntimeKind.LockingBeam:
                    TryApplyBeamDamage(profile, timeSeconds);
                    return;
                case EnemyAttackRuntimeKind.FanProjectile:
                    FireFanProjectiles(resolvedDirection, count, profile != null ? profile.HitArcDegrees : 45f, profile);
                    return;
                case EnemyAttackRuntimeKind.RadialProjectile:
                    FireRadialProjectiles(count, profile, PatternOffsetDegrees(timeSeconds, profile));
                    return;
                case EnemyAttackRuntimeKind.SequentialRadialProjectile:
                    FireSequentialRadialProjectile(count, profile);
                    return;
                default:
                    if (count > 1)
                    {
                        FireFanProjectiles(resolvedDirection, count, Mathf.Min(28f, profile != null ? profile.HitArcDegrees : 28f), profile);
                        return;
                    }

                    FireProjectile(resolvedDirection, profile);
                    return;
            }
        }

        private void BeginRangedTelegraph(EnemyAttackProfileDefinition profile, float timeSeconds)
        {
            ClearRangedTelegraphState();
            if (profile == null || profile.RuntimeKind != EnemyAttackRuntimeKind.LockingBeam)
            {
                return;
            }

            rangedTelegraphPhase = EnemyRangedTelegraphPhase.Tracking;
            var totalWindup = Mathf.Max(
                LockingBeamTrackingSeconds + LockingBeamLockedSeconds,
                ResolvedWindupSeconds(profile, RangedWindupSeconds));
            lockingBeamLockTime = timeSeconds + Mathf.Max(0f, totalWindup - LockingBeamLockedSeconds);
            if (TryGetDirectionToPlayer(out var direction))
            {
                telegraphDirection = direction;
            }
        }

        private void TickLockingBeamTelegraph(float timeSeconds)
        {
            if (activeRangedProfile == null ||
                activeRangedProfile.RuntimeKind != EnemyAttackRuntimeKind.LockingBeam ||
                rangedTelegraphPhase == EnemyRangedTelegraphPhase.None)
            {
                return;
            }

            if (!lockingBeamDirectionLocked && timeSeconds < lockingBeamLockTime)
            {
                if (TryGetDirectionToPlayer(out var trackingDirection))
                {
                    telegraphDirection = trackingDirection;
                }

                rangedTelegraphPhase = EnemyRangedTelegraphPhase.Tracking;
                return;
            }

            if (!lockingBeamDirectionLocked && TryGetDirectionToPlayer(out var lockedDirection))
            {
                telegraphDirection = lockedDirection;
            }

            lockingBeamDirectionLocked = true;
            rangedTelegraphPhase = EnemyRangedTelegraphPhase.Locked;
        }

        private void ClearRangedTelegraphState()
        {
            rangedTelegraphPhase = EnemyRangedTelegraphPhase.None;
            lockingBeamDirectionLocked = false;
            lockingBeamLockTime = 0f;
        }

        private bool TryGetDirectionToPlayer(out Vector3 direction)
        {
            direction = Vector3.zero;
            if (playerController == null)
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            direction = delta.normalized;
            return true;
        }

        private void FireSequentialRadialProjectile(int count, EnemyAttackProfileDefinition profile)
        {
            var safeCount = Mathf.Max(1, count);
            var key = profile != null && !string.IsNullOrWhiteSpace(profile.AttackId)
                ? profile.AttackId
                : "__default_sequential_radial";
            sequentialRadialSteps.TryGetValue(key, out var stepIndex);
            var angle = 360f * (stepIndex % safeCount) / safeCount;
            FireProjectile(Quaternion.Euler(0f, angle, 0f) * Vector3.forward, profile);
            sequentialRadialSteps[key] = (stepIndex + 1) % safeCount;
        }

        private bool TryApplyBeamDamage(EnemyAttackProfileDefinition profile, float timeSeconds)
        {
            if (profile == null ||
                playerHealth == null ||
                playerController == null ||
                !playerHealth.IsAlive ||
                IsInEntryGrace(timeSeconds))
            {
                return false;
            }

            var toPlayer = playerController.transform.localPosition - transform.localPosition;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= 0.001f ||
                toPlayer.magnitude > profile.RangeMeters + PlaceholderPlayerController.DefaultRadiusMeters ||
                !IsPlayerInsideProfileArc(profile, TelegraphDirection))
            {
                return false;
            }

            var feelProfile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
            var damaged = DamageSystem.ApplyDamage(
                playerHealth,
                profile.CreateDamageRequest(gameObject, toPlayer, feelProfile.KnockbackSeconds));
            if (damaged)
            {
                VfxPresenter.Play(VfxCueId.PlayerHit, playerController.transform.position, playerController.transform.parent);
                AudioPresenter.Play(AudioCueId.PlayerHit, playerController.transform.position);
            }

            return damaged;
        }

        private void FireFanProjectiles(Vector3 direction, int count, float spreadDegrees, EnemyAttackProfileDefinition profile)
        {
            var safeCount = Mathf.Max(1, count);
            var safeSpread = Mathf.Clamp(spreadDegrees, 1f, 180f);
            var start = safeCount == 1 ? 0f : -safeSpread * 0.5f;
            var step = safeCount == 1 ? 0f : safeSpread / (safeCount - 1);
            for (var index = 0; index < safeCount; index++)
            {
                FireProjectile(Quaternion.Euler(0f, start + step * index, 0f) * direction.normalized, profile);
            }
        }

        private void FireRadialProjectiles(int count, EnemyAttackProfileDefinition profile, float offsetDegrees)
        {
            var safeCount = Mathf.Max(1, count);
            for (var index = 0; index < safeCount; index++)
            {
                var angle = offsetDegrees + 360f * index / safeCount;
                FireProjectile(Quaternion.Euler(0f, angle, 0f) * Vector3.forward, profile);
            }
        }

        private float PatternOffsetDegrees(float timeSeconds, EnemyAttackProfileDefinition profile)
        {
            var idHash = profile != null ? StableActionHash(profile.AttackId) : 0;
            return Mathf.Abs(idHash + Mathf.FloorToInt(timeSeconds * 11f) + Mathf.Max(0, spawnIndex) * 37) % 360;
        }

        private void FireProjectile(Vector3 direction, EnemyAttackProfileDefinition profile)
        {
            var projectileObject = enemyProjectilePrefab != null
                ? Hollow.Core.HollowRuntimePool.Rent(enemyProjectilePrefab, transform.parent)
                : Hollow.Core.HollowRuntimePool.RentPrimitive("EnemyProjectile.Fallback", PrimitiveType.Sphere, transform.parent);
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

        private bool IsPlayerInsideProfileArc(EnemyAttackProfileDefinition profile, Vector3 forward)
        {
            if (playerController == null || profile == null)
            {
                return false;
            }

            var toPlayer = playerController.transform.localPosition - transform.localPosition;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= 0.001f)
            {
                return true;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = FacingDirection;
            }

            var arc = ResolvedHitArcDegrees(profile);
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

            SeparateFromPlayerBump();
            ReceiveStimulus(EnemyStimulusKind.Bump, playerController.transform.localPosition, timeSeconds, EnemyStimulusTier.Normal, "body_overlap");
        }

        private void SeparateFromPlayerBump()
        {
            var away = transform.localPosition - playerController.transform.localPosition;
            away.y = 0f;
            if (away.sqrMagnitude <= 0.001f)
            {
                away = -FacingDirection;
            }

            var desired = transform.localPosition + away.normalized * 0.08f;
            ApplyNavigationMove(ResolveNavigationMove(desired, EnemyNavigationIntent.BumpSeparation), "bump_separation");
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

            if (behaviorId == EnemyBehaviorId.HollowBird)
            {
                return Definition.ResolveAttackProfile(DistanceToPlayer() <= 1.25f ? "swoop_peck" : "claw_dive");
            }

            if (behaviorId == EnemyBehaviorId.HollowBeast)
            {
                return Definition.ResolveAttackProfile(DistanceToPlayer() <= 1.35f ? "leap_bite" : "body_check");
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

            if (behaviorId == EnemyBehaviorId.HollowArcher)
            {
                var archerSlot = Mathf.Abs(Mathf.FloorToInt(timeSeconds * 0.65f) + Mathf.Max(0, spawnIndex)) % 4;
                return Definition.ResolveAttackProfile(archerSlot == 0 ? "arrow_volley" : archerSlot == 2 ? "retreating_arrow" : "arrow_shot");
            }

            if (behaviorId == EnemyBehaviorId.PowderGunner)
            {
                return Definition.ResolveAttackProfile(DistanceToPlayer() <= 5.2f ? "scatter_shot" : "aimed_musket_shot");
            }

            if (behaviorId == EnemyBehaviorId.KnifeThrower)
            {
                var throwerSlot = Mathf.Abs(Mathf.FloorToInt(timeSeconds * 0.9f) + Mathf.Max(0, spawnIndex)) % 3;
                return Definition.ResolveAttackProfile(throwerSlot == 1 ? "knife_fan" : "throwing_knife");
            }

            if (behaviorId == EnemyBehaviorId.RepeaterTurret)
            {
                var repeaterSlot = Mathf.Abs(Mathf.FloorToInt(timeSeconds * 0.8f) + Mathf.Max(0, spawnIndex)) % 4;
                return Definition.ResolveAttackProfile(repeaterSlot == 0 ? "suppressing_arc" : repeaterSlot == 2 ? "lock_on_dart" : "repeater_burst");
            }

            if (behaviorId == EnemyBehaviorId.ClockworkSentry)
            {
                var clockworkSlot = Mathf.Abs(Mathf.FloorToInt(timeSeconds * 0.75f) + Mathf.Max(0, spawnIndex)) % 5;
                return Definition.ResolveAttackProfile(clockworkSlot == 0 ? "clockwork_radial" : clockworkSlot == 2 ? "rotating_fan" : "gear_shot");
            }

            if (behaviorId == EnemyBehaviorId.HollowAcolyte)
            {
                var acolyteSlot = Mathf.Abs(Mathf.FloorToInt(timeSeconds * 0.75f) + Mathf.Max(0, spawnIndex)) % 4;
                return Definition.ResolveAttackProfile(acolyteSlot == 0 ? "rune_burst" : "slow_soul_orb");
            }

            if (behaviorId == EnemyBehaviorId.Wraith)
            {
                return Definition.ResolveAttackProfile("wraith_bolt");
            }

            if (behaviorId == EnemyBehaviorId.SoulEater)
            {
                var eaterSlot = Mathf.Abs(Mathf.FloorToInt(timeSeconds * 0.7f) + Mathf.Max(0, spawnIndex)) % 4;
                return Definition.ResolveAttackProfile(eaterSlot == 1 ? "soul_burst" : "soul_drain");
            }

            if (behaviorId == EnemyBehaviorId.CurseBinder)
            {
                var curseSlot = Mathf.Abs(Mathf.FloorToInt(timeSeconds * 0.7f) + Mathf.Max(0, spawnIndex)) % 4;
                return Definition.ResolveAttackProfile(curseSlot == 1 ? "sigil_fan" : "binding_bolt");
            }

            if (behaviorId == EnemyBehaviorId.GraveLantern)
            {
                var lanternSlot = Mathf.Abs(Mathf.FloorToInt(timeSeconds * 0.65f) + Mathf.Max(0, spawnIndex)) % 5;
                return Definition.ResolveAttackProfile(lanternSlot == 0 ? "lantern_soul_ring" : lanternSlot == 2 ? "lantern_curse_fan" : "grave_orb");
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

            if (behaviorId == EnemyBehaviorId.HollowBird)
            {
                return "swoop_peck";
            }

            if (behaviorId == EnemyBehaviorId.HollowBeast)
            {
                return "leap_bite";
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

        private void ApplyDefinitionPresentation(EnemyDefinition definition)
        {
            ApplyVisualMaterial(RoleForDefinition(definition));
            var prefabRole = PrefabRoleForDefinition(definition);
            var visual = PresentationPrefabResolver.InstantiateVisual(prefabRole, transform, Vector3.zero, Vector3.one);
            SetGameplayPlaceholderRenderersVisible(!ShouldHideGameplayPlaceholder(prefabRole, visual));
        }

        private void RefreshVisibilityRenderers()
        {
            cachedVisibilityRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        private static bool ShouldHideGameplayPlaceholder(PresentationPrefabRole role, GameObject visual)
        {
            if (visual == null || !IsMeshyMechanicalRole(role))
            {
                return false;
            }

            var marker = visual.GetComponent<PresentationVisualMarker>();
            return marker != null &&
                marker.Role == role &&
                !marker.IsFallback &&
                visual.GetComponentsInChildren<Renderer>(includeInactive: true).Length > 0;
        }

        private void SetGameplayPlaceholderRenderersVisible(bool visible)
        {
            foreach (var renderer in GetComponents<Renderer>())
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        private static bool IsMeshyMechanicalRole(PresentationPrefabRole role)
        {
            return role is PresentationPrefabRole.EnemyStarforgedOctantSentry or
                PresentationPrefabRole.EnemyCrimsonRailSpider or
                PresentationPrefabRole.EnemyAzureMinigunTurret;
        }

        private static MaterialRole RoleForDefinition(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return MaterialRole.EnemyNormal;
            }

            return definition.PresentationMaterialRole;
        }

        private static PresentationPrefabRole PrefabRoleForDefinition(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return PresentationPrefabRole.EnemyNormal;
            }

            return definition.PresentationPrefabRole;
        }

        private void InstantiateOptionalEnemyVisuals(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (definition.HasWeaponPrefabRoleOverride)
            {
                PresentationPrefabResolver.InstantiateVisual(definition.WeaponPrefabRole, transform, new Vector3(0.28f, 0.18f, 0.18f), Vector3.one * 0.45f);
            }

            if (definition.HasOffhandPrefabRoleOverride)
            {
                PresentationPrefabResolver.InstantiateVisual(definition.OffhandPrefabRole, transform, new Vector3(-0.28f, 0.18f, 0.12f), Vector3.one * 0.38f);
            }
        }

        private void InstantiateOptionalBossVisuals(BossDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (definition.HasWeaponPrefabRoleOverride)
            {
                PresentationPrefabResolver.InstantiateVisual(definition.WeaponPrefabRole, transform, new Vector3(0.42f, 0.24f, 0.2f), Vector3.one * 0.65f);
            }

            if (definition.HasOffhandPrefabRoleOverride)
            {
                PresentationPrefabResolver.InstantiateVisual(definition.OffhandPrefabRole, transform, new Vector3(-0.42f, 0.24f, 0.16f), Vector3.one * 0.55f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (lastNavigationResult.Backend != EnemyNavigationBackend.UnityNavMesh ||
                lastNavigationResult.PathStatus is not (EnemyPathStatus.Ready or EnemyPathStatus.Partial))
            {
                return;
            }

            var origin = transform.position + Vector3.up * 0.08f;
            var waypoint = NavigationLocalToWorld(lastNavigationResult.NextWaypointLocalPosition) + Vector3.up * 0.08f;
            var goal = NavigationLocalToWorld(lastNavigationResult.FinalGoalLocalPosition) + Vector3.up * 0.08f;
            Gizmos.color = lastNavigationResult.PathStatus == EnemyPathStatus.Ready
                ? new Color(0.15f, 0.85f, 1f, 0.85f)
                : new Color(1f, 0.75f, 0.15f, 0.85f);
            Gizmos.DrawLine(origin, waypoint);
            Gizmos.DrawWireSphere(waypoint, 0.12f);
            Gizmos.color = new Color(0.25f, 1f, 0.35f, 0.75f);
            Gizmos.DrawWireSphere(goal, 0.18f);
        }

        private void UpdateNavigationDebugOverlay()
        {
            if (lastNavigationResult.Backend == EnemyNavigationBackend.UnityNavMesh &&
                lastNavigationResult.PathStatus is EnemyPathStatus.Ready or EnemyPathStatus.Partial)
            {
                EnemyNavigationDebugOverlay.ReportActivePathUser(GetInstanceID());
            }

            if (!EnemyNavigationDebugOverlay.PathTracingEnabled ||
                lastNavigationResult.Backend != EnemyNavigationBackend.UnityNavMesh ||
                lastNavigationResult.PathStatus is not (EnemyPathStatus.Ready or EnemyPathStatus.Partial) ||
                !isActiveAndEnabled)
            {
                SetNavigationDebugLineVisible(false);
                return;
            }

            var line = EnsureNavigationDebugLine();
            if (line == null)
            {
                return;
            }

            var path = lastNavigationResult.PathWaypointsLocalPositions;
            var pathStartIndex = ResolveDebugPathStartIndex(path, lastNavigationResult.NextWaypointLocalPosition);
            var pointCount = path != null && path.Length > 0
                ? path.Length - pathStartIndex + 1
                : 3;
            line.positionCount = pointCount;
            line.SetPosition(0, transform.position + Vector3.up * 0.1f);
            if (path != null && path.Length > 0)
            {
                var lineIndex = 1;
                for (var index = pathStartIndex; index < path.Length; index++)
                {
                    line.SetPosition(lineIndex, NavigationLocalToWorld(path[index]) + Vector3.up * 0.1f);
                    lineIndex++;
                }
            }
            else
            {
                line.SetPosition(1, NavigationLocalToWorld(lastNavigationResult.NextWaypointLocalPosition) + Vector3.up * 0.1f);
                line.SetPosition(2, NavigationLocalToWorld(lastNavigationResult.FinalGoalLocalPosition) + Vector3.up * 0.1f);
            }

            var color = lastNavigationResult.PathStatus == EnemyPathStatus.Ready
                ? new Color(0.1f, 0.85f, 1f, 0.92f)
                : new Color(1f, 0.72f, 0.12f, 0.92f);
            line.startColor = color;
            line.endColor = new Color(0.25f, 1f, 0.35f, 0.92f);
            line.enabled = true;
        }

        private static int ResolveDebugPathStartIndex(Vector3[] path, Vector3 nextWaypointLocalPosition)
        {
            if (path == null || path.Length == 0)
            {
                return 0;
            }

            var bestIndex = 0;
            var bestDistance = float.MaxValue;
            for (var index = 0; index < path.Length; index++)
            {
                var distance = (Flat(path[index]) - Flat(nextWaypointLocalPosition)).sqrMagnitude;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestIndex = index;
            }

            return bestIndex;
        }

        private LineRenderer EnsureNavigationDebugLine()
        {
            if (navigationDebugLine != null)
            {
                return navigationDebugLine;
            }

            var lineObject = new GameObject("Debug.NavigationPathTrace");
            lineObject.transform.SetParent(transform, false);
            navigationDebugLine = lineObject.AddComponent<LineRenderer>();
            navigationDebugLine.useWorldSpace = true;
            navigationDebugLine.loop = false;
            navigationDebugLine.widthMultiplier = 0.055f;
            navigationDebugLine.numCapVertices = 3;
            navigationDebugLine.numCornerVertices = 2;
            navigationDebugLine.sharedMaterial = MaterialResolver.Resolve(MaterialRole.VfxDebug);
            navigationDebugLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            navigationDebugLine.receiveShadows = false;
            return navigationDebugLine;
        }

        private void SetNavigationDebugLineVisible(bool visible)
        {
            if (navigationDebugLine != null)
            {
                navigationDebugLine.enabled = visible;
            }
        }

        private void UpdateAiDebugOverlay()
        {
            if (bossRuntime != null || bossDefinition != null || archetypeId == EnemyArchetypeId.Boss)
            {
                SetAiDebugTextVisible(false);
                return;
            }

            EnemyAiDebugOverlay.ReportBlackboard(GetInstanceID(), aiBrain.Blackboard);
            EnemyTacticalDebugOverlay.ReportIntent(GetInstanceID(), lastTacticalIntent);
            if (EnemyDesignerDebugOverlay.Enabled)
            {
                EnemyDesignerDebugOverlay.ReportEnemy(GetInstanceID());
            }

            if ((!EnemyDesignerDebugOverlay.Enabled && !EnemyAiDebugOverlay.BlackboardEnabled && !EnemyTacticalDebugOverlay.Enabled) || !isActiveAndEnabled)
            {
                SetAiDebugTextVisible(false);
                return;
            }

            var text = EnsureAiDebugText();
            if (text == null)
            {
                return;
            }

            var unityBehaviorTrace = behaviorRuntimeMode == EnemyBehaviorRuntimeMode.UnityBehaviorGraph && unityBehaviorGraphBridge != null
                ? $"\nUB {unityBehaviorGraphBridge.LastTraceSummary} | fail {unityBehaviorGraphBridge.LastOfficialGraphFailureReason}"
                : string.Empty;
            text.characterSize = EnemyDesignerDebugOverlay.Enabled ? 0.065f : 0.08f;
            text.text = EnemyDesignerDebugOverlay.Enabled
                ? EnemyDesignerDebugOverlay.BuildOverlayText(this)
                : EnemyTacticalDebugOverlay.Enabled
                    ? $"{aiBrain.Blackboard.Summary}\n{lastTacticalIntent.Summary}{unityBehaviorTrace}"
                    : $"{aiBrain.Blackboard.Summary}{unityBehaviorTrace}";
            text.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            text.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            text.gameObject.SetActive(true);
        }

        private TextMesh EnsureAiDebugText()
        {
            if (aiDebugText != null)
            {
                return aiDebugText;
            }

            var textObject = new GameObject("Debug.AiBlackboard");
            textObject.transform.SetParent(transform, false);
            aiDebugText = textObject.AddComponent<TextMesh>();
            aiDebugText.anchor = TextAnchor.MiddleCenter;
            aiDebugText.alignment = TextAlignment.Center;
            aiDebugText.characterSize = 0.08f;
            aiDebugText.fontSize = 28;
            aiDebugText.color = new Color(0.75f, 1f, 0.78f, 0.95f);
            return aiDebugText;
        }

        private void SetAiDebugTextVisible(bool visible)
        {
            if (aiDebugText != null)
            {
                aiDebugText.gameObject.SetActive(visible);
            }
        }

        private Vector3 NavigationLocalToWorld(Vector3 localPosition)
        {
            return roomRuntimeRoot != null
                ? roomRuntimeRoot.transform.TransformPoint(localPosition)
                : localPosition;
        }

        private static Vector3 Flat(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }

        private static float FlatDistance(Vector3 left, Vector3 right)
        {
            return Vector3.Distance(Flat(left), Flat(right));
        }
    }
}
