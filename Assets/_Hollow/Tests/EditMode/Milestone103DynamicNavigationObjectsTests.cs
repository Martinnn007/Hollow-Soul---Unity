using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone103DynamicNavigationObjectsTests
    {
        [Test]
        public void InteractiveBlockerCreatesSafeCarvingObstacleAndDisablesOnDestroy()
        {
            var root = BuildHarness(out var room);
            try
            {
                var interactive = room.InteractiveObjectMarkers.Single(marker => marker.ObjectId == "barrel_0");
                Assert.IsTrue(interactive.TryGetComponent<RoomDynamicNavigationObjectMarker>(out var navigation));
                Assert.IsTrue(interactive.TryGetComponent<NavMeshObstacle>(out var obstacle));
                Assert.AreEqual(RoomDynamicNavigationObjectCategory.DynamicCarver, navigation.Category);
                Assert.IsTrue(navigation.CarvingActive, navigation.StatusSummary);
                Assert.IsTrue(obstacle.enabled);
                Assert.IsTrue(obstacle.carving);

                interactive.MarkDestroyed();

                Assert.IsFalse(navigation.CarvingActive, navigation.StatusSummary);
                Assert.IsFalse(obstacle.enabled);
                StringAssert.Contains("destroyed", navigation.LastReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DoorStatesToggleNavigationCarving()
        {
            var root = BuildHarness(out var room);
            try
            {
                var doorNavigation = room.DynamicNavigationObjects.Single(marker => marker.ObjectId == "east_0");
                Assert.AreEqual(RoomDynamicNavigationObjectCategory.Door, doorNavigation.Category);
                Assert.IsFalse(doorNavigation.CarvingActive, doorNavigation.StatusSummary);
                Assert.IsTrue(doorNavigation.TryGetComponent<NavMeshObstacle>(out var obstacle));
                Assert.IsFalse(obstacle.enabled);

                room.SetDoorVisualStateById("east_0", RoomDoorVisualState.Locked);

                Assert.IsTrue(doorNavigation.CarvingActive, doorNavigation.StatusSummary);
                Assert.IsTrue(obstacle.enabled);
                StringAssert.Contains("locked", doorNavigation.LastReason);

                room.SetDoorVisualState("east", RoomDoorVisualState.Active);

                Assert.IsFalse(doorNavigation.CarvingActive, doorNavigation.StatusSummary);
                Assert.IsFalse(obstacle.enabled);
                StringAssert.Contains("active", doorNavigation.LastReason);

                room.SetDoorVisualStateById("east_0", RoomDoorVisualState.Locked);
                Assert.IsTrue(doorNavigation.CarvingActive, doorNavigation.StatusSummary);

                room.SetDoorVisualStateById("east_0", RoomDoorVisualState.Cleared);
                room.SetDoorVisualStateById("east_0", RoomDoorVisualState.Cleared);
                Assert.IsFalse(doorNavigation.CarvingActive, doorNavigation.StatusSummary);
                Assert.IsFalse(obstacle.enabled);
                StringAssert.Contains("cleared", doorNavigation.LastReason);

                room.SetDoorVisualStateById("east_0", RoomDoorVisualState.Unavailable);
                Assert.IsTrue(doorNavigation.CarvingActive, doorNavigation.StatusSummary);

                room.SetDoorVisualState("east", RoomDoorVisualState.Active);
                room.SetDoorVisualState("east", RoomDoorVisualState.Active);
                Assert.IsFalse(doorNavigation.CarvingActive, doorNavigation.StatusSummary);
                Assert.IsFalse(obstacle.enabled);
                StringAssert.Contains("active", doorNavigation.LastReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StaticRocksAndHolesAreRegisteredAsBakedNavigationDebugObjects()
        {
            var root = BuildHarness(out var room);
            try
            {
                var rock = room.DynamicNavigationObjects.Single(marker => marker.ObjectId == "rock_0");
                var hole = room.DynamicNavigationObjects.Single(marker => marker.ObjectId == "hole_1_0");

                Assert.AreEqual(RoomDynamicNavigationObjectCategory.StaticBakedBlocker, rock.Category);
                Assert.AreEqual(RoomDynamicNavigationObjectCategory.HoleBakedBlocker, hole.Category);
                Assert.IsFalse(rock.UsesCarving);
                Assert.IsFalse(hole.UsesCarving);
                StringAssert.Contains("baked", rock.LastReason);
                StringAssert.Contains("baked", hole.LastReason);
                Assert.IsNull(rock.transform.Find("DynamicNavigationDebugLabel"));

                room.SetDynamicNavigationDebugLabelsVisible(true);

                var debugLabel = rock.transform.Find("DynamicNavigationDebugLabel");
                Assert.IsNotNull(debugLabel);
                Assert.IsTrue(debugLabel.gameObject.activeSelf);

                room.SetDynamicNavigationDebugLabelsVisible(false);

                Assert.IsFalse(debugLabel.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsReportAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone103DynamicNavigationObjectsAssetGenerator.DocsPath), Milestone103DynamicNavigationObjectsAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone103DynamicNavigationObjectsAssetGenerator.ReportPath), Milestone103DynamicNavigationObjectsAssetGenerator.ReportPath);
            Assert.IsEmpty(Milestone103DynamicNavigationObjectsValidator.CollectFailures());
        }

        private static GameObject BuildHarness(out RoomRuntimeRoot room)
        {
            var root = new GameObject("M103DynamicNavigationHarness");
            room = root.AddComponent<RoomRuntimeRoot>();
            LogAssert.Expect(LogType.Warning, new Regex("dev-only runtime Unity NavMesh fallback"));
            room.BuildFrom(CreateRoomAsset(), RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake);
            return root;
        }

        private static ImportedRoomRuntimeAsset CreateRoomAsset()
        {
            var layout = new RoomLayout(
                7,
                5,
                Rect.MinMaxRect(-3.5f, -2.5f, 3.5f, 2.5f),
                new List<Vector2Int>(),
                new List<Vector2Int> { new(1, 0) },
                new[]
                {
                    new RoomLayoutFloorRegion("full_floor", Vector3.zero, new Vector2(3.5f, 2.5f))
                },
                new[]
                {
                    new RoomLayoutObstacle("rock_0", "rock", new Vector3(-1f, 0.5f, 0f), new Vector3(1f, 1f, 1f), true)
                });

            return new ImportedRoomRuntimeAsset(
                "m103_dynamic_navigation_room",
                "M103 Dynamic Navigation Room",
                layout,
                new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(7, 5)),
                new[]
                {
                    new RoomDoorPort("east_0", "east", 0, Vector2Int.zero, new Vector2(3.5f, 0f), new Vector3(3.5f, 0f, 0f), "door")
                },
                new List<ImportedSpawnPoint>(),
                new List<ImportedSpawnPoint>(),
                new ImportedSpawnPoint
                {
                    id = "safe_start",
                    kind = "spawn_point_safeStart",
                    position = new ImportedVector3 { x = 0f, y = 0f, z = 0f }
                },
                new List<ImportedRoomHazard>(),
                new[]
                {
                    new ImportedRoomInteractiveObject
                    {
                        id = "barrel_0",
                        kind = RoomInteractiveObjectKind.StandardBarrel,
                        center = new ImportedVector3 { x = 0f, y = 0.5f, z = 1f },
                        size = new ImportedVector3 { x = 0.75f, y = 1f, z = 0.75f },
                        blocksMovement = true,
                        blocksProjectiles = true
                    }
                },
                new List<ImportedRoomDecor>(),
                null);
        }
    }
}
