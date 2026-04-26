using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomLayoutFloorRegion
    {
        public RoomLayoutFloorRegion(string id, Vector3 center, Vector2 halfSize)
        {
            Id = id;
            Center = center;
            HalfSize = halfSize;
        }

        public string Id { get; }

        public Vector3 Center { get; }

        public Vector2 HalfSize { get; }
    }
}
