using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone108DesignerDebuggingPassAssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M108_Designer_Debugging_Pass.md";
        public const string ReportPath = "output/reports/m108_designer_debugging_pass.md";

        [MenuItem("Hollow/Generation/Generate Milestone 108 Designer Debugging Pass Artifacts")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteDocs();
            WriteReport();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M108 Designer Debugging Pass artifacts.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M108: Designer Debugging Pass");
            builder.AppendLine();
            builder.AppendLine("M108 unifies the enemy tuning overlays so Rafal and Pawel can inspect a room without mentally stitching together separate NavMesh, tactical, behavior, and attack-window readouts.");
            builder.AppendLine();
            builder.AppendLine("## Unified Enemy Overlay");
            builder.AppendLine();
            builder.AppendLine("The `Designer Enemy Debug` switch in the Developer Spawn Menu enables:");
            builder.AppendLine();
            builder.AppendLine("- NavMesh path tracing and path status.");
            builder.AppendLine("- Tactical role, active threat slot, reserved action, and reservation path result.");
            builder.AppendLine("- Unity Behavior graph state or Hollow behavior-tree node.");
            builder.AppendLine("- Chosen command/action, AI LOD, current awareness, and awareness reason.");
            builder.AppendLine("- Blocked/fallback reason from action spacing, NavMesh, locomotion, or scorer cooldowns.");
            builder.AppendLine("- Current readability phase and active attack window countdown.");
            builder.AppendLine();
            builder.AppendLine("## Designer Workflow");
            builder.AppendLine();
            builder.AppendLine("Turn on `Designer Enemy Debug`, spawn or play a room, and read each enemy label from top to bottom: current awareness, active attack window, action choice, tactical slot, NavMesh path, blocked reason, and Behavior graph trace. The path line remains the visible movement guide; the text explains why that path and action were chosen.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M108 Designer Debugging Pass Report

- Unified overlay: `EnemyDesignerDebugOverlay`.
- Runtime hook: `EnemyRuntimeController.UpdateAiDebugOverlay`.
- Debug menu switch: `Designer Enemy Debug`.
- Combined diagnostics: NavMesh path, tactical slot, Behavior graph state, chosen action, blocked reason, current awareness, and active attack window.
- Docs: `{DocsPath}`.
");
        }
    }
}
