using System.IO;
using System.Linq;
using Hollow.Combat;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone97EnemyPreviewLabTests
    {
        [Test]
        public void PreviewRoomAssetContainsRenderableLabLayout()
        {
            var asset = EnemyPreviewLabController.CreatePreviewRoomAsset("spawnEnemySpider");

            Assert.AreEqual("enemy_preview_lab", asset.Id);
            Assert.AreEqual("Enemy Preview Lab", asset.DisplayName);
            Assert.AreEqual(18, asset.Layout.WidthTiles);
            Assert.AreEqual(12, asset.Layout.HeightTiles);
            Assert.That(asset.Layout.Obstacles.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(asset.Layout.HoleTiles.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(asset.DoorPorts.Count, Is.EqualTo(4));
            Assert.That(asset.Hazards.Count, Is.EqualTo(1));
            Assert.AreEqual("spawnEnemySpider", asset.EnemySpawns.Single().kind);
        }

        [Test]
        public void ControllerSelectionDefaultsSafely()
        {
            var gameObject = new GameObject("PreviewLabTest");
            try
            {
                var controller = gameObject.AddComponent<EnemyPreviewLabController>();
                controller.SetSelectedSpawnKind(string.Empty, respawnIfPlaying: false);

                Assert.AreEqual(EnemyPreviewLabController.DefaultSelectedSpawnKind, controller.SelectedSpawnKind);
                Assert.NotNull(controller.SelectedDefinition);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ControllerOverlayTogglesFeedDebugState()
        {
            var gameObject = new GameObject("PreviewLabDebugTest");
            try
            {
                var controller = gameObject.AddComponent<EnemyPreviewLabController>();

                controller.SetOverlayToggles(true, true, true, true, true);
                Assert.IsTrue(EnemyNavigationDebugOverlay.PathTracingEnabled);
                Assert.IsTrue(EnemyAiDebugOverlay.BlackboardEnabled);

                controller.SetOverlayToggles(false, false, false, false, false);
                Assert.IsFalse(EnemyNavigationDebugOverlay.PathTracingEnabled);
                Assert.IsFalse(EnemyAiDebugOverlay.BlackboardEnabled);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DocsAndReportExist()
        {
            Assert.IsTrue(File.Exists(EnemyPreviewLabController.DefaultScenePath));
            Assert.IsTrue(File.Exists("Docs/Hollow_M97_Enemy_Preview_Lab.md"));
            Assert.IsTrue(File.Exists("output/reports/enemy_preview_lab/m97_enemy_preview_lab.md"));
        }
    }
}
