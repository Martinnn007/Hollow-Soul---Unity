using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone82Validator
    {
        private static readonly string[] RequiredText =
        {
            "Lightweight Behavior Tree Layer",
            "windup",
            "active",
            "recovery",
            "side_pounce",
            "stomp",
            "warning_squeal",
            "side_hop_bite",
            "metadata-only"
        };

        [MenuItem("Hollow/Validation/Run Milestone 82 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidatePromotedProfiles(failures);
            ValidateRuntimeTreeDefaults(failures);
            ValidateDefinitionFallbacks(failures);
            ValidateBossMetadata(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 82 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateFiles(List<string> failures)
        {
            ExpectFile(Milestone82AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone82AssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone82AssetGenerator.DocsPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone82AssetGenerator.DocsPath);
            foreach (var required in RequiredText)
            {
                if (markdown.IndexOf(required, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add($"M82 documentation is missing `{required}`.");
                }
            }
        }

        private static void ValidatePromotedProfiles(List<string> failures)
        {
            foreach (var actionId in Milestone82AssetGenerator.PromotedEnemyActionIds)
            {
                var attack = EnemyAttackProfileDefaults.AllEnemySpecs.FirstOrDefault(spec => spec.AttackId == actionId);
                if (string.IsNullOrWhiteSpace(attack.AttackId))
                {
                    failures.Add($"M82 promoted attack profile `{actionId}` is missing.");
                    continue;
                }

                var action = EnemyActionProfileDefaults.AllEnemySpecs.FirstOrDefault(spec =>
                    spec.ActionId == actionId &&
                    spec.LinkedAttackId == actionId &&
                    spec.UsageState == EnemyActionUsageState.CurrentRuntime);
                if (string.IsNullOrWhiteSpace(action.ActionId))
                {
                    failures.Add($"M82 promoted action `{actionId}` is not linked as a current M81 runtime action.");
                }
            }
        }

        private static void ValidateRuntimeTreeDefaults(List<string> failures)
        {
            foreach (var tree in Milestone82AssetGenerator.RuntimeEnemyTreeDefaults())
            {
                ValidateTree(tree, expectBossMetadataOnly: false, failures);
            }
        }

        private static void ValidateDefinitionFallbacks(List<string> failures)
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss"))
            {
                var tree = enemy.BehaviorTree;
                if (tree == null)
                {
                    failures.Add($"{enemy.SpawnKind} resolves no M82 behavior tree.");
                    continue;
                }

                if (tree.BossMetadataOnly)
                {
                    failures.Add($"{enemy.SpawnKind} resolved a boss metadata-only tree.");
                }

                ValidateTree(tree, expectBossMetadataOnly: false, failures);
            }
        }

        private static void ValidateBossMetadata(List<string> failures)
        {
            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                var tree = boss.BehaviorTreeMetadata;
                if (tree == null)
                {
                    failures.Add($"{boss.BossId} resolves no M82 boss metadata tree.");
                    continue;
                }

                ValidateTree(tree, expectBossMetadataOnly: true, failures);
            }
        }

        private static void ValidateTree(EnemyBehaviorTreeDefinition tree, bool expectBossMetadataOnly, List<string> failures)
        {
            if (tree == null)
            {
                failures.Add("Encountered a null M82 tree.");
                return;
            }

            if (tree.BossMetadataOnly != expectBossMetadataOnly)
            {
                failures.Add($"{tree.TreeId} has unexpected boss metadata flag `{tree.BossMetadataOnly}`.");
            }

            if (tree.RootNode == null)
            {
                failures.Add($"{tree.TreeId} has no root node.");
                return;
            }

            var nodes = tree.Nodes.ToArray();
            if (nodes.Length == 0)
            {
                failures.Add($"{tree.TreeId} has no node sub-assets/default nodes.");
                return;
            }

            var ids = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (node == null)
                {
                    failures.Add($"{tree.TreeId} has a null node.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.NodeId))
                {
                    failures.Add($"{tree.TreeId} has a node with no id.");
                }

                if (!ids.Add(node.NodeId))
                {
                    failures.Add($"{tree.TreeId} has duplicate node id `{node.NodeId}`.");
                }

                ValidateNode(tree, node, nodes, failures);
            }
        }

        private static void ValidateNode(EnemyBehaviorTreeDefinition tree, EnemyBehaviorTreeNodeDefinition node, IReadOnlyCollection<EnemyBehaviorTreeNodeDefinition> nodes, List<string> failures)
        {
            foreach (var child in node.Children)
            {
                if (child == null)
                {
                    failures.Add($"{tree.TreeId}:{node.NodeId} has a null child.");
                    continue;
                }

                if (!nodes.Contains(child))
                {
                    failures.Add($"{tree.TreeId}:{node.NodeId} references child `{child.NodeId}` outside the tree node list.");
                }
            }

            if (node is EnemyBehaviorWeightedSelectorNodeDefinition weighted)
            {
                foreach (var child in weighted.WeightedChildren)
                {
                    if (child == null || child.Child == null || child.Weight <= 0f)
                    {
                        failures.Add($"{tree.TreeId}:{node.NodeId} has an invalid weighted child.");
                    }
                }
            }

            if (node is EnemyBehaviorActionNodeDefinition action)
            {
                ValidateActionReference(tree, action.CommandKind, action.ActionId, failures);
            }

            if (node is EnemyBehaviorConditionNodeDefinition condition &&
                !string.IsNullOrWhiteSpace(condition.ActionId))
            {
                ValidateAttackReference(tree, condition.ActionId, failures);
            }
        }

        private static void ValidateActionReference(EnemyBehaviorTreeDefinition tree, EnemyBehaviorCommandKind commandKind, string actionId, List<string> failures)
        {
            if (commandKind is EnemyBehaviorCommandKind.StartMeleeAction
                or EnemyBehaviorCommandKind.StartRangedAction
                or EnemyBehaviorCommandKind.StartChargeAction
                or EnemyBehaviorCommandKind.StartAreaAction
                or EnemyBehaviorCommandKind.StartFeintWarning
                or EnemyBehaviorCommandKind.StartGuardAction
                or EnemyBehaviorCommandKind.StartCreatureMoveAction
                or EnemyBehaviorCommandKind.StartCreatureSignalAction)
            {
                ValidateAttackReference(tree, actionId, failures);
            }
        }

        private static void ValidateAttackReference(EnemyBehaviorTreeDefinition tree, string actionId, List<string> failures)
        {
            if (tree.BossMetadataOnly || string.IsNullOrWhiteSpace(actionId))
            {
                return;
            }

            var attack = EnemyAttackProfileDefaults.AllEnemySpecs.FirstOrDefault(spec =>
                spec.OwnerId == tree.OwnerId &&
                spec.AttackId == actionId);
            if (string.IsNullOrWhiteSpace(attack.AttackId))
            {
                failures.Add($"{tree.TreeId} references unknown runtime attack `{actionId}`.");
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M82 file: {path}");
            }
        }
    }
}
