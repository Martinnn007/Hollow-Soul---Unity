using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollow.Branches
{
    public static class EncounterResolver
    {
        public static EncounterPlan CreateSeededPlan(BranchFloorGraph graph, EncounterCatalogDefinition catalog, int seed)
        {
            if (graph == null || catalog == null)
            {
                return EncounterPlan.Empty;
            }

            var distances = DistancesFromOrigin(graph);
            var assignments = new List<RoomEncounterAssignment>();
            foreach (var room in graph.Rooms.OrderBy(room => room.Id.Value, StringComparer.Ordinal))
            {
                if (room.Role is BranchRoomRole.Treasure or BranchRoomRole.Secret)
                {
                    continue;
                }

                distances.TryGetValue(room.Id.Value, out var difficultyBand);
                var footprintCells = Math.Max(1, room.Footprint?.OccupiedCells?.Count ?? 1);
                var encounter = room.Role == BranchRoomRole.Boss
                    ? catalog.BossEncounter ?? ChooseEncounter(graph, catalog, room, difficultyBand, footprintCells, seed)
                    : ChooseEncounter(graph, catalog, room, difficultyBand, footprintCells, seed);
                if (encounter == null)
                {
                    continue;
                }

                assignments.Add(new RoomEncounterAssignment(room.Id.Value, encounter.EncounterId, encounter.ExpandSpawnKinds()));
            }

            return new EncounterPlan(assignments);
        }

        private static EncounterDefinition ChooseEncounter(
            BranchFloorGraph graph,
            EncounterCatalogDefinition catalog,
            BranchRoomState room,
            int difficultyBand,
            int footprintCells,
            int seed)
        {
            var candidates = catalog.Encounters
                .Where(encounter => encounter != null && encounter.Supports(room.Role, difficultyBand, footprintCells))
                .ToList();
            if (candidates.Count == 0 && room.Role == BranchRoomRole.Reward)
            {
                candidates = catalog.Encounters
                    .Where(encounter => encounter != null && encounter.Supports(BranchRoomRole.Combat, difficultyBand, footprintCells))
                    .ToList();
            }

            if (candidates.Count == 0 && room.Role != BranchRoomRole.Origin)
            {
                candidates = catalog.Encounters
                    .Where(encounter => encounter != null && encounter.Supports(BranchRoomRole.Combat, 0, footprintCells))
                    .ToList();
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var totalWeight = candidates.Sum(candidate => Math.Max(1, candidate.Weight));
            var roll = StableHash($"{graph.BranchId}|{seed}|{room.Id.Value}|{catalog.CatalogId}") % totalWeight;
            foreach (var candidate in candidates.OrderBy(candidate => candidate.EncounterId, StringComparer.Ordinal))
            {
                roll -= Math.Max(1, candidate.Weight);
                if (roll < 0)
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        private static Dictionary<string, int> DistancesFromOrigin(BranchFloorGraph graph)
        {
            var distances = new Dictionary<string, int>();
            var queue = new Queue<BranchRoomId>();
            queue.Enqueue(BranchRoomId.Origin);
            distances[BranchRoomId.Origin.Value] = 0;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var nextDistance = distances[current.Value] + 1;
                foreach (var connection in graph.ConnectionsFrom(current))
                {
                    if (distances.ContainsKey(connection.ToRoomId.Value))
                    {
                        continue;
                    }

                    distances[connection.ToRoomId.Value] = nextDistance;
                    queue.Enqueue(connection.ToRoomId);
                }
            }

            return distances;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
