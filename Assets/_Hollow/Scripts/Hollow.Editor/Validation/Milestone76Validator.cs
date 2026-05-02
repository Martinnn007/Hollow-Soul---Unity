using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone76Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 76 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateAttackProfileAssets(failures);
            ValidateEnemyAssignments(failures);
            ValidateBossAssignments(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 76 validation passed.");
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
            ExpectFile(Milestone76AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone76AssetGenerator.PdfPath, failures);
            ExpectFile("tools/generate_m76_enemy_attack_profiles_pdf.py", failures);
            ExpectFile("tools/verify_m76_enemy_attack_profiles_pdf.py", failures);
        }

        private static void ValidateAttackProfileAssets(List<string> failures)
        {
            foreach (var spec in Milestone76AssetGenerator.AllProfileSpecs())
            {
                var path = $"{Milestone76AssetGenerator.AttackDirectory}/{spec.AssetName}";
                var profile = AssetDatabase.LoadAssetAtPath<EnemyAttackProfileDefinition>(path);
                if (profile == null)
                {
                    failures.Add($"Missing M76 attack profile asset: {path}");
                    continue;
                }

                if (profile.AttackId != spec.AttackId ||
                    profile.RuntimeKind != spec.RuntimeKind ||
                    profile.Damage != Mathf.Max(0, spec.Damage) ||
                    profile.DamageChannel != spec.DamageChannel ||
                    profile.DamageDelivery != spec.DamageDelivery ||
                    profile.DamageElement != spec.DamageElement ||
                    profile.ForceClass != spec.ForceClass ||
                    profile.ThreatKind != spec.ThreatKind ||
                    !Approximately(profile.KnockbackMeters, spec.KnockbackMeters) ||
                    !Approximately(profile.GuardKnockbackMultiplier, spec.GuardKnockbackMultiplier))
                {
                    failures.Add($"{profile.AttackId} does not match its M76 attack profile spec.");
                }
            }
        }

        private static void ValidateEnemyAssignments(List<string> failures)
        {
            foreach (var row in Milestone76AssetGenerator.EnemyAssetPathsBySpawnKind())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(row.Value);
                if (enemy == null)
                {
                    failures.Add($"Missing M76 enemy asset: {row.Value}");
                    continue;
                }

                var expected = EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(spec => spec.OwnerId == row.Key)
                    .Select(spec => spec.AttackId)
                    .ToArray();
                var actual = enemy.AttackProfiles.Select(profile => profile.AttackId).ToArray();
                foreach (var attackId in expected)
                {
                    if (!actual.Contains(attackId))
                    {
                        failures.Add($"{enemy.SpawnKind} is missing attack profile `{attackId}`.");
                    }
                }
            }
        }

        private static void ValidateBossAssignments(List<string> failures)
        {
            foreach (var row in Milestone75AssetGenerator.BossRows())
            {
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>($"{Milestone53AssetGenerator.BossDirectory}/{row.FileName}");
                if (boss == null)
                {
                    failures.Add($"Missing M76 boss asset: {row.FileName}");
                    continue;
                }

                var expected = EnemyAttackProfileDefaults.AllBossSpecs
                    .Where(spec => spec.OwnerId == boss.BossId)
                    .Select(spec => spec.AttackId)
                    .ToArray();
                var actual = boss.AttackProfiles.Select(profile => profile.AttackId).ToArray();
                foreach (var attackId in expected)
                {
                    if (!actual.Contains(attackId))
                    {
                        failures.Add($"{boss.BossId} is missing attack profile `{attackId}`.");
                    }
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M76 file: {path}");
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.001f;
        }
    }
}
