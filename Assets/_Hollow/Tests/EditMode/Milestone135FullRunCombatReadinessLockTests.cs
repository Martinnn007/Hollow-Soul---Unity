using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone135FullRunCombatReadinessLockTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void RollTuningMatchesM135GentleForgivenessLock()
        {
            Assert.AreEqual(30f, PlayerWeaponController.RollStaminaCost, 0.0001f);
            Assert.AreEqual(0.55f, PlayerWeaponController.StaminaRegenDelaySeconds, 0.0001f);
            Assert.AreEqual(0.04f, PlayerWeaponController.RollStartupSeconds, 0.0001f);
            Assert.AreEqual(0.26f, PlayerWeaponController.RollInvulnerabilitySeconds, 0.0001f);
            Assert.AreEqual(0.16f, PlayerWeaponController.RollRecoverySeconds, 0.0001f);
            Assert.AreEqual(1.35f, PlayerWeaponController.RollDistanceMeters, 0.0001f);
            Assert.AreEqual(0.46f, PlayerWeaponController.RollDurationSeconds, 0.0001f);
            Assert.AreEqual(0.30f, PlayerWeaponController.RollTravelSeconds, 0.0001f);
            Assert.IsTrue(M135CombatReadinessPolicy.ValidateRollLock(out var detail), detail);
        }

        [Test]
        public void RollInvulnerabilityBlocksDamageOnlyDuringLockedWindow()
        {
            var root = new GameObject("M135RollWindowHarness");
            var source = new GameObject("M135DamageSource");
            source.transform.SetParent(root.transform, false);
            var player = CreateRollPlayer(root.transform);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var feedback = player.GetComponent<PlayerDamageFeedbackController>();
                var request = new DamageRequest(1, source, DamageFeedbackContext.Knockback(Vector3.forward, 1f, 0.1f));

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.zero, 0f));
                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds * 0.5f);
                Assert.AreEqual(PlayerRollPhase.Startup, weapon.CurrentRollPhase);
                Assert.IsFalse(weapon.IsRollInvulnerable);
                Assert.AreEqual(1, feedback.ModifyIncomingDamage(request, 1));

                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds + 0.01f);
                Assert.AreEqual(PlayerRollPhase.InvulnerableTravel, weapon.CurrentRollPhase);
                Assert.IsTrue(weapon.IsRollInvulnerable);
                Assert.AreEqual(0, feedback.ModifyIncomingDamage(request, 1));

                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds + PlayerWeaponController.RollInvulnerabilitySeconds + 0.01f);
                Assert.AreEqual(PlayerRollPhase.Recovery, weapon.CurrentRollPhase);
                Assert.IsFalse(weapon.IsRollInvulnerable);
                Assert.AreEqual(1, feedback.ModifyIncomingDamage(request, 1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FullRunRouteContractValidatesThreeWorldOrderAndBossAssignments()
        {
            var framingCatalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(
                Milestone132BiomeWorldSelectionLockAssetGenerator.RunFramingCatalogPath);
            Assert.IsNotNull(framingCatalog);

            var expectedNames = new[] { "Before Teeth", "The Sunken Cartouche", "The Rust Choir" };
            var expectedBiomes = Milestone132BiomeWorldSelectionLockAssetGenerator.BetaBiomeIds.ToArray();
            for (var worldIndex = 1; worldIndex <= 3; worldIndex++)
            {
                Assert.IsTrue(framingCatalog.TryGetWorld(worldIndex, out var world));
                Assert.AreEqual(expectedNames[worldIndex - 1], world.DisplayName);
                Assert.IsTrue(RoomBiomeIds.Matches(world.BiomeId, expectedBiomes[worldIndex - 1]));
            }

            var content = CreateContent(out var settings);
            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath);
            var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath);
            var bossCatalog = BossCatalogDefinition.CreateRuntimeDefault();
            Assert.IsNotNull(profile);
            Assert.IsNotNull(encounterCatalog);

            for (var worldIndex = 1; worldIndex <= 3; worldIndex++)
            {
                var seed = 13500 + worldIndex;
                var selectedBoss = BossSelectionResolver.Resolve(
                    bossCatalog,
                    seed,
                    seed,
                    worldIndex,
                    "boss_01",
                    BranchGenerator.DirectedEncounterBranchId);
                Assert.IsNotNull(selectedBoss);

                var graph = BranchGenerator.CreateDirectedEncounterBranch(
                    content,
                    settings,
                    profile,
                    worldIndex,
                    seed,
                    selectedBoss.Arena.arenaId,
                    expectedBiomes[worldIndex - 1],
                    enableCorruptedChestLeaf: true,
                    enableWaveRoomLeaf: true,
                    enableSpecialEncounterLeaf: true);

                Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError), topologyError);
                Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Boss));
                Assert.Greater(graph.Rooms.Count(room => room.Role == BranchRoomRole.Combat), 0);
                Assert.GreaterOrEqual(graph.Rooms.Count(room => room.Role == BranchRoomRole.Reward), 1);
                Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Wave));

                var plan = EncounterResolver.CreateDirectedSeededPlan(graph, encounterCatalog, seed, worldIndex, profile, 0, bossCatalog);
                var bossRoom = graph.Rooms.First(room => room.Role == BranchRoomRole.Boss);
                Assert.IsTrue(plan.TryResolve(bossRoom.Id.Value, out var bossAssignment));
                CollectionAssert.Contains(bossAssignment.EnemySpawnKinds.ToArray(), "spawnEnemyBoss");
                Assert.IsFalse(string.IsNullOrWhiteSpace(bossAssignment.BossId));
                Assert.IsTrue(bossCatalog.TryGetBoss(bossAssignment.BossId, out var boss));
                Assert.AreEqual(selectedBoss.BossId, boss.BossId);
                Assert.AreEqual(BossSelectionResolver.BandForWorld(worldIndex), boss.WorldBand);

                Assert.IsTrue(
                    plan.Assignments.Any(assignment =>
                        graph.Rooms.Any(room => room.Id.Value == assignment.RoomId && room.Role == BranchRoomRole.Combat) &&
                        assignment.EnemySpawnKinds.Count > 0),
                    $"World {worldIndex} branch should assign enemies to Combat rooms.");
            }
        }

        [Test]
        public void BetaAndSpecialRoomTemplatesResolveSharedNavMeshBakes()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomNavMeshCatalogDefinition>(
                Milestone135FullRunCombatReadinessLockAssetGenerator.RoomNavMeshCatalogPath);
            Assert.IsNotNull(catalog);

            AssertSharedBake(catalog, "before_teeth_macro_single_1x1", "combat_macro_single_1x1");
            AssertSharedBake(catalog, "before_teeth_macro_wide_2x1", "combat_macro_wide_2x1");
            AssertSharedBake(catalog, "before_teeth_macro_tall_1x2", "combat_macro_tall_1x2");
            AssertSharedBake(catalog, "before_teeth_macro_block_2x2", "combat_macro_block_2x2");
            AssertSharedBake(catalog, "before_teeth_macro_l_3cell", "combat_macro_l_3cell");
            AssertSharedBake(catalog, "sunken_cartouche_macro_single_1x1", "combat_macro_single_1x1");
            AssertSharedBake(catalog, "rust_choir_macro_single_1x1", "combat_macro_single_1x1");
            AssertSharedBake(catalog, "corrupted_chest_single_1x1", "combat_macro_single_1x1");
            AssertSharedBake(catalog, "wave_room_single_1x1", "combat_macro_single_1x1");
            AssertSharedBake(catalog, "special_soul_eater_single_1x1", "combat_macro_single_1x1");
            AssertSharedBake(catalog, "special_escapist_single_1x1", "combat_macro_single_1x1");
        }

        [Test]
        public void AnchorBossesMeetDeepPolishReadabilityContract()
        {
            var anchors = M135CombatReadinessPolicy.DeepPolishBossIds.ToArray();
            var roster = BossCatalogDefinition.CreateRuntimeRoster();
            CollectionAssert.AreEquivalent(new[] { "stone_warden", "cartouche_widow", "choir_of_teeth" }, anchors);

            foreach (var bossId in anchors)
            {
                var boss = roster.FirstOrDefault(candidate => candidate.BossId == bossId);
                Assert.IsNotNull(boss, bossId);
                Assert.IsTrue(M135CombatReadinessPolicy.ValidateMinimumBossReadiness(boss, out var minimum), minimum);
                Assert.IsTrue(M135CombatReadinessPolicy.ValidateAnchorBossPolish(boss, out var detail), detail);
            }
        }

        [Test]
        public void FullBossRosterMeetsMinimumSmokeContract()
        {
            var roster = BossCatalogDefinition.CreateRuntimeRoster();
            Assert.AreEqual(10, roster.Length);
            foreach (var boss in roster)
            {
                Assert.IsTrue(M135CombatReadinessPolicy.ValidateMinimumBossReadiness(boss, out var detail), detail);
                Assert.IsFalse(string.IsNullOrWhiteSpace(boss.BossStatusLabelForTest()), boss.BossId);
            }

            var bossLab = new GameObject("M135BossLab").AddComponent<BossLabController>();
            try
            {
                foreach (var boss in roster)
                {
                    bossLab.Configure(BossCatalogDefinition.CreateRuntimeDefault(), boss.BossId);
                    Assert.AreEqual(boss.BossId, bossLab.SelectedBossId);
                }
            }
            finally
            {
                Object.DestroyImmediate(bossLab.gameObject);
            }
        }

        [Test]
        public void LiveReportPassesAllM135Checks()
        {
            var report = Milestone135FullRunCombatReadinessLockAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.Greater(report.totalChecks, 15);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            CollectionAssert.Contains(report.evidencePaths, Milestone135FullRunCombatReadinessLockAssetGenerator.DocsPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone135FullRunCombatReadinessLockAssetGenerator.M134ReportPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone135FullRunCombatReadinessLockAssetGenerator.QaChecklistPath);
        }

        [Test]
        public void GeneratedReportsAndChecklistArePresentPassingAndUseM135LockId()
        {
            Assert.IsTrue(File.Exists(Milestone135FullRunCombatReadinessLockAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone135FullRunCombatReadinessLockAssetGenerator.ReportJsonPath));
            Assert.IsTrue(File.Exists(Milestone135FullRunCombatReadinessLockAssetGenerator.QaChecklistPath));

            var markdown = File.ReadAllText(Milestone135FullRunCombatReadinessLockAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M135 Full-Run Combat Readiness Lock Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone135FullRunCombatReadinessLockAssetGenerator.LockId, markdown);
            StringAssert.Contains("normal run validates", markdown);
            StringAssert.Contains("Stone Warden", markdown);

            var checklist = File.ReadAllText(Milestone135FullRunCombatReadinessLockAssetGenerator.QaChecklistPath);
            StringAssert.Contains("Full Run Flow", checklist);
            StringAssert.Contains("Boss Anchors", checklist);
            StringAssert.Contains("Roster Smoke", checklist);
            StringAssert.Contains("Room Combat Spawn Readiness", checklist);

            var json = JsonUtility.FromJson<Milestone135FullRunCombatReadinessLockReport>(
                File.ReadAllText(Milestone135FullRunCombatReadinessLockAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone135FullRunCombatReadinessLockAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.IsTrue((json.failures ?? new string[0]).Length == 0);
        }

        [Test]
        public void ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone135FullRunCombatReadinessLockValidator.Validate(exitOnFailure: false));
        }

        private static GameObject CreateRollPlayer(Transform parent)
        {
            var player = new GameObject("Player");
            player.transform.SetParent(parent, false);
            player.AddComponent<PlayerWeaponController>();
            player.AddComponent<PlayerMovementController>();
            player.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            player.AddComponent<CombatKnockbackReceiver>().Configure(null, Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            player.AddComponent<PlayerDamageFeedbackController>().Configure(null, null);
            return player;
        }

        private static BranchSessionContent CreateContent(out BranchGenerationSettingsDefinition settings)
        {
            settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            Assert.IsNotNull(settings);
            Assert.IsNotNull(catalog);
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            return content;
        }

        private static void AssertSharedBake(RoomNavMeshCatalogDefinition catalog, string roomId, string expectedSharedRoomId)
        {
            Assert.IsTrue(RoomNavMeshCatalogDefinition.TryResolveSharedBakeRoomId(roomId, out var sharedRoomId), roomId);
            Assert.AreEqual(expectedSharedRoomId, sharedRoomId);
            Assert.IsTrue(catalog.TryGetNavMeshData(roomId, out var data, out var resolvedRoomId), roomId);
            Assert.IsNotNull(data, roomId);
            Assert.AreEqual(expectedSharedRoomId, resolvedRoomId, roomId);
        }
    }

    internal static class M135BossTestExtensions
    {
        public static string BossStatusLabelForTest(this BossDefinition boss)
        {
            return boss.Phases.FirstOrDefault()?.statusText ?? string.Empty;
        }
    }
}
