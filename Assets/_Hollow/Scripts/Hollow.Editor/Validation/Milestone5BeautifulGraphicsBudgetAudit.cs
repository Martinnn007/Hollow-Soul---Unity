using System;
using System.Collections.Generic;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public readonly struct Milestone5TextureAuditInput
    {
        public Milestone5TextureAuditInput(string path, TextureImporterType textureType, bool mipmapEnabled, int maxTextureSize, TextureImporterCompression compression)
        {
            Path = path ?? string.Empty;
            TextureType = textureType;
            MipmapEnabled = mipmapEnabled;
            MaxTextureSize = maxTextureSize;
            Compression = compression;
        }

        public string Path { get; }

        public TextureImporterType TextureType { get; }

        public bool MipmapEnabled { get; }

        public int MaxTextureSize { get; }

        public TextureImporterCompression Compression { get; }
    }

    public static class Milestone5BeautifulGraphicsBudgetAudit
    {
        public static IEnumerable<string> CollectProjectWarnings(IReadOnlyList<HollowRenderProfileDefinition> profiles)
        {
            var strictestWorldTexture = 1024;
            var strictestUiSprite = 768;
            foreach (var profile in profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                strictestWorldTexture = Mathf.Min(strictestWorldTexture, profile.WorldTextureMaxSize);
                strictestUiSprite = Mathf.Min(strictestUiSprite, profile.UiSpriteMaxSize);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { "Assets/_Hollow" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var input = new Milestone5TextureAuditInput(path, importer.textureType, importer.mipmapEnabled, importer.maxTextureSize, importer.textureCompression);
                if (TryEvaluateTexture(input, strictestWorldTexture, strictestUiSprite, out var warning))
                {
                    yield return warning;
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Hollow/Prefabs" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("VFX", StringComparison.OrdinalIgnoreCase) < 0 &&
                    path.IndexOf("Vfx", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                if (TryEvaluateVfxPrefab(prefab, out var warning))
                {
                    yield return $"{path}: {warning}";
                }
            }
        }

        public static bool TryEvaluateTexture(Milestone5TextureAuditInput input, int worldTextureMaxSize, int uiSpriteMaxSize, out string warning)
        {
            warning = string.Empty;
            var isUi = input.TextureType == TextureImporterType.Sprite || input.Path.IndexOf("/UI/", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isUi)
            {
                if (input.MipmapEnabled)
                {
                    warning = $"{input.Path}: UI sprite texture has mipmaps enabled.";
                    return true;
                }

                if (input.MaxTextureSize > uiSpriteMaxSize)
                {
                    warning = $"{input.Path}: UI sprite max size {input.MaxTextureSize} exceeds {uiSpriteMaxSize}.";
                    return true;
                }
            }
            else
            {
                if (!input.MipmapEnabled)
                {
                    warning = $"{input.Path}: world texture has mipmaps disabled.";
                    return true;
                }

                if (input.MaxTextureSize > worldTextureMaxSize)
                {
                    warning = $"{input.Path}: world texture max size {input.MaxTextureSize} exceeds {worldTextureMaxSize}.";
                    return true;
                }
            }

            if (input.MaxTextureSize >= 2048 && input.Compression == TextureImporterCompression.Uncompressed)
            {
                warning = $"{input.Path}: large texture is uncompressed.";
                return true;
            }

            return false;
        }

        public static bool TryEvaluateVfxPrefab(GameObject prefab, out string warning)
        {
            warning = string.Empty;
            if (prefab == null)
            {
                return false;
            }

            var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            for (var index = 0; index < particleSystems.Length; index++)
            {
                var particleSystem = particleSystems[index];
                if (particleSystem == null)
                {
                    continue;
                }

                var main = particleSystem.main;
                if (main.maxParticles > 256)
                {
                    warning = $"{particleSystem.name} max particles {main.maxParticles} exceeds M5 repeated-combat VFX budget.";
                    return true;
                }
            }

            return false;
        }
    }
}
