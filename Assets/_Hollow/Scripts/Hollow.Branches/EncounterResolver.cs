using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;

namespace Hollow.Branches
{
    public static class EncounterResolver
    {
        private const int DefaultMaxDirectedSpawns = 6;
        private static readonly string[] EmptyAllowedNonBossSpawnKinds = Array.Empty<string>();

        public static EncounterPlan CreateSeededPlan(BranchFloorGraph graph, EncounterCatalogDefinition catalog, int seed)
        {
            return CreateSeededPlan(graph, catalog, seed, null);
        }

        public static EncounterPlan CreateSeededPlan(
            BranchFloorGraph graph,
            EncounterCatalogDefinition catalog,
            int seed,
            IEnumerable<string> allowedNonBossSpawnKinds,
            BranchRoomDistanceMap distanceMap = null)
        {
            if (graph == null || catalog == null)
            {
                return EncounterPlan.Empty;
            }

            var allowedSpawns = NormalizeAllowedNonBossSpawnKinds(allowedNonBossSpawnKinds);
            var distances = distanceMap ?? BranchRoomDistanceMap.Create(graph);
            var assignments = new List<RoomEncounterAssignment>();
            foreach (var room in graph.Rooms.OrderBy(room => room.Id.Value, StringComparer.Ordinal))
            {
                if (room.Role is BranchRoomRole.Origin or BranchRoomRole.Reward or BranchRoomRole.Treasure or BranchRoomRole.Secret or BranchRoomRole.CorruptedChest)
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

                var spawns = encounter.ExpandSpawnKinds();
                spawns = ApplyAllowedNonBossSpawnKinds(spawns, room, seed, allowedSpawns);
                assignments.Add(new RoomEncounterAssignment(room.Id.Value, encounter.EncounterId, spawns));
            }

            return new EncounterPlan(assignments);
        }

        public static EncounterPlan CreateDirectedSeededPlan(BranchFloorGraph graph, EncounterCatalogDefinition catalog, int seed, int worldIndex, EncounterDirectorProfileDefinition profile)
        {
            return CreateDirectedSeededPlan(graph, catalog, seed, worldIndex, profile, difficultyBandBonus: 0);
        }

        public static EncounterPlan CreateDirectedSeededPlan(BranchFloorGraph graph, EncounterCatalogDefinition catalog, int seed, int worldIndex, EncounterDirectorProfileDefinition profile, int difficultyBandBonus)
        {
            return CreateDirectedSeededPlan(graph, catalog, seed, worldIndex, profile, difficultyBandBonus, null);
        }

        public static EncounterPlan CreateDirectedSeededPlan(
            BranchFloorGraph graph,
            EncounterCatalogDefinition catalog,
            int seed,
            int worldIndex,
            EncounterDirectorProfileDefinition profile,
            int difficultyBandBonus,
            BossCatalogDefinition bossCatalog)
        {
            return CreateDirectedSeededPlan(graph, catalog, seed, worldIndex, profile, difficultyBandBonus, bossCatalog, null);
        }

        public static EncounterPlan CreateDirectedSeededPlan(
            BranchFloorGraph graph,
            EncounterCatalogDefinition catalog,
            int seed,
            int worldIndex,
            EncounterDirectorProfileDefinition profile,
            int difficultyBandBonus,
            BossCatalogDefinition bossCatalog,
            IEnumerable<string> allowedNonBossSpawnKinds,
            BranchRoomDistanceMap distanceMap = null)
        {
            if (graph == null || catalog == null)
            {
                return EncounterPlan.Empty;
            }

            var context = new EncounterDirectorContext(graph, seed, worldIndex, profile);
            var allowedSpawns = NormalizeAllowedNonBossSpawnKinds(allowedNonBossSpawnKinds);
            var distances = distanceMap ?? BranchRoomDistanceMap.Create(graph);
            var assignments = new List<RoomEncounterAssignment>();
            foreach (var room in graph.Rooms.OrderBy(room => room.Id.Value, StringComparer.Ordinal))
            {
                if (room.Role is BranchRoomRole.Origin or BranchRoomRole.Reward or BranchRoomRole.Treasure or BranchRoomRole.Secret or BranchRoomRole.CorruptedChest)
                {
                    continue;
                }

                distances.TryGetValue(room.Id.Value, out var roomDistance);
                var difficultyBand = roomDistance + context.WorldConfig.DifficultyOffset + Math.Max(0, difficultyBandBonus);
                var footprintCells = Math.Max(1, room.Footprint?.OccupiedCells?.Count ?? 1);
                var encounter = room.Role == BranchRoomRole.Boss
                    ? catalog.BossEncounter ?? ChooseDirectedEncounter(context, catalog, room, difficultyBand, footprintCells)
                    : ChooseDirectedEncounter(context, catalog, room, difficultyBand, footprintCells);
                if (encounter == null)
                {
                    continue;
                }

                var spawns = encounter.ExpandSpawnKinds();
                if (room.Role != BranchRoomRole.Boss)
                {
                    spawns = spawns.Take(Math.Max(1, context.Profile?.MaxNonBossEnemySpawns ?? DefaultMaxDirectedSpawns)).ToArray();
                    spawns = ApplyAllowedNonBossSpawnKinds(spawns, room, seed, allowedSpawns);
                }

                var boss = room.Role == BranchRoomRole.Boss
                    ? BossSelectionResolver.Resolve(bossCatalog, seed, graph.Seed, context.WorldIndex, room.Id.Value, graph.BranchId)
                    : null;
                assignments.Add(new RoomEncounterAssignment(
                    room.Id.Value,
                    encounter.EncounterId,
                    spawns,
                    context.WorldIndex,
                    difficultyBand,
                    DirectorPressureFor(context, encounter, difficultyBand),
                    boss != null ? boss.BossId : string.Empty,
                    boss != null ? boss.Arena.arenaId : string.Empty,
                    boss != null ? (int)boss.WorldBand : 0,
                    string.Empty));
            }

            return new EncounterPlan(assignments);
        }

