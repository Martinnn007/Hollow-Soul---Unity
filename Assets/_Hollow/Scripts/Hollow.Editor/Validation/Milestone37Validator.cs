using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone37Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Combat/EnemyReadabilityState.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/CombatReadabilityPresenter.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone37EnemyBossReadabilityTests.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone37AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone37Validator.cs",
            "Docs/Milestone37EnemyBossBehaviorReadability.md",
            Milestone37AssetGenerator.BaselineReportPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 37 Validation")]
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
                    failures.Add($"Missing M37 file: {file}");
                }
            }

            if (EnemyRuntimeController.ChargeWindupSeconds <= 0f ||
                EnemyRuntimeController.RangedWindupSeconds <= 0f ||
                EnemyRuntimeController.BossBurstWindupSeconds <= 0f)
            {
                failures.Add("M37 enemy windup timings must be positive.");
            }

            ValidateMaterialRole(MaterialRole.CombatTelegraphSafe, failures);
            ValidateMaterialRole(MaterialRole.CombatTelegraphWarning, failures);
            ValidateMaterialRole(MaterialRole.CombatTelegraphDanger, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 37 validation passed.");
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

        private static void ValidateMaterialRole(MaterialRole role, List<string> failures)
        {
            if (MaterialResolver.Resolve(role) == null)
            {
                failures.Add($"M37 material resolver must resolve {role}.");
            }

            var prototypePalette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone9AssetGenerator.PalettePath);
            if (prototypePalette != null && (!prototypePalette.TryResolve(role, out var prototypeMaterial) || prototypeMaterial == null))
            {
                failures.Add($"M37 prototype palette must resolve {role}.");
            }

            var artPassPalette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone23AssetGenerator.ArtPassPalettePath);
            if (artPassPalette != null && (!artPassPalette.TryResolve(role, out var artPassMaterial) || artPassMaterial == null))
            {
                failures.Add($"M37 ArtPass palette must resolve {role}.");
            }
        }
    }
}
