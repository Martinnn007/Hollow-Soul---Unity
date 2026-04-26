using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Rooms
{
    public enum RoomFootprintShape
    {
        Unsupported = 0,
        Single1x1 = 1,
        Wide2x1 = 2,
        Tall1x2 = 3,
        Block2x2 = 4,
        L3Cell = 5
    }

    public static class RoomFootprintShapeUtility
    {
        public static RoomFootprintShape Classify(RoomInstanceFootprint footprint)
        {
            if (footprint == null || footprint.OccupiedCells == null)
            {
                return RoomFootprintShape.Unsupported;
            }

            var cells = Normalize(footprint.OccupiedCells).ToArray();
            return cells.Length switch
            {
                1 when Contains(cells, 0, 0) => RoomFootprintShape.Single1x1,
                2 when Contains(cells, 0, 0) && Contains(cells, 1, 0) => RoomFootprintShape.Wide2x1,
                2 when Contains(cells, 0, 0) && Contains(cells, 0, 1) => RoomFootprintShape.Tall1x2,
                3 when IsAnyL3Cell(cells) => RoomFootprintShape.L3Cell,
                4 when Contains(cells, 0, 0) && Contains(cells, 1, 0) && Contains(cells, 0, 1) && Contains(cells, 1, 1) => RoomFootprintShape.Block2x2,
                _ => RoomFootprintShape.Unsupported
            };
        }

        public static bool IsSupported(RoomInstanceFootprint footprint)
        {
            return Classify(footprint) != RoomFootprintShape.Unsupported;
        }

        private static IEnumerable<Vector2Int> Normalize(IEnumerable<Vector2Int> cells)
        {
            var source = cells?.ToArray() ?? System.Array.Empty<Vector2Int>();
            if (source.Length == 0)
            {
                return source;
            }

            var minX = source.Min(cell => cell.x);
            var minY = source.Min(cell => cell.y);
            return source.Select(cell => new Vector2Int(cell.x - minX, cell.y - minY))
                .Distinct()
                .OrderBy(cell => cell.x)
                .ThenBy(cell => cell.y);
        }

        private static bool IsAnyL3Cell(IReadOnlyCollection<Vector2Int> cells)
        {
            return cells.All(cell => cell.x is 0 or 1 && cell.y is 0 or 1);
        }

        private static bool Contains(IEnumerable<Vector2Int> cells, int x, int y)
        {
            return cells.Any(cell => cell.x == x && cell.y == y);
        }
    }
}
