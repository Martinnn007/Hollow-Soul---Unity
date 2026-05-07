using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone107AiPerformanceLodLockAssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M107_AI_Performance_And_LOD_Lock.md";
        public const string ReportPath = "output/reports/m107_ai_performance_lod_lock.md";

        [MenuItem("Hollow/Generation/Generate Milestone 107 AI Performance + LOD Lock Artifacts")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteDocs();
            WriteReport();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M107 AI Performance + LOD Lock artifacts.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M107: AI Performance + LOD Lock");
            builder.AppendLine();
            builder.AppendLine("M107 stabilizes large Arena encounters by making enemy AI cost visible and adaptive. Hollow keeps Souls-like responsiveness for close, endangered, and committed enemies while stretching update cadence for reduced/background enemies under swarm or path pressure.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Counters");
            builder.AppendLine();
            builder.AppendLine("- Active AI agents and Full/Reduced/Background LOD counts.");
            builder.AppendLine("- NavMesh agent users, pending paths, stuck agents, invalid paths, solve timing, and fallback reasons.");
            builder.AppendLine("- Unity Behavior graph ticks and emergency fallback ticks.");
            builder.AppendLine("- Enemy action scorer calls and candidate counts.");
            builder.AppendLine("- Room pressure lanes and scorer pressure penalties.");
            builder.AppendLine();
            builder.AppendLine("## Adaptive Cadence");
            builder.AppendLine();
            builder.AppendLine("- Full LOD enemies close to the player, endangered, or already committed keep their base think interval.");
            builder.AppendLine("- Full LOD backliners receive only a small interval stretch under 20-40 enemy load.");
            builder.AppendLine("- Reduced and Background enemies stretch more aggressively, reusing cached plans instead of constantly rescoring.");
            builder.AppendLine("- The cap prevents visible dumbness: responsive threats stay fast, while far enemies hold, face, reposition, or reuse plans.");
            builder.AppendLine();
            builder.AppendLine("## Arena Profiling Contract");
            builder.AppendLine();
            builder.AppendLine("Arena smoke profiling should watch `EnemyNavigationDebugOverlay.DiagnosticsSummary` and `EnemyAiDebugOverlay.DiagnosticsSummary` while spawning 20-40 enemies. Healthy runs should show stable pending path counts, low stuck agents, bounded scorer calls, and pressure penalties instead of dogpiling.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M107 AI Performance + LOD Lock Report

- AI diagnostics: `EnemyAiDebugOverlay.PerformanceStats`.
- Navigation diagnostics: `EnemyNavigationDebugOverlay.Stats`.
- Adaptive cadence entrypoint: `EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics`.
- Runtime counters added: agent count, path pending count, behavior graph ticks, scorer calls, stuck agents, pressure penalties.
- Design rule: close/endangered/committed enemies keep fast updates; reduced/background enemies degrade gracefully under Arena swarm load.
- Docs: `{DocsPath}`.
");
        }
    }
}
