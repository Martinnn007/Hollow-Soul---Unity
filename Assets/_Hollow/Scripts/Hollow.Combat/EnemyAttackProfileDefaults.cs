using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;

namespace Hollow.Combat
{
    public static class EnemyAttackProfileDefaults
    {
        private const float DefaultGuardRecoil = 0.35f;

        public static IReadOnlyList<EnemyAttackProfileSpec> AllEnemySpecs { get; } = new[]
        {
            Enemy("spawnEnemyNormal", "claw_lunge", "Claw Lunge", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.15f, 0.22f, 0.18f, 1.4f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "Primary M75 lunge profile."),
            Enemy("spawnEnemyNormal", "desperate_bite", "Desperate Bite", EnemyAttackRuntimeKind.Contact, 1, 1f, 0f, 0.1f, 0.7f, 0, 0f, DamageChannel.Physical, DamageDelivery.Contact, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.3f, "Fallback body/contact bite."),
            Enemy("spawnEnemyFlying", "panic_peck", "Panic Peck", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.15f, 0.18f, 0.16f, 1.35f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.25f, "Prey panic lunge when endangered."),
            Enemy("spawnEnemyFlying", "dive_scratch", "Dive Scratch", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.25f, 0.22f, 0.18f, 1.35f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.45f, "Sharper engaged dive; no new pathfinding."),
            Enemy("spawnEnemyFlying", "wing_buffet", "Wing Buffet", EnemyAttackRuntimeKind.Area, 1, 1.8f, 0.28f, 0.18f, 1.05f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.5f, "Catalogue pressure profile for close-range shove."),
            Enemy("spawnEnemyFast", "quick_pounce", "Quick Pounce", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.05f, 0.18f, 0.16f, 1.25f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.3f, "Fast primary lunge."),
            Enemy("spawnEnemyFast", "side_pounce", "Side Pounce", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.2f, 0.2f, 0.17f, 1.45f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.38f, "M82 committed lateral pounce prototype.", recoverySeconds: 0.2f, hitArcDegrees: 120f, poiseBreakThreshold: ImpactForceClass.Medium),
            Enemy("spawnEnemyFast", "needle_rush", "Needle Rush", EnemyAttackRuntimeKind.Contact, 1, 0.85f, 0f, 0.12f, 0.65f, 0, 0f, DamageChannel.Physical, DamageDelivery.Contact, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.45f, "Contact pressure while weaving through the player."),
            Enemy("spawnEnemyFast", "snap_followup", "Snap Followup", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.35f, 0.16f, 0.12f, 1.05f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.25f, "Short fallback bite profile."),
            Enemy("spawnEnemyHeavy", "body_slam", "Body Slam", EnemyAttackRuntimeKind.Contact, 2, 1.1f, 0f, 0.14f, 0.8f, 0, 0f, DamageChannel.Physical, DamageDelivery.Contact, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Heavy, 0.75f, "High-stability contact threat."),
            Enemy("spawnEnemyHeavy", "stomp", "Stomp", EnemyAttackRuntimeKind.Area, 2, 2.2f, 0.42f, 0.18f, 1.25f, 0, 0f, DamageChannel.Physical, DamageDelivery.Area, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Heavy, 0.85f, "M82 heavy circular stomp prototype with high guard pressure.", recoverySeconds: 0.38f, hitArcDegrees: 360f, poiseBreakThreshold: ImpactForceClass.Heavy),
            Enemy("spawnEnemyHeavy", "maul_lunge", "Maul Lunge", EnemyAttackRuntimeKind.MeleeLunge, 2, 1.35f, 0.28f, 0.2f, 1.7f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Heavy, 0.7f, "Slow readable heavy lunge."),
            Enemy("spawnEnemyHeavy", "heavy_shove", "Heavy Shove", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.45f, 0.24f, 0.16f, 1.25f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.6f, "Lower-damage control hit."),
            Enemy("spawnEnemyCharger", "ash_charge", "Ash Charge", EnemyAttackRuntimeKind.Charge, 1, 2f, 0.42f, 0.38f, 5.5f, 0, 0f, DamageChannel.Elemental, DamageDelivery.Contact, DamageElement.Fire, ImpactForceClass.Heavy, DamageThreatKind.Heavy, 0.8f, "Existing charge with explicit fire identity."),
            Enemy("spawnEnemyCharger", "ember_clash", "Ember Clash", EnemyAttackRuntimeKind.Contact, 1, 1f, 0f, 0.12f, 0.7f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.5f, "Fallback body clash outside active charge."),
            Enemy("spawnEnemyTurret", "bone_dart", "Bone Dart", EnemyAttackRuntimeKind.Projectile, 1, 1.35f, 0.34f, 0.1f, 8f, 1, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "Standard sentinel shot."),
            Enemy("spawnEnemyTurret", "braced_spike", "Braced Spike", EnemyAttackRuntimeKind.Projectile, 1, 1.8f, 0.42f, 0.1f, 8f, 1, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.48f, "Slower stronger ranged shot."),
            Enemy("spawnEnemyTurret", "rattle_volley", "Rattle Volley", EnemyAttackRuntimeKind.Projectile, 1, 1.6f, 0.34f, 0.1f, 8f, 3, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.3f, "Catalogue volley profile; current runtime fires one projectile in V1."),
            Enemy("spawnEnemySplitter", "husk_cleave", "Husk Cleave", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.25f, 0.22f, 0.18f, 1.45f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.5f, "Basic predator cleave."),
            Enemy("spawnEnemySplitter", "splinter_lunge", "Splinter Lunge", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.15f, 0.22f, 0.18f, 1.6f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.45f, "Primary M75 splitter lunge."),
            Enemy("spawnEnemySplitter", "death_split", "Death Split", EnemyAttackRuntimeKind.Split, 0, 0.1f, 0f, 0.1f, 1.2f, 0, 0f, DamageChannel.Physical, DamageDelivery.Area, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.35f, "Metadata/runtime event profile for split children."),
            Enemy("spawnEnemySpittingPod", "spit_lob", "Spit Lob", EnemyAttackRuntimeKind.Projectile, 1, 1f, 0.24f, 0.85f, 8f, 1, 5f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "Visible ballistic lob that lands as a small splash where the player was targeted."),
            Enemy("spawnEnemyRat", "warning_squeal", "Warning Squeal", EnemyAttackRuntimeKind.Movement, 0, 0.9f, 0.25f, 0.1f, 2.5f, 0, 0f, DamageChannel.Physical, DamageDelivery.Area, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0f, "M82 non-damaging territorial warning before bite commitment.", recoverySeconds: 0.1f, hitArcDegrees: 220f, poiseBreakThreshold: ImpactForceClass.Light),
            Enemy("spawnEnemyRat", "rat_bite", "Rat Bite", EnemyAttackRuntimeKind.MeleeLunge, 1, 0.9f, 0.14f, 0.14f, 0.95f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.22f, "Territorial close bite after warning or disturbance."),
            Enemy("spawnEnemySpider", "startle_hop", "Startle Hop", EnemyAttackRuntimeKind.MeleeLunge, 1, 0.85f, 0.12f, 0.16f, 1.15f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.3f, "Chaotic hop-forward attack chosen after startle."),
            Enemy("spawnEnemySpider", "close_bite", "Close Bite", EnemyAttackRuntimeKind.MeleeLunge, 1, 0.75f, 0.1f, 0.12f, 0.75f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.22f, "Very close panic bite."),
            Enemy("spawnEnemySpider", "side_hop_bite", "Side Hop Bite", EnemyAttackRuntimeKind.MeleeLunge, 1, 0.9f, 0.12f, 0.14f, 1.1f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.26f, "M82 quick side-hop bite prototype.", recoverySeconds: 0.14f, hitArcDegrees: 125f, poiseBreakThreshold: ImpactForceClass.Light),
            Enemy("spawnEnemySkeletonSword", "rusty_slash", "Rusty Slash", EnemyAttackRuntimeKind.WeaponMelee, 1, 1.35f, 0.28f, 0.14f, 1.45f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "M84 sword skeleton opener; can chain once into backhand slash.", recoverySeconds: 0.24f, hitArcDegrees: 120f, poiseBreakThreshold: ImpactForceClass.Light, activeMovementDistanceMeters: 0f, comboFollowUpAttackId: "backhand_slash"),
            Enemy("spawnEnemySkeletonSword", "backhand_slash", "Backhand Slash", EnemyAttackRuntimeKind.WeaponMelee, 1, 1.15f, 0.18f, 0.14f, 1.35f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.3f, "M84 sword skeleton one-follow-up finisher.", recoverySeconds: 0.34f, hitArcDegrees: 140f, poiseBreakThreshold: ImpactForceClass.Light, activeMovementDistanceMeters: 0f),
            Enemy("spawnEnemySkeletonSpear", "spear_thrust", "Spear Thrust", EnemyAttackRuntimeKind.WeaponMelee, 1, 1.55f, 0.34f, 0.12f, 2.4f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.45f, "M84 spear skeleton narrow reach poke with a small forward step.", recoverySeconds: 0.34f, hitArcDegrees: 55f, poiseBreakThreshold: ImpactForceClass.Medium, activeMovementDistanceMeters: 0.28f),
            Enemy("spawnEnemySkeletonSpear", "spear_sweep", "Spear Sweep", EnemyAttackRuntimeKind.WeaponMelee, 1, 1.75f, 0.3f, 0.16f, 1.65f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "M84 spear skeleton wide close-range sweep.", recoverySeconds: 0.38f, hitArcDegrees: 160f, poiseBreakThreshold: ImpactForceClass.Light, activeMovementDistanceMeters: 0f),
            Enemy("spawnEnemyKnight", "shield_guard", "Shield Guard", EnemyAttackRuntimeKind.Defense, 0, 1.25f, 0.12f, 0.65f, 2.25f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Light, 0f, "M84 medium-shield frontal guard window.", recoverySeconds: 0.28f, hitArcDegrees: 150f, poiseBreakThreshold: ImpactForceClass.Heavy, activeMovementDistanceMeters: 0f),
            Enemy("spawnEnemyKnight", "knight_slash", "Knight Slash", EnemyAttackRuntimeKind.WeaponMelee, 1, 1.6f, 0.36f, 0.16f, 1.65f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.5f, "M84 knight committed slash; can chain once into shield bash.", recoverySeconds: 0.36f, hitArcDegrees: 120f, poiseBreakThreshold: ImpactForceClass.Medium, activeMovementDistanceMeters: 0.08f, comboFollowUpAttackId: "shield_bash"),
            Enemy("spawnEnemyKnight", "knight_thrust", "Knight Thrust", EnemyAttackRuntimeKind.WeaponMelee, 1, 1.55f, 0.34f, 0.13f, 2.15f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.45f, "M84 knight narrow forward thrust.", recoverySeconds: 0.38f, hitArcDegrees: 65f, poiseBreakThreshold: ImpactForceClass.Medium, activeMovementDistanceMeters: 0.22f),
            Enemy("spawnEnemyKnight", "shield_bash", "Shield Bash", EnemyAttackRuntimeKind.WeaponMelee, 1, 1.85f, 0.28f, 0.14f, 1.15f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.65f, "M84 knight short control hit and combo finisher.", recoverySeconds: 0.5f, hitArcDegrees: 90f, poiseBreakThreshold: ImpactForceClass.Heavy, activeMovementDistanceMeters: 0.2f),
            Enemy("spawnEnemyGiant", "club_sweep", "Club Sweep", EnemyAttackRuntimeKind.WeaponMelee, 2, 2.65f, 0.65f, 0.22f, 2.25f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Heavy, 0.9f, "M84 giant slow elite sweeping punish.", recoverySeconds: 0.75f, hitArcDegrees: 190f, poiseBreakThreshold: ImpactForceClass.Heavy, activeMovementDistanceMeters: 0.08f),
            Enemy("spawnEnemyGiant", "overhead_slam", "Overhead Slam", EnemyAttackRuntimeKind.Area, 2, 3.15f, 0.78f, 0.2f, 1.55f, 0, 0f, DamageChannel.Physical, DamageDelivery.Area, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Heavy, 1.1f, "M84 giant circular impact slam.", recoverySeconds: 0.9f, hitArcDegrees: 360f, poiseBreakThreshold: ImpactForceClass.Heavy),
            Enemy("spawnEnemyGiant", "stomp", "Giant Stomp", EnemyAttackRuntimeKind.Area, 1, 2.35f, 0.5f, 0.18f, 1.25f, 0, 0f, DamageChannel.Physical, DamageDelivery.Area, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Heavy, 0.8f, "M84 giant shorter area punish.", recoverySeconds: 0.6f, hitArcDegrees: 360f, poiseBreakThreshold: ImpactForceClass.Medium),
            Enemy("spawnEnemyBoss", "stone_charge", "Stone Warden Contact", EnemyAttackRuntimeKind.Contact, 2, 3.2f, 0.5f, 0.22f, 2.2f, 0, 0f, DamageChannel.Physical, DamageDelivery.Contact, DamageElement.None, ImpactForceClass.Massive, DamageThreatKind.Boss, 0.9f, "Legacy generic boss spawn fallback contact profile."),
            Enemy("spawnEnemyBoss", "stone_four_way_burst", "Stone Warden Burst Shards", EnemyAttackRuntimeKind.RadialProjectile, 1, 3.8f, 0.45f, 0.1f, 6f, 4, 4.5f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.StrongProjectile, 0.55f, "Legacy generic boss spawn fallback low-health burst.")
        };

