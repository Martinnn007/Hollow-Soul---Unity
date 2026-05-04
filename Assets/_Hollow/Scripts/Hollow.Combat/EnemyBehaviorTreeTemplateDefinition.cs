using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public enum EnemyBehaviorTreeTemplateRole
    {
        ChaserPressure = 0,
        PreyFleeCommit = 1,
        StationaryRangedSentinel = 2,
        WeaponUser = 3,
        CreatureSkirmisher = 4,
        CasterRanged = 5,
        BossMetadata = 6
    }

    [CreateAssetMenu(menuName = "Hollow/Combat/Enemy Behavior Tree Template", fileName = "EnemyBehaviorTreeTemplate")]
    public sealed class EnemyBehaviorTreeTemplateDefinition : ScriptableObject
    {
        [SerializeField] private string templateId = "behavior_tree_template";
        [SerializeField] private string displayName = "Behavior Tree Template";
        [TextArea(2, 5)]
        [SerializeField] private string description = string.Empty;
        [SerializeField] private EnemyBehaviorTreeTemplateRole role = EnemyBehaviorTreeTemplateRole.ChaserPressure;
        [SerializeField] private EnemyBehaviorId recommendedBehaviorId = EnemyBehaviorId.Chaser;
        [SerializeField] private EnemyInstinctDisposition recommendedDisposition = EnemyInstinctDisposition.Predator;
        [SerializeField] private EnemyIntelligenceLevel minimumIntelligence = EnemyIntelligenceLevel.Instinctive;
        [SerializeField] private bool bossMetadataOnly;
        [SerializeField] private EnemyBehaviorTreeNodeDefinition rootNode;
        [SerializeField] private List<EnemyBehaviorTreeNodeDefinition> nodes = new();

        public string TemplateId => string.IsNullOrWhiteSpace(templateId) ? "behavior_tree_template" : templateId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TemplateId : displayName;

        public string Description => description ?? string.Empty;

        public EnemyBehaviorTreeTemplateRole Role => role;

        public EnemyBehaviorId RecommendedBehaviorId => recommendedBehaviorId;

        public EnemyInstinctDisposition RecommendedDisposition => recommendedDisposition;

        public EnemyIntelligenceLevel MinimumIntelligence => EnemyIntelligenceLevelExtensions.Clamp((int)minimumIntelligence);

        public bool BossMetadataOnly => bossMetadataOnly;

        public EnemyBehaviorTreeNodeDefinition RootNode => rootNode;

        public IReadOnlyList<EnemyBehaviorTreeNodeDefinition> Nodes => nodes?
            .Where(node => node != null)
            .Distinct()
            .ToArray() ?? Array.Empty<EnemyBehaviorTreeNodeDefinition>();

        public void Configure(
            string nextTemplateId,
            string nextDisplayName,
            string nextDescription,
            EnemyBehaviorTreeTemplateRole nextRole,
            EnemyBehaviorId nextRecommendedBehaviorId,
            EnemyInstinctDisposition nextRecommendedDisposition,
            EnemyIntelligenceLevel nextMinimumIntelligence,
            bool nextBossMetadataOnly,
            EnemyBehaviorTreeNodeDefinition nextRootNode,
            IEnumerable<EnemyBehaviorTreeNodeDefinition> nextNodes)
        {
            templateId = string.IsNullOrWhiteSpace(nextTemplateId) ? "behavior_tree_template" : nextTemplateId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? templateId : nextDisplayName;
            description = nextDescription ?? string.Empty;
            role = nextRole;
            recommendedBehaviorId = nextRecommendedBehaviorId;
            recommendedDisposition = nextRecommendedDisposition;
            minimumIntelligence = EnemyIntelligenceLevelExtensions.Clamp((int)nextMinimumIntelligence);
            bossMetadataOnly = nextBossMetadataOnly;
            rootNode = nextRootNode;
            nodes = nextNodes?.Where(node => node != null).Distinct().ToList() ?? new List<EnemyBehaviorTreeNodeDefinition>();
        }
    }
}
