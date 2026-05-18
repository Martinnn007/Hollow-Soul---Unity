using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class VerdantRuinsBiomeFoundationTests
    {
        private const string LegacyRoomPath = "Assets/_Hollow/Data/Rooms/MacroFixtures/combat_macro_single_1x1.hollowruntime.json";
        private const string SampleRoomPath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void LegacyRoomsImportWithHollowThresholdBiomeByDefault()
        {
            var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(LegacyRoomPath));

            Assert.AreEqual(RoomBiomeIds.HollowThreshold, asset.BiomeId);
        }

        [Test]
        public void VerdantBiomeCatalogProvidesCompleteMacroShapeCoverage()
        {
            var catalog = LoadBiomeCatalog();

            Assert.IsTrue(catalog.TryGetBiome(RoomBiomeIds.VerdantRuins, out var verdant));
            Assert.AreEqual("Verdant Ruins", verdant.DisplayName);
            Assert.AreEqual(5, verdant.RoomTemplates.Count);

            var shapes = verdant.RoomTemplates
                .Select(template => HollowRuntimeV2Importer.Import(template.text))
                .Select(asset =>
                {
                    Assert.AreEqual(RoomBiomeIds.VerdantRuins, asset.BiomeId);
                    Assert.IsTrue(asset.Id.StartsWith("verdant_macro_", System.StringComparison.Ordinal));
                    return RoomFootprintShapeUtility.Classify(asset.Footprint);
                })
                .ToHashSet();

            Assert.IsTrue(shapes.Contains(RoomFootprintShape.Single1x1));
            Assert.IsTrue(shapes.Contains(RoomFootprintShape.Wide2x1));
            Assert.IsTrue(shapes.Contains(RoomFootprintShape.Tall1x2));
            Assert.IsTrue(shapes.Contains(RoomFootprintShape.Block2x2));
            Assert.IsTrue(shapes.Contains(RoomFootprintShape.L3Cell));
        }

        [Test]
        public void BranchGenerationUsesVerdantPoolWhenBiomeCoverageIsComplete()
        {
            var branchCatalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SampleRoomPath));
            var content = BranchSessionContent.Create(sample, branchCatalog, 0, out var error);

            Assert.IsEmpty(error);
            Assert.IsTrue(content.HasCompleteBiomePool(RoomBiomeIds.VerdantRuins));

            var pool = content.ResolveRoomPoolForBiome(RoomBiomeIds.VerdantRuins, out var usedFallback);
            var graph = BranchGenerator.CreateMacroFixtureBranch(pool, content.BranchSeed);

            Assert.IsFalse(usedFallback);
            Assert.IsTrue(graph.Rooms.All(room => room.RuntimeRoomAssetId.StartsWith("verdant_macro_", System.StringComparison.Ordinal)));
        }

        [Test]
        public void VerdantRuntimeRootUsesBiomeMaterialsAndVisualOnlyDecor()
        {
            var room = HollowRuntimeV2Importer.Import(File.ReadAllText(VerdantRuinsBiomeAssetGenerator.BiomeRoomDirectory + "/verdant_macro_single_1x1.hollowruntime.json"));
            var root = new GameObject("VerdantRuntimeRoot").AddComponent<RoomRuntimeRoot>();
            try
            {
                root.BuildFrom(room, RoomNavMeshRuntimeFallbackMode.RequireCatalogBake);

                Assert.AreEqual(RoomBiomeIds.VerdantRuins, root.BiomeId);
                var floor = GameObject.Find("tileGround.derived_full_floor");
                Assert.IsNotNull(floor);
                var floorRenderer = floor.GetComponent<Renderer>();
                Assert.IsNotNull(floorRenderer);
                Assert.AreEqual(
                    VerdantRuinsBiomeAssetGenerator.GrassStoneFloorTexturePath,
                    AssetDatabase.GetAssetPath(BaseTexture(floorRenderer.sharedMaterial)));

                var walls = root.GetComponent<RoomWallVisibilityController>();
                Assert.IsNotNull(walls);
                Assert.AreEqual(RoomBiomeIds.VerdantRuins, walls.BiomeId);

                foreach (var decorKind in new[]
                         {
                             RoomBiomeDecorKinds.GrassTuft,
                             RoomBiomeDecorKinds.CrystalCluster,
                             RoomBiomeDecorKinds.SmallTree,
                             RoomBiomeDecorKinds.StoneRuin
                         })
                {
                    var decor = FindChild(root.transform, decorKind);
                    Assert.IsNotNull(decor, $"Missing generated Verdant decor kind {decorKind}.");
                    Assert.IsEmpty(decor.GetComponentsInChildren<Collider>(includeInactive: true));
                    Assert.IsEmpty(decor.GetComponentsInChildren<Rigidbody>(includeInactive: true));
                }
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        private static RoomBiomeCatalogDefinition LoadBiomeCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomBiomeCatalogDefinition>(VerdantRuinsBiomeAssetGenerator.BiomeCatalogPath);
            Assert.IsNotNull(catalog);
            return catalog;
        }

        private static Texture BaseTexture(Material material)
        {
            if (material == null)
            {
                return null;
            }

            return material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
        }

        private static Transform FindChild(Transform root, string namePrefix)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child != root && child.name.StartsWith(namePrefix, System.StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }
    }
}
