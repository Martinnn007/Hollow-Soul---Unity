using System.Collections.Generic;
using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone9Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/MaterialRole.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/MaterialPaletteDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/VfxCueDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/AudioCueDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/PresentationContentCatalog.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/MaterialResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/VfxPresenter.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/AudioPresenter.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/AddressableAssetLoader.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone9AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/ContentImportValidator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone9Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone9ContentPipelineTests.cs",
            "Docs/Milestone9ContentPipeline.md",
            Milestone9AssetGenerator.PalettePath,
            Milestone9AssetGenerator.CatalogPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 9 Validation")]
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
                    failures.Add($"Missing M9 file: {file}");
                }
            }

            var report = ContentImportValidator.ValidateAll();
            failures.AddRange(report.Failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 9 validation passed.");
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