        public static IReadOnlyList<EnemyAttackProfileSpec> AllBossSpecs { get; } = new[]
        {
            Boss("stone_warden", "stone_charge", "Stone Charge", EnemyAttackRuntimeKind.Charge, 1, 3.2f, 0.5f, 0.22f, 2.2f, 0, 0f, DamageChannel.Physical, DamageDelivery.Contact, DamageElement.None, ImpactForceClass.Massive, DamageThreatKind.Boss, 0.9f, "Stone Warden dash contact."),
            Boss("stone_warden", "stone_stomp_burst", "Stone Stomp Burst", EnemyAttackRuntimeKind.RadialProjectile, 2, 4.6f, 0.55f, 0.1f, 6f, 8, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Boss, 0.65f, "Radial stomp debris."),
            Boss("stone_warden", "stone_four_way_burst", "Stone Four-Way Burst", EnemyAttackRuntimeKind.RadialProjectile, 1, 3.8f, 0.45f, 0.1f, 6f, 4, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.StrongProjectile, 0.55f, "Low-health cardinal burst."),
            Boss("splinter_saint", "splinter_side_hop_radial", "Side-Hop Radial", EnemyAttackRuntimeKind.RadialProjectile, 1, 1.75f, 0.2f, 0.1f, 5f, 6, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "Side hop shot ring."),
            Boss("gravel_maw", "gravel_burrow_summon", "Burrow Summon", EnemyAttackRuntimeKind.Summon, 0, 7f, 0.5f, 0.1f, 1.2f, 3, 0f, DamageChannel.Physical, DamageDelivery.Area, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.35f, "Summons a small gravel pack."),
            Boss("gravel_maw", "gravel_rubble_spray", "Rubble Spray", EnemyAttackRuntimeKind.RadialProjectile, 1, 7f, 0.3f, 0.1f, 5f, 5, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "Rubble spray after summon."),
            Boss("cartouche_widow", "cartouche_falling_marks", "Falling Marks", EnemyAttackRuntimeKind.FanProjectile, 1, 1.65f, 0.25f, 0.1f, 6f, 5, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "Fan pressure pattern."),
            Boss("iron_reliquary", "iron_peek_shot", "Peek Shot", EnemyAttackRuntimeKind.FanProjectile, 1, 1.9f, 0.25f, 0.1f, 6f, 3, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "Three-shot cover peek."),
            Boss("iron_reliquary", "iron_relocate_bash", "Relocate Bash", EnemyAttackRuntimeKind.Movement, 1, 4.2f, 0.1f, 0.22f, 1.1f, 0, 0f, DamageChannel.Physical, DamageDelivery.Contact, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Heavy, 0.55f, "Retreat bash profile for relocate movement."),
            Boss("mirror_husk", "mirror_chase_contact", "Mirror Chase Contact", EnemyAttackRuntimeKind.Contact, 1, 1f, 0f, 0.1f, 0.8f, 0, 0f, DamageChannel.Physical, DamageDelivery.Contact, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Boss, 0.55f, "Chase contact body profile."),
            Boss("mirror_husk", "mirror_split", "Mirror Split", EnemyAttackRuntimeKind.Split, 0, 0.1f, 0f, 0.1f, 1.2f, 4, 0f, DamageChannel.Physical, DamageDelivery.Area, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.35f, "Split event profile."),
            Boss("ash_comet", "ash_comet_dash", "Comet Dash", EnemyAttackRuntimeKind.Charge, 2, 2.6f, 0.25f, 0.22f, 2.2f, 0, 0f, DamageChannel.Elemental, DamageDelivery.Contact, DamageElement.Fire, ImpactForceClass.Massive, DamageThreatKind.Boss, 0.95f, "Fire-aspected dash contact."),
            Boss("ash_comet", "ash_fire_radial", "Ash Fire Radial", EnemyAttackRuntimeKind.RadialProjectile, 2, 2.6f, 0.35f, 0.1f, 6f, 8, 4.8f, DamageChannel.Elemental, DamageDelivery.Projectile, DamageElement.Fire, ImpactForceClass.Heavy, DamageThreatKind.Boss, 0.65f, "Fire radial after dash."),
            Boss("choir_of_teeth", "choir_rotating_hymn", "Rotating Hymn", EnemyAttackRuntimeKind.RadialProjectile, 1, 2.2f, 0.3f, 0.1f, 6f, 12, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.32f, "Rotating tooth ring."),
            Boss("choir_of_teeth", "choir_tooth_storm", "Tooth Storm", EnemyAttackRuntimeKind.RadialProjectile, 2, 4.2f, 0.45f, 0.1f, 6f, 16, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.StrongProjectile, 0.6f, "Low-health dense tooth storm."),
            Boss("rust_bishop", "rust_beam", "Rust Beam", EnemyAttackRuntimeKind.FanProjectile, 2, 2.8f, 0.4f, 0.1f, 6f, 3, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.StrongProjectile, 0.65f, "Narrow heavy fan used as beam V1."),
            Boss("rust_bishop", "rust_mine_pattern", "Mine Pattern", EnemyAttackRuntimeKind.RadialProjectile, 1, 3.6f, 0.3f, 0.1f, 5f, 6, 4.8f, DamageChannel.Physical, DamageDelivery.Projectile, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.35f, "Radial mine-like pressure pattern."),
            Boss("hollow_star_larva", "larva_abyss_call", "Abyss Call", EnemyAttackRuntimeKind.Summon, 0, 0.1f, 0f, 0.1f, 1.2f, 3, 0f, DamageChannel.Elemental, DamageDelivery.Area, DamageElement.Cosmic, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.4f, "Cosmic summon event."),
            Boss("hollow_star_larva", "larva_starfall", "Starfall", EnemyAttackRuntimeKind.FanProjectile, 1, 2.1f, 0.3f, 0.1f, 6f, 7, 4.8f, DamageChannel.Elemental, DamageDelivery.Projectile, DamageElement.Cosmic, ImpactForceClass.Light, DamageThreatKind.Light, 0.38f, "Cosmic fan pattern."),
            Boss("hollow_star_larva", "larva_desperation", "Desperation", EnemyAttackRuntimeKind.RadialProjectile, 2, 3.1f, 0.5f, 0.1f, 6f, 18, 4.8f, DamageChannel.Elemental, DamageDelivery.Projectile, DamageElement.Cosmic, ImpactForceClass.Heavy, DamageThreatKind.StrongProjectile, 0.65f, "Cosmic desperation storm.")
        };

