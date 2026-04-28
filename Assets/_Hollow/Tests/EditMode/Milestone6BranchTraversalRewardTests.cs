using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Rooms;
using Hollow.UI.Shell;
using Hollow.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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
        public void StarterRoomAllowsImmediateTraversal()
        {
            var root = CreateBranchHarness(out var branch, out _, out _);
            try
            {
                Assert.IsTrue(branch.State.CurrentRoom.IsCleared);
                Assert.IsTrue(branch.TryTraverse("north"));
                Assert.AreEqual(BranchRoomId.North, branch.State.CurrentRoomId);
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
                Assert.IsTrue(model.Nodes.All(node => node.IsRevealed));
                Assert.IsTrue(model.Nodes.All(node => node.OccupiedCells.Count == 1));
                Assert.AreEqual(4, model.Connections.Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MiniMapControllerBuildsShapeMapGraphicsInsteadOfAsciiTokens()
        {
            var root = CreateBranchHarness(out var branch, out _, out _);
            var canvasObject = new GameObject("PlatformShellCanvas", typeof(RectTransform), typeof(Canvas), typeof(BranchMiniMapController));
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var controller = canvasObject.GetComponent<BranchMiniMapController>();

                controller.Bind(branch);

                var shapeRoot = canvasObject.GetComponentsInChildren<RectTransform>(true)
                    .SingleOrDefault(rect => rect.name == "BranchMiniMap.ShapeRoot");
                Assert.IsNotNull(shapeRoot);

                var cells = shapeRoot.GetComponentsInChildren<Image>(true)
                    .Where(image => image.name.StartsWith("MiniMapRoomCell_"))
                    .ToArray();
                Assert.GreaterOrEqual(cells.Length, 5);

                var originCell = shapeRoot.Find("MiniMapRoomCell_origin_0_0") as RectTransform;
                var northCell = shapeRoot.Find("MiniMapRoomCell_north_0_-1") as RectTransform;
                var southCell = shapeRoot.Find("MiniMapRoomCell_south_0_1") as RectTransform;
                Assert.IsNotNull(originCell);
                Assert.IsNotNull(northCell);
                Assert.IsNotNull(southCell);
                Assert.Less(northCell.anchoredPosition.y, originCell.anchoredPosition.y);
                Assert.Greater(southCell.anchoredPosition.y, originCell.anchoredPosition.y);
                var currentDot = shapeRoot.Find("MiniMapCurrentPositionDot") as RectTransform;
                Assert.IsNotNull(currentDot);
                Assert.AreEqual(originCell.anchoredPosition.x, currentDot.anchoredPosition.x, 0.001f);
                Assert.AreEqual(originCell.anchoredPosition.y, currentDot.anchoredPosition.y, 0.001f);

                var mapText = canvasObject.GetComponentsInChildren<Text>(true)
                    .Single(text => text.name == "BranchMiniMap.MapPanel.Text");
                Assert.IsTrue(mapText.text.Contains("Branch Map"));
                Assert.IsFalse(mapText.text.Contains("[C]"));

                var economyText = canvasObject.GetComponentsInChildren<Text>(true)
                    .Single(text => text.name == "BranchMiniMap.EconomyPanel.Text");
                Assert.IsTrue(economyText.text.Contains("Seed:"));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MiniMapRecentersCurrentRoomAfterTraversal()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out _);
            var originCanvas = new GameObject("OriginCanvas", typeof(RectTransform), typeof(Canvas), typeof(BranchMiniMapController));
            var eastCanvas = new GameObject("EastCanvas", typeof(RectTransform), typeof(Canvas), typeof(BranchMiniMapController));
            try
            {
                originCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var originController = originCanvas.GetComponent<BranchMiniMapController>();
                originController.Bind(branch);
                var originShapeRoot = originCanvas.GetComponentsInChildren<RectTransform>(true)
                    .Single(rect => rect.name == "BranchMiniMap.ShapeRoot");
                var originDot = originShapeRoot.Find("MiniMapCurrentPositionDot") as RectTransform;
                Assert.IsNotNull(originDot);

                ClearCurrentRoom(combat);
                Assert.IsTrue(branch.TryTraverse("east"));

                eastCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var eastController = eastCanvas.GetComponent<BranchMiniMapController>();
                eastController.Bind(branch);
                var eastShapeRoot = eastCanvas.GetComponentsInChildren<RectTransform>(true)
                    .Single(rect => rect.name == "BranchMiniMap.ShapeRoot");
                var eastCell = eastShapeRoot.Find("MiniMapRoomCell_east_1_0") as RectTransform;
                var eastDot = eastShapeRoot.Find("MiniMapCurrentPositionDot") as RectTransform;
                var originCellAfterMove = eastShapeRoot.Find("MiniMapRoomCell_origin_0_0") as RectTransform;

                Assert.IsNotNull(eastCell);
                Assert.IsNotNull(eastDot);
                Assert.IsNotNull(originCellAfterMove);
                Assert.AreEqual(originDot.anchoredPosition.x, eastDot.anchoredPosition.x, 0.001f);
                Assert.AreEqual(originDot.anchoredPosition.y, eastDot.anchoredPosition.y, 0.001f);
                Assert.AreEqual(eastCell.anchoredPosition.x, eastDot.anchoredPosition.x, 0.001f);
                Assert.AreEqual(eastCell.anchoredPosition.y, eastDot.anchoredPosition.y, 0.001f);
                Assert.Less(originCellAfterMove.anchoredPosition.x, eastCell.anchoredPosition.x);
            }
            finally
            {
                Object.DestroyImmediate(eastCanvas);
                Object.DestroyImmediate(originCanvas);
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
