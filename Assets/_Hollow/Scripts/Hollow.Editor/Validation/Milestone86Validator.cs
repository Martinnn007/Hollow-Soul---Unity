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
    public static class Milestone86Validator
    {
        private static readonly string[] RequiredText =
        {
            "Ranged + Firearm Enemies",
            "Hollow Archer",
            "Powder Gunner",
            "Knife Thrower",
            "Repeater Turret",
            "Clockwork Sentry",
            "fan",
            "radial",
            "active window"
        };

        [MenuItem("Hollow/Validation/Run Milestone 86 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateRuntimeRoster(failures);
            ValidateAttackProfiles(failures);
            ValidateBehaviorTrees(failures);
            ValidateCuratedRooms(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 86 validation passed.");
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
            ExpectFile(Milestone86AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone86AssetGenerator.ReportPath, failures);
            ExpectFile(Milestone86AssetGenerator.PdfPath, failures);
            ExpectFile(Milestone86AssetGenerator.GeneratorScriptPath, failures);
            ExpectFile(Milestone86AssetGenerator.VerifyScriptPath, failures);
            if (!File.Exists(Milestone86AssetGenerator.DocsPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone86AssetGenerator.DocsPath);
            foreach (var required in RequiredText)
            {
                if (markdown.IndexOf(required, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add($"M86 documentation is missing `{required}`.");
                }
            }
        }

        private static void ValidateRuntimeRoster(List<string> failures)
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spec in Milestone86AssetGenerator.EnemyRows())
            {
                var enemy = catalog.Resolve(spec.SpawnKind);
                if (enemy == null)
                {
                    failures.Add($"M86 enemy `{spec.SpawnKind}` is missing from runtime catalog.");
                    continue;
                }

                if (enemy.DisplayName != spec.DisplayName ||
                    enemy.BehaviorId != spec.BehaviorId ||
                    enemy.MaxHealth != spec.MaxHealth ||
                    enemy.BodyClass != spec.BodyClass ||
                    enemy.Intelligence != spec.Intelligence ||
                    enemy.Disposition != spec.Disposition)
                {
                    failures.Add($"{spec.SpawnKind} does not match M86 authored identity/stat defaults.");
                }

                if (enemy.ContactDamagePolicy != EnemyContactDamagePolicy.ActiveOnly ||
                    enemy.PassiveContactHazardType != EnemyPassiveContactHazardType.None)
                {
                    failures.Add($"{spec.SpawnKind} must use ActiveOnly contact policy with no passive hazard.");
                }

                if (Mathf.Abs(enemy.SpeedMetersPerSecond - spec.SpeedMetersPerSecond) > 0.01f ||
                    Mathf.Abs(enemy.RadiusMeters - spec.RadiusMeters) > 0.01f ||
                    Mathf.Abs(enemy.AttackRangeMeters - spec.AttackRangeMeters) > 0.01f ||
                    Mathf.Abs(enemy.PreferredRangeMinMeters - spec.PreferredRangeMinMeters) > 0.01f ||
                    Mathf.Abs(enemy.PreferredRangeMaxMeters - spec.PreferredRangeMaxMeters) > 0.01f ||
                    Mathf.Abs(enemy.SightRadiusMeters - spec.SightRadiusMeters) > 0.01f ||
                    Mathf.Abs(enemy.SightAngleDegrees - spec.SightAngleDegrees) > 0.01f ||
                    Mathf.Abs(enemy.HearingRadiusMeters - spec.HearingRadiusMeters) > 0.01f)
                {
                    failures.Add($"{spec.SpawnKind} has incorrect movement/range/sense defaults.");
                }

                if (enemy.LungeAttackEnabled)
                {
                    failures.Add($"{spec.SpawnKind} must not fall back to contact/lunge pressure in M86.");
                }

                if (enemy.AttackProfiles.Count == 0 || enemy.ActionProfiles.Count == 0 || enemy.BehaviorTree == null)
                {
                    failures.Add($"{spec.SpawnKind} is missing profiles or behavior tree metadata.");
                }
            }
        }

        private static void ValidateAttackProfiles(List<string> failures)
        {
            foreach (var spec in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => Milestone86AssetGenerator.SpawnKinds.Contains(spec.OwnerId)))
            {
                if (spec.RuntimeKind is not (EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile or EnemyAttackRuntimeKind.CreatureMove))
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} has unexpected M86 runtime kind {spec.RuntimeKind}.");
                }

                if (spec.RuntimeKind != EnemyAttackRuntimeKind.CreatureMove && spec.Damage <= 0)
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} must be a damaging ranged profile.");
                }

                if (spec.RuntimeKind == EnemyAttackRuntimeKind.CreatureMove && spec.Damage != 0)
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} movement profile must be non-damaging.");
                }

                if (spec.DamageChannel != DamageChannel.Physical ||
                    spec.DamageDelivery != (spec.RuntimeKind == EnemyAttackRuntimeKind.CreatureMove ? DamageDelivery.Area : DamageDelivery.Projectile) ||
                    spec.DamageElement != DamageElement.None)
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} must remain physical projectile/no-element in M86.");
                }

                if (spec.RuntimeKind == EnemyAttackRuntimeKind.FanProjectile && spec.ProjectileCount < 3)
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} fan profile needs at least three projectiles.");
                }

                if (spec.RuntimeKind == EnemyAttackRuntimeKind.RadialProjectile && spec.ProjectileCount < 6)
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} radial profile needs at least six projectiles.");
                }
            }
        }

        private static void ValidateBehaviorTrees(List<string> failures)
        {
            foreach (var owner in Milestone86AssetGenerator.SpawnKinds)
            {
                var tree = EnemyBehaviorTreeDefaults.CreateEnemyTree(owner);
                if (tree == null || tree.RootNode == null || tree.Nodes.Count == 0)
                {
                    failures.Add($"{owner} resolves an invalid M86 behavior tree.");
                    continue;
                }

                var rangedActions = tree.Nodes
                    .OfType<EnemyBehaviorActionNodeDefinition>()
                    .Where(action => action.CommandKind == EnemyBehaviorCommandKind.StartRangedAction)
                    .Select(action => action.ActionId)
                    .ToArray();
                if (rangedActions.Length == 0)
                {
                    failures.Add($"{owner} must start at least one profile-specific ranged action.");
                }

                foreach (var actionId in rangedActions)
                {
                    var profile = EnemyAttackProfileDefaults.AllEnemySpecs.FirstOrDefault(spec => spec.OwnerId == owner && spec.AttackId == actionId);
                    if (string.IsNullOrWhiteSpace(profile.AttackId))
                    {
                        failures.Add($"{owner} tree references missing ranged profile `{actionId}`.");
                    }
                }
            }
        }

        private static void ValidateCuratedRooms(List<string> failures)
        {
            foreach (var roomId in Milestone86AssetGenerator.RangedRoomIds)
            {
                ExpectFile($"{Milestone86AssetGenerator.RangedRoomDirectory}/{roomId}.hollowruntime.json", failures);
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M86 file: {path}");
            }
        }
    }
}
