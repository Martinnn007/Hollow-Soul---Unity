using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone36RoomEncounterContentExpansionTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void ApprovedRoomPoolContainsOneNewRoomPerMacroFootprint()
        {
            var content = LoadContent();
            var rooms = Milestone36AssetGenerator.ApprovedRoomIds
                .Select(id => content.ApprovedRoomPool.TryGetValue(id, out var room) ? room : null)
                .ToArray();

            Assert.AreEqual(Milestone36AssetGenerator.ApprovedRoomIds.Count, rooms.Count(room => room != null));
            CollectionAssert.AreEquivalent(
                new[] { RoomFootprintShape.Single1x1, RoomFootprintShape.Wide2x1, RoomFootprintShape.Tall1x2, RoomFootprintShape.Block2x2, RoomFootprintShape.L3Cell },
                rooms.Select(room => RoomFootprintShapeUtility.Classify(room.Footprint)).ToArray());
            Assert.IsTrue(rooms.All(room => room.EnemySpawns.Count >= 4));
            Assert.IsTrue(rooms.All(room => room.Layout.Obstacles.Count >= 4));
        }

        [Test]
        public void SeededBranchCanSelectApprovedDesignerRoomsWithoutBreakingTopology()
        {
            var content = LoadContent();
            var settings = LoadSettings();
            var selectedApprovedRoomIds = new HashSet<string>();

            for (var seed = 36001; seed < 36025; seed++)
            {
                var graph = BranchGenerator.CreateSeededBranchFeatures(content, settings, seed);
                Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError), topologyError);
                Assert.IsFalse(HasFootprintOverlap(graph));

                foreach (var room in graph.Rooms.Where(room => Milestone36AssetGenerator.ApprovedRoomIds.Contains(room.RuntimeRoomAssetId)))
                {
                    selectedApprovedRoomIds.Add(room.RuntimeRoomAssetId);
                }
            }

            Assert.GreaterOrEqual(selectedApprovedRoomIds.Count, 2, "M36 approved content should be eligible enough to appear across validation seeds.");
        }

        [Test]
        public void ExpandedEncounterCatalogProducesSeededVariety()
        {
            var catalog = LoadEncounterCatalog();
            var content = LoadContent();
            var settings = LoadSettings();
            var signatures = new HashSet<string>();
            var encounterIds = new HashSet<string>();

            Assert.GreaterOrEqual(catalog.Encounters.Count, 10);
            Assert.IsTrue(catalog.Encounters.Any(encounter => encounter.EncounterId == "macro_mixup"));
            Assert.IsTrue(catalog.Encounters.Any(encounter => encounter.EncounterId == "reward_watchers"));

            for (var seed = 36001; seed < 36008; seed++)
            {
                var graph = BranchGenerator.CreateSeededBranchFeatures(content, settings, seed);
                var first = EncounterResolver.CreateSeededPlan(graph, catalog, graph.Seed);
                var second = EncounterResolver.CreateSeededPlan(graph, catalog, graph.Seed);

                Assert.AreEqual(Signature(first), Signature(second));
                signatures.Add(Signature(first));
                foreach (var assignment in first.Assignments)
                {
                    encounterIds.Add(assignment.EncounterId);
                }
            }

            Assert.GreaterOrEqual(signatures.Count, 2);
            Assert.GreaterOrEqual(encounterIds.Count, 4);
        }

        [Test]
        public void Milestone36ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone36Validator.Validate());
        }

        private static BranchSessionContent LoadContent()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = LoadSettings();
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            return content;
        }

        private static BranchGenerationSettingsDefinition LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            Assert.IsNotNull(settings, "Run M36 generation before running M36 tests.");
            return settings;
        }

        private static EncounterCatalogDefinition LoadEncounterCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone36AssetGenerator.EncounterCatalogPath);
            Assert.IsNotNull(catalog, "Run M36 generation before running M36 tests.");
            return catalog;
        }

        private static bool HasFootprintOverlap(BranchFloorGraph graph)
        {
            return graph.OccupancyMap.OwnerByCell.Count != graph.Rooms.Sum(room => room.Footprint?.OccupiedCellCount ?? 0);
        }

        private static string Signature(EncounterPlan plan)
        {
            return string.Join("|", plan.Assignments
                .OrderBy(assignment => assignment.RoomId)
                .Select(assignment => $"{assignment.RoomId}:{assignment.EncounterId}:{string.Join(",", assignment.EnemySpawnKinds)}"));
        }
    }
}
