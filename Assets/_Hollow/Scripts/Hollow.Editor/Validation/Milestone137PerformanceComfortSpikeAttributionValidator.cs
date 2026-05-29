using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone137PerformanceComfortSpikeAttributionValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 137 Performance Comfort + Spike Attribution Validation")]
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
            var report = Milestone137PerformanceComfortSpikeAttributionAssetGenerator.BuildReport();
            var generatedReportsValid = ValidateGeneratedReports(out var reportError);
            var passed = report.passed && generatedReportsValid;
            if (passed)
            {
                Debug.Log("Milestone 137 Performance Comfort + Spike Attribution validation passed.");
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

            Debug.LogError($"Milestone 137 Performance Comfort + Spike Attribution validation failed.\n{failures}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static bool ValidateGeneratedReports(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.DocsPath))
            {
                error = $"Missing `{Milestone137PerformanceComfortSpikeAttributionAssetGenerator.DocsPath}`.";
                return false;
            }

            if (!File.Exists(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportMarkdownPath))
            {
                error = $"Missing `{Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportMarkdownPath}`.";
                return false;
            }

            if (!File.Exists(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportJsonPath))
            {
                error = $"Missing `{Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportJsonPath}`.";
                return false;
            }

            if (!File.Exists(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportPdfPath))
            {
                error = $"Missing `{Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportPdfPath}`.";
                return false;
            }

            var docs = File.ReadAllText(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.DocsPath);
            if (!docs.Contains("# M137: Performance Comfort + Spike Attribution") ||
                !docs.Contains("Windows standard target frame rate") ||
                !docs.Contains("Profiler traces stay optional"))
            {
                error = "Generated M137 docs are missing the locked performance policy.";
                return false;
            }

            var markdown = File.ReadAllText(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M137 Performance Comfort + Spike Attribution") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.LockId) ||
                !markdown.Contains("Post-Fix Recapture Checklist"))
            {
                error = "Generated M137 markdown report is not passing or has the wrong lock id.";
                return false;
            }

            var json = JsonUtility.FromJson<Milestone137PerformanceComfortSpikeAttributionReport>(
                File.ReadAllText(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportJsonPath));
            if (json == null ||
                !json.passed ||
                json.lockId != Milestone137PerformanceComfortSpikeAttributionAssetGenerator.LockId ||
                json.windowsTargetFrameRate != Hollow.Core.Diagnostics.M137PerformanceComfortPolicy.WindowsComfortTargetFrameRate ||
                json.validBaselineCaptureCount < 6 ||
                json.recaptureChecklist == null ||
                json.recaptureChecklist.Length != 6)
            {
                error = "Generated M137 JSON report is not passing or has the wrong policy/capture coverage.";
                return false;
            }

            var pdfBytes = File.ReadAllBytes(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportPdfPath);
            if (pdfBytes.Length < 4096 ||
                pdfBytes[0] != '%' ||
                pdfBytes[1] != 'P' ||
                pdfBytes[2] != 'D' ||
                pdfBytes[3] != 'F')
            {
                error = "Generated M137 PDF is missing or too small to be a valid performance handoff.";
                return false;
            }

            return true;
        }
    }
}
