using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone92AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M92_Pathfinding_Backend_Adapter.md";
        public const string ReportPath = "output/reports/m92_pathfinding_backend_adapter.md";

        [MenuItem("Hollow/Generation/Generate Milestone 92 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteDocs();
            WriteReport();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 92 pathfinding backend adapter docs and report.");
        }

        private static void WriteDocs()
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .ToArray();
            var groundedMobile = enemies
                .Where(enemy => enemy.MovementMode == EnemyMovementMode.Grounded && enemy.SpeedMetersPerSecond > 0f)
                .ToArray();
            var groundedStationary = enemies
                .Where(enemy => enemy.MovementMode == EnemyMovementMode.Grounded && enemy.SpeedMetersPerSecond <= 0f)
                .ToArray();
            var flying = enemies
                .Where(enemy => enemy.MovementMode == EnemyMovementMode.Flying)
                .ToArray();

            var builder = new StringBuilder();
            builder.AppendLine("# M92: Pathfinding Backend Adapter V1");
            builder.AppendLine();
            builder.AppendLine("M92 adds an optional custom `RoomGridAStar` backend behind the existing M88 `EnemyNavigationAdapter`. Behavior trees still issue the same movement intents; navigation converts those intents into path-aware goals and keeps local steering as the mandatory fallback.");
            builder.AppendLine();
            builder.AppendLine("## Contract");
            builder.AppendLine();
            builder.AppendLine("- `RoomGridAStar` uses a cached 0.5m room graph built from `RoomRuntimeRoot` bounds, walkable tiles, holes, obstacles, and blocking interactive objects.");
            builder.AppendLine("- Grounded mobile non-boss enemies can path for approach, preferred/action-envelope spacing, flee/reset, investigate, wander, and return-home intents.");
            builder.AppendLine("- Path goals target M91 action envelopes instead of blindly routing to the player center.");
            builder.AppendLine("- Action-envelope goals are sampled around the player/anchor so enemies do not pin themselves against rocks when the direct start point is blocked.");
            builder.AppendLine("- Active attacks, lunges, charges, creature bursts, recovery commitment, bump separation, flying movement, phase movement, stationary enemies, and bosses keep existing local movement rules.");
            builder.AppendLine("- Enemy and player bodies remain local steering/separation concerns, not A* blockers.");
            builder.AppendLine("- If a full path fails, the backend attempts a partial reachable node toward the goal; if that fails, local steering fallback remains authoritative.");
            builder.AppendLine("- Runtime diagnostics expose backend, path status, final goal, next waypoint, path age, waypoint count, and fallback reason.");
            builder.AppendLine("- The Developer Spawn Menu exposes an `Enemy Path Tracing` toggle that draws runtime path lines over the room.");
            builder.AppendLine();
            builder.AppendLine("## Backend Status");
            builder.AppendLine();
            builder.AppendLine($"- M88 compatibility backend constant: `{EnemyNavigationAdapter.CurrentBackend}`.");
            builder.AppendLine($"- Optional runtime backend: `{EnemyNavigationBackend.RoomGridAStar}`.");
            builder.AppendLine($"- Grid cell size: `{RoomGridAStarPathfinder.CellSizeMeters:0.00}m`.");
            builder.AppendLine("- Repath cadence is staggered per enemy by intelligence and spawn index; smarter enemies refresh faster.");
            builder.AppendLine();
            builder.AppendLine("## Roster Routing Table");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Movement | Speed | Pathfinding Runtime | Notes |");
            builder.AppendLine("| --- | --- | ---: | --- | --- |");
            foreach (var enemy in enemies)
            {
                var runtime = enemy.MovementMode == EnemyMovementMode.Grounded && enemy.SpeedMetersPerSecond > 0f
                    ? "RoomGridAStar + local fallback"
                    : "local rules only";
                var note = enemy.MovementMode == EnemyMovementMode.Flying
                    ? "flying exempt"
                    : enemy.SpeedMetersPerSecond <= 0f
                        ? "stationary diagnostics only"
                        : "grounded mobile";
                builder.AppendLine($"| {enemy.DisplayName} | {enemy.MovementMode} | {enemy.SpeedMetersPerSecond:0.00} | {runtime} | {note} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Intent Coverage");
            builder.AppendLine();
            builder.AppendLine("| Intent | M92 Backend | Goal Source |");
            builder.AppendLine("| --- | --- | --- |");
            builder.AppendLine("| MoveToPlayer | path-aware | M91 current action envelope near player |");
            builder.AppendLine("| PreferredRange | path-aware | M91 current action envelope near player |");
            builder.AppendLine("| Flee / Reset | path-aware | short capped retreat goal |");
            builder.AppendLine("| Wander | path-aware | short local wander goal |");
            builder.AppendLine("| Investigate | path-aware | last stimulus position |");
            builder.AppendLine("| ReturnHome | path-aware | spawn/home position |");
            builder.AppendLine("| ActiveCharge / ActiveLunge / CreatureBurst / PhaseMove / BumpSeparation | local rules | existing commitment or separation logic |");
            builder.AppendLine();
            builder.AppendLine("## Exemptions");
            builder.AppendLine();
            builder.AppendLine("- Flying enemies keep floor-region local movement.");
            builder.AppendLine("- Phase enemies keep obstacle-ignoring local movement.");
            builder.AppendLine("- Stationary enemies such as turrets, pods, and lanterns keep valid metadata and diagnostics but do not request movement paths.");
            builder.AppendLine("- Boss runtime behavior remains unchanged.");
            builder.AppendLine();
            builder.AppendLine("## Current Counts");
            builder.AppendLine();
            builder.AppendLine($"- Grounded mobile path users: {groundedMobile.Length}.");
            builder.AppendLine($"- Grounded stationary exemptions: {groundedStationary.Length}.");
            builder.AppendLine($"- Flying exemptions: {flying.Length}.");
            builder.AppendLine();
            builder.AppendLine("## Tuning Notes");
            builder.AppendLine();
            builder.AppendLine("- M92 does not rewrite behavior trees. Trees still decide what to do; navigation decides how to reach the chosen movement goal.");
            builder.AppendLine("- The backend paths toward action start positions, so enemies should stop shoving into rocks while trying to reach melee, ranged, investigate, flee, or return-home positions.");
            builder.AppendLine("- When the direct action start point is blocked, smart goal sampling searches nearby valid envelope positions before the A* request is made.");
            builder.AppendLine("- Local steering remains necessary for final approach, crowd separation, player/enemy body smoothing, and fallback.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .ToArray();
            var groundedMobile = enemies.Count(enemy => enemy.MovementMode == EnemyMovementMode.Grounded && enemy.SpeedMetersPerSecond > 0f);
            var stationary = enemies.Count(enemy => enemy.MovementMode == EnemyMovementMode.Grounded && enemy.SpeedMetersPerSecond <= 0f);
            var flying = enemies.Count(enemy => enemy.MovementMode == EnemyMovementMode.Flying);
            var bosses = BossCatalogDefinition.CreateRuntimeRoster().Length;

            File.WriteAllText(ReportPath, $@"# M92 Pathfinding Backend Adapter Report

- Backend added: `RoomGridAStar`.
- Grid cell size: `{RoomGridAStarPathfinder.CellSizeMeters:0.00}m`.
- Grounded mobile non-boss enemies using path-aware movement: {groundedMobile}.
- Grounded stationary non-boss enemies remaining local/diagnostic-only: {stationary}.
- Flying enemies remaining local/floor-region movement: {flying}.
- Bosses remaining runtime-exempt: {bosses}.
- Path-aware intents: approach, preferred/action-envelope spacing, flee/reset, investigate, wander, return-home.
- Smart action-envelope goal sampling: enabled.
- Exempt intents: active attacks, lunges, charges, creature bursts, phase movement, bump separation, recovery commitment.
- Debug menu path tracing overlay: enabled.
- Local steering fallback: enabled.
- Docs: `{DocsPath}`.
- Report: `{ReportPath}`.
");
        }
    }
}
