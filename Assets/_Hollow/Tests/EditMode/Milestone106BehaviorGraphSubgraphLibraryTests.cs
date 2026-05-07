using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Combat.UnityBehaviorNodes;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEditor;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone106BehaviorGraphSubgraphLibraryTests
    {
        [Test]
        public void SubgraphLibraryDefinesTheEightReusableIntentContracts()
        {
            var specs = Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.SubgraphSpecs;
            Assert.AreEqual(8, specs.Count);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    EnemyUnityBehaviorSubgraphKind.NoticePlayer,
                    EnemyUnityBehaviorSubgraphKind.InvestigateNoise,
                    EnemyUnityBehaviorSubgraphKind.Flee,
                    EnemyUnityBehaviorSubgraphKind.Circle,
                    EnemyUnityBehaviorSubgraphKind.ApproachActionRange,
                    EnemyUnityBehaviorSubgraphKind.RequestAttackSlot,
                    EnemyUnityBehaviorSubgraphKind.StartAction,
                    EnemyUnityBehaviorSubgraphKind.RecoverHold
                },
                specs.Select(spec => spec.Kind).ToArray());
        }

        [Test]
        public void SubgraphAssetsResolveStableBlackboardAndCommandMetadata()
        {
            foreach (var spec in Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.SubgraphSpecs)
            {
                var path = $"{Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.DataFolder}/{spec.FileName}";
                var subgraph = AssetDatabase.LoadAssetAtPath<EnemyUnityBehaviorSubgraphDefinition>(path);
                Assert.NotNull(subgraph, path);
                Assert.AreEqual(spec.SubgraphId, subgraph.SubgraphId, path);
                Assert.AreEqual(spec.Kind, subgraph.Kind, path);
                Assert.AreEqual(spec.OutputCommandKind, subgraph.OutputCommandKind, path);
                CollectionAssert.IsSubsetOf(EnemyUnityBehaviorBlackboardSchema.RequiredInputNames.ToArray(), subgraph.RequiredBlackboardInputs.ToArray(), path);
                CollectionAssert.IsSubsetOf(EnemyUnityBehaviorBlackboardSchema.RequiredOutputNames.ToArray(), subgraph.RequiredBlackboardOutputs.ToArray(), path);
                foreach (var requiredNode in spec.RequiredNodeNames)
                {
                    CollectionAssert.Contains(subgraph.RequiredNodeNames.ToArray(), requiredNode, path);
                }
            }
        }

        [Test]
        public void HollowUnityBehaviorNodeWrappersExistForLibrarySubgraphs()
        {
            Assert.NotNull(typeof(HollowEnemyAlertedCondition));
            Assert.NotNull(typeof(HollowEnemyNoticePlayerAction));
            Assert.NotNull(typeof(HollowEnemyInvestigateNoiseAction));
            Assert.NotNull(typeof(HollowEnemyFleeAction));
            Assert.NotNull(typeof(HollowEnemyCircleAction));
            Assert.NotNull(typeof(HollowEnemyChaseApproachAction));
            Assert.NotNull(typeof(HollowEnemyRequestAttackSlotAction));
            Assert.NotNull(typeof(HollowEnemyStartLinkedAction));
            Assert.NotNull(typeof(HollowEnemyRecoverHoldAction));
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.DocsPath), Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.ReportPath), Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.DocsPath);
            StringAssert.Contains("Behavior Graph Subgraph Library", markdown);
            StringAssert.Contains("notice player", markdown.ToLowerInvariant());
            StringAssert.Contains("request attack slot", markdown.ToLowerInvariant());
            StringAssert.Contains("EnemyActionScorer", markdown);
            Assert.IsTrue(Milestone106BehaviorGraphSubgraphLibraryValidator.Validate());
        }
    }
}
