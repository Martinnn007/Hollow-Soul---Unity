using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone101NavMeshLocomotionOwnershipAssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M101_NavMesh_Locomotion_Ownership.md";
        public const string ReportPath = "output/reports/m101_navmesh_locomotion_ownership.md";

        [MenuItem("Hollow/Generation/Generate Milestone 101 NavMesh Locomotion Ownership Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteDocs();
            WriteReport();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M101 NavMesh locomotion ownership docs and report.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M101: NavMesh Locomotion Ownership V1");
            builder.AppendLine();
            builder.AppendLine("M101 defines the movement contract between Unity NavMesh and Hollow combat execution.");
            builder.AppendLine();
            builder.AppendLine("## Ownership Contract");
            builder.AppendLine();
            builder.AppendLine("- Unity `NavMeshAgent` owns grounded non-boss locomotion during normal movement states: approach, spacing, flee, wander, investigate, and return-home.");
            builder.AppendLine("- Hollow owns committed combat motion: windups, active lunges, charges, creature bursts, recovery movement, bump separation, knockback, death, and disabled/stationary states.");
            builder.AppendLine("- Hollow-owned motion stops or clears the agent path first, moves through Hollow collision rules, then syncs/warps the agent back to the enemy transform.");
            builder.AppendLine("- Agent-owned movement uses `NavMeshAgent.Move` and returns the agent-owned next position to the runtime controller.");
            builder.AppendLine("- Damage still lands only through M79/M80 active windows; M101 changes ownership and synchronization, not attack balance.");
            builder.AppendLine();
            builder.AppendLine("## Debug Contract");
            builder.AppendLine();
            builder.AppendLine("- `EnemyNavMeshAgentBridge.CurrentOwnership` records `UnityNavMeshAgent`, `HollowManual`, or `Disabled`.");
            builder.AppendLine("- `LastOwnershipReason`, `LastSyncReason`, and `SyncToTransformCount` expose handoff reasons for tests and debug overlays.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, @"# M101 NavMesh Locomotion Ownership Report

- Added `EnemyLocomotionOwnership`.
- `EnemyNavMeshAgentBridge` now tracks ownership, sync reasons, and sync count.
- Agent-owned movement uses `NavMeshAgent.Move`.
- `EnemyRuntimeController` applies navigation moves through one helper and syncs after Hollow-owned movement.
- Knockback now stops/syncs enemy NavMesh agents while Hollow displacement owns motion.
- Committed attacks, bump separation, recovery movement, death, and disabled states remain Hollow-owned.
");
        }
    }
}
