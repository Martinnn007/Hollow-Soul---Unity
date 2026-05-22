using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;

namespace Hollow.Rewards
{
    public sealed class RunEconomy
    {
        private readonly List<CollectedRewardRecord> collectedRewards = new();

        public int RunSouls { get; private set; }

        public int RunCoins { get; private set; }

        public IReadOnlyList<CollectedRewardRecord> CollectedRewards => collectedRewards;

        public void AddSouls(int amount)
        {
            RunSouls += System.Math.Max(0, amount);
        }

        public bool ApplyReward(RewardGrant grant)
        {
            if (grant.IsEmpty || collectedRewards.Any(record => record.RoomId == grant.RoomId))
            {
                return false;
            }

            RunSouls += grant.Souls;
            RunCoins += grant.Coins;
            collectedRewards.Add(new CollectedRewardRecord(grant.RoomId, grant.RewardId, grant.DisplayName, grant.RewardKind, grant.Souls, grant.Coins, grant.Effects, grant.MaxStacks));
            return true;
        }

        public bool HasCollectedRoomReward(string roomId)
        {
            return collectedRewards.Any(record => record.RoomId == roomId);
        }

        public bool SpendSouls(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (RunSouls < amount)
            {
                return false;
            }

            RunSouls -= amount;
            return true;
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (RunCoins < amount)
            {
                return false;
            }

            RunCoins -= amount;
            return true;
        }

        public RunEconomySaveState ToSaveState()
        {
            return new RunEconomySaveState
            {
                runSouls = RunSouls,
                runCoins = RunCoins,
                collectedRewards = collectedRewards.Select(record => record.ToSaveState()).ToList()
            };
        }

        public static RunEconomy FromSaveState(RunEconomySaveState saveState)
        {
            var economy = new RunEconomy();
            if (saveState == null)
            {
                return economy;
            }

            economy.RunSouls = saveState.runSouls;
            economy.RunCoins = saveState.runCoins;
            if (saveState.collectedRewards != null)
            {
                foreach (var reward in saveState.collectedRewards)
                {
                    var record = CollectedRewardRecord.FromSaveState(reward);
                    if (record != null)
                    {
                        economy.collectedRewards.Add(record);
                    }
                }
            }

            return economy;
        }
    }
}
