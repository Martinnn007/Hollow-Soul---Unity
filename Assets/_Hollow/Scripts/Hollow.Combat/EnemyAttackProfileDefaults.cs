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
            Enemy("spawnEnemyFast", "needle_rush", "Needle Rush", EnemyAttackRuntimeKind.Contact, 1, 0.85f, 0f, 0.12f, 0.65f, 0, 0f, DamageChannel.Physical, DamageDelivery.Contact, DamageElement.None, ImpactForceClass.Medium, DamageThreatKind.Heavy, 0.45f, "Contact pressure while weaving through the player."),
            Enemy("spawnEnemyFast", "snap_followup", "Snap Followup", EnemyAttackRuntimeKind.MeleeLunge, 1, 1.35f, 0.16f, 0.12f, 1.05f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.25f, "Short fallback bite profile."),
            Enemy("spawnEnemyHeavy", "body_slam", "Body Slam", EnemyAttackRuntimeKind.Contact, 2, 1.1f, 0f, 0.14f, 0.8f, 0, 0f, DamageChannel.Physical, DamageDelivery.Contact, DamageElement.None, ImpactForceClass.Heavy, DamageThreatKind.Heavy, 0.75f, "High-stability contact threat."),
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
            Enemy("spawnEnemyRat", "rat_bite", "Rat Bite", EnemyAttackRuntimeKind.MeleeLunge, 1, 0.9f, 0.14f, 0.14f, 0.95f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.22f, "Territorial close bite after warning or disturbance."),
            Enemy("spawnEnemySpider", "startle_hop", "Startle Hop", EnemyAttackRuntimeKind.MeleeLunge, 1, 0.85f, 0.12f, 0.16f, 1.15f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.3f, "Chaotic hop-forward attack chosen after startle."),
            Enemy("spawnEnemySpider", "close_bite", "Close Bite", EnemyAttackRuntimeKind.MeleeLunge, 1, 0.75f, 0.1f, 0.12f, 0.75f, 0, 0f, DamageChannel.Physical, DamageDelivery.Melee, DamageElement.None, ImpactForceClass.Light, DamageThreatKind.Light, 0.22f, "Very close panic bite."),
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
            string notes)
        {
            return new EnemyAttackProfileSpec(ownerId, false, attackId, displayName, runtimeKind, damage, cooldownSeconds, windupSeconds, activeSeconds, rangeMeters, projectileCount, projectileSpeedMetersPerSecond, channel, delivery, element, forceClass, threatKind, knockbackMeters, DefaultGuardRecoil, notes);
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
            string notes)
        {
            return new EnemyAttackProfileSpec(ownerId, true, attackId, displayName, runtimeKind, damage, cooldownSeconds, windupSeconds, activeSeconds, rangeMeters, projectileCount, projectileSpeedMetersPerSecond, channel, delivery, element, forceClass, threatKind, knockbackMeters, DefaultGuardRecoil, notes);
        }
    }
}
