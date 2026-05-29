using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone136EditorLaptopPerformancePowerInvestigationValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 136 Editor Laptop Performance + Power Investigation Validation")]
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
            var report = Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.BuildReport();
            var generatedReportsValid = ValidateGeneratedReports(out var reportError);
            var passed = report.passed && generatedReportsValid;
            if (passed)
            {
                Debug.Log("Milestone 136 Editor Laptop Performance + Power Investigation validation passed.");
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

            Debug.LogError($"Milestone 136 Editor Laptop Performance + Power Investigation validation failed.\n{failures}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static bool ValidateGeneratedReports(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportMarkdownPath))
            {
                error = $"Missing `{Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportMarkdownPath}`.";
                return false;
            }

            if (!File.Exists(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportJsonPath))
            {
                error = $"Missing `{Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportJsonPath}`.";
                return false;
            }

            if (!File.Exists(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportPdfPath))
            {
                error = $"Missing `{Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportPdfPath}`.";
                return false;
            }

            var markdown = File.ReadAllText(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M136 Editor Laptop Performance + Power Investigation Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.LockId) ||
                !markdown.Contains("Ranked Suspects And Suggested Solutions"))
            {
                error = "Generated M136 markdown report is not passing or has the wrong lock id.";
                return false;
            }

            var json = JsonUtility.FromJson<Milestone136EditorLaptopPerformancePowerInvestigationReport>(
                File.ReadAllText(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportJsonPath));
            if (json == null ||
                !json.passed ||
                json.lockId != Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.LockId ||
                json.scenarios == null ||
                json.scenarios.Length != 6)
            {
                error = "Generated M136 JSON report is not passing or has the wrong lock id/scenario coverage.";
                return false;
            }

            var pdfBytes = File.ReadAllBytes(Milestone136EditorLaptopPerformancePowerInvestigationAssetGenerator.ReportPdfPath);
            if (pdfBytes.Length < 4096 ||
                pdfBytes[0] != '%' ||
                pdfBytes[1] != 'P' ||
                pdfBytes[2] != 'D' ||
                pdfBytes[3] != 'F')
            {
                error = "Generated M136 PDF is missing or too small to be a valid investigation handoff.";
                return false;
            }

            return true;
        }
    }
}
