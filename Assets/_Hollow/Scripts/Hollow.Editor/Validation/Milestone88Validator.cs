using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone88Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 88 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateAdapterContract(failures);
            ValidateRosterModes(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 88 validation passed.");
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
            ExpectFile(Milestone88AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone88AssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone88AssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone88AssetGenerator.DocsPath);
            foreach (var required in new[] { "Navigation Adapter", "LocalSteering", "no full pathfinding", "PhaseMove", "M89", "Movement Intent Table" })
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M88 docs are missing `{required}`.");
                }
            }
        }

        private static void ValidateAdapterContract(List<string> failures)
        {
            if (EnemyNavigationAdapter.CurrentBackend != EnemyNavigationBackend.LocalSteering &&
                EnemyNavigationAdapter.CurrentBackend != EnemyNavigationBackend.UnityNavMesh)
            {
                failures.Add("M88 expects LocalSteering, or the later M97 UnityNavMesh replacement backend.");
            }

            if (EnemyNavigationAdapter.DefaultModeFor(EnemyMovementMode.Grounded) != EnemyNavigationMode.GroundedLocal ||
                EnemyNavigationAdapter.DefaultModeFor(EnemyMovementMode.Flying) != EnemyNavigationMode.FlyingLocal)
            {
                failures.Add("M88 default navigation modes do not match movement modes.");
            }
        }

        private static void ValidateRosterModes(List<string> failures)
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null))
            {
                var mode = EnemyNavigationAdapter.DefaultModeFor(enemy.MovementMode);
                if (enemy.MovementMode == EnemyMovementMode.Flying && mode != EnemyNavigationMode.FlyingLocal)
                {
                    failures.Add($"{enemy.SpawnKind} should resolve flying navigation mode.");
                }

                if (enemy.MovementMode == EnemyMovementMode.Grounded && mode != EnemyNavigationMode.GroundedLocal)
                {
                    failures.Add($"{enemy.SpawnKind} should resolve grounded navigation mode.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M88 file: {path}");
            }
        }
    }
}
