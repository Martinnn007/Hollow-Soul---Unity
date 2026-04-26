using System.Collections.Generic;
using System.Linq;

namespace Hollow.Branches
{
    public sealed class BranchFloorGraph
    {
        private readonly Dictionary<BranchRoomId, BranchRoomState> rooms = new();
        private readonly List<BranchConnection> connections = new();

        public IReadOnlyCollection<BranchRoomState> Rooms => rooms.Values;

        public IReadOnlyList<BranchConnection> Connections => connections;

        public int RoomCount => rooms.Count;

        public void AddRoom(BranchRoomState room)
        {
            if (room != null)
            {
                rooms[room.Id] = room;
            }
        }

        public void AddBidirectionalConnection(BranchRoomId fromRoomId, BranchRoomId toRoomId, string fromDirection, string toDirection)
        {
            connections.Add(new BranchConnection(fromRoomId, toRoomId, fromDirection, toDirection));
            connections.Add(new BranchConnection(toRoomId, fromRoomId, toDirection, fromDirection));
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
    }
}
