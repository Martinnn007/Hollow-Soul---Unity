using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone75Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 75 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateEnemies(failures);
            ValidateBosses(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 75 validation passed.");
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
            ExpectFile(Milestone75AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone75AssetGenerator.PdfPath, failures);
            ExpectFile("tools/generate_m75_enemy_attacks_and_senses_pdf.py", failures);
            ExpectFile("tools/verify_m75_enemy_attacks_and_senses_pdf.py", failures);
        }

        private static void ValidateEnemies(List<string> failures)
        {
            foreach (var row in Milestone75AssetGenerator.EnemyRows())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Hollow/Data/Enemies/{row.FileName}");
                if (enemy == null)
                {
                    failures.Add($"Missing M75 enemy asset: {row.FileName}");
                    continue;
                }

                if (!Approximately(enemy.SightRadiusMeters, row.Sight) ||
                    !Approximately(enemy.SightAngleDegrees, row.Angle) ||
                    !Approximately(enemy.HearingRadiusMeters, row.Hearing))
                {
                    failures.Add($"{enemy.SpawnKind} should use senses {row.Sight:0.##}m/{row.Angle:0.##}deg/{row.Hearing:0.##}m.");
                }

                if (enemy.LungeAttackEnabled != row.LungeEnabled ||
                    !Approximately(enemy.LungeTriggerRangeMeters, row.LungeTrigger) ||
                    !Approximately(enemy.LungeWindupSeconds, Milestone75AssetGenerator.DefaultLungeWindupSeconds) ||
                    !Approximately(enemy.LungeActiveSeconds, Milestone75AssetGenerator.DefaultLungeActiveSeconds) ||
                    !Approximately(enemy.LungeDistanceMeters, Milestone75AssetGenerator.DefaultLungeDistanceMeters) ||
                    !Approximately(enemy.LungeCooldownSeconds, Milestone75AssetGenerator.DefaultLungeCooldownSeconds))
                {
                    failures.Add($"{enemy.SpawnKind} should use the M75 lunge profile.");
                }
            }
        }

        private static void ValidateBosses(List<string> failures)
        {
            foreach (var row in Milestone75AssetGenerator.BossRows())
            {
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>($"{Milestone53AssetGenerator.BossDirectory}/{row.FileName}");
                if (boss == null)
                {
                    failures.Add($"Missing M75 boss asset: {row.FileName}");
                    continue;
                }

                if (!Approximately(boss.SightRadiusMeters, row.Sight) ||
                    !Approximately(boss.SightAngleDegrees, row.Angle) ||
                    !Approximately(boss.HearingRadiusMeters, row.Hearing))
                {
                    failures.Add($"{boss.BossId} should use boss sense metadata {row.Sight:0.##}m/{row.Angle:0.##}deg/{row.Hearing:0.##}m.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M75 file: {path}");
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.001f;
        }
    }
}
