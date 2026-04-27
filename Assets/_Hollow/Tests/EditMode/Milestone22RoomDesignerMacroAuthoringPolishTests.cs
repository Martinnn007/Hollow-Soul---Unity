using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Persistence;
using Hollow.Presentation;
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
        public void ControllerAlignsCursorFlatTilesAndDoorBottomsToGridOrigin()
        {
            var root = new GameObject("RoomDesignerGridOriginController");
            try
            {
                var project = RoomDesignerProject.CreateDefault();
                project.cells.RemoveAll(cell => cell.x == 1 && cell.z == 0 && cell.layer == 0);
                project.cells.Add(new RoomDesignerCell(1, 0, 0, RoomDesignerCellKinds.Hole));

                var controller = root.AddComponent<RoomDesignerController>();
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), project);

                AssertBottomOnGrid(FindChild(root, "cursor_0_0_0"));
                AssertBottomOnGrid(FindChild(root, "tileGround_0_0"));
                AssertBottomOnGrid(FindChild(root, "tileHole_1_0"));
                AssertBottomOnGrid(FindChild(root, "doorAnchor_north_0_available"));
                AssertBottomOnGrid(FindChildStartingWith(root, "grid_x_"));

                var rock = root.GetComponentsInChildren<Transform>(true)
                    .First(transform => transform.name.StartsWith("rockTile_"));
                AssertBottomOnGrid(rock);
                Assert.AreEqual(0.5f, rock.localPosition.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
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

        [Test]
        public void ControllerTogglesScenePreviewAndAttachesArtPassVisuals()
        {
            var root = new GameObject("RoomDesignerScenePreviewController");
            try
            {
                var catalog = Resources.Load<PresentationContentCatalog>("Hollow/Presentation/PresentationContentCatalog");
                if (catalog != null)
                {
                    PresentationContentProvider.Configure(catalog);
                }

                var controller = root.AddComponent<RoomDesignerController>();
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), RoomDesignerProject.CreateDefault());

                Assert.AreEqual(RoomDesignerPreviewMode.Graybox, controller.PreviewMode);

                controller.ApplyInput(Input(togglePreview: true), 1f);

                Assert.AreEqual(RoomDesignerPreviewMode.Scene, controller.PreviewMode);
                Assert.IsNotNull(GameObject.Find("RoomDesignerSceneLightingRig"));
                Assert.IsNotNull(GameObject.Find("RoomDesignerPreviewModeButton"));
                AssertVisualMarker(root, PresentationPrefabRole.RoomFloor);
                AssertVisualMarker(root, PresentationPrefabRole.RoomObstacleRock);
                AssertVisualMarker(root, PresentationPrefabRole.Player);
                AssertVisualMarker(root, PresentationPrefabRole.EnemyNormal);
                AssertVisualMarker(root, PresentationPrefabRole.RewardPickup);
                Assert.AreEqual(0, root.GetComponentsInChildren<PresentationVisualMarker>(true)
                    .SelectMany(marker => marker.GetComponentsInChildren<Collider>(true))
                    .Count());
            }
            finally
            {
                PresentationContentProvider.Reset();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CameraControllerSupportsPerspectiveAndTopDownModes()
        {
            var cameraObject = new GameObject("RoomDesignerModeCamera", typeof(Camera));
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                var controller = new RoomDesignerCameraController();
                controller.SetTarget(new Vector3(3f, 0f, -2f), RoomDesignerFootprintPreset.Block2x2);

                controller.ApplyImmediate(camera);
                Assert.AreEqual(RoomDesignerCameraViewMode.Perspective, controller.ViewMode);
                Assert.IsFalse(camera.orthographic);
                var defaultPerspectiveDistance = Vector3.Distance(camera.transform.position, controller.TargetPosition);

                controller.AdjustZoom(-0.15f);
                controller.ApplyImmediate(camera);
                Assert.Less(Vector3.Distance(camera.transform.position, controller.TargetPosition), defaultPerspectiveDistance);

                controller.ResetZoom();
                controller.SetViewMode(RoomDesignerCameraViewMode.TopDown);
                controller.ApplyImmediate(camera);

                Assert.IsTrue(camera.orthographic);
                Assert.AreEqual(new Vector3(3f, 0f, -2f), controller.TargetPosition);
                Assert.Greater(camera.orthographicSize, 10f);
                Assert.Greater(Vector3.Dot(camera.transform.forward, Vector3.down), 0.99f);

                var defaultOrthoSize = camera.orthographicSize;
                controller.AdjustZoom(10f);
                controller.ApplyImmediate(camera);
                Assert.AreEqual(1.8f, controller.ZoomMultiplier, 0.001f);
                Assert.Greater(camera.orthographicSize, defaultOrthoSize);

                controller.AdjustZoom(-10f);
                controller.ApplyImmediate(camera);
                Assert.AreEqual(0.55f, controller.ZoomMultiplier, 0.001f);
                Assert.Less(camera.orthographicSize, defaultOrthoSize);

                controller.SetViewMode(RoomDesignerCameraViewMode.Perspective);
                controller.ApplyImmediate(camera);

                Assert.IsFalse(camera.orthographic);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ControllerShowsCameraToggleButtonAndSupportsSpatialTilt()
        {
            var root = new GameObject("RoomDesignerCameraModeController");
            var cameraObject = new GameObject("MainCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Wide2x1, "Camera Toggle"));

                Assert.AreEqual(RoomDesignerCameraViewMode.Perspective, controller.CameraViewMode);
                var cameraButton = GameObject.Find("RoomDesignerCameraModeButton");
                Assert.IsNotNull(cameraButton);
                Assert.IsTrue(cameraButton.activeSelf);
                Assert.IsTrue(cameraButton.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("Perspective"));

                cameraButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.AreEqual(RoomDesignerCameraViewMode.TopDown, controller.CameraViewMode);
                cameraButton = GameObject.Find("RoomDesignerCameraModeButton");
                Assert.IsTrue(cameraButton.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("Top"));

                var previewRoot = FindChild(root, "RoomDesignerPreviewRoot");
                Assert.AreEqual(0f, previewRoot.localEulerAngles.x, 0.001f);

                controller.SetSpatialTopDownTiltForTests(true);
                Assert.AreEqual(55f, previewRoot.localEulerAngles.x, 0.001f);

                controller.ApplyInput(Input(toggleCamera: true), 1f);
                Assert.AreEqual(RoomDesignerCameraViewMode.Perspective, controller.CameraViewMode);
                Assert.AreEqual(0f, previewRoot.localEulerAngles.x, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ControllerShowsZoomButtonsAndZoomsWithoutMovingTarget()
        {
            var root = new GameObject("RoomDesignerZoomController");
            var cameraObject = new GameObject("MainCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            try
            {
                var controller = root.AddComponent<RoomDesignerController>();
                controller.InitializeForTest(new RoomDesignerStore(tempRoot), new ProfileSlotId(0), RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Block2x2, "Zoom Draft"));

                var zoomOutButton = GameObject.Find("RoomDesignerZoomOutButton");
                var zoomInButton = GameObject.Find("RoomDesignerZoomInButton");
                Assert.IsNotNull(zoomOutButton);
                Assert.IsNotNull(zoomInButton);
                Assert.IsTrue(zoomOutButton.activeSelf);
                Assert.IsTrue(zoomInButton.activeSelf);

                var target = controller.CameraTargetPosition;
                Assert.AreEqual(1f, controller.CameraZoomMultiplier, 0.001f);

                zoomInButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.Less(controller.CameraZoomMultiplier, 1f);
                Assert.AreEqual(target, controller.CameraTargetPosition);

                zoomOutButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.AreEqual(1f, controller.CameraZoomMultiplier, 0.001f);

                controller.ApplyInput(Input(zoomDelta: -1), 1f);
                Assert.Greater(controller.CameraZoomMultiplier, 1f);

                controller.ApplyInput(Input(toggleCamera: true), 2f);
                Assert.AreEqual(RoomDesignerCameraViewMode.TopDown, controller.CameraViewMode);
                controller.SetSpatialTopDownTiltForTests(true);
                var previewRoot = FindChild(root, "RoomDesignerPreviewRoot");
                Assert.AreEqual(55f, previewRoot.localEulerAngles.x, 0.001f);

                controller.ApplyInput(Input(zoomDelta: 1), 3f);
                Assert.AreEqual(55f, previewRoot.localEulerAngles.x, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static RoomDesignerInputSnapshot Input(
            int moveX = 0,
            int moveZ = 0,
            int toolDelta = 0,
            int layerDelta = 0,
            bool place = false,
            bool erase = false,
            bool togglePreview = false,
            bool toggleCamera = false,
            int zoomDelta = 0)
        {
            return new RoomDesignerInputSnapshot(moveX, moveZ, toolDelta, layerDelta, place, erase, false, false, false, false, false, false, togglePreview, toggleCamera, zoomDelta);
        }

        private static Transform FindChild(GameObject root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).Single(transform => transform.name == name);
        }

        private static Transform FindChildStartingWith(GameObject root, string prefix)
        {
            return root.GetComponentsInChildren<Transform>(true).First(transform => transform.name.StartsWith(prefix));
        }

        private static void AssertBottomOnGrid(Transform transform)
        {
            Assert.AreEqual(0f, transform.localPosition.y - transform.localScale.y * 0.5f, 0.001f, transform.name);
        }

        private static void AssertVisualMarker(GameObject root, PresentationPrefabRole role)
        {
            Assert.IsTrue(root.GetComponentsInChildren<PresentationVisualMarker>(true)
                .Any(marker => marker.Role == role), $"Missing scene preview visual for {role}");
        }
    }
}
