namespace Hollow.Combat
{
    public enum EnemyBehaviorTreeNodeKind
    {
        Selector = 0,
        Sequence = 1,
        WeightedSelector = 2,
        Condition = 3,
        Action = 4
    }

    public enum EnemyBehaviorConditionKind
    {
        Always = 0,
        IsIdle = 1,
        AwarenessAtLeast = 2,
        IsEndangered = 3,
        DistanceLessOrEqual = 4,
        DistanceGreaterThan = 5,
        IsTooClose = 6,
        IsTooFar = 7,
        IsInPreferredRange = 8,
        CanStartMeleeAction = 9,
        CanStartRangedAttack = 10,
        CanStartChargeAttack = 11,
        CanStartAreaAction = 12,
        ShouldSentinelEngage = 13,
        DeterministicChance = 14,
        BehaviorMatches = 15,
        DispositionMatches = 16,
        IntelligenceAtLeast = 17
    }

    public enum EnemyBehaviorCommandKind
    {
        None = 0,
        Hold = 1,
        MoveToPlayer = 2,
        MovePreferredRange = 3,
        Flee = 4,
        Wander = 5,
        FacePlayer = 6,
        StartMeleeAction = 7,
        StartRangedAction = 8,
        StartChargeAction = 9,
        StartAreaAction = 10,
        StartFeintWarning = 11
    }
}
