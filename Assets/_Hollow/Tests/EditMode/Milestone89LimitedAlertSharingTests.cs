using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    public sealed class Milestone89LimitedAlertSharingTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void AlertSharingDefaultsSelectDisciplinedEnemiesOnly()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            Assert.AreEqual(EnemyStimulusTier.Normal, EnemyStimulusTierExtensions.DefaultFor(EnemyStimulusKind.AllyAlert));

            var knight = catalog.Resolve("spawnEnemyKnight");
            Assert.IsTrue(knight.AllyAlertSharingEnabled);
            Assert.AreEqual(5f, knight.AllyAlertRadiusMeters, 0.001f);
            Assert.AreEqual(EnemyAwarenessState.Engaged, knight.AllyAlertMinimumAwareness);

            Assert.IsTrue(catalog.Resolve("spawnEnemyPowderGunner").AllyAlertSharingEnabled);
            Assert.IsTrue(catalog.Resolve("spawnEnemyCurseBinder").AllyAlertSharingEnabled);
            Assert.IsFalse(catalog.Resolve("spawnEnemyRat").AllyAlertSharingEnabled);
            Assert.IsFalse(catalog.Resolve("spawnEnemySpider").AllyAlertSharingEnabled);
            Assert.IsFalse(catalog.Resolve("spawnEnemyHollowBird").AllyAlertSharingEnabled);
            Assert.IsFalse(catalog.Resolve("spawnEnemyBoss").AllyAlertSharingEnabled);
        }

        [Test]
        public void SelectedEnemyDamageSharesAllyAlertToNearbyNonBossOnly()
        {
            var root = CreateHarness(out var room, out var player, out var combat);
            try
            {
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var source = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyKnight"));
                var nearby = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyRat"));
                var far = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyRat"));
                var bossFallback = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyBoss"));
                source.transform.localPosition = Vector3.zero;
                nearby.transform.localPosition = new Vector3(0f, 0f, 2.2f);
                far.transform.localPosition = new Vector3(0f, 0f, 8.5f);
                bossFallback.transform.localPosition = new Vector3(0f, 0f, 2.6f);
                BindEnemies(combat, source, nearby, far, bossFallback);

                DamageSystem.ApplyDamage(source.Health, new DamageRequest(1, player.gameObject));

                Assert.AreEqual(EnemyAwarenessState.Engaged, source.AwarenessState);
                Assert.AreEqual(1, source.LastAllyAlertRecipientCount);
                Assert.AreEqual(EnemyStimulusKind.AllyAlert, nearby.LastStimulusKind);
                Assert.AreEqual(EnemyAwarenessState.Alerted, nearby.AwarenessState);
                Assert.AreNotEqual(EnemyStimulusKind.AllyAlert, far.LastStimulusKind);
                Assert.AreNotEqual(EnemyStimulusKind.AllyAlert, bossFallback.LastStimulusKind);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UnselectedEnemyDoesNotShareGeneralAllyAlert()
        {
            var root = CreateHarness(out var room, out var player, out var combat);
            try
            {
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var source = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyRat"));
                var nearby = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemySkeletonSword"));
                source.transform.localPosition = Vector3.zero;
                nearby.transform.localPosition = new Vector3(0f, 0f, 1.8f);
                BindEnemies(combat, source, nearby);

                DamageSystem.ApplyDamage(source.Health, new DamageRequest(1, player.gameObject));

                Assert.AreEqual(EnemyAwarenessState.Engaged, source.AwarenessState);
                Assert.AreEqual(0, source.LastAllyAlertRecipientCount);
                Assert.AreNotEqual(EnemyStimulusKind.AllyAlert, nearby.LastStimulusKind);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AllyAlertDoesNotCascadeThroughSelectedRecipients()
        {
            var root = CreateHarness(out var room, out var player, out var combat);
            try
            {
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var source = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyKnight"));
                var selectedRecipient = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemySkeletonSpear"));
                var outsideSourceRange = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyRat"));
                source.transform.localPosition = Vector3.zero;
                selectedRecipient.transform.localPosition = new Vector3(0f, 0f, 3f);
                outsideSourceRange.transform.localPosition = new Vector3(0f, 0f, 6.8f);
                BindEnemies(combat, source, selectedRecipient, outsideSourceRange);

                DamageSystem.ApplyDamage(source.Health, new DamageRequest(1, player.gameObject));

                Assert.AreEqual(1, source.LastAllyAlertRecipientCount);
                Assert.AreEqual(EnemyStimulusKind.AllyAlert, selectedRecipient.LastStimulusKind);
                Assert.AreEqual(0, selectedRecipient.LastAllyAlertRecipientCount);
                Assert.AreNotEqual(EnemyStimulusKind.AllyAlert, outsideSourceRange.LastStimulusKind);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AllyAlertCooldownLimitsRepeatedBroadcasts()
        {
            var root = CreateHarness(out var room, out var player, out var combat);
            try
            {
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var source = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyKnight"));
                var first = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyRat"));
                var second = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemySpider"));
                source.transform.localPosition = Vector3.zero;
                first.transform.localPosition = new Vector3(0f, 0f, 2f);
                second.transform.localPosition = new Vector3(0f, 0f, -2f);
                BindEnemies(combat, source, first, second);

                source.ReceiveStimulus(EnemyStimulusKind.Damage, player.transform.localPosition, 3f, EnemyStimulusTier.Violent, "first_hit");
                Assert.AreEqual(2, source.LastAllyAlertRecipientCount);
                var firstBroadcast = source.LastAllyAlertSharedTime;

                source.ReceiveStimulus(EnemyStimulusKind.Damage, player.transform.localPosition, 3.4f, EnemyStimulusTier.Violent, "second_hit");
                Assert.AreEqual(firstBroadcast, source.LastAllyAlertSharedTime);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone89AssetGenerator.DocsPath), Milestone89AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone89AssetGenerator.ReportPath), Milestone89AssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone89AssetGenerator.DocsPath);
            StringAssert.Contains("Limited Alert Sharing", markdown);
            StringAssert.Contains("AllyAlert", markdown);
            StringAssert.Contains("M88", markdown);
            StringAssert.Contains("M90", markdown);
            Assert.IsTrue(Milestone89Validator.Validate());
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player, out RoomCombatController combat)
        {
            var root = new GameObject("M89Harness");
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

            combat = root.AddComponent<RoomCombatController>();
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

        private static void BindEnemies(RoomCombatController combat, params EnemyRuntimeController[] enemies)
        {
            var field = typeof(RoomCombatController).GetField("enemies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var list = (List<EnemyRuntimeController>)field.GetValue(combat);
            list.Clear();
            foreach (var enemy in enemies)
            {
                enemy.BindRoomCombatController(combat);
                list.Add(enemy);
            }
        }
    }
}
