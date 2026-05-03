using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;

namespace Hollow.Combat
{
    public static class EnemyActionProfileDefaults
    {
        private static readonly EnemyInstinctDisposition[] AllDispositions =
        {
            EnemyInstinctDisposition.Predator,
            EnemyInstinctDisposition.Prey,
            EnemyInstinctDisposition.Sentinel,
            EnemyInstinctDisposition.Mindless,
            EnemyInstinctDisposition.Territorial
        };

        public static IReadOnlyList<EnemyActionProfileSpec> AllEnemySpecs { get; } = BuildEnemySpecs().ToArray();

        public static IReadOnlyList<EnemyActionProfileSpec> AllBossSpecs { get; } = BuildBossSpecs().ToArray();

        public static IReadOnlyList<EnemyActionProfileSpec> LibraryTemplateSpecs { get; } = BuildLibraryTemplateSpecs().ToArray();

        public static IReadOnlyList<EnemyActionProfileSpec> AllSpecs { get; } = AllEnemySpecs
            .Concat(AllBossSpecs)
            .Concat(LibraryTemplateSpecs)
            .ToArray();

        public static IReadOnlyList<EnemyActionProfileDefinition> CreateEnemyActions(string spawnKind)
        {
            return AllEnemySpecs
                .Where(spec => string.Equals(spec.OwnerId, spawnKind, StringComparison.Ordinal))
                .Select(spec => EnemyActionProfileDefinition.CreateRuntime(spec, ResolveLinkedAttack(spec)))
                .ToArray();
        }

        public static IReadOnlyList<EnemyActionProfileDefinition> CreateBossActions(string bossId)
        {
            return AllBossSpecs
                .Where(spec => string.Equals(spec.OwnerId, bossId, StringComparison.Ordinal))
                .Select(spec => EnemyActionProfileDefinition.CreateRuntime(spec, ResolveLinkedAttack(spec)))
                .ToArray();
        }

        public static IReadOnlyList<EnemyActionProfileDefinition> CreateLibraryTemplates()
        {
            return LibraryTemplateSpecs
                .Select(spec => EnemyActionProfileDefinition.CreateRuntime(spec, null))
                .ToArray();
        }

        public static EnemyActionProfileDefinition ResolveEnemyAction(string spawnKind, string actionId)
        {
            var spec = AllEnemySpecs.FirstOrDefault(candidate =>
                string.Equals(candidate.OwnerId, spawnKind, StringComparison.Ordinal) &&
                string.Equals(candidate.ActionId, actionId, StringComparison.Ordinal));
            return string.IsNullOrWhiteSpace(spec.ActionId)
                ? null
                : EnemyActionProfileDefinition.CreateRuntime(spec, ResolveLinkedAttack(spec));
        }

        public static EnemyActionProfileDefinition ResolveBossAction(string bossId, string actionId)
        {
            var spec = AllBossSpecs.FirstOrDefault(candidate =>
                string.Equals(candidate.OwnerId, bossId, StringComparison.Ordinal) &&
                string.Equals(candidate.ActionId, actionId, StringComparison.Ordinal));
            return string.IsNullOrWhiteSpace(spec.ActionId)
                ? null
                : EnemyActionProfileDefinition.CreateRuntime(spec, ResolveLinkedAttack(spec));
        }

        private static IEnumerable<EnemyActionProfileSpec> BuildEnemySpecs()
        {
            foreach (var attack in EnemyAttackProfileDefaults.AllEnemySpecs)
            {
                yield return CurrentFromAttack(attack);
            }

            foreach (var future in FutureEnemySpecs())
            {
                yield return future;
            }
        }

        private static IEnumerable<EnemyActionProfileSpec> BuildBossSpecs()
        {
            foreach (var attack in EnemyAttackProfileDefaults.AllBossSpecs)
            {
                yield return CurrentFromAttack(attack);
            }

            foreach (var future in FutureBossSpecs())
            {
                yield return future;
            }
        }

        private static EnemyActionProfileSpec CurrentFromAttack(EnemyAttackProfileSpec attack)
        {
            var range = ScoringRangeFor(attack);
            var category = CategoryFor(attack);
            var shape = ShapeFor(attack);
            var forceRating = ForceRating(attack.ForceClass);
            var isDirectional = shape is EnemyActionShape.ForwardArc or EnemyActionShape.Lane or EnemyActionShape.Cone or EnemyActionShape.Projectile or EnemyActionShape.Fan;
            var deliveryName = attack.DamageDelivery.ToString().ToLowerInvariant();
            return new EnemyActionProfileSpec(
                attack.OwnerId,
                attack.IsBoss,
                attack.AttackId,
                attack.DisplayName,
                category,
                IntentFor(attack),
                shape,
                EnemyActionUsageState.CurrentRuntime,
                attack.AttackId,
                explicitlyNonDamaging: attack.Damage <= 0,
                range.min,
                range.ideal,
                range.max,
                Math.Max(0.2f, 1f + attack.Damage * 0.2f + forceRating * 0.15f),
                attack.Damage <= 0 ? 0 : Math.Max(1, forceRating),
                $"current_{attack.RuntimeKind.ToString().ToLowerInvariant()}",
                MinimumIntelligenceForOwner(attack.OwnerId, attack.IsBoss),
                AllowedDispositionsForOwner(attack.OwnerId, attack.IsBoss),
                attack.Damage > 0 ? EnemyAwarenessState.Engaged : EnemyAwarenessState.Alerted,
                isDirectional,
                isDirectional ? attack.HitArcDegrees : 360f,
                $"Readable {deliveryName} tell before the active window; M80 timing remains the runtime source.",
                Math.Max(1, 4 - forceRating),
                Math.Min(5, forceRating + (attack.Damage > 1 ? 2 : 1)),
                $"Windup poise uses the linked M76/M80 threshold `{attack.PoiseBreakThreshold}`.",
                IsParryable(attack),
                attack.Damage > 0,
                true,
                "Recovery should leave a short punish window when the player dodges, blocks, or spaces the action.",
                TagsFor(attack),
                "M81 action wrapper around an existing M76 attack profile; runtime behavior is unchanged.");
        }

