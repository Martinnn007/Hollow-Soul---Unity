using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone81Validator
    {
        private static readonly string[] RequiredText =
        {
            "Enemy Action Profiles V2",
            "Body",
            "Weapon",
            "Magic",
            "Defense",
            "Hazard",
            "poise",
            "counterplay",
            "Rat",
            "Spider"
        };

        [MenuItem("Hollow/Validation/Run Milestone 81 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateCurrentAttackLinks(failures);
            ValidateSchema(failures);
            ValidateCoverage(failures);
            ValidateDefinitionFallbacks(failures);
            ValidatePdfExtraction(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 81 validation passed.");
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
            ExpectFile(Milestone81AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone81AssetGenerator.PdfPath, failures);
            ExpectFile(Milestone81AssetGenerator.ReportPath, failures);
            ExpectFile(Milestone81AssetGenerator.GeneratorScriptPath, failures);
            ExpectFile(Milestone81AssetGenerator.VerifyScriptPath, failures);

            if (!File.Exists(Milestone81AssetGenerator.DocsPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone81AssetGenerator.DocsPath);
            foreach (var required in RequiredText)
            {
                if (markdown.IndexOf(required, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add($"M81 catalogue markdown is missing `{required}`.");
                }
            }
        }

        private static void ValidateCurrentAttackLinks(List<string> failures)
        {
            foreach (var attack in EnemyAttackProfileDefaults.AllEnemySpecs)
            {
                var action = EnemyActionProfileDefaults.AllEnemySpecs.FirstOrDefault(candidate =>
                    candidate.OwnerId == attack.OwnerId &&
                    candidate.ActionId == attack.AttackId &&
                    candidate.UsageState == EnemyActionUsageState.CurrentRuntime);
                if (string.IsNullOrWhiteSpace(action.ActionId))
                {
                    failures.Add($"Missing M81 enemy action wrapper for `{attack.OwnerId}:{attack.AttackId}`.");
                    continue;
                }

                if (action.LinkedAttackId != attack.AttackId)
                {
                    failures.Add($"M81 enemy action `{action.ActionId}` does not link its M76 attack.");
                }
            }

            foreach (var attack in EnemyAttackProfileDefaults.AllBossSpecs)
            {
                var action = EnemyActionProfileDefaults.AllBossSpecs.FirstOrDefault(candidate =>
                    candidate.OwnerId == attack.OwnerId &&
                    candidate.ActionId == attack.AttackId &&
                    candidate.UsageState == EnemyActionUsageState.CurrentRuntime);
                if (string.IsNullOrWhiteSpace(action.ActionId))
                {
                    failures.Add($"Missing M81 boss action wrapper for `{attack.OwnerId}:{attack.AttackId}`.");
                    continue;
                }

                if (action.LinkedAttackId != attack.AttackId)
                {
                    failures.Add($"M81 boss action `{action.ActionId}` does not link its M76 attack.");
                }
            }
        }

        private static void ValidateSchema(List<string> failures)
        {
            foreach (var spec in Milestone81AssetGenerator.AllActionSpecs())
            {
                if (string.IsNullOrWhiteSpace(spec.ActionId) || string.IsNullOrWhiteSpace(spec.DisplayName))
                {
                    failures.Add("M81 action profile has a missing id or display name.");
                }

                if (spec.MinRangeMeters < 0f || spec.MaxRangeMeters <= 0f || spec.MinRangeMeters > spec.IdealRangeMeters || spec.IdealRangeMeters > spec.MaxRangeMeters)
                {
                    failures.Add($"{spec.ActionId} has invalid scoring range fields.");
                }

                if (spec.BaseWeight <= 0f)
                {
                    failures.Add($"{spec.ActionId} must have a positive base weight.");
                }

                if (spec.PressureCost < 0)
                {
                    failures.Add($"{spec.ActionId} has invalid pressure cost.");
                }

                if (string.IsNullOrWhiteSpace(spec.CooldownGroup))
                {
                    failures.Add($"{spec.ActionId} must have a cooldown group.");
                }

                if (spec.AllowedDispositions.Count == 0)
                {
                    failures.Add($"{spec.ActionId} must allow at least one disposition.");
                }

                if (spec.FacingArcDegrees < 0f || spec.FacingArcDegrees > 360f)
                {
                    failures.Add($"{spec.ActionId} has invalid facing arc.");
                }

                if (spec.PunishabilityRating < 0 || spec.PunishabilityRating > 5 || spec.GuardPressureRating < 0 || spec.GuardPressureRating > 5)
                {
                    failures.Add($"{spec.ActionId} has invalid counterplay ratings.");
                }

                if (spec.BestUserTags.Count == 0)
                {
                    failures.Add($"{spec.ActionId} must have best-user tags.");
                }

                if (!spec.HasLinkedAttack && !spec.ExplicitlyNonDamaging)
                {
                    failures.Add($"{spec.ActionId} is unlinked but not explicitly non-damaging.");
                }
            }
        }

        private static void ValidateCoverage(List<string> failures)
        {
            if (EnemyActionProfileDefaults.LibraryTemplateSpecs.Count < 60)
            {
                failures.Add($"M81 must provide at least 60 reusable action templates; found {EnemyActionProfileDefaults.LibraryTemplateSpecs.Count}.");
            }

            foreach (EnemyActionCategory category in Enum.GetValues(typeof(EnemyActionCategory)))
            {
                if (!EnemyActionProfileDefaults.LibraryTemplateSpecs.Any(spec => spec.Category == category))
                {
                    failures.Add($"M81 library templates are missing category `{category}`.");
                }
            }

            foreach (var owner in EnemyAttackProfileDefaults.AllEnemySpecs.Select(spec => spec.OwnerId).Distinct())
            {
                var count = EnemyActionProfileDefaults.AllEnemySpecs.Count(spec => spec.OwnerId == owner);
                if (count == 0)
                {
                    failures.Add($"M81 enemy owner `{owner}` has no action profiles.");
                }
            }

            foreach (var owner in EnemyAttackProfileDefaults.AllBossSpecs.Select(spec => spec.OwnerId).Distinct())
            {
                var count = EnemyActionProfileDefaults.AllBossSpecs.Count(spec => spec.OwnerId == owner);
                if (count == 0)
                {
                    failures.Add($"M81 boss owner `{owner}` has no action profiles.");
                }
            }
        }

        private static void ValidateDefinitionFallbacks(List<string> failures)
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null))
            {
                if (enemy.ActionProfiles.Count == 0)
                {
                    failures.Add($"{enemy.SpawnKind} resolves no M81 action profiles.");
                }

                foreach (var attack in enemy.AttackProfiles)
                {
                    var action = enemy.ResolveActionProfile(attack.AttackId);
                    if (action == null || action.LinkedAttackId != attack.AttackId)
                    {
                        failures.Add($"{enemy.SpawnKind} cannot resolve action wrapper for attack `{attack.AttackId}`.");
                    }
                }
            }

            foreach (var owner in EnemyAttackProfileDefaults.AllBossSpecs.Select(spec => spec.OwnerId).Distinct())
            {
                if (EnemyActionProfileDefaults.CreateBossActions(owner).Count == 0)
                {
                    failures.Add($"{owner} resolves no M81 boss action profiles.");
                }
            }
        }

        private static void ValidatePdfExtraction(List<string> failures)
        {
            if (!File.Exists(Milestone81AssetGenerator.VerifyScriptPath) ||
                !File.Exists(Milestone81AssetGenerator.PdfPath) ||
                !File.Exists(Milestone81AssetGenerator.DocsPath))
            {
                return;
            }

            try
            {
                var startInfo = new DiagnosticsProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"\"{Path.GetFullPath(Milestone81AssetGenerator.VerifyScriptPath)}\"",
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = DiagnosticsProcess.Start(startInfo);
                if (process == null)
                {
                    failures.Add("M81 PDF extraction validation did not start.");
                    return;
                }

                if (!process.WaitForExit(15000))
                {
                    process.Kill();
                    failures.Add("M81 PDF extraction validation timed out.");
                    return;
                }

                var error = process.StandardError.ReadToEnd();
                if (process.ExitCode != 0)
                {
                    failures.Add($"M81 PDF extraction validation failed: {error}");
                }
            }
            catch (Exception exception)
            {
                failures.Add($"M81 PDF extraction validation failed: {exception.Message}");
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M81 file: {path}");
            }
        }
    }
}
