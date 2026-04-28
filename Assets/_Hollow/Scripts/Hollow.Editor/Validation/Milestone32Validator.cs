using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Editor.Build;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone32Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone32AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/MilestoneValidationExitPolicy.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone32Validator.cs",
            "Docs/Milestone32FullQaGateRebaseline.md",
            Milestone32AssetGenerator.BaselineReportPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 32 Validation")]
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
                    failures.Add($"Missing M32 baseline file: {file}");
                }
            }

            ValidateTestFrameworkSetup(failures);
            ValidateLatestQaReport(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 32 validation passed.");
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

        private static void ValidateTestFrameworkSetup(List<string> failures)
        {
            var manifestPath = "Packages/manifest.json";
            if (!File.Exists(manifestPath) || !File.ReadAllText(manifestPath).Contains("\"com.unity.test-framework\""))
            {
                failures.Add("M32 requires com.unity.test-framework in Packages/manifest.json.");
            }

            ValidateTestAsmdef("Assets/_Hollow/Tests/EditMode/Hollow.Tests.EditMode.asmdef", "Hollow.Tests.EditMode", failures);
            ValidateTestAsmdef("Assets/_Hollow/Tests/PlayMode/Hollow.Tests.PlayMode.asmdef", "Hollow.Tests.PlayMode", failures);
            var editorAsmdef = File.Exists("Assets/_Hollow/Scripts/Hollow.Editor/Hollow.Editor.asmdef")
                ? File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Editor/Hollow.Editor.asmdef")
                : string.Empty;
            if (!editorAsmdef.Contains("UnityEditor.TestRunner") || !editorAsmdef.Contains("UnityEngine.TestRunner"))
            {
                failures.Add("M32 requires Hollow.Editor.asmdef to reference UnityEditor.TestRunner and UnityEngine.TestRunner.");
            }
        }

        private static void ValidateTestAsmdef(string path, string assemblyName, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing test assembly definition: {path}");
                return;
            }

            var text = File.ReadAllText(path);
            if (!text.Contains($"\"name\": \"{assemblyName}\"") || !text.Contains("TestAssemblies"))
            {
                failures.Add($"{assemblyName} asmdef must be named correctly and include TestAssemblies.");
            }
        }

        private static void ValidateLatestQaReport(List<string> failures)
        {
            var profile = PlatformBuildQaRunner.LoadProfileOrThrow();
            var jsonPath = Path.Combine(profile.ReportRoot, profile.LatestQaJsonFileName);
            if (!File.Exists(jsonPath))
            {
                failures.Add($"M32 latest QA report is missing: {jsonPath}");
                return;
            }

            var report = JsonUtility.FromJson<PlatformBuildQaReport>(File.ReadAllText(jsonPath));
            if (report == null || string.IsNullOrWhiteSpace(report.reportId))
            {
                failures.Add("M32 latest QA report could not be decoded.");
                return;
            }

            RequireTarget(report, "editmode-tests", failures);
            RequireTarget(report, "playmode-smoke-tests", failures);
            if (report.targets.Any(target => target.result == PlatformBuildQaResult.NotRun))
            {
                failures.Add("M32 QA report must not contain NotRun targets after the rebaseline gate.");
            }

            if (!File.Exists(Path.Combine(profile.ReportRoot, "m24-editmode-results.xml")))
            {
                failures.Add("M32 requires the M24/M32 EditMode XML result file.");
            }

            if (!File.Exists(Path.Combine(profile.ReportRoot, "m24-playmode-smoke-editor-probe.md")))
            {
                failures.Add("M32 requires the editor-side platform scene smoke report.");
            }
        }

        private static void RequireTarget(PlatformBuildQaReport report, string targetId, List<string> failures)
        {
            var target = report.targets.FirstOrDefault(candidate => candidate.id == targetId);
            if (target == null)
            {
                failures.Add($"M32 QA report missing target: {targetId}.");
                return;
            }

            if (target.result != PlatformBuildQaResult.Passed)
            {
                failures.Add($"M32 QA target {targetId} must pass, but was {target.result}.");
            }
        }
    }
}
