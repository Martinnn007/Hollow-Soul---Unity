using System;
using System.Linq;
using Hollow.Persistence;

namespace Hollow.Rewards
{
    [Serializable]
    public sealed class CollectedRewardRecord
    {
        public CollectedRewardRecord(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls)
            : this(roomId, rewardId, displayName, rewardKind, souls, RewardEffect.DefaultsForRewardId(rewardId))
        {
        }

        public CollectedRewardRecord(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls, System.Collections.Generic.IEnumerable<RewardEffect> effects)
        {
            RoomId = roomId ?? string.Empty;
            RewardId = rewardId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            RewardKind = rewardKind;
            Souls = souls;
            Effects = RewardEffect.Clean(effects);
        }

        public string RoomId { get; }

        public string RewardId { get; }

        public string DisplayName { get; }

        public RewardKind RewardKind { get; }

        public int Souls { get; }

        public System.Collections.Generic.IReadOnlyList<RewardEffect> Effects { get; }

        public RunRewardSaveState ToSaveState()
        {
            return new RunRewardSaveState
            {
                roomId = RoomId,
                rewardId = RewardId,
                displayName = DisplayName,
                rewardKind = RewardKind.ToString(),
                souls = Souls,
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
            return new CollectedRewardRecord(saveState.roomId, saveState.rewardId, saveState.displayName, rewardKind, saveState.souls, effects);
        }
    }
}
