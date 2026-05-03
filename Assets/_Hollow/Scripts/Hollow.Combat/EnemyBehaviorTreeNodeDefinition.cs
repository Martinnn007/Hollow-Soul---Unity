using System;
using System.Collections.Generic;
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
}
