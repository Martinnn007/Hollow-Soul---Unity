using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomLayout
    {
        public RoomLayout(
            int widthTiles,
            int heightTiles,
            Rect bounds,
            IReadOnlyList<Vector2Int> walkableTiles,
            IReadOnlyList<Vector2Int> holeTiles,
            IReadOnlyList<RoomLayoutFloorRegion> floorRegions,
            IReadOnlyList<RoomLayoutObstacle> obstacles)
        {
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
            Bounds = bounds;
            WalkableTiles = walkableTiles;
            HoleTiles = holeTiles;
            FloorRegions = floorRegions;
            Obstacles = obstacles;
        }

        public int WidthTiles { get; }

        public int HeightTiles { get; }

        public Rect Bounds { get; }

        public IReadOnlyList<Vector2Int> WalkableTiles { get; }

        public IReadOnlyList<Vector2Int> HoleTiles { get; }

        public IReadOnlyList<RoomLayoutFloorRegion> FloorRegions { get; }

        public IReadOnlyList<RoomLayoutObstacle> Obstacles { get; }
    }
}
