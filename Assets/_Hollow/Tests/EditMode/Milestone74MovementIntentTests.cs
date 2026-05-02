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
    public sealed class Milestone74MovementIntentTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void RuntimeCatalogCarriesM74PreferredRanges()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();

            AssertRange(catalog, "spawnEnemyNormal", 1.05f, 1.75f);
            AssertRange(catalog, "spawnEnemyFlying", 2.75f, 4.25f);
            AssertRange(catalog, "spawnEnemyFast", 0.9f, 1.45f);
            AssertRange(catalog, "spawnEnemyHeavy", 1.35f, 2.15f);
            AssertRange(catalog, "spawnEnemyCharger", 0.8f, 1.35f);
            AssertRange(catalog, "spawnEnemyTurret", 5.25f, 7.5f);
            AssertRange(catalog, "spawnEnemySplitter", 1.25f, 2f);
            AssertRange(catalog, "spawnEnemyBoss", 4.5f, 6.5f);
        }

        [Test]
        public void PreferredRangesPropagateToRuntimeEnemy()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFast"));

                Assert.AreEqual(0.9f, enemy.PreferredRangeMinMeters, 0.001f);
                Assert.AreEqual(1.45f, enemy.PreferredRangeMaxMeters, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FlyingChaserRetreatBurstCapsAndReassesses()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 2f);
                var prey = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                prey.ConfigureSpawnContext(null, null, null, null, null, 0);
                prey.transform.localPosition = Vector3.zero;

                prey.Tick(0.2f, 10f);
                Assert.Less(prey.transform.localPosition.z, 0f);

                var burstEndPosition = prey.transform.localPosition.z;
                prey.Tick(0.2f, 10.85f);
                Assert.Greater(prey.transform.localPosition.z, burstEndPosition);

                var reassessedPosition = prey.transform.localPosition.z;
                prey.Tick(0.2f, 11.2f);
                Assert.Less(prey.transform.localPosition.z, reassessedPosition);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ContactBufferStopsDirectShoveIntoPlayer()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0.5f, 0f, -3f);
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                enemy.transform.localPosition = new Vector3(0f, 0f, -3f);

                enemy.Tick(0.2f, 4f);

                Assert.Less(enemy.transform.localPosition.x, 0f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SoftSeparationNudgesNearbyEnemiesApart()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 4f);
                var combat = root.AddComponent<RoomCombatController>();
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var left = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyNormal"));
                var right = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyNormal"));
                left.transform.localPosition = new Vector3(-0.1f, 0f, 0f);
                right.transform.localPosition = new Vector3(0.1f, 0f, 0f);
                RegisterEnemies(combat, left, right);

                left.Tick(0.2f, 5f);
                right.Tick(0.2f, 5f);

                Assert.Less(left.transform.localPosition.x, -0.1f);
                Assert.Greater(right.transform.localPosition.x, 0.1f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BoneTurretRangeDataDoesNotMakeItMove()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 3f);
                var turret = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret"));
                turret.transform.localPosition = Vector3.zero;

                turret.Tick(0.2f, 6f);

                Assert.AreEqual(Vector3.zero, turret.transform.localPosition);
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, turret.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BasicSplitterMaintainsPreferredBand()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var splitter = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySplitter"));

                player.transform.localPosition = new Vector3(1.8f, 0f, -3f);
                splitter.transform.localPosition = new Vector3(0f, 0f, -3f);
                splitter.Tick(0.2f, 7f);
                Assert.AreEqual(0f, splitter.transform.localPosition.x, 0.001f);

                player.transform.localPosition = new Vector3(3f, 0f, -3f);
                splitter.transform.localPosition = new Vector3(0f, 0f, -3f);
                splitter.Tick(0.2f, 7.3f);
                Assert.Greater(splitter.transform.localPosition.x, 0f);

                player.transform.localPosition = new Vector3(0.8f, 0f, -3f);
                splitter.transform.localPosition = new Vector3(0f, 0f, -3f);
                splitter.Tick(0.2f, 7.6f);
                Assert.Less(splitter.transform.localPosition.x, 0f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AshChargerStillStartsChargeWindupDirectly()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 3f);
                var charger = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyCharger"));
                charger.transform.localPosition = Vector3.zero;

                charger.Tick(0.1f, 4f);

                Assert.AreEqual(EnemyReadabilityState.ChargeWindup, charger.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CatalogueFilesExistPdfExtractsAndValidatorPasses()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_M74_Movement_Intent_V2.md"));
            Assert.IsTrue(File.Exists("output/pdf/Hollow_M74_Movement_Intent_V2.pdf"));
            var markdown = File.ReadAllText("Docs/Hollow_M74_Movement_Intent_V2.md");
            StringAssert.Contains("Movement Intent V2", markdown);
            StringAssert.Contains("preferred range", markdown);
            StringAssert.Contains("Flying Chaser", markdown);
            StringAssert.Contains("Bone Turret", markdown);
            StringAssert.Contains("separation", markdown);
            StringAssert.Contains("Current Roster Range Table", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone74Validator.Validate());
        }

        private static void AssertRange(EnemyCatalog catalog, string spawnKind, float min, float max)
        {
            var enemy = catalog.Resolve(spawnKind);
            Assert.NotNull(enemy, spawnKind);
            Assert.AreEqual(min, enemy.PreferredRangeMinMeters, 0.001f, spawnKind);
            Assert.AreEqual(max, enemy.PreferredRangeMaxMeters, 0.001f, spawnKind);
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath("tools/verify_m74_movement_intent_pdf.py");
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
                Assert.Fail("Timed out while verifying the M74 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player, out GameObject enemyPrefab)
        {
            var root = new GameObject("M74Harness");
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
            enemyPrefab.name = "EnemyBase_M74TestPrefab";
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