        private static IEnumerable<EnemyActionProfileSpec> FutureEnemySpecs()
        {
            yield return FutureEnemy("spawnEnemyNormal", "claw_combo", "Claw Combo", EnemyActionCategory.Body, EnemyActionIntent.Pressure, EnemyActionShape.ForwardArc, 0.6f, 1.1f, 1.6f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Engaged, 125f, "Two committed swipes with clear recovery.", "body-only", "Future creature pressure option; needs M82 selection and M76 impact profile.");
            yield return FutureEnemy("spawnEnemyNormal", "short_backstep", "Short Backstep", EnemyActionCategory.Movement, EnemyActionIntent.Reposition, EnemyActionShape.Self, 0.7f, 1.2f, 2f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Alerted, 360f, "Small hop away before re-engaging.", "body-only", "Keeps chasers from only driving straight forward.");
            yield return FutureEnemy("spawnEnemyNormal", "warning_feint", "Warning Feint", EnemyActionCategory.Body, EnemyActionIntent.Feint, EnemyActionShape.ForwardArc, 0.7f, 1.2f, 1.8f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Alerted, 100f, "Quick shoulder twitch without a hit window.", "body-only", "Future bait action; explicitly non-damaging in M81.");

            yield return FutureEnemy("spawnEnemyFlying", "fly_strafe", "Fly Strafe", EnemyActionCategory.Movement, EnemyActionIntent.Reposition, EnemyActionShape.Lane, 1.5f, 2.8f, 4.5f, EnemyIntelligenceLevel.Instinctive, new[] { EnemyInstinctDisposition.Prey }, EnemyAwarenessState.Suspicious, 180f, "Lateral drift that exposes the next dive angle.", "flying creature", "Preserves prey identity while making spacing legible.");
            yield return FutureEnemy("spawnEnemyFlying", "dive_feint", "Dive Feint", EnemyActionCategory.Body, EnemyActionIntent.Feint, EnemyActionShape.ForwardArc, 1f, 1.8f, 3f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Prey }, EnemyAwarenessState.Alerted, 120f, "False dive with no active damage.", "flying creature", "Future readable panic tell.");
            yield return FutureEnemy("spawnEnemyFlying", "panic_retreat", "Panic Retreat", EnemyActionCategory.Movement, EnemyActionIntent.Escape, EnemyActionShape.Self, 0.4f, 1.3f, 2.5f, EnemyIntelligenceLevel.Instinctive, new[] { EnemyInstinctDisposition.Prey }, EnemyAwarenessState.Alerted, 360f, "Short retreat burst after being threatened.", "flying creature", "Keeps Flying Chaser as the authored prey example.");

            yield return FutureEnemy("spawnEnemyFast", "snap_combo", "Snap Combo", EnemyActionCategory.Body, EnemyActionIntent.Pressure, EnemyActionShape.ForwardArc, 0.4f, 0.9f, 1.4f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Engaged, 130f, "Two light snaps with low guard pressure.", "body-only", "Future combo candidate for M82+.");
            yield return FutureEnemy("spawnEnemyFast", "evasive_skitter", "Evasive Skitter", EnemyActionCategory.Movement, EnemyActionIntent.Reposition, EnemyActionShape.Self, 0.6f, 1.1f, 2.2f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Alerted, 360f, "Brief diagonal reposition with no damage.", "body-only", "Lets fast enemies feel less magnetic.");

