using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone132BiomeWorldSelectionLockValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 132 Biome World Selection Lock Validation")]
        public static void ValidateMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static bool Validate()
        {
            return Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        public static bool Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var report = Milestone132BiomeWorldSelectionLockAssetGenerator.BuildReport();
            var generatedReportsValid = ValidateGeneratedReports(out var reportError);
            var passed = report.passed && generatedReportsValid;
            if (passed)
            {
                Debug.Log("Milestone 132 Biome + World Selection Lock validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return true;
            }

            var failures = string.Join("\n", report.failures ?? new string[0]);
            if (!string.IsNullOrWhiteSpace(reportError))
            {
                failures = string.IsNullOrWhiteSpace(failures) ? reportError : $"{failures}\n{reportError}";
            }

            Debug.LogError($"Milestone 132 Biome + World Selection Lock validation failed.\n{failures}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static bool ValidateGeneratedReports(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone132BiomeWorldSelectionLockAssetGenerator.ReportMarkdownPath))
            {
                error = $"Missing `{Milestone132BiomeWorldSelectionLockAssetGenerator.ReportMarkdownPath}`.";
                return false;
            }

            if (!File.Exists(Milestone132BiomeWorldSelectionLockAssetGenerator.ReportJsonPath))
            {
                error = $"Missing `{Milestone132BiomeWorldSelectionLockAssetGenerator.ReportJsonPath}`.";
                return false;
            }

            var markdown = File.ReadAllText(Milestone132BiomeWorldSelectionLockAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M132 Biome + World Selection Lock Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone132BiomeWorldSelectionLockAssetGenerator.LockId))
            {
                error = "Generated M132 markdown report is not passing or has the wrong lock id.";
                return false;
            }

            var json = JsonUtility.FromJson<Milestone132BiomeWorldSelectionLockReport>(
                File.ReadAllText(Milestone132BiomeWorldSelectionLockAssetGenerator.ReportJsonPath));
            if (json == null ||
                !json.passed ||
                json.lockId != Milestone132BiomeWorldSelectionLockAssetGenerator.LockId)
            {
                error = "Generated M132 JSON report is not passing or has the wrong lock id.";
                return false;
            }

            return true;
        }
    }
}
