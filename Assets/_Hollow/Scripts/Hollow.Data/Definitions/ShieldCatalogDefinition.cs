using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Equipment/Shield Catalog")]
    public sealed class ShieldCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId = "m69_shield_catalog_v1";
        [SerializeField] private List<ShieldDefinition> shields = new();

        public string CatalogId => catalogId;

        public IReadOnlyList<ShieldDefinition> Shields => shields;

        public void Configure(string nextCatalogId, IEnumerable<ShieldDefinition> nextShields)
        {
            catalogId = string.IsNullOrWhiteSpace(nextCatalogId) ? "m69_shield_catalog_v1" : nextCatalogId;
            shields = (nextShields ?? Enumerable.Empty<ShieldDefinition>())
                .Where(shield => shield != null && !string.IsNullOrWhiteSpace(shield.ShieldId))
                .GroupBy(shield => shield.ShieldId)
                .Select(group => group.First())
                .OrderBy(shield => shield.ShieldId)
                .ToList();
        }

        public bool TryGetShield(string shieldId, out ShieldDefinition shield)
        {
            shield = shields.FirstOrDefault(candidate => candidate != null && candidate.ShieldId == shieldId);
            return shield != null;
        }

        public ShieldDefinition Resolve(string shieldId)
        {
            if (TryGetShield(shieldId, out var shield))
            {
                return shield;
            }

            return TryGetShield(ShieldDefinition.StarterShieldId, out var starter)
                ? starter
                : null;
        }
    }
}
