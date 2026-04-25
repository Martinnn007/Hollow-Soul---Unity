using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone0Validator
    {
        private static readonly string[] RequiredDirectories =
        {
            "Assets/_Hollow",
            "Assets/_Hollow/Scripts/Hollow.Core",
            "Assets/_Hollow/Scripts/Hollow.Data",
            "Assets/_Hollow/Scripts/Hollow.Platform",
            "Assets/_Hollow/Scripts/Hollow.Input",
            "Assets/_Hollow/Scripts/Hollow.Diagnostics",
            "Assets/_Hollow/Scripts/Hollow.Editor",
            "Assets/_Hollow/Tests/EditMode"
        };

        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Core/Hollow.Core.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Data/Hollow.Data.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Platform/Hollow.Platform.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Input/Hollow.Input.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Diagnostics/Hollow.Diagnostics.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Editor/Hollow.Editor.asmdef",
            "Assets/_Hollow/Tests/EditMode/Hollow.Tests.EditMode.asmdef",
            "Packages/manifest.json"
        };

        private static readonly string[] RequiredPackages =
        {
            "com.unity.addressables",
            "com.unity.inputsystem",
            "com.unity.render-pipelines.universal",
            "com.unity.test-framework"
        };

        [MenuItem("Hollow/Validation/Run Milestone 0 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: Application.isBatchMode);
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();

            foreach (var directory in RequiredDirectories)
            {
                if (!Directory.Exists(directory))
                {
                    failures.Add($"Missing directory: {directory}");
                }
            }

            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing file: {file}");
                }
            }

            var manifestPath = "Packages/manifest.json";
            var manifest = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : string.Empty;
            foreach (var packageName in RequiredPackages)
            {
                if (!manifest.Contains(packageName))
                {
                    failures.Add($"Missing package dependency: {packageName}");
                }
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 0 validation passed.");
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
