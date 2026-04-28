using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone33Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Combat/CombatFeelTuning.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/RoomLocalCollision.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/GameplayCameraFollowController.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone33CombatFeelPhysicsCameraTests.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone33AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone33Validator.cs",
            "Docs/Milestone33CombatFeelPhysicsCameraPolish.md",
            Milestone33AssetGenerator.BaselineReportPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 33 Validation")]
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
                    failures.Add($"Missing M33 file: {file}");
                }
            }

            if (CombatFeelTuning.MovementSubstepMeters <= 0f || CombatFeelTuning.MovementSubstepMeters > 0.25f)
            {
                failures.Add("M33 movement substep budget must be positive and no larger than 0.25m.");
            }

            if (CombatFeelTuning.ProjectileSubstepMeters <= 0f || CombatFeelTuning.ProjectileSubstepMeters > 0.2f)
            {
                failures.Add("M33 projectile substep budget must be positive and no larger than 0.2m.");
            }

            if (GameplayCameraFollowController.DefaultSnapDistanceMeters < 4f)
            {
                failures.Add("M33 camera snap distance must allow normal movement smoothing while still snapping large traversal jumps.");
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 33 validation passed.");
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
