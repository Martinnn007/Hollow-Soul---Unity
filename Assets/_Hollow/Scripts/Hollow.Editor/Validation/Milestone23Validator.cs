using System.Collections.Generic;
using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone23Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/PresentationPrefabRole.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/PresentationPrefabBinding.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/PresentationPrefabResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/PresentationVisualMarker.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone23AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/ArtPassContentValidator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone23Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone23ArtContentReplacementTests.cs",
            "Docs/Milestone23ArtContentReplacementPipeline.md",
            Milestone23AssetGenerator.ArtPassPalettePath
        };

        [MenuItem("Hollow/Validation/Run Milestone 23 Validation")]
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
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M23 file: {file}");
                }
            }

            var report = ArtPassContentValidator.ValidateAll();
            failures.AddRange(report.Failures);
            foreach (var warning in report.Warnings)
            {
                Debug.LogWarning(warning);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 23 validation passed.");
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
