using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class VerdantRuinsBiomeAssetGenerator
    {
        public const int TextureSize = 512;
        public const string TextureDirectory = "Assets/_Hollow/Art/Textures/VerdantRuins";
        public const string BiomeRoomDirectory = "Assets/_Hollow/Data/Rooms/Biomes/VerdantRuins";
        public const string BiomeResourceDirectory = "Assets/_Hollow/Resources/Hollow/Biomes";
        public const string BiomePrefabDirectory = "Assets/_Hollow/Prefabs/ArtPass/Biomes/VerdantRuins";

        public const string GrassStoneFloorTexturePath = TextureDirectory + "/T_VerdantRuins_GrassStoneFloor_BaseColor.png";
        public const string MossyWallTexturePath = TextureDirectory + "/T_VerdantRuins_MossyStoneWall_BaseColor.png";
        public const string WeatheredRockTexturePath = TextureDirectory + "/T_VerdantRuins_WeatheredRock_BaseColor.png";
        public const string CrystalTexturePath = TextureDirectory + "/T_VerdantRuins_Crystal_BaseColor.png";
        public const string FoliageTexturePath = TextureDirectory + "/T_VerdantRuins_Foliage_BaseColor.png";
        public const string TreeRuinTexturePath = TextureDirectory + "/T_VerdantRuins_TreeRuin_BaseColor.png";

        public const string HollowBiomePath = BiomeResourceDirectory + "/Biome_HollowThreshold.asset";
        public const string VerdantBiomePath = BiomeResourceDirectory + "/Biome_VerdantRuins.asset";
        public const string BiomeCatalogPath = BiomeResourceDirectory + "/RoomBiomeCatalog.asset";

        public static readonly string[] VerdantRoomIds =
        {
            "verdant_macro_single_1x1",
            "verdant_macro_wide_2x1",
            "verdant_macro_tall_1x2",
            "verdant_macro_block_2x2",
            "verdant_macro_l_3cell"
        };

        [MenuItem("Hollow/Biomes/Generate Verdant Ruins Foundation")]
        public static void Generate()
        {
            GenerateAssets();
        }

        public static void GenerateBatch()
        {
            try
            {
                GenerateAssets();
                Debug.Log("Verdant Ruins biome foundation generation passed.");
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
                Debug.Log("Verdant Ruins biome foundation validation passed.");
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
            GenerateTextures();
            var materials = GenerateMaterials();
            var prefabs = GenerateDecorPrefabs(materials);
            GenerateRoomTemplates();
            GenerateBiomeCatalog(materials, prefabs);

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
            foreach (var path in new[]
                     {
                         GrassStoneFloorTexturePath,
                         MossyWallTexturePath,
                         WeatheredRockTexturePath,
                         CrystalTexturePath,
                         FoliageTexturePath,
                         TreeRuinTexturePath
                     })
            {
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                {
                    throw new InvalidOperationException($"Missing Verdant BaseColor texture: {path}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RoomBiomeCatalogDefinition>(BiomeCatalogPath);
            if (catalog == null || !catalog.TryGetBiome(RoomBiomeIds.VerdantRuins, out var verdant) || verdant == null)
            {
                throw new InvalidOperationException("Room biome catalog does not resolve Verdant Ruins.");
            }

            if (verdant.RoomTemplates.Count != VerdantRoomIds.Length)
            {
                throw new InvalidOperationException($"Verdant Ruins should have {VerdantRoomIds.Length} room templates.");
            }

            foreach (var role in new[]
                     {
                         MaterialRole.RoomFloor,
                         MaterialRole.RoomWall,
                         MaterialRole.RoomWallTransparent,
                         MaterialRole.RoomObstacleRock,
                         MaterialRole.DoorActive,
                         MaterialRole.DoorCleared,
                         MaterialRole.DoorLocked,
                         MaterialRole.DoorUnavailable,
                         MaterialRole.DecorGrassTuft,
                         MaterialRole.DecorCrystalCluster,
                         MaterialRole.DecorSmallTree,
                         MaterialRole.DecorStoneRuin
                     })
            {
                if (!verdant.TryResolve(role, out var material) || material == null)
                {
                    throw new InvalidOperationException($"Verdant Ruins is missing material override for {role}.");
                }
            }

            foreach (var role in new[]
                     {
                         PresentationPrefabRole.DecorGrassTuft,
                         PresentationPrefabRole.DecorCrystalCluster,
                         PresentationPrefabRole.DecorSmallTree,
                         PresentationPrefabRole.DecorStoneRuin
                     })
            {
                if (!verdant.TryResolve(role, out var prefab) || prefab == null)
                {
                    throw new InvalidOperationException($"Verdant Ruins is missing prefab override for {role}.");
                }

                if (prefab.GetComponentsInChildren<Collider>(includeInactive: true).Length > 0 ||
                    prefab.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length > 0)
                {
                    throw new InvalidOperationException($"Verdant decor prefab {role} must be visual-only.");
                }
            }

            var shapes = new HashSet<RoomFootprintShape>();
            foreach (var template in verdant.RoomTemplates)
            {
                if (template == null)
                {
                    throw new InvalidOperationException("Verdant Ruins room template list contains a null entry.");
                }

                var asset = HollowRuntimeV2Importer.Import(template.text);
                if (!RoomBiomeIds.Matches(asset.BiomeId, RoomBiomeIds.VerdantRuins))
                {
                    throw new InvalidOperationException($"Verdant room {asset.Id} imported as biome '{asset.BiomeId}'.");
                }

                if (!asset.Id.StartsWith("verdant_macro_", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Verdant room {asset.Id} does not use the expected stable id prefix.");
                }

                if (!asset.Decor.Any(decor => decor != null && RoomBiomeDecorKinds.IsKnown(decor.kind)))
                {
                    throw new InvalidOperationException($"Verdant room {asset.Id} does not contain any known Verdant decor markers.");
                }

                shapes.Add(RoomFootprintShapeUtility.Classify(asset.Footprint));
            }

            foreach (var requiredShape in new[]
                     {
                         RoomFootprintShape.Single1x1,
                         RoomFootprintShape.Wide2x1,
                         RoomFootprintShape.Tall1x2,
                         RoomFootprintShape.Block2x2,
                         RoomFootprintShape.L3Cell
                     })
            {
                if (!shapes.Contains(requiredShape))
                {
                    throw new InvalidOperationException($"Verdant room templates are missing shape coverage for {requiredShape}.");
                }
            }

            var branchCatalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var sampleText = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var sample = HollowRuntimeV2Importer.Import(sampleText.text);
            var content = BranchSessionContent.Create(sample, branchCatalog, 0, out var error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException($"Branch content reported errors while validating Verdant Ruins: {error}");
            }

            if (!content.HasCompleteBiomePool(RoomBiomeIds.VerdantRuins))
            {
                throw new InvalidOperationException("Branch session content does not expose a complete Verdant Ruins room pool.");
            }
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(TextureDirectory);
            Directory.CreateDirectory(BiomeRoomDirectory);
            Directory.CreateDirectory(BiomeResourceDirectory);
            Directory.CreateDirectory(BiomePrefabDirectory);
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassMaterialDirectory);
        }

        private static void GenerateTextures()
        {
            WriteBaseColorTexture(GrassStoneFloorTexturePath, SampleGrassStoneFloorPixel);
            WriteBaseColorTexture(MossyWallTexturePath, SampleMossyWallPixel);
            WriteBaseColorTexture(WeatheredRockTexturePath, SampleWeatheredRockPixel);
            WriteBaseColorTexture(CrystalTexturePath, SampleCrystalPixel);
            WriteBaseColorTexture(FoliageTexturePath, SampleFoliagePixel);
            WriteBaseColorTexture(TreeRuinTexturePath, SampleTreeRuinPixel);
        }

        private static Dictionary<MaterialRole, Material> GenerateMaterials()
        {
            var floorTexture = LoadTexture(GrassStoneFloorTexturePath);
            var wallTexture = LoadTexture(MossyWallTexturePath);
            var rockTexture = LoadTexture(WeatheredRockTexturePath);
            var crystalTexture = LoadTexture(CrystalTexturePath);
            var foliageTexture = LoadTexture(FoliageTexturePath);
            var treeRuinTexture = LoadTexture(TreeRuinTexturePath);

            var materials = new Dictionary<MaterialRole, Material>
            {
                [MaterialRole.RoomFloor] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_RoomFloor"),
                    "AP_M_Verdant_RoomFloor",
                    floorTexture,
                    new Vector2(7f, 5f),
                    0.28f),
                [MaterialRole.RoomWall] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_RoomWall"),
                    "AP_M_Verdant_RoomWall",
                    wallTexture,
                    new Vector2(4f, 4f),
                    0.2f,
                    doubleSided: true),
                [MaterialRole.RoomWallTransparent] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_RoomWallTransparent"),
                    "AP_M_Verdant_RoomWallTransparent",
                    wallTexture,
                    new Vector2(4f, 4f),
                    0.2f,
                    new Color(1f, 1f, 1f, RoomWallVisibilityController.TransparentAlpha),
                    transparent: true,
                    doubleSided: true),
                [MaterialRole.RoomObstacleRock] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_RoomObstacleRock"),
                    "AP_M_Verdant_RoomObstacleRock",
                    rockTexture,
                    new Vector2(2.4f, 2.4f),
                    0.26f),
                [MaterialRole.DoorActive] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_DoorActive"),
                    "AP_M_Verdant_DoorActive",
                    wallTexture,
                    new Vector2(2f, 2f),
                    0.24f,
                    new Color(0.82f, 1f, 0.78f, 1f)),
                [MaterialRole.DoorCleared] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_DoorCleared"),
                    "AP_M_Verdant_DoorCleared",
                    crystalTexture,
                    new Vector2(2f, 2f),
                    0.38f,
                    new Color(0.75f, 1f, 0.88f, 1f)),
                [MaterialRole.DoorLocked] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_DoorLocked"),
                    "AP_M_Verdant_DoorLocked",
                    treeRuinTexture,
                    new Vector2(2f, 2f),
                    0.2f,
                    new Color(0.72f, 0.62f, 0.5f, 1f)),
                [MaterialRole.DoorUnavailable] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_DoorUnavailable"),
                    "AP_M_Verdant_DoorUnavailable",
                    wallTexture,
                    new Vector2(2f, 2f),
                    0.12f,
                    new Color(0.48f, 0.58f, 0.5f, 0.92f)),
                [MaterialRole.DecorGrassTuft] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_DecorGrassTuft"),
                    "AP_M_Verdant_DecorGrassTuft",
                    foliageTexture,
                    new Vector2(1f, 1f),
                    0.34f),
                [MaterialRole.DecorCrystalCluster] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_DecorCrystalCluster"),
                    "AP_M_Verdant_DecorCrystalCluster",
                    crystalTexture,
                    new Vector2(1f, 1f),
                    0.62f,
                    new Color(0.75f, 1f, 0.95f, 1f)),
                [MaterialRole.DecorSmallTree] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_DecorSmallTree"),
                    "AP_M_Verdant_DecorSmallTree",
                    foliageTexture,
                    new Vector2(1.5f, 1.5f),
                    0.28f),
                [MaterialRole.DecorStoneRuin] = CreateOrUpdateLitMaterial(
                    MaterialPath("AP_M_Verdant_DecorStoneRuin"),
                    "AP_M_Verdant_DecorStoneRuin",
                    treeRuinTexture,
                    new Vector2(1.6f, 1.6f),
                    0.22f)
            };

            return materials;
        }

        private static Dictionary<PresentationPrefabRole, GameObject> GenerateDecorPrefabs(
            IReadOnlyDictionary<MaterialRole, Material> materials)
        {
            return new Dictionary<PresentationPrefabRole, GameObject>
            {
                [PresentationPrefabRole.DecorGrassTuft] = CreateGrassTuftPrefab(materials[MaterialRole.DecorGrassTuft]),
                [PresentationPrefabRole.DecorCrystalCluster] = CreateCrystalClusterPrefab(materials[MaterialRole.DecorCrystalCluster]),
                [PresentationPrefabRole.DecorSmallTree] = CreateSmallTreePrefab(
                    materials[MaterialRole.DecorSmallTree],
                    materials[MaterialRole.DecorStoneRuin]),
                [PresentationPrefabRole.DecorStoneRuin] = CreateStoneRuinPrefab(materials[MaterialRole.DecorStoneRuin])
            };
        }

        private static void GenerateRoomTemplates()
        {
            var specs = new[]
            {
                new RoomTemplateSpec("combat_macro_single_1x1", "verdant_macro_single_1x1", "Verdant Ruins Single 1x1"),
                new RoomTemplateSpec("combat_macro_wide_2x1", "verdant_macro_wide_2x1", "Verdant Ruins Wide 2x1"),
                new RoomTemplateSpec("combat_macro_tall_1x2", "verdant_macro_tall_1x2", "Verdant Ruins Tall 1x2"),
                new RoomTemplateSpec("combat_macro_block_2x2", "verdant_macro_block_2x2", "Verdant Ruins Block 2x2"),
                new RoomTemplateSpec("combat_macro_l_3cell", "verdant_macro_l_3cell", "Verdant Ruins L 3-cell")
            };

            foreach (var spec in specs)
            {
                var sourcePath = $"{Milestone13AssetGenerator.MacroFixtureDirectory}/{spec.SourceRoomId}.hollowruntime.json";
                var targetPath = RoomPath(spec.TargetRoomId);
                var sourceJson = File.ReadAllText(sourcePath);
                var manifest = JsonUtility.FromJson<ImportedHollowRoomManifest>(sourceJson);
                if (manifest?.hollowRuntime == null)
                {
                    throw new InvalidOperationException($"Cannot duplicate room template because {sourcePath} is not a HollowRuntime manifest.");
                }

                var runtime = manifest.hollowRuntime;
                runtime.sourceProjectId = spec.TargetRoomId;
                runtime.canonicalRoomId = spec.TargetRoomId;
                runtime.displayName = spec.DisplayName;
                runtime.biomeId = RoomBiomeIds.VerdantRuins;
                runtime.decor = CreateVerdantDecor(runtime);
                File.WriteAllText(targetPath, JsonUtility.ToJson(manifest, prettyPrint: true));
                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static List<ImportedRoomDecor> CreateVerdantDecor(ImportedHollowRuntime runtime)
        {
            var existing = runtime.decor?
                .Where(decor => decor != null && !((decor.id ?? string.Empty).StartsWith("verdant_decor_", StringComparison.Ordinal)))
                .ToList() ?? new List<ImportedRoomDecor>();

            existing.Add(new ImportedRoomDecor
            {
                id = "verdant_decor_grass_tuft_01",
                kind = RoomBiomeDecorKinds.GrassTuft,
                center = DecorPosition(runtime, 0.18f, 0.78f),
                size = Vec(0.65f, 0.35f, 0.65f)
            });
            existing.Add(new ImportedRoomDecor
            {
                id = "verdant_decor_crystal_cluster_01",
                kind = RoomBiomeDecorKinds.CrystalCluster,
                center = DecorPosition(runtime, 0.82f, 0.25f),
                size = Vec(0.58f, 0.72f, 0.58f)
            });
            existing.Add(new ImportedRoomDecor
            {
                id = "verdant_decor_small_tree_01",
                kind = RoomBiomeDecorKinds.SmallTree,
                center = DecorPosition(runtime, 0.08f, 0.18f),
                size = Vec(0.85f, 1.35f, 0.85f)
            });
            existing.Add(new ImportedRoomDecor
            {
                id = "verdant_decor_stone_ruin_01",
                kind = RoomBiomeDecorKinds.StoneRuin,
                center = DecorPosition(runtime, 0.9f, 0.82f),
                size = Vec(0.9f, 0.65f, 0.52f)
            });
            return existing;
        }

        private static void GenerateBiomeCatalog(
            IReadOnlyDictionary<MaterialRole, Material> materials,
            IReadOnlyDictionary<PresentationPrefabRole, GameObject> prefabs)
        {
            var hollow = LoadOrCreate<RoomBiomeDefinition>(HollowBiomePath);
            hollow.Configure(
                RoomBiomeIds.HollowThreshold,
                "The Hollow Threshold",
                new[] { WorldBiomeTag.MixedThreshold },
                Array.Empty<TextAsset>(),
                Array.Empty<RoomBiomeMaterialOverride>(),
                Array.Empty<RoomBiomePrefabOverride>(),
                RoomBiomeCatalogDefinition.DefaultDecorBindings());

            var verdant = LoadOrCreate<RoomBiomeDefinition>(VerdantBiomePath);
            verdant.Configure(
                RoomBiomeIds.VerdantRuins,
                "Verdant Ruins",
                new[] { WorldBiomeTag.VerdantRuins, WorldBiomeTag.Natural, WorldBiomeTag.Ruin },
                VerdantRoomIds.Select(id => AssetDatabase.LoadAssetAtPath<TextAsset>(RoomPath(id))).Where(asset => asset != null),
                materials.Select(pair => new RoomBiomeMaterialOverride(pair.Key, pair.Value)),
                prefabs.Select(pair => new RoomBiomePrefabOverride(pair.Key, pair.Value)),
                RoomBiomeCatalogDefinition.DefaultDecorBindings());

            var catalog = LoadOrCreate<RoomBiomeCatalogDefinition>(BiomeCatalogPath);
            catalog.Configure(RoomBiomeIds.HollowThreshold, new[] { hollow, verdant });
            EditorUtility.SetDirty(hollow);
            EditorUtility.SetDirty(verdant);
            EditorUtility.SetDirty(catalog);
        }

        private static GameObject CreateGrassTuftPrefab(Material material)
        {
            return CreatePrefab(PresentationPrefabRole.DecorGrassTuft, "AP_Verdant_DecorGrassTuft", root =>
            {
                for (var index = 0; index < 7; index++)
                {
                    var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    blade.name = $"Blade_{index:00}";
                    blade.transform.SetParent(root.transform, false);
                    blade.transform.localPosition = new Vector3((index - 3) * 0.055f, 0.15f, Mathf.Sin(index * 1.7f) * 0.07f);
                    blade.transform.localRotation = Quaternion.Euler(0f, index * 28f, 8f - index * 2f);
                    blade.transform.localScale = new Vector3(0.035f, 0.3f + (index % 3) * 0.05f, 0.035f);
                    AssignMaterialAndStrip(blade, material);
                }
            });
        }

        private static GameObject CreateCrystalClusterPrefab(Material material)
        {
            return CreatePrefab(PresentationPrefabRole.DecorCrystalCluster, "AP_Verdant_DecorCrystalCluster", root =>
            {
                for (var index = 0; index < 5; index++)
                {
                    var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crystal.name = $"Crystal_{index:00}";
                    crystal.transform.SetParent(root.transform, false);
                    crystal.transform.localPosition = new Vector3(Mathf.Cos(index * 1.25f) * 0.16f, 0.22f + index * 0.03f, Mathf.Sin(index * 1.25f) * 0.14f);
                    crystal.transform.localRotation = Quaternion.Euler(0f, 45f + index * 31f, 0f);
                    crystal.transform.localScale = new Vector3(0.12f, 0.42f + index * 0.04f, 0.12f);
                    AssignMaterialAndStrip(crystal, material);
                }
            });
        }

        private static GameObject CreateSmallTreePrefab(Material foliageMaterial, Material trunkMaterial)
        {
            return CreatePrefab(PresentationPrefabRole.DecorSmallTree, "AP_Verdant_DecorSmallTree", root =>
            {
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(root.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 0.34f, 0f);
                trunk.transform.localScale = new Vector3(0.12f, 0.34f, 0.12f);
                AssignMaterialAndStrip(trunk, trunkMaterial);

                var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy.name = "Canopy";
                canopy.transform.SetParent(root.transform, false);
                canopy.transform.localPosition = new Vector3(0f, 0.92f, 0f);
                canopy.transform.localScale = new Vector3(0.52f, 0.44f, 0.52f);
                AssignMaterialAndStrip(canopy, foliageMaterial);
            });
        }

        private static GameObject CreateStoneRuinPrefab(Material material)
        {
            return CreatePrefab(PresentationPrefabRole.DecorStoneRuin, "AP_Verdant_DecorStoneRuin", root =>
            {
                var blocks = new[]
                {
                    new Vector4(-0.18f, 0.18f, 0.12f, 0.32f),
                    new Vector4(0.12f, 0.16f, -0.08f, 0.24f),
                    new Vector4(0.32f, 0.12f, 0.1f, 0.18f)
                };
                for (var index = 0; index < blocks.Length; index++)
                {
                    var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = $"RuinBlock_{index:00}";
                    block.transform.SetParent(root.transform, false);
                    block.transform.localPosition = new Vector3(blocks[index].x, blocks[index].w * 0.5f, blocks[index].z);
                    block.transform.localRotation = Quaternion.Euler(0f, index * 9f - 8f, 0f);
                    block.transform.localScale = new Vector3(0.32f + index * 0.08f, blocks[index].w, 0.22f + index * 0.03f);
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
                var path = $"{BiomePrefabDirectory}/{prefabName}.prefab";
                return PrefabUtility.SaveAsPrefabAsset(root, path);
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

        private static ImportedVector3 DecorPosition(ImportedHollowRuntime runtime, float xFraction, float zFraction)
        {
            var bounds = runtime.dimensions?.bounds;
            var targetX = bounds != null ? Mathf.Lerp(bounds.minX + 1f, bounds.maxX - 1f, xFraction) : Mathf.Lerp(-5.5f, 5.5f, xFraction);
            var targetZ = bounds != null ? Mathf.Lerp(bounds.minZ + 1f, bounds.maxZ - 1f, zFraction) : Mathf.Lerp(-2.5f, 2.5f, zFraction);
            var tile = (runtime.walkableTiles ?? new List<ImportedGridPosition>())
                .OrderBy(candidate => Mathf.Pow(candidate.x - targetX, 2f) + Mathf.Pow(candidate.z - targetZ, 2f))
                .FirstOrDefault();
            return tile != null ? Vec(tile.x, 0f, tile.z) : Vec(targetX, 0f, targetZ);
        }

        private static ImportedVector3 Vec(float x, float y, float z)
        {
            return new ImportedVector3 { x = x, y = y, z = z };
        }

        private static string MaterialPath(string materialName)
        {
            return $"{Milestone23AssetGenerator.ArtPassMaterialDirectory}/{materialName}.mat";
        }

        private static string RoomPath(string roomId)
        {
            return $"{BiomeRoomDirectory}/{roomId}.hollowruntime.json";
        }

        private static Texture2D LoadTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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
                    pixels[y * TextureSize + x] = sample(x / (float)TextureSize, v);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
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
            if (shader != null)
            {
                material.shader = shader;
            }

            var color = baseColor ?? Color.white;
            material.color = color;
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

        private static Color SampleGrassStoneFloorPixel(float u, float v)
        {
            var stone = SampleBlockPattern(u, v, 5f, 5f, new Color(0.24f, 0.29f, 0.26f, 1f), new Color(0.39f, 0.43f, 0.34f, 1f), 0.08f);
            var moss = TileNoise(u + 0.14f, v + 0.4f, 12, 2101);
            var grassVein = Mathf.Abs(Mathf.Sin((u * 8f + v * 5f) * Mathf.PI));
            if (moss > 0.52f || grassVein > 0.92f)
            {
                stone = Color.Lerp(stone, new Color(0.17f, 0.42f, 0.19f, 1f), 0.34f);
            }

            return ClampColor(stone);
        }

        private static Color SampleMossyWallPixel(float u, float v)
        {
            var wall = SampleBlockPattern(u, v, 5f, 6f, new Color(0.26f, 0.31f, 0.31f, 1f), new Color(0.4f, 0.45f, 0.4f, 1f), 0.09f);
            var moss = TileNoise(u * 0.75f, v + 0.22f, 10, 3203);
            var drip = Mathf.Pow(Mathf.Clamp01(1f - v), 1.4f);
            return ClampColor(Color.Lerp(wall, new Color(0.13f, 0.34f, 0.18f, 1f), Mathf.Clamp01((moss - 0.45f) * 1.3f + drip * 0.18f)));
        }

        private static Color SampleWeatheredRockPixel(float u, float v)
        {
            var noise = TileNoise(u, v, 18, 4411);
            var cracks = Mathf.Abs(Mathf.Sin((u * 12f + noise * 1.5f) * Mathf.PI)) < 0.06f ||
                Mathf.Abs(Mathf.Sin((v * 10f + noise) * Mathf.PI)) < 0.04f;
            var color = Color.Lerp(new Color(0.25f, 0.28f, 0.23f, 1f), new Color(0.47f, 0.5f, 0.39f, 1f), noise);
            if (cracks)
            {
                color = Color.Lerp(color, new Color(0.08f, 0.09f, 0.08f, 1f), 0.55f);
            }

            return ClampColor(color);
        }

        private static Color SampleCrystalPixel(float u, float v)
        {
            var band = Mathf.Abs(Mathf.Sin((u * 4f + v * 6f) * Mathf.PI));
            var glow = TileNoise(u, v, 9, 5517);
            return ClampColor(Color.Lerp(new Color(0.22f, 0.68f, 0.62f, 1f), new Color(0.86f, 1f, 0.93f, 1f), band * 0.45f + glow * 0.3f));
        }

        private static Color SampleFoliagePixel(float u, float v)
        {
            var leaf = TileNoise(u, v, 20, 6607);
            var stripe = Mathf.Abs(Mathf.Sin((u * 18f + v * 4f) * Mathf.PI));
            return ClampColor(Color.Lerp(new Color(0.08f, 0.25f, 0.1f, 1f), new Color(0.32f, 0.62f, 0.24f, 1f), leaf * 0.68f + stripe * 0.12f));
        }

        private static Color SampleTreeRuinPixel(float u, float v)
        {
            var grain = TileNoise(u, v, 18, 7709);
            var barkLine = Mathf.Abs(Mathf.Sin(u * 22f * Mathf.PI)) < 0.08f;
            var color = Color.Lerp(new Color(0.32f, 0.29f, 0.23f, 1f), new Color(0.5f, 0.48f, 0.37f, 1f), grain);
            if (barkLine)
            {
                color = Color.Lerp(color, new Color(0.13f, 0.18f, 0.14f, 1f), 0.32f);
            }

            return ClampColor(color);
        }

        private static Color SampleBlockPattern(float u, float v, float columns, float rows, Color dark, Color light, float mortarWidth)
        {
            var row = Mathf.FloorToInt(v * rows);
            var shiftedU = u + ((row & 1) == 1 ? 0.5f / columns : 0f);
            var localU = Frac(shiftedU * columns);
            var localV = Frac(v * rows);
            var edge = Mathf.Min(Mathf.Min(localU, 1f - localU), Mathf.Min(localV, 1f - localV));
            var mortar = 1f - Smooth01(mortarWidth * 0.45f, mortarWidth, edge);
            var block = Hash01(Mathf.FloorToInt(shiftedU * columns), row, 9103);
            var color = Color.Lerp(dark, light, block * 0.65f + TileNoise(u, v, 24, 1201) * 0.25f);
            return Color.Lerp(color, new Color(0.06f, 0.075f, 0.065f, 1f), mortar * 0.88f);
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
            return new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), Mathf.Clamp01(color.a));
        }

        private readonly struct RoomTemplateSpec
        {
            public RoomTemplateSpec(string sourceRoomId, string targetRoomId, string displayName)
            {
                SourceRoomId = sourceRoomId;
                TargetRoomId = targetRoomId;
                DisplayName = displayName;
            }

            public string SourceRoomId { get; }

            public string TargetRoomId { get; }

            public string DisplayName { get; }
        }
    }
}
