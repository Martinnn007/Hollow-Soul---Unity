using System.IO;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone98UnityBehaviorMigrationAssetGenerator
    {
        public const string DataFolder = "Assets/_Hollow/Data/EnemyUnityBehavior/M98";
        public const string DocsPath = "Docs/Hollow_M98_Unity_Behavior_Runtime_Migration_Pilot.md";
        public const string ReportPath = "output/reports/m98_unity_behavior_runtime_migration_pilot.md";

        [MenuItem("Hollow/Generation/Generate Milestone 98 Unity Behavior Migration Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var ratGraph = CreateOrUpdatePilotGraph("Rat_UnityBehaviorPilot.asset", "spawnEnemyRat", "M98 Rat Unity Behavior Pilot", EnemyUnityBehaviorPilotKind.Rat);
            var skeletonGraph = CreateOrUpdatePilotGraph("SkeletonSword_UnityBehaviorPilot.asset", "spawnEnemySkeletonSword", "M98 Skeleton Sword Unity Behavior Pilot", EnemyUnityBehaviorPilotKind.SkeletonSword);
            AssignPilotGraph("Assets/_Hollow/Data/Enemies/Enemy_Rat.asset", ratGraph);
            AssignPilotGraph("Assets/_Hollow/Data/Enemies/Enemy_SkeletonSword.asset", skeletonGraph);
            WriteDocs();
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M98 Unity Behavior migration pilot assets.");
        }

        private static EnemyUnityBehaviorPilotGraphDefinition CreateOrUpdatePilotGraph(
            string fileName,
            string spawnKind,
            string displayName,
            EnemyUnityBehaviorPilotKind pilotKind)
        {
            var path = $"{DataFolder}/{fileName}";
            var graph = AssetDatabase.LoadAssetAtPath<EnemyUnityBehaviorPilotGraphDefinition>(path);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<EnemyUnityBehaviorPilotGraphDefinition>();
                AssetDatabase.CreateAsset(graph, path);
            }

            graph.Configure(
                $"m98_{spawnKind}_unity_behavior",
                displayName,
                spawnKind,
                pilotKind,
                graph.BehaviorGraph,
                "Assign an official Unity BehaviorGraph asset here once the visual graph is authored. The deterministic fallback mirrors the intended V1 graph contract.");
            EditorUtility.SetDirty(graph);
            return graph;
        }

        private static void AssignPilotGraph(string enemyPath, EnemyUnityBehaviorPilotGraphDefinition graph)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(enemyPath);
            if (enemy == null)
            {
                return;
            }

            enemy.ConfigureUnityBehaviorGraph(EnemyBehaviorRuntimeMode.UnityBehaviorGraph, graph);
            EditorUtility.SetDirty(enemy);
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M98: Unity Behavior Runtime Migration Pilot V1");
            builder.AppendLine();
            builder.AppendLine("M98 adds Unity's official `com.unity.behavior` package to the M96 bake-off and pilots Unity Behavior as the high-level decision graph for Rat and Skeleton Sword.");
            builder.AppendLine();
            builder.AppendLine("## Contract");
            builder.AppendLine();
            builder.AppendLine("- Hollow remains authoritative for attack profiles, action profiles, spacing profiles, active hit windows, damage, NavMesh locomotion, tactical slots, pressure budgets, saves, and boss exemptions.");
            builder.AppendLine("- Unity Behavior graphs output `EnemyBehaviorCommand` intent only while the enemy is idle.");
            builder.AppendLine("- `EnemyUnityBehaviorGraphBridge` feeds distance, awareness, disposition, endangered state, tactical role, and path status into the graph blackboard.");
            builder.AppendLine("- Official graphs can use custom Hollow nodes or write blackboard outputs: `OutputCommandKind`, `OutputActionId`, `OutputSpeedMultiplier`, `OutputReason`.");
            builder.AppendLine("- If a graph asset is not authored yet, the deterministic pilot fallback preserves the same Rat/Skeleton Sword behavior contract for tests and runtime.");
            builder.AppendLine("- Boss runtime behavior remains unchanged.");
            builder.AppendLine();
            builder.AppendLine("## Pilot Graphs");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Runtime mode | V1 behavior |");
            builder.AppendLine("| --- | --- | --- |");
            builder.AppendLine("| Rat | Unity Behavior Graph | Random idle wander, territorial warning/pressure, bite when engaged and close, skitter/flee after damage or endangered. |");
            builder.AppendLine("| Skeleton Sword | Unity Behavior Graph | Idle/face, close to slash range, start `rusty_slash`; Hollow combo/recovery handles follow-up commitment. |");
            builder.AppendLine();
            builder.AppendLine("## Custom Nodes");
            builder.AppendLine();
            builder.AppendLine("- Conditions: engaged, endangered, should flee, can start action, in action range.");
            builder.AppendLine("- Actions: set command, wander, chase/approach, flee, hold/face, start linked Hollow action.");
            builder.AppendLine();
            builder.AppendLine("## M96 Bake-Off Addition");
            builder.AppendLine();
            builder.AppendLine("`Unity Behavior` is now evaluated separately from paid `Behavior Designer Pro 3`; it is a free official Unity graph/runtime candidate for designer-readable enemy decision flow.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M98 Unity Behavior Runtime Migration Pilot Report

- Package dependency: `{EnemyUnityBehaviorPackageProbe.PackageName}` `{EnemyUnityBehaviorPackageProbe.RequiredVersion}`.
- Package assembly probe: `{EnemyUnityBehaviorPackageProbe.RuntimeAssemblyName}`.
- Pilot enemies: `spawnEnemyRat`, `spawnEnemySkeletonSword`.
- Runtime bridge: `EnemyUnityBehaviorGraphBridge`.
- Deterministic fallback: `EnemyUnityBehaviorPilotEvaluator`.
- Hollow systems retained: M97 NavMesh, M93 threat director, M91 spacing, M80 active windows, M79 contact rules.
- Docs: `{DocsPath}`.
");
        }
    }
}
