using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone132BiomeWorldSelectionLockTests
    {
        private const string SampleRoomPath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void BiomeIdsAndRunFramingCatalogLockTheBetaWorldOrder()
        {
            CollectionAssert.AreEqual(
                new[] { RoomBiomeIds.BeforeTeeth, RoomBiomeIds.SunkenCartouche, RoomBiomeIds.RustChoir },
                Milestone132BiomeWorldSelectionLockAssetGenerator.BetaBiomeIds.ToArray());
            CollectionAssert.AreEqual(
                new[] { "Before Teeth", "The Sunken Cartouche", "The Rust Choir" },
                Milestone132BiomeWorldSelectionLockAssetGenerator.BetaWorldDisplayNames.ToArray());

            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(
                Milestone132BiomeWorldSelectionLockAssetGenerator.RunFramingCatalogPath);
            Assert.IsNotNull(catalog);
            Assert.AreEqual(Milestone132BiomeWorldSelectionLockAssetGenerator.CatalogId, catalog.CatalogId);

            var itinerary = RunWorldItineraryService.ResolveItinerary(catalog, runSeed: 13201, count: 3);
            CollectionAssert.AreEqual(
                new[] { "Before Teeth", "The Sunken Cartouche", "The Rust Choir" },
                itinerary.Select(world => world.DisplayName).ToArray());
            CollectionAssert.AreEqual(
                new[] { RoomBiomeIds.BeforeTeeth, RoomBiomeIds.SunkenCartouche, RoomBiomeIds.RustChoir },
                itinerary.Select(world => world.BiomeId).ToArray());
            Assert.IsFalse(itinerary.Any(world => RoomBiomeIds.Matches(world.BiomeId, RoomBiomeIds.HollowThreshold)));
        }

        [Test]
        public void TextureSourcesExistAt1024AndUseLockedPbrImportPolicy()
        {
            foreach (var path in Milestone132BiomeWorldSelectionLockAssetGenerator.RequiredTexturePaths)
            {
                Assert.IsTrue(File.Exists(path), path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.IsNotNull(texture, path);
                Assert.AreEqual(Milestone132BiomeWorldSelectionLockAssetGenerator.TextureSize, texture.width, path);
                Assert.AreEqual(Milestone132BiomeWorldSelectionLockAssetGenerator.TextureSize, texture.height, path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, path);
                Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode, path);
                Assert.IsTrue(importer.mipmapEnabled, path);
                Assert.AreEqual(FilterMode.Trilinear, importer.filterMode, path);

                if (path.Contains("_BaseColor"))
                {
                    Assert.AreEqual(TextureImporterType.Default, importer.textureType, path);
                    Assert.IsTrue(importer.sRGBTexture, path);
                }
                else if (path.Contains("_Normal"))
                {
                    Assert.AreEqual(TextureImporterType.NormalMap, importer.textureType, path);
                    Assert.IsFalse(importer.sRGBTexture, path);
                }
                else if (path.Contains("_Mask"))
                {
                    Assert.AreEqual(TextureImporterType.Default, importer.textureType, path);
                    Assert.IsFalse(importer.sRGBTexture, path);
                    Assert.AreEqual(TextureImporterAlphaSource.FromInput, importer.alphaSource, path);
                }
            }
        }

        [Test]
        public void BiomeCatalogResolvesSelectedWorldsWithMaterialAndRoomCoverage()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomBiomeCatalogDefinition>(
                Milestone132BiomeWorldSelectionLockAssetGenerator.BiomeCatalogPath);
            Assert.IsNotNull(catalog);

            foreach (var biomeId in Milestone132BiomeWorldSelectionLockAssetGenerator.BetaBiomeIds)
            {
                Assert.IsTrue(catalog.TryGetBiome(biomeId, out var biome), biomeId);
                Assert.IsNotNull(biome);
                Assert.AreEqual(5, biome.RoomTemplates.Count, biomeId);

                foreach (var role in Milestone132BiomeWorldSelectionLockAssetGenerator.RequiredMaterialRoles)
                {
                    Assert.IsTrue(biome.TryResolve(role, out var material), $"{biomeId} missing {role}");
                    Assert.IsNotNull(material, $"{biomeId} null {role}");
                    Assert.IsNotNull(BaseTexture(material), $"{biomeId} material {role} has no base map.");
                    Assert.IsNotNull(Texture(material, "_BumpMap"), $"{biomeId} material {role} has no normal map.");
                    Assert.IsNotNull(Texture(material, "_MetallicGlossMap"), $"{biomeId} material {role} has no metallic/smoothness mask.");
                    Assert.IsNotNull(Texture(material, "_OcclusionMap"), $"{biomeId} material {role} has no occlusion mask.");
                    if (role == MaterialRole.RoomWallTransparent)
                    {
                        Assert.Less(material.color.a, 0.5f, $"{biomeId} transparent wall should retain wall alpha behavior.");
                    }
                }

                var shapes = biome.RoomTemplates
                    .Select(template => HollowRuntimeV2Importer.Import(template.text))
                    .Select(asset =>
                    {
                        Assert.AreEqual(biomeId, asset.BiomeId);
                        return RoomFootprintShapeUtility.Classify(asset.Footprint);
                    })
                    .ToHashSet();

                Assert.IsTrue(shapes.Contains(RoomFootprintShape.Single1x1), biomeId);
                Assert.IsTrue(shapes.Contains(RoomFootprintShape.Wide2x1), biomeId);
                Assert.IsTrue(shapes.Contains(RoomFootprintShape.Tall1x2), biomeId);
                Assert.IsTrue(shapes.Contains(RoomFootprintShape.Block2x2), biomeId);
                Assert.IsTrue(shapes.Contains(RoomFootprintShape.L3Cell), biomeId);
            }
        }

        [Test]
        public void BranchGenerationUsesSelectedBiomePoolsAndCorruptedRoomsStayAshenShrine()
        {
            var branchCatalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SampleRoomPath));
            var content = BranchSessionContent.Create(sample, branchCatalog, 13201, out var error);
            Assert.IsEmpty(error);

            foreach (var biomeId in Milestone132BiomeWorldSelectionLockAssetGenerator.BetaBiomeIds)
            {
                Assert.IsTrue(content.HasCompleteBiomePool(biomeId), biomeId);
                var pool = content.ResolveRoomPoolForBiome(biomeId, out var usedFallback);
                Assert.IsFalse(usedFallback, biomeId);
                Assert.IsTrue(pool.Values.All(asset => RoomBiomeIds.Matches(asset.BiomeId, biomeId)), biomeId);
            }

            var corrupted = AssetDatabase.LoadAssetAtPath<TextAsset>(CorruptedAshenShrineBiomeAssetGenerator.CorruptedRoomPath);
            Assert.IsNotNull(corrupted);
            Assert.AreEqual(RoomBiomeIds.CorruptedAshenShrine, HollowRuntimeV2Importer.Import(corrupted.text).BiomeId);
            Assert.IsTrue(content.TryGetRoomAsset(BranchGenerator.WaveRoomAssetId, RoomBiomeIds.RustChoir, out var wave));
            Assert.AreEqual(RoomBiomeIds.RustChoir, wave.BiomeId);
        }

        [Test]
        public void RoomDesignerPreviewSupportsBetaBiomesAndKeepsChestAffordancesGlobal()
        {
            foreach (var biomeId in Milestone132BiomeWorldSelectionLockAssetGenerator.BetaBiomeIds)
            {
                var floorHost = new GameObject($"{biomeId}_floor_preview");
                var goldenChestHost = new GameObject($"{biomeId}_golden_chest_preview");
                try
                {
                    Assert.IsTrue(RoomDesignerScenePreviewBuilder.BuildVisualForCell(
                        floorHost,
                        new RoomDesignerCell(0, 0, 0, RoomDesignerCellKinds.Ground),
                        biomeId));
                    Assert.IsTrue(RoomDesignerScenePreviewBuilder.BuildVisualForMarker(
                        goldenChestHost,
                        new RoomDesignerMarker("spawn_golden_chest", RoomDesignerMarkerKinds.GoldenChestSpawn, 0f, 0f, 0f),
                        biomeId));

                    var catalog = AssetDatabase.LoadAssetAtPath<RoomBiomeCatalogDefinition>(
                        Milestone132BiomeWorldSelectionLockAssetGenerator.BiomeCatalogPath);
                    Assert.IsTrue(catalog.TryGetBiome(biomeId, out var biome));
                    Assert.IsFalse(biome.TryResolve(PresentationPrefabRole.ChestNormal, out _));
                    Assert.IsFalse(biome.TryResolve(PresentationPrefabRole.ChestGolden, out _));
                    Assert.IsFalse(biome.TryResolve(PresentationPrefabRole.ChestCorrupted, out _));
                    Assert.IsNotNull(RoomBiomePresentationResolver.ResolvePrefab(biomeId, PresentationPrefabRole.ChestGolden));
                }
                finally
                {
                    Object.DestroyImmediate(floorHost);
                    Object.DestroyImmediate(goldenChestHost);
                }
            }
        }

        [Test]
        public void BranchPortalStoresAndUsesBiomeTrimIdentity()
        {
            var host = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            try
            {
                var portal = host.AddComponent<NextBranchPortal>();
                portal.Configure(
                    NextBranchChoice.CreateWorldBranch(13201, 2, 0, HubBranchPortalState.Open),
                    "Lapis Teeth",
                    RoomBiomeIds.SunkenCartouche);

                Assert.AreEqual(RoomBiomeIds.SunkenCartouche, portal.BranchBiomeId);
                Assert.AreEqual("Lapis Teeth", portal.DisplayLabel);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void LiveReportPassesAndGeneratedReportsUseM132LockId()
        {
            var report = Milestone132BiomeWorldSelectionLockAssetGenerator.BuildReport();
            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.AreEqual(report.totalChecks, report.passedChecks);

            Assert.IsTrue(File.Exists(Milestone132BiomeWorldSelectionLockAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone132BiomeWorldSelectionLockAssetGenerator.ReportJsonPath));
            var markdown = File.ReadAllText(Milestone132BiomeWorldSelectionLockAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M132 Biome + World Selection Lock Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone132BiomeWorldSelectionLockAssetGenerator.LockId, markdown);

            var json = JsonUtility.FromJson<Milestone132BiomeWorldSelectionLockReport>(
                File.ReadAllText(Milestone132BiomeWorldSelectionLockAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone132BiomeWorldSelectionLockAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
        }

        [Test]
        public void ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone132BiomeWorldSelectionLockValidator.Validate(exitOnFailure: false));
        }

        private static Texture BaseTexture(Material material)
        {
            if (material == null)
            {
                return null;
            }

            return material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
        }

        private static Texture Texture(Material material, string propertyName)
        {
            return material != null && material.HasProperty(propertyName) ? material.GetTexture(propertyName) : null;
        }
    }
}
