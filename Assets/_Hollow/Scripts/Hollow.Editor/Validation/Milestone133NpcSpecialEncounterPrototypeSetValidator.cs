using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone133NpcSpecialEncounterPrototypeSetValidator
    {
        [MenuItem("Hollow/Validation/Run Milestone 133 NPC Special Encounter Prototype Set Validation")]
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
            var report = Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.BuildReport();
            var generatedReportsValid = ValidateGeneratedReports(out var reportError);
            var passed = report.passed && generatedReportsValid;
            if (passed)
            {
                Debug.Log("Milestone 133 NPC Special Encounter Prototype Set validation passed.");
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

            Debug.LogError($"Milestone 133 NPC Special Encounter Prototype Set validation failed.\n{failures}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        private static bool ValidateGeneratedReports(out string error)
        {
            error = string.Empty;
            if (!File.Exists(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportMarkdownPath))
            {
                error = $"Missing `{Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportMarkdownPath}`.";
                return false;
            }

            if (!File.Exists(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportJsonPath))
            {
                error = $"Missing `{Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportJsonPath}`.";
                return false;
            }

            var markdown = File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportMarkdownPath);
            if (!markdown.Contains("# M133 NPC/Special Encounter Prototype Set Report") ||
                !markdown.Contains("- Result: PASSED") ||
                !markdown.Contains(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.LockId))
            {
                error = "Generated M133 markdown report is not passing or has the wrong lock id.";
                return false;
            }

            var json = JsonUtility.FromJson<Milestone133NpcSpecialEncounterPrototypeSetReport>(
                File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportJsonPath));
            if (json == null ||
                !json.passed ||
                json.lockId != Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.LockId)
            {
                error = "Generated M133 JSON report is not passing or has the wrong lock id.";
                return false;
            }

            return true;
        }
    }
}
