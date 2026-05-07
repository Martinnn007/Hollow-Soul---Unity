using System.IO;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone104UnityBehaviorPilotHardeningAssetGenerator
    {
        public const string DataFolder = Milestone98UnityBehaviorMigrationAssetGenerator.DataFolder;
        public const string DocsPath = "Docs/Hollow_M104_Unity_Behavior_Pilot_Hardening.md";
        public const string ReportPath = "output/reports/m104_unity_behavior_pilot_hardening.md";

        [MenuItem("Hollow/Generation/Generate Milestone 104 Unity Behavior Pilot Hardening Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var ratGraph = CreateOrUpdatePilotGraph(
                "Rat_UnityBehaviorPilot.asset",
                "spawnEnemyRat",
                "M104 Rat Unity Behavior Pilot",
                EnemyUnityBehaviorPilotKind.Rat,
                "Rat graph contract: wander, warn/pressure, bite through Hollow action data, and flee/retreat when endangered. Official graph asset is preferred; emergency fallback is trace-visible.");
            var skeletonGraph = CreateOrUpdatePilotGraph(
                "SkeletonSword_UnityBehaviorPilot.asset",
                "spawnEnemySkeletonSword",
                "M104 Skeleton Sword Unity Behavior Pilot",
                EnemyUnityBehaviorPilotKind.SkeletonSword,
                "Skeleton Sword graph contract: face/approach, start rusty_slash only while idle, and let Hollow handle combo, recovery, active hit windows, NavMesh, and pressure slots.");

            AssignPilotGraph("Assets/_Hollow/Data/Enemies/Enemy_Rat.asset", ratGraph);
            AssignPilotGraph("Assets/_Hollow/Data/Enemies/Enemy_SkeletonSword.asset", skeletonGraph);
            WriteDocs();
            WriteReport(ratGraph, skeletonGraph);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M104 Unity Behavior pilot hardening assets.");
        }

        private static EnemyUnityBehaviorPilotGraphDefinition CreateOrUpdatePilotGraph(
            string fileName,
            string spawnKind,
            string displayName,
            EnemyUnityBehaviorPilotKind pilotKind,
            string notes)
        {
            var path = $"{DataFolder}/{fileName}";
            var graph = AssetDatabase.LoadAssetAtPath<EnemyUnityBehaviorPilotGraphDefinition>(path);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<EnemyUnityBehaviorPilotGraphDefinition>();
                AssetDatabase.CreateAsset(graph, path);
            }

            graph.ConfigureHardened(
                $"m104_{spawnKind}_unity_behavior",
                displayName,
                spawnKind,
                pilotKind,
                graph.BehaviorGraph,
                EnemyUnityBehaviorFallbackPolicy.EmergencyOnly,
                true,
                notes);
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
            builder.AppendLine("# M104: Unity Behavior Pilot Hardening");
            builder.AppendLine();
            builder.AppendLine("M104 hardens the Rat and Skeleton Sword Unity Behavior pilot so it behaves like a real migration contract instead of a silent deterministic fallback.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine("- Rat and Skeleton Sword are the hardened first pilot enemies; later family migrations may route additional non-boss enemies through the same bridge.");
            builder.AppendLine("- Hollow remains authoritative for attack/action/spacing profiles, active hit windows, damage, NavMesh locomotion, tactical pressure, saves, and boss exemptions.");
            builder.AppendLine("- Unity Behavior graphs output commands only while the enemy is idle; windup, active, recovery, stun, death, knockback, lunges, charges, and combos stay locked in Hollow runtime.");
            builder.AppendLine("- Emergency fallback is allowed only when the official graph is missing, uncompiled, missing required variables, or throws during evaluation.");
            builder.AppendLine("- Every emergency fallback evaluation is trace-visible through `EnemyUnityBehaviorGraphBridge.TraceHistory` and `UsedEmergencyFallbackLastEvaluation`.");
            builder.AppendLine();
            builder.AppendLine("## Stable Blackboard Schema");
            builder.AppendLine();
            builder.AppendLine("| Direction | Variables |");
            builder.AppendLine("| --- | --- |");
            builder.AppendLine("| Inputs | `DistanceToPlayer`, `Awareness`, `Disposition`, `Endangered`, `IsIdle`, `TacticalRole`, `PathStatus` |");
            builder.AppendLine("| Optional inputs | `Enemy`, `Player`, `TimeSeconds`, `DeltaTime` |");
            builder.AppendLine("| Outputs | `OutputCommandKind`, `OutputActionId`, `OutputSpeedMultiplier`, `OutputReason` |");
            builder.AppendLine();
            builder.AppendLine("## Pilot Graphs");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Expected graph behavior | Emergency fallback |");
            builder.AppendLine("| --- | --- | --- |");
            builder.AppendLine("| Rat | Wander, warn, pressure territory, bite when committed, flee when damaged/endangered. | Same command sequence, marked `unity_behavior_emergency_fallback`. |");
            builder.AppendLine("| Skeleton Sword | Face/approach, start `rusty_slash` from idle, Hollow handles combo/recovery. | Same command sequence, marked `unity_behavior_emergency_fallback`. |");
            builder.AppendLine();
            builder.AppendLine("## Validation");
            builder.AppendLine();
            builder.AppendLine("The M104 validator checks package availability, schema version, required input/output variable names, emergency fallback policy, pilot enemy wiring, docs/report artifacts, and runtime trace hooks.");
            builder.AppendLine();
            builder.AppendLine("## Authoring Note");
            builder.AppendLine();
            builder.AppendLine("Official Unity Behavior graph assets should be authored in Unity's Behavior Graph editor and assigned into the Rat/Skeleton pilot definitions. Until those graph assets are compiled and contain the required schema, the runtime uses the emergency guard and reports it explicitly.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport(
            EnemyUnityBehaviorPilotGraphDefinition ratGraph,
            EnemyUnityBehaviorPilotGraphDefinition skeletonGraph)
        {
            File.WriteAllText(ReportPath, $@"# M104 Unity Behavior Pilot Hardening Report

- Package: `{EnemyUnityBehaviorPackageProbe.PackageName}` `{EnemyUnityBehaviorPackageProbe.RequiredVersion}`.
- Schema version: `{EnemyUnityBehaviorBlackboardSchema.SchemaVersion}`.
- Runtime bridge: `EnemyUnityBehaviorGraphBridge`.
- Stable schema source: `EnemyUnityBehaviorBlackboardSchema`.
- Trace source: `EnemyUnityBehaviorTraceEntry`.
- Rat graph contract: `{ratGraph.GraphId}` (`{ratGraph.FallbackPolicy}`).
- Skeleton Sword graph contract: `{skeletonGraph.GraphId}` (`{skeletonGraph.FallbackPolicy}`).
- Required outputs: `OutputCommandKind`, `OutputActionId`, `OutputSpeedMultiplier`, `OutputReason`.
- Emergency fallback: explicit and trace-visible; no silent deterministic override.
- Boss runtime: unchanged.
- Docs: `{DocsPath}`.
");
        }
    }
}
