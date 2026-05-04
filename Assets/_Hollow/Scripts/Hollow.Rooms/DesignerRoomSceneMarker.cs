using UnityEngine;

namespace Hollow.Rooms
{
    public enum DesignerRoomSceneMarkerKind
    {
        RoomRoot = 0,
        Folder = 1,
        FloorRegion = 2,
        DoorPort = 3,
        SafeStart = 4,
        EnemySpawn = 5,
        ItemSpawn = 6,
        Obstacle = 7,
        Hazard = 8,
        InteractiveObject = 9,
        HoleTile = 10
    }

    [DisallowMultipleComponent]
    public sealed class DesignerRoomSceneMarker : MonoBehaviour
    {
        [SerializeField] private string markerId = string.Empty;
        [SerializeField] private DesignerRoomSceneMarkerKind markerKind = DesignerRoomSceneMarkerKind.Folder;
        [SerializeField] private string runtimeKind = string.Empty;
        [SerializeField] private string sourceRoomId = string.Empty;
        [SerializeField] private string sourceRuntimePath = string.Empty;
        [SerializeField] private string notes = string.Empty;
        [SerializeField] private bool editableByDesigner = true;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private bool showLabel = true;
        [SerializeField] private bool lockedLayer;
        [SerializeField] private float previewRadiusMeters = 0.5f;
        [SerializeField] private string doorDirection = string.Empty;
        [SerializeField] private int doorLaneIndex;
        [SerializeField] private int hostCellX;
        [SerializeField] private int hostCellZ;
        [SerializeField] private string doorState = "door";

        public string MarkerId => markerId;

        public DesignerRoomSceneMarkerKind MarkerKind => markerKind;

        public string RuntimeKind => runtimeKind;

        public string SourceRoomId => sourceRoomId;

        public string SourceRuntimePath => sourceRuntimePath;

        public string Notes => notes;

        public bool EditableByDesigner => editableByDesigner;

        public string DisplayName => displayName;

        public bool ShowLabel => showLabel;

        public bool LockedLayer => lockedLayer;

        public float PreviewRadiusMeters => Mathf.Max(0.05f, previewRadiusMeters);

        public string DoorDirection => doorDirection;

        public int DoorLaneIndex => Mathf.Max(0, doorLaneIndex);

        public int HostCellX => hostCellX;

        public int HostCellZ => hostCellZ;

        public string DoorState => string.IsNullOrWhiteSpace(doorState) ? "door" : doorState;

        public string Label
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName;
                }

