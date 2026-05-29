using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Core;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Persistence;
using Hollow.Rewards;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone134BranchPacingRewardRoomShapePassTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void NormalBranchesGetOneOrTwoMainRouteRewardRooms()
        {
            var content = CreateContent(out var settings);
            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath);
            var sawOneReward = false;
            var sawTwoRewards = false;

            for (var seed = 13400; seed < 13460; seed++)
            {
                var m20Graph = BranchGenerator.CreateSeededBranchFeatures(
                    content,
                    settings,
                    seed,
                    RoomBiomeIds.HollowThreshold,
                    enableCorruptedChestLeaf: true,
                    enableWaveRoomLeaf: true,
                    enableSpecialEncounterLeaf: true);
                AssertRewardPolicy(m20Graph, ref sawOneReward, ref sawTwoRewards);

                var m46Graph = BranchGenerator.CreateDirectedEncounterBranch(
                    content,
                    settings,
                    profile,
                    worldIndex: 1,
                    seed: seed,
                    bossRoomAssetId: string.Empty,
                    biomeId: RoomBiomeIds.HollowThreshold,
                    enableCorruptedChestLeaf: true,
                    enableWaveRoomLeaf: true,
                    enableSpecialEncounterLeaf: true);
                AssertRewardPolicy(m46Graph, ref sawOneReward, ref sawTwoRewards);
            }

            Assert.IsTrue(sawOneReward, "M134 should produce one-Reward branches across deterministic seeds.");
            Assert.IsTrue(sawTwoRewards, "M134 should produce two-Reward branches across deterministic seeds.");
        }

        [Test]
        public void RewardRoomsSkipEncounterPlansAndBossKeyPlacement()
        {
            var content = CreateContent(out var settings);
            var graph = BranchGenerator.CreateSeededBranchFeatures(
                content,
                settings,
                13421,
                RoomBiomeIds.HollowThreshold,
                enableCorruptedChestLeaf: true,
                enableWaveRoomLeaf: true,
                enableSpecialEncounterLeaf: true);
            var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath);
            var encounterPlan = EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed);
            var featurePlan = BranchFeaturePlan.Create(graph);

            foreach (var rewardRoom in graph.Rooms.Where(room => room.Role == BranchRoomRole.Reward))
            {
                Assert.IsFalse(encounterPlan.TryResolve(rewardRoom.Id.Value, out _), rewardRoom.Id.Value);
                Assert.AreNotEqual(rewardRoom.Id.Value, featurePlan.BossKeyRoomId);
            }

            var sessionSource = File.ReadAllText(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.BranchSessionControllerPath);
            StringAssert.Contains("BranchRoomRole.Reward or BranchRoomRole.Treasure", sessionSource);
            StringAssert.Contains("State.CurrentRoom.MarkCleared()", sessionSource);
            StringAssert.Contains("State.CurrentRoom.MarkRewardPending()", sessionSource);
        }

        [Test]
        public void FreshDirectedBetaBranchKeepsCombatBossAndEncounterAssignments()
        {
            var content = CreateContent(out var settings);
            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath);
            var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath);
            var graph = BranchGenerator.CreateDirectedEncounterBranch(
                content,
                settings,
                profile,
                worldIndex: 1,
                seed: 13421,
                bossRoomAssetId: string.Empty,
                biomeId: RoomBiomeIds.BeforeTeeth,
                enableCorruptedChestLeaf: true,
                enableWaveRoomLeaf: true,
                enableSpecialEncounterLeaf: true);
            var plan = EncounterResolver.CreateDirectedSeededPlan(graph, encounterCatalog, graph.Seed, 1, profile);

            Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Boss));
            Assert.Greater(graph.Rooms.Count(room => room.Role == BranchRoomRole.Combat), 0);
            Assert.IsTrue(
                plan.Assignments.Any(assignment =>
                    graph.Rooms.Any(room => room.Id.Value == assignment.RoomId && room.Role == BranchRoomRole.Combat) &&
                    assignment.EnemySpawnKinds.Count > 0),
                "Fresh beta branches must assign enemies to Combat rooms.");
            Assert.IsTrue(
                plan.Assignments.Any(assignment =>
                    graph.Rooms.Any(room => room.Id.Value == assignment.RoomId && room.Role == BranchRoomRole.Boss) &&
                    assignment.EnemySpawnKinds.Count > 0),
                "Fresh beta branches must assign a boss encounter to the Boss room.");
        }

        [Test]
        public void ProfileContinueRejectsLegacyRewardOnlySnapshotsForBetaWorldLoop()
        {
            var legacy = new RunSaveSnapshot { branchId = BranchGenerator.MacroFixtureBranchId };
            var current = new RunSaveSnapshot { branchId = BranchGenerator.DirectedEncounterBranchId };
            var challenge = new RunSaveSnapshot
            {
                branchId = BranchGenerator.MacroFixtureBranchId,
                challengeId = "small_monsters"
            };

            Assert.IsTrue(BranchSessionController.ShouldDiscardLegacyWorldLoopSnapshotForBeta(
                legacy,
                RuntimeSessionMode.ProfileBacked,
                hasBetaBranchRuntime: true,
                isSpaceshipHub: false,
                isDeveloperLab: false));
            Assert.IsFalse(BranchSessionController.ShouldDiscardLegacyWorldLoopSnapshotForBeta(
                current,
                RuntimeSessionMode.ProfileBacked,
                hasBetaBranchRuntime: true,
                isSpaceshipHub: false,
                isDeveloperLab: false));
            Assert.IsFalse(BranchSessionController.ShouldDiscardLegacyWorldLoopSnapshotForBeta(
                challenge,
                RuntimeSessionMode.ProfileBacked,
                hasBetaBranchRuntime: true,
                isSpaceshipHub: false,
                isDeveloperLab: false));
            Assert.IsFalse(BranchSessionController.ShouldDiscardLegacyWorldLoopSnapshotForBeta(
                legacy,
                RuntimeSessionMode.TransientDeveloperSample,
                hasBetaBranchRuntime: true,
                isSpaceshipHub: false,
                isDeveloperLab: false));
            Assert.IsFalse(BranchSessionController.ShouldDiscardLegacyWorldLoopSnapshotForBeta(
                legacy,
                RuntimeSessionMode.ProfileBacked,
                hasBetaBranchRuntime: false,
                isSpaceshipHub: false,
                isDeveloperLab: false));
        }

        [Test]
        public void RewardRoomCacheRollUsesWoodenCacheBump()
        {
            var sawRewardGolden = false;
            var sawRewardNormal = false;
            var sawRewardCoins = false;
            var sawRewardHeal = false;
            var sawRewardNothing = false;
            var rewardNormalCount = 0;
            var combatNormalCount = 0;
            var standard = ScriptableObject.CreateInstance<RewardPoolDefinition>();
            standard.Configure(ProceduralRewardResolver.PreBetaStandardPoolId, System.Array.Empty<RewardDefinition>());
            try
            {
                for (var seed = 13400; seed < 15400; seed++)
                {
                    var rewardGrant = ProceduralRewardResolver.RollM134RewardRoomCacheReward("reward_01", BranchGenerator.DirectedEncounterBranchId, seed);
                    sawRewardNothing |= rewardGrant.IsEmpty;
                    sawRewardGolden |= rewardGrant.RewardId == ChestRewardResolver.GoldenChestRewardId;
                    sawRewardNormal |= rewardGrant.RewardId == ChestRewardResolver.NormalChestRewardId;
                    sawRewardCoins |= rewardGrant.RewardId == ChestRewardResolver.SmallCoinPouchRewardId && rewardGrant.Coins > 0;
                    sawRewardHeal |= rewardGrant.RewardKind == RewardKind.Heal;
                    rewardNormalCount += rewardGrant.RewardId == ChestRewardResolver.NormalChestRewardId ? 1 : 0;

                    var combatPlan = ProceduralRewardResolver.CreateSeededPlan(CreateSingleRoomGraph(seed, BranchRoomRole.Combat), standard, null, null);
                    Assert.IsTrue(combatPlan.TryResolve("room_01", out var combatGrant));
                    combatNormalCount += combatGrant.RewardId == ChestRewardResolver.NormalChestRewardId ? 1 : 0;
                }

                Assert.IsTrue(sawRewardGolden);
                Assert.IsTrue(sawRewardNormal);
                Assert.IsTrue(sawRewardCoins);
                Assert.IsTrue(sawRewardHeal);
                Assert.IsTrue(sawRewardNothing);
                Assert.Greater(rewardNormalCount, combatNormalCount, "Reward rooms should have a larger wooden chest share than M52 Combat rooms.");
            }
            finally
            {
                Object.DestroyImmediate(standard);
            }
        }

        [Test]
        public void ShapeWeightsFavorSmallAndMediumRooms()
        {
            Assert.AreEqual(30, BranchPacingPolicy.Single1x1Weight);
            Assert.AreEqual(25, BranchPacingPolicy.Wide2x1Weight);
            Assert.AreEqual(20, BranchPacingPolicy.Tall1x2Weight);
            Assert.AreEqual(15, BranchPacingPolicy.Block2x2Weight);
            Assert.AreEqual(10, BranchPacingPolicy.L3CellWeight);

            var content = CreateContent(out var settings);
            var smallShapes = 0;
            var largeShapes = 0;
            for (var seed = 13400; seed < 13540; seed++)
            {
                var graph = BranchGenerator.CreateSeededBranchFeatures(
                    content,
                    settings,
                    seed,
                    RoomBiomeIds.HollowThreshold,
                    enableCorruptedChestLeaf: true,
                    enableWaveRoomLeaf: true,
                    enableSpecialEncounterLeaf: true);

                foreach (var room in graph.Rooms.Where(room => room.Role is BranchRoomRole.Combat or BranchRoomRole.Reward))
                {
                    var shape = RoomFootprintShapeUtility.Classify(room.Footprint);
                    if (shape is RoomFootprintShape.Single1x1 or RoomFootprintShape.Wide2x1 or RoomFootprintShape.Tall1x2)
                    {
                        smallShapes++;
                    }
                    else if (shape is RoomFootprintShape.Block2x2 or RoomFootprintShape.L3Cell)
                    {
                        largeShapes++;
                    }
                }
            }

            Assert.Greater(smallShapes, largeShapes);
        }

        [Test]
        public void LiveReportPassesAllBranchPacingChecks()
        {
            var report = Milestone134BranchPacingRewardRoomShapePassAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.Greater(report.totalChecks, 8);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            CollectionAssert.Contains(report.evidencePaths, Milestone134BranchPacingRewardRoomShapePassAssetGenerator.DocsPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone134BranchPacingRewardRoomShapePassAssetGenerator.M133ReportPath);
        }

        [Test]
        public void GeneratedReportsArePresentPassingAndUseM134LockId()
        {
            Assert.IsTrue(File.Exists(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportJsonPath));

            var markdown = File.ReadAllText(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M134 Branch Pacing + Reward Room Shape Pass Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.LockId, markdown);
            StringAssert.Contains("one guaranteed non-combat Reward room plus a deterministic 50% second Reward room", markdown);
            StringAssert.Contains("M134 wooden-cache roll", markdown);

            var json = JsonUtility.FromJson<Milestone134BranchPacingRewardRoomShapePassReport>(
                File.ReadAllText(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.IsTrue((json.failures ?? new string[0]).Length == 0);
        }

        [Test]
        public void ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone134BranchPacingRewardRoomShapePassValidator.Validate(exitOnFailure: false));
        }

        private static void AssertRewardPolicy(BranchFloorGraph graph, ref bool sawOneReward, ref bool sawTwoRewards)
        {
            Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError), topologyError);
            var rewardRooms = graph.Rooms.Where(room => room.Role == BranchRoomRole.Reward).ToArray();
            Assert.GreaterOrEqual(rewardRooms.Length, 1);
            Assert.LessOrEqual(rewardRooms.Length, 2);
            sawOneReward |= rewardRooms.Length == 1;
            sawTwoRewards |= rewardRooms.Length == 2;

            var featurePlan = BranchFeaturePlan.Create(graph);
            foreach (var rewardRoom in rewardRooms)
            {
                Assert.AreNotEqual(BranchRoomId.Origin.Value, rewardRoom.Id.Value);
                Assert.AreNotEqual(featurePlan.BossKeyRoomId, rewardRoom.Id.Value);
                Assert.GreaterOrEqual(
                    graph.ConnectionsFrom(rewardRoom.Id).Select(connection => connection.ToRoomId).Distinct().Count(),
                    2,
                    "Reward rooms should be selected from the route body, not terminal special leaves.");
            }
        }

        private static BranchFloorGraph CreateSingleRoomGraph(int seed, BranchRoomRole role)
        {
            var graph = new BranchFloorGraph(BranchGenerator.DirectedEncounterBranchId, seed);
            graph.AddRoom(CreateRoom("origin", BranchRoomRole.Origin, Vector2Int.zero));
            graph.AddRoom(CreateRoom("room_01", role, new Vector2Int(1, 0)));
            graph.AddBidirectionalConnection(new BranchRoomId("origin"), new BranchRoomId("room_01"), "east", "west");
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

        private static BranchSessionContent CreateContent(out BranchGenerationSettingsDefinition settings)
        {
            settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            Assert.IsNotNull(catalog);
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            return content;
        }
    }
}
