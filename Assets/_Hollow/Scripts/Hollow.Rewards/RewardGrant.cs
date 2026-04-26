namespace Hollow.Rewards
{
    public readonly struct RewardGrant
    {
        public RewardGrant(string roomId, string rewardId, string displayName, RewardKind rewardKind, int souls)
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

        public bool IsEmpty => string.IsNullOrWhiteSpace(RewardId) && Souls <= 0;
    }
}
