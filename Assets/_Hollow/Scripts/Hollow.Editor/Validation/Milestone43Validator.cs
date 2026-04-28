using System.Collections.Generic;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone43Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/CombatFeelProfileDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/DamageFeedbackContext.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/CombatKnockbackReceiver.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/PlayerDamageFeedbackController.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/CorpseGhostPresenter.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone43AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone43Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone43CombatFeelTests.cs",
            "Docs/Milestone43CombatFeelDamageFeedback.md",
            Milestone43AssetGenerator.ProfilePath,
            Milestone43AssetGenerator.ReportPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 43 Validation")]
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
                    failures.Add($"Missing M43 file: {file}");
                }
            }

            ValidateProfile(failures);
            ValidateCueEnums(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 43 validation passed.");
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
            var profile = AssetDatabase.LoadAssetAtPath<CombatFeelProfileDefinition>(Milestone43AssetGenerator.ProfilePath);
            if (profile == null)
            {
                failures.Add("M43 combat feel profile asset is missing or invalid.");
                return;
            }

            if (profile.PlayerInvulnerabilitySeconds < 0.59f || profile.PlayerInvulnerabilitySeconds > 0.61f)
            {
                failures.Add("M43 profile must keep player invulnerability at 0.6 seconds.");
            }

            if (profile.CorpseGhostSeconds < 1.49f || profile.CorpseGhostSeconds > 1.51f)
            {
                failures.Add("M43 profile must keep corpse ghosts at 1.5 seconds.");
            }

            if (profile.ShowWindupLabels)
            {
                failures.Add("M43 profile should keep windup labels disabled for subtle telegraphs.");
            }
        }

        private static void ValidateCueEnums(List<string> failures)
        {
            foreach (var cue in new[]
                     {
                         VfxCueId.PlayerInvulnerable,
                         VfxCueId.KnockbackImpact,
                         VfxCueId.EnemyWindup,
                         VfxCueId.EnemyCorpseGhost,
                         VfxCueId.DamageBlocked
                     })
            {
                if (!System.Enum.IsDefined(typeof(VfxCueId), cue))
                {
                    failures.Add($"Missing M43 VFX cue enum: {cue}");
                }
            }
        }
    }
}
