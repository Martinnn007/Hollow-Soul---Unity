using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone84Validator
    {
        private static readonly string[] RequiredText =
        {
            "Weapon-User Enemies",
            "Skeleton",
            "Knight",
            "Giant",
            "shield",
            "combo",
            "recovery",
            "rusty_slash",
            "spear_thrust",
            "club_sweep"
        };

        [MenuItem("Hollow/Validation/Run Milestone 84 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateRuntimeRoster(failures);
            ValidateAttackProfiles(failures);
            ValidateActionProfiles(failures);
            ValidateBehaviorTrees(failures);
            ValidateGuards(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 84 validation passed.");
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
            ExpectFile(Milestone84AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone84AssetGenerator.ReportPath, failures);
            ExpectFile(Milestone84AssetGenerator.PdfPath, failures);
            ExpectFile(Milestone84AssetGenerator.GeneratorScriptPath, failures);
            ExpectFile(Milestone84AssetGenerator.VerifyScriptPath, failures);
            if (!File.Exists(Milestone84AssetGenerator.DocsPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone84AssetGenerator.DocsPath);
            foreach (var required in RequiredText)
            {
                if (markdown.IndexOf(required, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add($"M84 documentation is missing `{required}`.");
                }
            }
        }

        private static void ValidateRuntimeRoster(List<string> failures)
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spec in Milestone84AssetGenerator.EnemyRows())
            {
                var enemy = catalog.Resolve(spec.SpawnKind);
                if (enemy == null)
                {
                    failures.Add($"M84 enemy `{spec.SpawnKind}` is missing from runtime catalog.");
                    continue;
                }

                if (enemy.DisplayName != spec.DisplayName ||
                    enemy.BehaviorId != spec.BehaviorId ||
                    enemy.MaxHealth != spec.MaxHealth ||
                    enemy.BodyClass != spec.BodyClass ||
                    enemy.Intelligence != spec.Intelligence ||
                    enemy.Disposition != spec.Disposition)
                {
                    failures.Add($"{spec.SpawnKind} does not match M84 authored identity/stat defaults.");
                }

                if (enemy.ContactDamagePolicy != EnemyContactDamagePolicy.ActiveOnly ||
                    enemy.PassiveContactHazardType != EnemyPassiveContactHazardType.None)
                {
                    failures.Add($"{spec.SpawnKind} must use ActiveOnly contact policy with no passive hazard.");
                }

                if (Mathf.Abs(enemy.PreferredRangeMinMeters - spec.PreferredRangeMinMeters) > 0.01f ||
                    Mathf.Abs(enemy.PreferredRangeMaxMeters - spec.PreferredRangeMaxMeters) > 0.01f ||
                    Mathf.Abs(enemy.SightRadiusMeters - spec.SightRadiusMeters) > 0.01f ||
                    Mathf.Abs(enemy.SightAngleDegrees - spec.SightAngleDegrees) > 0.01f ||
                    Mathf.Abs(enemy.HearingRadiusMeters - spec.HearingRadiusMeters) > 0.01f)
                {
                    failures.Add($"{spec.SpawnKind} has incorrect range/sense defaults.");
                }
            }
        }

        private static void ValidateAttackProfiles(List<string> failures)
        {
            foreach (var owner in Milestone84AssetGenerator.SpawnKinds)
            {
                var attacks = EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => spec.OwnerId == owner).ToArray();
                if (attacks.Length == 0)
                {
                    failures.Add($"{owner} has no M84 attack profiles.");
                }

                foreach (var attack in attacks)
                {
                    if (attack.RuntimeKind != EnemyAttackRuntimeKind.WeaponMelee &&
                        attack.RuntimeKind != EnemyAttackRuntimeKind.Area &&
                        attack.RuntimeKind != EnemyAttackRuntimeKind.Defense)
                    {
                        failures.Add($"{owner}:{attack.AttackId} has unexpected runtime kind {attack.RuntimeKind}.");
                    }

                    if (attack.RuntimeKind != EnemyAttackRuntimeKind.Defense && attack.Damage <= 0)
                    {
                        failures.Add($"{owner}:{attack.AttackId} must have damage unless it is defense.");
                    }

                    if (!string.IsNullOrWhiteSpace(attack.ComboFollowUpAttackId) &&
                        attacks.All(candidate => candidate.AttackId != attack.ComboFollowUpAttackId))
                    {
                        failures.Add($"{owner}:{attack.AttackId} links unknown combo follow-up `{attack.ComboFollowUpAttackId}`.");
                    }
                }
            }
        }

        private static void ValidateActionProfiles(List<string> failures)
        {
            foreach (var owner in Milestone84AssetGenerator.SpawnKinds)
            {
                var actions = EnemyActionProfileDefaults.AllEnemySpecs.Where(spec => spec.OwnerId == owner).ToArray();
                if (actions.Length < 3)
                {
                    failures.Add($"{owner} should have current and future M81 action coverage.");
                }

                foreach (var action in actions.Where(action => !string.IsNullOrWhiteSpace(action.LinkedAttackId)))
                {
                    if (EnemyAttackProfileDefaults.AllEnemySpecs.All(attack => attack.OwnerId != owner || attack.AttackId != action.LinkedAttackId))
                    {
                        failures.Add($"{owner}:{action.ActionId} links missing attack `{action.LinkedAttackId}`.");
                    }
                }
            }
        }

        private static void ValidateBehaviorTrees(List<string> failures)
        {
            foreach (var owner in Milestone84AssetGenerator.SpawnKinds)
            {
                var tree = EnemyBehaviorTreeDefaults.CreateEnemyTree(owner);
                if (tree == null || tree.RootNode == null || tree.Nodes.Count == 0)
                {
                    failures.Add($"{owner} resolves an invalid M82/M84 behavior tree.");
                }
            }
        }

        private static void ValidateGuards(List<string> failures)
        {
            var knight = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyKnight");
            if (knight?.GuardProfile == null || knight.GuardProfile.ShieldTier != EnemyShieldTier.Medium)
            {
                failures.Add("Knight must resolve the M84 medium shield guard profile.");
            }

            foreach (var tier in new[] { EnemyShieldTier.Small, EnemyShieldTier.Medium, EnemyShieldTier.Heavy })
            {
                var guard = EnemyGuardProfileDefinition.CreateRuntime(tier);
                if (guard.FrontalArcDegrees <= 0f || guard.GuardBreakRecoverySeconds <= 0f)
                {
                    failures.Add($"{tier} guard profile has invalid arc or recovery.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M84 file: {path}");
            }
        }
    }
}
