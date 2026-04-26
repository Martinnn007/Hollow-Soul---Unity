namespace Hollow.Rewards
{
    public sealed class RuntimeRewardCounter
    {
        public int ClaimedRewards { get; private set; }

        public void SetClaimedRewards(int claimedRewards)
        {
            ClaimedRewards = System.Math.Max(0, claimedRewards);
        }

        public void IncrementClaimedRewards()
        {
            ClaimedRewards++;
        }
    }
}
