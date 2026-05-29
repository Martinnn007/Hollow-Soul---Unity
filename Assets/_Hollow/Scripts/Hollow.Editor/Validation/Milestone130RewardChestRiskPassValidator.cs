using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone130RewardChestRiskPassValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 130 Reward Chest Risk Pass Validation")]
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
            var report = Milestone130RewardChestRiskPassAssetGenerator.BuildReport();
            var generatedReportsValid = ValidateGeneratedReports(out var reportError);
            var passed = report.passed && generatedReportsValid;
            if (passed)
            {
                Debug.Log("Milestone 130 Reward + Chest Risk Pass validation passed.");
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

            Debug.LogError($"Milestone 130 Reward + Chest Risk Pass validation failed.\n{failures}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static bool ValidateGeneratedReports(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone130RewardChestRiskPassAssetGenerator.ReportMarkdownPath))
            {
                error = $"Missing `{Milestone130RewardChestRiskPassAssetGenerator.ReportMarkdownPath}`.";
                return false;
            }

            if (!File.Exists(Milestone130RewardChestRiskPassAssetGenerator.ReportJsonPath))
            {
                error = $"Missing `{Milestone130RewardChestRiskPassAssetGenerator.ReportJsonPath}`.";
                return false;
            }

            var markdown = File.ReadAllText(Milestone130RewardChestRiskPassAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M130 Reward + Chest Risk Pass Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone130RewardChestRiskPassAssetGenerator.LockId))
            {
                error = "Generated M130 markdown report is not passing or has the wrong lock id.";
                return false;
            }

            var json = JsonUtility.FromJson<Milestone130RewardChestRiskPassReport>(
                File.ReadAllText(Milestone130RewardChestRiskPassAssetGenerator.ReportJsonPath));
            if (json == null ||
                !json.passed ||
                json.lockId != Milestone130RewardChestRiskPassAssetGenerator.LockId)
            {
                error = "Generated M130 JSON report is not passing or has the wrong lock id.";
                return false;
            }

            return true;
        }
    }
}
