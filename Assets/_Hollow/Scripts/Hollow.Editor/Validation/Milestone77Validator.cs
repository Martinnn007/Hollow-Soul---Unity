using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone77Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 77 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateEnemies(failures);
            ValidateEncounters(failures);
            ValidatePresentationRoles(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 77 validation passed.");
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
            ExpectFile(Milestone77AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone77AssetGenerator.PdfPath, failures);
            ExpectFile(Milestone77AssetGenerator.ReportPath, failures);
            ExpectFile("tools/generate_m77_critter_roster_pdf.py", failures);
            ExpectFile("tools/verify_m77_critter_roster_pdf.py", failures);
            foreach (var roomId in Milestone77AssetGenerator.ShowcaseRoomIds)
            {
                ExpectFile($"{Milestone77AssetGenerator.ShowcaseRoomDirectory}/{roomId}.hollowruntime.json", failures);
            }
        }

        private static void ValidateEnemies(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>("Assets/_Hollow/Data/Enemies/EnemyCatalog.asset");
            foreach (var spec in Milestone77AssetGenerator.EnemyRows())
            {
                var assetPath = $"Assets/_Hollow/Data/Enemies/{spec.FileName}";
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(assetPath);
                if (enemy == null)
                {
                    failures.Add($"Missing M77 enemy asset: {assetPath}");
                    continue;
                }

                if (catalog == null || catalog.Resolve(spec.SpawnKind)?.SpawnKind != spec.SpawnKind)
                {
                    failures.Add($"{spec.SpawnKind} is not resolvable from EnemyCatalog.asset.");
                }

                if (enemy.BehaviorId != spec.BehaviorId ||
                    enemy.MaxHealth != spec.MaxHealth ||
                    !Approximately(enemy.SpeedMetersPerSecond, spec.SpeedMetersPerSecond) ||
                    !Approximately(enemy.RadiusMeters, spec.RadiusMeters) ||
                    enemy.Intelligence != spec.Intelligence ||
                    enemy.Disposition != spec.Disposition ||
                    !Approximately(enemy.SightRadiusMeters, spec.SightRadiusMeters) ||
                    !Approximately(enemy.SightAngleDegrees, spec.SightAngleDegrees) ||
                    !Approximately(enemy.HearingRadiusMeters, spec.HearingRadiusMeters) ||
                    !Approximately(enemy.PreferredRangeMinMeters, spec.PreferredRangeMinMeters) ||
                    !Approximately(enemy.PreferredRangeMaxMeters, spec.PreferredRangeMaxMeters))
                {
                    failures.Add($"{spec.SpawnKind} does not match the M77 authored stat contract.");
                }

                var expectedAttacks = EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(profile => profile.OwnerId == spec.SpawnKind)
                    .Select(profile => profile.AttackId)
                    .ToArray();
                var actualAttacks = enemy.AttackProfiles.Select(profile => profile.AttackId).ToArray();
                foreach (var attackId in expectedAttacks)
                {
                    if (!actualAttacks.Contains(attackId))
                    {
                        failures.Add($"{spec.SpawnKind} is missing M77 attack profile `{attackId}`.");
                    }
                }
            }
        }

        private static void ValidateEncounters(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone48AssetGenerator.EncounterCatalogPath);
            if (catalog == null)
            {
                failures.Add("Missing active M48 encounter catalog for M77 rotation.");
                return;
            }

            foreach (var encounterId in Milestone77AssetGenerator.EncounterIds)
            {
                if (!catalog.Encounters.Any(encounter => encounter != null && encounter.EncounterId == encounterId))
                {
                    failures.Add($"M77 encounter `{encounterId}` is not in the active encounter catalog.");
                }
            }
        }

        private static void ValidatePresentationRoles(List<string> failures)
        {
            var roles = new[]
            {
                PresentationPrefabRole.EnemySpittingPod,
                PresentationPrefabRole.EnemyRat,
                PresentationPrefabRole.EnemySpider
            };

            foreach (var role in roles)
            {
                if (PresentationPrefabResolver.Resolve(role) == null)
                {
                    failures.Add($"{role} did not resolve to a fallback presentation prefab.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M77 file: {path}");
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.001f;
        }
    }
}
