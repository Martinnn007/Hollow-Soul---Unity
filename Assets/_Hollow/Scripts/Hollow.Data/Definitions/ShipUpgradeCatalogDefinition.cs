using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Spaceship/Ship Upgrade Catalog", fileName = "ShipUpgradeCatalog")]
    public sealed class ShipUpgradeCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId;
        [SerializeField] private List<ShipUpgradeDefinition> upgrades = new();

        public string CatalogId => catalogId ?? string.Empty;

        public IReadOnlyList<ShipUpgradeDefinition> Upgrades =>
            upgrades != null ? (IReadOnlyList<ShipUpgradeDefinition>)upgrades : Array.Empty<ShipUpgradeDefinition>();

        public bool TryGetUpgrade(string upgradeId, out ShipUpgradeDefinition upgrade)
        {
            upgrade = Upgrades.FirstOrDefault(candidate => candidate != null && candidate.UpgradeId == upgradeId);
            return upgrade != null;
        }

        public ShipUpgradeDefinition Resolve(string upgradeId)
        {
            return TryGetUpgrade(upgradeId, out var upgrade) ? upgrade : null;
        }

        public void Configure(string nextCatalogId, IEnumerable<ShipUpgradeDefinition> nextUpgrades)
        {
            catalogId = nextCatalogId ?? string.Empty;
            name = string.IsNullOrWhiteSpace(nextCatalogId) ? "ShipUpgradeCatalog" : nextCatalogId;
            upgrades = (nextUpgrades ?? Enumerable.Empty<ShipUpgradeDefinition>())
                .Where(upgrade => upgrade != null && !string.IsNullOrWhiteSpace(upgrade.UpgradeId))
                .GroupBy(upgrade => upgrade.UpgradeId)
                .Select(group => group.First())
                .ToList();
        }

        public static ShipUpgradeCatalogDefinition CreateRuntimeDefault()
        {
            var catalog = CreateInstance<ShipUpgradeCatalogDefinition>();
            catalog.Configure("ship_upgrade_catalog_default", new[]
            {
                Upgrade("reinforced_vitals", "Reinforced Vitals", 5, new CharacterStatModifier(maxHealth: 1)),
                Upgrade("stamina_cell", "Stamina Cell", 5, new CharacterStatModifier(maxStamina: 10f)),
                Upgrade("reactor_tuning", "Reactor Tuning", 6, new CharacterStatModifier(staminaRegen: 1f)),
                Upgrade("combat_calibration", "Combat Calibration", 8, new CharacterStatModifier(meleeDamage: 1, rangedDamage: 1))
            });
            return catalog;
        }

        private static ShipUpgradeDefinition Upgrade(
            string upgradeId,
            string displayName,
            int soulCost,
            CharacterStatModifier modifier)
        {
            var upgrade = new ShipUpgradeDefinition();
            upgrade.Configure(upgradeId, displayName, soulCost, modifier);
            return upgrade;
        }
    }
}
