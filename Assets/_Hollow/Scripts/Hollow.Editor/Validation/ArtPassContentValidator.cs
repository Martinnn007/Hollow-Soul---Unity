using System;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class ArtPassContentValidator
    {
        public static ContentValidationReport ValidateAll()
        {
            AssetDatabase.Refresh();
            var report = new ContentValidationReport();
            ValidateDirectories(report);
            ValidateCatalog(report);
            ValidatePrefabs(report);
            ValidateAddressables(report);
            return report;
        }

        private static void ValidateDirectories(ContentValidationReport report)
        {
            foreach (var directory in new[]
            {
                Milestone23AssetGenerator.ArtPassRoot,
                Milestone23AssetGenerator.ArtPassVfxDirectory,
                Milestone23AssetGenerator.ArtPassMaterialDirectory,
                Milestone23AssetGenerator.ArtPassAudioDirectory,
                Milestone23AssetGenerator.ArtPassModelSourceDirectory
            })
            {
                if (!Directory.Exists(directory))
                {
                    report.AddFailure($"Missing ArtPass directory: {directory}");
                }
            }
        }

        private static void ValidateCatalog(ContentValidationReport report)
        {
            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone23AssetGenerator.ArtPassPalettePath);
            if (palette == null)
            {
                report.AddFailure($"Missing ArtPass material palette: {Milestone23AssetGenerator.ArtPassPalettePath}");
            }
            else
            {
                foreach (MaterialRole role in Enum.GetValues(typeof(MaterialRole)))
                {
                    if (!palette.TryResolve(role, out var material) || material == null)
                    {
                        report.AddFailure($"ArtPass palette does not resolve material role {role}.");
                    }
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                report.AddFailure($"Missing presentation catalog: {Milestone9AssetGenerator.CatalogPath}");
                return;
            }

            PresentationContentProvider.Configure(catalog);
            foreach (PresentationPrefabRole role in Enum.GetValues(typeof(PresentationPrefabRole)))
            {
                if (!catalog.TryGetPrefab(role, out var prefab) || prefab == null)
                {
                    var fallback = PresentationPrefabResolver.Resolve(role);
                    if (fallback != null)
                    {
                        report.AddWarning($"ArtPass prefab binding missing for {role}; runtime fallback is available.");
                    }
                    else
                    {
                        report.AddFailure($"ArtPass prefab binding missing for {role} and no fallback could be resolved.");
                    }

                    continue;
                }

                ValidatePrefab(prefab, role, report);
            }

            foreach (VfxCueId cueId in Enum.GetValues(typeof(VfxCueId)))
            {
                if (!catalog.TryGetVfxCue(cueId, out var cue) || cue == null)
                {
                    report.AddFailure($"ArtPass VFX cue {cueId} must have a placeholder prefab or debug primitive fallback.");
                }
                else if (cue.Prefab == null && !cue.CreateDebugPrimitive)
                {
                    report.AddFailure($"ArtPass VFX cue {cueId} must have a placeholder prefab or debug primitive fallback.");
                }
            }

            foreach (AudioCueId cueId in Enum.GetValues(typeof(AudioCueId)))
            {
                if (!catalog.TryGetAudioCue(cueId, out var cue) || cue == null || cue.Clip == null)
                {
                    report.AddFailure($"ArtPass audio cue {cueId} must have a placeholder clip.");
                }
            }
        }

        private static void ValidatePrefabs(ContentValidationReport report)
        {
            if (!Directory.Exists(Milestone23AssetGenerator.ArtPassRoot))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(Milestone23AssetGenerator.ArtPassRoot, "*.prefab", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(path);
                if (!fileName.StartsWith("AP_", StringComparison.Ordinal) &&
                    !fileName.StartsWith("VFX_", StringComparison.Ordinal))
                {
                    report.AddFailure($"ArtPass prefab wrapper must use AP_ or VFX_ prefix: {path}");
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    report.AddFailure($"ArtPass prefab could not be loaded: {path}");
                }
                else if (!prefab.TryGetComponent<PresentationVisualMarker>(out _))
                {
                    report.AddFailure($"ArtPass prefab root must include PresentationVisualMarker: {path}");
                }
            }
        }

        private static void ValidatePrefab(GameObject prefab, PresentationPrefabRole role, ContentValidationReport report)
        {
            var path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(Milestone23AssetGenerator.ArtPassRoot, StringComparison.Ordinal))
            {
                report.AddFailure($"Prefab binding for {role} must point under {Milestone23AssetGenerator.ArtPassRoot}: {path}");
                return;
            }

            var marker = prefab.GetComponent<PresentationVisualMarker>();
            if (marker == null || marker.Role != role)
            {
                report.AddFailure($"ArtPass prefab {path} must declare PresentationVisualMarker role {role}.");
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
            {
                report.AddFailure($"ArtPass prefab {path} has no renderer.");
            }

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null)
                {
                    report.AddFailure($"ArtPass prefab {path}/{renderer.gameObject.name} has no material.");
                }

                var size = renderer.bounds.size;
                if (size.x > 8f || size.y > 8f || size.z > 8f)
                {
                    report.AddWarning($"ArtPass prefab {path}/{renderer.gameObject.name} has large bounds {size}; verify Vision Pro comfort/performance.");
                }
            }

            foreach (var collider in prefab.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                report.AddFailure($"ArtPass visual prefab must not include gameplay colliders: {path}/{collider.gameObject.name}");
            }

            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                if (component == null)
                {
                    report.AddFailure($"ArtPass prefab has missing script component: {path}");
                    continue;
                }

                if (component is not PresentationVisualMarker)
                {
                    report.AddFailure($"ArtPass visual prefab must not require gameplay scripts: {path}/{component.GetType().Name}");
                }
            }
        }

        private static void ValidateAddressables(ContentValidationReport report)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            if (settings == null)
            {
                report.AddFailure("Addressables settings are missing.");
                return;
            }

            var labels = settings.GetLabels();
            foreach (var label in Milestone23AssetGenerator.RequiredAddressableLabels)
            {
                if (!labels.Contains(label))
                {
                    report.AddFailure($"ArtPass Addressables label is missing: {label}");
                }
            }

            if (settings.FindGroup(Milestone23AssetGenerator.AddressablesGroupName) == null)
            {
                report.AddFailure($"ArtPass Addressables group is missing: {Milestone23AssetGenerator.AddressablesGroupName}");
            }

            AssertAddressable(settings, Milestone23AssetGenerator.ArtPassPalettePath, "hollow.artpass.materials", report);
            if (Directory.Exists(Milestone23AssetGenerator.ArtPassRoot))
            {
                foreach (var path in Directory.GetFiles(Milestone23AssetGenerator.ArtPassRoot, "*.prefab", SearchOption.AllDirectories))
                {
                    var requiredLabel = path.Contains("/VFX/", StringComparison.Ordinal)
                        ? "hollow.artpass.vfx"
                        : "hollow.artpass.prefabs";
                    AssertAddressable(settings, path, requiredLabel, report);
                }
            }
        }

        private static void AssertAddressable(AddressableAssetSettings settings, string path, string requiredLabel, ContentValidationReport report)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                report.AddFailure($"ArtPass addressable source is missing: {path}");
                return;
            }

            var entry = settings.FindAssetEntry(guid, includeImplicit: false);
            if (entry == null)
            {
                report.AddFailure($"ArtPass asset is not addressable: {path}");
                return;
            }

            if (!entry.labels.Contains("hollow.artpass") || !entry.labels.Contains(requiredLabel))
            {
                report.AddFailure($"ArtPass asset {path} is missing labels hollow.artpass and/or {requiredLabel}.");
            }
        }
    }
}
