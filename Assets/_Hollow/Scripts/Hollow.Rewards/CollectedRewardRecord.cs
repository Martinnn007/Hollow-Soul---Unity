using System;
using System.Linq;
using Hollow.Persistence;

namespace Hollow.Rewards
{
    [Serializable]
    public sealed class CollectedRewardRecord
    {
        public CollectedRewardRecord(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls)
            : this(roomId, rewardId, displayName, rewardKind, souls, 0, RewardEffect.DefaultsForRewardId(rewardId))
        {
        }

        public CollectedRewardRecord(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls, System.Collections.Generic.IEnumerable<RewardEffect> effects)
            : this(roomId, rewardId, displayName, rewardKind, souls, 0, effects)
        {
        }

        public CollectedRewardRecord(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls, int coins, System.Collections.Generic.IEnumerable<RewardEffect> effects)
            : this(roomId, rewardId, displayName, rewardKind, souls, coins, effects, 1)
        {
        }

        public CollectedRewardRecord(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls, int coins, System.Collections.Generic.IEnumerable<RewardEffect> effects, int maxStacks)
        {
            RoomId = roomId ?? string.Empty;
            RewardId = rewardId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            RewardKind = rewardKind;
            Souls = souls;
            Coins = coins;
            Effects = RewardEffect.Clean(effects);
            MaxStacks = System.Math.Max(1, maxStacks);
        }

        public string RoomId { get; }

        public string RewardId { get; }

        public string DisplayName { get; }

        public RewardKind RewardKind { get; }

        public int Souls { get; }

        public int Coins { get; }

        public System.Collections.Generic.IReadOnlyList<RewardEffect> Effects { get; }

        public int MaxStacks { get; }

        public RunRewardSaveState ToSaveState()
        {
            return new RunRewardSaveState
            {
                roomId = RoomId,
                rewardId = RewardId,
                displayName = DisplayName,
                rewardKind = RewardKind.ToString(),
                souls = Souls,
                coins = Coins,
                maxStacks = MaxStacks,
                effects = Effects?.Select(effect => effect.ToSaveState()).ToList() ?? new System.Collections.Generic.List<RunRewardEffectSaveState>()
            };
        }

        public static CollectedRewardRecord FromSaveState(RunRewardSaveState saveState)
        {
            if (saveState == null)
            {
                return null;
            }

            Enum.TryParse(saveState.rewardKind, out RewardKind rewardKind);
            var effects = saveState.effects != null && saveState.effects.Count > 0
                ? saveState.effects.Select(RewardEffect.FromSaveState).ToArray()
                : RewardEffect.DefaultsForRewardId(saveState.rewardId);
            return new CollectedRewardRecord(saveState.roomId, saveState.rewardId, saveState.displayName, rewardKind, saveState.souls, saveState.coins, effects, saveState.maxStacks);
        }
    }
}
