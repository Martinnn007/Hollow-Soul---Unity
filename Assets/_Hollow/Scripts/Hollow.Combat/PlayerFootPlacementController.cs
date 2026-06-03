using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Hollow.Combat
{
    [DefaultExecutionOrder(75)]
    public sealed class PlayerFootPlacementController : MonoBehaviour
    {
        public const float DefaultStrideLengthMeters = 0.52f;
        public const float DefaultLockThresholdMetersPerSecond = 0.08f;
        public const float DefaultPelvisSmoothing = 10f;
        public const float DefaultFootHeightMeters = 0.025f;
        public const float DefaultRaycastDistanceMeters = 1.25f;
        public const float DefaultIkBlendSpeed = 10f;
        public const float DefaultYawBlend = 1f;
        public const float DefaultFootPlantHalfCycleSeconds = 0.28f;
        public const float DefaultFootSeparationMeters = 0.23f;
        private const float DefaultSwingForwardMeters = 0.18f;
        private const float DefaultPelvisDownLimitMeters = -0.18f;
        private const float DefaultPelvisUpLimitMeters = 0.06f;

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerLocomotionAnimator locomotionAnimator;
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private CombatantHealth health;
        [SerializeField] private PlayerAnimationPoseCoordinator poseCoordinator;
        [SerializeField] private Transform leftFootTarget;
        [SerializeField] private Transform rightFootTarget;
        [SerializeField] private Transform pelvisTarget;
        [SerializeField] private TwoBoneIKConstraint leftFootIkConstraint;
        [SerializeField] private TwoBoneIKConstraint rightFootIkConstraint;
        [SerializeField] private MultiPositionConstraint pelvisPositionConstraint;
        [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;
        [SerializeField] private float strideLengthMeters = DefaultStrideLengthMeters;
        [SerializeField] private float lockThresholdMetersPerSecond = DefaultLockThresholdMetersPerSecond;
        [SerializeField] private float pelvisSmoothing = DefaultPelvisSmoothing;
        [SerializeField] private float footHeightMeters = DefaultFootHeightMeters;
        [SerializeField] private float raycastDistanceMeters = DefaultRaycastDistanceMeters;
        [SerializeField] private float ikBlendSpeed = DefaultIkBlendSpeed;
        [SerializeField] private float yawBlend = DefaultYawBlend;
        [SerializeField] private float footPlantHalfCycleSeconds = DefaultFootPlantHalfCycleSeconds;

        private bool initialized;
        private bool wasLeftPlanting = true;
        private bool wasRightPlanting = true;
        private float stridePhase;
        private float coordinatorWeight = 1f;
        private float leftFootLockWeight;
        private float rightFootLockWeight;
        private float pelvisOffset;
        private Vector3 leftPlantPosition;
        private Vector3 rightPlantPosition;
        private Quaternion leftPlantRotation = Quaternion.identity;
        private Quaternion rightPlantRotation = Quaternion.identity;
        private int lastSampleFrame = -1;
        private bool isEligible;
        private bool isUsingGroundFallback;

        public float LeftFootLockWeight => leftFootLockWeight * coordinatorWeight;

        public float RightFootLockWeight => rightFootLockWeight * coordinatorWeight;

        public float PelvisOffset => pelvisOffset * coordinatorWeight;

        public bool IsFootIkEligible => isEligible;

        public bool IsUsingGroundFallback => isUsingGroundFallback;

        public Transform LeftFootTarget => leftFootTarget;

        public Transform RightFootTarget => rightFootTarget;

        public Transform PelvisTarget => pelvisTarget;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            strideLengthMeters = Mathf.Max(0.05f, strideLengthMeters);
            lockThresholdMetersPerSecond = Mathf.Max(0f, lockThresholdMetersPerSecond);
            pelvisSmoothing = Mathf.Max(0f, pelvisSmoothing);
            footHeightMeters = Mathf.Max(0f, footHeightMeters);
            raycastDistanceMeters = Mathf.Max(0.05f, raycastDistanceMeters);
            ikBlendSpeed = Mathf.Max(0f, ikBlendSpeed);
            yawBlend = Mathf.Clamp01(yawBlend);
            footPlantHalfCycleSeconds = Mathf.Max(0.05f, footPlantHalfCycleSeconds);
        }

        private void LateUpdate()
        {
            ResolveReferences();
            if (poseCoordinator != null)
            {
                return;
            }

            if (lastSampleFrame == Time.frameCount)
            {
                return;
            }

            var lowerBodyForward = locomotionAnimator != null ? locomotionAnimator.FacingDirectionWorld : transform.forward;
            var aimForward = locomotionAnimator != null ? locomotionAnimator.AimFacingDirectionWorld : lowerBodyForward;
            var relativeMove = locomotionAnimator != null ? locomotionAnimator.RelativeMove : Vector2.zero;
            SamplePlacement(Time.deltaTime, allowFootIk: true, lowerBodyForward, aimForward, relativeMove, 15f);
            ApplyConstraintWeights(1f);
        }

        public void Bind(
            Animator nextAnimator,
            PlayerLocomotionAnimator nextLocomotionAnimator,
            PlayerWeaponController nextWeaponController,
            CombatantHealth nextHealth,
            Transform nextLeftFootTarget,
            Transform nextRightFootTarget,
            Transform nextPelvisTarget)
        {
            animator = nextAnimator;
            locomotionAnimator = nextLocomotionAnimator;
            weaponController = nextWeaponController;
            health = nextHealth;
            leftFootTarget = nextLeftFootTarget;
            rightFootTarget = nextRightFootTarget;
            pelvisTarget = nextPelvisTarget;
            ResolveReferences();
        }

        public void BindConstraints(
            TwoBoneIKConstraint nextLeftFootIkConstraint,
            TwoBoneIKConstraint nextRightFootIkConstraint,
            MultiPositionConstraint nextPelvisPositionConstraint)
        {
            leftFootIkConstraint = nextLeftFootIkConstraint;
            rightFootIkConstraint = nextRightFootIkConstraint;
            pelvisPositionConstraint = nextPelvisPositionConstraint;
            ApplyConstraintWeights(coordinatorWeight);
        }

        public void Configure(
            float nextStrideLengthMeters,
            float nextLockThresholdMetersPerSecond,
            float nextPelvisSmoothing,
            float nextFootHeightMeters,
            float nextRaycastDistanceMeters,
            float nextIkBlendSpeed,
            float nextYawBlend,
            float nextFootPlantHalfCycleSeconds)
        {
            strideLengthMeters = Mathf.Max(0.05f, nextStrideLengthMeters);
            lockThresholdMetersPerSecond = Mathf.Max(0f, nextLockThresholdMetersPerSecond);
            pelvisSmoothing = Mathf.Max(0f, nextPelvisSmoothing);
            footHeightMeters = Mathf.Max(0f, nextFootHeightMeters);
            raycastDistanceMeters = Mathf.Max(0.05f, nextRaycastDistanceMeters);
            ikBlendSpeed = Mathf.Max(0f, nextIkBlendSpeed);
            yawBlend = Mathf.Clamp01(nextYawBlend);
            footPlantHalfCycleSeconds = Mathf.Max(0.05f, nextFootPlantHalfCycleSeconds);
        }

        public void SamplePlacement(
            float deltaTime,
            bool allowFootIk,
            Vector3 lowerBodyForwardWorld,
            Vector3 aimForwardWorld,
            Vector2 relativeMove,
            float maxAimYawInfluenceDegrees)
        {
            ResolveReferences();
            lastSampleFrame = Time.frameCount;
            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            var lowerForward = SafePlanarDirection(lowerBodyForwardWorld, transform.forward);
            var aimForward = SafePlanarDirection(aimForwardWorld, lowerForward);
            var travelDirection = ResolveTravelDirection(relativeMove, lowerForward);
            var speed = locomotionAnimator != null ? locomotionAnimator.PlanarSpeedMetersPerSecond : 0f;
            var isMoving = speed > lockThresholdMetersPerSecond || relativeMove.sqrMagnitude > 0.01f;
            var requestedEligible = allowFootIk &&
                leftFootTarget != null &&
                rightFootTarget != null &&
                pelvisTarget != null &&
                (health == null || health.IsAlive) &&
                (weaponController == null || !weaponController.IsRolling);

            if (!requestedEligible)
            {
                isEligible = false;
                isUsingGroundFallback = false;
                MoveWeights(0f, 0f, safeDeltaTime);
                MovePelvisOffset(0f, safeDeltaTime);
                return;
            }

            if (!initialized)
            {
                initialized = TryInitializePlants(lowerForward, aimForward, maxAimYawInfluenceDegrees);
            }

            var leftPlanting = !isMoving || stridePhase < 0.5f;
            var rightPlanting = !isMoving || stridePhase >= 0.5f;
            if (isMoving && safeDeltaTime > 0f)
            {
                stridePhase = Mathf.Repeat(
                    stridePhase + safeDeltaTime / footPlantHalfCycleSeconds * 0.5f,
                    1f);
                leftPlanting = stridePhase < 0.5f;
                rightPlanting = stridePhase >= 0.5f;
            }

            var footRotation = ResolveFootRotation(travelDirection, lowerForward, aimForward, maxAimYawInfluenceDegrees);
            var leftBase = BaseFootPosition(-DefaultFootSeparationMeters * 0.5f, lowerForward);
            var rightBase = BaseFootPosition(DefaultFootSeparationMeters * 0.5f, lowerForward);
            var leftDesired = leftBase + (isMoving && leftPlanting ? travelDirection * DefaultSwingForwardMeters : Vector3.zero);
            var rightDesired = rightBase + (isMoving && rightPlanting ? travelDirection * DefaultSwingForwardMeters : Vector3.zero);

            isUsingGroundFallback = false;
            if (leftPlanting && (!wasLeftPlanting || !initialized))
            {
                initialized &= TryUpdatePlant(leftDesired, footRotation, ref leftPlantPosition, ref leftPlantRotation);
            }

            if (rightPlanting && (!wasRightPlanting || !initialized))
            {
                initialized &= TryUpdatePlant(rightDesired, footRotation, ref rightPlantPosition, ref rightPlantRotation);
            }

            if (!initialized)
            {
                isEligible = false;
                MoveWeights(0f, 0f, safeDeltaTime);
                MovePelvisOffset(0f, safeDeltaTime);
                return;
            }

            isEligible = true;
            ApplyFootTarget(leftFootTarget, leftPlanting ? leftPlantPosition : leftDesired, leftPlanting ? leftPlantRotation : footRotation, safeDeltaTime);
            ApplyFootTarget(rightFootTarget, rightPlanting ? rightPlantPosition : rightDesired, rightPlanting ? rightPlantRotation : footRotation, safeDeltaTime);
            MoveWeights(leftPlanting ? 1f : 0.18f, rightPlanting ? 1f : 0.18f, safeDeltaTime);
            UpdatePelvisTarget(safeDeltaTime);
            wasLeftPlanting = leftPlanting;
            wasRightPlanting = rightPlanting;
        }

        public void ApplyConstraintWeights(float nextCoordinatorWeight)
        {
            coordinatorWeight = Mathf.Clamp01(nextCoordinatorWeight);
            if (leftFootIkConstraint != null)
            {
                leftFootIkConstraint.weight = LeftFootLockWeight;
            }

            if (rightFootIkConstraint != null)
            {
                rightFootIkConstraint.weight = RightFootLockWeight;
            }

            if (pelvisPositionConstraint != null)
            {
                pelvisPositionConstraint.weight = Mathf.Clamp01(Mathf.Max(LeftFootLockWeight, RightFootLockWeight) * 0.55f);
            }
        }

        private bool TryInitializePlants(Vector3 lowerForward, Vector3 aimForward, float maxAimYawInfluenceDegrees)
        {
            var footRotation = ResolveFootRotation(lowerForward, lowerForward, aimForward, maxAimYawInfluenceDegrees);
            return TryUpdatePlant(BaseFootPosition(-DefaultFootSeparationMeters * 0.5f, lowerForward), footRotation, ref leftPlantPosition, ref leftPlantRotation) &&
                TryUpdatePlant(BaseFootPosition(DefaultFootSeparationMeters * 0.5f, lowerForward), footRotation, ref rightPlantPosition, ref rightPlantRotation);
        }

        private Vector3 BaseFootPosition(float sideOffsetMeters, Vector3 lowerForward)
        {
            var right = new Vector3(lowerForward.z, 0f, -lowerForward.x);
            right = right.sqrMagnitude > 0.000001f ? right.normalized : transform.right;
            return transform.position + right * sideOffsetMeters;
        }

        private bool TryUpdatePlant(Vector3 desiredWorldPosition, Quaternion desiredWorldRotation, ref Vector3 plantPosition, ref Quaternion plantRotation)
        {
            if (!TryProjectToGround(desiredWorldPosition, out var grounded))
            {
                isUsingGroundFallback = true;
                return false;
            }

            plantPosition = grounded;
            plantRotation = desiredWorldRotation;
            return true;
        }

        private bool TryProjectToGround(Vector3 desiredWorldPosition, out Vector3 groundedPosition)
        {
            var origin = desiredWorldPosition + Vector3.up * (raycastDistanceMeters * 0.5f);
            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out var hit,
                    raycastDistanceMeters,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                groundedPosition = hit.point + Vector3.up * footHeightMeters;
                return true;
            }

            groundedPosition = desiredWorldPosition;
            return false;
        }

        private void ApplyFootTarget(Transform target, Vector3 worldPosition, Quaternion worldRotation, float deltaTime)
        {
            if (target == null)
            {
                return;
            }

            var positionBlend = ikBlendSpeed <= 0f ? 1f : Mathf.Clamp01(deltaTime * ikBlendSpeed);
            target.position = Vector3.Lerp(target.position, worldPosition, positionBlend);
            target.rotation = Quaternion.Slerp(target.rotation, worldRotation, positionBlend);
        }

        private void MoveWeights(float leftDesired, float rightDesired, float deltaTime)
        {
            leftFootLockWeight = MoveWeight(leftFootLockWeight, leftDesired, deltaTime);
            rightFootLockWeight = MoveWeight(rightFootLockWeight, rightDesired, deltaTime);
        }

        private float MoveWeight(float current, float desired, float deltaTime)
        {
            return ikBlendSpeed <= 0f
                ? Mathf.Clamp01(desired)
                : Mathf.MoveTowards(current, Mathf.Clamp01(desired), deltaTime * ikBlendSpeed);
        }

        private void UpdatePelvisTarget(float deltaTime)
        {
            if (pelvisTarget == null || leftFootTarget == null || rightFootTarget == null)
            {
                return;
            }

            var lowestFoot = Mathf.Min(leftFootTarget.position.y, rightFootTarget.position.y);
            var desiredOffset = Mathf.Clamp(
                lowestFoot - transform.position.y,
                DefaultPelvisDownLimitMeters,
                DefaultPelvisUpLimitMeters);
            MovePelvisOffset(desiredOffset, deltaTime);
            pelvisTarget.position = transform.position + Vector3.up * pelvisOffset;
            pelvisTarget.rotation = transform.rotation;
        }

        private void MovePelvisOffset(float desiredOffset, float deltaTime)
        {
            pelvisOffset = pelvisSmoothing <= 0f
                ? desiredOffset
                : Mathf.MoveTowards(pelvisOffset, desiredOffset, deltaTime * pelvisSmoothing);
        }

        private Quaternion ResolveFootRotation(
            Vector3 travelDirection,
            Vector3 lowerForward,
            Vector3 aimForward,
            float maxAimYawInfluenceDegrees)
        {
            var baseForward = SafePlanarDirection(travelDirection, lowerForward);
            var signedAimOffset = Vector3.SignedAngle(baseForward, aimForward, Vector3.up);
            var clampedAimOffset = Mathf.Clamp(
                signedAimOffset,
                -Mathf.Max(0f, maxAimYawInfluenceDegrees),
                Mathf.Max(0f, maxAimYawInfluenceDegrees)) * yawBlend;
            var finalForward = Quaternion.AngleAxis(clampedAimOffset, Vector3.up) * baseForward;
            finalForward = SafePlanarDirection(finalForward, baseForward);
            return Quaternion.LookRotation(finalForward, Vector3.up);
        }

        private Vector3 ResolveTravelDirection(Vector2 relativeMove, Vector3 lowerForward)
        {
            lowerForward = SafePlanarDirection(lowerForward, transform.forward);
            var right = new Vector3(lowerForward.z, 0f, -lowerForward.x);
            right = right.sqrMagnitude > 0.000001f ? right.normalized : transform.right;
            var direction = right * relativeMove.x + lowerForward * relativeMove.y;
            return SafePlanarDirection(direction, lowerForward);
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

        private void ResolveReferences()
        {
            animator ??= GetComponentInChildren<Animator>(includeInactive: true);
            locomotionAnimator ??= GetComponent<PlayerLocomotionAnimator>();
            weaponController ??= GetComponent<PlayerWeaponController>();
            health ??= GetComponent<CombatantHealth>();
            poseCoordinator ??= GetComponent<PlayerAnimationPoseCoordinator>();
        }
    }
}
