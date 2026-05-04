using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.BehaviorTreeStudio;
using Hollow.Editor.EnemyAuthoring;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone95BehaviourTreeStudioTests
    {
        [Test]
        public void TemplateLibraryProvidesCoreRoles()
        {
            var templates = BehaviourTreeStudioTemplateGenerator.CreateRuntimeTemplates();
            try
            {
                Assert.That(templates.Count, Is.GreaterThanOrEqualTo(7));
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyBehaviorTreeTemplateRole.ChaserPressure);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyBehaviorTreeTemplateRole.PreyFleeCommit);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyBehaviorTreeTemplateRole.StationaryRangedSentinel);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyBehaviorTreeTemplateRole.WeaponUser);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyBehaviorTreeTemplateRole.CreatureSkirmisher);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyBehaviorTreeTemplateRole.CasterRanged);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyBehaviorTreeTemplateRole.BossMetadata);

                foreach (var template in templates)
                {
                    Assert.NotNull(template.RootNode, template.TemplateId);
                    Assert.That(template.Nodes.Count, Is.GreaterThan(0), template.TemplateId);
                    Assert.IsTrue(BehaviourTreeStudioAnalysis.Validate(template).IsValid, template.TemplateId);
                }
            }
            finally
            {
                foreach (var template in templates)
                {
                    DestroyTreeLike(template);
                }
            }
        }

        [Test]
        public void DraftTemplateApplicationDoesNotMutateSourceTree()
        {
            var source = CreateSimpleTree("source_tree", EnemyBehaviorCommandKind.Hold);
            var templates = BehaviourTreeStudioTemplateGenerator.CreateRuntimeTemplates();
            var template = templates.First(candidate => candidate.Role == EnemyBehaviorTreeTemplateRole.ChaserPressure);
            var draft = new EnemyAuthoringDraft();
            try
            {
                draft.Load(source);
                var sourceRootBefore = source.RootNode;
                var sourceCountBefore = source.Nodes.Count;

                BehaviourTreeStudioGraphUtility.ReplaceTreeDraftWithTemplate((EnemyBehaviorTreeDefinition)draft.Draft, template, draft);

                Assert.AreSame(sourceRootBefore, source.RootNode);
                Assert.AreEqual(sourceCountBefore, source.Nodes.Count);
                Assert.AreNotEqual(source.RootNode.NodeId, ((EnemyBehaviorTreeDefinition)draft.Draft).RootNode.NodeId);
                Assert.That(((EnemyBehaviorTreeDefinition)draft.Draft).Nodes.Count, Is.EqualTo(template.Nodes.Count));
            }
            finally
            {
                draft.Dispose();
                DestroyTreeLike(source);
                foreach (var generated in templates)
                {
                    DestroyTreeLike(generated);
                }
            }
        }

        [Test]
        public void GraphUtilityCreatesConnectsDuplicatesAndDisconnectsNodes()
        {
            var tree = ScriptableObject.CreateInstance<EnemyBehaviorTreeDefinition>();
            var draft = new EnemyAuthoringDraft();
            try
            {
                draft.Load(tree);
                var selector = BehaviourTreeStudioGraphUtility.AddNode<EnemyBehaviorSelectorNodeDefinition>(draft.Draft, draft, "selector");
                var action = BehaviourTreeStudioGraphUtility.AddNode<EnemyBehaviorActionNodeDefinition>(draft.Draft, draft, "action");
                action.Configure(action.NodeId, EnemyBehaviorCommandKind.Hold, nextReason: "test_hold");

                Assert.IsTrue(BehaviourTreeStudioGraphUtility.ConnectChild(selector, action));
                BehaviourTreeStudioGraphUtility.SetRoot(draft.Draft, selector);
                var duplicate = BehaviourTreeStudioGraphUtility.DuplicateNode(draft.Draft, draft, action);

                Assert.That(((EnemyBehaviorTreeDefinition)draft.Draft).RootNode, Is.EqualTo(selector));
                Assert.That(selector.Children, Does.Contain(action));
                Assert.NotNull(duplicate);
                Assert.AreNotEqual(action.NodeId, duplicate.NodeId);

                Assert.IsTrue(BehaviourTreeStudioGraphUtility.DisconnectChild(selector, action));
                Assert.IsFalse(selector.Children.Contains(action));
            }
            finally
            {
                draft.Dispose();
                DestroyTreeLike(tree);
            }
        }

        [Test]
        public void ValidationFindsCyclesAndUnreachableNodes()
        {
            var tree = ScriptableObject.CreateInstance<EnemyBehaviorTreeDefinition>();
            var first = ScriptableObject.CreateInstance<EnemyBehaviorSelectorNodeDefinition>();
            var second = ScriptableObject.CreateInstance<EnemyBehaviorSelectorNodeDefinition>();
            var orphan = ScriptableObject.CreateInstance<EnemyBehaviorActionNodeDefinition>();
            try
            {
                first.Configure("first", new EnemyBehaviorTreeNodeDefinition[] { second });
                second.Configure("second", new EnemyBehaviorTreeNodeDefinition[] { first });
                orphan.Configure("orphan", EnemyBehaviorCommandKind.Hold);
                tree.Configure("cycle_tree", "Cycle Tree", "test", false, first, new EnemyBehaviorTreeNodeDefinition[] { first, second, orphan });

                var result = BehaviourTreeStudioAnalysis.Validate(tree);

                Assert.IsFalse(result.IsValid);
                Assert.That(result.Errors.Any(error => error.Contains("cycle")), Is.True);
                Assert.That(result.Warnings.Any(warning => warning.Contains("unreachable")), Is.True);
            }
            finally
            {
                DestroyTreeLike(tree);
            }
        }

        [Test]
        public void SyntheticSandboxEvaluatesNodePathAndCommand()
        {
            var tree = CreateSimpleSequenceTree();
            try
            {
                var result = BehaviourTreeStudioAnalysis.EvaluateSynthetic(
                    tree,
                    new BehaviourTreeStudioSyntheticContext
                    {
                        IsIdle = true,
                        Awareness = EnemyAwarenessState.Engaged,
                        DistanceToPlayer = 1.2f
                    });

                Assert.IsTrue(result.Success);
                Assert.AreEqual(EnemyBehaviorCommandKind.Hold, result.Command.Kind);
                Assert.That(result.Path.Select(node => node.NodeId), Is.EquivalentTo(new[] { "root_sequence", "if_idle", "hold_action" }));
            }
            finally
            {
                DestroyTreeLike(tree);
            }
        }

        [Test]
        public void DocsAndReportExist()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_M95_Behaviour_Tree_Studio.md"));
            Assert.IsTrue(File.Exists("output/reports/behavior_tree_studio/m95_behaviour_tree_studio.md"));
            var docs = File.ReadAllText("Docs/Hollow_M95_Behaviour_Tree_Studio.md");
            StringAssert.Contains("Behaviour Tree Studio", docs);
            StringAssert.Contains("Play Mode live trace", docs);
            StringAssert.Contains("Room Sandbox", docs);
        }

        private static EnemyBehaviorTreeDefinition CreateSimpleTree(string treeId, EnemyBehaviorCommandKind command)
        {
            var tree = ScriptableObject.CreateInstance<EnemyBehaviorTreeDefinition>();
            var action = ScriptableObject.CreateInstance<EnemyBehaviorActionNodeDefinition>();
            action.Configure("root_action", command, nextReason: "simple");
            tree.Configure(treeId, treeId, "test", false, action, new[] { action });
            return tree;
        }

        private static EnemyBehaviorTreeDefinition CreateSimpleSequenceTree()
        {
            var tree = ScriptableObject.CreateInstance<EnemyBehaviorTreeDefinition>();
            var condition = ScriptableObject.CreateInstance<EnemyBehaviorConditionNodeDefinition>();
            var action = ScriptableObject.CreateInstance<EnemyBehaviorActionNodeDefinition>();
            var root = ScriptableObject.CreateInstance<EnemyBehaviorSequenceNodeDefinition>();
            condition.Configure("if_idle", EnemyBehaviorConditionKind.IsIdle);
            action.Configure("hold_action", EnemyBehaviorCommandKind.Hold, nextReason: "hold");
            root.Configure("root_sequence", new EnemyBehaviorTreeNodeDefinition[] { condition, action });
            tree.Configure("simple_sequence", "Simple Sequence", "test", false, root, new EnemyBehaviorTreeNodeDefinition[] { root, condition, action });
            return tree;
        }

        private static void DestroyTreeLike(EnemyBehaviorTreeDefinition tree)
        {
            if (tree == null)
            {
                return;
            }

            foreach (var node in tree.Nodes)
            {
                Object.DestroyImmediate(node);
            }

            Object.DestroyImmediate(tree);
        }

        private static void DestroyTreeLike(EnemyBehaviorTreeTemplateDefinition template)
        {
            if (template == null)
            {
                return;
            }

            foreach (var node in template.Nodes)
            {
                Object.DestroyImmediate(node);
            }

            Object.DestroyImmediate(template);
        }
    }
}
