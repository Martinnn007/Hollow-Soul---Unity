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

        public WeaponAttackDefinition(AttackKind attackKind, int damage, float cooldownSeconds, float staminaCost, float rangeMeters)
        {
            this.attackKind = attackKind;
            this.damage = Mathf.Max(0, damage);
            this.cooldownSeconds = Mathf.Max(0.01f, cooldownSeconds);
            this.staminaCost = Mathf.Max(0f, staminaCost);
            this.rangeMeters = Mathf.Max(0.1f, rangeMeters);
        }

        public AttackKind AttackKind => attackKind;

        public int Damage => Mathf.Max(0, damage);

        public float CooldownSeconds => Mathf.Max(0.01f, cooldownSeconds);

        public float StaminaCost => Mathf.Max(0f, staminaCost);

        public float RangeMeters => Mathf.Max(0.1f, rangeMeters);

        public static WeaponAttackDefinition DefaultLight(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee
                ? new WeaponAttackDefinition(AttackKind.Light, 1, 0.28f, 6f, 0.95f)
                : new WeaponAttackDefinition(AttackKind.Light, 1, 0.22f, 0f, 9f);
        }

        public static WeaponAttackDefinition DefaultHeavy(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee
                ? new WeaponAttackDefinition(AttackKind.Heavy, 2, 0.55f, 18f, 1.15f)
                : new WeaponAttackDefinition(AttackKind.Heavy, 2, 0.48f, 12f, 9f);
        }
    }
}
