using System.IO;
using System.Text;
using Hollow.Editor.Navigation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone100NavMeshBakePipelineAssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M100_NavMesh_Bake_Pipeline_V2.md";
        public const string ReportPath = "output/reports/m100_navmesh_bake_pipeline_v2.md";

        [MenuItem("Hollow/Generation/Generate Milestone 100 NavMesh Bake Pipeline Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteDocs();
            WriteReport();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M100 NavMesh bake pipeline docs and report.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M100: NavMesh Bake Pipeline V2");
            builder.AppendLine();
            builder.AppendLine("M100 unifies the room NavMesh bake path so editor baking and development runtime fallback use the same source geometry, the same agent settings, and the same catalog contract.");
            builder.AppendLine();
            builder.AppendLine("## Contract");
            builder.AppendLine();
            builder.AppendLine($"- Shared builder: `RoomNavMeshBuildUtility`.");
            builder.AppendLine($"- Agent settings: `{RoomNavMeshBuildUtility.DescribeAgentSettings()}`.");
            builder.AppendLine($"- Preferred bake command: `{RoomNavMeshBakeUtility.PreferredBakeMenuPath}`.");
            builder.AppendLine($"- Catalog: `{RoomNavMeshBakeUtility.CatalogAssetPath}`.");
            builder.AppendLine($"- Runtime source roots: `{string.Join("`, `", RoomNavMeshBakeUtility.RuntimeRoomRoots)}`.");
            builder.AppendLine("- Catalog bakes are the normal authored-room path.");
            builder.AppendLine("- Runtime baking is editor/development-only and logs a warning when used.");
            builder.AppendLine("- Non-development builds fail loudly when an authored room is missing catalog NavMesh data.");
            builder.AppendLine();
            builder.AppendLine("## Validation");
            builder.AppendLine();
            builder.AppendLine("- Designer Room validation reports missing NavMesh bakes with the exact room id, catalog path, and bake command.");
            builder.AppendLine("- Arena curated preset validation reports missing room bakes before launch.");
            builder.AppendLine("- Generated/transient designer and arena rooms can use dev fallback while being edited, but should be promoted and baked before QA lock.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M100 NavMesh Bake Pipeline V2 Report

- Shared bake builder: `RoomNavMeshBuildUtility`.
- Runtime compatibility wrapper: `RoomRuntimeNavMeshBuilder`.
- Editor baker: `RoomNavMeshBakeUtility`.
- Preferred bake command: `{RoomNavMeshBakeUtility.PreferredBakeMenuPath}`.
- Catalog path: `{RoomNavMeshBakeUtility.CatalogAssetPath}`.
- Runtime roots: `{string.Join("`, `", RoomNavMeshBakeUtility.RuntimeRoomRoots)}`.
- Agent settings: `{RoomNavMeshBuildUtility.DescribeAgentSettings()}`.
- Fallback policy: `RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake` is allowed only in editor/development; `RequireCatalogBake` blocks with exact missing-bake diagnostics.
");
        }
    }
}
