using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    public enum RoomDesignerFootprintPreset
    {
        Single1x1,
        Wide2x1,
        Tall1x2,
        Block2x2,
        L3Cell
    }

    public static class RoomDesignerFootprintUtility
    {
        public const int ChunkWidthTiles = 13;
        public const int ChunkHeightTiles = 7;

        private static readonly string[] DirectionOrder = { "north", "south", "east", "west" };

        public static IReadOnlyList<Vector2Int> OccupiedCells(RoomDesignerFootprintPreset preset)
        {
            return preset switch
            {
                RoomDesignerFootprintPreset.Wide2x1 => new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
                RoomDesignerFootprintPreset.Tall1x2 => new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) },
                RoomDesignerFootprintPreset.Block2x2 => new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                RoomDesignerFootprintPreset.L3Cell => new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
                _ => new[] { new Vector2Int(0, 0) }
            };
        }

        public static Vector2Int Dimensions(RoomDesignerFootprintPreset preset)
        {
            var occupied = OccupiedCells(preset);
            var minX = occupied.Min(cell => cell.x);
            var maxX = occupied.Max(cell => cell.x);
            var minZ = occupied.Min(cell => cell.y);
            var maxZ = occupied.Max(cell => cell.y);
            return new Vector2Int((maxX - minX + 1) * ChunkWidthTiles, (maxZ - minZ + 1) * ChunkHeightTiles);
        }

        public static void RoomBounds(RoomDesignerFootprintPreset preset, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            var dimensions = Dimensions(preset);
            minX = -dimensions.x * 0.5f;
            maxX = dimensions.x * 0.5f;
            minZ = -dimensions.y * 0.5f;
            maxZ = dimensions.y * 0.5f;
        }

        public static void TileCoordinateBounds(RoomDesignerFootprintPreset preset, out int minX, out int maxX, out int minZ, out int maxZ)
        {
            var dimensions = Dimensions(preset);
            minX = -dimensions.x / 2;
            maxX = minX + dimensions.x - 1;
            minZ = -dimensions.y / 2;
            maxZ = minZ + dimensions.y - 1;
        }

        public static bool ContainsTile(RoomDesignerFootprintPreset preset, int x, int z)
        {
            var occupied = OccupiedCells(preset);
            var minCellX = occupied.Min(cell => cell.x);
            var minCellZ = occupied.Min(cell => cell.y);
            TileCoordinateBounds(preset, out var minX, out _, out var minZ, out _);
            foreach (var cell in occupied)
            {
                var xStart = minX + (cell.x - minCellX) * ChunkWidthTiles;
                var zStart = minZ + (cell.y - minCellZ) * ChunkHeightTiles;
                if (x >= xStart && x < xStart + ChunkWidthTiles && z >= zStart && z < zStart + ChunkHeightTiles)
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<Vector2Int> GroundTiles(RoomDesignerFootprintPreset preset)
        {
            var occupied = OccupiedCells(preset);
            var minCellX = occupied.Min(cell => cell.x);
            var minCellZ = occupied.Min(cell => cell.y);
            TileCoordinateBounds(preset, out var minX, out _, out var minZ, out _);
            foreach (var cell in occupied)
            {
                var xStart = minX + (cell.x - minCellX) * ChunkWidthTiles;
                var zStart = minZ + (cell.y - minCellZ) * ChunkHeightTiles;
                for (var z = zStart; z < zStart + ChunkHeightTiles; z++)
                {
                    for (var x = xStart; x < xStart + ChunkWidthTiles; x++)
                    {
                        yield return new Vector2Int(x, z);
                    }
                }
            }
        }

        public static Vector2Int NearestContainedTile(RoomDesignerFootprintPreset preset, int targetX, int targetZ)
        {
            return GroundTiles(preset)
                .OrderBy(tile => (tile.x - targetX) * (tile.x - targetX) + (tile.y - targetZ) * (tile.y - targetZ))
                .ThenBy(tile => tile.y)
                .ThenBy(tile => tile.x)
                .FirstOrDefault();
        }

        public static IEnumerable<RoomDesignerDoorPortState> CreateAvailableDoorPorts(RoomDesignerFootprintPreset preset)
        {
            var occupied = OccupiedCells(preset);
            var occupiedSet = new HashSet<Vector2Int>(occupied);
            foreach (var direction in DirectionOrder)
            {
                var ports = new List<(Vector2Int hostCell, Vector2 position)>();
                foreach (var cell in occupied)
                {
                    if (occupiedSet.Contains(cell + DirectionOffsetInt(direction)))
                    {
                        continue;
                    }

                    ports.Add((cell, DoorPositionFor(preset, cell, direction)));
                }

                var sorted = direction is "north" or "south"
                    ? ports.OrderBy(port => port.position.x).ToList()
                    : ports.OrderBy(port => port.position.y).ToList();
                for (var index = 0; index < sorted.Count; index++)
                {
                    var port = sorted[index];
                    yield return RoomDesignerDoorPortState.Create(
                        direction,
                        index,
                        port.position.x,
                        port.position.y,
                        RoomDesignerDoorKinds.Available,
                        port.hostCell.x,
                        port.hostCell.y);
                }
            }
        }

        public static Vector2 ChunkCenter(RoomDesignerFootprintPreset preset, Vector2Int cell)
        {
            var occupied = OccupiedCells(preset);
            var minCellX = occupied.Min(candidate => candidate.x);
            var minCellZ = occupied.Min(candidate => candidate.y);
            RoomBounds(preset, out var minX, out _, out var minZ, out _);
            return new Vector2(
                minX + (cell.x - minCellX + 0.5f) * ChunkWidthTiles,
                minZ + (cell.y - minCellZ + 0.5f) * ChunkHeightTiles);
        }

        public static Vector2 DirectionOffset(string direction)
        {
            return direction switch
            {
                "north" => new Vector2(0f, -1f),
                "south" => new Vector2(0f, 1f),
                "east" => new Vector2(1f, 0f),
                "west" => new Vector2(-1f, 0f),
                _ => Vector2.zero
            };
        }

        public static Vector2Int DirectionOffsetInt(string direction)
        {
            return direction switch
            {
                "north" => new Vector2Int(0, -1),
                "south" => new Vector2Int(0, 1),
                "east" => new Vector2Int(1, 0),
                "west" => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }

        public static string DisplayName(RoomDesignerFootprintPreset preset)
        {
            return preset switch
            {
                RoomDesignerFootprintPreset.Wide2x1 => "Designer Draft 2x1 Wide",
                RoomDesignerFootprintPreset.Tall1x2 => "Designer Draft 1x2 Tall",
                RoomDesignerFootprintPreset.Block2x2 => "Designer Draft 2x2 Block",
                RoomDesignerFootprintPreset.L3Cell => "Designer Draft L 3-Cell",
                _ => "Designer Draft 13x7"
            };
        }

        private static Vector2 DoorPositionFor(RoomDesignerFootprintPreset preset, Vector2Int hostCell, string direction)
        {
            var center = ChunkCenter(preset, hostCell);
            return direction switch
            {
                "north" => new Vector2(center.x, center.y - ChunkHeightTiles * 0.5f),
                "south" => new Vector2(center.x, center.y + ChunkHeightTiles * 0.5f),
                "east" => new Vector2(center.x + ChunkWidthTiles * 0.5f, center.y),
                "west" => new Vector2(center.x - ChunkWidthTiles * 0.5f, center.y),
                _ => center
            };
        }
    }
}
