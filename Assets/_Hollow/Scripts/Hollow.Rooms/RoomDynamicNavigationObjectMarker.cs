using UnityEngine;
using UnityEngine.AI;

namespace Hollow.Rooms
{
    public sealed class RoomDynamicNavigationObjectMarker : MonoBehaviour
    {
        private const string DebugLabelName = "DynamicNavigationDebugLabel";

        [SerializeField] private string objectId = string.Empty;
        [SerializeField] private string objectKind = string.Empty;
        [SerializeField] private RoomDynamicNavigationObjectCategory category = RoomDynamicNavigationObjectCategory.NonBlocking;
        [SerializeField] private Vector3 sizeMeters = Vector3.one;
        [SerializeField] private bool usesCarving;
        [SerializeField] private bool carvingActive;
        [SerializeField] private string lastReason = "not_configured";
        [SerializeField] private bool showDebugLabel;

        private NavMeshObstacle cachedObstacle;
        private TextMesh debugLabel;

        public string ObjectId => objectId;

        public string ObjectKind => objectKind;

        public RoomDynamicNavigationObjectCategory Category => category;

        public Vector3 SizeMeters => SanitizeSize(sizeMeters);

        public bool UsesCarving => usesCarving;

        public bool CarvingActive => carvingActive;

        public string LastReason => lastReason;

        public string StatusSummary => $"{objectId}:{category}:carving={(carvingActive ? "on" : "off")}:{lastReason}";

        public void ConfigureStaticBaked(string id, string kind, Vector3 size, RoomDynamicNavigationObjectCategory staticCategory, string reason)
        {
            objectId = ResolveId(id);
            objectKind = string.IsNullOrWhiteSpace(kind) ? staticCategory.ToString() : kind;
            category = staticCategory;
            sizeMeters = SanitizeSize(size);
            usesCarving = false;
            carvingActive = false;
            lastReason = string.IsNullOrWhiteSpace(reason) ? "static_baked_navmesh" : reason;
            DisableObstacleIfPresent();
            RefreshDebugLabel();
        }

        public void ConfigureDynamicCarver(string id, string kind, Vector3 size, bool active, string reason)
        {
            objectId = ResolveId(id);
            objectKind = string.IsNullOrWhiteSpace(kind) ? RoomDynamicNavigationObjectCategory.DynamicCarver.ToString() : kind;
            category = RoomDynamicNavigationObjectCategory.DynamicCarver;
            sizeMeters = SanitizeSize(size);
            usesCarving = true;
            SetCarvingActive(active, string.IsNullOrWhiteSpace(reason) ? "dynamic_carver_configured" : reason);
        }

        public void ConfigureDoor(string id, string kind, Vector3 size, RoomDoorVisualState state)
        {
            objectId = ResolveId(id);
            objectKind = string.IsNullOrWhiteSpace(kind) ? "door" : kind;
            category = RoomDynamicNavigationObjectCategory.Door;
            sizeMeters = SanitizeSize(size);
            usesCarving = true;
            ApplyDoorState(state);
        }

        public void ApplyDoorState(RoomDoorVisualState state)
        {
            var blocksNavigation = state == RoomDoorVisualState.Locked || state == RoomDoorVisualState.Unavailable;
            SetCarvingActive(blocksNavigation, $"door_state_{state.ToString().ToLowerInvariant()}");
        }

        public void MarkDestroyed()
        {
            SetCarvingActive(false, "destroyed");
        }

        public void SetCarvingActive(bool active, string reason)
        {
            usesCarving = true;
            carvingActive = active;
            lastReason = string.IsNullOrWhiteSpace(reason) ? (active ? "carving_enabled" : "carving_disabled") : reason;

            if (!EnsureObstacle(out var obstacle))
            {
                carvingActive = false;
                RefreshDebugLabel();
                return;
            }

            obstacle.enabled = false;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = Vector3.zero;
            obstacle.size = SizeMeters;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            obstacle.enabled = active;
            RefreshDebugLabel();
        }

        public void SetDebugLabelVisible(bool visible)
        {
            showDebugLabel = visible;
            RefreshDebugLabel();
        }

        private bool EnsureObstacle(out NavMeshObstacle obstacle)
        {
            obstacle = cachedObstacle;
            if (obstacle == null && !TryGetComponent(out obstacle))
            {
                obstacle = gameObject.AddComponent<NavMeshObstacle>();
            }

            cachedObstacle = obstacle;
            return obstacle != null;
        }

        private void DisableObstacleIfPresent()
        {
            if (cachedObstacle == null)
            {
                TryGetComponent(out cachedObstacle);
            }

            if (cachedObstacle != null)
            {
                cachedObstacle.enabled = false;
            }
        }

        private void RefreshDebugLabel()
        {
            if (!showDebugLabel)
            {
                if (debugLabel != null)
                {
                    debugLabel.gameObject.SetActive(false);
                }

                return;
            }

            if (debugLabel == null)
            {
                var child = transform.Find(DebugLabelName);
                if (child != null)
                {
                    debugLabel = child.GetComponent<TextMesh>();
                }

                if (debugLabel == null)
                {
                    var labelObject = new GameObject(DebugLabelName);
                    labelObject.transform.SetParent(transform, false);
                    labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
                    debugLabel = labelObject.AddComponent<TextMesh>();
                    debugLabel.anchor = TextAnchor.MiddleCenter;
                    debugLabel.alignment = TextAlignment.Center;
                    debugLabel.characterSize = 0.12f;
                    debugLabel.fontSize = 32;
                    debugLabel.color = new Color(0.74f, 0.92f, 1f, 0.9f);
                }
            }

            debugLabel.gameObject.SetActive(true);
            debugLabel.transform.localPosition = new Vector3(0f, Mathf.Max(0.4f, SizeMeters.y * 0.5f + 0.22f), 0f);
            debugLabel.text = $"{objectId}\n{category} {(carvingActive ? "carve" : "baked")}";
        }

        private string ResolveId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? name : id;
        }

        private static Vector3 SanitizeSize(Vector3 size)
        {
            return new Vector3(
                Mathf.Max(0.05f, size.x),
                Mathf.Max(0.05f, size.y),
                Mathf.Max(0.05f, size.z));
        }
    }
}
