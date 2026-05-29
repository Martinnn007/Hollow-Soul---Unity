using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone135FullRunCombatReadinessLockValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 135 Full Run Combat Readiness Lock Validation")]
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
            var report = Milestone135FullRunCombatReadinessLockAssetGenerator.BuildReport();
            var generatedReportsValid = ValidateGeneratedReports(out var reportError);
            var checklistValid = ValidateQaChecklist(out var checklistError);
            var passed = report.passed && generatedReportsValid && checklistValid;
            if (passed)
            {
                Debug.Log("Milestone 135 Full-Run Combat Readiness Lock validation passed.");
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

            if (!string.IsNullOrWhiteSpace(checklistError))
            {
                failures = string.IsNullOrWhiteSpace(failures) ? checklistError : $"{failures}\n{checklistError}";
            }

            Debug.LogError($"Milestone 135 Full-Run Combat Readiness Lock validation failed.\n{failures}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static bool ValidateGeneratedReports(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone135FullRunCombatReadinessLockAssetGenerator.ReportMarkdownPath))
            {
                error = $"Missing `{Milestone135FullRunCombatReadinessLockAssetGenerator.ReportMarkdownPath}`.";
                return false;
            }

            if (!File.Exists(Milestone135FullRunCombatReadinessLockAssetGenerator.ReportJsonPath))
            {
                error = $"Missing `{Milestone135FullRunCombatReadinessLockAssetGenerator.ReportJsonPath}`.";
                return false;
            }

            var markdown = File.ReadAllText(Milestone135FullRunCombatReadinessLockAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M135 Full-Run Combat Readiness Lock Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone135FullRunCombatReadinessLockAssetGenerator.LockId))
            {
                error = "Generated M135 markdown report is not passing or has the wrong lock id.";
                return false;
            }

            var json = JsonUtility.FromJson<Milestone135FullRunCombatReadinessLockReport>(
                File.ReadAllText(Milestone135FullRunCombatReadinessLockAssetGenerator.ReportJsonPath));
            if (json == null ||
                !json.passed ||
                json.lockId != Milestone135FullRunCombatReadinessLockAssetGenerator.LockId)
            {
                error = "Generated M135 JSON report is not passing or has the wrong lock id.";
                return false;
            }

            return true;
        }

        private static bool ValidateQaChecklist(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone135FullRunCombatReadinessLockAssetGenerator.QaChecklistPath))
            {
                error = $"Missing `{Milestone135FullRunCombatReadinessLockAssetGenerator.QaChecklistPath}`.";
                return false;
            }

            var checklist = File.ReadAllText(Milestone135FullRunCombatReadinessLockAssetGenerator.QaChecklistPath);
            if (!checklist.Contains("Full Run Flow") ||
                !checklist.Contains("Boss Anchors") ||
                !checklist.Contains("Roster Smoke") ||
                !checklist.Contains("Room Combat Spawn Readiness") ||
                !checklist.Contains("Return to Ship"))
            {
                error = "Generated M135 playable QA checklist is missing required sections.";
                return false;
            }

            return true;
        }
    }
}
