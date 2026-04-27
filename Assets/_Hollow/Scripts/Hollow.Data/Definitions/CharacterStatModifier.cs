using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public struct CharacterStatModifier
    {
        [SerializeField] private int maxHealth;
        [SerializeField] private float speed;
        [SerializeField] private int strength;
        [SerializeField] private float maxStamina;
        [SerializeField] private float staminaRegen;
        [SerializeField] private int defense;
        [SerializeField] private int meleeDamage;
        [SerializeField] private int rangedDamage;
        [SerializeField] private float attackCooldownMultiplier;

        public CharacterStatModifier(
            int maxHealth = 0,
            float speed = 0f,
            int strength = 0,
            float maxStamina = 0f,
            float staminaRegen = 0f,
            int defense = 0,
            int meleeDamage = 0,
            int rangedDamage = 0,
            float attackCooldownMultiplier = 0f)
        {
            this.maxHealth = maxHealth;
            this.speed = speed;
            this.strength = strength;
            this.maxStamina = maxStamina;
            this.staminaRegen = staminaRegen;
            this.defense = defense;
            this.meleeDamage = meleeDamage;
            this.rangedDamage = rangedDamage;
            this.attackCooldownMultiplier = attackCooldownMultiplier;
        }

        public int MaxHealth => maxHealth;

        public float Speed => speed;

        public int Strength => strength;

        public float MaxStamina => maxStamina;

        public float StaminaRegen => staminaRegen;

        public int Defense => defense;

        public int MeleeDamage => meleeDamage;

        public int RangedDamage => rangedDamage;

        public float AttackCooldownMultiplier => attackCooldownMultiplier;

        public bool IsEmpty =>
            maxHealth == 0 &&
            Mathf.Approximately(speed, 0f) &&
            strength == 0 &&
            Mathf.Approximately(maxStamina, 0f) &&
            Mathf.Approximately(staminaRegen, 0f) &&
            defense == 0 &&
            meleeDamage == 0 &&
            rangedDamage == 0 &&
            Mathf.Approximately(attackCooldownMultiplier, 0f);
    }
}
