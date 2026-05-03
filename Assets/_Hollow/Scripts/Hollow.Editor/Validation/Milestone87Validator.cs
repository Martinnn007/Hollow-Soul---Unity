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
    public static class Milestone87Validator
    {
        private static readonly string[] RequiredText =
        {
            "Magic/Ghost/Soul Enemies",
            "Hollow Acolyte",
            "Wraith",
            "Soul Eater",
            "Curse Binder",
            "Grave Lantern",
            "Beam",
            "PhaseMove",
            "curse",
            "M88"
        };

        [MenuItem("Hollow/Validation/Run Milestone 87 Validation")]
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
                Debug.Log("Milestone 87 validation passed.");
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
            ExpectFile(Milestone87AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone87AssetGenerator.ReportPath, failures);
            ExpectFile(Milestone87AssetGenerator.PdfPath, failures);
            ExpectFile(Milestone87AssetGenerator.GeneratorScriptPath, failures);
            ExpectFile(Milestone87AssetGenerator.VerifyScriptPath, failures);
            if (!File.Exists(Milestone87AssetGenerator.DocsPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone87AssetGenerator.DocsPath);
            foreach (var required in RequiredText)
            {
                if (markdown.IndexOf(required, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add($"M87 documentation is missing `{required}`.");
                }
            }
        }

        private static void ValidateRuntimeRoster(List<string> failures)
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spec in Milestone87AssetGenerator.EnemyRows())
            {
                var enemy = catalog.Resolve(spec.SpawnKind);
                if (enemy == null)
                {
                    failures.Add($"M87 enemy `{spec.SpawnKind}` is missing from runtime catalog.");
                    continue;
                }

                if (enemy.DisplayName != spec.DisplayName ||
                    enemy.BehaviorId != spec.BehaviorId ||
                    enemy.MovementMode != spec.MovementMode ||
                    enemy.MaxHealth != spec.MaxHealth ||
                    enemy.BodyClass != spec.BodyClass ||
                    enemy.Intelligence != spec.Intelligence ||
                    enemy.Disposition != spec.Disposition)
                {
                    failures.Add($"{spec.SpawnKind} does not match M87 authored identity/stat defaults.");
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
                    failures.Add($"{spec.SpawnKind} must not fall back to generic lunge pressure in M87.");
                }

                if (enemy.AttackProfiles.Count == 0 || enemy.ActionProfiles.Count == 0 || enemy.BehaviorTree == null)
                {
                    failures.Add($"{spec.SpawnKind} is missing profiles or behavior tree metadata.");
                }
            }
        }

        private static void ValidateAttackProfiles(List<string> failures)
        {
            foreach (var spec in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => Milestone87AssetGenerator.SpawnKinds.Contains(spec.OwnerId)))
            {
                if (spec.RuntimeKind is not (EnemyAttackRuntimeKind.Projectile
                    or EnemyAttackRuntimeKind.FanProjectile
                    or EnemyAttackRuntimeKind.RadialProjectile
                    or EnemyAttackRuntimeKind.Area
                    or EnemyAttackRuntimeKind.Beam
                    or EnemyAttackRuntimeKind.PhaseMove
                    or EnemyAttackRuntimeKind.MeleeLunge))
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} has unexpected M87 runtime kind {spec.RuntimeKind}.");
                }

                if (spec.RuntimeKind == EnemyAttackRuntimeKind.PhaseMove && spec.Damage != 0)
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} phase movement must be non-damaging.");
                }

                if (spec.RuntimeKind != EnemyAttackRuntimeKind.PhaseMove && spec.Damage <= 0)
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} must be a damaging explicit magic/ghost action.");
                }

                if (spec.DamageChannel != DamageChannel.Elemental ||
                    spec.DamageElement is not (DamageElement.Soul or DamageElement.Cursed))
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} must carry Soul or Cursed elemental identity in M87.");
                }

                if (spec.RuntimeKind == EnemyAttackRuntimeKind.Beam &&
                    (spec.DamageDelivery != DamageDelivery.Area || spec.HitArcDegrees > 90f))
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} beam profiles must be narrow area/lane damage.");
                }

                if (spec.RuntimeKind == EnemyAttackRuntimeKind.RadialProjectile && spec.ProjectileCount < 6)
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} radial magic profile needs at least six projectiles.");
                }

                if (spec.RuntimeKind == EnemyAttackRuntimeKind.FanProjectile && spec.ProjectileCount < 3)
                {
                    failures.Add($"{spec.OwnerId}:{spec.AttackId} fan magic profile needs at least three projectiles.");
                }
            }
        }

        private static void ValidateBehaviorTrees(List<string> failures)
        {
            foreach (var owner in Milestone87AssetGenerator.SpawnKinds)
            {
                var tree = EnemyBehaviorTreeDefaults.CreateEnemyTree(owner);
                if (tree == null || tree.RootNode == null || tree.Nodes.Count == 0)
                {
                    failures.Add($"{owner} resolves an invalid M87 behavior tree.");
                    continue;
                }

                var actions = tree.Nodes
                    .OfType<EnemyBehaviorActionNodeDefinition>()
                    .Where(action => action.CommandKind is EnemyBehaviorCommandKind.StartRangedAction or EnemyBehaviorCommandKind.StartAreaAction or EnemyBehaviorCommandKind.StartCreatureMoveAction or EnemyBehaviorCommandKind.StartMeleeAction)
                    .Select(action => action.ActionId)
                    .Where(actionId => !string.IsNullOrWhiteSpace(actionId))
                    .Distinct()
                    .ToArray();
                if (actions.Length == 0)
                {
                    failures.Add($"{owner} must start at least one profile-specific M87 action.");
                }

                foreach (var actionId in actions)
                {
                    var profile = EnemyAttackProfileDefaults.AllEnemySpecs.FirstOrDefault(spec => spec.OwnerId == owner && spec.AttackId == actionId);
                    if (string.IsNullOrWhiteSpace(profile.AttackId))
                    {
                        failures.Add($"{owner} tree references missing M87 profile `{actionId}`.");
                    }
                }
            }
        }

        private static void ValidateCuratedRooms(List<string> failures)
        {
            foreach (var roomId in Milestone87AssetGenerator.MagicRoomIds)
            {
                ExpectFile($"{Milestone87AssetGenerator.MagicRoomDirectory}/{roomId}.hollowruntime.json", failures);
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M87 file: {path}");
            }
        }
    }
}
