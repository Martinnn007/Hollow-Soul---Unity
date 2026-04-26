using Hollow.Rewards;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchRoomState
    {
        public BranchRoomState(BranchRoomId id, Vector2Int coordinate)
        {
            Id = id;
            Coordinate = coordinate;
            RewardState = id == BranchRoomId.Origin ? RoomRewardState.Unavailable : RoomRewardState.None;
        }

        public BranchRoomId Id { get; }

        public Vector2Int Coordinate { get; }

        public bool IsVisited { get; private set; }

        public bool IsCleared { get; private set; }

        public RoomRewardState RewardState { get; private set; }

        public bool HasPendingReward => RewardState == RoomRewardState.Pending;

        public void MarkVisited()
        {
            IsVisited = true;
        }

        public void MarkCleared()
        {
            IsCleared = true;
        }

        public void MarkRewardPending()
        {
            if (RewardState == RoomRewardState.None)
            {
                RewardState = RoomRewardState.Pending;
            }
        }

        public void MarkRewardClaimed()
        {
            if (RewardState == RoomRewardState.Pending)
            {
                RewardState = RoomRewardState.Claimed;
            }
        }
    }
}
