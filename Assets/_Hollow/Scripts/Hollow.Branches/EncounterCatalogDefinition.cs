using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class EncounterCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId = "m19_encounter_catalog_v1";
        [SerializeField] private List<EncounterDefinition> encounters = new();
        [SerializeField] private EncounterDefinition bossEncounter;

        public string CatalogId => string.IsNullOrWhiteSpace(catalogId) ? "encounter_catalog" : catalogId;

        public IReadOnlyList<EncounterDefinition> Encounters => encounters;

        public EncounterDefinition BossEncounter => bossEncounter;

        public void Configure(string nextCatalogId, IEnumerable<EncounterDefinition> nextEncounters, EncounterDefinition nextBossEncounter)
        {
            catalogId = string.IsNullOrWhiteSpace(nextCatalogId) ? "encounter_catalog" : nextCatalogId;
            encounters = nextEncounters?.Where(encounter => encounter != null).ToList() ?? new List<EncounterDefinition>();
            bossEncounter = nextBossEncounter;
        }
    }
}
