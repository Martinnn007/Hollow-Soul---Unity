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
            var doorPorts = BuildDoorPorts(runtime);
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
                doorPorts,
                runtime.enemySpawns ?? new List<ImportedSpawnPoint>(),
                runtime.itemSpawns ?? new List<ImportedSpawnPoint>(),
                safeStart,
                runtime.decor ?? new List<ImportedRoomDecor>(),
                manifest);
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
    }
}
