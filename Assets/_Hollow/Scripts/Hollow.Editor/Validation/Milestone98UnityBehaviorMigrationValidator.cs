using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone98UnityBehaviorMigrationValidator
    {
        private static readonly string[] RequiredDocsText =
        {
            "Unity Behavior",
            "EnemyUnityBehaviorGraphBridge",
            "Rat",
            "Skeleton Sword",
            "OutputCommandKind",
            "Hollow remains authoritative"
        };

        [MenuItem("Hollow/Validation/Run Milestone 98 Unity Behavior Migration Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidatePackage(failures);
            ValidateBakeOff(failures);
            ValidatePilotData(failures);
            ValidateFiles(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 98 Unity Behavior migration validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidatePackage(List<string> failures)
        {
            if (!EnemyUnityBehaviorPackageProbe.TypesAvailable ||
                EnemyUnityBehaviorPackageProbe.RuntimeAssemblyName != "Unity.Behavior")
            {
                failures.Add("M98 requires Unity Behavior runtime types from the Unity.Behavior assembly.");
            }
        }

        private static void ValidateBakeOff(List<string> failures)
        {
            var unityBehavior = EnemyAiToolBakeOffEvaluation.Resolve("Unity Behavior");
            if (string.IsNullOrWhiteSpace(unityBehavior.Name) ||
                unityBehavior.RequiresPurchase ||
                !unityBehavior.Role.Contains("official Unity", System.StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("M98 must add Unity Behavior to the M96 bake-off as a free official graph/runtime candidate.");
            }
        }

        private static void ValidatePilotData(List<string> failures)
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            ValidatePilotEnemy(catalog.Resolve("spawnEnemyRat"), EnemyUnityBehaviorPilotKind.CritterFamily, failures);
            ValidatePilotEnemy(catalog.Resolve("spawnEnemySkeletonSword"), EnemyUnityBehaviorPilotKind.WeaponUserFamily, failures);

            foreach (var enemy in catalog.Definitions.Where(enemy => enemy != null && enemy.ArchetypeId == EnemyArchetypeId.Boss))
            {
                if (enemy.BehaviorRuntimeMode == EnemyBehaviorRuntimeMode.UnityBehaviorGraph)
                {
                    failures.Add($"Boss runtime should remain exempt from Unity Behavior migration; `{enemy.SpawnKind}` is migrated.");
                }
            }
        }

        private static void ValidatePilotEnemy(
            EnemyDefinition enemy,
            EnemyUnityBehaviorPilotKind expectedPilotKind,
            List<string> failures)
        {
            if (enemy == null)
            {
                failures.Add($"Missing M98 pilot enemy `{expectedPilotKind}`.");
                return;
            }

            if (enemy.BehaviorRuntimeMode != EnemyBehaviorRuntimeMode.UnityBehaviorGraph)
            {
                failures.Add($"{enemy.DisplayName} must resolve Unity Behavior runtime mode.");
            }

            if (enemy.UnityBehaviorGraph == null ||
                enemy.UnityBehaviorGraph.PilotKind != expectedPilotKind)
            {
                failures.Add($"{enemy.DisplayName} must resolve an M98 Unity Behavior pilot graph for `{expectedPilotKind}`.");
            }
        }

        private static void ValidateFiles(List<string> failures)
        {
            ExpectFile(Milestone98UnityBehaviorMigrationAssetGenerator.DocsPath, failures);
            ExpectFile(Milestone98UnityBehaviorMigrationAssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone98UnityBehaviorMigrationAssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone98UnityBehaviorMigrationAssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M98 docs are missing `{required}`.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M98 artifact `{path}`.");
            }
        }
    }
}
