using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public static class BranchTraversalService
    {
        public const float DoorInteractionRadiusMeters = 1.35f;
        public const float DoorEntryInsetMeters = 0.75f;

        public static bool CanTraverse(BranchSessionState state, string direction, out BranchConnection connection)
        {
            connection = null;
            return state != null &&
                   state.CurrentRoom.IsCleared &&
                   state.Graph.TryGetConnection(state.CurrentRoomId, direction, out connection);
        }

        public static Vector3 EntryPositionFor(RoomRuntimeRoot room, string entryDirection)
        {
            var position = Vector3.zero;
            if (room != null && room.TryGetDoorPort(entryDirection, out var port))
            {
                position = port.Position;
            }

            return entryDirection switch
            {
                "north" => new Vector3(position.x, 0f, position.z + DoorEntryInsetMeters),
                "south" => new Vector3(position.x, 0f, position.z - DoorEntryInsetMeters),
                "east" => new Vector3(position.x - DoorEntryInsetMeters, 0f, position.z),
                "west" => new Vector3(position.x + DoorEntryInsetMeters, 0f, position.z),
                _ => Vector3.zero
            };
        }

        public static Vector3 EntryPositionFor(RoomRuntimeRoot room, BranchConnection connection)
        {
            if (connection == null)
            {
                return Vector3.zero;
            }

            var position = Vector3.zero;
            if (room != null)
            {
                if (connection.HasExplicitPorts && room.TryGetDoorPortById(connection.ToPortId, out var explicitPort))
                {
                    position = explicitPort.Position;
                }
                else if (room.TryGetDoorPort(connection.ToDirection, out var directionalPort))
                {
                    position = directionalPort.Position;
                }
            }

            return connection.ToDirection switch
            {
                "north" => new Vector3(position.x, 0f, position.z + DoorEntryInsetMeters),
                "south" => new Vector3(position.x, 0f, position.z - DoorEntryInsetMeters),
                "east" => new Vector3(position.x - DoorEntryInsetMeters, 0f, position.z),
                "west" => new Vector3(position.x + DoorEntryInsetMeters, 0f, position.z),
                _ => Vector3.zero
            };
        }
    }
}
