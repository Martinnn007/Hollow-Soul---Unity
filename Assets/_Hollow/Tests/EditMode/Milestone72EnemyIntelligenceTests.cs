using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone72EnemyIntelligenceTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void RuntimeCatalogCarriesM72EnemyDefaults()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();

            AssertEnemy(catalog, "spawnEnemyNormal", EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Predator);
            AssertEnemy(catalog, "spawnEnemyFlying", EnemyIntelligenceLevel.Instinctive, EnemyInstinctDisposition.Prey);
            AssertEnemy(catalog, "spawnEnemyFast", EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Predator);
            AssertEnemy(catalog, "spawnEnemyHeavy", EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Mindless);
            AssertEnemy(catalog, "spawnEnemyCharger", EnemyIntelligenceLevel.Instinctive, EnemyInstinctDisposition.Predator);
            AssertEnemy(catalog, "spawnEnemyTurret", EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel);
            AssertEnemy(catalog, "spawnEnemySplitter", EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Predator);
            AssertEnemy(catalog, "spawnEnemyBoss", EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Sentinel);
        }

        [Test]
        public void RuntimeBossRosterCarriesSignatureIntelligenceMetadata()
        {
            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                Assert.AreEqual(BossDefinition.SignatureIntelligenceFor(boss.BehaviorId), boss.Intelligence, boss.BossId);
            }
        }

        [Test]
        public void EncounterSaveSnapshotsAndRestoresResolvedIntelligence()
        {
            var plan = new EncounterPlan(new[]
            {
                new RoomEncounterAssignment("room_01", "m72_mix", new[] { "spawnEnemyFlying", "spawnEnemyTurret" })
            });

            var save = plan.ToSaveState(EnemyCatalog.CreateRuntimeDefault()).Single();
            CollectionAssert.AreEqual(new[] { 0, 3 }, save.enemyIntelligenceLevels);
            CollectionAssert.AreEqual(new[] { "prey", "sentinel" }, save.enemyDispositions);

            var restored = EncounterPlan.FromSaveState(new[] { save });
            Assert.IsTrue(restored.TryResolve("room_01", out var assignment));
            CollectionAssert.AreEqual(new[] { 0, 3 }, assignment.EnemyIntelligenceLevels);
            CollectionAssert.AreEqual(new[] { "prey", "sentinel" }, assignment.EnemyDispositions);

            var legacy = EncounterPlan.FromSaveState(new[]
            {
                new RoomEncounterSaveState
                {
                    roomId = "legacy_room",
                    encounterId = "legacy",
                    enemySpawnKinds = new List<string> { "spawnEnemyFlying" }
                }
            }).ToSaveState(EnemyCatalog.CreateRuntimeDefault()).Single();
            CollectionAssert.AreEqual(new[] { 0 }, legacy.enemyIntelligenceLevels);
            CollectionAssert.AreEqual(new[] { "prey" }, legacy.enemyDispositions);
        }

        [Test]
        public void EncounterSaveCanUseLiveRuntimeIntelligenceSnapshot()
        {
            var plan = new EncounterPlan(new[]
            {
                new RoomEncounterAssignment("room_01", "m72_live", new[] { "spawnEnemyNormal" }),
                new RoomEncounterAssignment("room_02", "m72_catalog", new[] { "spawnEnemyFlying" })
            });

            var save = plan.ToSaveState(
                EnemyCatalog.CreateRuntimeDefault(),
                "room_01",
                new[] { 5 },
                new[] { "mindless" });
            var liveRoom = save.Single(state => state.roomId == "room_01");
            var catalogRoom = save.Single(state => state.roomId == "room_02");

            CollectionAssert.AreEqual(new[] { 5 }, liveRoom.enemyIntelligenceLevels);
            CollectionAssert.AreEqual(new[] { "mindless" }, liveRoom.enemyDispositions);
            CollectionAssert.AreEqual(new[] { 0 }, catalogRoom.enemyIntelligenceLevels);
            CollectionAssert.AreEqual(new[] { "prey" }, catalogRoom.enemyDispositions);
        }

        [Test]
        public void SpawnContextOverridesRuntimeEnemyIntelligenceByIndex()
        {
            var root = CreateHarness(out var room, out var player, out var enemyPrefab);
            try
            {
                var context = new RoomCombatEncounterContext(
                    "m72_override",
                    new[] { "spawnEnemyNormal" },
                    1,
                    0,
                    0,
                    string.Empty,
                    string.Empty,
                    0,
                    string.Empty,
                    new[] { 5 },
                    new[] { "mindless" });

                var result = EnemySpawnService.SpawnEnemies(new EnemySpawnRequest(
                    room,
                    root.transform,
                    enemyPrefab,
                    null,
                    player,
                    EnemyCatalog.CreateRuntimeDefault(),
                    DifficultyTierDefinition.CreateRuntimeDeveloperSample(),
                    new CombatDiagnosticsModel(),
                    context));

                Assert.AreEqual(1, result.Enemies.Count);
                Assert.AreEqual(EnemyIntelligenceLevel.Cunning, result.Enemies[0].Intelligence);
                Assert.AreEqual(EnemyInstinctDisposition.Mindless, result.Enemies[0].Disposition);
                Assert.AreEqual(0, result.Enemies[0].SpawnIndex);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InstinctivePreyBacksAwayUntilRecentlyDamaged()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 2f);
                var prey = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                prey.transform.localPosition = Vector3.zero;

                prey.Tick(0.2f, 10f);
                Assert.Less(prey.transform.localPosition.z, 0f);

                prey.Tick(0.01f, 10.2f);
                DamageSystem.ApplyDamage(prey.Health, new DamageRequest(1, root));
                var before = prey.transform.localPosition.z;
                prey.Tick(0.2f, 10.3f);
                Assert.Greater(prey.transform.localPosition.z, before);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InstinctivePreyContactDamageRequiresEndangered()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 0.2f);
                var playerHealth = player.GetComponent<CombatantHealth>();
                var prey = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyFlying"));
                prey.transform.localPosition = Vector3.zero;

                Assert.IsFalse(prey.TryApplyContactDamage(12f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);

                prey.Tick(0.01f, 12.1f);
                DamageSystem.ApplyDamage(prey.Health, new DamageRequest(1, root));

                Assert.IsTrue(prey.TryApplyContactDamage(12.2f));
                Assert.Less(playerHealth.CurrentHealth, RoomCombatController.PlayerMaxHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InstinctivePredatorChargerAttacksDirectly()
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
        public void SentinelHoldsUntilPlayerApproaches()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var turret = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyTurret"));
                turret.transform.localPosition = Vector3.zero;

                player.transform.localPosition = new Vector3(0f, 0f, 7f);
                turret.Tick(0.1f, 3f);
                Assert.AreEqual(EnemyReadabilityState.Idle, turret.ReadabilityState);

                player.transform.localPosition = new Vector3(0f, 0f, 3f);
                turret.Tick(0.1f, 3.2f);
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, turret.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AttackBudgetPrefersHigherIntelligenceWithoutExtraPressure()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 3f);
                var combat = root.AddComponent<RoomCombatController>();
                var low = CreateEnemy(root.transform, room, player, CreateTurretDefinition("low_turret", EnemyIntelligenceLevel.Simple));
                var high = CreateEnemy(root.transform, room, player, CreateTurretDefinition("high_turret", EnemyIntelligenceLevel.Cunning));
                low.transform.localPosition = Vector3.zero;
                high.transform.localPosition = Vector3.zero;
                RegisterBudgetEnemies(combat, low, high);

                Assert.Greater(high.AttackPriorityScore(5f), low.AttackPriorityScore(5f));
                Assert.IsFalse(combat.TryReserveEnemyAttack(low, 5f));
                Assert.IsTrue(combat.TryReserveEnemyAttack(high, 5f));
                Assert.IsFalse(combat.TryReserveEnemyAttack(high, 5.1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CatalogueFilesExistAndValidatorPasses()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_M72_Enemy_Intelligence_Catalogue.md"));
            Assert.IsTrue(File.Exists("output/pdf/Hollow_M72_Enemy_Intelligence_Catalogue.pdf"));
            var markdown = File.ReadAllText("Docs/Hollow_M72_Enemy_Intelligence_Catalogue.md");
            StringAssert.Contains("Instinctive", markdown);
            StringAssert.Contains("Cunning", markdown);
            StringAssert.Contains("prey", markdown);
            StringAssert.Contains("predator", markdown);
            StringAssert.Contains("Current Base Enemy Table", markdown);
            StringAssert.Contains("Current Boss Metadata Table", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone72Validator.Validate());
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath("tools/verify_m72_enemy_intelligence_pdf.py");
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
                Assert.Fail("Timed out while verifying the M72 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }

        private static void AssertEnemy(
            EnemyCatalog catalog,
            string spawnKind,
            EnemyIntelligenceLevel intelligence,
            EnemyInstinctDisposition disposition)
        {
            var enemy = catalog.Resolve(spawnKind);
            Assert.NotNull(enemy, spawnKind);
            Assert.AreEqual(intelligence, enemy.Intelligence, spawnKind);
            Assert.AreEqual(disposition, enemy.Disposition, spawnKind);
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player, out GameObject enemyPrefab)
        {
            var root = new GameObject("M72Harness");
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
            enemyPrefab.name = "EnemyBase_M72TestPrefab";
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

        private static EnemyDefinition CreateTurretDefinition(string spawnKind, EnemyIntelligenceLevel intelligence)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.Configure(
                spawnKind,
                spawnKind,
                EnemyArchetypeId.Heavy,
                EnemyBehaviorId.TurretShooter,
                EnemyMovementMode.Grounded,
                4,
                0f,
                1,
                1f,
                0.34f,
                8f,
                1.35f,
                1,
                4.8f,
                0f,
                2f,
                "spawnEnemyNormal",
                0,
                EnemyBodyClass.Heavy,
                intelligence,
                EnemyInstinctDisposition.Sentinel,
                Color.white);
            return definition;
        }

        private static void RegisterBudgetEnemies(RoomCombatController combat, params EnemyRuntimeController[] enemies)
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
