using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone79Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 79 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateRuntimeCatalog(failures);
            ValidateBossRoster(failures);
            ValidateAssetPolicies(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 79 validation passed.");
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
            ExpectFile(Milestone79AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone79AssetGenerator.ReportPath, failures);
            if (File.Exists(Milestone79AssetGenerator.DocsPath))
            {
                var docs = File.ReadAllText(Milestone79AssetGenerator.DocsPath);
                foreach (var required in new[] { "Contact Damage Rework", "ActiveOnly", "PassiveHazard", "Disabled", "Proximity", "Mirror Husk" })
                {
                    if (!docs.Contains(required))
                    {
                        failures.Add($"M79 docs are missing `{required}`.");
                    }
                }
            }
        }

        private static void ValidateRuntimeCatalog(List<string> failures)
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null))
            {
                if (enemy.ContactDamagePolicy != EnemyContactDamagePolicy.ActiveOnly ||
                    enemy.PassiveContactHazardType != EnemyPassiveContactHazardType.None)
                {
                    failures.Add($"{enemy.SpawnKind} must be ActiveOnly + None in M79.");
                }
            }
        }

        private static void ValidateBossRoster(List<string> failures)
        {
            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                if (boss.ContactDamagePolicy != EnemyContactDamagePolicy.ActiveOnly ||
                    boss.PassiveContactHazardType != EnemyPassiveContactHazardType.None)
                {
                    failures.Add($"{boss.BossId} must be ActiveOnly + None in M79.");
                }
            }
        }

        private static void ValidateAssetPolicies(List<string> failures)
        {
            foreach (var row in Milestone76AssetGenerator.EnemyAssetPathsBySpawnKind())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(row.Value);
                if (enemy == null)
                {
                    continue;
                }

                if (enemy.ContactDamagePolicy != EnemyContactDamagePolicy.ActiveOnly ||
                    enemy.PassiveContactHazardType != EnemyPassiveContactHazardType.None)
                {
                    failures.Add($"{row.Value} must be ActiveOnly + None in M79.");
                }
            }

            foreach (var row in Milestone75AssetGenerator.BossRows())
            {
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>($"{Milestone53AssetGenerator.BossDirectory}/{row.FileName}");
                if (boss == null)
                {
                    continue;
                }

                if (boss.ContactDamagePolicy != EnemyContactDamagePolicy.ActiveOnly ||
                    boss.PassiveContactHazardType != EnemyPassiveContactHazardType.None)
                {
                    failures.Add($"{row.FileName} must be ActiveOnly + None in M79.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M79 file: {path}");
            }
        }
    }
}
