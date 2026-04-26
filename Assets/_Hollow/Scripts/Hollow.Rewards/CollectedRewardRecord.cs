using System;
using Hollow.Persistence;

namespace Hollow.Rewards
{
    [Serializable]
    public sealed class CollectedRewardRecord
    {
        public CollectedRewardRecord(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls)
        {
            RoomId = roomId ?? string.Empty;
            RewardId = rewardId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            RewardKind = rewardKind;
            Souls = souls;
        }

        public string RoomId { get; }

        public string RewardId { get; }

        public string DisplayName { get; }

        public RewardKind RewardKind { get; }

        public int Souls { get; }

        public RunRewardSaveState ToSaveState()
        {
            return new RunRewardSaveState
            {
                roomId = RoomId,
                rewardId = RewardId,
                displayName = DisplayName,
                rewardKind = RewardKind.ToString(),
                souls = Souls
            };
        }

        public static CollectedRewardRecord FromSaveState(RunRewardSaveState saveState)
        {
            if (saveState == null)
            {
                return null;
            }

            Enum.TryParse(saveState.rewardKind, out RewardKind rewardKind);
            return new CollectedRewardRecord(saveState.roomId, saveState.rewardId, saveState.displayName, rewardKind, saveState.souls);
        }
    }
}
