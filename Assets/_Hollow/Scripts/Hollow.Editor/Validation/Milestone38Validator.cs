using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone38Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ArtPassAssetTargetDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ArtPassTargetCatalogDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone38AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone38Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone38ArtPassRafalPipelineTests.cs",
            "Docs/Milestone38ArtPassRafalPipeline.md",
            Milestone38AssetGenerator.TargetCatalogPath,
            Milestone38AssetGenerator.HandoffReportPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 38 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M38 file: {file}");
                }
            }

            ValidateDirectories(failures);
            ValidateTargetCatalog(failures);
            ValidateArtPassRuntimeCatalog(failures);

            var artPassReport = ArtPassContentValidator.ValidateAll();
            failures.AddRange(artPassReport.Failures);
            foreach (var warning in artPassReport.Warnings)
            {
                Debug.LogWarning(warning);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 38 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateDirectories(List<string> failures)
        {
            foreach (var directory in new[]
            {
                Milestone38AssetGenerator.RafalIntakeDirectory,
                Milestone38AssetGenerator.RafalModelDirectory,
                Milestone38AssetGenerator.RafalTextureDirectory,
                Milestone38AssetGenerator.RafalMaterialDirectory,
                Milestone38AssetGenerator.RafalPrefabDirectory,
                Milestone38AssetGenerator.TargetDirectory
            })
            {
                if (!Directory.Exists(directory))
                {
                    failures.Add($"Missing M38 ArtPass directory: {directory}");
                }
            }
        }

        private static void ValidateTargetCatalog(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ArtPassTargetCatalogDefinition>(Milestone38AssetGenerator.TargetCatalogPath);
            if (catalog == null)
            {
                failures.Add($"Missing M38 target catalog: {Milestone38AssetGenerator.TargetCatalogPath}");
                return;
            }

            if (catalog.Targets.Count < Enum.GetValues(typeof(PresentationPrefabRole)).Length)
            {
                failures.Add("M38 target catalog must include at least one target for every PresentationPrefabRole.");
            }

            var duplicate = catalog.Targets
                .Where(target => target != null)
                .GroupBy(target => target.TargetId)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                failures.Add($"M38 target catalog has duplicate target ID: {duplicate.Key}");
            }

            foreach (PresentationPrefabRole role in Enum.GetValues(typeof(PresentationPrefabRole)))
            {
                if (!catalog.Targets.Any(target => target != null && target.PrefabRole == role))
                {
                    failures.Add($"M38 target catalog is missing role {role}.");
                }
            }

            foreach (var role in Milestone38AssetGenerator.RequiredRuntimeRoles)
            {
                var target = catalog.Targets.FirstOrDefault(candidate => candidate != null && candidate.PrefabRole == role);
                if (target == null || target.Priority != ArtPassAssetTargetPriority.Critical || !target.RequiredForVerticalSlice)
                {
                    failures.Add($"M38 runtime role {role} must be a critical vertical-slice target.");
                }
            }

            foreach (var target in catalog.Targets.Where(target => target != null))
            {
                if (string.IsNullOrWhiteSpace(target.TargetId) ||
                    string.IsNullOrWhiteSpace(target.DisplayName) ||
                    string.IsNullOrWhiteSpace(target.Goal) ||
                    string.IsNullOrWhiteSpace(target.PrefabPath) ||
                    target.RequiredAssets.Count == 0 ||
                    target.AcceptanceChecks.Count == 0)
                {
                    failures.Add($"M38 target {target.name} is missing handoff metadata.");
                }

                if (!File.Exists(target.PrefabPath))
                {
                    failures.Add($"M38 target {target.TargetId} points to missing prefab: {target.PrefabPath}");
                }
            }
        }

        private static void ValidateArtPassRuntimeCatalog(List<string> failures)
        {
            var presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (presentationCatalog == null)
            {
                failures.Add($"Missing presentation catalog: {Milestone9AssetGenerator.CatalogPath}");
                return;
            }

            foreach (var role in new[]
            {
                PresentationPrefabRole.HubShopCard,
                PresentationPrefabRole.WeaponMelee,
                PresentationPrefabRole.WeaponRanged,
                PresentationPrefabRole.Armor,
                PresentationPrefabRole.ActiveItemPickup,
                PresentationPrefabRole.ConsumableCardPickup
            })
            {
                if (!presentationCatalog.TryGetPrefab(role, out var prefab) || prefab == null)
                {
                    failures.Add($"M38 presentation catalog is missing generated ArtPass prefab binding for {role}.");
                }
            }
        }
    }
}
