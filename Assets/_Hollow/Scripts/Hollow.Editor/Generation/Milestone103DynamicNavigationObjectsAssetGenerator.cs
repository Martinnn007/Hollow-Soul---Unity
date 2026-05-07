using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone103DynamicNavigationObjectsAssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M103_Dynamic_Navigation_Objects.md";
        public const string ReportPath = "output/reports/m103_dynamic_navigation_objects.md";

        [MenuItem("Hollow/Generation/Generate Milestone 103 Dynamic Navigation Objects Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteDocs();
            WriteReport();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M103 dynamic navigation objects docs and report.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M103: Dynamic Navigation Objects V1");
            builder.AppendLine();
            builder.AppendLine("M103 makes runtime navigation blockers predictable under the Unity NavMesh replacement.");
            builder.AppendLine();
            builder.AppendLine("## Contract");
            builder.AppendLine();
            builder.AppendLine("- Static rocks and authored holes are baked into room NavMesh data and exposed as debug navigation markers.");
            builder.AppendLine("- Barrels, destructible blockers, and future gates use `RoomDynamicNavigationObjectMarker` plus `NavMeshObstacle` carving when they block movement at runtime; they are not baked into the static room NavMesh.");
            builder.AppendLine("- Destroyed interactive objects disable carving immediately through `RoomInteractiveObjectMarker.MarkDestroyed`.");
            builder.AppendLine("- Doors are visual by default, but `Locked` and `Unavailable` states enable carving while `Active` and `Cleared` states disable it.");
            builder.AppendLine("- Runtime code should not assume a `NavMeshObstacle` already exists; dynamic navigation markers create and configure one safely.");
            builder.AppendLine();
            builder.AppendLine("## Debugging");
            builder.AppendLine();
            builder.AppendLine("- `RoomRuntimeRoot.DynamicNavigationObjects` lists rocks, holes, doors, and interactive blockers with category, carving state, and last reason.");
            builder.AppendLine("- Each marker exposes `StatusSummary` and optional compact scene labels via `SetDynamicNavigationDebugLabelsVisible`.");
            builder.AppendLine("- This layer does not rebake rooms at runtime; it only toggles Unity carving obstacles for dynamic blockers.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, @"# M103 Dynamic Navigation Objects Report

- Added `RoomDynamicNavigationObjectMarker` as the shared bridge for doors, destructibles, blockers, holes, and future gates.
- Interactive blockers safely create/configure `NavMeshObstacle` carving and disable it on destruction.
- Interactive blockers are no longer baked into static room NavMesh data, so disabling carving can actually reopen paths.
- Door state changes now drive navigation carving for locked/unavailable doors.
- Rocks and holes are registered as baked navigation objects for debugging; holes also feed the shared room NavMesh bake utility.
- `RoomRuntimeRoot.DynamicNavigationObjects` provides a single runtime inspection surface, with optional debug labels toggled through `SetDynamicNavigationDebugLabelsVisible`.
");
        }
    }
}
