using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone89AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M89_Limited_Alert_Sharing.md";
        public const string ReportPath = "output/reports/m89_limited_alert_sharing.md";

        [MenuItem("Hollow/Generation/Generate Milestone 89 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            WriteDocs();
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 89 limited alert sharing docs and report.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M89: Limited Alert Sharing V1");
            builder.AppendLine();
            builder.AppendLine("M89 adds a restrained ally-alert layer over the M83 disturbance system. Selected non-boss enemies can wake nearby allies when they meaningfully escalate to combat or are damaged, but the recipient still resolves the warning through its own disposition, awareness, hearing, and behavior tree. This is not squad tactics.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine("- New stimulus kind: `EnemyStimulusKind.AllyAlert`.");
            builder.AppendLine("- Alert sharing is authored on `EnemyDefinition` with enable flag, radius, cooldown, and minimum source awareness.");
            builder.AppendLine("- Sources broadcast only through `RoomCombatController.EmitEnemyAllyAlert`; they never directly edit another enemy's movement or state.");
            builder.AppendLine("- Recipients hear the alert through normal hearing sensitivity and disposition logic.");
            builder.AppendLine("- `AllyAlert` and `CreatureSignal` do not recursively trigger another ally-alert broadcast, preventing room-wide chains.");
            builder.AppendLine("- Bosses are exempt as sources and recipients.");
            builder.AppendLine("- M88 remains the movement boundary: awakened enemies investigate, face, return-home, or attack through navigation intents.");
            builder.AppendLine("- No pathfinding, obstacle LOS, squad tactics, formation behavior, boss runtime change, or save schema change is included.");
            builder.AppendLine();
            builder.AppendLine("## Selected Alert Sources");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Enabled | Radius | Cooldown | Minimum Awareness | Notes |");
            builder.AppendLine("| --- | ---: | ---: | ---: | --- | --- |");
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss"))
            {
                builder.AppendLine($"| {enemy.DisplayName} | {enemy.AllyAlertSharingEnabled} | {enemy.AllyAlertRadiusMeters:0.00}m | {enemy.AllyAlertCooldownSeconds:0.00}s | {enemy.AllyAlertMinimumAwareness} | {AlertNote(enemy)} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Disposition Responses");
            builder.AppendLine();
            builder.AppendLine("- `Predator`: investigates the warning; loud or repeated pressure can commit it.");
            builder.AppendLine("- `Prey`: startles or raises suspicion instead of becoming a pure chaser.");
            builder.AppendLine("- `Sentinel`: faces/holds first, then attacks only if disturbance pressure warrants it.");
            builder.AppendLine("- `Territorial`: warns or paces before committing.");
            builder.AppendLine("- `Mindless`: turns toward pressure simply, with less nuance.");
            builder.AppendLine();
            builder.AppendLine("## M90 QA Notes");
            builder.AppendLine();
            builder.AppendLine("M90 should manually check mixed rooms with weapon users, ranged enemies, and casters to ensure alert sharing makes rooms feel alive without producing instant dogpiles. The desired feel is Dark Souls-like: allies notice noise and combat nearby, but each enemy still commits to readable individual actions.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            var enabled = EnemyCatalog.CreateRuntimeDefault()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss" && enemy.AllyAlertSharingEnabled)
                .Select(enemy => enemy.DisplayName)
                .ToArray();

            File.WriteAllText(ReportPath, $@"# M89 Limited Alert Sharing Report

- Added `EnemyStimulusKind.AllyAlert`.
- Added enemy alert-sharing metadata: enabled, radius, cooldown, and minimum source awareness.
- Added local room broadcast through `RoomCombatController.EmitEnemyAllyAlert`.
- Added enemy-side non-recursive broadcast hooks for damage, loud disturbance, and sight/engagement escalation.
- Selected alert-capable enemies: {string.Join(", ", enabled)}.
- Documentation: `{DocsPath}`.
- Report: `{ReportPath}`.
- M90 should perform a full combat AI QA lock across contact, active windows, movement, disturbance, alert sharing, and bosses.
");
        }

        private static string AlertNote(EnemyDefinition enemy)
        {
            if (!enemy.AllyAlertSharingEnabled)
            {
                return enemy.Disposition == EnemyInstinctDisposition.Prey
                    ? "solo/startle behavior; no ally broadcast"
                    : "stays local or uses existing creature-family signals";
            }

            if (enemy.SpeedMetersPerSecond <= 0.01f)
            {
                return "stationary sentinel warning radius";
            }

            return enemy.Intelligence >= EnemyIntelligenceLevel.Trained
                ? "disciplined local wake-up call"
                : "short practical local warning";
        }
    }
}
