using UnityEngine;

namespace Hollow.Combat
{
    public sealed class EnemyBehaviorConditionNodeDefinition : EnemyBehaviorTreeNodeDefinition
    {
        [SerializeField] private EnemyBehaviorConditionKind condition = EnemyBehaviorConditionKind.Always;
        [SerializeField] private bool invert;
        [SerializeField] private EnemyAwarenessState awareness = EnemyAwarenessState.Engaged;
        [SerializeField] private EnemyInstinctDisposition disposition = EnemyInstinctDisposition.Predator;
        [SerializeField] private EnemyIntelligenceLevel intelligence = EnemyIntelligenceLevel.Instinctive;
        [SerializeField] private EnemyBehaviorId behaviorId = EnemyBehaviorId.Chaser;
        [SerializeField] private string actionId = string.Empty;
        [SerializeField] private float threshold;

        public override EnemyBehaviorTreeNodeKind Kind => EnemyBehaviorTreeNodeKind.Condition;

        public EnemyBehaviorConditionKind Condition => condition;

        public string ActionId => actionId ?? string.Empty;

        public void Configure(
            string nodeId,
            EnemyBehaviorConditionKind nextCondition,
            bool nextInvert = false,
            float nextThreshold = 0f,
            string nextActionId = "",
            EnemyAwarenessState nextAwareness = EnemyAwarenessState.Engaged,
            EnemyInstinctDisposition nextDisposition = EnemyInstinctDisposition.Predator,
            EnemyIntelligenceLevel nextIntelligence = EnemyIntelligenceLevel.Instinctive,
            EnemyBehaviorId nextBehaviorId = EnemyBehaviorId.Chaser)
        {
            ConfigureNodeId(nodeId);
            condition = nextCondition;
            invert = nextInvert;
            threshold = nextThreshold;
            actionId = nextActionId ?? string.Empty;
            awareness = nextAwareness;
            disposition = nextDisposition;
            intelligence = nextIntelligence;
            behaviorId = nextBehaviorId;
        }

        public override bool TryEvaluate(EnemyBehaviorTreeContext context, out EnemyBehaviorCommand command)
        {
            var passed = condition switch
            {
                EnemyBehaviorConditionKind.IsIdle => context.IsIdle,
                EnemyBehaviorConditionKind.AwarenessAtLeast => (int)context.Awareness >= (int)awareness,
                EnemyBehaviorConditionKind.IsEndangered => context.IsEndangered,
                EnemyBehaviorConditionKind.DistanceLessOrEqual => context.DistanceToPlayer <= threshold,
                EnemyBehaviorConditionKind.DistanceGreaterThan => context.DistanceToPlayer > threshold,
                EnemyBehaviorConditionKind.IsTooClose => context.IsTooClose,
                EnemyBehaviorConditionKind.IsTooFar => context.IsTooFar,
                EnemyBehaviorConditionKind.IsInPreferredRange => context.IsInPreferredRange,
                EnemyBehaviorConditionKind.CanStartMeleeAction => context.CanStartMeleeAction(actionId),
                EnemyBehaviorConditionKind.CanStartRangedAttack => context.CanStartRangedAttack,
                EnemyBehaviorConditionKind.CanStartChargeAttack => context.CanStartChargeAttack,
                EnemyBehaviorConditionKind.CanStartAreaAction => context.CanStartAreaAction(actionId),
                EnemyBehaviorConditionKind.CanStartGuardAction => context.CanStartGuardAction(actionId),
                EnemyBehaviorConditionKind.CanStartCreatureMoveAction => context.CanStartCreatureMoveAction(actionId),
                EnemyBehaviorConditionKind.CanStartCreatureSignalAction => context.CanStartCreatureSignalAction(actionId),
                EnemyBehaviorConditionKind.CanStartRangedAction => context.CanStartRangedAction(actionId),
                EnemyBehaviorConditionKind.ShouldSentinelEngage => context.ShouldSentinelEngage,
                EnemyBehaviorConditionKind.DeterministicChance => context.Deterministic01(NodeId) <= Mathf.Clamp01(threshold),
                EnemyBehaviorConditionKind.BehaviorMatches => context.BehaviorId == behaviorId,
                EnemyBehaviorConditionKind.DispositionMatches => context.Disposition == disposition,
                EnemyBehaviorConditionKind.IntelligenceAtLeast => (int)context.Intelligence >= (int)intelligence,
                _ => true
            };

            if (invert)
            {
                passed = !passed;
            }

            command = EnemyBehaviorCommand.None(passed ? NodeId : $"{NodeId}_failed");
            return passed;
        }
    }
}
