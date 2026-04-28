using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone40Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ExternalHandoffDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/ExternalHandoffReport.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone40AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone40Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone40ExternalHandoffReadinessTests.cs",
            "Docs/Milestone40VerticalSliceRelockExternalHandoff.md",
            Milestone40AssetGenerator.HandoffDefinitionPath,
            Milestone40AssetGenerator.LatestJsonPath,
            Milestone40AssetGenerator.LatestMarkdownPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 40 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M40 file: {file}");
                }
            }

            var definition = AssetDatabase.LoadAssetAtPath<ExternalHandoffDefinition>(Milestone40AssetGenerator.HandoffDefinitionPath);
            ValidateDefinition(definition, failures);
            ValidateReport(definition, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 40 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateDefinition(ExternalHandoffDefinition definition, List<string> failures)
        {
            if (definition == null)
            {
                failures.Add("M40 external handoff definition is missing.");
                return;
            }

            if (!definition.AcceptedEnvironmentBlocks.Contains("windows-development-build"))
            {
                failures.Add("M40 must explicitly document the accepted Windows build-module environment block.");
            }

            foreach (var file in definition.RequiredDocs.Concat(definition.RequiredReports))
            {
                if (!File.Exists(file))
                {
                    failures.Add($"M40 handoff definition points to a missing file: {file}");
                }
            }

            if (definition.ManualHandoffChecklist.Count < 4)
            {
                failures.Add("M40 manual handoff checklist must include concrete external QA steps.");
            }
        }

        private static void ValidateReport(ExternalHandoffDefinition definition, List<string> failures)
        {
            if (definition == null || !File.Exists(definition.LatestJsonPath))
            {
                failures.Add("M40 latest handoff JSON report is missing.");
                return;
            }

            var report = JsonUtility.FromJson<ExternalHandoffReport>(File.ReadAllText(definition.LatestJsonPath));
            if (report == null || string.IsNullOrWhiteSpace(report.reportId))
            {
                failures.Add("M40 latest handoff JSON report could not be decoded.");
                return;
            }

            if (!report.checks.Any(check => check.id == "platform-qa") ||
                !report.checks.Any(check => check.id == "vertical-slice-lock") ||
                !report.checks.Any(check => check.id == "m39-world-framing"))
            {
                failures.Add("M40 handoff report must include platform QA, vertical-slice lock, and M39 world-framing checks.");
            }

            if (!File.Exists(definition.LatestMarkdownPath) || !File.ReadAllText(definition.LatestMarkdownPath).Contains("Hollow M40 External Handoff Readiness"))
            {
                failures.Add("M40 latest handoff Markdown report is missing or malformed.");
            }
        }
    }
}
