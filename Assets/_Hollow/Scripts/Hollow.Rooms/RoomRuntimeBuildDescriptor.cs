using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomRuntimeBuildDescriptor
    {
        internal RoomRuntimeBuildDescriptor(ImportedRoomRuntimeAsset asset)
        {
            AssetId = asset?.Id ?? string.Empty;
            BiomeId = RoomBiomeIds.Normalize(asset?.BiomeId);
            Layout = CloneLayout(asset?.Layout);
            Footprint = CloneFootprint(asset?.Footprint);
            DoorPorts = CloneDoorPorts(asset?.DoorPorts);
            Hazards = CloneHazards(asset?.Hazards);
            InteractiveObjects = CloneInteractiveObjects(asset?.InteractiveObjects);
            Decor = CloneDecor(asset?.Decor);
            EnemySpawns = CloneSpawnPoints(asset?.EnemySpawns);
            ItemSpawns = CloneSpawnPoints(asset?.ItemSpawns);
            SafeStart = CloneSpawnPoint(asset?.SafeStart);
            Asset = new ImportedRoomRuntimeAsset(
                AssetId,
                asset?.DisplayName ?? AssetId,
                BiomeId,
                Layout,
                Footprint,
                DoorPorts,
                EnemySpawns,
                ItemSpawns,
                SafeStart,
                Hazards,
                InteractiveObjects,
                Decor,
                asset?.SourceManifest);
        }

        public ImportedRoomRuntimeAsset Asset { get; }

        public string AssetId { get; }

        public string BiomeId { get; }

        public RoomLayout Layout { get; }

        public RoomInstanceFootprint Footprint { get; }

        public IReadOnlyList<RoomDoorPort> DoorPorts { get; }

        public IReadOnlyList<ImportedRoomHazard> Hazards { get; }

        public IReadOnlyList<ImportedRoomInteractiveObject> InteractiveObjects { get; }

        public IReadOnlyList<ImportedRoomDecor> Decor { get; }

        public IReadOnlyList<ImportedSpawnPoint> EnemySpawns { get; }

        public IReadOnlyList<ImportedSpawnPoint> ItemSpawns { get; }

        public ImportedSpawnPoint SafeStart { get; }

        private static RoomLayout CloneLayout(RoomLayout layout)
        {
            if (layout == null)
            {
                return null;
            }

            return new RoomLayout(
                layout.WidthTiles,
                layout.HeightTiles,
                layout.Bounds,
                CloneVector2IntList(layout.WalkableTiles),
                CloneVector2IntList(layout.HoleTiles),
                CloneFloorRegions(layout.FloorRegions),
                CloneObstacles(layout.Obstacles));
        }

        private static RoomInstanceFootprint CloneFootprint(RoomInstanceFootprint footprint)
        {
            return footprint == null
                ? null
                : new RoomInstanceFootprint(footprint.PrimaryCell, CloneVector2IntCollection(footprint.OccupiedCells), footprint.ChunkBasisTiles);
        }

        private static Vector2Int[] CloneVector2IntList(IReadOnlyList<Vector2Int> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<Vector2Int>();
            }

            var clone = new Vector2Int[values.Count];
            for (var index = 0; index < values.Count; index++)
            {
                clone[index] = values[index];
            }

            return clone;
        }

        private static Vector2Int[] CloneVector2IntCollection(IReadOnlyCollection<Vector2Int> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<Vector2Int>();
            }

            var clone = new Vector2Int[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                clone[index++] = value;
            }

            return clone;
        }

        private static RoomLayoutFloorRegion[] CloneFloorRegions(IReadOnlyList<RoomLayoutFloorRegion> regions)
        {
            if (regions == null || regions.Count == 0)
            {
                return Array.Empty<RoomLayoutFloorRegion>();
            }

            var clone = new RoomLayoutFloorRegion[regions.Count];
            for (var index = 0; index < regions.Count; index++)
            {
                var region = regions[index];
                clone[index] = region == null ? null : new RoomLayoutFloorRegion(region.Id, region.Center, region.HalfSize);
            }

            return clone;
        }

        private static RoomLayoutObstacle[] CloneObstacles(IReadOnlyList<RoomLayoutObstacle> obstacles)
        {
            if (obstacles == null || obstacles.Count == 0)
            {
                return Array.Empty<RoomLayoutObstacle>();
            }

            var clone = new RoomLayoutObstacle[obstacles.Count];
            for (var index = 0; index < obstacles.Count; index++)
            {
                var obstacle = obstacles[index];
                clone[index] = obstacle == null
                    ? null
                    : new RoomLayoutObstacle(obstacle.Id, obstacle.Kind, obstacle.Center, obstacle.Size, obstacle.BlocksProjectiles);
            }

            return clone;
        }

        private static RoomDoorPort[] CloneDoorPorts(IReadOnlyList<RoomDoorPort> ports)
        {
            if (ports == null || ports.Count == 0)
            {
                return Array.Empty<RoomDoorPort>();
            }

            var clone = new RoomDoorPort[ports.Count];
            for (var index = 0; index < ports.Count; index++)
            {
                var port = ports[index];
                clone[index] = port == null
                    ? null
                    : new RoomDoorPort(port.Id, port.Direction, port.LaneIndex, port.HostCell, port.GridEdgeCenter, port.Position, port.Kind);
            }

            return clone;
        }

        private static ImportedRoomHazard[] CloneHazards(IReadOnlyList<ImportedRoomHazard> hazards)
        {
            if (hazards == null || hazards.Count == 0)
            {
                return Array.Empty<ImportedRoomHazard>();
            }

            var clone = new ImportedRoomHazard[hazards.Count];
            for (var index = 0; index < hazards.Count; index++)
            {
                var hazard = hazards[index];
                clone[index] = hazard == null
                    ? null
                    : new ImportedRoomHazard
                    {
                        id = hazard.id,
                        kind = hazard.kind,
                        center = CloneVector3(hazard.center),
                        radius = hazard.radius
                    };
            }

            return clone;
        }

        private static ImportedRoomInteractiveObject[] CloneInteractiveObjects(IReadOnlyList<ImportedRoomInteractiveObject> objects)
        {
            if (objects == null || objects.Count == 0)
            {
                return Array.Empty<ImportedRoomInteractiveObject>();
            }

            var clone = new ImportedRoomInteractiveObject[objects.Count];
            for (var index = 0; index < objects.Count; index++)
            {
                var roomObject = objects[index];
                clone[index] = roomObject == null
                    ? null
                    : new ImportedRoomInteractiveObject
                    {
                        id = roomObject.id,
                        kind = roomObject.kind,
                        center = CloneVector3(roomObject.center),
                        size = CloneVector3(roomObject.size),
                        blocksMovement = roomObject.blocksMovement,
                        blocksProjectiles = roomObject.blocksProjectiles
                    };
            }

            return clone;
        }

        private static ImportedRoomDecor[] CloneDecor(IReadOnlyList<ImportedRoomDecor> decor)
        {
            if (decor == null || decor.Count == 0)
            {
                return Array.Empty<ImportedRoomDecor>();
            }

            var clone = new ImportedRoomDecor[decor.Count];
            for (var index = 0; index < decor.Count; index++)
            {
                var item = decor[index];
                clone[index] = item == null
                    ? null
                    : new ImportedRoomDecor
                    {
                        id = item.id,
                        kind = item.kind,
                        center = CloneVector3(item.center),
                        size = CloneVector3(item.size),
                        blocking = item.blocking,
                        blocksProjectiles = item.blocksProjectiles
                    };
            }

            return clone;
        }

        private static ImportedSpawnPoint[] CloneSpawnPoints(IReadOnlyList<ImportedSpawnPoint> spawns)
        {
            if (spawns == null || spawns.Count == 0)
            {
                return Array.Empty<ImportedSpawnPoint>();
            }

            var clone = new ImportedSpawnPoint[spawns.Count];
            for (var index = 0; index < spawns.Count; index++)
            {
                clone[index] = CloneSpawnPoint(spawns[index]);
            }

            return clone;
        }

        private static ImportedSpawnPoint CloneSpawnPoint(ImportedSpawnPoint spawn)
        {
            return spawn == null
                ? null
                : new ImportedSpawnPoint
                {
                    id = spawn.id,
                    kind = spawn.kind,
                    position = CloneVector3(spawn.position)
                };
        }

        private static ImportedVector3 CloneVector3(ImportedVector3 value)
        {
            return value == null
                ? null
                : new ImportedVector3
                {
                    x = value.x,
                    y = value.y,
                    z = value.z
                };
        }
    }

    public static class RoomRuntimeDescriptorCache
    {
        private const int DescriptorSchemaVersion = 2;
        private static readonly Dictionary<string, RoomRuntimeBuildDescriptor> Descriptors = new(StringComparer.Ordinal);
        private static readonly List<string> Lru = new();

        public static RoomRuntimeBuildDescriptor GetOrCreate(ImportedRoomRuntimeAsset asset)
        {
            if (asset == null)
            {
                return null;
            }

            var key = DescriptorKey(asset);
            if (Descriptors.TryGetValue(key, out var descriptor) && descriptor != null)
            {
                Touch(key);
                M136PerformanceOperationCounters.ReportBranchRuntimeCacheHit();
                return descriptor;
            }

            M136PerformanceOperationCounters.ReportBranchRuntimeCacheMiss("descriptor", key, asset.DisplayName);
            M136PerformanceOperationCounters.ReportRoomDescriptorBuild();
            descriptor = new RoomRuntimeBuildDescriptor(asset);
            Descriptors[key] = descriptor;
            Touch(key);
            Trim();
            return descriptor;
        }

        public static void Clear()
        {
            if (Descriptors.Count > 0)
            {
                M136PerformanceOperationCounters.ReportBranchRuntimeCacheEviction();
            }

            Descriptors.Clear();
            Lru.Clear();
        }

        public static int Count => Descriptors.Count;

        public static int MaxDescriptors => HollowRuntimeCachePolicy.Default.MaxRoomDescriptorEntries;

        private static string DescriptorKey(ImportedRoomRuntimeAsset asset)
        {
            var layout = asset.Layout;
            var builder = new StringBuilder(512);
            builder
                .Append("room-desc-v").Append(DescriptorSchemaVersion)
                .Append("|id:").Append(asset.Id ?? string.Empty)
                .Append("|biome:").Append(RoomBiomeIds.Normalize(asset.BiomeId))
                .Append("|schema:").Append(asset.SourceManifest?.hollowRuntime?.schemaVersion ?? 0);
            AppendLayoutSignature(builder, layout);
            AppendFootprintSignature(builder, asset.Footprint);
            AppendDoorSignature(builder, asset.DoorPorts);
            AppendSpawnSignature(builder, "enemy", asset.EnemySpawns);
            AppendSpawnSignature(builder, "item", asset.ItemSpawns);
            AppendSpawnSignature(builder, "safe", asset.SafeStart);
            AppendHazardSignature(builder, asset.Hazards);
            AppendInteractiveObjectSignature(builder, asset.InteractiveObjects);
            AppendDecorSignature(builder, asset.Decor);
            return builder.ToString();
        }

        private static void AppendLayoutSignature(StringBuilder builder, RoomLayout layout)
        {
            if (layout == null)
            {
                builder.Append("|layout:null");
                return;
            }

            builder
                .Append("|layout:")
                .Append(layout.WidthTiles)
                .Append('x')
                .Append(layout.HeightTiles)
                .Append(":bounds:");
            AppendRect(builder, layout.Bounds);
            AppendVector2IntSignature(builder, ":walkable", layout.WalkableTiles);
            AppendVector2IntSignature(builder, ":holes", layout.HoleTiles);
            AppendFloorRegionSignature(builder, layout.FloorRegions);
            AppendObstacleSignature(builder, layout.Obstacles);
        }

        private static void AppendFootprintSignature(StringBuilder builder, RoomInstanceFootprint footprint)
        {
            if (footprint == null)
            {
                builder.Append("|footprint:null");
                return;
            }

            builder.Append("|footprint:primary:");
            AppendVector2Int(builder, footprint.PrimaryCell);
            builder.Append(":basis:");
            AppendVector2Int(builder, footprint.ChunkBasisTiles);
            builder.Append(":occupied:").Append(footprint.OccupiedCells?.Count ?? 0);
            if (footprint.OccupiedCells == null)
            {
                return;
            }

            foreach (var cell in footprint.OccupiedCells)
            {
                builder.Append(';');
                AppendVector2Int(builder, cell);
            }
        }

        private static void AppendVector2IntSignature(StringBuilder builder, string label, IReadOnlyList<Vector2Int> values)
        {
            builder.Append(label).Append(':').Append(values?.Count ?? 0);
            if (values == null)
            {
                return;
            }

            for (var index = 0; index < values.Count; index++)
            {
                builder.Append(';');
                AppendVector2Int(builder, values[index]);
            }
        }

        private static void AppendFloorRegionSignature(StringBuilder builder, IReadOnlyList<RoomLayoutFloorRegion> regions)
        {
            builder.Append(":floor:").Append(regions?.Count ?? 0);
            if (regions == null)
            {
                return;
            }

            for (var index = 0; index < regions.Count; index++)
            {
                var region = regions[index];
                builder.Append(';');
                if (region == null)
                {
                    builder.Append("null");
                    continue;
                }

                AppendString(builder, region.Id);
                builder.Append(',');
                AppendVector3(builder, region.Center);
                builder.Append(',');
                AppendVector2(builder, region.HalfSize);
            }
        }

        private static void AppendObstacleSignature(StringBuilder builder, IReadOnlyList<RoomLayoutObstacle> obstacles)
        {
            builder.Append(":obstacles:").Append(obstacles?.Count ?? 0);
            if (obstacles == null)
            {
                return;
            }

            for (var index = 0; index < obstacles.Count; index++)
            {
                var obstacle = obstacles[index];
                builder.Append(';');
                if (obstacle == null)
                {
                    builder.Append("null");
                    continue;
                }

                AppendString(builder, obstacle.Id);
                builder.Append(',');
                AppendString(builder, obstacle.Kind);
                builder.Append(',');
                AppendVector3(builder, obstacle.Center);
                builder.Append(',');
                AppendVector3(builder, obstacle.Size);
                builder.Append(',').Append(obstacle.BlocksProjectiles ? '1' : '0');
            }
        }

        private static void AppendDoorSignature(StringBuilder builder, IReadOnlyList<RoomDoorPort> ports)
        {
            builder.Append("|doors:").Append(ports?.Count ?? 0);
            if (ports == null)
            {
                return;
            }

            for (var index = 0; index < ports.Count; index++)
            {
                var port = ports[index];
                builder.Append(';');
                if (port == null)
                {
                    builder.Append("null");
                    continue;
                }

                AppendString(builder, port.Id);
                builder.Append(',');
                AppendString(builder, port.Direction);
                builder.Append(',').Append(port.LaneIndex).Append(',');
                AppendVector2Int(builder, port.HostCell);
                builder.Append(',');
                AppendVector2(builder, port.GridEdgeCenter);
                builder.Append(',');
                AppendVector3(builder, port.Position);
                builder.Append(',');
                AppendString(builder, port.Kind);
            }
        }

        private static void AppendSpawnSignature(StringBuilder builder, string label, IReadOnlyList<ImportedSpawnPoint> spawns)
        {
            builder.Append('|').Append(label).Append("Spawns:").Append(spawns?.Count ?? 0);
            if (spawns == null)
            {
                return;
            }

            for (var index = 0; index < spawns.Count; index++)
            {
                AppendSpawnSignature(builder, spawns[index]);
            }
        }

        private static void AppendSpawnSignature(StringBuilder builder, string label, ImportedSpawnPoint spawn)
        {
            builder.Append('|').Append(label).Append("Spawn:");
            AppendSpawnSignature(builder, spawn);
        }

        private static void AppendSpawnSignature(StringBuilder builder, ImportedSpawnPoint spawn)
        {
            builder.Append(';');
            if (spawn == null)
            {
                builder.Append("null");
                return;
            }

            AppendString(builder, spawn.id);
            builder.Append(',');
            AppendString(builder, spawn.kind);
            builder.Append(',');
            AppendImportedVector3(builder, spawn.position);
        }

        private static void AppendHazardSignature(StringBuilder builder, IReadOnlyList<ImportedRoomHazard> hazards)
        {
            builder.Append("|hazards:").Append(hazards?.Count ?? 0);
            if (hazards == null)
            {
                return;
            }

            for (var index = 0; index < hazards.Count; index++)
            {
                var hazard = hazards[index];
                builder.Append(';');
                if (hazard == null)
                {
                    builder.Append("null");
                    continue;
                }

                AppendString(builder, hazard.id);
                builder.Append(',');
                AppendString(builder, hazard.kind);
                builder.Append(',');
                AppendImportedVector3(builder, hazard.center);
                builder.Append(',');
                AppendFloat(builder, hazard.radius);
            }
        }

        private static void AppendInteractiveObjectSignature(StringBuilder builder, IReadOnlyList<ImportedRoomInteractiveObject> objects)
        {
            builder.Append("|objects:").Append(objects?.Count ?? 0);
            if (objects == null)
            {
                return;
            }

            for (var index = 0; index < objects.Count; index++)
            {
                var roomObject = objects[index];
                builder.Append(';');
                if (roomObject == null)
                {
                    builder.Append("null");
                    continue;
                }

                AppendString(builder, roomObject.id);
                builder.Append(',');
                AppendString(builder, roomObject.kind);
                builder.Append(',');
                AppendImportedVector3(builder, roomObject.center);
                builder.Append(',');
                AppendImportedVector3(builder, roomObject.size);
                builder.Append(',').Append(roomObject.blocksMovement ? '1' : '0');
                builder.Append(',').Append(roomObject.blocksProjectiles ? '1' : '0');
            }
        }

        private static void AppendDecorSignature(StringBuilder builder, IReadOnlyList<ImportedRoomDecor> decor)
        {
            builder.Append("|decor:").Append(decor?.Count ?? 0);
            if (decor == null)
            {
                return;
            }

            for (var index = 0; index < decor.Count; index++)
            {
                var item = decor[index];
                builder.Append(';');
                if (item == null)
                {
                    builder.Append("null");
                    continue;
                }

                AppendString(builder, item.id);
                builder.Append(',');
                AppendString(builder, item.kind);
                builder.Append(',');
                AppendImportedVector3(builder, item.center);
                builder.Append(',');
                AppendImportedVector3(builder, item.size);
                builder.Append(',').Append(item.blocking ? '1' : '0');
                builder.Append(',').Append(item.blocksProjectiles ? '1' : '0');
            }
        }

        private static void AppendRect(StringBuilder builder, Rect rect)
        {
            AppendFloat(builder, rect.xMin);
            builder.Append(',');
            AppendFloat(builder, rect.yMin);
            builder.Append(',');
            AppendFloat(builder, rect.xMax);
            builder.Append(',');
            AppendFloat(builder, rect.yMax);
        }

        private static void AppendVector2Int(StringBuilder builder, Vector2Int value)
        {
            builder.Append(value.x).Append(',').Append(value.y);
        }

        private static void AppendVector2(StringBuilder builder, Vector2 value)
        {
            AppendFloat(builder, value.x);
            builder.Append(',');
            AppendFloat(builder, value.y);
        }

        private static void AppendVector3(StringBuilder builder, Vector3 value)
        {
            AppendFloat(builder, value.x);
            builder.Append(',');
            AppendFloat(builder, value.y);
            builder.Append(',');
            AppendFloat(builder, value.z);
        }

        private static void AppendImportedVector3(StringBuilder builder, ImportedVector3 value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            AppendFloat(builder, value.x);
            builder.Append(',');
            AppendFloat(builder, value.y);
            builder.Append(',');
            AppendFloat(builder, value.z);
        }

        private static void AppendFloat(StringBuilder builder, float value)
        {
            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void AppendString(StringBuilder builder, string value)
        {
            builder.Append(value ?? string.Empty);
        }

        private static void Touch(string key)
        {
            Lru.Remove(key);
            Lru.Add(key);
        }

        private static void Trim()
        {
            while (Descriptors.Count > MaxDescriptors && Lru.Count > 0)
            {
                var oldest = Lru[0];
                Lru.RemoveAt(0);
                if (Descriptors.Remove(oldest))
                {
                    M136PerformanceOperationCounters.ReportBranchRuntimeCacheEviction();
                }
            }
        }
    }
}
