using System;
using System.Linq;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public static class DeveloperInspectionBranchBuilder
    {
        public static BranchFloorGraph CreateGraph(BranchSessionContent content, int seed)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var defaultRoomAsset = ResolveWideRoom(content, DeveloperLabDefinition.WideRoomAssetId);
            if (defaultRoomAsset == null)
            {
                throw new InvalidOperationException("Developer Lab requires at least one runtime room asset.");
            }

            var graph = new BranchFloorGraph(DeveloperLabDefinition.BranchId, seed == 0 ? DeveloperLabDefinition.Seed : seed);
            BranchRoomId previousRoomId = default;
            for (var index = 0; index < DeveloperLabDefinition.RoomCount; index++)
            {
                var roomId = index == 0 ? BranchRoomId.Origin : new BranchRoomId($"lab_room_{index + 1:00}");
                var primaryCell = new Vector2Int(index * 2, 0);
                var role = index == 0 ? BranchRoomRole.Origin : BranchRoomRole.Combat;
                var roomAsset = ResolveRoomForIndex(content, index) ?? defaultRoomAsset;
                var room = new BranchRoomState(
                    roomId,
                    primaryCell,
                    new BranchRoomInstanceId(roomId.Value),
                    roomAsset.Id,
                    PlaceFootprint(roomAsset.Footprint, primaryCell),
                    role);
                room.MarkVisited();
                room.MarkCleared();
                room.MarkRewardUnavailable();
                graph.AddRoom(room);

                if (index > 0)
                {
                    graph.AddBidirectionalConnection(previousRoomId, roomId, "east", "west", "east_0", "west_0");
                }

                previousRoomId = roomId;
            }

            return graph;
        }

        private static ImportedRoomRuntimeAsset ResolveRoomForIndex(BranchSessionContent content, int index)
        {
            if (index >= 0 &&
                index < DeveloperLabDefinition.RoomAssetIds.Length &&
                content.MacroRoomPool.TryGetValue(DeveloperLabDefinition.RoomAssetIds[index], out var labRoom) &&
                labRoom != null &&
                RoomFootprintShapeUtility.Classify(labRoom.Footprint) == RoomFootprintShape.Wide2x1)
            {
                return labRoom;
            }

            var preferredId = index switch
            {
                5 => "approved_pressure_lane_wide_2x1",
                _ => DeveloperLabDefinition.WideRoomAssetId
            };
            return ResolveWideRoom(content, preferredId);
        }

        private static ImportedRoomRuntimeAsset ResolveWideRoom(BranchSessionContent content, string preferredId)
        {
            if (!string.IsNullOrWhiteSpace(preferredId) &&
                content.MacroRoomPool.TryGetValue(preferredId, out var wide) &&
                wide != null &&
                RoomFootprintShapeUtility.Classify(wide.Footprint) == RoomFootprintShape.Wide2x1)
            {
                return wide;
            }

            return content.MacroRoomPool.Values
                .Where(asset => asset != null)
                .FirstOrDefault(asset => RoomFootprintShapeUtility.Classify(asset.Footprint) == RoomFootprintShape.Wide2x1) ??
                   content.MacroRoomPool.Values.FirstOrDefault(asset => asset != null) ??
                   content.LegacySampleRoomAsset;
        }

        private static RoomInstanceFootprint PlaceFootprint(RoomInstanceFootprint source, Vector2Int primaryCell)
        {
            if (source == null)
            {
                return null;
            }

            var offset = primaryCell - source.PrimaryCell;
            return new RoomInstanceFootprint(
                primaryCell,
                source.OccupiedCells.Select(cell => cell + offset).ToArray(),
                source.ChunkBasisTiles);
        }
    }
}
