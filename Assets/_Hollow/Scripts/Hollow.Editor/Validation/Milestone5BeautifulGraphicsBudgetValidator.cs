using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone5BeautifulGraphicsBudgetValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 5 Beautiful Graphics Budget Validation")]
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
            var report = Milestone5BeautifulGraphicsBudgetAssetGenerator.BuildReport();
            var reportsValid = ValidateGeneratedReports(out var reportError);
            var passed = report.passed && reportsValid;
            if (passed)
            {
                Debug.Log("Milestone 5 Beautiful Graphics Budget validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return true;
            }

            var failures = string.Join("\n", report.failures ?? System.Array.Empty<string>());
            if (!string.IsNullOrWhiteSpace(reportError))
            {
                failures = string.IsNullOrWhiteSpace(failures) ? reportError : $"{failures}\n{reportError}";
            }

            Debug.LogError($"Milestone 5 Beautiful Graphics Budget validation failed.\n{failures}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static bool ValidateGeneratedReports(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone5BeautifulGraphicsBudgetAssetGenerator.ReportMarkdownPath))
            {
                error = $"Missing `{Milestone5BeautifulGraphicsBudgetAssetGenerator.ReportMarkdownPath}`.";
                return false;
            }

            if (!File.Exists(Milestone5BeautifulGraphicsBudgetAssetGenerator.ReportJsonPath))
            {
                error = $"Missing `{Milestone5BeautifulGraphicsBudgetAssetGenerator.ReportJsonPath}`.";
                return false;
            }

            var markdown = File.ReadAllText(Milestone5BeautifulGraphicsBudgetAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M5 Beautiful Graphics Budget") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone5BeautifulGraphicsBudgetAssetGenerator.LockId))
            {
                error = "Generated M5 markdown report is missing the passing graphics-budget lock.";
                return false;
            }

            var json = JsonUtility.FromJson<Milestone5BeautifulGraphicsBudgetReport>(
                File.ReadAllText(Milestone5BeautifulGraphicsBudgetAssetGenerator.ReportJsonPath));
            if (json == null ||
                !json.passed ||
                json.lockId != Milestone5BeautifulGraphicsBudgetAssetGenerator.LockId ||
                json.profiles == null ||
                json.profiles.Length != 4 ||
                json.liveCaptureScenarios == null ||
                json.liveCaptureScenarios.Length != 5)
            {
                error = "Generated M5 JSON report is missing profile or live-capture coverage.";
                return false;
            }

            return true;
        }
    }
}
