using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public struct PlayerBaseStats
    {
        [SerializeField] private int maxHealth;
        [SerializeField] private float speedMetersPerSecond;
        [SerializeField] private int strength;
        [SerializeField] private float maxStamina;
        [SerializeField] private float staminaRegenPerSecond;
        [SerializeField] private int defense;
        [SerializeField] private int meleeDamageBonus;
        [SerializeField] private int rangedDamageBonus;
        [SerializeField] private int stability;
        [SerializeField] private float attackCooldownMultiplier;
        [SerializeField] private float meleeRangeBonusMeters;
        [SerializeField] private float rangedRangeBonusMeters;

        public PlayerBaseStats(
            int maxHealth,
            float speedMetersPerSecond,
            int strength,
            float maxStamina,
            float staminaRegenPerSecond,
            int defense,
            int meleeDamageBonus,
            int rangedDamageBonus,
            float attackCooldownMultiplier,
            float meleeRangeBonusMeters = 0f,
            float rangedRangeBonusMeters = 0f,
            int stability = 1)
        {
            this.maxHealth = Mathf.Max(1, maxHealth);
            this.speedMetersPerSecond = Mathf.Max(0.1f, speedMetersPerSecond);
            this.strength = Mathf.Max(0, strength);
            this.maxStamina = Mathf.Max(1f, maxStamina);
            this.staminaRegenPerSecond = Mathf.Max(0f, staminaRegenPerSecond);
            this.defense = Mathf.Max(0, defense);
            this.meleeDamageBonus = Mathf.Max(0, meleeDamageBonus);
            this.rangedDamageBonus = Mathf.Max(0, rangedDamageBonus);
            this.stability = Mathf.Max(0, stability);
            this.attackCooldownMultiplier = attackCooldownMultiplier <= 0f ? 1f : attackCooldownMultiplier;
            this.meleeRangeBonusMeters = Mathf.Max(0f, meleeRangeBonusMeters);
            this.rangedRangeBonusMeters = Mathf.Max(0f, rangedRangeBonusMeters);
        }

        public int MaxHealth => Mathf.Max(1, maxHealth);

        public float SpeedMetersPerSecond => Mathf.Max(0.1f, speedMetersPerSecond);

        public int Strength => Mathf.Max(0, strength);

        public float MaxStamina => Mathf.Max(1f, maxStamina);

        public float StaminaRegenPerSecond => Mathf.Max(0f, staminaRegenPerSecond);

        public int Defense => Mathf.Max(0, defense);

        public int MeleeDamageBonus => Mathf.Max(0, meleeDamageBonus);

        public int RangedDamageBonus => Mathf.Max(0, rangedDamageBonus);

        public int Stability => stability <= 0 ? 1 : stability;

        public float AttackCooldownMultiplier => attackCooldownMultiplier <= 0f ? 1f : attackCooldownMultiplier;

        public float MeleeRangeBonusMeters => Mathf.Max(0f, meleeRangeBonusMeters);

        public float RangedRangeBonusMeters => Mathf.Max(0f, rangedRangeBonusMeters);

        public bool IsConfigured => maxHealth > 0 && speedMetersPerSecond > 0f && maxStamina > 0f;

        public static PlayerBaseStats Default => new(
            maxHealth: 6,
            speedMetersPerSecond: 4f,
            strength: 1,
            maxStamina: 100f,
            staminaRegenPerSecond: 18f,
            defense: 0,
            meleeDamageBonus: 0,
            rangedDamageBonus: 0,
            stability: 1,
            attackCooldownMultiplier: 1f,
            meleeRangeBonusMeters: 0f,
            rangedRangeBonusMeters: 0f);
    }
}
