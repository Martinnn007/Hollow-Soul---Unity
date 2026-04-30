namespace Hollow.Rewards
{
    public readonly struct ChestRewardContents
    {
        public ChestRewardContents(int coinValue, RewardGrant rewardGrant)
        {
            CoinValue = System.Math.Max(0, coinValue);
            RewardGrant = rewardGrant;
        }

        public int CoinValue { get; }

        public RewardGrant RewardGrant { get; }

        public bool IsEmpty => CoinValue <= 0 && RewardGrant.IsEmpty;
    }
}
