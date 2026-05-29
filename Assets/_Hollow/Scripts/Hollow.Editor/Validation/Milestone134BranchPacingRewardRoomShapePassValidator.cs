using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone134BranchPacingRewardRoomShapePassValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 134 Branch Pacing Reward Room Shape Pass Validation")]
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
            var report = Milestone134BranchPacingRewardRoomShapePassAssetGenerator.BuildReport();
            var generatedReportsValid = ValidateGeneratedReports(out var reportError);
            var passed = report.passed && generatedReportsValid;
            if (passed)
            {
                Debug.Log("Milestone 134 Branch Pacing Reward Room Shape Pass validation passed.");
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

            Debug.LogError($"Milestone 134 Branch Pacing Reward Room Shape Pass validation failed.\n{failures}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static bool ValidateGeneratedReports(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportMarkdownPath))
            {
                error = $"Missing `{Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportMarkdownPath}`.";
                return false;
            }

            if (!File.Exists(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportJsonPath))
            {
                error = $"Missing `{Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportJsonPath}`.";
                return false;
            }

            var markdown = File.ReadAllText(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M134 Branch Pacing + Reward Room Shape Pass Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.LockId))
            {
                error = "Generated M134 markdown report is not passing or has the wrong lock id.";
                return false;
            }

            var json = JsonUtility.FromJson<Milestone134BranchPacingRewardRoomShapePassReport>(
                File.ReadAllText(Milestone134BranchPacingRewardRoomShapePassAssetGenerator.ReportJsonPath));
            if (json == null ||
                !json.passed ||
                json.lockId != Milestone134BranchPacingRewardRoomShapePassAssetGenerator.LockId)
            {
                error = "Generated M134 JSON report is not passing or has the wrong lock id.";
                return false;
            }

            return true;
        }
    }
}
