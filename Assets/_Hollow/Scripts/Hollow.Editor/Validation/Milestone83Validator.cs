using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone83Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 83 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateEnemyTuning(failures);
            ValidateTierDefaults(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 83 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateFiles(List<string> failures)
        {
            ExpectFile(Milestone83AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone83AssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone83AssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone83AssetGenerator.DocsPath);
            foreach (var required in new[] { "Noise + Disturbance", "Quiet", "Normal", "Loud", "Violent", "Bump", "territorial", "Bosses are metadata/docs only" })
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M83 docs are missing `{required}`.");
                }
            }
        }

        private static void ValidateEnemyTuning(List<string> failures)
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null))
            {
                if (enemy.HearingSensitivityMultiplier < 0.1f || enemy.HearingSensitivityMultiplier > 3f)
                {
                    failures.Add($"{enemy.SpawnKind} has invalid hearing sensitivity.");
                }

                if (enemy.DisturbanceEscalationThreshold <= 0f)
                {
                    failures.Add($"{enemy.SpawnKind} has invalid disturbance threshold.");
                }

                if (enemy.InvestigationDurationSeconds <= 0f)
                {
                    failures.Add($"{enemy.SpawnKind} has invalid investigation duration.");
                }

                var expected = EnemyDefinition.DefaultDisturbanceTuningFor(enemy.ArchetypeId, enemy.BehaviorId, enemy.MovementMode);
                if (!Approximately(enemy.HearingSensitivityMultiplier, expected.x) ||
                    !Approximately(enemy.DisturbanceEscalationThreshold, expected.y) ||
                    !Approximately(enemy.InvestigationDurationSeconds, expected.z))
                {
                    failures.Add($"{enemy.SpawnKind} does not match M83 default disturbance tuning.");
                }
            }
        }

        private static void ValidateTierDefaults(List<string> failures)
        {
            if (EnemyStimulusTierExtensions.DefaultFor(EnemyStimulusKind.Footstep) != EnemyStimulusTier.Quiet ||
                EnemyStimulusTierExtensions.DefaultFor(EnemyStimulusKind.Roll) != EnemyStimulusTier.Normal ||
                EnemyStimulusTierExtensions.DefaultFor(EnemyStimulusKind.MeleeAttack) != EnemyStimulusTier.Normal ||
                EnemyStimulusTierExtensions.DefaultFor(EnemyStimulusKind.RangedAttack) != EnemyStimulusTier.Normal ||
                EnemyStimulusTierExtensions.DefaultFor(EnemyStimulusKind.GuardImpact) != EnemyStimulusTier.Loud ||
                EnemyStimulusTierExtensions.DefaultFor(EnemyStimulusKind.Damage) != EnemyStimulusTier.Violent)
            {
                failures.Add("M83 stimulus tier defaults are invalid.");
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.001f;
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M83 file: {path}");
            }
        }
    }
}
