using System;
using System.Collections.Generic;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class SampleEnvironmentTextureSetGenerator
    {
        public const int TextureSize = 512;
        public const string TextureDirectory = "Assets/_Hollow/Art/Textures/SampleEnvironment";
        public const string RoomFloorTexturePath = TextureDirectory + "/T_RoomFloor_StoneSlabs_BaseColor.png";
        public const string RoomWallTexturePath = TextureDirectory + "/T_RoomWall_BlueGreyBlocks_BaseColor.png";
        public const string StoneTrimTexturePath = TextureDirectory + "/T_StoneTrim_Chiseled_BaseColor.png";
        public const string CaveGroundTexturePath = TextureDirectory + "/T_Ground_Rubble_BaseColor.png";

        public const string RoomFloorMaterialPath = Milestone23AssetGenerator.ArtPassMaterialDirectory + "/AP_M_RoomFloor.mat";
        public const string DesignerGroundMaterialPath = Milestone23AssetGenerator.ArtPassMaterialDirectory + "/AP_M_DesignerGround.mat";
        public const string RoomWallMaterialPath = Milestone23AssetGenerator.ArtPassMaterialDirectory + "/AP_M_RoomWall.mat";
        public const string RoomWallTransparentMaterialPath = Milestone23AssetGenerator.ArtPassMaterialDirectory + "/AP_M_RoomWallTransparent.mat";
        public const string StoneTrimMaterialPath = Milestone23AssetGenerator.ArtPassMaterialDirectory + "/AP_M_SampleStoneTrim.mat";
        public const string CaveGroundMaterialPath = Milestone23AssetGenerator.ArtPassMaterialDirectory + "/AP_M_SampleRubbleGround.mat";
        public const string PrototypeRoomWallMaterialPath = Milestone9AssetGenerator.MaterialDirectory + "/M_RoomWall.mat";
        public const string PrototypeRoomWallTransparentMaterialPath = Milestone9AssetGenerator.MaterialDirectory + "/M_RoomWallTransparent.mat";

        public static readonly string[] BaseColorTexturePaths =
        {
            RoomFloorTexturePath,
            RoomWallTexturePath,
            StoneTrimTexturePath,
            CaveGroundTexturePath
        };

        public static readonly string[] SampleMaterialPaths =
        {
            RoomFloorMaterialPath,
            DesignerGroundMaterialPath,
            RoomWallMaterialPath,
            RoomWallTransparentMaterialPath,
            StoneTrimMaterialPath,
            CaveGroundMaterialPath,
            PrototypeRoomWallMaterialPath,
            PrototypeRoomWallTransparentMaterialPath
        };

        [MenuItem("Hollow/ArtPass/Generate Sample Environment BaseColor Textures")]
        public static void Generate()
        {
            GenerateAssets();
        }

        public static void GenerateAssets(bool saveAssets = true, bool refresh = true)
        {
            Directory.CreateDirectory(TextureDirectory);
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassMaterialDirectory);
            Directory.CreateDirectory(Milestone9AssetGenerator.MaterialDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(Milestone23AssetGenerator.ArtPassPalettePath) ?? "Assets/_Hollow/Data/Presentation");
            Directory.CreateDirectory(Path.GetDirectoryName(Milestone9AssetGenerator.PalettePath) ?? "Assets/_Hollow/Data/Presentation");

            WriteBaseColorTexture(RoomFloorTexturePath, SampleFloorPixel);
            WriteBaseColorTexture(RoomWallTexturePath, SampleWallPixel);
            WriteBaseColorTexture(StoneTrimTexturePath, SampleTrimPixel);
            WriteBaseColorTexture(CaveGroundTexturePath, SampleRubblePixel);

            var floorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RoomFloorTexturePath);
            var wallTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RoomWallTexturePath);
            var trimTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(StoneTrimTexturePath);
            var rubbleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CaveGroundTexturePath);

            var floorMaterial = CreateOrUpdateLitMaterial(
                RoomFloorMaterialPath,
                "AP_M_RoomFloor",
                floorTexture,
                new Vector2(6f, 6f),
                0.24f);
            var designerGroundMaterial = CreateOrUpdateLitMaterial(
                DesignerGroundMaterialPath,
                "AP_M_DesignerGround",
                floorTexture,
                new Vector2(6f, 6f),
                0.24f);
            var wallMaterial = CreateOrUpdateLitMaterial(
                RoomWallMaterialPath,
                "AP_M_RoomWall",
                wallTexture,
                new Vector2(4f, 4f),
                0.18f,
                doubleSided: true);
            var transparentWallMaterial = CreateOrUpdateLitMaterial(
                RoomWallTransparentMaterialPath,
                "AP_M_RoomWallTransparent",
                wallTexture,
                new Vector2(4f, 4f),
                0.18f,
                new Color(1f, 1f, 1f, RoomWallVisibilityController.TransparentAlpha),
                transparent: true,
                doubleSided: true);
            var prototypeWallMaterial = CreateOrUpdateLitMaterial(
                PrototypeRoomWallMaterialPath,
                "M_RoomWall",
                wallTexture,
                new Vector2(4f, 4f),
                0.18f,
                doubleSided: true);
            var prototypeTransparentWallMaterial = CreateOrUpdateLitMaterial(
                PrototypeRoomWallTransparentMaterialPath,
                "M_RoomWallTransparent",
                wallTexture,
                new Vector2(4f, 4f),
                0.18f,
                new Color(1f, 1f, 1f, RoomWallVisibilityController.TransparentAlpha),
                transparent: true,
                doubleSided: true);
            CreateOrUpdateLitMaterial(
                StoneTrimMaterialPath,
                "AP_M_SampleStoneTrim",
                trimTexture,
                new Vector2(3f, 3f),
                0.2f);
            CreateOrUpdateLitMaterial(
                CaveGroundMaterialPath,
                "AP_M_SampleRubbleGround",
                rubbleTexture,
                new Vector2(5f, 5f),
                0.12f);

            UpsertArtPassPaletteBinding(MaterialRole.RoomFloor, floorMaterial, new Color(0.25f, 0.28f, 0.27f, 1f));
            UpsertArtPassPaletteBinding(MaterialRole.DesignerGround, designerGroundMaterial, new Color(0.25f, 0.28f, 0.27f, 1f));
            UpsertArtPassPaletteBinding(MaterialRole.RoomWall, wallMaterial, MaterialResolver.FallbackColorFor(MaterialRole.RoomWall));
            UpsertArtPassPaletteBinding(MaterialRole.RoomWallTransparent, transparentWallMaterial, MaterialResolver.FallbackColorFor(MaterialRole.RoomWallTransparent));
            UpsertPaletteBinding(Milestone9AssetGenerator.PalettePath, MaterialRole.RoomWall, prototypeWallMaterial, MaterialResolver.FallbackColorFor(MaterialRole.RoomWall));
            UpsertPaletteBinding(Milestone9AssetGenerator.PalettePath, MaterialRole.RoomWallTransparent, prototypeTransparentWallMaterial, MaterialResolver.FallbackColorFor(MaterialRole.RoomWallTransparent));

            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
            }

            if (refresh)
            {
                AssetDatabase.Refresh();
            }

            Debug.Log("Generated sample environment BaseColor textures and applied floor and wall sets to the presentation palettes.");
        }

        public static void ValidateGeneratedAssetsBatch()
        {
            try
            {
                ValidateGeneratedAssets();
                Debug.Log("Sample environment BaseColor texture validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void ValidateGeneratedAssets()
        {
            foreach (var path in BaseColorTexturePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                {
                    throw new InvalidOperationException($"Missing generated BaseColor texture: {path}");
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"Missing texture importer for generated BaseColor texture: {path}");
                }

                if (importer.textureType != TextureImporterType.Default ||
                    !importer.sRGBTexture ||
                    importer.wrapMode != TextureWrapMode.Repeat ||
                    importer.filterMode != FilterMode.Trilinear ||
                    !importer.mipmapEnabled)
                {
                    throw new InvalidOperationException($"Unexpected importer settings for generated BaseColor texture: {path}");
                }
            }

            ValidateMaterialTexture(RoomFloorMaterialPath, RoomFloorTexturePath);
            ValidateMaterialTexture(DesignerGroundMaterialPath, RoomFloorTexturePath);
            ValidateMaterialTexture(RoomWallMaterialPath, RoomWallTexturePath);
            ValidateMaterialTexture(RoomWallTransparentMaterialPath, RoomWallTexturePath);
            ValidateMaterialTexture(StoneTrimMaterialPath, StoneTrimTexturePath);
            ValidateMaterialTexture(CaveGroundMaterialPath, CaveGroundTexturePath);
            ValidateMaterialTexture(PrototypeRoomWallMaterialPath, RoomWallTexturePath);
            ValidateMaterialTexture(PrototypeRoomWallTransparentMaterialPath, RoomWallTexturePath);
            ValidateTransparentMaterial(RoomWallTransparentMaterialPath);
            ValidateTransparentMaterial(PrototypeRoomWallTransparentMaterialPath);
            ValidateDoubleSidedMaterial(RoomWallMaterialPath);
            ValidateDoubleSidedMaterial(RoomWallTransparentMaterialPath);
            ValidateDoubleSidedMaterial(PrototypeRoomWallMaterialPath);
            ValidateDoubleSidedMaterial(PrototypeRoomWallTransparentMaterialPath);

            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone23AssetGenerator.ArtPassPalettePath);
            if (palette == null ||
                !palette.TryResolve(MaterialRole.RoomFloor, out var floorMaterial) ||
                AssetDatabase.GetAssetPath(floorMaterial) != RoomFloorMaterialPath ||
                !palette.TryResolve(MaterialRole.DesignerGround, out var designerGroundMaterial) ||
                AssetDatabase.GetAssetPath(designerGroundMaterial) != DesignerGroundMaterialPath ||
                !palette.TryResolve(MaterialRole.RoomWall, out var wallMaterial) ||
                AssetDatabase.GetAssetPath(wallMaterial) != RoomWallMaterialPath ||
                !palette.TryResolve(MaterialRole.RoomWallTransparent, out var wallTransparentMaterial) ||
                AssetDatabase.GetAssetPath(wallTransparentMaterial) != RoomWallTransparentMaterialPath)
            {
                throw new InvalidOperationException("ArtPass material palette does not resolve generated floor and wall materials.");
            }

            var prototypePalette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone9AssetGenerator.PalettePath);
            if (prototypePalette == null ||
                !prototypePalette.TryResolve(MaterialRole.RoomWall, out var prototypeWallMaterial) ||
                AssetDatabase.GetAssetPath(prototypeWallMaterial) != PrototypeRoomWallMaterialPath ||
                !prototypePalette.TryResolve(MaterialRole.RoomWallTransparent, out var prototypeWallTransparentMaterial) ||
                AssetDatabase.GetAssetPath(prototypeWallTransparentMaterial) != PrototypeRoomWallTransparentMaterialPath)
            {
                throw new InvalidOperationException("Prototype material palette does not resolve generated wall materials.");
            }
        }

        private static void ValidateMaterialTexture(string materialPath, string texturePath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Missing generated sample material: {materialPath}");
            }

            var texture = material.HasProperty("_BaseMap")
                ? material.GetTexture("_BaseMap")
                : material.mainTexture;
            if (texture == null || AssetDatabase.GetAssetPath(texture) != texturePath)
            {
                throw new InvalidOperationException($"{materialPath} is not using generated BaseColor texture {texturePath}.");
            }
        }

        private static void ValidateTransparentMaterial(string materialPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Missing generated transparent wall material: {materialPath}");
            }

            var color = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.color;
            if (!Mathf.Approximately(color.a, RoomWallVisibilityController.TransparentAlpha))
            {
                throw new InvalidOperationException($"{materialPath} alpha should be {RoomWallVisibilityController.TransparentAlpha:0.00}.");
            }

            if (material.HasProperty("_Surface") && material.GetFloat("_Surface") < 0.5f)
            {
                throw new InvalidOperationException($"{materialPath} should use a transparent URP surface.");
            }
        }

        private static void ValidateDoubleSidedMaterial(string materialPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Missing generated wall material: {materialPath}");
            }

            if (material.HasProperty("_Cull") && !Mathf.Approximately(material.GetFloat("_Cull"), 0f))
            {
                throw new InvalidOperationException($"{materialPath} should render both sides so room-facing walls keep their texture.");
            }
        }

        private static void WriteBaseColorTexture(string path, Func<float, float, Color> sample)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: true, linear: false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                name = Path.GetFileNameWithoutExtension(path)
            };
            var pixels = new Color32[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++)
            {
                var v = y / (float)TextureSize;
                for (var x = 0; x < TextureSize; x++)
                {
                    var u = x / (float)TextureSize;
                    pixels[y * TextureSize + x] = sample(u, v);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = TextureSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Material CreateOrUpdateLitMaterial(
            string path,
            string materialName,
            Texture texture,
            Vector2 textureScale,
            float smoothness,
            Color? baseColor = null,
            bool transparent = false,
            bool doubleSided = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = materialName;
            var color = baseColor ?? Color.white;
            material.color = color;
            if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            SetTexture(material, "_BaseMap", texture, textureScale);
            SetTexture(material, "_MainTex", texture, textureScale);
            SetColor(material, "_BaseColor", color);
            SetColor(material, "_Color", color);
            SetFloat(material, "_Metallic", 0f);
            SetFloat(material, "_Smoothness", smoothness);
            SetFloat(material, "_Glossiness", smoothness);
            ConfigureSurface(material, transparent, doubleSided);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void UpsertArtPassPaletteBinding(MaterialRole role, Material material, Color fallbackColor)
        {
            UpsertPaletteBinding(Milestone23AssetGenerator.ArtPassPalettePath, role, material, fallbackColor);
        }

        private static void UpsertPaletteBinding(string palettePath, MaterialRole role, Material material, Color fallbackColor)
        {
            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(palettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MaterialPaletteDefinition>();
                AssetDatabase.CreateAsset(palette, palettePath);
            }

            var bindings = new List<MaterialRoleBinding>(palette.Bindings ?? Array.Empty<MaterialRoleBinding>());
            var index = bindings.FindIndex(binding => binding.Role == role);
            var replacement = new MaterialRoleBinding(role, material, fallbackColor);
            if (index >= 0)
            {
                bindings[index] = replacement;
            }
            else
            {
                bindings.Add(replacement);
            }

            palette.Configure(bindings.ToArray());
            EditorUtility.SetDirty(palette);
        }

        private static void ConfigureSurface(Material material, bool transparent, bool doubleSided)
        {
            SetFloat(material, "_Cull", doubleSided ? 0f : 2f);
            if (transparent)
            {
                SetFloat(material, "_Surface", 1f);
                SetFloat(material, "_Blend", 0f);
                SetFloat(material, "_AlphaClip", 0f);
                SetFloat(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                SetFloat(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                SetFloat(material, "_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON");
                return;
            }

            SetFloat(material, "_Surface", 0f);
            SetFloat(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            SetFloat(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            SetFloat(material, "_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        private static void SetTexture(Material material, string propertyName, Texture texture, Vector2 scale)
        {
            if (!material.HasProperty(propertyName))
            {
                return;
            }

            material.SetTexture(propertyName, texture);
            material.SetTextureScale(propertyName, scale);
            material.SetTextureOffset(propertyName, Vector2.zero);
        }

        private static void SetColor(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static Color SampleFloorPixel(float u, float v)
        {
            var gridU = u * 4f;
            var gridV = v * 4f;
            var cellX = Mathf.FloorToInt(gridU);
            var cellY = Mathf.FloorToInt(gridV);
            var localU = Frac(gridU);
            var localV = Frac(gridV);
            var edge = Mathf.Min(Mathf.Min(localU, 1f - localU), Mathf.Min(localV, 1f - localV));
            var mortar = 1f - Smooth01(0.025f, 0.055f, edge);
            var slabVariation = Hash01(cellX, cellY, 17) * 0.16f - 0.08f;
            var grain = (TileNoise(u, v, 16, 41) - 0.5f) * 0.1f;
            var wear = TileNoise(u + 0.17f, v + 0.31f, 32, 81);

            var color = new Color(0.29f + slabVariation + grain, 0.32f + slabVariation + grain, 0.31f + slabVariation + grain, 1f);
            if (wear > 0.72f && edge > 0.09f)
            {
                color = Color.Lerp(color, new Color(0.43f, 0.44f, 0.4f, 1f), 0.2f);
            }

            return ClampColor(Color.Lerp(color, new Color(0.12f, 0.13f, 0.12f, 1f), mortar * 0.86f));
        }

        private static Color SampleWallPixel(float u, float v)
        {
            var rows = 6f;
            var columns = 5f;
            var row = Mathf.FloorToInt(v * rows);
            var shiftedU = u + ((row & 1) == 1 ? 0.5f / columns : 0f);
            var localU = Frac(shiftedU * columns);
            var localV = Frac(v * rows);
            var edge = Mathf.Min(Mathf.Min(localU, 1f - localU), Mathf.Min(localV, 1f - localV));
            var mortar = 1f - Smooth01(0.035f, 0.07f, edge);
            var blockX = Mathf.FloorToInt(shiftedU * columns);
            var blockColor = Hash01(blockX, row, 103) * 0.18f - 0.08f;
            var pits = TileNoise(u * 1.3f + 0.22f, v * 1.1f + 0.11f, 24, 203) - 0.5f;

            var color = new Color(0.28f + blockColor + pits * 0.08f, 0.31f + blockColor + pits * 0.08f, 0.34f + blockColor + pits * 0.1f, 1f);
            return ClampColor(Color.Lerp(color, new Color(0.1f, 0.11f, 0.12f, 1f), mortar * 0.9f));
        }

        private static Color SampleTrimPixel(float u, float v)
        {
            var band = Mathf.Abs(Frac(v * 4f) - 0.5f) * 2f;
            var groove = 1f - Smooth01(0.05f, 0.13f, Mathf.Min(Frac(v * 4f), 1f - Frac(v * 4f)));
            var chip = TileNoise(u, v, 20, 409) - 0.5f;
            var color = new Color(0.25f + chip * 0.12f, 0.27f + chip * 0.1f, 0.27f + chip * 0.09f, 1f);
            color = Color.Lerp(color, new Color(0.38f, 0.38f, 0.34f, 1f), (1f - band) * 0.18f);
            return ClampColor(Color.Lerp(color, new Color(0.08f, 0.08f, 0.08f, 1f), groove * 0.75f));
        }

        private static Color SampleRubblePixel(float u, float v)
        {
            var coarse = TileNoise(u, v, 10, 607);
            var fine = TileNoise(u + 0.43f, v + 0.19f, 34, 911);
            var speckle = Hash01(Mathf.FloorToInt(u * TextureSize), Mathf.FloorToInt(v * TextureSize), 37);
            var color = new Color(0.22f, 0.21f, 0.19f, 1f);
            color = Color.Lerp(color, new Color(0.36f, 0.34f, 0.29f, 1f), coarse * 0.45f + fine * 0.25f);
            if (speckle > 0.988f)
            {
                color = Color.Lerp(color, new Color(0.52f, 0.5f, 0.44f, 1f), 0.45f);
            }

            return ClampColor(color);
        }

        private static float TileNoise(float u, float v, int cells, int seed)
        {
            u = Frac(u);
            v = Frac(v);
            var x = u * cells;
            var y = v * cells;
            var x0 = Mod(Mathf.FloorToInt(x), cells);
            var y0 = Mod(Mathf.FloorToInt(y), cells);
            var x1 = (x0 + 1) % cells;
            var y1 = (y0 + 1) % cells;
            var tx = Smooth01(0f, 1f, Frac(x));
            var ty = Smooth01(0f, 1f, Frac(y));

            var a = Mathf.Lerp(Hash01(x0, y0, seed), Hash01(x1, y0, seed), tx);
            var b = Mathf.Lerp(Hash01(x0, y1, seed), Hash01(x1, y1, seed), tx);
            return Mathf.Lerp(a, b, ty);
        }

        private static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return (h & 0x00FFFFFF) / 16777215f;
            }
        }

        private static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }

        private static int Mod(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private static float Smooth01(float from, float to, float value)
        {
            var t = Mathf.Clamp01((value - from) / Mathf.Max(0.0001f, to - from));
            return t * t * (3f - 2f * t);
        }

        private static Color ClampColor(Color color)
        {
            return new Color(
                Mathf.Clamp01(color.r),
                Mathf.Clamp01(color.g),
                Mathf.Clamp01(color.b),
                Mathf.Clamp01(color.a));
        }
    }
}
