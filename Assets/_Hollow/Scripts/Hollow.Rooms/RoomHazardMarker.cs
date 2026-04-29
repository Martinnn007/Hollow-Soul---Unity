using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomHazardMarker : MonoBehaviour
    {
        [SerializeField] private string hazardId = string.Empty;
        [SerializeField] private string hazardKind = RoomHazardKind.Spike;
        [SerializeField] private float radiusMeters = 0.45f;

        public string HazardId => hazardId;

        public string HazardKind => hazardKind;

        public float RadiusMeters => Mathf.Max(0.05f, radiusMeters);

        public void Configure(ImportedRoomHazard hazard)
        {
            hazardId = string.IsNullOrWhiteSpace(hazard?.id) ? name : hazard.id;
            hazardKind = string.IsNullOrWhiteSpace(hazard?.kind) ? RoomHazardKind.Spike : hazard.kind;
            radiusMeters = hazard != null ? Mathf.Max(0.05f, hazard.radius) : 0.45f;
        }
    }
}
