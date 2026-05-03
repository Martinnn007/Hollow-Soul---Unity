namespace Hollow.Combat
{
    public readonly struct EnemyAiActionScore
    {
        public EnemyAiActionScore(
            EnemyActionProfileDefinition actionProfile,
            EnemyAttackProfileDefinition attackProfile,
            EnemyBehaviorCommandKind commandKind,
            float score,
            float pressurePenalty,
            string reason)
        {
            ActionProfile = actionProfile;
            AttackProfile = attackProfile;
            CommandKind = commandKind;
            Score = score;
            PressurePenalty = pressurePenalty;
            Reason = reason ?? string.Empty;
        }

        public EnemyActionProfileDefinition ActionProfile { get; }

        public EnemyAttackProfileDefinition AttackProfile { get; }

        public EnemyBehaviorCommandKind CommandKind { get; }

        public float Score { get; }

        public float PressurePenalty { get; }

        public string Reason { get; }

        public string ActionId => AttackProfile != null
            ? AttackProfile.AttackId
            : ActionProfile != null ? ActionProfile.ActionId : string.Empty;

        public bool IsValid => ActionProfile != null;
    }
}
