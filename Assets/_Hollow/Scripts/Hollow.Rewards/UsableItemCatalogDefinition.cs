using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Rewards
{
    [CreateAssetMenu(menuName = "Hollow/Rewards/Usable Item Catalog")]
    public sealed class UsableItemCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId;
        [SerializeField] private List<UsableItemDefinition> items = new();

        public string CatalogId => catalogId;

        public IReadOnlyList<UsableItemDefinition> Items => items;

        public void Configure(string nextCatalogId, IEnumerable<UsableItemDefinition> nextItems)
        {
            catalogId = nextCatalogId ?? string.Empty;
            items = (nextItems ?? Enumerable.Empty<UsableItemDefinition>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.ItemId))
                .GroupBy(item => item.ItemId)
                .Select(group => group.First())
                .OrderBy(item => item.ItemId)
                .ToList();
        }

        public bool TryGet(string itemId, out UsableItemDefinition item)
        {
            item = items.FirstOrDefault(candidate => candidate != null && candidate.ItemId == itemId);
            return item != null;
        }
    }
}
