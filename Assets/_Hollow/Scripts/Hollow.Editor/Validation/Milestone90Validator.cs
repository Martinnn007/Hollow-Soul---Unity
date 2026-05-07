using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone90Validator
    {
        private static readonly string[] RequiredDocsText =
        {
            "Combat AI QA Lock",
            "contact",
            "active windows",
            "weapon users",
            "senses",
            "movement",
            "knockback",
            "bosses",
            "Preferred Distance",
            "M91"
        };

        [MenuItem("Hollow/Validation/Run Milestone 90 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateGlobalContracts(failures);
            ValidateEnemyRoster(failures);
            ValidateBossRoster(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 90 validation passed.");
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
            ExpectFile(Milestone90AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone90AssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone90AssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone90AssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M90 docs are missing `{required}`.");
                }
            }
        }

        private static void ValidateGlobalContracts(List<string> failures)
        {
            if (EnemyNavigationAdapter.CurrentBackend != EnemyNavigationBackend.LocalSteering &&
                EnemyNavigationAdapter.CurrentBackend != EnemyNavigationBackend.UnityNavMesh)
            {
                failures.Add("M90 expects LocalSteering, or the later M97 UnityNavMesh backend once explicitly enabled.");
            }

            if (EnemyStimulusTierExtensions.DefaultFor(EnemyStimulusKind.AllyAlert) != EnemyStimulusTier.Normal)
            {
                failures.Add("M90 expects M89 AllyAlert stimuli to remain Normal tier.");
            }
        }

        private static void ValidateEnemyRoster(List<string> failures)
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .ToArray();
            if (enemies.Length < 20)
            {
                failures.Add("M90 expected the expanded M72-M89 non-boss roster to be present.");
            }

            foreach (var enemy in enemies)
            {
                if (string.IsNullOrWhiteSpace(enemy.SpawnKind) || string.IsNullOrWhiteSpace(enemy.DisplayName))
                {
                    failures.Add("Enemy roster contains an unnamed definition.");
                }

                if (enemy.MaxHealth <= 0 || enemy.RadiusMeters <= 0f)
                {
                    failures.Add($"{enemy.SpawnKind} has invalid health or radius.");
                }

                if (enemy.ContactDamagePolicy != EnemyContactDamagePolicy.ActiveOnly ||
                    enemy.PassiveContactHazardType != EnemyPassiveContactHazardType.None)
                {
                    failures.Add($"{enemy.SpawnKind} should remain ActiveOnly with no passive hazard for M90.");
                }

                if (enemy.PreferredRangeMaxMeters <= enemy.PreferredRangeMinMeters)
                {
                    failures.Add($"{enemy.SpawnKind} has invalid preferred distance tuning.");
                }

                if (enemy.SightRadiusMeters <= 0f && enemy.HearingRadiusMeters <= 0f)
                {
                    failures.Add($"{enemy.SpawnKind} has no active sense.");
                }

                ValidateAttackProfiles(enemy.SpawnKind, enemy.AttackProfiles, failures);
                ValidateActionProfiles(enemy.SpawnKind, enemy.ActionProfiles, enemy.ResolveAttackProfile, failures);

                if (enemy.BehaviorTree == null || enemy.BehaviorTree.RootNode == null)
                {
                    failures.Add($"{enemy.SpawnKind} must resolve a runtime behavior tree.");
                }
            }
        }

        private static void ValidateBossRoster(List<string> failures)
        {
            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            if (bosses.Length < 10)
            {
                failures.Add("M90 expected the M53 boss roster to be present.");
            }

            foreach (var boss in bosses)
            {
                if (boss.ContactDamagePolicy != EnemyContactDamagePolicy.ActiveOnly ||
                    boss.PassiveContactHazardType != EnemyPassiveContactHazardType.None)
                {
                    failures.Add($"{boss.BossId} should remain ActiveOnly with no passive hazard for M90.");
                }

                ValidateAttackProfiles(boss.BossId, boss.AttackProfiles, failures);
                ValidateActionProfiles(boss.BossId, boss.ActionProfiles, boss.ResolveAttackProfile, failures);

                if (boss.BehaviorTreeMetadata == null || boss.BehaviorTreeMetadata.RootNode == null)
                {
                    failures.Add($"{boss.BossId} must resolve metadata-only boss behavior tree data.");
                }
            }
        }

        private static void ValidateAttackProfiles(string ownerId, IReadOnlyList<EnemyAttackProfileDefinition> profiles, List<string> failures)
        {
            if (profiles == null || profiles.Count == 0)
            {
                failures.Add($"{ownerId} has no attack profiles.");
                return;
            }

            foreach (var profile in profiles.Where(profile => profile != null))
            {
                if (string.IsNullOrWhiteSpace(profile.AttackId))
                {
                    failures.Add($"{ownerId} has an attack profile with no id.");
                }

                if (profile.CooldownSeconds <= 0f || profile.ActiveSeconds <= 0f || profile.RecoverySeconds <= 0f)
                {
                    failures.Add($"{ownerId}/{profile.AttackId} has invalid execution timing.");
                }

                if (profile.HitArcDegrees <= 0f || profile.HitArcDegrees > 360f)
                {
                    failures.Add($"{ownerId}/{profile.AttackId} has invalid hit arc.");
                }

                if (profile.Damage > 0 &&
                    ((int)profile.ForceClass < (int)ImpactForceClass.Light ||
                     (int)profile.ForceClass > (int)ImpactForceClass.Massive))
                {
                    failures.Add($"{ownerId}/{profile.AttackId} has invalid force class.");
                }
            }
        }

        private static void ValidateActionProfiles(
            string ownerId,
            IReadOnlyList<EnemyActionProfileDefinition> profiles,
            Func<string, EnemyAttackProfileDefinition> resolveAttack,
            List<string> failures)
        {
            if (profiles == null || profiles.Count == 0)
            {
                failures.Add($"{ownerId} has no action profiles.");
                return;
            }

            foreach (var profile in profiles.Where(profile => profile != null))
            {
                if (string.IsNullOrWhiteSpace(profile.ActionId))
                {
                    failures.Add($"{ownerId} has an action profile with no id.");
                }

                if (profile.MaxRangeMeters <= profile.MinRangeMeters)
                {
                    failures.Add($"{ownerId}/{profile.ActionId} has invalid scoring range.");
                }

                if (profile.UsageState == EnemyActionUsageState.CurrentRuntime &&
                    !profile.ExplicitlyNonDamaging &&
                    profile.HasLinkedAttack &&
                    resolveAttack(profile.LinkedAttackId) == null)
                {
                    failures.Add($"{ownerId}/{profile.ActionId} links missing attack `{profile.LinkedAttackId}`.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M90 file: {path}");
            }
        }
    }
}
