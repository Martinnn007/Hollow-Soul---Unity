using Hollow.Data.Definitions;

namespace Hollow.Combat
{
    public readonly struct EnemyAttackProfileSpec
    {
        public EnemyAttackProfileSpec(
            string ownerId,
            bool isBoss,
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
            DamageChannel damageChannel,
            DamageDelivery damageDelivery,
            DamageElement damageElement,
            ImpactForceClass forceClass,
            DamageThreatKind threatKind,
            float knockbackMeters,
            float guardKnockbackMultiplier,
            string notes,
            float recoverySeconds = -1f,
            float hitArcDegrees = -1f,
            ImpactForceClass poiseBreakThreshold = ImpactForceClass.Medium)
        {
            OwnerId = ownerId ?? string.Empty;
            IsBoss = isBoss;
            AttackId = attackId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            RuntimeKind = runtimeKind;
            Damage = damage;
            CooldownSeconds = cooldownSeconds;
            WindupSeconds = windupSeconds;
            ActiveSeconds = activeSeconds;
            RangeMeters = rangeMeters;
            ProjectileCount = projectileCount;
            ProjectileSpeedMetersPerSecond = projectileSpeedMetersPerSecond;
            DamageChannel = damageChannel;
            DamageDelivery = damageDelivery;
            DamageElement = damageElement;
            ForceClass = forceClass;
            ThreatKind = threatKind;
            KnockbackMeters = knockbackMeters;
            GuardKnockbackMultiplier = guardKnockbackMultiplier;
            Notes = notes ?? string.Empty;
            RecoverySeconds = recoverySeconds >= 0f
                ? recoverySeconds
                : EnemyAttackProfileDefinition.DefaultRecoverySeconds(runtimeKind, forceClass);
            HitArcDegrees = hitArcDegrees >= 0f
                ? hitArcDegrees
                : EnemyAttackProfileDefinition.DefaultHitArcDegrees(runtimeKind, damageDelivery);
            PoiseBreakThreshold = poiseBreakThreshold;
        }

        public string OwnerId { get; }

        public bool IsBoss { get; }

        public string AttackId { get; }

        public string DisplayName { get; }

        public EnemyAttackRuntimeKind RuntimeKind { get; }

        public int Damage { get; }

        public float CooldownSeconds { get; }

        public float WindupSeconds { get; }

        public float ActiveSeconds { get; }

        public float RangeMeters { get; }

        public int ProjectileCount { get; }

        public float ProjectileSpeedMetersPerSecond { get; }

        public DamageChannel DamageChannel { get; }

        public DamageDelivery DamageDelivery { get; }

        public DamageElement DamageElement { get; }

        public ImpactForceClass ForceClass { get; }

        public DamageThreatKind ThreatKind { get; }

        public float KnockbackMeters { get; }

        public float GuardKnockbackMultiplier { get; }

        public string Notes { get; }

        public float RecoverySeconds { get; }

        public float HitArcDegrees { get; }

        public ImpactForceClass PoiseBreakThreshold { get; }

        public string AssetName => $"{(IsBoss ? "Boss" : "Enemy")}_{OwnerId}_{AttackId}.asset";
    }
}
