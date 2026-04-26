using System.Linq;
using Hollow.Rewards;

namespace Hollow.Branches
{
    public sealed class BranchSessionState
    {
        private BranchSessionState(BranchFloorGraph graph)
        {
            Graph = graph;
            CurrentRoomId = BranchRoomId.Origin;
            CurrentRoom.MarkVisited();
        }

        public BranchFloorGraph Graph { get; }

        public BranchRoomId CurrentRoomId { get; private set; }

        public BranchRoomState CurrentRoom => Graph.GetRoom(CurrentRoomId);

        public static BranchSessionState Create(BranchFloorGraph graph)
        {
            return new BranchSessionState(graph);
        }

        public void EnterRoom(BranchRoomId roomId)
        {
            CurrentRoomId = roomId;
            CurrentRoom.MarkVisited();
        }

        public bool AreAllRoomsCleared()
        {
            return Graph.Rooms.All(room => room.IsCleared);
        }

        public bool AreAllRewardsClaimed()
        {
            return Graph.Rooms.All(room => room.RewardState is RoomRewardState.Unavailable or RoomRewardState.Claimed);
        }
    }
}