        private static string[] NormalizeAllowedNonBossSpawnKinds(IEnumerable<string> allowedNonBossSpawnKinds)
        {
            return allowedNonBossSpawnKinds?
                .Where(kind => !string.IsNullOrWhiteSpace(kind))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(kind => kind, StringComparer.Ordinal)
                .ToArray() ?? EmptyAllowedNonBossSpawnKinds;
        }

        private static IReadOnlyList<string> ApplyAllowedNonBossSpawnKinds(
            IReadOnlyList<string> originalSpawns,
            BranchRoomState room,
            int seed,
            IReadOnlyList<string> allowedNonBossSpawnKinds)
        {
            if (allowedNonBossSpawnKinds == null ||
                allowedNonBossSpawnKinds.Count == 0 ||
                room == null ||
                room.Role == BranchRoomRole.Boss)
            {
                return originalSpawns;
            }

            var count = Math.Max(1, originalSpawns?.Count ?? 0);
            var result = new string[count];
            for (var index = 0; index < count; index++)
            {
                var roll = StableHash($"{room.Id.Value}|{seed}|small-monsters|{index}");
                result[index] = allowedNonBossSpawnKinds[roll % allowedNonBossSpawnKinds.Count];
            }

            return result;
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

        private static EncounterDefinition ChooseDirectedEncounter(
            EncounterDirectorContext context,
            EncounterCatalogDefinition catalog,
            BranchRoomState room,
            int difficultyBand,
            int footprintCells)
        {
            var candidates = catalog.Encounters
                .Where(encounter => encounter != null && encounter.Supports(room.Role, difficultyBand, footprintCells))
                .ToList();
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

            var totalWeight = candidates.Sum(candidate => DirectedWeightFor(context, candidate, difficultyBand));
            var roll = StableHash($"{context.Graph.BranchId}|{context.Seed}|world:{context.WorldIndex}|{room.Id.Value}|{catalog.CatalogId}") % totalWeight;
            foreach (var candidate in candidates.OrderBy(candidate => candidate.EncounterId, StringComparer.Ordinal))
            {
                roll -= DirectedWeightFor(context, candidate, difficultyBand);
                if (roll < 0)
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        private static int DirectedWeightFor(EncounterDirectorContext context, EncounterDefinition encounter, int difficultyBand)
        {
            var weight = Math.Max(1, encounter.Weight);
            var config = context?.WorldConfig;
            if (config == null)
            {
                return weight;
            }

            if (encounter.MinDifficultyBand >= 2 || difficultyBand >= 3)
            {
                weight += config.VeryHardEncounterWeightBonus;
            }
            else if (encounter.MinDifficultyBand >= 1 || difficultyBand >= 2)
            {
                weight += config.HardEncounterWeightBonus;
            }

            return Math.Max(1, weight);
        }

        private static int DirectorPressureFor(EncounterDirectorContext context, EncounterDefinition encounter, int difficultyBand)
        {
            return difficultyBand + Math.Max(0, encounter?.MinDifficultyBand ?? 0) + Math.Max(0, context?.WorldConfig?.DifficultyOffset ?? 0);
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
