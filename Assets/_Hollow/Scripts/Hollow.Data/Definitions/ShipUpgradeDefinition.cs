using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public sealed class ShipUpgradeDefinition
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private string displayName;
        [SerializeField] private int soulCost;
        [SerializeField] private CharacterStatModifier statModifier;

        public string UpgradeId => upgradeId ?? string.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? UpgradeId : displayName;

        public int SoulCost => Mathf.Max(0, soulCost);

        public CharacterStatModifier StatModifier => statModifier;

        public void Configure(
            string nextUpgradeId,
            string nextDisplayName,
            int nextSoulCost,
            CharacterStatModifier nextStatModifier)
        {
            upgradeId = nextUpgradeId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            soulCost = Mathf.Max(0, nextSoulCost);
            statModifier = nextStatModifier;
        }
    }
}
