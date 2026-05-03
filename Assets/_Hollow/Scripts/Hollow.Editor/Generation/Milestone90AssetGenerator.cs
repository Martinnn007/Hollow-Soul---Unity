using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone90AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M90_Combat_AI_QA_Lock.md";
        public const string ReportPath = "output/reports/m90_combat_ai_qa_lock.md";

        [MenuItem("Hollow/Generation/Generate Milestone 90 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            WriteDocs();
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 90 combat AI QA lock docs and report.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M90: Combat AI QA Lock");
            builder.AppendLine();
            builder.AppendLine("M90 is a QA and feel lock over the modern enemy combat stack from M72 through M89. It does not add a new enemy family. It proves that the foundation is coherent: ordinary contact is harmless, attacks use active windows, senses and disturbance feed behavior trees, navigation remains adapter-bound, weapon users keep recovery windows, ranged and magic enemies stay budgeted, knockback data remains profile-driven, and bosses stay stable.");
            builder.AppendLine();
            builder.AppendLine("## Lock Criteria");
            builder.AppendLine();
            builder.AppendLine("- Unity compiles without Safe Mode errors.");
            builder.AppendLine("- Focused EditMode regressions pass for contact, active windows, attack profiles, movement, senses, disturbance, behavior trees, weapon users, creature actions, ranged enemies, magic enemies, navigation, alert sharing, knockback, room clear, projectiles, split children, and bosses.");
            builder.AppendLine("- M72 priority remains strict: only Tactical and Cunning intelligence receive attack-budget tie bonuses.");
            builder.AppendLine("- M79 contact contract remains intact: normal body overlap disturbs or alerts, but does not damage.");
            builder.AppendLine("- M80 active windows remain intact: windup and recovery are safe from damage, active frames apply damage once per activation unless authored otherwise.");
            builder.AppendLine("- M83-M89 AI layers remain local and readable: no hidden pathfinding dependency, no room-wide alert chains, no boss runtime behavior rewrite.");
            builder.AppendLine();
            builder.AppendLine("## QA Surface");
            builder.AppendLine();
            builder.AppendLine("| Surface | Desired Result |");
            builder.AppendLine("| --- | --- |");
            builder.AppendLine("| contact | Ordinary enemy overlap separates and disturbs only; passive hazards are explicit opt-ins. |");
            builder.AppendLine("| active windows | Melee, lunge, charge, ranged, area, weapon, creature, and magic actions commit through windup, active, recovery. |");
            builder.AppendLine("| weapon users | Skeletons, knights, and giants use ranges, arcs, guard windows, combos, and recovery instead of contact damage. |");
            builder.AppendLine("| senses | Sight, hearing, noise tiers, proximity, and bump stimuli produce disposition-specific responses. |");
            builder.AppendLine("| movement | Enemies use preferred range, local steering, and the M88 navigation adapter without hard pathfinding assumptions. |");
            builder.AppendLine("| knockback | M76 attack profiles carry damage classification, force class, knockback, and guard recoil. |");
            builder.AppendLine("| bosses | Existing boss behavior and HUD remain unchanged except active contact bridges already added in M79. |");
            builder.AppendLine();
            builder.AppendLine("## Current Roster Snapshot");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Intelligence | Disposition | Preferred Distance | Attacks | Actions | Tree | Alert Sharing |");
            builder.AppendLine("| --- | --- | --- | ---: | ---: | ---: | --- | --- |");
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss"))
            {
                builder.AppendLine($"| {enemy.DisplayName} | {enemy.Intelligence} | {enemy.Disposition} | {enemy.PreferredRangeMinMeters:0.00}-{enemy.PreferredRangeMaxMeters:0.00}m | {enemy.AttackProfiles.Count} | {enemy.ActionProfiles.Count} | {enemy.BehaviorTree.TreeId} | {enemy.AllyAlertSharingEnabled} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Boss QA Snapshot");
            builder.AppendLine();
            builder.AppendLine("| Boss | Intelligence Metadata | Attacks | Actions | Tree Metadata | Contact Policy |");
            builder.AppendLine("| --- | --- | ---: | ---: | --- | --- |");
            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                builder.AppendLine($"| {boss.DisplayName} | {boss.Intelligence} | {boss.AttackProfiles.Count} | {boss.ActionProfiles.Count} | {boss.BehaviorTreeMetadata.TreeId} | {boss.ContactDamagePolicy} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Feel Notes");
            builder.AppendLine();
            builder.AppendLine("- Dark Souls-style combat should not mean slow combat; it means readable commitment, recovery, and counterplay.");
            builder.AppendLine("- The old min/max range idea should become preferred-distance behavior. Enemies can prefer a band, but attacks, lunges, charges, guards, and retreats must intentionally break the band when their action calls for it.");
            builder.AppendLine("- Ranged enemies should reposition for line pressure later, but M90 keeps the M88 adapter boundary and avoids committing to a pathfinding backend.");
            builder.AppendLine("- Alert sharing should make rooms feel awake, not unfair. It is a local nudge into the same behavior rules, not squad command.");
            builder.AppendLine();
            builder.AppendLine("## Suggested Next Milestones");
            builder.AppendLine();
            builder.AppendLine("1. M91 Preferred Distance + Commitment Tuning V2: replace any remaining rigid min/max behavior with soft preferred-distance envelopes, action-specific range overrides, retreat caps, and punishable recovery spacing.");
            builder.AppendLine("2. M92 Pathfinding Backend Adapter V1: add a real optional backend behind M88 for selected grounded enemies, with local steering fallback and no behavior-tree rewrite.");
            builder.AppendLine("3. M93 Boss Behavior Trees + Active Windows V1: move boss decision metadata into controlled runtime trees while preserving boss identities and explicit attack windows.");
            builder.AppendLine("4. M94 Combat Feedback + Feel Integration V1: improve telegraphs, hit sparks, shield reactions, poise break feedback, damage health bars, and audio cue hooks.");
            builder.AppendLine("5. M95 Advanced Attack Families + Status V1: add poison, bleed, curse, fire, frost, soul, grab, and hazard-zone actions with explicit counters.");
            builder.AppendLine("6. M96 Encounter Director Pressure Budgets V2: tune melee, ranged, magic, alert, and boss pressure budgets together for mixed rooms.");
            builder.AppendLine("7. M97 Enemy AI Personality Pass V1: author per-enemy aggression, courage, patience, retreat bias, combo bias, and disturbance tolerance over the behavior tree layer.");
            builder.AppendLine("8. M98 New Boss Integration V1: add a new boss built on active windows, action profiles, behavior trees, and feedback from the start.");
            builder.AppendLine("9. M99 Combat AI Metrics Rooms V1: add designer test rooms for distance, alert, projectile, weapon, creature, magic, pathing, and boss regressions.");
            builder.AppendLine("10. M100 Combat AI QA Lock 2: full-suite and manual feel lock after pathfinding, boss trees, statuses, and feedback are online.");
            builder.AppendLine();
            builder.AppendLine("## M90 Output");
            builder.AppendLine();
            builder.AppendLine("M90 is complete only when focused regressions are green. If Unity licensing, Safe Mode, or broad legacy tests block the full suite, record the blocker and leave the milestone unlocked.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss").ToArray();
            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            var attackProfiles = enemies.Sum(enemy => enemy.AttackProfiles.Count) + bosses.Sum(boss => boss.AttackProfiles.Count);
            var actionProfiles = enemies.Sum(enemy => enemy.ActionProfiles.Count) + bosses.Sum(boss => boss.ActionProfiles.Count);
            var alertSources = enemies.Count(enemy => enemy.AllyAlertSharingEnabled);

            File.WriteAllText(ReportPath, $@"# M90 Combat AI QA Lock Report

- Enemy definitions covered: {enemies.Length}.
- Boss definitions covered: {bosses.Length}.
- Resolved attack profiles covered: {attackProfiles}.
- Resolved action profiles covered: {actionProfiles}.
- Limited alert-sharing sources: {alertSources}.
- Navigation backend: `{EnemyNavigationAdapter.CurrentBackend}`.
- Contact policy target: all current roster bodies are `ActiveOnly` with no passive hazard.
- M72 priority target: only `Tactical` and `Cunning` get intelligence tie bonuses.
- M90 docs: `{DocsPath}`.
- M90 report: `{ReportPath}`.
- Next recommended milestone: `M91 Preferred Distance + Commitment Tuning V2`.
");
        }
    }
}
