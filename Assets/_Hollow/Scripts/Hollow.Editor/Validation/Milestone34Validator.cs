using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone34Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Combat/IIncomingDamageModifier.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/PlayerDefenseController.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone34ShieldDefenseArmorBehaviorTests.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone34AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone34Validator.cs",
            "Docs/Milestone34ShieldDefenseArmorBehavior.md",
            Milestone34AssetGenerator.BaselineReportPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 34 Validation")]
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
                    failures.Add($"Missing M34 file: {file}");
                }
            }

            if (PlayerDefenseController.DefensePerPassiveDamageReduction != 2)
            {
                failures.Add("M34 passive defense should reduce incoming damage by 1 per 2 defense.");
            }

            if (PlayerDefenseController.GuardDamageReduction < 1)
            {
                failures.Add("M34 guard must reduce incoming damage by at least 1.");
            }

            if (PlayerDefenseController.GuardBlockStaminaCost <= 0f || PlayerDefenseController.GuardDrainStaminaPerSecond <= 0f)
            {
                failures.Add("M34 guard must have positive stamina block and hold costs.");
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 34 validation passed.");
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
