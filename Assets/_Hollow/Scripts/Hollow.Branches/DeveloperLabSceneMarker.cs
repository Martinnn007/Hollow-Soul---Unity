using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Branches
{
    [DisallowMultipleComponent]
    public sealed class DeveloperLabSceneMarker : MonoBehaviour
    {
        [SerializeField] private string entryId = string.Empty;
        [SerializeField] private DeveloperLabContentCategory category = DeveloperLabContentCategory.ArtPassDisplay;
        [SerializeField] private string label = string.Empty;
        [SerializeField] private Vector3 visualScale = Vector3.one;
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
        public Vector3 VisualScale => visualScale;
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
            Vector3 nextVisualScale,
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
            entryId = string.IsNullOrWhiteSpace(nextEntryId) ? name : nextEntryId;
            category = nextCategory;
            label = nextLabel ?? string.Empty;
            visualScale = nextVisualScale == Vector3.zero ? Vector3.one : nextVisualScale;
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

        public DeveloperLabContentEntry ToContentEntry()
        {
            var entry = new DeveloperLabContentEntry();
            entry.Configure(
                entryId,
                category,
                label,
                transform.localPosition,
                visualScale,
                presentationRole,
                materialRole,
                primitiveType,
                cellKind,
                markerKind,
                pickupId,
                enemyKind,
                bossId,
                chestKind,
                coinDenomination,
                spawnMode,
                exportToRuntime,
                includeInGallery,
                includeLabel,
                labelOffset,
                labelScale,
                labelColor,
                doorDirection,
                doorLaneIndex,
                hostCellX,
                hostCellZ,
                doorState);
            return entry;
        }
    }
}
