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

        public string MarkerId => markerId;

        public DesignerRoomSceneMarkerKind MarkerKind => markerKind;

        public string RuntimeKind => runtimeKind;

        public string SourceRoomId => sourceRoomId;

        public string SourceRuntimePath => sourceRuntimePath;

        public string Notes => notes;

        public bool EditableByDesigner => editableByDesigner;

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

        private static Color ColorFor(DesignerRoomSceneMarkerKind kind)
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
    }
}
