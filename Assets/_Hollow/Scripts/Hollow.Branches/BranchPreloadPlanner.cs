using System;
using System.Collections.Generic;
using Hollow.Core;

namespace Hollow.Branches
{
    public static class BranchPreloadPlanner
    {
        public static List<BranchRoomState> BuildPreloadRoomList(BranchSessionState state)
        {
            return BuildPreloadRoomList(state, HollowRuntimeCachePolicy.Default.MaxPredictivePreloadRooms);
        }

        public static List<BranchRoomState> BuildPreloadRoomList(BranchSessionState state, int maxRooms)
        {
            var rooms = new List<BranchRoomState>();
            if (state?.CurrentRoom == null || state.Graph == null || maxRooms <= 0)
            {
                return rooms;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            AddRoom(state.CurrentRoom, rooms, seen, maxRooms);
            if (rooms.Count >= maxRooms)
            {
                return rooms;
            }

            var candidates = new List<PreloadCandidate>();
            foreach (var connection in state.Graph.ConnectionsFrom(state.CurrentRoomId))
            {
                if (connection == null ||
                    !state.Graph.TryGetRoom(connection.ToRoomId, out var connectedRoom) ||
                    connectedRoom == null)
                {
                    continue;
                }

                candidates.Add(new PreloadCandidate(connection, connectedRoom, CandidatePriority(state.CurrentRoom, connection, connectedRoom)));
            }

            candidates.Sort((left, right) =>
            {
                var priority = left.Priority.CompareTo(right.Priority);
                if (priority != 0)
                {
                    return priority;
                }

                return string.CompareOrdinal(left.Room.Id.Value, right.Room.Id.Value);
            });

            foreach (var candidate in candidates)
            {
                AddRoom(candidate.Room, rooms, seen, maxRooms);
                if (rooms.Count >= maxRooms)
                {
                    break;
                }
            }

            return rooms;
        }

        private static int CandidatePriority(BranchRoomState currentRoom, BranchConnection connection, BranchRoomState candidate)
        {
            if (currentRoom != null && currentRoom.IsCleared && connection.LockKind == BranchConnectionLockKind.None && !candidate.IsVisited)
            {
                return 0;
            }

            if (currentRoom != null && currentRoom.IsCleared && connection.LockKind == BranchConnectionLockKind.None)
            {
                return 1;
            }

            if (currentRoom != null && currentRoom.IsCleared)
            {
                return 2;
            }

            return candidate != null && !candidate.IsVisited ? 3 : 4;
        }

        private static void AddRoom(BranchRoomState room, List<BranchRoomState> rooms, HashSet<string> seen, int maxRooms)
        {
            if (room == null || rooms == null || seen == null || rooms.Count >= maxRooms || !seen.Add(room.Id.Value))
            {
                return;
            }

            rooms.Add(room);
        }

        private readonly struct PreloadCandidate
        {
            public PreloadCandidate(BranchConnection connection, BranchRoomState room, int priority)
            {
                Connection = connection;
                Room = room;
                Priority = priority;
            }

            public BranchConnection Connection { get; }

            public BranchRoomState Room { get; }

            public int Priority { get; }
        }
    }
}
