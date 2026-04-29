using Hollow.Rewards;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchRoomState
    {
        public BranchRoomState(BranchRoomId id, Vector2Int coordinate)
            : this(id, coordinate, new BranchRoomInstanceId(id.Value), string.Empty, null, id == BranchRoomId.Origin ? BranchRoomRole.Origin : BranchRoomRole.Combat)
        {
        }

        public BranchRoomState(
            BranchRoomId id,
            Vector2Int coordinate,
            BranchRoomInstanceId instanceId,
            string runtimeRoomAssetId,
            RoomInstanceFootprint footprint)
            : this(id, coordinate, instanceId, runtimeRoomAssetId, footprint, id == BranchRoomId.Origin ? BranchRoomRole.Origin : BranchRoomRole.Combat)
        {
        }

        public BranchRoomState(
            BranchRoomId id,
            Vector2Int coordinate,
            BranchRoomInstanceId instanceId,
            string runtimeRoomAssetId,
            RoomInstanceFootprint footprint,
            BranchRoomRole role)
        {
            Id = id;
            Coordinate = coordinate;
            InstanceId = instanceId;
            RuntimeRoomAssetId = runtimeRoomAssetId ?? string.Empty;
            Footprint = footprint;
            Role = role;
            RewardState = id == BranchRoomId.Origin ? RoomRewardState.Unavailable : RoomRewardState.None;
        }

        public BranchRoomId Id { get; }

        public Vector2Int Coordinate { get; }

        public BranchRoomInstanceId InstanceId { get; }

        public string RuntimeRoomAssetId { get; }

        public RoomInstanceFootprint Footprint { get; }

        public BranchRoomRole Role { get; private set; }

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

        public void MarkRewardUnavailable()
        {
            if (RewardState == RoomRewardState.Pending || RewardState == RoomRewardState.None)
            {
                RewardState = RoomRewardState.Unavailable;
            }
        }

        public void OverrideRole(BranchRoomRole role)
        {
            Role = role;
        }

        public void Restore(bool isVisited, bool isCleared, RoomRewardState rewardState)
        {
            IsVisited = isVisited;
            IsCleared = isCleared;
            RewardState = rewardState;
        }
    }
}
