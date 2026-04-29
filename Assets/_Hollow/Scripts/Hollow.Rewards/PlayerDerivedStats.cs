using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Rewards
{
    public readonly struct PlayerDerivedStats
    {
        public PlayerDerivedStats(
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
            float rangedRangeBonusMeters = 0f)
        {
            MaxHealth = Mathf.Max(1, maxHealth);
            SpeedMetersPerSecond = Mathf.Max(0.1f, speedMetersPerSecond);
            Strength = Mathf.Max(0, strength);
            MaxStamina = Mathf.Max(1f, maxStamina);
            StaminaRegenPerSecond = Mathf.Max(0f, staminaRegenPerSecond);
            Defense = Mathf.Max(0, defense);
            MeleeDamageBonus = Mathf.Max(0, meleeDamageBonus);
            RangedDamageBonus = Mathf.Max(0, rangedDamageBonus);
            AttackCooldownMultiplier = attackCooldownMultiplier <= 0f ? 1f : attackCooldownMultiplier;
            MeleeRangeBonusMeters = Mathf.Max(0f, meleeRangeBonusMeters);
            RangedRangeBonusMeters = Mathf.Max(0f, rangedRangeBonusMeters);
        }

        public int MaxHealth { get; }

        public float SpeedMetersPerSecond { get; }

        public int Strength { get; }

        public float MaxStamina { get; }

        public float StaminaRegenPerSecond { get; }

        public int Defense { get; }

        public int MeleeDamageBonus { get; }

        public int RangedDamageBonus { get; }

        public float AttackCooldownMultiplier { get; }

        public float MeleeRangeBonusMeters { get; }

        public float RangedRangeBonusMeters { get; }

        public static PlayerDerivedStats FromBase(PlayerBaseStats baseStats)
        {
            return new PlayerDerivedStats(
                baseStats.MaxHealth,
                baseStats.SpeedMetersPerSecond,
                baseStats.Strength,
                baseStats.MaxStamina,
                baseStats.StaminaRegenPerSecond,
                baseStats.Defense,
                baseStats.MeleeDamageBonus,
                baseStats.RangedDamageBonus,
                baseStats.AttackCooldownMultiplier,
                baseStats.MeleeRangeBonusMeters,
                baseStats.RangedRangeBonusMeters);
        }
    }
}
