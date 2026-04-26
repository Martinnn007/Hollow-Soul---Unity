using System;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class CombatantHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private int currentHealth = 1;

        public int MaxHealth => maxHealth;

        public int CurrentHealth => currentHealth;

        public bool IsAlive => currentHealth > 0;

        public event Action<CombatantHealth> Damaged;

        public event Action<CombatantHealth> Died;

        public void Configure(int nextMaxHealth)
        {
            maxHealth = Mathf.Max(1, nextMaxHealth);
            currentHealth = maxHealth;
        }

        public bool ApplyDamage(DamageRequest request)
        {
            if (!IsAlive || request.Amount <= 0)
            {
                return false;
            }

            currentHealth = Mathf.Max(0, currentHealth - request.Amount);
            Damaged?.Invoke(this);
            if (currentHealth == 0)
            {
                Died?.Invoke(this);
            }

            return true;
        }
    }
}
