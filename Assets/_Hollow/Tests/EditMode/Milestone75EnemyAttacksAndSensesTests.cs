using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Hollow.Combat;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone75EnemyAttacksAndSensesTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void RuntimeCatalogCarriesM75SenseAndLungeDefaults()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();

            AssertEnemyM75(catalog, "spawnEnemyNormal", 6.5f, 150f, 4.5f, true, 1.4f);
            AssertEnemyM75(catalog, "spawnEnemyFlying", 7.5f, 240f, 6.5f, true, 1.35f);
            AssertEnemyM75(catalog, "spawnEnemyFast", 7f, 170f, 5f, true, 1.25f);
            AssertEnemyM75(catalog, "spawnEnemyHeavy", 5f, 110f, 3.5f, true, 1.7f);
            AssertEnemyM75(catalog, "spawnEnemyCharger", 7f, 120f, 5f, false, 1.4f);
            AssertEnemyM75(catalog, "spawnEnemyTurret", 9.5f, 70f, 2.5f, false, 1.4f);
            AssertEnemyM75(catalog, "spawnEnemySplitter", 6.5f, 160f, 5f, true, 1.6f);

            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                var senses = BossDefinition.SignatureSensesFor(boss.BehaviorId);
                Assert.AreEqual(senses.x, boss.SightRadiusMeters, 0.001f, boss.BossId);
                Assert.AreEqual(senses.y, boss.SightAngleDegrees, 0.001f, boss.BossId);
                Assert.AreEqual(senses.z, boss.HearingRadiusMeters, 0.001f, boss.BossId);
            }
        }

        [Test]
        public void NormalChaserLungesFromPreferredBandEdgeAndDamagesPlayer()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                player.transform.localPosition = new Vector3(0f, 0f, 1.35f);
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                enemy.transform.localPosition = Vector3.zero;

                enemy.Tick(0.05f, 5f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, enemy.ReadabilityState);

                enemy.Tick(0.05f, 5.23f);
                Assert.AreEqual(EnemyReadabilityState.MeleeLunge, enemy.ReadabilityState);

                enemy.Tick(0.18f, 5.35f);
                Assert.Less(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ContactEnemiesCanStartLungeFromTheirBandEdges()
        {
            var cases = new[]
            {
                ("spawnEnemyFast", 1.25f),
                ("spawnEnemyHeavy", 1.7f),
                ("spawnEnemySplitter", 1.6f)
            };

            foreach (var testCase in cases)
            {
                var root = CreateHarness(out var room, out var player, out _);
                try
                {
                    player.transform.localPosition = new Vector3(testCase.Item2, 0f, -3f);
                    var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve(testCase.Item1));
                    enemy.transform.localPosition = new Vector3(0f, 0f, -3f);

                    enemy.Tick(0.05f, 7f);

                    Assert.AreEqual(EnemyReadabilityState.MeleeWindup, enemy.ReadabilityState, testCase.Item1);
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void FlyingChaserStartsUnawareAndPanicLungesOnlyWhenEndangered()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 1.2f);
                var prey = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                prey.transform.localPosition = Vector3.zero;

                Assert.AreEqual(EnemyAwarenessState.Unaware, prey.AwarenessState);
                prey.Tick(0.1f, 5f);
                Assert.AreNotEqual(EnemyReadabilityState.MeleeWindup, prey.ReadabilityState);

                DamageSystem.ApplyDamage(prey.Health, new DamageRequest(1, player.gameObject));
                prey.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.2f);
                prey.Tick(0.05f, 5.1f);

                Assert.AreEqual(EnemyAwarenessState.Engaged, prey.AwarenessState);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, prey.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SightHearingStimuliAndEngagedPersistenceWork()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var prey = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                prey.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, -3f);

                prey.Tick(0.1f, 3f);
                Assert.AreEqual(EnemyAwarenessState.Unaware, prey.AwarenessState);

                prey.ReceiveStimulus(EnemyStimulusKind.Footstep, player.transform.localPosition, 3.1f);
                Assert.AreEqual(EnemyAwarenessState.Suspicious, prey.AwarenessState);
                prey.ReceiveStimulus(EnemyStimulusKind.Footstep, player.transform.localPosition, 3.6f);
                Assert.AreEqual(EnemyAwarenessState.Alerted, prey.AwarenessState);

                prey.ReceiveStimulus(EnemyStimulusKind.RangedAttack, player.transform.localPosition, 4f);
                Assert.AreEqual(EnemyAwarenessState.Engaged, prey.AwarenessState);

                player.transform.localPosition = new Vector3(0f, 0f, -12f);
                prey.Tick(0.2f, 4.2f);
                Assert.AreEqual(EnemyAwarenessState.Engaged, prey.AwarenessState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BlindAndDeafZeroValuesDisableLocalSenses()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var definition = EnemyDefinition.CreateRuntime(
                    "spawnBlindTest",
                    "Blind Test",
                    EnemyArchetypeId.Flying,
                    EnemyMovementMode.Flying,
                    3,
                    1.8f,
                    1,
                    EnemyIntelligenceLevel.Instinctive,
                    EnemyInstinctDisposition.Prey,
                    Color.white);
                definition.ConfigureSenseAndLunge(0f, 0f, 0f, true, 1.35f, 0.22f, 0.18f, 0.75f, 1.15f);

                var enemy = CreateEnemy(root.transform, room, player, definition);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1f);

                enemy.Tick(0.1f, 2f);
                enemy.ReceiveStimulus(EnemyStimulusKind.Footstep, player.transform.localPosition, 2.1f);

                Assert.AreEqual(EnemyAwarenessState.Unaware, enemy.AwarenessState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BoneTurretAndAshChargerKeepExistingAttackRoles()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                player.transform.localPosition = new Vector3(0f, 0f, 3f);
                var turret = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyTurret"));
                turret.transform.localPosition = Vector3.zero;
                turret.Tick(0.1f, 6f);
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, turret.ReadabilityState);
                Assert.AreEqual(Vector3.zero, turret.transform.localPosition);

                var charger = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyCharger"));
                charger.transform.localPosition = Vector3.zero;
                charger.Tick(0.1f, 8f);
                Assert.AreEqual(EnemyReadabilityState.ChargeWindup, charger.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MeleeLungeBudgetLimitsConcurrentStarts()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal");
                player.transform.localPosition = new Vector3(0f, 0f, 1.25f);
                var first = CreateEnemy(root.transform, room, player, definition);
                var second = CreateEnemy(root.transform, room, player, definition);
                first.transform.localPosition = new Vector3(-0.1f, 0f, 0f);
                second.transform.localPosition = new Vector3(0.1f, 0f, 0f);
                RegisterEnemies(combat, first, second);

                first.Tick(0.05f, 10f);
                second.Tick(0.05f, 10f);

                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, first.ReadabilityState);
                Assert.AreNotEqual(EnemyReadabilityState.MeleeWindup, second.ReadabilityState);

                second.Tick(0.05f, 10.31f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, second.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CatalogueFilesExistPdfExtractsAndValidatorPasses()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_M75_Enemy_Attacks_And_Senses.md"));
            Assert.IsTrue(File.Exists("output/pdf/Hollow_M75_Enemy_Attacks_And_Senses.pdf"));
            var markdown = File.ReadAllText("Docs/Hollow_M75_Enemy_Attacks_And_Senses.md");
            StringAssert.Contains("Enemy Attacks", markdown);
            StringAssert.Contains("Senses", markdown);
            StringAssert.Contains("sight", markdown);
            StringAssert.Contains("hearing", markdown);
            StringAssert.Contains("lunge", markdown);
            StringAssert.Contains("Flying Chaser", markdown);
            StringAssert.Contains("Bone Turret", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone75Validator.Validate());
        }

        private static void AssertEnemyM75(
            EnemyCatalog catalog,
            string spawnKind,
            float sight,
            float angle,
            float hearing,
            bool lungeEnabled,
            float triggerRange)
        {
            var enemy = catalog.Resolve(spawnKind);
            Assert.NotNull(enemy, spawnKind);
            Assert.AreEqual(sight, enemy.SightRadiusMeters, 0.001f, spawnKind);
            Assert.AreEqual(angle, enemy.SightAngleDegrees, 0.001f, spawnKind);
            Assert.AreEqual(hearing, enemy.HearingRadiusMeters, 0.001f, spawnKind);
            Assert.AreEqual(lungeEnabled, enemy.LungeAttackEnabled, spawnKind);
            Assert.AreEqual(triggerRange, enemy.LungeTriggerRangeMeters, 0.001f, spawnKind);
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath("tools/verify_m75_enemy_attacks_and_senses_pdf.py");
            Assert.IsTrue(File.Exists(scriptPath), scriptPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            Assert.NotNull(process);
            if (!process.WaitForExit(15000))
            {
                process.Kill();
                Assert.Fail("Timed out while verifying the M75 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player, out GameObject enemyPrefab)
        {
            var root = new GameObject("M75Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);

            enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyPrefab.name = "EnemyBase_M75TestPrefab";
            enemyPrefab.transform.SetParent(root.transform, false);
            enemyPrefab.SetActive(false);
            enemyPrefab.AddComponent<CombatantHealth>();
            enemyPrefab.AddComponent<EnemyRuntimeController>();
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

        private static void RegisterEnemies(RoomCombatController combat, params EnemyRuntimeController[] enemies)
        {
            var field = typeof(RoomCombatController).GetField("enemies", BindingFlags.NonPublic | BindingFlags.Instance);
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
