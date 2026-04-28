using System.Collections.Generic;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone11Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/PrototypeLockStatus.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/PrototypeLockChecklistDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/PerformanceBudgetDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/BuildHandoffDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone11AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/PrototypeLockValidationHarness.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone11Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone11PrototypeLockTests.cs",
            "Docs/Milestone11PrototypeLock.md",
            Milestone11AssetGenerator.ChecklistPath,
            Milestone11AssetGenerator.PerformanceBudgetPath,
            Milestone11AssetGenerator.BuildHandoffPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 11 Validation")]
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
                    failures.Add($"Missing M11 file: {file}");
                }
            }

            var checklist = AssetDatabase.LoadAssetAtPath<PrototypeLockChecklistDefinition>(Milestone11AssetGenerator.ChecklistPath);
            var budget = AssetDatabase.LoadAssetAtPath<PerformanceBudgetDefinition>(Milestone11AssetGenerator.PerformanceBudgetPath);
            var handoff = AssetDatabase.LoadAssetAtPath<BuildHandoffDefinition>(Milestone11AssetGenerator.BuildHandoffPath);
            var report = PrototypeLockValidationHarness.ValidateAll(checklist, budget, handoff);
            foreach (var failure in report.Failures)
            {
                failures.Add(failure);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 11 validation passed.");
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
    }
}