        public static IReadOnlyList<EnemyAttackProfileDefinition> CreateEnemyProfiles(string spawnKind)
        {
            return AllEnemySpecs
                .Where(spec => string.Equals(spec.OwnerId, spawnKind, StringComparison.Ordinal))
                .Select(EnemyAttackProfileDefinition.CreateRuntime)
                .ToArray();
        }

        public static IReadOnlyList<EnemyAttackProfileDefinition> CreateBossProfiles(string bossId)
        {
            return AllBossSpecs
                .Where(spec => string.Equals(spec.OwnerId, bossId, StringComparison.Ordinal))
                .Select(EnemyAttackProfileDefinition.CreateRuntime)
                .ToArray();
        }

        public static EnemyAttackProfileDefinition ResolveEnemyProfile(string spawnKind, string attackId)
        {
            var spec = AllEnemySpecs.FirstOrDefault(candidate =>
                string.Equals(candidate.OwnerId, spawnKind, StringComparison.Ordinal) &&
                string.Equals(candidate.AttackId, attackId, StringComparison.Ordinal));
            return string.IsNullOrWhiteSpace(spec.AttackId) ? null : EnemyAttackProfileDefinition.CreateRuntime(spec);
        }

        public static EnemyAttackProfileDefinition ResolveBossProfile(string bossId, string attackId)
        {
            var spec = AllBossSpecs.FirstOrDefault(candidate =>
                string.Equals(candidate.OwnerId, bossId, StringComparison.Ordinal) &&
                string.Equals(candidate.AttackId, attackId, StringComparison.Ordinal));
            return string.IsNullOrWhiteSpace(spec.AttackId) ? null : EnemyAttackProfileDefinition.CreateRuntime(spec);
        }

