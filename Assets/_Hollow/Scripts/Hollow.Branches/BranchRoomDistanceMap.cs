using System.Collections.Generic;

namespace Hollow.Branches
{
    public sealed class BranchRoomDistanceMap
    {
        private readonly Dictionary<string, int> distances;

        private BranchRoomDistanceMap(Dictionary<string, int> nextDistances)
        {
            distances = nextDistances ?? new Dictionary<string, int>();
        }

        public static BranchRoomDistanceMap Empty { get; } = new(new Dictionary<string, int>());

        public int Count => distances.Count;

        public bool TryGetValue(string roomId, out int distance)
        {
            return distances.TryGetValue(roomId ?? string.Empty, out distance);
        }

        public static BranchRoomDistanceMap Create(BranchFloorGraph graph)
        {
            if (graph == null)
            {
                return Empty;
            }

            var nextDistances = new Dictionary<string, int>();
            var queue = new Queue<BranchRoomId>();
            nextDistances[BranchRoomId.Origin.Value] = 0;
            queue.Enqueue(BranchRoomId.Origin);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var nextDistance = nextDistances[current.Value] + 1;
                foreach (var connection in graph.ConnectionsFrom(current))
                {
                    if (connection == null || nextDistances.ContainsKey(connection.ToRoomId.Value))
                    {
                        continue;
                    }

                    nextDistances[connection.ToRoomId.Value] = nextDistance;
                    queue.Enqueue(connection.ToRoomId);
                }
            }

            return new BranchRoomDistanceMap(nextDistances);
        }
    }
}
