using System.Collections.Generic;
using System.Linq;

namespace Hollow.Combat
{
    public sealed class EnemyBehaviorSequenceNodeDefinition : EnemyBehaviorTreeNodeDefinition
    {
        [UnityEngine.SerializeField] private List<EnemyBehaviorTreeNodeDefinition> children = new();

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
}
