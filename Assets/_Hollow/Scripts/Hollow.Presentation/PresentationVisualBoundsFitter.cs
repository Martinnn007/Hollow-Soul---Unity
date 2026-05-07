using UnityEngine;

namespace Hollow.Presentation
{
    public static class PresentationVisualBoundsFitter
    {
        public static bool FitToTargetBounds(
            Transform visualRoot,
            Vector3 targetSize,
            float targetBottomLocalY = 0f,
            Quaternion? initialLocalRotation = null)
        {
            if (visualRoot == null)
            {
                return false;
            }

            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = initialLocalRotation ?? Quaternion.identity;
            visualRoot.localScale = Vector3.one;

            if (!TryGetRendererBounds(visualRoot, out var bounds))
            {
                return false;
            }

            var size = bounds.size;
            var uniformScale = Mathf.Min(
                SafeScale(targetSize.x, size.x),
                SafeScale(targetSize.y, size.y),
                SafeScale(targetSize.z, size.z));

            if (!IsValidScaleComponent(uniformScale))
            {
                return false;
            }

            visualRoot.localScale = Vector3.one * uniformScale;
            if (!TryGetRendererBounds(visualRoot, out bounds) || visualRoot.parent == null)
            {
                return true;
            }

            var localCenter = visualRoot.parent.InverseTransformPoint(bounds.center);
            var localMin = visualRoot.parent.InverseTransformPoint(bounds.min);
            visualRoot.localPosition += new Vector3(
                -localCenter.x,
                targetBottomLocalY - localMin.y,
                -localCenter.z);
            return true;
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds && bounds.size.sqrMagnitude > 0.0001f;
        }

        private static float SafeScale(float target, float current)
        {
            return Mathf.Abs(current) > 0.0001f ? target / current : float.PositiveInfinity;
        }

        private static bool IsValidScaleComponent(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
