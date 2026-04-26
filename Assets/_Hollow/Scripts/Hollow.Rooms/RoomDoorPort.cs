using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomDoorPort
    {
        public RoomDoorPort(
            string id,
            string direction,
            int laneIndex,
            Vector2Int hostCell,
            Vector2 gridEdgeCenter,
            Vector3 position,
            string kind)
        {
            Id = id;
            Direction = direction;
            LaneIndex = laneIndex;
            HostCell = hostCell;
            GridEdgeCenter = gridEdgeCenter;
            Position = position;
            Kind = kind;
        }

        public string Id { get; }

        public string Direction { get; }

        public int LaneIndex { get; }

        public Vector2Int HostCell { get; }

        public Vector2 GridEdgeCenter { get; }

        public Vector3 Position { get; }

        public string Kind { get; }
    }
}
