using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomLayoutObstacle
    {
        public RoomLayoutObstacle(string id, string kind, Vector3 center, Vector3 size, bool blocksProjectiles)
        {
            Id = id;
            Kind = kind;
            Center = center;
            Size = size;
            BlocksProjectiles = blocksProjectiles;
        }

        public string Id { get; }

        public string Kind { get; }

        public Vector3 Center { get; }

        public Vector3 Size { get; }

        public bool BlocksProjectiles { get; }
    }
}
