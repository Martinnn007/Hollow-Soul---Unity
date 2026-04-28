using System.Collections.Generic;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone12Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/BuildAutomationProfileDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/PrototypeAuditReport.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/PrototypeAuditRunner.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/BuildArtifactManifest.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/BuildManifestWriter.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/HollowBuildAutomation.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone12AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/BuildAutomationValidationHarness.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone12Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone12BuildAutomationTests.cs",
            "Assets/_Hollow/Tests/PlayMode/Milestone12BootSmokePlayModeTests.cs",
            "Docs/Milestone12BuildAutomation.md",
            Milestone12AssetGenerator.BuildAutomationProfilePath
        };

        [MenuItem("Hollow/Validation/Run Milestone 12 Validation")]
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
                    failures.Add($"Missing M12 file: {file}");
                }
            }

            var profile = AssetDatabase.LoadAssetAtPath<BuildAutomationProfileDefinition>(Milestone12AssetGenerator.BuildAutomationProfilePath);
            var report = BuildAutomationValidationHarness.ValidateAll(profile, runAudit: true);
            foreach (var failure in report.Failures)
            {
                failures.Add(failure);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 12 validation passed.");
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
