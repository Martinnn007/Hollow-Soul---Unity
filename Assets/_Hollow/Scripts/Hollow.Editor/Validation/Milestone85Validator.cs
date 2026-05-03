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
    public static class Milestone85Validator
    {
        private static readonly string[] RequiredText =
        {
            "Creature Action Expansion",
            "Hollow Bird",
            "Hollow Beast",
            "Rat",
            "Spider",
            "signal",
            "swoop",
            "leap",
            "active window"
        };

        [MenuItem("Hollow/Validation/Run Milestone 85 Validation")]
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
                Debug.Log("Milestone 85 validation passed.");
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
            ExpectFile(Milestone85AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone85AssetGenerator.ReportPath, failures);
            ExpectFile(Milestone85AssetGenerator.PdfPath, failures);
            ExpectFile(Milestone85AssetGenerator.GeneratorScriptPath, failures);
            ExpectFile(Milestone85AssetGenerator.VerifyScriptPath, failures);
            if (!File.Exists(Milestone85AssetGenerator.DocsPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone85AssetGenerator.DocsPath);
            foreach (var required in RequiredText)
            {
                if (markdown.IndexOf(required, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add($"M85 documentation is missing `{required}`.");
                }
            }
        }

        private static void ValidateRuntimeRoster(List<string> failures)
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spec in Milestone85AssetGenerator.NewCreatureRows())
            {
                var enemy = catalog.Resolve(spec.SpawnKind);
                if (enemy == null)
                {
                    failures.Add($"M85 enemy `{spec.SpawnKind}` is missing from runtime catalog.");
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
                    failures.Add($"{spec.SpawnKind} does not match M85 authored identity/stat defaults.");
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
            foreach (var actionId in Milestone85AssetGenerator.PromotedCreatureActionIds)
            {
                var matches = EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(spec => spec.AttackId == actionId && Milestone85AssetGenerator.BodyCreatureSpawnKinds.Contains(spec.OwnerId))
                    .ToArray();
                if (matches.Length == 0)
                {
                    failures.Add($"M85 promoted action `{actionId}` has no attack profile.");
                    continue;
                }

                foreach (var spec in matches)
                {
                    if (spec.DamageChannel != DamageChannel.Physical ||
                        spec.DamageElement != DamageElement.None)
                    {
                        failures.Add($"{spec.OwnerId}:{spec.AttackId} must remain physical/no-element in M85.");
                    }

                    if (spec.RuntimeKind is not (EnemyAttackRuntimeKind.MeleeLunge or EnemyAttackRuntimeKind.Area or EnemyAttackRuntimeKind.CreatureMove or EnemyAttackRuntimeKind.CreatureSignal))
                    {
                        failures.Add($"{spec.OwnerId}:{spec.AttackId} has unexpected M85 runtime kind {spec.RuntimeKind}.");
                    }

                    if ((spec.RuntimeKind == EnemyAttackRuntimeKind.CreatureMove || spec.RuntimeKind == EnemyAttackRuntimeKind.CreatureSignal) && spec.Damage != 0)
                    {
                        failures.Add($"{spec.OwnerId}:{spec.AttackId} movement/signal actions must be non-damaging.");
                    }
                }
            }
        }

        private static void ValidateBehaviorTrees(List<string> failures)
        {
            foreach (var owner in Milestone85AssetGenerator.BodyCreatureSpawnKinds)
            {
                var tree = EnemyBehaviorTreeDefaults.CreateEnemyTree(owner);
                if (tree == null || tree.RootNode == null || tree.Nodes.Count == 0)
                {
                    failures.Add($"{owner} resolves an invalid creature behavior tree.");
                    continue;
                }

                var actionIds = tree.Nodes
                    .OfType<EnemyBehaviorActionNodeDefinition>()
                    .Where(action => action.CommandKind is EnemyBehaviorCommandKind.StartCreatureMoveAction or EnemyBehaviorCommandKind.StartCreatureSignalAction)
                    .Select(action => action.ActionId)
                    .ToArray();
                if (owner is "spawnEnemyRat" or "spawnEnemySpider" or "spawnEnemyHollowBird" or "spawnEnemyHollowBeast" &&
                    actionIds.Length == 0)
                {
                    failures.Add($"{owner} must use at least one M85 creature movement/signal command.");
                }
            }
        }

        private static void ValidateCuratedRooms(List<string> failures)
        {
            foreach (var roomId in Milestone85AssetGenerator.CreatureRoomIds)
            {
                ExpectFile($"{Milestone85AssetGenerator.CreatureRoomDirectory}/{roomId}.hollowruntime.json", failures);
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M85 file: {path}");
            }
        }
    }
}
