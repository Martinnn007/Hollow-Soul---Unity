using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone83NoiseDisturbanceTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void CurrentRosterHasValidDisturbanceTuning()
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null))
            {
                var expected = EnemyDefinition.DefaultDisturbanceTuningFor(enemy.ArchetypeId, enemy.BehaviorId, enemy.MovementMode);
                Assert.AreEqual(expected.x, enemy.HearingSensitivityMultiplier, 0.001f, enemy.SpawnKind);
                Assert.AreEqual(expected.y, enemy.DisturbanceEscalationThreshold, 0.001f, enemy.SpawnKind);
                Assert.AreEqual(expected.z, enemy.InvestigationDurationSeconds, 0.001f, enemy.SpawnKind);
                Assert.Greater(enemy.HearingSensitivityMultiplier, 0f, enemy.SpawnKind);
                Assert.Greater(enemy.DisturbanceEscalationThreshold, 0f, enemy.SpawnKind);
                Assert.Greater(enemy.InvestigationDurationSeconds, 0f, enemy.SpawnKind);
            }
        }

        [Test]
        public void StimulusTierMappingMatchesM83Contract()
        {
            Assert.AreEqual(EnemyStimulusTier.Quiet, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.Footstep));
            Assert.AreEqual(EnemyStimulusTier.Normal, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.Roll));
            Assert.AreEqual(EnemyStimulusTier.Normal, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.Bump));
            Assert.AreEqual(EnemyStimulusTier.Normal, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.Proximity));
            Assert.AreEqual(EnemyStimulusTier.Loud, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.GuardImpact));
            Assert.AreEqual(EnemyStimulusTier.Violent, RoomCombatController.DefaultStimulusTierFor(EnemyStimulusKind.Damage));
            Assert.AreEqual(EnemyStimulusTier.Normal, RoomCombatController.StimulusTierForPlayerAttack(AttackKind.Light));
            Assert.AreEqual(EnemyStimulusTier.Loud, RoomCombatController.StimulusTierForPlayerAttack(AttackKind.Heavy));
        }

        [Test]
        public void PreyFootstepsStartleAndFleeBeforeAttackPanicEngages()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var prey = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                prey.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, -3f);

                prey.ReceiveStimulus(EnemyStimulusKind.Footstep, player.transform.localPosition, 2f, EnemyStimulusTier.Quiet);
                Assert.AreEqual(EnemyAwarenessState.Suspicious, prey.AwarenessState);
                prey.ReceiveStimulus(EnemyStimulusKind.Footstep, player.transform.localPosition, 2.4f, EnemyStimulusTier.Quiet);
                Assert.AreEqual(EnemyAwarenessState.Alerted, prey.AwarenessState);

                prey.Tick(0.2f, 2.5f);
                Assert.AreNotEqual(EnemyAwarenessState.Engaged, prey.AwarenessState);
                Assert.Greater(prey.transform.localPosition.z, 0f);

                prey.ReceiveStimulus(EnemyStimulusKind.RangedAttack, player.transform.localPosition, 3f, EnemyStimulusTier.Normal);
                Assert.AreEqual(EnemyAwarenessState.Engaged, prey.AwarenessState);
                StringAssert.Contains("prey", prey.LastAwarenessReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SentinelFacesAndHoldsUntilLoudDisturbanceCommits()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var turret = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret"));
                turret.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 6.2f);

                turret.ReceiveStimulus(EnemyStimulusKind.Footstep, new Vector3(0f, 0f, 2f), 2f, EnemyStimulusTier.Quiet);
                turret.Tick(0.05f, 2.1f);
                Assert.AreEqual(EnemyAwarenessState.Alerted, turret.AwarenessState);
                Assert.AreEqual(EnemyReadabilityState.Idle, turret.ReadabilityState);
                Assert.AreEqual(Vector3.zero, turret.transform.localPosition);

                turret.ReceiveStimulus(EnemyStimulusKind.GuardImpact, new Vector3(0f, 0f, 2f), 2.3f, EnemyStimulusTier.Loud);
                turret.Tick(0.05f, 2.35f);
                Assert.AreEqual(EnemyAwarenessState.Engaged, turret.AwarenessState);
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, turret.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TerritorialEnemyWarnsBeforeLoudDisturbanceCommits()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var rat = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyRat"));
                rat.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 3f);

                Assert.AreEqual(EnemyAwarenessState.Suspicious, rat.AwarenessState);
                rat.ReceiveStimulus(EnemyStimulusKind.RangedAttack, player.transform.localPosition, 1f, EnemyStimulusTier.Normal);
                Assert.AreEqual(EnemyAwarenessState.Alerted, rat.AwarenessState);
                StringAssert.Contains("territorial", rat.LastAwarenessReason);

                rat.ReceiveStimulus(EnemyStimulusKind.MeleeAttack, player.transform.localPosition, 1.4f, EnemyStimulusTier.Loud);
                Assert.AreEqual(EnemyAwarenessState.Engaged, rat.AwarenessState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MindlessEnemiesPressureSimplyFromNormalDisturbance()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var heavy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyHeavy"));
                heavy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 3f);

                Assert.AreEqual(EnemyAwarenessState.Alerted, heavy.AwarenessState);
                heavy.ReceiveStimulus(EnemyStimulusKind.Roll, player.transform.localPosition, 1f, EnemyStimulusTier.Normal);
                Assert.AreEqual(EnemyAwarenessState.Engaged, heavy.AwarenessState);
                StringAssert.Contains("mindless", heavy.LastAwarenessReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BumpDisturbanceIsHarmlessDiagnosticAndSeparatesBodies()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var prey = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                prey.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.2f);

                Assert.IsFalse(prey.TryApplyContactDamage(4f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);
                Assert.AreEqual(EnemyStimulusKind.Bump, prey.LastStimulusKind);
                Assert.AreEqual(EnemyStimulusTier.Normal, prey.LastStimulusTier);
                Assert.Greater(prey.CurrentDisturbanceScore, 0f);
                Assert.Less(prey.transform.localPosition.z, 0f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DebugDiagnosticsUpdateWithoutPlayerFacingUi()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var spider = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySpider"));
                spider.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(1f, 0f, 1f);

                spider.ReceiveStimulus(EnemyStimulusKind.Roll, player.transform.localPosition, 7f, EnemyStimulusTier.Normal, "test_roll");

                Assert.AreEqual(EnemyStimulusKind.Roll, spider.LastStimulusKind);
                Assert.AreEqual(EnemyStimulusTier.Normal, spider.LastStimulusTier);
                Assert.AreEqual(7f, spider.LastStimulusTime, 0.001f);
                Assert.AreEqual(player.transform.localPosition, spider.LastStimulusLocalPosition);
                Assert.IsFalse(string.IsNullOrWhiteSpace(spider.LastAwarenessReason));
                Assert.Greater(spider.CurrentDisturbanceScore, 0f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone83AssetGenerator.DocsPath), Milestone83AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone83AssetGenerator.ReportPath), Milestone83AssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone83AssetGenerator.DocsPath);
            StringAssert.Contains("Noise + Disturbance", markdown);
            StringAssert.Contains("Quiet", markdown);
            StringAssert.Contains("Loud", markdown);
            StringAssert.Contains("Bump", markdown);
            StringAssert.Contains("territorial", markdown);
            Assert.IsTrue(Milestone83Validator.Validate());
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M83Harness");
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
