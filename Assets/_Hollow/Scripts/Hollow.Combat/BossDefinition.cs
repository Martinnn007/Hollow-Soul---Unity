using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public enum BossWorldBand
    {
        World1 = 1,
        World2 = 2,
        World3 = 3
    }

    public enum BossBehaviorId
    {
        StoneWarden,
        SplinterSaint,
        GravelMaw,
        CartoucheWidow,
        IronReliquary,
        MirrorHusk,
        AshComet,
        ChoirOfTeeth,
        RustBishop,
        HollowStarLarva
    }

    public enum BossAttackKind
    {
        Chase,
        Charge,
        Jump,
        RadialBurst,
        FallingProjectiles,
        CoverShot,
        Split,
        BurrowSummon,
        Beam,
        Mine,
        Stomp,
        DesperationBurst
    }

    [Serializable]
    public sealed class BossPhaseDefinition
    {
        [Range(0f, 1f)] public float healthThreshold01 = 1f;
        public string displayName = "Phase 1";
        public string statusText = "Engaging";

        public BossPhaseDefinition()
        {
        }

        public BossPhaseDefinition(float threshold01, string name, string status)
        {
            healthThreshold01 = Mathf.Clamp01(threshold01);
            displayName = string.IsNullOrWhiteSpace(name) ? "Phase" : name;
            statusText = string.IsNullOrWhiteSpace(status) ? displayName : status;
        }
    }

    [Serializable]
    public sealed class BossAttackDefinition
    {
        public BossAttackKind kind;
        public float cooldownSeconds = 2f;
        public float windupSeconds = 0.45f;
        public int damage = 1;
        public int projectileCount = 1;

        public BossAttackDefinition()
        {
        }

        public BossAttackDefinition(BossAttackKind nextKind, float cooldown, float windup, int nextDamage, int nextProjectileCount = 1)
        {
            kind = nextKind;
            cooldownSeconds = Mathf.Max(0.05f, cooldown);
            windupSeconds = Mathf.Max(0f, windup);
            damage = Mathf.Clamp(nextDamage, 1, 2);
            projectileCount = Mathf.Max(0, nextProjectileCount);
        }
    }

    [Serializable]
    public sealed class BossArenaDefinition
    {
        public string arenaId = string.Empty;
        public string displayName = string.Empty;

        public BossArenaDefinition()
        {
        }

        public BossArenaDefinition(string nextArenaId, string nextDisplayName)
        {
            arenaId = string.IsNullOrWhiteSpace(nextArenaId) ? string.Empty : nextArenaId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? arenaId : nextDisplayName;
        }
    }

    public sealed class BossDefinition : ScriptableObject
    {
        [SerializeField] private string bossId = "stone_warden";
        [SerializeField] private string displayName = "Stone Warden";
        [SerializeField] private BossWorldBand worldBand = BossWorldBand.World1;
        [SerializeField] private BossBehaviorId behaviorId = BossBehaviorId.StoneWarden;
        [SerializeField] private int maxHealth = 24;
        [SerializeField] private float speedMetersPerSecond = 0.85f;
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float contactCooldownSeconds = 1f;
        [SerializeField] private EnemyContactDamagePolicy contactDamagePolicy = EnemyContactDamagePolicy.ActiveOnly;
        [SerializeField] private EnemyPassiveContactHazardType passiveContactHazardType = EnemyPassiveContactHazardType.None;
        [SerializeField] private float radiusMeters = 0.65f;
        [SerializeField] private float projectileSpeedMetersPerSecond = 4.8f;
        [SerializeField] private float visualScale = 2f;
        [SerializeField] private EnemyBodyClass bodyClass = EnemyBodyClass.Massive;
        [SerializeField] private EnemyIntelligenceLevel intelligence = EnemyIntelligenceLevel.Basic;
        [SerializeField] private float sightRadiusMeters = 8f;
        [SerializeField] private float sightAngleDegrees = 140f;
        [SerializeField] private float hearingRadiusMeters = 5f;
        [SerializeField] private Color debugColor = new(0.42f, 0.34f, 0.28f, 1f);
        [SerializeField] private BossArenaDefinition arena = new("boss_arena_broken_gateyard", "Broken Gateyard");
        [SerializeField] private List<BossPhaseDefinition> phases = new();
        [SerializeField] private List<BossAttackDefinition> attacks = new();
        [SerializeField] private List<EnemyAttackProfileDefinition> attackProfiles = new();
        [SerializeField] private List<EnemyActionProfileDefinition> actionProfiles = new();
        [SerializeField] private EnemyBehaviorTreeDefinition behaviorTreeMetadata;
        [SerializeField] private EnemySpacingProfileDefinition spacingProfileMetadata;

        public string BossId => string.IsNullOrWhiteSpace(bossId) ? "stone_warden" : bossId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? BossId : displayName;

        public BossWorldBand WorldBand => worldBand;

        public BossBehaviorId BehaviorId => behaviorId;

        public int MaxHealth => Mathf.Clamp(maxHealth, 20, 50);

        public float SpeedMetersPerSecond => Mathf.Max(0.05f, speedMetersPerSecond);

        public int ContactDamage => Mathf.Clamp(contactDamage, 1, 2);

        public float ContactCooldownSeconds => Mathf.Max(0.2f, contactCooldownSeconds);

        public EnemyContactDamagePolicy ContactDamagePolicy => contactDamagePolicy;

        public EnemyPassiveContactHazardType PassiveContactHazardType => passiveContactHazardType;

        public float RadiusMeters => Mathf.Max(0.25f, radiusMeters);

        public float ProjectileSpeedMetersPerSecond => Mathf.Max(0.1f, projectileSpeedMetersPerSecond);

        public float VisualScale => Mathf.Clamp(visualScale, 1f, 3.5f);

        public EnemyBodyClass BodyClass => bodyClass;

        public EnemyIntelligenceLevel Intelligence => EnemyIntelligenceLevelExtensions.Clamp((int)intelligence);

        public float SightRadiusMeters => Mathf.Max(0f, sightRadiusMeters);

        public float SightAngleDegrees => SightRadiusMeters <= 0f ? 0f : Mathf.Clamp(sightAngleDegrees, 0f, 360f);

        public float HearingRadiusMeters => Mathf.Max(0f, hearingRadiusMeters);

        public Color DebugColor => debugColor;

        public BossArenaDefinition Arena => arena ?? new BossArenaDefinition("boss_arena_broken_gateyard", "Broken Gateyard");

        public IReadOnlyList<BossPhaseDefinition> Phases => phases;

        public IReadOnlyList<BossAttackDefinition> Attacks => attacks;

        public IReadOnlyList<EnemyAttackProfileDefinition> AttackProfiles
        {
            get
            {
                var authored = attackProfiles?.Where(profile => profile != null).ToArray() ?? Array.Empty<EnemyAttackProfileDefinition>();
                var fallback = EnemyAttackProfileDefaults.CreateBossProfiles(BossId);
                return authored
                    .Concat(fallback.Where(profile => authored.All(existing => existing.AttackId != profile.AttackId)))
                    .ToArray();
            }
        }

        public IReadOnlyList<EnemyActionProfileDefinition> ActionProfiles
        {
            get
            {
                var authored = actionProfiles?.Where(profile => profile != null).ToArray() ?? Array.Empty<EnemyActionProfileDefinition>();
                var fallback = EnemyActionProfileDefaults.CreateBossActions(BossId);
                return authored
                    .Concat(fallback.Where(profile => authored.All(existing => existing.ActionId != profile.ActionId)))
                    .ToArray();
            }
        }

        public EnemyBehaviorTreeDefinition BehaviorTreeMetadata => behaviorTreeMetadata != null
            ? behaviorTreeMetadata
            : EnemyBehaviorTreeDefaults.ResolveBossTree(BossId);

        public EnemySpacingProfileDefinition SpacingProfileMetadata => spacingProfileMetadata != null
            ? spacingProfileMetadata
            : EnemySpacingProfileDefaults.CreateBossMetadataProfile(this);

        public void Configure(
            string nextBossId,
            string nextDisplayName,
            BossWorldBand nextWorldBand,
            BossBehaviorId nextBehaviorId,
            int nextMaxHealth,
            float nextSpeedMetersPerSecond,
            int nextContactDamage,
            float nextContactCooldownSeconds,
            float nextRadiusMeters,
            float nextProjectileSpeedMetersPerSecond,
            float nextVisualScale,
            Color nextDebugColor,
            BossArenaDefinition nextArena,
            IEnumerable<BossPhaseDefinition> nextPhases,
            IEnumerable<BossAttackDefinition> nextAttacks,
            EnemyBodyClass nextBodyClass = EnemyBodyClass.Massive)
        {
            Configure(
                nextBossId,
                nextDisplayName,
                nextWorldBand,
                nextBehaviorId,
                nextMaxHealth,
                nextSpeedMetersPerSecond,
                nextContactDamage,
                nextContactCooldownSeconds,
                nextRadiusMeters,
                nextProjectileSpeedMetersPerSecond,
                nextVisualScale,
                nextDebugColor,
                nextArena,
                nextPhases,
                nextAttacks,
                nextBodyClass,
                SignatureIntelligenceFor(nextBehaviorId));
        }

        public void Configure(
            string nextBossId,
            string nextDisplayName,
            BossWorldBand nextWorldBand,
            BossBehaviorId nextBehaviorId,
            int nextMaxHealth,
            float nextSpeedMetersPerSecond,
            int nextContactDamage,
            float nextContactCooldownSeconds,
            float nextRadiusMeters,
            float nextProjectileSpeedMetersPerSecond,
            float nextVisualScale,
            Color nextDebugColor,
            BossArenaDefinition nextArena,
            IEnumerable<BossPhaseDefinition> nextPhases,
            IEnumerable<BossAttackDefinition> nextAttacks,
            EnemyBodyClass nextBodyClass,
            EnemyIntelligenceLevel nextIntelligence)
        {
            bossId = string.IsNullOrWhiteSpace(nextBossId) ? "boss" : nextBossId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? bossId : nextDisplayName;
            worldBand = nextWorldBand;
            behaviorId = nextBehaviorId;
            maxHealth = Mathf.Clamp(nextMaxHealth, 20, 50);
            speedMetersPerSecond = Mathf.Max(0.05f, nextSpeedMetersPerSecond);
            contactDamage = Mathf.Clamp(nextContactDamage, 1, 2);
            contactCooldownSeconds = Mathf.Max(0.2f, nextContactCooldownSeconds);
            contactDamagePolicy = EnemyContactDamagePolicy.ActiveOnly;
            passiveContactHazardType = EnemyPassiveContactHazardType.None;
            radiusMeters = Mathf.Max(0.25f, nextRadiusMeters);
            projectileSpeedMetersPerSecond = Mathf.Max(0.1f, nextProjectileSpeedMetersPerSecond);
            visualScale = Mathf.Clamp(nextVisualScale, 1f, 3.5f);
            bodyClass = nextBodyClass;
            intelligence = EnemyIntelligenceLevelExtensions.Clamp((int)nextIntelligence);
            var senses = SignatureSensesFor(nextBehaviorId);
            ConfigureSenseMetadata(senses.x, senses.y, senses.z);
            debugColor = nextDebugColor;
            arena = nextArena ?? new BossArenaDefinition("boss_arena_broken_gateyard", "Broken Gateyard");
            phases = nextPhases?.Where(phase => phase != null).OrderByDescending(phase => phase.healthThreshold01).ToList() ?? new List<BossPhaseDefinition>();
            attacks = nextAttacks?.Where(attack => attack != null).ToList() ?? new List<BossAttackDefinition>();
        }

        public void ConfigureSenseMetadata(float nextSightRadiusMeters, float nextSightAngleDegrees, float nextHearingRadiusMeters)
        {
            sightRadiusMeters = Mathf.Max(0f, nextSightRadiusMeters);
            sightAngleDegrees = sightRadiusMeters <= 0f ? 0f : Mathf.Clamp(nextSightAngleDegrees, 0f, 360f);
            hearingRadiusMeters = Mathf.Max(0f, nextHearingRadiusMeters);
        }

        public void ConfigureContactPolicy(
            EnemyContactDamagePolicy nextContactDamagePolicy,
            EnemyPassiveContactHazardType nextPassiveContactHazardType)
        {
            contactDamagePolicy = nextContactDamagePolicy;
            passiveContactHazardType = contactDamagePolicy == EnemyContactDamagePolicy.PassiveHazard
                ? nextPassiveContactHazardType
                : EnemyPassiveContactHazardType.None;
        }

        public void ConfigureAttackProfiles(IEnumerable<EnemyAttackProfileDefinition> nextAttackProfiles)
        {
            attackProfiles = nextAttackProfiles?.Where(profile => profile != null).ToList() ?? new List<EnemyAttackProfileDefinition>();
        }

        public void ConfigureActionProfiles(IEnumerable<EnemyActionProfileDefinition> nextActionProfiles)
        {
            actionProfiles = nextActionProfiles?.Where(profile => profile != null).ToList() ?? new List<EnemyActionProfileDefinition>();
        }

        public void ConfigureBehaviorTreeMetadata(EnemyBehaviorTreeDefinition nextBehaviorTreeMetadata)
        {
            behaviorTreeMetadata = nextBehaviorTreeMetadata;
        }

        public void ConfigureSpacingProfileMetadata(EnemySpacingProfileDefinition nextSpacingProfileMetadata)
        {
            spacingProfileMetadata = nextSpacingProfileMetadata;
        }

        public EnemyAttackProfileDefinition ResolveAttackProfile(string attackId)
        {
            if (attackProfiles != null)
            {
                var authored = attackProfiles.FirstOrDefault(profile =>
                    profile != null &&
                    string.Equals(profile.AttackId, attackId, StringComparison.Ordinal));
                if (authored != null)
                {
                    return authored;
                }
            }

            return EnemyAttackProfileDefaults.ResolveBossProfile(BossId, attackId) ?? AttackProfiles.FirstOrDefault();
        }

        public EnemyActionProfileDefinition ResolveActionProfile(string actionId)
        {
            if (actionProfiles != null)
            {
                var authored = actionProfiles.FirstOrDefault(profile =>
                    profile != null &&
                    string.Equals(profile.ActionId, actionId, StringComparison.Ordinal));
                if (authored != null)
                {
                    return authored;
                }
            }

            return EnemyActionProfileDefaults.ResolveBossAction(BossId, actionId) ?? ActionProfiles.FirstOrDefault();
        }

        public static BossDefinition CreateRuntime(
            string bossId,
            string displayName,
            BossWorldBand band,
            BossBehaviorId behavior,
            int hp,
            string arenaId,
            string arenaName,
            float speed,
            float radius,
            float scale,
            Color color)
        {
            var definition = CreateInstance<BossDefinition>();
            definition.Configure(
                bossId,
                displayName,
                band,
                behavior,
                hp,
                speed,
                1,
                1f,
                radius,
                4.8f,
                scale,
                color,
                new BossArenaDefinition(arenaId, arenaName),
                new[]
                {
                    new BossPhaseDefinition(1f, "Phase 1", "Testing the remnant"),
                    new BossPhaseDefinition(0.5f, "Phase 2", "Roused")
                },
                DefaultAttacksFor(behavior));
            return definition;
        }

        public static EnemyIntelligenceLevel SignatureIntelligenceFor(BossBehaviorId behavior)
        {
            return behavior switch
            {
                BossBehaviorId.StoneWarden => EnemyIntelligenceLevel.Basic,
                BossBehaviorId.SplinterSaint => EnemyIntelligenceLevel.Trained,
                BossBehaviorId.GravelMaw => EnemyIntelligenceLevel.Basic,
                BossBehaviorId.CartoucheWidow => EnemyIntelligenceLevel.Cunning,
                BossBehaviorId.IronReliquary => EnemyIntelligenceLevel.Tactical,
                BossBehaviorId.MirrorHusk => EnemyIntelligenceLevel.Cunning,
                BossBehaviorId.AshComet => EnemyIntelligenceLevel.Trained,
                BossBehaviorId.ChoirOfTeeth => EnemyIntelligenceLevel.Tactical,
                BossBehaviorId.RustBishop => EnemyIntelligenceLevel.Cunning,
                BossBehaviorId.HollowStarLarva => EnemyIntelligenceLevel.Cunning,
                _ => EnemyIntelligenceLevel.Basic
            };
        }

        public static Vector3 SignatureSensesFor(BossBehaviorId behavior)
        {
            return behavior switch
            {
                BossBehaviorId.StoneWarden => new Vector3(8f, 140f, 5f),
                BossBehaviorId.SplinterSaint => new Vector3(8f, 180f, 5.5f),
                BossBehaviorId.GravelMaw => new Vector3(6.5f, 110f, 6f),
                BossBehaviorId.CartoucheWidow => new Vector3(10f, 220f, 6.5f),
                BossBehaviorId.IronReliquary => new Vector3(8.5f, 120f, 4f),
                BossBehaviorId.MirrorHusk => new Vector3(9f, 220f, 6f),
                BossBehaviorId.AshComet => new Vector3(9f, 160f, 7f),
                BossBehaviorId.ChoirOfTeeth => new Vector3(10f, 300f, 7f),
                BossBehaviorId.RustBishop => new Vector3(9.5f, 180f, 5.5f),
                BossBehaviorId.HollowStarLarva => new Vector3(0f, 0f, 9.5f),
                _ => new Vector3(8f, 160f, 5f)
            };
        }

        private static IEnumerable<BossAttackDefinition> DefaultAttacksFor(BossBehaviorId behavior)
        {
            return behavior switch
            {
                BossBehaviorId.StoneWarden => new[]
                {
                    new BossAttackDefinition(BossAttackKind.Charge, 3.2f, 0.5f, 1),
                    new BossAttackDefinition(BossAttackKind.Stomp, 4.5f, 0.55f, 2),
                    new BossAttackDefinition(BossAttackKind.RadialBurst, 5f, 0.65f, 1, 4)
                },
                BossBehaviorId.GravelMaw => new[]
                {
                    new BossAttackDefinition(BossAttackKind.BurrowSummon, 6f, 0.5f, 1, 3),
                    new BossAttackDefinition(BossAttackKind.Charge, 2.4f, 0.25f, 1)
                },
                BossBehaviorId.MirrorHusk => new[]
                {
                    new BossAttackDefinition(BossAttackKind.Split, 0.1f, 0f, 1),
                    new BossAttackDefinition(BossAttackKind.Chase, 1f, 0f, 1)
                },
                BossBehaviorId.ChoirOfTeeth => new[]
                {
                    new BossAttackDefinition(BossAttackKind.RadialBurst, 2.3f, 0.45f, 1, 12),
                    new BossAttackDefinition(BossAttackKind.DesperationBurst, 3.5f, 0.65f, 2, 16)
                },
                _ => new[]
                {
                    new BossAttackDefinition(BossAttackKind.Chase, 1f, 0f, 1),
                    new BossAttackDefinition(BossAttackKind.RadialBurst, 3f, 0.45f, 1, 6)
                }
            };
        }
    }
}
