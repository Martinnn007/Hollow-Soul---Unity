using System;
using System.Collections.Generic;
using System.IO;
using Hollow.Core.App;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hollow.Editor.Generation
{
    public readonly struct HollowShaderWarmupValidationResult
    {
        public HollowShaderWarmupValidationResult(bool profileExists, bool collectionExists, int collectionCount, string message)
        {
            ProfileExists = profileExists;
            CollectionExists = collectionExists;
            CollectionCount = collectionCount;
            Message = message ?? string.Empty;
        }

        public bool ProfileExists { get; }

        public bool CollectionExists { get; }

        public int CollectionCount { get; }

        public string Message { get; }

        public bool IsValid => ProfileExists && CollectionExists && CollectionCount > 0;
    }

    public static class HollowShaderWarmupAssetGenerator
    {
        public const string BootShaderVariantCollectionPath = "Assets/_Hollow/Shaders/HollowBootShaderVariants.shadervariants";
        public const string BootShaderWarmupProfilePath = "Assets/_Hollow/Resources/Hollow/HollowBootShaderWarmupProfile.asset";

        private static readonly string[] SearchRoots =
        {
            "Assets/_Hollow",
            "Assets/Settings"
        };

        private static readonly string[] DefaultShaderNames =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "UI/Default"
        };

        private static readonly string[] CandidatePassTypes =
        {
            "Normal"
        };

        [MenuItem("Hollow/Performance/Generate Shader Warmup Assets")]
        public static void GenerateShaderWarmupAssets()
        {
            EnsureDirectory(BootShaderVariantCollectionPath);
            EnsureDirectory(BootShaderWarmupProfilePath);

            var collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(BootShaderVariantCollectionPath);
            if (collection == null)
            {
                collection = new ShaderVariantCollection();
                AssetDatabase.CreateAsset(collection, BootShaderVariantCollectionPath);
            }

            collection.Clear();
            var added = PopulateCollection(collection);
            EditorUtility.SetDirty(collection);

            var profile = AssetDatabase.LoadAssetAtPath<HollowShaderWarmupProfile>(BootShaderWarmupProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<HollowShaderWarmupProfile>();
                AssetDatabase.CreateAsset(profile, BootShaderWarmupProfilePath);
            }

            profile.Configure(
                "Boot Shader Warmup",
                nextEnabledForBoot: true,
                nextTargetRenderProfileLabel: "Boot/Global",
                nextCollections: new[] { collection },
                nextMaxExpectedWarmupMilliseconds: 100f,
                nextNotes: $"Generated from project materials and common URP/UI shaders. Initial variant count: {added}.");
            EditorUtility.SetDirty(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated shader warmup assets: {BootShaderVariantCollectionPath} ({added} variants), {BootShaderWarmupProfilePath}.");
        }

        public static HollowShaderWarmupValidationResult ValidateBootShaderWarmupAssets()
        {
            var profile = AssetDatabase.LoadAssetAtPath<HollowShaderWarmupProfile>(BootShaderWarmupProfilePath);
            var collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(BootShaderVariantCollectionPath);
            var collectionCount = profile != null ? profile.CollectionCount : 0;
            var message = profile == null
                ? "Boot shader warmup profile is missing."
                : collection == null
                    ? "Boot shader variant collection is missing."
                    : collectionCount <= 0
                        ? "Boot shader warmup profile has no collections."
                        : "Boot shader warmup assets are present.";

            return new HollowShaderWarmupValidationResult(profile != null, collection != null, collectionCount, message);
        }

        private static int PopulateCollection(ShaderVariantCollection collection)
        {
            var added = 0;
            var visitedShaders = new HashSet<Shader>();
            foreach (var materialGuid in AssetDatabase.FindAssets("t:Material", SearchRoots))
            {
                var materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null || material.shader == null)
                {
                    continue;
                }

                visitedShaders.Add(material.shader);
                added += AddVariants(collection, material.shader, material.shaderKeywords);
                added += AddVariants(collection, material.shader, Array.Empty<string>());
            }

            foreach (var shaderName in DefaultShaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null || !visitedShaders.Add(shader))
                {
                    continue;
                }

                added += AddVariants(collection, shader, Array.Empty<string>());
            }

            return added;
        }

        private static int AddVariants(ShaderVariantCollection collection, Shader shader, string[] keywords)
        {
            var added = 0;
            foreach (var passName in CandidatePassTypes)
            {
                if (!Enum.TryParse(passName, out PassType passType))
                {
                    continue;
                }

                if (TryAddVariant(collection, shader, passType, keywords))
                {
                    added++;
                }
            }

            return added;
        }

        private static bool TryAddVariant(ShaderVariantCollection collection, Shader shader, PassType passType, string[] keywords)
        {
            try
            {
                var variant = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords ?? Array.Empty<string>());
                return collection.Add(variant);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void EnsureDirectory(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
