using System.IO;
using System.Linq;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Persistence;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using Hollow.UI.MainMenu;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone8RoomDesignerTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m8_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(tempRoot) && Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void MainMenuRoutesSelectedProfileToRoomDesignerWithoutStartingRun()
        {
            var profileStore = new JsonProfileStore(tempRoot);
            var selected = new SelectedProfileContext();
            var appState = new AppStateMachine();
            var viewModel = new MainMenuViewModel(profileStore, selected, appState);

            viewModel.SelectOrCreateSlot(0);
            var route = viewModel.OpenRoomDesigner();
            var summary = profileStore.LoadSlotSummaries()[0];

            Assert.AreEqual(AppShellRoute.RoomDesigner, route);
            Assert.AreEqual(AppShellRoute.RoomDesigner, appState.CurrentRoute);
            Assert.AreEqual(0, summary.TotalRuns);
            Assert.IsFalse(summary.HasActiveRun);
        }

        [Test]
        public void StoreCreatesDuplicatesAndDeletesPerSlotWithoutProfileMutation()
        {
            var profileStore = new JsonProfileStore(tempRoot);
            var slotId = new ProfileSlotId(1);
            profileStore.CreateOrLoadProfile(slotId, "Designer");
            profileStore.MarkRunStarted(slotId);
            profileStore.SaveActiveRun(slotId, new RunSaveSnapshot { runId = "active-run", currentRoomId = "north" });

            var store = new RoomDesignerStore(tempRoot);
            var drafts = store.LoadDrafts(slotId);
            var duplicate = store.DuplicateDraft(slotId, drafts[0].projectId);
            store.DeleteDraft(slotId, duplicate.projectId);
            var summary = profileStore.LoadSlotSummaries()[1];

            Assert.AreEqual(1, drafts.Count);
            Assert.AreNotEqual(drafts[0].projectId, duplicate.projectId);
            Assert.AreEqual(1, store.LoadDrafts(slotId).Count);
            Assert.IsTrue(summary.HasActiveRun);
            Assert.AreEqual(1, summary.TotalRuns);
            Assert.AreEqual(0, summary.BankedSouls);
        }

        [Test]
        public void DefaultDraftCompilesToValidHollowRuntimeV2()
        {
            var project = RoomDesignerProject.CreateDefault();
            var json = RoomDesignerCompiler.ExportRuntimeJson(project);

            Assert.IsFalse(json.Contains("grid.y"));
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(json, out var asset, out var error), error);
            Assert.AreEqual(13, asset.Layout.WidthTiles);
            Assert.AreEqual(7, asset.Layout.HeightTiles);
            Assert.AreEqual(4, asset.DoorPorts.Count);
            Assert.AreEqual(4, asset.EnemySpawns.Count);
            Assert.AreEqual(1, asset.ItemSpawns.Count);
            Assert.GreaterOrEqual(asset.Layout.Obstacles.Count, 1);
            Assert.IsTrue(asset.DoorPorts.All(port => port.Kind == RoomDesignerDoorKinds.Available));
        }

        [Test]
        public void ControllerAppliesKeyboardStyleInputToCursorToolsAndCells()
        {
            var root = new GameObject("RoomDesignerControllerTest");
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                var store = new RoomDesignerStore(tempRoot);
                var project = RoomDesignerProject.CreateDefault();
                controller.InitializeForTest(store, new ProfileSlotId(0), project);

                controller.ApplyInput(Input(moveX: 1));
                controller.ApplyInput(Input(toolDelta: 1));
                controller.ApplyInput(Input(place: true));
                var asset = RoomDesignerCompiler.Compile(controller.CurrentProject);

                Assert.AreEqual(1, controller.CursorX);
                Assert.AreEqual(RoomDesignerTool.Hole, controller.CurrentTool);
                Assert.IsTrue(asset.Layout.HoleTiles.Any(tile => tile.x == 1 && tile.y == 0));

                controller.ApplyInput(Input(erase: true));
                asset = RoomDesignerCompiler.Compile(controller.CurrentProject);
                Assert.IsFalse(asset.Layout.HoleTiles.Any(tile => tile.x == 1 && tile.y == 0));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ControllerTreatsPositiveZAsDesignerForwardInput()
        {
            var root = new GameObject("RoomDesignerForwardInputTest");
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), RoomDesignerProject.CreateDefault());

                controller.ApplyInput(Input(moveZ: 1), 0f);
                Assert.AreEqual(1, controller.CursorZ);

                controller.ApplyInput(Input(moveZ: -1), 1f);
                Assert.AreEqual(0, controller.CursorZ);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ExportersWriteProjectRuntimeAndUsdaCompanion()
        {
            var project = RoomDesignerProject.CreateDefault();
            var projectPath = RoomDesignerJsonExporter.ExportProject(project, tempRoot);
            var runtimePath = RoomDesignerJsonExporter.ExportRuntime(project, tempRoot);
            var usdaPath = RoomDesignerUsdaExporter.ExportScene(project, tempRoot);

            Assert.IsTrue(File.Exists(projectPath));
            Assert.IsTrue(File.Exists(runtimePath));
            Assert.IsTrue(File.Exists(usdaPath));
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(File.ReadAllText(runtimePath), out _, out var error), error);
            Assert.That(File.ReadAllText(usdaPath), Does.Contain("RoomTemplateRoot"));
            Assert.That(File.ReadAllText(usdaPath), Does.Contain("DoorAnchors"));
        }

        [Test]
        public void PlaytestHandoffUsesTransientRoomDesignerSession()
        {
            var root = new GameObject("RoomDesignerPlaytestTest");
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), RoomDesignerProject.CreateDefault());
                controller.PlaytestCurrentDraft();

                Assert.IsTrue(RoomPlaytestHandoff.TryConsume(out var runtimeJson, out var mode, out var returnRoute));
                Assert.AreEqual(RuntimeSessionMode.TransientRoomDesignerPlaytest, mode);
                Assert.AreEqual(AppShellRoute.RoomDesigner, returnRoute);
                Assert.IsTrue(HollowRuntimeV2Importer.TryImport(runtimeJson, out _, out var error), error);
                Assert.IsFalse(TransientSessionGuard.CanPersist(mode, hasProfile: true));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static RoomDesignerInputSnapshot Input(
            int moveX = 0,
            int moveZ = 0,
            int toolDelta = 0,
            int layerDelta = 0,
            bool place = false,
            bool erase = false)
        {
            return new RoomDesignerInputSnapshot(moveX, moveZ, toolDelta, layerDelta, place, erase, false, false, false, false, false, false);
        }
    }
}
