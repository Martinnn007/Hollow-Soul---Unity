using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    public enum ArmorRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    [CreateAssetMenu(menuName = "Hollow/Equipment/Armor Definition")]
    public sealed class ArmorDefinition : ScriptableObject
    {
        [SerializeField] private string armorId;
        [SerializeField] private string displayName;
        [SerializeField] private ArmorRarity rarity = ArmorRarity.Common;
        [SerializeField] private CharacterStatModifier statModifier;
        [SerializeField] private BuildTag[] tags = System.Array.Empty<BuildTag>();

        public string ArmorId => armorId;

        public string DisplayName => displayName;

        public ArmorRarity Rarity => rarity;

        public CharacterStatModifier StatModifier => statModifier;

        public IReadOnlyList<BuildTag> Tags => tags;

        public void Configure(
            string nextArmorId,
            string nextDisplayName,
            ArmorRarity nextRarity,
            CharacterStatModifier nextStatModifier,
            IEnumerable<BuildTag> nextTags)
        {
            armorId = nextArmorId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            rarity = nextRarity;
            statModifier = nextStatModifier;
            tags = (nextTags ?? Enumerable.Empty<BuildTag>())
                .Where(tag => tag != BuildTag.None)
                .Distinct()
                .ToArray();
        }
    }
}
