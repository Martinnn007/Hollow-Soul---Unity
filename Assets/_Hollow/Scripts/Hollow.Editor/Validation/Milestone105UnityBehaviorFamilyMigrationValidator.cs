using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone105UnityBehaviorFamilyMigrationValidator
    {
        private static readonly string[] RequiredDocsText =
        {
            "Unity Behavior Family Migration",
            "Critters",
            "Chasers",
            "Weapon Users",
            "Ranged + Firearm",
            "Magic + Ghost",
            "EnemyActionScorer",
            "Hollow profiles"
        };

        [MenuItem("Hollow/Validation/Run Milestone 105 Unity Behavior Family Migration Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidatePackage(failures);
            ValidateFamilyMigration(failures);
            ValidateFiles(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 105 Unity Behavior family migration validation passed.");
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
                failures.Add("M105 requires Unity Behavior runtime types from the Unity.Behavior assembly.");
            }
        }

        private static void ValidateFamilyMigration(List<string> failures)
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            var migrated = EnemyUnityBehaviorPilotGraphDefinition.MigratedUnityBehaviorSpawnKinds;
            foreach (var spawnKind in migrated)
            {
                var enemy = catalog.Resolve(spawnKind);
                if (enemy == null)
                {
                    failures.Add($"M105 missing migrated enemy `{spawnKind}`.");
                    continue;
                }

                if (enemy.ArchetypeId == EnemyArchetypeId.Boss)
                {
                    failures.Add($"M105 should not migrate boss enemy `{spawnKind}`.");
                    continue;
                }

                if (enemy.BehaviorRuntimeMode != EnemyBehaviorRuntimeMode.UnityBehaviorGraph)
                {
                    failures.Add($"{enemy.DisplayName} `{spawnKind}` must resolve Unity Behavior runtime mode.");
                }

                var graph = enemy.UnityBehaviorGraph;
                if (graph == null)
                {
                    failures.Add($"{enemy.DisplayName} `{spawnKind}` must resolve a Unity Behavior family graph contract.");
                    continue;
                }

                var expectedKind = EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor(spawnKind);
                if (expectedKind == EnemyUnityBehaviorPilotKind.None)
                {
                    failures.Add($"M105 has no family mapping for `{spawnKind}`.");
                    continue;
                }

                if (graph.PilotKind != expectedKind)
                {
                    failures.Add($"{enemy.DisplayName} `{spawnKind}` resolves `{graph.PilotKind}` instead of `{expectedKind}`.");
                }

                if (!EnemyUnityBehaviorBlackboardSchema.TryValidateDefinition(graph, out var schemaFailure))
                {
                    failures.Add($"{enemy.DisplayName} `{spawnKind}` has invalid Unity Behavior schema: {schemaFailure}.");
                }

                if (graph.FallbackPolicy != EnemyUnityBehaviorFallbackPolicy.EmergencyOnly)
                {
                    failures.Add($"{enemy.DisplayName} `{spawnKind}` must use emergency-only fallback policy.");
                }
            }

            foreach (var enemy in catalog.Definitions.Where(enemy => enemy != null && enemy.ArchetypeId != EnemyArchetypeId.Boss))
            {
                if (!migrated.Contains(enemy.SpawnKind))
                {
                    failures.Add($"M105 non-boss enemy `{enemy.SpawnKind}` is not covered by a Unity Behavior family.");
                }
            }

            var boss = catalog.Resolve("spawnEnemyBoss");
            if (boss != null && boss.BehaviorRuntimeMode == EnemyBehaviorRuntimeMode.UnityBehaviorGraph)
            {
                failures.Add("M105 must keep boss runtime behavior exempt from Unity Behavior family migration.");
            }
        }

        private static void ValidateFiles(List<string> failures)
        {
            ExpectFile(Milestone105UnityBehaviorFamilyMigrationAssetGenerator.DocsPath, failures);
            ExpectFile(Milestone105UnityBehaviorFamilyMigrationAssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone105UnityBehaviorFamilyMigrationAssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone105UnityBehaviorFamilyMigrationAssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M105 docs are missing `{required}`.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M105 artifact `{path}`.");
            }
        }
    }
}
