using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone79AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M79_Contact_Damage_Rework.md";
        public const string ReportPath = "output/reports/m79_contact_damage_rework.md";

        [MenuItem("Hollow/Generation/Generate Milestone 79 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            ApplyCurrentRosterContactPolicies();
            WriteDocs();
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 79 contact damage rework assets.");
        }

        public static string[] CurrentEnemySpawnKinds()
        {
            return Milestone76AssetGenerator.EnemyAssetPathsBySpawnKind()
                .Keys
                .OrderBy(spawnKind => spawnKind)
                .ToArray();
        }

        private static void ApplyCurrentRosterContactPolicies()
        {
            foreach (var row in Milestone76AssetGenerator.EnemyAssetPathsBySpawnKind())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(row.Value);
                if (enemy == null)
                {
                    continue;
                }

                enemy.ConfigureContactPolicy(EnemyContactDamagePolicy.ActiveOnly, EnemyPassiveContactHazardType.None);
                EditorUtility.SetDirty(enemy);
            }

            foreach (var row in Milestone75AssetGenerator.BossRows())
            {
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>($"{Milestone53AssetGenerator.BossDirectory}/{row.FileName}");
                if (boss == null)
                {
                    continue;
                }

                boss.ConfigureContactPolicy(EnemyContactDamagePolicy.ActiveOnly, EnemyPassiveContactHazardType.None);
                EditorUtility.SetDirty(boss);
            }
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M79: Contact Damage Rework V1");
            builder.AppendLine();
            builder.AppendLine("M79 changes enemy body overlap from automatic damage into a disturbance event. Ordinary enemies can be bumped or overlapped without hurting the player; explicit attacks, projectiles, room hazards, and future hazardous bodies remain the sources of damage.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine("- `EnemyContactDamagePolicy.ActiveOnly`: body damage is allowed only during an explicit active attack window.");
            builder.AppendLine("- `EnemyContactDamagePolicy.PassiveHazard`: body overlap can tick damage on contact cooldowns when a non-None hazard type is authored.");
            builder.AppendLine("- `EnemyContactDamagePolicy.Disabled`: body overlap never applies contact damage.");
            builder.AppendLine("- Current normal enemies and bosses are `ActiveOnly` with `EnemyPassiveContactHazardType.None`.");
            builder.AppendLine("- Idle, chase, hold, wander, retreat, windup, entry grace, stun/death, ranged windup, and ordinary overlap do not damage the player.");
            builder.AppendLine("- `MeleeLunge`, `Charging`, and armed boss dash/bash windows can damage once per activation using their M76 attack profiles.");
            builder.AppendLine("- Non-hazard overlap emits `EnemyStimulusKind.Proximity`, so bumping an enemy can alert, engage, or startle it without reducing player HP.");
            builder.AppendLine("- Existing projectile, room hazard, guard, knockback, split child, and boss projectile behavior remains unchanged.");
            builder.AppendLine();
            builder.AppendLine("## Current Roster Contact Policy");
            builder.AppendLine();
            builder.AppendLine("| Enemy Spawn | Policy | Passive Hazard |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var spawnKind in CurrentEnemySpawnKinds())
            {
                builder.AppendLine($"| `{spawnKind}` | ActiveOnly | None |");
            }

            builder.AppendLine();
            builder.AppendLine("## Boss Contact Bridge");
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("- Stone Warden `stone_charge`, Iron Reliquary `iron_relocate_bash`, and Ash Comet `ash_comet_dash` arm short active contact windows.");
            builder.AppendLine("- Mirror Husk chase overlap is harmless in M79 until a later milestone gives it an explicit active attack window.");
            builder.AppendLine("- Boss HUD/readability and projectile attacks stay unchanged.");
            builder.AppendLine();
            builder.AppendLine("## Compatibility");
            builder.AppendLine();
            builder.AppendLine("- No save schema change; Continue derives contact policy and hazard type from the current catalog.");
            builder.AppendLine("- The legacy contact damage and cooldown fields remain for tuning active body attacks and future passive hazards.");
            builder.AppendLine("- M79 does not add behavior trees, pathfinding, line of sight, alert sharing, new attacks, or new enemies.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M79 Contact Damage Rework Report

- Runtime rule: ordinary body overlap disturbs/alerts but does not hurt the player.
- Current enemy roster policy: `ActiveOnly` + `None` for {CurrentEnemySpawnKinds().Length} enemy spawn kinds.
- Current boss roster policy: `ActiveOnly` + `None` for {Milestone75AssetGenerator.BossRows().Count} bosses.
- Active damage remains for melee lunges, charges, and armed boss dash/bash windows.
- Passive hazardous body support exists for future enemies via `EnemyPassiveContactHazardType`.
- Documentation: `{DocsPath}`.
- Report: `{ReportPath}`.
");
        }
    }
}
