using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class CorruptedAshenShrineBiomeAssetGenerator
    {
        public const int TextureSize = 1024;
        public const string TextureDirectory = "Assets/_Hollow/Art/Textures/CorruptedAshenShrine";
        public const string BiomePrefabDirectory = "Assets/_Hollow/Prefabs/ArtPass/Biomes/CorruptedAshenShrine";
        public const string BiomeResourceDirectory = "Assets/_Hollow/Resources/Hollow/Biomes";
        public const string FloorTexturePath = TextureDirectory + "/T_CorruptedAshenShrine_Floor_BaseColor.png";
        public const string WallTexturePath = TextureDirectory + "/T_CorruptedAshenShrine_Wall_BaseColor.png";
        public const string BiomePath = BiomeResourceDirectory + "/Biome_CorruptedAshenShrine.asset";
        public const string BiomeCatalogPath = BiomeResourceDirectory + "/RoomBiomeCatalog.asset";
        public const string CorruptedRoomPath = "Assets/_Hollow/Data/Rooms/MacroFixtures/corrupted_chest_single_1x1.hollowruntime.json";

        private static readonly MaterialSpec[] MaterialSpecs =
        {
            new(MaterialRole.RoomFloor, "AP_M_CorruptedAshenShrine_RoomFloor", FloorTexturePath, new Vector2(7f, 5f), 0.24f, Color.white),
            new(MaterialRole.RoomWall, "AP_M_CorruptedAshenShrine_RoomWall", WallTexturePath, new Vector2(4f, 4f), 0.18f, Color.white, doubleSided: true),
            new(MaterialRole.RoomWallTransparent, "AP_M_CorruptedAshenShrine_RoomWallTransparent", WallTexturePath, new Vector2(4f, 4f), 0.18f, new Color(1f, 1f, 1f, RoomWallVisibilityController.TransparentAlpha), transparent: true, doubleSided: true),
            new(MaterialRole.RoomObstacleRock, "AP_M_CorruptedAshenShrine_RoomObstacleRock", FloorTexturePath, new Vector2(2.4f, 2.4f), 0.22f, new Color(0.72f, 0.7f, 0.64f, 1f)),
            new(MaterialRole.DoorActive, "AP_M_CorruptedAshenShrine_DoorActive", WallTexturePath, new Vector2(2f, 2f), 0.24f, new Color(0.88f, 0.78f, 0.52f, 1f)),
            new(MaterialRole.DoorCleared, "AP_M_CorruptedAshenShrine_DoorCleared", WallTexturePath, new Vector2(2f, 2f), 0.2f, new Color(0.78f, 0.76f, 0.7f, 1f)),
            new(MaterialRole.DoorLocked, "AP_M_CorruptedAshenShrine_DoorLocked", WallTexturePath, new Vector2(2f, 2f), 0.18f, new Color(0.42f, 0.36f, 0.32f, 1f)),
            new(MaterialRole.DoorUnavailable, "AP_M_CorruptedAshenShrine_DoorUnavailable", WallTexturePath, new Vector2(2f, 2f), 0.12f, new Color(0.36f, 0.34f, 0.32f, 0.92f)),
            new(MaterialRole.DecorGrassTuft, "AP_M_CorruptedAshenShrine_DecorAshPile", FloorTexturePath, Vector2.one, 0.18f, new Color(0.65f, 0.63f, 0.58f, 1f)),
            new(MaterialRole.DecorCrystalCluster, "AP_M_CorruptedAshenShrine_DecorSigilShard", WallTexturePath, Vector2.one, 0.45f, new Color(0.9f, 0.78f, 0.45f, 1f)),
            new(MaterialRole.DecorSmallTree, "AP_M_CorruptedAshenShrine_DecorAshenObelisk", WallTexturePath, new Vector2(1.2f, 1.2f), 0.22f, new Color(0.76f, 0.72f, 0.66f, 1f)),
            new(MaterialRole.DecorStoneRuin, "AP_M_CorruptedAshenShrine_DecorBrokenShrineStones", WallTexturePath, new Vector2(1.4f, 1.4f), 0.18f, new Color(0.68f, 0.65f, 0.58f, 1f))
        };

        [MenuItem("Hollow/Biomes/Generate Corrupted Ashen Shrine Pack")]
        public static void Generate()
        {
            GenerateAssets();
        }

        public static void GenerateBatch()
        {
            try
            {
                GenerateAssets();
                Debug.Log("Corrupted Ashen Shrine biome pack generation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void ValidateGeneratedAssetsBatch()
        {
            try
            {
                ValidateGeneratedAssets();
                Debug.Log("Corrupted Ashen Shrine biome pack validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void GenerateAssets(bool saveAssets = true, bool refresh = true)
        {
            EnsureDirectories();
            ConfigureSourceTextures();
            var materials = GenerateMaterials();
            var prefabs = GenerateDecorPrefabs(materials);
            GenerateBiomeCatalog(materials, prefabs);
            PatchCorruptedRoomBiome();

            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
            }

            if (refresh)
            {
                AssetDatabase.Refresh();
            }
        }

        public static void ValidateGeneratedAssets()
        {
            CorruptedAshenShrineBiomePackValidator.ValidateOrThrow();
        }

        public static string MaterialPath(string materialName)
        {
            return $"{Milestone23AssetGenerator.ArtPassMaterialDirectory}/{materialName}.mat";
        }

        public static IReadOnlyList<MaterialRole> RequiredMaterialRoles => MaterialSpecs.Select(spec => spec.Role).ToArray();

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(TextureDirectory);
            Directory.CreateDirectory(BiomePrefabDirectory);
            Directory.CreateDirectory(BiomeResourceDirectory);
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassMaterialDirectory);
        }

        private static void ConfigureSourceTextures()
        {
            foreach (var path in new[] { FloorTexturePath, WallTexturePath })
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"Missing Ashen Shrine source texture: {path}");
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    throw new InvalidOperationException($"Ashen Shrine source texture is not importable: {path}");
                }

                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = TextureSize;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<MaterialRole, Material> GenerateMaterials()
        {
            var materials = new Dictionary<MaterialRole, Material>();
            foreach (var spec in MaterialSpecs)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.TexturePath);
                if (texture == null)
                {
                    throw new InvalidOperationException($"Missing Ashen Shrine texture for {spec.Role}: {spec.TexturePath}");
                }

                materials[spec.Role] = CreateOrUpdateLitMaterial(spec, texture);
            }

            return materials;
        }

        private static Dictionary<PresentationPrefabRole, GameObject> GenerateDecorPrefabs(IReadOnlyDictionary<MaterialRole, Material> materials)
        {
            return new Dictionary<PresentationPrefabRole, GameObject>
            {
                [PresentationPrefabRole.DecorGrassTuft] = CreateAshPilePrefab(materials[MaterialRole.DecorGrassTuft]),
                [PresentationPrefabRole.DecorCrystalCluster] = CreateSigilShardPrefab(materials[MaterialRole.DecorCrystalCluster]),
                [PresentationPrefabRole.DecorSmallTree] = CreateAshenObeliskPrefab(materials[MaterialRole.DecorSmallTree]),
                [PresentationPrefabRole.DecorStoneRuin] = CreateBrokenShrineStonesPrefab(materials[MaterialRole.DecorStoneRuin])
            };
        }

        private static void GenerateBiomeCatalog(
            IReadOnlyDictionary<MaterialRole, Material> materials,
            IReadOnlyDictionary<PresentationPrefabRole, GameObject> prefabs)
        {
            var biome = LoadOrCreate<RoomBiomeDefinition>(BiomePath);
            biome.Configure(
                RoomBiomeIds.CorruptedAshenShrine,
                "Ashen Shrine",
                new[] { WorldBiomeTag.EndTimes, WorldBiomeTag.Abyss, WorldBiomeTag.Ritual, WorldBiomeTag.Ruin },
                Array.Empty<TextAsset>(),
                materials.Select(pair => new RoomBiomeMaterialOverride(pair.Key, pair.Value)),
                prefabs.Select(pair => new RoomBiomePrefabOverride(pair.Key, pair.Value)),
                RoomBiomeCatalogDefinition.DefaultDecorBindings());

            var catalog = LoadOrCreate<RoomBiomeCatalogDefinition>(BiomeCatalogPath);
            var biomes = catalog.Biomes
                .Where(existing => existing != null && !RoomBiomeIds.Matches(existing.BiomeId, RoomBiomeIds.CorruptedAshenShrine))
                .Concat(new[] { biome })
                .ToArray();
            catalog.Configure(RoomBiomeIds.HollowThreshold, biomes);

            EditorUtility.SetDirty(biome);
            EditorUtility.SetDirty(catalog);
        }

        private static void PatchCorruptedRoomBiome()
        {
            if (!File.Exists(CorruptedRoomPath))
            {
                throw new FileNotFoundException($"Missing corrupted chest room fixture: {CorruptedRoomPath}");
            }

            var json = File.ReadAllText(CorruptedRoomPath);
            var manifest = JsonUtility.FromJson<ImportedHollowRoomManifest>(json);
            if (manifest?.hollowRuntime == null)
            {
                throw new InvalidOperationException($"Cannot read HollowRuntime manifest: {CorruptedRoomPath}");
            }

            manifest.hollowRuntime.biomeId = RoomBiomeIds.CorruptedAshenShrine;
            File.WriteAllText(CorruptedRoomPath, JsonUtility.ToJson(manifest, prettyPrint: true));
            AssetDatabase.ImportAsset(CorruptedRoomPath, ImportAssetOptions.ForceUpdate);
        }

        private static GameObject CreateAshPilePrefab(Material material)
        {
            return CreatePrefab(PresentationPrefabRole.DecorGrassTuft, "AP_CorruptedAshenShrine_DecorAshPile", root =>
            {
                for (var index = 0; index < 5; index++)
                {
                    var ash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ash.name = $"AshMound_{index:00}";
                    ash.transform.SetParent(root.transform, false);
                    ash.transform.localPosition = new Vector3((index - 2) * 0.11f, 0.045f, Mathf.Sin(index * 1.7f) * 0.1f);
                    ash.transform.localScale = new Vector3(0.28f - index * 0.018f, 0.055f, 0.2f + index * 0.012f);
                    AssignMaterialAndStrip(ash, material);
                }
            });
        }

        private static GameObject CreateSigilShardPrefab(Material material)
        {
            return CreatePrefab(PresentationPrefabRole.DecorCrystalCluster, "AP_CorruptedAshenShrine_DecorSigilShard", root =>
            {
                for (var index = 0; index < 4; index++)
                {
                    var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shard.name = $"SigilShard_{index:00}";
                    shard.transform.SetParent(root.transform, false);
                    shard.transform.localPosition = new Vector3(Mathf.Cos(index * 1.55f) * 0.14f, 0.19f + index * 0.035f, Mathf.Sin(index * 1.55f) * 0.12f);
                    shard.transform.localRotation = Quaternion.Euler(0f, 28f + index * 37f, 9f - index * 4f);
                    shard.transform.localScale = new Vector3(0.075f, 0.38f + index * 0.045f, 0.05f);
                    AssignMaterialAndStrip(shard, material);
                }
            });
        }

        private static GameObject CreateAshenObeliskPrefab(Material material)
        {
            return CreatePrefab(PresentationPrefabRole.DecorSmallTree, "AP_CorruptedAshenShrine_DecorAshenObelisk", root =>
            {
                var baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseBlock.name = "Base";
                baseBlock.transform.SetParent(root.transform, false);
                baseBlock.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                baseBlock.transform.localScale = new Vector3(0.42f, 0.16f, 0.42f);
                AssignMaterialAndStrip(baseBlock, material);

                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Obelisk";
                pillar.transform.SetParent(root.transform, false);
                pillar.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                pillar.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                pillar.transform.localScale = new Vector3(0.22f, 0.82f, 0.22f);
                AssignMaterialAndStrip(pillar, material);
            });
        }

        private static GameObject CreateBrokenShrineStonesPrefab(Material material)
        {
            return CreatePrefab(PresentationPrefabRole.DecorStoneRuin, "AP_CorruptedAshenShrine_DecorBrokenShrineStones", root =>
            {
                var blocks = new[]
                {
                    new Vector4(-0.22f, 0.16f, 0.1f, 0.28f),
                    new Vector4(0.06f, 0.13f, -0.08f, 0.2f),
                    new Vector4(0.3f, 0.09f, 0.12f, 0.16f)
                };
                for (var index = 0; index < blocks.Length; index++)
                {
                    var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = $"BrokenStone_{index:00}";
                    block.transform.SetParent(root.transform, false);
                    block.transform.localPosition = new Vector3(blocks[index].x, blocks[index].w * 0.5f, blocks[index].z);
                    block.transform.localRotation = Quaternion.Euler(0f, index * 17f - 12f, index * 3f);
                    block.transform.localScale = new Vector3(0.28f + index * 0.07f, blocks[index].w, 0.2f + index * 0.04f);
                    AssignMaterialAndStrip(block, material);
                }
            });
        }

        private static GameObject CreatePrefab(PresentationPrefabRole role, string prefabName, Action<GameObject> build)
        {
            var root = new GameObject(prefabName);
            try
            {
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
                root.AddComponent<PresentationVisualMarker>().Configure(role, isFallback: false);
                build(root);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{BiomePrefabDirectory}/{prefabName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssignMaterialAndStrip(GameObject target, Material material)
        {
            if (target.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.sharedMaterial = material;
            }

            foreach (var collider in target.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Material CreateOrUpdateLitMaterial(MaterialSpec spec, Texture texture)
        {
            var path = MaterialPath(spec.MaterialName);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = spec.MaterialName;
            if (shader != null)
            {
                material.shader = shader;
            }

            material.color = spec.Color;
            SetTexture(material, "_BaseMap", texture, spec.TextureScale);
            SetTexture(material, "_MainTex", texture, spec.TextureScale);
            SetColor(material, "_BaseColor", spec.Color);
            SetColor(material, "_Color", spec.Color);
            SetFloat(material, "_Metallic", 0f);
            SetFloat(material, "_Smoothness", spec.Smoothness);
            SetFloat(material, "_Glossiness", spec.Smoothness);
            ConfigureSurface(material, spec.Transparent, spec.DoubleSided);
            EditorUtility.SetDirty(material);
            return material;
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

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private readonly struct MaterialSpec
        {
            public MaterialSpec(
                MaterialRole role,
                string materialName,
                string texturePath,
                Vector2 textureScale,
                float smoothness,
                Color color,
                bool transparent = false,
                bool doubleSided = false)
            {
                Role = role;
                MaterialName = materialName;
                TexturePath = texturePath;
                TextureScale = textureScale;
                Smoothness = smoothness;
                Color = color;
                Transparent = transparent;
                DoubleSided = doubleSided;
            }

            public MaterialRole Role { get; }

            public string MaterialName { get; }

            public string TexturePath { get; }

            public Vector2 TextureScale { get; }

            public float Smoothness { get; }

            public Color Color { get; }

            public bool Transparent { get; }

            public bool DoubleSided { get; }
        }
    }
}
