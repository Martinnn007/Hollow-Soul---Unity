using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Enemy Behavior Tree", fileName = "EnemyBehaviorTree")]
    public sealed class EnemyBehaviorTreeDefinition : ScriptableObject
    {
        [SerializeField] private string treeId = "enemy_tree";
        [SerializeField] private string displayName = "Enemy Behavior Tree";
        [SerializeField] private string ownerId = string.Empty;
        [SerializeField] private bool bossMetadataOnly;
        [SerializeField] private EnemyBehaviorTreeNodeDefinition rootNode;
        [SerializeField] private List<EnemyBehaviorTreeNodeDefinition> nodes = new();

        public string TreeId => string.IsNullOrWhiteSpace(treeId) ? "enemy_tree" : treeId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TreeId : displayName;

        public string OwnerId => ownerId ?? string.Empty;

        public bool BossMetadataOnly => bossMetadataOnly;

        public EnemyBehaviorTreeNodeDefinition RootNode => rootNode;

        public IReadOnlyList<EnemyBehaviorTreeNodeDefinition> Nodes => nodes?
            .Where(node => node != null)
            .Distinct()
            .ToArray() ?? Array.Empty<EnemyBehaviorTreeNodeDefinition>();

        public void Configure(
            string nextTreeId,
            string nextDisplayName,
            string nextOwnerId,
            bool nextBossMetadataOnly,
            EnemyBehaviorTreeNodeDefinition nextRootNode,
            IEnumerable<EnemyBehaviorTreeNodeDefinition> nextNodes)
        {
            treeId = string.IsNullOrWhiteSpace(nextTreeId) ? "enemy_tree" : nextTreeId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? treeId : nextDisplayName;
            ownerId = nextOwnerId ?? string.Empty;
            bossMetadataOnly = nextBossMetadataOnly;
            rootNode = nextRootNode;
            nodes = nextNodes?.Where(node => node != null).Distinct().ToList() ?? new List<EnemyBehaviorTreeNodeDefinition>();
        }

        public bool TryEvaluate(EnemyBehaviorTreeContext context, out EnemyBehaviorCommand command)
        {
            if (rootNode == null)
            {
                command = EnemyBehaviorCommand.None("tree_missing_root");
                return false;
            }

            return rootNode.TryEvaluate(context, out command);
        }
    }
}
