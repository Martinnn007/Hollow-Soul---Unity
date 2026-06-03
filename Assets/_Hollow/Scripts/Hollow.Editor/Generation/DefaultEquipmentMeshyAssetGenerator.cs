using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class DefaultEquipmentMeshyAssetGenerator
    {
        public const string ArtPassWeaponRangedPrefabPath = "Assets/_Hollow/Prefabs/ArtPass/AP_WeaponRanged.prefab";
        public const string ArtPassArmorPrefabPath = "Assets/_Hollow/Prefabs/ArtPass/AP_Armor.prefab";

        public const string MeshyPistolSourceFbxPath = "Assets/MeshyImports/Meshy_Model_20260603_020012/Meshy_AI_Alien_Tech_Pistol_0603010006_image-to-3d-texture.fbx";
        public const string MeshyPistolMaterialPath = "Assets/MeshyImports/Meshy_Model_20260603_020012/Material.001.mat";
        public const string MeshyPistolNormalPath = "Assets/MeshyImports/Meshy_Model_20260603_020012/Meshy_AI_Alien_Tech_Pistol_0603010006_image-to-3d-texture_normal.png";

        public const string MeshyShieldSourceFbxPath = "Assets/MeshyImports/Meshy_Model_20260603_015821/Meshy_AI_Basic_Small_Shield_0603005815_image-to-3d-texture.fbx";
        public const string MeshyShieldMaterialPath = "Assets/MeshyImports/Meshy_Model_20260603_015821/Material.001.mat";
        public const string MeshyShieldNormalPath = "Assets/MeshyImports/Meshy_Model_20260603_015821/Meshy_AI_Basic_Small_Shield_0603005815_image-to-3d-texture_normal.png";

        private static readonly Vector3 PistolModelLocalEuler = new(0f, 180f, 90f);
        private static readonly Vector3 PistolTargetBounds = new(0.34f, 0.62f, 0.4f);
        private const float PistolTargetBottomLocalY = -0.31f;

        private static readonly Vector3 ShieldModelLocalEuler = new(-90f, 0f, 0f);
        private static readonly Vector3 ShieldTargetBounds = new(0.68f, 0.58f, 0.46f);
        private const float ShieldTargetBottomLocalY = -0.29f;

        [MenuItem("Hollow/Generation/Generate Meshy Default Equipment Art")]
        public static void Generate()
        {
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassRoot);
            ConfigureTextureImporters();

            var pistolMaterial = RequireAsset<Material>(MeshyPistolMaterialPath);
            var shieldMaterial = RequireAsset<Material>(MeshyShieldMaterialPath);
            ConfigureGameplayReadableMaterial(pistolMaterial);
            ConfigureGameplayReadableMaterial(shieldMaterial);

            var rangedPrefab = SaveArtPassPrefab(new MeshyEquipmentSpec(
                ArtPassWeaponRangedPrefabPath,
                PresentationPrefabRole.WeaponRanged,
                "AP_WeaponRanged",
                "MeshyAlienTechPistolModel",
                MeshyPistolSourceFbxPath,
                pistolMaterial,
                PistolTargetBounds,
                PistolTargetBottomLocalY,
                Quaternion.Euler(PistolModelLocalEuler)));

            var armorPrefab = SaveArtPassPrefab(new MeshyEquipmentSpec(
                ArtPassArmorPrefabPath,
                PresentationPrefabRole.Armor,
                "AP_Armor",
                "MeshyBasicSmallShieldModel",
                MeshyShieldSourceFbxPath,
                shieldMaterial,
                ShieldTargetBounds,
                ShieldTargetBottomLocalY,
                Quaternion.Euler(ShieldModelLocalEuler)));

            RefreshPresentationCatalog(new[]
            {
                new PresentationPrefabBinding(PresentationPrefabRole.WeaponRanged, rangedPrefab),
                new PresentationPrefabBinding(PresentationPrefabRole.Armor, armorPrefab)
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Meshy ranged weapon and shield/armor ArtPass visuals and catalog bindings.");
        }

        private static GameObject SaveArtPassPrefab(MeshyEquipmentSpec spec)
        {
            var source = RequireAsset<GameObject>(spec.SourceFbxPath);
            var root = new GameObject(spec.RootName);
            try
            {
                root.AddComponent<PresentationVisualMarker>().Configure(spec.Role, isFallback: false);

                var model = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (model == null)
                {
                    model = Object.Instantiate(source);
                }

                model.name = spec.ModelName;
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                StripBlenderTemplateObjects(model.transform);
                AssignMaterialToRenderers(model, spec.Material);
                StripVisualOnlyComponents(root);
                PresentationVisualBoundsFitter.FitToTargetBounds(
                    model.transform,
                    spec.TargetBounds,
                    spec.TargetBottomLocalY,
                    spec.InitialLocalRotation);

                Directory.CreateDirectory(Path.GetDirectoryName(spec.PrefabPath) ?? Milestone23AssetGenerator.ArtPassRoot);
                return PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void RefreshPresentationCatalog(IReadOnlyCollection<PresentationPrefabBinding> replacements)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            var replacementRoles = replacements.Select(binding => binding.Role).ToHashSet();
            var bindings = catalog.PrefabBindings
                .Where(binding => !replacementRoles.Contains(binding.Role))
                .ToList();
            bindings.AddRange(replacements);
            catalog.Configure(
                catalog.MaterialPalette,
                catalog.VfxCues,
                catalog.AudioCues,
                bindings.OrderBy(binding => binding.Role.ToString(), StringComparer.Ordinal).ToArray());
            EditorUtility.SetDirty(catalog);
        }

        private static void ConfigureTextureImporters()
        {
            ConfigureTextureImporter(MeshyPistolNormalPath, TextureImporterType.NormalMap);
            ConfigureTextureImporter(MeshyShieldNormalPath, TextureImporterType.NormalMap);
        }

        private static T RequireAsset<T>(string path)
            where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException($"Missing default equipment Meshy asset at {path}", path);
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

        private readonly struct MeshyEquipmentSpec
        {
            public MeshyEquipmentSpec(
                string prefabPath,
                PresentationPrefabRole role,
                string rootName,
                string modelName,
                string sourceFbxPath,
                Material material,
                Vector3 targetBounds,
                float targetBottomLocalY,
                Quaternion initialLocalRotation)
            {
                PrefabPath = prefabPath;
                Role = role;
                RootName = rootName;
                ModelName = modelName;
                SourceFbxPath = sourceFbxPath;
                Material = material;
                TargetBounds = targetBounds;
                TargetBottomLocalY = targetBottomLocalY;
                InitialLocalRotation = initialLocalRotation;
            }

            public string PrefabPath { get; }

            public PresentationPrefabRole Role { get; }

            public string RootName { get; }

            public string ModelName { get; }

            public string SourceFbxPath { get; }

            public Material Material { get; }

            public Vector3 TargetBounds { get; }

            public float TargetBottomLocalY { get; }

            public Quaternion InitialLocalRotation { get; }
        }
    }
}
