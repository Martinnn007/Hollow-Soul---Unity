using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Presentation;
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
        public void BranchDoorVisualsHideAuthoredPortsWithoutConnections()
        {
            var root = new GameObject("M6SparseDoorVisualHarness");
            try
            {
                var asset = ImportSampleRoom();
                var roomObject = new GameObject("RoomRuntimeRoot");
                roomObject.transform.SetParent(root.transform, false);
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(asset);

                var graph = new BranchFloorGraph("sparse_door_graph", 1);
                graph.AddRoom(new BranchRoomState(BranchRoomId.Origin, Vector2Int.zero, new BranchRoomInstanceId("origin"), asset.Id, null, BranchRoomRole.Origin));
                graph.AddRoom(new BranchRoomState(BranchRoomId.North, new Vector2Int(0, -1), new BranchRoomInstanceId("north"), asset.Id, null, BranchRoomRole.Combat));
                graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south", "north_0", "south_0");
                graph.GetRoom(BranchRoomId.Origin).MarkCleared();

                var branch = root.AddComponent<BranchSessionController>();
                SetPrivateField(branch, "roomRuntimeRoot", room);
                SetState(branch, BranchSessionState.Create(graph));
                InvokePrivate(branch, "UpdateDoorVisuals");

                Assert.IsTrue(FindDoorAnchor(room.transform, "north_0").gameObject.activeSelf);
                Assert.IsFalse(FindDoorAnchor(room.transform, "south_0").gameObject.activeSelf);
                Assert.IsFalse(FindDoorAnchor(room.transform, "east_0").gameObject.activeSelf);
                Assert.IsFalse(FindDoorAnchor(room.transform, "west_0").gameObject.activeSelf);
                Assert.IsFalse(room.GetComponentsInChildren<PresentationVisualMarker>()
                    .Any(marker => marker.Role == PresentationPrefabRole.DoorUnavailable));
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
        public void TraversalPreservesDamagedPlayerHealth()
        {
            var root = CreateBranchHarness(out var branch, out var combat, out _);
            try
            {
                combat.PlayerHealth.Restore(combat.PlayerHealth.MaxHealth, 2);

                Assert.IsTrue(branch.TryTraverse("north"));

                Assert.AreEqual(BranchRoomId.North, branch.State.CurrentRoomId);
                Assert.AreEqual(2, combat.PlayerHealth.CurrentHealth);
                Assert.AreEqual(2, branch.CreatePlayerBuildHudModel().CurrentHealth);
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

                var contentRoot = shapeRoot.Find("BranchMiniMap.ContentRoot") as RectTransform;
                Assert.IsNotNull(contentRoot);
                Assert.AreEqual(0f, Mathf.DeltaAngle(contentRoot.localEulerAngles.z, 45f), 0.001f);
                Assert.AreEqual(1f, contentRoot.localScale.x, 0.001f);
                Assert.AreEqual(1f, contentRoot.localScale.y, 0.001f);
                Assert.IsNotNull(shapeRoot.GetComponent<RectMask2D>());
                var mapPanel = canvasObject.GetComponentsInChildren<Image>(true)
                    .Single(image => image.name == "BranchMiniMap.MapPanel");
                Assert.IsNotNull(mapPanel.sprite);
                Assert.IsFalse(mapPanel.preserveAspect);

                var legacyCells = shapeRoot.GetComponentsInChildren<Image>(true)
                    .Where(image => image.name.StartsWith("MiniMapRoomCell_"))
                    .ToArray();
                Assert.AreEqual(0, legacyCells.Length);

                var footprints = shapeRoot.GetComponentsInChildren<RectTransform>(true)
                    .Where(rect => rect.name.StartsWith("MiniMapRoomFootprint_"))
                    .ToArray();
                Assert.GreaterOrEqual(footprints.Length, 5);

                var originCell = FindMiniMapRect(shapeRoot, "MiniMapRoomFootprintFill_origin_0_0");
                var northCell = FindMiniMapRect(shapeRoot, "MiniMapRoomFootprintFill_north_0_-1");
                var southCell = FindMiniMapRect(shapeRoot, "MiniMapRoomFootprintFill_south_0_1");
                Assert.IsNotNull(originCell);
                Assert.IsNotNull(northCell);
                Assert.IsNotNull(southCell);
                Assert.Less(northCell.anchoredPosition.y, originCell.anchoredPosition.y);
                Assert.Greater(southCell.anchoredPosition.y, originCell.anchoredPosition.y);
                var currentDot = FindMiniMapRect(shapeRoot, "MiniMapCurrentPositionDot");
                Assert.IsNotNull(currentDot);
                Assert.AreEqual(originCell.anchoredPosition.x, currentDot.anchoredPosition.x, 0.001f);
                Assert.AreEqual(originCell.anchoredPosition.y, currentDot.anchoredPosition.y, 0.001f);

                var originMarker = shapeRoot.GetComponentsInChildren<Text>(true)
                    .Single(text => text.name == "MiniMapMarker_O");
                Assert.AreEqual(0f, Mathf.DeltaAngle(originMarker.rectTransform.localEulerAngles.z, -45f), 0.001f);

                Assert.IsFalse(canvasObject.GetComponentsInChildren<Text>(true)
                    .Any(text => text.name == "BranchMiniMap.MapPanel.Text"));

                Assert.IsFalse(canvasObject.GetComponentsInChildren<Text>(true)
                    .Any(text => text.name.StartsWith("BranchMiniMap.") || text.text.Contains("Seed:")));
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
                var originDot = FindMiniMapRect(originShapeRoot, "MiniMapCurrentPositionDot");
                Assert.IsNotNull(originDot);

                ClearCurrentRoom(combat);
                Assert.IsTrue(branch.TryTraverse("east"));

                eastCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var eastController = eastCanvas.GetComponent<BranchMiniMapController>();
                eastController.Bind(branch);
                var eastShapeRoot = eastCanvas.GetComponentsInChildren<RectTransform>(true)
                    .Single(rect => rect.name == "BranchMiniMap.ShapeRoot");
                var eastCell = FindMiniMapRect(eastShapeRoot, "MiniMapRoomFootprintFill_east_1_0");
                var eastDot = FindMiniMapRect(eastShapeRoot, "MiniMapCurrentPositionDot");
                var originCellAfterMove = FindMiniMapRect(eastShapeRoot, "MiniMapRoomFootprintFill_origin_0_0");

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

        private static Transform FindDoorAnchor(Transform root, string portId)
        {
            return root.GetComponentsInChildren<Transform>(includeInactive: true)
                .Single(transform => transform.name == $"doorAnchorActive.{portId}");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private static void SetState(BranchSessionController branch, BranchSessionState state)
        {
            var field = typeof(BranchSessionController).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, nameof(BranchSessionController.State));
            field.SetValue(branch, state);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(target, null);
        }

        private static RectTransform FindMiniMapRect(RectTransform root, string name)
        {
            return root.GetComponentsInChildren<RectTransform>(true)
                .SingleOrDefault(rect => rect.name == name);
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
