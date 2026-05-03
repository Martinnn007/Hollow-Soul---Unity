using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone90CombatAiQaLockTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void CurrentRosterResolvesModernCombatContracts()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            var enemies = catalog.Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss").ToArray();
            Assert.GreaterOrEqual(enemies.Length, 20);

            foreach (var enemy in enemies)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(enemy.DisplayName), enemy.SpawnKind);
                Assert.AreEqual(EnemyContactDamagePolicy.ActiveOnly, enemy.ContactDamagePolicy, enemy.SpawnKind);
                Assert.AreEqual(EnemyPassiveContactHazardType.None, enemy.PassiveContactHazardType, enemy.SpawnKind);
                Assert.Greater(enemy.PreferredRangeMaxMeters, enemy.PreferredRangeMinMeters, enemy.SpawnKind);
                Assert.Greater(enemy.AttackProfiles.Count, 0, enemy.SpawnKind);
                Assert.Greater(enemy.ActionProfiles.Count, 0, enemy.SpawnKind);
                Assert.NotNull(enemy.BehaviorTree, enemy.SpawnKind);
                Assert.NotNull(enemy.BehaviorTree.RootNode, enemy.SpawnKind);

                foreach (var attack in enemy.AttackProfiles)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(attack.AttackId), enemy.SpawnKind);
                    Assert.Greater(attack.ActiveSeconds, 0f, attack.AttackId);
                    Assert.Greater(attack.RecoverySeconds, 0f, attack.AttackId);
                    Assert.Greater(attack.HitArcDegrees, 0f, attack.AttackId);
                    Assert.LessOrEqual(attack.HitArcDegrees, 360f, attack.AttackId);
                }
            }
        }

        [Test]
        public void BossRosterKeepsMetadataButRuntimeContactPolicyUnchanged()
        {
            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            Assert.GreaterOrEqual(bosses.Length, 10);

            foreach (var boss in bosses)
            {
                Assert.AreEqual(EnemyContactDamagePolicy.ActiveOnly, boss.ContactDamagePolicy, boss.BossId);
                Assert.AreEqual(EnemyPassiveContactHazardType.None, boss.PassiveContactHazardType, boss.BossId);
                Assert.Greater(boss.AttackProfiles.Count, 0, boss.BossId);
                Assert.Greater(boss.ActionProfiles.Count, 0, boss.BossId);
                Assert.NotNull(boss.BehaviorTreeMetadata, boss.BossId);
                Assert.NotNull(boss.BehaviorTreeMetadata.RootNode, boss.BossId);
            }
        }

        [Test]
        public void ContactAndPriorityRulesRemainLocked()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var normal = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                normal.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.1f);

                Assert.IsFalse(normal.TryApplyContactDamage(1f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);

                player.transform.localPosition = new Vector3(0f, 0f, 2f);
                normal.ApplyIntelligenceDisposition(EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Predator);
                var simplePriority = normal.AttackPriorityScore(2f);
                normal.ApplyIntelligenceDisposition(EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Predator);
                var trainedPriority = normal.AttackPriorityScore(2f);
                normal.ApplyIntelligenceDisposition(EnemyIntelligenceLevel.Tactical, EnemyInstinctDisposition.Predator);
                var tacticalPriority = normal.AttackPriorityScore(2f);
                normal.ApplyIntelligenceDisposition(EnemyIntelligenceLevel.Cunning, EnemyInstinctDisposition.Predator);
                var cunningPriority = normal.AttackPriorityScore(2f);

                Assert.AreEqual(simplePriority, trainedPriority, 0.001f);
                Assert.Greater(tacticalPriority, trainedPriority);
                Assert.Greater(cunningPriority, tacticalPriority);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NavigationStimulusAndAlertSharingContractsRemainStable()
        {
            Assert.AreEqual(EnemyNavigationBackend.LocalSteering, EnemyNavigationAdapter.CurrentBackend);
            Assert.AreEqual(EnemyStimulusTier.Normal, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.AllyAlert));
            Assert.AreEqual(EnemyStimulusTier.Quiet, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.Footstep));
            Assert.AreEqual(EnemyStimulusTier.Loud, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.GuardImpact));
            Assert.AreEqual(EnemyStimulusTier.Violent, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.Damage));
        }

        [Test]
        public void DocsRoadmapAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone90AssetGenerator.DocsPath), Milestone90AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone90AssetGenerator.ReportPath), Milestone90AssetGenerator.ReportPath);

            var markdown = File.ReadAllText(Milestone90AssetGenerator.DocsPath);
            StringAssert.Contains("Combat AI QA Lock", markdown);
            StringAssert.Contains("active windows", markdown);
            StringAssert.Contains("weapon users", markdown);
            StringAssert.Contains("knockback", markdown);
            StringAssert.Contains("M91 Preferred Distance", markdown);
            StringAssert.Contains("M93 Boss Behavior Trees", markdown);
            StringAssert.Contains("M100 Combat AI QA Lock 2", markdown);
            Assert.IsTrue(Milestone90Validator.Validate());
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M90Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            playerObject.AddComponent<CombatKnockbackReceiver>().Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            return root;
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(room, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            return enemy;
        }
    }
}
