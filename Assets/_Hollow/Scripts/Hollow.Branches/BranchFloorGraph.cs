using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchFloorGraph
    {
        private readonly Dictionary<BranchRoomId, BranchRoomState> rooms = new();
        private readonly List<BranchConnection> connections = new();

        public BranchFloorGraph()
            : this(BranchGenerator.LegacyFiveRoomBranchId, 0)
        {
        }

        public BranchFloorGraph(string branchId, int seed)
        {
            BranchId = string.IsNullOrWhiteSpace(branchId) ? BranchGenerator.LegacyFiveRoomBranchId : branchId;
            Seed = seed;
        }

        public string BranchId { get; }

        public int Seed { get; }

        public BranchCellOccupancyMap OccupancyMap { get; } = new();

        public IReadOnlyCollection<BranchRoomState> Rooms => rooms.Values;

        public IReadOnlyList<BranchConnection> Connections => connections;

        public int RoomCount => rooms.Count;

        public void AddRoom(BranchRoomState room)
        {
            if (room != null)
            {
                if (room.Footprint != null && !OccupancyMap.TryRegister(room.InstanceId, room.Footprint))
                {
                    Debug.LogError($"Branch room '{room.Id}' overlaps an already occupied macro branch cell.");
                    return;
                }

                rooms[room.Id] = room;
            }
        }

        public void AddBidirectionalConnection(BranchRoomId fromRoomId, BranchRoomId toRoomId, string fromDirection, string toDirection)
        {
            AddBidirectionalConnection(fromRoomId, toRoomId, fromDirection, toDirection, string.Empty, string.Empty);
        }

        public void AddBidirectionalConnection(
            BranchRoomId fromRoomId,
            BranchRoomId toRoomId,
            string fromDirection,
            string toDirection,
            string fromPortId,
            string toPortId)
        {
            AddBidirectionalConnection(fromRoomId, toRoomId, fromDirection, toDirection, fromPortId, toPortId, BranchConnectionLockKind.None);
        }

        public void AddBidirectionalConnection(
            BranchRoomId fromRoomId,
            BranchRoomId toRoomId,
            string fromDirection,
            string toDirection,
            string fromPortId,
            string toPortId,
            BranchConnectionLockKind lockKind)
        {
            connections.Add(new BranchConnection(fromRoomId, toRoomId, fromDirection, toDirection, fromPortId, toPortId, lockKind));
            connections.Add(new BranchConnection(toRoomId, fromRoomId, toDirection, fromDirection, toPortId, fromPortId, lockKind));
        }

        public BranchRoomState GetRoom(BranchRoomId id)
        {
            return rooms[id];
        }

        public bool TryGetRoom(BranchRoomId id, out BranchRoomState room)
        {
            return rooms.TryGetValue(id, out room);
        }

        public IReadOnlyList<BranchConnection> ConnectionsFrom(BranchRoomId roomId)
        {
            return connections.Where(connection => connection.FromRoomId == roomId).ToList();
        }

        public bool TryGetConnection(BranchRoomId roomId, string direction, out BranchConnection connection)
        {
            connection = connections.FirstOrDefault(candidate => candidate.FromRoomId == roomId && candidate.FromDirection == direction);
            return connection != null;
        }

        public bool TryGetConnectionByPort(BranchRoomId roomId, string portId, out BranchConnection connection)
        {
            connection = connections.FirstOrDefault(candidate => candidate.FromRoomId == roomId && candidate.FromPortId == portId);
            return connection != null;
        }

        public bool HasConnectionByPortPair(BranchRoomId fromRoomId, string fromPortId, BranchRoomId toRoomId, string toPortId)
        {
            return connections.Any(candidate =>
                candidate.FromRoomId == fromRoomId &&
                candidate.ToRoomId == toRoomId &&
                candidate.FromPortId == (fromPortId ?? string.Empty) &&
                candidate.ToPortId == (toPortId ?? string.Empty));
        }
    }
}
