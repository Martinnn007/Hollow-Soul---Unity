using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Combat.UnityBehaviorNodes;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone106BehaviorGraphSubgraphLibraryValidator
    {
        private static readonly string[] RequiredDocsText =
        {
            "Behavior Graph Subgraph Library",
            "notice player",
            "investigate noise",
            "flee",
            "circle",
            "approach action range",
            "request attack slot",
            "start action",
            "recover/hold",
            "EnemyActionScorer"
        };

        [MenuItem("Hollow/Validation/Run Milestone 106 Behavior Graph Subgraph Library Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidatePackage(failures);
            ValidateNodeWrappers(failures);
            ValidateSubgraphs(failures);
            ValidateFiles(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 106 Behavior Graph subgraph library validation passed.");
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
                failures.Add("M106 requires Unity Behavior runtime types from the Unity.Behavior assembly.");
            }
        }

        private static void ValidateNodeWrappers(List<string> failures)
        {
            ExpectType(typeof(HollowEnemyAlertedCondition), "HollowEnemyAlertedCondition", failures);
            ExpectType(typeof(HollowEnemyNoticePlayerAction), "HollowEnemyNoticePlayerAction", failures);
            ExpectType(typeof(HollowEnemyInvestigateNoiseAction), "HollowEnemyInvestigateNoiseAction", failures);
            ExpectType(typeof(HollowEnemyFleeAction), "HollowEnemyFleeAction", failures);
            ExpectType(typeof(HollowEnemyCircleAction), "HollowEnemyCircleAction", failures);
            ExpectType(typeof(HollowEnemyChaseApproachAction), "HollowEnemyChaseApproachAction", failures);
            ExpectType(typeof(HollowEnemyRequestAttackSlotAction), "HollowEnemyRequestAttackSlotAction", failures);
            ExpectType(typeof(HollowEnemyStartLinkedAction), "HollowEnemyStartLinkedAction", failures);
            ExpectType(typeof(HollowEnemyRecoverHoldAction), "HollowEnemyRecoverHoldAction", failures);
            ExpectType(typeof(HollowEnemyHoldFaceAction), "HollowEnemyHoldFaceAction", failures);
        }

        private static void ExpectType(System.Type type, string label, List<string> failures)
        {
            if (type == null)
            {
                failures.Add($"Missing M106 Unity Behavior node wrapper `{label}`.");
            }
        }

        private static void ValidateSubgraphs(List<string> failures)
        {
            var ids = new HashSet<string>();
            foreach (var spec in Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.SubgraphSpecs)
            {
                var path = $"{Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.DataFolder}/{spec.FileName}";
                var subgraph = AssetDatabase.LoadAssetAtPath<EnemyUnityBehaviorSubgraphDefinition>(path);
                if (subgraph == null)
                {
                    failures.Add($"Missing M106 subgraph asset `{path}`.");
                    continue;
                }

                if (!ids.Add(subgraph.SubgraphId))
                {
                    failures.Add($"Duplicate M106 subgraph id `{subgraph.SubgraphId}`.");
                }

                if (subgraph.SubgraphId != spec.SubgraphId)
                {
                    failures.Add($"{path} has id `{subgraph.SubgraphId}` instead of `{spec.SubgraphId}`.");
                }

                if (subgraph.Kind != spec.Kind)
                {
                    failures.Add($"{subgraph.DisplayName} has kind `{subgraph.Kind}` instead of `{spec.Kind}`.");
                }

                if (subgraph.OutputCommandKind != spec.OutputCommandKind)
                {
                    failures.Add($"{subgraph.DisplayName} outputs `{subgraph.OutputCommandKind}` instead of `{spec.OutputCommandKind}`.");
                }

                if (!ContainsAll(subgraph.RequiredBlackboardInputs, EnemyUnityBehaviorBlackboardSchema.RequiredInputNames, out var missingInput))
                {
                    failures.Add($"{subgraph.DisplayName} is missing blackboard input `{missingInput}`.");
                }

                if (!ContainsAll(subgraph.RequiredBlackboardOutputs, EnemyUnityBehaviorBlackboardSchema.RequiredOutputNames, out var missingOutput))
                {
                    failures.Add($"{subgraph.DisplayName} is missing blackboard output `{missingOutput}`.");
                }

                foreach (var nodeName in spec.RequiredNodeNames)
                {
                    if (!subgraph.RequiredNodeNames.Contains(nodeName))
                    {
                        failures.Add($"{subgraph.DisplayName} is missing required node `{nodeName}`.");
                    }
                }
            }

            if (Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.SubgraphSpecs.Count != 8)
            {
                failures.Add("M106 must define the eight requested reusable subgraphs.");
            }
        }

        private static bool ContainsAll(IReadOnlyList<string> values, IReadOnlyList<string> required, out string missing)
        {
            missing = string.Empty;
            if (values == null)
            {
                missing = required.Count > 0 ? required[0] : string.Empty;
                return false;
            }

            foreach (var requiredValue in required)
            {
                if (!values.Contains(requiredValue))
                {
                    missing = requiredValue;
                    return false;
                }
            }

            return true;
        }

        private static void ValidateFiles(List<string> failures)
        {
            ExpectFile(Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.DocsPath, failures);
            ExpectFile(Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone106BehaviorGraphSubgraphLibraryAssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required, System.StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"M106 docs are missing `{required}`.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M106 artifact `{path}`.");
            }
        }
    }
}
