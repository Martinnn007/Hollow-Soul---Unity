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
            "spawnEnemyHollowBird",
            "spawnEnemyHollowBeast",
            "spawnEnemySkeletonSword",
            "spawnEnemySkeletonSpear",
            "spawnEnemyKnight",
            "spawnEnemyGiant",
            "spawnEnemyHollowArcher",
            "spawnEnemyPowderGunner",
            "spawnEnemyKnifeThrower",
            "spawnEnemyRepeaterTurret",
            "spawnEnemyClockworkSentry",
            "spawnEnemyHollowAcolyte",
            "spawnEnemyWraith",
            "spawnEnemySoulEater",
            "spawnEnemyCurseBinder",
            "spawnEnemyGraveLantern",
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
                "spawnEnemyHollowBird" => BuildHollowBird(builder),
                "spawnEnemyHollowBeast" => BuildHollowBeast(builder),
                "spawnEnemySkeletonSword" => BuildSkeletonSword(builder),
                "spawnEnemySkeletonSpear" => BuildSkeletonSpear(builder),
                "spawnEnemyKnight" => BuildKnight(builder),
                "spawnEnemyGiant" => BuildGiant(builder),
                "spawnEnemyHollowArcher" => BuildHollowArcher(builder),
                "spawnEnemyPowderGunner" => BuildPowderGunner(builder),
                "spawnEnemyKnifeThrower" => BuildKnifeThrower(builder),
                "spawnEnemyRepeaterTurret" => BuildRepeaterTurret(builder),
                "spawnEnemyClockworkSentry" => BuildClockworkSentry(builder),
                "spawnEnemyHollowAcolyte" => BuildHollowAcolyte(builder),
                "spawnEnemyWraith" => BuildWraith(builder),
                "spawnEnemySoulEater" => BuildSoulEater(builder),
                "spawnEnemyCurseBinder" => BuildCurseBinder(builder),
                "spawnEnemyGraveLantern" => BuildGraveLantern(builder),
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
                b.Sequence("normal_warning_feint", b.Condition("normal_alerted", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Alerted), b.CanCreatureSignal("warning_feint"), b.StartCreatureSignal("warning_feint")),
                b.Sequence("normal_claw", b.CanMelee("claw_lunge"), b.StartMelee("claw_lunge")),
                b.Sequence("normal_bite", b.CanMelee("desperate_bite"), b.StartMelee("desperate_bite")),
                b.Sequence("normal_backstep_close", b.Condition("normal_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("short_backstep"), b.StartCreatureMove("short_backstep")),
                b.Action("normal_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 1f, reason: "Normal chaser respects range before committing."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildFlying(TreeBuilder b)
        {
            return b.Selector(
                "flying_root",
                b.Sequence("flying_dive", b.Condition("flying_endangered", EnemyBehaviorConditionKind.IsEndangered), b.CanMelee("dive_scratch"), b.StartMelee("dive_scratch")),
                b.Sequence("flying_peck", b.Condition("flying_engaged", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Engaged), b.CanMelee("panic_peck"), b.StartMelee("panic_peck")),
                b.Sequence("flying_strafe", b.Condition("flying_alerted_strafe", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Alerted), b.CanCreatureMove("fly_strafe"), b.StartCreatureMove("fly_strafe")),
                b.Sequence("flying_feint", b.Condition("flying_alerted_feint", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Alerted), b.CanCreatureSignal("dive_feint"), b.StartCreatureSignal("dive_feint")),
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
                    (b.Sequence("fast_snap_combo", b.CanMelee("snap_combo"), b.StartMelee("snap_combo")), 1.1f),
                    (b.Sequence("fast_snap", b.CanMelee("snap_followup"), b.StartMelee("snap_followup")), 0.8f)),
                b.Sequence("fast_skitter_close", b.Condition("fast_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("evasive_skitter"), b.StartCreatureMove("evasive_skitter")),
                b.Action("fast_pressure", EnemyBehaviorCommandKind.MovePreferredRange, speed: 1.1f, reason: "Fast chaser pressures but waits for active windows."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildHeavy(TreeBuilder b)
        {
            return b.Selector(
                "heavy_root",
                b.Sequence("heavy_slow_overhead", b.CanArea("slow_overhead_slam"), b.StartArea("slow_overhead_slam")),
                b.Sequence("heavy_stomp", b.CanArea("stomp"), b.StartArea("stomp")),
                b.Sequence("heavy_maul", b.CanMelee("maul_lunge"), b.StartMelee("maul_lunge")),
                b.Sequence("heavy_guarded_shove", b.CanMelee("guarded_shove"), b.StartMelee("guarded_shove")),
                b.Sequence("heavy_shove", b.CanMelee("heavy_shove"), b.StartMelee("heavy_shove")),
                b.Action("heavy_pressure", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.85f, reason: "Heavy chaser advances into punishable commitment."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildCharger(TreeBuilder b)
        {
            return b.Selector(
                "charger_root",
                b.Sequence("charger_charge", b.Condition("charger_can_charge", EnemyBehaviorConditionKind.CanStartChargeAttack), b.Action("charger_start", EnemyBehaviorCommandKind.StartChargeAction, "ash_charge", reason: "Ash Charger commits to charge.")),
                b.Sequence("charger_shoulder_check", b.CanMelee("shoulder_check"), b.StartMelee("shoulder_check")),
                b.Sequence("charger_clash", b.CanMelee("ember_clash"), b.StartMelee("ember_clash")),
                b.Sequence("charger_recover_hop", b.Condition("charger_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("short_recover_hop"), b.StartCreatureMove("short_recover_hop")),
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
                b.Sequence("splitter_feint", b.Condition("splitter_alerted", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Alerted), b.CanCreatureSignal("cleave_feint"), b.StartCreatureSignal("cleave_feint")),
                b.Sequence("splitter_lunge", b.CanMelee("splinter_lunge"), b.StartMelee("splinter_lunge")),
                b.Sequence("splitter_cleave", b.CanMelee("husk_cleave"), b.StartMelee("husk_cleave")),
                b.Sequence("splitter_backstep_close", b.Condition("splitter_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("splitter_backstep"), b.StartCreatureMove("splitter_backstep")),
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
                b.Sequence("rat_retreat", b.Condition("rat_endangered", EnemyBehaviorConditionKind.IsEndangered), b.CanCreatureMove("skitter_retreat"), b.StartCreatureMove("skitter_retreat")),
                b.Sequence("rat_alarm", b.Condition("rat_engaged_alarm", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Engaged), b.CanCreatureSignal("alarm_squeal"), b.StartCreatureSignal("alarm_squeal")),
                b.Sequence("rat_warning", b.Condition("rat_alerted", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Alerted), b.Condition("rat_close_warning", EnemyBehaviorConditionKind.DistanceLessOrEqual, threshold: 2.6f), b.Action("rat_start_warning", EnemyBehaviorCommandKind.StartFeintWarning, "warning_squeal", reason: "warning_squeal")),
                b.Sequence("rat_panic_pounce", b.Condition("rat_panic_roll", EnemyBehaviorConditionKind.DeterministicChance, threshold: 0.45f), b.CanMelee("panic_pounce"), b.StartMelee("panic_pounce")),
                b.Sequence("rat_bite", b.CanMelee("rat_bite"), b.StartMelee("rat_bite")),
                b.Action("rat_wander", EnemyBehaviorCommandKind.Wander, speed: 0.95f, reason: "Rat roams unpredictably until committed."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildSpider(TreeBuilder b)
        {
            return b.Selector(
                "spider_root",
                b.Weighted(
                    "spider_fight_flight",
                    (b.Sequence("spider_panic_flee", b.Condition("spider_flee_roll", EnemyBehaviorConditionKind.DeterministicChance, threshold: 0.45f), b.CanCreatureMove("panic_flee"), b.StartCreatureMove("panic_flee")), 1.5f),
                    (b.Sequence("spider_web_feint", b.Condition("spider_feint_roll", EnemyBehaviorConditionKind.DeterministicChance, threshold: 0.35f), b.CanCreatureSignal("web_feint"), b.StartCreatureSignal("web_feint")), 1.2f),
                    (b.Sequence("spider_side_bite", b.Condition("spider_fight_roll", EnemyBehaviorConditionKind.DeterministicChance, threshold: 0.65f), b.CanMelee("side_hop_bite"), b.StartMelee("side_hop_bite")), 2.2f),
                    (b.Sequence("spider_hop", b.Condition("spider_hop_roll", EnemyBehaviorConditionKind.DeterministicChance, threshold: 0.55f), b.CanMelee("startle_hop"), b.StartMelee("startle_hop")), 1.4f),
                    (b.Sequence("spider_bite", b.CanMelee("close_bite"), b.StartMelee("close_bite")), 0.8f)),
                b.Action("spider_flee", EnemyBehaviorCommandKind.Flee, speed: 1.1f, reason: "Spider often chooses flight."),
                b.Action("spider_wander", EnemyBehaviorCommandKind.Wander, speed: 1f, reason: "Spider skitters erratically."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildHollowBird(TreeBuilder b)
        {
            return b.Selector(
                "hollow_bird_root",
                b.Sequence("hollow_bird_caw", b.Condition("hollow_bird_alerted", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Alerted), b.CanCreatureSignal("caw_signal"), b.StartCreatureSignal("caw_signal")),
                b.Sequence("hollow_bird_retreat_close", b.Condition("hollow_bird_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("wing_retreat"), b.StartCreatureMove("wing_retreat")),
                b.Weighted(
                    "hollow_bird_attack_choice",
                    (b.Sequence("hollow_bird_claw_dive", b.CanMelee("claw_dive"), b.StartMelee("claw_dive")), 1.4f),
                    (b.Sequence("hollow_bird_swoop_peck", b.CanMelee("swoop_peck"), b.StartMelee("swoop_peck")), 1.1f)),
                b.Action("hollow_bird_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.95f, reason: "Hollow Bird harasses with local flying bursts only."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildHollowBeast(TreeBuilder b)
        {
            return b.Selector(
                "hollow_beast_root",
                b.Sequence("hollow_beast_howl", b.Condition("hollow_beast_engaged", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Engaged), b.CanCreatureSignal("howl_signal"), b.StartCreatureSignal("howl_signal")),
                b.Sequence("hollow_beast_leap_back", b.Condition("hollow_beast_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("leap_back"), b.StartCreatureMove("leap_back")),
                b.Sequence("hollow_beast_body_check", b.CanMelee("body_check"), b.StartMelee("body_check")),
                b.Sequence("hollow_beast_leap_bite", b.CanMelee("leap_bite"), b.StartMelee("leap_bite")),
                b.Action("hollow_beast_pressure", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.92f, reason: "Hollow Beast punishes overcommitment with committed leaps."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildSkeletonSword(TreeBuilder b)
        {
            return b.Selector(
                "skeleton_sword_root",
                b.Sequence("skeleton_sword_slash", b.CanMelee("rusty_slash"), b.StartMelee("rusty_slash")),
                b.Sequence("skeleton_sword_backhand", b.CanMelee("backhand_slash"), b.StartMelee("backhand_slash")),
                b.Action("skeleton_sword_pressure", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.95f, reason: "Sword skeleton pressures into committed slash range."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildSkeletonSpear(TreeBuilder b)
        {
            return b.Selector(
                "skeleton_spear_root",
                b.Sequence("skeleton_spear_thrust", b.CanMelee("spear_thrust"), b.StartMelee("spear_thrust")),
                b.Sequence("skeleton_spear_sweep", b.CanMelee("spear_sweep"), b.StartMelee("spear_sweep")),
                b.Action("skeleton_spear_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.9f, reason: "Spear skeleton holds a longer weapon range."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildKnight(TreeBuilder b)
        {
            return b.Selector(
                "knight_root",
                b.Weighted(
                    "knight_choice",
                    (b.Sequence("knight_guard", b.CanGuard("shield_guard"), b.StartGuard("shield_guard")), 1.15f),
                    (b.Sequence("knight_slash", b.CanMelee("knight_slash"), b.StartMelee("knight_slash")), 1.45f),
                    (b.Sequence("knight_thrust", b.CanMelee("knight_thrust"), b.StartMelee("knight_thrust")), 1.05f),
                    (b.Sequence("knight_bash", b.CanMelee("shield_bash"), b.StartMelee("shield_bash")), 0.75f)),
                b.Action("knight_guarded_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.8f, reason: "Knight advances carefully behind shield timing."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildGiant(TreeBuilder b)
        {
            return b.Selector(
                "giant_root",
                b.Sequence("giant_overhead_slam", b.CanArea("overhead_slam"), b.StartArea("overhead_slam")),
                b.Sequence("giant_stomp", b.CanArea("stomp"), b.StartArea("stomp")),
                b.Sequence("giant_sweep", b.CanMelee("club_sweep"), b.StartMelee("club_sweep")),
                b.Action("giant_slow_pressure", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.72f, reason: "Giant slowly enters range for a punishable heavy attack."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildHollowArcher(TreeBuilder b)
        {
            return b.Selector(
                "hollow_archer_root",
                b.Sequence("hollow_archer_backstep", b.Condition("hollow_archer_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("archer_backstep"), b.StartCreatureMove("archer_backstep")),
                b.Weighted(
                    "hollow_archer_shot_choice",
                    (b.Sequence("hollow_archer_volley", b.CanRanged("arrow_volley"), b.StartRanged("arrow_volley")), 0.9f),
                    (b.Sequence("hollow_archer_retreating", b.CanRanged("retreating_arrow"), b.StartRanged("retreating_arrow")), 1.0f),
                    (b.Sequence("hollow_archer_arrow", b.CanRanged("arrow_shot"), b.StartRanged("arrow_shot")), 1.8f)),
                b.Action("hollow_archer_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.82f, reason: "Hollow Archer keeps bow range and commits from idle."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildPowderGunner(TreeBuilder b)
        {
            return b.Selector(
                "powder_gunner_root",
                b.Sequence("powder_gunner_backstep", b.Condition("powder_gunner_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("gunner_backstep"), b.StartCreatureMove("gunner_backstep")),
                b.Weighted(
                    "powder_gunner_shot_choice",
                    (b.Sequence("powder_gunner_scatter", b.Condition("powder_gunner_close_scatter", EnemyBehaviorConditionKind.DistanceLessOrEqual, threshold: 5.4f), b.CanRanged("scatter_shot"), b.StartRanged("scatter_shot")), 1.2f),
                    (b.Sequence("powder_gunner_musket", b.CanRanged("aimed_musket_shot"), b.StartRanged("aimed_musket_shot")), 2.0f)),
                b.Action("powder_gunner_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.68f, reason: "Powder Gunner braces for slow, punishable firearm shots."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildKnifeThrower(TreeBuilder b)
        {
            return b.Selector(
                "knife_thrower_root",
                b.Sequence("knife_thrower_backstep", b.Condition("knife_thrower_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("thrower_backstep"), b.StartCreatureMove("thrower_backstep")),
                b.Weighted(
                    "knife_thrower_choice",
                    (b.Sequence("knife_thrower_fan", b.CanRanged("knife_fan"), b.StartRanged("knife_fan")), 1.1f),
                    (b.Sequence("knife_thrower_quick", b.CanRanged("throwing_knife"), b.StartRanged("throwing_knife")), 1.9f)),
                b.Action("knife_thrower_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 1f, reason: "Knife Thrower darts around mid-range before committing."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildRepeaterTurret(TreeBuilder b)
        {
            return b.Selector(
                "repeater_turret_root",
                b.Sequence("repeater_turret_engage", b.Condition("repeater_turret_should_fire", EnemyBehaviorConditionKind.ShouldSentinelEngage), b.Weighted(
                    "repeater_turret_pattern_choice",
                    (b.Sequence("repeater_turret_arc", b.CanRanged("suppressing_arc"), b.StartRanged("suppressing_arc")), 1.0f),
                    (b.Sequence("repeater_turret_burst", b.CanRanged("repeater_burst"), b.StartRanged("repeater_burst")), 1.5f),
                    (b.Sequence("repeater_turret_lock", b.CanRanged("lock_on_dart"), b.StartRanged("lock_on_dart")), 1.0f))),
                b.Action("repeater_turret_hold", EnemyBehaviorCommandKind.Hold, reason: "Repeater Turret remains planted and budgeted."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildClockworkSentry(TreeBuilder b)
        {
            return b.Selector(
                "clockwork_sentry_root",
                b.Weighted(
                    "clockwork_sentry_pattern_choice",
                    (b.Sequence("clockwork_sentry_radial", b.CanRanged("clockwork_radial"), b.StartRanged("clockwork_radial")), 1.3f),
                    (b.Sequence("clockwork_sentry_fan", b.CanRanged("rotating_fan"), b.StartRanged("rotating_fan")), 1.1f),
                    (b.Sequence("clockwork_sentry_shot", b.CanRanged("gear_shot"), b.StartRanged("gear_shot")), 1.4f)),
                b.Action("clockwork_sentry_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.58f, reason: "Clockwork Sentry slowly repositions into projectile pattern range."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildHollowAcolyte(TreeBuilder b)
        {
            return b.Selector(
                "hollow_acolyte_root",
                b.Sequence("hollow_acolyte_veil_close", b.Condition("hollow_acolyte_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("veil_step"), b.StartCreatureMove("veil_step")),
                b.Weighted(
                    "hollow_acolyte_spell_choice",
                    (b.Sequence("hollow_acolyte_burst", b.Condition("hollow_acolyte_close_burst", EnemyBehaviorConditionKind.DistanceLessOrEqual, threshold: 5.8f), b.CanRanged("rune_burst"), b.StartRanged("rune_burst")), 0.95f),
                    (b.Sequence("hollow_acolyte_orb", b.CanRanged("slow_soul_orb"), b.StartRanged("slow_soul_orb")), 1.8f)),
                b.Action("hollow_acolyte_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.72f, reason: "Hollow Acolyte keeps caster range and commits from idle."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildWraith(TreeBuilder b)
        {
            return b.Selector(
                "wraith_root",
                b.Sequence("wraith_phase_close", b.Condition("wraith_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("phase_shift"), b.StartCreatureMove("phase_shift")),
                b.Weighted(
                    "wraith_choice",
                    (b.Sequence("wraith_touch", b.Condition("wraith_touch_range", EnemyBehaviorConditionKind.DistanceLessOrEqual, threshold: 1.3f), b.CanMelee("curse_touch"), b.StartMelee("curse_touch")), 1.35f),
                    (b.Sequence("wraith_bolt", b.CanRanged("wraith_bolt"), b.StartRanged("wraith_bolt")), 1.65f),
                    (b.Sequence("wraith_phase", b.CanCreatureMove("phase_shift"), b.StartCreatureMove("phase_shift")), 0.85f)),
                b.Action("wraith_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.95f, reason: "Wraith circles through local phase bursts and explicit attacks."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildSoulEater(TreeBuilder b)
        {
            return b.Selector(
                "soul_eater_root",
                b.Sequence("soul_eater_phase_close", b.Condition("soul_eater_too_close", EnemyBehaviorConditionKind.IsTooClose), b.CanCreatureMove("eater_phase_step"), b.StartCreatureMove("eater_phase_step")),
                b.Weighted(
                    "soul_eater_choice",
                    (b.Sequence("soul_eater_drain", b.CanRanged("soul_drain"), b.StartRanged("soul_drain")), 1.8f),
                    (b.Sequence("soul_eater_burst", b.Condition("soul_eater_burst_close", EnemyBehaviorConditionKind.DistanceLessOrEqual, threshold: 5.2f), b.CanRanged("soul_burst"), b.StartRanged("soul_burst")), 1.15f),
                    (b.Sequence("soul_eater_phase", b.CanCreatureMove("eater_phase_step"), b.StartCreatureMove("eater_phase_step")), 0.65f)),
                b.Action("soul_eater_pressure", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.82f, reason: "Soul Eater hunts for drain lanes without passive contact damage."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildCurseBinder(TreeBuilder b)
        {
            return b.Selector(
                "curse_binder_root",
                b.Sequence("curse_binder_field", b.Condition("curse_binder_close_field", EnemyBehaviorConditionKind.DistanceLessOrEqual, threshold: 2.55f), b.CanArea("curse_field"), b.StartArea("curse_field")),
                b.Weighted(
                    "curse_binder_choice",
                    (b.Sequence("curse_binder_fan", b.CanRanged("sigil_fan"), b.StartRanged("sigil_fan")), 1.15f),
                    (b.Sequence("curse_binder_bolt", b.CanRanged("binding_bolt"), b.StartRanged("binding_bolt")), 1.65f)),
                b.Action("curse_binder_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.68f, reason: "Curse Binder claims territory with dodgeable sigils and projectiles."));
        }

        private static EnemyBehaviorTreeNodeDefinition BuildGraveLantern(TreeBuilder b)
        {
            return b.Selector(
                "grave_lantern_root",
                b.Sequence("grave_lantern_engage", b.Condition("grave_lantern_should_fire", EnemyBehaviorConditionKind.ShouldSentinelEngage), b.Weighted(
                    "grave_lantern_pattern_choice",
                    (b.Sequence("grave_lantern_ring", b.CanRanged("lantern_soul_ring"), b.StartRanged("lantern_soul_ring")), 1.35f),
                    (b.Sequence("grave_lantern_fan", b.CanRanged("lantern_curse_fan"), b.StartRanged("lantern_curse_fan")), 1.1f),
                    (b.Sequence("grave_lantern_orb", b.CanRanged("grave_orb"), b.StartRanged("grave_orb")), 1.45f))),
                b.Action("grave_lantern_hold", EnemyBehaviorCommandKind.Hold, reason: "Grave Lantern is stationary magical area pressure."));
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
                "spawnEnemyHollowBird" => "Hollow Bird",
                "spawnEnemyHollowBeast" => "Hollow Beast",
                "spawnEnemySkeletonSword" => "Skeleton Sword",
                "spawnEnemySkeletonSpear" => "Skeleton Spear",
                "spawnEnemyKnight" => "Knight",
                "spawnEnemyGiant" => "Giant",
                "spawnEnemyHollowArcher" => "Hollow Archer",
                "spawnEnemyPowderGunner" => "Powder Gunner",
                "spawnEnemyKnifeThrower" => "Knife Thrower",
                "spawnEnemyRepeaterTurret" => "Repeater Turret",
                "spawnEnemyClockworkSentry" => "Clockwork Sentry",
                "spawnEnemyHollowAcolyte" => "Hollow Acolyte",
                "spawnEnemyWraith" => "Wraith",
                "spawnEnemySoulEater" => "Soul Eater",
                "spawnEnemyCurseBinder" => "Curse Binder",
                "spawnEnemyGraveLantern" => "Grave Lantern",
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

            public EnemyBehaviorConditionNodeDefinition CanGuard(string actionId)
            {
                return Condition($"can_guard_{actionId}", EnemyBehaviorConditionKind.CanStartGuardAction, actionId: actionId);
            }

            public EnemyBehaviorConditionNodeDefinition CanCreatureMove(string actionId)
            {
                return Condition($"can_creature_move_{actionId}", EnemyBehaviorConditionKind.CanStartCreatureMoveAction, actionId: actionId);
            }

            public EnemyBehaviorConditionNodeDefinition CanCreatureSignal(string actionId)
            {
                return Condition($"can_creature_signal_{actionId}", EnemyBehaviorConditionKind.CanStartCreatureSignalAction, actionId: actionId);
            }

            public EnemyBehaviorConditionNodeDefinition CanRanged(string actionId)
            {
                return Condition($"can_ranged_{actionId}", EnemyBehaviorConditionKind.CanStartRangedAction, actionId: actionId);
            }

            public EnemyBehaviorActionNodeDefinition StartMelee(string actionId)
            {
                return Action($"start_melee_{actionId}", EnemyBehaviorCommandKind.StartMeleeAction, actionId, reason: $"Start melee action {actionId}.");
            }

            public EnemyBehaviorActionNodeDefinition StartArea(string actionId)
            {
                return Action($"start_area_{actionId}", EnemyBehaviorCommandKind.StartAreaAction, actionId, reason: $"Start area action {actionId}.");
            }

            public EnemyBehaviorActionNodeDefinition StartGuard(string actionId)
            {
                return Action($"start_guard_{actionId}", EnemyBehaviorCommandKind.StartGuardAction, actionId, reason: $"Start guard action {actionId}.");
            }

            public EnemyBehaviorActionNodeDefinition StartCreatureMove(string actionId)
            {
                return Action($"start_creature_move_{actionId}", EnemyBehaviorCommandKind.StartCreatureMoveAction, actionId, reason: $"Start creature move action {actionId}.");
            }

            public EnemyBehaviorActionNodeDefinition StartCreatureSignal(string actionId)
            {
                return Action($"start_creature_signal_{actionId}", EnemyBehaviorCommandKind.StartCreatureSignalAction, actionId, reason: $"Start creature signal action {actionId}.");
            }

            public EnemyBehaviorActionNodeDefinition StartRanged(string actionId)
            {
                return Action($"start_ranged_{actionId}", EnemyBehaviorCommandKind.StartRangedAction, actionId, reason: $"Start ranged action {actionId}.");
            }
        }
    }
}
