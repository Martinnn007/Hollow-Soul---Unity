using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
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
}
