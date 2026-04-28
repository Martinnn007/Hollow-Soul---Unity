using Hollow.Platform;
using UnityEngine;

namespace Hollow.Presentation
{
    public sealed class GameplayCameraFollowController : MonoBehaviour
    {
        private const float MinimumSharpness = 0.01f;
        public const float DefaultLookAheadSeconds = 0.12f;
        public const float DefaultMaxLookAheadMeters = 1.1f;
        public const float DefaultSnapDistanceMeters = 7.5f;

        [SerializeField] private Transform target;
        [SerializeField] private float followSharpness = 8f;
        [SerializeField] private float lookAheadSeconds = DefaultLookAheadSeconds;
        [SerializeField] private float maxLookAheadMeters = DefaultMaxLookAheadMeters;
        [SerializeField] private float snapDistanceMeters = DefaultSnapDistanceMeters;

        private Vector3 baselineRigPosition;
        private Vector3 lastTargetPosition;
        private Vector3 planarLookAhead;
        private bool hasBaseline;
        private bool hasLastTargetPosition;

        public Transform Target => target;

        public Vector3 DesiredRigPosition => CalculateDesiredRigPosition();

        public void Configure(Transform nextTarget, HollowPlatformKind platformKind, bool immediate = true)
        {
            target = nextTarget;
            followSharpness = platformKind == HollowPlatformKind.VisionOSBoundedTabletop ? 12f : 8f;
            lookAheadSeconds = platformKind == HollowPlatformKind.VisionOSBoundedTabletop ? 0.04f : DefaultLookAheadSeconds;
            CaptureBaselineIfNeeded();
            ResetTargetHistory();

            if (immediate)
            {
                ApplyImmediate();
            }
        }

        public void ApplyImmediate()
        {
            if (target == null)
            {
                return;
            }

            transform.position = CalculateDesiredRigPosition();
            ResetTargetHistory();
        }

        private void LateUpdate()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (target == null)
            {
                return;
            }

            CaptureBaselineIfNeeded();
            UpdateLookAhead(Mathf.Max(0f, deltaTime));
            var desired = CalculateDesiredRigPosition();
            if (PlanarDistance(transform.position, desired) > snapDistanceMeters)
            {
                planarLookAhead = Vector3.zero;
                transform.position = CalculateDesiredRigPosition();
                return;
            }

            var sharpness = Mathf.Max(MinimumSharpness, followSharpness);
            var t = 1f - Mathf.Exp(-sharpness * Mathf.Max(0f, deltaTime));
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }

        private void CaptureBaselineIfNeeded()
        {
            if (hasBaseline)
            {
                return;
            }

            baselineRigPosition = transform.position;
            hasBaseline = true;
        }

        private Vector3 CalculateDesiredRigPosition()
        {
            CaptureBaselineIfNeeded();
            if (target == null)
            {
                return transform.position;
            }

            return new Vector3(target.position.x + planarLookAhead.x, baselineRigPosition.y, target.position.z + planarLookAhead.z);
        }

        private void ResetTargetHistory()
        {
            if (target == null)
            {
                hasLastTargetPosition = false;
                planarLookAhead = Vector3.zero;
                return;
            }

            lastTargetPosition = target.position;
            planarLookAhead = Vector3.zero;
            hasLastTargetPosition = true;
        }

        private void UpdateLookAhead(float deltaTime)
        {
            if (target == null || deltaTime <= 0f)
            {
                return;
            }

            if (!hasLastTargetPosition)
            {
                ResetTargetHistory();
                return;
            }

            var targetDelta = target.position - lastTargetPosition;
            targetDelta.y = 0f;
            lastTargetPosition = target.position;
            var desiredLookAhead = Vector3.ClampMagnitude(targetDelta / deltaTime * Mathf.Max(0f, lookAheadSeconds), Mathf.Max(0f, maxLookAheadMeters));
            var t = 1f - Mathf.Exp(-12f * deltaTime);
            planarLookAhead = Vector3.Lerp(planarLookAhead, desiredLookAhead, t);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
