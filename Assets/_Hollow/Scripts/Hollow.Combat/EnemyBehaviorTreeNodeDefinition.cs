using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public abstract class EnemyBehaviorTreeNodeDefinition : ScriptableObject
    {
        [SerializeField] private string nodeId = "node";

        public string NodeId => string.IsNullOrWhiteSpace(nodeId) ? name : nodeId;

        public abstract EnemyBehaviorTreeNodeKind Kind { get; }

        public virtual IReadOnlyList<EnemyBehaviorTreeNodeDefinition> Children => Array.Empty<EnemyBehaviorTreeNodeDefinition>();

        public void ConfigureNodeId(string nextNodeId)
        {
            nodeId = string.IsNullOrWhiteSpace(nextNodeId) ? "node" : nextNodeId;
            name = nodeId;
        }

        public abstract bool TryEvaluate(EnemyBehaviorTreeContext context, out EnemyBehaviorCommand command);
    }

    public sealed class EnemyBehaviorSelectorNodeDefinition : EnemyBehaviorTreeNodeDefinition
    {
        [SerializeField] private List<EnemyBehaviorTreeNodeDefinition> children = new();

        public override EnemyBehaviorTreeNodeKind Kind => EnemyBehaviorTreeNodeKind.Selector;

        public override IReadOnlyList<EnemyBehaviorTreeNodeDefinition> Children => children;

        public void Configure(string nodeId, IEnumerable<EnemyBehaviorTreeNodeDefinition> nextChildren)
        {
            ConfigureNodeId(nodeId);
            children = nextChildren?.Where(child => child != null).ToList() ?? new List<EnemyBehaviorTreeNodeDefinition>();
        }

        public override bool TryEvaluate(EnemyBehaviorTreeContext context, out EnemyBehaviorCommand command)
        {
            foreach (var child in children)
            {
                if (child != null && child.TryEvaluate(context, out command))
                {
                    return true;
                }
            }

            command = EnemyBehaviorCommand.None("selector_failed");
            return false;
        }
    }

    public sealed class EnemyBehaviorSequenceNodeDefinition : EnemyBehaviorTreeNodeDefinition
    {
        [SerializeField] private List<EnemyBehaviorTreeNodeDefinition> children = new();

        public override EnemyBehaviorTreeNodeKind Kind => EnemyBehaviorTreeNodeKind.Sequence;

        public override IReadOnlyList<EnemyBehaviorTreeNodeDefinition> Children => children;

        public void Configure(string nodeId, IEnumerable<EnemyBehaviorTreeNodeDefinition> nextChildren)
        {
            ConfigureNodeId(nodeId);
            children = nextChildren?.Where(child => child != null).ToList() ?? new List<EnemyBehaviorTreeNodeDefinition>();
        }

        public override bool TryEvaluate(EnemyBehaviorTreeContext context, out EnemyBehaviorCommand command)
        {
            command = EnemyBehaviorCommand.None(NodeId);
            foreach (var child in children)
            {
                if (child == null || !child.TryEvaluate(context, out command))
                {
                    command = EnemyBehaviorCommand.None($"{NodeId}_failed");
                    return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public sealed class EnemyBehaviorWeightedChild
    {
        [SerializeField] private EnemyBehaviorTreeNodeDefinition child;
        [SerializeField] private float weight = 1f;

        public EnemyBehaviorTreeNodeDefinition Child => child;

        public float Weight => Mathf.Max(0.01f, weight);

        public void Configure(EnemyBehaviorTreeNodeDefinition nextChild, float nextWeight)
        {
            child = nextChild;
            weight = Mathf.Max(0.01f, nextWeight);
        }
    }

    public sealed class EnemyBehaviorWeightedSelectorNodeDefinition : EnemyBehaviorTreeNodeDefinition
    {
        [SerializeField] private List<EnemyBehaviorWeightedChild> children = new();

        public override EnemyBehaviorTreeNodeKind Kind => EnemyBehaviorTreeNodeKind.WeightedSelector;

        public IReadOnlyList<EnemyBehaviorWeightedChild> WeightedChildren => children;

        public override IReadOnlyList<EnemyBehaviorTreeNodeDefinition> Children => children
            .Select(child => child.Child)
            .Where(child => child != null)
            .ToArray();

        public void Configure(string nodeId, IEnumerable<(EnemyBehaviorTreeNodeDefinition Child, float Weight)> nextChildren)
        {
            ConfigureNodeId(nodeId);
            children = new List<EnemyBehaviorWeightedChild>();
            if (nextChildren == null)
            {
                return;
            }

            foreach (var row in nextChildren)
            {
                if (row.Child == null)
                {
                    continue;
                }

                var entry = new EnemyBehaviorWeightedChild();
                entry.Configure(row.Child, row.Weight);
                children.Add(entry);
            }
        }

        public override bool TryEvaluate(EnemyBehaviorTreeContext context, out EnemyBehaviorCommand command)
        {
            var valid = children.Where(child => child.Child != null).ToArray();
            if (valid.Length == 0)
            {
                command = EnemyBehaviorCommand.None("weighted_empty");
                return false;
            }

            var totalWeight = valid.Sum(child => child.Weight);
            var roll = context.Deterministic01(NodeId) * totalWeight;
            foreach (var child in valid)
            {
                roll -= child.Weight;
                if (roll > 0f)
                {
                    continue;
                }

                if (child.Child.TryEvaluate(context, out command))
                {
                    return true;
                }

                break;
            }

            foreach (var child in valid)
            {
                if (child.Child.TryEvaluate(context, out command))
                {
                    return true;
                }
            }

            command = EnemyBehaviorCommand.None($"{NodeId}_failed");
            return false;
        }
    }

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

    public sealed class EnemyBehaviorActionNodeDefinition : EnemyBehaviorTreeNodeDefinition
    {
        [SerializeField] private EnemyBehaviorCommandKind commandKind = EnemyBehaviorCommandKind.None;
        [SerializeField] private string actionId = string.Empty;
        [SerializeField] private float speedMultiplier = 1f;
        [TextArea(1, 3)]
        [SerializeField] private string reason = string.Empty;

        public override EnemyBehaviorTreeNodeKind Kind => EnemyBehaviorTreeNodeKind.Action;

        public EnemyBehaviorCommandKind CommandKind => commandKind;

        public string ActionId => actionId ?? string.Empty;

        public void Configure(string nodeId, EnemyBehaviorCommandKind nextCommandKind, string nextActionId = "", float nextSpeedMultiplier = 1f, string nextReason = "")
        {
            ConfigureNodeId(nodeId);
            commandKind = nextCommandKind;
            actionId = nextActionId ?? string.Empty;
            speedMultiplier = Mathf.Max(0f, nextSpeedMultiplier);
            reason = nextReason ?? string.Empty;
        }

        public override bool TryEvaluate(EnemyBehaviorTreeContext context, out EnemyBehaviorCommand command)
        {
            command = new EnemyBehaviorCommand(commandKind, actionId, speedMultiplier, string.IsNullOrWhiteSpace(reason) ? NodeId : reason);
            return true;
        }
    }
}
