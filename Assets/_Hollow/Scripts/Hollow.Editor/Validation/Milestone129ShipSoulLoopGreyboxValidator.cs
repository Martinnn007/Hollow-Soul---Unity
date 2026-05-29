using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone129ShipSoulLoopGreyboxValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 129 Ship-Soul Loop Greybox Validation")]
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

            var liveReport = Milestone129ShipSoulLoopGreyboxAssetGenerator.BuildReport();
            if (!liveReport.passed)
            {
                failures.AddRange(liveReport.failures ?? new string[0]);
            }

            ValidateGeneratedReports(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 129 Ship-Soul Loop Greybox validation passed.");
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
            foreach (var path in Milestone129ShipSoulLoopGreyboxAssetGenerator.RequiredReportPaths)
            {
                if (!File.Exists(path))
                {
                    failures.Add($"Missing M129 generated report `{path}`.");
                }
            }

            if (File.Exists(Milestone129ShipSoulLoopGreyboxAssetGenerator.ReportJsonPath))
            {
                var jsonReport = JsonUtility.FromJson<Milestone129ShipSoulLoopGreyboxReport>(
                    File.ReadAllText(Milestone129ShipSoulLoopGreyboxAssetGenerator.ReportJsonPath));
                var reportedFailures = jsonReport?.failures ?? new string[0];
                if (jsonReport == null ||
                    jsonReport.lockId != Milestone129ShipSoulLoopGreyboxAssetGenerator.LockId ||
                    !jsonReport.passed ||
                    reportedFailures.Length != 0)
                {
                    failures.Add("M129 JSON report is missing, stale, or not passing.");
                }
                else if (jsonReport.evidencePaths == null ||
                         !jsonReport.evidencePaths.Contains(Milestone129ShipSoulLoopGreyboxAssetGenerator.DocsPath))
                {
                    failures.Add("M129 JSON report does not include the M129 documentation evidence path.");
                }
            }

            if (!File.Exists(Milestone129ShipSoulLoopGreyboxAssetGenerator.ReportMarkdownPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone129ShipSoulLoopGreyboxAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M129 Ship-Soul Loop Greybox Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone129ShipSoulLoopGreyboxAssetGenerator.LockId) ||
                !markdown.Contains("Portal Engine launches normal runs") ||
                !markdown.Contains("world-loop souls bank only on final `Return to Ship`") ||
                !markdown.Contains("ship/module surfaces use `Banked Souls`"))
            {
                failures.Add("M129 Markdown report is missing required lock evidence text.");
            }
        }
    }
}
