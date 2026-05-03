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
    public sealed class Milestone82BehaviorTreeLayerTests
    {
        [Test]
        public void CurrentRosterResolvesRuntimeTreesAndBossesResolveMetadataOnlyTrees()
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss"))
            {
                Assert.NotNull(enemy.BehaviorTree, enemy.SpawnKind);
                Assert.IsFalse(enemy.BehaviorTree.BossMetadataOnly, enemy.SpawnKind);
                Assert.NotNull(enemy.BehaviorTree.RootNode, enemy.SpawnKind);
                Assert.Greater(enemy.BehaviorTree.Nodes.Count, 0, enemy.SpawnKind);
            }

            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                Assert.NotNull(boss.BehaviorTreeMetadata, boss.BossId);
                Assert.IsTrue(boss.BehaviorTreeMetadata.BossMetadataOnly, boss.BossId);
                Assert.NotNull(boss.BehaviorTreeMetadata.RootNode, boss.BossId);
            }
        }

        [Test]
        public void PromotedPrototypeActionsResolveToAttackProfiles()
        {
            foreach (var actionId in Milestone82AssetGenerator.PromotedEnemyActionIds)
            {
                var spec = EnemyAttackProfileDefaults.AllEnemySpecs.FirstOrDefault(candidate => candidate.AttackId == actionId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(spec.AttackId), actionId);
                var profile = EnemyAttackProfileDefaults.ResolveEnemyProfile(spec.OwnerId, actionId);
                Assert.NotNull(profile, actionId);

                var action = EnemyActionProfileDefaults.ResolveEnemyAction(spec.OwnerId, actionId);
                Assert.NotNull(action, actionId);
                Assert.AreEqual(EnemyActionUsageState.CurrentRuntime, action.UsageState, actionId);
                Assert.AreEqual(actionId, action.LinkedAttackId, actionId);
            }
        }

        [Test]
        public void TreeStartsCommittedActionsOnlyFromIdleAndDoesNotReplanDuringWindup()
        {
            var root = CreateHarness(out var player);
            try
            {
                var fast = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFast"), spawnIndex: 0);
                fast.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.3f);

                fast.Tick(0.05f, 2f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, fast.ReadabilityState);
                Assert.AreEqual("side_pounce", fast.LastBehaviorReason);
                var command = fast.LastBehaviorCommand;

                fast.Tick(0.05f, 2.05f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, fast.ReadabilityState);
                Assert.AreEqual(command, fast.LastBehaviorCommand);
                Assert.AreEqual("side_pounce", fast.LastBehaviorReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HeavyStompUsesAreaWindowAndDealsTwoDamageOnlyDuringActive()
        {
            var root = CreateHarness(out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyHeavy");
                var profile = definition.ResolveAttackProfile("stomp");
                var heavy = CreateEnemy(root.transform, player, definition);
                heavy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.1f);
                var windupSeconds = profile.WindupSeconds * heavy.AttackWindupScale;
                var activeSeconds = profile.ActiveSeconds * heavy.AttackActiveScale;

                heavy.Tick(0.05f, 12f);
                Assert.AreEqual(EnemyReadabilityState.AreaWindup, heavy.ReadabilityState);
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);

                heavy.Tick(0.05f, 12f + windupSeconds + 0.01f);
                Assert.AreEqual(EnemyReadabilityState.AreaActive, heavy.ReadabilityState);
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);

                heavy.Tick(0.05f, 12f + windupSeconds + 0.05f);
                Assert.AreEqual(playerHealth.MaxHealth - 2, playerHealth.CurrentHealth);

                heavy.Tick(0.05f, 12f + windupSeconds + activeSeconds + 0.12f);
                Assert.AreEqual(EnemyReadabilityState.AreaRecovery, heavy.ReadabilityState);
                playerHealth.Restore(playerHealth.MaxHealth, playerHealth.MaxHealth);
                Assert.IsFalse(heavy.TryApplyContactDamage(13f));
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RatWarnsBeforeCommittedBiteAndFlyingPreyStillFleesBeforeEndangered()
        {
            var root = CreateHarness(out var player);
            try
            {
                var rat = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyRat"));
                rat.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.9f);

                rat.Tick(0.05f, 5f);
                Assert.AreEqual(EnemyReadabilityState.FeintWarning, rat.ReadabilityState);
                Assert.AreEqual("warning_squeal", rat.LastBehaviorReason);

                rat.Tick(0.05f, 5.5f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, rat.ReadabilityState);
                Assert.AreEqual("rat_bite", rat.LastBehaviorReason);

                var flying = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                flying.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.5f);
                flying.Tick(0.2f, 8f);
                Assert.AreNotEqual(EnemyReadabilityState.MeleeWindup, flying.ReadabilityState);
                Assert.Less(flying.transform.localPosition.z, 0f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SpiderCanChooseDeterministicSideHopBite()
        {
            var root = CreateHarness(out var player);
            try
            {
                var spider = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySpider"));
                spider.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.05f);
                spider.ReceiveStimulus(EnemyStimulusKind.Damage, player.transform.localPosition, 3.7f);

                spider.Tick(0.05f, 3.8f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, spider.ReadabilityState);
                Assert.AreEqual("side_hop_bite", spider.LastBehaviorReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone82AssetGenerator.DocsPath), Milestone82AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone82AssetGenerator.ReportPath), Milestone82AssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone82AssetGenerator.DocsPath);
            StringAssert.Contains("Lightweight Behavior Tree Layer", markdown);
            StringAssert.Contains("side_pounce", markdown);
            StringAssert.Contains("stomp", markdown);
            StringAssert.Contains("warning_squeal", markdown);
            StringAssert.Contains("side_hop_bite", markdown);
            StringAssert.Contains("metadata-only", markdown);
            Assert.IsTrue(Milestone82Validator.Validate());
        }

        private static GameObject CreateHarness(out PlaceholderPlayerController player)
        {
            var root = new GameObject("M82Harness");
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

        private static EnemyRuntimeController CreateEnemy(Transform parent, PlaceholderPlayerController player, EnemyDefinition definition, int spawnIndex = -1)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(null, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            enemy.ConfigureSpawnContext(null, null, null, null, null, spawnIndex);
            return enemy;
        }
    }
}
