namespace Hollow.Rewards
{
    public readonly struct RewardGrant
    {
        public RewardGrant(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls)
            : this(roomId, rewardId, displayName, rewardKind, souls, 0, RewardEffect.DefaultsForRewardId(rewardId))
        {
        }

        public RewardGrant(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls, System.Collections.Generic.IEnumerable<RewardEffect> effects)
            : this(roomId, rewardId, displayName, rewardKind, souls, 0, effects)
        {
        }

        public RewardGrant(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls, int coins, System.Collections.Generic.IEnumerable<RewardEffect> effects)
        {
            RoomId = roomId ?? string.Empty;
            RewardId = rewardId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            RewardKind = rewardKind;
            Souls = souls;
            Coins = coins;
            Effects = RewardEffect.Clean(effects);
        }

        public string RoomId { get; }

        public string RewardId { get; }

        public string DisplayName { get; }

        public RewardKind RewardKind { get; }

        public int Souls { get; }

        public int Coins { get; }

        public System.Collections.Generic.IReadOnlyList<RewardEffect> Effects { get; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(RewardId) && Souls <= 0 && Coins <= 0;
    }
}
