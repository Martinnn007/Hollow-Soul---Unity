using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone104UnityBehaviorPilotHardeningValidator
    {
        private static readonly string[] RequiredDocsText =
        {
            "Unity Behavior Pilot Hardening",
            "Stable Blackboard Schema",
            "Emergency fallback",
            "Rat",
            "Skeleton Sword",
            "OutputCommandKind",
            "TraceHistory"
        };

        [MenuItem("Hollow/Validation/Run Milestone 104 Unity Behavior Pilot Hardening Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidatePackage(failures);
            ValidatePilotData(failures);
            ValidateRuntimeBridge(failures);
            ValidateFiles(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 104 Unity Behavior pilot hardening validation passed.");
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
                failures.Add("M104 requires Unity Behavior runtime types from the Unity.Behavior assembly.");
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
                failures.Add($"Missing M104 pilot enemy `{expectedPilotKind}`.");
                return;
            }

            if (enemy.BehaviorRuntimeMode != EnemyBehaviorRuntimeMode.UnityBehaviorGraph)
            {
                failures.Add($"{enemy.DisplayName} must resolve Unity Behavior runtime mode.");
            }

            var graph = enemy.UnityBehaviorGraph;
            if (graph == null)
            {
                failures.Add($"{enemy.DisplayName} must resolve an M104 Unity Behavior graph contract.");
                return;
            }

            if (graph.PilotKind != expectedPilotKind)
            {
                failures.Add($"{enemy.DisplayName} resolves `{graph.PilotKind}` instead of `{expectedPilotKind}`.");
            }

            if (graph.SchemaVersion < EnemyUnityBehaviorBlackboardSchema.SchemaVersion)
            {
                failures.Add($"{enemy.DisplayName} Unity Behavior schema is `{graph.SchemaVersion}`, expected `{EnemyUnityBehaviorBlackboardSchema.SchemaVersion}`.");
            }

            if (!graph.RequiresOfficialBehaviorGraph)
            {
                failures.Add($"{enemy.DisplayName} must require an official Unity Behavior graph asset, with fallback only as an emergency guard.");
            }

            if (graph.FallbackPolicy != EnemyUnityBehaviorFallbackPolicy.EmergencyOnly)
            {
                failures.Add($"{enemy.DisplayName} must use emergency-only fallback policy.");
            }

            if (!EnemyUnityBehaviorBlackboardSchema.TryValidateDefinition(graph, out var schemaFailure))
            {
                failures.Add($"{enemy.DisplayName} has invalid Unity Behavior schema: {schemaFailure}.");
            }
        }

        private static void ValidateRuntimeBridge(List<string> failures)
        {
            var bridgeType = typeof(EnemyUnityBehaviorGraphBridge);
            if (bridgeType.GetProperty(nameof(EnemyUnityBehaviorGraphBridge.TraceHistory)) == null)
            {
                failures.Add("EnemyUnityBehaviorGraphBridge must expose trace history for play-mode debug.");
            }

            if (bridgeType.GetProperty(nameof(EnemyUnityBehaviorGraphBridge.UsedEmergencyFallbackLastEvaluation)) == null)
            {
                failures.Add("EnemyUnityBehaviorGraphBridge must expose whether emergency fallback was used last evaluation.");
            }

            if (bridgeType.GetProperty(nameof(EnemyUnityBehaviorGraphBridge.LastOfficialGraphFailureReason)) == null)
            {
                failures.Add("EnemyUnityBehaviorGraphBridge must expose official graph failure reason.");
            }
        }

        private static void ValidateFiles(List<string> failures)
        {
            ExpectFile(Milestone104UnityBehaviorPilotHardeningAssetGenerator.DocsPath, failures);
            ExpectFile(Milestone104UnityBehaviorPilotHardeningAssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone104UnityBehaviorPilotHardeningAssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone104UnityBehaviorPilotHardeningAssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M104 docs are missing `{required}`.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M104 artifact `{path}`.");
            }
        }
    }
}
