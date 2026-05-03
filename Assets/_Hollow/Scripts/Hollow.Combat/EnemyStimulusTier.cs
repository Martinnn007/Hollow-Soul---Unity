namespace Hollow.Combat
{
    public enum EnemyStimulusTier
    {
        Quiet = 0,
        Normal = 1,
        Loud = 2,
        Violent = 3
    }

    public static class EnemyStimulusTierExtensions
    {
        public static EnemyStimulusTier DefaultFor(EnemyStimulusKind kind)
        {
            return kind switch
            {
                EnemyStimulusKind.Footstep => EnemyStimulusTier.Quiet,
                EnemyStimulusKind.Damage => EnemyStimulusTier.Violent,
                EnemyStimulusKind.GuardImpact => EnemyStimulusTier.Loud,
                _ => EnemyStimulusTier.Normal
            };
        }

        public static float Score(this EnemyStimulusTier tier)
        {
            return tier switch
            {
                EnemyStimulusTier.Quiet => 0.55f,
                EnemyStimulusTier.Normal => 1f,
                EnemyStimulusTier.Loud => 1.75f,
                EnemyStimulusTier.Violent => 3f,
                _ => 1f
            };
        }

        public static float HearingMultiplier(this EnemyStimulusTier tier)
        {
            return tier switch
            {
                EnemyStimulusTier.Quiet => 0.85f,
                EnemyStimulusTier.Normal => 1f,
                EnemyStimulusTier.Loud => 1.35f,
                EnemyStimulusTier.Violent => 1.75f,
                _ => 1f
            };
        }
    }
}
