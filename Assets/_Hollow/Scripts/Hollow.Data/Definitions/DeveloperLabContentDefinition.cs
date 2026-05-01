using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    public enum DeveloperLabContentCategory
    {
        Label = 0,
        ArtPassDisplay = 1,
        PrimitiveDisplay = 2,
        Coin = 3,
        Chest = 4,
        Enemy = 5,
        Boss = 6,
        RoomCell = 7,
        RoomMarker = 8,
        DoorPort = 9
    }

    [Serializable]
    public sealed class DeveloperLabContentEntry
    {
        [SerializeField] private string entryId = string.Empty;
        [SerializeField] private DeveloperLabContentCategory category = DeveloperLabContentCategory.Label;
        [SerializeField] private string label = string.Empty;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private PresentationPrefabRole presentationRole = PresentationPrefabRole.RewardPickup;
        [SerializeField] private MaterialRole materialRole = MaterialRole.RewardPickup;
        [SerializeField] private PrimitiveType primitiveType = PrimitiveType.Cube;
        [SerializeField] private string cellKind = string.Empty;
        [SerializeField] private string markerKind = string.Empty;
        [SerializeField] private string pickupId = string.Empty;
        [SerializeField] private string enemyKind = string.Empty;
        [SerializeField] private string bossId = string.Empty;
        [SerializeField] private string chestKind = string.Empty;
        [SerializeField] private string coinDenomination = string.Empty;
        [SerializeField] private InspectionEntityMode spawnMode = InspectionEntityMode.FrozenRuntime;
        [SerializeField] private bool exportToRuntime;
        [SerializeField] private bool includeInGallery = true;
        [SerializeField] private bool includeLabel = true;
        [SerializeField] private Vector3 labelOffset = new(0f, 0.92f, 0f);
        [SerializeField] private float labelScale = 0.065f;
        [SerializeField] private Color labelColor = Color.white;
        [SerializeField] private string doorDirection = string.Empty;
        [SerializeField] private int doorLaneIndex;
        [SerializeField] private int hostCellX;
        [SerializeField] private int hostCellZ;
        [SerializeField] private string doorState = "door";

        public string EntryId => entryId;
        public DeveloperLabContentCategory Category => category;
        public string Label => label;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalScale => localScale;
        public PresentationPrefabRole PresentationRole => presentationRole;
        public MaterialRole MaterialRole => materialRole;
        public PrimitiveType PrimitiveType => primitiveType;
        public string CellKind => cellKind;
        public string MarkerKind => markerKind;
        public string PickupId => pickupId;
        public string EnemyKind => enemyKind;
        public string BossId => bossId;
        public string ChestKind => chestKind;
        public string CoinDenomination => coinDenomination;
        public InspectionEntityMode SpawnMode => spawnMode;
        public bool ExportToRuntime => exportToRuntime;
        public bool IncludeInGallery => includeInGallery;
        public bool IncludeLabel => includeLabel;
        public Vector3 LabelOffset => labelOffset;
        public float LabelScale => labelScale;
        public Color LabelColor => labelColor;
        public string DoorDirection => doorDirection;
        public int DoorLaneIndex => doorLaneIndex;
        public int HostCellX => hostCellX;
        public int HostCellZ => hostCellZ;
        public string DoorState => doorState;

        public void Configure(
            string nextEntryId,
            DeveloperLabContentCategory nextCategory,
            string nextLabel,
            Vector3 nextLocalPosition,
            Vector3 nextLocalScale,
            PresentationPrefabRole nextPresentationRole,
            MaterialRole nextMaterialRole,
            PrimitiveType nextPrimitiveType,
            string nextCellKind,
            string nextMarkerKind,
            string nextPickupId,
            string nextEnemyKind,
            string nextBossId,
            string nextChestKind,
            string nextCoinDenomination,
            InspectionEntityMode nextSpawnMode,
            bool nextExportToRuntime,
            bool nextIncludeInGallery,
            bool nextIncludeLabel,
            Vector3 nextLabelOffset,
            float nextLabelScale,
            Color nextLabelColor,
            string nextDoorDirection = "",
            int nextDoorLaneIndex = 0,
            int nextHostCellX = 0,
            int nextHostCellZ = 0,
            string nextDoorState = "door")
        {
            entryId = string.IsNullOrWhiteSpace(nextEntryId) ? Guid.NewGuid().ToString("N") : nextEntryId;
            category = nextCategory;
            label = nextLabel ?? string.Empty;
            localPosition = nextLocalPosition;
            localScale = nextLocalScale == Vector3.zero ? Vector3.one : nextLocalScale;
            presentationRole = nextPresentationRole;
            materialRole = nextMaterialRole;
            primitiveType = nextPrimitiveType;
            cellKind = nextCellKind ?? string.Empty;
            markerKind = nextMarkerKind ?? string.Empty;
            pickupId = nextPickupId ?? string.Empty;
            enemyKind = nextEnemyKind ?? string.Empty;
            bossId = nextBossId ?? string.Empty;
            chestKind = nextChestKind ?? string.Empty;
            coinDenomination = nextCoinDenomination ?? string.Empty;
            spawnMode = nextSpawnMode;
            exportToRuntime = nextExportToRuntime;
            includeInGallery = nextIncludeInGallery;
            includeLabel = nextIncludeLabel;
            labelOffset = nextLabelOffset;
            labelScale = nextLabelScale <= 0f ? 0.065f : nextLabelScale;
            labelColor = nextLabelColor;
            doorDirection = nextDoorDirection ?? string.Empty;
            doorLaneIndex = Mathf.Max(0, nextDoorLaneIndex);
            hostCellX = nextHostCellX;
            hostCellZ = nextHostCellZ;
            doorState = string.IsNullOrWhiteSpace(nextDoorState) ? "door" : nextDoorState;
        }
    }

    [Serializable]
    public sealed class DeveloperLabRoomContentDefinition
    {
        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private int roomIndex = 1;
        [SerializeField] private string title = string.Empty;
        [SerializeField] private List<DeveloperLabContentEntry> entries = new();

        public string RoomId => roomId;
        public int RoomIndex => roomIndex;
        public string Title => title;
        public IReadOnlyList<DeveloperLabContentEntry> Entries
        {
            get
            {
                return entries != null ? (IReadOnlyList<DeveloperLabContentEntry>)entries : Array.Empty<DeveloperLabContentEntry>();
            }
        }

        public void Configure(string nextRoomId, int nextRoomIndex, string nextTitle, IEnumerable<DeveloperLabContentEntry> nextEntries)
        {
            roomId = nextRoomId ?? string.Empty;
            roomIndex = Mathf.Clamp(nextRoomIndex, 1, 99);
            title = nextTitle ?? string.Empty;
            entries = (nextEntries ?? Enumerable.Empty<DeveloperLabContentEntry>())
                .Where(entry => entry != null)
                .ToList();
        }
    }

    [CreateAssetMenu(menuName = "Hollow/Developer Lab/Content Definition", fileName = "DeveloperLabContentDefinition")]
    public sealed class DeveloperLabContentDefinition : HollowDefinition
    {
        [SerializeField] private string catalogId = "m66_developer_lab_scene_content_v1";
        [SerializeField] private List<DeveloperLabRoomContentDefinition> rooms = new();

        public string CatalogId => catalogId;
        public IReadOnlyList<DeveloperLabRoomContentDefinition> Rooms
        {
            get
            {
                return rooms != null ? (IReadOnlyList<DeveloperLabRoomContentDefinition>)rooms : Array.Empty<DeveloperLabRoomContentDefinition>();
            }
        }

        public void Configure(string nextCatalogId, IEnumerable<DeveloperLabRoomContentDefinition> nextRooms)
        {
            catalogId = string.IsNullOrWhiteSpace(nextCatalogId) ? "m66_developer_lab_scene_content_v1" : nextCatalogId;
            rooms = (nextRooms ?? Enumerable.Empty<DeveloperLabRoomContentDefinition>())
                .Where(room => room != null)
                .OrderBy(room => room.RoomIndex)
                .ToList();
        }

        public bool TryGetRoom(string roomId, int roomIndex, out DeveloperLabRoomContentDefinition room)
        {
            room = Rooms.FirstOrDefault(candidate =>
                (!string.IsNullOrWhiteSpace(roomId) && string.Equals(candidate.RoomId, roomId, StringComparison.Ordinal)) ||
                candidate.RoomIndex == roomIndex);
            return room != null;
        }
    }
}
