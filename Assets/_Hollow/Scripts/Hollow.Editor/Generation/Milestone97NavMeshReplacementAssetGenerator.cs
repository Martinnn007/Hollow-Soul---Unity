using System.IO;
using System.Text;
using Hollow.Combat;
using Hollow.Editor.Navigation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone97NavMeshReplacementAssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M97_Unity_NavMesh_Navigation_Replacement.md";
        public const string ReportPath = "output/reports/m97_unity_navmesh_navigation_replacement.md";

        [MenuItem("Hollow/Generation/Generate Milestone 97 Unity NavMesh Replacement Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteDocs();
            WriteReport();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M97 Unity NavMesh replacement docs and report.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M97: Unity NavMesh Navigation Replacement V1");
            builder.AppendLine();
            builder.AppendLine("M97 replaces Hollow's runtime grounded enemy pathfinding with native Unity NavMesh. Hollow still owns combat intent, tactical threat slots, active hit windows, action scoring, pressure, and boss exemptions.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine($"- Current navigation backend: `{EnemyNavigationAdapter.CurrentBackend}`.");
            builder.AppendLine("- Mobile grounded non-boss enemies use `NavMeshAgent` through `EnemyNavMeshAgentBridge`.");
            builder.AppendLine("- `EnemyNavigationAdapter` no longer calls `RoomGridAStarPathfinder` at runtime.");
            builder.AppendLine("- Grounded locomotion destinations target M91/M96 action/reservation positions rather than the player center.");
            builder.AppendLine("- Active attacks, lunges, charge movement, stun, death, flying, phase movement, stationary enemies, and bosses remain exempt from agent-driven locomotion.");
            builder.AppendLine("- Ordinary contact remains harmless from M79, and attack damage remains active-window-only from M80.");
            builder.AppendLine();
            builder.AppendLine("## Room Baking");
            builder.AppendLine();
            builder.AppendLine($"- Runtime room sources: `{string.Join("`, `", RoomNavMeshBakeUtility.RuntimeRoomRoots)}`.");
            builder.AppendLine($"- NavMesh data output: `{RoomNavMeshBakeUtility.OutputFolder}`.");
            builder.AppendLine($"- Runtime catalog: `{RoomNavMeshBakeUtility.CatalogAssetPath}`.");
            builder.AppendLine($"- Preferred bake menu: `{RoomNavMeshBakeUtility.PreferredBakeMenuPath}`.");
            builder.AppendLine("- Authored playable rooms without a prebaked catalog entry are invalid outside editor/development fallback and fail with a clear console error.");
            builder.AppendLine("- Static rocks, holes, room bounds, and blocking room objects are baked. Destructible blockers use `NavMeshObstacle` carving.");
            builder.AppendLine();
            builder.AppendLine("## Debugging");
            builder.AppendLine();
            builder.AppendLine("- Enemy path tracing draws Unity `NavMeshPath.corners`.");
            builder.AppendLine("- Debug stats now report NavMesh users, requests, path calculations, invalid/fallback reasons, solve time, and active destination data.");
            builder.AppendLine();
            builder.AppendLine("## Test Focus");
            builder.AppendLine();
            builder.AppendLine("- Bake coverage for every approved room.");
            builder.AppendLine("- Grounded enemies route around rocks to tactical reserved positions.");
            builder.AppendLine("- Agents stop during committed attack windows and sync cleanly after manual displacement/knockback.");
            builder.AppendLine("- Arena, Designer Room playtest, projectiles, room clear, split children, and boss runtime stay stable.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M97 Unity NavMesh Navigation Replacement Report

- Runtime backend: `{EnemyNavigationAdapter.CurrentBackend}`.
- Custom `RoomGridAStarPathfinder` is not called by `EnemyNavigationAdapter`.
- Grounded non-boss runtime bridge: `EnemyNavMeshAgentBridge`.
- NavMesh bake menu: `{RoomNavMeshBakeUtility.PreferredBakeMenuPath}`.
- Catalog path: `{RoomNavMeshBakeUtility.CatalogAssetPath}`.
- Missing room bake policy: use catalog bakes as the normal path; editor/development runtime bake is a diagnostic fallback only.
- Dynamic blocker policy: `NavMeshObstacle` carving, no runtime full rebake in V1.
");
        }
    }
}
