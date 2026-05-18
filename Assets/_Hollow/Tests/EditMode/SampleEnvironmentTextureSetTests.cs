using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class SampleEnvironmentTextureSetTests
    {
        [Test]
        public void SampleBaseColorTexturesAreRepeatableImportedAssets()
        {
            foreach (var path in SampleEnvironmentTextureSetGenerator.BaseColorTexturePaths)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.IsNotNull(texture, path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, path);
                Assert.AreEqual(TextureImporterType.Default, importer.textureType, path);
                Assert.IsTrue(importer.sRGBTexture, path);
                Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode, path);
                Assert.AreEqual(FilterMode.Trilinear, importer.filterMode, path);
                Assert.IsTrue(importer.mipmapEnabled, path);
            }
        }

        [Test]
        public void ArtPassFloorMaterialsUseGeneratedBaseColorTextures()
        {
            AssertMaterialUsesTexture(
                SampleEnvironmentTextureSetGenerator.RoomFloorMaterialPath,
                SampleEnvironmentTextureSetGenerator.RoomFloorTexturePath);
            AssertMaterialUsesTexture(
                SampleEnvironmentTextureSetGenerator.DesignerGroundMaterialPath,
                SampleEnvironmentTextureSetGenerator.RoomFloorTexturePath);
            AssertMaterialUsesTexture(
                SampleEnvironmentTextureSetGenerator.RoomWallMaterialPath,
                SampleEnvironmentTextureSetGenerator.RoomWallTexturePath);
            AssertMaterialUsesTexture(
                SampleEnvironmentTextureSetGenerator.RoomWallTransparentMaterialPath,
                SampleEnvironmentTextureSetGenerator.RoomWallTexturePath);
            AssertMaterialUsesTexture(
                SampleEnvironmentTextureSetGenerator.StoneTrimMaterialPath,
                SampleEnvironmentTextureSetGenerator.StoneTrimTexturePath);
            AssertMaterialUsesTexture(
                SampleEnvironmentTextureSetGenerator.CaveGroundMaterialPath,
                SampleEnvironmentTextureSetGenerator.CaveGroundTexturePath);
            AssertMaterialUsesTexture(
                SampleEnvironmentTextureSetGenerator.PrototypeRoomWallMaterialPath,
                SampleEnvironmentTextureSetGenerator.RoomWallTexturePath);
            AssertMaterialUsesTexture(
                SampleEnvironmentTextureSetGenerator.PrototypeRoomWallTransparentMaterialPath,
                SampleEnvironmentTextureSetGenerator.RoomWallTexturePath);
        }

        [Test]
        public void WallTransparentMaterialsUseTransparentAlpha()
        {
            AssertTransparentWallMaterial(SampleEnvironmentTextureSetGenerator.RoomWallTransparentMaterialPath);
            AssertTransparentWallMaterial(SampleEnvironmentTextureSetGenerator.PrototypeRoomWallTransparentMaterialPath);
            AssertDoubleSidedWallMaterial(SampleEnvironmentTextureSetGenerator.RoomWallMaterialPath);
            AssertDoubleSidedWallMaterial(SampleEnvironmentTextureSetGenerator.RoomWallTransparentMaterialPath);
            AssertDoubleSidedWallMaterial(SampleEnvironmentTextureSetGenerator.PrototypeRoomWallMaterialPath);
            AssertDoubleSidedWallMaterial(SampleEnvironmentTextureSetGenerator.PrototypeRoomWallTransparentMaterialPath);
        }

        [Test]
        public void PresentationPalettesResolveSampleFloorAndWallMaterials()
        {
            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone23AssetGenerator.ArtPassPalettePath);
            Assert.IsNotNull(palette);

            Assert.IsTrue(palette.TryResolve(MaterialRole.RoomFloor, out var floorMaterial));
            Assert.AreEqual(
                SampleEnvironmentTextureSetGenerator.RoomFloorMaterialPath,
                AssetDatabase.GetAssetPath(floorMaterial));

            Assert.IsTrue(palette.TryResolve(MaterialRole.DesignerGround, out var designerGroundMaterial));
            Assert.AreEqual(
                SampleEnvironmentTextureSetGenerator.DesignerGroundMaterialPath,
                AssetDatabase.GetAssetPath(designerGroundMaterial));

            Assert.IsTrue(palette.TryResolve(MaterialRole.RoomWall, out var wallMaterial));
            Assert.AreEqual(
                SampleEnvironmentTextureSetGenerator.RoomWallMaterialPath,
                AssetDatabase.GetAssetPath(wallMaterial));

            Assert.IsTrue(palette.TryResolve(MaterialRole.RoomWallTransparent, out var transparentWallMaterial));
            Assert.AreEqual(
                SampleEnvironmentTextureSetGenerator.RoomWallTransparentMaterialPath,
                AssetDatabase.GetAssetPath(transparentWallMaterial));

            var prototypePalette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone9AssetGenerator.PalettePath);
            Assert.IsNotNull(prototypePalette);
            Assert.IsTrue(prototypePalette.TryResolve(MaterialRole.RoomWall, out var prototypeWallMaterial));
            Assert.AreEqual(
                SampleEnvironmentTextureSetGenerator.PrototypeRoomWallMaterialPath,
                AssetDatabase.GetAssetPath(prototypeWallMaterial));
            Assert.IsTrue(prototypePalette.TryResolve(MaterialRole.RoomWallTransparent, out var prototypeTransparentWallMaterial));
            Assert.AreEqual(
                SampleEnvironmentTextureSetGenerator.PrototypeRoomWallTransparentMaterialPath,
                AssetDatabase.GetAssetPath(prototypeTransparentWallMaterial));
        }

        private static void AssertMaterialUsesTexture(string materialPath, string texturePath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, materialPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Assert.IsNotNull(texture, texturePath);

            var baseMap = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
            Assert.IsNotNull(baseMap, materialPath);
            Assert.AreEqual(texturePath, AssetDatabase.GetAssetPath(baseMap), materialPath);
        }

        private static void AssertTransparentWallMaterial(string materialPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, materialPath);

            var color = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.color;
            Assert.AreEqual(0.32f, color.a, 0.001f, materialPath);
            if (material.HasProperty("_Surface"))
            {
                Assert.AreEqual(1f, material.GetFloat("_Surface"), 0.001f, materialPath);
            }

            Assert.GreaterOrEqual(material.renderQueue, (int)UnityEngine.Rendering.RenderQueue.Transparent, materialPath);
        }

        private static void AssertDoubleSidedWallMaterial(string materialPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, materialPath);
            if (material.HasProperty("_Cull"))
            {
                Assert.AreEqual(0f, material.GetFloat("_Cull"), 0.001f, materialPath);
            }
        }
    }
}
