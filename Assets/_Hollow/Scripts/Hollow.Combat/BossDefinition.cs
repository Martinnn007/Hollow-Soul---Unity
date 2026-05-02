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
        [SerializeField] private float radiusMeters = 0.65f;
        [SerializeField] private float projectileSpeedMetersPerSecond = 4.8f;
        [SerializeField] private float visualScale = 2f;
        [SerializeField] private EnemyBodyClass bodyClass = EnemyBodyClass.Massive;
        [SerializeField] private Color debugColor = new(0.42f, 0.34f, 0.28f, 1f);
        [SerializeField] private BossArenaDefinition arena = new("boss_arena_broken_gateyard", "Broken Gateyard");
        [SerializeField] private List<BossPhaseDefinition> phases = new();
        [SerializeField] private List<BossAttackDefinition> attacks = new();

        public string BossId => string.IsNullOrWhiteSpace(bossId) ? "stone_warden" : bossId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? BossId : displayName;

        public BossWorldBand WorldBand => worldBand;

        public BossBehaviorId BehaviorId => behaviorId;

        public int MaxHealth => Mathf.Clamp(maxHealth, 20, 50);

        public float SpeedMetersPerSecond => Mathf.Max(0.05f, speedMetersPerSecond);

        public int ContactDamage => Mathf.Clamp(contactDamage, 1, 2);

        public float ContactCooldownSeconds => Mathf.Max(0.2f, contactCooldownSeconds);

        public float RadiusMeters => Mathf.Max(0.25f, radiusMeters);

        public float ProjectileSpeedMetersPerSecond => Mathf.Max(0.1f, projectileSpeedMetersPerSecond);

        public float VisualScale => Mathf.Clamp(visualScale, 1f, 3.5f);

        public EnemyBodyClass BodyClass => bodyClass;

        public Color DebugColor => debugColor;

        public BossArenaDefinition Arena => arena ?? new BossArenaDefinition("boss_arena_broken_gateyard", "Broken Gateyard");

        public IReadOnlyList<BossPhaseDefinition> Phases => phases;

        public IReadOnlyList<BossAttackDefinition> Attacks => attacks;

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
            bossId = string.IsNullOrWhiteSpace(nextBossId) ? "boss" : nextBossId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? bossId : nextDisplayName;
            worldBand = nextWorldBand;
            behaviorId = nextBehaviorId;
            maxHealth = Mathf.Clamp(nextMaxHealth, 20, 50);
            speedMetersPerSecond = Mathf.Max(0.05f, nextSpeedMetersPerSecond);
            contactDamage = Mathf.Clamp(nextContactDamage, 1, 2);
            contactCooldownSeconds = Mathf.Max(0.2f, nextContactCooldownSeconds);
            radiusMeters = Mathf.Max(0.25f, nextRadiusMeters);
            projectileSpeedMetersPerSecond = Mathf.Max(0.1f, nextProjectileSpeedMetersPerSecond);
            visualScale = Mathf.Clamp(nextVisualScale, 1f, 3.5f);
            bodyClass = nextBodyClass;
            debugColor = nextDebugColor;
            arena = nextArena ?? new BossArenaDefinition("boss_arena_broken_gateyard", "Broken Gateyard");
            phases = nextPhases?.Where(phase => phase != null).OrderByDescending(phase => phase.healthThreshold01).ToList() ?? new List<BossPhaseDefinition>();
            attacks = nextAttacks?.Where(attack => attack != null).ToList() ?? new List<BossAttackDefinition>();
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
