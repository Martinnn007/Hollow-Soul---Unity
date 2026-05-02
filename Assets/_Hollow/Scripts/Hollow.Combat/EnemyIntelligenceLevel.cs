namespace Hollow.Combat
{
    public enum EnemyIntelligenceLevel
    {
        Instinctive = 0,
        Simple = 1,
        Basic = 2,
        Trained = 3,
        Tactical = 4,
        Cunning = 5
    }

    public static class EnemyIntelligenceLevelExtensions
    {
        public static EnemyIntelligenceLevel Clamp(int value)
        {
            if (value <= (int)EnemyIntelligenceLevel.Instinctive)
            {
                return EnemyIntelligenceLevel.Instinctive;
            }

            if (value >= (int)EnemyIntelligenceLevel.Cunning)
            {
                return EnemyIntelligenceLevel.Cunning;
            }

            return (EnemyIntelligenceLevel)value;
        }

        public static string DisplayLabel(this EnemyIntelligenceLevel level)
        {
            var safeLevel = Clamp((int)level);
            return $"{(int)safeLevel} {safeLevel}";
        }
    }
}
