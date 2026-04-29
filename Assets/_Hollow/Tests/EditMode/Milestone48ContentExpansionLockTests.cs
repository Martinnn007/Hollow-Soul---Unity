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
    public sealed class Milestone48ContentExpansionLockTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void M48ApprovedRoomsImportWithHazardsAndFootprints()
        {
            var content = LoadContent();
            var rooms = Milestone48AssetGenerator.ApprovedRoomIds
                .Select(id => content.ApprovedRoomPool.TryGetValue(id, out var room) ? room : null)
                .ToArray();

            Assert.AreEqual(Milestone48AssetGenerator.ApprovedRoomIds.Count, rooms.Count(room => room != null));
            CollectionAssert.AreEquivalent(
                new[] { RoomFootprintShape.Single1x1, RoomFootprintShape.Wide2x1, RoomFootprintShape.Tall1x2, RoomFootprintShape.Block2x2, RoomFootprintShape.L3Cell },
                rooms.Select(room => RoomFootprintShapeUtility.Classify(room.Footprint)).ToArray());
            Assert.IsTrue(rooms.All(room => room.EnemySpawns.Count >= 4));
            Assert.IsTrue(rooms.All(room => room.ItemSpawns.Count >= 1));
            Assert.IsTrue(rooms.All(room => room.Hazards.Count + room.InteractiveObjects.Count > 0));
        }

        [Test]
        public void M48CuratedDraftsExistAndRoundTrip()
        {
            foreach (var roomId in Milestone48AssetGenerator.ApprovedRoomIds)
            {
                var runtimePath = $"{Milestone16AssetGenerator.ApprovedRoomDirectory}/{roomId}.hollowruntime.json";
                Assert.IsTrue(File.Exists(runtimePath), runtimePath);
                var curatedId = CuratedRoomDesignerDraftGenerator.CuratedProjectIdForRuntimeRoom(File.ReadAllText(runtimePath));
                var curatedPath = $"{CuratedRoomDesignerDraftGenerator.CuratedDraftDirectory}/{curatedId}.roomdesigner.json";
                Assert.IsTrue(File.Exists(curatedPath), curatedPath);
            }

            Assert.DoesNotThrow(() => CuratedRoomDesignerDraftValidator.Validate());
        }

        [Test]
        public void M48EncounterCatalogExtendsM46WithExistingSpawnKinds()
        {
            var catalog = LoadEncounterCatalog();
            var expected = Milestone46AssetGenerator.EncounterIds.Concat(Milestone48AssetGenerator.EncounterIds).ToArray();
            foreach (var encounterId in expected)
            {
                Assert.IsTrue(catalog.Encounters.Any(encounter => encounter.EncounterId == encounterId), encounterId);
            }

            Assert.AreEqual("stone_warden_boss", catalog.BossEncounter.EncounterId);
            var allowed = new HashSet<string>
            {
                "spawnEnemyNormal",
                "spawnEnemyFlying",
                "spawnEnemyFast",
                "spawnEnemyHeavy",
                "spawnEnemyCharger",
                "spawnEnemyTurret",
                "spawnEnemySplitter",
                "spawnEnemyBoss"
            };
            foreach (var encounter in catalog.Encounters)
            {
                var spawns = encounter.ExpandSpawnKinds();
                Assert.IsTrue(spawns.All(allowed.Contains), encounter.EncounterId);
                if (encounter.RoomRole != BranchRoomRole.Boss)
                {
                    Assert.LessOrEqual(spawns.Count, 6, encounter.EncounterId);
                }
            }
        }

        [Test]
        public void DirectedSeedSweepSelectsM48RoomsAndEncounters()
        {
            var content = LoadContent();
            var settings = LoadSettings();
            var profile = LoadDirectorProfile();
            var catalog = LoadEncounterCatalog();
            var selectedRooms = new HashSet<string>();
            var selectedEncounters = new HashSet<string>();

            for (var seed = 48001; seed < 48060; seed++)
            {
                var world = 1 + (seed % 3);
                var graph = BranchGenerator.CreateDirectedEncounterBranch(content, settings, profile, world, seed);
                Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError), topologyError);
                foreach (var room in graph.Rooms.Where(room => Milestone48AssetGenerator.ApprovedRoomIds.Contains(room.RuntimeRoomAssetId)))
                {
                    selectedRooms.Add(room.RuntimeRoomAssetId);
                }

                var plan = EncounterResolver.CreateDirectedSeededPlan(graph, catalog, graph.Seed, world, profile);
                foreach (var assignment in plan.Assignments.Where(assignment => Milestone48AssetGenerator.EncounterIds.Contains(assignment.EncounterId)))
                {
                    selectedEncounters.Add(assignment.EncounterId);
                }
            }

            Assert.GreaterOrEqual(selectedRooms.Count, 3);
            Assert.GreaterOrEqual(selectedEncounters.Count, 4);
        }

        [Test]
        public void Milestone48ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone48Validator.Validate());
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
            Assert.IsNotNull(settings, "Run M48 generation before running M48 tests.");
            return settings;
        }

        private static EncounterDirectorProfileDefinition LoadDirectorProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath);
            Assert.IsNotNull(profile, "Run M48 generation before running M48 tests.");
            return profile;
        }

        private static EncounterCatalogDefinition LoadEncounterCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone48AssetGenerator.EncounterCatalogPath);
            Assert.IsNotNull(catalog, "Run M48 generation before running M48 tests.");
            return catalog;
        }
    }
}
