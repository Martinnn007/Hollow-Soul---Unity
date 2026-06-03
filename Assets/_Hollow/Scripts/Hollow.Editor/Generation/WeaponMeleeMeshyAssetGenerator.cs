using System;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class WeaponMeleeMeshyAssetGenerator
    {
        public const string ArtPassWeaponMeleePrefabPath = "Assets/_Hollow/Prefabs/ArtPass/AP_WeaponMelee.prefab";
        public const string MeshySourceFbxPath = "Assets/MeshyImports/Meshy_Model_20260603_005320/Meshy_AI_The_Silent_Blade_0602235317_texture.fbx";
        public const string MeshyMaterialPath = "Assets/MeshyImports/Meshy_Model_20260603_005320/Material.001.mat";
        public const string MeshyNormalPath = "Assets/MeshyImports/Meshy_Model_20260603_005320/Meshy_AI_The_Silent_Blade_0602235317_texture_normal.png";
        public const string MeshyMetallicPath = "Assets/MeshyImports/Meshy_Model_20260603_005320/Meshy_AI_The_Silent_Blade_0602235317_texture_metallic.png";
        public const string MeshyRoughnessPath = "Assets/MeshyImports/Meshy_Model_20260603_005320/Meshy_AI_The_Silent_Blade_0602235317_texture_roughness.png";

        private static readonly Vector3 MeshyModelLocalEuler = new(-90f, 0f, 0f);
        private static readonly Vector3 TargetBounds = new(0.32f, 0.95f, 0.18f);
        private const float TargetBottomLocalY = -0.12f;

        [MenuItem("Hollow/Generation/Generate Meshy Melee Weapon Art")]
        public static void Generate()
        {
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassRoot);
            ConfigureTextureImporters();

            var material = RequireAsset<Material>(MeshyMaterialPath);
            ConfigureGameplayReadableMaterial(material);
            var artPassPrefab = SaveArtPassPrefab(material);
            RefreshPresentationCatalog(artPassPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Meshy melee weapon ArtPass visual and catalog binding.");
        }

        private static GameObject SaveArtPassPrefab(Material material)
        {
            var source = RequireAsset<GameObject>(MeshySourceFbxPath);
            var root = new GameObject("AP_WeaponMelee");
            try
            {
                root.AddComponent<PresentationVisualMarker>().Configure(PresentationPrefabRole.WeaponMelee, isFallback: false);

                var model = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (model == null)
                {
                    model = Object.Instantiate(source);
                }

                model.name = "MeshySilentBladeModel";
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

                Directory.CreateDirectory(Path.GetDirectoryName(ArtPassWeaponMeleePrefabPath) ?? Milestone23AssetGenerator.ArtPassRoot);
                return PrefabUtility.SaveAsPrefabAsset(root, ArtPassWeaponMeleePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void RefreshPresentationCatalog(GameObject artPassPrefab)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            var bindings = catalog.PrefabBindings
                .Where(binding => binding.Role != PresentationPrefabRole.WeaponMelee)
                .ToList();
            bindings.Add(new PresentationPrefabBinding(PresentationPrefabRole.WeaponMelee, artPassPrefab));
            catalog.Configure(
                catalog.MaterialPalette,
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
                throw new FileNotFoundException($"Missing melee weapon Meshy asset at {path}", path);
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

        private static void ConfigureGameplayReadableMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.doubleSidedGI = true;
            var serializedMaterial = new SerializedObject(material);
            var doubleSidedGi = serializedMaterial.FindProperty("m_DoubleSidedGI");
            if (doubleSidedGi != null)
            {
                doubleSidedGi.boolValue = true;
                serializedMaterial.ApplyModifiedPropertiesWithoutUndo();
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            EditorUtility.SetDirty(material);
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
