namespace Hollow.Persistence
{
    public interface IShipUpgradeStore
    {
        bool TryPurchaseShipUpgrade(
            ProfileSlotId slotId,
            string upgradeId,
            int soulCost,
            out ProfileSlotSummary updatedSummary,
            out string errorMessage);
    }
}
