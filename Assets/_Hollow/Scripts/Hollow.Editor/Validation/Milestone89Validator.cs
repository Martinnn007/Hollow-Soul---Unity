using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone89Validator
    {
        private static readonly string[] RequiredDocsText =
        {
            "Limited Alert Sharing",
            "AllyAlert",
            "M88",
            "not squad tactics",
            "Bosses are exempt",
            "M90"
        };

        [MenuItem("Hollow/Validation/Run Milestone 89 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateEnemyDefaults(failures);
            ValidateStimulusContract(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 89 validation passed.");
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
            ExpectFile(Milestone89AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone89AssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone89AssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone89AssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M89 docs are missing `{required}`.");
                }
            }
        }

        private static void ValidateEnemyDefaults(List<string> failures)
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            var enabled = catalog.Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss" && enemy.AllyAlertSharingEnabled)
                .ToArray();
            if (enabled.Length < 8)
            {
                failures.Add("M89 should enable limited alert sharing on a meaningful subset of weapon/ranged/magic enemies.");
            }

            foreach (var enemy in catalog.Definitions.Where(enemy => enemy != null))
            {
                if (enemy.SpawnKind == "spawnEnemyBoss")
                {
                    if (enemy.AllyAlertSharingEnabled || enemy.AllyAlertRadiusMeters > 0f)
                    {
                        failures.Add("Boss fallback enemy must not share M89 ally alerts.");
                    }

                    continue;
                }

                if (enemy.AllyAlertCooldownSeconds <= 0f)
                {
                    failures.Add($"{enemy.SpawnKind} has invalid ally-alert cooldown.");
                }

                if (enemy.AllyAlertSharingEnabled && enemy.AllyAlertRadiusMeters <= 0f)
                {
                    failures.Add($"{enemy.SpawnKind} is alert-enabled but has no radius.");
                }
            }

            ExpectEnabled(catalog, "spawnEnemyKnight", failures);
            ExpectEnabled(catalog, "spawnEnemyPowderGunner", failures);
            ExpectEnabled(catalog, "spawnEnemyCurseBinder", failures);
            ExpectDisabled(catalog, "spawnEnemyRat", failures);
            ExpectDisabled(catalog, "spawnEnemySpider", failures);
            ExpectDisabled(catalog, "spawnEnemyHollowBird", failures);
        }

        private static void ValidateStimulusContract(List<string> failures)
        {
            if (EnemyStimulusTierExtensions.DefaultFor(EnemyStimulusKind.AllyAlert) != EnemyStimulusTier.Normal)
            {
                failures.Add("AllyAlert should default to Normal stimulus tier.");
            }
        }

        private static void ExpectEnabled(EnemyCatalog catalog, string spawnKind, List<string> failures)
        {
            var enemy = catalog.Resolve(spawnKind);
            if (enemy == null || !enemy.AllyAlertSharingEnabled)
            {
                failures.Add($"{spawnKind} should be an M89 alert-sharing source.");
            }
        }

        private static void ExpectDisabled(EnemyCatalog catalog, string spawnKind, List<string> failures)
        {
            var enemy = catalog.Resolve(spawnKind);
            if (enemy != null && enemy.AllyAlertSharingEnabled)
            {
                failures.Add($"{spawnKind} should stay out of general M89 alert sharing.");
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M89 file: {path}");
            }
        }
    }
}
