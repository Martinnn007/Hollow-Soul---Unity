using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone131RoomTypeExpansionLockValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 131 Room Type Expansion Lock Validation")]
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
            var report = Milestone131RoomTypeExpansionLockAssetGenerator.BuildReport();
            var generatedReportsValid = ValidateGeneratedReports(out var reportError);
            var passed = report.passed && generatedReportsValid;
            if (passed)
            {
                Debug.Log("Milestone 131 Room Type Expansion Lock validation passed.");
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

            Debug.LogError($"Milestone 131 Room Type Expansion Lock validation failed.\n{failures}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static bool ValidateGeneratedReports(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone131RoomTypeExpansionLockAssetGenerator.ReportMarkdownPath))
            {
                error = $"Missing `{Milestone131RoomTypeExpansionLockAssetGenerator.ReportMarkdownPath}`.";
                return false;
            }

            if (!File.Exists(Milestone131RoomTypeExpansionLockAssetGenerator.ReportJsonPath))
            {
                error = $"Missing `{Milestone131RoomTypeExpansionLockAssetGenerator.ReportJsonPath}`.";
                return false;
            }

            var markdown = File.ReadAllText(Milestone131RoomTypeExpansionLockAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M131 Room Type Expansion Lock Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone131RoomTypeExpansionLockAssetGenerator.LockId))
            {
                error = "Generated M131 markdown report is not passing or has the wrong lock id.";
                return false;
            }

            var json = JsonUtility.FromJson<Milestone131RoomTypeExpansionLockReport>(
                File.ReadAllText(Milestone131RoomTypeExpansionLockAssetGenerator.ReportJsonPath));
            if (json == null ||
                !json.passed ||
                json.lockId != Milestone131RoomTypeExpansionLockAssetGenerator.LockId)
            {
                error = "Generated M131 JSON report is not passing or has the wrong lock id.";
                return false;
            }

            return true;
        }
    }
}
