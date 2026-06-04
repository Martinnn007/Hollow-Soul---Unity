using Hollow.Data.Definitions;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Hollow.Combat
{
    public enum PlayerAnimationLocomotionMode
    {
        Idle,
        Moving,
        Locked,
        Roll,
        Dead
    }

    public enum PlayerAnimationUpperBodyPose
    {
        None,
        MeleeAttack,
        RangedAim,
        ShieldGuard
    }

    public enum PlayerAnimationActionPhase
    {
        None,
        MeleeAttack,
        RangedAttack,
        Guard,
        Roll,
        Dead
    }

    [DefaultExecutionOrder(80)]
    public sealed class PlayerAnimationPoseCoordinator : MonoBehaviour
    {
        public const float DefaultRigBlendSpeed = 9f;
        public const float DefaultImpulseDecaySpeed = 5.5f;
        public const float DefaultLeanBlendSpeed = 8f;
        public const float DefaultLeanSpeedReferenceMetersPerSecond = PlayerMovementController.DefaultSpeedMetersPerSecond;
        public const float DefaultFootYawAimInfluenceMaxDegrees = 15f;
        public const float DefaultHitReactionFootIkSuppressSeconds = 0.35f;
        public const string ModernAnimationRigRootName = "MainCharacter_ModernAnimationRigRoot";
        public const string BaseLocomotionRigName = "BaseLocomotionRig";
        public const string FullBodyActionRigName = "FullBodyActionRig";
        public const string UpperBodyCombatRigName = "UpperBodyCombatRig";
        public const string AdditivePhysicalResponseRigName = "AdditivePhysicalResponseRig";
        public const string RigTargetsRootName = "RigTargets";
        public const string RightHandWeaponTargetName = "RightHandWeaponTarget";
        public const string LeftHandShieldTargetName = "LeftHandShieldTarget";
        public const string ChestAimTargetName = "ChestAimTarget";
        public const string PhysicalResponseTargetName = "PhysicalResponseTarget";
        public const string LeftFootGroundTargetName = "LeftFootGroundTarget";
        public const string RightFootGroundTargetName = "RightFootGroundTarget";
        public const string RightElbowHintTargetName = "RightElbowHintTarget";
        public const string LeftElbowHintTargetName = "LeftElbowHintTarget";
        public const string PelvisTargetName = "PelvisTarget";
        public const string LeftKneeHintTargetName = "LeftKneeHintTarget";
        public const string RightKneeHintTargetName = "RightKneeHintTarget";
        public const string RightHandWeaponIkConstraintName = "RightHandWeaponIK";
        public const string LeftHandShieldIkConstraintName = "LeftHandShieldIK";
        public const string ChestAimConstraintName = "ChestAim";
        public const string LeftFootIkConstraintName = "LeftFootIK";
        public const string RightFootIkConstraintName = "RightFootIK";
        public const string PelvisPositionConstraintName = "PelvisPosition";

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerLocomotionAnimator locomotionAnimator;
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private PlayerDefenseController defenseController;
        [SerializeField] private CombatantHealth health;
        [SerializeField] private PlayerHeldWeaponVisualController heldWeaponVisual;
        [SerializeField] private PlayerRangedHandPoseController rangedHandPose;
        [SerializeField] private PlayerShieldGuardPoseController shieldGuardPose;
        [SerializeField] private PlayerAnimationProfileController animationProfileController;
        [SerializeField] private PlayerFootPlacementController footPlacement;
        [SerializeField] private Rig baseLocomotionRig;
        [SerializeField] private Rig fullBodyActionRig;
        [SerializeField] private Rig upperBodyCombatRig;
        [SerializeField] private Rig additivePhysicalResponseRig;
        [SerializeField] private TwoBoneIKConstraint rightHandWeaponIkConstraint;
        [SerializeField] private TwoBoneIKConstraint leftHandShieldIkConstraint;
        [SerializeField] private TwoBoneIKConstraint leftFootIkConstraint;
        [SerializeField] private TwoBoneIKConstraint rightFootIkConstraint;
        [SerializeField] private MultiPositionConstraint pelvisPositionConstraint;
        [SerializeField] private MultiAimConstraint chestAimConstraint;
        [SerializeField] private Transform rightHandWeaponTarget;
        [SerializeField] private Transform leftHandShieldTarget;
        [SerializeField] private Transform chestAimTarget;
        [SerializeField] private Transform physicalResponseTarget;
        [SerializeField] private Transform leftFootGroundTarget;
        [SerializeField] private Transform rightFootGroundTarget;
        [SerializeField] private Transform pelvisTarget;
        [SerializeField] private float rigBlendSpeed = DefaultRigBlendSpeed;
        [SerializeField] private float impulseDecaySpeed = DefaultImpulseDecaySpeed;
        [SerializeField] private float leanBlendSpeed = DefaultLeanBlendSpeed;
        [SerializeField] private float leanSpeedReferenceMetersPerSecond = DefaultLeanSpeedReferenceMetersPerSecond;
        [SerializeField] private float footYawAimInfluenceMaxDegrees = DefaultFootYawAimInfluenceMaxDegrees;
        [SerializeField] private float hitReactionFootIkSuppressSeconds = DefaultHitReactionFootIkSuppressSeconds;

        private PlayerWeaponController subscribedWeaponController;
        private CombatantHealth subscribedHealth;
        private bool hasPreviousPosition;
        private Vector3 previousWorldPosition;
        private float baseRigWeight = 1f;
        private float fullBodyActionRigWeight;
        private float upperBodyCombatRigWeight;
        private float additivePhysicalResponseRigWeight;
        private float footIkWeight;
        private float recoilImpulse;
        private float flinchImpulse;
        private float bodyLean01;
        private float footIkSuppressedUntilTime;

        public PlayerAnimationLocomotionMode CurrentLocomotionMode { get; private set; } = PlayerAnimationLocomotionMode.Idle;

        public PlayerAnimationUpperBodyPose CurrentUpperBodyPose { get; private set; } = PlayerAnimationUpperBodyPose.None;

        public PlayerAnimationActionPhase CurrentActionPhase { get; private set; } = PlayerAnimationActionPhase.None;

        public float BaseRigWeight => baseRigWeight;

        public float FullBodyActionRigWeight => fullBodyActionRigWeight;

        public float UpperBodyCombatRigWeight => upperBodyCombatRigWeight;

        public float AdditivePhysicalResponseRigWeight => additivePhysicalResponseRigWeight;

        public float FootIkWeight => footIkWeight;

        public float RightHandWeaponIkWeight => rightHandWeaponIkConstraint != null ? rightHandWeaponIkConstraint.weight : 0f;

        public float LeftHandShieldIkWeight => leftHandShieldIkConstraint != null ? leftHandShieldIkConstraint.weight : 0f;

        public float ChestAimWeight => chestAimConstraint != null ? chestAimConstraint.weight : 0f;

        public float LeftFootLockWeight => footPlacement != null ? footPlacement.LeftFootLockWeight : 0f;

        public float RightFootLockWeight => footPlacement != null ? footPlacement.RightFootLockWeight : 0f;

        public float PelvisOffset => footPlacement != null ? footPlacement.PelvisOffset : 0f;

        public float AimBodyAngleDegrees => locomotionAnimator != null ? locomotionAnimator.AimBodyAngleDegrees : 0f;

        public bool IsTurnInPlaceActive => locomotionAnimator != null && locomotionAnimator.IsTurnInPlaceActive;

        public PlayerAnimationProfileId CurrentProfileId => animationProfileController != null
            ? animationProfileController.CurrentProfileId
            : PlayerAnimationProfileId.UnarmedLocomotion;

        public bool AllowsShieldGuard => animationProfileController != null && animationProfileController.AllowsShieldGuard;

        public float BodyLean01 => bodyLean01;

        public float PhysicalImpulse01 => Mathf.Clamp01(Mathf.Max(recoilImpulse, flinchImpulse, bodyLean01 * 0.35f));

        public Transform RightHandWeaponTarget => rightHandWeaponTarget;

        public Transform LeftHandShieldTarget => leftHandShieldTarget;

        public Transform ChestAimTarget => chestAimTarget;

        public Transform PhysicalResponseTarget => physicalResponseTarget;

        public Transform LeftFootGroundTarget => leftFootGroundTarget;

        public Transform RightFootGroundTarget => rightFootGroundTarget;

        public Transform PelvisTarget => pelvisTarget;

        private void Awake()
        {
            ResolveReferences();
            SnapPreviousPosition();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SnapPreviousPosition();
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
            rigBlendSpeed = Mathf.Max(0f, rigBlendSpeed);
            impulseDecaySpeed = Mathf.Max(0f, impulseDecaySpeed);
            leanBlendSpeed = Mathf.Max(0f, leanBlendSpeed);
            leanSpeedReferenceMetersPerSecond = Mathf.Max(0.01f, leanSpeedReferenceMetersPerSecond);
            footYawAimInfluenceMaxDegrees = Mathf.Clamp(footYawAimInfluenceMaxDegrees, 0f, 60f);
            hitReactionFootIkSuppressSeconds = Mathf.Max(0f, hitReactionFootIkSuppressSeconds);
        }

        private void LateUpdate()
        {
            SamplePose(Time.deltaTime);
        }

        public void Bind(
            Animator nextAnimator,
            PlayerLocomotionAnimator nextLocomotionAnimator,
            PlayerWeaponController nextWeaponController,
            PlayerDefenseController nextDefenseController,
            CombatantHealth nextHealth,
            PlayerHeldWeaponVisualController nextHeldWeaponVisual,
            PlayerRangedHandPoseController nextRangedHandPose,
            PlayerShieldGuardPoseController nextShieldGuardPose)
        {
            animator = nextAnimator;
            locomotionAnimator = nextLocomotionAnimator;
            weaponController = nextWeaponController;
            defenseController = nextDefenseController;
            health = nextHealth;
            heldWeaponVisual = nextHeldWeaponVisual;
            rangedHandPose = nextRangedHandPose;
            shieldGuardPose = nextShieldGuardPose;
            ResolveReferences();
        }

        public void BindRigs(
            Rig nextBaseLocomotionRig,
            Rig nextFullBodyActionRig,
            Rig nextUpperBodyCombatRig,
            Rig nextAdditivePhysicalResponseRig)
        {
            baseLocomotionRig = nextBaseLocomotionRig;
            fullBodyActionRig = nextFullBodyActionRig;
            upperBodyCombatRig = nextUpperBodyCombatRig;
            additivePhysicalResponseRig = nextAdditivePhysicalResponseRig;
            ApplyRigWeights();
        }

        public void BindRigConstraints(
            TwoBoneIKConstraint nextRightHandWeaponIkConstraint,
            TwoBoneIKConstraint nextLeftHandShieldIkConstraint,
            MultiAimConstraint nextChestAimConstraint)
        {
            rightHandWeaponIkConstraint = nextRightHandWeaponIkConstraint;
            leftHandShieldIkConstraint = nextLeftHandShieldIkConstraint;
            chestAimConstraint = nextChestAimConstraint;
            ApplyRigWeights();
        }

        public void BindFootPlacement(
            PlayerFootPlacementController nextFootPlacement,
            TwoBoneIKConstraint nextLeftFootIkConstraint,
            TwoBoneIKConstraint nextRightFootIkConstraint,
            MultiPositionConstraint nextPelvisPositionConstraint,
            Transform nextPelvisTarget)
        {
            footPlacement = nextFootPlacement;
            leftFootIkConstraint = nextLeftFootIkConstraint;
            rightFootIkConstraint = nextRightFootIkConstraint;
            pelvisPositionConstraint = nextPelvisPositionConstraint;
            pelvisTarget = nextPelvisTarget;
            RebindFootPlacement();
            ApplyRigWeights();
        }

        public void BindTargets(
            Transform nextRightHandWeaponTarget,
            Transform nextLeftHandShieldTarget,
            Transform nextChestAimTarget,
            Transform nextPhysicalResponseTarget,
            Transform nextLeftFootGroundTarget,
            Transform nextRightFootGroundTarget)
        {
            rightHandWeaponTarget = nextRightHandWeaponTarget;
            leftHandShieldTarget = nextLeftHandShieldTarget;
            chestAimTarget = nextChestAimTarget;
            physicalResponseTarget = nextPhysicalResponseTarget;
            leftFootGroundTarget = nextLeftFootGroundTarget;
            rightFootGroundTarget = nextRightFootGroundTarget;
            RebindFootPlacement();
            DriveRigTargets(0f);
        }

        public void Configure(
            float nextRigBlendSpeed,
            float nextImpulseDecaySpeed,
            float nextLeanBlendSpeed,
            float nextLeanSpeedReferenceMetersPerSecond,
            float nextFootYawAimInfluenceMaxDegrees = DefaultFootYawAimInfluenceMaxDegrees,
            float nextHitReactionFootIkSuppressSeconds = DefaultHitReactionFootIkSuppressSeconds)
        {
            rigBlendSpeed = Mathf.Max(0f, nextRigBlendSpeed);
            impulseDecaySpeed = Mathf.Max(0f, nextImpulseDecaySpeed);
            leanBlendSpeed = Mathf.Max(0f, nextLeanBlendSpeed);
            leanSpeedReferenceMetersPerSecond = Mathf.Max(0.01f, nextLeanSpeedReferenceMetersPerSecond);
            footYawAimInfluenceMaxDegrees = Mathf.Clamp(nextFootYawAimInfluenceMaxDegrees, 0f, 60f);
            hitReactionFootIkSuppressSeconds = Mathf.Max(0f, nextHitReactionFootIkSuppressSeconds);
        }

        public void SamplePose(float deltaTime)
        {
            ResolveReferences();
            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            UpdateModes(safeDeltaTime);
            UpdateRigWeights(safeDeltaTime);
            DriveRigTargets(safeDeltaTime);
            ApplyRigWeights();
        }

        public void NotifyShieldImpact()
        {
            flinchImpulse = Mathf.Max(flinchImpulse, 0.55f);
        }

        private void UpdateModes(float deltaTime)
        {
            var isDead = health != null && !health.IsAlive;
            if (isDead)
            {
                CurrentLocomotionMode = PlayerAnimationLocomotionMode.Dead;
            }
            else if (weaponController != null && weaponController.IsRolling)
            {
                CurrentLocomotionMode = PlayerAnimationLocomotionMode.Roll;
            }
            else if (locomotionAnimator != null && locomotionAnimator.IsTargetLockedForLocomotion)
            {
                CurrentLocomotionMode = PlayerAnimationLocomotionMode.Locked;
            }
            else if (locomotionAnimator != null && locomotionAnimator.IsMoving)
            {
                CurrentLocomotionMode = PlayerAnimationLocomotionMode.Moving;
            }
            else
            {
                CurrentLocomotionMode = PlayerAnimationLocomotionMode.Idle;
            }

            var isGuarding = defenseController != null &&
                defenseController.IsGuarding &&
                (animationProfileController == null || animationProfileController.AllowsShieldGuard);
            var isRangedPose = weaponController != null &&
                weaponController.ActiveWeaponSlot == WeaponSlot.Ranged &&
                (weaponController.IsRangedHeldAttackPoseActive || weaponController.IsRangedAttackCommitted);
            var isMeleeAttack = weaponController != null &&
                weaponController.ActiveWeaponSlot == WeaponSlot.Melee &&
                weaponController.IsAttackCommitted;

            CurrentUpperBodyPose = isGuarding
                ? PlayerAnimationUpperBodyPose.ShieldGuard
                : isRangedPose
                    ? PlayerAnimationUpperBodyPose.RangedAim
                    : isMeleeAttack
                        ? PlayerAnimationUpperBodyPose.MeleeAttack
                        : PlayerAnimationUpperBodyPose.None;

            CurrentActionPhase = isDead
                ? PlayerAnimationActionPhase.Dead
                : weaponController != null && weaponController.IsRolling
                    ? PlayerAnimationActionPhase.Roll
                    : isGuarding
                        ? PlayerAnimationActionPhase.Guard
                        : isRangedPose
                            ? PlayerAnimationActionPhase.RangedAttack
                            : isMeleeAttack
                                ? PlayerAnimationActionPhase.MeleeAttack
                                : PlayerAnimationActionPhase.None;

            UpdateLean(deltaTime);
            recoilImpulse = Mathf.MoveTowards(recoilImpulse, 0f, deltaTime * impulseDecaySpeed);
            flinchImpulse = Mathf.MoveTowards(flinchImpulse, 0f, deltaTime * impulseDecaySpeed);
        }

        private void UpdateLean(float deltaTime)
        {
            var desiredLean = 0f;
            if (hasPreviousPosition && deltaTime > 0f)
            {
                var delta = transform.position - previousWorldPosition;
                delta.y = 0f;
                desiredLean = Mathf.Clamp01(delta.magnitude / Mathf.Max(0.01f, leanSpeedReferenceMetersPerSecond * deltaTime));
            }

            previousWorldPosition = transform.position;
            hasPreviousPosition = true;
            bodyLean01 = leanBlendSpeed <= 0f
                ? desiredLean
                : Mathf.MoveTowards(bodyLean01, desiredLean, deltaTime * leanBlendSpeed);
        }

        private void UpdateRigWeights(float deltaTime)
        {
            var desiredFullBody = CurrentLocomotionMode is PlayerAnimationLocomotionMode.Roll or PlayerAnimationLocomotionMode.Dead ? 1f : 0f;
            var desiredUpperBody = CurrentUpperBodyPose == PlayerAnimationUpperBodyPose.None ? 0f : 1f;
            var desiredAdditive = PhysicalImpulse01;
            var desiredFootIk = ShouldAllowFootIk() &&
                footPlacement != null &&
                (animationProfileController == null || animationProfileController.UsesFootIk)
                    ? 1f
                    : 0f;
            baseRigWeight = 1f;
            fullBodyActionRigWeight = MoveWeight(fullBodyActionRigWeight, desiredFullBody, deltaTime);
            upperBodyCombatRigWeight = MoveWeight(upperBodyCombatRigWeight, desiredUpperBody, deltaTime);
            additivePhysicalResponseRigWeight = MoveWeight(additivePhysicalResponseRigWeight, desiredAdditive, deltaTime);
            footIkWeight = MoveWeight(footIkWeight, desiredFootIk, deltaTime);
        }

        private float MoveWeight(float current, float target, float deltaTime)
        {
            return rigBlendSpeed <= 0f
                ? Mathf.Clamp01(target)
                : Mathf.MoveTowards(current, Mathf.Clamp01(target), Mathf.Max(0f, deltaTime) * rigBlendSpeed);
        }

        private void DriveRigTargets(float deltaTime)
        {
            DriveRightHandTarget();
            DriveLeftHandTarget();
            DriveChestAimTarget();
            DrivePhysicalResponseTarget();
            DriveFootTargets(deltaTime);
        }

        private void DriveRightHandTarget()
        {
            if (rightHandWeaponTarget == null)
            {
                return;
            }

            if (CurrentUpperBodyPose == PlayerAnimationUpperBodyPose.RangedAim && rangedHandPose != null)
            {
                rightHandWeaponTarget.position = rangedHandPose.TargetWorldPosition;
                rightHandWeaponTarget.rotation = rangedHandPose.TargetWorldRotation;
                return;
            }

            var socket = heldWeaponVisual != null
                ? weaponController != null && weaponController.ActiveWeaponSlot == WeaponSlot.Melee
                    ? heldWeaponVisual.MeleeHandSocket
                    : heldWeaponVisual.RangedHandSocket
                : null;
            CopyTransform(socket, rightHandWeaponTarget);
        }

        private void DriveLeftHandTarget()
        {
            if (leftHandShieldTarget == null)
            {
                return;
            }

            if (CurrentUpperBodyPose == PlayerAnimationUpperBodyPose.ShieldGuard && shieldGuardPose != null)
            {
                leftHandShieldTarget.position = shieldGuardPose.TargetWorldPosition;
                leftHandShieldTarget.rotation = shieldGuardPose.TargetWorldRotation;
                return;
            }

            CopyTransform(heldWeaponVisual != null ? heldWeaponVisual.ShieldForearmSocket : null, leftHandShieldTarget);
        }

        private void DriveChestAimTarget()
        {
            if (chestAimTarget == null)
            {
                return;
            }

            var direction = Vector3.forward;
            if (CurrentUpperBodyPose == PlayerAnimationUpperBodyPose.ShieldGuard && defenseController != null)
            {
                direction = defenseController.GuardFacing;
            }
            else if (weaponController != null)
            {
                var aim = weaponController.HasVisualAimCommitment ? weaponController.VisualAimDirection : weaponController.LastAimDirection;
                direction = new Vector3(aim.x, 0f, aim.y);
            }

            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.forward;
            chestAimTarget.position = transform.position + direction * 2f + Vector3.up * 1.15f;
            chestAimTarget.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void DrivePhysicalResponseTarget()
        {
            if (physicalResponseTarget == null)
            {
                return;
            }

            var forward = chestAimTarget != null ? chestAimTarget.forward : transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.000001f ? forward.normalized : Vector3.forward;
            var response = PhysicalImpulse01;
            physicalResponseTarget.position = transform.position -
                forward * 0.08f * response +
                Vector3.up * (0.95f + 0.04f * response);
            physicalResponseTarget.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void DriveFootTargets(float deltaTime)
        {
            if (footPlacement != null)
            {
                footPlacement.SamplePlacement(
                    deltaTime,
                    ShouldAllowFootIk(),
                    locomotionAnimator != null ? locomotionAnimator.FacingDirectionWorld : transform.forward,
                    ResolveAimDirectionWorld(),
                    locomotionAnimator != null ? locomotionAnimator.RelativeMove : Vector2.zero,
                    footYawAimInfluenceMaxDegrees);
                return;
            }

            if (leftFootGroundTarget != null)
            {
                leftFootGroundTarget.position = transform.position + transform.TransformDirection(new Vector3(-0.12f, 0f, 0.02f));
                leftFootGroundTarget.rotation = transform.rotation;
            }

            if (rightFootGroundTarget != null)
            {
                rightFootGroundTarget.position = transform.position + transform.TransformDirection(new Vector3(0.12f, 0f, 0.02f));
                rightFootGroundTarget.rotation = transform.rotation;
            }
        }

        private void ApplyRigWeights()
        {
            if (baseLocomotionRig != null)
            {
                baseLocomotionRig.weight = baseRigWeight;
            }

            if (fullBodyActionRig != null)
            {
                fullBodyActionRig.weight = fullBodyActionRigWeight;
            }

            if (upperBodyCombatRig != null)
            {
                upperBodyCombatRig.weight = upperBodyCombatRigWeight;
            }

            if (additivePhysicalResponseRig != null)
            {
                additivePhysicalResponseRig.weight = additivePhysicalResponseRigWeight;
            }

            if (rightHandWeaponIkConstraint != null)
            {
                rightHandWeaponIkConstraint.weight = IsConstraintUsable(rightHandWeaponIkConstraint)
                    ? CurrentUpperBodyPose switch
                    {
                        PlayerAnimationUpperBodyPose.RangedAim => upperBodyCombatRigWeight,
                        PlayerAnimationUpperBodyPose.MeleeAttack => upperBodyCombatRigWeight * 0.65f,
                        _ => 0f
                    }
                    : 0f;
            }

            if (leftHandShieldIkConstraint != null)
            {
                leftHandShieldIkConstraint.weight = IsConstraintUsable(leftHandShieldIkConstraint) &&
                    CurrentUpperBodyPose == PlayerAnimationUpperBodyPose.ShieldGuard
                    ? upperBodyCombatRigWeight
                    : 0f;
            }

            if (chestAimConstraint != null)
            {
                chestAimConstraint.weight = IsConstraintUsable(chestAimConstraint)
                    ? CurrentUpperBodyPose switch
                    {
                        PlayerAnimationUpperBodyPose.RangedAim => upperBodyCombatRigWeight * 0.45f,
                        PlayerAnimationUpperBodyPose.ShieldGuard => upperBodyCombatRigWeight * 0.35f,
                        PlayerAnimationUpperBodyPose.MeleeAttack => upperBodyCombatRigWeight * 0.18f,
                        _ => 0f
                    }
                    : 0f;
            }

            if (footPlacement != null &&
                IsConstraintUsable(leftFootIkConstraint) &&
                IsConstraintUsable(rightFootIkConstraint) &&
                IsConstraintUsable(pelvisPositionConstraint))
            {
                footPlacement.ApplyConstraintWeights(footIkWeight);
            }
            else
            {
                if (leftFootIkConstraint != null)
                {
                    leftFootIkConstraint.weight = 0f;
                }

                if (rightFootIkConstraint != null)
                {
                    rightFootIkConstraint.weight = 0f;
                }

                if (pelvisPositionConstraint != null)
                {
                    pelvisPositionConstraint.weight = 0f;
                }
            }
        }

        private void HandleWeaponAttackVisualRequested(WeaponSlot slot, AttackKind kind, Vector2 direction)
        {
            recoilImpulse = Mathf.Max(recoilImpulse, kind == AttackKind.Heavy ? 1f : 0.65f);
        }

        private void HandleDamaged(CombatantHealth damagedHealth)
        {
            flinchImpulse = Mathf.Max(flinchImpulse, 1f);
            footIkSuppressedUntilTime = Mathf.Max(
                footIkSuppressedUntilTime,
                Time.time + hitReactionFootIkSuppressSeconds);
        }

        private void ResolveReferences()
        {
            animator ??= GetComponentInChildren<Animator>(includeInactive: true);
            RepairRigHierarchy();
            locomotionAnimator ??= GetComponent<PlayerLocomotionAnimator>();
            weaponController ??= GetComponent<PlayerWeaponController>();
            defenseController ??= GetComponent<PlayerDefenseController>();
            health ??= GetComponent<CombatantHealth>();
            heldWeaponVisual ??= GetComponent<PlayerHeldWeaponVisualController>();
            rangedHandPose ??= GetComponent<PlayerRangedHandPoseController>();
            shieldGuardPose ??= GetComponent<PlayerShieldGuardPoseController>();
            animationProfileController ??= GetComponent<PlayerAnimationProfileController>();
            footPlacement ??= GetComponent<PlayerFootPlacementController>();
            DisableInvalidRigConstraints();
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
                subscribedWeaponController.WeaponAttackVisualRequested += HandleWeaponAttackVisualRequested;
            }
        }

        private void UnbindWeaponEvents()
        {
            if (subscribedWeaponController == null)
            {
                return;
            }

            subscribedWeaponController.WeaponAttackVisualRequested -= HandleWeaponAttackVisualRequested;
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
                subscribedHealth.Damaged += HandleDamaged;
            }
        }

        private void UnbindHealthEvents()
        {
            if (subscribedHealth == null)
            {
                return;
            }

            subscribedHealth.Damaged -= HandleDamaged;
            subscribedHealth = null;
        }

        private void SnapPreviousPosition()
        {
            previousWorldPosition = transform.position;
            hasPreviousPosition = true;
        }

        private static void CopyTransform(Transform source, Transform destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            destination.position = source.position;
            destination.rotation = source.rotation;
        }

        private void RepairRigHierarchy()
        {
            if (animator == null)
            {
                return;
            }

            var rigRoot = FindDescendant(transform, ModernAnimationRigRootName);
            if (rigRoot == null || IsDescendantOf(rigRoot, animator.transform))
            {
                return;
            }

            rigRoot.SetParent(animator.transform, false);
        }

        private void DisableInvalidRigConstraints()
        {
            DisableIfInvalid(rightHandWeaponIkConstraint);
            DisableIfInvalid(leftHandShieldIkConstraint);
            DisableIfInvalid(leftFootIkConstraint);
            DisableIfInvalid(rightFootIkConstraint);
            DisableIfInvalid(chestAimConstraint);
            DisableIfInvalid(pelvisPositionConstraint);
        }

        private static void DisableIfInvalid(TwoBoneIKConstraint constraint)
        {
            if (constraint == null || IsConstraintUsable(constraint))
            {
                return;
            }

            constraint.weight = 0f;
            constraint.enabled = false;
        }

        private static void DisableIfInvalid(MultiAimConstraint constraint)
        {
            if (constraint == null || IsConstraintUsable(constraint))
            {
                return;
            }

            constraint.weight = 0f;
            constraint.enabled = false;
        }

        private static void DisableIfInvalid(MultiPositionConstraint constraint)
        {
            if (constraint == null || IsConstraintUsable(constraint))
            {
                return;
            }

            constraint.weight = 0f;
            constraint.enabled = false;
        }

        private static bool IsConstraintUsable(TwoBoneIKConstraint constraint)
        {
            return constraint != null &&
                constraint.enabled &&
                constraint.data.root != null &&
                constraint.data.mid != null &&
                constraint.data.tip != null &&
                constraint.data.target != null;
        }

        private static bool IsConstraintUsable(MultiAimConstraint constraint)
        {
            return constraint != null &&
                constraint.enabled &&
                constraint.data.constrainedObject != null &&
                constraint.data.sourceObjects.Count > 0 &&
                constraint.data.sourceObjects[0].transform != null;
        }

        private static bool IsConstraintUsable(MultiPositionConstraint constraint)
        {
            return constraint != null &&
                constraint.enabled &&
                constraint.data.constrainedObject != null &&
                constraint.data.sourceObjects.Count > 0 &&
                constraint.data.sourceObjects[0].transform != null;
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static bool IsDescendantOf(Transform child, Transform ancestor)
        {
            var cursor = child;
            while (cursor != null)
            {
                if (cursor == ancestor)
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private void RebindFootPlacement()
        {
            if (footPlacement == null)
            {
                return;
            }

            footPlacement.Bind(
                animator,
                locomotionAnimator,
                weaponController,
                health,
                leftFootGroundTarget,
                rightFootGroundTarget,
                pelvisTarget);
            footPlacement.BindConstraints(leftFootIkConstraint, rightFootIkConstraint, pelvisPositionConstraint);
        }

        private bool ShouldAllowFootIk()
        {
            return CurrentActionPhase is not PlayerAnimationActionPhase.Roll and not PlayerAnimationActionPhase.Dead &&
                Time.time >= footIkSuppressedUntilTime;
        }

        private Vector3 ResolveAimDirectionWorld()
        {
            var direction = Vector3.forward;
            if (CurrentUpperBodyPose == PlayerAnimationUpperBodyPose.ShieldGuard && defenseController != null)
            {
                direction = defenseController.GuardFacing;
            }
            else if (weaponController != null)
            {
                var aim = weaponController.HasVisualAimCommitment ? weaponController.VisualAimDirection : weaponController.LastAimDirection;
                direction = new Vector3(aim.x, 0f, aim.y);
            }
            else if (locomotionAnimator != null)
            {
                direction = locomotionAnimator.AimFacingDirectionWorld;
            }

            direction.y = 0f;
            return direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.forward;
        }
    }
}
