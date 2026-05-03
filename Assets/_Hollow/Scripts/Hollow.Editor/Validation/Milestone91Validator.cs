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
    public static class Milestone91Validator
    {
        private static readonly string[] RequiredDocsText =
        {
            "Preferred Distance",
            "action-specific range",
            "recovery spacing",
            "retreat caps",
            "Current Roster Spacing Table",
            "Boss runtime spacing remains unchanged"
        };

        [MenuItem("Hollow/Validation/Run Milestone 91 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateEnemySpacing(failures);
            ValidateBossMetadata(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 91 validation passed.");
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
            ExpectFile(Milestone91AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone91AssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone91AssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone91AssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M91 docs are missing `{required}`.");
                }
            }
        }

        private static void ValidateEnemySpacing(List<string> failures)
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .ToArray();
            if (enemies.Length < 20)
            {
                failures.Add("M91 expected the expanded non-boss roster to be present.");
            }

            foreach (var enemy in enemies)
            {
                var profile = enemy.SpacingProfile;
                if (profile == null)
                {
                    failures.Add($"{enemy.SpawnKind} does not resolve a spacing profile.");
                    continue;
                }

                if (profile.DefaultIdealDistanceMeters <= 0f ||
                    profile.DefaultCloseToleranceMeters < 0f ||
                    profile.DefaultLongToleranceMeters < 0f)
                {
                    failures.Add($"{enemy.SpawnKind} has invalid default spacing values.");
                }

                if (enemy.PreferredRangeMaxMeters <= enemy.PreferredRangeMinMeters)
                {
                    failures.Add($"{enemy.SpawnKind} has invalid deprecated fallback preferred range values.");
                }

                var currentActions = enemy.ActionProfiles
                    .Where(action => action != null && action.UsageState == EnemyActionUsageState.CurrentRuntime)
                    .ToArray();
                if (currentActions.Length == 0)
                {
                    failures.Add($"{enemy.SpawnKind} has no current runtime actions for M91 spacing.");
                }

                foreach (var action in currentActions)
                {
                    var attack = action.HasLinkedAttack ? enemy.ResolveAttackProfile(action.LinkedAttackId) : null;
                    var spacing = profile.ResolveActionSpacing(action, attack);
                    if (spacing.CommitRangeMaxMeters <= spacing.CommitRangeMinMeters)
                    {
                        failures.Add($"{enemy.SpawnKind}/{action.ActionId} has invalid action-specific range.");
                    }

                    if (spacing.RecoveryMovementDistanceMeters < 0f || spacing.RecoverySpeedMultiplier < 0f)
                    {
                        failures.Add($"{enemy.SpawnKind}/{action.ActionId} has invalid recovery spacing.");
                    }

                    if (spacing.MaxResetCountBeforeCommit < 0)
                    {
                        failures.Add($"{enemy.SpawnKind}/{action.ActionId} has invalid retreat cap.");
                    }
                }

                if (IsRangedOrCaster(enemy.BehaviorId) &&
                    profile.ActionOverrides.Any(row => row.RecoveryMovementMode is EnemySpacingRecoveryMode.RangedReset or EnemySpacingRecoveryMode.PhaseDrift && row.MaxResetCountBeforeCommit > 1))
                {
                    failures.Add($"{enemy.SpawnKind} should not get more than one ranged/caster reset before commit or hold.");
                }
            }
        }

        private static void ValidateBossMetadata(List<string> failures)
        {
            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                var profile = boss.SpacingProfileMetadata;
                if (profile == null)
                {
                    failures.Add($"{boss.BossId} does not resolve M91 spacing metadata.");
                    continue;
                }

                if (profile.DefaultIdealDistanceMeters <= 0f)
                {
                    failures.Add($"{boss.BossId} has invalid boss spacing metadata.");
                }

                if (boss.BehaviorTreeMetadata == null || boss.BehaviorTreeMetadata.RootNode == null)
                {
                    failures.Add($"{boss.BossId} should remain metadata-driven and runtime-exempt for behavior trees.");
                }
            }
        }

        private static bool IsRangedOrCaster(EnemyBehaviorId behaviorId)
        {
            return behaviorId is EnemyBehaviorId.TurretShooter
                or EnemyBehaviorId.SpittingPod
                or EnemyBehaviorId.HollowArcher
                or EnemyBehaviorId.PowderGunner
                or EnemyBehaviorId.KnifeThrower
                or EnemyBehaviorId.RepeaterTurret
                or EnemyBehaviorId.ClockworkSentry
                or EnemyBehaviorId.HollowAcolyte
                or EnemyBehaviorId.Wraith
                or EnemyBehaviorId.SoulEater
                or EnemyBehaviorId.CurseBinder
                or EnemyBehaviorId.GraveLantern;
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M91 file: {path}");
            }
        }
    }
}
