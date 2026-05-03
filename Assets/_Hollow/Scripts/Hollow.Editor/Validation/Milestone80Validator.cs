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
    public static class Milestone80Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 80 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateAttackProfileTiming(failures);
            ValidateEnemyExecutionModifiers(failures);
            ValidatePlayerTiming(failures);
            ValidateRollTuning(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 80 validation passed.");
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
            ExpectFile(Milestone80AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone80AssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone80AssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone80AssetGenerator.DocsPath);
            foreach (var required in new[] { "Active Hit Windows", "windup", "active", "recovery", "poise", "roll", "i-frames" })
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M80 docs are missing `{required}`.");
                }
            }
        }

        private static void ValidateAttackProfileTiming(List<string> failures)
        {
            foreach (var spec in Milestone76AssetGenerator.AllProfileSpecs())
            {
                var profile = spec.IsBoss
                    ? EnemyAttackProfileDefaults.ResolveBossProfile(spec.OwnerId, spec.AttackId)
                    : EnemyAttackProfileDefaults.ResolveEnemyProfile(spec.OwnerId, spec.AttackId);
                if (profile == null)
                {
                    failures.Add($"Missing runtime attack profile `{spec.AttackId}`.");
                    continue;
                }

                if (profile.RecoverySeconds <= 0f)
                {
                    failures.Add($"{spec.AttackId} must have positive recovery seconds.");
                }

                if (profile.HitArcDegrees <= 0f || profile.HitArcDegrees > 360f)
                {
                    failures.Add($"{spec.AttackId} must have a valid hit arc.");
                }

                if ((int)profile.PoiseBreakThreshold < (int)ImpactForceClass.Light ||
                    (int)profile.PoiseBreakThreshold > (int)ImpactForceClass.Massive)
                {
                    failures.Add($"{spec.AttackId} must have a valid poise break threshold.");
                }
            }
        }

        private static void ValidateEnemyExecutionModifiers(List<string> failures)
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null))
            {
                if (enemy.AttackWindupScale <= 0f || enemy.AttackActiveScale <= 0f || enemy.AttackRecoveryScale <= 0f)
                {
                    failures.Add($"{enemy.SpawnKind} has invalid attack execution scales.");
                }

                if (enemy.HitArcDegreesBonus < -90f || enemy.HitArcDegreesBonus > 120f)
                {
                    failures.Add($"{enemy.SpawnKind} has an invalid hit arc bonus.");
                }

                if (enemy.PoiseBreakThresholdOffset < -3 || enemy.PoiseBreakThresholdOffset > 3)
                {
                    failures.Add($"{enemy.SpawnKind} has an invalid poise threshold offset.");
                }
            }
        }

        private static void ValidatePlayerTiming(List<string> failures)
        {
            foreach (var attack in new[]
                     {
                         WeaponAttackDefinition.DefaultLight(WeaponSlot.Melee),
                         WeaponAttackDefinition.DefaultHeavy(WeaponSlot.Melee),
                         WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged),
                         WeaponAttackDefinition.DefaultHeavy(WeaponSlot.Ranged)
                     })
            {
                if (attack.WindupSeconds < 0f || attack.ActiveSeconds <= 0f || attack.RecoverySeconds <= 0f)
                {
                    failures.Add($"{attack.AttackKind} player attack has invalid timing.");
                }

                if (attack.WindupSeconds + attack.ActiveSeconds + attack.RecoverySeconds > attack.CooldownSeconds)
                {
                    failures.Add($"{attack.AttackKind} player attack commitment should fit inside its cooldown.");
                }
            }
        }

        private static void ValidateRollTuning(List<string> failures)
        {
            if (PlayerWeaponController.RollStaminaCost <= 0f ||
                PlayerWeaponController.RollDurationSeconds <= 0f ||
                PlayerWeaponController.RollInvulnerabilitySeconds <= 0f ||
                PlayerWeaponController.RollInvulnerabilitySeconds > PlayerWeaponController.RollDurationSeconds ||
                PlayerWeaponController.RollDistanceMeters <= 0f)
            {
                failures.Add("M80 roll constants are invalid.");
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M80 file: {path}");
            }
        }
    }
}
