using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEditor;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone50StoryWorldIdentityTests
    {
        [Test]
        public void CatalogContainsEightHollowStarWorlds()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone50AssetGenerator.RunFramingCatalogPath);
            Assert.IsNotNull(catalog);
            Assert.AreEqual(Milestone50AssetGenerator.CatalogId, catalog.CatalogId);
            Assert.AreEqual(8, catalog.Worlds.Count);
            Assert.IsTrue(catalog.Worlds.All(world => !string.IsNullOrWhiteSpace(world.IdentityId)));
            Assert.IsTrue(catalog.Worlds.All(world => world.BiomeTags.Count > 0));
            Assert.IsTrue(catalog.Worlds.All(world => world.BranchEchoNames.Count >= 3));
        }

        [Test]
        public void ItineraryIsSeededDeterministicAndDistinct()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone50AssetGenerator.RunFramingCatalogPath);
            var first = RunWorldItineraryService.ResolveItinerary(catalog, 15001, 3).Select(world => world.IdentityId).ToArray();
            var second = RunWorldItineraryService.ResolveItinerary(catalog, 15001, 3).Select(world => world.IdentityId).ToArray();
            Assert.AreEqual(3, first.Length);
            CollectionAssert.AreEqual(first, second);
            Assert.AreEqual(3, first.Distinct().Count());
        }

        [Test]
        public void RunFramingUsesMechanicalWorldWithSeededIdentity()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone50AssetGenerator.RunFramingCatalogPath);
            var snapshot = RunFramingService.Create(catalog, 1, RunWorldPhase.Prologue, 15001, 47001, bossRoomActive: false);
            Assert.IsTrue(snapshot.Title.StartsWith("World 1:"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(snapshot.WorldIdentityId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(snapshot.WorldDisplayName));
            Assert.IsTrue(snapshot.Message.Contains("Hollow Star") || snapshot.Subtitle.Length > 12);
        }

        [Test]
        public void BranchEchoLabelsResolveForHubPortalSlots()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone50AssetGenerator.RunFramingCatalogPath);
            var echoes = Enumerable.Range(0, 3)
                .Select(slot => RunWorldItineraryService.ResolveBranchEcho(catalog, 15001, 1, slot))
                .ToArray();
            Assert.AreEqual(3, echoes.Length);
            Assert.IsTrue(echoes.All(echo => !string.IsNullOrWhiteSpace(echo)));
            Assert.AreEqual(3, echoes.Distinct().Count());
        }

        [Test]
        public void Milestone50ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone50Validator.Validate());
        }
    }
}
