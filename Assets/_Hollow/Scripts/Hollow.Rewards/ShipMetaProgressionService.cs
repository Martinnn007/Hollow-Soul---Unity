using System.Collections.Generic;
using Hollow.Data.Definitions;
using Hollow.Persistence;

namespace Hollow.Rewards
{
    public static class ShipMetaProgressionService
    {
        public static bool TryPurchase(
            IShipUpgradeStore store,
            ProfileSlotId slotId,
            ShipUpgradeDefinition upgrade,
            out ProfileSlotSummary updatedSummary,
            out string errorMessage)
        {
            updatedSummary = null;
            errorMessage = string.Empty;
            if (store == null)
            {
                errorMessage = "No profile store is available.";
                return false;
            }

            if (upgrade == null)
            {
                errorMessage = "Upgrade is missing.";
                return false;
            }

            return store.TryPurchaseShipUpgrade(slotId, upgrade.UpgradeId, upgrade.SoulCost, out updatedSummary, out errorMessage);
        }

        public static IEnumerable<PlayerStatModifier> CreatePurchasedModifiers(
            ShipUpgradeCatalogDefinition catalog,
            IEnumerable<string> purchasedUpgradeIds)
        {
            catalog ??= ShipUpgradeCatalogDefinition.CreateRuntimeDefault();
            foreach (var upgradeId in purchasedUpgradeIds ?? System.Array.Empty<string>())
            {
                if (!catalog.TryGetUpgrade(upgradeId, out var upgrade) || upgrade.StatModifier.IsEmpty)
                {
                    continue;
                }

                yield return PlayerStatModifier.FromCharacterStatModifier($"ship_upgrade:{upgrade.UpgradeId}", upgrade.StatModifier);
            }
        }
    }
}
