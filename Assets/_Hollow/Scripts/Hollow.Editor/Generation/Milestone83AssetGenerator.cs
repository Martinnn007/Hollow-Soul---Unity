using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone83AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M83_Noise_And_Disturbance_V2.md";
        public const string ReportPath = "output/reports/m83_noise_and_disturbance_v2.md";

        [MenuItem("Hollow/Generation/Generate Milestone 83 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            ApplyEnemyDisturbanceDefaults();
            WriteDocs();
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 83 noise and disturbance assets.");
        }

        private static void ApplyEnemyDisturbanceDefaults()
        {
            foreach (var row in Milestone76AssetGenerator.EnemyAssetPathsBySpawnKind())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(row.Value);
                if (enemy == null)
                {
                    continue;
                }

                var tuning = EnemyDefinition.DefaultDisturbanceTuningFor(enemy.ArchetypeId, enemy.BehaviorId, enemy.MovementMode);
                enemy.ConfigureDisturbance(tuning.x, tuning.y, tuning.z);
                EditorUtility.SetDirty(enemy);
            }
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M83: Noise + Disturbance V2");
            builder.AppendLine();
            builder.AppendLine("M83 turns the M75 stimulus spine into a small Souls-like disturbance layer. Footsteps, rolls, light and heavy attacks, guard impacts, proximity, and harmless body bumps now carry a tier, accumulate local disturbance, and resolve through enemy disposition instead of a universal aggro switch.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine("- Stimuli use `EnemyStimulusTier`: `Quiet`, `Normal`, `Loud`, and `Violent`.");
            builder.AppendLine("- Footsteps are `Quiet`; rolls and light attacks are `Normal`; heavy attacks and guard/block/parry impacts are `Loud`; direct damage is `Violent`.");
            builder.AppendLine("- Enemy definitions author `hearingSensitivityMultiplier`, `disturbanceEscalationThreshold`, and `investigationDurationSeconds`.");
            builder.AppendLine("- Predators investigate and then commit; prey startle/flee or panic when close/attacked; sentinels face/hold until disturbance warrants attacks; territorial enemies warn before aggression; mindless enemies pressure simply.");
            builder.AppendLine("- Ordinary M79 body contact remains harmless. Bumps emit `EnemyStimulusKind.Bump`, separate bodies lightly, and feed the same disturbance rules.");
            builder.AppendLine("- Runtime diagnostics expose the last stimulus kind/tier/time/position, awareness reason, and current disturbance score for debug tooling only.");
            builder.AppendLine("- Bosses are metadata/docs only for M83; boss runtime behavior is unchanged.");
            builder.AppendLine();
            builder.AppendLine("## Tier Table");
            builder.AppendLine();
            builder.AppendLine("| Source | Kind | Tier |");
            builder.AppendLine("| --- | --- | --- |");
            builder.AppendLine("| Player footstep pulse | `Footstep` | `Quiet` |");
            builder.AppendLine("| Player roll | `Roll` | `Normal` |");
            builder.AppendLine("| Light melee / ranged | `MeleeAttack` / `RangedAttack` | `Normal` |");
            builder.AppendLine("| Heavy melee / ranged | `MeleeAttack` / `RangedAttack` | `Loud` |");
            builder.AppendLine("| Guard, block, or parry impact | `GuardImpact` | `Loud` |");
            builder.AppendLine("| Passive overlap bump | `Bump` | `Normal` |");
            builder.AppendLine("| Direct damage | `Damage` | `Violent` |");
            builder.AppendLine();
            builder.AppendLine("## Roster Tuning");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Hearing Sensitivity | Escalation Threshold | Investigation | Notes |");
            builder.AppendLine("| --- | ---: | ---: | ---: | --- |");
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss"))
            {
                builder.AppendLine($"| {enemy.DisplayName} | x{enemy.HearingSensitivityMultiplier:0.00} | {enemy.DisturbanceEscalationThreshold:0.00} | {enemy.InvestigationDurationSeconds:0.00}s | {DispositionNote(enemy)} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Deferred");
            builder.AppendLine();
            builder.AppendLine("- No stealth UI, pathfinding, obstacle LOS, alert sharing, save migration, or boss behavior changes are included.");
            builder.AppendLine("- Later milestones can add noise surfaces, richer investigation paths, limited alert sharing, and dedicated stealth feedback without replacing this local disturbance contract.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M83 Noise + Disturbance V2 Report

- Added `EnemyStimulusTier` and tier-aware stimulus APIs.
- Added enemy disturbance fields: hearing sensitivity, escalation threshold, and investigation duration.
- Updated player footsteps, rolls, light/heavy attacks, guard impacts, direct damage, proximity, and bumps to feed tiered disturbance.
- Ordinary body bumps remain harmless, emit `Bump`, and apply light separation.
- Boss runtime remains unchanged.
- Documentation: `{DocsPath}`.
- Report: `{ReportPath}`.
");
        }

        private static string DispositionNote(EnemyDefinition enemy)
        {
            return enemy.Disposition switch
            {
                EnemyInstinctDisposition.Prey => "startles, flees, then panics when close or attacked",
                EnemyInstinctDisposition.Sentinel => "faces/holds until disturbance warrants fire",
                EnemyInstinctDisposition.Territorial => "warns/paces before committing",
                EnemyInstinctDisposition.Mindless => "simple disturbance pressure",
                _ => "near-default predator investigation"
            };
        }
    }
}
