using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;

namespace Hollow.Branches
{
    public static class RunWorldItineraryService
    {
        public const int DefaultMechanicalWorldCount = 3;

        public static RunFramingDefinition Resolve(RunFramingCatalogDefinition catalog, int runSeed, int mechanicalWorldIndex)
        {
            var itinerary = ResolveItinerary(catalog, runSeed, DefaultMechanicalWorldCount);
            if (itinerary.Count == 0)
            {
                return catalog != null && catalog.TryGetWorld(mechanicalWorldIndex, out var fallback) ? fallback : null;
            }

            var index = (Math.Max(1, mechanicalWorldIndex) - 1) % itinerary.Count;
            return itinerary[index];
        }

        public static IReadOnlyList<RunFramingDefinition> ResolveItinerary(RunFramingCatalogDefinition catalog, int runSeed, int count)
        {
            var worlds = catalog != null
                ? catalog.Worlds
                    .Where(world => world != null)
                    .OrderBy(world => world.WorldIndex)
                    .ToArray()
                : Array.Empty<RunFramingDefinition>();
            if (worlds.Length == 0)
            {
                return Array.Empty<RunFramingDefinition>();
            }

            var desired = Math.Max(1, Math.Min(count <= 0 ? DefaultMechanicalWorldCount : count, worlds.Length));
            if (worlds.Length <= desired)
            {
                return worlds.Take(desired).ToArray();
            }

            var seed = runSeed == 0 ? 1 : Math.Abs(runSeed);
            return worlds
                .OrderBy(world => StableHash($"{catalog.CatalogId}|{seed}|{world.IdentityId}|{world.WorldIndex}"))
                .ThenBy(world => world.IdentityId, StringComparer.Ordinal)
                .Take(desired)
                .ToArray();
        }

        public static string ResolveBranchEcho(RunFramingCatalogDefinition catalog, int runSeed, int mechanicalWorldIndex, int slotIndex)
        {
            var world = Resolve(catalog, runSeed, mechanicalWorldIndex);
            var echoes = world != null
                ? world.BranchEchoNames
                    .Where(echo => !string.IsNullOrWhiteSpace(echo))
                    .ToArray()
                : Array.Empty<string>();
            if (echoes.Length == 0)
            {
                return string.Empty;
            }

            var index = Math.Max(0, slotIndex) % echoes.Length;
            return echoes[index];
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
