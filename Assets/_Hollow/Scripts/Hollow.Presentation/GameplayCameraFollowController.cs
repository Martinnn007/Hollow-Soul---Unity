using Hollow.Platform;
using UnityEngine;

namespace Hollow.Presentation
{
    public sealed class GameplayCameraFollowController : MonoBehaviour
    {
        private const float MinimumSharpness = 0.01f;

        [SerializeField] private Transform target;
        [SerializeField] private float followSharpness = 8f;

        private Vector3 baselineRigPosition;
        private bool hasBaseline;

        public Transform Target => target;

        public Vector3 DesiredRigPosition => CalculateDesiredRigPosition();

        public void Configure(Transform nextTarget, HollowPlatformKind platformKind, bool immediate = true)
        {
            target = nextTarget;
            followSharpness = platformKind == HollowPlatformKind.VisionOSBoundedTabletop ? 12f : 8f;
            CaptureBaselineIfNeeded();

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
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            CaptureBaselineIfNeeded();
            var desired = CalculateDesiredRigPosition();
            var sharpness = Mathf.Max(MinimumSharpness, followSharpness);
            var t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
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

            return new Vector3(target.position.x, baselineRigPosition.y, target.position.z);
        }
    }
}
