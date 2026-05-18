using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Rooms;
using Hollow.UI.MainMenu;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone47ChallengeModeV2CuratedSeedsTests
    {
        [Test]
        public void ChallengeCatalogContainsSevenCuratedSeedsAndRules()
        {
            var catalog = LoadChallengeCatalog();

            Assert.AreEqual(Milestone47AssetGenerator.CatalogId, catalog.CatalogId);
            Assert.AreEqual(7, catalog.Challenges.Count);
            AssertChallenge(catalog, "blade_trial", 47001, "balanced");
            AssertChallenge(catalog, "glass_runner", 47002, "balanced");
            AssertChallenge(catalog, "stone_oath", 47003, "heavy");
            AssertChallenge(catalog, "macro_maze", 47004, "balanced");
            AssertChallenge(catalog, "splitter_swarm", 47005, "balanced");
            AssertChallenge(catalog, "merchants_debt", 47006, "balanced");
            AssertChallenge(catalog, "small_monsters", 47007, "balanced");

            Assert.IsTrue(catalog.Resolve("blade_trial").HasRule(ChallengeRuleKind.BlockShops));
            Assert.IsTrue(catalog.Resolve("glass_runner").HasRule(ChallengeRuleKind.BlockHealingRewards));
            Assert.AreEqual(2, catalog.Resolve("splitter_swarm").RuleIntValue(ChallengeRuleKind.EncounterPressureBonus));
            Assert.IsTrue(catalog.Resolve("small_monsters").HasRule(ChallengeRuleKind.SmallMonstersOnly));
        }

        [Test]
        public void ChallengeLaunchRecordsAttemptWithoutTouchingActiveRun()
        {
            var tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m47_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
            try
            {
                var store = new JsonProfileStore(tempRoot);
                var slotId = new ProfileSlotId(0);
                var profile = store.CreateOrLoadProfile(slotId, "Challenge Runner");
                store.SaveActiveRun(slotId, new RunSaveSnapshot { runId = "existing", branchSeed = 777, currentRoomId = "origin" });
                var selectedContext = new SelectedProfileContext();
                var viewModel = new MainMenuViewModel(store, selectedContext, new AppStateMachine(), LoadChallengeCatalog());
                viewModel.SelectOrCreateSlot(profile.SlotIndex);

                viewModel.OpenChallenges();
                var route = viewModel.LaunchChallenge("merchants_debt", HollowPlatformKind.WindowsStandard3D);

                Assert.AreEqual(AppShellRoute.GameWindows, route);
                Assert.AreEqual("merchants_debt", selectedContext.SelectedChallengeId);
                Assert.AreEqual("balanced", selectedContext.SelectedCharacterId);
                Assert.IsTrue(store.TryLoadActiveRun(slotId, out var preserved));
                Assert.AreEqual(777, preserved.branchSeed);

                var record = store.GetChallengeRecord(slotId, "merchants_debt");
                Assert.AreEqual(1, record.Attempts);
                Assert.AreEqual(0, record.Completions);
                Assert.AreEqual(47006, record.LastPlayedSeed);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        [Test]
        public void VisionOSChallengeLaunchUsesBoundedRouteAndFixedChallengeCharacter()
        {
            var tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m47_visionos_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
            try
            {
                var store = new JsonProfileStore(tempRoot);
                var profile = store.CreateOrLoadProfile(new ProfileSlotId(0), "Vision Runner");
                var selectedContext = new SelectedProfileContext();
                var viewModel = new MainMenuViewModel(store, selectedContext, new AppStateMachine(), LoadChallengeCatalog());
                viewModel.SelectOrCreateSlot(profile.SlotIndex);

                viewModel.OpenChallenges();
                var route = viewModel.LaunchChallenge("stone_oath", HollowPlatformKind.VisionOSBoundedTabletop);

                Assert.AreEqual(AppShellRoute.GameVisionOSBounded, route);
                Assert.AreEqual("stone_oath", selectedContext.SelectedChallengeId);
                Assert.AreEqual("heavy", selectedContext.SelectedCharacterId);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        [Test]
        public void ChallengeRecordsPersistBestClearTimeOnlyWhenImproved()
        {
            var tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m47_record_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
            try
            {
                var store = new JsonProfileStore(tempRoot);
                var slotId = new ProfileSlotId(0);
                store.CreateOrLoadProfile(slotId, "Challenge Runner");
                store.MarkChallengeAttemptStarted(slotId, "blade_trial", 47001);
                store.CompleteChallengeAttempt(slotId, "blade_trial", 47001, 320f);
                store.MarkChallengeAttemptStarted(slotId, "blade_trial", 47001);
                store.CompleteChallengeAttempt(slotId, "blade_trial", 47001, 280f);
                store.MarkChallengeAttemptStarted(slotId, "blade_trial", 47001);
                store.CompleteChallengeAttempt(slotId, "blade_trial", 47001, 400f);

                var record = store.GetChallengeRecord(slotId, "blade_trial");
                Assert.AreEqual(3, record.Attempts);
                Assert.AreEqual(3, record.Completions);
                Assert.AreEqual(280f, record.BestClearTimeSeconds);
                Assert.AreEqual("Completed", record.LastResult);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        [Test]
        public void EncounterPressureRuleRaisesDirectedDifficultyBand()
        {
            var profile = EncounterDirectorProfileDefinition.CreateRuntimeDefault();
            var catalog = CreateEncounterCatalog();
            try
            {
                var graph = CreateGraph();
                var normal = EncounterResolver.CreateDirectedSeededPlan(graph, catalog, 47005, 1, profile);
                var pressured = EncounterResolver.CreateDirectedSeededPlan(graph, catalog, 47005, 1, profile, 2);

                Assert.IsTrue(normal.TryResolve("combat_01", out var normalCombat));
                Assert.IsTrue(pressured.TryResolve("combat_01", out var pressuredCombat));
                Assert.AreEqual(normalCombat.DifficultyBand + 2, pressuredCombat.DifficultyBand);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                foreach (var encounter in catalog.Encounters.Distinct())
                {
                    Object.DestroyImmediate(encounter);
                }

                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void SmallMonstersRuleRemapsNonBossSpawnsAndLeavesBossAssignment()
        {
            var profile = EncounterDirectorProfileDefinition.CreateRuntimeDefault();
            var catalog = CreateEncounterCatalog();
            try
            {
                var graph = CreateGraph();
                var plan = EncounterResolver.CreateDirectedSeededPlan(
                    graph,
                    catalog,
                    47007,
                    1,
                    profile,
                    0,
                    null,
                    new[] { "spawnEnemyRat", "spawnEnemySpider" });

                Assert.IsTrue(plan.TryResolve("combat_01", out var combat));
                Assert.Greater(combat.EnemySpawnKinds.Count, 0);
                Assert.IsTrue(combat.EnemySpawnKinds.All(kind => kind == "spawnEnemyRat" || kind == "spawnEnemySpider"));

                Assert.IsTrue(plan.TryResolve("boss_01", out var boss));
                CollectionAssert.Contains(boss.EnemySpawnKinds.ToArray(), "spawnEnemyBoss");
            }
            finally
            {
                Object.DestroyImmediate(profile);
                foreach (var encounter in catalog.Encounters.Distinct())
                {
                    Object.DestroyImmediate(encounter);
                }

                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Milestone47ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Hollow.Editor.Validation.Milestone47Validator.Validate());
        }

        private static void AssertChallenge(ChallengeCatalogDefinition catalog, string challengeId, int seed, string characterId)
        {
            Assert.IsTrue(catalog.TryGetChallenge(challengeId, out var challenge), $"Missing challenge {challengeId}.");
            Assert.AreEqual(seed, challenge.FixedRunSeed);
            Assert.AreEqual(characterId, challenge.SelectedCharacterId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(challenge.Loadout.MeleeWeaponId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(challenge.Loadout.RangedWeaponId));
            Assert.Greater(challenge.Rules.Count, 0);
        }

        private static ChallengeCatalogDefinition LoadChallengeCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalogDefinition>(Milestone47AssetGenerator.ChallengeCatalogPath);
            Assert.IsNotNull(catalog, "Run M47 generation before validating challenge catalog.");
            return catalog;
        }

        private static BranchFloorGraph CreateGraph()
        {
            var graph = new BranchFloorGraph(BranchGenerator.DirectedEncounterBranchId, 47005);
            graph.AddRoom(CreateRoom("origin", BranchRoomRole.Origin, Vector2Int.zero));
            graph.AddRoom(CreateRoom("combat_01", BranchRoomRole.Combat, new Vector2Int(1, 0)));
            graph.AddRoom(CreateRoom("boss_01", BranchRoomRole.Boss, new Vector2Int(2, 0)));
            graph.AddBidirectionalConnection(new BranchRoomId("origin"), new BranchRoomId("combat_01"), "east", "west");
            graph.AddBidirectionalConnection(new BranchRoomId("combat_01"), new BranchRoomId("boss_01"), "east", "west");
            return graph;
        }

        private static BranchRoomState CreateRoom(string id, BranchRoomRole role, Vector2Int cell)
        {
            return new BranchRoomState(
                new BranchRoomId(id),
                cell,
                new BranchRoomInstanceId(id),
                "test_room",
                new RoomInstanceFootprint(cell, new[] { cell }, new Vector2Int(13, 7)),
                role);
        }

        private static EncounterCatalogDefinition CreateEncounterCatalog()
        {
            var easy = ScriptableObject.CreateInstance<EncounterDefinition>();
            easy.Configure(
                "easy_chasers",
                "Easy Chasers",
                BranchRoomRole.Combat,
                0,
                99,
                1,
                99,
                1,
                new[] { new EncounterSpawnEntry("spawnEnemyNormal", 2) });
            var boss = ScriptableObject.CreateInstance<EncounterDefinition>();
            boss.Configure(
                "stone_warden_boss",
                "Stone Warden",
                BranchRoomRole.Boss,
                0,
                99,
                1,
                99,
                1,
                new[] { new EncounterSpawnEntry("spawnEnemyBoss", 1) });
            var catalog = ScriptableObject.CreateInstance<EncounterCatalogDefinition>();
            catalog.Configure("m47_test_encounters", new[] { easy, boss }, boss);
            return catalog;
        }
    }
}
