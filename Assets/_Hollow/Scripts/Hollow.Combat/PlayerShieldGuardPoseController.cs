using UnityEngine;

namespace Hollow.Combat
{
    [DefaultExecutionOrder(60)]
    public sealed class PlayerShieldGuardPoseController : MonoBehaviour
    {
        public const float DefaultBlendSpeed = 10f;
        public const float DefaultPositionWeight = 1f;
        public const float DefaultRotationWeight = 0.9f;
        public const float DefaultHandHeightMeters = 1.04f;
        public const float DefaultForwardOffsetMeters = 0.46f;
        public const float DefaultSideOffsetMeters = -0.22f;

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerDefenseController defenseController;
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
        private int lastIkFrame = -1;

        public float CurrentBlend01 => currentBlend;

        public Vector3 TargetWorldPosition => targetWorldPosition;

        public Quaternion TargetWorldRotation => targetWorldRotation;

        public bool IsShieldPoseActive => defenseController != null && defenseController.IsGuarding;

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
            ApplyDirectShieldPoseFallback();
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
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, positionWeight * currentBlend);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, rotationWeight * currentBlend);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, targetWorldPosition);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, targetWorldRotation);
        }

        public void Bind(
            Animator nextAnimator,
            PlayerDefenseController nextDefenseController,
            PlayerHeldWeaponVisualController nextHeldWeaponVisual)
        {
            animator = nextAnimator;
            defenseController = nextDefenseController;
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
            RefreshTargetPose();
            var desired = IsShieldPoseActive ? 1f : 0f;
            currentBlend = blendSpeed <= 0f
                ? desired
                : Mathf.MoveTowards(currentBlend, desired, Mathf.Max(0f, deltaTime) * blendSpeed);
        }

        private void ApplyDirectShieldPoseFallback()
        {
            if (currentBlend <= 0.0001f)
            {
                return;
            }

            if (CanUseHumanoidIk() && lastIkFrame == Time.frameCount)
            {
                return;
            }

            var target = ResolveLeftHandTransform();
            if (target == null)
            {
                target = heldWeaponVisual != null ? heldWeaponVisual.ShieldForearmSocket : null;
            }

            if (target == null)
            {
                return;
            }

            target.position = Vector3.Lerp(target.position, targetWorldPosition, positionWeight * currentBlend);
            target.rotation = Quaternion.Slerp(target.rotation, targetWorldRotation, rotationWeight * currentBlend);
        }

        private void RefreshTargetPose()
        {
            var guardFacing = defenseController != null ? defenseController.GuardFacing : Vector3.forward;
            guardFacing.y = 0f;
            if (guardFacing.sqrMagnitude < 0.000001f)
            {
                guardFacing = Vector3.forward;
            }

            guardFacing.Normalize();
            var space = transform.parent;
            var forwardWorld = space != null
                ? space.TransformDirection(guardFacing)
                : transform.TransformDirection(guardFacing);
            forwardWorld.y = 0f;
            forwardWorld = forwardWorld.sqrMagnitude > 0.000001f ? forwardWorld.normalized : Vector3.forward;
            var rightWorld = new Vector3(forwardWorld.z, 0f, -forwardWorld.x);
            rightWorld = rightWorld.sqrMagnitude > 0.000001f ? rightWorld.normalized : Vector3.right;

            targetWorldPosition = transform.position +
                forwardWorld * forwardOffsetMeters +
                rightWorld * sideOffsetMeters +
                Vector3.up * handHeightMeters;
            targetWorldRotation = Quaternion.LookRotation(forwardWorld, Vector3.up);
        }

        private bool CanUseHumanoidIk()
        {
            return animator != null &&
                animator.avatar != null &&
                animator.avatar.isValid &&
                animator.avatar.isHuman &&
                animator.isHuman;
        }

        private Transform ResolveLeftHandTransform()
        {
            if (animator != null && animator.isHuman)
            {
                var hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                if (hand != null)
                {
                    return hand;
                }
            }

            return null;
        }

        private void ResolveReferences()
        {
            if (defenseController == null)
            {
                defenseController = GetComponent<PlayerDefenseController>();
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
                    ikRelay.BindShield(this);
                }
            }
        }
    }
}
