using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public static class BranchGenerator
    {
        public static BranchFloorGraph CreateFiveRoomCross(ImportedRoomRuntimeAsset _)
        {
            var graph = new BranchFloorGraph();
            graph.AddRoom(new BranchRoomState(BranchRoomId.Origin, Vector2Int.zero));
            graph.AddRoom(new BranchRoomState(BranchRoomId.North, new Vector2Int(0, -1)));
            graph.AddRoom(new BranchRoomState(BranchRoomId.South, new Vector2Int(0, 1)));
            graph.AddRoom(new BranchRoomState(BranchRoomId.East, new Vector2Int(1, 0)));
            graph.AddRoom(new BranchRoomState(BranchRoomId.West, new Vector2Int(-1, 0)));

            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.South, "south", "north");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.East, "east", "west");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.West, "west", "east");
            return graph;
        }
    }
}