        private static EnemyAttackProfileSpec Enemy(
            string ownerId,
            string attackId,
            string displayName,
            EnemyAttackRuntimeKind runtimeKind,
            int damage,
            float cooldownSeconds,
            float windupSeconds,
            float activeSeconds,
            float rangeMeters,
            int projectileCount,
            float projectileSpeedMetersPerSecond,
            DamageChannel channel,
            DamageDelivery delivery,
            DamageElement element,
            ImpactForceClass forceClass,
            DamageThreatKind threatKind,
            float knockbackMeters,
            string notes,
            float recoverySeconds = -1f,
            float hitArcDegrees = -1f,
            ImpactForceClass poiseBreakThreshold = ImpactForceClass.Medium,
            float activeMovementDistanceMeters = -1f,
            string comboFollowUpAttackId = "")
        {
            return new EnemyAttackProfileSpec(ownerId, false, attackId, displayName, runtimeKind, damage, cooldownSeconds, windupSeconds, activeSeconds, rangeMeters, projectileCount, projectileSpeedMetersPerSecond, channel, delivery, element, forceClass, threatKind, knockbackMeters, DefaultGuardRecoil, notes, recoverySeconds, hitArcDegrees, poiseBreakThreshold, activeMovementDistanceMeters, comboFollowUpAttackId);
        }

