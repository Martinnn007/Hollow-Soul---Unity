using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using NUnit.Framework;
using Unity.Behavior;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone98UnityBehaviorMigrationTests
    {
        [Test]
        public void UnityBehaviorPackageTypesAndBakeOffCandidateResolve()
        {
            Assert.IsTrue(EnemyUnityBehaviorPackageProbe.TypesAvailable);
            Assert.AreEqual("Unity.Behavior", EnemyUnityBehaviorPackageProbe.RuntimeAssemblyName);
            Assert.NotNull(typeof(BehaviorGraphAgent));
            Assert.NotNull(typeof(BehaviorGraph));

            var options = EnemyAiToolBakeOffEvaluation.Options.Select(option => option.Name).ToArray();
            CollectionAssert.Contains(options, "Unity Behavior");
            CollectionAssert.Contains(options, "Behavior Designer Pro 3");
            Assert.IsFalse(EnemyAiToolBakeOffEvaluation.Resolve("Unity Behavior").RequiresPurchase);
        }

        [Test]
        public void RatAndSkeletonSwordResolveUnityBehaviorRuntimeMode()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            AssertPilot(catalog.Resolve("spawnEnemyRat"), EnemyUnityBehaviorPilotKind.CritterFamily);
            AssertPilot(catalog.Resolve("spawnEnemySkeletonSword"), EnemyUnityBehaviorPilotKind.WeaponUserFamily);

            foreach (var enemy in catalog.Definitions.Where(enemy => enemy != null && enemy.ArchetypeId == EnemyArchetypeId.Boss))
            {
                Assert.AreEqual(EnemyBehaviorRuntimeMode.HollowBehaviorTree, enemy.BehaviorRuntimeMode, enemy.SpawnKind);
            }
        }

        [Test]
        public void RatUnityBehaviorFallbackWarnsBitesAndFleesThroughHollowCommands()
        {
            var root = CreateHarness(out var player);
            try
            {
                var rat = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyRat"));
                rat.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.9f);

                rat.Tick(0.05f, 5f);
                Assert.AreEqual(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, rat.BehaviorRuntimeMode);
                Assert.NotNull(rat.UnityBehaviorGraphDefinition);
                Assert.AreEqual(EnemyReadabilityState.FeintWarning, rat.ReadabilityState);
                Assert.AreEqual("warning_squeal", rat.LastBehaviorReason);
                StringAssert.Contains("unity_behavior", rat.LastBehaviorTreeNodeId);

                rat.Tick(1.35f, 6.35f);
                rat.Tick(0.05f, 6.4f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, rat.ReadabilityState);
                Assert.AreEqual("rat_bite", rat.LastBehaviorReason);

                rat.Health.ApplyDamage(new DamageRequest(1, player.gameObject));
                rat.Tick(0.05f, 8f);
                Assert.That(rat.LastBehaviorCommand, Is.EqualTo(EnemyBehaviorCommandKind.StartCreatureMoveAction.ToString()).Or.EqualTo(EnemyBehaviorCommandKind.Flee.ToString()));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SkeletonSwordUnityBehaviorFallbackStartsRustySlashAndKeepsActiveWindowRules()
        {
            var root = CreateHarness(out var player);
            try
            {
                var skeleton = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySkeletonSword"));
                skeleton.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.2f);

                skeleton.Tick(0.05f, 3f);
                Assert.AreEqual(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, skeleton.BehaviorRuntimeMode);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, skeleton.ReadabilityState);
                Assert.AreEqual("rusty_slash", skeleton.LastBehaviorReason);

                var stateAfterWindupStart = skeleton.ReadabilityState;
                skeleton.Tick(0.05f, 3.05f);
                Assert.AreEqual(stateAfterWindupStart, skeleton.ReadabilityState);
                StringAssert.Contains("rusty_slash", skeleton.LastBehaviorReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone98UnityBehaviorMigrationAssetGenerator.DocsPath), Milestone98UnityBehaviorMigrationAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone98UnityBehaviorMigrationAssetGenerator.ReportPath), Milestone98UnityBehaviorMigrationAssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone98UnityBehaviorMigrationAssetGenerator.DocsPath);
            StringAssert.Contains("Unity Behavior", markdown);
            StringAssert.Contains("Rat", markdown);
            StringAssert.Contains("Skeleton Sword", markdown);
            StringAssert.Contains("OutputCommandKind", markdown);
            Assert.IsTrue(Milestone98UnityBehaviorMigrationValidator.Validate());
        }

        private static void AssertPilot(EnemyDefinition enemy, EnemyUnityBehaviorPilotKind pilotKind)
        {
            Assert.NotNull(enemy);
            Assert.AreEqual(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, enemy.BehaviorRuntimeMode, enemy.SpawnKind);
            Assert.NotNull(enemy.UnityBehaviorGraph, enemy.SpawnKind);
            Assert.AreEqual(pilotKind, enemy.UnityBehaviorGraph.PilotKind, enemy.SpawnKind);
        }

        private static GameObject CreateHarness(out PlaceholderPlayerController player)
        {
            var root = new GameObject("M98Harness");
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
