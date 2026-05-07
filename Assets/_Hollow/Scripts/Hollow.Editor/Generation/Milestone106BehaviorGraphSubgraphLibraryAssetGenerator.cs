using System.Collections.Generic;
using System.IO;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone106BehaviorGraphSubgraphLibraryAssetGenerator
    {
        public const string DataFolder = "Assets/_Hollow/Data/EnemyUnityBehavior/M106/Subgraphs";
        public const string DocsPath = "Docs/Hollow_M106_Behavior_Graph_Subgraph_Library.md";
        public const string ReportPath = "output/reports/m106_behavior_graph_subgraph_library.md";

        public readonly struct SubgraphSpec
        {
            public SubgraphSpec(
                string fileName,
                string subgraphId,
                string displayName,
                EnemyUnityBehaviorSubgraphKind kind,
                EnemyBehaviorCommandKind outputCommandKind,
                string outputActionId,
                float speedMultiplier,
                string outputReason,
                string[] requiredNodeNames,
                string notes)
            {
                FileName = fileName;
                SubgraphId = subgraphId;
                DisplayName = displayName;
                Kind = kind;
                OutputCommandKind = outputCommandKind;
                OutputActionId = outputActionId;
                SpeedMultiplier = speedMultiplier;
                OutputReason = outputReason;
                RequiredNodeNames = requiredNodeNames;
                Notes = notes;
            }

            public string FileName { get; }
            public string SubgraphId { get; }
            public string DisplayName { get; }
            public EnemyUnityBehaviorSubgraphKind Kind { get; }
            public EnemyBehaviorCommandKind OutputCommandKind { get; }
            public string OutputActionId { get; }
            public float SpeedMultiplier { get; }
            public string OutputReason { get; }
            public string[] RequiredNodeNames { get; }
            public string Notes { get; }
        }

        private static readonly SubgraphSpec[] Specs =
        {
            new("UBSG_NoticePlayer.asset", "notice_player", "Notice Player", EnemyUnityBehaviorSubgraphKind.NoticePlayer, EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_notice_player", new[] { "HollowEnemyAlertedCondition", "HollowEnemyNoticePlayerAction" }, "First readable acknowledgement beat. No damage, no direct movement."),
            new("UBSG_InvestigateNoise.asset", "investigate_noise", "Investigate Noise", EnemyUnityBehaviorSubgraphKind.InvestigateNoise, EnemyBehaviorCommandKind.Wander, string.Empty, 0.8f, "unity_behavior_investigate_noise", new[] { "HollowEnemyAlertedCondition", "HollowEnemyInvestigateNoiseAction" }, "Moves through Hollow investigation/local navigation toward the latest disturbance."),
            new("UBSG_Flee.asset", "flee", "Flee", EnemyUnityBehaviorSubgraphKind.Flee, EnemyBehaviorCommandKind.Flee, string.Empty, 1.1f, "unity_behavior_flee", new[] { "HollowEnemyShouldFleeCondition", "HollowEnemyFleeAction" }, "Prey, critters, and damaged enemies can request a capped Hollow flee/reset."),
            new("UBSG_Circle.asset", "circle", "Circle", EnemyUnityBehaviorSubgraphKind.Circle, EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 0.75f, "unity_behavior_circle", new[] { "HollowEnemyCircleAction" }, "Requests tactical circling/repositioning; NavMesh and spacing profiles own motion."),
            new("UBSG_ApproachActionRange.asset", "approach_action_range", "Approach Action Range", EnemyUnityBehaviorSubgraphKind.ApproachActionRange, EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 1f, "unity_behavior_approach", new[] { "HollowEnemyChaseApproachAction" }, "Moves toward M91/M102 reachable action envelopes instead of player center."),
            new("UBSG_RequestAttackSlot.asset", "request_attack_slot", "Request Attack Slot", EnemyUnityBehaviorSubgraphKind.RequestAttackSlot, EnemyBehaviorCommandKind.StartMeleeAction, string.Empty, 1f, "unity_behavior_request_attack_slot", new[] { "HollowEnemyRequestAttackSlotAction" }, "Asks Hollow scorer/director to choose or approve a concrete attack. Empty action id is intentional."),
            new("UBSG_StartAction.asset", "start_action", "Start Action", EnemyUnityBehaviorSubgraphKind.StartAction, EnemyBehaviorCommandKind.StartMeleeAction, string.Empty, 1f, "unity_behavior_start_action", new[] { "HollowEnemyCanStartActionCondition", "HollowEnemyInActionRangeCondition", "HollowEnemyStartLinkedAction" }, "Starts an explicit Hollow action id while preserving active windows and budgets."),
            new("UBSG_RecoverHold.asset", "recover_hold", "Recover / Hold", EnemyUnityBehaviorSubgraphKind.RecoverHold, EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "unity_behavior_recover_hold", new[] { "HollowEnemyRecoverHoldAction", "HollowEnemyHoldFaceAction" }, "Non-damaging recovery/hold branch used after failed or deferred commits.")
        };

        public static IReadOnlyList<SubgraphSpec> SubgraphSpecs => Specs;

        [MenuItem("Hollow/Generation/Generate Milestone 106 Behavior Graph Subgraph Library Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var generated = new List<EnemyUnityBehaviorSubgraphDefinition>();
            foreach (var spec in Specs)
            {
                generated.Add(CreateOrUpdateSubgraph(spec));
            }

            WriteDocs();
            WriteReport(generated);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M106 Unity Behavior subgraph library assets.");
        }

        private static EnemyUnityBehaviorSubgraphDefinition CreateOrUpdateSubgraph(SubgraphSpec spec)
        {
            var path = $"{DataFolder}/{spec.FileName}";
            var subgraph = AssetDatabase.LoadAssetAtPath<EnemyUnityBehaviorSubgraphDefinition>(path);
            if (subgraph == null)
            {
                subgraph = ScriptableObject.CreateInstance<EnemyUnityBehaviorSubgraphDefinition>();
                AssetDatabase.CreateAsset(subgraph, path);
            }

            subgraph.Configure(
                spec.SubgraphId,
                spec.DisplayName,
                spec.Kind,
                subgraph.BehaviorGraph,
                spec.OutputCommandKind,
                spec.OutputActionId,
                spec.SpeedMultiplier,
                spec.OutputReason,
                spec.RequiredNodeNames,
                spec.Notes);
            EditorUtility.SetDirty(subgraph);
            return subgraph;
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M106: Behavior Graph Subgraph Library");
            builder.AppendLine();
            builder.AppendLine("M106 adds reusable Unity Behavior subgraph contracts for common Hollow enemy intent. These subgraphs choose intent only; Hollow action profiles, attack profiles, active windows, NavMesh locomotion, tactical slots, pressure budgets, and damage math remain authoritative.");
            builder.AppendLine();
            builder.AppendLine("## Reusable Subgraphs");
            builder.AppendLine();
            builder.AppendLine("| Subgraph | Output | Purpose |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var spec in Specs)
            {
                builder.AppendLine($"| {spec.DisplayName} | `{spec.OutputCommandKind}` | {spec.Notes} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Authoring Contract");
            builder.AppendLine();
            builder.AppendLine("- Every contract has an official Unity `BehaviorGraph` slot for the visual subgraph asset.");
            builder.AppendLine("- Required Hollow nodes include notice player, investigate noise, flee, circle, approach action range, request attack slot, start action, and recover/hold wrappers.");
            builder.AppendLine("- `Request Attack Slot` may output an empty action id; `EnemyActionScorer` and `RoomTacticalDirector` choose the concrete Hollow action later.");
            builder.AppendLine("- The subgraph library is reusable by family graphs from M105 and by future enemy-specific Unity Behavior graphs.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport(IReadOnlyList<EnemyUnityBehaviorSubgraphDefinition> subgraphs)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M106 Behavior Graph Subgraph Library Report");
            builder.AppendLine();
            builder.AppendLine("- Library path: `Assets/_Hollow/Data/EnemyUnityBehavior/M106/Subgraphs`.");
            builder.AppendLine("- Runtime source of truth remains Hollow combat execution.");
            builder.AppendLine("- Subgraphs:");
            foreach (var subgraph in subgraphs)
            {
                builder.AppendLine($"  - `{subgraph.SubgraphId}` / `{subgraph.Kind}` / output `{subgraph.OutputCommandKind}`.");
            }

            builder.AppendLine($"- Docs: `{DocsPath}`.");
            File.WriteAllText(ReportPath, builder.ToString());
        }
    }
}
