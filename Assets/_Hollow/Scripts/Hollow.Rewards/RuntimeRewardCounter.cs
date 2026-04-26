namespace Hollow.Rewards
{
    public sealed class RuntimeRewardCounter
    {
        public int ClaimedRewards { get; private set; }

        public void IncrementClaimedRewards()
        {
            ClaimedRewards++;
        }
    }
}
