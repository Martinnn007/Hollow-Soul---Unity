using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Characters/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string characterId;
        [SerializeField] private string displayName;
        [SerializeField] private PlayerBaseStats baseStats;
        [SerializeField] private string starterMeleeWeaponId = "starter_blade";
        [SerializeField] private string starterRangedWeaponId = "starter_bow";
        [SerializeField] private string starterPassiveRewardId;
        [SerializeField] private CharacterPassiveSkillDefinition passiveSkill;
        [SerializeField] private BuildTag[] tags = System.Array.Empty<BuildTag>();

        public string CharacterId => characterId;

        public string DisplayName => displayName;

        public PlayerBaseStats BaseStats => baseStats.IsConfigured ? baseStats : PlayerBaseStats.Default;

        public string StarterMeleeWeaponId => starterMeleeWeaponId;

        public string StarterRangedWeaponId => starterRangedWeaponId;

        public string StarterPassiveRewardId => starterPassiveRewardId;

        public CharacterPassiveSkillDefinition PassiveSkill => passiveSkill;

        public IReadOnlyList<BuildTag> Tags => tags;

        public void Configure(
            string nextCharacterId,
            string nextDisplayName,
            PlayerBaseStats nextBaseStats,
            string nextStarterMeleeWeaponId,
            string nextStarterRangedWeaponId,
            CharacterPassiveSkillDefinition nextPassiveSkill,
            string nextStarterPassiveRewardId = "",
            IEnumerable<BuildTag> nextTags = null)
        {
            characterId = nextCharacterId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            baseStats = nextBaseStats.IsConfigured ? nextBaseStats : PlayerBaseStats.Default;
            starterMeleeWeaponId = string.IsNullOrWhiteSpace(nextStarterMeleeWeaponId) ? "starter_blade" : nextStarterMeleeWeaponId;
            starterRangedWeaponId = string.IsNullOrWhiteSpace(nextStarterRangedWeaponId) ? "starter_bow" : nextStarterRangedWeaponId;
            passiveSkill = nextPassiveSkill;
            starterPassiveRewardId = nextStarterPassiveRewardId ?? string.Empty;
            tags = (nextTags ?? Enumerable.Empty<BuildTag>())
                .Where(tag => tag != BuildTag.None)
                .Distinct()
                .ToArray();
        }
    }
}
