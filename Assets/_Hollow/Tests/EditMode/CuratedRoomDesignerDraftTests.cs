using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Persistence;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class CuratedRoomDesignerDraftTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_curated_room_designer_tests", Path.GetRandomFileName());
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
        public void RuntimeImporterConvertsCurrentGeneratableRoomsIntoValidDrafts()
        {
            var sourcePaths = CuratedRoomDesignerDraftGenerator.SourceRuntimeRoomPaths();
            Assert.AreEqual(10, sourcePaths.Count);

            foreach (var sourcePath in sourcePaths)
            {
                var sourceJson = File.ReadAllText(sourcePath);
                var runtime = HollowRuntimeV2Importer.Import(sourceJson);
                var project = RoomDesignerRuntimeDraftImporter.FromRuntimeJson(sourceJson, sourcePath);
                var report = RoomDesignerDraftValidator.Validate(project);

                Assert.IsTrue(report.IsValid, $"{sourcePath}: {string.Join("; ", report.Errors)}");
                Assert.AreEqual($"curated_{runtime.Id}", project.projectId);
                Assert.AreEqual(runtime.Layout.WidthTiles, project.widthTiles);
                Assert.AreEqual(runtime.Layout.HeightTiles, project.heightTiles);
                Assert.Greater(project.cells.Count(cell => cell.kind == RoomDesignerCellKinds.Ground), 0);
                Assert.IsTrue(project.markers.Any(marker => marker.kind == RoomDesignerMarkerKinds.SafeStart));
                Assert.IsTrue(project.markers.Any(marker => RoomDesignerMarkerKinds.IsEnemy(marker.kind)));
                Assert.IsTrue(project.markers.Any(marker => marker.kind == RoomDesignerMarkerKinds.RoomReward));
            }
        }

        [Test]
        public void CuratedDraftsRoundTripThroughCompilerAndRuntimeImporter()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomDesignerCuratedDraftCatalogDefinition>(CuratedRoomDesignerDraftGenerator.CuratedDraftCatalogPath);
            Assert.IsNotNull(catalog, "Run CuratedRoomDesignerDraftGenerator.Generate before running curated draft tests.");
            Assert.AreEqual(10, catalog.CuratedDrafts.Length);

            foreach (var textAsset in catalog.CuratedDrafts)
            {
                Assert.IsNotNull(textAsset);
                var project = JsonUtility.FromJson<RoomDesignerProject>(textAsset.text);
                Assert.IsTrue(RoomDesignerDraftValidator.Validate(project).IsValid, project.projectId);
                Assert.IsTrue(HollowRuntimeV2Importer.TryImport(RoomDesignerCompiler.ExportRuntimeJson(project), out _, out var error), error);
            }
        }

        [Test]
        public void OpeningCuratedDraftCreatesSlotLocalEditableCopy()
        {
            var source = RoomDesignerRuntimeDraftImporter.FromRuntimeJson(
                File.ReadAllText(CuratedRoomDesignerDraftGenerator.SourceRuntimeRoomPaths().First()),
                CuratedRoomDesignerDraftGenerator.SourceRuntimeRoomPaths().First());
            var catalog = ScriptableObject.CreateInstance<RoomDesignerCuratedDraftCatalogDefinition>();
            var sourceText = new TextAsset(JsonUtility.ToJson(source, prettyPrint: true));
            catalog.Configure("test_curated_catalog", new[] { sourceText });

            try
            {
                var store = new RoomDesignerStore(tempRoot);
                var slotId = new ProfileSlotId(0);
                var library = new RoomDesignerDraftLibraryState(store, slotId, autoCreateDefaultDraft: false, catalog);

                Assert.AreEqual(0, library.Drafts.Count);
                Assert.AreEqual(1, library.CuratedDrafts.Count);

                var copy = library.OpenCuratedAsEditableCopy(source.projectId);
                var savedDrafts = store.LoadExistingDrafts(slotId);

                Assert.AreEqual(1, savedDrafts.Count);
                Assert.AreNotEqual(source.projectId, copy.projectId);
                Assert.AreEqual($"{source.displayName} - Edit Copy", copy.displayName);
                Assert.AreEqual(source.projectId, library.CuratedDrafts[0].projectId);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CuratedCatalogGroupsExposeRoomTypeBuckets()
        {
            Assert.AreEqual("Combat Rooms", RoomDesignerCatalogGroups.ForProject(Project("combat_macro_single_1x1", "Combat Macro Single")));
            Assert.AreEqual("Treasure Rooms", RoomDesignerCatalogGroups.ForProject(Project("treasure_room_single_1x1", "Treasure Room")));
            Assert.AreEqual("Boss Rooms", RoomDesignerCatalogGroups.ForProject(Project("boss_arena_single_1x1", "Boss Arena")));
            Assert.AreEqual("Secret Rooms", RoomDesignerCatalogGroups.ForProject(Project("secret_hidden_single_1x1", "Secret Hidden")));
            Assert.AreEqual("Hub / Shop Rooms", RoomDesignerCatalogGroups.ForProject(Project("hub_shop_single_1x1", "Hub Shop")));
        }

        [Test]
        public void CuratedDraftValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => CuratedRoomDesignerDraftValidator.Validate());
        }

        private static RoomDesignerProject Project(string projectId, string displayName)
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, displayName);
            project.projectId = projectId;
            return project;
        }
    }
}
