using UnityEngine;

namespace Hollow.Combat
{
    [DisallowMultipleComponent]
    public sealed class SimpleFullBodyGroundingController : MonoBehaviour
    {
        public const float DefaultGroundClearanceMeters = 0.025f;
        public const float DefaultMaxCorrectionMeters = 2.5f;
        public const float DefaultMaxPlanarCorrectionMeters = 1.25f;
        private const float FootBoneSoleApproximationMeters = 0.08f;

        [SerializeField] private Transform measuredRoot;
        [SerializeField] private Transform offsetRoot;
        [SerializeField] private Transform groundReference;
        [SerializeField] private bool groundingEnabled = true;
        [SerializeField] private bool stabilizePlanarCenter = true;
        [SerializeField] private float groundClearanceMeters = DefaultGroundClearanceMeters;
        [SerializeField] private float maxCorrectionMeters = DefaultMaxCorrectionMeters;
        [SerializeField] private float maxPlanarCorrectionMeters = DefaultMaxPlanarCorrectionMeters;

        private bool hasPlanarAnchor;
        private Vector2 planarCenterAnchor;

        public bool GroundingEnabled => groundingEnabled;
        public bool StabilizePlanarCenter => stabilizePlanarCenter;
        public Transform MeasuredRoot => measuredRoot;
        public Transform OffsetRoot => offsetRoot;
        public Transform GroundReference => groundReference;
        public float GroundClearanceMeters => groundClearanceMeters;
        public float MaxPlanarCorrectionMeters => maxPlanarCorrectionMeters;
        public float LastBodyBottomY { get; private set; }
        public float LastMeasuredFootBottomY { get; private set; }
        public float LastGroundY { get; private set; }
        public float LastCorrectionY { get; private set; }
        public Vector2 LastPlanarCorrection { get; private set; }
        public bool LastApplySucceeded { get; private set; }

        public void Configure(
            Transform nextMeasuredRoot,
            Transform nextOffsetRoot,
            Transform nextGroundReference,
            bool enabled,
            float clearanceMeters = DefaultGroundClearanceMeters,
            float maximumCorrectionMeters = DefaultMaxCorrectionMeters,
            bool stabilizePlanar = true,
            float maximumPlanarCorrectionMeters = DefaultMaxPlanarCorrectionMeters)
        {
            measuredRoot = nextMeasuredRoot;
            offsetRoot = nextOffsetRoot;
            groundReference = nextGroundReference;
            groundingEnabled = enabled;
            stabilizePlanarCenter = stabilizePlanar;
            groundClearanceMeters = Mathf.Max(0f, clearanceMeters);
            maxCorrectionMeters = Mathf.Max(0.01f, maximumCorrectionMeters);
            maxPlanarCorrectionMeters = Mathf.Max(0.01f, maximumPlanarCorrectionMeters);
            hasPlanarAnchor = false;
        }

        public void SetGroundingEnabled(bool enabled)
        {
            groundingEnabled = enabled;
            if (!enabled)
            {
                LastCorrectionY = 0f;
                LastPlanarCorrection = Vector2.zero;
                LastApplySucceeded = false;
                hasPlanarAnchor = false;
            }
        }

        private void OnEnable()
        {
            ApplyGrounding();
        }

        private void Start()
        {
            ApplyGrounding();
        }

        private void LateUpdate()
        {
            ApplyGrounding();
        }

        public bool ApplyGrounding()
        {
            LastCorrectionY = 0f;
            LastPlanarCorrection = Vector2.zero;
            LastApplySucceeded = false;

            if (!groundingEnabled || offsetRoot == null || !TryResolveBodyBounds(out var bounds))
            {
                return false;
            }

            ApplyPlanarCenterStabilization(bounds);
            if (LastPlanarCorrection.sqrMagnitude > 0.000001f && !TryResolveBodyBounds(out bounds))
            {
                return false;
            }

            LastBodyBottomY = bounds.min.y;
            LastGroundY = groundReference != null ? groundReference.position.y : 0f;
            var correction = LastGroundY + groundClearanceMeters - LastBodyBottomY;
            if (Mathf.Abs(correction) <= 0.001f)
            {
                LastApplySucceeded = true;
                return true;
            }

            correction = Mathf.Clamp(correction, -maxCorrectionMeters, maxCorrectionMeters);
            offsetRoot.position += Vector3.up * correction;
            LastCorrectionY = correction;
            LastApplySucceeded = true;
            return true;
        }

