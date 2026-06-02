using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone77CritterRosterTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void RuntimeCatalogCarriesM77CritterDefaults()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();

            AssertCritter(catalog.Resolve("spawnEnemySpittingPod"), EnemyBehaviorId.SpittingPod, 10, 0f, 0.44f, EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Sentinel, 0f, 0f, 9f, 5.5f, 8f);
            AssertCritter(catalog.Resolve("spawnEnemyRat"), EnemyBehaviorId.Rat, 3, 2.65f, 0.2f, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Territorial, 8f, 260f, 7.5f, 1.2f, 2.2f);
            AssertCritter(catalog.Resolve("spawnEnemySpider"), EnemyBehaviorId.Spider, 2, 2.9f, 0.22f, EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Prey, 8.5f, 300f, 8f, 1f, 1.9f);

            CollectionAssert.Contains(catalog.Resolve("spawnEnemySpittingPod").AttackProfiles.Select(profile => profile.AttackId).ToArray(), "spit_lob");
            CollectionAssert.Contains(catalog.Resolve("spawnEnemyRat").AttackProfiles.Select(profile => profile.AttackId).ToArray(), "rat_bite");
            CollectionAssert.Contains(catalog.Resolve("spawnEnemySpider").AttackProfiles.Select(profile => profile.AttackId).ToArray(), "startle_hop");
        }

        [Test]
        public void SpittingPodIsStationaryBlindHearingDrivenAndFiresBallisticSplash()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                player.transform.localPosition = new Vector3(5.8f, 0f, 0f);
                var pod = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySpittingPod"));
                pod.transform.localPosition = Vector3.zero;

                pod.Tick(0.1f, 1f);
                Assert.AreEqual(EnemyReadabilityState.Idle, pod.ReadabilityState);
                Assert.AreEqual(Vector3.zero, pod.transform.localPosition);

                pod.ReceiveStimulus(EnemyStimulusKind.Footstep, player.transform.localPosition, 1.1f);
                pod.Tick(0.05f, 1.12f);
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, pod.ReadabilityState);

                pod.Tick(0.3f, 1.4f);
                var projectile = Object.FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).FirstOrDefault();
                Assert.NotNull(projectile);
                Assert.IsTrue(projectile.IsBallistic);
                Assert.AreEqual(player.transform.localPosition.x, projectile.BallisticTargetLocalPosition.x, 0.05f);

                projectile.Tick(1f);
                Assert.Less(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SpittingPodStillLobsWhenCrowdTacticsWouldOtherwiseHoldStaticEnemies()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                player.transform.localPosition = Vector3.zero;
                var enemies = new List<EnemyRuntimeController>();
                for (var index = 0; index < 11; index++)
                {
                    var blocker = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyFast"));
                    blocker.transform.localPosition = new Vector3(0.6f + index * 0.08f, 0f, 0.9f + index * 0.04f);
                    blocker.BindRoomCombatController(combat);
                    enemies.Add(blocker);
                    AddEnemyToCombat(combat, blocker);
                }

                var pod = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemySpittingPod"));
                pod.transform.localPosition = new Vector3(5.8f, 0f, 0f);
                pod.BindRoomCombatController(combat);
                enemies.Add(pod);
                AddEnemyToCombat(combat, pod);
                pod.ReceiveStimulus(EnemyStimulusKind.Footstep, player.transform.localPosition, 2f);

                combat.TacticalDirector.Tick(enemies, room, player, 2.05f);
                pod.Tick(0.05f, 2.1f);

                Assert.IsTrue(pod.IsRootedStaticEnemy);
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, pod.ReadabilityState);
                Assert.AreEqual("StartRangedAction", pod.LastBehaviorCommand);
                Assert.AreEqual("spit_lob", pod.LastBehaviorReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RatDelaysFirstBiteThenRetreatsAfterDamage()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 0.9f);
                var rat = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyRat"));
                rat.transform.localPosition = Vector3.zero;

                rat.Tick(0.05f, 5f);
                Assert.AreEqual(EnemyAwarenessState.Engaged, rat.AwarenessState);
                Assert.AreNotEqual(EnemyReadabilityState.MeleeWindup, rat.ReadabilityState);

                rat.Tick(0.05f, 5.5f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, rat.ReadabilityState);

                rat.Tick(0.2f, 5.9f);
                rat.Health.ApplyDamage(new DamageRequest(1, player.gameObject));
                var beforeRetreat = rat.transform.localPosition.z;
                rat.Tick(0.2f, 6.05f);
                Assert.Less(rat.transform.localPosition.z, beforeRetreat);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SpiderStartlesAwayThenCanHopWhenDisturbed()
        {
            var root = CreateHarness(out var room, out var player, out _);
            try
            {
                player.transform.localPosition = new Vector3(0f, 0f, 1.4f);
                var spider = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySpider"));
                spider.transform.localPosition = Vector3.zero;

                spider.Tick(0.2f, 4.1f);
                Assert.AreEqual(EnemyAwarenessState.Alerted, spider.AwarenessState);
                Assert.Less(spider.transform.localPosition.z, 0f);

                spider.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.05f);
                spider.ReceiveStimulus(EnemyStimulusKind.Damage, player.transform.localPosition, 10f);
                spider.Tick(0.05f, 10f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, spider.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EncounterRotationAndShowcaseRoomsIncludeM77Critters()
        {
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone48AssetGenerator.EncounterCatalogPath);
            Assert.NotNull(catalog);
            foreach (var encounterId in Milestone77AssetGenerator.EncounterIds)
            {
                Assert.IsTrue(catalog.Encounters.Any(encounter => encounter != null && encounter.EncounterId == encounterId), encounterId);
            }

            foreach (var roomId in Milestone77AssetGenerator.ShowcaseRoomIds)
            {
                var path = $"{Milestone77AssetGenerator.ShowcaseRoomDirectory}/{roomId}.hollowruntime.json";
                Assert.IsTrue(File.Exists(path), path);
                var json = File.ReadAllText(path);
                StringAssert.Contains("spawnEnemy", json);
            }
        }

        [Test]
        public void BespokeCritterRoomsHaveRequestedCompositionsAndAreCataloged()
        {
            AssertRoomComposition("m77_spider_brood_den_wide", RoomDesignerMarkerKinds.EnemySpider, 8, RoomDesignerMarkerKinds.EnemySpittingPod, 0, 0);
            AssertRoomComposition("m77_rat_warren_single", RoomDesignerMarkerKinds.EnemyRat, 5, RoomDesignerMarkerKinds.EnemySpittingPod, 0, 0);
            AssertRoomComposition("m77_rocky_spider_pod_wide", RoomDesignerMarkerKinds.EnemySpider, 6, RoomDesignerMarkerKinds.EnemySpittingPod, 1, 10);
            AssertRoomComposition("m77_rocky_rat_pod_wide", RoomDesignerMarkerKinds.EnemyRat, 5, RoomDesignerMarkerKinds.EnemySpittingPod, 1, 10);

            var roomCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            Assert.NotNull(roomCatalog);
            var importReport = ApprovedDesignerRoomImporter.ImportApprovedRooms(roomCatalog.AdditionalTemplates);
            Assert.IsFalse(importReport.HasErrors, string.Join("; ", importReport.Errors));
            foreach (var roomId in Milestone77AssetGenerator.CuratedEncounterRoomIds)
            {
                Assert.IsTrue(importReport.ValidRooms.Any(room => room.Id == roomId), roomId);
            }
        }

        [Test]
        public void CatalogueFilesExistPdfExtractsAndValidatorPasses()
        {
            Assert.IsTrue(File.Exists(Milestone77AssetGenerator.DocsPath));
            Assert.IsTrue(File.Exists(Milestone77AssetGenerator.PdfPath));
            var markdown = File.ReadAllText(Milestone77AssetGenerator.DocsPath);
            StringAssert.Contains("Spitting Pod", markdown);
            StringAssert.Contains("Rat", markdown);
            StringAssert.Contains("Spider", markdown);
            StringAssert.Contains("territorial", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone77Validator.Validate());
        }

        private static void AssertCritter(
            EnemyDefinition enemy,
            EnemyBehaviorId behavior,
            int health,
            float speed,
            float radius,
            EnemyIntelligenceLevel intelligence,
            EnemyInstinctDisposition disposition,
            float sight,
            float angle,
            float hearing,
            float preferredMin,
            float preferredMax)
        {
            Assert.NotNull(enemy);
            Assert.AreEqual(behavior, enemy.BehaviorId);
            Assert.AreEqual(health, enemy.MaxHealth);
            Assert.AreEqual(speed, enemy.SpeedMetersPerSecond, 0.001f);
            Assert.AreEqual(radius, enemy.RadiusMeters, 0.001f);
            Assert.AreEqual(intelligence, enemy.Intelligence);
            Assert.AreEqual(disposition, enemy.Disposition);
            Assert.AreEqual(sight, enemy.SightRadiusMeters, 0.001f);
            Assert.AreEqual(angle, enemy.SightAngleDegrees, 0.001f);
            Assert.AreEqual(hearing, enemy.HearingRadiusMeters, 0.001f);
            Assert.AreEqual(preferredMin, enemy.PreferredRangeMinMeters, 0.001f);
            Assert.AreEqual(preferredMax, enemy.PreferredRangeMaxMeters, 0.001f);
        }

        private static void AssertRoomComposition(
            string roomId,
            string primaryEnemyKind,
            int expectedPrimaryCount,
            string secondaryEnemyKind,
            int expectedSecondaryCount,
            int expectedMinimumRockCount)
        {
            var asset = LoadM77Room(roomId);
            Assert.AreEqual(expectedPrimaryCount, asset.EnemySpawns.Count(spawn => spawn.kind == primaryEnemyKind), roomId);
            Assert.AreEqual(expectedSecondaryCount, asset.EnemySpawns.Count(spawn => spawn.kind == secondaryEnemyKind), roomId);
            Assert.GreaterOrEqual(asset.Layout.Obstacles.Count, expectedMinimumRockCount, roomId);
            Assert.Greater(asset.Layout.WalkableTiles.Count, 0, roomId);
            Assert.Greater(asset.DoorPorts.Count, 0, roomId);
            Assert.NotNull(asset.SafeStart?.position, roomId);
            Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.RoomReward), roomId);
        }

        private static ImportedRoomRuntimeAsset LoadM77Room(string roomId)
        {
            var path = $"{Milestone77AssetGenerator.ShowcaseRoomDirectory}/{roomId}.hollowruntime.json";
            Assert.IsTrue(File.Exists(path), path);
            return HollowRuntimeV2Importer.Import(File.ReadAllText(path));
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath("tools/verify_m77_critter_roster_pdf.py");
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
                Assert.Fail("Timed out while verifying the M77 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player, out GameObject enemyPrefab)
        {
            var root = new GameObject("M77Harness");
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
            enemyPrefab.name = "EnemyBase_M77TestPrefab";
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

        private static void AddEnemyToCombat(RoomCombatController combat, EnemyRuntimeController enemy)
        {
            var field = typeof(RoomCombatController).GetField("enemies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var enemies = (List<EnemyRuntimeController>)field.GetValue(combat);
            enemies.Add(enemy);
        }
    }
}
