using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Rooms
{
    public static class HollowRuntimeV2Importer
    {
        public const int SupportedSchemaVersion = 2;

        public static bool TryImport(string json, out ImportedRoomRuntimeAsset asset, out string error)
        {
            try
            {
                asset = Import(json);
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                asset = null;
                error = exception.Message;
                return false;
            }
        }

        public static ImportedRoomRuntimeAsset Import(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("HollowRuntime V2 import failed: JSON payload is empty.");
            }

            if (!json.Contains("\"hollowRuntime\"", StringComparison.Ordinal))
            {
                throw new ArgumentException("HollowRuntime V2 import failed: missing hollowRuntime block.");
            }

            var manifest = JsonUtility.FromJson<ImportedHollowRoomManifest>(json);
            if (manifest?.hollowRuntime == null)
            {
                throw new ArgumentException("HollowRuntime V2 import failed: missing hollowRuntime block.");
            }

            var runtime = manifest.hollowRuntime;
            if (runtime.schemaVersion != SupportedSchemaVersion)
            {
                throw new ArgumentException($"HollowRuntime V2 import failed: unsupported schemaVersion {runtime.schemaVersion}.");
            }

            var layout = BuildLayout(runtime);
            var footprint = BuildFootprint(runtime);
            var doorPorts = BuildDoorPorts(runtime);
            ValidateDoorPorts(footprint, doorPorts);
            var safeStart = new ImportedSpawnPoint
            {
                id = "spawn_point_safeStart",
                kind = "spawn_point_safeStart",
                position = runtime.playerSafeStart
            };

            return new ImportedRoomRuntimeAsset(
                runtime.canonicalRoomId,
                runtime.displayName,
                layout,
                footprint,
                doorPorts,
                runtime.enemySpawns ?? new List<ImportedSpawnPoint>(),
                runtime.itemSpawns ?? new List<ImportedSpawnPoint>(),
                safeStart,
                runtime.hazards ?? new List<ImportedRoomHazard>(),
                runtime.interactiveObjects ?? new List<ImportedRoomInteractiveObject>(),
                runtime.decor ?? new List<ImportedRoomDecor>(),
                manifest);
        }

        private static RoomInstanceFootprint BuildFootprint(ImportedHollowRuntime runtime)
        {
            var source = runtime.footprint;
            var primaryCell = new Vector2Int(source?.primaryCell?.x ?? 0, source?.primaryCell?.z ?? 0);
            var occupiedCells = new List<Vector2Int>();
            foreach (var cell in source?.occupiedBranchCells ?? new List<ImportedGridPosition>())
            {
                occupiedCells.Add(new Vector2Int(cell.x, cell.z));
            }

            if (occupiedCells.Count == 0)
            {
                occupiedCells.Add(primaryCell);
            }

            var chunkBasis = new Vector2Int(source?.chunkBasisTiles?.width ?? 13, source?.chunkBasisTiles?.height ?? 7);
            return new RoomInstanceFootprint(primaryCell, occupiedCells, chunkBasis);
        }

        private static RoomLayout BuildLayout(ImportedHollowRuntime runtime)
        {
            var bounds = runtime.dimensions?.bounds ?? new ImportedRoomBounds
            {
                minX = -RoomRuntimeRoot.DefaultWidthMeters * 0.5f,
                maxX = RoomRuntimeRoot.DefaultWidthMeters * 0.5f,
                minZ = -RoomRuntimeRoot.DefaultDepthMeters * 0.5f,
                maxZ = RoomRuntimeRoot.DefaultDepthMeters * 0.5f
            };

            var walkableTiles = new List<Vector2Int>();
            foreach (var tile in runtime.walkableTiles ?? new List<ImportedGridPosition>())
            {
                walkableTiles.Add(new Vector2Int(tile.x, tile.z));
            }

            var holeTiles = new List<Vector2Int>();
            foreach (var tile in runtime.holeTiles ?? new List<ImportedGridPosition>())
            {
                holeTiles.Add(new Vector2Int(tile.x, tile.z));
            }

            var floorRegions = new List<RoomLayoutFloorRegion>();
            foreach (var region in runtime.floorRegions ?? new List<ImportedRoomFloorRegion>())
            {
                var center = region.center?.ToUnityVector3() ?? Vector3.zero;
                var halfSize = region.halfSize != null
                    ? new Vector2(region.halfSize.x, region.halfSize.z)
                    : new Vector2((bounds.maxX - bounds.minX) * 0.5f, (bounds.maxZ - bounds.minZ) * 0.5f);
                floorRegions.Add(new RoomLayoutFloorRegion(region.id, center, halfSize));
            }

            if (floorRegions.Count == 0)
            {
                floorRegions.Add(new RoomLayoutFloorRegion(
                    "derived_full_floor",
                    new Vector3((bounds.minX + bounds.maxX) * 0.5f, 0f, (bounds.minZ + bounds.maxZ) * 0.5f),
                    new Vector2((bounds.maxX - bounds.minX) * 0.5f, (bounds.maxZ - bounds.minZ) * 0.5f)));
            }

            var obstacles = new List<RoomLayoutObstacle>();
            foreach (var obstacle in runtime.obstacles ?? new List<ImportedRoomObstacle>())
            {
                obstacles.Add(new RoomLayoutObstacle(
                    obstacle.id,
                    obstacle.kind,
                    obstacle.center?.ToUnityVector3() ?? Vector3.zero,
                    obstacle.size?.ToUnityVector3() ?? Vector3.one,
                    obstacle.blocksProjectiles));
            }

            var widthTiles = runtime.dimensions?.widthTiles ?? (int)RoomRuntimeRoot.DefaultWidthMeters;
            var heightTiles = runtime.dimensions?.heightTiles ?? (int)RoomRuntimeRoot.DefaultDepthMeters;

            return new RoomLayout(
                widthTiles,
                heightTiles,
                Rect.MinMaxRect(bounds.minX, bounds.minZ, bounds.maxX, bounds.maxZ),
                walkableTiles,
                holeTiles,
                floorRegions,
                obstacles);
        }

        private static IReadOnlyList<RoomDoorPort> BuildDoorPorts(ImportedHollowRuntime runtime)
        {
            var ports = new List<RoomDoorPort>();
            foreach (var port in runtime.doorPorts ?? new List<ImportedRoomDoorPort>())
            {
                ports.Add(new RoomDoorPort(
                    port.id,
                    port.direction,
                    port.laneIndex,
                    new Vector2Int(port.hostCell?.x ?? 0, port.hostCell?.z ?? 0),
                    new Vector2(port.gridEdgeCenter?.x ?? 0f, port.gridEdgeCenter?.z ?? 0f),
                    port.positionMeters?.ToUnityVector3() ?? Vector3.zero,
                    port.kind));
            }

            return ports;
        }

        private static void ValidateDoorPorts(RoomInstanceFootprint footprint, IReadOnlyList<RoomDoorPort> doorPorts)
        {
            if (footprint == null)
            {
                throw new ArgumentException("HollowRuntime V2 import failed: missing room footprint.");
            }

            var expectedPortCount = ExpectedExposedPortCount(footprint);
            if (doorPorts.Count == 0 || doorPorts.Count > expectedPortCount)
            {
                throw new ArgumentException($"HollowRuntime V2 import failed: footprint exposes {expectedPortCount} ports but payload contains {doorPorts.Count} enabled ports.");
            }

            var seenIds = new HashSet<string>();
            foreach (var port in doorPorts)
            {
                if (!seenIds.Add(port.Id))
                {
                    throw new ArgumentException($"HollowRuntime V2 import failed: duplicate door port id {port.Id}.");
                }

                if (!footprint.ContainsCell(port.HostCell))
                {
                    throw new ArgumentException($"HollowRuntime V2 import failed: door port {port.Id} is hosted by an unoccupied cell {port.HostCell}.");
                }

                var adjacent = port.HostCell + DirectionOffset(port.Direction);
                if (footprint.ContainsCell(adjacent))
                {
                    throw new ArgumentException($"HollowRuntime V2 import failed: door port {port.Id} is on an internal occupied-cell seam.");
                }
            }
        }

        public static int ExpectedExposedPortCount(RoomInstanceFootprint footprint)
        {
            if (footprint == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var cell in footprint.OccupiedCells)
            {
                if (!footprint.ContainsCell(cell + Vector2Int.up))
                {
                    count++;
                }

                if (!footprint.ContainsCell(cell + Vector2Int.down))
                {
                    count++;
                }

                if (!footprint.ContainsCell(cell + Vector2Int.left))
                {
                    count++;
                }

                if (!footprint.ContainsCell(cell + Vector2Int.right))
                {
                    count++;
                }
            }

            return count;
        }

        private static Vector2Int DirectionOffset(string direction)
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
    }
}
