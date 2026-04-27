using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Characters/Passive Skill Definition")]
    public sealed class CharacterPassiveSkillDefinition : ScriptableObject
    {
        [SerializeField] private string skillId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private BuildTag[] tags = System.Array.Empty<BuildTag>();

        public string SkillId => skillId;

        public string DisplayName => displayName;

        public string Description => description;

        public IReadOnlyList<BuildTag> Tags => tags;

        public void Configure(string nextSkillId, string nextDisplayName, string nextDescription, IEnumerable<BuildTag> nextTags = null)
        {
            skillId = nextSkillId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            description = nextDescription ?? string.Empty;
            tags = (nextTags ?? Enumerable.Empty<BuildTag>())
                .Where(tag => tag != BuildTag.None)
                .Distinct()
                .ToArray();
        }
    }
}
