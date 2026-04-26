using Hollow.Persistence;

namespace Hollow.Rewards
{
    public static class MetaProgressionService
    {
        public static void CompleteRun(IRunSaveStore store, ProfileSlotId slotId, RunEconomy economy)
        {
            if (store == null || economy == null)
            {
                return;
            }

            store.CompleteActiveRun(slotId, new RunCompletionSummary
            {
                soulsToBank = economy.RunSouls,
                rewardsClaimed = economy.CollectedRewards.Count
            });
        }
    }
}