        private void ApplyPlanarCenterStabilization(Bounds bounds)
        {
            if (!stabilizePlanarCenter)
            {
                hasPlanarAnchor = false;
                return;
            }

            var referencePosition = groundReference != null ? groundReference.position : transform.position;
            var referencePlanar = new Vector2(referencePosition.x, referencePosition.z);
            var centerPlanar = new Vector2(bounds.center.x, bounds.center.z);
            if (!hasPlanarAnchor)
            {
                planarCenterAnchor = centerPlanar - referencePlanar;
                hasPlanarAnchor = true;
                return;
            }

            var desiredCenter = referencePlanar + planarCenterAnchor;
            var correction = desiredCenter - centerPlanar;
            if (correction.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            correction = Vector2.ClampMagnitude(correction, maxPlanarCorrectionMeters);
            offsetRoot.position += new Vector3(correction.x, 0f, correction.y);
            LastPlanarCorrection = correction;
        }

        private bool TryResolveBodyBounds(out Bounds bounds)
        {
            bounds = default;
            var root = measuredRoot != null ? measuredRoot : transform;
            var hasBounds = false;
            LastMeasuredFootBottomY = float.NaN;

            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: false))
            {
                if (!IsUsableRenderer(renderer))
                {
                    continue;
                }

                bounds = hasBounds ? Encapsulate(bounds, renderer.bounds) : renderer.bounds;
                hasBounds = true;
            }

            if (hasBounds)
            {
                return bounds.size.sqrMagnitude > 0.0001f;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: false))
            {
                if (!IsUsableRenderer(renderer))
                {
                    continue;
                }

                bounds = hasBounds ? Encapsulate(bounds, renderer.bounds) : renderer.bounds;
                hasBounds = true;
            }

            if (TryResolveAnimatedFootBottomY(root, out var footBottomY))
            {
                LastMeasuredFootBottomY = footBottomY;
                if (hasBounds)
                {
                    bounds.Encapsulate(new Vector3(bounds.center.x, footBottomY, bounds.center.z));
                }
                else
                {
                    bounds = new Bounds(new Vector3(root.position.x, footBottomY, root.position.z), Vector3.one * 0.01f);
                    hasBounds = true;
                }
            }

            return hasBounds && bounds.size.sqrMagnitude > 0.0001f;
        }

        private static bool TryResolveAnimatedFootBottomY(Transform root, out float footBottomY)
        {
            footBottomY = 0f;
            var animator = root.GetComponentInChildren<Animator>(includeInactive: false);
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                return false;
            }

            var found = false;
            var lowest = float.PositiveInfinity;
            ConsiderFoot(animator.GetBoneTransform(HumanBodyBones.LeftFoot), ref found, ref lowest);
            ConsiderFoot(animator.GetBoneTransform(HumanBodyBones.RightFoot), ref found, ref lowest);
            if (!found)
            {
                return false;
            }

            footBottomY = lowest - FootBoneSoleApproximationMeters;
            return true;
        }

        private static void ConsiderFoot(Transform foot, ref bool found, ref float lowest)
        {
            if (foot == null || !foot.gameObject.activeInHierarchy)
            {
                return;
            }

            found = true;
            lowest = Mathf.Min(lowest, foot.position.y);
        }

        private static bool IsUsableRenderer(Renderer renderer)
        {
            return renderer != null &&
                   renderer.enabled &&
                   renderer.gameObject.activeInHierarchy &&
                   renderer.bounds.size.sqrMagnitude > 0.0001f;
        }

        private static Bounds Encapsulate(Bounds current, Bounds next)
        {
            current.Encapsulate(next);
            return current;
        }
    }
}
