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
    public static class Milestone41Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/CurrentMilestoneAuditDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone41AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone41Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone41CurrentMilestoneAuditTests.cs",
            "Docs/Milestone41CurrentMilestoneAudit.md",
            Milestone41AssetGenerator.AuditDefinitionPath,
            Milestone41AssetGenerator.LatestJsonPath,
            Milestone41AssetGenerator.LatestMarkdownPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 41 Validation")]
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
                    failures.Add($"Missing M41 file: {file}");
                }
            }

            var definition = AssetDatabase.LoadAssetAtPath<CurrentMilestoneAuditDefinition>(Milestone41AssetGenerator.AuditDefinitionPath);
            ValidateDefinition(definition, failures);
            ValidateReport(definition, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 41 validation passed.");
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

        private static void ValidateDefinition(CurrentMilestoneAuditDefinition definition, List<string> failures)
        {
            if (definition == null)
            {
                failures.Add("M41 current milestone audit definition is missing.");
                return;
            }

            foreach (var validator in Milestone41AssetGenerator.CurrentValidationTypes)
            {
                if (!definition.ValidationTypes.Contains(validator))
                {
                    failures.Add($"M41 audit definition is missing validator {validator}.");
                }
            }

            foreach (var report in definition.RequiredEvidenceReports)
            {
                if (!File.Exists(report))
                {
                    failures.Add($"M41 required evidence report is missing: {report}");
                }
            }
        }

        private static void ValidateReport(CurrentMilestoneAuditDefinition definition, List<string> failures)
        {
            if (definition == null || !File.Exists(definition.LatestJsonPath))
            {
                failures.Add("M41 latest current milestone audit JSON report is missing.");
                return;
            }

            var report = JsonUtility.FromJson<PrototypeAuditReport>(File.ReadAllText(definition.LatestJsonPath));
            if (report == null || string.IsNullOrWhiteSpace(report.auditId))
            {
                failures.Add("M41 current milestone audit JSON report could not be decoded.");
                return;
            }

            if (!report.Passed)
            {
                failures.Add($"M41 current milestone audit is not passing: {report.passedChecks}/{report.totalChecks}.");
            }

            if (report.totalChecks != Milestone41AssetGenerator.CurrentValidationTypes.Length)
            {
                failures.Add($"M41 current milestone audit expected {Milestone41AssetGenerator.CurrentValidationTypes.Length} checks but found {report.totalChecks}.");
            }

            foreach (var validator in Milestone41AssetGenerator.CurrentValidationTypes)
            {
                var shortName = validator.Split('.').Last();
                if (!report.entries.Any(entry => entry.id == shortName && entry.passed))
                {
                    failures.Add($"M41 current milestone audit did not pass {shortName}.");
                }
            }

            if (!File.Exists(definition.LatestMarkdownPath) || !File.ReadAllText(definition.LatestMarkdownPath).Contains("Hollow Prototype Audit"))
            {
                failures.Add("M41 latest current milestone audit Markdown report is missing or malformed.");
            }
        }
    }
}
