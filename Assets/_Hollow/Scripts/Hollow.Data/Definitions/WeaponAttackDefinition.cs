using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public struct WeaponAttackDefinition
    {
        [SerializeField] private AttackKind attackKind;
        [SerializeField] private int damage;
        [SerializeField] private float cooldownSeconds;
        [SerializeField] private float staminaCost;
        [SerializeField] private float rangeMeters;
        [SerializeField] private ImpactForceClass impactForceClass;
        [SerializeField] private float knockbackMeters;
        [SerializeField] private float windupSeconds;
        [SerializeField] private float activeSeconds;
        [SerializeField] private float recoverySeconds;
        [SerializeField] private float hitArcDegrees;
        [SerializeField] private float requiredDrawSeconds;

        public WeaponAttackDefinition(AttackKind attackKind, int damage, float cooldownSeconds, float staminaCost, float rangeMeters)
            : this(attackKind, damage, cooldownSeconds, staminaCost, rangeMeters, ImpactForceClass.Light, attackKind == AttackKind.Heavy ? 0.55f : 0.3f)
        {
        }

        public WeaponAttackDefinition(
            AttackKind attackKind,
            int damage,
            float cooldownSeconds,
            float staminaCost,
            float rangeMeters,
            ImpactForceClass impactForceClass,
            float knockbackMeters,
            float windupSeconds = -1f,
            float activeSeconds = -1f,
            float recoverySeconds = -1f,
            float hitArcDegrees = -1f,
            float requiredDrawSeconds = 0f)
        {
            this.attackKind = attackKind;
            this.damage = Mathf.Max(0, damage);
            this.cooldownSeconds = Mathf.Max(0.01f, cooldownSeconds);
            this.staminaCost = Mathf.Max(0f, staminaCost);
            this.rangeMeters = Mathf.Max(0.1f, rangeMeters);
            this.impactForceClass = impactForceClass;
            this.knockbackMeters = Mathf.Max(0f, knockbackMeters);
            this.windupSeconds = windupSeconds >= 0f ? Mathf.Max(0f, windupSeconds) : DefaultWindupSeconds(attackKind, rangeMeters);
            this.activeSeconds = activeSeconds >= 0f ? Mathf.Max(0.01f, activeSeconds) : DefaultActiveSeconds(attackKind, rangeMeters);
            this.recoverySeconds = recoverySeconds >= 0f ? Mathf.Max(0.01f, recoverySeconds) : DefaultRecoverySeconds(attackKind, rangeMeters);
            this.hitArcDegrees = hitArcDegrees >= 0f ? Mathf.Clamp(hitArcDegrees, 1f, 360f) : DefaultHitArcDegrees(attackKind, rangeMeters);
            this.requiredDrawSeconds = Mathf.Max(0f, requiredDrawSeconds);
        }

        public AttackKind AttackKind => attackKind;

        public int Damage => Mathf.Max(0, damage);

        public float CooldownSeconds => Mathf.Max(0.01f, cooldownSeconds);

        public float StaminaCost => Mathf.Max(0f, staminaCost);

        public float RangeMeters => Mathf.Max(0.1f, rangeMeters);

        public ImpactForceClass ImpactForceClass => impactForceClass;

        public float KnockbackMeters => knockbackMeters > 0f ? knockbackMeters : DefaultKnockbackMeters(attackKind);

        public float WindupSeconds => windupSeconds > 0f ? windupSeconds : DefaultWindupSeconds(attackKind, rangeMeters);

        public float ActiveSeconds => activeSeconds > 0f ? activeSeconds : DefaultActiveSeconds(attackKind, rangeMeters);

        public float RecoverySeconds => recoverySeconds > 0f ? recoverySeconds : DefaultRecoverySeconds(attackKind, rangeMeters);

        public float HitArcDegrees => hitArcDegrees > 0f ? Mathf.Clamp(hitArcDegrees, 1f, 360f) : DefaultHitArcDegrees(attackKind, rangeMeters);

        public float RequiredDrawSeconds => Mathf.Max(0f, requiredDrawSeconds);

        public static WeaponAttackDefinition DefaultLight(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee
                ? new WeaponAttackDefinition(AttackKind.Light, 1, 0.67f, 14f, 0.95f, ImpactForceClass.Medium, 0.55f, 0.06f, 0.08f, 0.1f, 115f)
                : new WeaponAttackDefinition(AttackKind.Light, 1, 0.5f, 6f, 6.5f, ImpactForceClass.Light, 0.32f, 0.01f, 0.03f, 0.16f, 1f);
        }

        public static WeaponAttackDefinition DefaultHeavy(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee
                ? new WeaponAttackDefinition(AttackKind.Heavy, 2, 3.5f, 42f, 1.15f, ImpactForceClass.Heavy, 0.85f, 0.22f, 0.14f, 0.34f, 135f)
                : new WeaponAttackDefinition(AttackKind.Heavy, 2, 8f, 34f, 6.75f, ImpactForceClass.Medium, 0.55f, 0.01f, 0.04f, 0.32f, 1f);
        }

        private static float DefaultKnockbackMeters(AttackKind attackKind)
        {
            return attackKind == AttackKind.Heavy ? 0.55f : 0.3f;
        }

        private static float DefaultWindupSeconds(AttackKind attackKind, float rangeMeters)
        {
            var isMelee = rangeMeters <= 2.5f;
            if (attackKind == AttackKind.Heavy)
            {
                return isMelee ? 0.22f : 0.28f;
            }

            return 0.06f;
        }

        private static float DefaultActiveSeconds(AttackKind attackKind, float rangeMeters)
        {
            var isMelee = rangeMeters <= 2.5f;
            if (attackKind == AttackKind.Heavy)
            {
                return isMelee ? 0.14f : 0.04f;
            }

            return isMelee ? 0.08f : 0.03f;
        }

        private static float DefaultRecoverySeconds(AttackKind attackKind, float rangeMeters)
        {
            var isMelee = rangeMeters <= 2.5f;
            if (attackKind == AttackKind.Heavy)
            {
                return isMelee ? 0.34f : 0.36f;
            }

            return isMelee ? 0.1f : 0.08f;
        }

        private static float DefaultHitArcDegrees(AttackKind attackKind, float rangeMeters)
        {
            if (rangeMeters > 2.5f)
            {
                return 1f;
            }

            return attackKind == AttackKind.Heavy ? 135f : 115f;
        }
    }
}
