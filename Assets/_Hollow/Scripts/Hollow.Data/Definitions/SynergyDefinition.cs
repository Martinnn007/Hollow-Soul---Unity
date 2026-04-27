using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Synergies/Synergy Definition")]
    public sealed class SynergyDefinition : ScriptableObject
    {
        [SerializeField] private string synergyId;
        [SerializeField] private string displayName;
        [SerializeField] private BuildTag requiredTag;
        [SerializeField] private int requiredTagCount = 2;
        [SerializeField] private string[] requiredIds = System.Array.Empty<string>();
        [SerializeField] private PlayerBaseStats statBonus;

        public string SynergyId => synergyId;

        public string DisplayName => displayName;

        public BuildTag RequiredTag => requiredTag;

        public int RequiredTagCount => Mathf.Max(1, requiredTagCount);

        public IReadOnlyList<string> RequiredIds => requiredIds;

        public PlayerBaseStats StatBonus => statBonus;

        public void Configure(
            string nextSynergyId,
            string nextDisplayName,
            BuildTag nextRequiredTag,
            int nextRequiredTagCount,
            IEnumerable<string> nextRequiredIds,
            PlayerBaseStats nextStatBonus)
        {
            synergyId = nextSynergyId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            requiredTag = nextRequiredTag;
            requiredTagCount = Mathf.Max(1, nextRequiredTagCount);
            requiredIds = (nextRequiredIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToArray();
            statBonus = nextStatBonus;
        }
    }
}
