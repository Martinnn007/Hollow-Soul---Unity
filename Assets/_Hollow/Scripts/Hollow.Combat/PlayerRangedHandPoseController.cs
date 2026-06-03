using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Combat
{
    [DefaultExecutionOrder(60)]
    public sealed class PlayerRangedHandPoseController : MonoBehaviour
    {
        public const float DefaultBlendSpeed = 8f;
        public const float DefaultPositionWeight = 1f;
        public const float DefaultRotationWeight = 0.85f;
        public const float DefaultHandHeightMeters = 1.08f;
        public const float DefaultForwardOffsetMeters = 0.48f;
        public const float DefaultSideOffsetMeters = 0.24f;
        public const float LightRecoilDistanceMeters = 0.045f;
        public const float HeavyRecoilDistanceMeters = 0.075f;
        public const float LightRecoilPitchDegrees = 5f;
        public const float HeavyRecoilPitchDegrees = 8f;
        public const float LightRecoilSeconds = 0.14f;
        public const float HeavyRecoilSeconds = 0.22f;

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private PlayerHeldWeaponVisualController heldWeaponVisual;
        [SerializeField] private float blendSpeed = DefaultBlendSpeed;
        [SerializeField] private float positionWeight = DefaultPositionWeight;
        [SerializeField] private float rotationWeight = DefaultRotationWeight;
        [SerializeField] private float handHeightMeters = DefaultHandHeightMeters;
        [SerializeField] private float forwardOffsetMeters = DefaultForwardOffsetMeters;
        [SerializeField] private float sideOffsetMeters = DefaultSideOffsetMeters;

        private float currentBlend;
        private Vector3 targetWorldPosition;
        private Quaternion targetWorldRotation = Quaternion.identity;
        private PlayerRangedHandPoseIkRelay ikRelay;
        private PlayerWeaponController subscribedWeaponController;
        private AttackKind recoilAttackKind = AttackKind.Light;
        private Vector2 recoilDirection = Vector2.up;
        private float recoilAgeSeconds = float.PositiveInfinity;
        private float recoilDurationSeconds = LightRecoilSeconds;
        private int lastIkFrame = -1;

        public float CurrentBlend01 => currentBlend;

        public Vector3 TargetWorldPosition => targetWorldPosition;

        public Quaternion TargetWorldRotation => targetWorldRotation;

        public bool IsRangedPoseActive => ShouldUseRangedPose();

        public bool IsIkEligible => CanUseHumanoidIk();

        private void Awake()
        {
            ResolveReferences();
            RefreshTargetPose();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshTargetPose();
        }

        private void OnDisable()
        {
            UnbindWeaponEvents();
        }

        private void OnDestroy()
        {
            UnbindWeaponEvents();
        }

        private void OnValidate()
        {
            blendSpeed = Mathf.Max(0f, blendSpeed);
            positionWeight = Mathf.Clamp01(positionWeight);
            rotationWeight = Mathf.Clamp01(rotationWeight);
            handHeightMeters = Mathf.Max(0.1f, handHeightMeters);
            forwardOffsetMeters = Mathf.Max(0f, forwardOffsetMeters);
        }

        private void Update()
        {
            SamplePose(Time.deltaTime);
        }

        private void LateUpdate()
        {
            ApplyDirectHandPoseFallback();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            ApplyAnimatorIK(layerIndex);
        }

        internal void ApplyAnimatorIK(int layerIndex)
        {
            if (!CanUseHumanoidIk() || currentBlend <= 0.0001f)
            {
                return;
            }

            lastIkFrame = Time.frameCount;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, positionWeight * currentBlend);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationWeight * currentBlend);
            animator.SetIKPosition(AvatarIKGoal.RightHand, targetWorldPosition);
            animator.SetIKRotation(AvatarIKGoal.RightHand, targetWorldRotation);
        }

        public void Bind(
            Animator nextAnimator,
            PlayerWeaponController nextWeaponController,
            PlayerHeldWeaponVisualController nextHeldWeaponVisual)
        {
            animator = nextAnimator;
            weaponController = nextWeaponController;
            heldWeaponVisual = nextHeldWeaponVisual;
            ResolveReferences();
            RefreshTargetPose();
        }

        public void Configure(
            float nextBlendSpeed,
            float nextPositionWeight,
            float nextRotationWeight,
            float nextHandHeightMeters,
            float nextForwardOffsetMeters,
            float nextSideOffsetMeters)
        {
            blendSpeed = Mathf.Max(0f, nextBlendSpeed);
            positionWeight = Mathf.Clamp01(nextPositionWeight);
            rotationWeight = Mathf.Clamp01(nextRotationWeight);
            handHeightMeters = Mathf.Max(0.1f, nextHandHeightMeters);
            forwardOffsetMeters = Mathf.Max(0f, nextForwardOffsetMeters);
            sideOffsetMeters = nextSideOffsetMeters;
        }

        public void SamplePose(float deltaTime)
        {
            ResolveReferences();
            TickRecoil(deltaTime);
            RefreshTargetPose();
            var desired = ShouldUseRangedPose() ? 1f : 0f;
            currentBlend = blendSpeed <= 0f
                ? desired
                : Mathf.MoveTowards(currentBlend, desired, Mathf.Max(0f, deltaTime) * blendSpeed);
        }

        private void ApplyDirectHandPoseFallback()
        {
            if (currentBlend <= 0.0001f)
            {
                return;
            }

            if (CanUseHumanoidIk() && lastIkFrame == Time.frameCount)
            {
                return;
            }

            var hand = ResolveRightHandTransform();
            if (hand == null)
            {
                hand = heldWeaponVisual != null ? heldWeaponVisual.RangedHandSocket : null;
            }

            if (hand == null)
            {
                return;
            }

            hand.position = Vector3.Lerp(hand.position, targetWorldPosition, positionWeight * currentBlend);
            hand.rotation = Quaternion.Slerp(hand.rotation, targetWorldRotation, rotationWeight * currentBlend);
        }

        private void RefreshTargetPose()
        {
            var direction = weaponController != null
                ? weaponController.HasVisualAimCommitment
                    ? weaponController.VisualAimDirection
                    : weaponController.LastAimDirection
                : Vector2.up;
            direction = PlayerWeaponVisualPosePolicy.SafeAim(direction);
            var forwardLocal = PlayerWeaponVisualPosePolicy.PlanarForward(direction);
            var rightLocal = new Vector3(direction.y, 0f, -direction.x).normalized;
            var space = transform.parent;
            var forwardWorld = space != null
                ? space.TransformDirection(forwardLocal)
                : transform.TransformDirection(forwardLocal);
            var rightWorld = space != null
                ? space.TransformDirection(rightLocal)
                : transform.TransformDirection(rightLocal);
            forwardWorld.y = 0f;
            rightWorld.y = 0f;
            forwardWorld = forwardWorld.sqrMagnitude > 0.000001f ? forwardWorld.normalized : Vector3.forward;
            rightWorld = rightWorld.sqrMagnitude > 0.000001f ? rightWorld.normalized : Vector3.right;
            targetWorldPosition = transform.position +
                forwardWorld * forwardOffsetMeters +
                rightWorld * sideOffsetMeters +
                Vector3.up * handHeightMeters;
            targetWorldRotation = Quaternion.LookRotation(forwardWorld, Vector3.up);
            ApplyRecoilOffset(forwardWorld, rightWorld);
        }

        private bool ShouldUseRangedPose()
        {
            return weaponController != null &&
                weaponController.ActiveWeaponSlot == WeaponSlot.Ranged &&
                (weaponController.IsRangedHeldAttackPoseActive || weaponController.IsRangedAttackCommitted);
        }

        private void HandleWeaponAttackVisualRequested(WeaponSlot slot, AttackKind kind, Vector2 direction)
        {
            if (slot != WeaponSlot.Ranged)
            {
                return;
            }

            recoilAttackKind = kind;
            recoilDirection = PlayerWeaponVisualPosePolicy.SafeAim(direction);
            recoilDurationSeconds = kind == AttackKind.Heavy ? HeavyRecoilSeconds : LightRecoilSeconds;
            recoilAgeSeconds = 0f;
        }

        private void TickRecoil(float deltaTime)
        {
            if (recoilAgeSeconds >= recoilDurationSeconds)
            {
                return;
            }

            recoilAgeSeconds = Mathf.Min(
                recoilDurationSeconds,
                recoilAgeSeconds + Mathf.Max(0f, deltaTime));
        }

        private void ApplyRecoilOffset(Vector3 forwardWorld, Vector3 rightWorld)
        {
            if (recoilAgeSeconds >= recoilDurationSeconds || recoilDurationSeconds <= 0f)
            {
                return;
            }

            var recoilForwardWorld = forwardWorld;
            if (recoilDirection.sqrMagnitude > 0.001f)
            {
                var recoilForwardLocal = PlayerWeaponVisualPosePolicy.PlanarForward(recoilDirection);
                var space = transform.parent;
                recoilForwardWorld = space != null
                    ? space.TransformDirection(recoilForwardLocal)
                    : transform.TransformDirection(recoilForwardLocal);
                recoilForwardWorld.y = 0f;
                recoilForwardWorld = recoilForwardWorld.sqrMagnitude > 0.000001f
                    ? recoilForwardWorld.normalized
                    : forwardWorld;
                rightWorld = new Vector3(recoilForwardWorld.z, 0f, -recoilForwardWorld.x);
                rightWorld = rightWorld.sqrMagnitude > 0.000001f ? rightWorld.normalized : Vector3.right;
            }

            var recoil01 = 1f - Mathf.Clamp01(recoilAgeSeconds / recoilDurationSeconds);
            recoil01 *= recoil01;
            var distance = recoilAttackKind == AttackKind.Heavy ? HeavyRecoilDistanceMeters : LightRecoilDistanceMeters;
            var pitch = recoilAttackKind == AttackKind.Heavy ? HeavyRecoilPitchDegrees : LightRecoilPitchDegrees;
            targetWorldPosition -= recoilForwardWorld * distance * recoil01;
            targetWorldPosition += Vector3.up * distance * 0.22f * recoil01;
            targetWorldRotation = Quaternion.AngleAxis(-pitch * recoil01, rightWorld) * targetWorldRotation;
        }

        private bool CanUseHumanoidIk()
        {
            return animator != null &&
                animator.avatar != null &&
                animator.avatar.isValid &&
                animator.avatar.isHuman &&
                animator.isHuman;
        }

        private Transform ResolveRightHandTransform()
        {
            if (animator != null && animator.isHuman)
            {
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand != null)
                {
                    return hand;
                }
            }

            return null;
        }

        private void ResolveReferences()
        {
            if (weaponController == null)
            {
                weaponController = GetComponent<PlayerWeaponController>();
            }

            if (heldWeaponVisual == null)
            {
                heldWeaponVisual = GetComponent<PlayerHeldWeaponVisualController>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(includeInactive: true);
            }

            if (animator != null && animator.gameObject != gameObject)
            {
                ikRelay = animator.GetComponent<PlayerRangedHandPoseIkRelay>();
                if (ikRelay == null && Application.isPlaying)
                {
                    ikRelay = animator.gameObject.AddComponent<PlayerRangedHandPoseIkRelay>();
                }

                if (ikRelay != null)
                {
                    ikRelay.Bind(this);
                }
            }

            BindWeaponEvents();
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
    }
}
