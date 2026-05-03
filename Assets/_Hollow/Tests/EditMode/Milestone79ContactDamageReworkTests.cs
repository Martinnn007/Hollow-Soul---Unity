using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone79ContactDamageReworkTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void CurrentRosterDefaultsToActiveOnlyWithoutPassiveHazards()
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null))
            {
                Assert.AreEqual(EnemyContactDamagePolicy.ActiveOnly, enemy.ContactDamagePolicy, enemy.SpawnKind);
                Assert.AreEqual(EnemyPassiveContactHazardType.None, enemy.PassiveContactHazardType, enemy.SpawnKind);
            }

            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                Assert.AreEqual(EnemyContactDamagePolicy.ActiveOnly, boss.ContactDamagePolicy, boss.BossId);
                Assert.AreEqual(EnemyPassiveContactHazardType.None, boss.PassiveContactHazardType, boss.BossId);
            }
        }

        [Test]
        public void PassiveOverlapDoesNotDamageCurrentNonBossRoster()
        {
            var spawnKinds = new[]
            {
                "spawnEnemyNormal",
                "spawnEnemyFlying",
                "spawnEnemyHeavy",
                "spawnEnemyTurret",
                "spawnEnemySpittingPod",
                "spawnEnemyRat",
                "spawnEnemySpider"
            };

            foreach (var spawnKind in spawnKinds)
            {
                var root = CreateHarness(out var room, out var player);
                try
                {
                    var playerHealth = player.GetComponent<CombatantHealth>();
                    var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve(spawnKind));
                    enemy.transform.localPosition = Vector3.zero;
                    player.transform.localPosition = new Vector3(0f, 0f, 0.2f);

                    Assert.IsFalse(enemy.TryApplyContactDamage(4f), spawnKind);
                    Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth, spawnKind);
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void PassiveOverlapDisturbsWithoutHurting()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.2f);

                Assert.AreEqual(EnemyAwarenessState.Unaware, enemy.AwarenessState);
                Assert.IsFalse(enemy.TryApplyContactDamage(5f));

                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);
                Assert.AreEqual(EnemyAwarenessState.Engaged, enemy.AwarenessState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MeleeLungeDamagesOncePerActivation()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal");
                var profile = definition.ResolveAttackProfile("claw_lunge");
                player.transform.localPosition = new Vector3(0f, 0f, 1.35f);
                var enemy = CreateEnemy(root.transform, room, player, definition);
                enemy.transform.localPosition = Vector3.zero;

                enemy.Tick(0.05f, 6f);
                enemy.Tick(0.05f, 6f + profile.WindupSeconds + 0.01f);
                enemy.transform.localPosition = new Vector3(0f, 0f, 0.82f);

                Assert.AreEqual(EnemyReadabilityState.MeleeLunge, enemy.ReadabilityState);
                Assert.IsTrue(enemy.TryApplyContactDamage(6f + profile.WindupSeconds + 0.08f));
                Assert.IsFalse(enemy.TryApplyContactDamage(6f + profile.WindupSeconds + 0.09f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth - profile.Damage, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ChargeDamagesOncePerActivation()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyCharger");
                var profile = definition.ResolveAttackProfile("ash_charge");
                player.transform.localPosition = new Vector3(0f, 0f, 3f);
                var enemy = CreateEnemy(root.transform, room, player, definition);
                enemy.transform.localPosition = Vector3.zero;

                enemy.Tick(0.05f, 8f);
                enemy.Tick(0.05f, 8f + profile.WindupSeconds + 0.01f);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.2f);

                Assert.AreEqual(EnemyReadabilityState.Charging, enemy.ReadabilityState);
                Assert.IsTrue(enemy.TryApplyContactDamage(8f + profile.WindupSeconds + 0.08f));
                Assert.IsFalse(enemy.TryApplyContactDamage(8f + profile.WindupSeconds + 0.09f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth - profile.Damage, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BossContactRequiresArmedActiveWindow()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var boss = CreateBoss(root.transform, room, player, BossDefinition.CreateRuntime(
                    "mirror_husk",
                    "Mirror Husk",
                    BossWorldBand.World2,
                    BossBehaviorId.MirrorHusk,
                    34,
                    "test_arena",
                    "Test Arena",
                    1.15f,
                    0.62f,
                    2.1f,
                    Color.white));
                boss.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.2f);

                Assert.IsFalse(boss.TryApplyContactDamage(12f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);

                var stoneProfile = EnemyAttackProfileDefaults.ResolveBossProfile("stone_warden", "stone_charge");
                var stone = CreateBoss(root.transform, room, player, BossDefinition.CreateRuntime(
                    "stone_warden",
                    "Stone Warden",
                    BossWorldBand.World1,
                    BossBehaviorId.StoneWarden,
                    24,
                    "test_arena",
                    "Test Arena",
                    0.82f,
                    0.68f,
                    2f,
                    Color.gray));
                stone.transform.localPosition = Vector3.zero;
                stone.ArmBossActiveContactWindow(stoneProfile, 13f);

                Assert.IsTrue(stone.TryApplyContactDamage(13.05f));
                Assert.IsFalse(stone.TryApplyContactDamage(13.06f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth - stoneProfile.Damage, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PassiveHazardBodyTicksOnContactCooldown()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var definition = EnemyDefinition.CreateRuntime(
                    "spawnSyntheticHazard",
                    "Synthetic Hazard",
                    EnemyArchetypeId.Normal,
                    EnemyMovementMode.Grounded,
                    3,
                    0f,
                    1,
                    Color.red);
                definition.ConfigureContactPolicy(EnemyContactDamagePolicy.PassiveHazard, EnemyPassiveContactHazardType.Fire);
                var enemy = CreateEnemy(root.transform, room, player, definition);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.2f);

                Assert.IsTrue(enemy.TryApplyContactDamage(1f));
                Assert.IsFalse(enemy.TryApplyContactDamage(1.5f));
                Assert.IsTrue(enemy.TryApplyContactDamage(2.05f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth - 2, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_M79_Contact_Damage_Rework.md"));
            Assert.IsTrue(File.Exists("output/reports/m79_contact_damage_rework.md"));
            var markdown = File.ReadAllText("Docs/Hollow_M79_Contact_Damage_Rework.md");
            StringAssert.Contains("Contact Damage Rework", markdown);
            StringAssert.Contains("ActiveOnly", markdown);
            StringAssert.Contains("PassiveHazard", markdown);
            StringAssert.Contains("Disabled", markdown);
            StringAssert.Contains("Proximity", markdown);
            StringAssert.Contains("Mirror Husk", markdown);
            Assert.IsTrue(Milestone79Validator.Validate());
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M79Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            var receiver = playerObject.AddComponent<CombatKnockbackReceiver>();
            receiver.Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            receiver.ConfigureStability(0);
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

        private static EnemyRuntimeController CreateBoss(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, BossDefinition definition)
        {
            var enemy = CreateEnemy(parent, room, player, EnemyDefinition.CreateRuntimeBoss());
            enemy.ConfigureBoss(definition);
            return enemy;
        }
    }
}
