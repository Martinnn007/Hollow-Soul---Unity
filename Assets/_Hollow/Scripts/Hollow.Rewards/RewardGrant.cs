namespace Hollow.Rewards
{
    public readonly struct RewardGrant
    {
        public RewardGrant(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls)
            : this(roomId, rewardId, displayName, rewardKind, souls, RewardEffect.DefaultsForRewardId(rewardId))
        {
        }

        public RewardGrant(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls, System.Collections.Generic.IEnumerable<RewardEffect> effects)
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

        public bool IsEmpty => string.IsNullOrWhiteSpace(RewardId) && Souls <= 0;
    }
}
