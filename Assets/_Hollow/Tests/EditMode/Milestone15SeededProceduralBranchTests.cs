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
    public sealed class Milestone15SeededProceduralBranchTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";
        private const string CatalogPath = "Assets/_Hollow/Data/Branches/BranchRoomTemplateCatalog_MacroFixtures.asset";
        private const string SettingsPath = "Assets/_Hollow/Data/Branches/BranchGenerationSettings_M15.asset";

        [Test]
        public void SeededMacroBranchIsDeterministicForSameSeed()
        {
            var content = LoadContent();
            var settings = LoadSettings();

            var first = BranchGenerator.CreateSeededMacroBranch(content, settings, settings.DefaultSeed);
            var second = BranchGenerator.CreateSeededMacroBranch(content, settings, settings.DefaultSeed);

            Assert.AreEqual(Signature(first), Signature(second));
            Assert.AreEqual(BranchGenerator.SeededMacroBranchId, first.BranchId);
            Assert.AreEqual(8, first.RoomCount);
            Assert.AreEqual(14, first.Connections.Count);
        }

        [Test]
        public void SeededMacroBranchHasNoOverlapsOneBossLeafAndExplicitPorts()
        {
            var graph = BranchGenerator.CreateSeededMacroBranch(LoadContent(), LoadSettings(), BranchGenerator.DefaultSeededMacroSeed);

            Assert.AreEqual(graph.Rooms.Sum(room => room.Footprint.OccupiedCellCount), graph.OccupancyMap.OwnerByCell.Count);
            Assert.IsTrue(graph.Connections.All(connection => connection.HasExplicitPorts));
            Assert.AreEqual(graph.RoomCount - 1, graph.Connections.Count / 2);
            AssertGraphConnected(graph);

            var boss = graph.Rooms.Single(room => room.Role == BranchRoomRole.Boss);
            Assert.AreEqual("boss_01", boss.Id.Value);
            Assert.AreEqual(1, graph.ConnectionsFrom(boss.Id).Count);
            Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Origin));
        }

        [Test]
        public void ProceduralRewardPlanCoversGeneratedRoomsAndBossReward()
        {
            var graph = BranchGenerator.CreateSeededMacroBranch(LoadContent(), LoadSettings(), BranchGenerator.DefaultSeededMacroSeed);
            var plan = ProceduralRewardResolver.CreatePlan(graph);

            Assert.AreEqual(7, plan.Rewards.Count);
            Assert.IsTrue(plan.TryResolve("boss_01", out var bossReward));
            Assert.AreEqual("boss_sigil", bossReward.RewardId);
            Assert.AreEqual(25, bossReward.Souls);
            Assert.AreEqual(85, plan.Rewards.Sum(reward => reward.Souls));

            var restored = ProceduralRewardPlan.FromSaveState(plan.ToSaveState());
            Assert.IsTrue(restored.TryResolve("boss_01", out var restoredBossReward));
            Assert.AreEqual(bossReward.DisplayName, restoredBossReward.DisplayName);
        }

        [Test]
        public void ExactPortTraversalUsesGeneratedConnectionAfterRoomClear()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out var player, out _);
            try
            {
                Assert.AreEqual(BranchGenerator.SeededMacroBranchId, branch.State.Graph.BranchId);
                var connection = branch.State.Graph.ConnectionsFrom(BranchRoomId.Origin).First();
                Assert.IsFalse(branch.TryTraverse(connection.FromDirection));

                ClearCurrentRoom(combat);
                var roomRuntime = root.GetComponentInChildren<RoomRuntimeRoot>();
                Assert.IsTrue(roomRuntime.TryGetDoorPortById(connection.FromPortId, out var port));
                player.transform.localPosition = port.Position;

                Assert.IsTrue(branch.TryInteract());
                Assert.AreEqual(connection.ToRoomId, branch.State.CurrentRoomId);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BossRoomSpawnsSingleStoneWarden()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out _, out var sessionState);
            try
            {
                var bossRoom = branch.State.Graph.Rooms.Single(room => room.Role == BranchRoomRole.Boss);
                var snapshot = branch.CreateSnapshot();
                snapshot.currentRoomId = bossRoom.Id.Value;
                branch.InitializeFromSnapshot(ImportSampleRoom(), sessionState, snapshot);

                Assert.AreEqual(bossRoom.Id, branch.State.CurrentRoomId);
                Assert.AreEqual(1, combat.Enemies.Count);
                Assert.AreEqual(EnemyArchetypeId.Boss, combat.Enemies[0].ArchetypeId);
                Assert.AreEqual(14, combat.Enemies[0].Health.MaxHealth);
                Assert.AreEqual(2, combat.Enemies[0].ContactDamage);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void M15SnapshotPersistsBranchSeedAndRewardPlan()
        {
            var root = CreateBranchHarness(out var branch, out _, out _, out var sessionState);
            try
            {
                var snapshot = branch.CreateSnapshot();
                Assert.AreEqual(BranchGenerator.SeededMacroBranchId, snapshot.branchId);
                Assert.AreEqual(BranchGenerator.DefaultSeededMacroSeed, snapshot.branchSeed);
                Assert.AreEqual(7, snapshot.proceduralRewardPlan.Count);

                var bossRoom = branch.State.Graph.Rooms.Single(room => room.Role == BranchRoomRole.Boss);
                snapshot.currentRoomId = bossRoom.Id.Value;
                branch.InitializeFromSnapshot(ImportSampleRoom(), sessionState, snapshot);

                Assert.AreEqual(BranchGenerator.SeededMacroBranchId, branch.State.Graph.BranchId);
                Assert.AreEqual(bossRoom.Id, branch.State.CurrentRoomId);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void M14SnapshotStillRestoresFixedMacroBranch()
        {
            var root = CreateBranchHarness(out var branch, out _, out _, out var sessionState);
            try
            {
                var snapshot = new Hollow.Persistence.RunSaveSnapshot
                {
                    runId = "m14-legacy",
                    branchId = BranchGenerator.MacroFixtureBranchId,
                    branchSeed = BranchGenerator.DefaultMacroFixtureSeed,
                    currentRoomId = BranchRoomId.West.Value
                };

                branch.InitializeFromSnapshot(ImportSampleRoom(), sessionState, snapshot);
                Assert.AreEqual(BranchGenerator.MacroFixtureBranchId, branch.State.Graph.BranchId);
                Assert.AreEqual(BranchRoomId.West, branch.State.CurrentRoomId);
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
            var root = new GameObject("M15BranchHarness");

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
            branch.ConfigureGenerationSettings(CreateM15CompatibleSettings());
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

        private static BranchGenerationSettingsDefinition LoadSettings()
        {
            return AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(SettingsPath);
        }

        private static BranchGenerationSettingsDefinition CreateM15CompatibleSettings()
        {
            var settings = ScriptableObject.CreateInstance<BranchGenerationSettingsDefinition>();
            settings.Configure(
                BranchGenerator.DefaultSeededMacroSeed,
                nextTargetRoomCount: 8,
                nextMaxPlacementAttempts: 250,
                nextAllowLoops: false,
                nextEnableBossLeaf: true,
                nextEnableTreasureLeaf: false,
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

        private static void AssertGraphConnected(BranchFloorGraph graph)
        {
            var visited = new HashSet<BranchRoomId> { BranchRoomId.Origin };
            var queue = new Queue<BranchRoomId>();
            queue.Enqueue(BranchRoomId.Origin);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var connection in graph.ConnectionsFrom(current))
                {
                    if (visited.Add(connection.ToRoomId))
                    {
                        queue.Enqueue(connection.ToRoomId);
                    }
                }
            }

            Assert.AreEqual(graph.RoomCount, visited.Count);
        }

        private static void ClearCurrentRoom(RoomCombatController combat)
        {
            foreach (var enemy in combat.Enemies.ToArray())
            {
                DamageSystem.ApplyDamage(enemy.Health, new DamageRequest(99, combat.gameObject));
            }

            combat.EvaluateRoomState();
            Assert.AreEqual(RoomObjectiveState.Cleared, combat.ObjectiveState);
        }
    }
}
