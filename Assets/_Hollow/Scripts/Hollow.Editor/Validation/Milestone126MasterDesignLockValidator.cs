using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone126MasterDesignLockValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 126 Master Design Lock Validation")]
        public static bool Validate()
        {
            return Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        public static bool Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();

            var liveReport = Milestone126MasterDesignLockAssetGenerator.BuildReport();
            if (!liveReport.passed)
            {
                failures.AddRange(liveReport.failures ?? new string[0]);
            }

            ValidateGeneratedReports(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 126 Master Design Lock validation passed.");
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
            foreach (var path in Milestone126MasterDesignLockAssetGenerator.RequiredReportPaths)
            {
                if (!File.Exists(path))
                {
                    failures.Add($"Missing M126 generated report `{path}`.");
                }
            }

            if (File.Exists(Milestone126MasterDesignLockAssetGenerator.ReportJsonPath))
            {
                var jsonReport = JsonUtility.FromJson<Milestone126MasterDesignLockReport>(
                    File.ReadAllText(Milestone126MasterDesignLockAssetGenerator.ReportJsonPath));
                var reportedFailures = jsonReport?.failures ?? new string[0];
                if (jsonReport == null ||
                    jsonReport.lockId != Milestone126MasterDesignLockAssetGenerator.LockId ||
                    !jsonReport.passed ||
                    reportedFailures.Length != 0)
                {
                    failures.Add("M126 JSON report is missing, stale, or not passing.");
                }
                else if (jsonReport.evidencePaths == null ||
                         !jsonReport.evidencePaths.Contains(Milestone126MasterDesignLockAssetGenerator.PolishPdfPath))
                {
                    failures.Add("M126 JSON report does not include the Polish PDF evidence path.");
                }
            }

            if (!File.Exists(Milestone126MasterDesignLockAssetGenerator.ReportMarkdownPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone126MasterDesignLockAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M126 Master Design Lock Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone126MasterDesignLockAssetGenerator.PolishPdfPath) ||
                !markdown.Contains("no gameplay, prefab, scene, balance, or content-system changes"))
            {
                failures.Add("M126 Markdown report is missing required lock evidence text.");
            }
        }
    }
}
