namespace Hollow.Rewards
{
    public readonly struct RewardApplicationResult
    {
        public RewardApplicationResult(bool applied, int healAmount, string message)
        {
            Applied = applied;
            HealAmount = healAmount;
            Message = message ?? string.Empty;
        }

        public bool Applied { get; }

        public int HealAmount { get; }

        public string Message { get; }
    }
}
