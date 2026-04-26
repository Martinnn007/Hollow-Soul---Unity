using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public static class BranchGenerator
    {
        public const string LegacyFiveRoomBranchId = "m7_five_room_cross";
        public const string MacroFixtureBranchId = "m14_macro_fixture_branch_v1";
        public const string SeededMacroBranchId = "m15_seeded_macro_branch_v1";
        public const int DefaultMacroFixtureSeed = 14001;
        public const int DefaultSeededMacroSeed = 15001;

        public static BranchFloorGraph CreateFiveRoomCross(ImportedRoomRuntimeAsset roomAsset)
        {
            var graph = new BranchFloorGraph(LegacyFiveRoomBranchId, 0);
            var roomAssetId = roomAsset?.Id ?? string.Empty;
            graph.AddRoom(new BranchRoomState(BranchRoomId.Origin, Vector2Int.zero, new BranchRoomInstanceId("origin"), roomAssetId, null, BranchRoomRole.Origin));
            graph.AddRoom(new BranchRoomState(BranchRoomId.North, new Vector2Int(0, -1), new BranchRoomInstanceId("north"), roomAssetId, null, BranchRoomRole.Reward));
            graph.AddRoom(new BranchRoomState(BranchRoomId.South, new Vector2Int(0, 1), new BranchRoomInstanceId("south"), roomAssetId, null, BranchRoomRole.Reward));
            graph.AddRoom(new BranchRoomState(BranchRoomId.East, new Vector2Int(1, 0), new BranchRoomInstanceId("east"), roomAssetId, null, BranchRoomRole.Reward));
            graph.AddRoom(new BranchRoomState(BranchRoomId.West, new Vector2Int(-1, 0), new BranchRoomInstanceId("west"), roomAssetId, null, BranchRoomRole.Reward));

            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.South, "south", "north");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.East, "east", "west");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.West, "west", "east");
            return graph;
        }

        public static BranchFloorGraph CreateMacroFixtureBranch(IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> roomPool, int seed)
        {
            var graph = new BranchFloorGraph(MacroFixtureBranchId, seed == 0 ? DefaultMacroFixtureSeed : seed);
            var origin = RequireRoom(roomPool, "combat_macro_single_1x1");
            var north = RequireRoom(roomPool, "combat_macro_tall_1x2");
            var south = RequireRoom(roomPool, "combat_macro_l_3cell");
            var east = RequireRoom(roomPool, "combat_macro_wide_2x1");
            var west = RequireRoom(roomPool, "combat_macro_block_2x2");

            graph.AddRoom(CreateRoom(BranchRoomId.Origin, Vector2Int.zero, origin, BranchRoomRole.Origin));
            graph.AddRoom(CreateRoom(BranchRoomId.North, new Vector2Int(0, -2), north, BranchRoomRole.Reward));
            graph.AddRoom(CreateRoom(BranchRoomId.South, new Vector2Int(0, 1), south, BranchRoomRole.Reward));
            graph.AddRoom(CreateRoom(BranchRoomId.East, new Vector2Int(1, 0), east, BranchRoomRole.Reward));
            graph.AddRoom(CreateRoom(BranchRoomId.West, new Vector2Int(-2, -1), west, BranchRoomRole.Reward));

            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south", "north_0", "south_0");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.South, "south", "north", "south_0", "north_0");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.East, "east", "west", "east_0", "west_0");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.West, "west", "east", "west_0", "east_1");
            return graph;
        }

        public static BranchFloorGraph CreateSeededMacroBranch(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed)
        {
            if (content == null || !content.HasMacroFixturePool)
            {
                throw new InvalidOperationException("Seeded macro branch generation requires a complete macro room pool.");
            }

            settings = settings != null ? settings : BranchGenerationSettingsDefinition.CreateRuntimeDefault();
            if (settings.AllowLoops)
            {
                throw new InvalidOperationException("M15 seeded macro branch generation does not support loops.");
            }

            var resolvedSeed = seed == 0 ? settings.DefaultSeed : seed;
            var random = new System.Random(resolvedSeed);
            var roomPool = content.MacroRoomPool;
            var fixturePool = content.FixtureRoomPool;
            var candidatesByShape = BuildCandidatesByShape(roomPool.Values);
            var fixtureIds = settings.AllowedFixtureIds
                .Where(id => fixturePool.ContainsKey(id))
                .Distinct()
                .OrderBy(id => id)
                .ToList();
            if (fixtureIds.Count == 0)
            {
                fixtureIds = fixturePool.Keys.OrderBy(id => id).ToList();
            }

            var targetRoomCount = Mathf.Max(2, settings.TargetRoomCount);
            var records = new List<PlacementRecord>();
            var usedPortsByTempIndex = new Dictionary<int, HashSet<string>>();
            var occupiedCells = new HashSet<Vector2Int>();

            var originFallback = RequireRoom(fixturePool, "combat_macro_single_1x1");
            var originAsset = ChooseCandidateForShape(candidatesByShape, originFallback, random);
            var origin = new PlacementRecord(0, originAsset, Vector2Int.zero, PlaceFootprint(originAsset.Footprint, Vector2Int.zero));
            records.Add(origin);
            usedPortsByTempIndex[0] = new HashSet<string>();
            RegisterCells(occupiedCells, origin.Footprint);

            for (var tempIndex = 1; tempIndex < targetRoomCount; tempIndex++)
            {
                if (!TryPlaceNextRecord(records, usedPortsByTempIndex, occupiedCells, fixturePool, candidatesByShape, fixtureIds, random, settings.MaxPlacementAttempts, tempIndex, out var record))
                {
                    throw new InvalidOperationException($"M15 seeded macro branch generation failed to place room {tempIndex} after {settings.MaxPlacementAttempts} attempts.");
                }

                records.Add(record);
                usedPortsByTempIndex[record.TempIndex] = new HashSet<string> { record.ToPortId };
                usedPortsByTempIndex[record.ParentTempIndex].Add(record.FromPortId);
                RegisterCells(occupiedCells, record.Footprint);
            }

            var bossTempIndex = settings.EnableBossLeaf ? SelectBossLeaf(records) : -1;
            var idByTempIndex = AssignRoomIds(records, bossTempIndex);
            var graph = new BranchFloorGraph(SeededMacroBranchId, resolvedSeed);

            foreach (var record in records)
            {
                var roomId = idByTempIndex[record.TempIndex];
                var role = record.TempIndex == 0
                    ? BranchRoomRole.Origin
                    : record.TempIndex == bossTempIndex
                        ? BranchRoomRole.Boss
                        : RoomNumber(roomId) % 2 == 0 ? BranchRoomRole.Combat : BranchRoomRole.Reward;
                graph.AddRoom(new BranchRoomState(
                    roomId,
                    record.PrimaryCell,
                    new BranchRoomInstanceId(roomId.Value),
                    record.Asset.Id,
                    record.Footprint,
                    role));
            }

            foreach (var record in records.Where(record => record.TempIndex != 0))
            {
                var parentId = idByTempIndex[record.ParentTempIndex];
                var childId = idByTempIndex[record.TempIndex];
                graph.AddBidirectionalConnection(
                    parentId,
                    childId,
                    record.FromDirection,
                    record.ToDirection,
                    record.FromPortId,
                    record.ToPortId);
            }

            return graph;
        }

        private static bool TryPlaceNextRecord(
            IReadOnlyList<PlacementRecord> records,
            IReadOnlyDictionary<int, HashSet<string>> usedPortsByTempIndex,
            HashSet<Vector2Int> occupiedCells,
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> fixturePool,
            IReadOnlyDictionary<RoomFootprintShape, IReadOnlyList<ImportedRoomRuntimeAsset>> candidatesByShape,
            IReadOnlyList<string> fixtureIds,
            System.Random random,
            int maxAttempts,
            int tempIndex,
            out PlacementRecord record)
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var parent = records[random.Next(records.Count)];
                var parentPorts = parent.Asset.DoorPorts
                    .Where(port => !usedPortsByTempIndex[parent.TempIndex].Contains(port.Id))
                    .OrderBy(_ => random.Next())
                    .ToList();
                if (parentPorts.Count == 0)
                {
                    continue;
                }

                var parentPort = parentPorts[random.Next(parentPorts.Count)];
                var childFallback = fixturePool[fixtureIds[random.Next(fixtureIds.Count)]];
                var childAsset = ChooseCandidateForShape(candidatesByShape, childFallback, random);
                var childPorts = childAsset.DoorPorts
                    .Where(port => port.Direction == Opposite(parentPort.Direction))
                    .OrderBy(_ => random.Next())
                    .ToList();
                if (childPorts.Count == 0)
                {
                    continue;
                }

                var childPort = childPorts[random.Next(childPorts.Count)];
                var parentWorldHost = WorldHostCell(parent, parentPort);
                var childWorldHost = parentWorldHost + DirectionOffset(parentPort.Direction);
                var childPrimary = childWorldHost - childPort.HostCell + childAsset.Footprint.PrimaryCell;
                var childFootprint = PlaceFootprint(childAsset.Footprint, childPrimary);
                if (Overlaps(occupiedCells, childFootprint))
                {
                    continue;
                }

                record = new PlacementRecord(tempIndex, childAsset, childPrimary, childFootprint)
                {
                    ParentTempIndex = parent.TempIndex,
                    FromDirection = parentPort.Direction,
                    ToDirection = childPort.Direction,
                    FromPortId = parentPort.Id,
                    ToPortId = childPort.Id,
                    Depth = parent.Depth + 1
                };
                return true;
            }

            record = null;
            return false;
        }

        private static IReadOnlyDictionary<RoomFootprintShape, IReadOnlyList<ImportedRoomRuntimeAsset>> BuildCandidatesByShape(IEnumerable<ImportedRoomRuntimeAsset> assets)
        {
            return (assets ?? Enumerable.Empty<ImportedRoomRuntimeAsset>())
                .Where(asset => asset != null && RoomFootprintShapeUtility.IsSupported(asset.Footprint))
                .GroupBy(asset => RoomFootprintShapeUtility.Classify(asset.Footprint))
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ImportedRoomRuntimeAsset>)group.OrderBy(asset => asset.Id).ToArray());
        }

        private static ImportedRoomRuntimeAsset ChooseCandidateForShape(
            IReadOnlyDictionary<RoomFootprintShape, IReadOnlyList<ImportedRoomRuntimeAsset>> candidatesByShape,
            ImportedRoomRuntimeAsset fallback,
            System.Random random)
        {
            var shape = RoomFootprintShapeUtility.Classify(fallback?.Footprint);
            if (shape == RoomFootprintShape.Unsupported ||
                candidatesByShape == null ||
                !candidatesByShape.TryGetValue(shape, out var candidates) ||
                candidates.Count == 0)
            {
                return fallback;
            }

            return candidates.Count == 1 ? candidates[0] : candidates[random.Next(candidates.Count)];
        }

        private static Dictionary<int, BranchRoomId> AssignRoomIds(IReadOnlyList<PlacementRecord> records, int bossTempIndex)
        {
            var idByTempIndex = new Dictionary<int, BranchRoomId> { [0] = BranchRoomId.Origin };
            var roomNumber = 1;
            foreach (var record in records.Where(record => record.TempIndex != 0).OrderBy(record => record.TempIndex))
            {
                if (record.TempIndex == bossTempIndex)
                {
                    idByTempIndex[record.TempIndex] = new BranchRoomId("boss_01");
                    continue;
                }

                idByTempIndex[record.TempIndex] = new BranchRoomId($"room_{roomNumber:00}");
                roomNumber++;
            }

            return idByTempIndex;
        }

        private static int SelectBossLeaf(IReadOnlyList<PlacementRecord> records)
        {
            var parentIds = records.Where(record => record.TempIndex != 0)
                .Select(record => record.ParentTempIndex)
                .ToHashSet();
            return records
                .Where(record => record.TempIndex != 0 && !parentIds.Contains(record.TempIndex))
                .OrderByDescending(record => record.Depth)
                .ThenBy(record => $"room_{record.TempIndex:00}")
                .First()
                .TempIndex;
        }

        private static int RoomNumber(BranchRoomId roomId)
        {
            var value = roomId.Value ?? string.Empty;
            return value.StartsWith("room_", StringComparison.Ordinal) && int.TryParse(value.Substring(5), out var number)
                ? number
                : 0;
        }

        private static BranchRoomState CreateRoom(BranchRoomId id, Vector2Int primaryCell, ImportedRoomRuntimeAsset asset, BranchRoomRole role)
        {
            return new BranchRoomState(
                id,
                primaryCell,
                new BranchRoomInstanceId(id.Value),
                asset.Id,
                PlaceFootprint(asset.Footprint, primaryCell),
                role);
        }

        private static RoomInstanceFootprint PlaceFootprint(RoomInstanceFootprint source, Vector2Int primaryCell)
        {
            if (source == null)
            {
                return null;
            }

            var offset = primaryCell - source.PrimaryCell;
            var placedCells = source.OccupiedCells.Select(cell => cell + offset).ToArray();
            return new RoomInstanceFootprint(primaryCell, placedCells, source.ChunkBasisTiles);
        }

        private static ImportedRoomRuntimeAsset RequireRoom(IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> roomPool, string id)
        {
            if (roomPool != null && roomPool.TryGetValue(id, out var asset) && asset != null)
            {
                return asset;
            }

            throw new KeyNotFoundException($"Macro fixture branch requires room asset '{id}'.");
        }

        private static Vector2Int WorldHostCell(PlacementRecord record, RoomDoorPort port)
        {
            var offset = record.PrimaryCell - record.Asset.Footprint.PrimaryCell;
            return port.HostCell + offset;
        }

        private static void RegisterCells(HashSet<Vector2Int> occupiedCells, RoomInstanceFootprint footprint)
        {
            foreach (var cell in footprint.OccupiedCells)
            {
                occupiedCells.Add(cell);
            }
        }

        private static bool Overlaps(HashSet<Vector2Int> occupiedCells, RoomInstanceFootprint footprint)
        {
            return footprint.OccupiedCells.Any(occupiedCells.Contains);
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

        private static string Opposite(string direction)
        {
            return direction switch
            {
                "north" => "south",
                "south" => "north",
                "east" => "west",
                "west" => "east",
                _ => string.Empty
            };
        }

        private sealed class PlacementRecord
        {
            public PlacementRecord(int tempIndex, ImportedRoomRuntimeAsset asset, Vector2Int primaryCell, RoomInstanceFootprint footprint)
            {
                TempIndex = tempIndex;
                Asset = asset;
                PrimaryCell = primaryCell;
                Footprint = footprint;
            }

            public int TempIndex { get; }

            public ImportedRoomRuntimeAsset Asset { get; }

            public Vector2Int PrimaryCell { get; }

            public RoomInstanceFootprint Footprint { get; }

            public int ParentTempIndex { get; set; } = -1;

            public int Depth { get; set; }

            public string FromDirection { get; set; } = string.Empty;

            public string ToDirection { get; set; } = string.Empty;

            public string FromPortId { get; set; } = string.Empty;

            public string ToPortId { get; set; } = string.Empty;
        }
    }
}
