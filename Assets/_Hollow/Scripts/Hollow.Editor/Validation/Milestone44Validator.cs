using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone44Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ShieldGuardProfileDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/DamageThreatKind.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/ShieldGuardResult.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/ShieldGuardVisualController.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone44AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone44Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone44ShieldArmorBehaviorV2Tests.cs",
            "Docs/Milestone44ShieldArmorBehaviorV2.md",
            Milestone44AssetGenerator.ProfilePath,
            Milestone44AssetGenerator.ReportPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 44 Validation")]
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
                    failures.Add($"Missing M44 file: {file}");
                }
            }

            ValidateProfile(failures);
            ValidateRuntimeConstants(failures);
            ValidateEnums(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 44 validation passed.");
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

        private static void ValidateProfile(List<string> failures)
        {
            var profile = AssetDatabase.LoadAssetAtPath<ShieldGuardProfileDefinition>(Milestone44AssetGenerator.ProfilePath);
            if (profile == null)
            {
                failures.Add("M44 shield guard profile asset is missing or invalid.");
                return;
            }

            if (profile.ParryWindowSeconds < 0.29f || profile.ParryWindowSeconds > 0.31f)
            {
                failures.Add("M44 profile must keep parry window at 0.30 seconds.");
            }

            if (profile.GuardConeDegrees < 139f || profile.GuardConeDegrees > 141f)
            {
                failures.Add("M44 profile must keep guard cone at 140 degrees.");
            }

            if (profile.GuardDrainStaminaPerSecond != 0f || profile.GuardHitStaminaCost != 22f || profile.ParryStaminaCost != 28f)
            {
                failures.Add("M44 profile must keep no passive hold drain and high-cost guard/parry stamina tuning.");
            }
        }

        private static void ValidateRuntimeConstants(List<string> failures)
        {
            if (PlayerDefenseController.ParryWindowSeconds != 0.3f ||
                PlayerDefenseController.GuardConeDegrees != 140f ||
                PlayerDefenseController.GuardDrainStaminaPerSecond != 0f ||
                PlayerDefenseController.GuardBlockStaminaCost != 22f ||
                PlayerDefenseController.ParryStaminaCost != 28f)
            {
                failures.Add("M44 PlayerDefenseController constants must match the locked shield tuning.");
            }
        }

        private static void ValidateEnums(List<string> failures)
        {
            foreach (var threat in new[]
                     {
                         DamageThreatKind.Light,
                         DamageThreatKind.Heavy,
                         DamageThreatKind.Boss,
                         DamageThreatKind.StrongProjectile
                     })
            {
                if (!System.Enum.IsDefined(typeof(DamageThreatKind), threat))
                {
                    failures.Add($"Missing M44 damage threat kind: {threat}");
                }
            }

            foreach (var role in new[]
                     {
                         MaterialRole.ShieldGuard,
                         MaterialRole.ShieldParry,
                         MaterialRole.ShieldBlock,
                         MaterialRole.ShieldUnavailable
                     })
            {
                if (!System.Enum.IsDefined(typeof(MaterialRole), role))
                {
                    failures.Add($"Missing M44 shield material role: {role}");
                }
            }

            foreach (var cue in new[]
                     {
                         VfxCueId.ShieldGuardStart,
                         VfxCueId.ShieldBlock,
                         VfxCueId.ShieldParryCounter,
                         VfxCueId.ShieldUnavailable
                     })
            {
                if (!System.Enum.IsDefined(typeof(VfxCueId), cue))
                {
                    failures.Add($"Missing M44 shield VFX cue: {cue}");
                }
            }

            foreach (var cue in new[]
                     {
                         AudioCueId.ShieldGuardStart,
                         AudioCueId.ShieldBlock,
                         AudioCueId.ShieldParryCounter,
                         AudioCueId.ShieldUnavailable
                     })
            {
                if (!System.Enum.IsDefined(typeof(AudioCueId), cue))
                {
                    failures.Add($"Missing M44 shield audio cue: {cue}");
                }
            }
        }
    }
}
