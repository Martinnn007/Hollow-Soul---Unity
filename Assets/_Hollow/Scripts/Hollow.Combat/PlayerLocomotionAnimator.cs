using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerLocomotionAnimator : MonoBehaviour
    {
        public const string IsMovingParameter = "IsMoving";
        public const string MoveSpeedParameter = "MoveSpeed";
        public const string ActionSpeedParameter = "ActionSpeed";
        public const string RollTriggerParameter = "Roll";
        public const string SlashTriggerParameter = "Slash";
        public const string HitTriggerParameter = "Hit";
        public const string DeathTriggerParameter = "Death";
        public const string IsDeadParameter = "IsDead";
        public const string IsTargetLockedParameter = "IsTargetLocked";
        public const string LockedMoveXParameter = "LockedMoveX";
        public const string LockedMoveYParameter = "LockedMoveY";
        public const float DefaultTurnInPlaceStartDegrees = 85f;
        public const float DefaultTurnInPlaceFullDegrees = 125f;
        public const float DefaultDiagnosticSmoothingSeconds = 0.08f;

        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualFacingRoot;
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private CombatantHealth health;
        [SerializeField] private PlayerAimLockController aimLockController;
        [SerializeField] private float movementThresholdMetersPerSecond = 0.05f;
        [SerializeField] private float turnSpeedDegreesPerSecond = 720f;
        [SerializeField] private float walkSpeedNormalizationMetersPerSecond = PlayerMovementController.DefaultSpeedMetersPerSecond;
        [SerializeField] private float teleportResetDistanceMeters = 1.5f;
        [SerializeField] private float rollClipDurationSeconds = PlayerWeaponController.RollDurationSeconds;
        [SerializeField] private float slashClipDurationSeconds = 0.75f;
        [SerializeField] private float hitClipDurationSeconds = 0.45f;
        [SerializeField] private float deathClipDurationSeconds = 1.1f;
        [SerializeField] private float turnInPlaceStartDegrees = DefaultTurnInPlaceStartDegrees;
        [SerializeField] private float turnInPlaceFullDegrees = DefaultTurnInPlaceFullDegrees;
        [SerializeField] private float diagnosticSmoothingSeconds = DefaultDiagnosticSmoothingSeconds;

        private static readonly int IsMovingHash = Animator.StringToHash(IsMovingParameter);
        private static readonly int MoveSpeedHash = Animator.StringToHash(MoveSpeedParameter);
        private static readonly int ActionSpeedHash = Animator.StringToHash(ActionSpeedParameter);
        private static readonly int RollTriggerHash = Animator.StringToHash(RollTriggerParameter);
        private static readonly int SlashTriggerHash = Animator.StringToHash(SlashTriggerParameter);
        private static readonly int HitTriggerHash = Animator.StringToHash(HitTriggerParameter);
        private static readonly int DeathTriggerHash = Animator.StringToHash(DeathTriggerParameter);
        private static readonly int IsDeadHash = Animator.StringToHash(IsDeadParameter);
        private static readonly int IsTargetLockedHash = Animator.StringToHash(IsTargetLockedParameter);
        private static readonly int LockedMoveXHash = Animator.StringToHash(LockedMoveXParameter);
        private static readonly int LockedMoveYHash = Animator.StringToHash(LockedMoveYParameter);

        private PlayerWeaponController subscribedWeaponController;
        private CombatantHealth subscribedHealth;
        private Vector3 previousWorldPosition;
        private Vector3 facingDirectionWorld = Vector3.forward;
        private bool hasIsMovingParameter;
        private bool hasMoveSpeedParameter;
        private bool hasActionSpeedParameter;
        private bool hasRollTriggerParameter;
        private bool hasSlashTriggerParameter;
        private bool hasHitTriggerParameter;
        private bool hasDeathTriggerParameter;
        private bool hasIsDeadParameter;
        private bool hasIsTargetLockedParameter;
        private bool hasLockedMoveXParameter;
        private bool hasLockedMoveYParameter;
        private bool parameterCacheValid;
        private bool initialized;
        private bool isMoving;
        private bool isDead;
        private bool isTargetLockedForLocomotion;
        private bool pendingRollTrigger;
        private bool pendingSlashTrigger;
        private bool pendingHitTrigger;
        private bool pendingDeathTrigger;
        private float planarSpeedMetersPerSecond;
        private float actionSpeed = 1f;
        private float actionFacingUntilTime;
        private Vector2 lockedRelativeMove;
        private Vector2 smoothedRelativeMove;
        private Vector3 worldVelocity;
        private Vector3 smoothedWorldVelocity;
        private PlayerRollPhase lastObservedRollPhase = PlayerRollPhase.None;
        private Vector3 aimFacingDirectionWorld = Vector3.forward;
        private float aimBodyAngleDegrees;
        private bool isTurnInPlaceActive;

        public bool IsMoving => isMoving;

        public bool IsDead => isDead;

        public PlayerRollPhase LastObservedRollPhase => lastObservedRollPhase;

        public float PlanarSpeedMetersPerSecond => planarSpeedMetersPerSecond;

        public float ActionSpeed => actionSpeed;

        public Vector3 FacingDirectionWorld => facingDirectionWorld;

        public Vector3 AimFacingDirectionWorld => aimFacingDirectionWorld;

        public Vector3 WorldVelocity => smoothedWorldVelocity;

        public Vector3 RawWorldVelocity => worldVelocity;

        public bool IsTargetLockedForLocomotion => isTargetLockedForLocomotion;

        public Vector2 LockedRelativeMove => lockedRelativeMove;

        public Vector2 RelativeMove => smoothedRelativeMove;

        public float MoveAngleDegrees => smoothedRelativeMove.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(smoothedRelativeMove.x, smoothedRelativeMove.y) * Mathf.Rad2Deg
            : 0f;

        public bool IsBackpedaling => smoothedRelativeMove.y < -0.35f;

        public bool IsStrafing => Mathf.Abs(smoothedRelativeMove.x) > 0.35f;

        public float AimBodyAngleDegrees => aimBodyAngleDegrees;

        public bool IsTurnInPlaceActive => isTurnInPlaceActive;

        private void Awake()
        {
            ResolveReferences();
            ResetTracking();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResetTracking();
        }

        private void OnDisable()
        {
            UnbindWeaponEvents();
            UnbindHealthEvents();
        }

        private void OnDestroy()
        {
            UnbindWeaponEvents();
            UnbindHealthEvents();
        }

        private void OnValidate()
        {
            movementThresholdMetersPerSecond = Mathf.Max(0f, movementThresholdMetersPerSecond);
            turnSpeedDegreesPerSecond = Mathf.Max(0f, turnSpeedDegreesPerSecond);
            walkSpeedNormalizationMetersPerSecond = Mathf.Max(0.01f, walkSpeedNormalizationMetersPerSecond);
            teleportResetDistanceMeters = Mathf.Max(0.1f, teleportResetDistanceMeters);
            rollClipDurationSeconds = Mathf.Max(0.01f, rollClipDurationSeconds);
            slashClipDurationSeconds = Mathf.Max(0.01f, slashClipDurationSeconds);
            hitClipDurationSeconds = Mathf.Max(0.01f, hitClipDurationSeconds);
            deathClipDurationSeconds = Mathf.Max(0.01f, deathClipDurationSeconds);
            turnInPlaceStartDegrees = Mathf.Clamp(turnInPlaceStartDegrees, 0f, 180f);
            turnInPlaceFullDegrees = Mathf.Clamp(Mathf.Max(turnInPlaceStartDegrees, turnInPlaceFullDegrees), 0f, 180f);
            diagnosticSmoothingSeconds = Mathf.Max(0f, diagnosticSmoothingSeconds);
        }

        private void LateUpdate()
        {
            Sample(Time.deltaTime);
        }

        public void Bind(Animator nextAnimator, Transform nextVisualFacingRoot)
        {
            animator = nextAnimator;
            visualFacingRoot = nextVisualFacingRoot;
            parameterCacheValid = false;
        }

        public void BindGameplay(PlayerWeaponController nextWeaponController, CombatantHealth nextHealth)
        {
            BindGameplay(nextWeaponController, nextHealth, aimLockController);
        }

        public void BindGameplay(
            PlayerWeaponController nextWeaponController,
            CombatantHealth nextHealth,
            PlayerAimLockController nextAimLockController)
        {
            if (weaponController != nextWeaponController)
            {
                UnbindWeaponEvents();
                weaponController = nextWeaponController;
            }

            if (health != nextHealth)
            {
                UnbindHealthEvents();
                health = nextHealth;
            }

            aimLockController = nextAimLockController;
            ResolveReferences();
        }

        public void Configure(
            float nextMovementThresholdMetersPerSecond,
            float nextTurnSpeedDegreesPerSecond,
            float nextWalkSpeedNormalizationMetersPerSecond,
            float nextTeleportResetDistanceMeters,
            float nextTurnInPlaceStartDegrees = DefaultTurnInPlaceStartDegrees,
            float nextTurnInPlaceFullDegrees = DefaultTurnInPlaceFullDegrees)
        {
            movementThresholdMetersPerSecond = Mathf.Max(0f, nextMovementThresholdMetersPerSecond);
            turnSpeedDegreesPerSecond = Mathf.Max(0f, nextTurnSpeedDegreesPerSecond);
            walkSpeedNormalizationMetersPerSecond = Mathf.Max(0.01f, nextWalkSpeedNormalizationMetersPerSecond);
            teleportResetDistanceMeters = Mathf.Max(0.1f, nextTeleportResetDistanceMeters);
            turnInPlaceStartDegrees = Mathf.Clamp(nextTurnInPlaceStartDegrees, 0f, 180f);
            turnInPlaceFullDegrees = Mathf.Clamp(Mathf.Max(turnInPlaceStartDegrees, nextTurnInPlaceFullDegrees), 0f, 180f);
        }

        public void ConfigureActionClips(
            float nextRollClipDurationSeconds,
            float nextSlashClipDurationSeconds,
            float nextHitClipDurationSeconds,
            float nextDeathClipDurationSeconds)
        {
            rollClipDurationSeconds = Mathf.Max(0.01f, nextRollClipDurationSeconds);
            slashClipDurationSeconds = Mathf.Max(0.01f, nextSlashClipDurationSeconds);
            hitClipDurationSeconds = Mathf.Max(0.01f, nextHitClipDurationSeconds);
            deathClipDurationSeconds = Mathf.Max(0.01f, nextDeathClipDurationSeconds);
        }

        public void ResetTracking()
        {
            ResolveReferences();
            previousWorldPosition = transform.position;
            initialized = true;
            planarSpeedMetersPerSecond = 0f;
            isMoving = false;
            isDead = health != null && !health.IsAlive;
            isTargetLockedForLocomotion = false;
            lockedRelativeMove = Vector2.zero;
            smoothedRelativeMove = Vector2.zero;
            worldVelocity = Vector3.zero;
            smoothedWorldVelocity = Vector3.zero;
            aimFacingDirectionWorld = facingDirectionWorld;
            aimBodyAngleDegrees = 0f;
            isTurnInPlaceActive = false;
            lastObservedRollPhase = weaponController != null ? weaponController.CurrentRollPhase : PlayerRollPhase.None;
            ApplyAnimatorParameters();
        }

        public void Sample(float deltaTime)
        {
            ResolveReferences();
            ObserveRollPhase();
            if (!initialized)
            {
                ResetTracking();
            }

            var currentWorldPosition = transform.position;
            var planarDelta = currentWorldPosition - previousWorldPosition;
            planarDelta.y = 0f;
            previousWorldPosition = currentWorldPosition;
            worldVelocity = deltaTime > 0f && planarDelta.magnitude < teleportResetDistanceMeters
                ? planarDelta / deltaTime
                : Vector3.zero;
            smoothedWorldVelocity = SmoothVector(smoothedWorldVelocity, worldVelocity, deltaTime);

            if (isDead)
            {
                planarSpeedMetersPerSecond = 0f;
                isMoving = false;
                isTargetLockedForLocomotion = false;
                lockedRelativeMove = Vector2.zero;
                smoothedRelativeMove = SmoothVector2(smoothedRelativeMove, Vector2.zero, deltaTime);
                aimBodyAngleDegrees = 0f;
                isTurnInPlaceActive = false;
                ApplyAnimatorParameters();
                return;
            }

            var hasLockedFacing = UpdateLockedFacing(planarDelta, deltaTime, rotateVisual: true);
            if (deltaTime <= 0f)
            {
                planarSpeedMetersPerSecond = 0f;
                isMoving = false;
                if (!hasLockedFacing)
                {
                    lockedRelativeMove = Vector2.zero;
                }

                smoothedRelativeMove = SmoothVector2(smoothedRelativeMove, lockedRelativeMove, deltaTime);
                ApplyAnimatorParameters();
                return;
            }

            if (planarDelta.magnitude >= teleportResetDistanceMeters)
            {
                planarSpeedMetersPerSecond = 0f;
                isMoving = false;
                lockedRelativeMove = Vector2.zero;
                smoothedRelativeMove = SmoothVector2(smoothedRelativeMove, Vector2.zero, deltaTime);
                ApplyAnimatorParameters();
                return;
            }

            planarSpeedMetersPerSecond = planarDelta.magnitude / deltaTime;
            isMoving = planarSpeedMetersPerSecond >= movementThresholdMetersPerSecond;
            if (!hasLockedFacing && isMoving && planarDelta.sqrMagnitude > 0.000001f)
            {
                if (Time.time >= actionFacingUntilTime)
                {
                    facingDirectionWorld = planarDelta.normalized;
                }

                RotateVisualFacingRoot(deltaTime, instant: false);
            }

            smoothedRelativeMove = SmoothVector2(smoothedRelativeMove, lockedRelativeMove, deltaTime);
            ApplyAnimatorParameters();
        }

        private void ResolveReferences()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(includeInactive: true);
                parameterCacheValid = false;
            }

            if (visualFacingRoot == null && animator != null)
            {
                visualFacingRoot = animator.transform;
            }

            if (weaponController == null)
            {
                weaponController = GetComponent<PlayerWeaponController>();
            }

            if (health == null)
            {
                health = GetComponent<CombatantHealth>();
            }

            if (aimLockController == null)
            {
                aimLockController = GetComponent<PlayerAimLockController>();
            }

            BindWeaponEvents();
            BindHealthEvents();
        }

        private void BindWeaponEvents()
        {
            if (subscribedWeaponController == weaponController)
            {
                return;
            }

            UnbindWeaponEvents();
            subscribedWeaponController = weaponController;
            if (subscribedWeaponController != null)
            {
                subscribedWeaponController.WeaponActionAnimationRequested += OnWeaponActionAnimationRequested;
            }
        }

        private void UnbindWeaponEvents()
        {
            if (subscribedWeaponController == null)
            {
                return;
            }

            subscribedWeaponController.WeaponActionAnimationRequested -= OnWeaponActionAnimationRequested;
            subscribedWeaponController = null;
        }

        private void BindHealthEvents()
        {
            if (subscribedHealth == health)
            {
                return;
            }

            UnbindHealthEvents();
            subscribedHealth = health;
            if (subscribedHealth != null)
            {
                subscribedHealth.Damaged += OnDamaged;
                subscribedHealth.Died += OnDied;
            }
        }

        private void UnbindHealthEvents()
        {
            if (subscribedHealth == null)
            {
                return;
            }

            subscribedHealth.Damaged -= OnDamaged;
            subscribedHealth.Died -= OnDied;
            subscribedHealth = null;
        }

        private void ObserveRollPhase()
        {
            if (weaponController == null || isDead)
            {
                lastObservedRollPhase = PlayerRollPhase.None;
                return;
            }

            var phase = weaponController.CurrentRollPhase;
            if (phase == PlayerRollPhase.Startup && lastObservedRollPhase == PlayerRollPhase.None)
            {
                PlayRollAnimation(weaponController.RollDirection, PlayerWeaponController.RollDurationSeconds);
            }

            lastObservedRollPhase = phase;
        }

        private void OnWeaponActionAnimationRequested(
            WeaponSlot slot,
            AttackKind attackKind,
            Vector2 direction,
            float actionDurationSeconds)
        {
            if (isDead)
            {
                return;
            }

            actionSpeed = ClipSpeedForDuration(slashClipDurationSeconds, actionDurationSeconds);
            if (slot == WeaponSlot.Melee)
            {
                pendingSlashTrigger = true;
            }

            SetActionFacing(direction, actionDurationSeconds);
            ApplyAnimatorParameters();
        }

        private void OnDamaged(CombatantHealth damagedHealth)
        {
            if (isDead || damagedHealth == null || !damagedHealth.IsAlive)
            {
                return;
            }

            actionSpeed = ClipSpeedForDuration(hitClipDurationSeconds, hitClipDurationSeconds);
            pendingHitTrigger = true;
            actionFacingUntilTime = Mathf.Max(actionFacingUntilTime, Time.time + hitClipDurationSeconds);
            ApplyAnimatorParameters();
        }

        private void OnDied(CombatantHealth _)
        {
            isDead = true;
            isMoving = false;
            planarSpeedMetersPerSecond = 0f;
            isTargetLockedForLocomotion = false;
            lockedRelativeMove = Vector2.zero;
            actionSpeed = ClipSpeedForDuration(deathClipDurationSeconds, deathClipDurationSeconds);
            pendingDeathTrigger = true;
            actionFacingUntilTime = Mathf.Max(actionFacingUntilTime, Time.time + deathClipDurationSeconds);
            ApplyAnimatorParameters();
        }

        private void PlayRollAnimation(Vector2 direction, float actionDurationSeconds)
        {
            actionSpeed = ClipSpeedForDuration(rollClipDurationSeconds, actionDurationSeconds);
            pendingRollTrigger = true;
            SetActionFacing(direction, actionDurationSeconds);
            ApplyAnimatorParameters();
        }

        private void SetActionFacing(Vector2 direction, float holdSeconds)
        {
            if (UpdateLockedFacing(Vector3.zero, Time.deltaTime, rotateVisual: true))
            {
                actionFacingUntilTime = Mathf.Max(actionFacingUntilTime, Time.time + Mathf.Max(0.05f, holdSeconds));
                return;
            }

            if (direction.sqrMagnitude > 0.001f)
            {
                facingDirectionWorld = new Vector3(direction.x, 0f, direction.y).normalized;
                RotateVisualFacingRoot(0f, instant: true);
            }

            actionFacingUntilTime = Mathf.Max(actionFacingUntilTime, Time.time + Mathf.Max(0.05f, holdSeconds));
        }

        private bool UpdateLockedFacing(Vector3 planarDelta, float deltaTime, bool rotateVisual)
        {
            var lockedDirection = Vector2.zero;
            var hasLockedDirection = !isDead && (
                TryGetWeaponAimCommitment(out lockedDirection) ||
                (aimLockController != null && aimLockController.TryGetLockedTargetDirection(out lockedDirection)) ||
                TryGetManualAimDirection(out lockedDirection));
            isTargetLockedForLocomotion = hasLockedDirection;
            if (!hasLockedDirection)
            {
                lockedRelativeMove = Vector2.zero;
                aimFacingDirectionWorld = facingDirectionWorld;
                aimBodyAngleDegrees = 0f;
                isTurnInPlaceActive = false;
                return false;
            }

            aimFacingDirectionWorld = new Vector3(lockedDirection.x, 0f, lockedDirection.y).normalized;
            aimBodyAngleDegrees = Vector3.Angle(SafePlanarDirection(facingDirectionWorld, Vector3.forward), aimFacingDirectionWorld);
            isTurnInPlaceActive = aimBodyAngleDegrees >= turnInPlaceStartDegrees;
            if (isTurnInPlaceActive)
            {
                var turnWeight = turnInPlaceFullDegrees <= turnInPlaceStartDegrees
                    ? 1f
                    : Mathf.InverseLerp(turnInPlaceStartDegrees, turnInPlaceFullDegrees, aimBodyAngleDegrees);
                facingDirectionWorld = rotateVisual && deltaTime > 0f
                    ? Vector3.RotateTowards(
                        SafePlanarDirection(facingDirectionWorld, aimFacingDirectionWorld),
                        aimFacingDirectionWorld,
                        Mathf.Deg2Rad * turnSpeedDegreesPerSecond * Mathf.Max(0.15f, turnWeight) * deltaTime,
                        0f)
                    : aimFacingDirectionWorld;
                facingDirectionWorld = SafePlanarDirection(facingDirectionWorld, aimFacingDirectionWorld);
                aimBodyAngleDegrees = Vector3.Angle(facingDirectionWorld, aimFacingDirectionWorld);
            }

            lockedRelativeMove = CalculateLockedRelativeMove(planarDelta, facingDirectionWorld, deltaTime);
            if (rotateVisual)
            {
                RotateVisualFacingRoot(deltaTime, instant: deltaTime <= 0f);
            }

            return true;
        }

        private bool TryGetWeaponAimCommitment(out Vector2 direction)
        {
            direction = Vector2.zero;
            if (weaponController == null || !weaponController.HasVisualAimCommitment)
            {
                return false;
            }

            direction = weaponController.VisualAimDirection;
            return direction.sqrMagnitude > 0.001f;
        }

        private bool TryGetManualAimDirection(out Vector2 direction)
        {
            direction = Vector2.zero;
            if (aimLockController == null || !aimLockController.HasManualAimOverride)
            {
                return false;
            }

            direction = aimLockController.AttackDirection;
            return direction.sqrMagnitude > 0.001f;
        }

        private Vector2 CalculateLockedRelativeMove(Vector3 planarDelta, Vector3 forward, float deltaTime)
        {
            if (deltaTime <= 0f || planarDelta.sqrMagnitude <= 0.000001f)
            {
                return Vector2.zero;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.000001f)
            {
                return Vector2.zero;
            }

            forward.Normalize();
            var right = new Vector3(forward.z, 0f, -forward.x);
            var normalizedDelta = planarDelta / Mathf.Max(0.01f, walkSpeedNormalizationMetersPerSecond * deltaTime);
            return new Vector2(
                Mathf.Clamp(Vector3.Dot(normalizedDelta, right), -1f, 1f),
                Mathf.Clamp(Vector3.Dot(normalizedDelta, forward), -1f, 1f));
        }

        private Vector3 SmoothVector(Vector3 current, Vector3 target, float deltaTime)
        {
            if (diagnosticSmoothingSeconds <= 0f || deltaTime <= 0f)
            {
                return target;
            }

            return Vector3.Lerp(current, target, 1f - Mathf.Exp(-deltaTime / diagnosticSmoothingSeconds));
        }

        private Vector2 SmoothVector2(Vector2 current, Vector2 target, float deltaTime)
        {
            if (diagnosticSmoothingSeconds <= 0f || deltaTime <= 0f)
            {
                return target;
            }

            return Vector2.Lerp(current, target, 1f - Mathf.Exp(-deltaTime / diagnosticSmoothingSeconds));
        }

        private static Vector3 SafePlanarDirection(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.000001f)
            {
                return direction.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.000001f ? fallback.normalized : Vector3.forward;
        }

        private void RotateVisualFacingRoot(float deltaTime, bool instant)
        {
            if (visualFacingRoot == null)
            {
                return;
            }

            var localDirection = transform.parent != null
                ? transform.parent.InverseTransformDirection(facingDirectionWorld)
                : facingDirectionWorld;
            localDirection.y = 0f;
            if (localDirection.sqrMagnitude < 0.000001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(localDirection.normalized, Vector3.up);
            visualFacingRoot.localRotation = instant
                ? targetRotation
                : Quaternion.RotateTowards(
                    visualFacingRoot.localRotation,
                    targetRotation,
                    turnSpeedDegreesPerSecond * deltaTime);
        }

        private void ApplyAnimatorParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            CacheAnimatorParameters();
            if (hasIsMovingParameter)
            {
                animator.SetBool(IsMovingHash, isMoving && !isDead);
            }

            if (hasMoveSpeedParameter)
            {
                animator.SetFloat(MoveSpeedHash, Mathf.Clamp01(planarSpeedMetersPerSecond / walkSpeedNormalizationMetersPerSecond));
            }

            if (hasActionSpeedParameter)
            {
                animator.SetFloat(ActionSpeedHash, Mathf.Max(0.01f, actionSpeed));
            }

            if (hasIsDeadParameter)
            {
                animator.SetBool(IsDeadHash, isDead);
            }

            if (hasIsTargetLockedParameter)
            {
                animator.SetBool(IsTargetLockedHash, isTargetLockedForLocomotion && !isDead);
            }

            if (hasLockedMoveXParameter)
            {
                animator.SetFloat(LockedMoveXHash, isTargetLockedForLocomotion && !isDead ? lockedRelativeMove.x : 0f);
            }

            if (hasLockedMoveYParameter)
            {
                animator.SetFloat(LockedMoveYHash, isTargetLockedForLocomotion && !isDead ? lockedRelativeMove.y : 0f);
            }

            SetTriggerIfAvailable(ref pendingRollTrigger, hasRollTriggerParameter, RollTriggerHash);
            SetTriggerIfAvailable(ref pendingSlashTrigger, hasSlashTriggerParameter, SlashTriggerHash);
            SetTriggerIfAvailable(ref pendingHitTrigger, hasHitTriggerParameter, HitTriggerHash);
            SetTriggerIfAvailable(ref pendingDeathTrigger, hasDeathTriggerParameter, DeathTriggerHash);
        }

        private void SetTriggerIfAvailable(ref bool pendingTrigger, bool hasParameter, int hash)
        {
            if (!pendingTrigger || !hasParameter)
            {
                return;
            }

            animator.ResetTrigger(hash);
            animator.SetTrigger(hash);
            pendingTrigger = false;
        }

        private void CacheAnimatorParameters()
        {
            if (parameterCacheValid)
            {
                return;
            }

            hasIsMovingParameter = false;
            hasMoveSpeedParameter = false;
            hasActionSpeedParameter = false;
            hasRollTriggerParameter = false;
            hasSlashTriggerParameter = false;
            hasHitTriggerParameter = false;
            hasDeathTriggerParameter = false;
            hasIsDeadParameter = false;
            hasIsTargetLockedParameter = false;
            hasLockedMoveXParameter = false;
            hasLockedMoveYParameter = false;
            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash == IsMovingHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsMovingParameter = true;
                }
                else if (parameter.nameHash == MoveSpeedHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasMoveSpeedParameter = true;
                }
                else if (parameter.nameHash == ActionSpeedHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasActionSpeedParameter = true;
                }
                else if (parameter.nameHash == RollTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasRollTriggerParameter = true;
                }
                else if (parameter.nameHash == SlashTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasSlashTriggerParameter = true;
                }
                else if (parameter.nameHash == HitTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasHitTriggerParameter = true;
                }
                else if (parameter.nameHash == DeathTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasDeathTriggerParameter = true;
                }
                else if (parameter.nameHash == IsDeadHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsDeadParameter = true;
                }
                else if (parameter.nameHash == IsTargetLockedHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsTargetLockedParameter = true;
                }
                else if (parameter.nameHash == LockedMoveXHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasLockedMoveXParameter = true;
                }
                else if (parameter.nameHash == LockedMoveYHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasLockedMoveYParameter = true;
                }
            }

            parameterCacheValid = true;
        }

        private static float ClipSpeedForDuration(float clipDurationSeconds, float targetDurationSeconds)
        {
            return Mathf.Clamp(
                Mathf.Max(0.01f, clipDurationSeconds) / Mathf.Max(0.01f, targetDurationSeconds),
                0.05f,
                5f);
        }
    }
}
