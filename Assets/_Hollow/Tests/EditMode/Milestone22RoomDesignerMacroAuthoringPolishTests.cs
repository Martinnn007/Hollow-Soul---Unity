using System.IO;
using System.Linq;
using Hollow.Persistence;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone22RoomDesignerMacroAuthoringPolishTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m22_tests", Path.GetRandomFileName());
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
        public void DraftLibraryCreatesPresetDraftsWithoutMutatingFootprints()
        {
            var store = new RoomDesignerStore(tempRoot);
            var library = new RoomDesignerDraftLibraryState(store, new ProfileSlotId(0));

            var wide = library.CreateDraft(RoomDesignerFootprintPreset.Wide2x1);
            var lRoom = library.CreateDraft(RoomDesignerFootprintPreset.L3Cell);
            var duplicate = library.DuplicateDraft(wide.projectId);

            Assert.AreEqual(RoomDesignerFootprintPreset.Wide2x1, wide.footprintPreset);
            Assert.AreEqual(26, wide.widthTiles);
            Assert.AreEqual(RoomDesignerFootprintPreset.L3Cell, lRoom.footprintPreset);
            Assert.AreEqual(RoomDesignerFootprintPreset.Wide2x1, duplicate.footprintPreset);
            Assert.AreEqual(4, store.LoadDrafts(new ProfileSlotId(0)).Count);
        }

        [Test]
        public void ValidationBlocksMissingSafeStartPortsEnemiesAndInvalidPlacements()
        {
            var noSafeStart = RoomDesignerProject.CreateDefault();
            noSafeStart.markers.RemoveAll(marker => marker.kind == RoomDesignerMarkerKinds.SafeStart);
            Assert.IsTrue(RoomDesignerDraftValidator.Validate(noSafeStart).Errors.Any(error => error.Contains("safe-start")));

            var noPorts = RoomDesignerProject.CreateDefault();
            foreach (var port in noPorts.doorPorts)
            {
                port.state = RoomDesignerDoorKinds.Inactive;
            }
            Assert.IsTrue(RoomDesignerDraftValidator.Validate(noPorts).Errors.Any(error => error.Contains("door port")));

            var noEnemies = RoomDesignerProject.CreateDefault();
            noEnemies.markers.RemoveAll(marker => RoomDesignerMarkerKinds.IsEnemy(marker.kind));
            Assert.IsTrue(RoomDesignerDraftValidator.Validate(noEnemies).Errors.Any(error => error.Contains("enemy spawn")));

            var markerOnHole = RoomDesignerProject.CreateDefault();
            markerOnHole.cells.RemoveAll(cell => cell.x == 0 && cell.z == 0 && cell.layer == 0);
            markerOnHole.cells.Add(new RoomDesignerCell(0, 0, 0, RoomDesignerCellKinds.Hole));
            markerOnHole.markers.RemoveAll(marker => marker.kind == RoomDesignerMarkerKinds.SafeStart);
            markerOnHole.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, 0, 0, 0));
            Assert.IsTrue(RoomDesignerDraftValidator.Validate(markerOnHole).Errors.Any(error => error.Contains("hole")));
        }

        [Test]
        public void CompilerOmitsInactivePortsAndExportsEncounterReadyEnemyKinds()
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Wide2x1, "M22 Wide");
            project.doorPorts[0].state = RoomDesignerDoorKinds.Inactive;
            project.markers.RemoveAll(marker => RoomDesignerMarkerKinds.IsEnemy(marker.kind));
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_charger_00", RoomDesignerMarkerKinds.EnemyCharger, -2, 0, 0));
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_turret_00", RoomDesignerMarkerKinds.EnemyTurret, 2, 0, 0));

            var asset = RoomDesignerCompiler.Compile(project);

            Assert.AreEqual(project.doorPorts.Count - 1, asset.DoorPorts.Count);
            Assert.IsFalse(asset.DoorPorts.Any(port => port.Id == project.doorPorts[0].id));
            Assert.IsTrue(asset.EnemySpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.EnemyCharger));
            Assert.IsTrue(asset.EnemySpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.EnemyTurret));
        }

        [Test]
        public void ExportBundleWritesRuntimeProjectUsdaAndValidationReport()
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Block2x2, "M22 Export");
            var bundle = RoomDesignerExportBundle.Export(project, tempRoot);

            Assert.IsTrue(File.Exists(bundle.projectJsonPath));
            Assert.IsTrue(File.Exists(bundle.runtimeJsonPath));
            Assert.IsTrue(File.Exists(bundle.usdaPath));
            Assert.IsTrue(File.Exists(bundle.validationReportPath));
            Assert.IsTrue(bundle.validationReport.IsValid);
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(File.ReadAllText(bundle.runtimeJsonPath), out var asset, out var error), error);
            Assert.AreEqual(RoomFootprintShape.Block2x2, RoomFootprintShapeUtility.Classify(asset.Footprint));
        }

        [Test]
        public void ControllerShowsMacroHudBlocksInvalidPlaytestAndSupportsNewTools()
        {
            var root = new GameObject("M22RoomDesignerController");
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.L3Cell, "M22 Controller");
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), project);

                controller.SelectTool(RoomDesignerTool.EnemySplitter);
                controller.ApplyInput(new RoomDesignerInputSnapshot(1, 0, 0, 0, true, false, false, false, false, false, false, false));
                controller.SelectTool(RoomDesignerTool.InactiveDoor);
                controller.ApplyInput(new RoomDesignerInputSnapshot(0, 0, 0, 0, true, false, false, false, false, false, false, false));

                Assert.IsTrue(controller.CurrentProject.markers.Any(marker => marker.kind == RoomDesignerMarkerKinds.EnemySplitter));
                Assert.IsTrue(controller.CurrentProject.doorPorts.Any(port => port.state == RoomDesignerDoorKinds.Inactive));
                Assert.IsTrue(controller.LastValidationReport.IsValid, string.Join("; ", controller.LastValidationReport.Errors));

                controller.CurrentProject.markers.RemoveAll(marker => marker.kind == RoomDesignerMarkerKinds.SafeStart);
                controller.PlaytestCurrentDraft();
                Assert.IsFalse(controller.LastValidationReport.IsValid);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ControllerStartsOnTemplateScreenWithoutAutoCreatingHiddenDefaultDraft()
        {
            var root = new GameObject("RoomDesignerEntryController");
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                var store = new RoomDesignerStore(tempRoot);
                var slotId = new ProfileSlotId(0);

                controller.InitializeLibraryForTest(store, slotId);

                Assert.AreEqual(RoomDesignerMode.CreateTemplate, controller.Mode);
                Assert.IsNull(controller.CurrentProject);
                Assert.AreEqual(0, store.LoadExistingDrafts(slotId).Count);
                Assert.IsNotNull(controller.LibraryViewModel);
                Assert.AreEqual(5, controller.LibraryViewModel.Templates.Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ControllerCreatesOpensAndDeletesDraftsThroughLibraryStates()
        {
            var root = new GameObject("RoomDesignerLibraryFlowController");
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                var store = new RoomDesignerStore(tempRoot);
                var slotId = new ProfileSlotId(0);
                controller.InitializeLibraryForTest(store, slotId);

                controller.ApplyInput(Input(moveZ: 1), 1f);
                controller.ApplyInput(Input(place: true), 2f);

                Assert.AreEqual(RoomDesignerMode.Editing, controller.Mode);
                Assert.AreEqual(RoomDesignerFootprintPreset.Wide2x1, controller.CurrentProject.footprintPreset);
                var createdId = controller.CurrentProject.projectId;

                controller.ShowLibrary();
                Assert.AreEqual(RoomDesignerMode.Library, controller.Mode);
                controller.OpenSelectedDraft();
                Assert.AreEqual(createdId, controller.CurrentProject.projectId);

                controller.ShowLibrary();
                controller.RequestDeleteDraft();
                Assert.AreEqual(RoomDesignerMode.ConfirmDelete, controller.Mode);
                controller.ConfirmDeleteDraft();

                Assert.AreEqual(RoomDesignerMode.CreateTemplate, controller.Mode);
                Assert.AreEqual(0, store.LoadExistingDrafts(slotId).Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ControllerCameraTargetFollowsActiveCursorCell()
        {
            var root = new GameObject("RoomDesignerCameraController");
            var cameraObject = new GameObject("MainCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Block2x2, "Camera Draft"));

                controller.ApplyInput(Input(moveX: 1), 1f);
                controller.ApplyInput(Input(moveZ: 1), 2f);

                Assert.AreEqual(new Vector3(1f, 0f, 1f), controller.CameraTargetPosition);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void DisplayNamesCoverCurrentDesignerToolsAndSemanticKinds()
        {
            foreach (RoomDesignerTool tool in System.Enum.GetValues(typeof(RoomDesignerTool)))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(RoomDesignerDisplayNames.ForTool(tool)), tool.ToString());
                Assert.IsFalse(string.IsNullOrWhiteSpace(RoomDesignerDisplayNames.ForToolIcon(tool)), tool.ToString());
            }

            Assert.AreEqual("Ground", RoomDesignerDisplayNames.ForCellKind(RoomDesignerCellKinds.Ground));
            Assert.AreEqual("Hole", RoomDesignerDisplayNames.ForCellKind(RoomDesignerCellKinds.Hole));
            Assert.AreEqual("Rock", RoomDesignerDisplayNames.ForCellKind(RoomDesignerCellKinds.Rock));
            Assert.AreEqual("Start", RoomDesignerDisplayNames.ForMarkerKind(RoomDesignerMarkerKinds.SafeStart));
            Assert.AreEqual("Flying", RoomDesignerDisplayNames.ForMarkerKind(RoomDesignerMarkerKinds.EnemyFlying));
            Assert.AreEqual("Charger", RoomDesignerDisplayNames.ForMarkerKind(RoomDesignerMarkerKinds.EnemyCharger));
            Assert.AreEqual("N0 Door", RoomDesignerDisplayNames.ForDoor(RoomDesignerDoorPortState.Create("north", 0, 0f, -3.5f, RoomDesignerDoorKinds.Door)));
        }

        [Test]
        public void ControllerUsesImportantShortLabelsWithoutGroundTileSpam()
        {
            var root = new GameObject("RoomDesignerShortLabelController");
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), RoomDesignerProject.CreateDefault());
                controller.SelectTool(RoomDesignerTool.Hole);
                controller.ApplyInput(Input(place: true), 1f);

                var labels = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Exclude)
                    .Where(text => text.transform.IsChildOf(root.transform))
                    .Select(text => text.text)
                    .ToArray();

                Assert.IsFalse(labels.Contains(RoomDesignerCellKinds.Ground));
                Assert.IsFalse(labels.Contains("Ground"));
                Assert.IsTrue(labels.Contains("Hole"));
                Assert.IsTrue(labels.Contains("Rock"));
                Assert.IsTrue(labels.Contains("Start"));
                Assert.IsTrue(labels.Any(label => label.Contains("Port") || label.Contains("Door") || label.Contains("Secret")));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ControllerShowsClickableFullToolToolbarAndHighlightsCurrentTool()
        {
            var root = new GameObject("RoomDesignerToolbarController");
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), RoomDesignerProject.CreateDefault());

                var toolbar = GameObject.Find("RoomDesignerToolToolbar");
                Assert.IsNotNull(toolbar);
                Assert.IsTrue(toolbar.activeSelf);
                Assert.AreEqual(System.Enum.GetValues(typeof(RoomDesignerTool)).Length, toolbar.transform.childCount);

                var groundTile = GameObject.Find("ToolTile_Ground");
                Assert.IsNotNull(groundTile);
                Assert.Greater(groundTile.GetComponent<UnityEngine.UI.Outline>().effectColor.g, 0.9f);

                var turretTile = GameObject.Find("ToolTile_EnemyTurret");
                Assert.IsNotNull(turretTile);
                turretTile.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.AreEqual(RoomDesignerTool.EnemyTurret, controller.CurrentTool);

                turretTile = GameObject.Find("ToolTile_EnemyTurret");
                Assert.IsNotNull(turretTile);
                Assert.Greater(turretTile.GetComponent<UnityEngine.UI.Outline>().effectColor.g, 0.9f);
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
