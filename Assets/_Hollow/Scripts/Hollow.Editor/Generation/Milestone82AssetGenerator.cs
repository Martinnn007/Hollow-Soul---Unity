using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Hollow.Editor.Generation
{
    public static class Milestone82AssetGenerator
    {
        public const string TreeDirectory = "Assets/_Hollow/Data/EnemyBehaviorTrees/M82";
        public const string DocsPath = "Docs/Hollow_M82_Lightweight_Behavior_Tree_Layer.md";
        public const string ReportPath = "output/reports/m82_lightweight_behavior_tree_layer.md";

        public static readonly string[] PromotedEnemyActionIds =
        {
            "side_pounce",
            "stomp",
            "warning_squeal",
            "side_hop_bite"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 82 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(TreeDirectory);
            Directory.CreateDirectory(Milestone76AssetGenerator.AttackDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            ApplyPromotedAttackProfiles();
            var enemyTrees = GenerateEnemyTrees();
            var bossTrees = GenerateBossMetadataTrees();
            AssignEnemyTrees(enemyTrees);
            AssignBossMetadataTrees(bossTrees);
            WriteDocs();
            WriteReport(enemyTrees.Count, bossTrees.Count);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 82 behavior tree assets.");
        }

        public static IReadOnlyList<EnemyBehaviorTreeDefinition> RuntimeEnemyTreeDefaults()
        {
            return EnemyBehaviorTreeDefaults.EnemyOwnerIds
                .Where(owner => owner != "spawnEnemyBoss")
                .Select(EnemyBehaviorTreeDefaults.CreateEnemyTree)
                .ToArray();
        }

        public static IReadOnlyList<EnemyBehaviorTreeDefinition> RuntimeBossTreeDefaults()
        {
            return EnemyBehaviorTreeDefaults.BossOwnerIds
                .Select(EnemyBehaviorTreeDefaults.CreateBossMetadataTree)
                .ToArray();
        }

        private static void ApplyPromotedAttackProfiles()
        {
            foreach (var spec in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => PromotedEnemyActionIds.Contains(spec.AttackId)))
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

        private static Dictionary<string, EnemyBehaviorTreeDefinition> GenerateEnemyTrees()
        {
            var result = new Dictionary<string, EnemyBehaviorTreeDefinition>();
            foreach (var ownerId in EnemyBehaviorTreeDefaults.EnemyOwnerIds.Where(owner => owner != "spawnEnemyBoss"))
            {
                var tree = EnemyBehaviorTreeDefaults.CreateEnemyTree(ownerId);
                var path = $"{TreeDirectory}/{EnemyBehaviorTreeDefaults.AssetNameForEnemy(ownerId)}";
                SaveTreeAsset(path, tree);
                result[ownerId] = tree;
            }

            return result;
        }

        private static Dictionary<string, EnemyBehaviorTreeDefinition> GenerateBossMetadataTrees()
        {
            var result = new Dictionary<string, EnemyBehaviorTreeDefinition>();
            foreach (var bossId in EnemyBehaviorTreeDefaults.BossOwnerIds)
            {
                var tree = EnemyBehaviorTreeDefaults.CreateBossMetadataTree(bossId);
                var path = $"{TreeDirectory}/{EnemyBehaviorTreeDefaults.AssetNameForBoss(bossId)}";
                SaveTreeAsset(path, tree);
                result[bossId] = tree;
            }

            return result;
        }

        private static void SaveTreeAsset(string path, EnemyBehaviorTreeDefinition tree)
        {
            if (AssetDatabase.LoadAssetAtPath<EnemyBehaviorTreeDefinition>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(tree, path);
            foreach (var node in tree.Nodes)
            {
                if (node == null)
                {
                    continue;
                }

                AssetDatabase.AddObjectToAsset(node, tree);
            }

            EditorUtility.SetDirty(tree);
        }

        private static void AssignEnemyTrees(IReadOnlyDictionary<string, EnemyBehaviorTreeDefinition> trees)
        {
            foreach (var row in Milestone76AssetGenerator.EnemyAssetPathsBySpawnKind())
            {
                if (row.Key == "spawnEnemyBoss" || !trees.TryGetValue(row.Key, out var tree))
                {
                    continue;
                }

                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(row.Value);
                if (enemy == null)
                {
                    continue;
                }

                enemy.ConfigureBehaviorTree(tree);
                EditorUtility.SetDirty(enemy);
            }
        }

        private static void AssignBossMetadataTrees(IReadOnlyDictionary<string, EnemyBehaviorTreeDefinition> trees)
        {
            foreach (var row in Milestone75AssetGenerator.BossRows())
            {
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>($"{Milestone53AssetGenerator.BossDirectory}/{row.FileName}");
                if (boss == null || !trees.TryGetValue(boss.BossId, out var tree))
                {
                    continue;
                }

                boss.ConfigureBehaviorTreeMetadata(tree);
                EditorUtility.SetDirty(boss);
            }
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M82: Lightweight Behavior Tree Layer V1");
            builder.AppendLine();
            builder.AppendLine("M82 moves normal enemy decisions into authored ScriptableObject behavior trees while keeping M80 attack execution authoritative. Trees choose only from idle; once an attack enters windup, active, or recovery, the runner stops replanning until the committed action finishes.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine("- Non-boss enemies resolve an `EnemyBehaviorTreeDefinition` from authored assets or runtime defaults.");
            builder.AppendLine("- The tree context includes awareness, intelligence, disposition, distance, preferred range bands, recent damage/endangered state, spawn index, current readability state, and room attack budget availability.");
            builder.AppendLine("- Commands are intentionally small: hold, move, preferred-range movement, flee, wander, face player, start linked action, warning feint, or no-op.");
            builder.AppendLine("- Attack commands only start from `EnemyReadabilityState.Idle`; M80 owns windup, active, and recovery after commitment.");
            builder.AppendLine("- Existing melee and ranged/charge budgets remain authoritative. Tactical/Cunning intelligence only improves priority tie-breaking and does not increase total room pressure.");
            builder.AppendLine("- Boss trees are metadata-only in M82 and are ignored by boss runtime behavior.");
            builder.AppendLine();
            builder.AppendLine("## Promoted Prototype Actions");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Action | Runtime Kind | Damage | Purpose |");
            builder.AppendLine("| --- | --- | --- | ---: | --- |");
            builder.AppendLine("| Fast Chaser | `side_pounce` | MeleeLunge | 1 | Committed lateral pounce prototype. |");
            builder.AppendLine("| Heavy Chaser | `stomp` | Area | 2 | Circular high-commitment punishable impact. |");
            builder.AppendLine("| Rat | `warning_squeal` | Movement | 0 | Non-damaging territorial warning before bite pressure. |");
            builder.AppendLine("| Spider | `side_hop_bite` | MeleeLunge | 1 | Quick side-hop bite chosen by deterministic weighted tree. |");
            builder.AppendLine();
            builder.AppendLine("## Runtime Enemy Trees");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Tree | Primary Decisions |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss"))
            {
                var tree = enemy.BehaviorTree;
                builder.AppendLine($"| {enemy.DisplayName} | `{tree.TreeId}` | {DecisionSummary(enemy.SpawnKind)} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Boss Metadata Trees");
            builder.AppendLine();
            builder.AppendLine("Boss definitions resolve metadata-only trees for documentation and M82 validation. Boss runtime remains unchanged.");
            builder.AppendLine();
            builder.AppendLine("| Boss | Tree | Runtime |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                var tree = boss.BehaviorTreeMetadata;
                builder.AppendLine($"| {boss.DisplayName} | `{tree.TreeId}` | metadata-only, ignored by runtime |");
            }

            builder.AppendLine();
            builder.AppendLine("## Deferred");
            builder.AppendLine();
            builder.AppendLine("- No pathfinding, obstacle LOS, squad tactics, alert sharing, save schema changes, generic combo planner, or boss behavior rewrite is included.");
            builder.AppendLine("- Future milestones can expand action selection with richer conditions and navigation adapters without replacing M80 committed attack windows.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport(int enemyTreeCount, int bossTreeCount)
        {
            File.WriteAllText(ReportPath, $@"# M82 Lightweight Behavior Tree Layer Report

- Runtime non-boss trees: {enemyTreeCount}.
- Metadata-only boss trees: {bossTreeCount}.
- Promoted actions: `{string.Join("`, `", PromotedEnemyActionIds)}`.
- Attack execution remains M80-owned after idle-gated commitment.
- Contact damage remains M79 active-only for the current roster.
- Documentation: `{DocsPath}`.
- Report: `{ReportPath}`.
");
        }

        private static string DecisionSummary(string spawnKind)
        {
            return spawnKind switch
            {
                "spawnEnemyNormal" => "claw/bite commitment, then preferred-range pressure",
                "spawnEnemyFlying" => "prey flee/wander until endangered or engaged",
                "spawnEnemyFast" => "weighted side pounce, quick pounce, snap follow-up",
                "spawnEnemyHeavy" => "stomp, maul, shove, punishable pressure",
                "spawnEnemyCharger" => "charge first, close clash, direct pressure",
                "spawnEnemyTurret" => "stationary sentinel ranged budget",
                "spawnEnemySplitter" => "splinter lunge or cleave",
                "spawnEnemySpittingPod" => "stationary hearing-driven ballistic lob",
                "spawnEnemyRat" => "warning squeal, bite, retreat when endangered",
                "spawnEnemySpider" => "deterministic fight/flee, side-hop bite, hop, bite",
                _ => "fallback preferred-range pressure"
            };
        }
    }
}
