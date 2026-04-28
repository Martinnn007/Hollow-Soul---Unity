using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Rewards;
using Hollow.Rooms;
using Hollow.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone17FeatureBranchTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";
        private const string CatalogPath = "Assets/_Hollow/Data/Branches/BranchRoomTemplateCatalog_MacroFixtures.asset";

        [Test]
        public void SeededFeatureBranchIsDeterministicAndAddsOneTreasureLeaf()
        {
            var content = LoadContent();
            var settings = CreateM17Settings();

            var first = BranchGenerator.CreateSeededFeatureBranch(content, settings, settings.DefaultSeed);
            var second = BranchGenerator.CreateSeededFeatureBranch(content, settings, settings.DefaultSeed);

            Assert.AreEqual(Signature(first), Signature(second));
            Assert.AreEqual(BranchGenerator.FeatureBranchId, first.BranchId);
            Assert.AreEqual(8, first.RoomCount);
            Assert.GreaterOrEqual(first.Connections.Count, 14);
            Assert.IsTrue(first.Connections.All(connection => connection.HasExplicitPorts));
            Assert.AreEqual(first.Rooms.Sum(room => room.Footprint.OccupiedCellCount), first.OccupancyMap.OwnerByCell.Count);
            Assert.AreEqual(1, first.Rooms.Count(room => room.Role == BranchRoomRole.Origin));
            Assert.AreEqual(1, first.Rooms.Count(room => room.Role == BranchRoomRole.Boss));
            Assert.AreEqual(1, first.Rooms.Count(room => room.Role == BranchRoomRole.Treasure));
            Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(first, out var topologyError), topologyError);

            var boss = first.Rooms.Single(room => room.Role == BranchRoomRole.Boss);
            Assert.AreEqual(1, first.ConnectionsFrom(boss.Id).Count);

            var treasure = first.Rooms.Single(room => room.Role == BranchRoomRole.Treasure);
            Assert.GreaterOrEqual(first.ConnectionsFrom(treasure.Id).Count, 1);

            var legacyMacro = BranchGenerator.CreateSeededMacroBranch(content, settings, settings.DefaultSeed);
            Assert.AreEqual(BranchGenerator.SeededMacroBranchId, legacyMacro.BranchId);
            Assert.AreEqual(0, legacyMacro.Rooms.Count(room => room.Role == BranchRoomRole.Treasure));
        }

        [Test]
        public void FeatureRewardPlanIncludesTreasureCacheAndBossReward()
        {
            var graph = BranchGenerator.CreateSeededFeatureBranch(LoadContent(), CreateM17Settings(), BranchGenerator.DefaultSeededMacroSeed);
            var plan = ProceduralRewardResolver.CreatePlan(graph);
            var treasure = graph.Rooms.Single(room => room.Role == BranchRoomRole.Treasure);

            Assert.AreEqual(7, plan.Rewards.Count);
            Assert.IsTrue(plan.TryResolve(treasure.Id.Value, out var treasureReward));
            Assert.AreEqual("treasure_cache", treasureReward.RewardId);
            Assert.AreEqual("Treasure Cache", treasureReward.DisplayName);
            Assert.AreEqual(RewardKind.Currency, treasureReward.RewardKind);
            Assert.AreEqual(15, treasureReward.Souls);
            Assert.IsTrue(plan.TryResolve("boss_01", out var bossReward));
            Assert.AreEqual("boss_sigil", bossReward.RewardId);
            Assert.AreEqual(25, bossReward.Souls);
            Assert.AreEqual(90, plan.Rewards.Sum(reward => reward.Souls));
        }

        [Test]
        public void FreshSessionUsesFeatureBranchButLegacyM15SnapshotsStillRestore()
        {
            var root = CreateBranchHarness(out var branch, out _, out _, out var sessionState);
            try
            {
                var featureSnapshot = branch.CreateSnapshot();
                Assert.AreEqual(BranchGenerator.FeatureBranchId, featureSnapshot.branchId);
                Assert.AreEqual(BranchGenerator.DefaultSeededMacroSeed, featureSnapshot.branchSeed);
                Assert.AreEqual(7, featureSnapshot.proceduralRewardPlan.Count);

                var legacySnapshot = new Hollow.Persistence.RunSaveSnapshot
                {
                    runId = "m15-legacy",
                    branchId = BranchGenerator.SeededMacroBranchId,
                    branchSeed = BranchGenerator.DefaultSeededMacroSeed,
                    currentRoomId = BranchRoomId.Origin.Value
                };

                branch.InitializeFromSnapshot(ImportSampleRoom(), sessionState, legacySnapshot);
                Assert.AreEqual(BranchGenerator.SeededMacroBranchId, branch.State.Graph.BranchId);
                Assert.AreEqual(0, branch.State.Graph.Rooms.Count(room => room.Role == BranchRoomRole.Treasure));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TreasureRoomSkipsCombatAndMakesRewardAvailableOnEntry()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out _, out var sessionState);
            try
            {
                var treasureRoom = branch.State.Graph.Rooms.Single(room => room.Role == BranchRoomRole.Treasure);
                var snapshot = branch.CreateSnapshot();
                snapshot.currentRoomId = treasureRoom.Id.Value;

                branch.InitializeFromSnapshot(ImportSampleRoom(), sessionState, snapshot);

                Assert.AreEqual(treasureRoom.Id, branch.State.CurrentRoomId);
                Assert.AreEqual(BranchRoomRole.Treasure, branch.State.CurrentRoom.Role);
                Assert.IsTrue(branch.State.CurrentRoom.IsCleared);
                Assert.AreEqual(RoomRewardState.Pending, branch.State.CurrentRoom.RewardState);
                Assert.AreEqual(RoomObjectiveState.Cleared, combat.ObjectiveState);
                Assert.AreEqual(0, combat.Enemies.Count);
                Assert.IsNotNull(branch.CurrentRewardPickup);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateBranchHarness(
            out BranchSessionController branch,
            out RoomCombatController combat,
            out PlaceholderPlayerController player,
            out GameSessionState sessionState)
        {
            var root = new GameObject("M17BranchHarness");

            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            roomObject.AddComponent<RoomRuntimeRoot>();

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);

            var enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyPrefab.name = "EnemyBase_TestPrefab";
            enemyPrefab.transform.SetParent(root.transform, false);
            enemyPrefab.SetActive(false);
            enemyPrefab.AddComponent<CombatantHealth>();
            enemyPrefab.AddComponent<EnemyRuntimeController>();

            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.name = "ProjectileBase_TestPrefab";
            projectilePrefab.transform.SetParent(root.transform, false);
            projectilePrefab.SetActive(false);
            projectilePrefab.AddComponent<ProjectileController>();

            combat = root.AddComponent<RoomCombatController>();
            combat.Configure(enemyPrefab, projectilePrefab, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());

            branch = root.AddComponent<BranchSessionController>();
            branch.Configure(null, null);
            branch.ConfigureTemplateCatalog(LoadCatalog(), BranchGenerator.DefaultSeededMacroSeed);
            branch.ConfigureGenerationSettings(CreateM17Settings());
            sessionState = GameSessionState.Create(RuntimeSessionMode.ProfileBacked, HollowPlatformKind.WindowsStandard3D, null, Vector3.zero);
            branch.Initialize(ImportSampleRoom(), sessionState);
            return root;
        }

        private static BranchSessionContent LoadContent()
        {
            var content = BranchSessionContent.Create(ImportSampleRoom(), LoadCatalog(), BranchGenerator.DefaultSeededMacroSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            Assert.IsTrue(content.HasMacroFixturePool);
            return content;
        }

        private static BranchRoomTemplateCatalogDefinition LoadCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(CatalogPath);
        }

        private static BranchGenerationSettingsDefinition CreateM17Settings()
        {
            var settings = ScriptableObject.CreateInstance<BranchGenerationSettingsDefinition>();
            settings.Configure(
                BranchGenerator.DefaultSeededMacroSeed,
                nextTargetRoomCount: 8,
                nextMaxPlacementAttempts: 250,
                nextAllowLoops: false,
                nextEnableBossLeaf: true,
                nextEnableTreasureLeaf: true,
                nextAllowedFixtureIds: new[]
                {
                    "combat_macro_single_1x1",
                    "combat_macro_wide_2x1",
                    "combat_macro_tall_1x2",
                    "combat_macro_block_2x2",
                    "combat_macro_l_3cell"
                });
            return settings;
        }

        private static ImportedRoomRuntimeAsset ImportSampleRoom()
        {
            return HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
        }

        private static string Signature(BranchFloorGraph graph)
        {
            var rooms = graph.Rooms
                .OrderBy(room => room.Id.Value)
                .Select(room => $"{room.Id.Value}:{room.Role}:{room.RuntimeRoomAssetId}:{room.Coordinate}:{string.Join(",", room.Footprint.OccupiedCells.OrderBy(cell => cell.x).ThenBy(cell => cell.y))}");
            var connections = graph.Connections
                .OrderBy(connection => connection.FromRoomId.Value)
                .ThenBy(connection => connection.FromPortId)
                .Select(connection => $"{connection.FromRoomId}.{connection.FromPortId}->{connection.ToRoomId}.{connection.ToPortId}");
            return $"{string.Join("|", rooms)}::{string.Join("|", connections)}";
        }
    }
}
