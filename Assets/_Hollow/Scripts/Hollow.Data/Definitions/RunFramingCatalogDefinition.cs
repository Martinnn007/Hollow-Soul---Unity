using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/World/Run Framing Catalog")]
    public sealed class RunFramingCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId = "run_framing_catalog";
        [SerializeField] private List<RunFramingDefinition> worlds = new();

        public string CatalogId => string.IsNullOrWhiteSpace(catalogId) ? "run_framing_catalog" : catalogId;

        public IReadOnlyList<RunFramingDefinition> Worlds => worlds;

        public void Configure(string nextCatalogId, IEnumerable<RunFramingDefinition> nextWorlds)
        {
            catalogId = string.IsNullOrWhiteSpace(nextCatalogId) ? "run_framing_catalog" : nextCatalogId;
            worlds = (nextWorlds ?? Enumerable.Empty<RunFramingDefinition>())
                .Where(world => world != null)
                .GroupBy(world => world.WorldIndex)
                .Select(group => group.First())
                .OrderBy(world => world.WorldIndex)
                .ToList();
        }

        public bool TryGetWorld(int worldIndex, out RunFramingDefinition definition)
        {
            var normalizedWorldIndex = Mathf.Max(1, worldIndex);
            definition = worlds.FirstOrDefault(world => world != null && world.WorldIndex == normalizedWorldIndex)
                         ?? worlds.FirstOrDefault(world => world != null);
            return definition != null;
        }
    }
}
