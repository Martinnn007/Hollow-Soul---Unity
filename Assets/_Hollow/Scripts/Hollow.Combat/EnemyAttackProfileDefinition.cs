using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Combat
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Enemy Attack Profile", fileName = "EnemyAttackProfile")]
    public sealed class EnemyAttackProfileDefinition : ScriptableObject
    {
        [SerializeField] private string attackId = "enemy_attack";
        [SerializeField] private string displayName = "Enemy Attack";
        [SerializeField] private EnemyAttackRuntimeKind runtimeKind = EnemyAttackRuntimeKind.Contact;
        [SerializeField] private int damage = 1;
        [SerializeField] private float cooldownSeconds = 1f;
        [SerializeField] private float windupSeconds = 0.2f;
        [SerializeField] private float activeSeconds = 0.15f;
        [SerializeField] private float rangeMeters = 1f;
        [SerializeField] private int projectileCount = 1;
        [SerializeField] private float projectileSpeedMetersPerSecond = 5f;
        [SerializeField] private DamageChannel damageChannel = DamageChannel.Physical;
        [SerializeField] private DamageDelivery damageDelivery = DamageDelivery.Contact;
        [SerializeField] private DamageElement damageElement = DamageElement.None;
        [SerializeField] private ImpactForceClass forceClass = ImpactForceClass.Light;
        [SerializeField] private DamageThreatKind threatKind = DamageThreatKind.Light;
        [SerializeField] private float knockbackMeters = 0.3f;
        [SerializeField] private float guardKnockbackMultiplier = 0.35f;
        [SerializeField] private float recoverySeconds;
        [SerializeField] private float hitArcDegrees;
        [SerializeField] private ImpactForceClass poiseBreakThreshold = ImpactForceClass.Medium;
        [TextArea(1, 4)]
        [SerializeField] private string notes = string.Empty;

        public string AttackId => string.IsNullOrWhiteSpace(attackId) ? "enemy_attack" : attackId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? AttackId : displayName;

        public EnemyAttackRuntimeKind RuntimeKind => runtimeKind;

        public int Damage => Mathf.Max(0, damage);

        public float CooldownSeconds => Mathf.Max(0.01f, cooldownSeconds);

        public float WindupSeconds => Mathf.Max(0f, windupSeconds);

        public float ActiveSeconds => Mathf.Max(0.01f, activeSeconds);

        public float RangeMeters => Mathf.Max(0f, rangeMeters);

        public int ProjectileCount => Mathf.Max(0, projectileCount);

        public float ProjectileSpeedMetersPerSecond => Mathf.Max(0.1f, projectileSpeedMetersPerSecond);

        public DamageChannel DamageChannel => damageChannel;

        public DamageDelivery DamageDelivery => damageDelivery;

        public DamageElement DamageElement => damageElement;

        public ImpactForceClass ForceClass => forceClass;

        public DamageThreatKind ThreatKind => threatKind;

        public float KnockbackMeters => Mathf.Max(0f, knockbackMeters);

        public float GuardKnockbackMultiplier => Mathf.Clamp01(guardKnockbackMultiplier);

        public float RecoverySeconds => recoverySeconds > 0f ? recoverySeconds : DefaultRecoverySeconds(RuntimeKind, ForceClass);

        public float HitArcDegrees => hitArcDegrees > 0f ? Mathf.Clamp(hitArcDegrees, 1f, 360f) : DefaultHitArcDegrees(RuntimeKind, DamageDelivery);

        public ImpactForceClass PoiseBreakThreshold => poiseBreakThreshold;

        public string Notes => notes ?? string.Empty;

        public DamageClassification Classification => new(DamageChannel, DamageDelivery, ForceClass, DamageElement);

        public void Configure(EnemyAttackProfileSpec spec)
        {
            attackId = string.IsNullOrWhiteSpace(spec.AttackId) ? "enemy_attack" : spec.AttackId;
            displayName = string.IsNullOrWhiteSpace(spec.DisplayName) ? attackId : spec.DisplayName;
            runtimeKind = spec.RuntimeKind;
            damage = Mathf.Max(0, spec.Damage);
            cooldownSeconds = Mathf.Max(0.01f, spec.CooldownSeconds);
            windupSeconds = Mathf.Max(0f, spec.WindupSeconds);
            activeSeconds = Mathf.Max(0.01f, spec.ActiveSeconds);
            rangeMeters = Mathf.Max(0f, spec.RangeMeters);
            projectileCount = Mathf.Max(0, spec.ProjectileCount);
            projectileSpeedMetersPerSecond = Mathf.Max(0.1f, spec.ProjectileSpeedMetersPerSecond);
            damageChannel = spec.DamageChannel;
            damageDelivery = spec.DamageDelivery;
            damageElement = spec.DamageElement;
            forceClass = spec.ForceClass;
            threatKind = spec.ThreatKind;
            knockbackMeters = Mathf.Max(0f, spec.KnockbackMeters);
            guardKnockbackMultiplier = Mathf.Clamp01(spec.GuardKnockbackMultiplier);
            recoverySeconds = Mathf.Max(0.01f, spec.RecoverySeconds);
            hitArcDegrees = Mathf.Clamp(spec.HitArcDegrees, 1f, 360f);
            poiseBreakThreshold = spec.PoiseBreakThreshold;
            notes = spec.Notes ?? string.Empty;
        }

        public DamageRequest CreateDamageRequest(GameObject source, Vector3 direction, float knockbackSeconds)
        {
            return new DamageRequest(
                Damage,
                source,
                DamageFeedbackContext.Knockback(direction, KnockbackMeters, knockbackSeconds),
                ThreatKind,
                Classification,
                GuardKnockbackMultiplier);
        }

        public static EnemyAttackProfileDefinition CreateRuntime(EnemyAttackProfileSpec spec)
        {
            var profile = CreateInstance<EnemyAttackProfileDefinition>();
            profile.Configure(spec);
            return profile;
        }

        public static float DefaultRecoverySeconds(EnemyAttackRuntimeKind runtimeKind, ImpactForceClass forceClass)
        {
            return runtimeKind switch
            {
                EnemyAttackRuntimeKind.Charge => 0.24f,
                EnemyAttackRuntimeKind.MeleeLunge => (int)forceClass >= (int)ImpactForceClass.Heavy ? 0.24f : 0.16f,
                EnemyAttackRuntimeKind.Contact => 0.14f,
                EnemyAttackRuntimeKind.Area => 0.2f,
                EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile => 0.18f,
                EnemyAttackRuntimeKind.Movement => 0.18f,
                _ => 0.12f
            };
        }

        public static float DefaultHitArcDegrees(EnemyAttackRuntimeKind runtimeKind, DamageDelivery delivery)
        {
            if (delivery is DamageDelivery.Projectile or DamageDelivery.Area)
            {
                return 360f;
            }

            return runtimeKind switch
            {
                EnemyAttackRuntimeKind.Charge => 90f,
                EnemyAttackRuntimeKind.MeleeLunge => 125f,
                EnemyAttackRuntimeKind.Contact => 110f,
                EnemyAttackRuntimeKind.Area => 360f,
                _ => 120f
            };
        }
    }
}
