using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone16ApprovedDesignerRoomPoolTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";
        private const string FixtureRoot = "Assets/_Hollow/Data/Rooms/MacroFixtures";
        private const string SettingsPath = "Assets/_Hollow/Data/Branches/BranchGenerationSettings_M15.asset";

        [Test]
        public void ApprovedImporterAcceptsValidDesignerRoomAndRejectsInvalidDraft()
        {
            var valid = CreateApprovedTextAsset(RoomDesignerFootprintPreset.Wide2x1, "approved_wide");
            var report = ApprovedDesignerRoomImporter.ImportApprovedRooms(new[] { valid });

            Assert.IsFalse(report.HasErrors, string.Join("; ", report.Errors));
            Assert.AreEqual(1, report.ValidRooms.Count);
            Assert.AreEqual("designer_approved_wide", report.ValidRooms[0].Id);
            Assert.AreEqual(RoomFootprintShape.Wide2x1, RoomFootprintShapeUtility.Classify(report.ValidRooms[0].Footprint));

            var invalid = CreateApprovedTextAsset(RoomDesignerFootprintPreset.Single1x1, "approved_invalid_no_enemy", removeEnemies: true);
            var invalidReport = ApprovedDesignerRoomImporter.ImportApprovedRooms(new[] { invalid });
            Assert.IsTrue(invalidReport.HasErrors);
            Assert.IsTrue(invalidReport.Errors.Any(error => error.Contains("enemy spawn")));
        }

        [Test]
        public void ApprovedImporterRejectsDuplicateApprovedRoomIds()
        {
            var first = CreateApprovedTextAsset(RoomDesignerFootprintPreset.Single1x1, "approved_duplicate");
            var second = CreateApprovedTextAsset(RoomDesignerFootprintPreset.Single1x1, "approved_duplicate");
            var report = ApprovedDesignerRoomImporter.ImportApprovedRooms(new[] { first, second });

            Assert.IsTrue(report.HasErrors);
            Assert.AreEqual(1, report.ValidRooms.Count);
            Assert.IsTrue(report.Errors.Any(error => error.Contains("duplicates canonicalRoomId")));
        }

        [Test]
        public void BranchSessionContentImportsApprovedRoomsAdditivelyAndRejectsFixtureDuplicate()
        {
            var catalog = CreateCatalog(CreateApprovedTextAsset(RoomDesignerFootprintPreset.Tall1x2, "approved_tall"));
            var content = BranchSessionContent.Create(ImportSampleRoom(), catalog, BranchGenerator.DefaultSeededMacroSeed, out var error);

            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            Assert.IsTrue(content.HasMacroFixturePool);
            Assert.IsTrue(content.ApprovedRoomPool.ContainsKey("designer_approved_tall"));
            Assert.IsTrue(content.MacroRoomPool.ContainsKey("designer_approved_tall"));

            var duplicate = CreateFixtureDuplicateTextAsset("combat_macro_single_1x1");
            var duplicateCatalog = CreateCatalog(duplicate);
            BranchSessionContent.Create(ImportSampleRoom(), duplicateCatalog, BranchGenerator.DefaultSeededMacroSeed, out var duplicateError);
            Assert.IsTrue(duplicateError.Contains("duplicates an existing branch template"));
        }

        [Test]
        public void SeededGeneratorCanSelectApprovedRoomsButFallsBackWithoutThem()
        {
            var fallbackContent = BranchSessionContent.Create(ImportSampleRoom(), CreateCatalog(), BranchGenerator.DefaultSeededMacroSeed, out var fallbackError);
            Assert.IsTrue(string.IsNullOrWhiteSpace(fallbackError), fallbackError);
            var fallbackGraph = BranchGenerator.CreateSeededMacroBranch(fallbackContent, LoadSettings(), BranchGenerator.DefaultSeededMacroSeed);
            Assert.IsFalse(fallbackGraph.Rooms.Any(room => room.RuntimeRoomAssetId.StartsWith("designer_")));

            var approvedContent = BranchSessionContent.Create(
                ImportSampleRoom(),
                CreateCatalog(
                    CreateApprovedTextAsset(RoomDesignerFootprintPreset.Single1x1, "approved_single"),
                    CreateApprovedTextAsset(RoomDesignerFootprintPreset.Wide2x1, "approved_wide"),
                    CreateApprovedTextAsset(RoomDesignerFootprintPreset.Tall1x2, "approved_tall"),
                    CreateApprovedTextAsset(RoomDesignerFootprintPreset.Block2x2, "approved_block"),
                    CreateApprovedTextAsset(RoomDesignerFootprintPreset.L3Cell, "approved_l")),
                BranchGenerator.DefaultSeededMacroSeed,
                out var approvedError);
            Assert.IsTrue(string.IsNullOrWhiteSpace(approvedError), approvedError);

            var selectedApproved = false;
            for (var seed = BranchGenerator.DefaultSeededMacroSeed; seed < BranchGenerator.DefaultSeededMacroSeed + 80; seed++)
            {
                var graph = BranchGenerator.CreateSeededMacroBranch(approvedContent, LoadSettings(), seed);
                if (graph.Rooms.Any(room => room.RuntimeRoomAssetId.StartsWith("designer_")))
                {
                    selectedApproved = true;
                    break;
                }
            }

            Assert.IsTrue(selectedApproved, "Expected at least one deterministic seed to select an approved designer room.");
        }

        [Test]
        public void ApprovedDesignerRoomBuildsRuntimePresentation()
        {
            var report = ApprovedDesignerRoomImporter.ImportApprovedRooms(new[] { CreateApprovedTextAsset(RoomDesignerFootprintPreset.Block2x2, "approved_runtime_build") });
            Assert.IsFalse(report.HasErrors, string.Join("; ", report.Errors));

            var root = new GameObject("ApprovedRoomRuntimeRoot");
            try
            {
                var room = root.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(report.ValidRooms[0]);

                Assert.AreEqual("designer_approved_runtime_build", room.LastBuiltAsset.Id);
                Assert.Greater(room.DoorPorts.Count, 0);
                Assert.Greater(room.EnemySpawns.Count, 0);
                Assert.AreEqual(RoomFootprintShape.Block2x2, RoomFootprintShapeUtility.Classify(room.LastBuiltAsset.Footprint));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static TextAsset CreateApprovedTextAsset(RoomDesignerFootprintPreset preset, string projectId, bool removeEnemies = false)
        {
            var project = RoomDesignerProject.CreateDefault(preset, $"Approved {projectId}");
            project.projectId = projectId;
            if (removeEnemies)
            {
                project.markers.RemoveAll(marker => marker.kind == RoomDesignerMarkerKinds.Enemy);
            }

            return new TextAsset(RoomDesignerCompiler.ExportRuntimeJson(project, prettyPrint: true))
            {
                name = $"{projectId}.hollowruntime"
            };
        }

        private static TextAsset CreateFixtureDuplicateTextAsset(string duplicateId)
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "Duplicate Fixture ID");
            project.projectId = "duplicate_fixture_id";
            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = duplicateId;
            return new TextAsset(JsonUtility.ToJson(manifest, prettyPrint: true))
            {
                name = "duplicate_fixture_id.hollowruntime"
            };
        }

        private static BranchRoomTemplateCatalogDefinition CreateCatalog(params TextAsset[] additionalTemplates)
        {
            var catalog = ScriptableObject.CreateInstance<BranchRoomTemplateCatalogDefinition>();
            catalog.Configure(
                LoadFixture("combat_macro_single_1x1"),
                LoadFixture("combat_macro_wide_2x1"),
                LoadFixture("combat_macro_tall_1x2"),
                LoadFixture("combat_macro_block_2x2"),
                LoadFixture("combat_macro_l_3cell"),
                BranchGenerator.DefaultSeededMacroSeed,
                additionalTemplates);
            return catalog;
        }

        private static TextAsset LoadFixture(string id)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{FixtureRoot}/{id}.hollowruntime.json");
            Assert.IsNotNull(asset, id);
            return asset;
        }

        private static BranchGenerationSettingsDefinition LoadSettings()
        {
            return AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(SettingsPath);
        }

        private static ImportedRoomRuntimeAsset ImportSampleRoom()
        {
            return HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
        }
    }
}
