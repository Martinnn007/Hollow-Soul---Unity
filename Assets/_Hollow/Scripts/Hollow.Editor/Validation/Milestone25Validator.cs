using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone25Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/VerticalSliceLockDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/VerticalSliceLockReport.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/VerticalSliceLockRunner.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/VerticalSlicePdfExporter.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/VerticalSliceContentValidator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone25AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone25Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone25VerticalSliceContentLockTests.cs",
            "Docs/Milestone25VerticalSliceContentLock.md",
            Milestone25AssetGenerator.VerticalSliceLockPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 25 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: Application.isBatchMode);
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M25 file: {file}");
                }
            }

            var definition = AssetDatabase.LoadAssetAtPath<VerticalSliceLockDefinition>(Milestone25AssetGenerator.VerticalSliceLockPath);
            ValidateDefinition(definition, failures);
            ValidateAddressables(failures);
            ValidateReports(definition, failures);

            if (definition != null)
            {
                var report = VerticalSliceContentValidator.ValidateLock(definition);
                foreach (var check in report.checks.Where(check => check.result == PlatformBuildQaResult.Failed))
                {
                    failures.Add($"M25 lock check failed ({check.id}): {string.Join("; ", check.messages)}");
                }
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 25 validation passed.");
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

        private static void ValidateDefinition(VerticalSliceLockDefinition definition, List<string> failures)
        {
            if (definition == null)
            {
                failures.Add("M25 vertical slice lock definition is missing.");
                return;
            }

            if (definition.BranchIdentity != BranchGenerator.BranchFeaturesId)
            {
                failures.Add("M25 lock must pin the M20 branch features identity.");
            }

            if (definition.LockedSeed != BranchGenerator.DefaultSeededMacroSeed)
            {
                failures.Add("M25 lock must pin seed 15001.");
            }

            if (!definition.AllowEmptyApprovedRoomPool)
            {
                failures.Add("M25 lock must allow an empty approved-room pool while keeping fixtures as fallback.");
            }

            if (!definition.RequireArtPassPrefabs)
            {
                failures.Add("M25 lock must require ArtPass prefab bindings for core presentation roles.");
            }

            if (definition.RequiredShopOfferCount != 3 || definition.RequiredNextBranchPortalCount != 3)
            {
                failures.Add("M25 lock must require three shop offers and three next-branch portals.");
            }

            if (definition.RequiredPrefabRoles == null ||
                !definition.RequiredPrefabRoles.Contains(PresentationPrefabRole.Player) ||
                !definition.RequiredPrefabRoles.Contains(PresentationPrefabRole.EnemyBoss) ||
                !definition.RequiredPrefabRoles.Contains(PresentationPrefabRole.HubShop) ||
                !definition.RequiredPrefabRoles.Contains(PresentationPrefabRole.NextBranchPortal))
            {
                failures.Add("M25 lock is missing required core ArtPass prefab roles.");
            }

            foreach (var target in new[] { "Windows", "VisionOSBounded", "VisionOSImmersive" })
            {
                if (!definition.PlatformChecklistTargets.Contains(target))
                {
                    failures.Add($"M25 lock missing platform checklist target {target}.");
                }
            }
        }

        private static void ValidateAddressables(List<string> failures)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            if (settings == null)
            {
                failures.Add("M25 requires Addressables settings.");
                return;
            }

            if (!settings.GetLabels().Contains(Milestone25AssetGenerator.VerticalSliceAddressableLabel))
            {
                failures.Add($"Missing Addressables label {Milestone25AssetGenerator.VerticalSliceAddressableLabel}.");
            }

            var guid = AssetDatabase.AssetPathToGUID(Milestone25AssetGenerator.VerticalSliceLockPath);
            var entry = string.IsNullOrWhiteSpace(guid) ? null : settings.FindAssetEntry(guid, includeImplicit: false);
            if (entry == null ||
                !entry.labels.Contains(Milestone25AssetGenerator.VerticalSliceAddressableLabel) ||
                !entry.labels.Contains("hollow.data"))
            {
                failures.Add("M25 lock asset must be Addressable with vertical-slice and data labels.");
            }
        }

        private static void ValidateReports(VerticalSliceLockDefinition definition, List<string> failures)
        {
            if (definition == null)
            {
                return;
            }

            var jsonPath = Path.Combine(definition.ReportRoot, definition.LatestJsonFileName);
            var markdownPath = Path.Combine(definition.ReportRoot, definition.LatestMarkdownFileName);
            if (!File.Exists(jsonPath))
            {
                failures.Add($"Latest M25 lock JSON report is missing: {jsonPath}");
            }
            else
            {
                var report = JsonUtility.FromJson<VerticalSliceLockReport>(File.ReadAllText(jsonPath));
                if (report == null || string.IsNullOrWhiteSpace(report.reportId))
                {
                    failures.Add("Latest M25 lock JSON report could not be decoded.");
                }
            }

            if (!File.Exists(markdownPath))
            {
                failures.Add($"Latest M25 lock Markdown report is missing: {markdownPath}");
            }

            if (!File.Exists(definition.PdfOutputPath))
            {
                failures.Add($"M25 PDF handoff is missing: {definition.PdfOutputPath}");
            }
        }
    }
}
