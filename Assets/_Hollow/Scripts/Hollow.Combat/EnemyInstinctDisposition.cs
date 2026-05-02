namespace Hollow.Combat
{
    public enum EnemyInstinctDisposition
    {
        Predator = 0,
        Prey = 1,
        Sentinel = 2,
        Mindless = 3
    }

    public static class EnemyInstinctDispositionExtensions
    {
        public static EnemyInstinctDisposition Clamp(int value)
        {
            if (value <= (int)EnemyInstinctDisposition.Predator)
            {
                return EnemyInstinctDisposition.Predator;
            }

            if (value >= (int)EnemyInstinctDisposition.Mindless)
            {
                return EnemyInstinctDisposition.Mindless;
            }

            return (EnemyInstinctDisposition)value;
        }

        public static string ToSaveString(this EnemyInstinctDisposition disposition)
        {
            return Clamp((int)disposition).ToString().ToLowerInvariant();
        }

        public static EnemyInstinctDisposition FromSaveString(string value, EnemyInstinctDisposition fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "prey" => EnemyInstinctDisposition.Prey,
                "sentinel" => EnemyInstinctDisposition.Sentinel,
                "mindless" => EnemyInstinctDisposition.Mindless,
                "predator" => EnemyInstinctDisposition.Predator,
                _ => fallback
            };
        }
    }
}
