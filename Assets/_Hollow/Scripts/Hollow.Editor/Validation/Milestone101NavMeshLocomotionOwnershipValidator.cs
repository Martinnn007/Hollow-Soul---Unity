using System.Collections.Generic;
using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone101NavMeshLocomotionOwnershipValidator
    {
        [MenuItem("Hollow/Validation/Validate Milestone 101 NavMesh Locomotion Ownership")]
        public static void Validate()
        {
            var failures = CollectFailures();
            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("M101 validation failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log("M101 NavMesh locomotion ownership validation passed.");
        }

        public static List<string> CollectFailures()
        {
            var failures = new List<string>();
            if (!File.Exists(Milestone101NavMeshLocomotionOwnershipAssetGenerator.DocsPath))
            {
                failures.Add($"Missing docs: {Milestone101NavMeshLocomotionOwnershipAssetGenerator.DocsPath}.");
            }

            if (!File.Exists(Milestone101NavMeshLocomotionOwnershipAssetGenerator.ReportPath))
            {
                failures.Add($"Missing report: {Milestone101NavMeshLocomotionOwnershipAssetGenerator.ReportPath}.");
            }

            var bridgePath = "Assets/_Hollow/Scripts/Hollow.Combat/EnemyNavMeshAgentBridge.cs";
            var runtimePath = "Assets/_Hollow/Scripts/Hollow.Combat/EnemyRuntimeController.cs";
            var knockbackPath = "Assets/_Hollow/Scripts/Hollow.Combat/CombatKnockbackReceiver.cs";
            if (File.Exists(bridgePath))
            {
                var bridge = File.ReadAllText(bridgePath);
                if (!bridge.Contains("NavMeshAgent.Move", System.StringComparison.Ordinal) && !bridge.Contains("agent.Move", System.StringComparison.Ordinal))
                {
                    failures.Add("EnemyNavMeshAgentBridge must use NavMeshAgent.Move for agent-owned locomotion.");
                }

                if (!bridge.Contains("CurrentOwnership", System.StringComparison.Ordinal) ||
                    !bridge.Contains("SyncAfterHollowOwnedMotion", System.StringComparison.Ordinal))
                {
                    failures.Add("EnemyNavMeshAgentBridge must expose ownership and sync diagnostics.");
                }
            }

            if (File.Exists(runtimePath))
            {
                var runtime = File.ReadAllText(runtimePath);
                if (runtime.Contains("transform.localPosition = ResolveNavigationMove(", System.StringComparison.Ordinal))
                {
                    failures.Add("EnemyRuntimeController must apply ResolveNavigationMove through ApplyNavigationMove so agent sync can run.");
                }

                if (!runtime.Contains("SyncNavMeshAgentAfterExternalDisplacement", System.StringComparison.Ordinal))
                {
                    failures.Add("EnemyRuntimeController must expose external displacement sync for knockback.");
                }
            }

            if (File.Exists(knockbackPath) &&
                !File.ReadAllText(knockbackPath).Contains("SyncNavMeshAgentAfterExternalDisplacement", System.StringComparison.Ordinal))
            {
                failures.Add("CombatKnockbackReceiver must sync enemy NavMesh agents during knockback.");
            }

            return failures;
        }
    }
}
