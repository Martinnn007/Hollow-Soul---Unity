using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomInstanceFootprint
    {
        public RoomInstanceFootprint(Vector2Int primaryCell, IReadOnlyCollection<Vector2Int> occupiedCells, Vector2Int chunkBasisTiles)
        {
            PrimaryCell = primaryCell;
            OccupiedCells = occupiedCells != null && occupiedCells.Count > 0
                ? occupiedCells.ToArray()
                : new[] { primaryCell };
            ChunkBasisTiles = chunkBasisTiles.x > 0 && chunkBasisTiles.y > 0 ? chunkBasisTiles : new Vector2Int(13, 7);
        }

        public Vector2Int PrimaryCell { get; }

        public IReadOnlyCollection<Vector2Int> OccupiedCells { get; }

        public Vector2Int ChunkBasisTiles { get; }

        public int OccupiedCellCount => OccupiedCells.Count;

        public bool ContainsCell(Vector2Int cell)
        {
            return OccupiedCells.Contains(cell);
        }
    }
}
