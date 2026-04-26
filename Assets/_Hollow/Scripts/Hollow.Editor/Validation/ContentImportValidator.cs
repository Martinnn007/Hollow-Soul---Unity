using System;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class ContentImportValidator
    {
        public static ContentValidationReport ValidateAll()
        {
            AssetDatabase.Refresh();
            var report = new ContentValidationReport();
            ValidatePresentationCatalog(report);
            ValidateAddressables(report);
            ValidatePrefabMaterials(report);
            ValidateNamingConventions(report);
            return report;
        }

        private static void ValidatePresentationCatalog(ContentValidationReport report)
        {
            if (!File.Exists(Milestone9AssetGenerator.PalettePath))
            {
                report.AddFailure($"Missing material palette: {Milestone9AssetGenerator.PalettePath}");
            }

            if (!File.Exists(Milestone9AssetGenerator.CatalogPath))
            {
                report.AddFailure($"Missing presentation catalog: {Milestone9AssetGenerator.CatalogPath}");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                report.AddFailure("Presentation content catalog could not be loaded.");
                return;
            }

            if (catalog.MaterialPalette == null)
            {
                report.AddFailure("Presentation content catalog has no material palette.");
            }
            else
            {
                foreach (MaterialRole role in Enum.GetValues(typeof(MaterialRole)))
                {
                    if (!catalog.MaterialPalette.TryResolve(role, out var material) || material == null)
                    {
                        report.AddFailure($"Material palette does not resolve role {role}.");
                    }
                }
            }

            foreach (VfxCueId cueId in Enum.GetValues(typeof(VfxCueId)))
            {
                if (!catalog.TryGetVfxCue(cueId, out _))
                {
                    report.AddFailure($"Presentation catalog is missing VFX cue {cueId}.");
                }
            }

            foreach (AudioCueId cueId in Enum.GetValues(typeof(AudioCueId)))
            {
                if (!catalog.TryGetAudioCue(cueId, out _))
                {
                    report.AddFailure($"Presentation catalog is missing audio cue {cueId}.");
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
            foreach (var label in Milestone9AssetGenerator.RequiredAddressableLabels)
            {
                if (!labels.Contains(label))
                {
                    report.AddFailure($"Addressables label is missing: {label}");
                }
            }

            if (settings.FindGroup(Milestone9AssetGenerator.AddressablesGroupName) == null)
            {
                report.AddFailure($"Addressables group is missing: {Milestone9AssetGenerator.AddressablesGroupName}");
            }

            AssertAddressable(settings, Milestone9AssetGenerator.CatalogPath, "hollow.data", report);
            AssertAddressable(settings, Milestone9AssetGenerator.PalettePath, "hollow.data", report);
            AssertAddressable(settings, "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json", "hollow.rooms", report);
            AssertAddressable(settings, "Assets/_Hollow/Prefabs/Rooms/RoomRuntimeRoot.prefab", "hollow.rooms", report);
            AssertAddressable(settings, "Assets/_Hollow/Prefabs/Combat/EnemyBase.prefab", "hollow.enemies", report);
            AssertAddressable(settings, "Assets/_Hollow/Prefabs/Combat/ProjectileBase.prefab", "hollow.player", report);
            AssertAddressable(settings, "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab", "hollow.player", report);
            AssertAddressable(settings, "Assets/_Hollow/Prefabs/UI/MainMenuRoot.prefab", "hollow.ui", report);
            AssertAddressable(settings, "Assets/_Hollow/Prefabs/Designer/RoomDesignerRoot.prefab", "hollow.designer", report);
        }

        private static void AssertAddressable(AddressableAssetSettings settings, string path, string requiredLabel, ContentValidationReport report)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                report.AddFailure($"Addressable source asset is missing: {path}");
                return;
            }

            var entry = settings.FindAssetEntry(guid, includeImplicit: false);
            if (entry == null)
            {
                report.AddFailure($"Asset is not addressable: {path}");
                return;
            }

            if (!entry.labels.Contains(requiredLabel))
            {
                report.AddFailure($"Asset {path} is missing Addressables label {requiredLabel}.");
            }
        }

        private static void ValidatePrefabMaterials(ContentValidationReport report)
        {
            AssertPrefabHasMaterials("Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab", report);
            AssertPrefabHasMaterials("Assets/_Hollow/Prefabs/Combat/EnemyBase.prefab", report);
            AssertPrefabHasMaterials("Assets/_Hollow/Prefabs/Combat/ProjectileBase.prefab", report);
            AssertPrefabHasMaterials("Assets/_Hollow/Prefabs/Rewards/RoomRewardPickup.prefab", report);
            AssertPrefabHasMaterials("Assets/_Hollow/Prefabs/Rewards/HubReturnPortal.prefab", report);
        }

        private static void AssertPrefabHasMaterials(string path, ContentValidationReport report)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                report.AddFailure($"Missing prefab: {path}");
                return;
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
            {
                report.AddFailure($"Prefab has no renderers to validate: {path}");
                return;
            }

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null)
                {
                    report.AddFailure($"Prefab renderer has no material: {path}/{renderer.gameObject.name}");
                }
            }
        }

        private static void ValidateNamingConventions(ContentValidationReport report)
        {
            if (!Directory.Exists(Milestone9AssetGenerator.MaterialDirectory))
            {
                report.AddFailure($"Missing material directory: {Milestone9AssetGenerator.MaterialDirectory}");
                return;
            }

            if (!Directory.Exists(Milestone9AssetGenerator.VfxCueDirectory))
            {
                report.AddFailure($"Missing VFX cue directory: {Milestone9AssetGenerator.VfxCueDirectory}");
                return;
            }

            if (!Directory.Exists(Milestone9AssetGenerator.AudioCueDirectory))
            {
                report.AddFailure($"Missing audio cue directory: {Milestone9AssetGenerator.AudioCueDirectory}");
                return;
            }

            foreach (var materialPath in Directory.GetFiles(Milestone9AssetGenerator.MaterialDirectory, "*.mat"))
            {
                if (!Path.GetFileName(materialPath).StartsWith("M_", StringComparison.Ordinal))
                {
                    report.AddFailure($"Prototype material must use M_ prefix: {materialPath}");
                }
            }

            foreach (var cuePath in Directory.GetFiles(Milestone9AssetGenerator.VfxCueDirectory, "*.asset"))
            {
                if (!Path.GetFileName(cuePath).StartsWith("VfxCue_", StringComparison.Ordinal))
                {
                    report.AddFailure($"VFX cue asset must use VfxCue_ prefix: {cuePath}");
                }
            }

            foreach (var cuePath in Directory.GetFiles(Milestone9AssetGenerator.AudioCueDirectory, "*.asset"))
            {
                if (!Path.GetFileName(cuePath).StartsWith("AudioCue_", StringComparison.Ordinal))
                {
                    report.AddFailure($"Audio cue asset must use AudioCue_ prefix: {cuePath}");
                }
            }
        }
    }
}
