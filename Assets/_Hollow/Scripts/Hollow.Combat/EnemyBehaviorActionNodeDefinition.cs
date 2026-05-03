using UnityEngine;

namespace Hollow.Combat
{
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
