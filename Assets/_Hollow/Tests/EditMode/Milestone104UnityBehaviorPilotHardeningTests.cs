using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone104UnityBehaviorPilotHardeningTests
    {
        [Test]
        public void BlackboardSchemaDefinesStableRequiredInputsAndOutputs()
        {
            Assert.AreEqual(104, EnemyUnityBehaviorBlackboardSchema.SchemaVersion);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredInputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.InputDistanceToPlayer);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredInputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.InputAwareness);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredInputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.InputDisposition);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredInputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.InputEndangered);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredInputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.InputIdle);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredInputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.InputTacticalRole);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredInputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.InputPathStatus);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredOutputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.OutputCommandKind);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredOutputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.OutputActionId);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredOutputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.OutputSpeedMultiplier);
            CollectionAssert.Contains(EnemyUnityBehaviorBlackboardSchema.RequiredOutputNames.ToArray(), EnemyUnityBehaviorBlackboardSchema.OutputReason);
        }

        [Test]
        public void RatAndSkeletonSwordResolveHardenedGraphContracts()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            AssertHardenedPilot(catalog.Resolve("spawnEnemyRat"), EnemyUnityBehaviorPilotKind.CritterFamily);
            AssertHardenedPilot(catalog.Resolve("spawnEnemySkeletonSword"), EnemyUnityBehaviorPilotKind.WeaponUserFamily);
        }

        [Test]
        public void RatEmergencyFallbackIsExplicitAndTraceVisibleWhenOfficialGraphMissing()
        {
            var root = CreateHarness(out var player);
            try
            {
                var rat = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyRat"));
                rat.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.9f);

                rat.Tick(0.05f, 5f);

                Assert.NotNull(rat.UnityBehaviorGraphBridge);
                Assert.IsTrue(rat.UnityBehaviorGraphBridge.UsedEmergencyFallbackLastEvaluation);
                StringAssert.Contains("unity_behavior_graph", rat.UnityBehaviorGraphBridge.LastOfficialGraphFailureReason);
                StringAssert.Contains("unity_behavior_emergency_fallback", rat.LastBehaviorTreeNodeId);
                Assert.That(rat.UnityBehaviorGraphBridge.TraceHistory.Count, Is.GreaterThan(0));
                var trace = rat.UnityBehaviorGraphBridge.TraceHistory[rat.UnityBehaviorGraphBridge.TraceHistory.Count - 1];
                Assert.AreEqual("emergency_fallback", trace.Source);
                Assert.AreEqual(EnemyUnityBehaviorPilotKind.CritterFamily, trace.PilotKind);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SkeletonSwordEmergencyFallbackStillUsesCommittedHollowAction()
        {
            var root = CreateHarness(out var player);
            try
            {
                var skeleton = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySkeletonSword"));
                skeleton.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.2f);

                skeleton.Tick(0.05f, 3f);

                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, skeleton.ReadabilityState);
                Assert.AreEqual("rusty_slash", skeleton.LastBehaviorReason);
                Assert.NotNull(skeleton.UnityBehaviorGraphBridge);
                Assert.IsTrue(skeleton.UnityBehaviorGraphBridge.UsedEmergencyFallbackLastEvaluation);
                var trace = skeleton.UnityBehaviorGraphBridge.TraceHistory[skeleton.UnityBehaviorGraphBridge.TraceHistory.Count - 1];
                Assert.AreEqual("emergency_fallback", trace.Source);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone104UnityBehaviorPilotHardeningAssetGenerator.DocsPath), Milestone104UnityBehaviorPilotHardeningAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone104UnityBehaviorPilotHardeningAssetGenerator.ReportPath), Milestone104UnityBehaviorPilotHardeningAssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone104UnityBehaviorPilotHardeningAssetGenerator.DocsPath);
            StringAssert.Contains("Unity Behavior Pilot Hardening", markdown);
            StringAssert.Contains("Emergency fallback", markdown);
            StringAssert.Contains("Rat", markdown);
            StringAssert.Contains("Skeleton Sword", markdown);
            StringAssert.Contains("OutputCommandKind", markdown);
            Assert.IsTrue(Milestone104UnityBehaviorPilotHardeningValidator.Validate());
        }

        private static void AssertHardenedPilot(EnemyDefinition enemy, EnemyUnityBehaviorPilotKind pilotKind)
        {
            Assert.NotNull(enemy);
            Assert.AreEqual(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, enemy.BehaviorRuntimeMode, enemy.SpawnKind);
            Assert.NotNull(enemy.UnityBehaviorGraph, enemy.SpawnKind);
            Assert.AreEqual(pilotKind, enemy.UnityBehaviorGraph.PilotKind, enemy.SpawnKind);
            Assert.AreEqual(EnemyUnityBehaviorBlackboardSchema.SchemaVersion, enemy.UnityBehaviorGraph.SchemaVersion, enemy.SpawnKind);
            Assert.IsTrue(enemy.UnityBehaviorGraph.RequiresOfficialBehaviorGraph, enemy.SpawnKind);
            Assert.AreEqual(EnemyUnityBehaviorFallbackPolicy.EmergencyOnly, enemy.UnityBehaviorGraph.FallbackPolicy, enemy.SpawnKind);
            Assert.IsTrue(EnemyUnityBehaviorBlackboardSchema.TryValidateDefinition(enemy.UnityBehaviorGraph, out var reason), reason);
        }

        private static GameObject CreateHarness(out PlaceholderPlayerController player)
        {
            var root = new GameObject("M104Harness");
            player = CreatePlayer(root.transform).GetComponent<PlaceholderPlayerController>();
            return root;
        }

        private static GameObject CreatePlayer(Transform parent)
        {
            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(parent, false);
            var player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            playerObject.AddComponent<CombatKnockbackReceiver>().Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            return playerObject;
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, PlaceholderPlayerController player, EnemyDefinition definition)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(null, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            enemy.ConfigureSpawnContext(null, null, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample(), new CombatDiagnosticsModel(), 0);
            return enemy;
        }
    }
}
