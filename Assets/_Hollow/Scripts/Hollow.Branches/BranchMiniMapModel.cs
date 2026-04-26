using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchMiniMapModel
    {
        public BranchMiniMapModel(BranchSessionState state)
        {
            Nodes = state?.Graph.Rooms
                .OrderBy(room => room.Coordinate.y)
                .ThenBy(room => room.Coordinate.x)
                .Select(room => new BranchMiniMapNode(
                    room.Id,
                    room.Coordinate,
                    room.Role,
                    room.Id == state.CurrentRoomId,
                    room.IsVisited,
                    room.IsCleared,
                    room.HasPendingReward))
                .ToList() ?? new List<BranchMiniMapNode>();
        }

        public IReadOnlyList<BranchMiniMapNode> Nodes { get; }

        public string Summary()
        {
            return string.Join("  ", Nodes.Select(node =>
                $"{node.Id}:{node.Role}:{(node.IsCurrent ? "Current" : node.IsCleared ? "Cleared" : node.IsVisited ? "Visited" : "Hidden")}{(node.HasPendingReward ? "+Reward" : string.Empty)}"));
        }
    }

    public sealed class BranchMiniMapNode
    {
        public BranchMiniMapNode(BranchRoomId id, Vector2Int coordinate, BranchRoomRole role, bool isCurrent, bool isVisited, bool isCleared, bool hasPendingReward)
        {
            Id = id;
            Coordinate = coordinate;
            Role = role;
            IsCurrent = isCurrent;
            IsVisited = isVisited;
            IsCleared = isCleared;
            HasPendingReward = hasPendingReward;
        }

        public BranchRoomId Id { get; }

        public Vector2Int Coordinate { get; }

        public BranchRoomRole Role { get; }

        public bool IsCurrent { get; }

        public bool IsVisited { get; }

        public bool IsCleared { get; }

        public bool HasPendingReward { get; }
    }
}