                return markerKind switch
                {
                    DesignerRoomSceneMarkerKind.RoomRoot => string.IsNullOrWhiteSpace(sourceRoomId) ? "Room Root" : sourceRoomId,
                    DesignerRoomSceneMarkerKind.FloorRegion => "Floor Region",
                    DesignerRoomSceneMarkerKind.DoorPort => $"Door {DoorDirectionLabel()}_{DoorLaneIndex}",
                    DesignerRoomSceneMarkerKind.SafeStart => "Safe Start",
                    DesignerRoomSceneMarkerKind.EnemySpawn => EnemyDisplayName(runtimeKind),
                    DesignerRoomSceneMarkerKind.ItemSpawn => runtimeKind == "spawn_point_chest" ? "Chest" : "Room Reward",
                    DesignerRoomSceneMarkerKind.Obstacle => "Rock",
                    DesignerRoomSceneMarkerKind.Hazard => "Spike",
                    DesignerRoomSceneMarkerKind.InteractiveObject => runtimeKind == "barrelExplosive" ? "Explosive Barrel" : "Barrel",
                    DesignerRoomSceneMarkerKind.HoleTile => "Hole",
                    _ => string.IsNullOrWhiteSpace(markerId) ? markerKind.ToString() : markerId
                };
            }
        }

        public void Configure(
            string nextMarkerId,
            DesignerRoomSceneMarkerKind nextMarkerKind,
            string nextRuntimeKind,
            string nextSourceRoomId,
            string nextSourceRuntimePath,
            string nextNotes,
            bool nextEditableByDesigner = true)
        {
            markerId = nextMarkerId ?? string.Empty;
            markerKind = nextMarkerKind;
            runtimeKind = nextRuntimeKind ?? string.Empty;
            sourceRoomId = nextSourceRoomId ?? string.Empty;
            sourceRuntimePath = nextSourceRuntimePath ?? string.Empty;
            notes = nextNotes ?? string.Empty;
            editableByDesigner = nextEditableByDesigner;
        }

        public void ConfigureAuthoring(
            string nextMarkerId,
            DesignerRoomSceneMarkerKind nextMarkerKind,
            string nextRuntimeKind,
            string nextSourceRoomId,
            string nextSourceRuntimePath,
            string nextNotes,
            bool nextEditableByDesigner,
            string nextDisplayName,
            bool nextShowLabel,
            bool nextLockedLayer,
            float nextPreviewRadiusMeters,
            string nextDoorDirection = "",
            int nextDoorLaneIndex = 0,
            int nextHostCellX = 0,
            int nextHostCellZ = 0,
            string nextDoorState = "door")
        {
            Configure(
                nextMarkerId,
                nextMarkerKind,
                nextRuntimeKind,
                nextSourceRoomId,
                nextSourceRuntimePath,
                nextNotes,
                nextEditableByDesigner);
            displayName = nextDisplayName ?? string.Empty;
            showLabel = nextShowLabel;
            lockedLayer = nextLockedLayer;
            previewRadiusMeters = nextPreviewRadiusMeters <= 0f ? 0.5f : nextPreviewRadiusMeters;
            ConfigureDoor(nextDoorDirection, nextDoorLaneIndex, nextHostCellX, nextHostCellZ, nextDoorState);
        }

        public void ConfigureDoor(string nextDoorDirection, int nextDoorLaneIndex, int nextHostCellX, int nextHostCellZ, string nextDoorState)
        {
            doorDirection = nextDoorDirection ?? string.Empty;
            doorLaneIndex = Mathf.Max(0, nextDoorLaneIndex);
            hostCellX = nextHostCellX;
            hostCellZ = nextHostCellZ;
            doorState = string.IsNullOrWhiteSpace(nextDoorState) ? "door" : nextDoorState;
        }

        public void SetDisplayName(string nextDisplayName)
        {
            displayName = nextDisplayName ?? string.Empty;
        }

        public void SetShowLabel(bool nextShowLabel)
        {
            showLabel = nextShowLabel;
        }

        public void SetLockedLayer(bool nextLockedLayer)
        {
            lockedLayer = nextLockedLayer;
        }

        public void SetPreviewRadius(float nextPreviewRadiusMeters)
        {
            previewRadiusMeters = nextPreviewRadiusMeters <= 0f ? 0.5f : nextPreviewRadiusMeters;
        }

        private void OnDrawGizmos()
        {
            if (markerKind == DesignerRoomSceneMarkerKind.Folder)
            {
                return;
            }

            var previous = Gizmos.color;
            Gizmos.color = ColorFor(markerKind);
            var position = transform.position;
            var scale = transform.lossyScale;
            var size = new Vector3(
                Mathf.Max(0.12f, Mathf.Abs(scale.x)),
                Mathf.Max(0.12f, Mathf.Abs(scale.y)),
                Mathf.Max(0.12f, Mathf.Abs(scale.z)));

            switch (markerKind)
            {
                case DesignerRoomSceneMarkerKind.SafeStart:
                case DesignerRoomSceneMarkerKind.EnemySpawn:
                case DesignerRoomSceneMarkerKind.ItemSpawn:
                    Gizmos.DrawWireSphere(position, Mathf.Max(size.x, size.z) * 0.5f);
                    break;
                case DesignerRoomSceneMarkerKind.Hazard:
                    Gizmos.DrawWireSphere(position, Mathf.Max(size.x, size.z) * 0.5f);
                    Gizmos.DrawLine(position + Vector3.left * size.x * 0.5f, position + Vector3.right * size.x * 0.5f);
                    Gizmos.DrawLine(position + Vector3.back * size.z * 0.5f, position + Vector3.forward * size.z * 0.5f);
                    break;
                default:
                    Gizmos.DrawWireCube(position, size);
                    break;
            }

            Gizmos.color = previous;
        }

        public static Color ColorFor(DesignerRoomSceneMarkerKind kind)
        {
            return kind switch
            {
                DesignerRoomSceneMarkerKind.RoomRoot => new Color(0.75f, 0.75f, 0.75f, 0.85f),
                DesignerRoomSceneMarkerKind.FloorRegion => new Color(0.25f, 0.75f, 0.35f, 0.35f),
                DesignerRoomSceneMarkerKind.DoorPort => new Color(0.2f, 0.55f, 1f, 0.95f),
                DesignerRoomSceneMarkerKind.SafeStart => new Color(0.2f, 1f, 0.45f, 0.95f),
                DesignerRoomSceneMarkerKind.EnemySpawn => new Color(1f, 0.2f, 0.2f, 0.95f),
                DesignerRoomSceneMarkerKind.ItemSpawn => new Color(1f, 0.9f, 0.25f, 0.95f),
                DesignerRoomSceneMarkerKind.Obstacle => new Color(0.62f, 0.62f, 0.68f, 0.9f),
                DesignerRoomSceneMarkerKind.Hazard => new Color(1f, 0.35f, 0.08f, 0.95f),
                DesignerRoomSceneMarkerKind.InteractiveObject => new Color(0.65f, 0.38f, 0.18f, 0.95f),
                DesignerRoomSceneMarkerKind.HoleTile => new Color(0.08f, 0.08f, 0.1f, 0.95f),
                _ => Color.white
            };
        }

        private string DoorDirectionLabel()
        {
            if (!string.IsNullOrWhiteSpace(doorDirection))
            {
                return doorDirection;
            }

            if (!string.IsNullOrWhiteSpace(markerId))
            {
                var separator = markerId.IndexOf('_');
                if (separator > 0)
                {
                    return markerId.Substring(0, separator);
                }
            }

            return "?";
        }

        private static string EnemyDisplayName(string kind)
        {
            return kind switch
            {
                "spawnEnemyFlying" => "Flying Chaser",
                "spawnEnemyFast" => "Fast Chaser",
                "spawnEnemyHeavy" => "Heavy Chaser",
                "spawnEnemyCharger" => "Ash Charger",
                "spawnEnemyTurret" => "Bone Turret",
                "spawnEnemySplitter" => "Husk Splitter",
                "spawnEnemySpittingPod" => "Spitting Pod",
                "spawnEnemyRat" => "Rat",
                "spawnEnemySpider" => "Spider",
                "spawnEnemyHollowBird" => "Hollow Bird",
                "spawnEnemyHollowBeast" => "Hollow Beast",
                "spawnEnemySkeletonSword" => "Skeleton Sword",
                "spawnEnemySkeletonSpear" => "Skeleton Spear",
                "spawnEnemyKnight" => "Knight",
                "spawnEnemyGiant" => "Giant",
                "spawnEnemyHollowArcher" => "Hollow Archer",
                "spawnEnemyPowderGunner" => "Powder Gunner",
                "spawnEnemyKnifeThrower" => "Knife Thrower",
                "spawnEnemyRepeaterTurret" => "Repeater Turret",
                "spawnEnemyClockworkSentry" => "Clockwork Sentry",
                "spawnEnemyHollowAcolyte" => "Hollow Acolyte",
                "spawnEnemyWraith" => "Wraith",
                "spawnEnemySoulEater" => "Soul Eater",
                "spawnEnemyCurseBinder" => "Curse Binder",
                "spawnEnemyGraveLantern" => "Grave Lantern",
                _ => "Normal Chaser"
            };
        }
    }
}