            yield return FutureEnemy("spawnEnemyHeavy", "guarded_shove", "Guarded Shove", EnemyActionCategory.Body, EnemyActionIntent.Interrupt, EnemyActionShape.ForwardArc, 0.5f, 1f, 1.55f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Mindless }, EnemyAwarenessState.Engaged, 110f, "Braced shove that checks greedy approaches.", "beast", "Future control action with medium guard pressure.");
            yield return FutureEnemy("spawnEnemyHeavy", "slow_overhead_slam", "Slow Overhead Slam", EnemyActionCategory.Body, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, 0.7f, 1.25f, 1.85f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Mindless }, EnemyAwarenessState.Engaged, 95f, "Long windup, high poise, large recovery.", "beast", "Dark Souls-style commitment candidate for heavy creatures.");

            yield return FutureEnemy("spawnEnemyCharger", "fire_trail_charge", "Fire Trail Charge", EnemyActionCategory.Hazard, EnemyActionIntent.HazardSetup, EnemyActionShape.Lane, 1.2f, 2.6f, 5.5f, EnemyIntelligenceLevel.Instinctive, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Engaged, 80f, "Charge path leaves a short-lived warning trail.", "beast", "Future hazard profile; no lingering fire in M81.");
            yield return FutureEnemy("spawnEnemyCharger", "ember_stomp", "Ember Stomp", EnemyActionCategory.Hazard, EnemyActionIntent.Pressure, EnemyActionShape.CircleArea, 0.4f, 0.9f, 1.5f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Engaged, 360f, "Ground flare after a heavy step.", "beast", "Future fire-aspected close pressure.");
            yield return FutureEnemy("spawnEnemyCharger", "short_recover_hop", "Short Recover Hop", EnemyActionCategory.Movement, EnemyActionIntent.Reposition, EnemyActionShape.Self, 0.4f, 1f, 1.8f, EnemyIntelligenceLevel.Instinctive, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Alerted, 360f, "Small reset after charge recovery.", "beast", "Gives charger readable reset behavior later.");

            yield return FutureEnemy("spawnEnemyTurret", "aimed_bone_shot", "Aimed Bone Shot", EnemyActionCategory.Ranged, EnemyActionIntent.Damage, EnemyActionShape.Projectile, 3f, 5.5f, 8.5f, EnemyIntelligenceLevel.Trained, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Alerted, 70f, "Narrow aim line, slow shot, clean side-step counter.", "ranged", "Stationary ranged option for a future action planner.");
            yield return FutureEnemy("spawnEnemyTurret", "wide_bone_volley", "Wide Bone Volley", EnemyActionCategory.Projectile, EnemyActionIntent.Pressure, EnemyActionShape.Fan, 3f, 5.5f, 8.5f, EnemyIntelligenceLevel.Trained, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Alerted, 95f, "Fan tell with gaps between projectiles.", "ranged", "Budgeted ranged pressure candidate.");
            yield return FutureEnemy("spawnEnemyTurret", "rattle_warning", "Rattle Warning", EnemyActionCategory.Defense, EnemyActionIntent.Feint, EnemyActionShape.Self, 2f, 4f, 7f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Suspicious, 360f, "Audible rattle before choosing a shot.", "ranged", "Non-damaging sentinel readability hook.");

            yield return FutureEnemy("spawnEnemySplitter", "cleave_feint", "Cleave Feint", EnemyActionCategory.Body, EnemyActionIntent.Feint, EnemyActionShape.ForwardArc, 0.7f, 1.25f, 1.9f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Alerted, 120f, "Raises arm, then resets without active damage.", "body-only", "Future bait action for Basic+ enemies.");
            yield return FutureEnemy("spawnEnemySplitter", "splitter_backstep", "Splitter Backstep", EnemyActionCategory.Movement, EnemyActionIntent.Reposition, EnemyActionShape.Self, 0.5f, 1.2f, 2f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Alerted, 360f, "Short step back before committing to cleave.", "body-only", "Keeps splitter from constant body pressure.");
            yield return FutureEnemy("spawnEnemySplitter", "splinter_burst", "Splinter Burst", EnemyActionCategory.Hazard, EnemyActionIntent.HazardSetup, EnemyActionShape.Radial, 0.5f, 1.1f, 1.8f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Engaged, 360f, "Radial splinter tell with obvious gaps.", "body-only", "Future area pressure; unlinked in M81.");

            yield return FutureEnemy("spawnEnemySpittingPod", "seed_burst", "Seed Burst", EnemyActionCategory.Projectile, EnemyActionIntent.Pressure, EnemyActionShape.Radial, 2f, 5f, 8f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Alerted, 360f, "Root shake before several arcing seeds.", "ranged", "Pod identity stays stationary and hearing-driven.");
            yield return FutureEnemy("spawnEnemySpittingPod", "hazard_seed_lob", "Hazard Seed Lob", EnemyActionCategory.Hazard, EnemyActionIntent.HazardSetup, EnemyActionShape.TargetPoint, 4f, 6f, 8f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Alerted, 360f, "Higher lob with clear landing marker.", "ranged", "Future hazard setup without adding puddles in M81.");
            yield return FutureEnemy("spawnEnemySpittingPod", "rooted_warning", "Rooted Warning", EnemyActionCategory.Defense, EnemyActionIntent.Feint, EnemyActionShape.Self, 2f, 5f, 8f, EnemyIntelligenceLevel.Instinctive, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Suspicious, 360f, "Stem coils before the pod decides to fire.", "ranged", "Non-damaging awareness transition tell.");

            yield return FutureEnemy("spawnEnemyRat", "skitter_retreat", "Skitter Retreat", EnemyActionCategory.Movement, EnemyActionIntent.Escape, EnemyActionShape.Self, 0.3f, 1f, 2.2f, EnemyIntelligenceLevel.Instinctive, new[] { EnemyInstinctDisposition.Territorial, EnemyInstinctDisposition.Prey }, EnemyAwarenessState.Alerted, 360f, "Fast retreat burst after damage.", "body-only", "Supports the authored 'retreats easily when damaged' rule.");
            yield return FutureEnemy("spawnEnemyRat", "panic_pounce", "Panic Pounce", EnemyActionCategory.Body, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, 0.4f, 0.8f, 1.2f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Territorial }, EnemyAwarenessState.Engaged, 120f, "Short committed leap after warning fails.", "body-only", "Needs explicit attack profile before runtime use.");

            yield return FutureEnemy("spawnEnemySpider", "web_feint", "Web Feint", EnemyActionCategory.Body, EnemyActionIntent.Feint, EnemyActionShape.Cone, 0.6f, 1.2f, 2f, EnemyIntelligenceLevel.Simple, new[] { EnemyInstinctDisposition.Prey }, EnemyAwarenessState.Suspicious, 180f, "Brief rear-up tell, then flee or hop.", "body-only", "Keeps spider fight/flight readable.");
            yield return FutureEnemy("spawnEnemySpider", "panic_flee", "Panic Flee", EnemyActionCategory.Movement, EnemyActionIntent.Escape, EnemyActionShape.Self, 0.2f, 0.9f, 2f, EnemyIntelligenceLevel.Instinctive, new[] { EnemyInstinctDisposition.Prey }, EnemyAwarenessState.Alerted, 360f, "Erratic burst away from the player.", "body-only", "Chaotic but capped flee action.");

            yield return FutureEnemy("spawnEnemySkeletonSword", "short_backstep", "Short Backstep", EnemyActionCategory.Movement, EnemyActionIntent.Reposition, EnemyActionShape.Self, 0.6f, 1.3f, 2.4f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Alerted, 360f, "Small backward hop after a blocked slash.", "weapon-user", "Future spacing reset for sword skeletons.");
            yield return FutureEnemy("spawnEnemySkeletonSword", "overhead_chop", "Overhead Chop", EnemyActionCategory.Weapon, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, 0.8f, 1.25f, 1.75f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Engaged, 80f, "Sword rises overhead before a narrow committed chop.", "weapon-user", "Future high-punish sword option.");
            yield return FutureEnemy("spawnEnemySkeletonSpear", "brace_poke", "Brace Poke", EnemyActionCategory.Weapon, EnemyActionIntent.Interrupt, EnemyActionShape.Lane, 1.2f, 2.1f, 2.9f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Alerted, 45f, "Spear plants before a precise poke.", "weapon-user", "Future anti-rush spear option.");
            yield return FutureEnemy("spawnEnemySkeletonSpear", "retreating_thrust", "Retreating Thrust", EnemyActionCategory.Weapon, EnemyActionIntent.Reposition, EnemyActionShape.Lane, 0.8f, 1.9f, 2.8f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Engaged, 50f, "Steps back as the spear point stays forward.", "weapon-user", "Future spacing attack once movement actions are richer.");
            yield return FutureEnemy("spawnEnemyKnight", "guarded_advance", "Guarded Advance", EnemyActionCategory.Defense, EnemyActionIntent.Defend, EnemyActionShape.Self, 1f, 1.8f, 2.8f, EnemyIntelligenceLevel.Trained, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Alerted, 150f, "Shield comes up before a slow advance.", "weapon-user", "Future shield movement action using the M84 guard tier.");
            yield return FutureEnemy("spawnEnemyKnight", "riposte", "Riposte", EnemyActionCategory.Weapon, EnemyActionIntent.Interrupt, EnemyActionShape.ForwardArc, 0.5f, 1f, 1.5f, EnemyIntelligenceLevel.Trained, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Engaged, 80f, "Short stab after a successful block.", "weapon-user", "Future counter action; no enemy parry in M84.");
            yield return FutureEnemy("spawnEnemyGiant", "ground_drag", "Ground Drag", EnemyActionCategory.Weapon, EnemyActionIntent.Pressure, EnemyActionShape.ForwardArc, 1.2f, 2f, 3.1f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Mindless }, EnemyAwarenessState.Engaged, 210f, "Club scrapes the ground before a very wide sweep.", "giant", "Future slow arena-control swing.");
            yield return FutureEnemy("spawnEnemyGiant", "slow_turn_slam", "Slow Turn Slam", EnemyActionCategory.Weapon, EnemyActionIntent.Damage, EnemyActionShape.CircleArea, 0.8f, 1.7f, 2.6f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Mindless }, EnemyAwarenessState.Engaged, 360f, "Whole body turns before a delayed slam.", "giant", "Future punish for staying behind too long.");

            yield return FutureEnemy("spawnEnemyBoss", "boss_stomp", "Boss Stomp", EnemyActionCategory.BossScale, EnemyActionIntent.Pressure, EnemyActionShape.CircleArea, 0.5f, 1.3f, 2.3f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Engaged, 360f, "Large foot lift, clear shockwave edge.", "boss-scale", "Generic fallback boss action wrapper for future planner work.");
            yield return FutureEnemy("spawnEnemyBoss", "boss_shockwave", "Boss Shockwave", EnemyActionCategory.BossScale, EnemyActionIntent.Pressure, EnemyActionShape.Radial, 0.8f, 2f, 5f, EnemyIntelligenceLevel.Basic, new[] { EnemyInstinctDisposition.Sentinel }, EnemyAwarenessState.Engaged, 360f, "Radial ring with visible travel time.", "boss-scale", "Fallback boss-scale pressure template.");
        }

        private static IEnumerable<EnemyActionProfileSpec> FutureBossSpecs()
        {
            yield return FutureBoss("stone_warden", "stone_shockwave", "Stone Shockwave", EnemyActionIntent.Pressure, EnemyActionShape.Radial, 0.8f, 2.2f, 5.5f, EnemyIntelligenceLevel.Basic, "Raised foot and ground crack before radial wave.", "future boss-scale stone pressure.");
            yield return FutureBoss("stone_warden", "stone_guarded_recover", "Stone Guarded Recover", EnemyActionIntent.Defend, EnemyActionShape.Self, 0f, 1f, 2.5f, EnemyIntelligenceLevel.Basic, "Braced recovery pose that invites spacing rather than greed.", "future defensive recovery option.");
            yield return FutureBoss("splinter_saint", "splinter_dash_feint", "Splinter Dash Feint", EnemyActionIntent.Feint, EnemyActionShape.Lane, 1f, 3f, 5.5f, EnemyIntelligenceLevel.Trained, "Side-hop dust tell with no active hit.", "future evasive feint.");
            yield return FutureBoss("splinter_saint", "splinter_needle_ring", "Splinter Needle Ring", EnemyActionIntent.Pressure, EnemyActionShape.Radial, 1f, 3f, 5.5f, EnemyIntelligenceLevel.Trained, "Brief hover before needles fan outward.", "future projectile boss pressure.");
            yield return FutureBoss("gravel_maw", "gravel_emerge_bite", "Gravel Emerge Bite", EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, 0.6f, 1.4f, 2.2f, EnemyIntelligenceLevel.Basic, "Bulging ground marker before emerge bite.", "future burrow punish action.");
            yield return FutureBoss("gravel_maw", "gravel_rock_wall", "Gravel Rock Wall", EnemyActionIntent.HazardSetup, EnemyActionShape.HazardZone, 1.5f, 3f, 5f, EnemyIntelligenceLevel.Basic, "Rubble line forms with safe gaps.", "future arena shaping action.");
            yield return FutureBoss("cartouche_widow", "cartouche_mark_delay", "Cartouche Mark Delay", EnemyActionIntent.HazardSetup, EnemyActionShape.TargetPoint, 2f, 4f, 7f, EnemyIntelligenceLevel.Cunning, "Delayed sigil mark gives a dodge timing puzzle.", "future marked-area action.");
            yield return FutureBoss("cartouche_widow", "cartouche_thread_guard", "Cartouche Thread Guard", EnemyActionIntent.Defend, EnemyActionShape.Self, 0f, 1f, 3f, EnemyIntelligenceLevel.Cunning, "Thread cocoon blocks greed, then opens.", "future boss defense action.");
            yield return FutureBoss("iron_reliquary", "iron_guard_peek", "Iron Guard Peek", EnemyActionIntent.Defend, EnemyActionShape.Cone, 2f, 4.5f, 6.5f, EnemyIntelligenceLevel.Tactical, "Cover lift before a disciplined peek.", "future cover-aware boss action metadata.");
            yield return FutureBoss("iron_reliquary", "iron_bash_recover", "Iron Bash Recover", EnemyActionIntent.Interrupt, EnemyActionShape.ForwardArc, 0.6f, 1.2f, 2f, EnemyIntelligenceLevel.Tactical, "Slow hinge windup before close bash.", "future punish for greedy close pressure.");
            yield return FutureBoss("mirror_husk", "mirror_decoy", "Mirror Decoy", EnemyActionIntent.Feint, EnemyActionShape.Self, 1f, 2f, 4f, EnemyIntelligenceLevel.Cunning, "Reflection flicker before the real body commits.", "future ghost-like misdirection.");
            yield return FutureBoss("mirror_husk", "mirror_parry_pulse", "Mirror Parry Pulse", EnemyActionIntent.Interrupt, EnemyActionShape.CircleArea, 0.5f, 1.1f, 2.1f, EnemyIntelligenceLevel.Cunning, "Mirror shine warns against attacking into the pulse.", "future interrupt profile.");
            yield return FutureBoss("ash_comet", "ash_fire_trail", "Ash Fire Trail", EnemyActionIntent.HazardSetup, EnemyActionShape.Lane, 1f, 3f, 6f, EnemyIntelligenceLevel.Trained, "Dash path glows before leaving a temporary trail.", "future fire hazard setup.");
            yield return FutureBoss("ash_comet", "ash_reentry_burst", "Ash Reentry Burst", EnemyActionIntent.Pressure, EnemyActionShape.CircleArea, 0.6f, 1.8f, 3f, EnemyIntelligenceLevel.Trained, "Comet hang time before landing burst.", "future jump/readability action.");
            yield return FutureBoss("choir_of_teeth", "choir_silence_pulse", "Choir Silence Pulse", EnemyActionIntent.Interrupt, EnemyActionShape.Radial, 1f, 2.4f, 5f, EnemyIntelligenceLevel.Tactical, "Brief quiet before a radial pulse.", "future rhythm-break action.");
            yield return FutureBoss("choir_of_teeth", "choir_rotating_guard", "Choir Rotating Guard", EnemyActionIntent.Defend, EnemyActionShape.Radial, 1f, 2.5f, 4.5f, EnemyIntelligenceLevel.Tactical, "Teeth circle inward, then reopen.", "future defensive pattern.");
            yield return FutureBoss("rust_bishop", "rust_guard_beam", "Rust Guard Beam", EnemyActionIntent.Defend, EnemyActionShape.Lane, 2f, 4f, 7f, EnemyIntelligenceLevel.Cunning, "Bishop braces before projecting a lane.", "future beam with guard posture.");
            yield return FutureBoss("rust_bishop", "rust_hazard_minefield", "Rust Hazard Minefield", EnemyActionIntent.HazardSetup, EnemyActionShape.HazardZone, 1f, 3f, 6f, EnemyIntelligenceLevel.Cunning, "Mines blink before arming.", "future arena hazard action.");
            yield return FutureBoss("hollow_star_larva", "larva_void_pulse", "Larva Void Pulse", EnemyActionIntent.Pressure, EnemyActionShape.Radial, 0.8f, 2.2f, 5.5f, EnemyIntelligenceLevel.Cunning, "Void breath in before radial pulse.", "future cosmic pressure action.");
            yield return FutureBoss("hollow_star_larva", "larva_star_guard", "Larva Star Guard", EnemyActionIntent.Defend, EnemyActionShape.Self, 0.5f, 1.4f, 3f, EnemyIntelligenceLevel.Cunning, "Stars orbit tightly before releasing.", "future cosmic defense action.");
        }

        private static IEnumerable<EnemyActionProfileSpec> BuildLibraryTemplateSpecs()
        {
            yield return Template("bite", "Bite", EnemyActionCategory.Body, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, "rats, spiders, beasts", "Small forward active window, clear head pull-back.", "Space or roll through the bite; punish recovery.");
            yield return Template("claw", "Claw", EnemyActionCategory.Body, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, "beasts, undead crows", "Shoulder and arm windup before slash.", "Backstep or block light guard pressure.");
            yield return Template("peck", "Peck", EnemyActionCategory.Body, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, "undead crows, flying critters", "Tiny forward jab with low recovery.", "Sidestep; whiff recovery is short.");
            yield return Template("pounce", "Pounce", EnemyActionCategory.Body, EnemyActionIntent.Pressure, EnemyActionShape.Lane, "rats, spiders, beasts", "Crouch, leap, active landing.", "Roll sideways; punish landing recovery.");
            yield return Template("tail_swipe", "Tail Swipe", EnemyActionCategory.Body, EnemyActionIntent.Interrupt, EnemyActionShape.CircleArea, "beasts, dragons, bosses", "Body twists before circular sweep.", "Move outside radius or roll through the active sweep.");
            yield return Template("body_slam", "Body Slam", EnemyActionCategory.Body, EnemyActionIntent.Damage, EnemyActionShape.CircleArea, "giants, beasts, bosses", "Large lift and fall, long recovery.", "Roll late; punish recovery heavily.");

            yield return Template("light_slash", "Light Slash", EnemyActionCategory.Weapon, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, "skeletons, knights", "Weapon shoulder tell, quick active arc.", "Block, parry, or roll behind.");
            yield return Template("heavy_slash", "Heavy Slash", EnemyActionCategory.Weapon, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, "knights, giants", "Slow two-handed windup.", "Dodge and punish long recovery.");
            yield return Template("thrust", "Thrust", EnemyActionCategory.Weapon, EnemyActionIntent.Damage, EnemyActionShape.Lane, "skeletons, knights, spear users", "Weapon point aligns before lane hit.", "Sidestep instead of backstepping.");
            yield return Template("overhead_slash", "Overhead Slash", EnemyActionCategory.Weapon, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, "knights, giants", "Weapon held high, narrow high-force impact.", "Strafe around the narrow arc.");
            yield return Template("sweep", "Sweep", EnemyActionCategory.Weapon, EnemyActionIntent.Pressure, EnemyActionShape.ForwardArc, "skeletons, knights, giants", "Low stance and wide horizontal tell.", "Roll through or stay outside the arc.");
            yield return Template("shield_bash", "Shield Bash", EnemyActionCategory.Weapon, EnemyActionIntent.Interrupt, EnemyActionShape.ForwardArc, "knights, shield skeletons", "Shield comes forward before stun/push.", "Bait it, then punish recovery.");

            yield return Template("arrow_shot", "Arrow Shot", EnemyActionCategory.Ranged, EnemyActionIntent.Damage, EnemyActionShape.Projectile, "archers", "Draw bow, aim line, release.", "Strafe after the draw commits.");
            yield return Template("arrow_volley", "Arrow Volley", EnemyActionCategory.Ranged, EnemyActionIntent.Pressure, EnemyActionShape.Fan, "archers, bosses", "Group aim or raised bow tell.", "Find gaps; avoid panic rolling into lanes.");
            yield return Template("aimed_shot", "Aimed Shot", EnemyActionCategory.Ranged, EnemyActionIntent.Damage, EnemyActionShape.Projectile, "archers, gunslingers, turrets", "Long aim lock before firing.", "Break timing with sidestep or cover.");
            yield return Template("thrown_knife", "Thrown Knife", EnemyActionCategory.Ranged, EnemyActionIntent.Interrupt, EnemyActionShape.Projectile, "rogues, skeletons", "Hand flick telegraph.", "Small dodge; recovery is modest.");
            yield return Template("pistol_shot", "Pistol Shot", EnemyActionCategory.Ranged, EnemyActionIntent.Damage, EnemyActionShape.Lane, "gunslingers, machines", "Muzzle raise and flash.", "React to aim, not the bullet.");
            yield return Template("cannon_shot", "Cannon Shot", EnemyActionCategory.Ranged, EnemyActionIntent.HazardSetup, EnemyActionShape.TargetPoint, "machines, giants, bosses", "Slow aim and loud charge.", "Move before impact marker lands.");

            yield return Template("slow_orb", "Slow Orb", EnemyActionCategory.Projectile, EnemyActionIntent.Pressure, EnemyActionShape.Projectile, "wizards, ghosts", "Orb grows before launch.", "Sidestep, then manage lingering screen pressure.");
            yield return Template("fast_bolt", "Fast Bolt", EnemyActionCategory.Projectile, EnemyActionIntent.Damage, EnemyActionShape.Projectile, "wizards, turrets", "Short cast spark before a straight bolt.", "Dodge on cast flash.");
            yield return Template("spread_shot", "Spread Shot", EnemyActionCategory.Projectile, EnemyActionIntent.Pressure, EnemyActionShape.Fan, "wizards, bosses", "Caster fans hands outward.", "Stand in a gap rather than outrunning the pattern.");
            yield return Template("radial_burst", "Radial Burst", EnemyActionCategory.Projectile, EnemyActionIntent.Pressure, EnemyActionShape.Radial, "wizards, bosses", "Core expands before ring release.", "Back out or roll through one projectile lane.");
            yield return Template("homing_shot", "Homing Shot", EnemyActionCategory.Projectile, EnemyActionIntent.Pressure, EnemyActionShape.Projectile, "wizards, ghosts", "Slow tracking orb with visible curve.", "Drag it wide, then dodge late.");
            yield return Template("falling_mark", "Falling Mark", EnemyActionCategory.Projectile, EnemyActionIntent.HazardSetup, EnemyActionShape.TargetPoint, "wizards, bosses", "Ground mark appears before impact.", "Keep moving; punish caster after drop.");

            yield return Template("beam", "Beam", EnemyActionCategory.Magic, EnemyActionIntent.Pressure, EnemyActionShape.Lane, "wizards, machines, bosses", "Charge line locks direction before active beam.", "Move perpendicular; punish long recovery.");
            yield return Template("fire_trail", "Fire Trail", EnemyActionCategory.Magic, EnemyActionIntent.HazardSetup, EnemyActionShape.HazardZone, "wizards, fire beasts", "Ground glows before trail becomes hazardous.", "Do not chase through the trail.");
            yield return Template("curse_field", "Curse Field", EnemyActionCategory.Magic, EnemyActionIntent.HazardSetup, EnemyActionShape.CircleArea, "wizards, soul eaters", "Circle sigil pulses before effect.", "Leave the circle; punish setup if close.");
            yield return Template("ground_eruption", "Ground Eruption", EnemyActionCategory.Magic, EnemyActionIntent.Damage, EnemyActionShape.TargetPoint, "wizards, giants, bosses", "Cracks appear under target.", "Move immediately; punish after eruption.");
            yield return Template("summoned_orb", "Summoned Orb", EnemyActionCategory.Magic, EnemyActionIntent.Summon, EnemyActionShape.Self, "wizards, ghosts", "Caster channels before helper appears.", "Interrupt windup or destroy helper.");
            yield return Template("magic_counter", "Magic Counter", EnemyActionCategory.Magic, EnemyActionIntent.Interrupt, EnemyActionShape.Self, "wizards, bosses", "Protective shimmer before counter pulse.", "Stop attacking or bait expiry.");

            yield return Template("sidestep", "Sidestep", EnemyActionCategory.Movement, EnemyActionIntent.Reposition, EnemyActionShape.Self, "knights, beasts, spiders", "Weight shift before lateral move.", "Track recovery; do not swing into empty space.");
            yield return Template("backstep", "Backstep", EnemyActionCategory.Movement, EnemyActionIntent.Escape, EnemyActionShape.Self, "rats, knights, wizards", "Small backward hop.", "Advance after recovery if stamina allows.");
            yield return Template("roll", "Roll", EnemyActionCategory.Movement, EnemyActionIntent.Escape, EnemyActionShape.Self, "humanoids, bosses", "Crouch before invulnerable movement.", "Punish roll recovery.");
            yield return Template("circle", "Circle", EnemyActionCategory.Movement, EnemyActionIntent.Reposition, EnemyActionShape.Self, "beasts, knights", "Side movement around player.", "Hold spacing and force commitment.");
            yield return Template("teleport", "Teleport", EnemyActionCategory.Movement, EnemyActionIntent.Reposition, EnemyActionShape.TargetPoint, "wizards, ghosts", "Vanish tell and destination flare.", "Read destination flare before chasing.");
            yield return Template("burrow", "Burrow", EnemyActionCategory.Movement, EnemyActionIntent.Escape, EnemyActionShape.TargetPoint, "beasts, bosses", "Dig tell and ground trail.", "Avoid emerge marker.");

            yield return Template("guard", "Guard", EnemyActionCategory.Defense, EnemyActionIntent.Defend, EnemyActionShape.Self, "knights, skeletons", "Shield raised, movement slows.", "Kick, flank, magic, or bait stamina break.");
            yield return Template("brace", "Brace", EnemyActionCategory.Defense, EnemyActionIntent.Defend, EnemyActionShape.Self, "giants, machines", "Rooted posture before resisting knockback.", "Use heavy attacks or reposition.");
            yield return Template("parry", "Parry", EnemyActionCategory.Defense, EnemyActionIntent.Interrupt, EnemyActionShape.ForwardArc, "knights, bosses", "Short weapon flourish before parry window.", "Delay attack or punish recovery.");
            yield return Template("evade", "Evade", EnemyActionCategory.Defense, EnemyActionIntent.Escape, EnemyActionShape.Self, "rogues, spiders", "Quick anticipation twitch.", "Bait evade, then punish landing.");
            yield return Template("shield_wall", "Shield Wall", EnemyActionCategory.Defense, EnemyActionIntent.Defend, EnemyActionShape.Cone, "knights, machines", "Shield locks forward.", "Attack sides or wait for guard drop.");
            yield return Template("counter_stance", "Counter Stance", EnemyActionCategory.Defense, EnemyActionIntent.Interrupt, EnemyActionShape.Self, "knights, bosses", "Still stance with clear shine.", "Do not hit into it; throw projectile or wait.");

            yield return Template("summon_minion", "Summon Minion", EnemyActionCategory.Summon, EnemyActionIntent.Summon, EnemyActionShape.Self, "wizards, bosses", "Long channel with spawn circles.", "Interrupt channel or clear adds.");
            yield return Template("summon_wave", "Summon Wave", EnemyActionCategory.Summon, EnemyActionIntent.Summon, EnemyActionShape.HazardZone, "bosses", "Multiple portals pulse in order.", "Prioritize safe lanes and avoid being boxed in.");
            yield return Template("raise_skeleton", "Raise Skeleton", EnemyActionCategory.Summon, EnemyActionIntent.Summon, EnemyActionShape.TargetPoint, "necromancers", "Bone pile shakes before raising.", "Destroy pile or interrupt caster.");
            yield return Template("spawn_trap", "Spawn Trap", EnemyActionCategory.Summon, EnemyActionIntent.HazardSetup, EnemyActionShape.HazardZone, "machines, wizards", "Trap marker arms after delay.", "Move out before it becomes active.");
            yield return Template("call_swarm", "Call Swarm", EnemyActionCategory.Summon, EnemyActionIntent.Pressure, EnemyActionShape.CircleArea, "beasts, bosses", "Cry tell before swarm arrives.", "Create distance, then clear small enemies.");
            yield return Template("clone_split", "Clone Split", EnemyActionCategory.Summon, EnemyActionIntent.Feint, EnemyActionShape.Self, "ghosts, bosses", "Body fractures into copies.", "Identify real body through movement or tell.");

            yield return Template("spike_trap", "Spike Trap", EnemyActionCategory.Hazard, EnemyActionIntent.HazardSetup, EnemyActionShape.HazardZone, "machines, rooms", "Floor plates rise before spikes.", "Step out; remember armed zones.");
            yield return Template("acid_puddle", "Acid Puddle", EnemyActionCategory.Hazard, EnemyActionIntent.HazardSetup, EnemyActionShape.CircleArea, "pods, beasts", "Glob lands, pool flashes active.", "Do not stand in the pool; kite around it.");
            yield return Template("fire_patch", "Fire Patch", EnemyActionCategory.Hazard, EnemyActionIntent.HazardSetup, EnemyActionShape.CircleArea, "fire beasts, wizards", "Ignition spark before active flame.", "Wait out duration or rotate around it.");
            yield return Template("mine", "Mine", EnemyActionCategory.Hazard, EnemyActionIntent.HazardSetup, EnemyActionShape.TargetPoint, "machines, bosses", "Small blinking object arms.", "Trigger safely or route around.");
            yield return Template("falling_debris", "Falling Debris", EnemyActionCategory.Hazard, EnemyActionIntent.Pressure, EnemyActionShape.TargetPoint, "giants, bosses", "Shadow marker before impact.", "Keep moving through safe cells.");
            yield return Template("closing_wall", "Closing Wall", EnemyActionCategory.Hazard, EnemyActionIntent.Pressure, EnemyActionShape.Lane, "machines, bosses", "Wall edge and rumble telegraph.", "Move through available gaps.");

            yield return Template("phase", "Phase", EnemyActionCategory.GhostSoul, EnemyActionIntent.Reposition, EnemyActionShape.Self, "ghosts, soul eaters", "Transparency tell before pass-through.", "Track reappearance; do not rely on body blocking.");
            yield return Template("possess", "Possess", EnemyActionCategory.GhostSoul, EnemyActionIntent.Interrupt, EnemyActionShape.Projectile, "ghosts, soul eaters", "Slow soul tether reaches out.", "Break tether or dodge projectile.");
            yield return Template("soul_drain", "Soul Drain", EnemyActionCategory.GhostSoul, EnemyActionIntent.Damage, EnemyActionShape.Cone, "soul eaters, bosses", "Channel cone pulls energy.", "Strafe out of cone; interrupt if close.");
            yield return Template("curse", "Curse", EnemyActionCategory.GhostSoul, EnemyActionIntent.HazardSetup, EnemyActionShape.TargetPoint, "ghosts, wizards", "Mark appears over target.", "Move, cleanse later, or interrupt cast.");
            yield return Template("fear_pulse", "Fear Pulse", EnemyActionCategory.GhostSoul, EnemyActionIntent.Pressure, EnemyActionShape.Radial, "ghosts, bosses", "Audio drop before pulse expands.", "Roll through edge or stay outside.");
            yield return Template("re_form", "Re-form", EnemyActionCategory.GhostSoul, EnemyActionIntent.Defend, EnemyActionShape.Self, "ghosts, soul eaters", "Fragments gather before body returns.", "Punish just after re-form.");

            yield return Template("shockwave", "Shockwave", EnemyActionCategory.BossScale, EnemyActionIntent.Pressure, EnemyActionShape.Radial, "bosses, giants", "Ground ring expands from boss.", "Roll through edge or create distance.");
            yield return Template("arena_hazard", "Arena Hazard", EnemyActionCategory.BossScale, EnemyActionIntent.HazardSetup, EnemyActionShape.HazardZone, "bosses", "Arena zones pulse before activation.", "Read safe lanes, then punish boss downtime.");
            yield return Template("multi_stage_combo", "Multi-Stage Combo", EnemyActionCategory.BossScale, EnemyActionIntent.Pressure, EnemyActionShape.ForwardArc, "bosses, knights", "Distinct chained tells, each with recovery.", "Learn cadence; punish final recovery.");
            yield return Template("desperation_burst", "Desperation Burst", EnemyActionCategory.BossScale, EnemyActionIntent.Pressure, EnemyActionShape.Radial, "bosses", "Low-health scream and long windup.", "Retreat, roll final ring, then punish.");
            yield return Template("rotating_pattern", "Rotating Pattern", EnemyActionCategory.BossScale, EnemyActionIntent.Pressure, EnemyActionShape.Radial, "bosses, machines", "Pattern rotates at readable speed.", "Move with the rotation through gaps.");
            yield return Template("boss_grab", "Boss Grab", EnemyActionCategory.BossScale, EnemyActionIntent.Damage, EnemyActionShape.ForwardArc, "bosses, giants", "Open hand and forward lean.", "Dodge sideways; never block full grab.");
        }

        private static EnemyActionProfileSpec FutureEnemy(
            string ownerId,
            string actionId,
            string displayName,
            EnemyActionCategory category,
            EnemyActionIntent intent,
            EnemyActionShape shape,
            float minRange,
            float idealRange,
            float maxRange,
            EnemyIntelligenceLevel minimumIntelligence,
            IEnumerable<EnemyInstinctDisposition> dispositions,
            EnemyAwarenessState awareness,
            float facingArc,
            string telegraph,
            string bestUserTag,
            string notes)
        {
            return Action(ownerId, false, actionId, displayName, category, intent, shape, EnemyActionUsageState.FutureCandidate, string.Empty, true, minRange, idealRange, maxRange, 0.75f, 0, $"future_{category.ToString().ToLowerInvariant()}", minimumIntelligence, dispositions, awareness, shape != EnemyActionShape.Self && shape != EnemyActionShape.CircleArea && shape != EnemyActionShape.Radial, facingArc, telegraph, 3, 1, "Future action has no runtime poise break until linked to an attack profile.", false, false, true, "Future recovery should be authored before enabling runtime selection.", new[] { bestUserTag }, notes);
        }

        private static EnemyActionProfileSpec FutureBoss(
            string ownerId,
            string actionId,
            string displayName,
            EnemyActionIntent intent,
            EnemyActionShape shape,
            float minRange,
            float idealRange,
            float maxRange,
            EnemyIntelligenceLevel minimumIntelligence,
            string telegraph,
            string notes)
        {
            return Action(ownerId, true, actionId, displayName, EnemyActionCategory.BossScale, intent, shape, EnemyActionUsageState.FutureCandidate, string.Empty, true, minRange, idealRange, maxRange, 0.8f, 0, "future_boss", minimumIntelligence, new[] { EnemyInstinctDisposition.Sentinel, EnemyInstinctDisposition.Predator }, EnemyAwarenessState.Engaged, shape is EnemyActionShape.ForwardArc or EnemyActionShape.Lane or EnemyActionShape.Cone, shape is EnemyActionShape.ForwardArc ? 130f : 360f, telegraph, 3, 2, "Boss future action has no interrupt rule until a runtime profile is linked.", false, true, true, "Boss-scale recovery should be explicit before runtime use.", new[] { "boss-scale" }, notes);
        }

        private static EnemyActionProfileSpec Template(
            string actionId,
            string displayName,
            EnemyActionCategory category,
            EnemyActionIntent intent,
            EnemyActionShape shape,
            string bestUsers,
            string telegraph,
            string counterplay)
        {
            var tags = bestUsers
                .Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                .ToArray();
            return Action("library", false, actionId, displayName, category, intent, shape, EnemyActionUsageState.LibraryTemplate, string.Empty, true, 0f, 1.5f, 6f, 0.5f, 0, $"template_{category.ToString().ToLowerInvariant()}", EnemyIntelligenceLevel.Instinctive, AllDispositions, EnemyAwarenessState.Alerted, shape is EnemyActionShape.ForwardArc or EnemyActionShape.Lane or EnemyActionShape.Cone or EnemyActionShape.Projectile or EnemyActionShape.Fan, shape is EnemyActionShape.ForwardArc ? 120f : 360f, telegraph, 3, category is EnemyActionCategory.Defense ? 4 : 2, "Template poise is decided by the eventual linked attack or behavior action.", category is EnemyActionCategory.Body or EnemyActionCategory.Weapon, true, true, counterplay, tags, "Reusable M78-derived action template for M82+ behavior tree selection.");
        }

        private static EnemyActionProfileSpec Action(
            string ownerId,
            bool isBoss,
            string actionId,
            string displayName,
            EnemyActionCategory category,
            EnemyActionIntent intent,
            EnemyActionShape shape,
            EnemyActionUsageState usageState,
            string linkedAttackId,
            bool explicitlyNonDamaging,
            float minRange,
            float idealRange,
            float maxRange,
            float baseWeight,
            int pressureCost,
            string cooldownGroup,
            EnemyIntelligenceLevel minimumIntelligence,
            IEnumerable<EnemyInstinctDisposition> allowedDispositions,
            EnemyAwarenessState awareness,
            bool requiresFacing,
            float facingArc,
            string telegraph,
            int punishability,
            int guardPressure,
            string poise,
            bool parryable,
            bool blockable,
            bool dodgeable,
            string recoveryPunish,
            IEnumerable<string> tags,
            string notes)
        {
            return new EnemyActionProfileSpec(ownerId, isBoss, actionId, displayName, category, intent, shape, usageState, linkedAttackId, explicitlyNonDamaging, minRange, idealRange, maxRange, baseWeight, pressureCost, cooldownGroup, minimumIntelligence, allowedDispositions, awareness, requiresFacing, facingArc, telegraph, punishability, guardPressure, poise, parryable, blockable, dodgeable, recoveryPunish, tags, notes);
        }

        private static (float min, float ideal, float max) ScoringRangeFor(EnemyAttackProfileSpec attack)
        {
            if (attack.DamageDelivery == DamageDelivery.Projectile || attack.RuntimeKind is EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile)
            {
                var max = Math.Max(attack.RangeMeters, 3f);
                return (Math.Min(2f, max * 0.35f), max * 0.7f, max);
            }

            if (attack.RuntimeKind == EnemyAttackRuntimeKind.Summon)
            {
                return (0.5f, 3f, 7f);
            }

            var meleeMax = Math.Max(attack.RangeMeters, 0.75f);
            return (0f, Math.Max(0.4f, meleeMax * 0.75f), meleeMax);
        }

        private static EnemyActionCategory CategoryFor(EnemyAttackProfileSpec attack)
        {
            if (attack.IsBoss)
            {
                return EnemyActionCategory.BossScale;
            }

            if (attack.AttackId is "stomp" or "warning_squeal")
            {
                return EnemyActionCategory.Body;
            }

            return attack.RuntimeKind switch
            {
                EnemyAttackRuntimeKind.WeaponMelee => EnemyActionCategory.Weapon,
                EnemyAttackRuntimeKind.Defense => EnemyActionCategory.Defense,
                EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile => EnemyActionCategory.Projectile,
                EnemyAttackRuntimeKind.Summon or EnemyAttackRuntimeKind.Split => EnemyActionCategory.Summon,
                EnemyAttackRuntimeKind.Area => EnemyActionCategory.Hazard,
                EnemyAttackRuntimeKind.Movement => EnemyActionCategory.Movement,
                _ => EnemyActionCategory.Body
            };
        }

        private static EnemyActionIntent IntentFor(EnemyAttackProfileSpec attack)
        {
            if (attack.AttackId == "warning_squeal")
            {
                return EnemyActionIntent.Feint;
            }

            if (attack.RuntimeKind == EnemyAttackRuntimeKind.Defense)
            {
                return EnemyActionIntent.Defend;
            }

            if (attack.AttackId == "stomp")
            {
                return EnemyActionIntent.Damage;
            }

            if (attack.RuntimeKind == EnemyAttackRuntimeKind.Summon || attack.RuntimeKind == EnemyAttackRuntimeKind.Split)
            {
                return EnemyActionIntent.Summon;
            }

            if (attack.RuntimeKind == EnemyAttackRuntimeKind.Area || attack.DamageDelivery == DamageDelivery.Area)
            {
                return attack.Damage > 0 ? EnemyActionIntent.Pressure : EnemyActionIntent.HazardSetup;
            }

            return attack.Damage > 0 ? EnemyActionIntent.Damage : EnemyActionIntent.Pressure;
        }

        private static EnemyActionShape ShapeFor(EnemyAttackProfileSpec attack)
        {
            if (attack.AttackId == "warning_squeal")
            {
                return EnemyActionShape.Cone;
            }

            if (attack.RuntimeKind == EnemyAttackRuntimeKind.Defense)
            {
                return EnemyActionShape.Self;
            }

            if (attack.AttackId == "stomp")
            {
                return EnemyActionShape.CircleArea;
            }

            return attack.RuntimeKind switch
            {
                EnemyAttackRuntimeKind.Projectile => EnemyActionShape.Projectile,
                EnemyAttackRuntimeKind.FanProjectile => EnemyActionShape.Fan,
                EnemyAttackRuntimeKind.RadialProjectile => EnemyActionShape.Radial,
                EnemyAttackRuntimeKind.Charge => EnemyActionShape.Lane,
                EnemyAttackRuntimeKind.Summon or EnemyAttackRuntimeKind.Split or EnemyAttackRuntimeKind.Area => EnemyActionShape.CircleArea,
                EnemyAttackRuntimeKind.Movement => EnemyActionShape.Lane,
                _ => EnemyActionShape.ForwardArc
            };
        }

        private static EnemyIntelligenceLevel MinimumIntelligenceForOwner(string ownerId, bool isBoss)
        {
            if (isBoss)
            {
                return ownerId switch
                {
                    "stone_warden" => EnemyIntelligenceLevel.Basic,
                    "splinter_saint" => EnemyIntelligenceLevel.Trained,
                    "gravel_maw" => EnemyIntelligenceLevel.Basic,
                    "cartouche_widow" => EnemyIntelligenceLevel.Cunning,
                    "iron_reliquary" => EnemyIntelligenceLevel.Tactical,
                    "mirror_husk" => EnemyIntelligenceLevel.Cunning,
                    "ash_comet" => EnemyIntelligenceLevel.Trained,
                    "choir_of_teeth" => EnemyIntelligenceLevel.Tactical,
                    "rust_bishop" => EnemyIntelligenceLevel.Cunning,
                    "hollow_star_larva" => EnemyIntelligenceLevel.Cunning,
                    _ => EnemyIntelligenceLevel.Basic
                };
            }

            return ownerId switch
            {
                "spawnEnemyFlying" => EnemyIntelligenceLevel.Instinctive,
                "spawnEnemyCharger" => EnemyIntelligenceLevel.Instinctive,
                "spawnEnemySplitter" => EnemyIntelligenceLevel.Basic,
                "spawnEnemyTurret" => EnemyIntelligenceLevel.Trained,
                "spawnEnemyRat" => EnemyIntelligenceLevel.Basic,
                "spawnEnemySpider" => EnemyIntelligenceLevel.Simple,
                "spawnEnemySkeletonSword" or "spawnEnemySkeletonSpear" or "spawnEnemyGiant" => EnemyIntelligenceLevel.Basic,
                "spawnEnemyKnight" => EnemyIntelligenceLevel.Trained,
                _ => EnemyIntelligenceLevel.Simple
            };
        }

        private static IReadOnlyList<EnemyInstinctDisposition> AllowedDispositionsForOwner(string ownerId, bool isBoss)
        {
            if (isBoss)
            {
                return new[] { EnemyInstinctDisposition.Sentinel, EnemyInstinctDisposition.Predator };
            }

            return ownerId switch
            {
                "spawnEnemyFlying" => new[] { EnemyInstinctDisposition.Prey },
                "spawnEnemyHeavy" => new[] { EnemyInstinctDisposition.Mindless },
                "spawnEnemyTurret" or "spawnEnemySpittingPod" or "spawnEnemyBoss" => new[] { EnemyInstinctDisposition.Sentinel },
                "spawnEnemyRat" => new[] { EnemyInstinctDisposition.Territorial },
                "spawnEnemySpider" => new[] { EnemyInstinctDisposition.Prey },
                "spawnEnemySkeletonSpear" or "spawnEnemyKnight" => new[] { EnemyInstinctDisposition.Sentinel },
                "spawnEnemyGiant" => new[] { EnemyInstinctDisposition.Mindless },
                _ => new[] { EnemyInstinctDisposition.Predator }
            };
        }

        private static IReadOnlyList<string> TagsFor(EnemyAttackProfileSpec attack)
        {
            if (attack.IsBoss)
            {
                return new[] { "boss-scale" };
            }

            return attack.OwnerId switch
            {
                "spawnEnemyTurret" or "spawnEnemySpittingPod" => new[] { "ranged", "stationary" },
                "spawnEnemyRat" or "spawnEnemySpider" => new[] { "body-only", "critter" },
                "spawnEnemySkeletonSword" or "spawnEnemySkeletonSpear" or "spawnEnemyKnight" => new[] { "weapon-user", "humanoid" },
                "spawnEnemyGiant" => new[] { "weapon-user", "giant" },
                "spawnEnemyFlying" => new[] { "body-only", "flying creature" },
                _ => new[] { "body-only" }
            };
        }

        private static bool IsParryable(EnemyAttackProfileSpec attack)
        {
            return (attack.RuntimeKind is EnemyAttackRuntimeKind.Contact or EnemyAttackRuntimeKind.MeleeLunge or EnemyAttackRuntimeKind.Charge or EnemyAttackRuntimeKind.WeaponMelee) &&
                   attack.DamageDelivery != DamageDelivery.Projectile;
        }

        private static int ForceRating(ImpactForceClass forceClass)
        {
            return forceClass switch
            {
                ImpactForceClass.Massive => 4,
                ImpactForceClass.Heavy => 3,
                ImpactForceClass.Medium => 2,
                _ => 1
            };
        }

        private static EnemyAttackProfileDefinition ResolveLinkedAttack(EnemyActionProfileSpec spec)
        {
            if (!spec.HasLinkedAttack)
            {
                return null;
            }

            return spec.IsBoss
                ? EnemyAttackProfileDefaults.ResolveBossProfile(spec.OwnerId, spec.LinkedAttackId)
                : EnemyAttackProfileDefaults.ResolveEnemyProfile(spec.OwnerId, spec.LinkedAttackId);
        }
    }
}
