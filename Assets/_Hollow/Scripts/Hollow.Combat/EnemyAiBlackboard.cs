namespace Hollow.Combat
{
    public readonly struct EnemyAiBlackboard
    {
        public EnemyAiBlackboard(
            EnemyAiLodTier lodTier,
            EnemyBehaviorCommandKind treeCommand,
            EnemyBehaviorCommandKind chosenCommand,
            string chosenActionId,
            float chosenScore,
            float pressurePenalty,
            float targetDistanceMeters,
            EnemyPathStatus pathStatus,
            string cooldownReason,
            string topScores)
        {
            LodTier = lodTier;
            TreeCommand = treeCommand;
            ChosenCommand = chosenCommand;
            ChosenActionId = chosenActionId ?? string.Empty;
            ChosenScore = chosenScore;
            PressurePenalty = pressurePenalty;
            TargetDistanceMeters = targetDistanceMeters;
            PathStatus = pathStatus;
            CooldownReason = cooldownReason ?? string.Empty;
            TopScores = topScores ?? string.Empty;
        }

        public EnemyAiLodTier LodTier { get; }

        public EnemyBehaviorCommandKind TreeCommand { get; }

        public EnemyBehaviorCommandKind ChosenCommand { get; }

        public string ChosenActionId { get; }

        public float ChosenScore { get; }

        public float PressurePenalty { get; }

        public float TargetDistanceMeters { get; }

        public EnemyPathStatus PathStatus { get; }

        public string CooldownReason { get; }

        public string TopScores { get; }

        public string Summary => $"LOD {LodTier} | {ChosenCommand}:{ChosenActionId} | score {ChosenScore:0.00} | pressure -{PressurePenalty:0.00} | dist {TargetDistanceMeters:0.0} | path {PathStatus} | {CooldownReason} | {TopScores}";

        public static EnemyAiBlackboard Empty { get; } = new(
            EnemyAiLodTier.Background,
            EnemyBehaviorCommandKind.None,
            EnemyBehaviorCommandKind.None,
            string.Empty,
            0f,
            0f,
            0f,
            EnemyPathStatus.NotRequested,
            string.Empty,
            string.Empty);
    }
}
