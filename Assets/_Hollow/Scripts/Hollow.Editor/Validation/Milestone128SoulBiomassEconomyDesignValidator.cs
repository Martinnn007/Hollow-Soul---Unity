using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone128SoulBiomassEconomyDesignValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 128 Soul + Biomass Economy Design Validation")]
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

            var liveReport = Milestone128SoulBiomassEconomyDesignAssetGenerator.BuildReport();
            if (!liveReport.passed)
            {
                failures.AddRange(liveReport.failures ?? new string[0]);
            }

            ValidateGeneratedReports(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 128 Soul + Biomass Economy Design validation passed.");
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
            foreach (var path in Milestone128SoulBiomassEconomyDesignAssetGenerator.RequiredReportPaths)
            {
                if (!File.Exists(path))
                {
                    failures.Add($"Missing M128 generated report `{path}`.");
                }
            }

            if (File.Exists(Milestone128SoulBiomassEconomyDesignAssetGenerator.ReportJsonPath))
            {
                var jsonReport = JsonUtility.FromJson<Milestone128SoulBiomassEconomyDesignReport>(
                    File.ReadAllText(Milestone128SoulBiomassEconomyDesignAssetGenerator.ReportJsonPath));
                var reportedFailures = jsonReport?.failures ?? new string[0];
                if (jsonReport == null ||
                    jsonReport.lockId != Milestone128SoulBiomassEconomyDesignAssetGenerator.LockId ||
                    !jsonReport.passed ||
                    reportedFailures.Length != 0)
                {
                    failures.Add("M128 JSON report is missing, stale, or not passing.");
                }
                else if (jsonReport.evidencePaths == null ||
                         !jsonReport.evidencePaths.Contains(Milestone128SoulBiomassEconomyDesignAssetGenerator.DocsPath))
                {
                    failures.Add("M128 JSON report does not include the M128 documentation evidence path.");
                }
            }

            if (!File.Exists(Milestone128SoulBiomassEconomyDesignAssetGenerator.ReportMarkdownPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone128SoulBiomassEconomyDesignAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M128 Soul + Biomass Economy Design Pass Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains("design artifact only") ||
                !markdown.Contains("Canonical future run label: `Unbanked Souls`") ||
                !markdown.Contains("Biomass runtime: deferred beyond M128") ||
                !markdown.Contains("M129 Ship-Soul Loop Greybox"))
            {
                failures.Add("M128 Markdown report is missing required lock evidence text.");
            }
        }
    }
}
