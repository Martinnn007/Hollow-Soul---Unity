using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone80AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M80_Active_Hit_Windows.md";
        public const string ReportPath = "output/reports/m80_active_hit_windows.md";

        [MenuItem("Hollow/Generation/Generate Milestone 80 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Milestone76AssetGenerator.AttackDirectory);

            ApplyAttackProfileTiming();
            ApplyEnemyExecutionModifiers();
            WriteDocs();
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 80 active hit window assets.");
        }

        private static void ApplyAttackProfileTiming()
        {
            foreach (var spec in Milestone76AssetGenerator.AllProfileSpecs())
            {
                var path = $"{Milestone76AssetGenerator.AttackDirectory}/{spec.AssetName}";
                var profile = AssetDatabase.LoadAssetAtPath<EnemyAttackProfileDefinition>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<EnemyAttackProfileDefinition>();
                    AssetDatabase.CreateAsset(profile, path);
                }

                profile.Configure(spec);
                EditorUtility.SetDirty(profile);
            }
        }

        private static void ApplyEnemyExecutionModifiers()
        {
            foreach (var row in Milestone76AssetGenerator.EnemyAssetPathsBySpawnKind())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(row.Value);
                if (enemy == null)
                {
                    continue;
                }

                var execution = EnemyDefinition.DefaultAttackExecutionFor(enemy.ArchetypeId, enemy.BehaviorId, enemy.MovementMode);
                enemy.ConfigureAttackExecutionModifiers(
                    execution.windupScale,
                    execution.activeScale,
                    execution.recoveryScale,
                    execution.hitArcDegreesBonus,
                    execution.poiseBreakThresholdOffset);
                EditorUtility.SetDirty(enemy);
            }
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M80: Active Hit Windows V1");
            builder.AppendLine();
            builder.AppendLine("M80 moves both enemy and player combat away from proximity-only damage and toward explicit readable action phases. Attacks now resolve as windup, active, and recovery. Damage lands only during active frames, ordinary body overlap remains harmless from M79, and recovery creates a small punish window.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine("- Enemy melee, charge, and ranged attacks use `Windup -> Active -> Recovery -> Idle`.");
            builder.AppendLine("- Ranged projectiles spawn on the active transition, then the enemy remains in an explicit recovery state.");
            builder.AppendLine("- Melee and lunge attacks use a simple forward arc plus range during active frames; each activation can hit once.");
            builder.AppendLine("- Windup can be broken by incoming player damage whose `ImpactForceClass` meets the resolved poise threshold.");
            builder.AppendLine("- Player light/heavy attacks pay stamina on start, then use windup, active, and recovery windows. The debug 2x light-attack toggle still halves final light cooldowns only.");
            builder.AppendLine("- Player attack commitment slows movement to `55%` and cannot be cancelled by roll, guard, or another attack in V1.");
            builder.AppendLine("- Player roll uses Space / gamepad east, costs stamina, travels in the move or aim direction, and grants dedicated roll i-frames separate from post-damage invulnerability.");
            builder.AppendLine("- Boss runtime remains largely unchanged; M79 dash/bash active contact bridges remain protected by explicit active windows.");
            builder.AppendLine();
            builder.AppendLine("## Enemy Execution Modifiers");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Windup | Active | Recovery | Arc Bonus | Poise Offset |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: |");
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null))
            {
                builder.AppendLine($"| {enemy.DisplayName} | x{enemy.AttackWindupScale:0.00} | x{enemy.AttackActiveScale:0.00} | x{enemy.AttackRecoveryScale:0.00} | {enemy.HitArcDegreesBonus:0.#}deg | {enemy.PoiseBreakThresholdOffset:+#;-#;0} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Player Timing Defaults");
            builder.AppendLine();
            builder.AppendLine("| Attack | Windup | Active | Recovery | Arc |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: |");
            AppendWeaponRow(builder, "Melee Light", Hollow.Data.Definitions.WeaponAttackDefinition.DefaultLight(Hollow.Data.Definitions.WeaponSlot.Melee));
            AppendWeaponRow(builder, "Melee Heavy", Hollow.Data.Definitions.WeaponAttackDefinition.DefaultHeavy(Hollow.Data.Definitions.WeaponSlot.Melee));
            AppendWeaponRow(builder, "Ranged Light", Hollow.Data.Definitions.WeaponAttackDefinition.DefaultLight(Hollow.Data.Definitions.WeaponSlot.Ranged));
            AppendWeaponRow(builder, "Ranged Heavy", Hollow.Data.Definitions.WeaponAttackDefinition.DefaultHeavy(Hollow.Data.Definitions.WeaponSlot.Ranged));
            builder.AppendLine();
            builder.AppendLine("## Deferred");
            builder.AppendLine();
            builder.AppendLine("- No combo trees, animation hitboxes, weapon-user enemy overhaul, full boss action rewrite, pathfinding, LOS, or behavior tree system is added in M80.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M80 Active Hit Windows Report

- Enemy actions now expose active/recovery states for melee, charge, and ranged attacks.
- Attack profiles carry recovery, hit arc, and poise break metadata.
- Enemy definitions carry execution modifiers for windup, active, recovery, hit arc, and poise threshold.
- Player light/heavy attacks use windup, active, and recovery timing while preserving stamina/cooldown rules.
- Player roll constants: cost `{PlayerWeaponController.RollStaminaCost:0.#}`, duration `{PlayerWeaponController.RollDurationSeconds:0.00}s`, i-frames `{PlayerWeaponController.RollInvulnerabilitySeconds:0.00}s`, distance `{PlayerWeaponController.RollDistanceMeters:0.00}m`.
- Documentation: `{DocsPath}`.
- Report: `{ReportPath}`.
");
        }

        private static void AppendWeaponRow(StringBuilder builder, string label, Hollow.Data.Definitions.WeaponAttackDefinition attack)
        {
            builder.AppendLine($"| {label} | {attack.WindupSeconds:0.00}s | {attack.ActiveSeconds:0.00}s | {attack.RecoverySeconds:0.00}s | {attack.HitArcDegrees:0.#}deg |");
        }
    }
}
