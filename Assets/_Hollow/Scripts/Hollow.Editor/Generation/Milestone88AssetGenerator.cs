using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone88AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M88_Navigation_Adapter.md";
        public const string ReportPath = "output/reports/m88_navigation_adapter.md";

        [MenuItem("Hollow/Generation/Generate Milestone 88 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            WriteDocs();
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 88 navigation adapter docs and report.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M88: Navigation Adapter V1");
            builder.AppendLine();
            builder.AppendLine("M88 introduces a navigation wrapper between behavior decisions and room movement. It adds no full pathfinding. The active backend is `LocalSteering`, but enemy runtime movement now asks an adapter to resolve desired local positions, modes, and intents so future pathfinding or local-navigation systems can be swapped in without rewriting combat AI.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine("- `EnemyNavigationRequest` carries room, current position, desired position, radius, mode, intent, intelligence, and whether local detours are allowed.");
            builder.AppendLine("- `EnemyNavigationResult` records backend, mode, intent, requested/resolved positions, steering direction, reached/blocked flags, and whether fallback steering was used.");
            builder.AppendLine("- `EnemyNavigationAdapter` is the only place normal enemy runtime movement resolves room collision for chase, preferred range, flee, wander, investigation, return-home, active attacks, creature bursts, phase moves, and bump separation.");
            builder.AppendLine("- Current backend: `LocalSteering`. It samples short local detours for non-committed grounded movement when direct motion stalls on rocks or blockers.");
            builder.AppendLine("- Committed attacks remain committed: charges and lunges do not pathfind around obstacles during active frames.");
            builder.AppendLine("- Flying movement keeps ignoring rocks while respecting floor-region bounds. Phase movement can ignore obstacles while staying inside room bounds.");
            builder.AppendLine("- No A*, navmesh, obstacle LOS, squad navigation, boss behavior changes, save migration, or new enemy roster is included.");
            builder.AppendLine();
            builder.AppendLine("## Movement Intent Table");
            builder.AppendLine();
            builder.AppendLine("| Intent | Current Handling | Future Hook |");
            builder.AppendLine("| --- | --- | --- |");
            builder.AppendLine("| `MoveToPlayer` | local collision + optional detour | chase destination through path adapter |");
            builder.AppendLine("| `PreferredRange` | local collision + optional detour | range-band destination scoring |");
            builder.AppendLine("| `Flee` | local collision + optional detour | retreat destination scoring with caps |");
            builder.AppendLine("| `Wander` | deterministic local steering | idle patrol/roam destinations |");
            builder.AppendLine("| `Investigate` | move/facing toward last disturbance | noise-source path target |");
            builder.AppendLine("| `ReturnHome` | local return to spawn/home | leash/path return target |");
            builder.AppendLine("| `ActiveCharge` / `ActiveLunge` | no detour, collision constrained | animation-authored movement lanes |");
            builder.AppendLine("| `CreatureBurst` | local burst with optional detour | burst destination validation |");
            builder.AppendLine("| `PhaseMove` | ignores obstacles, clamps to valid room | ghost/caster phase target picker |");
            builder.AppendLine("| `BumpSeparation` | tiny local separation | body-resolution policy |");
            builder.AppendLine();
            builder.AppendLine("## Current Roster Modes");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Movement | Default Mode | Notes |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss"))
            {
                var mode = EnemyNavigationAdapter.DefaultModeFor(enemy.MovementMode);
                builder.AppendLine($"| {enemy.DisplayName} | {enemy.MovementMode} | {mode} | {ModeNote(enemy)} |");
            }

            builder.AppendLine();
            builder.AppendLine("## M89 Bridge");
            builder.AppendLine();
            builder.AppendLine("M89 Limited Alert Sharing should emit awareness/stimulus decisions separately from movement. When an ally wakes another enemy, that enemy should still use M88 navigation intents for investigate, face, return-home, or attack-range movement instead of receiving direct position edits.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M88 Navigation Adapter Report

- Added `EnemyNavigationBackend`, `EnemyNavigationMode`, and `EnemyNavigationIntent`.
- Added request/result structs and `EnemyNavigationAdapter`.
- Routed non-boss enemy movement through the adapter for chase, range, flee, wander, investigate, return-home, active attack movement, creature bursts, phase moves, and bump separation.
- Current backend remains local and non-pathfinding: `{EnemyNavigationAdapter.CurrentBackend}`.
- Documentation: `{DocsPath}`.
- Report: `{ReportPath}`.
- Next milestone M89 can add limited alert sharing without directly coupling ally wake-up logic to movement resolution.
");
        }

        private static string ModeNote(EnemyDefinition enemy)
        {
            if (enemy.MovementMode == EnemyMovementMode.Flying)
            {
                return "flying adapter ignores rocks and respects floor bounds";
            }

            if (enemy.SpeedMetersPerSecond <= 0.01f)
            {
                return "stationary behavior tree; adapter still available for bump/phase-safe resolution";
            }

            return enemy.Intelligence >= EnemyIntelligenceLevel.Basic
                ? "uses local detour sampling when non-committed movement stalls"
                : "uses conservative local steering only";
        }
    }
}
