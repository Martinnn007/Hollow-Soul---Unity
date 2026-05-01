using Hollow.Rewards;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class RoomChestController : MonoBehaviour
    {
        public string RoomId { get; private set; } = string.Empty;

        public string ChestId { get; private set; } = string.Empty;

        public ChestKind Kind { get; private set; }

        public ChestState State { get; private set; }

        public bool IsOpened => State == ChestState.Opened;

        public void Configure(string roomId, string chestId, ChestKind kind, ChestState state)
        {
            RoomId = roomId ?? string.Empty;
            ChestId = chestId ?? string.Empty;
            Kind = kind;
            State = state;
            ApplyVisualState();
        }

        public bool Open()
        {
            if (State == ChestState.Opened)
            {
                return false;
            }

            State = ChestState.Opened;
            ApplyVisualState();
            return true;
        }

        private void ApplyVisualState()
        {
            gameObject.name = State == ChestState.Opened ? $"OpenedChest_{Kind}_{ChestId}" : $"Chest_{Kind}_{ChestId}";

            var artPassVisual = FindArtPassVisualRoot();
            if (artPassVisual != null)
            {
                transform.localScale = Vector3.one;
                NormalizeArtPassVisual(artPassVisual);
                return;
            }

            var baseScale = Kind == ChestKind.Golden
                ? new Vector3(0.78f, 0.5f, 0.62f)
                : new Vector3(0.72f, 0.46f, 0.58f);
            transform.localScale = State == ChestState.Opened
                ? new Vector3(baseScale.x, baseScale.y * 0.72f, baseScale.z)
                : baseScale;
        }

        private Transform FindArtPassVisualRoot()
        {
            foreach (var marker in GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true))
            {
                if (marker != null && marker.transform.parent == transform)
                {
                    return marker.transform;
                }
            }

            var fallbackMarker = GetComponentInChildren<PresentationVisualMarker>(includeInactive: true);
            return fallbackMarker != null ? fallbackMarker.transform : null;
        }

        private void NormalizeArtPassVisual(Transform visualRoot)
        {
            if (visualRoot == null)
            {
                return;
            }

            var targetSize = Kind == ChestKind.Golden
                ? new Vector3(0.88f, 0.58f, 0.7f)
                : new Vector3(0.78f, 0.52f, 0.64f);

            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;

            if (!TryGetRendererBounds(visualRoot, out var bounds))
            {
                visualRoot.localScale = State == ChestState.Opened
                    ? new Vector3(1f, 0.72f, 1f)
                    : Vector3.one;
                return;
            }

            var size = bounds.size;
            var scale = new Vector3(
                SafeScale(targetSize.x, size.x),
                SafeScale(targetSize.y, size.y) * (State == ChestState.Opened ? 0.72f : 1f),
                SafeScale(targetSize.z, size.z));

            if (!IsValidScale(scale))
            {
                scale = Vector3.one;
            }

            visualRoot.localScale = scale;

            if (!TryGetRendererBounds(visualRoot, out bounds) || visualRoot.parent == null)
            {
                return;
            }

            var localCenter = visualRoot.parent.InverseTransformPoint(bounds.center);
            var localMin = visualRoot.parent.InverseTransformPoint(bounds.min);
            var targetBottomLocalY = -visualRoot.parent.localPosition.y;
            visualRoot.localPosition += new Vector3(
                -localCenter.x,
                targetBottomLocalY - localMin.y,
                -localCenter.z);
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

        private static bool IsValidScale(Vector3 value)
        {
            return IsValidScaleComponent(value.x) &&
                IsValidScaleComponent(value.y) &&
                IsValidScaleComponent(value.z);
        }

        private static bool IsValidScaleComponent(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
