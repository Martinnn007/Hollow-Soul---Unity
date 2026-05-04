using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone96TacticalNavigationAssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M96_Tactical_Navigation_AI_Tool_Bakeoff.md";
        public const string ReportPath = "output/reports/m96_tactical_navigation_ai_tool_bakeoff.md";

        [MenuItem("Hollow/Generation/Generate Milestone 96 Tactical Navigation Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteDocs();
            WriteReport();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M96 tactical navigation and AI tool bake-off docs.");
        }

        private static void WriteDocs()
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .ToArray();
            var groundedMobile = enemies.Count(enemy => enemy.MovementMode == EnemyMovementMode.Grounded && enemy.SpeedMetersPerSecond > 0f);
            var stationary = enemies.Count(enemy => enemy.SpeedMetersPerSecond <= 0f);
            var flying = enemies.Count(enemy => enemy.MovementMode == EnemyMovementMode.Flying);
            var builder = new StringBuilder();
            builder.AppendLine("# M96: Tactical Navigation + AI Tool Bake-Off V1");
            builder.AppendLine();
            builder.AppendLine("M96 adds a tactical intent layer above the existing behavior tree, action scorer, threat director, spacing profiles, and M92 pathfinding backend. The target feel is Pure Souls leaning: only 2-4 enemies become active tactical threats while the rest hold, reposition, investigate, or wait.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine($"- `RoomTacticalDirector` limits active non-boss tactical threats to `{RoomTacticalDirector.MinActiveThreatSlots}-{RoomTacticalDirector.MaxActiveThreatSlots}` slots.");
            builder.AppendLine("- `EnemyTacticalIntent` records role, commit policy, action id, reserved local position, path backend, pressure lane, and debug reason.");
            builder.AppendLine("- `EnemyLocomotionAgent` remains behind `EnemyRuntimeController` movement and can recover from blocked tactical path steps with small sidestep attempts.");
            builder.AppendLine("- Behavior trees remain personality/role gates; the scorer and tactical director decide concrete action and position ownership.");
            builder.AppendLine("- Path goals should move enemies toward reserved action positions rather than the player center or a rigid preferred band.");
            builder.AppendLine("- Boss runtime behavior remains unchanged.");
            builder.AppendLine();
            builder.AppendLine("## Tool Bake-Off");
            builder.AppendLine();
            builder.AppendLine($"Source of truth: `{EnemyAiToolBakeOffEvaluation.HollowSourceOfTruth}`.");
            builder.AppendLine();
            builder.AppendLine("| Candidate | Role | Paid | Risk | Gate |");
            builder.AppendLine("| --- | --- | --- | ---: | --- |");
            foreach (var option in EnemyAiToolBakeOffEvaluation.Options)
            {
                builder.AppendLine($"| {option.Name} | {option.Role} | {(option.RequiresPurchase ? "yes" : "no")} | {option.IntegrationRisk} | {option.AdoptionGate} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Evaluation Rooms");
            builder.AppendLine();
            builder.AppendLine("- `Room_Small_RatRoom_001`: critter swarm with rocks and narrow lanes.");
            builder.AppendLine("- Rock-heavy designer rooms: obstacle routing, stuck recovery, and path-corridor quality.");
            builder.AppendLine("- Weapon-user rooms: Souls-like approach, attack range, and punishable recovery spacing.");
            builder.AppendLine("- Ranged/caster rooms: backline reservations and non-dogpile pressure.");
            builder.AppendLine("- Arena swarms: 20-40 enemy frame stability, solve counts, fallback reasons, and readability.");
            builder.AppendLine();
            builder.AppendLine("## Current Roster Shape");
            builder.AppendLine();
            builder.AppendLine($"- Non-boss enemies: {enemies.Length}.");
            builder.AppendLine($"- Grounded mobile enemies: {groundedMobile}.");
            builder.AppendLine($"- Stationary enemies: {stationary}.");
            builder.AppendLine($"- Flying enemies: {flying}.");
            builder.AppendLine();
            builder.AppendLine("## Adoption Rule");
            builder.AppendLine();
            builder.AppendLine("External packages are adopted only if they clearly improve rock/obstacle navigation, 20-40 enemy performance, designer debugging speed, and integration cost while keeping Hollow data as the source of truth.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            var paidCandidates = EnemyAiToolBakeOffEvaluation.Options.Count(option => option.RequiresPurchase);
            var freeCandidates = EnemyAiToolBakeOffEvaluation.Options.Length - paidCandidates;
            File.WriteAllText(ReportPath, $@"# M96 Tactical Navigation + AI Tool Bake-Off Report

- Runtime tactical layer: `RoomTacticalDirector`, `EnemyTacticalIntent`, `EnemyLocomotionAgent`.
- Active tactical threat slots: {RoomTacticalDirector.MinActiveThreatSlots}-{RoomTacticalDirector.MaxActiveThreatSlots}.
- Source of truth: `{EnemyAiToolBakeOffEvaluation.HollowSourceOfTruth}`.
- Free/built-in candidates: {freeCandidates}.
- Paid external candidates: {paidCandidates}.
- Adopt external tools only after feel + performance gates pass.
- Debug overlays: tactical role/slot/intent summary plus existing path tracing and AI blackboard.
- Docs: `{DocsPath}`.
");
        }
    }
}
