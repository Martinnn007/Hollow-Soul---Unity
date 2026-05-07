using System.Collections.Generic;
using System.IO;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone105UnityBehaviorFamilyMigrationAssetGenerator
    {
        public const string DataFolder = "Assets/_Hollow/Data/EnemyUnityBehavior/M105";
        public const string DocsPath = "Docs/Hollow_M105_Unity_Behavior_Family_Migration.md";
        public const string ReportPath = "output/reports/m105_unity_behavior_family_migration.md";

        private static readonly (string FileName, string FamilyId, string DisplayName, EnemyUnityBehaviorPilotKind Kind, string[] EnemyAssetPaths)[] Families =
        {
            ("Chasers_UnityBehaviorFamily.asset", "family:chasers", "M105 Chasers Unity Behavior Family", EnemyUnityBehaviorPilotKind.ChaserFamily, new[]
            {
                "Assets/_Hollow/Data/Enemies/Enemy_Normal.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_Flying.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_Fast.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_Heavy.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_Charger.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_Splitter.asset"
            }),
            ("Critters_UnityBehaviorFamily.asset", "family:critters", "M105 Critters Unity Behavior Family", EnemyUnityBehaviorPilotKind.CritterFamily, new[]
            {
                "Assets/_Hollow/Data/Enemies/Enemy_Rat.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_Spider.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_HollowBird.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_HollowBeast.asset"
            }),
            ("WeaponUsers_UnityBehaviorFamily.asset", "family:weapon_users", "M105 Weapon Users Unity Behavior Family", EnemyUnityBehaviorPilotKind.WeaponUserFamily, new[]
            {
                "Assets/_Hollow/Data/Enemies/Enemy_SkeletonSword.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_SkeletonSpear.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_Knight.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_Giant.asset"
            }),
            ("RangedFirearm_UnityBehaviorFamily.asset", "family:ranged_firearm", "M105 Ranged + Firearm Unity Behavior Family", EnemyUnityBehaviorPilotKind.RangedFirearmFamily, new[]
            {
                "Assets/_Hollow/Data/Enemies/Enemy_Turret.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_SpittingPod.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_HollowArcher.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_PowderGunner.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_KnifeThrower.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_RepeaterTurret.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_ClockworkSentry.asset"
            }),
            ("MagicGhost_UnityBehaviorFamily.asset", "family:magic_ghost", "M105 Magic + Ghost Unity Behavior Family", EnemyUnityBehaviorPilotKind.MagicGhostFamily, new[]
            {
                "Assets/_Hollow/Data/Enemies/Enemy_HollowAcolyte.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_Wraith.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_SoulEater.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_CurseBinder.asset",
                "Assets/_Hollow/Data/Enemies/Enemy_GraveLantern.asset"
            })
        };

        [MenuItem("Hollow/Generation/Generate Milestone 105 Unity Behavior Family Migration Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var created = new List<EnemyUnityBehaviorPilotGraphDefinition>();
            foreach (var family in Families)
            {
                var graph = CreateOrUpdateFamilyGraph(family.FileName, family.FamilyId, family.DisplayName, family.Kind);
                created.Add(graph);
                AssignFamilyGraph(graph, family.EnemyAssetPaths);
            }

            WriteDocs();
            WriteReport(created);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M105 Unity Behavior family migration assets.");
        }

        private static EnemyUnityBehaviorPilotGraphDefinition CreateOrUpdateFamilyGraph(
            string fileName,
            string familyId,
            string displayName,
            EnemyUnityBehaviorPilotKind kind)
        {
            var path = $"{DataFolder}/{fileName}";
            var graph = AssetDatabase.LoadAssetAtPath<EnemyUnityBehaviorPilotGraphDefinition>(path);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<EnemyUnityBehaviorPilotGraphDefinition>();
                AssetDatabase.CreateAsset(graph, path);
            }

            graph.ConfigureHardened(
                $"m105_{familyId.Replace(':', '_')}_unity_behavior",
                displayName,
                familyId,
                kind,
                graph.BehaviorGraph,
                EnemyUnityBehaviorFallbackPolicy.EmergencyOnly,
                true,
                "M105 family Unity Behavior contract. Official graph should choose high-level intent only; Hollow action profiles and combat execution remain authoritative.");
            EditorUtility.SetDirty(graph);
            return graph;
        }

        private static void AssignFamilyGraph(EnemyUnityBehaviorPilotGraphDefinition graph, IEnumerable<string> enemyAssetPaths)
        {
            foreach (var path in enemyAssetPaths)
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (enemy == null)
                {
                    continue;
                }

                enemy.ConfigureUnityBehaviorGraph(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, graph);
                EditorUtility.SetDirty(enemy);
            }
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M105: Unity Behavior Family Migration");
            builder.AppendLine();
            builder.AppendLine("M105 migrates normal enemies from one-off Unity Behavior pilots into family-level intent graphs. Unity Behavior chooses intent; Hollow keeps damage math, action profiles, spacing, NavMesh locomotion, pressure budgets, active windows, and boss exemptions.");
            builder.AppendLine();
            builder.AppendLine("## Family Contracts");
            builder.AppendLine();
            builder.AppendLine("| Family | Spawn kinds | Intent responsibility |");
            builder.AppendLine("| --- | --- | --- |");
            builder.AppendLine("| Critters | Rat, Spider, Hollow Bird, Hollow Beast | Wander, startle, warn/signal, flee, or request body attack intent. |");
            builder.AppendLine("| Chasers | Normal, Flying, Fast, Heavy, Ash Charger, Husk Splitter | Pressure, close to action envelope, flee if prey, request melee/area/charge intent. |");
            builder.AppendLine("| Weapon Users | Skeleton Sword, Skeleton Spear, Knight, Giant | Face/approach, guard when appropriate, request weapon melee or area intent. |");
            builder.AppendLine("| Ranged + Firearm | Turrets, Pod, Archer, Gunner, Thrower, Repeater, Clockwork | Hold lines, reset distance, request ranged fire intent. |");
            builder.AppendLine("| Magic + Ghost | Acolyte, Wraith, Soul Eater, Curse Binder, Grave Lantern | Cast, phase/reposition, apply area pressure, or hold occult range. |");
            builder.AppendLine();
            builder.AppendLine("## Source Of Truth");
            builder.AppendLine();
            builder.AppendLine("- Unity Behavior outputs `EnemyBehaviorCommand` intent only.");
            builder.AppendLine("- Empty action ids are intentional for family graphs; `EnemyActionScorer` selects the concrete Hollow action profile.");
            builder.AppendLine("- Runtime damage, knockback, guard recoil, windup/active/recovery, cooldowns, projectile data, and pressure budgets stay in Hollow profiles.");
            builder.AppendLine("- Emergency fallback remains explicit and trace-visible until official family graph assets are authored and assigned.");
            builder.AppendLine("- Boss runtime remains unchanged.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport(IReadOnlyList<EnemyUnityBehaviorPilotGraphDefinition> graphs)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M105 Unity Behavior Family Migration Report");
            builder.AppendLine();
            builder.AppendLine($"- Schema version: `{EnemyUnityBehaviorBlackboardSchema.SchemaVersion}`.");
            builder.AppendLine("- Migrated families: critters, chasers, weapon users, ranged/firearm, magic/ghost.");
            builder.AppendLine("- Runtime mode: all current non-boss enemies resolve `UnityBehaviorGraph`.");
            builder.AppendLine("- Hollow source of truth: action profiles, attack profiles, active windows, NavMesh, threat director, saves.");
            builder.AppendLine("- Graph contracts:");
            foreach (var graph in graphs)
            {
                builder.AppendLine($"  - `{graph.GraphId}` / `{graph.PilotKind}` / fallback `{graph.FallbackPolicy}`.");
            }

            builder.AppendLine($"- Docs: `{DocsPath}`.");
            File.WriteAllText(ReportPath, builder.ToString());
        }
    }
}