        private static EnemyAttackProfileSpec Boss(
            string ownerId,
            string attackId,
            string displayName,
            EnemyAttackRuntimeKind runtimeKind,
            int damage,
            float cooldownSeconds,
            float windupSeconds,
            float activeSeconds,
            float rangeMeters,
            int projectileCount,
            float projectileSpeedMetersPerSecond,
            DamageChannel channel,
            DamageDelivery delivery,
            DamageElement element,
            ImpactForceClass forceClass,
            DamageThreatKind threatKind,
            float knockbackMeters,
            string notes,
            float recoverySeconds = -1f,
            float hitArcDegrees = -1f,
            ImpactForceClass poiseBreakThreshold = ImpactForceClass.Medium,
            float activeMovementDistanceMeters = -1f,
            string comboFollowUpAttackId = "")
        {
            return new EnemyAttackProfileSpec(ownerId, true, attackId, displayName, runtimeKind, damage, cooldownSeconds, windupSeconds, activeSeconds, rangeMeters, projectileCount, projectileSpeedMetersPerSecond, channel, delivery, element, forceClass, threatKind, knockbackMeters, DefaultGuardRecoil, notes, recoverySeconds, hitArcDegrees, poiseBreakThreshold, activeMovementDistanceMeters, comboFollowUpAttackId);
        }
    }
}
