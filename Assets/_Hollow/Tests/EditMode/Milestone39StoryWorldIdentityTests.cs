using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.UI.Shell;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone39StoryWorldIdentityTests
    {
        [Test]
        public void CatalogContainsThreePrototypeWorlds()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone39AssetGenerator.RunFramingCatalogPath);
            Assert.IsNotNull(catalog);

            for (var world = 1; world <= 3; world++)
            {
                Assert.IsTrue(catalog.TryGetWorld(world, out var definition));
                Assert.AreEqual(world, definition.WorldIndex);
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.DisplayName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.HubLine));
            }
        }

        [Test]
        public void RunFramingServiceChoosesPhaseSpecificLines()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone39AssetGenerator.RunFramingCatalogPath);
            var prologue = RunFramingService.Create(catalog, 1, RunWorldPhase.Prologue, 123, 456, bossRoomActive: false);
            var hub = RunFramingService.Create(catalog, 1, RunWorldPhase.Hub, 123, 456, bossRoomActive: false);
            var boss = RunFramingService.Create(catalog, 1, RunWorldPhase.Branch, 123, 456, bossRoomActive: true);

            Assert.AreNotEqual(prologue.Message, hub.Message);
            Assert.AreEqual("Inter-Branch Hub", hub.PhaseLabel);
            Assert.AreEqual("Boss Threshold", boss.PhaseLabel);
            Assert.IsTrue(prologue.SeedSummary.Contains("123"));
        }

        [Test]
        public void RunFramingHudStaysOnPlatformShellCanvas()
        {
            var shell = new GameObject("PlatformShellCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var worldRoot = new GameObject("WorldPresentationRoot");
            try
            {
                var hud = shell.AddComponent<RunFramingHudController>();
                var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone39AssetGenerator.RunFramingCatalogPath);
                hud.Configure(catalog);
                Assert.AreEqual(catalog, hud.Catalog);
                Assert.IsFalse(shell.transform.IsChildOf(worldRoot.transform));
            }
            finally
            {
                Object.DestroyImmediate(shell);
                Object.DestroyImmediate(worldRoot);
            }
        }

        [Test]
        public void Milestone39ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone39Validator.Validate());
        }
    }
}
