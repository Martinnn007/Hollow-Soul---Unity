using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Rooms;
using Hollow.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone14MacroBranchTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";
        private const string CatalogPath = "Assets/_Hollow/Data/Branches/BranchRoomTemplateCatalog_MacroFixtures.asset";

        [Test]
        public void MacroFixtureBranchHasExpectedTopologyFootprintsAndPortConnections()
        {
            var content = LoadContent();
            var graph = BranchGenerator.CreateMacroFixtureBranch(content.MacroRoomPool, content.BranchSeed);

            Assert.AreEqual(BranchGenerator.MacroFixtureBranchId, graph.BranchId);
            Assert.AreEqual(5, graph.RoomCount);
            Assert.GreaterOrEqual(graph.Connections.Count, 8);
            Assert.AreEqual(12, graph.OccupancyMap.OwnerByCell.Count);
            AssertRoomAsset(graph, BranchRoomId.Origin, "combat_macro_single_1x1", 1);
            AssertRoomAsset(graph, BranchRoomId.North, "combat_macro_tall_1x2", 2);
            AssertRoomAsset(graph, BranchRoomId.South, "combat_macro_l_3cell", 3);
            AssertRoomAsset(graph, BranchRoomId.East, "combat_macro_wide_2x1", 2);
            AssertRoomAsset(graph, BranchRoomId.West, "combat_macro_block_2x2", 4);
            AssertPortConnection(graph, BranchRoomId.Origin, "north_0", BranchRoomId.North, "south_0");
            AssertPortConnection(graph, BranchRoomId.Origin, "south_0", BranchRoomId.South, "north_0");
            AssertPortConnection(graph, BranchRoomId.Origin, "east_0", BranchRoomId.East, "west_0");
            AssertPortConnection(graph, BranchRoomId.Origin, "west_0", BranchRoomId.West, "east_1");
            Assert.IsFalse(graph.Connections
                .GroupBy(connection => $"{connection.FromRoomId.Value}:{connection.FromPortId}->{connection.ToRoomId.Value}:{connection.ToPortId}")
                .Any(group => group.Count() > 1));
        }

        [Test]
        public void MacroMiniMapModelPreservesActualRoomFootprintShapes()
        {
            var content = LoadContent();
            var graph = BranchGenerator.CreateMacroFixtureBranch(content.MacroRoomPool, content.BranchSeed);
            var state = BranchSessionState.Create(graph);

            var model = new BranchMiniMapModel(state);

            Assert.AreEqual(5, model.Nodes.Count);
            Assert.AreEqual(12, model.Nodes.Sum(node => node.OccupiedCells.Count));
            AssertMiniMapCells(model, BranchRoomId.Origin, 1);
            AssertMiniMapCells(model, BranchRoomId.North, 2);
            AssertMiniMapCells(model, BranchRoomId.South, 3);
            AssertMiniMapCells(model, BranchRoomId.East, 2);
            AssertMiniMapCells(model, BranchRoomId.West, 4);
            Assert.IsTrue(model.Nodes.All(node => node.IsRevealed));
            Assert.AreEqual(graph.Connections.Count / 2, model.Connections.Count);
            Assert.IsTrue(model.Connections.All(connection => connection.LockKind == BranchConnectionLockKind.None));
        }

        [Test]
        public void ExplicitPortTraversalLocksUntilClearAndUsesDestinationPortEntry()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out var player, out _);
            try
            {
                Assert.AreEqual(BranchGenerator.MacroFixtureBranchId, branch.State.Graph.BranchId);
                Assert.IsFalse(branch.TryTraverse("west"));

                ClearCurrentRoom(combat);
                Assert.IsTrue(branch.TryTraverse("west"));

                Assert.AreEqual(BranchRoomId.West, branch.State.CurrentRoomId);
                Assert.AreEqual("combat_macro_block_2x2", branch.CurrentRoomAsset.Id);
                Assert.AreEqual(12.25f, player.transform.localPosition.x, 0.001f);
                Assert.AreEqual(3.5f, player.transform.localPosition.z, 0.001f);
                Assert.IsTrue(branch.State.Graph.TryGetConnectionByPort(BranchRoomId.West, "east_1", out _));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AdjacentPassConnectsEveryCompatiblePortTouchingWideRoom()
        {
            var content = LoadContent();
            var graph = new BranchFloorGraph("adjacent_wide_test", 0);
            var wide = content.MacroRoomPool["combat_macro_wide_2x1"];
            var single = content.MacroRoomPool["combat_macro_single_1x1"];
            var wideId = new BranchRoomId("wide");
            var northLeftId = new BranchRoomId("north_left");
            var northRightId = new BranchRoomId("north_right");
            AddTestRoom(graph, wideId, new Vector2Int(0, 0), wide);
            AddTestRoom(graph, northLeftId, new Vector2Int(0, -1), single);
            AddTestRoom(graph, northRightId, new Vector2Int(1, -1), single);

            BranchGenerator.ConnectAdjacentCompatiblePorts(graph, content.MacroRoomPool);

            AssertPortConnection(graph, wideId, "north_0", northLeftId, "south_0");
            AssertPortConnection(graph, wideId, "north_1", northRightId, "south_0");
            Assert.IsTrue(graph.Connections.Count(connection => connection.FromRoomId == wideId && connection.ToRoomId != wideId) >= 2);
        }

        [Test]
        public void AdjacentPassConnectsLRoomMissingQuadrantOnBothValidFaces()
        {
            var content = LoadContent();
            var graph = new BranchFloorGraph("adjacent_l_test", 0);
            var lRoom = content.MacroRoomPool["combat_macro_l_3cell"];
            var single = content.MacroRoomPool["combat_macro_single_1x1"];
            var lId = new BranchRoomId("l_room");
            var pocketId = new BranchRoomId("pocket");
            AddTestRoom(graph, lId, new Vector2Int(0, 0), lRoom);
            AddTestRoom(graph, pocketId, new Vector2Int(1, 1), single);

            BranchGenerator.ConnectAdjacentCompatiblePorts(graph, content.MacroRoomPool);

            Assert.AreEqual(2, graph.Connections.Count(connection => connection.FromRoomId == pocketId && connection.ToRoomId == lId));
            Assert.AreEqual(2, graph.Connections.Count(connection => connection.FromRoomId == lId && connection.ToRoomId == pocketId));
        }

        [Test]
        public void AdjacentPassIgnoresSecretPorts()
        {
            var content = LoadContent();
            var wide = content.MacroRoomPool["combat_macro_wide_2x1"];
            var secretSingle = WithPortKind(content.MacroRoomPool["combat_macro_single_1x1"], "south_0", "secret");
            var roomPool = new Dictionary<string, ImportedRoomRuntimeAsset>
            {
                [wide.Id] = wide,
                [secretSingle.Id] = secretSingle
            };
            var graph = new BranchFloorGraph("adjacent_secret_test", 0);
            var wideId = new BranchRoomId("wide");
            var secretId = new BranchRoomId("secret_single");
            AddTestRoom(graph, wideId, new Vector2Int(0, 0), wide);
            AddTestRoom(graph, secretId, new Vector2Int(0, -1), secretSingle);

            BranchGenerator.ConnectAdjacentCompatiblePorts(graph, roomPool);

            Assert.IsFalse(graph.TryGetConnectionByPort(wideId, "north_0", out _));
            Assert.IsFalse(graph.TryGetConnectionByPort(secretId, "south_0", out _));
        }

        [Test]
        public void M14SnapshotPersistsBranchIdentitySeedAndRestoresMacroRooms()
        {
            var root = CreateBranchHarness(out var branch, out _, out _, out var sessionState);
            try
            {
                var snapshot = branch.CreateSnapshot();
                Assert.AreEqual(BranchGenerator.MacroFixtureBranchId, snapshot.branchId);
                Assert.AreEqual(BranchGenerator.DefaultMacroFixtureSeed, snapshot.branchSeed);

                snapshot.currentRoomId = BranchRoomId.West.Value;
                branch.InitializeFromSnapshot(ImportSampleRoom(), sessionState, snapshot);

                Assert.AreEqual(BranchGenerator.MacroFixtureBranchId, branch.State.Graph.BranchId);
                Assert.AreEqual(BranchRoomId.West, branch.State.CurrentRoomId);
                Assert.AreEqual("combat_macro_block_2x2", branch.CurrentRoomAsset.Id);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LegacyM7SnapshotStillRestoresFiveRoomSingleRoomBranch()
        {
            var root = CreateBranchHarness(out var branch, out _, out _, out var sessionState);
            try
            {
                var snapshot = new Hollow.Persistence.RunSaveSnapshot
                {
                    runId = "legacy",
                    branchId = BranchGenerator.LegacyFiveRoomBranchId,
                    currentRoomId = BranchRoomId.East.Value
                };

                branch.InitializeFromSnapshot(ImportSampleRoom(), sessionState, snapshot);

                Assert.AreEqual(BranchGenerator.LegacyFiveRoomBranchId, branch.State.Graph.BranchId);
                Assert.AreEqual(BranchRoomId.East, branch.State.CurrentRoomId);
                Assert.AreEqual("combat_single_sample", branch.CurrentRoomAsset.Id);
                Assert.AreEqual(4, branch.CurrentRoomAsset.DoorPorts.Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MacroBranchRewardsAndHubCompletionRemainCompatible()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out var player, out _);
            try
            {
                ClearCurrentRoom(combat);
                foreach (var direction in new[] { "north", "south", "east", "west" })
                {
                    Assert.IsTrue(branch.TryTraverse(direction));
                    ClearCurrentRoom(combat);
                    Assert.IsNotNull(branch.CurrentRewardPickup);
                    player.transform.localPosition = branch.CurrentRewardPickup.transform.localPosition;
                    Assert.IsTrue(branch.TryInteract());
                    Assert.IsTrue(branch.TryTraverse(Opposite(direction)));
                }

                Assert.IsTrue(branch.State.AreAllRoomsCleared());
                Assert.IsTrue(branch.State.AreAllRewardsClaimed());
                Assert.AreEqual(4, branch.RewardCounter.ClaimedRewards);
                Assert.AreEqual(40, branch.RunEconomy.RunSouls);
                Assert.IsNotNull(branch.CurrentHubPortal);
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
            var root = new GameObject("M14BranchHarness");

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
            enemyPrefab.AddComponent<ChaserEnemyController>();

            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.name = "ProjectileBase_TestPrefab";
            projectilePrefab.transform.SetParent(root.transform, false);
            projectilePrefab.SetActive(false);
            projectilePrefab.AddComponent<ProjectileController>();

            combat = root.AddComponent<RoomCombatController>();
            combat.Configure(enemyPrefab, projectilePrefab, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());

            branch = root.AddComponent<BranchSessionController>();
            branch.Configure(null, null);
            branch.ConfigureTemplateCatalog(AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(CatalogPath), BranchGenerator.DefaultMacroFixtureSeed);
            sessionState = GameSessionState.Create(RuntimeSessionMode.ProfileBacked, HollowPlatformKind.WindowsStandard3D, null, Vector3.zero);
            branch.Initialize(ImportSampleRoom(), sessionState);
            return root;
        }

        private static BranchSessionContent LoadContent()
        {
            var content = BranchSessionContent.Create(
                ImportSampleRoom(),
                AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(CatalogPath),
                BranchGenerator.DefaultMacroFixtureSeed,
                out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            Assert.IsTrue(content.HasMacroFixturePool);
            return content;
        }

        private static ImportedRoomRuntimeAsset ImportSampleRoom()
        {
            return HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
        }

        private static void AssertRoomAsset(BranchFloorGraph graph, BranchRoomId roomId, string assetId, int occupiedCells)
        {
            Assert.IsTrue(graph.TryGetRoom(roomId, out var room));
            Assert.AreEqual(assetId, room.RuntimeRoomAssetId);
            Assert.AreEqual(occupiedCells, room.Footprint.OccupiedCellCount);
        }

        private static void AssertPortConnection(BranchFloorGraph graph, BranchRoomId from, string fromPortId, BranchRoomId to, string toPortId)
        {
            Assert.IsTrue(graph.TryGetConnectionByPort(from, fromPortId, out var connection), $"{from}.{fromPortId}");
            Assert.AreEqual(to, connection.ToRoomId);
            Assert.AreEqual(toPortId, connection.ToPortId);
            Assert.IsTrue(connection.HasExplicitPorts);
        }

        private static void AssertMiniMapCells(BranchMiniMapModel model, BranchRoomId roomId, int expectedCells)
        {
            var node = model.Nodes.Single(candidate => candidate.Id == roomId);
            Assert.AreEqual(expectedCells, node.OccupiedCells.Count);
        }

        private static void AddTestRoom(BranchFloorGraph graph, BranchRoomId roomId, Vector2Int primaryCell, ImportedRoomRuntimeAsset asset)
        {
            graph.AddRoom(new BranchRoomState(
                roomId,
                primaryCell,
                new BranchRoomInstanceId(roomId.Value),
                asset.Id,
                PlaceFootprint(asset.Footprint, primaryCell),
                BranchRoomRole.Combat));
        }

        private static RoomInstanceFootprint PlaceFootprint(RoomInstanceFootprint source, Vector2Int primaryCell)
        {
            var offset = primaryCell - source.PrimaryCell;
            return new RoomInstanceFootprint(
                primaryCell,
                source.OccupiedCells.Select(cell => cell + offset).ToArray(),
                source.ChunkBasisTiles);
        }

        private static ImportedRoomRuntimeAsset WithPortKind(ImportedRoomRuntimeAsset source, string portId, string kind)
        {
            var ports = source.DoorPorts.Select(port => new RoomDoorPort(
                    port.Id,
                    port.Direction,
                    port.LaneIndex,
                    port.HostCell,
                    port.GridEdgeCenter,
                    port.Position,
                    port.Id == portId ? kind : port.Kind))
                .ToArray();
            return new ImportedRoomRuntimeAsset(
                $"{source.Id}_{kind}_{portId}",
                source.DisplayName,
                source.Layout,
                source.Footprint,
                ports,
                source.EnemySpawns,
                source.ItemSpawns,
                source.SafeStart,
                source.Decor,
                source.SourceManifest);
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

        private static string Opposite(string direction)
        {
            return direction switch
            {
                "north" => "south",
                "south" => "north",
                "east" => "west",
                "west" => "east",
                _ => string.Empty
            };
        }
    }
}
