using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.EnemyAuthoring;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.BehaviorTreeStudio
{
    public sealed class BehaviourTreeStudioAnalysisResult
    {
        private readonly List<string> errors = new();
        private readonly List<string> warnings = new();
        private readonly List<string> readabilityNotes = new();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public IReadOnlyList<string> ReadabilityNotes => readabilityNotes;
        public bool IsValid => errors.Count == 0;

        public void Error(string message) => errors.Add(message);
        public void Warning(string message) => warnings.Add(message);
        public void Readability(string message) => readabilityNotes.Add(message);
    }

    public sealed class BehaviourTreeStudioSyntheticContext
    {
        public EnemyAwarenessState Awareness = EnemyAwarenessState.Engaged;
        public EnemyInstinctDisposition Disposition = EnemyInstinctDisposition.Predator;
        public EnemyIntelligenceLevel Intelligence = EnemyIntelligenceLevel.Basic;
        public EnemyBehaviorId BehaviorId = EnemyBehaviorId.Chaser;
        public float DistanceToPlayer = 1.5f;
        public bool IsIdle = true;
        public bool IsEndangered;
        public bool ShouldSentinelEngage = true;
        public bool CanStartMelee = true;
        public bool CanStartRanged = true;
        public bool CanStartCharge = true;
        public bool CanStartArea = true;
        public bool CanStartGuard = true;
        public bool CanStartCreatureMove = true;
        public bool CanStartCreatureSignal = true;
        public float TooCloseDistance = 0.9f;
        public float TooFarDistance = 3.5f;
        public float PreferredMin = 1f;
        public float PreferredMax = 2.5f;
        public float TimeSeconds = 1f;
        public int SpawnIndex;
    }

    public sealed class BehaviourTreeStudioSyntheticResult
    {
        public bool Success;
        public EnemyBehaviorCommand Command;
        public readonly List<EnemyBehaviorTreeNodeDefinition> Path = new();
        public string FailureReason = string.Empty;
    }

    public static class BehaviourTreeStudioAnalysis
    {
        public static BehaviourTreeStudioAnalysisResult Validate(
            EnemyBehaviorTreeDefinition tree,
            IReadOnlyCollection<EnemyActionProfileDefinition> ownerActions = null,
            bool bossMetadataOnly = false)
        {
            return ValidateTreeLike(
                tree != null ? tree.DisplayName : "Behavior Tree",
                tree != null ? tree.RootNode : null,
                tree != null ? tree.Nodes : Array.Empty<EnemyBehaviorTreeNodeDefinition>(),
                ownerActions,
                bossMetadataOnly || (tree != null && tree.BossMetadataOnly));
        }

        public static BehaviourTreeStudioAnalysisResult Validate(
            EnemyBehaviorTreeTemplateDefinition template,
            IReadOnlyCollection<EnemyActionProfileDefinition> ownerActions = null)
        {
            return ValidateTreeLike(
                template != null ? template.DisplayName : "Behavior Tree Template",
                template != null ? template.RootNode : null,
                template != null ? template.Nodes : Array.Empty<EnemyBehaviorTreeNodeDefinition>(),
                ownerActions,
                template != null && template.BossMetadataOnly);
        }

        public static BehaviourTreeStudioAnalysisResult ValidateTreeLike(
            string label,
            EnemyBehaviorTreeNodeDefinition root,
            IEnumerable<EnemyBehaviorTreeNodeDefinition> inputNodes,
            IReadOnlyCollection<EnemyActionProfileDefinition> ownerActions,
            bool bossMetadataOnly)
        {
            var result = new BehaviourTreeStudioAnalysisResult();
            var nodes = inputNodes?.Where(node => node != null).Distinct().ToArray() ?? Array.Empty<EnemyBehaviorTreeNodeDefinition>();
            if (root == null)
            {
                result.Error($"{label}: root node is missing.");
            }

            if (nodes.Length == 0)
            {
                result.Error($"{label}: no nodes are assigned.");
                return result;
            }

            foreach (var duplicate in nodes.GroupBy(node => node.NodeId).Where(group => group.Count() > 1))
            {
                result.Error($"{label}: duplicate node id `{duplicate.Key}`.");
            }

            foreach (var node in nodes)
            {
                if ((node.Kind is EnemyBehaviorTreeNodeKind.Selector or EnemyBehaviorTreeNodeKind.Sequence or EnemyBehaviorTreeNodeKind.WeightedSelector) &&
                    node.Children.Count == 0)
                {
                    result.Error($"{label}/{node.NodeId}: {node.Kind} has no child links.");
                }

                if (node is EnemyBehaviorActionNodeDefinition actionNode)
                {
                    ValidateActionNode(label, actionNode, ownerActions, bossMetadataOnly, result);
                }

                if (node is EnemyBehaviorWeightedSelectorNodeDefinition weighted)
                {
                    ValidateWeightedNode(label, weighted, result);
                }
            }

            if (root != null)
            {
                var visited = new HashSet<EnemyBehaviorTreeNodeDefinition>();
                var recursion = new HashSet<EnemyBehaviorTreeNodeDefinition>();
                DetectCycles(label, root, visited, recursion, result);
                foreach (var unreachable in nodes.Where(node => !visited.Contains(node)))
                {
                    result.Warning($"{label}/{unreachable.NodeId}: unreachable from root.");
                }
            }

            AnalyzeReadability(label, nodes, result);
            return result;
        }

        public static BehaviourTreeStudioSyntheticResult EvaluateSynthetic(
            EnemyBehaviorTreeDefinition tree,
            BehaviourTreeStudioSyntheticContext context)
        {
            return EvaluateSynthetic(tree != null ? tree.RootNode : null, context);
        }

        public static BehaviourTreeStudioSyntheticResult EvaluateSynthetic(
            EnemyBehaviorTreeTemplateDefinition template,
            BehaviourTreeStudioSyntheticContext context)
        {
            return EvaluateSynthetic(template != null ? template.RootNode : null, context);
        }

        public static BehaviourTreeStudioSyntheticResult EvaluateSynthetic(
            EnemyBehaviorTreeNodeDefinition root,
            BehaviourTreeStudioSyntheticContext context)
        {
            var result = new BehaviourTreeStudioSyntheticResult();
            if (root == null)
            {
                result.Command = EnemyBehaviorCommand.None("synthetic_missing_root");
                result.FailureReason = "missing_root";
                return result;
            }

            result.Success = EvaluateNode(root, context ?? new BehaviourTreeStudioSyntheticContext(), result.Path, out var command, out var reason);
            result.Command = command;
            result.FailureReason = reason;
            return result;
        }

        public static string SummaryFor(EnemyBehaviorTreeNodeDefinition node)
        {
            if (node == null)
            {
                return "(missing node)";
            }

            return node switch
            {
                EnemyBehaviorActionNodeDefinition action => SummaryForAction(action),
                EnemyBehaviorConditionNodeDefinition condition => SummaryForCondition(condition),
                EnemyBehaviorWeightedSelectorNodeDefinition weighted => $"Weighted selector: {weighted.WeightedChildren.Count} choices",
                EnemyBehaviorSelectorNodeDefinition => $"Selector: first valid of {node.Children.Count}",
                EnemyBehaviorSequenceNodeDefinition => $"Sequence: all {node.Children.Count} must pass",
                _ => $"{node.Kind}: {node.NodeId}"
            };
        }

        public static string BadgeFor(
            EnemyBehaviorActionNodeDefinition action,
            IReadOnlyCollection<EnemyActionProfileDefinition> ownerActions,
            IReadOnlyCollection<EnemyAttackProfileDefinition> ownerAttacks)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.ActionId))
            {
                return string.Empty;
            }

            var actionProfile = ownerActions?.FirstOrDefault(profile => profile != null && profile.ActionId == action.ActionId);
            var attack = actionProfile != null
                ? ownerAttacks?.FirstOrDefault(profile => profile != null && profile.AttackId == actionProfile.LinkedAttackId)
                : ownerAttacks?.FirstOrDefault(profile => profile != null && profile.AttackId == action.ActionId);

            if (actionProfile == null && attack == null)
            {
                return "missing action/attack";
            }

            var parts = new List<string>();
            if (actionProfile != null)
            {
                parts.Add($"{actionProfile.Category}/{actionProfile.Intent}");
                parts.Add($"{actionProfile.MinRangeMeters:0.0}-{actionProfile.MaxRangeMeters:0.0}m");
                parts.Add($"pressure {actionProfile.PressureCost}");
            }

            if (attack != null)
            {
                parts.Add($"dmg {attack.Damage}");
                parts.Add($"{attack.RuntimeKind}");
                parts.Add($"cd {attack.CooldownSeconds:0.00}s");
            }

            return string.Join(" | ", parts);
        }

        public static string DiffSummary(EnemyBehaviorTreeDefinition left, EnemyBehaviorTreeDefinition right)
        {
            if (left == null || right == null)
            {
                return "Select two trees to diff.";
            }

            var leftIds = left.Nodes.Select(node => node.NodeId).ToHashSet(StringComparer.Ordinal);
            var rightIds = right.Nodes.Select(node => node.NodeId).ToHashSet(StringComparer.Ordinal);
            var added = rightIds.Except(leftIds).OrderBy(id => id).ToArray();
            var removed = leftIds.Except(rightIds).OrderBy(id => id).ToArray();
            var shared = leftIds.Intersect(rightIds).Count();
            return $"Shared nodes: {shared}; Added: {string.Join(", ", added.DefaultIfEmpty("none"))}; Removed: {string.Join(", ", removed.DefaultIfEmpty("none"))}.";
        }

        private static void ValidateActionNode(
            string label,
            EnemyBehaviorActionNodeDefinition actionNode,
            IReadOnlyCollection<EnemyActionProfileDefinition> ownerActions,
            bool bossMetadataOnly,
            BehaviourTreeStudioAnalysisResult result)
        {
            if (!actionNode.CommandKind.StartsCommittedAction())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(actionNode.ActionId))
            {
                result.Error($"{label}/{actionNode.NodeId}: committed action has no action id.");
                return;
            }

            if (bossMetadataOnly)
            {
                result.Warning($"{label}/{actionNode.NodeId}: boss tree action is metadata-only and will not drive boss runtime.");
            }

            if (ownerActions != null && ownerActions.Count > 0 && ownerActions.All(action => action == null || action.ActionId != actionNode.ActionId))
            {
                result.Warning($"{label}/{actionNode.NodeId}: `{actionNode.ActionId}` is not represented by the owning action profiles.");
            }
        }

        private static void ValidateWeightedNode(string label, EnemyBehaviorWeightedSelectorNodeDefinition weighted, BehaviourTreeStudioAnalysisResult result)
        {
            if (weighted.WeightedChildren.Count == 0)
            {
                result.Error($"{label}/{weighted.NodeId}: weighted selector has no weighted children.");
                return;
            }

            var total = weighted.WeightedChildren.Where(child => child.Child != null).Sum(child => child.Weight);
            if (total <= 0f)
            {
                result.Error($"{label}/{weighted.NodeId}: weighted selector has zero total weight.");
            }
        }

        private static void DetectCycles(
            string label,
            EnemyBehaviorTreeNodeDefinition node,
            HashSet<EnemyBehaviorTreeNodeDefinition> visited,
            HashSet<EnemyBehaviorTreeNodeDefinition> recursion,
            BehaviourTreeStudioAnalysisResult result)
        {
            if (node == null)
            {
                return;
            }

            if (recursion.Contains(node))
            {
                result.Error($"{label}/{node.NodeId}: cycle detected.");
                return;
            }

            if (!visited.Add(node))
            {
                return;
            }

            recursion.Add(node);
            foreach (var child in node.Children)
            {
                DetectCycles(label, child, visited, recursion, result);
            }

            recursion.Remove(node);
        }

        private static void AnalyzeReadability(string label, IReadOnlyList<EnemyBehaviorTreeNodeDefinition> nodes, BehaviourTreeStudioAnalysisResult result)
        {
            var actionNodes = nodes.OfType<EnemyBehaviorActionNodeDefinition>().ToArray();
            var committed = actionNodes.Where(node => node.CommandKind.StartsCommittedAction()).ToArray();
            if (committed.Length > 0 && actionNodes.All(node => node.CommandKind != EnemyBehaviorCommandKind.Hold && node.CommandKind != EnemyBehaviorCommandKind.FacePlayer))
            {
                result.Readability($"{label}: consider a hold/face fallback so failed commits do not look indecisive.");
            }

            var repeated = committed
                .Where(node => !string.IsNullOrWhiteSpace(node.ActionId))
                .GroupBy(node => node.ActionId)
                .Where(group => group.Count() > 2)
                .ToArray();
            foreach (var group in repeated)
            {
                result.Readability($"{label}: action `{group.Key}` appears {group.Count()} times; consider one shared branch or varied fallback.");
            }

            if (nodes.OfType<EnemyBehaviorConditionNodeDefinition>().All(node => node.Condition != EnemyBehaviorConditionKind.IsEndangered))
            {
                result.Readability($"{label}: no endangered branch; prey and fragile enemies may need clearer flee/commit behavior.");
            }
        }

        private static bool EvaluateNode(
            EnemyBehaviorTreeNodeDefinition node,
            BehaviourTreeStudioSyntheticContext context,
            List<EnemyBehaviorTreeNodeDefinition> path,
            out EnemyBehaviorCommand command,
            out string reason)
        {
            command = EnemyBehaviorCommand.None(node != null ? node.NodeId : "missing_node");
            reason = string.Empty;
            if (node == null)
            {
                reason = "missing_node";
                return false;
            }

            path.Add(node);
            switch (node)
            {
                case EnemyBehaviorActionNodeDefinition action:
                    command = new EnemyBehaviorCommand(action.CommandKind, action.ActionId, SpeedMultiplierFor(action), string.IsNullOrWhiteSpace(ReasonFor(action)) ? action.NodeId : ReasonFor(action));
                    return true;
                case EnemyBehaviorConditionNodeDefinition condition:
                    var passed = EvaluateCondition(condition, context);
                    reason = passed ? condition.NodeId : $"{condition.NodeId}_failed";
                    command = EnemyBehaviorCommand.None(reason);
                    return passed;
                case EnemyBehaviorSequenceNodeDefinition:
                    foreach (var child in node.Children)
                    {
                        if (!EvaluateNode(child, context, path, out command, out reason))
                        {
                            command = EnemyBehaviorCommand.None($"{node.NodeId}_failed");
                            return false;
                        }
                    }

                    return true;
                case EnemyBehaviorSelectorNodeDefinition:
                    foreach (var child in node.Children)
                    {
                        var beforeCount = path.Count;
                        if (EvaluateNode(child, context, path, out command, out reason))
                        {
                            return true;
                        }

                        if (path.Count > beforeCount)
                        {
                            path.RemoveRange(beforeCount, path.Count - beforeCount);
                        }
                    }

                    command = EnemyBehaviorCommand.None($"{node.NodeId}_failed");
                    reason = $"{node.NodeId}_failed";
                    return false;
                case EnemyBehaviorWeightedSelectorNodeDefinition weighted:
                    foreach (var child in weighted.WeightedChildren.Where(child => child.Child != null).OrderByDescending(child => child.Weight))
                    {
                        var beforeCount = path.Count;
                        if (EvaluateNode(child.Child, context, path, out command, out reason))
                        {
                            return true;
                        }

                        if (path.Count > beforeCount)
                        {
                            path.RemoveRange(beforeCount, path.Count - beforeCount);
                        }
                    }

                    command = EnemyBehaviorCommand.None($"{node.NodeId}_failed");
                    reason = $"{node.NodeId}_failed";
                    return false;
                default:
                    reason = "unknown_node";
                    return false;
            }
        }

        private static bool EvaluateCondition(EnemyBehaviorConditionNodeDefinition condition, BehaviourTreeStudioSyntheticContext context)
        {
            var threshold = SerializedFloat(condition, "threshold");
            var awareness = (EnemyAwarenessState)SerializedEnum(condition, "awareness");
            var disposition = (EnemyInstinctDisposition)SerializedEnum(condition, "disposition");
            var intelligence = (EnemyIntelligenceLevel)SerializedEnum(condition, "intelligence");
            var behaviorId = (EnemyBehaviorId)SerializedEnum(condition, "behaviorId");
            var invert = SerializedBool(condition, "invert");
            var passed = condition.Condition switch
            {
                EnemyBehaviorConditionKind.Always => true,
                EnemyBehaviorConditionKind.IsIdle => context.IsIdle,
                EnemyBehaviorConditionKind.AwarenessAtLeast => (int)context.Awareness >= (int)awareness,
                EnemyBehaviorConditionKind.IsEndangered => context.IsEndangered,
                EnemyBehaviorConditionKind.DistanceLessOrEqual => context.DistanceToPlayer <= threshold,
                EnemyBehaviorConditionKind.DistanceGreaterThan => context.DistanceToPlayer > threshold,
                EnemyBehaviorConditionKind.IsTooClose => context.DistanceToPlayer <= context.TooCloseDistance,
                EnemyBehaviorConditionKind.IsTooFar => context.DistanceToPlayer >= context.TooFarDistance,
                EnemyBehaviorConditionKind.IsInPreferredRange => context.DistanceToPlayer >= context.PreferredMin && context.DistanceToPlayer <= context.PreferredMax,
                EnemyBehaviorConditionKind.CanStartMeleeAction => context.CanStartMelee,
                EnemyBehaviorConditionKind.CanStartRangedAttack => context.CanStartRanged,
                EnemyBehaviorConditionKind.CanStartChargeAttack => context.CanStartCharge,
                EnemyBehaviorConditionKind.CanStartAreaAction => context.CanStartArea,
                EnemyBehaviorConditionKind.CanStartGuardAction => context.CanStartGuard,
                EnemyBehaviorConditionKind.CanStartCreatureMoveAction => context.CanStartCreatureMove,
                EnemyBehaviorConditionKind.CanStartCreatureSignalAction => context.CanStartCreatureSignal,
                EnemyBehaviorConditionKind.CanStartRangedAction => context.CanStartRanged,
                EnemyBehaviorConditionKind.ShouldSentinelEngage => context.ShouldSentinelEngage,
                EnemyBehaviorConditionKind.DeterministicChance => Deterministic01(condition.NodeId, context) <= Mathf.Clamp01(threshold),
                EnemyBehaviorConditionKind.BehaviorMatches => context.BehaviorId == behaviorId,
                EnemyBehaviorConditionKind.DispositionMatches => context.Disposition == disposition,
                EnemyBehaviorConditionKind.IntelligenceAtLeast => (int)context.Intelligence >= (int)intelligence,
                _ => true
            };

            return invert ? !passed : passed;
        }

        private static float Deterministic01(string salt, BehaviourTreeStudioSyntheticContext context)
        {
            var hash = 17;
            var value = salt ?? string.Empty;
            for (var index = 0; index < value.Length; index++)
            {
                hash = hash * 31 + value[index];
            }

            var bucket = Mathf.FloorToInt(context.TimeSeconds * 1.6f);
            hash = hash * 31 + (context.SpawnIndex + 7) * 97 + bucket * 53;
            return Mathf.Abs(hash % 1000) / 1000f;
        }

        private static string SummaryForAction(EnemyBehaviorActionNodeDefinition action)
        {
            if (action.CommandKind.StartsCommittedAction() && !string.IsNullOrWhiteSpace(action.ActionId))
            {
                return $"Start {ReadableCommand(action.CommandKind)}: {action.ActionId}";
            }

            return action.CommandKind switch
            {
                EnemyBehaviorCommandKind.Hold => "Hold position",
                EnemyBehaviorCommandKind.MoveToPlayer => "Move toward player",
                EnemyBehaviorCommandKind.MovePreferredRange => "Move to action spacing",
                EnemyBehaviorCommandKind.Flee => "Flee/reset",
                EnemyBehaviorCommandKind.Wander => "Wander",
                EnemyBehaviorCommandKind.FacePlayer => "Face player",
                EnemyBehaviorCommandKind.None => "No-op",
                _ => $"{action.CommandKind} {action.ActionId}"
            };
        }

        private static string SummaryForCondition(EnemyBehaviorConditionNodeDefinition condition)
        {
            var prefix = SerializedBool(condition, "invert") ? "Unless" : "If";
            var threshold = SerializedFloat(condition, "threshold");
            return condition.Condition switch
            {
                EnemyBehaviorConditionKind.AwarenessAtLeast => $"{prefix} awareness >= {(EnemyAwarenessState)SerializedEnum(condition, "awareness")}",
                EnemyBehaviorConditionKind.DistanceLessOrEqual => $"{prefix} distance <= {threshold:0.00}m",
                EnemyBehaviorConditionKind.DistanceGreaterThan => $"{prefix} distance > {threshold:0.00}m",
                EnemyBehaviorConditionKind.DeterministicChance => $"{prefix} deterministic chance <= {Mathf.Clamp01(threshold):P0}",
                EnemyBehaviorConditionKind.BehaviorMatches => $"{prefix} behavior is {(EnemyBehaviorId)SerializedEnum(condition, "behaviorId")}",
                EnemyBehaviorConditionKind.DispositionMatches => $"{prefix} disposition is {(EnemyInstinctDisposition)SerializedEnum(condition, "disposition")}",
                EnemyBehaviorConditionKind.IntelligenceAtLeast => $"{prefix} intelligence >= {(EnemyIntelligenceLevel)SerializedEnum(condition, "intelligence")}",
                EnemyBehaviorConditionKind.CanStartMeleeAction or EnemyBehaviorConditionKind.CanStartAreaAction or EnemyBehaviorConditionKind.CanStartGuardAction
                    or EnemyBehaviorConditionKind.CanStartCreatureMoveAction or EnemyBehaviorConditionKind.CanStartCreatureSignalAction or EnemyBehaviorConditionKind.CanStartRangedAction => $"{prefix} can start {condition.ActionId}",
                _ => $"{prefix} {condition.Condition}"
            };
        }

        private static string ReadableCommand(EnemyBehaviorCommandKind kind)
        {
            return kind switch
            {
                EnemyBehaviorCommandKind.StartMeleeAction => "Melee",
                EnemyBehaviorCommandKind.StartRangedAction => "Ranged",
                EnemyBehaviorCommandKind.StartChargeAction => "Charge",
                EnemyBehaviorCommandKind.StartAreaAction => "Area",
                EnemyBehaviorCommandKind.StartFeintWarning => "Warning",
                EnemyBehaviorCommandKind.StartGuardAction => "Guard",
                EnemyBehaviorCommandKind.StartCreatureMoveAction => "Move",
                EnemyBehaviorCommandKind.StartCreatureSignalAction => "Signal",
                _ => kind.ToString()
            };
        }

        private static float SpeedMultiplierFor(EnemyBehaviorActionNodeDefinition action)
        {
            var serialized = new SerializedObject(action);
            return serialized.FindProperty("speedMultiplier")?.floatValue ?? 1f;
        }

        private static string ReasonFor(EnemyBehaviorActionNodeDefinition action)
        {
            var serialized = new SerializedObject(action);
            return serialized.FindProperty("reason")?.stringValue ?? string.Empty;
        }

        private static bool SerializedBool(UnityEngine.Object target, string propertyName)
        {
            var serialized = new SerializedObject(target);
            return serialized.FindProperty(propertyName)?.boolValue ?? false;
        }

        private static int SerializedEnum(UnityEngine.Object target, string propertyName)
        {
            var serialized = new SerializedObject(target);
            return serialized.FindProperty(propertyName)?.enumValueIndex ?? 0;
        }

        private static float SerializedFloat(UnityEngine.Object target, string propertyName)
        {
            var serialized = new SerializedObject(target);
            return serialized.FindProperty(propertyName)?.floatValue ?? 0f;
        }
    }
}
