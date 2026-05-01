using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Equipment/Shield Definition")]
    public sealed class ShieldDefinition : ScriptableObject
    {
        public const string StarterShieldId = "starter_buckler";

        [SerializeField] private string shieldId = StarterShieldId;
        [SerializeField] private string displayName = "Starter Buckler";
        [SerializeField] private ArmorRarity rarity = ArmorRarity.Common;
        [SerializeField] private EquipmentLoadClass loadClass = EquipmentLoadClass.Light;
        [SerializeField] private CharacterStatModifier statModifier;
        [SerializeField] private BuildTag[] tags = System.Array.Empty<BuildTag>();

        public string ShieldId => string.IsNullOrWhiteSpace(shieldId) ? StarterShieldId : shieldId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Starter Buckler" : displayName;

        public ArmorRarity Rarity => rarity;

        public EquipmentLoadClass LoadClass => loadClass;

        public CharacterStatModifier StatModifier => statModifier;

        public IReadOnlyList<BuildTag> Tags => tags;

        public void Configure(
            string nextShieldId,
            string nextDisplayName,
            ArmorRarity nextRarity,
            EquipmentLoadClass nextLoadClass,
            CharacterStatModifier nextStatModifier,
            IEnumerable<BuildTag> nextTags)
        {
            shieldId = string.IsNullOrWhiteSpace(nextShieldId) ? StarterShieldId : nextShieldId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? "Starter Buckler" : nextDisplayName;
            rarity = nextRarity;
            loadClass = nextLoadClass;
            statModifier = nextStatModifier;
            tags = (nextTags ?? Enumerable.Empty<BuildTag>())
                .Where(tag => tag != BuildTag.None)
                .Distinct()
                .ToArray();
        }

        public static ShieldDefinition CreateRuntimeStarter()
        {
            var shield = CreateInstance<ShieldDefinition>();
            shield.Configure(
                StarterShieldId,
                "Starter Buckler",
                ArmorRarity.Common,
                EquipmentLoadClass.Light,
                default,
                new[] { BuildTag.Defense });
            return shield;
        }
    }
}
