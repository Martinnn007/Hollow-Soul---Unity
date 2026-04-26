using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomRuntimeRoot : MonoBehaviour
    {
        public const float DefaultWidthMeters = 13f;
        public const float DefaultDepthMeters = 7f;

        [SerializeField] private Vector2 roomSizeMeters = new(DefaultWidthMeters, DefaultDepthMeters);

        public Vector2 RoomSizeMeters => roomSizeMeters;

        public Vector3 CenterWorldPosition => transform.position;

        public void ConfigureDefault()
        {
            roomSizeMeters = new Vector2(DefaultWidthMeters, DefaultDepthMeters);
        }
    }
}
