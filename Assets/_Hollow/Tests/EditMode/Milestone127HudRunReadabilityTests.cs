using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.UI.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone127HudRunReadabilityTests
    {
        [Test]
        public void MiniMapPanelBuildsPlainDarkRectangleWithLocationLabel()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas), typeof(BranchMiniMapController));
            try
            {
                var controller = canvasObject.GetComponent<BranchMiniMapController>();
                controller.Bind(null);

                var mapImage = controller.MapPanel.GetComponent<Image>();
                Assert.IsNull(mapImage.sprite);
                Assert.IsFalse(mapImage.preserveAspect);
                Assert.Greater(mapImage.color.a, 0.5f);
                Assert.Less(mapImage.color.r, 0.05f);
                Assert.IsNotNull(controller.LocationLabelText);
                Assert.AreEqual("BranchMiniMap.LocationLabel", controller.LocationLabelText.name);
                Assert.AreEqual(TextAnchor.UpperRight, controller.LocationLabelText.alignment);
                Assert.Less(controller.LocationLabelText.rectTransform.anchoredPosition.y, controller.MapPanel.anchoredPosition.y - controller.MapPanel.sizeDelta.y);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void MiniMapStillDrawsShapeCurrentRoomRewardDotAndMarkers()
        {
            var graph = new BranchFloorGraph("m127_minimap_shape", 127);
            var origin = new BranchRoomState(BranchRoomId.Origin, Vector2Int.zero, new BranchRoomInstanceId("origin"), "origin", null, BranchRoomRole.Origin);
            var treasure = new BranchRoomState(BranchRoomId.North, new Vector2Int(0, -1), new BranchRoomInstanceId("treasure"), "treasure", null, BranchRoomRole.Treasure);
            treasure.MarkRewardPending();
            graph.AddRoom(origin);
            graph.AddRoom(treasure);
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south");
            var model = new BranchMiniMapModel(BranchSessionState.Create(graph));

            var canvasObject = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas), typeof(BranchMiniMapController));
            try
            {
                var controller = canvasObject.GetComponent<BranchMiniMapController>();
                controller.Bind(null);
                controller.RebuildShapeMap(model);

                var shapeRoot = controller.ShapeRoot;
                Assert.IsNotNull(shapeRoot.Find("BranchMiniMap.ContentRoot"));
                Assert.IsNotNull(FindRect(shapeRoot, "MiniMapCurrentPositionDot"));
                Assert.IsNotNull(FindRect(shapeRoot, "MiniMapRewardDot"));
                Assert.IsTrue(shapeRoot.GetComponentsInChildren<Text>(true).Any(text => text.name == "MiniMapMarker_O"));
                Assert.IsTrue(shapeRoot.GetComponentsInChildren<Text>(true).Any(text => text.name == "MiniMapMarker_$"));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void LocationFormatterHandlesShipHubWorldBranchesAndFallbackBiome()
        {
            Assert.AreEqual(
                "Spaceship",
                RunLocationLabelFormatter.Format(new RunLocationLabelContext(true, false, false, 1, 1, RunWorldPhase.Legacy, Snapshot("World 1", string.Empty, string.Empty))));
            Assert.AreEqual(
                "Developer Lab",
                RunLocationLabelFormatter.Format(new RunLocationLabelContext(false, true, false, 1, 1, RunWorldPhase.Legacy, Snapshot("World 1", string.Empty, string.Empty))));
            Assert.AreEqual(
                "World 1 Hub",
                RunLocationLabelFormatter.Format(new RunLocationLabelContext(false, false, true, 1, 1, RunWorldPhase.Hub, Snapshot("World 1", "The Hollow Threshold", RoomBiomeIds.HollowThreshold))));
            Assert.AreEqual(
                "1-1: The Hollow Threshold",
                RunLocationLabelFormatter.Format(new RunLocationLabelContext(false, false, false, 1, 3, RunWorldPhase.Prologue, Snapshot("World 1", "The Hollow Threshold", RoomBiomeIds.HollowThreshold))));
            Assert.AreEqual(
                "1-2: The Hollow Threshold",
                RunLocationLabelFormatter.Format(new RunLocationLabelContext(false, false, false, 1, 2, RunWorldPhase.Branch, Snapshot("World 1", "The Hollow Threshold", RoomBiomeIds.HollowThreshold))));
            Assert.AreEqual(
                "2-1: Verdant Ruins",
                RunLocationLabelFormatter.Format(new RunLocationLabelContext(false, false, false, 2, 1, RunWorldPhase.Branch, Snapshot("World 2", string.Empty, RoomBiomeIds.VerdantRuins))));
        }

        [Test]
        public void DebugSpawnMenuDefaultsHiddenAndSourceHasNoScreenToggleButton()
        {
            var menuObject = new GameObject("DebugSpawnMenu", typeof(DebugSpawnMenuController));
            try
            {
                var menu = menuObject.GetComponent<DebugSpawnMenuController>();
                Assert.IsFalse(menu.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(menuObject);
            }

            var source = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Branches/DebugSpawnMenuController.cs");
            Assert.IsFalse(source.Contains("DrawToggleButton"));
            Assert.IsFalse(source.Contains("GUI.Button(rect"));
            StringAssert.Contains("ReadDebugSpawnMenuTogglePressed", source);
        }

        [Test]
        public void Milestone127LiveReportAndValidatorPass()
        {
            var report = Milestone127HudRunReadabilityAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.Greater(report.totalChecks, 10);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            Assert.IsTrue(Milestone127HudRunReadabilityValidator.Validate(exitOnFailure: false));
        }

        private static RunFramingSnapshot Snapshot(string title, string worldDisplayName, string biomeId)
        {
            return new RunFramingSnapshot(
                title,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                worldDisplayName,
                biomeId);
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            return root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rect => rect.name == name);
        }
    }
}
