using System;
using System.Linq;
using Hollow.Combat;

namespace Hollow.Branches
{
    public static class BossSelectionResolver
    {
        public static BossDefinition Resolve(
            BossCatalogDefinition catalog,
            int runSeed,
            int branchSeed,
            int worldIndex,
            string roomId,
            string branchId)
        {
            var resolvedCatalog = catalog != null ? catalog : BossCatalogDefinition.CreateRuntimeDefault();
            var band = BandForWorld(worldIndex);
            var candidates = resolvedCatalog.BossesForBand(band);
            if (candidates.Count == 0)
            {
                return resolvedCatalog.FallbackBoss;
            }

            var roll = StableHash($"{resolvedCatalog.CatalogId}|run:{runSeed}|branch:{branchSeed}|world:{Math.Max(1, worldIndex)}|room:{roomId}|{branchId}") % candidates.Count;
            return candidates.OrderBy(boss => boss.BossId, StringComparer.Ordinal).ElementAt(roll);
        }

        public static BossWorldBand BandForWorld(int worldIndex)
        {
            return worldIndex <= 1
                ? BossWorldBand.World1
                : worldIndex == 2
                    ? BossWorldBand.World2
                    : BossWorldBand.World3;
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
