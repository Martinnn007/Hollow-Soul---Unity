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
    public static class Milestone49Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ArtPassProductionStatus.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ArtPassProductionTargetRecord.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ArtPassProductionStatusReport.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/ArtPassProductionValidator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone49AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone49Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone49ArtPassProductionIntegrationTests.cs",
            Milestone49AssetGenerator.DocsPath,
            Milestone49AssetGenerator.ReportJsonPath,
            Milestone49AssetGenerator.ReportMarkdownPath,
            Milestone49AssetGenerator.PdfPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 49 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M49 file: {file}");
                }
            }

            ValidateGeneratedReport(failures);
            ValidateLiveProductionStatus(failures);

            var artPassReport = ArtPassContentValidator.ValidateAll();
            failures.AddRange(artPassReport.Failures);
            foreach (var warning in artPassReport.Warnings)
            {
                Debug.LogWarning(warning);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 49 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateGeneratedReport(List<string> failures)
        {
            if (!File.Exists(Milestone49AssetGenerator.ReportJsonPath))
            {
                return;
            }

            var report = JsonUtility.FromJson<ArtPassProductionStatusReport>(File.ReadAllText(Milestone49AssetGenerator.ReportJsonPath));
            if (report == null || report.targets == null || report.targets.Length == 0)
            {
                failures.Add("M49 production status JSON report could not be decoded.");
                return;
            }

            report.Recalculate();
            if (report.totalTargets != Enum.GetValues(typeof(PresentationPrefabRole)).Length)
            {
                failures.Add($"M49 report must track every PresentationPrefabRole. Expected {Enum.GetValues(typeof(PresentationPrefabRole)).Length}, found {report.totalTargets}.");
            }

            if (report.targets.Any(target => string.IsNullOrWhiteSpace(target.role) || string.IsNullOrWhiteSpace(target.displayName)))
            {
                failures.Add("M49 report contains target rows with missing role or display name.");
            }
        }

        private static void ValidateLiveProductionStatus(List<string> failures)
        {
            var report = ArtPassProductionValidator.BuildReport();
            if (report.totalTargets != Enum.GetValues(typeof(PresentationPrefabRole)).Length)
            {
                failures.Add("M49 live production report does not cover every PresentationPrefabRole.");
            }

            foreach (var target in report.targets)
            {
                if (target.status == ArtPassProductionStatus.MissingBinding)
                {
                    failures.Add($"M49 missing active ArtPass binding for {target.role}: {string.Join("; ", target.errors ?? Array.Empty<string>())}");
                }

                if (target.status == ArtPassProductionStatus.UnsafePrefab)
                {
                    failures.Add($"M49 unsafe ArtPass prefab for {target.role}: {string.Join("; ", target.errors ?? Array.Empty<string>())}");
                }
            }
        }
    }
}
