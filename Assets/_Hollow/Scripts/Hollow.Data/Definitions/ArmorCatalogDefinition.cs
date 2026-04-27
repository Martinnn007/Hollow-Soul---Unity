using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Equipment/Armor Catalog")]
    public sealed class ArmorCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId;
        [SerializeField] private List<ArmorDefinition> armors = new();

        public string CatalogId => catalogId;

        public IReadOnlyList<ArmorDefinition> Armors => armors;

        public void Configure(string nextCatalogId, IEnumerable<ArmorDefinition> nextArmors)
        {
            catalogId = nextCatalogId ?? string.Empty;
            armors = (nextArmors ?? Enumerable.Empty<ArmorDefinition>())
                .Where(armor => armor != null && !string.IsNullOrWhiteSpace(armor.ArmorId))
                .GroupBy(armor => armor.ArmorId)
                .Select(group => group.First())
                .OrderBy(armor => armor.ArmorId)
                .ToList();
        }

        public bool TryGetArmor(string armorId, out ArmorDefinition armor)
        {
            armor = armors.FirstOrDefault(candidate => candidate != null && candidate.ArmorId == armorId);
            return armor != null;
        }
    }
}
