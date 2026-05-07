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
    public sealed class Milestone105UnityBehaviorFamilyMigrationTests
    {
        [Test]
        public void EveryCurrentNonBossEnemyResolvesUnityBehaviorFamilyRuntime()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var enemy in catalog.Definitions.Where(enemy => enemy != null && enemy.ArchetypeId != EnemyArchetypeId.Boss))
            {
                Assert.AreEqual(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, enemy.BehaviorRuntimeMode, enemy.SpawnKind);
                Assert.NotNull(enemy.UnityBehaviorGraph, enemy.SpawnKind);
                Assert.AreNotEqual(EnemyUnityBehaviorPilotKind.None, enemy.UnityBehaviorGraph.PilotKind, enemy.SpawnKind);
                Assert.AreEqual(EnemyUnityBehaviorFallbackPolicy.EmergencyOnly, enemy.UnityBehaviorGraph.FallbackPolicy, enemy.SpawnKind);
                Assert.IsTrue(EnemyUnityBehaviorBlackboardSchema.TryValidateDefinition(enemy.UnityBehaviorGraph, out var reason), $"{enemy.SpawnKind}: {reason}");
            }

            Assert.AreNotEqual(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, catalog.Resolve("spawnEnemyBoss").BehaviorRuntimeMode);
        }

        [Test]
        public void FamilyMappingsCoverTheCurrentRoster()
        {
            Assert.AreEqual(EnemyUnityBehaviorPilotKind.ChaserFamily, EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor("spawnEnemyNormal"));
            Assert.AreEqual(EnemyUnityBehaviorPilotKind.ChaserFamily, EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor("spawnEnemyCharger"));
            Assert.AreEqual(EnemyUnityBehaviorPilotKind.CritterFamily, EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor("spawnEnemyRat"));
            Assert.AreEqual(EnemyUnityBehaviorPilotKind.CritterFamily, EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor("spawnEnemySpider"));
            Assert.AreEqual(EnemyUnityBehaviorPilotKind.WeaponUserFamily, EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor("spawnEnemySkeletonSword"));
            Assert.AreEqual(EnemyUnityBehaviorPilotKind.WeaponUserFamily, EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor("spawnEnemyKnight"));
            Assert.AreEqual(EnemyUnityBehaviorPilotKind.RangedFirearmFamily, EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor("spawnEnemyPowderGunner"));
            Assert.AreEqual(EnemyUnityBehaviorPilotKind.MagicGhostFamily, EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor("spawnEnemyWraith"));
            Assert.AreEqual(EnemyUnityBehaviorPilotKind.None, EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor("spawnEnemyBoss"));
        }

        [Test]
        public void ChaserFamilyUsesUnityBehaviorIntentAndHollowScorerChoosesConcreteAction()
        {
            var root = CreateHarness(out var player);
            try
            {
                var normal = CreateEnemy(root.transform, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                normal.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.25f);

                normal.Tick(0.05f, 4f);

                Assert.AreEqual(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, normal.BehaviorRuntimeMode);
                Assert.NotNull(normal.UnityBehaviorGraphBridge);
                Assert.AreEqual(EnemyUnityBehaviorPilotKind.ChaserFamily, normal.UnityBehaviorGraphDefinition.PilotKind);
                Assert.IsTrue(normal.UnityBehaviorGraphBridge.UsedEmergencyFallbackLastEvaluation);
                StringAssert.Contains("unity_behavior_emergency_fallback", normal.LastBehaviorTreeNodeId);
                Assert.That(normal.LastBehaviorCommand, Is.EqualTo(EnemyBehaviorCommandKind.StartMeleeAction.ToString()).Or.EqualTo(EnemyBehaviorCommandKind.MovePreferredRange.ToString()));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RangedAndMagicFamiliesRequestIntentWithoutOwningDamageMath()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            AssertIntentFamily(catalog.Resolve("spawnEnemyHollowArcher"), EnemyUnityBehaviorPilotKind.RangedFirearmFamily);
            AssertIntentFamily(catalog.Resolve("spawnEnemyCurseBinder"), EnemyUnityBehaviorPilotKind.MagicGhostFamily);
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone105UnityBehaviorFamilyMigrationAssetGenerator.DocsPath), Milestone105UnityBehaviorFamilyMigrationAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone105UnityBehaviorFamilyMigrationAssetGenerator.ReportPath), Milestone105UnityBehaviorFamilyMigrationAssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone105UnityBehaviorFamilyMigrationAssetGenerator.DocsPath);
            StringAssert.Contains("Unity Behavior Family Migration", markdown);
            StringAssert.Contains("Critters", markdown);
            StringAssert.Contains("Weapon Users", markdown);
            StringAssert.Contains("EnemyActionScorer", markdown);
            Assert.IsTrue(Milestone105UnityBehaviorFamilyMigrationValidator.Validate());
        }

        private static void AssertIntentFamily(EnemyDefinition enemy, EnemyUnityBehaviorPilotKind expectedKind)
        {
            Assert.NotNull(enemy);
            Assert.AreEqual(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, enemy.BehaviorRuntimeMode, enemy.SpawnKind);
            Assert.NotNull(enemy.UnityBehaviorGraph, enemy.SpawnKind);
            Assert.AreEqual(expectedKind, enemy.UnityBehaviorGraph.PilotKind, enemy.SpawnKind);
            Assert.IsTrue(enemy.UnityBehaviorGraph.Notes.Contains("official", System.StringComparison.OrdinalIgnoreCase) ||
                          enemy.UnityBehaviorGraph.Notes.Contains("Runtime", System.StringComparison.OrdinalIgnoreCase));
        }

        private static GameObject CreateHarness(out PlaceholderPlayerController player)
        {
            var root = new GameObject("M105Harness");
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
