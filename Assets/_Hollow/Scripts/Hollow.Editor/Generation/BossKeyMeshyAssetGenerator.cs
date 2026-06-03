using System;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class BossKeyMeshyAssetGenerator
    {
        public const string ArtPassBossKeyPrefabPath = "Assets/_Hollow/Prefabs/ArtPass/AP_BossKeyPickup.prefab";
        public const string MeshySourceFbxPath = "Assets/MeshyImports/Meshy_Model_20260602_223831/Meshy_AI_Infernal_Skull_Scepte_0602213825_texture.fbx";
        public const string MeshyMaterialPath = "Assets/MeshyImports/Meshy_Model_20260602_223831/Material.001.mat";
        public const string MeshyNormalPath = "Assets/MeshyImports/Meshy_Model_20260602_223831/Meshy_AI_Infernal_Skull_Scepte_0602213825_texture_normal.png";
        public const string MeshyMetallicPath = "Assets/MeshyImports/Meshy_Model_20260602_223831/Meshy_AI_Infernal_Skull_Scepte_0602213825_texture_metallic.png";
        public const string MeshyRoughnessPath = "Assets/MeshyImports/Meshy_Model_20260602_223831/Meshy_AI_Infernal_Skull_Scepte_0602213825_texture_roughness.png";

        private static readonly Vector3 MeshyModelLocalEuler = new(-90f, 0f, 0f);
        private static readonly Vector3 TargetBounds = new(2.820370f, 4.308899f, 1.096811f);
        private const float TargetBottomLocalY = -2.154449f;

        [MenuItem("Hollow/Generation/Generate Boss Key Meshy Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Milestone20AssetGenerator.BranchFeaturePrefabDirectory);
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassRoot);
            ConfigureTextureImporters();

            var material = RequireAsset<Material>(MeshyMaterialPath);
            var artPassPrefab = SaveArtPassPrefab(material);
            SaveGameplayHostPrefab();
            var palette = RefreshMaterialPalette(material);
            RefreshPresentationCatalog(artPassPrefab, palette);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Meshy boss key ArtPass visual, M20 host prefab, and catalog binding.");
        }

        private static GameObject SaveArtPassPrefab(Material material)
        {
            var source = RequireAsset<GameObject>(MeshySourceFbxPath);
            var root = new GameObject("AP_BossKeyPickup");
            try
            {
                root.AddComponent<PresentationVisualMarker>().Configure(PresentationPrefabRole.BossKeyPickup, isFallback: false);

                var model = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (model == null)
                {
                    model = Object.Instantiate(source);
                }

                model.name = "MeshyInfernalSkullScepterKeyModel";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                StripBlenderTemplateObjects(model.transform);
                AssignMaterialToRenderers(model, material);
                StripVisualOnlyComponents(root);
                PresentationVisualBoundsFitter.FitToTargetBounds(
                    model.transform,
                    TargetBounds,
                    TargetBottomLocalY,
                    Quaternion.Euler(MeshyModelLocalEuler));

                Directory.CreateDirectory(Path.GetDirectoryName(ArtPassBossKeyPrefabPath) ?? Milestone23AssetGenerator.ArtPassRoot);
                return PrefabUtility.SaveAsPrefabAsset(root, ArtPassBossKeyPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject SaveGameplayHostPrefab()
        {
            var root = new GameObject("BossKeyPickup");
            try
            {
                root.transform.localScale = Vector3.one * 0.32f;
                root.AddComponent<BossKeyPickup>();
                return PrefabUtility.SaveAsPrefabAsset(root, Milestone20AssetGenerator.BossKeyPickupPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static MaterialPaletteDefinition RefreshMaterialPalette(Material material)
        {
            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone23AssetGenerator.ArtPassPalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MaterialPaletteDefinition>();
                AssetDatabase.CreateAsset(palette, Milestone23AssetGenerator.ArtPassPalettePath);
            }

            var bindings = palette.Bindings
                .Where(binding => binding.Role != MaterialRole.BossKeyPickup)
                .ToList();
            bindings.Add(new MaterialRoleBinding(
                MaterialRole.BossKeyPickup,
                material,
                new Color(0.76f, 0.67f, 0.5f, 1f)));
            palette.Configure(bindings
                .OrderBy(binding => binding.Role.ToString(), StringComparer.Ordinal)
                .ToArray());
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static void RefreshPresentationCatalog(GameObject artPassPrefab, MaterialPaletteDefinition palette)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            var bindings = catalog.PrefabBindings
                .Where(binding => binding.Role != PresentationPrefabRole.BossKeyPickup)
                .ToList();
            bindings.Add(new PresentationPrefabBinding(PresentationPrefabRole.BossKeyPickup, artPassPrefab));
            catalog.Configure(
                catalog.MaterialPalette != null ? catalog.MaterialPalette : palette,
                catalog.VfxCues,
                catalog.AudioCues,
                bindings.OrderBy(binding => binding.Role.ToString(), StringComparer.Ordinal).ToArray());
            EditorUtility.SetDirty(catalog);
        }

        private static void ConfigureTextureImporters()
        {
            ConfigureTextureImporter(MeshyNormalPath, TextureImporterType.NormalMap);
            ConfigureLinearTextureImporter(MeshyMetallicPath);
            ConfigureLinearTextureImporter(MeshyRoughnessPath);
        }

        private static T RequireAsset<T>(string path)
            where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException($"Missing boss key Meshy asset at {path}", path);
            }

            return asset;
        }

        private static void AssignMaterialToRenderers(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer == null)
                {
                    continue;
                }

                var slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (var index = 0; index < slots.Length; index++)
                {
                    slots[index] = material;
                }

                renderer.sharedMaterials = slots;
            }
        }

        private static void StripVisualOnlyComponents(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(collider);
            }

            foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                Object.DestroyImmediate(rigidbody);
            }

            foreach (var animator in root.GetComponentsInChildren<Animator>(includeInactive: true))
            {
                Object.DestroyImmediate(animator);
            }

            foreach (var camera in root.GetComponentsInChildren<Camera>(includeInactive: true))
            {
                Object.DestroyImmediate(camera);
            }

            foreach (var light in root.GetComponentsInChildren<Light>(includeInactive: true))
            {
                Object.DestroyImmediate(light);
            }

            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                if (component is PresentationVisualMarker)
                {
                    continue;
                }

                Object.DestroyImmediate(component);
            }
        }

        private static void StripBlenderTemplateObjects(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(includeInactive: true);
            var hasNonTemplateRenderer = renderers.Any(renderer => renderer != null && renderer.transform.name != "Cube");
            if (!hasNonTemplateRenderer)
            {
                return;
            }

            for (var index = model.childCount - 1; index >= 0; index--)
            {
                var child = model.GetChild(index);
                if (child.name == "Cube")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void ConfigureTextureImporter(string path, TextureImporterType type)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == type)
            {
                return;
            }

            importer.textureType = type;
            importer.SaveAndReimport();
        }

        private static void ConfigureLinearTextureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !importer.sRGBTexture)
            {
                return;
            }

            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }
    }
}
