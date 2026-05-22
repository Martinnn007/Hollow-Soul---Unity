using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchMiniMapModel
    {
        public BranchMiniMapModel(BranchSessionState state)
            : this(state, revealAll: false, labelResolver: null)
        {
        }

        public BranchMiniMapModel(
            BranchSessionState state,
            bool revealAll,
            Func<BranchRoomState, string> labelResolver)
        {
            if (state?.Graph == null)
            {
                Nodes = new List<BranchMiniMapNode>();
                Connections = new List<BranchMiniMapConnectionVisual>();
                return;
            }

            var graph = state.Graph;
            var revealSourceIds = graph.Rooms
                .Where(room => room.Id == state.CurrentRoomId || room.IsVisited || room.IsCleared)
                .Select(room => room.Id)
                .ToHashSet();
            var revealedIds = revealSourceIds.ToHashSet();
            foreach (var connection in graph.Connections)
            {
                if (revealSourceIds.Contains(connection.FromRoomId))
                {
                    revealedIds.Add(connection.ToRoomId);
                }
            }

            Nodes = graph.Rooms
                .OrderBy(room => room.Coordinate.y)
                .ThenBy(room => room.Coordinate.x)
                .Select(room => new BranchMiniMapNode(
                    room.Id,
                    room.Coordinate,
                    OccupiedCellsFor(room),
                    room.Role,
                    room.Id == state.CurrentRoomId,
                    room.IsVisited,
                    room.IsCleared,
                    room.HasPendingReward,
                    revealAll || revealedIds.Contains(room.Id) || room.Role == BranchRoomRole.Secret,
                    labelResolver?.Invoke(room) ?? string.Empty))
                .ToList();
            var nodeById = Nodes.ToDictionary(node => node.Id);
            Connections = BuildConnections(graph, nodeById);
        }

        public IReadOnlyList<BranchMiniMapNode> Nodes { get; }

        public IReadOnlyList<BranchMiniMapConnectionVisual> Connections { get; }

        public string Summary()
        {
            return string.Join("  ", Nodes.Select(node =>
                $"{node.Id}:{node.Role}:{(node.IsCurrent ? "Current" : node.IsCleared ? "Cleared" : node.IsVisited ? "Visited" : node.IsRevealed ? "Revealed" : "Hidden")}:{node.OccupiedCells.Count}c:{node.DisplayLabel}{(node.HasPendingReward ? "+Reward" : string.Empty)}"));
        }

        private static IReadOnlyCollection<Vector2Int> OccupiedCellsFor(BranchRoomState room)
        {
            return room?.Footprint?.OccupiedCells != null && room.Footprint.OccupiedCells.Count > 0
                ? room.Footprint.OccupiedCells.ToArray()
                : new[] { room?.Coordinate ?? Vector2Int.zero };
        }

        private static IReadOnlyList<BranchMiniMapConnectionVisual> BuildConnections(BranchFloorGraph graph, IReadOnlyDictionary<BranchRoomId, BranchMiniMapNode> nodeById)
        {
            var seen = new HashSet<string>();
            var visuals = new List<BranchMiniMapConnectionVisual>();
            foreach (var connection in graph.Connections)
            {
                if (!nodeById.TryGetValue(connection.FromRoomId, out var fromNode) ||
                    !nodeById.TryGetValue(connection.ToRoomId, out var toNode) ||
                    !fromNode.IsRevealed ||
                    !toNode.IsRevealed)
                {
                    continue;
                }

                var key = UnorderedConnectionKey(connection);
                if (!seen.Add(key))
                {
                    continue;
                }

                var (fromCell, toCell) = ClosestConnectedCells(fromNode, toNode, connection.FromDirection);
                visuals.Add(new BranchMiniMapConnectionVisual(
                    connection.FromRoomId,
                    connection.ToRoomId,
                    fromCell,
                    toCell,
                    connection.LockKind));
            }

            return visuals;
        }

        private static string UnorderedConnectionKey(BranchConnection connection)
        {
            var fromEndpoint = connection.HasExplicitPorts
                ? $"{connection.FromRoomId.Value}:{connection.FromPortId}"
                : connection.FromRoomId.Value;
            var toEndpoint = connection.HasExplicitPorts
                ? $"{connection.ToRoomId.Value}:{connection.ToPortId}"
                : connection.ToRoomId.Value;
            return string.CompareOrdinal(fromEndpoint, toEndpoint) <= 0
                ? $"{fromEndpoint}|{toEndpoint}"
                : $"{toEndpoint}|{fromEndpoint}";
        }

        private static (Vector2Int fromCell, Vector2Int toCell) ClosestConnectedCells(BranchMiniMapNode fromNode, BranchMiniMapNode toNode, string fromDirection)
        {
            var preferredOffset = DirectionOffset(fromDirection);
            var candidates = from fromCell in fromNode.OccupiedCells
                from toCell in toNode.OccupiedCells
                let delta = toCell - fromCell
                let distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y)
                let directionScore = delta == preferredOffset ? 0 : 1
                orderby directionScore, distance, fromCell.y, fromCell.x, toCell.y, toCell.x
                select (fromCell, toCell);
            return candidates.FirstOrDefault();
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

    public sealed class BranchMiniMapNode
    {
        public BranchMiniMapNode(
            BranchRoomId id,
            Vector2Int coordinate,
            IReadOnlyCollection<Vector2Int> occupiedCells,
            BranchRoomRole role,
            bool isCurrent,
            bool isVisited,
            bool isCleared,
            bool hasPendingReward,
            bool isRevealed,
            string displayLabel = "")
        {
            Id = id;
            Coordinate = coordinate;
            OccupiedCells = occupiedCells?.ToArray() ?? new[] { coordinate };
            Role = role;
            IsCurrent = isCurrent;
            IsVisited = isVisited;
            IsCleared = isCleared;
            HasPendingReward = hasPendingReward;
            IsRevealed = isRevealed;
            DisplayLabel = displayLabel ?? string.Empty;
        }

        public BranchRoomId Id { get; }

        public Vector2Int Coordinate { get; }

        public IReadOnlyCollection<Vector2Int> OccupiedCells { get; }

        public BranchRoomRole Role { get; }

        public bool IsCurrent { get; }

        public bool IsVisited { get; }

        public bool IsCleared { get; }

        public bool HasPendingReward { get; }

        public bool IsRevealed { get; }

        public string DisplayLabel { get; }
    }

    public sealed class BranchMiniMapConnectionVisual
    {
        public BranchMiniMapConnectionVisual(
            BranchRoomId fromRoomId,
            BranchRoomId toRoomId,
            Vector2Int fromCell,
            Vector2Int toCell,
            BranchConnectionLockKind lockKind)
        {
            FromRoomId = fromRoomId;
            ToRoomId = toRoomId;
            FromCell = fromCell;
            ToCell = toCell;
            LockKind = lockKind;
        }

        public BranchRoomId FromRoomId { get; }

        public BranchRoomId ToRoomId { get; }

        public Vector2Int FromCell { get; }

        public Vector2Int ToCell { get; }

        public BranchConnectionLockKind LockKind { get; }
    }
}
