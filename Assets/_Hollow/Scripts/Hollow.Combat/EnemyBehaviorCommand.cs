namespace Hollow.Combat
{
    public readonly struct EnemyBehaviorCommand
    {
        public EnemyBehaviorCommand(EnemyBehaviorCommandKind kind, string actionId, float speedMultiplier, string reason)
        {
            Kind = kind;
            ActionId = actionId ?? string.Empty;
            SpeedMultiplier = speedMultiplier;
            Reason = reason ?? string.Empty;
        }

        public EnemyBehaviorCommandKind Kind { get; }

        public string ActionId { get; }

        public float SpeedMultiplier { get; }

        public string Reason { get; }

        public bool StartsCommittedAction => Kind is EnemyBehaviorCommandKind.StartMeleeAction
            or EnemyBehaviorCommandKind.StartRangedAction
            or EnemyBehaviorCommandKind.StartChargeAction
            or EnemyBehaviorCommandKind.StartAreaAction
            or EnemyBehaviorCommandKind.StartFeintWarning
            or EnemyBehaviorCommandKind.StartGuardAction
            or EnemyBehaviorCommandKind.StartCreatureMoveAction
            or EnemyBehaviorCommandKind.StartCreatureSignalAction;

        public static EnemyBehaviorCommand None(string reason = "")
        {
            return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.None, string.Empty, 0f, reason);
        }
    }
}
