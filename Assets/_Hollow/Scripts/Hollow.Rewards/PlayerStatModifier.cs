using System;
using Hollow.Persistence;
using UnityEngine;

namespace Hollow.Rewards
{
    [Serializable]
    public struct PlayerStatModifier
    {
        public string sourceId;
        public int maxHealth;
        public float speed;
        public int strength;
        public float maxStamina;
        public float staminaRegen;
        public int defense;
        public int meleeDamage;
        public int rangedDamage;
        public float attackCooldownMultiplier;

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

        public PlayerStatModifierSaveState ToSaveState()
        {
            return new PlayerStatModifierSaveState
            {
                sourceId = sourceId ?? string.Empty,
                maxHealth = maxHealth,
                speed = speed,
                strength = strength,
                maxStamina = maxStamina,
                staminaRegen = staminaRegen,
                defense = defense,
                meleeDamage = meleeDamage,
                rangedDamage = rangedDamage,
                attackCooldownMultiplier = attackCooldownMultiplier
            };
        }

        public static PlayerStatModifier FromSaveState(PlayerStatModifierSaveState saveState)
        {
            return saveState == null
                ? default
                : new PlayerStatModifier
                {
                    sourceId = saveState.sourceId ?? string.Empty,
                    maxHealth = saveState.maxHealth,
                    speed = saveState.speed,
                    strength = saveState.strength,
                    maxStamina = saveState.maxStamina,
                    staminaRegen = saveState.staminaRegen,
                    defense = saveState.defense,
                    meleeDamage = saveState.meleeDamage,
                    rangedDamage = saveState.rangedDamage,
                    attackCooldownMultiplier = saveState.attackCooldownMultiplier
                };
        }
    }
}
