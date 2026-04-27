using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Synergies/Synergy Catalog")]
    public sealed class SynergyCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId;
        [SerializeField] private List<SynergyDefinition> synergies = new();

        public string CatalogId => catalogId;

        public IReadOnlyList<SynergyDefinition> Synergies => synergies;

        public void Configure(string nextCatalogId, IEnumerable<SynergyDefinition> nextSynergies)
        {
            catalogId = nextCatalogId ?? string.Empty;
            synergies = (nextSynergies ?? Enumerable.Empty<SynergyDefinition>())
                .Where(synergy => synergy != null && !string.IsNullOrWhiteSpace(synergy.SynergyId))
                .GroupBy(synergy => synergy.SynergyId)
                .Select(group => group.First())
                .OrderBy(synergy => synergy.SynergyId)
                .ToList();
        }

        public bool TryGetSynergy(string synergyId, out SynergyDefinition synergy)
        {
            synergy = synergies.FirstOrDefault(candidate => candidate != null && candidate.SynergyId == synergyId);
            return synergy != null;
        }
    }
}
