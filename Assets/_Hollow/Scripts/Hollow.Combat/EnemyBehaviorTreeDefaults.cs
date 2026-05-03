using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public static class EnemyBehaviorTreeDefaults
    {
        private static readonly string[] BossIds =
        {
            "stone_warden",
            "splinter_saint",
            "gravel_maw",
            "cartouche_widow",
            "iron_reliquary",
            "mirror_husk",
            "ash_comet",
            "choir_of_teeth",
            "rust_bishop",
            "hollow_star_larva"
        };

        public static IReadOnlyList<string> EnemyOwnerIds { get; } = new[]
        {
            "spawnEnemyNormal",
            "spawnEnemyFlying",
            "spawnEnemyFast",
            "spawnEnemyHeavy",
            "spawnEnemyCharger",
            "spawnEnemyTurret",
            "spawnEnemySplitter",
            "spawnEnemySpittingPod",
            "spawnEnemyRat",
            "spawnEnemySpider",
            "spawnEnemyBoss"
        };

        public static IReadOnlyList<string> BossOwnerIds => BossIds;

        public static EnemyBehaviorTreeDefinition CreateEnemyTree(string spawnKind)
        {
            var builder = new TreeBuilder(spawnKind, isBoss: false);
            var root = spawnKind switch
            {
                "spawnEnemyFlying" => BuildFlying(builder),
                "spawnEnemyFast" => BuildFast(builder),
                "spawnEnemyHeavy" => BuildHeavy(builder),
                "spawnEnemyCharger" => BuildCharger(builder),
                "spawnEnemyTurret" => BuildTurret(builder),
                "spawnEnemySplitter" => BuildSplitter(builder),
                "spawnEnemySpittingPod" => BuildPod(builder),
                "spawnEnemyRat" => BuildRat(builder),
                "spawnEnemySpider" => BuildSpider(builder),
                "spawnEnemyBoss" => BuildBossFallback(builder),
                _ => BuildNormal(builder)
            };

            return builder.Build(root, OwnerLabel(spawnKind), bossMetadataOnly: false);
        }

        public static EnemyBehaviorTreeDefinition CreateBossMetadataTree(string bossId)
        {
            var builder = new TreeBuilder(bossId, isBoss: true);
            var root = builder.Selector(
                "boss_metadata_root",
                builder.Sequence(
                    "boss_metadata_current_actions",
                    builder.Condition("boss_metadata_idle", EnemyBehaviorConditionKind.IsIdle),
                    builder.Action("boss_metadata_noop", EnemyBehaviorCommandKind.None, reason: "Boss tree is metadata-only in M82.")));
            return builder.Build(root, BossLabel(bossId), bossMetadataOnly: true);
        }

        public static EnemyBehaviorTreeDefinition ResolveEnemyTree(string spawnKind)
        {
            return EnemyOwnerIds.Contains(spawnKind) ? CreateEnemyTree(spawnKind) : CreateEnemyTree("spawnEnemyNormal");
        }

        public static EnemyBehaviorTreeDefinition ResolveBossTree(string bossId)
        {
            return CreateBossMetadataTree(bossId);
        }

        public static string AssetNameForEnemy(string spawnKind)
        {
            return $"EnemyTree_{spawnKind}.asset";
        }

        public static string AssetNameForBoss(string bossId)
        {
            return $"BossTree_{bossId}.asset";
        }

        private static EnemyBehaviorTreeNodeDefinition BuildNormal(TreeBuilder b)
        {
            return b.Selector(
                "normal_root",
                b.Sequence("normal_claw", b.CanMelee("claw_lunge"), b.StartMelee("claw_lunge")),
                b.Sequence("normal_bite", b.CanMelee("desperate_bite"), b.StartMelee("desperate_bite")),
                b.Action("normal_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 1f, reason: "Normal chaser respects range before committing."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildFlying(TreeBuilder b)
        {
            return b.Selector(
                "flying_root",
                b.Sequence("flying_dive", b.Condition("flying_endangered", EnemyBehaviorConditionKind.IsEndangered), b.CanMelee("dive_scratch"), b.StartMelee("dive_scratch")),
                b.Sequence("flying_peck", b.Condition("flying_engaged", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Engaged), b.CanMelee("panic_peck"), b.StartMelee("panic_peck")),
                b.Sequence("flying_endangered_pressure", b.Condition("flying_endangered_approach", EnemyBehaviorConditionKind.IsEndangered), b.Action("flying_pressure", EnemyBehaviorCommandKind.MoveToPlayer, speed: 0.85f, reason: "Endangered prey briefly pressures instead of only fleeing.")),
                b.Sequence("flying_flee_close", b.Condition("flying_too_close", EnemyBehaviorConditionKind.DistanceLessOrEqual, threshold: 4.25f), b.Action("flying_flee", EnemyBehaviorCommandKind.Flee, speed: 0.9f, reason: "Prey keeps space until endangered.")),
                b.Action("flying_wander", EnemyBehaviorCommandKind.Wander, speed: 0.85f, reason: "Prey drifts while not committed."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildFast(TreeBuilder b)
        {
            return b.Selector(
                "fast_root",
                b.Weighted(
                    "fast_attack_choice",
                    (b.Sequence("fast_side_pounce", b.CanMelee("side_pounce"), b.StartMelee("side_pounce")), 2.2f),
                    (b.Sequence("fast_quick_pounce", b.CanMelee("quick_pounce"), b.StartMelee("quick_pounce")), 1.4f),
                    (b.Sequence("fast_snap", b.CanMelee("snap_followup"), b.StartMelee("snap_followup")), 0.8f)),
                b.Action("fast_pressure", EnemyBehaviorCommandKind.MovePreferredRange, speed: 1.1f, reason: "Fast chaser pressures but waits for active windows."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildHeavy(TreeBuilder b)
        {
            return b.Selector(
                "heavy_root",
                b.Sequence("heavy_stomp", b.CanArea("stomp"), b.StartArea("stomp")),
                b.Sequence("heavy_maul", b.CanMelee("maul_lunge"), b.StartMelee("maul_lunge")),
                b.Sequence("heavy_shove", b.CanMelee("heavy_shove"), b.StartMelee("heavy_shove")),
                b.Action("heavy_pressure", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.85f, reason: "Heavy chaser advances into punishable commitment."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildCharger(TreeBuilder b)
        {
            return b.Selector(
                "charger_root",
                b.Sequence("charger_charge", b.Condition("charger_can_charge", EnemyBehaviorConditionKind.CanStartChargeAttack), b.Action("charger_start", EnemyBehaviorCommandKind.StartChargeAction, "ash_charge", reason: "Ash Charger commits to charge.")),
                b.Sequence("charger_clash", b.CanMelee("ember_clash"), b.StartMelee("ember_clash")),
                b.Action("charger_pressure", EnemyBehaviorCommandKind.MoveToPlayer, speed: 0.95f, reason: "Charger lines up pressure."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildTurret(TreeBuilder b)
        {
            return b.Selector(
                "turret_root",
                b.Sequence("turret_shot", b.Condition("turret_engage", EnemyBehaviorConditionKind.ShouldSentinelEngage), b.Condition("turret_can_shoot", EnemyBehaviorConditionKind.CanStartRangedAttack), b.Action("turret_start_shot", EnemyBehaviorCommandKind.StartRangedAction, "bone_dart", reason: "Bone Turret fires from stationary sentinel tree.")),
                b.Action("turret_hold", EnemyBehaviorCommandKind.Hold, reason: "Bone Turret remains stationary."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildSplitter(TreeBuilder b)
        {
            return b.Selector(
                "splitter_root",
                b.Sequence("splitter_lunge", b.CanMelee("splinter_lunge"), b.StartMelee("splinter_lunge")),
                b.Sequence("splitter_cleave", b.CanMelee("husk_cleave"), b.StartMelee("husk_cleave")),
                b.Action("splitter_pressure", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.95f, reason: "Splitter closes for a readable cleave/lunge."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildPod(TreeBuilder b)
        {
            return b.Selector(
                "pod_root",
                b.Sequence("pod_lob", b.Condition("pod_engage", EnemyBehaviorConditionKind.ShouldSentinelEngage), b.Condition("pod_can_shoot", EnemyBehaviorConditionKind.CanStartRangedAttack), b.Action("pod_start_lob", EnemyBehaviorCommandKind.StartRangedAction, "spit_lob", reason: "Spitting Pod lobs from stationary hearing-driven sentinel tree.")),
                b.Action("pod_hold", EnemyBehaviorCommandKind.Hold, reason: "Spitting Pod is stationary."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildRat(TreeBuilder b)
        {
            return b.Selector(
                "rat_root",
                b.Sequence("rat_retreat", b.Condition("rat_endangered", EnemyBehaviorConditionKind.IsEndangered), b.Action("rat_flee", EnemyBehaviorCommandKind.Flee, speed: 1.15f, reason: "Rat retreats readily after damage or close threat.")),
                b.Sequence("rat_warning", b.Condition("rat_alerted", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Alerted), b.Condition("rat_close_warning", EnemyBehaviorConditionKind.DistanceLessOrEqual, threshold: 2.6f), b.Condition("rat_bite_not_ready", EnemyBehaviorConditionKind.CanStartMeleeAction, invert: true, actionId: "rat_bite"), b.Action("rat_warn", EnemyBehaviorCommandKind.StartFeintWarning, "warning_squeal", reason: "Rat warns before committing.")),
                b.Sequence("rat_bite", b.CanMelee("rat_bite"), b.StartMelee("rat_bite")),
                b.Action("rat_wander", EnemyBehaviorCommandKind.Wander, speed: 0.95f, reason: "Rat roams unpredictably until committed."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildSpider(TreeBuilder b)
        {
            return b.Selector(
                "spider_root",
                b.Weighted(
                    "spider_fight_flight",
                    (b.Sequence("spider_side_bite", b.Condition("spider_fight_roll", EnemyBehaviorConditionKind.DeterministicChance, threshold: 0.75f), b.CanMelee("side_hop_bite"), b.StartMelee("side_hop_bite")), 1.8f),
                    (b.Sequence("spider_hop", b.Condition("spider_hop_roll", EnemyBehaviorConditionKind.DeterministicChance, threshold: 0.55f), b.CanMelee("startle_hop"), b.StartMelee("startle_hop")), 1.4f),
                    (b.Sequence("spider_bite", b.CanMelee("close_bite"), b.StartMelee("close_bite")), 0.8f)),
                b.Action("spider_flee", EnemyBehaviorCommandKind.Flee, speed: 1.1f, reason: "Spider often chooses flight."),
                b.Action("spider_wander", EnemyBehaviorCommandKind.Wander, speed: 1f, reason: "Spider skitters erratically."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildBossFallback(TreeBuilder b)
        {
            return b.Selector(
                "boss_spawn_root",
                b.Sequence("boss_spawn_charge", b.Condition("boss_spawn_can_charge", EnemyBehaviorConditionKind.CanStartChargeAttack), b.Action("boss_spawn_start_charge", EnemyBehaviorCommandKind.StartChargeAction, "stone_charge")),
                b.Sequence("boss_spawn_burst", b.Condition("boss_spawn_can_ranged", EnemyBehaviorConditionKind.CanStartRangedAttack), b.Action("boss_spawn_start_burst", EnemyBehaviorCommandKind.StartRangedAction, "stone_four_way_burst")),
                b.Action("boss_spawn_pressure", EnemyBehaviorCommandKind.MoveToPlayer, speed: 0.8f));
        }

        private static string OwnerLabel(string ownerId)
        {
            return ownerId switch
            {
                "spawnEnemyNormal" => "Normal Chaser",
                "spawnEnemyFlying" => "Flying Chaser",
                "spawnEnemyFast" => "Fast Chaser",
                "spawnEnemyHeavy" => "Heavy Chaser",
                "spawnEnemyCharger" => "Ash Charger",
                "spawnEnemyTurret" => "Bone Turret",
                "spawnEnemySplitter" => "Husk Splitter",
                "spawnEnemySpittingPod" => "Spitting Pod",
                "spawnEnemyRat" => "Rat",
                "spawnEnemySpider" => "Spider",
                "spawnEnemyBoss" => "Generic Boss Spawn",
                _ => ownerId
            };
        }

        private static string BossLabel(string bossId)
        {
            return bossId switch
            {
                "stone_warden" => "Stone Warden",
                "splinter_saint" => "Splinter Saint",
                "gravel_maw" => "Gravel Maw",
                "cartouche_widow" => "Cartouche Widow",
                "iron_reliquary" => "Iron Reliquary",
                "mirror_husk" => "Mirror Husk",
                "ash_comet" => "Ash Comet",
                "choir_of_teeth" => "Choir of Teeth",
                "rust_bishop" => "Rust Bishop",
                "hollow_star_larva" => "Hollow Star Larva",
                _ => bossId
            };
        }

        private sealed class TreeBuilder
        {
            private readonly string ownerId;
            private readonly bool isBoss;
            private readonly List<EnemyBehaviorTreeNodeDefinition> nodes = new();

            public TreeBuilder(string ownerId, bool isBoss)
            {
                this.ownerId = ownerId;
                this.isBoss = isBoss;
            }

            public EnemyBehaviorTreeDefinition Build(EnemyBehaviorTreeNodeDefinition root, string ownerLabel, bool bossMetadataOnly)
            {
                var tree = ScriptableObject.CreateInstance<EnemyBehaviorTreeDefinition>();
                tree.Configure(
                    $"{(isBoss ? "boss" : "enemy")}_{ownerId}_m82_tree",
                    $"{ownerLabel} M82 Behavior Tree",
                    ownerId,
                    bossMetadataOnly,
                    root,
                    nodes);
                return tree;
            }

            public EnemyBehaviorSelectorNodeDefinition Selector(string id, params EnemyBehaviorTreeNodeDefinition[] children)
            {
                var node = ScriptableObject.CreateInstance<EnemyBehaviorSelectorNodeDefinition>();
                node.Configure(id, children);
                nodes.Add(node);
                return node;
            }

            public EnemyBehaviorSequenceNodeDefinition Sequence(string id, params EnemyBehaviorTreeNodeDefinition[] children)
            {
                var node = ScriptableObject.CreateInstance<EnemyBehaviorSequenceNodeDefinition>();
                node.Configure(id, children);
                nodes.Add(node);
                return node;
            }

            public EnemyBehaviorWeightedSelectorNodeDefinition Weighted(string id, params (EnemyBehaviorTreeNodeDefinition Child, float Weight)[] children)
            {
                var node = ScriptableObject.CreateInstance<EnemyBehaviorWeightedSelectorNodeDefinition>();
                node.Configure(id, children);
                nodes.Add(node);
                return node;
            }

            public EnemyBehaviorConditionNodeDefinition Condition(
                string id,
                EnemyBehaviorConditionKind condition,
                bool invert = false,
                float threshold = 0f,
                string actionId = "",
                EnemyAwarenessState awareness = EnemyAwarenessState.Engaged,
                EnemyInstinctDisposition disposition = EnemyInstinctDisposition.Predator,
                EnemyIntelligenceLevel intelligence = EnemyIntelligenceLevel.Instinctive,
                EnemyBehaviorId behavior = EnemyBehaviorId.Chaser)
            {
                var node = ScriptableObject.CreateInstance<EnemyBehaviorConditionNodeDefinition>();
                node.Configure(id, condition, invert, threshold, actionId, awareness, disposition, intelligence, behavior);
                nodes.Add(node);
                return node;
            }

            public EnemyBehaviorActionNodeDefinition Action(string id, EnemyBehaviorCommandKind command, string actionId = "", float speed = 1f, string reason = "")
            {
                var node = ScriptableObject.CreateInstance<EnemyBehaviorActionNodeDefinition>();
                node.Configure(id, command, actionId, speed, reason);
                nodes.Add(node);
                return node;
            }

            public EnemyBehaviorConditionNodeDefinition CanMelee(string actionId)
            {
                return Condition($"can_melee_{actionId}", EnemyBehaviorConditionKind.CanStartMeleeAction, actionId: actionId);
            }

            public EnemyBehaviorConditionNodeDefinition CanArea(string actionId)
            {
                return Condition($"can_area_{actionId}", EnemyBehaviorConditionKind.CanStartAreaAction, actionId: actionId);
            }

            public EnemyBehaviorActionNodeDefinition StartMelee(string actionId)
            {
                return Action($"start_melee_{actionId}", EnemyBehaviorCommandKind.StartMeleeAction, actionId, reason: $"Start melee action {actionId}.");
            }

            public EnemyBehaviorActionNodeDefinition StartArea(string actionId)
            {
                return Action($"start_area_{actionId}", EnemyBehaviorCommandKind.StartAreaAction, actionId, reason: $"Start area action {actionId}.");
            }
        }
    }
}
