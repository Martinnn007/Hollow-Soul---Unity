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
    public static class MeshyEnvironmentPropAssetGenerator
    {
        public static readonly Vector3 MeshyModelLocalEuler = new(-90f, 0f, 0f);
        public const float RockVisualYawCorrectionDegrees = 0f;

        [MenuItem("Hollow/Generation/Generate Meshy Environment Props")]
        public static void Generate()
        {
            GenerateAssets();
        }

        public static IReadOnlyList<MeshyEnvironmentPropSpec> PropRows()
        {
            return new[]
            {
                new MeshyEnvironmentPropSpec(
                    "Stacked Stone Rock",
                    PresentationPrefabRole.RoomObstacleRock,
                    MaterialRole.RoomObstacleRock,
                    "Assets/_Hollow/Prefabs/ArtPass/AP_RoomObstacleRock.prefab",
                    "AP_RoomObstacleRock",
                    "Assets/_Hollow/Art/Materials/ArtPass/AP_M_RoomObstacleRock.mat",
                    "MeshyStackedStoneModel",
                    "Assets/MeshyImports/Meshy_Model_20260508_195034/Meshy_AI_Stacked_Stone_0508185023_texture",
                    new Vector3(1f, 1f, 1f),
                    -0.5f,
                    Vector3.zero,
                    RockVisualYawCorrectionDegrees,
                    false,
                    new Color(0.22f, 0.21f, 0.18f, 1f),
                    0.08f,
                    0.38f),
                new MeshyEnvironmentPropSpec(
                    "Weathered Basic Chest",
                    PresentationPrefabRole.ChestNormal,
                    MaterialRole.ChestNormal,
                    "Assets/_Hollow/Prefabs/ArtPass/AP_ChestBasic.prefab",
                    "AP_ChestBasic",
                    "Assets/_Hollow/Art/Materials/ArtPass/AP_M_ChestBasic.mat",
                    "MeshyWeatheredChestModel",
                    "Assets/MeshyImports/Meshy_Model_20260508_195109/Meshy_AI_Weathered_Treasure_Ch_0508185058_texture",
                    new Vector3(0.78f, 0.52f, 0.64f),
                    0f,
                    MeshyModelLocalEuler,
                    0f,
                    false,
                    new Color(0.43f, 0.28f, 0.14f, 1f),
                    0.24f,
                    0.46f)
            };
        }

        public static void GenerateAssets(bool saveAssets = true, bool refresh = true)
        {
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassRoot);
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassMaterialDirectory);

            var materials = new Dictionary<MaterialRole, Material>();
            var prefabs = new Dictionary<PresentationPrefabRole, GameObject>();
            foreach (var spec in PropRows())
            {
                var material = CreateOrUpdateMeshyMaterial(spec);
                materials[spec.MaterialRole] = material;

                var prefab = CreateOrUpdateMeshyPrefab(spec, material);
                if (prefab != null)
                {
                    prefabs[spec.PrefabRole] = prefab;
                }
            }

            RefreshPresentationCatalog(materials, prefabs);

            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
            }

            if (refresh)
            {
                AssetDatabase.Refresh();
            }

            Debug.Log("Generated Hollow Meshy environment prop ArtPass visuals.");
        }

        private static Material CreateOrUpdateMeshyMaterial(MeshyEnvironmentPropSpec spec)
        {
            ConfigureTextureImporter(spec.NormalPath, TextureImporterType.NormalMap);
            ConfigureLinearTextureImporter(spec.MetallicPath);
            ConfigureLinearTextureImporter(spec.RoughnessPath);

            Directory.CreateDirectory(Path.GetDirectoryName(spec.MaterialPath) ?? Milestone23AssetGenerator.ArtPassMaterialDirectory);
            var material = AssetDatabase.LoadAssetAtPath<Material>(spec.MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                AssetDatabase.CreateAsset(material, spec.MaterialPath);
            }

            material.name = Path.GetFileNameWithoutExtension(spec.MaterialPath);
            material.shader = Shader.Find("Universal Render Pipeline/Lit") ?? material.shader;
            SetColor(material, "_BaseColor", Color.white);
            SetColor(material, "_Color", Color.white);
            SetTexture(material, "_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AlbedoPath));
            SetTexture(material, "_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AlbedoPath));
            SetTexture(material, "_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.NormalPath));
            SetTexture(material, "_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.MetallicPath));
            SetFloat(material, "_BumpScale", 1f);
            SetFloat(material, "_Metallic", spec.Metallic);
            SetFloat(material, "_Smoothness", spec.Smoothness);
            SetTexture(material, "_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.EmissionPath));
            SetColor(material, "_EmissionColor", Color.white);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateOrUpdateMeshyPrefab(MeshyEnvironmentPropSpec spec, Material material)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(spec.FbxPath);
            if (source == null)
            {
                Debug.LogWarning($"Meshy environment prop source missing for {spec.DisplayName}: {spec.FbxPath}");
                return null;
            }

            var root = new GameObject(spec.PrefabRootName);
            try
            {
                root.transform.localRotation = Quaternion.identity;
                root.transform.localPosition = Vector3.zero;
                root.transform.localScale = Vector3.one;
                root.AddComponent<PresentationVisualMarker>().Configure(spec.PrefabRole, isFallback: false);

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
                AssignMaterialToRenderers(model, material);
                StripVisualOnlyComponents(root);
                var modelRotation = Quaternion.Euler(spec.ModelLocalEuler) *
                    Quaternion.AngleAxis(spec.ModelYawCorrectionDegrees, Vector3.back);
                if (spec.StraightenFootprintYaw)
                {
                    modelRotation = ResolveStraightenedFootprintRotation(model.transform, modelRotation);
                }

                PresentationVisualBoundsFitter.FitToTargetBounds(
                    model.transform,
                    spec.TargetBounds,
                    spec.TargetBottomLocalY,
                    modelRotation);

                Directory.CreateDirectory(Path.GetDirectoryName(spec.PrefabPath) ?? Milestone23AssetGenerator.ArtPassRoot);
                return PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Quaternion ResolveStraightenedFootprintRotation(Transform model, Quaternion baseRotation)
        {
            var originalPosition = model.localPosition;
            var originalRotation = model.localRotation;
            var originalScale = model.localScale;
            try
            {
                const float maxYawDegrees = 45f;
                const float yawStepDegrees = 0.5f;
                const float minimumImprovement = 0.01f;

                model.localPosition = Vector3.zero;
                model.localScale = Vector3.one;
                model.localRotation = baseRotation;
                if (!TryGetRendererBounds(model, out var baseBounds))
                {
                    return baseRotation;
                }

                var baseScore = FootprintScore(baseBounds);
                var bestScore = baseScore;
                var bestRotation = baseRotation;
                for (var yaw = -maxYawDegrees; yaw <= maxYawDegrees; yaw += yawStepDegrees)
                {
                    var candidateRotation = Quaternion.AngleAxis(yaw, Vector3.up) * baseRotation;
                    model.localRotation = candidateRotation;
                    if (!TryGetRendererBounds(model, out var bounds))
                    {
                        continue;
                    }

                    var score = FootprintScore(bounds);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestRotation = candidateRotation;
                    }
                }

                return baseScore > 0f && (baseScore - bestScore) / baseScore >= minimumImprovement
                    ? bestRotation
                    : baseRotation;
            }
            finally
            {
                model.localPosition = originalPosition;
                model.localRotation = originalRotation;
                model.localScale = originalScale;
            }
        }

        private static float FootprintScore(Bounds bounds)
        {
            return bounds.size.x * bounds.size.z;
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds && bounds.size.sqrMagnitude > 0.0001f;
        }

        private static void StripBlenderTemplateObjects(Transform model)
        {
            if (model == null)
            {
                return;
            }

            var renderers = model.GetComponentsInChildren<Renderer>(includeInactive: true);
            var hasNonTemplateRenderer = renderers.Any(renderer => renderer != null && !IsBlenderTemplateObject(renderer.transform));
            if (!hasNonTemplateRenderer)
            {
                return;
            }

            for (var index = model.childCount - 1; index >= 0; index--)
            {
                var child = model.GetChild(index);
                if (IsBlenderTemplateObject(child))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static bool IsBlenderTemplateObject(Transform transform)
        {
            return transform != null && transform.name == "Cube";
        }

        private static void RefreshPresentationCatalog(
            IReadOnlyDictionary<MaterialRole, Material> materials,
            IReadOnlyDictionary<PresentationPrefabRole, GameObject> prefabs)
        {
            var specs = PropRows();
            var materialRoles = specs.Select(spec => spec.MaterialRole).ToHashSet();
            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone23AssetGenerator.ArtPassPalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MaterialPaletteDefinition>();
                AssetDatabase.CreateAsset(palette, Milestone23AssetGenerator.ArtPassPalettePath);
            }

            var materialBindings = palette.Bindings
                .Where(binding => !materialRoles.Contains(binding.Role))
                .ToList();
            foreach (var spec in specs)
            {
                if (materials.TryGetValue(spec.MaterialRole, out var material) && material != null)
                {
                    materialBindings.Add(new MaterialRoleBinding(spec.MaterialRole, material, spec.FallbackColor));
                }
            }

            palette.Configure(materialBindings
                .OrderBy(binding => binding.Role.ToString(), StringComparer.Ordinal)
                .ToArray());
            EditorUtility.SetDirty(palette);

            var prefabRoles = specs.Select(spec => spec.PrefabRole).ToHashSet();
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            var prefabBindings = catalog.PrefabBindings
                .Where(binding => !prefabRoles.Contains(binding.Role))
                .ToList();
            foreach (var spec in specs)
            {
                if (prefabs.TryGetValue(spec.PrefabRole, out var prefab) && prefab != null)
                {
                    prefabBindings.Add(new PresentationPrefabBinding(spec.PrefabRole, prefab));
                }
            }

            catalog.Configure(
                palette,
                catalog.VfxCues,
                catalog.AudioCues,
                prefabBindings.OrderBy(binding => binding.Role.ToString(), StringComparer.Ordinal).ToArray());
            EditorUtility.SetDirty(catalog);
        }

        private static void AssignMaterialToRenderers(GameObject root, Material material)
        {
            if (root == null || material == null)
            {
                return;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
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

        private static void SetTexture(Material material, string propertyName, Texture texture)
        {
            if (material != null && texture != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetColor(Material material, string propertyName, Color color)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        public readonly struct MeshyEnvironmentPropSpec
        {
            public MeshyEnvironmentPropSpec(
                string displayName,
                PresentationPrefabRole prefabRole,
                MaterialRole materialRole,
                string prefabPath,
                string prefabRootName,
                string materialPath,
                string modelName,
                string assetStem,
                Vector3 targetBounds,
                float targetBottomLocalY,
                Vector3 modelLocalEuler,
                float modelYawCorrectionDegrees,
                bool straightenFootprintYaw,
                Color fallbackColor,
                float metallic,
                float smoothness)
            {
                DisplayName = displayName;
                PrefabRole = prefabRole;
                MaterialRole = materialRole;
                PrefabPath = prefabPath;
                PrefabRootName = prefabRootName;
                MaterialPath = materialPath;
                ModelName = modelName;
                AssetStem = assetStem;
                TargetBounds = targetBounds;
                TargetBottomLocalY = targetBottomLocalY;
                ModelLocalEuler = modelLocalEuler;
                ModelYawCorrectionDegrees = modelYawCorrectionDegrees;
                StraightenFootprintYaw = straightenFootprintYaw;
                FallbackColor = fallbackColor;
                Metallic = metallic;
                Smoothness = smoothness;
            }

            public string DisplayName { get; }
            public PresentationPrefabRole PrefabRole { get; }
            public MaterialRole MaterialRole { get; }
            public string PrefabPath { get; }
            public string PrefabRootName { get; }
            public string MaterialPath { get; }
            public string ModelName { get; }
            public string AssetStem { get; }
            public Vector3 TargetBounds { get; }
            public float TargetBottomLocalY { get; }
            public Vector3 ModelLocalEuler { get; }
            public float ModelYawCorrectionDegrees { get; }
            public bool StraightenFootprintYaw { get; }
            public Color FallbackColor { get; }
            public float Metallic { get; }
            public float Smoothness { get; }
            public string FbxPath => $"{AssetStem}.fbx";
            public string AlbedoPath => $"{AssetStem}.png";
            public string EmissionPath => $"{AssetStem}_emission.png";
            public string MetallicPath => $"{AssetStem}_metallic.png";
            public string NormalPath => $"{AssetStem}_normal.png";
            public string RoughnessPath => $"{AssetStem}_roughness.png";
        }
    }
}
