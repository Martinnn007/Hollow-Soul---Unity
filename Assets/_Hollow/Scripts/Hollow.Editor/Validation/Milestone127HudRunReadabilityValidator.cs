using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone127HudRunReadabilityValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 127 HUD + Run Readability Validation")]
        public static void ValidateFromMenu()
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
            var failures = new List<string>();

            var liveReport = Milestone127HudRunReadabilityAssetGenerator.BuildReport();
            if (!liveReport.passed)
            {
                failures.AddRange(liveReport.failures ?? new string[0]);
            }

            ValidateGeneratedReports(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 127 HUD + Run Readability validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static void ValidateGeneratedReports(List<string> failures)
        {
            foreach (var path in Milestone127HudRunReadabilityAssetGenerator.RequiredReportPaths)
            {
                if (!File.Exists(path))
                {
                    failures.Add($"Missing M127 generated report `{path}`.");
                }
            }

            if (File.Exists(Milestone127HudRunReadabilityAssetGenerator.ReportJsonPath))
            {
                var jsonReport = JsonUtility.FromJson<Milestone127HudRunReadabilityReport>(
                    File.ReadAllText(Milestone127HudRunReadabilityAssetGenerator.ReportJsonPath));
                var reportedFailures = jsonReport?.failures ?? new string[0];
                if (jsonReport == null ||
                    jsonReport.lockId != Milestone127HudRunReadabilityAssetGenerator.LockId ||
                    !jsonReport.passed ||
                    reportedFailures.Length != 0)
                {
                    failures.Add("M127 JSON report is missing, stale, or not passing.");
                }
                else if (jsonReport.evidencePaths == null ||
                         !jsonReport.evidencePaths.Contains(Milestone127HudRunReadabilityAssetGenerator.DocsPath))
                {
                    failures.Add("M127 JSON report does not include the M127 documentation evidence path.");
                }
            }

            if (!File.Exists(Milestone127HudRunReadabilityAssetGenerator.ReportMarkdownPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone127HudRunReadabilityAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M127 HUD + Run Readability Pass Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains("Enlarged minimap/map interaction: deferred beyond M127") ||
                !markdown.Contains("M128 Soul + Biomass Economy Design Pass"))
            {
                failures.Add("M127 Markdown report is missing required lock evidence text.");
            }
        }
    }
}
