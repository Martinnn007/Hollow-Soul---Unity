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
            float knockbackMeters)
        {
            this.attackKind = attackKind;
            this.damage = Mathf.Max(0, damage);
            this.cooldownSeconds = Mathf.Max(0.01f, cooldownSeconds);
            this.staminaCost = Mathf.Max(0f, staminaCost);
            this.rangeMeters = Mathf.Max(0.1f, rangeMeters);
            this.impactForceClass = impactForceClass;
            this.knockbackMeters = Mathf.Max(0f, knockbackMeters);
        }

        public AttackKind AttackKind => attackKind;

        public int Damage => Mathf.Max(0, damage);

        public float CooldownSeconds => Mathf.Max(0.01f, cooldownSeconds);

        public float StaminaCost => Mathf.Max(0f, staminaCost);

        public float RangeMeters => Mathf.Max(0.1f, rangeMeters);

        public ImpactForceClass ImpactForceClass => impactForceClass;

        public float KnockbackMeters => knockbackMeters > 0f ? knockbackMeters : DefaultKnockbackMeters(attackKind);

        public static WeaponAttackDefinition DefaultLight(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee
                ? new WeaponAttackDefinition(AttackKind.Light, 1, 0.67f, 6f, 0.95f, ImpactForceClass.Medium, 0.55f)
                : new WeaponAttackDefinition(AttackKind.Light, 1, 1f, 0f, 6f, ImpactForceClass.Light, 0.3f);
        }

        public static WeaponAttackDefinition DefaultHeavy(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee
                ? new WeaponAttackDefinition(AttackKind.Heavy, 2, 3.5f, 40f, 1.15f, ImpactForceClass.Heavy, 0.85f)
                : new WeaponAttackDefinition(AttackKind.Heavy, 2, 10f, 35f, 6f, ImpactForceClass.Medium, 0.55f);
        }

        private static float DefaultKnockbackMeters(AttackKind attackKind)
        {
            return attackKind == AttackKind.Heavy ? 0.55f : 0.3f;
        }
    }
}
