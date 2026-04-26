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
    }
}
