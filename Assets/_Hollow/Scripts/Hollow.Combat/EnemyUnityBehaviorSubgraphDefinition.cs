using Unity.Behavior;
using UnityEngine;

namespace Hollow.Combat
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Unity Behavior Subgraph Contract", fileName = "EnemyUnityBehaviorSubgraph")]
    public sealed class EnemyUnityBehaviorSubgraphDefinition : ScriptableObject
    {
        [SerializeField] private string subgraphId = "notice_player";
        [SerializeField] private string displayName = "Notice Player";
        [SerializeField] private EnemyUnityBehaviorSubgraphKind kind = EnemyUnityBehaviorSubgraphKind.NoticePlayer;
        [SerializeField] private BehaviorGraph behaviorGraph;
        [SerializeField] private EnemyBehaviorCommandKind outputCommandKind = EnemyBehaviorCommandKind.FacePlayer;
        [SerializeField] private string outputActionId = string.Empty;
        [SerializeField] private float outputSpeedMultiplier = 1f;
        [SerializeField] private string outputReason = "unity_behavior_subgraph_notice_player";
        [SerializeField] private string[] requiredBlackboardInputs = EnemyUnityBehaviorBlackboardSchema.CopyRequiredInputs();
        [SerializeField] private string[] requiredBlackboardOutputs = EnemyUnityBehaviorBlackboardSchema.CopyRequiredOutputs();
        [SerializeField] private string[] requiredNodeNames = new string[0];
        [SerializeField] private string authoringNotes = string.Empty;

        public string SubgraphId => string.IsNullOrWhiteSpace(subgraphId) ? kind.ToString() : subgraphId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? SubgraphId : displayName;

        public EnemyUnityBehaviorSubgraphKind Kind => kind;

        public BehaviorGraph BehaviorGraph => behaviorGraph;

        public EnemyBehaviorCommandKind OutputCommandKind => outputCommandKind;

        public string OutputActionId => outputActionId ?? string.Empty;

        public float OutputSpeedMultiplier => Mathf.Max(0f, outputSpeedMultiplier);

        public string OutputReason => outputReason ?? string.Empty;

        public System.Collections.Generic.IReadOnlyList<string> RequiredBlackboardInputs => requiredBlackboardInputs ?? EnemyUnityBehaviorBlackboardSchema.RequiredInputNames;

        public System.Collections.Generic.IReadOnlyList<string> RequiredBlackboardOutputs => requiredBlackboardOutputs ?? EnemyUnityBehaviorBlackboardSchema.RequiredOutputNames;

        public System.Collections.Generic.IReadOnlyList<string> RequiredNodeNames => requiredNodeNames ?? System.Array.Empty<string>();

        public string AuthoringNotes => authoringNotes ?? string.Empty;

        public void Configure(
            string nextSubgraphId,
            string nextDisplayName,
            EnemyUnityBehaviorSubgraphKind nextKind,
            BehaviorGraph nextBehaviorGraph,
            EnemyBehaviorCommandKind nextOutputCommandKind,
            string nextOutputActionId,
            float nextOutputSpeedMultiplier,
            string nextOutputReason,
            string[] nextRequiredNodeNames,
            string nextAuthoringNotes)
        {
            subgraphId = string.IsNullOrWhiteSpace(nextSubgraphId) ? nextKind.ToString() : nextSubgraphId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? subgraphId : nextDisplayName;
            kind = nextKind;
            behaviorGraph = nextBehaviorGraph;
            outputCommandKind = nextOutputCommandKind;
            outputActionId = nextOutputActionId ?? string.Empty;
            outputSpeedMultiplier = Mathf.Max(0f, nextOutputSpeedMultiplier);
            outputReason = string.IsNullOrWhiteSpace(nextOutputReason) ? $"unity_behavior_subgraph_{subgraphId}" : nextOutputReason;
            requiredBlackboardInputs = EnemyUnityBehaviorBlackboardSchema.CopyRequiredInputs();
            requiredBlackboardOutputs = EnemyUnityBehaviorBlackboardSchema.CopyRequiredOutputs();
            requiredNodeNames = nextRequiredNodeNames ?? System.Array.Empty<string>();
            authoringNotes = nextAuthoringNotes ?? string.Empty;
        }
    }
}
