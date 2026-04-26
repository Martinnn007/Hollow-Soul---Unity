using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Rooms;
using Hollow.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone6BranchTraversalRewardTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void FiveRoomCrossHasExpectedRoomsAndConnections()
        {
            var asset = ImportSampleRoom();
            var graph = BranchGenerator.CreateFiveRoomCross(asset);

            Assert.AreEqual(5, graph.RoomCount);
            Assert.AreEqual(8, graph.Connections.Count);
            Assert.IsTrue(graph.TryGetConnection(BranchRoomId.Origin, "north", out _));
            Assert.IsTrue(graph.TryGetConnection(BranchRoomId.Origin, "south", out _));
            Assert.IsTrue(graph.TryGetConnection(BranchRoomId.Origin, "east", out _));
            Assert.IsTrue(graph.TryGetConnection(BranchRoomId.Origin, "west", out _));
            Assert.AreEqual(1, graph.ConnectionsFrom(BranchRoomId.North).Count);
        }

        [Test]
        public void TraversalFailsBeforeRoomClear()
        {
            var root = CreateBranchHarness(out var branch, out _, out _);
            try
            {
                Assert.IsFalse(branch.TryTraverse("north"));
                Assert.AreEqual(BranchRoomId.Origin, branch.State.CurrentRoomId);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TraversalSucceedsAfterClearAndPositionsPlayerInsideOppositeDoor()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out var player);
            try
            {
                ClearCurrentRoom(combat);

                Assert.IsTrue(branch.TryTraverse("north"));
                Assert.AreEqual(BranchRoomId.North, branch.State.CurrentRoomId);
                Assert.AreEqual(2.75f, player.transform.localPosition.z, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RevisitingClearedRoomDoesNotRespawnEnemies()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out _);
            try
            {
                ClearCurrentRoom(combat);
                Assert.IsTrue(branch.TryTraverse("north"));
                ClearCurrentRoom(combat);
                Assert.IsTrue(branch.TryTraverse("south"));

                Assert.AreEqual(BranchRoomId.Origin, branch.State.CurrentRoomId);
                Assert.AreEqual(RoomObjectiveState.Cleared, combat.ObjectiveState);
                Assert.AreEqual(0, combat.EnemiesRemaining());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RewardsCanBeClaimedOnceAndPortalAppearsWhenBranchComplete()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out var player);
            try
            {
                ClearCurrentRoom(combat);

                var directions = new[] { "north", "south", "east", "west" };
                for (var index = 0; index < directions.Length; index++)
                {
                    var direction = directions[index];
                    Assert.IsTrue(branch.TryTraverse(direction));
                    ClearCurrentRoom(combat);
                    Assert.IsNotNull(branch.CurrentRewardPickup);

                    player.transform.localPosition = branch.CurrentRewardPickup.transform.localPosition;
                    Assert.IsTrue(branch.TryInteract());
                    Assert.AreEqual(index + 1, branch.RewardCounter.ClaimedRewards);
                    Assert.IsNull(branch.CurrentRewardPickup);

                    if (index < directions.Length - 1)
                    {
                        Assert.IsTrue(branch.TryTraverse(Opposite(direction)));
                    }
                }

                Assert.IsTrue(branch.State.AreAllRoomsCleared());
                Assert.IsTrue(branch.State.AreAllRewardsClaimed());
                Assert.IsNotNull(branch.CurrentHubPortal);
                player.transform.localPosition = branch.CurrentHubPortal.transform.localPosition;
                Assert.IsTrue(branch.TryInteract());
                Assert.IsTrue(branch.HubReturnRequested);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MiniMapModelReportsCurrentVisitedClearedAndPendingReward()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out _);
            try
            {
                ClearCurrentRoom(combat);
                Assert.IsTrue(branch.TryTraverse("east"));
                ClearCurrentRoom(combat);

                var model = branch.CreateMiniMapModel();
                Assert.AreEqual(5, model.Nodes.Count);
                Assert.IsTrue(model.Nodes.Single(node => node.Id == BranchRoomId.East).IsCurrent);
                Assert.IsTrue(model.Nodes.Single(node => node.Id == BranchRoomId.East).HasPendingReward);
                Assert.IsTrue(model.Nodes.Single(node => node.Id == BranchRoomId.Origin).IsCleared);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateBranchHarness(
            out BranchSessionController branch,
            out RoomCombatController combat,
            out PlaceholderPlayerController player)
        {
            var root = new GameObject("M6BranchHarness");

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
            branch.Initialize(
                ImportSampleRoom(),
                GameSessionState.Create(RuntimeSessionMode.ProfileBacked, HollowPlatformKind.WindowsStandard3D, null, Vector3.zero));
            return root;
        }

        private static ImportedRoomRuntimeAsset ImportSampleRoom()
        {
            return HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
        }

        private static void ClearCurrentRoom(RoomCombatController combat)
        {
            foreach (var enemy in combat.Enemies)
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
