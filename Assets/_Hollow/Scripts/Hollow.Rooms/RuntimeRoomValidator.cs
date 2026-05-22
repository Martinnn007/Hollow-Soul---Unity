using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Rooms
{
    public static class RuntimeRoomValidator
    {
        private const float FloorAlignmentTolerance = 0.001f;

        public static RuntimeRoomValidationReport Validate(ImportedRoomRuntimeAsset room)
        {
            var report = new RuntimeRoomValidationReport();
            if (room == null)
            {
                report.AddError("Room asset is missing.");
                return report;
            }

            if (room.Layout == null)
            {
                report.AddError($"Room '{room.Id}' is missing layout data.");
                return report;
            }

            var walkableTiles = new HashSet<Vector2Int>(room.Layout.WalkableTiles ?? System.Array.Empty<Vector2Int>());
            if (walkableTiles.Count == 0)
            {
                report.AddError($"Room '{room.Id}' has no walkable floor tiles.");
                return report;
            }

            var holeTiles = new HashSet<Vector2Int>(room.Layout.HoleTiles ?? System.Array.Empty<Vector2Int>());
            var blockingTiles = BlockingTileSet(room.Layout.Obstacles);
            var passableTiles = new HashSet<Vector2Int>(walkableTiles);
            passableTiles.ExceptWith(holeTiles);
            passableTiles.ExceptWith(blockingTiles);

            ValidateFloorAlignedObstacles(room, report);
            ValidateSpawn(room, "safe start", room.SafeStart?.position, walkableTiles, holeTiles, blockingTiles, passableTiles, report);
            foreach (var spawn in room.EnemySpawns ?? System.Array.Empty<ImportedSpawnPoint>())
            {
                ValidateSpawn(room, $"enemy spawn '{SpawnLabel(spawn)}'", spawn?.position, walkableTiles, holeTiles, blockingTiles, passableTiles, report);
            }

            foreach (var spawn in room.ItemSpawns ?? System.Array.Empty<ImportedSpawnPoint>())
            {
                ValidateSpawn(room, $"item spawn '{SpawnLabel(spawn)}'", spawn?.position, walkableTiles, holeTiles, blockingTiles, passableTiles, report);
            }

            ValidateConnectivity(room, passableTiles, report);
            return report;
        }

        private static void ValidateSpawn(
            ImportedRoomRuntimeAsset room,
            string label,
            ImportedVector3 position,
            HashSet<Vector2Int> walkableTiles,
            HashSet<Vector2Int> holeTiles,
            HashSet<Vector2Int> blockingTiles,
            HashSet<Vector2Int> passableTiles,
            RuntimeRoomValidationReport report)
        {
            if (position == null)
            {
                report.AddError($"Room '{room.Id}' {label} is missing a position.");
                return;
            }

            var tile = TileFor(position);
            if (!walkableTiles.Contains(tile))
            {
                report.AddError($"Room '{room.Id}' {label} at ({tile.x},{tile.y}) is not on a walkable tile.");
                return;
            }

            if (holeTiles.Contains(tile))
            {
                report.AddError($"Room '{room.Id}' {label} at ({tile.x},{tile.y}) is on a hole tile.");
            }

            if (blockingTiles.Contains(tile))
            {
                report.AddError($"Room '{room.Id}' {label} at ({tile.x},{tile.y}) overlaps a blocking obstacle.");
            }

            if (!passableTiles.Contains(tile))
            {
                report.AddError($"Room '{room.Id}' {label} at ({tile.x},{tile.y}) is not passable.");
            }
        }

        private static void ValidateConnectivity(ImportedRoomRuntimeAsset room, HashSet<Vector2Int> passableTiles, RuntimeRoomValidationReport report)
        {
            if (passableTiles.Count == 0)
            {
                report.AddError($"Room '{room.Id}' has no passable walkable tiles after holes and blockers.");
                return;
            }

            var visited = FloodFill(passableTiles.First(), passableTiles);
            if (visited.Count == passableTiles.Count)
            {
                return;
            }

            report.AddError($"Room '{room.Id}' has disconnected passable walkable tiles ({visited.Count}/{passableTiles.Count} reachable in the first island).");
        }

        private static void ValidateFloorAlignedObstacles(ImportedRoomRuntimeAsset room, RuntimeRoomValidationReport report)
        {
            foreach (var obstacle in room.Layout.Obstacles ?? System.Array.Empty<RoomLayoutObstacle>())
            {
                if (obstacle == null)
                {
                    continue;
                }

                var expectedY = Mathf.Max(0.05f, obstacle.Size.y) * 0.5f;
                if (Mathf.Abs(obstacle.Center.y - expectedY) > FloorAlignmentTolerance)
                {
                    report.AddError($"Room '{room.Id}' obstacle '{obstacle.Id}' center.y is {obstacle.Center.y:0.###}; expected {expectedY:0.###}.");
                }
            }
        }

        private static HashSet<Vector2Int> BlockingTileSet(IReadOnlyList<RoomLayoutObstacle> obstacles)
        {
            var result = new HashSet<Vector2Int>();
            foreach (var obstacle in obstacles ?? System.Array.Empty<RoomLayoutObstacle>())
            {
                if (obstacle != null)
                {
                    result.Add(TileFor(obstacle.Center));
                }
            }

            return result;
        }

        private static HashSet<Vector2Int> FloodFill(Vector2Int start, HashSet<Vector2Int> passableTiles)
        {
            var visited = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in Neighbors(current))
                {
                    if (!passableTiles.Contains(neighbor) || !visited.Add(neighbor))
                    {
                        continue;
                    }

                    queue.Enqueue(neighbor);
                }
            }

            return visited;
        }

        private static IEnumerable<Vector2Int> Neighbors(Vector2Int tile)
        {
            yield return new Vector2Int(tile.x + 1, tile.y);
            yield return new Vector2Int(tile.x - 1, tile.y);
            yield return new Vector2Int(tile.x, tile.y + 1);
            yield return new Vector2Int(tile.x, tile.y - 1);
        }

        private static Vector2Int TileFor(ImportedVector3 position)
        {
            return new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
        }

        private static Vector2Int TileFor(Vector3 position)
        {
            return new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
        }

        private static string SpawnLabel(ImportedSpawnPoint spawn)
        {
            if (spawn == null)
            {
                return "<missing>";
            }

            return string.IsNullOrWhiteSpace(spawn.id) ? spawn.kind : spawn.id;
        }
    }
}
