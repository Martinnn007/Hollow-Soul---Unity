using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/ArtPass/Target Catalog", fileName = "ArtPassTargetCatalog")]
    public sealed class ArtPassTargetCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId = string.Empty;
        [SerializeField] private ArtPassAssetTargetDefinition[] targets = Array.Empty<ArtPassAssetTargetDefinition>();

        public string CatalogId => catalogId;

        public IReadOnlyList<ArtPassAssetTargetDefinition> Targets => targets;

        public void Configure(string nextCatalogId, IEnumerable<ArtPassAssetTargetDefinition> nextTargets)
        {
            catalogId = nextCatalogId ?? string.Empty;
            targets = (nextTargets ?? Enumerable.Empty<ArtPassAssetTargetDefinition>())
                .Where(target => target != null)
                .OrderBy(target => target.Priority)
                .ThenBy(target => target.Group)
                .ThenBy(target => target.TargetId)
                .ToArray();
        }

        public bool TryGet(string targetId, out ArtPassAssetTargetDefinition target)
        {
            target = targets.FirstOrDefault(candidate => candidate != null && candidate.TargetId == targetId);
            return target != null;
        }
    }
}
